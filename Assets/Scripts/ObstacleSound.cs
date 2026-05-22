using System;
using System.Collections;
using UnityEngine;
using UnityEngine.AI;

// 赛车受击与反馈脚本，处理碰撞、燃烧、打滑和旋转状态。
public class ObstacleSound : MonoBehaviour
{
    // 障碍物与赛车碰撞后的反馈中心：负责音效、燃烧、旋转和减速状态。
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
    // Start：完成启动初始化。
    void Start()
    {
        // 缓存刚体和导航代理，供后续碰撞反馈使用。
        // 先缓存刚体，后面所有受击状态都要靠它来控制运动。
        m_rigidbody = GetComponent<Rigidbody>();
        if(!IsPlayer){
            // AI 车辆额外缓存导航代理，便于受击时减速和停下。
            m_agent = GetComponent<NavMeshAgent>();
        }
    }

    // TriggerBurn：让赛车进入燃烧并减速状态。
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

    // Update：每帧更新主逻辑。
    private void Update()
    {
        // 根据当前受击状态更新旋转、翻滚和失控表现。
        // AI 被拳套命中后会进入持续旋转的受击状态。
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

    // OnTriggerEnter：处理触发器进入事件。
    private void OnTriggerEnter(Collider collision){
        // 进入触发器时，根据标签区分障碍物、道具和攻击命中。
        if (IsPropContact(collision))
        {
            return;
        }

        if (collision.gameObject.CompareTag("Glove"))
        {
            // 拳套命中会让目标进入短暂旋转状态。
            StartBoxingGloveSpin();
            return;
        }

        if (collision.gameObject.CompareTag("Obstacle")){
            // 撞到墙体或障碍物时播放刮碰音效，并让 AI 临时停速。
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
            // 压到油渍/泥浆会改变车体外形，并让车辆短时间失去控制。
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
            // AI 车如果撞到玩家，直接进入旋转受击状态。
            if (collision.gameObject.CompareTag("Glove"))
            {
                return;
            }

            AIisHit = true;
            m_agent.speed = 5.5f;
            AIRotating = true;
        }
        if (collision.gameObject.CompareTag("AI1") || collision.gameObject.CompareTag("AI2") || collision.gameObject.CompareTag("AI3")){
            // AI 之间相撞时也给出短暂反应，避免完全重叠。
            StartCoroutine(PlayerReact());
        }

        if (collision.gameObject.CompareTag("Missile"))
        {
            // 导弹命中时走单独的受击流程。
            if (!m_isMissileHit)
            {
                StartCoroutine(MissileHit());
            }
        }

        if (collision.gameObject.CompareTag("Burn"))
        {
            // 炸弹爆炸后会给目标附加燃烧状态。
            TriggerBurn();
        }
    }

    // OnTriggerStay：处理持续触发状态。
    private void OnTriggerStay(Collider collision)
    {
        // 持续接触油渍时，维持打滑旋转效果。
        if (IsPropContact(collision))
        {
            return;
        }

        if (collision.gameObject.CompareTag("Oil"))
        {
            // 持续压在油污上时，记录油污脚本并启动打滑旋转。
            m_currentOilScript = collision.GetComponent<OilScript>();
            if (IsPlayer && collision != null)
            {
                transform.Rotate(0, 1.5f, 0);
                return;
            }

            StartOilSpin();
        }
    }

    // OnTriggerExit：处理触发器离开事件。
    private void OnTriggerExit(Collider collision){
        // 离开触发器后，恢复部分受击状态。
        if (IsPropContact(collision))
        {
            return;
        }

        if (collision.gameObject.CompareTag("Obstacle")){
            // 离开障碍物后恢复碰撞标记，允许下次再次触发音效。
            if(m_hasHit){
                m_hasHit = false;
            }
        }
        if (collision.gameObject.CompareTag("Splat")){
            // 从油渍上离开后，先恢复尺寸，再播放回正音效。
            if (m_hasHit){
                m_hasHit = false;
                StartCoroutine(ResetKart());
            }
        }
        if (collision.gameObject.CompareTag("Oil"))
        {
            // 离开油污后清理协程并恢复速度。
            m_oilSound = false;
            m_isOilContact = false;
            m_currentOilScript = null;
            StopOilSpin();
        }
    }

    // ResetKart：恢复被压扁的赛车状态。
    private IEnumerator ResetKart(){
        // 受击结束后，延迟恢复赛车尺寸和控制状态。
        // 硬直结束后，恢复赛车外形和控制状态。
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

    // StopTheKart：执行燃烧后的爆炸与停机流程。
    private IEnumerator StopTheKart(){
        // 烧毁状态触发后，先生成爆炸再停掉赛车。
        // 燃烧状态最终会播放爆炸、停掉引擎并让赛车消失一段时间。
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

    // StopRotating：结束拳套命中的旋转受击。
    IEnumerator StopRotating()
    {
        // 拳套或旋转攻击只持续一小段时间，随后恢复正常朝向。
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

    // StartBoxingGloveSpin：启动拳套命中的旋转受击。
    private void StartBoxingGloveSpin()
    {
        // 拳套命中后，把目标标记成受击并启动旋转协程。
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

    // StartOilSpin：启动油污打滑受击。
    private void StartOilSpin()
    {
        // 油污只对 AI 有显著效果，触发后启动打滑和旋转协程。
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

    // StopOilSpin：停止油污打滑受击。
    private void StopOilSpin()
    {
        // 离开油污后清理协程并恢复速度。
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

    // OilSpinRoutine：执行油污打滑持续效果。
    private IEnumerator OilSpinRoutine()
    {
        // 油污打滑的持续时间和油污道具剩余寿命一致。
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

    // IsPlayerTag：判断状态是否满足条件。
    private bool IsPlayerTag(GameObject target)
    {
        // 把四个玩家标签统一视为“玩家目标”。
        if (target == null)
        {
            return false;
        }

        return target.CompareTag("Player1")
            || target.CompareTag("Player2")
            || target.CompareTag("Player3")
            || target.CompareTag("Player4");
    }

    // IsPropContact：判断状态是否满足条件。
    private bool IsPropContact(Collider collision)
    {
        // 赛道上的 prop 标签一般表示装饰或静态物件，单独过滤掉。
        if (collision == null)
        {
            return false;
        }

        Transform cursor = collision.transform;
        while (cursor != null)
        {
            if (cursor.tag == "prop")
            {
                return true;
            }

            cursor = cursor.parent;
        }

        return false;
    }

    // GetRemainingOilSpinDuration：读取油污剩余持续时间。
    private float GetRemainingOilSpinDuration()
    {
        // 油污剩余时间会直接影响 AI 打滑持续多久。
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

    // PlayerReact：处理短暂停顿的受击反应。
    IEnumerator PlayerReact()
    {
        // AI 互撞时短暂冻结旋转，避免车体瞬间翻飞。
        m_rigidbody.constraints = RigidbodyConstraints.FreezeRotationX |RigidbodyConstraints.FreezeRotationZ | RigidbodyConstraints.FreezeRotationY;
        yield return new WaitForSeconds(1);
        m_rigidbody.constraints = RigidbodyConstraints.None;
    }

    // MissileHit：执行导弹命中的受击流程。
    IEnumerator MissileHit()
    {
        // 导弹命中时走一套独立的硬直、缩放和恢复流程。
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
