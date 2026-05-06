using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

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
    void Start()
    {
        
    }
    void Update()
    {
        
    }

    IEnumerator SwitchoffBoxingGloveRight()
    {
        yield return new WaitForSeconds(0.75f);
        GetComponent<ObstacleSound>().HitByBoxingGlove = false;
        BoxingGloveRight.SetActive(false);
    }

    IEnumerator SwitchoffBoxingGloveLeft()
    {
        yield return new WaitForSeconds(0.75f);
        GetComponent<ObstacleSound>().HitByBoxingGlove = false;
        BoxingGloveLeft.SetActive(false);
    }

    public void OnSpecialItemRight(InputValue button)
    {
        if (m_specialItemID == 0)
        {
            BoxingGloveRight.SetActive(true);
            StartCoroutine(SwitchoffBoxingGloveRight());
        }
        if (m_specialItemID == 1)
        {
            Instantiate(Missile, MissileSpawnPointRight.position, MissileSpawnPointRight.rotation);
        }
        if (m_specialItemID == 2)
        {
            Instantiate(Bomb, BombSpawnPointRight.position, BombSpawnPointRight.rotation);
        }
    }

    public void OnSpecialItemLeft(InputValue button)
    {
        if (m_specialItemID == 0)
        {
            BoxingGloveLeft.SetActive(true);
            StartCoroutine(SwitchoffBoxingGloveLeft());
        }
        if (m_specialItemID == 1)
        {
            Instantiate(Missile, MissileSpawnPointLeft.position, MissileSpawnPointLeft.rotation);
        }
        if (m_specialItemID == 2)
        {
            Instantiate(Bomb, BombSpawnPointLeft.position, BombSpawnPointLeft.rotation);
        }
    }

    public void OnSpecialItemChoose(InputValue button)
    {
        if (m_specialItemID < 4)
        {
            m_specialItemID++;
        }
        else
        {
            m_specialItemID = 0;
        }
    }
}
