using UnityEngine;

// 菜单阶段的全局配置存储，保存单人/多人、车辆和赛道等选择结果。
public class SaveScript : MonoBehaviour
{
    // 菜单阶段的全局选择结果，后续由 RaceStart 和赛道选择界面读取。
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

    // Awake：初始化组件和运行时状态。
    private void Awake()
    {
        // 保证场景中只保留一个全局配置实例，避免跨场景重复创建。
        if (Instance)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }
    // Start：完成启动初始化。
    void Start()
    {
        // 配置对象需要跨场景存在，所以在启动后直接标记为 DontDestroyOnLoad。
        DontDestroyOnLoad(gameObject);
    }

}
