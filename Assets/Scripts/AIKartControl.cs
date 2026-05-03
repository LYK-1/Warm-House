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
    void Start(){
        m_agent = GetComponent<NavMeshAgent>();
        StartCoroutine(SetCheckDistance());
    }
    void Update(){
        m_agent.SetDestination(AIWaypoints[m_currentwaypoint].position);
        CheckDistanceToNextTarget();
        Rotatewheels();
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
        yield return new WaitForSeconds(0.1f);
        m_checkDistance = true;
    }
}
