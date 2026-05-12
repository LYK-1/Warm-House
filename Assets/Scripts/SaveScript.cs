using UnityEngine;

public class SaveScript : MonoBehaviour
{
    public static bool SinglePlayerMode;
    public static bool MultiPlayerMode;
    public static int PlayerlKartSelected;
    public static string PlayerlName;
    public static int TrackToPlay;

    void Start()
    {
        DontDestroyOnLoad(gameObject);
    }

}
