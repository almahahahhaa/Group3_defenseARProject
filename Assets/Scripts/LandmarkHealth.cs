using UnityEngine;
using ARDefense;

public class LandmarkHealth : MonoBehaviour
{
    public string landmarkName = "Landmark";
    public int maxHP = 100;
    public int currentHP;

    protected LandmarkHPSlider hpUI;

    protected virtual void Start()
    {
        currentHP = maxHP;

        hpUI = GetComponentInChildren<LandmarkHPSlider>(true);
        if (hpUI != null)
        {
            // The HP canvas starts inactive in the prefab; activate it now
            Canvas hpCanvas = hpUI.GetComponentInParent<Canvas>(true);
            if (hpCanvas != null) hpCanvas.gameObject.SetActive(true);
            hpUI.gameObject.SetActive(true);
            hpUI.Initialize(maxHP);
        }
    }

    public bool isShielded = false;

    public void SetShielded(bool state)
    {
        isShielded = state;
    }

    public virtual void TakeDamage(int damage)
    {
        if (isShielded) return;
        currentHP -= damage;
        currentHP = Mathf.Clamp(currentHP, 0, maxHP);
        Debug.Log($"{landmarkName} HP: {currentHP}");
        if (hpUI != null) hpUI.UpdateHP(currentHP);
        if (currentHP <= 0) OnLandmarkDestroyed();
    }

    protected virtual void OnLandmarkDestroyed()
    {
        Debug.Log($"{landmarkName} Destroyed!");
        if (GameManager.Instance != null)
            GameManager.Instance.OnGameOver();
    }
}
