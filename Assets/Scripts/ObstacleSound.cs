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

    void Start()
    {
        m_rigidbody = GetComponent<Rigidbody>();
        if(!IsPlayer){
            m_agent = GetComponent<NavMeshAgent>();
        }
    }

    private void Update()
    {
        if (AIRotating)
        {
            transform.Rotate(0, 25, 0);
            StartCoroutine(StopRotating());
        }
    }

    private void OnTriggerEnter(Collider collision){
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
                    m_rigidbody.isKinematic = true;
                    m_hasPlayed = true;
                }
            }
        }

        if (collision.gameObject.CompareTag("Player1"))
        {
            m_agent.speed = 5.5f;
            AIRotating = true;
        }
        if (collision.gameObject.CompareTag("AI1") || collision.gameObject.CompareTag("AI2") || collision.gameObject.CompareTag("AI3")){
            StartCoroutine(PlayerReact());
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
        if (collision.gameObject.CompareTag("Burn")){
            if (!m_hasBurned){
                m_hasBurned = true;
                StartCoroutine(StopTheKart());
                BurnSmoke.SetActive(true);
            }
        }
    }

    private IEnumerator ResetKart(){
        yield return new WaitForSeconds(1.5f);
        transform.localScale = new Vector3(1,1, 1);
        if (IsPlayer){
            m_rigidbody.isKinematic = false;
        }
        if (!IsPlayer){
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
        m_hasBurned = true;
        yield return new WaitForSeconds(3);
        EngineRunning.SetActive(false);
        BurnSmoke.SetActive(false);
        m_hasBurned =false;
        if (IsPlayer){
            m_rigidbody.isKinematic = true;
        }
        if (!IsPlayer){
            m_agent.speed =0;
        }           
        m_rigidbody.isKinematic =true;
        Vector3 kartPosition = new Vector3(transform.position.x, transform.position.y + 1,transform.position.z);
        Instantiate(Explosion,kartPosition,Quaternion.identity);
        if (!m_audioSource.isPlaying){
            m_audioSource.clip = ExplodeSound;
            m_audioSource.Play();
        }
        transform.localScale =new Vector3(0,0,0);
        yield return new WaitForSeconds(2);
        transform.localScale=new Vector3(1,1,1);
        EngineRunning.SetActive(true);
        if (IsPlayer){
            m_rigidbody.isKinematic = false;
        }
        if (!IsPlayer){
            m_agent.speed =25;
        }
        m_rigidbody.isKinematic = false;
        if (!m_audioSource.isPlaying)
        m_audioSource.clip= Return;
        m_audioSource.Play();
    }

    IEnumerator StopRotating()
    {
        if (!m_audioSource.isPlaying && !m_spinPlayed)
        {
            m_audioSource.clip = SpinSound;
            m_audioSource.Play();
            m_spinPlayed = true;
            yield return new WaitForSeconds(3);
            AIRotating = false;
            m_agent.speed = 10;
            m_spinPlayed = false;
        }
    }

    IEnumerator PlayerReact()
    {
        m_rigidbody.constraints = RigidbodyConstraints.FreezeRotationX |RigidbodyConstraints.FreezeRotationZ;
        yield return new WaitForSeconds(1);
        m_rigidbody.constraints = RigidbodyConstraints.None;
    }
}
