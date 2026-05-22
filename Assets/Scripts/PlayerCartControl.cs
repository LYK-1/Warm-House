using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using Unity.Cinemachine;

// 玩家赛车控制脚本，负责驾驶、漂移、复位、相机和比赛状态联动。
public class PlayerCartControl : MonoBehaviour
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
    private bool m_isFinishing = false;
    private Coroutine m_finishCoroutine;

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

    public GameObject UICanvas;

    public CinemachineBrain Brain;
    public ICinemachineCamera CamA;
    public ICinemachineCamera CamB;
    public Canvas canvasObject;

    // Awake：初始化组件和运行时状态。
    private void Awake()
    {
        // 仅做最早期的相机切换预约，避免对象初始化顺序问题。
        Invoke("ChangeCameras", 0.05f);
    }

    // Start：完成启动初始化。
    void Start()
    {
        // 初始化刚体、重心、参赛者身份和出生时的安全保存点。
        m_Rigidbody = GetComponent<Rigidbody>();
        m_Rigidbody.centerOfMass = new Vector3(0f, -0.5f, 0f);
        m_Rigidbody.maxAngularVelocity = 5f;
        m_participantIndex = ResolveParticipantIndex(transform);
        RegisterParticipantState();

        m_spawnPose = CreateRespawnPose(transform.position, transform.rotation);
        RecordSafePose(m_spawnPose, true);

        for (int i = 0; i < m_driveTorqueGear.Length; i++)
        {
            m_gearNumber++;
            m_driveTorqueGear[i] = DriveTorque + (MaxSpeed / 10f * m_gearNumber);
        }
        SetReverseCameraActive();
        ConfigureLeaderboardDisplay();

        InvokeRepeating("DisplayLap", 0.2f, 0.2f);
        InvokeRepeating(nameof(DisplayPosition), 0.2f, 0.2f);
        DisplayLap();
        DisplayPosition();
    }

    // FixedUpdate：执行物理帧更新。
    void FixedUpdate()
    {
        // 只有比赛正式开始后，才允许车辆持续受物理驱动。
        if (isResetting)
        {
            return;
        }

        if (m_isFinishing)
        {
            return;
        }

        if (SaveProgress.RaCeHasFiniShed)
        {
            ZeroMotionForFinish();
            return;
        }

        if (!SaveProgress.RaceHasStarted)
        {
            return;
        }

        Drive(m_gas, m_brake, m_steering, m_drift);
        AddDownForce();
        TrackSafeRespawnPose();
    }

    // Update：每帧更新主逻辑。
    void Update()
    {
        // 每帧刷新当前速度和 HUD 显示。
        currentSpeed = m_Rigidbody.linearVelocity.magnitude * 3.6f;

        if (speedText != null)
        {
            speedText.text = currentSpeed.ToString("F0");
        }

        if (SaveProgress.RaCeHasFiniShed)
        {
            UICanvas.SetActive(false);
        }
    }

    // ChangeCameras：切换多人模式下的摄像机配置。
    void ChangeCameras()
    {
        // 多人模式下切换相机和 HUD 布局，单人模式则保持默认。
        if (SaveScript.MultiPlayerMode)
        {
            CamA = ForwardCamera.GetComponent<CinemachineCamera>();
            CamB = ForwardCamera.GetComponent<CinemachineCamera>();
            Brain.SetCameraOverride(1, 1, CamA, CamB, 1, 1);
            Canvas hudCanvas = ResolveHudCanvas();
            if (hudCanvas != null && Brain != null)
            {
                hudCanvas.renderMode = RenderMode.ScreenSpaceCamera;
                hudCanvas.worldCamera = Brain.GetComponent<Camera>();
            }

            if (IsTwoPlayerSplitScreen())
            {
                ApplySplitScreenHudScaling();
            }
        }
    }

    // IsTwoPlayerSplitScreen：判断状态是否满足条件。
    private bool IsTwoPlayerSplitScreen()
    {
        return SaveScript.MultiPlayerMode && SaveScript.MultiPlayerAmount == 2;
    }

    // ResolveHudCanvas：解析并缓存相关引用。
    private Canvas ResolveHudCanvas()
    {
        if (canvasObject != null)
        {
            return canvasObject;
        }

        if (UICanvas != null)
        {
            canvasObject = UICanvas.GetComponent<Canvas>();
            if (canvasObject != null)
            {
                return canvasObject;
            }

            canvasObject = UICanvas.GetComponentInChildren<Canvas>(true);
            if (canvasObject != null)
            {
                return canvasObject;
            }
        }

        canvasObject = GetComponentInChildren<Canvas>(true);
        return canvasObject;
    }

    // ApplySplitScreenHudScaling：调整分屏模式下的 HUD 缩放。
    private void ApplySplitScreenHudScaling()
    {
        if (!IsTwoPlayerSplitScreen())
        {
            return;
        }

        Canvas hudCanvas = ResolveHudCanvas();
        if (hudCanvas == null)
        {
            return;
        }

        CanvasScaler scaler = hudCanvas.GetComponent<CanvasScaler>();
        if (scaler == null)
        {
            return;
        }

        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0f;
    }

    // IsPrimaryPlayer：判断状态是否满足条件。
    private bool IsPrimaryPlayer()
    {
        return GetParticipantIndex() == 0;
    }

    // Drive：根据输入控制赛车行驶、转向和漂移。
    private void Drive(float acceleration, float brake, Vector2 steer, Vector2 drift)
    {
        if (m_Rigidbody.isKinematic)
        {
            return;
        }

        // 当前速度越高，转向越钝，漂移和刹车表现也会随之变化。
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

    // ApplyDriftSpeedPenalty：应用漂移时的速度惩罚。
    private void ApplyDriftSpeedPenalty()
    {
        // 漂移惩罚只影响水平速度，不影响赛车上下抖动。
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

    // AddDownForce：给赛车施加下压力。
    private void AddDownForce()
    {
        // 只要任意轮子接地，就给赛车施加稳定下压力。
        bool anyGrounded = false;

        // All wheels count for grounding. Checking only the rear wheels caused
        // false airborne states on uneven ground, which then toggled rigidbody
        // constraints and made the camera look jittery.
        for (int i = 0; i < WheelColliders.Length; ++i)
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

    // SetConstraints：临时锁定赛车刚体约束。
    public IEnumerator SetConstraints()
    {
        yield return new WaitForSeconds(0.1f);
        if (isResetting || m_grounded)
        {
            constraintCoroutine = null;
            yield break;
        }

        m_Rigidbody.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;

        while (!isResetting && !m_grounded)
        {
            yield return new WaitForFixedUpdate();
        }

        if (!isResetting)
        {
            m_Rigidbody.constraints = RigidbodyConstraints.None;
        }

        constraintCoroutine = null;
    }

    // OnAccelerate：响应加速输入。
    public void OnAccelerate(InputValue value)
    {
        if (!IsPrimaryPlayer())
        {
            return;
        }

        if (!SaveProgress.RaceHasStarted)
        {
            m_gas = 0f;
            return;
        }

        m_gas = value.isPressed ? 1f : 0f;
    }

    // OnBrake：响应刹车输入。
    public void OnBrake(InputValue button)
    {
        if (!IsPrimaryPlayer())
        {
            return;
        }

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

    // OnDrift：响应漂移输入。
    public void OnDrift(InputValue value)
    {
        if (!IsPrimaryPlayer())
        {
            return;
        }

        if (!SaveProgress.RaceHasStarted)
        {
            m_drift = Vector2.zero;
            return;
        }

        m_drift = value.Get<Vector2>();
    }

    // OnReverse：响应倒车输入。
    public void OnReverse(InputValue button)
    {
        if (!IsPrimaryPlayer())
        {
            return;
        }

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
    // OnSteering：响应转向输入。
    public void OnSteering(InputValue value)
    {
        if (!IsPrimaryPlayer())
        {
            return;
        }

        if (!SaveProgress.RaceHasStarted)
        {
            m_steering = Vector2.zero;
            return;
        }

        m_steering = value.Get<Vector2>();
    }

    // OnReset：响应重置输入。
    public void OnReset(InputValue button)
    {
        if (!IsPrimaryPlayer())
        {
            return;
        }

        if (SaveProgress.RaCeHasFiniShed)
        {
            return;
        }

        if (!button.isPressed || isResetting)
        {
            return;
        }

        StartCoroutine(ResetCar());
    }

    // OnCameraChange：切换摄像机视角。
    public void OnCameraChange(InputValue button)
    {
        if (!IsPrimaryPlayer())
        {
            return;
        }

        if (m_isFinishing || SaveProgress.RaCeHasFiniShed)
        {
            return;
        }

        if (m_camReverse)
        {
            SetForwardCameraActive();
            if (SaveScript.MultiPlayerMode)
            {
                CamA = ForwardCamera.GetComponent<CinemachineCamera>();
                CamB = ForwardCamera.GetComponent<CinemachineCamera>();
                Brain.SetCameraOverride(1, 1, CamA, CamB, 1, 1);
            }
        }
        else if (!m_camReverse)
        {
            SetReverseCameraActive();
            if (SaveScript.MultiPlayerMode)
            {
                CamA = ReverseCamera.GetComponent<CinemachineCamera>();
                CamB = ReverseCamera.GetComponent<CinemachineCamera>();
                Brain.SetCameraOverride(1, 1, CamA, CamB, 1, 1);
            }
        }
    }

    // SetForwardCameraActive：切换到前向摄像机。
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

    // SetReverseCameraActive：切换到倒车摄像机。
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

    // ResetCar：执行赛车复位流程。
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

        // 优先从最近一次安全点复活，实在找不到再回退到出生点。
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

    // BeginFinishSequence：开始比赛结束流程。
    public void BeginFinishSequence()
    {
        // 比赛结束后先锁住赛车，避免终点动画还没播完就继续移动。
        if (m_Rigidbody == null || m_isFinishing)
        {
            return;
        }

        m_isFinishing = true;
        SetReverseCameraActive();

        if (m_finishCoroutine != null)
        {
            StopCoroutine(m_finishCoroutine);
        }

        m_finishCoroutine = StartCoroutine(FinishSequence());
    }

    // FinishSequence：执行终点结算过渡。
    private IEnumerator FinishSequence()
    {
        // 终点时先关闭所有输入，再把车速平滑压到 0。
        SaveProgress.RaceHasStarted = false;

        if (constraintCoroutine != null)
        {
            StopCoroutine(constraintCoroutine);
            constraintCoroutine = null;
        }

        m_changeDirection = false;
        m_reverse = false;
        m_gas = 0f;
        m_brake = 0f;
        m_steering = Vector2.zero;
        m_drift = Vector2.zero;

        KartSounds kartSounds = GetComponent<KartSounds>();
        if (kartSounds != null)
        {
            kartSounds.IsReversing = false;
        }

        m_Rigidbody.isKinematic = false;
        m_Rigidbody.constraints = RigidbodyConstraints.None;

        for (int i = 0; i < WheelColliders.Length; i++)
        {
            WheelColliders[i].motorTorque = 0f;
            WheelColliders[i].brakeTorque = 0f;
        }

        Vector3 startLinearVelocity = m_Rigidbody.linearVelocity;
        Vector3 startAngularVelocity = m_Rigidbody.angularVelocity;
        float finishDuration = 0.2f;
        float elapsed = 0f;

        while (elapsed < finishDuration)
        {
            float t = Mathf.Clamp01(elapsed / finishDuration);
            float damp = 1f - Mathf.SmoothStep(0f, 1f, t);
            m_Rigidbody.linearVelocity = startLinearVelocity * damp;
            m_Rigidbody.angularVelocity = startAngularVelocity * damp;

            elapsed += Time.fixedDeltaTime;
            yield return new WaitForFixedUpdate();
        }

        m_Rigidbody.linearVelocity = Vector3.zero;
        m_Rigidbody.angularVelocity = Vector3.zero;

        m_isFinishing = false;
        m_finishCoroutine = null;
    }

    // ZeroMotionForFinish：比赛结束时清空物理速度。
    private void ZeroMotionForFinish()
    {
        // 比赛已经结束时，持续清空物理速度，防止赛车滑出镜头。
        if (m_Rigidbody == null)
        {
            return;
        }

        m_gas = 0f;
        m_brake = 0f;
        m_steering = Vector2.zero;
        m_drift = Vector2.zero;
        m_Rigidbody.linearVelocity = Vector3.zero;
        m_Rigidbody.angularVelocity = Vector3.zero;

        for (int i = 0; i < WheelColliders.Length; i++)
        {
            WheelColliders[i].motorTorque = 0f;
            WheelColliders[i].brakeTorque = 0f;
        }
    }

    // RegisterProgressRespawnPoint：注册相关对象或状态。
    public void RegisterProgressRespawnPoint(Transform progressPoint)
    {
        // 当前检查点会被记录为临时重生点，掉出赛道时可以回到这里。
        if (progressPoint == null)
        {
            return;
        }

        // 生成新的重生点时，把它绑定到最近的检查点编号。
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

    // ResolveNearestCheckpointIndex：查找最近的检查点索引。
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

    // ResolveCheckpointForwardRotation：解析检查点正向朝向。
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

    // ResolveTrackRespawnRotation：解析赛道重生朝向。
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

    // ResolveTrackRespawnRotation：解析赛道重生朝向。
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

    // SetGears：根据速度刷新挡位。
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

    // OnCollisionEnter：处理碰撞进入事件。
    private void OnCollisionEnter(Collision collision)
    {
        if (IsPropContact(collision.collider))
        {
            return;
        }

        MarkHazardContact(collision.collider);
        ApplyWallCollisionResponse(collision, true);

        if (!CollisionDebugLogging || collision.collider == null)
        {
            return;
        }

        LogCollision("OnCollisionEnter", collision);
    }

    // OnCollisionStay：处理碰撞持续事件。
    private void OnCollisionStay(Collision collision)
    {
        if (IsPropContact(collision.collider))
        {
            return;
        }

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

    // OnCollisionExit：处理碰撞离开事件。
    private void OnCollisionExit(Collision collision)
    {
        if (collision.collider == m_lastCollisionStayCollider)
        {
            m_lastCollisionStayCollider = null;
        }
    }

    // OnTriggerEnter：处理触发器进入事件。
    private void OnTriggerEnter(Collider other)
    {
        if (IsPropContact(other))
        {
            return;
        }

        MarkHazardContact(other);

        if (!CollisionDebugLogging || other == null)
        {
            return;
        }

        LogTrigger("OnTriggerEnter", other);
    }

    // OnTriggerExit：处理触发器离开事件。
    private void OnTriggerExit(Collider other)
    {
        if (IsPropContact(other))
        {
            return;
        }

        if (!CollisionDebugLogging || other == null)
        {
            return;
        }

        LogTrigger("OnTriggerExit", other);
    }

    // ShouldLogCollisionStay：判断是否满足执行条件。
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

    // ApplyWallCollisionResponse：计算墙体碰撞后的反弹响应。
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

    // IsWallLikeCollider：判断碰撞体是否属于墙体。
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

    // LogCollision：输出碰撞调试日志。
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

    // LogTrigger：输出触发器调试日志。
    private void LogTrigger(string eventName, Collider other)
    {
        Vector3 closestPoint = other != null ? other.bounds.ClosestPoint(transform.position) : transform.position;
        string message =
            $"[CollisionDebug][{name}] {eventName} other={DescribeCollider(other)} " +
            $"playerPosition={transform.position} closestPoint={closestPoint}";

        // Debug.Log(message, other.gameObject);
    }

    // MarkHazardContact：记录最近一次危险接触时间。
    private void MarkHazardContact(Collider other)
    {
        if (other == null || IsSelfCollider(other) || other is TerrainCollider || IsPropContact(other))
        {
            return;
        }

        m_lastHazardContactTime = Time.time;
    }

    // TrackSafeRespawnPose：记录当前安全重生点。
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

    // CreateRespawnPose：创建一条重生姿态数据。
    private RespawnPose CreateRespawnPose(Vector3 position, Quaternion rotation)
    {
        return CreateRespawnPose(position, rotation, 0);
    }

    // CreateRespawnPose：创建一条重生姿态数据。
    private RespawnPose CreateRespawnPose(Vector3 position, Quaternion rotation, int checkpointNumber)
    {
        // 只保留 Y 轴朝向，避免重生时赛车发生奇怪翻转。
        Vector3 euler = rotation.eulerAngles;
        return new RespawnPose(position, Quaternion.Euler(0f, euler.y, 0f), checkpointNumber);
    }

    // GetCurrentCheckpointNumber：获取相关数据或对象。
    private int GetCurrentCheckpointNumber()
    {
        int participantIndex = GetParticipantIndex();
        if (participantIndex < 0 || participantIndex >= SaveProgress.CurrentCheckpoint.Length)
        {
            return 0;
        }

        return Mathf.Max(0, SaveProgress.CurrentCheckpoint[participantIndex]);
    }

    // RecordSafePose：记录一条安全姿态。
    private void RecordSafePose(RespawnPose pose, bool force)
    {
        // 记录安全姿态时会去重，避免每一帧都塞进历史列表。
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

    // IsRespawnPoseTooCloseToCurrent：判断重生点是否离当前位置过近。
    private bool IsRespawnPoseTooCloseToCurrent(RespawnPose pose, Vector3 currentPosition)
    {
        if (RespawnMinDistanceFromCurrent <= 0f)
        {
            return false;
        }

        float minDistanceSqr = RespawnMinDistanceFromCurrent * RespawnMinDistanceFromCurrent;
        return (pose.Position - currentPosition).sqrMagnitude < minDistanceSqr;
    }

    // TryGetSafeRespawnPose：尝试获取可用的安全重生点。
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

    // TryProjectRespawnToGround：把重生点投射到地面。
    private bool TryProjectRespawnToGround(RespawnPose pose, out RespawnPose groundedPose)
    {
        // 通过射线检测地面，把重生点贴到可行驶地面上。
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

    // IsRespawnPoseBlocked：判断重生位置是否被阻挡。
    private bool IsRespawnPoseBlocked(Vector3 position)
    {
        // 用胶囊体范围检查当前位置是否被墙体、车体或障碍物占住。
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

    // IsSelfCollider：判断状态是否满足条件。
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

    // IsPropContact：判断状态是否满足条件。
    private bool IsPropContact(Collider other)
    {
        if (other == null)
        {
            return false;
        }

        Transform cursor = other.transform;
        while (cursor != null)
        {
            if (cursor.tag == "prop")
            {
                return true;
            }

            cursor = cursor.parent;
        }

        return false;
    }

    // DescribeCollider：生成碰撞体的描述字符串。
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

    // GetHierarchyPath：获取对象层级路径。
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

    // DisplayPosition：刷新排名显示。
    private void DisplayPosition()
    {
        if (PositionDisplay == null)
        {
            return;
        }

        // HUD 左上角/右下角的名次显示只需要展示当前本地玩家的排名。
        PositionDisplay.text = BuildRankText(GetParticipantIndex());
    }

    // ConfigureLeaderboardDisplay：配置名次文本的显示样式。
    private void ConfigureLeaderboardDisplay()
    {
        if (PositionDisplay == null)
        {
            return;
        }

        // 右下角对齐，保证多行名次文本不会挤到速度表区域。
        PositionDisplay.alignment = TextAnchor.LowerRight;
        PositionDisplay.horizontalOverflow = HorizontalWrapMode.Overflow;
        PositionDisplay.verticalOverflow = VerticalWrapMode.Overflow;
    }

    // BuildRankText：构建排名显示文本。
    private string BuildRankText(int localParticipantIndex)
    {
        if (!SaveProgress.RaceHasStarted && !SaveProgress.RaCeHasFiniShed)
        {
            return "Rank: --";
        }

        int rank = SaveProgress.GetParticipantRank(localParticipantIndex);
        if (rank <= 0)
        {
            return "Rank: --";
        }

        return $"Rank: {rank}";
    }

    // GetParticipantIndex：获取当前脚本所属的参赛者编号。
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

    // ResolveParticipantIndex：解析参赛者的槽位编号。
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

    // GetProgressPoint：获取指定序号的检查点对象。
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

    // FaceForward：把赛车朝向修正为正向。
    public void FaceForward()
    {
        // 正在回正或刚体不存在时，不允许再次触发回正逻辑。
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

    // StopReverseAtProgressPoint：在检查点处��止倒车并回正。
    public void StopReverseAtProgressPoint()
    {
        // 逆行时一旦碰到有效检查点，就只做回正，不让玩家借此反复刷检查点。
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

    // ResetChangeDirection：等待回正流程结束并恢复物理控制。
    IEnumerator ResetChangeDirection()
    {
        // 回正过程先等待一小段时间，再重新开放物理控制。
        yield return new WaitForSeconds(1f);

        if (m_Rigidbody != null)
        {
            m_Rigidbody.isKinematic = false;
        }

        // 再等半秒后解除方向锁，防止连续触发抖动。
        yield return new WaitForSeconds(0.5f);
        m_changeDirection = false;
    }

    // DisplayLap：刷新圈数显示。
    public void DisplayLap()
    {
        // 圈数显示始终跟随当前参赛者的实时进度更新。
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

        if (SaveProgress.RaCeHasFiniShed)
        {
            LapDisplay.text = "Lap " + SaveProgress.MaxLaps.ToString();
            return;
        }

        // 圈数显示采用“当前圈 + 1”的方式，让 UI 更符合玩家直觉。
        int lapAmount = Mathf.Min(Mathf.Max(0, SaveProgress.CurrentLap[participantIndex]) + 1, SaveProgress.MaxLaps);
        LapDisplay.text = "Lap " + lapAmount.ToString();
    }

    // RegisterParticipantState：注册当前参赛者状态。
    private void RegisterParticipantState()
    {
        // 进入场景后把本车注册到比赛管理器，供排名和结算读取。
        int participantIndex = m_participantIndex;
        if (participantIndex < 0 || participantIndex >= SaveProgress.ParticipantTransforms.Length)
        {
            return;
        }

        SaveProgress.RegisterParticipant(participantIndex, transform);
    }

    // OnDestroy：清理引用并释放注册。
    private void OnDestroy()
    {
        // 对象销毁时取消注册，防止跨场景引用失效。
        if (m_participantIndex >= 0)
        {
            SaveProgress.UnregisterParticipant(m_participantIndex, transform);
        }
    }
}

