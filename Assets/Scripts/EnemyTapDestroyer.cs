using UnityEngine;
using UnityEngine.InputSystem;

public class EnemyTapDestroyer : MonoBehaviour
{
    // Shared across all instances so only one enemy claims each tap frame.
    public static int LastTapFrame = -1;

    const float TapRadius = 0.12f; // world-space perpendicular distance threshold

    private Camera arCamera;

    void Start()
    {
        arCamera = Camera.main;
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
        if (LastTapFrame == Time.frameCount) return; // another enemy already claimed this tap
        if (AttackTowerPowerup.Instance != null && AttackTowerPowerup.Instance.IsPlacingTower) return;
        if (arCamera == null) { arCamera = Camera.main; if (arCamera == null) return; }

        Ray ray = arCamera.ScreenPointToRay(screenPos);

        // Perpendicular distance from ray to this enemy's world-space center.
        // This bypasses physics/collider issues entirely (same pattern as DefenseTower).
        float dist = Vector3.Cross(ray.direction, transform.position - ray.origin).magnitude;
        if (dist > TapRadius) return;

        LastTapFrame = Time.frameCount;

        if (EnemySpawner.Instance != null)
            EnemySpawner.Instance.OnEnemyTapped();

        GameEvents.EnemyDestroyed();
        Destroy(gameObject);
    }
}
