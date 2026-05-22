using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

// 比赛进度管理脚本，统一记录圈数、检查点、参赛者和结算结果。
public class SaveProgress : MonoBehaviour
{
    public static SaveProgress Instance { get; private set; }
    public static readonly string[] ParticipantTags = new string[7] { "Player1", "Player2", "Player3", "Player4", "AI1", "AI2", "AI3" };
    public GameObject[] ProgressPointsItems;
    public static int[] CurrentLap = new int[7];
    public static int[] CurrentCheckpoint = new int[7];
    public static Transform[] ParticipantTransforms = new Transform[ParticipantTags.Length];
    public static bool[] ParticipantRegisteredThisRace = new bool[ParticipantTags.Length];

    public static bool Reset;
    private int m_resetAmounts;
    public static int LapNumber;

    public static int[] PlayerlItemsAmounts = new int[4] { 10, 30 ,20, 10 };

    public static bool RaceHasStarted;
    public int TotalLaps = 1;
    public static int MaxLaps = 1;
    public static bool RaCeHasFiniShed;

    public GameObject FinishUI;
    public Text PlayerlFinishPosition;
    public Text Winner;
    private Coroutine m_finishDisplayCoroutine;
    private bool m_finishDisplayQueued;
    private bool m_finishResultsShown;

    // Awake：初始化组件和运行时状态。
    private void Awake()
    {
        // 每局比赛开始前，重置圈数、检查点和比赛状态。
        // 每局比赛开始前重置所有静态状态，确保上一局的数据不会污染下一局。
        Instance = this;
        for (int i = 0; i < CurrentLap.Length; i++)
        {
            CurrentLap[i] = 0;
        }
        for (int i = 0; i < CurrentCheckpoint.Length; i++)
        {
            CurrentCheckpoint[i] = 0;
        }
        PlayerlItemsAmounts[0] = 10;
        PlayerlItemsAmounts[1] = 30;
        PlayerlItemsAmounts[2] = 20;
        PlayerlItemsAmounts[3] = 10;
        Reset = false;
        LapNumber = 1;
        RaceHasStarted = false;
        RaCeHasFiniShed = false;
        MaxLaps = Mathf.Max(1, TotalLaps);
    }

    // OnEnable：对象启用时重置状态。
    private void OnEnable()
    {
        // 场景重新启用时清空上一局的静态数据，避免脏状态残留。
        // 场景重新启用时清空所有参赛者缓存，避免残留引用导致排名异常。
        Array.Clear(CurrentLap, 0, CurrentLap.Length);
        Array.Clear(CurrentCheckpoint, 0, CurrentCheckpoint.Length);
        Array.Clear(ParticipantTransforms, 0, ParticipantTransforms.Length);
        Array.Clear(ParticipantRegisteredThisRace, 0, ParticipantRegisteredThisRace.Length);
        Reset = true;
        LapNumber = 0;
        m_resetAmounts = 0;
        RaceHasStarted = false;
        RaCeHasFiniShed = false;
        m_finishDisplayQueued = false;
        m_finishResultsShown = false;

        if (m_finishDisplayCoroutine != null)
        {
            StopCoroutine(m_finishDisplayCoroutine);
            m_finishDisplayCoroutine = null;
        }

        HideFinishUI();
    }

    // Start：完成启动初始化。
    void Start()
    {
        // 给赛道上的进度点编号，并标记起点线。
        // 给赛道上的每个检查点编号，并标记起点线，用于后续圈数计算。
        int progressNumber = 1;

        for (int i = 0; i < ProgressPointsItems.Length; i++)
        {
            GameObject pointObject = ProgressPointsItems[i];
            if (pointObject != null)
            {
                ProgressPoints progressPoint = pointObject.GetComponent<ProgressPoints>();
                if (progressPoint != null)
                {
                    progressPoint.ProgressNumber = progressNumber;
                    progressPoint.StartLine = progressNumber == 1;
                    m_resetAmounts++;
                }
            }

            progressNumber++;
        }

        Reset = false;
    }

    // OnDisable：对象禁用时收尾处理。
    private void OnDisable()
    {
        // 离开场景前停止结算协程，并清掉单例引用。
        if (m_finishDisplayCoroutine != null)
        {
            StopCoroutine(m_finishDisplayCoroutine);
            m_finishDisplayCoroutine = null;
        }

        if (Instance == this)
        {
            Instance = null;
        }
    }

    // QueueFinishDisplay：排队显示比赛结算界面。
    public void QueueFinishDisplay()
    {
        // 防止重复触发结算面板。
        // 防止终点重复触发导致结算面板重复打开。
        if (m_finishResultsShown || m_finishDisplayQueued)
        {
            return;
        }

        m_finishDisplayQueued = true;

        if (m_finishDisplayCoroutine != null)
        {
            StopCoroutine(m_finishDisplayCoroutine);
        }

        m_finishDisplayCoroutine = StartCoroutine(DisplayWinnerRoutine());
    }

    // DisplayWinnerRoutine：延迟后显示结算结果。
    private IEnumerator DisplayWinnerRoutine()
    {
        // 等待冲线动画后再显示最终结算结果。
        // 等待冲线动画播完后再显示最终名次和冠军信息。
        yield return new WaitForSeconds(2f);
        ShowFinishResults();
        m_finishResultsShown = true;
        m_finishDisplayQueued = false;
        m_finishDisplayCoroutine = null;
    }

    // ShowFinishResults：写入并显示结算结果。
    private void ShowFinishResults()
    {
        // 根据当前局面显示本地玩家名次和最终赢家。
        // 根据当前局面计算本地玩家名次和最终冠军，并把结果写进 UI。
        int localPlayerIndex = GetLocalPlayerIndex();
        int localPlayerRank = GetParticipantRank(localPlayerIndex);
        int winnerIndex = GetWinnerParticipantIndex();

        if (PlayerlFinishPosition != null)
        {
            PlayerlFinishPosition.text = localPlayerRank > 0 ? localPlayerRank.ToString() : "--";
        }

        if (Winner != null)
        {
            Winner.text = winnerIndex >= 0 ? GetParticipantDisplayName(winnerIndex) : "未知";
        }

        // 关闭结算界面并清空文字内容，避免下一局直接显示旧数据。
        if (FinishUI != null)
        {
            FinishUI.SetActive(true);
        }
    }

    // HideFinishUI：隐藏结算界面并清空文本。
    private void HideFinishUI()
    {
        // 关闭结算界面并清空文本内容。
        if (FinishUI != null)
        {
            FinishUI.SetActive(false);
        }

        if (PlayerlFinishPosition != null)
        {
            PlayerlFinishPosition.text = string.Empty;
        }

        if (Winner != null)
        {
            Winner.text = string.Empty;
        }
    }

    // RegisterParticipant：注册相关对象或状态。
    public static void RegisterParticipant(int index, Transform participantTransform)
    {
        // 记录参赛者的 Transform，供排名、结算和 UI 统一读取。
        // 记录参赛者的 Transform，供排名、重生和结算逻辑统一读取。
        if (index < 0 || index >= ParticipantTransforms.Length || participantTransform == null)
        {
            return;
        }

        ParticipantTransforms[index] = participantTransform;
        ParticipantRegisteredThisRace[index] = true;
    }

    // UnregisterParticipant：取消注册相关对象或状态。
    public static void UnregisterParticipant(int index, Transform participantTransform)
    {
        // 参赛者离场时释放对应引用。
        // 参赛者离场时释放对应引用，避免场景结束后还残留旧对象。
        if (index < 0 || index >= ParticipantTransforms.Length || participantTransform == null)
        {
            return;
        }

        if (ParticipantTransforms[index] == participantTransform)
        {
            ParticipantTransforms[index] = null;
        }
    }

    // GetParticipantTransform：获取相关数据或对象。
    public static Transform GetParticipantTransform(int index)
    {
        if (index < 0 || index >= ParticipantTransforms.Length)
        {
            return null;
        }

        return ParticipantTransforms[index];
    }

    // GetParticipantDisplayName：返回适合界面显示的参赛者名称。
    public static string GetParticipantDisplayName(int index)
    {
        // 把内部标签转换成更适合答辩展示的名字。
        // 把内部标签转换成更适合界面展示的中文名称。
        if (index < 0 || index >= ParticipantTags.Length)
        {
            return "未知";
        }

        switch (ParticipantTags[index])
        {
            case "Player1":
                return "玩家1";
            case "Player2":
                return "玩家2";
            case "Player3":
                return "玩家3";
            case "Player4":
                return "玩家4";
            case "AI1":
                return "AI1";
            case "AI2":
                return "AI2";
            case "AI3":
                return "AI3";
            default:
                return ParticipantTags[index];
        }
    }

    // GetParticipantRank：计算当前参赛者的名次。
    public static int GetParticipantRank(int participantIndex)
    {
        // 根据比赛分数计算当前参赛者名次。
        // 通过综合分数计算当前参赛者名次，名次越小代表越靠前。
        if (participantIndex < 0 || participantIndex >= ParticipantTags.Length)
        {
            return 0;
        }

        float participantScore = GetParticipantRaceScore(participantIndex);
        if (float.IsNegativeInfinity(participantScore))
        {
            return 0;
        }

        int rank = 1;
        for (int i = 0; i < ParticipantTags.Length; i++)
        {
            if (i == participantIndex)
            {
                continue;
            }

            float otherScore = GetParticipantRaceScore(i);
            if (!float.IsNegativeInfinity(otherScore) && otherScore > participantScore + 0.01f)
            {
                rank++;
            }
        }

        return rank;
    }

    // GetWinnerParticipantIndex：找出当前比赛的冠军。
    public static int GetWinnerParticipantIndex()
    {
        // 找出当前比赛中的第一名。
        // 遍历所有已注册参赛者，找出总进度最高的人作为冠军。
        int winnerIndex = -1;
        float winnerScore = float.NegativeInfinity;

        for (int i = 0; i < ParticipantTags.Length; i++)
        {
            float score = GetParticipantRaceScore(i);
            if (float.IsNegativeInfinity(score))
            {
                continue;
            }

            if (winnerIndex < 0 || score > winnerScore)
            {
                winnerIndex = i;
                winnerScore = score;
            }
        }

        return winnerIndex;
    }

    // GetLocalPlayerIndex：找到本地玩家对应的槽位。
    private static int GetLocalPlayerIndex()
    {
        // 本地玩家只看前四个参赛槽位。
        // 本地玩家只看前四个参赛槽位，找到第一个已注册的玩家作为 HUD 参考对象。
        for (int i = 0; i < 4 && i < ParticipantRegisteredThisRace.Length; i++)
        {
            if (ParticipantRegisteredThisRace[i])
            {
                return i;
            }
        }

        return 0;
    }

    // GetParticipantRaceScore：计算参赛者的综合进度分数。
    private static float GetParticipantRaceScore(int index)
    {
        // 圈数优先，其次看检查点和赛道进度，确保名次排序稳定。
        // 把圈数、检查点和当前检查点间的推进程度合成一个可排序的总分值。
        if (index < 0 || index >= CurrentCheckpoint.Length)
        {
            return float.NegativeInfinity;
        }

        // 通过当前位置在“当前检查点 -> 下一检查点”线段上的投影，得到更细的排名分数。
        SaveProgress saveProgress = Instance;
        if (saveProgress == null)
        {
            return float.NegativeInfinity;
        }

        if (index >= ParticipantRegisteredThisRace.Length || !ParticipantRegisteredThisRace[index])
        {
            return float.NegativeInfinity;
        }

        int checkpointCount = saveProgress.ProgressPointsItems != null ? saveProgress.ProgressPointsItems.Length : 0;
        int lap = Mathf.Max(0, CurrentLap[index]);
        int checkpoint = Mathf.Max(0, CurrentCheckpoint[index]);

        if (checkpointCount <= 0)
        {
            return lap * 100000f + checkpoint * 100f;
        }

        int normalizedCheckpoint = checkpoint <= 0 ? 0 : ((checkpoint - 1) % checkpointCount) + 1;
        float progressWithinCheckpoint = GetCheckpointProgress(index, normalizedCheckpoint, checkpointCount);
        return lap * 100000f + normalizedCheckpoint * 100f + progressWithinCheckpoint;
    }

    // GetCheckpointProgress：计算检查点之间的细分进度。
    private static float GetCheckpointProgress(int participantIndex, int normalizedCheckpoint, int checkpointCount)
    {
        // 检查点进度越接近终点，分数越高。
        SaveProgress saveProgress = Instance;
        Transform participantTransform = GetParticipantTransform(participantIndex);
        if (saveProgress == null || participantTransform == null || checkpointCount < 2 || normalizedCheckpoint <= 0)
        {
            return 0f;
        }

        ProgressPoints currentPoint = saveProgress.GetProgressPoint(normalizedCheckpoint - 1);
        ProgressPoints nextPoint = saveProgress.GetProgressPoint(normalizedCheckpoint % checkpointCount);
        if (currentPoint == null || nextPoint == null)
        {
            return 0f;
        }

        Vector3 segment = nextPoint.transform.position - currentPoint.transform.position;
        segment.y = 0f;
        float segmentLengthSqr = segment.sqrMagnitude;
        if (segmentLengthSqr <= 0.0001f)
        {
            return 0f;
        }

        Vector3 fromCurrent = participantTransform.position - currentPoint.transform.position;
        fromCurrent.y = 0f;
        float progress = Vector3.Dot(fromCurrent, segment) / segmentLengthSqr;
        return Mathf.Clamp01(progress);
    }

    // GetProgressPoint：获取指定序号的检查点对象。
    private ProgressPoints GetProgressPoint(int index)
    {
        // 从赛道检查点数组里取出对应脚本，供圈数和进度计算使用。
        if (ProgressPointsItems == null || index < 0 || index >= ProgressPointsItems.Length)
        {
            return null;
        }

        GameObject pointObject = ProgressPointsItems[index];
        if (pointObject == null)
        {
            return null;
        }

        return pointObject.GetComponent<ProgressPoints>();
    }
}
