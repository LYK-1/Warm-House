using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 赛道道具刷新脚本，负责浮动展示、拾取、隐藏与重生。
public class TrackPickupRespawn : MonoBehaviour
{
    // 赛道道具刷新脚本：负责浮动显示、拾取判定、隐藏和重生。
    [SerializeField] private int itemAmountIndex = -1;
    [SerializeField] private float bobHeight = 0.2f;
    [SerializeField] private float bobSpeed = 0.2f;
    [SerializeField] private float respawnDelay = 5f;

    private static readonly string[] CollectorTags =
    {
        "Player",
        "Player1",
        "Player2",
        "Player3",
        "Player4",
        "AI1",
        "AI2",
        "AI3"
    };

    private Vector3 m_spawnPosition;
    private Quaternion m_spawnRotation;
    private Vector3[] m_bobTargets;
    private int m_targetIndex = 1;
    private bool m_isCollected;
    private bool m_isInitialized;
    private Renderer[] m_renderers;
    private Collider[] m_colliders;
    private readonly Dictionary<int, int> m_activeCollectorCounts = new Dictionary<int, int>();
    private readonly HashSet<int> m_consumedCollectorRoots = new HashSet<int>();

    // Awake：初始化组件和运行时状态。
    private void Awake()
    {
        Initialize();
    }

    // Initialize：初始化道具出生位置和浮动点。
    private void Initialize()
    {
        // 只初始化一次，把出生位置、旋转和浮动目标点全部记录下来。
        if (m_isInitialized)
        {
            return;
        }

        m_spawnPosition = transform.position;
        m_spawnRotation = transform.rotation;
        m_bobTargets = new[]
        {
            m_spawnPosition,
            m_spawnPosition + Vector3.up * bobHeight,
            m_spawnPosition - Vector3.up * bobHeight,
            m_spawnPosition
        };
        m_renderers = GetComponentsInChildren<Renderer>(true);
        m_colliders = GetComponentsInChildren<Collider>(true);
        m_targetIndex = 1;
        m_isCollected = false;
        m_isInitialized = true;
    }

    // Update：每帧更新主逻辑。
    private void Update()
    {
        // 未被拾取时，道具会在几个高度点之间来回浮动，看起来更有“悬浮感”。
        if (!m_isInitialized || m_isCollected)
        {
            return;
        }

        Vector3 target = m_bobTargets[m_targetIndex];
        transform.position = Vector3.MoveTowards(transform.position, target, bobSpeed * Time.deltaTime);

        if ((transform.position - target).sqrMagnitude <= 0.000001f)
        {
            m_targetIndex++;
            if (m_targetIndex >= m_bobTargets.Length)
            {
                m_targetIndex = 1;
            }
        }
    }

    // OnTriggerEnter：处理触发器进入事件。
    private void OnTriggerEnter(Collider other)
    {
        // 只允许合法的赛车或 AI 进入拾取流程。
        if (other == null)
        {
            return;
        }

        if (!IsCollector(other))
        {
            return;
        }

        int collectorRootId = GetCollectorRootId(other);
        RegisterCollectorRoot(collectorRootId);

        if (m_isCollected || m_consumedCollectorRoots.Contains(collectorRootId))
        {
            return;
        }

        m_consumedCollectorRoots.Add(collectorRootId);
        // 立刻加锁，避免同一辆车的多个碰撞体在同一帧重复拾取。
        m_isCollected = true;
        SetVisible(false);
        AwardItemIfPlayer(other);
        StartCoroutine(RespawnRoutine());
    }

    // RespawnRoutine：等待后重生道具。
    private IEnumerator RespawnRoutine()
    {
        // 道具消失一段时间后重新出现在原点，并恢复可见状态。
        yield return new WaitForSeconds(respawnDelay);

        transform.SetPositionAndRotation(m_spawnPosition, m_spawnRotation);
        m_targetIndex = 1;
        SetVisible(true);
        m_isCollected = false;
    }

    // OnTriggerExit：处理触发器离开事件。
    private void OnTriggerExit(Collider other)
    {
        // 离开拾取区域后，减少对应车辆的活动计数。
        if (m_isCollected || other == null || !IsCollector(other))
        {
            return;
        }

        int collectorRootId = GetCollectorRootId(other);
        UnregisterCollectorRoot(collectorRootId);
    }

    // SetVisible：控制道具可见性和碰撞体。
    private void SetVisible(bool visible)
    {
        // 道具被拾取时同时隐藏渲染器和碰撞体，重生后再一起恢复。
        if (m_renderers != null)
        {
            for (int i = 0; i < m_renderers.Length; i++)
            {
                if (m_renderers[i] != null)
                {
                    m_renderers[i].enabled = visible;
                }
            }
        }

        if (m_colliders != null)
        {
            for (int i = 0; i < m_colliders.Length; i++)
            {
                if (m_colliders[i] != null)
                {
                    m_colliders[i].enabled = visible;
                }
            }
        }
    }

    // IsCollector：判断目标是否允许拾取道具。
    private bool IsCollector(Collider other)
    {
        // 只要层级中出现允许的标签，就认为这是可拾取对象。
        if (HasAllowedTagInHierarchy(other.transform, CollectorTags))
        {
            return true;
        }

        return false;
    }

    // GetCollectorRootId：获取拾取者的唯一根节点标识。
    private int GetCollectorRootId(Collider other)
    {
        // 用刚体或根节点的实例 ID 作为“同一辆车”的唯一标识。
        if (other == null)
        {
            return 0;
        }

        Rigidbody attachedRigidbody = other.attachedRigidbody;
        if (attachedRigidbody != null)
        {
            return attachedRigidbody.gameObject.GetInstanceID();
        }

        Transform root = other.transform.root;
        if (root != null)
        {
            return root.gameObject.GetInstanceID();
        }

        return other.gameObject.GetInstanceID();
    }

    // RegisterCollectorRoot：注册拾取者的活动计数。
    private void RegisterCollectorRoot(int collectorRootId)
    {
        // 同一辆车可能有多个碰撞体进入，所以需要计数而不是只记录一次。
        if (collectorRootId == 0)
        {
            return;
        }

        if (m_activeCollectorCounts.TryGetValue(collectorRootId, out int count))
        {
            m_activeCollectorCounts[collectorRootId] = count + 1;
            return;
        }

        m_activeCollectorCounts.Add(collectorRootId, 1);
    }

    // UnregisterCollectorRoot：注销拾取者的活动计数。
    private void UnregisterCollectorRoot(int collectorRootId)
    {
        // 车辆离开触发区时，逐步减少计数，直到可以再次拾取。
        if (collectorRootId == 0)
        {
            return;
        }

        if (!m_activeCollectorCounts.TryGetValue(collectorRootId, out int count))
        {
            return;
        }

        count--;
        if (count > 0)
        {
            m_activeCollectorCounts[collectorRootId] = count;
            return;
        }

        m_activeCollectorCounts.Remove(collectorRootId);
        m_consumedCollectorRoots.Remove(collectorRootId);
    }

    // AwardItemIfPlayer：在玩家拾取时增加道具数量。
    private void AwardItemIfPlayer(Collider other)
    {
        // 只有玩家道具才会直接加数量，AI 经过这里只做拾取表现。
        if (itemAmountIndex < 0 || SaveProgress.PlayerlItemsAmounts == null || itemAmountIndex >= SaveProgress.PlayerlItemsAmounts.Length)
        {
            return;
        }

        if (!HasAllowedTagInHierarchy(other.transform, PlayerTags))
        {
            return;
        }

        SaveProgress.PlayerlItemsAmounts[itemAmountIndex]++;
        SpecialItemsPlayer.RefreshAllItemAmounts();
    }

    private static readonly string[] PlayerTags =
    {
        "Player",
        "Player1",
        "Player2",
        "Player3",
        "Player4"
    };

    // HasAllowedTagInHierarchy：检查层级中是否包含允许的标签。
    private bool HasAllowedTagInHierarchy(Transform start, string[] tags)
    {
        // 从当前物体一路向上找父节点，只要任意层级命中标签就算有效。
        if (start == null || tags == null)
        {
            return false;
        }

        Transform cursor = start;
        while (cursor != null)
        {
            if (HasAnyTag(cursor, tags))
            {
                return true;
            }

            cursor = cursor.parent;
        }

        Transform root = start.root;
        if (root != null)
        {
            Transform[] transforms = root.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < transforms.Length; i++)
            {
                if (HasAnyTag(transforms[i], tags))
                {
                    return true;
                }
            }
        }

        return false;
    }

    // HasAnyTag：判断目标是否命中任意标签。
    private bool HasAnyTag(Transform target, string[] tags)
    {
        if (target == null || tags == null)
        {
            return false;
        }

        for (int i = 0; i < tags.Length; i++)
        {
            if (target.CompareTag(tags[i]))
            {
                return true;
            }
        }

        return false;
    }
}
