using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public class KartSounds : MonoBehaviour
{
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

    void Start()
    {
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

    void Update()
    {
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

    private void PlayIdleSound()
    {
        IdleSound.volume = Mathf.Lerp(0.4f, 0.0f, m_kartSpeed * 4);
    }

    private void PlayDrivingSound()
    {
        if (!IsReversing && m_kartSpeed > 0.0f)
        {
            ReverseSound.volume = 0.0f;
            DrivingSound.volume = Mathf.Lerp(0.1f, DrivingSoundVolume, m_kartSpeed * 1.2f);
            DrivingSound.pitch = Mathf.Lerp(0.3f, DrivingSoundMaxPitch, m_kartSpeed + (Mathf.Sin(Time.time) * .1f));
        }
    }

    private void PlayReversingSound()
    {
        if (IsReversing && m_kartSpeed > 0.0f)
        {
            DrivingSound.volume = 0.0f;
            ReverseSound.volume = Mathf.Lerp(0.1f, ReverseSoundVolume, m_kartSpeed * 1.2f);
            ReverseSound.pitch = Mathf.Lerp(0.3f, ReverseSoundMaxPitch, m_kartSpeed + (Mathf.Sin(Time.time) * .1f));
        }
    }

    private void PlayBrakeToAStop()
    {
        if (BrakeToAStop && m_kartSpeed < 0.50f)
        {
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
            DriftSound.volume = 0.0f;
            for (int i = 0; i < TireSmoke.Length; i++)
            {
                if (TireSmoke[i].isPlaying)
                    TireSmoke[i].Stop();
            }
        }
    }

    public void OnBrake(InputValue button)
    {
        if (m_PlayerCartControl == null || m_PlayerCartControl.ParticipantIndex != 0)
        {
            return;
        }

        if (button.isPressed)
        {
            BrakeToAStop = true;
            if (m_TailLightRenderer != null)
            {
                m_TailLightRenderer.material.EnableKeyword("_EMISSION");
            }
        }
        else
        {
            BrakeToAStop = false;
            if (m_TailLightRenderer != null)
            {
                m_TailLightRenderer.material.DisableKeyword("_EMISSION");
            }
        }
    }

    public void OnDrift(InputValue value)
    {
        if (m_PlayerCartControl == null || m_PlayerCartControl.ParticipantIndex != 0)
        {
            return;
        }

        m_drift = value.Get<Vector2>();
        if (m_drift.x>0)
        {
            DriftSound.volume = Mathf.Lerp(0.1f,DriftSoundVolume,m_kartSpeed * 1.2f);
            for (int i=0; i<TireSmoke.Length;i++){
                if (TireSmoke[i].isStopped){
                    TireSmoke[i].Play();
                }
            }
        }
        if(m_drift.x < 0){
            DriftSound.volume = Mathf.Lerp(0.1f,DriftSoundVolume,m_kartSpeed * 1.2f);
            for (int i=0; i<TireSmoke.Length;i++){
                if (TireSmoke[i].isStopped){
                    TireSmoke[i].Play();
                }
            }
        }
        if(m_drift.x == 0f){
            for (int i=0; i<TireSmoke.Length;i++){
                if (TireSmoke[i].isPlaying){
                    TireSmoke[i].Stop();
                }
            }
        }
    }

    private bool IsKartGrounded()
    {
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

    private void EnsureAudioSourcesPlaying()
    {
        PlayLoopingSource(IdleSound);
        PlayLoopingSource(DrivingSound);
        PlayLoopingSource(DriftSound);
        PlayLoopingSource(ReverseSound);
    }

    private void MuteAndStopAudioSources()
    {
        MuteAndStopAudioSource(IdleSound);
        MuteAndStopAudioSource(DrivingSound);
        MuteAndStopAudioSource(DriftSound);
        MuteAndStopAudioSource(ReverseSound);
        MuteAndStopAudioSource(StartSound);
    }

    private void PlayLoopingSource(AudioSource source)
    {
        if (source != null && !source.isPlaying)
        {
            source.Play();
        }
    }

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
