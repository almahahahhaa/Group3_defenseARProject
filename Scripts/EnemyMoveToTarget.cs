using UnityEngine;

public class EnemyMoveToTarget : MonoBehaviour
{
    public string targetTag = "Burj_Khalifa";
    public float moveSpeed = 0.05f;
    public float damageDistance = 0.15f;
    public int damageOnHit = 2;

    private Transform target;
    private bool hasDealtDamage = false;

    void Start()
    {
        TryFindTarget();
    }

    void Update()
    {
        if (hasDealtDamage) return;

        // Keep trying until target exists
        if (target == null)
        {
            TryFindTarget();
            return;
        }

        float distance = Vector3.Distance(transform.position, target.position);

        if (distance > damageDistance)
        {
            Vector3 dir = (target.position - transform.position).normalized;
            transform.position += dir * moveSpeed * Time.deltaTime;
        }
        else
        {
            hasDealtDamage = true;

            BurjKhalifaHealth health = target.GetComponent<BurjKhalifaHealth>();
            if (health != null)
                health.TakeDamage(damageOnHit);

            if (EnemySpawner.Instance != null)
                EnemySpawner.Instance.OnEnemyReachedTower();   // or your current tower-hit method

            Destroy(gameObject);
        }
    }

    void TryFindTarget()
    {
        GameObject obj = GameObject.FindGameObjectWithTag(targetTag);
        if (obj != null)
        {
            target = obj.transform;
        }
    }
}