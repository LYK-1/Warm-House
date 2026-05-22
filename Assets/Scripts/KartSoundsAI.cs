using UnityEngine;
using UnityEngine.AI;

// AI 赛车音效脚本，根据导航速度切换怠速和行驶音效。
public class KartSoundsAI : MonoBehaviour
{
    // AI 车的怠速和行驶音效，以及尾灯状态都在这里统一管理。
    public AudioSource IdleSound;
    public AudioSource DrivingSound;
    //Volume and pitch settings
    [Range(0.1f,1.0f)] public float DrivingSoundVolume=1.0f;
    [Range(0.1f,2.0f)] public float DrivingSoundMaxPitch = 1.0f;
    private NavMeshAgent m_agent;
    private float m_kartSpeed;
    public GameObject TailLights;
    // Start：完成启动初始化。
    void Start(){
        // 进入场景后先关闭尾灯自发光，再缓存导航代理。
        TailLights.GetComponent<Renderer>().material.DisableKeyword("_EMISSION");
        m_agent = GetComponent<NavMeshAgent>();
    }

    // Update：每帧更新主逻辑。
    void Update(){
        // AI 没有直接输入，所以用导航代理速度来驱动音效表现。
        m_kartSpeed =m_agent.speed / 100;
        PlayIdleSound();
        PlayDrivingSound();
    }
    // PlayIdleSound：播放对应音效或表现。
    private void PlayIdleSound(){
        // 速度越高，怠速声越弱。
        IdleSound.volume = Mathf.Lerp(0.4f,0.0f,m_kartSpeed * 4);
    }
    // PlayDrivingSound：播放对应音效或表现。
    private void PlayDrivingSound(){
        if (m_kartSpeed > 0.0f){
            // 根据当前速度调节音量和音高，让引擎声更像真实加速过程。
            DrivingSound.volume =Mathf.Lerp(0.1f,DrivingSoundVolume,m_kartSpeed*1.2f);
            DrivingSound.pitch =Mathf.Lerp(0.3f,DrivingSoundMaxPitch,m_kartSpeed +(Mathf.Sin(Time.time) *.1f));
        }
    }
}
