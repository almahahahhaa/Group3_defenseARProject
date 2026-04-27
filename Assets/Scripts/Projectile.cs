using UnityEngine;

public class Projectile : MonoBehaviour
{
    [Header("Settings")]
    public float speed = 1.5f;
    public float hitRadius = 0.05f;
    public float lifetime = 5f;

    private Transform target;

    public void SetTarget(Transform t)
    {
        target = t;
    }

    void Update()
    {
        // Target was destroyed by something else — clean up
        if (target == null)
        {
            Destroy(gameObject);
            return;
        }

        // Move toward target
        Vector3 dir = (target.position - transform.position).normalized;
        transform.position += dir * speed * Time.deltaTime;

        // Optionally face direction of travel
        if (dir != Vector3.zero)
            transform.rotation = Quaternion.LookRotation(dir);

        // Hit check
        if (Vector3.Distance(transform.position, target.position) <= hitRadius)
        {
            HitEnemy();
            return;
        }

        // Expire if it never reaches the target
        lifetime -= Time.deltaTime;
        if (lifetime <= 0f)
            Destroy(gameObject);
    }

    void HitEnemy()
    {
        if (EnemySpawner.Instance != null)
            EnemySpawner.Instance.OnEnemyTapped();

        GameEvents.EnemyDestroyed();
        Destroy(target.gameObject);
        Destroy(gameObject);
    }
}
