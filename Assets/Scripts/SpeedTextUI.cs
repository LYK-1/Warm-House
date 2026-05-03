using UnityEngine;
using UnityEngine.UI;

public class SpeedTextUI : MonoBehaviour
{
    private PlayerCartContaol m_Target;
    private Text m_Text;
    private float m_NextSearchTime;

    private void Awake()
    {
        m_Text = GetComponent<Text>();
    }

    private void Update()
    {
        if (m_Text == null)
        {
            return;
        }

        if (m_Target == null && Time.unscaledTime >= m_NextSearchTime)
        {
            m_Target = FindFirstObjectByType<PlayerCartContaol>();
            m_NextSearchTime = Time.unscaledTime + 0.5f;
        }

        if (m_Target == null)
        {
            m_Text.text = "SPEED\n--- km/h";
            return;
        }

        int displaySpeed = Mathf.RoundToInt(Mathf.Max(0f, m_Target.currentSpeed));
        m_Text.text = $"SPEED\n{displaySpeed:000} km/h";
    }
}
