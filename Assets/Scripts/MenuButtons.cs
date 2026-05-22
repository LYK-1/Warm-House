using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

// 主菜单脚本，负责单人/多人入口、车款浏览和场景跳转。
public class MenuButtons : MonoBehaviour
{
    // 主菜单阶段负责模式选择、赛车浏览、人数选择和退出逻辑。
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

    public GameObject MultiPlayerAmountObject;
    public GameObject[] KartSelectobjects;
    private bool m_amountChosen;
    private int m_currentkart = 1;

    // Start：完成启动初始化。
    void Start()
    {
        // 进入菜单后先缓存音源，并把展示赛车先隐藏起来。
        m_audioSource = GetComponent<AudioSource>();
        DisplayKarts.SetActive(false);
    }

    // Update：每帧更新主逻辑。
    void Update()
    {
        // 只有在赛车展示面板打开时，才让模型持续旋转。
        if (m_kartsVisible)
        {
            DisplayKarts.transform.Rotate(0, KartRotationSpeed * Time.deltaTime, 0);
        }
    }

    // ChoosePlayerAmount：选择多人模式的人数。
    public void ChoosePlayerAmount(int amount)
    {
        // 多人模式先记录人数，再进入后续赛车选择流程。
        SaveScript.MultiPlayerAmount = amount;
        m_amountChosen = true;
        MultiPlayerAmountObject.SetActive(false);
        for (int i = 0; i < KartSelectobjects.Length; i++)
        {
            KartSelectobjects[i].SetActive(true);
        }
        StartCoroutine(DisplayPlayerSelect());
    }

    // SinglePlayer：切换到单人模式。
    public void SinglePlayer()
    {
        // 单人模式：记录状态后切到赛车选择界面。
        m_audioSource.Play();
        SaveScript.MultiPlayerMode = false;
        SaveScript.SinglePlayerMode = true;
        StartCoroutine(DisplayPlayerSelect());
    }

    // MultiPlayer：切换到多人模式。
    public void MultiPlayer()
    {
        // 多人模式：同样先记录状态，再走统一的选择流程。
        m_audioSource.Play();
        SaveScript.SinglePlayerMode = false;
        SaveScript.MultiPlayerMode = true;
        StartCoroutine(DisplayPlayerSelect());
    }

    // Exit：播放音效后退出游戏。
    public void Exit()
    {
        // 退出游戏前先播放点击音效，再延迟退出。
        m_audioSource.Play();
        StartCoroutine(WaitToExit());
    }

    // DisplayPlayerSelect：切换到车辆选择界面。
    IEnumerator DisplayPlayerSelect()
    {
        // 让 UI 切换稍微延迟一点，避免界面瞬间闪变。
        yield return new WaitForSeconds(0.5f);
        MenuCanvasObjects[0].SetActive(false);
        MenuCanvasObjects[1].SetActive(true);
        if (SaveScript.MultiPlayerMode == true && !m_amountChosen)
        {
            MultiPlayerAmountObject.SetActive(true);
            for (int i = 0; i < KartSelectobjects.Length; i++)
            {
                KartSelectobjects[i].SetActive(false);
            }
        }
        else 
        {
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
    }

    // WaitToExit：延迟后退出游戏。
    IEnumerator WaitToExit()
    {
        // 给音效留一点播放时间，再关闭应用。
        yield return new WaitForSeconds(2);
        Application.Quit();
    }

    // NextButton：切换到下一辆赛车。
    public void NextButton()
    {
        // 切换到下一辆赛车，循环浏览所有车模。
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

    // BackButton：切换到上一辆赛车。
    public void BackButton()
    {
        // 切换到上一辆赛车，并同步更新按钮显隐。
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

    // Choose：确认当前赛车选择。
    public void Choose()
    {
        // 最终确认按钮：单人直接进场，多人则按顺序记录每个玩家选的车。
        if (SaveScript.SinglePlayerMode)
        {
            SaveScript.PlayerlKartSelected = m_kartID;
            SaveScript.PlayerlName = KartNames[m_kartID];
            SceneManager.LoadScene(1);
        }
        if (SaveScript.MultiPlayerMode)
        {
            if (m_currentkart <= SaveScript.MultiPlayerAmount)
            {
                if (m_currentkart == 1)
                {
                    SaveScript.PlayerlKartSelected = m_kartID;
                }
                if (m_currentkart == 2)
                {
                    SaveScript.Player2KartSelected = m_kartID;
                }
                if (m_currentkart == 3)
                {
                    SaveScript.Player3KartSelected = m_kartID;
                }
                if (m_currentkart == 4)
                {
                    SaveScript.Player4KartSelected = m_kartID;
                }
                if (m_currentkart == SaveScript.MultiPlayerAmount)
                {
                    SceneManager.LoadScene(1);
                }
                if (m_currentkart < SaveScript.MultiPlayerAmount)
                {
                    m_currentkart++;
                    SelectTitle.text = "Player " + m_currentkart.ToString() + "Choose your kart";
                }
            }
        }
    }
}
