using UnityEngine;
using UnityEngine.InputSystem;

public class OilSlickTapHandler : MonoBehaviour
{
    const float TapRadius = 0.12f;

    private Camera arCamera;
    private EnemyMoveToTarget mover;

    void Start()
    {
        arCamera = Camera.main;
        mover = GetComponent<EnemyMoveToTarget>();
    }

    void Update()
    {
        var ts = Touchscreen.current;
        if (ts != null && ts.primaryTouch.phase.ReadValue() == UnityEngine.InputSystem.TouchPhase.Began)
            TryHit(ts.primaryTouch.position.ReadValue());

        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            TryHit(Mouse.current.position.ReadValue());
    }

    void TryHit(Vector2 screenPos)
    {
        if (EnemyTapDestroyer.LastTapFrame == Time.frameCount) return;
        if (AttackTowerPowerup.Instance != null && AttackTowerPowerup.Instance.IsPlacingTower) return;
        if (arCamera == null) { arCamera = Camera.main; if (arCamera == null) return; }

        Ray ray = arCamera.ScreenPointToRay(screenPos);
        float dist = Vector3.Cross(ray.direction, transform.position - ray.origin).magnitude;
        if (dist > TapRadius) return;

        EnemyTapDestroyer.LastTapFrame = Time.frameCount;

        SpawnMinis();
    }

    // Called by XRObjectTapDestroyer so the XR path also splits correctly
    public void TriggerSplit() => SpawnMinis();

    void SpawnMinis()
    {
        var spawner = EnemySpawner.Instance;

        // Register the 2 incoming minis BEFORE OnEnemyTapped so CheckWaveCompleted
        // sees the updated spawnedThisWave count and doesn't end the wave early.
        if (spawner != null && spawner.miniOilSlickPrefab != null)
            spawner.RegisterExtraEnemies(2);

        if (spawner != null)
            spawner.OnEnemyTapped();

        GameEvents.EnemyDestroyed();

        if (spawner != null && spawner.miniOilSlickPrefab != null)
        {
            for (int i = 0; i < 2; i++)
            {
                float sideAngle = (i == 0 ? 60f : -60f) * Mathf.Deg2Rad;
                Vector3 offset = new Vector3(Mathf.Cos(sideAngle) * 0.07f, 0f, Mathf.Sin(sideAngle) * 0.07f);

                GameObject mini = Instantiate(
                    spawner.miniOilSlickPrefab,
                    transform.position + offset,
                    Quaternion.identity,
                    spawner.transform
                );

                var miniMover = mini.GetComponent<EnemyMoveToTarget>();
                if (miniMover != null && mover != null)
                {
                    miniMover.moveSpeed = mover.moveSpeed * 1.1f;
                    if (mover.Target != null)
                        miniMover.AssignTarget(mover.Target);
                }
            }
        }

        Destroy(gameObject);
    }
}
