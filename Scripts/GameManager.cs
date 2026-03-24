using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("UI Panels")]
    public GameObject waveClearedPanel;
    public GameObject gameOverPanel;

    [Header("Wave Cleared UI")]
    public TMPro.TextMeshProUGUI waveNumberText;
    public TMPro.TextMeshProUGUI enemiesDefeatedText;
    public TMPro.TextMeshProUGUI hpRemainingText;

    [Header("Game Over UI")]
    public TMPro.TextMeshProUGUI wavesSurvivedText;
    public TMPro.TextMeshProUGUI totalEnemiesText;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        if (waveClearedPanel != null) waveClearedPanel.SetActive(false);
        if (gameOverPanel != null) gameOverPanel.SetActive(false);
    }

    // waveEnemiesTapped = only this wave
    // totalEnemiesTapped = total across all waves
    public void OnWaveCleared(int wave, int waveEnemiesTapped, int totalEnemiesTapped)
    {
        Time.timeScale = 0f;

        if (waveNumberText != null)
            waveNumberText.text = "Wave " + wave;

        // Choose ONE of these based on what you want shown:
        // current wave only:
        if (enemiesDefeatedText != null)
            enemiesDefeatedText.text = totalEnemiesTapped.ToString();

        BurjKhalifaHealth health = FindFirstObjectByType<BurjKhalifaHealth>();
        if (hpRemainingText != null && health != null)
            hpRemainingText.text = health.currentHP.ToString();

        if (waveClearedPanel != null)
            waveClearedPanel.SetActive(true);
    }

    public void OnGameOver()
    {
        Time.timeScale = 0f;

        EnemySpawner spawner = FindFirstObjectByType<EnemySpawner>();
        if (wavesSurvivedText != null && spawner != null)
            wavesSurvivedText.text = spawner.currentWave.ToString();

        if (totalEnemiesText != null && spawner != null)
            totalEnemiesText.text = spawner.totalEnemiesTapped.ToString();

        if (gameOverPanel != null)
            gameOverPanel.SetActive(true);
    }

    public void OnNextWavePressed()
    {
        Time.timeScale = 1f;

        if (waveClearedPanel != null)
            waveClearedPanel.SetActive(false);

        EnemySpawner.Instance.StartNextWave();
    }

    public void OnTryAgainPressed()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void OnMainMenuPressed()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("Home");
    }
}