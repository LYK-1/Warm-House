using UnityEngine;

[RequireComponent(typeof(BoxCollider))]
public class OilScript : MonoBehaviour
{
    private const float StartSizeY = 0.1f;

    private BoxCollider m_BoxCollider;
    private Destoryer m_Destroyer;
    private float m_FixedEdgeY;
    private float m_DestroyAtTime;

    public float GrowthSpeed = 0.7f;
    public float MaxSize = 9f;
    public float Lifetime = 10f;
    [SerializeField] private bool m_KeepMinYFixed = true;

    private void Awake()
    {
        m_BoxCollider = GetComponent<BoxCollider>();
        m_Destroyer = GetComponent<Destoryer>();
    }

    private void Start()
    {
        float destroyDelay = GetDestroyDelay();
        m_DestroyAtTime = Time.time + destroyDelay;

        m_FixedEdgeY = m_KeepMinYFixed
            ? m_BoxCollider.center.y - m_BoxCollider.size.y * 0.5f
            : m_BoxCollider.center.y + m_BoxCollider.size.y * 0.5f;

        SetColliderHeight(StartSizeY);

        Destroy(gameObject, destroyDelay);
    }

    private void Update()
    {
        float currentSizeY = m_BoxCollider.size.y;
        if (currentSizeY >= MaxSize)
        {
            return;
        }

        float targetSizeY = Mathf.Min(currentSizeY + GrowthSpeed * Time.deltaTime, MaxSize);
        SetColliderHeight(targetSizeY);
    }

    private void SetColliderHeight(float sizeY)
    {
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
        get { return Mathf.Max(0f, m_DestroyAtTime - Time.time); }
    }

    private float GetDestroyDelay()
    {
        if (m_Destroyer != null && m_Destroyer.TimeToDestroy > 0f)
        {
            return m_Destroyer.TimeToDestroy;
        }

        return Mathf.Max(0f, Lifetime);
    }
}
