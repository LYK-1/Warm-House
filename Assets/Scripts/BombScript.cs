using UnityEngine;

public class BombScript : MonoBehaviour
{
    public GameObject Explosion;
    public GameObject ExplodeSound;
    public float BurnRadius = 5f;
    private BoxCollider m_collider;
    private bool m_exploded;

    void Start()
    {
        Invoke("Explode", 3);
        Invoke("DestroyBomb", 3.5f);
        m_collider = GetComponent<BoxCollider>();
    }

    private void TriggerBurnOnCar(GameObject target)
    {
        ObstacleSound obstacleSound = target.GetComponentInParent<ObstacleSound>();
        if (obstacleSound != null)
        {
            obstacleSound.TriggerBurn();
        }
    }

    private void TriggerBurnInRadius()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, BurnRadius);
        foreach (Collider hit in hits)
        {
            TriggerBurnOnCar(hit.gameObject);
        }
    }

    private void Explode()
    {
        if (m_exploded)
        {
            return;
        }

        m_exploded = true;
        Instantiate(Explosion, transform.position, Quaternion.identity);
        Instantiate(ExplodeSound, transform.position, Quaternion.identity);
        TriggerBurnInRadius();
    }

    private void DestroyBomb()
    {
        m_collider.size = new Vector3(0.24f, 0.24f, 0.24f);
        m_exploded = true;
        Destroy(gameObject, 0.5f);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (m_exploded)
        {
            return;
        }

        ObstacleSound obstacleSound = other.GetComponentInParent<ObstacleSound>();
        if (obstacleSound != null)
        {
            m_exploded = true;
            Instantiate(Explosion, transform.position, Quaternion.identity);
            Instantiate(ExplodeSound, transform.position, Quaternion.identity);
            obstacleSound.TriggerBurn();
            Destroy(gameObject);
        }
    }
}
