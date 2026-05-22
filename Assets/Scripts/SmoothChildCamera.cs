using UnityEngine;

// 子相机平滑跟随脚本，用于让挂在父物体下的摄像机以更柔和的方式贴近目标偏移。
public class SmoothChildCamera : MonoBehaviour
{
    [Header("平滑参数")]
    public float positionSmoothTime = 0.05f;   // 位置平滑时间，越小跟随越快，但也更容易抖动。
    public float rotationSmoothTime = 0.05f;   // 旋转平滑时间，越小响应越快。

    [Header("目标偏移（子物体本地坐标）")]
    public Vector3 targetLocalPosition = new Vector3(0f, 1f, -3f);
    public Vector3 targetLocalEulerAngles = Vector3.zero;

    // 平滑过程中使用的速度缓存，SmoothDamp 会在每帧自动更新它。
    private Vector3 currentVelocityPos;
    private Vector3 currentVelocityRot;

    // Start：初始化子相机的本地位置和旋转，避免进入场景时出现跳变。
    void Start()
    {
        // 先直接对齐到目标偏移，确保第一帧就是正确姿态。
        transform.localPosition = targetLocalPosition;
        transform.localEulerAngles = targetLocalEulerAngles;
    }

    // LateUpdate：在所有普通更新之后，对子相机做平滑跟随修正。
    void LateUpdate()
    {
        // 平滑过渡本地位置。
        Vector3 newLocalPos = Vector3.SmoothDamp(
            transform.localPosition,
            targetLocalPosition,
            ref currentVelocityPos,
            positionSmoothTime
        );
        transform.localPosition = newLocalPos;

        // 平滑过渡本地旋转。这里直接用欧拉角做插值，满足固定跟随偏移的需求。
        Vector3 newLocalEuler = Vector3.SmoothDamp(
            transform.localEulerAngles,
            targetLocalEulerAngles,
            ref currentVelocityRot,
            rotationSmoothTime
        );
        transform.localEulerAngles = newLocalEuler;
    }
}
