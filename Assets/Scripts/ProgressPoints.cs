using System.Collections;
using UnityEngine;
using static UnityEngine.UI.GridLayoutGroup;

public class ProgressPoints : MonoBehaviour
{
    public bool AISpeedControl = false;
    public float AISetSpeed;
    public float AIMaxSpeed = 45;

    public bool SpawnBomb;
    public bool SpawnOil;

    public int ProgressNumber;
    public bool StartLine;

    private void OnTriggerEnter(Collider other)
    {
        PlayerCartContaol player = other.GetComponentInParent<PlayerCartContaol>();
        if (player != null)
        {
            HandleProgressPoint(PlayerCartContaol.ResolveParticipantIndex(player.transform), player.transform, player);
            return;
        }

        AIKartControl aiKart = other.GetComponentInParent<AIKartControl>();
        if (aiKart != null)
        {
            if (AISpeedControl)
            {
                aiKart.MaxSpeed = Random.Range(AISetSpeed, AIMaxSpeed);
            }

            if (SpawnBomb)
            {
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
                SpecialItemsAI specialItems = other.GetComponentInParent<SpecialItemsAI>();
                if (specialItems != null)
                {
                    specialItems.Spawnoil();
                }

                SpawnOil = false;
                StartCoroutine(ResetSpawnoil());
            }

            HandleProgressPoint(ResolveKartIndex(aiKart.transform), aiKart.transform, null);
            return;
        }

        int kartIndex = ResolveKartIndex(other.transform);
        HandleProgressPoint(kartIndex, other.transform, null);
    }

    private void HandleProgressPoint(int kartIndex, Transform current, PlayerCartContaol player)
    {
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

        if (currentCheckpoint == expectedNextCheckpoint)
        {
            // Correct-direction reverse at a checkpoint: stop and switch back to forward without flipping the car.
            if (player != null && player.m_reverse)
            {
                player.StopReverseAtProgressPoint();
                return;
            }

            if (player != null)
            {
                player.RegisterProgressRespawnPoint(transform);
            }

            if (StartLine && previousCheckpoint == checkpointCount)
            {
                SaveProgress.CurrentLap[kartIndex]++;
            }

            SaveProgress.CurrentCheckpoint[kartIndex] = currentCheckpoint;
            return;
        }

        int expectedPreviousCheckpoint = previousCheckpoint - 1;
        if (expectedPreviousCheckpoint <= 0)
        {
            expectedPreviousCheckpoint = checkpointCount;
        }

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

    private int GetCheckpointCount(SaveProgress saveProgress)
    {
        if (saveProgress == null || saveProgress.ProgressPointsItems == null)
        {
            return 0;
        }

        return saveProgress.ProgressPointsItems.Length;
    }

    private int ResolveKartIndex(Transform current)
    {
        return PlayerCartContaol.ResolveParticipantIndex(current);
    }

    IEnumerator ResetSpawnBomb()
    {
        yield return new WaitForSeconds(4);
        SpawnBomb = true;
    }

    IEnumerator ResetSpawnoil()
    {
        yield return new WaitForSeconds(4);
        SpawnOil = true;
    }
}
