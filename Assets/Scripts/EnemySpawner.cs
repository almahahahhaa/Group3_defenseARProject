using System.Collections;
using UnityEngine;
using ARDefense;

public class EnemySpawner : MonoBehaviour
{
    public static EnemySpawner Instance;

    // Fired whenever a new wave starts (passes the 1-based wave number)
    public static event System.Action<int> OnWaveStarted;

    public const int TotalWaves = 10;

    [Header("Enemy Settings")]
    public GameObject enemyPrefab;
    public GameObject oilSlickPrefab;
    public GameObject miniOilSlickPrefab;

    [Header("Spawn Area")]
    public float spawnRadius = 0.4f;

    // ── Wave data (index 0 = wave 1) ─────────────────────────────────────────
    private static readonly int[]   EnemyCounts = { 3, 5, 8, 12, 17, 23, 30, 40, 52, 68 };
    private static readonly float[] Intervals   = { 3.0f, 2.5f, 2.2f, 1.9f, 1.7f, 1.5f, 1.3f, 1.1f, 0.9f, 0.7f };

    // Base move speed per wave. Early waves are forgiving; mid-game has two noticeable
    // jumps (waves 5 and 8); wave 10 spikes for a dramatic finale.
    private static readonly float[] Speeds = { 0.030f, 0.038f, 0.047f, 0.057f, 0.072f,
                                                0.088f, 0.105f, 0.130f, 0.160f, 0.220f };

    [HideInInspector] public int currentWave          = 0;
    [HideInInspector] public int totalEnemiesTapped   = 0;

    private int   spawnedThisWave  = 0;
    private int   removedThisWave  = 0;
    private int   tappedThisWave   = 0;
    private bool  waitingForNextWave = false;

    void Awake() { Instance = this; }

    // LandmarkManager.OnLandmarkPlaced calls StartNextWave() once all three landmarks
    // are placed. Nothing else should trigger it at startup.
    void Start() { }

    public void StartNextWave()
    {
        currentWave++;
        spawnedThisWave  = 0;
        removedThisWave  = 0;
        tappedThisWave   = 0;
        waitingForNextWave = false;

        int idx = Mathf.Clamp(currentWave - 1, 0, TotalWaves - 1);
        float interval   = Intervals[idx];
        int   enemyCount = EnemyCounts[idx];

        OnWaveStarted?.Invoke(currentWave);
        GameEvents.WaveChanged(currentWave);

        StartCoroutine(SpawnLoop(enemyCount, interval));
    }

    IEnumerator SpawnLoop(int count, float interval)
    {
        while (spawnedThisWave < count)
        {
            yield return new WaitForSeconds(interval);
            SpawnEnemy();
        }
    }

    void SpawnEnemy()
    {
        // From wave 3 onwards, oil slicks have a chance to spawn (scales up each wave)
        bool useOilSlick = currentWave >= 3
                        && oilSlickPrefab != null
                        && Random.value < Mathf.Min(0.2f + (currentWave - 3) * 0.025f, 0.45f);

        GameObject prefabToSpawn = useOilSlick ? oilSlickPrefab : enemyPrefab;
        if (prefabToSpawn == null) return;

        if (useOilSlick) Debug.Log($"[EnemySpawner] Spawning OilSlick on wave {currentWave}");

        // Pick a random landmark to spawn near and assign as the enemy's target
        GameObject targetLandmark = null;
        Vector3 spawnCenter = transform.position;

        if (LandmarkManager.Instance != null)
        {
            targetLandmark = LandmarkManager.Instance.GetRandomLandmark();
            if (targetLandmark != null)
                spawnCenter = targetLandmark.transform.position;
        }

        float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
        Vector3 offset = new Vector3(Mathf.Cos(angle) * spawnRadius, 0.1f, Mathf.Sin(angle) * spawnRadius);
        GameObject enemy = Instantiate(prefabToSpawn, spawnCenter + offset, Quaternion.identity);
        enemy.transform.SetParent(transform);

        // Apply wave speed with ±15% random variation so enemies spread naturally.
        int idx = Mathf.Clamp(currentWave - 1, 0, TotalWaves - 1);
        float baseSpeed = Speeds[idx];
        EnemyMoveToTarget mover = enemy.GetComponent<EnemyMoveToTarget>();
        if (mover != null)
        {
            mover.moveSpeed = baseSpeed * Random.Range(0.85f, 1.15f);
            if (targetLandmark != null)
                mover.AssignTarget(targetLandmark.transform);
        }

        spawnedThisWave++;
    }

    // Called by OilSlickTapHandler when a slick splits into mini slicks.
    // Registers extra enemies so the wave counter stays accurate.
    public void RegisterExtraEnemies(int count)
    {
        spawnedThisWave += count;
    }

    public void OnEnemyTapped()
    {
        totalEnemiesTapped++;
        tappedThisWave++;
        removedThisWave++;
        CheckWaveCompleted();
    }

    public void OnEnemyReachedTower()
    {
        removedThisWave++;
        CheckWaveCompleted();
    }

    void CheckWaveCompleted()
    {
        if (waitingForNextWave) return;

        int needed = EnemyCounts[Mathf.Clamp(currentWave - 1, 0, TotalWaves - 1)];
        if (spawnedThisWave >= needed && removedThisWave >= spawnedThisWave)
        {
            waitingForNextWave = true;

            if (currentWave >= TotalWaves)
            {
                if (GameManager.Instance != null)
                    GameManager.Instance.OnGameWon(tappedThisWave, totalEnemiesTapped);
            }
            else
            {
                if (GameManager.Instance != null)
                    GameManager.Instance.OnWaveCleared(currentWave, tappedThisWave, totalEnemiesTapped);
            }
        }
    }
}
