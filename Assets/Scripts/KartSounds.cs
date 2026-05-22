using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

// 玩家赛车音效控制脚本，负责引擎、漂移、倒车和刹停反馈。
public class KartSounds : MonoBehaviour
{
    // 玩家赛车音效控制脚本：负责引擎、倒车、漂移、刹车和尾灯联动。
    public AudioSource StartSound;
    public AudioSource IdleSound;
    public AudioSource DrivingSound;
    public AudioSource DriftSound;
    public AudioSource ReverseSound;

    [Range(0.1f, 1.0f)] public float DrivingSoundVolume = 1.0f;
    [Range(0.1f, 1.0f)] public float ReverseSoundVolume = 0.5f;
    [Range(0.1f, 1.0f)] public float DriftSoundVolume = 1.0f;
    [Range(0.1f, 2.0f)] public float DrivingSoundMaxPitch = 1.0f;
    [Range(0.1f, 2.0f)] public float ReverseSoundMaxPitch = 1.5f;

    private Rigidbody m_Rigidbody;
    private PlayerCartControl m_PlayerCartControl;
    private float m_kartSpeed;
    private Renderer m_TailLightRenderer;  // 缓存 Renderer

    [HideInInspector]
    public bool IsReversing = false;
    public bool BrakeToAStop = false;
    public ParticleSystem[] TireSmoke;
    public GameObject TailLights;
    public Vector2 m_drift;

    // Start：完成启动初始化。
    void Start()
    {
        // 启动时缓存刚体、车体控制脚本、烟雾和尾灯状态。
        m_Rigidbody = GetComponent<Rigidbody>();
        m_PlayerCartControl = GetComponent<PlayerCartControl>();

        for (int i = 0; i < TireSmoke.Length; i++)
        {
            TireSmoke[i].GetComponent<Transform>().gameObject.SetActive(true);
            TireSmoke[i].Stop();
        }

        // 缓存 Renderer，用 sharedMaterial 避免创建临时实例
        m_TailLightRenderer = TailLights.GetComponent<Renderer>();
        if (m_TailLightRenderer != null)
        {
            m_TailLightRenderer.material.DisableKeyword("_EMISSION");
        }

        MuteAndStopAudioSources();
    }

    // Update：每帧更新主逻辑。
    void Update()
    {
        // 通过刚体速度驱动音效，不依赖手动输入。
        m_kartSpeed = m_Rigidbody.linearVelocity.magnitude * 2.23693629f / 100;

        if (!IsKartGrounded() || m_kartSpeed <= 0.01f)
        {
            MuteAndStopAudioSources();
            return;
        }

        EnsureAudioSourcesPlaying();
        PlayIdleSound();
        PlayDrivingSound();
        PlayReversingSound();
        PlayBrakeToAStop();
    }

    // PlayIdleSound：播放对应音效或表现。
    private void PlayIdleSound()
    {
        // 速度越高，怠速声越弱。
        IdleSound.volume = Mathf.Lerp(0.4f, 0.0f, m_kartSpeed * 4);
    }

    // PlayDrivingSound：播放对应音效或表现。
    private void PlayDrivingSound()
    {
        if (!IsReversing && m_kartSpeed > 0.0f)
        {
            // 正向行驶时播放主引擎声，并根据速度调整音量和音高。
            ReverseSound.volume = 0.0f;
            DrivingSound.volume = Mathf.Lerp(0.1f, DrivingSoundVolume, m_kartSpeed * 1.2f);
            DrivingSound.pitch = Mathf.Lerp(0.3f, DrivingSoundMaxPitch, m_kartSpeed + (Mathf.Sin(Time.time) * .1f));
        }
    }

    // PlayReversingSound：播放对应音效或表现。
    private void PlayReversingSound()
    {
        if (IsReversing && m_kartSpeed > 0.0f)
        {
            // 倒车时切到倒车引擎声，正向引擎声同步静音。
            DrivingSound.volume = 0.0f;
            ReverseSound.volume = Mathf.Lerp(0.1f, ReverseSoundVolume, m_kartSpeed * 1.2f);
            ReverseSound.pitch = Mathf.Lerp(0.3f, ReverseSoundMaxPitch, m_kartSpeed + (Mathf.Sin(Time.time) * .1f));
        }
    }

    // PlayBrakeToAStop：播放对应音效或表现。
    private void PlayBrakeToAStop()
    {
        if (BrakeToAStop && m_kartSpeed < 0.50f)
        {
            // 刹车接近停止时播放漂移/刹车音效，并让轮胎烟雾重新播放。
            ReverseSound.volume = 0.0f;
            DrivingSound.volume = 0.0f;
            DriftSound.volume = Mathf.Lerp(0.1f, DriftSoundVolume, m_kartSpeed * 1.2f);
            for (int i = 0; i < TireSmoke.Length; i++)
            {
                if (TireSmoke[i].isStopped)
                    TireSmoke[i].Play();
            }
        }

        if (m_kartSpeed < 0.02f || !BrakeToAStop && m_drift.x == 0)
        {
            // 完全停止或未触发漂移时，关闭漂移音效和烟雾。
            DriftSound.volume = 0.0f;
            for (int i = 0; i < TireSmoke.Length; i++)
            {
                if (TireSmoke[i].isPlaying)
                    TireSmoke[i].Stop();
            }
        }
    }

    // OnBrake：响应刹车输入。
    public void OnBrake(InputValue button)
    {
        // 只有本地 1 号玩家才会响应这组输入。
        if (m_PlayerCartControl == null || m_PlayerCartControl.ParticipantIndex != 0)
        {
            return;
        }

        if (button.isPressed)
        {
            // 按下刹车时点亮尾灯并切换到刹车状态。
            BrakeToAStop = true;
            if (m_TailLightRenderer != null)
            {
                m_TailLightRenderer.material.EnableKeyword("_EMISSION");
            }
        }
        else
        {
            // 松开刹车后关闭尾灯自发光。
            BrakeToAStop = false;
            if (m_TailLightRenderer != null)
            {
                m_TailLightRenderer.material.DisableKeyword("_EMISSION");
            }
        }
    }

    // OnDrift：响应漂移输入。
    public void OnDrift(InputValue value)
    {
        // 漂移输入只对本地玩家生效，并会驱动烟雾和漂移音效。
        if (m_PlayerCartControl == null || m_PlayerCartControl.ParticipantIndex != 0)
        {
            return;
        }

        m_drift = value.Get<Vector2>();
        if (m_drift.x>0)
        {
            // 向右漂移时播放轮胎打滑效果。
            DriftSound.volume = Mathf.Lerp(0.1f,DriftSoundVolume,m_kartSpeed * 1.2f);
            for (int i=0; i<TireSmoke.Length;i++){
                if (TireSmoke[i].isStopped){
                    TireSmoke[i].Play();
                }
            }
        }
        if(m_drift.x < 0){
            // 向左漂移同样播放打滑和烟雾效果。
            DriftSound.volume = Mathf.Lerp(0.1f,DriftSoundVolume,m_kartSpeed * 1.2f);
            for (int i=0; i<TireSmoke.Length;i++){
                if (TireSmoke[i].isStopped){
                    TireSmoke[i].Play();
                }
            }
        }
        if(m_drift.x == 0f){
            // 回正后停止漂移音效和烟雾。
            for (int i=0; i<TireSmoke.Length;i++){
                if (TireSmoke[i].isPlaying){
                    TireSmoke[i].Stop();
                }
            }
        }
    }

    // IsKartGrounded：判断状态是否满足条件。
    private bool IsKartGrounded()
    {
        // 只要任意一个轮子接地，就认为赛车仍在正常贴地行驶。
        if (m_PlayerCartControl == null || m_PlayerCartControl.WheelColliders == null)
        {
            return true;
        }

        for (int i = 0; i < m_PlayerCartControl.WheelColliders.Length; i++)
        {
            WheelCollider wheelCollider = m_PlayerCartControl.WheelColliders[i];
            if (wheelCollider == null)
            {
                continue;
            }

            WheelHit wheelHit;
            if (wheelCollider.GetGroundHit(out wheelHit) && wheelHit.normal != Vector3.zero)
            {
                return true;
            }
        }

        return false;
    }

    // EnsureAudioSourcesPlaying：确保循环音源持续播放。
    private void EnsureAudioSourcesPlaying()
    {
        PlayLoopingSource(IdleSound);
        PlayLoopingSource(DrivingSound);
        PlayLoopingSource(DriftSound);
        PlayLoopingSource(ReverseSound);
    }

    // MuteAndStopAudioSources：把所有赛车音源静音并停止。
    private void MuteAndStopAudioSources()
    {
        MuteAndStopAudioSource(IdleSound);
        MuteAndStopAudioSource(DrivingSound);
        MuteAndStopAudioSource(DriftSound);
        MuteAndStopAudioSource(ReverseSound);
        MuteAndStopAudioSource(StartSound);
    }

    // PlayLoopingSource：播放对应音效或表现。
    private void PlayLoopingSource(AudioSource source)
    {
        // 循环音源如果被意外停掉，就重新播放。
        if (source != null && !source.isPlaying)
        {
            source.Play();
        }
    }

    // MuteAndStopAudioSource：静音并停止单个音源。
    private void MuteAndStopAudioSource(AudioSource source)
    {
        if (source == null)
        {
            return;
        }

        source.volume = 0.0f;
        if (source.isPlaying)
        {
            source.Stop();
        }
    }
}
