using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UIElements;

public class RaceStart : MonoBehaviour
{
    public GameObject[] Playerlkart;
    public Transform PlayerlSpawnPoint;
    public GameObject TimelineHolder;
    public Text StartText;
    public Text Title;
    public GameObject[] AIKarts;
    public Transform[] AISpawnPoints;

    private readonly List<PlayerCartControl> m_playerControllers = new List<PlayerCartControl>();

    IEnumerator Start()
    {
        yield return null;

        if (TimelineHolder != null)
        {
            Destroy(TimelineHolder.gameObject);
        }
        SpawnAI();
        SpawnPlayers();
    }

    void SpawnPlayers()
    {
        Instantiate(Playerlkart[SaveScript.PlayerlKartSelected], PlayerlSpawnPoint.position, PlayerlSpawnPoint.rotation);
        CachePlayerControllers();
        StartCoroutine(Countdown());
    }

    void SpawnAI()
    {
        if (SaveScript.PlayerlKartSelected >= 3)
        {
            for (int i = 0; i < 3; i++)
            {
                Instantiate(AIKarts[i], AISpawnPoints[i].position, AISpawnPoints[i].rotation);
            }
        }
        if (SaveScript.PlayerlKartSelected <= 2)
        {
            for (int i = 3; i < AIKarts.Length; i++)
            {
                Instantiate(AIKarts[i], AISpawnPoints[i - 3].position, AISpawnPoints[i - 3].rotation);
            }
        }
    }

    private void CachePlayerControllers()
    {
        m_playerControllers.Clear();
        m_playerControllers.AddRange(FindObjectsByType<PlayerCartControl>(FindObjectsSortMode.None));
    }

    IEnumerator Countdown()
    {
        Title.text = "";
        StartText.text = "Get Ready";
        yield return new WaitForSeconds(1);
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
}
