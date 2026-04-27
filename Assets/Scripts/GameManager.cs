using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ARDefense
{

public class GameManager : MonoBehaviour
{
    const string HomeSceneName = "Home";
    const string WaveClearedSubtitle = "Defenses held. Prepare for the next wave.";
    const string GameOverSubtitle = "The landmarks fell. Regroup and try again.";
    const string GameWonSubtitle = "All waves repelled. The city stands secure.";

    public static GameManager Instance;

    [Header("Panels")]
    public GameObject waveClearedPanel;
    public GameObject gameOverPanel;
    public GameObject gameWonPanel;

    [Header("Wave Cleared UI")]
    public TextMeshProUGUI waveNumberText;
    public TextMeshProUGUI enemiesDefeatedText;
    public TextMeshProUGUI hpRemainingText;
    public TextMeshProUGUI towersAwardedText;

    [Header("Game Over UI")]
    public TextMeshProUGUI wavesSurvivedText;
    public TextMeshProUGUI totalEnemiesText;

    [Header("Game Won UI")]
    public TextMeshProUGUI wonTotalEnemiesText;

    TextMeshProUGUI _waveClearedSubtitle;
    TextMeshProUGUI _gameOverSubtitle;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        if (waveClearedPanel != null) waveClearedPanel.SetActive(false);
        if (gameOverPanel != null) gameOverPanel.SetActive(false);
        if (gameWonPanel != null) gameWonPanel.SetActive(false);
        if (towersAwardedText != null) towersAwardedText.gameObject.SetActive(false);

        _waveClearedSubtitle = FindPanelText(waveClearedPanel, "Text (TMP)");
        _gameOverSubtitle = FindPanelText(gameOverPanel, "Text (TMP)");
    }

    public void OnWaveCleared(int wave, int waveEnemiesTapped, int totalEnemiesTapped)
    {
        Time.timeScale = 0f;

        if (waveNumberText != null) waveNumberText.text = $"Wave {wave} Cleared!";
        if (enemiesDefeatedText != null) enemiesDefeatedText.text = totalEnemiesTapped.ToString();
        if (hpRemainingText != null) hpRemainingText.text = BuildHPSummary();
        if (_waveClearedSubtitle != null) _waveClearedSubtitle.text = WaveClearedSubtitle;

        if (waveClearedPanel != null) waveClearedPanel.SetActive(true);
    }

    public void OnGameWon(int waveEnemiesTapped, int totalEnemiesTapped)
    {
        Time.timeScale = 0f;

        if (wonTotalEnemiesText != null)
        {
            wonTotalEnemiesText.text = totalEnemiesTapped.ToString();
        }

        if (gameWonPanel != null)
        {
            TextMeshProUGUI wonSubtitle = FindPanelText(gameWonPanel, "Text (TMP)");
            if (wonSubtitle != null) wonSubtitle.text = GameWonSubtitle;
            gameWonPanel.SetActive(true);
        }
        else if (waveClearedPanel != null)
        {
            if (waveNumberText != null) waveNumberText.text = "You Won!";
            if (enemiesDefeatedText != null) enemiesDefeatedText.text = totalEnemiesTapped.ToString();
            if (_waveClearedSubtitle != null) _waveClearedSubtitle.text = GameWonSubtitle;
            waveClearedPanel.SetActive(true);
        }
    }

    public void OnGameOver()
    {
        Time.timeScale = 0f;

        EnemySpawner spawner = FindFirstObjectByType<EnemySpawner>();
        if (wavesSurvivedText != null && spawner != null)
        {
            wavesSurvivedText.text = spawner.currentWave.ToString();
        }

        if (totalEnemiesText != null && spawner != null)
        {
            totalEnemiesText.text = spawner.totalEnemiesTapped.ToString();
        }

        if (_gameOverSubtitle != null)
        {
            _gameOverSubtitle.text = GameOverSubtitle;
        }

        if (gameOverPanel != null) gameOverPanel.SetActive(true);
    }

    public void OnNextWavePressed()
    {
        Time.timeScale = 1f;
        if (waveClearedPanel != null) waveClearedPanel.SetActive(false);

        int nextWave = EnemySpawner.Instance.currentWave + 1;
        if (FactCardSystem.Instance != null)
        {
            FactCardSystem.Instance.TriggerForWave(nextWave);
        }
        else
        {
            EnemySpawner.Instance.StartNextWave();
        }
    }

    public void OnTryAgainPressed()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void OnMainMenuPressed()
    {
        GameSessionFlow.PrepareForMainMenuReturn();
        SceneManager.LoadScene(HomeSceneName);
    }

    string BuildHPSummary()
    {
        var parts = new System.Text.StringBuilder();

        foreach (LandmarkHealth health in FindObjectsByType<LandmarkHealth>(FindObjectsSortMode.None))
        {
            if (parts.Length > 0) parts.Append("  ");
            parts.Append($"{Abbreviate(health.landmarkName)}:{health.currentHP}");
        }

        return parts.Length > 0 ? parts.ToString() : "-";
    }

    static string Abbreviate(string name)
    {
        var sb = new System.Text.StringBuilder();
        foreach (string word in name.Split(' '))
        {
            if (word.Length > 0)
            {
                sb.Append(char.ToUpper(word[0]));
            }
        }

        return sb.ToString();
    }

    static TextMeshProUGUI FindPanelText(GameObject panel, string childPath)
    {
        if (panel == null)
        {
            return null;
        }

        Transform child = panel.transform.Find(childPath);
        return child != null ? child.GetComponent<TextMeshProUGUI>() : null;
    }
}

}
