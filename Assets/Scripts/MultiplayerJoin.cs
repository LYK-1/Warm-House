using UnityEngine;

// 本地多人模式进入时的附加处理脚本，主要用于控制背景相机显示。
public class MultiplayerJoin : MonoBehaviour
{
    // 当多人数量达到特定配置时，再启用这个背景摄像机对象。
    public GameObject BackgroundCamera;
    // Awake：初始化组件和运行时状态。
    private void Awake()
    {
        // 这里主要用于三人模式，避免背景层和前景 HUD 冲突。
        if (SaveScript.MultiPlayerAmount == 3)
        {
            BackgroundCamera.SetActive(true);
        }
    }
}
