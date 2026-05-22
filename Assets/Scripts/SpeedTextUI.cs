using UnityEngine;
using UnityEngine.UI;

// 车速文本显示脚本，实时读取玩家车辆速度并刷新 HUD。
public class SpeedTextUI : MonoBehaviour
{
    // 目标赛车和文本组件都在运行时自动查找，减少手工拖拽出错的概率。
    private PlayerCartControl m_Target;
    private Text m_Text;
    private float m_NextSearchTime;

    // Awake：初始化组件和运行时状态。
    private void Awake()
    {
        // 先缓存自己的 Text 组件，后面每帧只更新文本内容即可。
        m_Text = GetComponent<Text>();
    }

    // Update：每帧更新主逻辑。
    private void Update()
    {
        // 如果文本不存在，直接退出，避免空引用报错。
        if (m_Text == null)
        {
            return;
        }

        // 目标赛车有可能稍晚生成，所以这里用低频重试的方式去找。
        if (m_Target == null && Time.unscaledTime >= m_NextSearchTime)
        {
            m_Target = GetComponentInParent<PlayerCartControl>();
            if (m_Target == null)
            {
                m_Target = FindFirstObjectByType<PlayerCartControl>();
            }
            m_NextSearchTime = Time.unscaledTime + 0.5f;
        }

        if (m_Target == null)
        {
            m_Text.text = "SPEED\n--- km/h";
            return;
        }

        // 把速度转换成整数后显示，格式固定成三位数，界面更整齐。
        int displaySpeed = Mathf.RoundToInt(Mathf.Max(0f, m_Target.currentSpeed));
        m_Text.text = $"SPEED\n{displaySpeed:000} km/h";
    }
}
