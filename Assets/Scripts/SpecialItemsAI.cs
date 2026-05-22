using UnityEngine;

// AI 参赛车辆的道具释放入口，负责生成炸弹和油渍。
public class SpecialItemsAI : MonoBehaviour
{
    // 道具预制体和出生点都由 Inspector 配置，方便不同 AI 车共用这套逻辑。
    public GameObject Bomb;
    public Transform BombSpawnPoint;
    public GameObject OilSpill;
    public Transform OilSpawnPoint;

    // SpawnBomb：生成炸弹道具。
    public void SpawnBomb()
    {
        // 在 AI 车前方生成炸弹，让 AI 也具备攻击性。
        Instantiate(Bomb, BombSpawnPoint.position, BombSpawnPoint.rotation);
    }

    // Spawnoil：生成油污道具。
    public void Spawnoil()
    {
        // 在 AI 车后方或脚下生成油污，形成干扰路面。
        Instantiate(OilSpill, OilSpawnPoint.position, OilSpawnPoint.rotation);
    }
}
