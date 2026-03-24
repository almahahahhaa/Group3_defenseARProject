using System.Collections;
using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    [Header("Enemy Settings")]
    public GameObject enemyPrefab;
    public float spawnInterval = 3f;
    public int enemiesPerWave = 5;

    [Header("Spawn Area")]
    public float spawnRadius = 0.4f;

    [HideInInspector] public int currentWave = 0;

    // Total across the whole game
    [HideInInspector] public int totalEnemiesTapped = 0;

    private int spawnedThisWave = 0;
    private int removedThisWave = 0;     // tapped OR reached tower
    private int tappedThisWave = 0;      // tapped only

    private bool waveActive = false;
    private bool waitingForNextWave = false;

    public static EnemySpawner Instance;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        StartNextWave();
    }

    public void StartNextWave()
    {
        currentWave++;
        spawnedThisWave = 0;
        removedThisWave = 0;
        tappedThisWave = 0;

        waveActive = true;
        waitingForNextWave = false;

        StartCoroutine(SpawnLoop());
    }

    IEnumerator SpawnLoop()
    {
        while (spawnedThisWave < enemiesPerWave)
        {
            yield return new WaitForSeconds(spawnInterval);
            SpawnEnemy();
        }
    }

    void SpawnEnemy()
    {
        if (enemyPrefab == null) return;

        float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
        Vector3 spawnOffset = new Vector3(
            Mathf.Cos(angle) * spawnRadius,
            0.1f,
            Mathf.Sin(angle) * spawnRadius
        );

        Vector3 spawnPos = transform.position + spawnOffset;
        GameObject enemy = Instantiate(enemyPrefab, spawnPos, Quaternion.identity);
        enemy.transform.SetParent(transform);

        spawnedThisWave++;
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

        // Wave ends when all spawned enemies are gone for any reason
        if (spawnedThisWave >= enemiesPerWave && removedThisWave >= enemiesPerWave)
        {
            waitingForNextWave = true;
            waveActive = false;

            if (GameManager.Instance != null)
            {
                // waveTapped = how many the player actually killed this wave
                // totalTapped = running total across all waves
                GameManager.Instance.OnWaveCleared(currentWave, tappedThisWave, totalEnemiesTapped);
            }
        }
    }
}