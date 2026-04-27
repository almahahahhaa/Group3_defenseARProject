using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class EnemyDirectoryNavigator : MonoBehaviour
{
    [SerializeField] public GameObject[] pages;
    [SerializeField] public TextMeshProUGUI pageIndicator;
    [SerializeField] public Text pageIndicatorText;
    [SerializeField] public Button prevButton;
    [SerializeField] public Button nextButton;

    int _current;

    void OnEnable()
    {
        if (pages == null || pages.Length == 0)
        {
            return;
        }

        ShowPage(0);
    }

    public void Next()
    {
        if (pages == null || pages.Length == 0)
        {
            return;
        }

        if (_current < pages.Length - 1)
            ShowPage(_current + 1);
    }

    public void Prev()
    {
        if (pages == null || pages.Length == 0)
        {
            return;
        }

        if (_current > 0)
            ShowPage(_current - 1);
    }

    void ShowPage(int index)
    {
        if (pages == null || pages.Length == 0)
        {
            return;
        }

        index = Mathf.Clamp(index, 0, pages.Length - 1);

        for (int i = 0; i < pages.Length; i++)
            if (pages[i] != null) pages[i].SetActive(i == index);
        _current = index;
        string indicator = $"{_current + 1} / {pages.Length}";
        if (pageIndicator != null) pageIndicator.text = indicator;
        if (pageIndicatorText != null) pageIndicatorText.text = indicator;
        if (prevButton != null) prevButton.interactable = _current > 0;
        if (nextButton != null) nextButton.interactable = _current < pages.Length - 1;
    }
}
