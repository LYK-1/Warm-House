using UnityEngine;

// 导弹道具脚本，负责飞行、碰撞触发爆炸以及超时销毁。
public class MissileScript : MonoBehaviour
{
    // 爆炸特效由外部预制体提供，方便不同攻击道具复用同一套视觉资源。
    public GameObject Explosion;
    private Collider m_collider;

    // Start：完成启动初始化。
    void Start()
    {
        // 先关闭碰撞，再在极短延迟后开启，避免发射瞬间误判碰撞。
        Invoke("WaitToDestroy", 8);
        m_collider = GetComponent<Collider>();
        m_collider.enabled = false;
        Invoke("CollideOn", 0.15f);
    }

    // Update：每帧更新主逻辑。
    void Update()
    {
        // 导弹沿自身前方持续移动，速度固定，表现成直线飞行。
        transform.Translate(-80 * Time.deltaTime,0, 0);
    }

    // OnTriggerEnter：处理触发器进入事件。
    private void OnTriggerEnter(Collider other)
    {
        // 只要命中障碍物、玩家或 AI，就立即播放爆炸并销毁自身。
        if (other.CompareTag("Obstacle") || other.CompareTag("AI1") || other.CompareTag("AI2") || other.CompareTag("AI3") || other.CompareTag("Player1") || other.CompareTag("Player2") || other.CompareTag("Player3") || other.CompareTag("Player4"))
        {
            Instantiate(Explosion, transform.position, transform.rotation);
            Destroy(gameObject);
        }
    }

    // WaitToDestroy：超时后销毁导弹对象。
    private void WaitToDestroy()
    {
        // 保险措施：如果一直没碰到目标，就到时自动销毁。
        Destroy(gameObject);
    }

    // CollideOn：延迟开启导弹碰撞体。
    private void CollideOn()
    {
        // 延迟一小段时间后再启用碰撞体，确保导弹已经从车体边缘离开。
        m_collider.enabled = true;
    }
}
