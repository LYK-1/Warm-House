using System;
using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class ObstacleSound : MonoBehaviour
{
    public AudioSource m_audioSource;
    private bool m_hasHit;
    public AudioClip FenceHit;
    public AudioClip Splat;
    public AudioClip Return;
    private Rigidbody m_rigidbody;
    private bool m_hasPlayed;
    private bool m_hasBurned;
    public GameObject BurnSmoke;
    public GameObject Explosion;
    public GameObject EngineRunning;
    public AudioClip ExplodeSound;
    public bool IsPlayer =false;
    private NavMeshAgent m_agent;
    private bool AIRotating;
    public AudioClip SpinSound;
    private bool m_spinPlayed;
    private bool m_isMissileHit;
    private Coroutine m_boxingGloveSpinCoroutine;

    public bool AIisHit;
    public bool HitByBoxingGlove;

    private bool m_oilSound;
    private bool m_isOilSpinning;
    private bool m_isOilContact;
    private OilScript m_currentOilScript;
    private Coroutine m_oilSpinCoroutine;
    void Start()
    {
        m_rigidbody = GetComponent<Rigidbody>();
        if(!IsPlayer){
            m_agent = GetComponent<NavMeshAgent>();
        }
    }

    public void TriggerBurn()
    {
        if (m_hasBurned)
        {
            return;
        }

        m_hasBurned = true;

        if (BurnSmoke != null)
        {
            BurnSmoke.SetActive(true);
        }

        if (EngineRunning != null)
        {
            EngineRunning.SetActive(false);
        }

        StartCoroutine(StopTheKart());
    }

    private void Update()
    {
        if (AIRotating)
        {
            transform.Rotate(0, 25, 0);
            if (!m_spinPlayed)
            {
                StartCoroutine(StopRotating());
            }
        }
        if (m_isOilSpinning)
        {
            transform.Rotate(0, 25, 0);
        }
        if (HitByBoxingGlove)
        {
            if (IsPlayer)
            {
                if (m_rigidbody != null)
                {
                    Vector3 angularVelocity = m_rigidbody.angularVelocity;
                    angularVelocity.y = Mathf.Max(angularVelocity.y, 6f);
                    m_rigidbody.angularVelocity = angularVelocity;
                }
            }
            if (!IsPlayer)
            {
                transform.Rotate(0, 25, 0);
            }
        }
    }      

    private void OnTriggerEnter(Collider collision){
        if (collision.gameObject.CompareTag("Glove"))
        {
            StartBoxingGloveSpin();
            return;
        }

        if (collision.gameObject.CompareTag("Obstacle")){
            if (!m_hasHit){
                if (!m_audioSource.isPlaying){
                    m_audioSource.clip = FenceHit;
                    m_audioSource.Play();
                    if (!IsPlayer){
                        m_agent.speed =0;
                    }
                    m_hasHit = true;
                }
            }
        }
        if (collision.gameObject.CompareTag("Splat")){
            if(!m_hasHit){
                if (!m_audioSource.isPlaying && !m_hasPlayed){
                    m_audioSource.clip= Splat;
                    m_audioSource.Play();
                    m_hasHit = true;
                    transform.localScale=new Vector3(2.5f,0.3f,1.8f);
                    if (IsPlayer)
                    {
                        m_rigidbody.isKinematic = true;
                    }
                    else
                    {
                        AIisHit = true;
                        m_agent.speed = 0;
                        m_hasPlayed = true;
                    }
                }
            }
        }

        if (IsPlayerTag(collision.gameObject))
        {
            if (collision.gameObject.CompareTag("Glove"))
            {
                return;
            }

            AIisHit = true;
            m_agent.speed = 5.5f;
            AIRotating = true;
        }
        if (collision.gameObject.CompareTag("AI1") || collision.gameObject.CompareTag("AI2") || collision.gameObject.CompareTag("AI3")){
            StartCoroutine(PlayerReact());
        }

        if (collision.gameObject.CompareTag("Missile"))
        {
            if (!m_isMissileHit)
            {
                StartCoroutine(MissileHit());
            }
        }

        if (collision.gameObject.CompareTag("Burn"))
        {
            TriggerBurn();
        }
    }

    private void OnTriggerStay(Collider collision)
    {
        if (collision.gameObject.CompareTag("Oil"))
        {
            m_currentOilScript = collision.GetComponent<OilScript>();
            if (IsPlayer && collision != null)
            {
                transform.Rotate(0, 1.5f, 0);
                return;
            }

            StartOilSpin();
        }
    }

    private void OnTriggerExit(Collider collision){
        if (collision.gameObject.CompareTag("Obstacle")){
            if(m_hasHit){
                m_hasHit = false;
            }
        }
        if (collision.gameObject.CompareTag("Splat")){
            if (m_hasHit){
                m_hasHit = false;
                StartCoroutine(ResetKart());
            }
        }
        if (collision.gameObject.CompareTag("Oil"))
        {
            m_oilSound = false;
            m_isOilContact = false;
            m_currentOilScript = null;
            StopOilSpin();
        }
    }

    private IEnumerator ResetKart(){
        yield return new WaitForSeconds(1.5f);
        transform.localScale = new Vector3(1,1, 1);
        if (IsPlayer){
            m_rigidbody.isKinematic = false;
        }
        if (!IsPlayer){
            AIisHit = false;
            m_agent.speed =25;
        }
        m_rigidbody.isKinematic = false;
        if (!m_audioSource.isPlaying && m_hasPlayed){
            m_audioSource.clip = Return;
            m_audioSource.Play();
            m_hasPlayed = false;
        }
    }

    private IEnumerator StopTheKart(){
        yield return new WaitForSeconds(0.2f);
        yield return new WaitForSeconds(3);
        Vector3 kartPosition = new Vector3(transform.position.x, transform.position.y + 1,transform.position.z);
        Instantiate(Explosion,kartPosition,Quaternion.identity);
        if (BurnSmoke != null)
        {
            BurnSmoke.SetActive(false);
        }
        if (m_audioSource != null && !m_audioSource.isPlaying){
            m_audioSource.clip = ExplodeSound;
            m_audioSource.Play();
        }
        if (m_rigidbody != null)
        {
            m_rigidbody.isKinematic = true;
        }
        if (!IsPlayer && m_agent != null)
        {
            m_agent.speed = 0;
            m_agent.isStopped = true;
        }
        transform.localScale = new Vector3(0, 0, 0);
        yield return new WaitForSeconds(2f);
        transform.localScale = new Vector3(1, 1, 1);
        if (EngineRunning != null)
        {
            EngineRunning.SetActive(true);
        }
        if (IsPlayer){
            m_rigidbody.isKinematic = false;
        }
        if (!IsPlayer){
            AIisHit = false;
            m_agent.speed =25;
            m_agent.isStopped = false;
        }
        m_rigidbody.isKinematic = false;
        if (m_rigidbody != null)
        {
            m_rigidbody.linearVelocity = Vector3.zero;
            m_rigidbody.angularVelocity = Vector3.zero;
        }
        if (m_audioSource != null)
        {
            m_audioSource.clip = Return;
            m_audioSource.Play();
        }
        m_hasBurned = false;
    }

    IEnumerator StopRotating()
    {
        if (m_spinPlayed)
        {
            yield break;
        }

        m_spinPlayed = true;

        if (m_audioSource != null && !m_audioSource.isPlaying)
        {
            m_audioSource.clip = SpinSound;
            m_audioSource.Play();
        }

        yield return new WaitForSeconds(3);
        AIRotating = false;
        if (IsPlayer)
        {
            if (m_rigidbody != null)
            {
                Vector3 angularVelocity = m_rigidbody.angularVelocity;
                angularVelocity.y = 0f;
                m_rigidbody.angularVelocity = angularVelocity;
            }
        }
        else if (m_agent != null)
        {
            m_agent.updateRotation = true;
            m_agent.speed = 20;
            AIisHit = false;
        }

        HitByBoxingGlove = false;
        m_spinPlayed = false;
        m_boxingGloveSpinCoroutine = null;
    }

    private void StartBoxingGloveSpin()
    {
        HitByBoxingGlove = true;

        if (!IsPlayer)
        {
            AIisHit = true;
            if (m_agent != null)
            {
                m_agent.speed = 0;
                m_agent.updateRotation = false;
            }
        }

        if (m_spinPlayed || m_boxingGloveSpinCoroutine != null)
        {
            return;
        }

        m_boxingGloveSpinCoroutine = StartCoroutine(StopRotating());
    }

    private void StartOilSpin()
    {
        if (IsPlayer || m_isOilContact || m_oilSpinCoroutine != null)
        {
            return;
        }

        if (!m_audioSource.isPlaying && !m_oilSound)
        {
            m_audioSource.clip = Splat;
            m_audioSource.Play();
            m_oilSound = true;
        }

        m_isOilContact = true;
        m_oilSpinCoroutine = StartCoroutine(OilSpinRoutine());
    }

    private void StopOilSpin()
    {
        if (m_oilSpinCoroutine != null)
        {
            StopCoroutine(m_oilSpinCoroutine);
            m_oilSpinCoroutine = null;
        }

        m_isOilContact = false;
        m_isOilSpinning = false;

        if (!IsPlayer)
        {
            AIisHit = false;
            m_agent.speed = 25;
        }
    }

    private IEnumerator OilSpinRoutine()
    {
        m_isOilSpinning = true;
        AIisHit = true;
        m_agent.speed = 0;

        float spinDuration = GetRemainingOilSpinDuration();
        yield return new WaitForSeconds(spinDuration);

        m_isOilSpinning = false;
        AIisHit = false;
        m_agent.speed = 25;
        m_isOilContact = false;
        m_oilSpinCoroutine = null;
    }

    private bool IsPlayerTag(GameObject target)
    {
        if (target == null)
        {
            return false;
        }

        return target.CompareTag("Player1")
            || target.CompareTag("Player2")
            || target.CompareTag("Player3")
            || target.CompareTag("Player4");
    }

    private float GetRemainingOilSpinDuration()
    {
        const float fallbackDuration = 3f;
        if (m_currentOilScript == null)
        {
            return fallbackDuration;
        }

        float remaining = m_currentOilScript.RemainingLifetime;
        if (remaining <= 0f)
        {
            return 0f;
        }

        return remaining;
    }

    IEnumerator PlayerReact()
    {
        m_rigidbody.constraints = RigidbodyConstraints.FreezeRotationX |RigidbodyConstraints.FreezeRotationZ | RigidbodyConstraints.FreezeRotationY;
        yield return new WaitForSeconds(1);
        m_rigidbody.constraints = RigidbodyConstraints.None;
    }

    IEnumerator MissileHit()
    {
        m_isMissileHit = true;

        EngineRunning.SetActive(false);

        if (m_audioSource != null)
        {
            m_audioSource.clip = ExplodeSound;
            m_audioSource.Play();
        }

        transform.localScale = new Vector3(2.5f, 0.3f, 1.8f);
        if (IsPlayer)
        {
            m_rigidbody.isKinematic = true;
        }
        else
        {
            AIisHit = true;
            m_agent.speed = 0;
        }

        yield return new WaitForSeconds(2);

        transform.localScale = new Vector3(1, 1, 1);
        EngineRunning.SetActive(true);
        if (IsPlayer)
        {
            m_rigidbody.isKinematic = false;
        }
        else
        {
            AIisHit = false;
            m_agent.speed = 25;
        }

        if (m_audioSource != null)
        {
            m_audioSource.clip = Return;
            m_audioSource.Play();
        }

        m_isMissileHit = false;
    }

}
