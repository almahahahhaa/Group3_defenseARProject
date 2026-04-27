using System;

public static class GameEvents
{
    public static event Action      OnEnemyDestroyed;
    public static event Action      OnTowerPlaced;
    public static event Action<int> OnWaveChanged;
    public static event Action<int> OnShieldChargesChanged;
    public static event Action<int> OnPylonChargesChanged;
    public static event Action<int> OnTowerChargesChanged;

    public static void EnemyDestroyed()            => OnEnemyDestroyed?.Invoke();
    public static void TowerPlaced()               => OnTowerPlaced?.Invoke();
    public static void WaveChanged(int wave)       => OnWaveChanged?.Invoke(wave);
    public static void ShieldChargesChanged(int count) => OnShieldChargesChanged?.Invoke(count);
    public static void PylonChargesChanged(int count)  => OnPylonChargesChanged?.Invoke(count);
    public static void TowerChargesChanged(int count)  => OnTowerChargesChanged?.Invoke(count);
}
