using UnityEngine;

public class SmoothChildCamera : MonoBehaviour
{
    [Header("平滑设置")]
    public float positionSmoothTime = 0.05f;   // 位置平滑时间（越小响应越快，但可能残留抖动）
    public float rotationSmoothTime = 0.05f;   // 旋转平滑时间

    [Header("目标偏移（赛车局部坐标系下的期望位置）")]
    public Vector3 targetLocalPosition = new Vector3(0f, 1f, -3f);
    public Vector3 targetLocalEulerAngles = Vector3.zero;

    // 内部平滑用的速度向量（由SmoothDamp自动维护）
    private Vector3 currentVelocityPos;
    private Vector3 currentVelocityRot;

    void Start()
    {
        // 初始化：直接设置到期望偏移，防止开始时跳变
        transform.localPosition = targetLocalPosition;
        transform.localEulerAngles = targetLocalEulerAngles;
    }

    void LateUpdate()
    {
        // 平滑移动局部位置
        Vector3 newLocalPos = Vector3.SmoothDamp(
            transform.localPosition,
            targetLocalPosition,
            ref currentVelocityPos,
            positionSmoothTime
        );
        transform.localPosition = newLocalPos;

        // 平滑旋转（注意欧拉角插值可能会有万向锁风险，但对普通赛车视角足够）
        Vector3 newLocalEuler = Vector3.SmoothDamp(
            transform.localEulerAngles,
            targetLocalEulerAngles,
            ref currentVelocityRot,
            rotationSmoothTime
        );
        transform.localEulerAngles = newLocalEuler;
    }
}