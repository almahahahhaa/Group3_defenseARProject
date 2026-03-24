using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class Burj_KhalifaHPSlider : MonoBehaviour
{
    private Slider hpSlider;
    public TextMeshProUGUI hpText; // drag your Text (TMP) child here

    void Awake()
    {
        hpSlider = GetComponent<Slider>();

        // Hide at start
        gameObject.SetActive(false);
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