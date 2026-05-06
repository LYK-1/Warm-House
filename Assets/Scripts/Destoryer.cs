using UnityEngine;

public class Destoryer : MonoBehaviour
{
    public float TimeToDestroy = 3;

    void Start()
    {
      Destroy(gameObject, TimeToDestroy);
    }
}
