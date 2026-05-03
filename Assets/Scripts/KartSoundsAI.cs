using UnityEngine;
using UnityEngine.AI;

public class KartSoundsAI : MonoBehaviour
{
    public AudioSource IdleSound;
    public AudioSource DrivingSound;
    //Volume and pitch settings
    [Range(0.1f,1.0f)] public float DrivingSoundVolume=1.0f;
    [Range(0.1f,2.0f)] public float DrivingSoundMaxPitch = 1.0f;
    private NavMeshAgent m_agent;
    private float m_kartSpeed;
    public GameObject TailLights;
    void Start(){
        TailLights.GetComponent<Renderer>().material.DisableKeyword("_EMISSION");
        m_agent = GetComponent<NavMeshAgent>();
    }

    void Update(){
        m_kartSpeed =m_agent.speed / 100;
        PlayIdleSound();
        PlayDrivingSound();
    }
    private void PlayIdleSound(){
        IdleSound.volume = Mathf.Lerp(0.4f,0.0f,m_kartSpeed * 4);
    }
    private void PlayDrivingSound(){
        if (m_kartSpeed > 0.0f){
            DrivingSound.volume =Mathf.Lerp(0.1f,DrivingSoundVolume,m_kartSpeed*1.2f);
            DrivingSound.pitch =Mathf.Lerp(0.3f,DrivingSoundMaxPitch,m_kartSpeed +(Mathf.Sin(Time.time) *.1f));
        }
    }
}
