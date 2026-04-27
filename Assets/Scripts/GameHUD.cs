using UnityEngine;

// Legacy stub — delegates to HUDManager. Kept so LandmarkManager.ShowHUD() calls still compile.
[DefaultExecutionOrder(-49)]
public class GameHUD : MonoBehaviour
{
    public static GameHUD Instance;

    void Awake()
    {
        Instance = this;

        // Auto-bootstrap HUDManager if it isn't already in the scene.
        // HUDManager.Awake() fires synchronously inside AddComponent, so
        // HUDManager.Instance will be valid before this Awake() returns.
        if (HUDManager.Instance == null)
        {
            var go = new GameObject("HUDManager");
            go.AddComponent<HUDManager>();
        }

        // Deactivate legacy WaveHUD text — HUDManager renders its own top bar.
        Canvas canvas = GetComponentInParent<Canvas>() ?? FindFirstObjectByType<Canvas>();
        if (canvas != null)
        {
            var oldWave = canvas.transform.Find("WaveHUD");
            if (oldWave) oldWave.gameObject.SetActive(false);

            var towerPlacementUI = canvas.transform.Find("TowerPlacementUI");
            if (towerPlacementUI) towerPlacementUI.gameObject.SetActive(false);
        }
    }

    // LandmarkManager calls this — delegate to HUDManager.
    public void ShowHUD() => HUDManager.Instance?.ShowHUD();
}
