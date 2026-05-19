using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UIElements;

public class RaceStart : MonoBehaviour
{
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

    IEnumerator Start()
    {
        yield return null;

        if (TimelineHolder != null)
        {
            Destroy(TimelineHolder.gameObject);
        }
        SpawnPlayers();
    }

    private void CachePlayerControllers()
    {
        m_playerControllers.Clear();
        m_playerControllers.AddRange(FindObjectsByType<PlayerCartControl>(FindObjectsSortMode.None));
    }

    IEnumerator Countdown()
    {
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

    private void SetForwardCamerasActive()
    {
        for (int i = 0; i < m_playerControllers.Count; i++)
        {
            PlayerCartControl controller = m_playerControllers[i];
            if (controller != null)
            {
                controller.SetForwardCameraActive();
            }
        }
    }

    void SpawnPlayers()
    {
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

    private void SpawnAI(int aiCount)
    {
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

    private void ApplyParticipantTag(GameObject kartInstance, string participantTag)
    {
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

    private int GetPlayerKartIndex(int playerSlot)
    {
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

    private bool TryGetSpawnPoint(Transform[] spawnPoints, int index, string label, out Transform spawnPoint)
    {
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
