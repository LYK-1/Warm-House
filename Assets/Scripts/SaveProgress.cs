using System;
using System.Collections;
using UnityEngine;
using TMPro;

public class SaveProgress : MonoBehaviour
{
    public static SaveProgress Instance { get; private set; }
    public static readonly string[] ParticipantTags = new string[7] { "Player1", "Player2", "Player3", "Player4", "AI1", "AI2", "AI3" };
    public GameObject[] ProgressPointsItems;
    public static int[] CurrentLap = new int[7];
    public static int[] CurrentCheckpoint = new int[7];
    public static Transform[] ParticipantTransforms = new Transform[ParticipantTags.Length];

    public static bool Reset;
    private int m_resetAmounts;
    public static int LapNumber;

    public static int[] PlayerlItemsAmounts = new int[4] { 10, 10 ,10, 10 };

    public static bool RaceHasStarted;

    [SerializeField] private float RaceStartCountdown = 3f;
    [SerializeField] private TMP_Text m_startText;

    private void Awake()
    {
        Instance = this;
    }

    private void OnEnable()
    {
        Array.Clear(CurrentLap, 0, CurrentLap.Length);
        Array.Clear(CurrentCheckpoint, 0, CurrentCheckpoint.Length);
        Array.Clear(ParticipantTransforms, 0, ParticipantTransforms.Length);
        Reset = true;
        LapNumber = 0;
        m_resetAmounts = 0;
        RaceHasStarted = false;
    }

    private void OnDisable()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    public static void RegisterParticipant(int index, Transform participantTransform)
    {
        if (index < 0 || index >= ParticipantTransforms.Length || participantTransform == null)
        {
            return;
        }

        ParticipantTransforms[index] = participantTransform;
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

    void Start()
    {
        CacheStartText();
        SetStartText(string.Empty);
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
        StartCoroutine(BeginRaceAfterCountdown());
    }

    private void CacheStartText()
    {
        if (m_startText != null)
        {
            return;
        }

        GameObject startTextObject = GameObject.Find("Start Text");
        if (startTextObject != null)
        {
            startTextObject.TryGetComponent(out m_startText);
        }
    }

    private void SetStartText(string value)
    {
        CacheStartText();
        if (m_startText != null)
        {
            m_startText.text = value;
        }
    }

    private IEnumerator BeginRaceAfterCountdown()
    {
        float countdown = Mathf.Max(0f, RaceStartCountdown);
        if (countdown <= 0f)
        {
            SetStartText(string.Empty);
            RaceHasStarted = true;
            yield break;
        }

        int displayCount = Mathf.CeilToInt(countdown);
        for (int remaining = displayCount; remaining > 0; remaining--)
        {
            SetStartText(remaining.ToString());
            yield return new WaitForSeconds(1f);
        }

        SetStartText("GO!");
        yield return new WaitForSeconds(1f);
        SetStartText(string.Empty);
        RaceHasStarted = true;
    }
}
