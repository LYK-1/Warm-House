using UnityEngine;
using System.Collections;

public class ProgressPoints : MonoBehaviour
{
    public bool AISpeedControl = false;
    public float AISetSpeed;
    public float AIMaxSpeed = 45;

    public bool SpawnBomb;
    public bool SpawnOil;

    private void OnTriggerEnter(Collider other)
    {
        PlayerCartContaol player = other.GetComponentInParent<PlayerCartContaol>();
        if (player != null && player.CompareTag("Player1"))
        {
            player.RegisterProgressRespawnPoint(transform);
        }

        if (other.CompareTag("AI1") || other.CompareTag("AI2") || other.CompareTag("AI3")){
            if (AISpeedControl)
            {
                AIKartControl aiKart = other.gameObject.GetComponentInParent<AIKartControl>();
                if (aiKart != null)
                {
                    aiKart.MaxSpeed = Random.Range(AISetSpeed, AIMaxSpeed);
                }
            }
            if (SpawnBomb)
            {
                other.GetComponentInParent<SpecialItemsAI>().SpawnBomb();
                SpawnBomb = false;
                StartCoroutine(ResetSpawnBomb());
            }
            if (SpawnOil)
            {
                other.GetComponentInParent<SpecialItemsAI>().Spawnoil();
                SpawnOil = false;
                StartCoroutine(ResetSpawnoil());
            }
        }
    }

    IEnumerator ResetSpawnBomb()
    {
        yield return new WaitForSeconds(4);
        SpawnBomb = true;
    }

    IEnumerator ResetSpawnoil()
    {
        yield return new WaitForSeconds(4);
        SpawnOil = true;
    }
}
