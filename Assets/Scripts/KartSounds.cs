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

        for (int i = 0; i < TireSmoke.Length; i++)
        {
            TireSmoke[i].GetComponent<Transform>().gameObject.SetActive(true);
            TireSmoke[i].Stop();
        }

        // 缓存 Renderer，用 sharedMaterial 避免创建临时实例
        m_TailLightRenderer = TailLights.GetComponent<Renderer>();
        m_TailLightRenderer.sharedMaterial.DisableKeyword("_EMISSION");
    }

    void Update()
    {
        m_kartSpeed = m_Rigidbody.linearVelocity.magnitude * 2.23693629f / 100;
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
        if (button.isPressed)
        {
            BrakeToAStop = true;
            m_TailLightRenderer.sharedMaterial.EnableKeyword("_EMISSION");
        }
        else
        {
            BrakeToAStop = false;
            m_TailLightRenderer.sharedMaterial.DisableKeyword("_EMISSION");
        }
    }

    public void OnDrift(InputValue value)
    {
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
}