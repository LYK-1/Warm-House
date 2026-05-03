using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class PlayerCartContaol : MonoBehaviour
{
    private Rigidbody m_Rigidbody;

    public WheelCollider[] WheelColliders;
    public GameObject[] Wheels;
    public float DriveTorque = 100f;
    public float BrakeTorque = 300f;
    public float MaxSpeed = 160f;
    public float currentSpeed;

    private float currentSteerAngle = 0f;
    private float m_forwardTorque;

    public float m_gas;
    public float m_brake;
    public Vector2 m_steering;
    public bool m_reverse = false;
    private Vector2 m_drift;

    public float DownForce = 100f;
    public float SteerAngle = 30f;
    public bool BrakeAssist = false;
    public float DriftSpeedLossPerSecond = 30f;
    public float DriftMinSpeed = 20f;

    private WheelFrictionCurve m_curve;
    private bool m_curveChanae = false;

    private bool m_grounded;
    private bool isResetting = false;
    private Coroutine constraintCoroutine;

    public Text speedText;
    public float[] m_driveTorqueGear = new float[5];
    private float m_gearNumber = 0f;

    [Header("Debug")]
    [SerializeField] private bool CollisionDebugLogging = true;
    [SerializeField] private float CollisionStayLogInterval = 0.5f;
    private Collider m_lastCollisionStayCollider;
    private float m_lastCollisionStayLogTime;
    private float m_lastHazardContactTime = -999f;

    [Header("Reset")]
    [SerializeField] private float SafePoseRecordInterval = 0.75f;
    [SerializeField] private float SafePoseMinDistance = 8f;
    [SerializeField] private int SafePoseHistoryLimit = 12;
    [SerializeField] private float SafePoseMinUprightDot = 0.9f;
    [SerializeField] private float SafePoseRecentHazardCooldown = 1f;
    [SerializeField] private float RespawnGroundProbeHeight = 4f;
    [SerializeField] private float RespawnGroundProbeDepth = 10f;
    [SerializeField] private float RespawnGroundLift = 0.35f;
    [SerializeField] private float RespawnVehicleCheckHeight = 1.2f;
    [SerializeField] private float RespawnVehicleCheckRadius = 1.0f;
    [SerializeField] private float RespawnMinGroundNormalDot = 0.65f;

    private readonly List<RespawnPose> m_safeRespawnHistory = new List<RespawnPose>();
    private Vector3 m_lastSafePosePosition;
    private Quaternion m_lastSafePoseRotation;
    private float m_lastSafePoseRecordTime;
    private RespawnPose m_spawnPose;

    void Start()
    {
        m_Rigidbody = GetComponent<Rigidbody>();
        m_Rigidbody.centerOfMass = new Vector3(0f, -0.5f, 0f);

        m_spawnPose = CreateRespawnPose(transform.position, transform.rotation);
        RecordSafePose(m_spawnPose, true);

        for (int i = 0; i < m_driveTorqueGear.Length; i++)
        {
            m_gearNumber++;
            m_driveTorqueGear[i] = DriveTorque + (MaxSpeed / 10f * m_gearNumber);
        }
    }

    void FixedUpdate()
    {
        if (isResetting)
        {
            return;
        }

        Drive(m_gas, m_brake, m_steering, m_drift);
        AddDownForce();
        TrackSafeRespawnPose();
    }

    void Update()
    {
        currentSpeed = m_Rigidbody.linearVelocity.magnitude * 3.6f;

        if (speedText != null)
        {
            speedText.text = currentSpeed.ToString("F0");
        }
    }

    private void Drive(float acceleration, float brake, Vector2 steer, Vector2 drift)
    {
        if (m_Rigidbody.isKinematic)
        {
            return;
        }

        float speed = m_Rigidbody.linearVelocity.magnitude;

        float speedFactor = Mathf.Clamp01(1f - (speed / MaxSpeed));
        speedFactor = Mathf.Max(speedFactor, 0.2f);

        m_forwardTorque = (m_reverse ? -1f : 1f) * acceleration * DriveTorque * speedFactor;
        brake *= BrakeTorque;

        float steerFactor = Mathf.Clamp01(1f - (speed / (MaxSpeed * 0.7f)));
        float targetSteer = steer.x * SteerAngle * steerFactor;

        float returnSpeed = 150f;
        float steerSpeed = 100f;

        if (Mathf.Abs(targetSteer) < 0.01f)
        {
            currentSteerAngle = Mathf.MoveTowards(currentSteerAngle, 0f, returnSpeed * Time.fixedDeltaTime);

            Vector3 av = m_Rigidbody.angularVelocity;
            av.y *= 0.8f;
            m_Rigidbody.angularVelocity = av;
        }
        else
        {
            currentSteerAngle = Mathf.MoveTowards(currentSteerAngle, targetSteer, steerSpeed * Time.fixedDeltaTime);

            Vector3 av = m_Rigidbody.angularVelocity;
            av.y *= 0.85f;
            m_Rigidbody.angularVelocity = av;
        }

        for (int i = 0; i < Wheels.Length; i++)
        {
            Vector3 wheelPosition;
            Quaternion wheelRotation;
            WheelColliders[i].GetWorldPose(out wheelPosition, out wheelRotation);
            Wheels[i].transform.position = wheelPosition;
            Wheels[i].transform.rotation = wheelRotation;
        }

        for (int i = 0; i < WheelColliders.Length; i++)
        {
            WheelColliders[i].motorTorque = m_forwardTorque;
            WheelColliders[i].brakeTorque = brake;

            if (i < 2)
            {
                WheelColliders[i].steerAngle = currentSteerAngle;
            }
        }

        if (drift.magnitude > 0.1f)
        {
            if (!m_curveChanae)
            {
                m_curveChanae = true;
                SteerAngle = 40f;

                for (int i = 2; i < WheelColliders.Length; i++)
                {
                    m_curve = WheelColliders[i].sidewaysFriction;
                    m_curve.stiffness = 0.5f;
                    m_curve.extremumSlip = 8.0f;
                    WheelColliders[i].sidewaysFriction = m_curve;
                }
            }

            ApplyDriftSpeedPenalty();
        }
        else if (m_curveChanae)
        {
            m_curveChanae = false;
            SteerAngle = 30f;

            for (int i = 2; i < WheelColliders.Length; i++)
            {
                m_curve = WheelColliders[i].sidewaysFriction;
                m_curve.stiffness = 1.0f;
                m_curve.extremumSlip = 0.2f;
                WheelColliders[i].sidewaysFriction = m_curve;
            }
        }
    }

    private void ApplyDriftSpeedPenalty()
    {
        Vector3 planarVelocity = new Vector3(m_Rigidbody.linearVelocity.x, 0f, m_Rigidbody.linearVelocity.z);
        float planarSpeed = planarVelocity.magnitude;

        if (planarSpeed <= 0.01f)
        {
            return;
        }

        float driftLossMetersPerSecond = DriftSpeedLossPerSecond / 3.6f;
        float minDriftSpeedMetersPerSecond = DriftMinSpeed / 3.6f;
        float reducedSpeed = Mathf.Max(minDriftSpeedMetersPerSecond, planarSpeed - driftLossMetersPerSecond * Time.fixedDeltaTime);

        if (reducedSpeed >= planarSpeed)
        {
            return;
        }

        Vector3 reducedPlanarVelocity = planarVelocity.normalized * reducedSpeed;
        m_Rigidbody.linearVelocity = new Vector3(reducedPlanarVelocity.x, m_Rigidbody.linearVelocity.y, reducedPlanarVelocity.z);
    }

    private void AddDownForce()
    {
        bool anyGrounded = false;

        for (int i = 2; i < WheelColliders.Length; ++i)
        {
            WheelHit wheelhit;
            WheelColliders[i].GetGroundHit(out wheelhit);
            if (wheelhit.normal != Vector3.zero)
            {
                anyGrounded = true;
            }
        }

        if (!anyGrounded && !isResetting && constraintCoroutine == null)
        {
            constraintCoroutine = StartCoroutine(SetConstraints());
        }

        m_grounded = anyGrounded;

        if (m_grounded)
        {
            m_Rigidbody.AddForce(Vector3.down * DownForce * m_Rigidbody.linearVelocity.magnitude);
        }
    }

    public IEnumerator SetConstraints()
    {
        yield return new WaitForSeconds(0.1f);
        m_Rigidbody.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        yield return new WaitForSeconds(0.3f);
        m_Rigidbody.constraints = RigidbodyConstraints.None;
        constraintCoroutine = null;
    }

    public void OnAccelerate(InputValue value)
    {
        m_gas = value.isPressed ? 1f : 0f;
    }

    public void OnBrake(InputValue button)
    {
        if (button.isPressed)
        {
            m_brake = 1f;
            if (BrakeAssist)
            {
                m_Rigidbody.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
            }
        }
        else
        {
            m_brake = 0f;
            if (BrakeAssist)
            {
                m_Rigidbody.constraints = RigidbodyConstraints.None;
            }
        }
    }

    public void OnDrift(InputValue value)
    {
        m_drift = value.Get<Vector2>();
    }

    public void OnReverse(InputValue button)
    {
        m_reverse = !m_reverse;
        GetComponent<KartSounds>().IsReversing = m_reverse;
    }

    public void OnSteering(InputValue value)
    {
        m_steering = value.Get<Vector2>();
    }

    public void OnReset(InputValue button)
    {
        if (!button.isPressed)
        {
            return;
        }

        if (Vector3.Dot(transform.up, Vector3.up) < 0.5f)
        {
            StartCoroutine(ResetCar());
        }
    }

    private IEnumerator ResetCar()
    {
        isResetting = true;

        if (constraintCoroutine != null)
        {
            StopCoroutine(constraintCoroutine);
            constraintCoroutine = null;
        }

        m_Rigidbody.constraints = RigidbodyConstraints.None;
        m_Rigidbody.linearVelocity = Vector3.zero;
        m_Rigidbody.angularVelocity = Vector3.zero;
        m_Rigidbody.isKinematic = true;

        m_gas = 0f;
        m_brake = 1f;
        m_steering = Vector2.zero;
        m_drift = Vector2.zero;

        RespawnPose respawnPose;
        if (!TryGetSafeRespawnPose(out respawnPose))
        {
            respawnPose = new RespawnPose(m_spawnPose.Position + Vector3.up * RespawnGroundLift, m_spawnPose.Rotation);
        }

        transform.SetPositionAndRotation(respawnPose.Position, respawnPose.Rotation);
        Physics.SyncTransforms();

        yield return new WaitForSeconds(0.2f);

        m_Rigidbody.isKinematic = false;
        m_Rigidbody.linearVelocity = Vector3.zero;
        m_Rigidbody.angularVelocity = Vector3.zero;

        RecordSafePose(CreateRespawnPose(transform.position, transform.rotation), true);
        m_brake = 0f;
        isResetting = false;
    }

    public void SetGears()
    {
        if (currentSpeed < 25f)
        {
            DriveTorque = m_driveTorqueGear[0];
            m_Rigidbody.angularDamping = 0.05f;
        }
        if (currentSpeed < 50f && currentSpeed > 24f)
        {
            DriveTorque = m_driveTorqueGear[1];
            m_Rigidbody.angularDamping = 0.03f;
        }
        if (currentSpeed < 80f && currentSpeed > 49f)
        {
            DriveTorque = m_driveTorqueGear[2];
            m_Rigidbody.angularDamping = 0.02f;
        }
        if (currentSpeed < 120f && currentSpeed > 79f)
        {
            DriveTorque = m_driveTorqueGear[3];
            m_Rigidbody.angularDamping = 0.01f;
        }
        if (currentSpeed > 119f && currentSpeed < MaxSpeed)
        {
            DriveTorque = m_driveTorqueGear[4];
            m_Rigidbody.angularDamping = 0f;
        }
        if (currentSpeed >= MaxSpeed)
        {
            DriveTorque = 0f;
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        MarkHazardContact(collision.collider);

        if (!CollisionDebugLogging || collision.collider == null)
        {
            return;
        }

        LogCollision("OnCollisionEnter", collision);
    }

    private void OnCollisionStay(Collision collision)
    {
        MarkHazardContact(collision.collider);

        if (!CollisionDebugLogging || collision.collider == null)
        {
            return;
        }

        if (!ShouldLogCollisionStay(collision.collider))
        {
            return;
        }

        LogCollision("OnCollisionStay", collision);
    }

    private void OnCollisionExit(Collision collision)
    {
        if (collision.collider == m_lastCollisionStayCollider)
        {
            m_lastCollisionStayCollider = null;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        MarkHazardContact(other);

        if (!CollisionDebugLogging || other == null)
        {
            return;
        }

        LogTrigger("OnTriggerEnter", other);
    }

    private void OnTriggerExit(Collider other)
    {
        if (!CollisionDebugLogging || other == null)
        {
            return;
        }

        LogTrigger("OnTriggerExit", other);
    }

    private bool ShouldLogCollisionStay(Collider other)
    {
        float now = Time.unscaledTime;
        if (other != m_lastCollisionStayCollider || now - m_lastCollisionStayLogTime >= CollisionStayLogInterval)
        {
            m_lastCollisionStayCollider = other;
            m_lastCollisionStayLogTime = now;
            return true;
        }

        return false;
    }

    private void LogCollision(string eventName, Collision collision)
    {
        ContactPoint contact = default;
        Collider selfCollider = null;
        bool hasContact = collision.contactCount > 0;
        if (hasContact)
        {
            contact = collision.GetContact(0);
            selfCollider = contact.thisCollider;
        }

        string message =
            $"[CollisionDebug][{name}] {eventName} other={DescribeCollider(collision.collider)} " +
            $"self={DescribeCollider(selfCollider)} relativeVelocity={collision.relativeVelocity} impulse={collision.impulse}";

        if (hasContact)
        {
            message += $" point={contact.point} normal={contact.normal}";
        }

        Debug.Log(message, collision.collider != null ? collision.collider.gameObject : gameObject);
    }

    private void LogTrigger(string eventName, Collider other)
    {
        Vector3 closestPoint = other.ClosestPoint(transform.position);
        string message =
            $"[CollisionDebug][{name}] {eventName} other={DescribeCollider(other)} " +
            $"playerPosition={transform.position} closestPoint={closestPoint}";

        Debug.Log(message, other.gameObject);
    }

    private void MarkHazardContact(Collider other)
    {
        if (other == null || IsSelfCollider(other) || other is TerrainCollider)
        {
            return;
        }

        m_lastHazardContactTime = Time.time;
    }

    private void TrackSafeRespawnPose()
    {
        if (!m_grounded)
        {
            return;
        }

        if (Vector3.Dot(transform.up, Vector3.up) < SafePoseMinUprightDot)
        {
            return;
        }

        if (Time.time - m_lastHazardContactTime < SafePoseRecentHazardCooldown)
        {
            return;
        }

        if (Time.time - m_lastSafePoseRecordTime < SafePoseRecordInterval)
        {
            return;
        }

        RespawnPose candidate = CreateRespawnPose(transform.position, transform.rotation);
        if (m_safeRespawnHistory.Count > 0)
        {
            float distance = Vector3.Distance(candidate.Position, m_lastSafePosePosition);
            float angle = Quaternion.Angle(candidate.Rotation, m_lastSafePoseRotation);
            if (distance < SafePoseMinDistance && angle < 8f)
            {
                return;
            }
        }

        RespawnPose groundedPose;
        if (!TryProjectRespawnToGround(candidate, out groundedPose))
        {
            return;
        }

        if (IsRespawnPoseBlocked(groundedPose.Position))
        {
            return;
        }

        RecordSafePose(groundedPose, false);
    }

    private RespawnPose CreateRespawnPose(Vector3 position, Quaternion rotation)
    {
        Vector3 euler = rotation.eulerAngles;
        return new RespawnPose(position, Quaternion.Euler(0f, euler.y, 0f));
    }

    private void RecordSafePose(RespawnPose pose, bool force)
    {
        if (!force && m_safeRespawnHistory.Count > 0)
        {
            float distance = Vector3.Distance(pose.Position, m_lastSafePosePosition);
            float angle = Quaternion.Angle(pose.Rotation, m_lastSafePoseRotation);
            if (distance < SafePoseMinDistance && angle < 8f)
            {
                return;
            }
        }

        m_safeRespawnHistory.Add(pose);
        if (m_safeRespawnHistory.Count > SafePoseHistoryLimit)
        {
            m_safeRespawnHistory.RemoveAt(0);
        }

        m_lastSafePoseRecordTime = Time.time;
        m_lastSafePosePosition = pose.Position;
        m_lastSafePoseRotation = pose.Rotation;
    }

    private bool TryGetSafeRespawnPose(out RespawnPose pose)
    {
        for (int i = m_safeRespawnHistory.Count - 1; i >= 0; i--)
        {
            RespawnPose groundedPose;
            if (!TryProjectRespawnToGround(m_safeRespawnHistory[i], out groundedPose))
            {
                continue;
            }

            if (IsRespawnPoseBlocked(groundedPose.Position))
            {
                continue;
            }

            pose = groundedPose;
            return true;
        }

        if (TryProjectRespawnToGround(m_spawnPose, out pose) && !IsRespawnPoseBlocked(pose.Position))
        {
            return true;
        }

        pose = new RespawnPose(m_spawnPose.Position + Vector3.up * RespawnGroundLift, m_spawnPose.Rotation);
        return true;
    }

    private bool TryProjectRespawnToGround(RespawnPose pose, out RespawnPose groundedPose)
    {
        Vector3 rayOrigin = pose.Position + Vector3.up * RespawnGroundProbeHeight;
        RaycastHit hit;

        if (Physics.Raycast(
            rayOrigin,
            Vector3.down,
            out hit,
            RespawnGroundProbeHeight + RespawnGroundProbeDepth,
            Physics.DefaultRaycastLayers,
            QueryTriggerInteraction.Ignore))
        {
            if (Vector3.Dot(hit.normal, Vector3.up) >= RespawnMinGroundNormalDot)
            {
                groundedPose = new RespawnPose(hit.point + Vector3.up * RespawnGroundLift, pose.Rotation);
                return true;
            }
        }

        groundedPose = default;
        return false;
    }

    private bool IsRespawnPoseBlocked(Vector3 position)
    {
        Vector3 capsuleBottom = position + Vector3.up * 0.4f;
        Vector3 capsuleTop = position + Vector3.up * (0.4f + RespawnVehicleCheckHeight);
        Collider[] overlaps = Physics.OverlapCapsule(
            capsuleBottom,
            capsuleTop,
            RespawnVehicleCheckRadius,
            Physics.DefaultRaycastLayers,
            QueryTriggerInteraction.Ignore);

        for (int i = 0; i < overlaps.Length; i++)
        {
            Collider overlap = overlaps[i];
            if (overlap == null || IsSelfCollider(overlap))
            {
                continue;
            }

            if (overlap.attachedRigidbody != null)
            {
                return true;
            }

            if (overlap.CompareTag("Obstacle"))
            {
                return true;
            }
        }

        return false;
    }

    private bool IsSelfCollider(Collider other)
    {
        if (other.transform.IsChildOf(transform))
        {
            return true;
        }

        if (other.attachedRigidbody != null && other.attachedRigidbody == m_Rigidbody)
        {
            return true;
        }

        return false;
    }

    private string DescribeCollider(Collider collider)
    {
        if (collider == null)
        {
            return "<null>";
        }

        string layerName = LayerMask.LayerToName(collider.gameObject.layer);
        if (string.IsNullOrEmpty(layerName))
        {
            layerName = collider.gameObject.layer.ToString();
        }

        string rigidbodyName = collider.attachedRigidbody != null ? collider.attachedRigidbody.name : "none";

        return
            $"name='{collider.name}' tag='{collider.tag}' layer='{layerName}' trigger={collider.isTrigger} " +
            $"type={collider.GetType().Name} path='{GetHierarchyPath(collider.transform)}' rigidbody='{rigidbodyName}'";
    }

    private string GetHierarchyPath(Transform current)
    {
        if (current == null)
        {
            return "<null>";
        }

        string path = current.name;
        while (current.parent != null)
        {
            current = current.parent;
            path = current.name + "/" + path;
        }

        return path;
    }

    private struct RespawnPose
    {
        public Vector3 Position;
        public Quaternion Rotation;

        public RespawnPose(Vector3 position, Quaternion rotation)
        {
            Position = position;
            Rotation = rotation;
        }
    }
}
