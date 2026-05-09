using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using TMPro;

public class SpecialItemsPlayer : MonoBehaviour
{
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

    private void Awake()
    {
        ResolveOilSpawnPoint();
    }

    void Start()
    {
        ResolveOilSpawnPoint();

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

    IEnumerator SwitchoffBoxingGloveRight()
    {
        yield return new WaitForSeconds(0.75f);
        if (BoxingGloveRight != null)
        {
            BoxingGloveRight.SetActive(false);
        }

        m_boxingGloveRightBusy = false;
    }

    IEnumerator SwitchoffBoxingGloveLeft()
    {
        yield return new WaitForSeconds(0.75f);
        if (BoxingGloveLeft != null)
        {
            BoxingGloveLeft.SetActive(false);
        }

        m_boxingGloveLeftBusy = false;
    }

    private bool TryConsumeSpecialItem(int itemID)
    {
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

    private bool TryUseOil()
    {
        if (OilSpill == null || OilSpawnPoint == null || !TryConsumeSpecialItem(3))
        {
            return false;
        }

        Instantiate(OilSpill, OilSpawnPoint.position, OilSpawnPoint.rotation);
        return true;
    }

    public void OnSpecialItemRight(InputValue button)
    {
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

    public void OnSpecialItemLeft(InputValue button)
    {
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

    public void OnSpecialItemChoose(InputValue button)
    {
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

    public void ItemAmountsUpdate()
    {
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

    private void ResolveOilSpawnPoint()
    {
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

    private Transform FindChildRecursive(Transform root, string childName)
    {
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
