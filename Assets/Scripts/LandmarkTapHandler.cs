using UnityEngine;
using UnityEngine.InputSystem;

// Attach to the same GameObject as the landmark's collider.
// Detects taps via Physics.Raycast and activates the Green Shield.
[DefaultExecutionOrder(-30)]
public class LandmarkTapHandler : MonoBehaviour
{
    // Set by LandmarkManager to the spawned landmark root (for dome positioning).
    public Transform landmarkRoot;

    private GreenShield _activeShield;
    private Camera _cam;

    const float ShieldDuration = 12f;

    void Start()
    {
        _cam = Camera.main;
        if (landmarkRoot == null) landmarkRoot = transform;
        EnsurePowerupManager();
    }

    void Update()
    {
        // Only process taps when the player has pressed the shield button
        PowerupManager pm = PowerupManager.Instance;
        if (pm == null || !pm.ShieldPlacementMode) return;

        Vector2 screenPos = default;
        bool hasTap = false;

        var ts = Touchscreen.current;
        if (ts != null && ts.primaryTouch.press.wasPressedThisFrame)
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

        // Claim this tap so enemies (order 0) don't also process it
        EnemyTapDestroyer.LastTapFrame = Time.frameCount;

        if (_cam == null) { _cam = Camera.main; if (_cam == null) return; }

        Ray ray = _cam.ScreenPointToRay(screenPos);
        if (!Physics.Raycast(ray, out RaycastHit hit, 100f)) return;

        // Only respond if the raycast hit this landmark's collider
        if (hit.collider == null || hit.collider.gameObject != gameObject) return;

        HandleLandmarkTap(pm);
    }

    void HandleLandmarkTap(PowerupManager pm)
    {
        if (_activeShield != null)
        {
            // Already shielded — cancel placement mode so player can pick another
            pm.ShieldPlacementMode = false;
            return;
        }

        if (!pm.ConsumeCharge())
        {
            pm.ShieldPlacementMode = false;
            return;
        }

        pm.ShieldPlacementMode = false;
        Time.timeScale = 1f;
        ActivateShield();
    }

    void ActivateShield()
    {
        Transform root = landmarkRoot != null ? landmarkRoot : transform;

        GameObject shieldGO = new GameObject("GreenShield");
        shieldGO.transform.SetParent(root, false);
        shieldGO.transform.localPosition = Vector3.zero;

        _activeShield = shieldGO.AddComponent<GreenShield>();
        _activeShield.Init(root, ShieldDuration);
        PowerupAudioManager.Instance?.PlaySpawn(root.position);

        StartCoroutine(WaitForShieldExpiry());
    }

    System.Collections.IEnumerator WaitForShieldExpiry()
    {
        while (_activeShield != null)
            yield return null;
        _activeShield = null;
    }

    void ShowNoShieldToast()
    {
        // Brief "No shields" log — UI toast can be added here if desired
        Debug.Log("[GreenShield] No shield charges available.");
    }

    public bool HasActiveShield => _activeShield != null;

    void EnsurePowerupManager()
    {
        if (PowerupManager.Instance == null)
        {
            var go = new GameObject("PowerupManager");
            go.AddComponent<PowerupManager>();
        }
    }
}
