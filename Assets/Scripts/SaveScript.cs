using UnityEngine;

public class SaveScript : MonoBehaviour
{
    public static bool SinglePlayerMode;
    public static bool MultiPlayerMode;
    public static int PlayerlKartSelected;
    public static string PlayerlName;
    public static int TrackToPlay;

    public static int MultiPlayerAmount;
    public static int Player2KartSelected;
    public static int Player3KartSelected;
    public static int Player4KartSelected;

    public static SaveScript Instance { get; private set; }

    private void Awake()
    {
        if (Instance)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }
    void Start()
    {
        DontDestroyOnLoad(gameObject);
    }

}
