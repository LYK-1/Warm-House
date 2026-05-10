using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

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

    public static int[] PlayerlItemsAmounts = new int[4] { 10, 10 ,10, 10 };

    public static bool RaceHasStarted;
    public static int MaxLaps = 1;
    public static bool RaCeHasFiniShed;

    public GameObject FinishUI;
    public Text PlayerlFinishPosition;
    public Text Winner;
    private Coroutine m_finishDisplayCoroutine;
    private bool m_finishDisplayQueued;
    private bool m_finishResultsShown;

    private void Awake()
    {
        Instance = this;
    }

    private void OnEnable()
    {
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

    void Start()
    {
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

    private void OnDisable()
    {
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

    public void QueueFinishDisplay()
    {
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

    private IEnumerator DisplayWinnerRoutine()
    {
        yield return new WaitForSeconds(2f);
        ShowFinishResults();
        m_finishResultsShown = true;
        m_finishDisplayQueued = false;
        m_finishDisplayCoroutine = null;
    }

    private void ShowFinishResults()
    {
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

        if (FinishUI != null)
        {
            FinishUI.SetActive(true);
        }
    }

    private void HideFinishUI()
    {
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

    public static void RegisterParticipant(int index, Transform participantTransform)
    {
        if (index < 0 || index >= ParticipantTransforms.Length || participantTransform == null)
        {
            return;
        }

        ParticipantTransforms[index] = participantTransform;
        ParticipantRegisteredThisRace[index] = true;
    }

    public static void UnregisterParticipant(int index, Transform participantTransform)
    {
        if (index < 0 || index >= ParticipantTransforms.Length || participantTransform == null)
        {
            return;
        }

        if (ParticipantTransforms[index] == participantTransform)
        {
            ParticipantTransforms[index] = null;
        }
    }

    public static Transform GetParticipantTransform(int index)
    {
        if (index < 0 || index >= ParticipantTransforms.Length)
        {
            return null;
        }

        return ParticipantTransforms[index];
    }

    public static string GetParticipantDisplayName(int index)
    {
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

    public static int GetParticipantRank(int participantIndex)
    {
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

    public static int GetWinnerParticipantIndex()
    {
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

    private static int GetLocalPlayerIndex()
    {
        for (int i = 0; i < 4 && i < ParticipantRegisteredThisRace.Length; i++)
        {
            if (ParticipantRegisteredThisRace[i])
            {
                return i;
            }
        }

        return 0;
    }

    private static float GetParticipantRaceScore(int index)
    {
        if (index < 0 || index >= CurrentCheckpoint.Length)
        {
            return float.NegativeInfinity;
        }

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

    private static float GetCheckpointProgress(int participantIndex, int normalizedCheckpoint, int checkpointCount)
    {
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

    private ProgressPoints GetProgressPoint(int index)
    {
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
