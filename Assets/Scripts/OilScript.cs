using UnityEngine;

[RequireComponent(typeof(BoxCollider))]
public class OilScript : MonoBehaviour
{
    private const float StartSizeY = 0.1f;

    private BoxCollider m_BoxCollider;
    private float m_FixedEdgeY;

    public float GrowthSpeed = 0.7f;
    public float MaxSize = 9f;
    [SerializeField] private bool m_KeepMinYFixed = true;

    private void Awake()
    {
        m_BoxCollider = GetComponent<BoxCollider>();
    }

    private void Start()
    {
        m_FixedEdgeY = m_KeepMinYFixed
            ? m_BoxCollider.center.y - m_BoxCollider.size.y * 0.5f
            : m_BoxCollider.center.y + m_BoxCollider.size.y * 0.5f;

        SetColliderHeight(StartSizeY);
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
}
