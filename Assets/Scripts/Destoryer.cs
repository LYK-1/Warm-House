using UnityEngine;

// 通用定时销毁工具，常用于临时特效、道具和一次性对象。
public class Destoryer : MonoBehaviour
{
    // 这个值决定当前对象在场景中还能存活多久。
    public float TimeToDestroy = 3;

    // Start：完成启动初始化。
    void Start()
    {
      // 只做一件事：按设定时间自动销毁当前对象。
      Destroy(gameObject, TimeToDestroy);
    }
}
