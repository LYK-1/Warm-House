using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class TrackSelection : MonoBehaviour
{
    private AudioSource m_audioSource;
    public GameObject LoadingText;
    public GameObject FroestChoosen;
    public GameObject ShowdownChoosen;
    public GameObject DvelChoosen;

    void Start()
    {
        SetAllChosenImages(false);
        if (LoadingText != null)
        {
            LoadingText.SetActive(false);
        }
        m_audioSource = GetComponent<AudioSource>();
    }

    public void TrackSelect(int trackIndex) 
    {
        SaveScript.TrackToPlay = trackIndex;
        SetChosenImage(trackIndex);
        m_audioSource.Play();
    }

    public void Race()
    {
        StartCoroutine(LoadRaceScene());
    }

    public void BackToMenu()
    {
        SceneManager.LoadScene(0);
    }

    private IEnumerator LoadRaceScene()
    {
        if (LoadingText != null)
        {
            LoadingText.SetActive(true);
            Canvas.ForceUpdateCanvases();
        }

        yield return null;

        AsyncOperation loadOperation = SceneManager.LoadSceneAsync(SaveScript.TrackToPlay);
        while (!loadOperation.isDone)
        {
            yield return null;
        }
    }

    private void SetChosenImage(int trackIndex)
    {
        SetAllChosenImages(false);

        switch (trackIndex)
        {
            case 2:
                if (FroestChoosen != null)
                {
                    FroestChoosen.SetActive(true);
                }
                break;
            case 3:
                if (ShowdownChoosen != null)
                {
                    ShowdownChoosen.SetActive(true);
                }
                break;
            case 4:
                if (DvelChoosen != null)
                {
                    DvelChoosen.SetActive(true);
                }
                break;
        }
    }

    private void SetAllChosenImages(bool active)
    {
        if (FroestChoosen != null)
        {
            FroestChoosen.SetActive(active);
        }

        if (ShowdownChoosen != null)
        {
            ShowdownChoosen.SetActive(active);
        }

        if (DvelChoosen != null)
        {
            DvelChoosen.SetActive(active);
        }
    }
}
