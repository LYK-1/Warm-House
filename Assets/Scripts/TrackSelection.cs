using UnityEngine;
using UnityEngine.SceneManagement;

public class TrackSelection : MonoBehaviour
{
    private AudioSource m_audioSource;
    public GameObject LoadingText;

    void Start()
    {
        LoadingText.SetActive(false);
        m_audioSource = GetComponent<AudioSource>();
    }

    public void TrackSelect(int trackIndex) 
    {
        SaveScript.TrackToPlay = trackIndex;
        m_audioSource.Play();
    }

    public void Race()
    {
        LoadingText.SetActive(true);
        SceneManager.LoadScene(SaveScript.TrackToPlay);
    }

    public void BackToMenu()
    {
        SceneManager.LoadScene(0);
    }
}
