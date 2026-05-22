using UnityEngine;

[RequireComponent(typeof(BoxCollider))]
// 油渍道具的扩散与销毁逻辑，控制面积增长、碰撞体更新和生命周期结束。
public class OilScript : MonoBehaviour
{
    // 油污道具会随着时间慢慢扩散，同时在生命周期结束后自动销毁。
    private const float StartSizeY = 0.1f;

    private BoxCollider m_BoxCollider;
    private Destoryer m_Destroyer;
    private float m_FixedEdgeY;
    private float m_DestroyAtTime;

    public float GrowthSpeed = 0.7f;
    public float MaxSize = 9f;
    public float Lifetime = 10f;
    [SerializeField] private bool m_KeepMinYFixed = true;

    // Awake：初始化组件和运行时状态。
    private void Awake()
    {
        // 缓存碰撞体和销毁器组件，后面只需要直接修改尺寸和计时即可。
        m_BoxCollider = GetComponent<BoxCollider>();
        m_Destroyer = GetComponent<Destoryer>();
    }

    // Start：完成启动初始化。
    private void Start()
    {
        // 记录销毁时间，固定扩散边界，并把碰撞体缩成初始尺寸。
        float destroyDelay = GetDestroyDelay();
        m_DestroyAtTime = Time.time + destroyDelay;

        m_FixedEdgeY = m_KeepMinYFixed
            ? m_BoxCollider.center.y - m_BoxCollider.size.y * 0.5f
            : m_BoxCollider.center.y + m_BoxCollider.size.y * 0.5f;

        SetColliderHeight(StartSizeY);

        Destroy(gameObject, destroyDelay);
    }

    // Update：每帧更新主逻辑。
    private void Update()
    {
        // 每帧逐步扩大油污范围，直到达到上限为止。
        float currentSizeY = m_BoxCollider.size.y;
        if (currentSizeY >= MaxSize)
        {
            return;
        }

        float targetSizeY = Mathf.Min(currentSizeY + GrowthSpeed * Time.deltaTime, MaxSize);
        SetColliderHeight(targetSizeY);
    }

    // SetColliderHeight：调整油污碰撞体高度。
    private void SetColliderHeight(float sizeY)
    {
        // 让油污在增长时保持底边或顶边稳定，避免视觉上“漂浮”。
        Vector3 size = m_BoxCollider.size;
        size.y = sizeY;
        m_BoxCollider.size = size;

        Vector3 center = m_BoxCollider.center;
        float halfSize = sizeY * 0.5f;
        center.y = m_KeepMinYFixed ? m_FixedEdgeY + halfSize : m_FixedEdgeY - halfSize;
        m_BoxCollider.center = center;
    }

    public float RemainingLifetime
    {
        // 其他道具脚本可以通过这个属性拿到油污剩余的持续时间。
        get { return Mathf.Max(0f, m_DestroyAtTime - Time.time); }
    }

    // GetDestroyDelay：计算道具的销毁延迟。
    private float GetDestroyDelay()
    {
        // 如果挂了 Destoryer，就优先使用它设定的销毁时间。
        if (m_Destroyer != null && m_Destroyer.TimeToDestroy > 0f)
        {
            return m_Destroyer.TimeToDestroy;
        }

        return Mathf.Max(0f, Lifetime);
    }
}
