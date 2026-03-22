using System.Collections;
using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    [Header("Enemy Settings")]
    public GameObject enemyPrefab;       // Drag PollutionCloud prefab here
    public float spawnInterval = 5f;     // Seconds between spawns
    public int maxEnemies = 10;          // Max enemies at once

    [Header("Spawn Area")]
    public float spawnRadius = 0.4f;     // How far from center enemies spawn

    private int spawnedCount = 0;

    void Start()
    {
        StartCoroutine(SpawnLoop());
    }

    IEnumerator SpawnLoop()
    {
        while (spawnedCount < maxEnemies)
        {
            yield return new WaitForSeconds(spawnInterval);
            SpawnEnemy();
        }
    }

    void SpawnEnemy()
    {
        if (enemyPrefab == null)
        {
            Debug.LogError("Enemy prefab not assigned to EnemySpawner!");
            return;
        }

        // Pick a random point on the edge of the platform
        float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
        Vector3 spawnOffset = new Vector3(
            Mathf.Cos(angle) * spawnRadius,
            0.1f,
            Mathf.Sin(angle) * spawnRadius
        );

        Vector3 spawnPos = transform.position + spawnOffset;

        GameObject enemy = Instantiate(enemyPrefab, spawnPos, Quaternion.identity);
        enemy.transform.SetParent(transform);
        spawnedCount++;

        Debug.Log("Enemy spawned at: " + spawnPos);
    }
}
