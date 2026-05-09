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

    [Header("Collision")]
    [SerializeField] private float WallBounceRetention = 0.25f;
    [SerializeField] private float WallRollPitchDamping = 0.35f;
    [SerializeField] private float WallYawDamping = 0.9f;
    [SerializeField] private float WallMinBounceSpeed = 2.5f;
    [SerializeField] private float WallPushOutDistance = 0.6f;

    [Header("Reset")]
    [SerializeField] private float SafePoseRecordInterval = 0.25f;
    [SerializeField] private float SafePoseMinDistance = 3f;
    [SerializeField] private int SafePoseHistoryLimit = 12;
    [SerializeField] private float SafePoseMinUprightDot = 0.9f;
    [SerializeField] private float SafePoseRecentHazardCooldown = 1f;
    [SerializeField] private float RespawnGroundProbeHeight = 4f;
    [SerializeField] private float RespawnGroundProbeDepth = 10f;
    [SerializeField] private float RespawnGroundLift = 0.35f;
    [SerializeField] private float RespawnVehicleCheckHeight = 1.2f;
    [SerializeField] private float RespawnVehicleCheckRadius = 1.0f;
    [SerializeField] private float RespawnMinGroundNormalDot = 0.65f;
    [SerializeField] private float RespawnMinDistanceFromCurrent = 4f;

    private readonly List<RespawnPose> m_safeRespawnHistory = new List<RespawnPose>();
    private Vector3 m_lastSafePosePosition;
    private Quaternion m_lastSafePoseRotation;
    private float m_lastSafePoseRecordTime;
    private RespawnPose m_spawnPose;

    public GameObject ForwardCamera;
    public GameObject ReverseCamera;
    private bool m_camReverse;

    public Text PositionDisplay;
    private bool m_changeDirection;
    public Text LapDisplay;
    private int m_participantIndex = -1;

    void Start()
    {
        m_Rigidbody = GetComponent<Rigidbody>();
        m_Rigidbody.centerOfMass = new Vector3(0f, -0.5f, 0f);
        m_Rigidbody.maxAngularVelocity = 5f;
        // SyncRaceStartState();
        m_participantIndex = ResolveParticipantIndex(transform);
        RegisterParticipantState();

        m_spawnPose = CreateRespawnPose(transform.position, transform.rotation);
        RecordSafePose(m_spawnPose, true);

        for (int i = 0; i < m_driveTorqueGear.Length; i++)
        {
            m_gearNumber++;
            m_driveTorqueGear[i] = DriveTorque + (MaxSpeed / 10f * m_gearNumber);
        }
        ReverseCamera.SetActive(false);
        ConfigureLeaderboardDisplay();

        InvokeRepeating("DisplayPosition", 0.2f, 0.2f);
        InvokeRepeating("DisplayLap", 0.2f, 0.2f);
        DisplayPosition();
        DisplayLap();
    }

    void FixedUpdate()
    {
        if (isResetting)
        {
            return;
        }

        // SyncRaceStartState();
        if (!SaveProgress.RaceHasStarted)
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

    /* private void SyncRaceStartState()
    {
        if (m_Rigidbody == null)
        {
            return;
        }

        if (!SaveProgress.RaceHasStarted)
        {
            if (!m_Rigidbody.isKinematic)
            {
                m_Rigidbody.linearVelocity = Vector3.zero;
                m_Rigidbody.angularVelocity = Vector3.zero;
                m_Rigidbody.isKinematic = true;
            }

            return;
        }

        if (m_Rigidbody.isKinematic)
        {
            m_Rigidbody.isKinematic = false;
            m_Rigidbody.linearVelocity = Vector3.zero;
            m_Rigidbody.angularVelocity = Vector3.zero;
        }
    } */

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
        if (!SaveProgress.RaceHasStarted)
        {
            m_gas = 0f;
            return;
        }

        m_gas = value.isPressed ? 1f : 0f;
    }

    public void OnBrake(InputValue button)
    {
        if (!SaveProgress.RaceHasStarted)
        {
            m_brake = 0f;
            return;
        }

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
        if (!SaveProgress.RaceHasStarted)
        {
            m_drift = Vector2.zero;
            return;
        }

        m_drift = value.Get<Vector2>();
    }

    public void OnReverse(InputValue button)
    {
        if (!SaveProgress.RaceHasStarted)
        {
            return;
        }

        if (m_reverse)
        {
            m_reverse = false;
            GetComponent<KartSounds>().IsReversing = false;
            if (m_Rigidbody.isKinematic)
            {
                StartCoroutine(ResetChangeDirection());
            }
        }
        else
        {
            m_reverse = true;
            GetComponent<KartSounds>().IsReversing = true;
        }
    }
    public void OnSteering(InputValue value)
    {
        if (!SaveProgress.RaceHasStarted)
        {
            m_steering = Vector2.zero;
            return;
        }

        m_steering = value.Get<Vector2>();
    }

    public void OnReset(InputValue button)
    {
        if (!button.isPressed || isResetting)
        {
            return;
        }

        StartCoroutine(ResetCar());
    }

    public void OnCameraChange(InputValue button)
    {
        if (m_camReverse)
        {
            SetForwardCameraActive();
        }
        else if (!m_camReverse)
        {
            SetReverseCameraActive();
        }
    }

    public void SetForwardCameraActive()
    {
        m_camReverse = false;
        if (ForwardCamera != null)
        {
            ForwardCamera.SetActive(true);
        }

        if (ReverseCamera != null)
        {
            ReverseCamera.SetActive(false);
        }
    }

    public void SetReverseCameraActive()
    {
        m_camReverse = true;
        if (ForwardCamera != null)
        {
            ForwardCamera.SetActive(false);
        }

        if (ReverseCamera != null)
        {
            ReverseCamera.SetActive(true);
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

        m_reverse = false;
        m_changeDirection = false;
        m_camReverse = false;
        KartSounds kartSounds = GetComponent<KartSounds>();
        if (kartSounds != null)
        {
            kartSounds.IsReversing = false;
        }
        if (ForwardCamera != null && ReverseCamera != null)
        {
            ForwardCamera.SetActive(true);
            ReverseCamera.SetActive(false);
        }

        m_gas = 0f;
        m_brake = 1f;
        m_steering = Vector2.zero;
        m_drift = Vector2.zero;

        RespawnPose respawnPose;
        if (!TryGetSafeRespawnPose(out respawnPose))
        {
            respawnPose = new RespawnPose(m_spawnPose.Position + Vector3.up * RespawnGroundLift, m_spawnPose.Rotation);
        }

        int checkpointNumber = respawnPose.CheckpointNumber;
        if (checkpointNumber <= 0)
        {
            checkpointNumber = GetCurrentCheckpointNumber();
        }

        Quaternion respawnRotation = ResolveCheckpointForwardRotation(checkpointNumber, respawnPose.Rotation);
        respawnPose = new RespawnPose(respawnPose.Position, respawnRotation, checkpointNumber);

        transform.SetPositionAndRotation(respawnPose.Position, respawnPose.Rotation);
        Physics.SyncTransforms();

        int participantIndex = GetParticipantIndex();
        if (participantIndex >= 0 && participantIndex < SaveProgress.CurrentCheckpoint.Length)
        {
            SaveProgress.CurrentCheckpoint[participantIndex] = Mathf.Max(0, checkpointNumber);
        }

        yield return new WaitForSeconds(0.2f);

        m_Rigidbody.isKinematic = !SaveProgress.RaceHasStarted;
        m_Rigidbody.linearVelocity = Vector3.zero;
        m_Rigidbody.angularVelocity = Vector3.zero;

        m_brake = 0f;
        isResetting = false;
    }

    public void RegisterProgressRespawnPoint(Transform progressPoint)
    {
        if (progressPoint == null)
        {
            return;
        }

        int checkpointNumber = 0;
        ProgressPoints progressPointComponent = progressPoint.GetComponent<ProgressPoints>();
        if (progressPointComponent != null)
        {
            checkpointNumber = Mathf.Max(0, progressPointComponent.ProgressNumber);
        }

        Quaternion respawnRotation = ResolveTrackRespawnRotation(progressPoint);
        RespawnPose progressPose = new RespawnPose(progressPoint.position, respawnRotation, checkpointNumber);
        RespawnPose groundedPose;
        if (TryProjectRespawnToGround(progressPose, out groundedPose) && !IsRespawnPoseBlocked(groundedPose.Position))
        {
            RecordSafePose(groundedPose, false);
            return;
        }

        RecordSafePose(new RespawnPose(progressPoint.position + Vector3.up * RespawnGroundLift, progressPose.Rotation, checkpointNumber), false);
    }

    private int ResolveNearestCheckpointIndex(Vector3 position)
    {
        SaveProgress saveProgress = SaveProgress.Instance;
        if (saveProgress == null || saveProgress.ProgressPointsItems == null || saveProgress.ProgressPointsItems.Length == 0)
        {
            return -1;
        }

        int nearestIndex = -1;
        float nearestDistanceSqr = float.MaxValue;

        for (int i = 0; i < saveProgress.ProgressPointsItems.Length; i++)
        {
            GameObject pointObject = saveProgress.ProgressPointsItems[i];
            if (pointObject == null)
            {
                continue;
            }

            float distanceSqr = (pointObject.transform.position - position).sqrMagnitude;
            if (distanceSqr < nearestDistanceSqr)
            {
                nearestDistanceSqr = distanceSqr;
                nearestIndex = i;
            }
        }

        return nearestIndex;
    }

    private Quaternion ResolveCheckpointForwardRotation(int checkpointNumber, Quaternion fallbackRotation)
    {
        SaveProgress saveProgress = SaveProgress.Instance;
        if (saveProgress == null || saveProgress.ProgressPointsItems == null || saveProgress.ProgressPointsItems.Length < 2)
        {
            return fallbackRotation;
        }

        int effectiveCheckpointNumber = checkpointNumber <= 0 ? 1 : checkpointNumber;
        int currentIndex = effectiveCheckpointNumber - 1;
        if (currentIndex < 0 || currentIndex >= saveProgress.ProgressPointsItems.Length)
        {
            return fallbackRotation;
        }

        ProgressPoints currentPoint = GetProgressPoint(currentIndex);
        ProgressPoints nextPoint = GetProgressPoint((currentIndex + 1) % saveProgress.ProgressPointsItems.Length);
        if (currentPoint == null || nextPoint == null)
        {
            return fallbackRotation;
        }

        Vector3 direction = nextPoint.transform.position - currentPoint.transform.position;
        direction.y = 0f;
        if (direction.sqrMagnitude <= 0.0001f)
        {
            return fallbackRotation;
        }

        return Quaternion.LookRotation(direction.normalized, Vector3.up);
    }

    private Quaternion ResolveTrackRespawnRotation(Transform progressPoint)
    {
        if (progressPoint != null)
        {
            ProgressPoints progressPointComponent = progressPoint.GetComponent<ProgressPoints>();
            if (progressPointComponent != null)
            {
                SaveProgress saveProgress = SaveProgress.Instance;
                int checkpointCount = saveProgress != null && saveProgress.ProgressPointsItems != null
                    ? saveProgress.ProgressPointsItems.Length
                    : 0;
                int currentIndex = progressPointComponent.ProgressNumber - 1;
                if (saveProgress != null && checkpointCount > 1 && currentIndex >= 0 && currentIndex < checkpointCount)
                {
                    ProgressPoints nextPoint = GetProgressPoint((currentIndex + 1) % checkpointCount);
                    if (nextPoint != null)
                    {
                        Vector3 direction = nextPoint.transform.position - progressPoint.position;
                        direction.y = 0f;
                        if (direction.sqrMagnitude > 0.0001f)
                        {
                            return Quaternion.LookRotation(direction.normalized, Vector3.up);
                        }
                    }
                }
            }
        }

        return progressPoint != null ? progressPoint.rotation : transform.rotation;
    }

    private Quaternion ResolveTrackRespawnRotation(Vector3 position)
    {
        SaveProgress saveProgress = SaveProgress.Instance;
        if (saveProgress == null || saveProgress.ProgressPointsItems == null || saveProgress.ProgressPointsItems.Length < 2)
        {
            return transform.rotation;
        }

        int nearestIndex = ResolveNearestCheckpointIndex(position);
        if (nearestIndex < 0)
        {
            return transform.rotation;
        }

        ProgressPoints currentPoint = GetProgressPoint(nearestIndex);
        ProgressPoints nextPoint = GetProgressPoint((nearestIndex + 1) % saveProgress.ProgressPointsItems.Length);
        if (currentPoint == null || nextPoint == null)
        {
            return transform.rotation;
        }

        Vector3 direction = nextPoint.transform.position - currentPoint.transform.position;
        direction.y = 0f;
        if (direction.sqrMagnitude <= 0.0001f)
        {
            return transform.rotation;
        }

        return Quaternion.LookRotation(direction.normalized, Vector3.up);
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
        ApplyWallCollisionResponse(collision, true);

        if (!CollisionDebugLogging || collision.collider == null)
        {
            return;
        }

        LogCollision("OnCollisionEnter", collision);
    }

    private void OnCollisionStay(Collision collision)
    {
        MarkHazardContact(collision.collider);
        ApplyWallCollisionResponse(collision, false);

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

    private void ApplyWallCollisionResponse(Collision collision, bool applyBounce)
    {
        if (m_Rigidbody == null || collision == null || collision.collider == null)
        {
            return;
        }

        if (IsSelfCollider(collision.collider) || !IsWallLikeCollider(collision.collider))
        {
            return;
        }

        if (collision.contactCount <= 0)
        {
            return;
        }

        ContactPoint contact = collision.GetContact(0);
        Vector3 wallNormal = Vector3.ProjectOnPlane(contact.normal, Vector3.up);
        if (wallNormal.sqrMagnitude <= 0.0001f)
        {
            return;
        }

        wallNormal.Normalize();

        Vector3 planarVelocity = Vector3.ProjectOnPlane(m_Rigidbody.linearVelocity, Vector3.up);
        float incomingSpeed = Vector3.Dot(planarVelocity, -wallNormal);

        if (applyBounce && incomingSpeed > WallMinBounceSpeed)
        {
            m_Rigidbody.position = m_Rigidbody.position + (wallNormal * WallPushOutDistance);

            Vector3 bouncedPlanarVelocity = Vector3.Reflect(planarVelocity, wallNormal) * WallBounceRetention;
            m_Rigidbody.linearVelocity = new Vector3(
                bouncedPlanarVelocity.x,
                m_Rigidbody.linearVelocity.y,
                bouncedPlanarVelocity.z);
        }

        Vector3 angularVelocity = m_Rigidbody.angularVelocity;
        angularVelocity.x *= WallRollPitchDamping;
        angularVelocity.y *= WallYawDamping;
        angularVelocity.z *= WallRollPitchDamping;
        m_Rigidbody.angularVelocity = angularVelocity;
    }

    private bool IsWallLikeCollider(Collider other)
    {
        if (other == null)
        {
            return false;
        }

        PhysicsMaterial sharedMaterial = other.sharedMaterial;
        if (sharedMaterial != null && sharedMaterial.name == "Blockers")
        {
            return true;
        }

        return other.name.Contains("Blocker") || other.name.Contains("Fence");
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

        // Debug.Log(message, collision.collider != null ? collision.collider.gameObject : gameObject);
    }

    private void LogTrigger(string eventName, Collider other)
    {
        Vector3 closestPoint = other != null ? other.bounds.ClosestPoint(transform.position) : transform.position;
        string message =
            $"[CollisionDebug][{name}] {eventName} other={DescribeCollider(other)} " +
            $"playerPosition={transform.position} closestPoint={closestPoint}";

        // Debug.Log(message, other.gameObject);
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
        if (!m_grounded || isResetting || m_changeDirection || m_reverse)
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

        RespawnPose candidate = CreateRespawnPose(transform.position, transform.rotation, GetCurrentCheckpointNumber());
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
        return CreateRespawnPose(position, rotation, 0);
    }

    private RespawnPose CreateRespawnPose(Vector3 position, Quaternion rotation, int checkpointNumber)
    {
        Vector3 euler = rotation.eulerAngles;
        return new RespawnPose(position, Quaternion.Euler(0f, euler.y, 0f), checkpointNumber);
    }

    private int GetCurrentCheckpointNumber()
    {
        int participantIndex = GetParticipantIndex();
        if (participantIndex < 0 || participantIndex >= SaveProgress.CurrentCheckpoint.Length)
        {
            return 0;
        }

        return Mathf.Max(0, SaveProgress.CurrentCheckpoint[participantIndex]);
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

    private bool IsRespawnPoseTooCloseToCurrent(RespawnPose pose, Vector3 currentPosition)
    {
        if (RespawnMinDistanceFromCurrent <= 0f)
        {
            return false;
        }

        float minDistanceSqr = RespawnMinDistanceFromCurrent * RespawnMinDistanceFromCurrent;
        return (pose.Position - currentPosition).sqrMagnitude < minDistanceSqr;
    }

    private bool TryGetSafeRespawnPose(out RespawnPose pose)
    {
        Vector3 currentPosition = m_Rigidbody != null ? m_Rigidbody.position : transform.position;
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

            if (IsRespawnPoseTooCloseToCurrent(groundedPose, currentPosition))
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

        pose = new RespawnPose(m_spawnPose.Position + Vector3.up * RespawnGroundLift, m_spawnPose.Rotation, m_spawnPose.CheckpointNumber);
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
                groundedPose = new RespawnPose(hit.point + Vector3.up * RespawnGroundLift, pose.Rotation, pose.CheckpointNumber);
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
        public int CheckpointNumber;

        public RespawnPose(Vector3 position, Quaternion rotation)
            : this(position, rotation, 0)
        {
        }

        public RespawnPose(Vector3 position, Quaternion rotation, int checkpointNumber)
        {
            Position = position;
            Rotation = rotation;
            CheckpointNumber = checkpointNumber;
        }
    }

    private void DisplayPosition()
    {
        if (PositionDisplay == null)
        {
            return;
        }

        PositionDisplay.text = BuildRankText(GetParticipantIndex());
    }

    private void ConfigureLeaderboardDisplay()
    {
        if (PositionDisplay == null)
        {
            return;
        }

        PositionDisplay.alignment = TextAnchor.LowerRight;
        PositionDisplay.horizontalOverflow = HorizontalWrapMode.Overflow;
        PositionDisplay.verticalOverflow = VerticalWrapMode.Overflow;
    }

    private string BuildRankText(int localParticipantIndex)
    {
        int rank = GetCurrentRacePosition(localParticipantIndex);
        if (rank <= 0)
        {
            return "Rank: --";
        }

        return $"Rank: {rank}";
    }

    private int GetParticipantIndex()
    {
        if (m_participantIndex >= 0 && m_participantIndex < SaveProgress.ParticipantTags.Length)
        {
            return m_participantIndex;
        }

        m_participantIndex = ResolveParticipantIndex(transform);
        RegisterParticipantState();
        return m_participantIndex;
    }

    public int ParticipantIndex
    {
        get { return GetParticipantIndex(); }
    }

    public static int ResolveParticipantIndex(Transform current)
    {
        if (current == null)
        {
            return -1;
        }

        Transform cursor = current;
        while (cursor != null)
        {
            for (int i = 0; i < SaveProgress.ParticipantTags.Length; i++)
            {
                if (cursor.CompareTag(SaveProgress.ParticipantTags[i]))
                {
                    return i;
                }
            }

            cursor = cursor.parent;
        }

        Transform root = current.root;
        if (root != null)
        {
            Transform[] transforms = root.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < transforms.Length; i++)
            {
                Transform node = transforms[i];
                if (node == null)
                {
                    continue;
                }

                for (int j = 0; j < SaveProgress.ParticipantTags.Length; j++)
                {
                    if (node.CompareTag(SaveProgress.ParticipantTags[j]))
                    {
                        return j;
                    }
                }
            }
        }

        return -1;
    }

    private int GetCurrentRacePosition(int playerIndex)
    {
        if (playerIndex < 0 || playerIndex >= SaveProgress.CurrentCheckpoint.Length)
        {
            return 0;
        }

        float playerScore = GetParticipantRaceScore(playerIndex);
        if (float.IsNegativeInfinity(playerScore))
        {
            return 0;
        }

        int rank = 1;
        for (int i = 0; i < SaveProgress.CurrentCheckpoint.Length; i++)
        {
            if (i == playerIndex)
            {
                continue;
            }

            float otherScore = GetParticipantRaceScore(i);
            if (!float.IsNegativeInfinity(otherScore) && otherScore > playerScore + 0.01f)
            {
                rank++;
            }
        }

        return rank;
    }

    private float GetParticipantRaceScore(int index)
    {
        if (index < 0 || index >= SaveProgress.CurrentCheckpoint.Length)
        {
            return float.NegativeInfinity;
        }

        SaveProgress saveProgress = SaveProgress.Instance;
        if (saveProgress == null)
        {
            return float.NegativeInfinity;
        }

        int checkpointCount = saveProgress.ProgressPointsItems != null ? saveProgress.ProgressPointsItems.Length : 0;
        int lap = Mathf.Max(0, SaveProgress.CurrentLap[index]);
        int checkpoint = Mathf.Max(0, SaveProgress.CurrentCheckpoint[index]);

        if (checkpointCount <= 0)
        {
            return lap * 100000f + checkpoint * 100f;
        }

        int normalizedCheckpoint = checkpoint <= 0 ? 0 : ((checkpoint - 1) % checkpointCount) + 1;
        float progressWithinCheckpoint = GetCheckpointProgress(index, normalizedCheckpoint, checkpointCount);
        return lap * 100000f + normalizedCheckpoint * 100f + progressWithinCheckpoint;
    }

    private float GetCheckpointProgress(int participantIndex, int normalizedCheckpoint, int checkpointCount)
    {
        Transform participantTransform = SaveProgress.GetParticipantTransform(participantIndex);
        if (participantTransform == null || checkpointCount < 2 || normalizedCheckpoint <= 0)
        {
            return 0f;
        }

        ProgressPoints currentPoint = GetProgressPoint(normalizedCheckpoint - 1);
        ProgressPoints nextPoint = GetProgressPoint(normalizedCheckpoint % checkpointCount);
        if (currentPoint == null || nextPoint == null)
        {
            return 0f;
        }

        Vector3 segment = nextPoint.transform.position - currentPoint.transform.position;
        segment.y = 0f;
        float segmentLengthSqr = segment.sqrMagnitude;
        if (segmentLengthSqr <= 0.0001f)
        {
            return 0f;
        }

        Vector3 fromCurrent = participantTransform.position - currentPoint.transform.position;
        fromCurrent.y = 0f;
        float progress = Vector3.Dot(fromCurrent, segment) / segmentLengthSqr;
        return Mathf.Clamp01(progress);
    }

    private ProgressPoints GetProgressPoint(int index)
    {
        SaveProgress saveProgress = SaveProgress.Instance;
        if (saveProgress == null || saveProgress.ProgressPointsItems == null)
        {
            return null;
        }

        if (index < 0 || index >= saveProgress.ProgressPointsItems.Length)
        {
            return null;
        }

        GameObject pointObject = saveProgress.ProgressPointsItems[index];
        if (pointObject == null)
        {
            return null;
        }

        return pointObject.GetComponent<ProgressPoints>();
    }

    private Transform GetParticipantTransform(int index)
    {
        if (index < 0 || index >= SaveProgress.ParticipantTransforms.Length)
        {
            return null;
        }

        return SaveProgress.GetParticipantTransform(index);
    }

    private string GetParticipantDisplayName(int index)
    {
        if (index < 0 || index >= SaveProgress.ParticipantTags.Length)
        {
            return "未知";
        }

        switch (SaveProgress.ParticipantTags[index])
        {
            case "Player1":
                return "玩家1";
            case "Player2":
                return "玩家2";
            case "Player3":
                return "玩家3";
            case "Player4":
                return "玩家4";
            case "AI1":
                return "AI1";
            case "AI2":
                return "AI2";
            case "AI3":
                return "AI3";
            default:
                return SaveProgress.ParticipantTags[index];
        }
    }

    private string FormatPosition(int position)
    {
        if (position <= 0)
        {
            return "--";
        }

        if (position % 100 >= 11 && position % 100 <= 13)
        {
            return position + "th";
        }

        switch (position % 10)
        {
            case 1:
                return position + "st";
            case 2:
                return position + "nd";
            case 3:
                return position + "rd";
            default:
                return position + "th";
        }
    }

    public void FaceForward()
    {
        if (m_changeDirection || m_Rigidbody == null)
        {
            return;
        }

        m_changeDirection = true;
        m_reverse = false;

        KartSounds kartSounds = GetComponent<KartSounds>();
        if (kartSounds != null)
        {
            kartSounds.IsReversing = false;
        }

        if (ForwardCamera != null && ReverseCamera != null)
        {
            m_camReverse = false;
            ForwardCamera.SetActive(true);
            ReverseCamera.SetActive(false);
        }

        m_gas = 0f;
        m_brake = 0f;
        m_steering = Vector2.zero;
        m_drift = Vector2.zero;

        m_Rigidbody.linearVelocity = Vector3.zero;
        m_Rigidbody.angularVelocity = Vector3.zero;
        transform.Rotate(0f, 180f, 0f);
        m_Rigidbody.isKinematic = true;
        StartCoroutine(ResetChangeDirection());
    }

    public void StopReverseAtProgressPoint()
    {
        if (m_changeDirection || m_Rigidbody == null)
        {
            return;
        }

        m_changeDirection = true;
        m_reverse = false;

        KartSounds kartSounds = GetComponent<KartSounds>();
        if (kartSounds != null)
        {
            kartSounds.IsReversing = false;
        }

        if (ForwardCamera != null && ReverseCamera != null)
        {
            m_camReverse = false;
            ForwardCamera.SetActive(true);
            ReverseCamera.SetActive(false);
        }

        m_gas = 0f;
        m_brake = 0f;
        m_steering = Vector2.zero;
        m_drift = Vector2.zero;

        m_Rigidbody.linearVelocity = Vector3.zero;
        m_Rigidbody.angularVelocity = Vector3.zero;
        m_Rigidbody.isKinematic = true;
        StartCoroutine(ResetChangeDirection());
    }

    IEnumerator ResetChangeDirection()
    {
        yield return new WaitForSeconds(1f);

        if (m_Rigidbody != null)
        {
            m_Rigidbody.isKinematic = false;
        }

        yield return new WaitForSeconds(0.5f);
        m_changeDirection = false;
    }

    public void DisplayLap()
    {
        if (LapDisplay == null)
        {
            return;
        }

        int participantIndex = GetParticipantIndex();
        if (participantIndex < 0 || participantIndex >= SaveProgress.CurrentLap.Length)
        {
            LapDisplay.text = "Lap --";
            return;
        }

        int lapAmount = Mathf.Max(0, SaveProgress.CurrentLap[participantIndex]) + 1;
        LapDisplay.text = "Lap " + lapAmount.ToString();
    }

    private void RegisterParticipantState()
    {
        int participantIndex = m_participantIndex;
        if (participantIndex < 0 || participantIndex >= SaveProgress.ParticipantTransforms.Length)
        {
            return;
        }

        SaveProgress.RegisterParticipant(participantIndex, transform);
    }

    private void OnDestroy()
    {
        if (m_participantIndex >= 0)
        {
            SaveProgress.UnregisterParticipant(m_participantIndex, transform);
        }
    }
}

