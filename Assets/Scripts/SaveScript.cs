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
    void Start()
    {
        DontDestroyOnLoad(gameObject);
    }

}
