using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

// 赛道选择界面脚本，负责切换赛道、加载比赛场景和返回菜单。
public class TrackSelection : MonoBehaviour
{
    private AudioSource m_audioSource;
    // 赛道选择界面上的加载提示、已选中高亮图，以及返回主菜单入口。
    public GameObject LoadingText;
    public GameObject FroestChoosen;
    public GameObject ShowdownChoosen;
    public GameObject DvelChoosen;

    // Start：完成启动初始化。
    void Start()
    {
        // 初始化时先清空所有“已选择”标记，避免上一次进入页面残留状态。
        SetAllChosenImages(false);
        if (LoadingText != null)
        {
            LoadingText.SetActive(false);
        }
        m_audioSource = GetComponent<AudioSource>();
    }

    // TrackSelect：选择赛道并高亮预览图。
    public void TrackSelect(int trackIndex) 
    {
        // 保存当前赛道编号，并高亮对应的预览图。
        SaveScript.TrackToPlay = trackIndex;
        SetChosenImage(trackIndex);
        m_audioSource.Play();
    }

    // Race：开始加载比赛场景。
    public void Race()
    {
        // 点击开始后异步加载对应赛道场景。
        StartCoroutine(LoadRaceScene());
    }

    // BackToMenu：返回主菜单。
    public void BackToMenu()
    {
        // 返回主菜单场景。
        SceneManager.LoadScene(0);
    }

    // LoadRaceScene：异步加载比赛场景。
    private IEnumerator LoadRaceScene()
    {
        // 先显示“加载中”文字，让切换场景的过程更自然。
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

    // SetChosenImage：设置或切换相关状态。
    private void SetChosenImage(int trackIndex)
    {
        // 根据赛道编号显示不同的高亮图片，方便玩家确认自己的选择。
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

    // SetAllChosenImages：设置或切换相关状态。
    private void SetAllChosenImages(bool active)
    {
        // 统一管理三张高亮图的显隐，避免重复代码。
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
