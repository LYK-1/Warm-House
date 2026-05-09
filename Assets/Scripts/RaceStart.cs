using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class RaceStart : MonoBehaviour
{
    public GameObject Playerlkart;
    public Transform PlayerlSpawnPoint;
    public GameObject TimelineHolder;
    public Text StartText;
    public Text Title;

    private readonly List<PlayerCartContaol> m_playerControllers = new List<PlayerCartContaol>();

    IEnumerator Start()
    {
        // Let the Timeline finish its last frame before we tear it down and start the race.
        yield return null;

        if (TimelineHolder != null)
        {
            Destroy(TimelineHolder.gameObject);
        }

        SpawnPlayers();
    }

    void SpawnPlayers()
    {
        Instantiate(Playerlkart, PlayerlSpawnPoint.position, PlayerlSpawnPoint.rotation);
        CachePlayerControllers();
        StartCoroutine(Countdown());
    }

    private void CachePlayerControllers()
    {
        m_playerControllers.Clear();
        m_playerControllers.AddRange(FindObjectsByType<PlayerCartContaol>(FindObjectsSortMode.None));
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
            PlayerCartContaol controller = m_playerControllers[i];
            if (controller != null)
            {
                controller.SetForwardCameraActive();
            }
        }
    }
}
