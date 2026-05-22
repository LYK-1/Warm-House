using System.Collections;
using UnityEngine;
using static UnityEngine.UI.GridLayoutGroup;

// 赛道进度点脚本，负责检查点、圈数和终点判定。
public class ProgressPoints : MonoBehaviour
{
    // 赛道进度点脚本：负责检查点、圈数、终点判定，以及 AI 道具和速度控制。
    private const int FinishCheckpointTolerance = 1;

    public bool AISpeedControl = false;
    public float AISetSpeed;
    public float AIMaxSpeed = 45;

    public bool SpawnBomb;
    public bool SpawnOil;

    public int ProgressNumber;
    public bool StartLine;

    // OnTriggerEnter：处理触发器进入事件。
    private void OnTriggerEnter(Collider other)
    {
        // 先判断碰到的是玩家还是 AI，再按对应逻辑处理检查点和道具。
        PlayerCartControl player = other.GetComponentInParent<PlayerCartControl>();
        if (player != null)
        {
            HandleProgressPoint(PlayerCartControl.ResolveParticipantIndex(player.transform), player.transform, player, null);
            return;
        }

        AIKartControl aiKart = other.GetComponentInParent<AIKartControl>();
        if (aiKart != null)
        {
            // AI 经过检查点时可以顺带调整速度，制造更真实的赛道竞争感。
            if (AISpeedControl)
            {
                aiKart.MaxSpeed = Random.Range(AISetSpeed, AIMaxSpeed);
            }

            if (SpawnBomb)
            {
                // 到达这个点时，触发 AI 炸弹生成，然后短暂锁定，避免连续刷道具。
                SpecialItemsAI specialItems = other.GetComponentInParent<SpecialItemsAI>();
                if (specialItems != null)
                {
                    specialItems.SpawnBomb();
                }

                SpawnBomb = false;
                StartCoroutine(ResetSpawnBomb());
            }

            if (SpawnOil)
            {
                // 同理，油污道具也通过检查点来控制刷新频率。
                SpecialItemsAI specialItems = other.GetComponentInParent<SpecialItemsAI>();
                if (specialItems != null)
                {
                    specialItems.Spawnoil();
                }

                SpawnOil = false;
                StartCoroutine(ResetSpawnoil());
            }

            HandleProgressPoint(ResolveKartIndex(aiKart.transform), aiKart.transform, null, aiKart);
            return;
        }

        int kartIndex = ResolveKartIndex(other.transform);
        HandleProgressPoint(kartIndex, other.transform, null, null);
    }

    // HandleProgressPoint：处理对应业务逻辑。
    private void HandleProgressPoint(int kartIndex, Transform current, PlayerCartControl player, AIKartControl aiKart)
    {
        // 把当前经过的点和上一检查点进行比较，决定是正常前进、逆行还是越线。
        if (kartIndex < 0 || kartIndex >= SaveProgress.CurrentCheckpoint.Length || current == null)
        {
            return;
        }

        SaveProgress saveProgress = SaveProgress.Instance;
        if (saveProgress == null)
        {
            return;
        }

        int checkpointCount = GetCheckpointCount(saveProgress);
        if (checkpointCount <= 0)
        {
            return;
        }

        int currentCheckpoint = ProgressNumber;
        int previousCheckpoint = SaveProgress.CurrentCheckpoint[kartIndex];

        int expectedNextCheckpoint = previousCheckpoint + 1;
        if (previousCheckpoint >= checkpointCount || previousCheckpoint <= 0)
        {
            expectedNextCheckpoint = 1;
        }

        // 起点线只在满足“已经跑完一圈”的前提下才计入圈数。
        if (StartLine && CanFinishRace(previousCheckpoint, checkpointCount))
        {
            if (player != null && player.m_reverse)
            {
                player.StopReverseAtProgressPoint();
                return;
            }

            if (player != null)
            {
                player.RegisterProgressRespawnPoint(transform);
            }

            SaveProgress.CurrentLap[kartIndex] = Mathf.Min(SaveProgress.CurrentLap[kartIndex] + 1, SaveProgress.MaxLaps);

            if (SaveProgress.CurrentLap[kartIndex] >= SaveProgress.MaxLaps)
            {
                TriggerRaceFinish();
                SaveProgress.CurrentCheckpoint[kartIndex] = currentCheckpoint;
                return;
            }

            SaveProgress.CurrentCheckpoint[kartIndex] = currentCheckpoint;
            return;
        }

        // 正常方向：经过下一个合法检查点时更新当前位置。
        if (currentCheckpoint == expectedNextCheckpoint)
        {
            if (player != null && player.m_reverse)
            {
                player.StopReverseAtProgressPoint();
                return;
            }

            if (player != null)
            {
                player.RegisterProgressRespawnPoint(transform);
            }

            SaveProgress.CurrentCheckpoint[kartIndex] = currentCheckpoint;
            return;
        }

        int expectedPreviousCheckpoint = previousCheckpoint - 1;
        if (expectedPreviousCheckpoint <= 0)
        {
            expectedPreviousCheckpoint = checkpointCount;
        }

        // 如果玩家走回头路，直接把车头掰回正确方向，避免倒着刷检查点。
        if (previousCheckpoint > 0 && currentCheckpoint == expectedPreviousCheckpoint && player != null)
        {
            // Reverse gear should still stop without flipping.
            if (player.m_reverse)
            {
                player.StopReverseAtProgressPoint();
                return;
            }

            // Wrong-way route: flip the car back to the forward direction.
            player.FaceForward();
        }
    }

    // CanFinishRace：判断条件是否成立。
    private bool CanFinishRace(int previousCheckpoint, int checkpointCount)
    {
        // 终点判定需要结合检查点数量，避免未完成赛道就提前算圈。
        if (checkpointCount <= 0)
        {
            return false;
        }

        int finishThreshold = checkpointCount;
        if (checkpointCount > 10)
        {
            finishThreshold = checkpointCount - FinishCheckpointTolerance;
        }

        return previousCheckpoint >= finishThreshold;
    }

    // GetCheckpointCount：获取相关数据或对象。
    private int GetCheckpointCount(SaveProgress saveProgress)
    {
        // 统计有效检查点数量，空对象不计入。
        if (saveProgress == null || saveProgress.ProgressPointsItems == null)
        {
            return 0;
        }

        int checkpointCount = 0;
        foreach (GameObject pointObject in saveProgress.ProgressPointsItems)
        {
            if (pointObject != null)
            {
                checkpointCount++;
            }
        }

        return checkpointCount;
    }

    // ResolveKartIndex：解析并缓存相关引用。
    private int ResolveKartIndex(Transform current)
    {
        return PlayerCartControl.ResolveParticipantIndex(current);
    }

    // TriggerRaceFinish：触发比赛结束并停止所有参赛者。
    private void TriggerRaceFinish()
    {
        if (SaveProgress.RaCeHasFiniShed)
        {
            return;
        }

        SaveProgress.RaCeHasFiniShed = true;
        SaveProgress.RaceHasStarted = false;

        for (int i = 0; i < SaveProgress.ParticipantTransforms.Length; i++)
        {
            Transform participantTransform = SaveProgress.GetParticipantTransform(i);
            if (participantTransform == null)
            {
                continue;
            }

            PlayerCartControl player = participantTransform.GetComponentInParent<PlayerCartControl>();
            if (player != null)
            {
                player.BeginFinishSequence();
                continue;
            }

            AIKartControl aiKart = participantTransform.GetComponentInParent<AIKartControl>();
            if (aiKart != null)
            {
                aiKart.BeginFinishSequence();
            }
        }

        if (SaveProgress.Instance != null)
        {
            SaveProgress.Instance.QueueFinishDisplay();
        }
    }

    // ResetSpawnBomb：延迟恢复炸弹刷新开关。
    IEnumerator ResetSpawnBomb()
    {
        yield return new WaitForSeconds(4);
        SpawnBomb = true;
    }

    // ResetSpawnoil：延迟恢复油污刷新开关。
    IEnumerator ResetSpawnoil()
    {
        yield return new WaitForSeconds(4);
        SpawnOil = true;
    }
}
