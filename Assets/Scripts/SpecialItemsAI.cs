using UnityEngine;

public class SpecialItemsAI : MonoBehaviour
{
    public GameObject Bomb;
    public Transform BombSpawnPoint;
    public GameObject OilSpill;
    public Transform OilSpawnPoint;

    public void SpawnBomb()
    {
        Instantiate(Bomb, BombSpawnPoint.position, BombSpawnPoint.rotation);
    }

    public void Spawnoil()
    {
        Instantiate(OilSpill, OilSpawnPoint.position, OilSpawnPoint.rotation);
    }
}
