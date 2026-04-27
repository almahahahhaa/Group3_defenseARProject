using UnityEngine;

public class PowerupManager : MonoBehaviour
{
    public static PowerupManager Instance;

    public int shieldCharges = 0;
    public int pylonCharges = 0;
    public int towerCharges = 0;
    public int maxCharges = 3;
    public int maxTowerCharges = 3;
    public bool isTowerActive = false;

    public bool ShieldPlacementMode { get; set; }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void AwardShieldCharge()
    {
        shieldCharges = Mathf.Min(shieldCharges + 1, maxCharges);
        GameEvents.ShieldChargesChanged(shieldCharges);
    }

    public bool ConsumeCharge()
    {
        if (shieldCharges <= 0)
        {
            return false;
        }

        shieldCharges--;
        GameEvents.ShieldChargesChanged(shieldCharges);
        return true;
    }

    public int GetCharges() => shieldCharges;

    public void AwardPylonCharge()
    {
        pylonCharges = Mathf.Min(pylonCharges + 1, maxCharges);
        GameEvents.PylonChargesChanged(pylonCharges);
    }

    public bool ConsumePylonCharge()
    {
        if (pylonCharges <= 0)
        {
            return false;
        }

        pylonCharges--;
        GameEvents.PylonChargesChanged(pylonCharges);
        return true;
    }

    public int GetPylonCharges() => pylonCharges;

    public void AwardTowerCharge()
    {
        towerCharges = Mathf.Min(towerCharges + 1, maxTowerCharges);
        GameEvents.TowerChargesChanged(towerCharges);
    }

    public bool ConsumeTowerCharge()
    {
        if (towerCharges <= 0)
        {
            return false;
        }

        towerCharges--;
        GameEvents.TowerChargesChanged(towerCharges);
        return true;
    }

    public int GetTowerCharges() => towerCharges;

    public void ResetForNewSession()
    {
        shieldCharges = 0;
        pylonCharges = 0;
        towerCharges = 0;
        isTowerActive = false;
        ShieldPlacementMode = false;

        GameEvents.ShieldChargesChanged(shieldCharges);
        GameEvents.PylonChargesChanged(pylonCharges);
        GameEvents.TowerChargesChanged(towerCharges);
    }
}
