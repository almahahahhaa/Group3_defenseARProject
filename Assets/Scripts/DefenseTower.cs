using UnityEngine;

public class DefenseTower : MonoBehaviour
{
    [Header("Combat")]
    public float attackRange = 0.5f;
    public float fireRate = 1.0f;
    public GameObject projectilePrefab;
    public Transform firePoint;

    [Header("Visuals")]
    public Transform turretPivot;       // assign TowerTurret child; rotates to face enemy
    public float turretRotateSpeed = 180f;

    private float fireCooldown = 0f;
    private Transform currentTarget;

    void Update()
    {
        fireCooldown -= Time.deltaTime;

        currentTarget = FindClosestEnemy();

        if (currentTarget != null)
            AimTurret(currentTarget);

        if (fireCooldown > 0f || currentTarget == null) return;

        Fire(currentTarget);
        fireCooldown = 1f / fireRate;
    }

    Transform FindClosestEnemy()
    {
        EnemyMoveToTarget[] enemies = FindObjectsByType<EnemyMoveToTarget>(FindObjectsSortMode.None);
        Transform closest = null;
        float minDist = attackRange;

        foreach (var e in enemies)
        {
            float dist = Vector3.Distance(transform.position, e.transform.position);
            if (dist < minDist)
            {
                minDist = dist;
                closest = e.transform;
            }
        }
        return closest;
    }

    void AimTurret(Transform target)
    {
        if (turretPivot == null) return;
        Vector3 dir = target.position - turretPivot.position;
        dir.y = 0f;
        if (dir == Vector3.zero) return;
        Quaternion desired = Quaternion.LookRotation(dir);
        turretPivot.rotation = Quaternion.RotateTowards(
            turretPivot.rotation, desired, turretRotateSpeed * Time.deltaTime);
    }

    void Fire(Transform target)
    {
        if (projectilePrefab == null) return;

        Vector3 origin = firePoint != null
            ? firePoint.position
            : transform.position + Vector3.up * 0.05f;

        GameObject proj = Instantiate(projectilePrefab, origin, Quaternion.identity);
        Projectile p = proj.GetComponent<Projectile>();
        if (p != null) p.SetTarget(target);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}
