using UnityEngine;

public class MultiplayerJoin : MonoBehaviour
{
    public GameObject BackgroundCamera;
    private void Awake()
    {
        if (SaveScript.MultiPlayerAmount == 3)
        {
            BackgroundCamera.SetActive(true);
        }
    }
}
