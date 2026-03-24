using UnityEngine;

public class BurjKhalifaHealth : MonoBehaviour
{
    public int maxHP = 100;
    public int currentHP;
    public int damagePerHit = 2;

    private Burj_KhalifaHPSlider hpUI;

    void Start()
    {
        currentHP = maxHP;

        hpUI = FindFirstObjectByType<Burj_KhalifaHPSlider>();

        if (hpUI != null)
            hpUI.Initialize(maxHP);
        else
            Debug.LogError("HP Slider UI not found!");
    }

    // Called by enemy on trigger contact
    public void TakeDamage(int damage)
    {
        currentHP -= damage;
        currentHP = Mathf.Clamp(currentHP, 0, maxHP);

        Debug.Log("Burj Khalifa HP: " + currentHP);

        if (hpUI != null)
            hpUI.UpdateHP(currentHP);

        if (currentHP <= 0)
            OnDestroyed();
    }

    void OnDestroyed()
    {
        Debug.Log("Burj Khalifa Destroyed!");
        if (GameManager.Instance != null)
            GameManager.Instance.OnGameOver();
    }
}