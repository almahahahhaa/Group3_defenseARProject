using UnityEngine;

public class EnemyMoveToTarget : MonoBehaviour
{
    public float moveSpeed = 0.05f;
    public float damageDistance = 0.15f;
    public int damageOnHit = 2;

    private Transform target;
    private bool hasDealtDamage = false;

    public Transform Target => target;

    void Start()
    {
        if (target == null)
            AssignRandomTarget();
    }

    // Called by EnemySpawner immediately after instantiation
    public void AssignTarget(Transform landmarkTransform)
    {
        target = landmarkTransform;
    }

    void AssignRandomTarget()
    {
        if (LandmarkManager.Instance == null) return;
        GameObject landmark = LandmarkManager.Instance.GetRandomLandmark();
        if (landmark != null)
            target = landmark.transform;
    }

    void Update()
    {
        if (hasDealtDamage) return;

        if (target == null)
        {
            AssignRandomTarget();
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

            if (!IsTargetShielded())
                DealDamage();
            // Shielded: no damage dealt; enemy is repelled/destroyed by the shield

            if (EnemySpawner.Instance != null)
                EnemySpawner.Instance.OnEnemyReachedTower();

            Destroy(gameObject);
        }
    }

    bool IsTargetShielded()
    {
        if (target == null) return false;
        var lh = target.GetComponentInChildren<LandmarkHealth>();
        return lh != null && lh.isShielded;
    }

    void DealDamage()
    {
        var lh = target.GetComponentInChildren<LandmarkHealth>();
        if (lh != null) lh.TakeDamage(damageOnHit);
    }
}
