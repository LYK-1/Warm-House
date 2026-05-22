using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using TMPro;

// 玩家道具脚本，负责拾取、切换、释放和界面刷新。
public class SpecialItemsPlayer : MonoBehaviour
{
    // 玩家道具系统：负责拾取、切换、释放和界面数量刷新。
    private int m_specialItemID = 0;
    public GameObject BoxingGloveRight;
    public GameObject BoxingGloveLeft;
    public GameObject Missile;
    public Transform MissileSpawnPointRight;
    public Transform MissileSpawnPointLeft;
    public GameObject Bomb;
    public Transform BombSpawnPointRight;
    public Transform BombSpawnPointLeft;
    public GameObject OilSpill;
    public Transform OilSpawnPoint;

    public Image SpecialItemsDisplay;
    public Sprite[] SpecialItems;
    public TMP_Text[] ItemAmounts;

    private bool m_boxingGloveRightBusy;
    private bool m_boxingGloveLeftBusy;
    private RectTransform m_specialItemsRoot;
    private Vector2 m_specialItemsBaseAnchoredPosition;
    [SerializeField] private float TwoPlayerSpecialItemsYOffset = -40f;

    // Awake：初始化组件和运行时状态。
    private void Awake()
    {
        // 油污出生点和 UI 根节点都可能在子物体里，所以这里先统一解析。
        ResolveOilSpawnPoint();
        CacheSpecialItemsRoot();
        ApplySpecialItemsLayout();
    }

    // Start：完成启动初始化。
    void Start()
    {
        // 开局时重新解析一次位置，确保场景实例化后引用都已就位。
        ResolveOilSpawnPoint();
        ApplySpecialItemsLayout();

        // 默认先显示第一个道具图标，其余道具数量文本先隐藏。
        if (SpecialItemsDisplay != null && SpecialItems != null && SpecialItems.Length > 0)
        {
            SpecialItemsDisplay.sprite = SpecialItems[0];
        }

        if (ItemAmounts == null)
        {
            return;
        }

        for (int i = 0; i < ItemAmounts.Length; i++)
        {
            if (ItemAmounts[i] == null)
            {
                continue;
            }

            if (i < SaveProgress.PlayerlItemsAmounts.Length)
            {
                ItemAmounts[i].text = SaveProgress.PlayerlItemsAmounts[i].ToString();
            }

            ItemAmounts[i].gameObject.SetActive(false);
        }

        if (ItemAmounts.Length > 0 && ItemAmounts[0] != null)
        {
            ItemAmounts[0].gameObject.SetActive(true);
        }
    }

    // CacheSpecialItemsRoot：缓存道具 UI 的根节点。
    private void CacheSpecialItemsRoot()
    {
        // 先尝试从图标本身取 RectTransform，再退回到子层级里找。
        if (m_specialItemsRoot != null)
        {
            return;
        }

        if (SpecialItemsDisplay != null)
        {
            m_specialItemsRoot = SpecialItemsDisplay.rectTransform;
            if (m_specialItemsRoot != null)
            {
                m_specialItemsBaseAnchoredPosition = m_specialItemsRoot.anchoredPosition;
                return;
            }
        }

        m_specialItemsRoot = GetComponentInChildren<RectTransform>(true);
        if (m_specialItemsRoot != null)
        {
            m_specialItemsBaseAnchoredPosition = m_specialItemsRoot.anchoredPosition;
        }
    }

    // ApplySpecialItemsLayout：调整道具 UI 的布局位置。
    private void ApplySpecialItemsLayout()
    {
        // 单人和双人模式下的 HUD 位置略有不同，这里统一处理偏移。
        CacheSpecialItemsRoot();
        if (m_specialItemsRoot == null)
        {
            return;
        }

        Vector2 targetPosition = m_specialItemsBaseAnchoredPosition;
        if (IsTwoPlayerMode())
        {
            targetPosition.y += TwoPlayerSpecialItemsYOffset;
        }

        m_specialItemsRoot.anchoredPosition = targetPosition;
    }

    // IsTwoPlayerMode：判断当前是否为双人模式。
    private bool IsTwoPlayerMode()
    {
        return SaveScript.MultiPlayerMode && SaveScript.MultiPlayerAmount == 2;
    }

    // GetParticipantIndex：获取当前脚本所属的参赛者编号。
    private int GetParticipantIndex()
    {
        // 通过 PlayerCartControl 识别当前脚本属于几号玩家。
        PlayerCartControl playerCartControl = GetComponent<PlayerCartControl>();
        if (playerCartControl == null)
        {
            playerCartControl = GetComponentInParent<PlayerCartControl>();
        }

        if (playerCartControl == null)
        {
            return -1;
        }

        return playerCartControl.ParticipantIndex;
    }

    // SwitchoffBoxingGloveRight：延迟关闭右拳套特效。
    IEnumerator SwitchoffBoxingGloveRight()
    {
        yield return new WaitForSeconds(0.75f);
        if (BoxingGloveRight != null)
        {
            BoxingGloveRight.SetActive(false);
        }

        m_boxingGloveRightBusy = false;
    }

    // SwitchoffBoxingGloveLeft：延迟关闭左拳套特效。
    IEnumerator SwitchoffBoxingGloveLeft()
    {
        yield return new WaitForSeconds(0.75f);
        if (BoxingGloveLeft != null)
        {
            BoxingGloveLeft.SetActive(false);
        }

        m_boxingGloveLeftBusy = false;
    }

    // TryConsumeSpecialItem：尝试扣除一个道具数量。
    private bool TryConsumeSpecialItem(int itemID)
    {
        // 先检查数量是否足够，足够才扣除并刷新 UI。
        if (SaveProgress.PlayerlItemsAmounts == null || itemID < 0 || itemID >= SaveProgress.PlayerlItemsAmounts.Length)
        {
            return false;
        }

        if (SaveProgress.PlayerlItemsAmounts[itemID] <= 0)
        {
            ItemAmountsUpdate();
            return false;
        }

        SaveProgress.PlayerlItemsAmounts[itemID]--;
        ItemAmountsUpdate();
        return true;
    }

    // TryUseOil：尝试生成油污道具。
    private bool TryUseOil()
    {
        // 油污使用前需要同时满足：有预制体、有出生点、并且道具数量充足。
        if (OilSpill == null || OilSpawnPoint == null || !TryConsumeSpecialItem(3))
        {
            return false;
        }

        Instantiate(OilSpill, OilSpawnPoint.position, OilSpawnPoint.rotation);
        return true;
    }

    // OnSpecialItemRight：从右侧释放当前道具。
    public void OnSpecialItemRight(InputValue button)
    {
        // 右侧按钮对应右手释放逻辑，实际释放哪种道具取决于当前选择的道具编号。
        if (GetParticipantIndex() != 0)
        {
            return;
        }

        if (m_specialItemID == 0)
        {
            if (m_boxingGloveRightBusy || BoxingGloveRight == null || !TryConsumeSpecialItem(0))
            {
                return;
            }

            m_boxingGloveRightBusy = true;
            BoxingGloveRight.SetActive(true);
            StartCoroutine(SwitchoffBoxingGloveRight());
            return;
        }
        if (m_specialItemID == 1)
        {
            if (Missile == null || MissileSpawnPointRight == null || !TryConsumeSpecialItem(1))
            {
                return;
            }

            Instantiate(Missile, MissileSpawnPointRight.position, MissileSpawnPointRight.rotation);
            return;
        }
        if (m_specialItemID == 2)
        {
            if (Bomb == null || BombSpawnPointRight == null || !TryConsumeSpecialItem(2))
            {
                return;
            }

            Instantiate(Bomb, BombSpawnPointRight.position, BombSpawnPointRight.rotation);
            return;
        }

        if (m_specialItemID == 3)
        {
            TryUseOil();
        }
    }

    // OnSpecialItemLeft：从左侧释放当前道具。
    public void OnSpecialItemLeft(InputValue button)
    {
        // 左侧按钮同样是释放道具，只是拳套、导弹和炸弹从左边生成。
        if (GetParticipantIndex() != 0)
        {
            return;
        }

        if (m_specialItemID == 0)
        {
            if (m_boxingGloveLeftBusy || BoxingGloveLeft == null || !TryConsumeSpecialItem(0))
            {
                return;
            }

            m_boxingGloveLeftBusy = true;
            BoxingGloveLeft.SetActive(true);
            StartCoroutine(SwitchoffBoxingGloveLeft());
            return;
        }
        if (m_specialItemID == 1)
        {
            if (Missile == null || MissileSpawnPointLeft == null || !TryConsumeSpecialItem(1))
            {
                return;
            }

            Instantiate(Missile, MissileSpawnPointLeft.position, MissileSpawnPointLeft.rotation);
            return;
        }
        if (m_specialItemID == 2)
        {
            if (Bomb == null || BombSpawnPointLeft == null || !TryConsumeSpecialItem(2))
            {
                return;
            }

            Instantiate(Bomb, BombSpawnPointLeft.position, BombSpawnPointLeft.rotation);
            return;
        }

        if (m_specialItemID == 3)
        {
            TryUseOil();
        }
    }

    // OnSpecialItemChoose：切换当前道具类型。
    public void OnSpecialItemChoose(InputValue button)
    {
        // 切换当前选择的道具类型，并同步更新界面图标和数量提示。
        if (GetParticipantIndex() != 0)
        {
            return;
        }

        if (SpecialItems == null || SpecialItems.Length == 0)
        {
            return;
        }

        m_specialItemID++;
        if (m_specialItemID >= SpecialItems.Length)
        {
            m_specialItemID = 0;
        }

        if (SpecialItemsDisplay != null)
        {
            SpecialItemsDisplay.sprite = SpecialItems[m_specialItemID];
        }

        ItemAmountsUpdate();
    }

    // ItemAmountsUpdate：刷新道具数量 UI。
    public void ItemAmountsUpdate()
    {
        // 把全局道具数量同步回 UI，并只高亮当前选中的道具数量。
        if (ItemAmounts == null || SaveProgress.PlayerlItemsAmounts == null)
        {
            return;
        }

        for (int i = 0; i < ItemAmounts.Length; i++)
        {
            if (ItemAmounts[i] == null)
            {
                continue;
            }

            if (i < SaveProgress.PlayerlItemsAmounts.Length)
            {
                ItemAmounts[i].text = SaveProgress.PlayerlItemsAmounts[i].ToString();
            }

            ItemAmounts[i].gameObject.SetActive(false);
        }

        if (m_specialItemID >= 0 && m_specialItemID < ItemAmounts.Length && ItemAmounts[m_specialItemID] != null)
        {
            ItemAmounts[m_specialItemID].gameObject.SetActive(true);
        }
    }

    // RefreshAllItemAmounts：刷新场景内所有玩家的道具数量 UI。
    public static void RefreshAllItemAmounts()
    {
        // 所有玩家都可能受到道具增减影响，所以这里统一刷新场景中的所有实例。
        SpecialItemsPlayer[] players = Object.FindObjectsByType<SpecialItemsPlayer>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        if (players == null)
        {
            return;
        }

        for (int i = 0; i < players.Length; i++)
        {
            if (players[i] != null)
            {
                players[i].ItemAmountsUpdate();
            }
        }
    }

    // ResolveOilSpawnPoint：查找油污道具的出生点。
    private void ResolveOilSpawnPoint()
    {
        // 油污出生点支持手动拖拽，也支持按名字自动搜索。
        if (OilSpawnPoint != null)
        {
            return;
        }

        Transform directChild = transform.Find("SpawnPoint_Oil");
        if (directChild != null)
        {
            OilSpawnPoint = directChild;
            return;
        }

        OilSpawnPoint = FindChildRecursive(transform, "SpawnPoint_Oil");
    }

    // FindChildRecursive：递归查找子物体。
    private Transform FindChildRecursive(Transform root, string childName)
    {
        // 递归查找子物体，兼容更深层级的 UI 或挂点结构。
        if (root == null)
        {
            return null;
        }

        for (int i = 0; i < root.childCount; i++)
        {
            Transform child = root.GetChild(i);
            if (child == null)
            {
                continue;
            }

            if (child.name == childName)
            {
                return child;
            }

            Transform nested = FindChildRecursive(child, childName);
            if (nested != null)
            {
                return nested;
            }
        }

        return null;
    }
}
