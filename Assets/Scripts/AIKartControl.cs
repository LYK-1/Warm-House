using UnityEngine;
using UnityEngine.AI;

// AI 赛车控制脚本，负责路点跟随、速度调整和完赛停靠。
public class AIKartControl : MonoBehaviour
{
    // AI 赛车控制脚本：负责循迹、加减速、轮子动画和终点停车。
    private const int WaypointCount = 4;
    private const string WaypointRootName = "AI waypoints";
    private const float WaypointAdvanceDistance = 3f;

    private NavMeshAgent m_agent;
    private int m_currentwaypoint = 0;
    public Transform[] AIWaypoints;
    public GameObject[] Wheels;
    public float MaxSpeed = 10f;
    private int m_participantIndex = -1;
    private bool m_isFinishing;
    private bool m_hasSetInitialDestination;
    private ObstacleSound m_obstacleSound;

    // Awake：初始化组件和运行时状态。
    private void Awake()
    {
        // 初始化导航代理和受击反馈组件。
        // 先缓存导航代理和受击反馈脚本，并准备 AI 赛道点。
        m_agent = GetComponent<NavMeshAgent>();
        m_obstacleSound = GetComponent<ObstacleSound>();

        if (m_agent != null)
        {
            m_agent.autoBraking = false;
            if (m_agent.stoppingDistance > 0.05f)
            {
                m_agent.stoppingDistance = 0.05f;
            }
        }

        ResolveWaypoints();
    }

    // Start：完成启动初始化。
    void Start(){
        // 开局注册参赛者身份，便于后续统一统计。
        if (!HasValidWaypoints())
        {
            ResolveWaypoints();
        }

        // 启动时注册参赛者身份，方便后续统一排名和结算。
        m_participantIndex = ResolveParticipantIndex(transform);
        if (m_participantIndex >= 0)
        {
            SaveProgress.RegisterParticipant(m_participantIndex, transform);
        }
    }

    // Update：每帧更新主逻辑。
    void Update(){
        // 比赛未开始或已结束时，直接停住 AI。
        // 比赛没开始或已经结束时，AI 不继续跑路线。
        if (m_isFinishing || SaveProgress.RaCeHasFiniShed)
        {
            StopMoving();
            return;
        }

        if (!SaveProgress.RaceHasStarted)
        {
            m_currentwaypoint = 0;
            m_hasSetInitialDestination = false;
            return;
        }

        if (m_agent == null || !HasValidWaypoints())
        {
            return;
        }

        // 先把第一个目标点发给 NavMeshAgent，然后再按路点逐个切换。
        if (!m_hasSetInitialDestination)
        {
            SetCurrentWaypointDestination();
        }
        else if (ShouldAdvanceToNextTarget())
        {
            m_currentwaypoint = (m_currentwaypoint + 1) % WaypointCount;
            SetCurrentWaypointDestination();
        }

        Rotatewheels();
        if (m_obstacleSound != null && m_obstacleSound.AIisHit == false)
        {
            ChangeSpeed();
        }
    }

    // ShouldAdvanceToNextTarget：判断是否该切换到下一个路点。
    private bool ShouldAdvanceToNextTarget()
    {
        if (!HasValidWaypoints() || m_agent == null)
        {
            return false;
        }

        if (m_agent.pathPending)
        {
            return false;
        }

        Transform currentWaypoint = AIWaypoints[m_currentwaypoint];
        if (currentWaypoint == null)
        {
            return false;
        }

        float distanceToWaypoint = Vector3.Distance(transform.position, currentWaypoint.position);
        return distanceToWaypoint <= WaypointAdvanceDistance;
    }

    // SetCurrentWaypointDestination：设置 AI 当前路点目标。
    private void SetCurrentWaypointDestination()
    {
        if (!HasValidWaypoints() || m_agent == null)
        {
            return;
        }

        Transform destinationWaypoint = AIWaypoints[m_currentwaypoint];
        if (destinationWaypoint == null)
        {
            return;
        }

        m_agent.isStopped = false;
        m_agent.SetDestination(destinationWaypoint.position);
        m_hasSetInitialDestination = true;
    }

    // ResolveWaypoints：解析并缓存 AI 路点。
    private void ResolveWaypoints()
    {
        // 从场景中的 AI 路点根节点自动收集目标点。
        // 从场景里自动找到 AI 路点根节点，避免手动拖引用出错。
        if (AIWaypoints == null || AIWaypoints.Length != WaypointCount)
        {
            AIWaypoints = new Transform[WaypointCount];
        }

        Transform waypointRoot = FindWaypointRoot();
        if (waypointRoot == null || waypointRoot.childCount < WaypointCount)
        {
            return;
        }

        for (int i = 0; i < WaypointCount; i++)
        {
            AIWaypoints[i] = waypointRoot.GetChild(i);
        }

        m_currentwaypoint = 0;
        m_hasSetInitialDestination = false;
    }

    // FindWaypointRoot：查找 AI 路点根节点。
    private Transform FindWaypointRoot()
    {
        GameObject waypointRoot = GameObject.Find(WaypointRootName);
        if (waypointRoot != null)
        {
            return waypointRoot.transform;
        }

        waypointRoot = GameObject.Find("AI1Waypoints");
        if (waypointRoot != null)
        {
            return waypointRoot.transform;
        }

        return null;
    }

    // HasValidWaypoints：判断路点是否都已配置。
    private bool HasValidWaypoints()
    {
        // 只有四个路点都有效时，AI 才能正常循迹。
        return AIWaypoints != null
            && AIWaypoints.Length >= WaypointCount
            && AIWaypoints[0] != null
            && AIWaypoints[1] != null
            && AIWaypoints[2] != null
            && AIWaypoints[3] != null;
    }

    // Rotatewheels：驱动车轮持续旋转。
    private void Rotatewheels(){
        // 纯表现层效果：让车轮持续旋转，看起来像真的在跑。
        for (int i=0;i<Wheels.Length;i++){
            Wheels[i].transform.Rotate(-10, 0, 0);
        }
    }

    // OnDestroy：清理引用并释放注册。
    private void OnDestroy()
    {
        // 离开场景时释放参赛者注册，避免计分表里残留无效对象。
        if (m_participantIndex >= 0)
        {
            SaveProgress.UnregisterParticipant(m_participantIndex, transform);
        }
    }

    // ResolveParticipantIndex：解析参赛者的槽位编号。
    private int ResolveParticipantIndex(Transform current)
    {
        // 根据 AI 标签在层级树中查找它属于哪个参赛槽位。
        if (current == null)
        {
            return -1;
        }

        Transform cursor = current;
        while (cursor != null)
        {
            if (cursor.CompareTag("AI1"))
            {
                return 4;
            }

            if (cursor.CompareTag("AI2"))
            {
                return 5;
            }

            if (cursor.CompareTag("AI3"))
            {
                return 6;
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

                if (node.CompareTag("AI1"))
                {
                    return 4;
                }

                if (node.CompareTag("AI2"))
                {
                    return 5;
                }

                if (node.CompareTag("AI3"))
                {
                    return 6;
                }
            }
        }

        return -1;
    }

    // ChangeSpeed：根据目标速度调整 AI 车速。
    private void ChangeSpeed()
    {
        // 根据导航速度同步轮胎转动和前进节奏。
        // 根据导航代理速度缓慢逼近 MaxSpeed，避免 AI 突然加减速。
        if (m_agent.speed < MaxSpeed)
        {
            float currentSpeed = 0;
            if (currentSpeed < MaxSpeed)
            {
                currentSpeed = m_agent.speed += 10 * Time.deltaTime;
                m_agent.speed = currentSpeed;
            }
        }
        else if (m_agent.speed > MaxSpeed)
        {
            float currentSpeed = 10000;
            if (currentSpeed > MaxSpeed)
            {
                currentSpeed = m_agent.speed -= 10 * Time.deltaTime;
                m_agent.speed = currentSpeed;
            }
        }
    }

    // BeginFinishSequence：开始比赛结束流程。
    public void BeginFinishSequence()
    {
        // 结算开始后，禁止 AI 继续参与竞速。
        // 比赛结束后通知 AI 停止继续参与竞速。
        if (m_isFinishing)
        {
            return;
        }

        m_isFinishing = true;
        StopMoving();
    }

    // StopMoving：停止 AI 当前移动。
    private void StopMoving()
    {
        // 清空 AI 速度并让导航代理停止工作。
        // 清空速度和路径，让 AI 真正停在终点或结算状态。
        if (m_agent == null)
        {
            return;
        }

        m_agent.isStopped = true;
        m_agent.ResetPath();
        m_agent.speed = 0f;
        m_agent.angularSpeed = 0f;
    }
}
