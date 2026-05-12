using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class MenuButtons : MonoBehaviour
{
    private AudioSource m_audioSource;
    public GameObject[] MenuCanvasObjects;
    private int m_kartID;
    public GameObject DisplayKarts;
    public GameObject[] Karts;
    public GameObject[] BackForwardButtons;
    public string[] KartNames;
    public Text SelectTitle;
    public Text DisplayName;
    private bool m_kartsVisible = false;
    public float KartRotationSpeed = 25f;

    void Start()
    {
        m_audioSource = GetComponent<AudioSource>();
        DisplayKarts.SetActive(false);
    }

    void Update()
    {
        if (m_kartsVisible)
        {
            DisplayKarts.transform.Rotate(0, KartRotationSpeed * Time.deltaTime, 0);
        }
    }
    public void SinglePlayer()
    {
        m_audioSource.Play();
        SaveScript.MultiPlayerMode = false;
        SaveScript.SinglePlayerMode = true;
        StartCoroutine(DisplayPlayerSelect());
    }

    public void MultiPlayer()
    {
        m_audioSource.Play();
        SaveScript.SinglePlayerMode = false;
        SaveScript.MultiPlayerMode = true;
        StartCoroutine(DisplayPlayerSelect());
    }

    public void Exit()
    {
        m_audioSource.Play();
        StartCoroutine(WaitToExit());
    }

    IEnumerator DisplayPlayerSelect()
    {
        yield return new WaitForSeconds(0.5f);
        MenuCanvasObjects[0].SetActive(false);
        MenuCanvasObjects[1].SetActive(true);
        DisplayKarts.SetActive(true);
        if (SaveScript.SinglePlayerMode == true)
        {
            SelectTitle.text = "Choose your kart";
        }
        if (SaveScript.MultiPlayerMode == true)
        {
            SelectTitle.text = "Player 1 Choose your kart";
        }
        for (int i = 0; i < Karts.Length; i++)
        {
            Karts[i].SetActive(false);
        }
        Karts[0].SetActive(true);
        DisplayName.text = KartNames[0];
        BackForwardButtons[1].SetActive(true);
        m_kartID = 0;
        m_kartsVisible = true;
    }

    IEnumerator WaitToExit()
    {
        yield return new WaitForSeconds(2);
        Application.Quit();
    }

    public void NextButton()
    {
        for (int i = 0; i < Karts.Length; i++)
        {
            Karts[i].SetActive(false);
        }
        if (m_kartID < Karts.Length)
        {
            m_kartID++;
        }
        Karts[m_kartID].SetActive(true);
        DisplayName.text = KartNames[m_kartID];
        BackForwardButtons[0].SetActive(true);
        if (m_kartID == Karts.Length-1)
        {
            BackForwardButtons[1].SetActive(false);
        }
    }

    public void BackButton()
    {
        for (int i = 0; i < Karts.Length; i++)
        {
            Karts[i].SetActive(false);
        }
        if (m_kartID > 0)
        {
            m_kartID--;
        }
        Karts[m_kartID].SetActive(true);
        DisplayName.text = KartNames[m_kartID];
        BackForwardButtons[1].SetActive(true);
        if (m_kartID == 0)
        {
            BackForwardButtons[0].SetActive(false);
        }
    }

    public void Choose()
    {
        SaveScript.PlayerlKartSelected = m_kartID;
        SaveScript.PlayerlName = KartNames[m_kartID];
        SceneManager.LoadScene(1);
    }
}
