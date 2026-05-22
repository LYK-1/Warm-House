using UnityEngine;

// 炸弹道具的生命周期脚本：定时引爆、处理范围燃烧效果，并在结束后销毁自身。
public class BombScript : MonoBehaviour
{
    // 爆炸特效、声音和作用半径都可以在 Inspector 里调节，便于不同地图复用。
    public GameObject Explosion;
    public GameObject ExplodeSound;
    public float BurnRadius = 5f;
    private BoxCollider m_collider;
    private bool m_exploded;

    // Start：完成启动初始化。
    void Start()
    {
        // 进入场景后先安排自动爆炸和自动销毁，避免道具一直留在赛道上。
        Invoke("Explode", 3);
        Invoke("DestroyBomb", 3.5f);
        m_collider = GetComponent<BoxCollider>();
    }

    // TriggerBurnOnCar：让命中的车辆进入燃烧状态。
    private void TriggerBurnOnCar(GameObject target)
    {
        // 通过车体任意子物体向上找到 ObstacleSound，再统一让整车进入燃烧状态。
        ObstacleSound obstacleSound = target.GetComponentInParent<ObstacleSound>();
        if (obstacleSound != null)
        {
            obstacleSound.TriggerBurn();
        }
    }

    // TriggerBurnInRadius：对周围车辆应用燃烧效果。
    private void TriggerBurnInRadius()
    {
        // 用物理球形范围扫描炸弹周围的目标，让附近车辆都受到燃烧影响。
        Collider[] hits = Physics.OverlapSphere(transform.position, BurnRadius);
        foreach (Collider hit in hits)
        {
            TriggerBurnOnCar(hit.gameObject);
        }
    }

    // Explode：触发炸弹爆炸并释放效果。
    private void Explode()
    {
        // 已经爆炸过就直接退出，避免重复播放特效和重复触发伤害。
        if (m_exploded)
        {
            return;
        }

        m_exploded = true;
        // 同时生成爆炸特效和爆炸音效，再对附近车辆施加燃烧效果。
        Instantiate(Explosion, transform.position, Quaternion.identity);
        Instantiate(ExplodeSound, transform.position, Quaternion.identity);
        TriggerBurnInRadius();
    }

    // DestroyBomb：延迟销毁炸弹对象。
    private void DestroyBomb()
    {
        // 先缩小碰撞体，再延迟销毁，避免视觉上突然消失过于生硬。
        m_collider.size = new Vector3(0.24f, 0.24f, 0.24f);
        m_exploded = true;
        Destroy(gameObject, 0.5f);
    }

    // OnTriggerEnter：处理触发器进入事件。
    private void OnTriggerEnter(Collider other)
    {
        // 炸弹在飞行或摆放时如果提前撞到赛车，就立即引爆，不必等倒计时结束。
        if (m_exploded)
        {
            return;
        }

        ObstacleSound obstacleSound = other.GetComponentInParent<ObstacleSound>();
        if (obstacleSound != null)
        {
            m_exploded = true;
            // 这里和定时爆炸保持一致：特效、声音、燃烧效果一起触发。
            Instantiate(Explosion, transform.position, Quaternion.identity);
            Instantiate(ExplodeSound, transform.position, Quaternion.identity);
            obstacleSound.TriggerBurn();
            Destroy(gameObject);
        }
    }
}
