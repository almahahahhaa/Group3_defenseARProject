using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    [Header("Panels")]
    public GameObject settingsPanel;
    public GameObject creditsPanel;
    public GameObject helpPanel;
    public GameObject enemyDirectoryPanel;

    [Header("Toggles")]
    public Toggle musicToggle;
    public Toggle soundToggle;
    public Toggle cameraToggle;

    [Header("Audio")]
    public AudioSource musicSource;

    void Start()
    {
        // Close all panels at start
        if (settingsPanel != null) settingsPanel.SetActive(false);
        if (creditsPanel != null)  creditsPanel.SetActive(false);
        if (helpPanel != null)     helpPanel.SetActive(false);
        if (enemyDirectoryPanel != null)     enemyDirectoryPanel.SetActive(false);

        // Load saved preferences
        bool musicOn  = PlayerPrefs.GetInt("Music",  1) == 1;
        bool soundOn  = PlayerPrefs.GetInt("Sound",  1) == 1;
        bool cameraOn = PlayerPrefs.GetInt("Camera", 1) == 1;

        if (musicToggle  != null) musicToggle.isOn  = musicOn;
        if (soundToggle  != null) soundToggle.isOn  = soundOn;
        if (cameraToggle != null) cameraToggle.isOn = cameraOn;

        ApplyMusic(musicOn);

        // Hook up toggle listeners
        if (musicToggle  != null) musicToggle.onValueChanged.AddListener(OnMusicToggle);
        if (soundToggle  != null) soundToggle.onValueChanged.AddListener(OnSoundToggle);
        if (cameraToggle != null) cameraToggle.onValueChanged.AddListener(OnCameraToggle);
    }

    // ── Play Button ──────────────────────────────────────────────
    public void OnPlayButtonPressed()
    {
        // Loads the AR scene (index 0 in build settings)
        // Change "ARScene" to your actual scene name if different
        SceneManager.LoadScene("ARScene");
    }

    // ── Settings Panel ───────────────────────────────────────────
    public void OpenSettings()
    {
        if (settingsPanel != null) settingsPanel.SetActive(true);
    }

    public void CloseSettings()
    {
        if (settingsPanel != null) settingsPanel.SetActive(false);
    }

    // ── Credits Panel ────────────────────────────────────────────
    public void OpenCredits()
    {
        if (creditsPanel != null) creditsPanel.SetActive(true);
    }

    public void CloseCredits()
    {
        if (creditsPanel != null) creditsPanel.SetActive(false);
    }

    // ── Help Panel ───────────────────────────────────────────────
    public void OpenHelp()
    {
        if (helpPanel != null) helpPanel.SetActive(true);
    }

    public void CloseHelp()
    {
        if (helpPanel != null) helpPanel.SetActive(false);
    }

    //── Enemy Directory Panel ───────────────────────────────────────────────
    public void OpenEnemyDirectory()
    {
        if (enemyDirectoryPanel != null) enemyDirectoryPanel.SetActive(true);
    }

    public void CloseEnemyDirectory()
    {
        if (enemyDirectoryPanel != null) enemyDirectoryPanel.SetActive(false);
    }

    // ── Toggle Handlers ──────────────────────────────────────────
    void OnMusicToggle(bool isOn)
    {
        PlayerPrefs.SetInt("Music", isOn ? 1 : 0);
        ApplyMusic(isOn);
    }

    void OnSoundToggle(bool isOn)
    {
        PlayerPrefs.SetInt("Sound", isOn ? 1 : 0);
        AudioListener.volume = isOn ? 1f : 0f;
    }

    void OnCameraToggle(bool isOn)
    {
        PlayerPrefs.SetInt("Camera", isOn ? 1 : 0);
        // Camera access is handled by ARFoundation at runtime on device
        // This saves the preference; enforce it in your AR scene if needed
        Debug.Log("Camera Access preference set to: " + isOn);
    }

    void ApplyMusic(bool isOn)
    {
        if (musicSource != null)
        {
            if (isOn) musicSource.Play();
            else      musicSource.Pause();
        }
    }
}
