using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class AIKartControl : MonoBehaviour
{
    private NavMeshAgent m_agent;
    private int m_currentwaypoint = 0;
    private bool m_checkDistance = false;
    public Transform[] AIWaypoints;
    public GameObject[] Wheels;
    public float MaxSpeed = 10f;
    private int m_participantIndex = -1;

    void Start(){
        m_agent = GetComponent<NavMeshAgent>();
        m_participantIndex = ResolveParticipantIndex(transform);
        if (m_participantIndex >= 0)
        {
            SaveProgress.RegisterParticipant(m_participantIndex, transform);
        }
        StartCoroutine(SetCheckDistance());
    }
    void Update(){
        m_agent.SetDestination(AIWaypoints[m_currentwaypoint].position);
        CheckDistanceToNextTarget();
        Rotatewheels();
        if (GetComponent<ObstacleSound>().AIisHit == false){
            ChangeSpeed();
        }
    }

    private void CheckDistanceToNextTarget(){
        if (m_agent.remainingDistance <= m_agent.stoppingDistance + 0.1f && m_checkDistance){
            if (m_currentwaypoint < AIWaypoints.Length-1){
                m_currentwaypoint++;
            }else{
                m_currentwaypoint =0;
            }
            m_checkDistance =false;
            StartCoroutine(SetCheckDistance());
        }
    }

    private void Rotatewheels(){
        for (int i=0;i<Wheels.Length;i++){
            Wheels[i].transform.Rotate(-10, 0, 0);
        }
    }

    private IEnumerator SetCheckDistance(){
        yield return new WaitForSeconds(1.0f);
        m_checkDistance = true;
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
}
