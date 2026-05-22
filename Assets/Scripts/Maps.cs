using UnityEngine;
using UnityEngine.UI;

// 比赛地图预览图控制脚本，根据当前选择的赛道切换小地图背景。
public class Maps : MonoBehaviour
{
    // UI 上显示的地图预览图，以及每条赛道对应的 RenderTexture。
    public RawImage MapImage;
    public RenderTexture[] MapTextures;

    // Start：完成启动初始化。
    void Start()
    {
        // 根据菜单阶段保存的赛道编号，切换到对应的小地图预览。
        for (int i = 0; i < MapTextures.Length; i++)
        {
            if (SaveScript.TrackToPlay - 2 == i)
            {
                MapImage.texture = MapTextures[SaveScript.TrackToPlay - 2];
            }
        }
    }
}
