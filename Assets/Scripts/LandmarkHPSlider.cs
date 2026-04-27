using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class LandmarkHPSlider : MonoBehaviour
{
    private Slider hpSlider;
    public TextMeshProUGUI hpText;

    void Awake()
    {
        hpSlider = GetComponent<Slider>();
        // Visibility is controlled by LandmarkHealth.Start() — canvas starts inactive in prefab
    }

    public void Initialize(int maxHP)
    {
        hpSlider.maxValue = maxHP;
        hpSlider.value = maxHP;
        if (hpText != null) hpText.text = maxHP.ToString();
    }

    public void UpdateHP(int currentHP)
    {
        hpSlider.value = currentHP;
        if (hpText != null) hpText.text = currentHP.ToString();
    }
}
