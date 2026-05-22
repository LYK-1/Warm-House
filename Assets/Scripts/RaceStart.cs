using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UIElements;

// 比赛初始化脚本，负责生成玩家和 AI、播放倒计时并启动比赛。
public class RaceStart : MonoBehaviour
{
    // 比赛开始阶段的统一入口：生成玩家、AI，然后播放倒计时并正式开赛。
    public GameObject[] Playerlkart;
    public Transform[] PlayerSpawnPoints;
    public GameObject TimelineHolder;
    public Text StartText;
    public Text Title;
    public GameObject[] AIKarts;
    public Transform[] AISpawnPoints;

    private static readonly string[] PlayerParticipantTags = new string[4] { "Player1", "Player2", "Player3", "Player4" };
    private static readonly string[] AIPlayerTags = new string[3] { "AI1", "AI2", "AI3" };

    private readonly List<PlayerCartControl> m_playerControllers = new List<PlayerCartControl>();

    // Start：完成启动初始化。
    IEnumerator Start()
    {
        // 等一帧再执行，确保菜单阶段传进来的配置已经准备好。
        yield return null;

        if (TimelineHolder != null)
        {
            Destroy(TimelineHolder.gameObject);
        }
        SpawnPlayers();
    }

    // CachePlayerControllers：缓存当前场景中的玩家控制器。
    private void CachePlayerControllers()
    {
        // 生成赛车后统一缓存控制器，倒计时结束时会用来切换相机。
        m_playerControllers.Clear();
        m_playerControllers.AddRange(FindObjectsByType<PlayerCartControl>(FindObjectsSortMode.None));
    }

    // Countdown：播放比赛倒计时。
    IEnumerator Countdown()
    {
        // 倒计时文字和正式比赛开始的时机都放在这里控制。
        Title.text = "";
        StartText.text = "3";
        yield return new WaitForSeconds(1);
        StartText.text = "2";
        SetForwardCamerasActive();
        yield return new WaitForSeconds(1);
        StartText.text = "1";
        yield return new WaitForSeconds(1);
        StartText.text = "Go!";
        SaveProgress.RaceHasStarted = true;
        yield return new WaitForSeconds(1);
        StartText.text = "";
    }

    // SetForwardCamerasActive：切回所有玩家的前向摄像机。
    private void SetForwardCamerasActive()
    {
        // 倒计时快结束时，把所有玩家视角切回前向摄像机。
        for (int i = 0; i < m_playerControllers.Count; i++)
        {
            PlayerCartControl controller = m_playerControllers[i];
            if (controller != null)
            {
                controller.SetForwardCameraActive();
            }
        }
    }

    // SpawnPlayers：生成玩家赛车实例。
    void SpawnPlayers()
    {
        // 先检查资源是否完整，再根据单人或多人模式决定生成多少辆玩家赛车。
        if (Playerlkart == null || Playerlkart.Length == 0)
        {
            Debug.LogError("RaceStart: Playerlkart array is not configured.");
            return;
        }

        if (PlayerSpawnPoints == null || PlayerSpawnPoints.Length == 0)
        {
            Debug.LogError("RaceStart: PlayerSpawnPoints array is not configured.");
            return;
        }

        int playerCount = SaveScript.MultiPlayerMode ? Mathf.Clamp(SaveScript.MultiPlayerAmount, 1, 4) : 1;
        int spawnCount = Mathf.Min(playerCount, PlayerSpawnPoints.Length);

        for (int i = 0; i < spawnCount; i++)
        {
            if (!TryGetSpawnPoint(PlayerSpawnPoints, i, "Player", out Transform spawnPoint))
            {
                continue;
            }

            int kartIndex = Mathf.Clamp(GetPlayerKartIndex(i), 0, Playerlkart.Length - 1);
            GameObject kartPrefab = Playerlkart[kartIndex];
            if (kartPrefab == null)
            {
                Debug.LogWarning($"RaceStart: Player kart prefab at index {kartIndex} is null.");
                continue;
            }

            GameObject kartInstance = Instantiate(kartPrefab, spawnPoint.position, spawnPoint.rotation);
            ApplyParticipantTag(kartInstance, PlayerParticipantTags[i]);
        }

        int aiCount = SaveScript.SinglePlayerMode ? 3 : Mathf.Max(0, 4 - spawnCount);
        SpawnAI(aiCount);

        CachePlayerControllers();
        StartCoroutine(Countdown());
    }

    // SpawnAI：生成 AI 赛车实例。
    private void SpawnAI(int aiCount)
    {
        // AI 的数量会根据当前模式和玩家人数自动缩减，避免赛道过于拥挤。
        if (AIKarts == null || AISpawnPoints == null || AIKarts.Length == 0 || AISpawnPoints.Length == 0)
        {
            Debug.LogWarning("RaceStart: AIKarts or AISpawnPoints is not configured.");
            return;
        }

        if (aiCount <= 0)
        {
            return;
        }

        int spawnCount = Mathf.Min(Mathf.Min(aiCount, AISpawnPoints.Length), AIPlayerTags.Length);
        for (int i = 0; i < spawnCount; i++)
        {
            if (!TryGetSpawnPoint(AISpawnPoints, i, "AI", out Transform spawnPoint))
            {
                continue;
            }

            int kartIndex = Mathf.Clamp(i, 0, AIKarts.Length - 1);
            GameObject kartPrefab = AIKarts[kartIndex];
            if (kartPrefab == null)
            {
                Debug.LogWarning($"RaceStart: AI kart prefab at index {kartIndex} is null.");
                continue;
            }

            GameObject kartInstance = Instantiate(kartPrefab, spawnPoint.position, spawnPoint.rotation);
            ApplyParticipantTag(kartInstance, AIPlayerTags[i]);
        }
    }

    // ApplyParticipantTag：为赛车及其子物体统一打标签。
    private void ApplyParticipantTag(GameObject kartInstance, string participantTag)
    {
        // 给整辆车的所有子物体统一打标签，便于后续碰撞和排名识别。
        if (kartInstance == null || string.IsNullOrEmpty(participantTag))
        {
            return;
        }

        Transform[] transforms = kartInstance.GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < transforms.Length; i++)
        {
            Transform node = transforms[i];
            if (node != null)
            {
                node.gameObject.tag = participantTag;
            }
        }
    }

    // GetPlayerKartIndex：读取玩家已经选好的赛车编号。
    private int GetPlayerKartIndex(int playerSlot)
    {
        // 根据玩家槽位读取菜单阶段保存的赛车编号。
        switch (playerSlot)
        {
            case 0:
                return SaveScript.PlayerlKartSelected;
            case 1:
                return SaveScript.Player2KartSelected;
            case 2:
                return SaveScript.Player3KartSelected;
            case 3:
                return SaveScript.Player4KartSelected;
            default:
                return SaveScript.PlayerlKartSelected;
        }
    }

    // TryGetSpawnPoint：尝试获取有效出生点。
    private bool TryGetSpawnPoint(Transform[] spawnPoints, int index, string label, out Transform spawnPoint)
    {
        // 统一做空值和越界检查，避免生成阶段因为配置问题直接报错。
        spawnPoint = null;

        if (spawnPoints == null || index < 0 || index >= spawnPoints.Length)
        {
            Debug.LogWarning($"RaceStart: {label} spawn point[{index}] is missing.");
            return false;
        }

        spawnPoint = spawnPoints[index];
        if (spawnPoint == null)
        {
            Debug.LogWarning($"RaceStart: {label} spawn point[{index}] is null.");
            return false;
        }

        return true;
    }
}
