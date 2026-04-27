using TMPro;
using UnityEngine;

// Attach to the persistent wave indicator text in the HUD.
// Subscribes to EnemySpawner.OnWaveStarted and updates the label.
public class WaveHUD : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI waveLabel;

    void Awake()
    {
        if (waveLabel == null)
            waveLabel = GetComponent<TextMeshProUGUI>();
    }

    void OnEnable()
    {
        EnemySpawner.OnWaveStarted += UpdateLabel;
    }

    void OnDisable()
    {
        EnemySpawner.OnWaveStarted -= UpdateLabel;
    }

    void UpdateLabel(int wave)
    {
        if (waveLabel != null)
            waveLabel.text = $"Wave {wave} / {EnemySpawner.TotalWaves}";
    }
}
