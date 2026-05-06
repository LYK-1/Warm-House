using UnityEngine;

public class MissileScript : MonoBehaviour
{
    public GameObject Explosion;
    private Collider m_collider;

    void Start()
    {
        Invoke("WaitToDestroy", 8);
        m_collider = GetComponent<Collider>();
        m_collider.enabled = false;
        Invoke("CollideOn", 0.15f);
    }

    void Update()
    {
        transform.Translate(-80 * Time.deltaTime,0, 0);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Obstacle") || other.CompareTag("AI1") || other.CompareTag("AI2") || other.CompareTag("AI3") || other.CompareTag("Player1") || other.CompareTag("Player2") || other.CompareTag("Player3") || other.CompareTag("Player4"))
        {
            Instantiate(Explosion, transform.position, transform.rotation);
            Destroy(gameObject);
        }
    }

    private void WaitToDestroy()
    {
        Destroy(gameObject);
    }

    private void CollideOn()
    {
        m_collider.enabled = true;
    }
}
