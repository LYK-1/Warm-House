using UnityEngine;
using UnityEngine.AI;

public class AIKartControl : MonoBehaviour
{
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

    private void Awake()
    {
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

    void Start(){
        if (!HasValidWaypoints())
        {
            ResolveWaypoints();
        }

        m_participantIndex = ResolveParticipantIndex(transform);
        if (m_participantIndex >= 0)
        {
            SaveProgress.RegisterParticipant(m_participantIndex, transform);
        }
    }

    void Update(){
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

    private void ResolveWaypoints()
    {
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

    private bool HasValidWaypoints()
    {
        return AIWaypoints != null
            && AIWaypoints.Length >= WaypointCount
            && AIWaypoints[0] != null
            && AIWaypoints[1] != null
            && AIWaypoints[2] != null
            && AIWaypoints[3] != null;
    }

    private void Rotatewheels(){
        for (int i=0;i<Wheels.Length;i++){
            Wheels[i].transform.Rotate(-10, 0, 0);
        }
    }

    private void OnDestroy()
    {
        if (m_participantIndex >= 0)
        {
            SaveProgress.UnregisterParticipant(m_participantIndex, transform);
        }
    }

    private int ResolveParticipantIndex(Transform current)
    {
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

    private void ChangeSpeed()
    {
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

    public void BeginFinishSequence()
    {
        if (m_isFinishing)
        {
            return;
        }

        m_isFinishing = true;
        StopMoving();
    }

    private void StopMoving()
    {
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
