using UnityEngine;

// Legacy holder for the existing defense tower prefab.
// The permanent placement flow has moved to AttackTowerPowerup.
public class TowerPlacer : MonoBehaviour
{
    public static TowerPlacer Instance;

    [Header("Tower Prefab")]
    public GameObject defenseTowerPrefab;

    public bool IsPlacingTower => AttackTowerPowerup.Instance != null && AttackTowerPowerup.Instance.IsPlacingTower;

    void Awake()
    {
        Instance = this;
    }

    public void CancelPlacement()
    {
        if (AttackTowerPowerup.Instance != null && AttackTowerPowerup.Instance.IsPlacingTower)
        {
            AttackTowerPowerup.Instance.DeactivateTower();
        }
    }
}
