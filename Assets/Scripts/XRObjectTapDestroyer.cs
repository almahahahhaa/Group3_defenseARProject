using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

// Central catch-all tap handler.
//
// Per-enemy handlers (EnemyTapDestroyer, OilSlickTapHandler) use the same
// geometric proximity check and set EnemyTapDestroyer.LastTapFrame when they
// claim a tap. This handler only fires when none of them claimed the tap first,
// providing a safety net for any enemy that somehow missed its per-instance handler.
public class XRObjectTapDestroyer : MonoBehaviour
{
    const float TapRadius = 0.12f;

    public XRRayInteractor rayInteractor;

    void Awake()
    {
        if (rayInteractor == null)
            rayInteractor = FindAnyObjectByType<XRRayInteractor>();
    }

    void Update()
    {
        // Per-enemy handlers run first (Unity default execution order).
        // If one already claimed this frame's tap, there is nothing left to do.
        if (EnemyTapDestroyer.LastTapFrame == Time.frameCount) return;
        if (AttackTowerPowerup.Instance != null && AttackTowerPowerup.Instance.IsPlacingTower) return;

        Vector2 screenPos = default;
        bool hasTap = false;

        var ts = Touchscreen.current;
        if (ts != null && ts.primaryTouch.phase.ReadValue() == UnityEngine.InputSystem.TouchPhase.Began)
        {
            screenPos = ts.primaryTouch.position.ReadValue();
            hasTap = true;
        }
        else if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            screenPos = Mouse.current.position.ReadValue();
            hasTap = true;
        }

        if (!hasTap) return;

        TryHitEnemy(screenPos);
    }

    void TryHitEnemy(Vector2 screenPos)
    {
        Camera cam = Camera.main;
        if (cam == null) return;

        Ray ray = cam.ScreenPointToRay(screenPos);

        EnemyMoveToTarget closest = null;
        float closestDist = TapRadius;

        foreach (var enemy in FindObjectsByType<EnemyMoveToTarget>(FindObjectsSortMode.None))
        {
            float d = Vector3.Cross(ray.direction, enemy.transform.position - ray.origin).magnitude;
            if (d < closestDist) { closestDist = d; closest = enemy; }
        }

        if (closest == null) return;

        EnemyTapDestroyer.LastTapFrame = Time.frameCount;

        OilSlickTapHandler oilSlick = closest.GetComponent<OilSlickTapHandler>();
        if (oilSlick != null)
        {
            oilSlick.TriggerSplit();
            return;
        }

        if (EnemySpawner.Instance != null)
            EnemySpawner.Instance.OnEnemyTapped();

        GameEvents.EnemyDestroyed();
        Destroy(closest.gameObject);
    }
}
