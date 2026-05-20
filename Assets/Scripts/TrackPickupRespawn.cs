using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TrackPickupRespawn : MonoBehaviour
{
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

    private void Awake()
    {
        Initialize();
    }

    private void Initialize()
    {
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

    private void Update()
    {
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

    private void OnTriggerEnter(Collider other)
    {
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
        // Lock immediately so multiple colliders on the same vehicle cannot
        // trigger repeated pickups in the same frame.
        m_isCollected = true;
        SetVisible(false);
        AwardItemIfPlayer(other);
        StartCoroutine(RespawnRoutine());
    }

    private IEnumerator RespawnRoutine()
    {
        yield return new WaitForSeconds(respawnDelay);

        transform.SetPositionAndRotation(m_spawnPosition, m_spawnRotation);
        m_targetIndex = 1;
        SetVisible(true);
        m_isCollected = false;
    }

    private void OnTriggerExit(Collider other)
    {
        if (m_isCollected || other == null || !IsCollector(other))
        {
            return;
        }

        int collectorRootId = GetCollectorRootId(other);
        UnregisterCollectorRoot(collectorRootId);
    }

    private void SetVisible(bool visible)
    {
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

    private bool IsCollector(Collider other)
    {
        if (HasAllowedTagInHierarchy(other.transform, CollectorTags))
        {
            return true;
        }

        return false;
    }

    private int GetCollectorRootId(Collider other)
    {
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

    private void RegisterCollectorRoot(int collectorRootId)
    {
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

    private void UnregisterCollectorRoot(int collectorRootId)
    {
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

    private void AwardItemIfPlayer(Collider other)
    {
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

    private bool HasAllowedTagInHierarchy(Transform start, string[] tags)
    {
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
