using UnityEngine;
using UnityEngine.UI;

public class Burj_KhalifaHPSlider : MonoBehaviour
{
    private Slider hpSlider;

    void Awake()
    {
        hpSlider = GetComponent<Slider>();
    }

    public void Initialize(int maxHP)
    {
        hpSlider.maxValue = maxHP;
        hpSlider.value = maxHP;
    }

    public void UpdateHP(int currentHP)
    {
        hpSlider.value = currentHP;
    }
}