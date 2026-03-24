using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

[System.Serializable]
public class FactCard
{
    public string fact;
    public string question;
    public string[] options = new string[4];
    public int correctIndex; // 0=A, 1=B, 2=C, 3=D
}

public class FactCardSystem : MonoBehaviour
{
    [Header("Timing")]
    public float delayBeforeFirst = 3f;
    public float factDisplayTime = 10f;
    public float questionDisplayTime = 10f;

    [Header("Fact Card UI")]
    public GameObject factCardPanel;
    public TextMeshProUGUI factText;
    public Slider factTimerSlider;      // CHANGED: was TextMeshProUGUI factTimerText

    [Header("Question Card UI")]
    public GameObject questionCardPanel;
    public TextMeshProUGUI questionText;
    public Slider questionTimerSlider;  // CHANGED: was TextMeshProUGUI questionTimerText
    public Button[] optionButtons = new Button[4];
    public TextMeshProUGUI[] optionTexts = new TextMeshProUGUI[4];

    [Header("Result UI")]
    public GameObject resultPanel;
    public TextMeshProUGUI resultText;

    [Header("Powerup UI")]
    public GameObject powerupPanel;
    public TextMeshProUGUI powerupText;

    private FactCard[] cards;
    private bool answered = false;
    private bool correct = false;

    void Start()
    {
        InitializeCards();
        factCardPanel.SetActive(false);
        questionCardPanel.SetActive(false);
        resultPanel.SetActive(false);
        if (powerupPanel != null) powerupPanel.SetActive(false);
        // Remove the StartCoroutine(CardSequence()) line
    }

    private bool hasStarted = false;

    public void StartCardSequence()
    {
        if (hasStarted) return; // prevent multiple calls
        hasStarted = true;
        StartCoroutine(CardSequence());
    }

    void InitializeCards()
    {
        cards = new FactCard[]
        {
            new FactCard
            {
                fact = "The Masdar City project in Abu Dhabi was launched in 2008 with the goal of being one of the world's most sustainable urban communities, powered entirely by renewable energy.",
                question = "What is the main energy goal of Masdar City?",
                options = new string[]
                {
                    "Use only nuclear power",
                    "Reduce oil imports",
                    "Run entirely on renewable energy",
                    "Become the largest solar farm in the world"
                },
                correctIndex = 2
            }
        };
    }

    IEnumerator CardSequence()
    {
        yield return new WaitForSeconds(delayBeforeFirst);

        foreach (FactCard card in cards)
        {
            Time.timeScale = 0f;
            yield return StartCoroutine(ShowFactCard(card));
            yield return StartCoroutine(ShowQuestionCard(card));
            yield return StartCoroutine(ShowResult());
            Time.timeScale = 1f;
        }
    }

    IEnumerator ShowFactCard(FactCard card)
    {
        factCardPanel.SetActive(true);
        factText.text = card.fact;

        float timer = factDisplayTime;
        if (factTimerSlider != null)
        {
            factTimerSlider.maxValue = factDisplayTime;
            factTimerSlider.value = factDisplayTime;
        }

        while (timer > 0)
        {
            timer -= Time.unscaledDeltaTime;
            if (factTimerSlider != null)
                factTimerSlider.value = timer;  // Drains left as time passes
            yield return null;
        }

        factCardPanel.SetActive(false);
    }

    IEnumerator ShowQuestionCard(FactCard card)
    {
        answered = false;
        correct = false;

        questionCardPanel.SetActive(true);
        questionText.text = card.question;

        //string[] labels = { "A", "B", "C", "D" };
        for (int i = 0; i < optionButtons.Length; i++)
        {
            int index = i;
            optionTexts[i].text = card.options[i];
            optionButtons[i].interactable = true;
            optionButtons[i].GetComponent<Image>().color = new Color(0.2f, 0.1f, 0.4f);
            optionButtons[i].onClick.RemoveAllListeners();
            optionButtons[i].onClick.AddListener(() => OnAnswerSelected(index, card.correctIndex));
        }

        float timer = questionDisplayTime;
        if (questionTimerSlider != null)
        {
            questionTimerSlider.maxValue = questionDisplayTime;
            questionTimerSlider.value = questionDisplayTime;
        }

        while (timer > 0 && !answered)
        {
            timer -= Time.unscaledDeltaTime;
            if (questionTimerSlider != null)
                questionTimerSlider.value = timer;
            yield return null;
        }

        foreach (var btn in optionButtons)
            btn.interactable = false;

        yield return new WaitForSecondsRealtime(1f);
        questionCardPanel.SetActive(false);
    }

    void OnAnswerSelected(int selectedIndex, int correctIndex)
    {
        answered = true;
        correct = selectedIndex == correctIndex;

        for (int i = 0; i < optionButtons.Length; i++)
        {
            if (i == correctIndex)
                optionButtons[i].GetComponent<Image>().color = new Color(0.1f, 0.6f, 0.2f);
            else if (i == selectedIndex && selectedIndex != correctIndex)
                optionButtons[i].GetComponent<Image>().color = new Color(0.7f, 0.1f, 0.1f);
        }
    }

    IEnumerator ShowResult()
    {
        resultPanel.SetActive(true);

        if (correct)
        {
            resultText.text = "Correct!\nYou earned a powerup: Slow Enemies!";
            resultText.color = Color.green;
            StartCoroutine(ApplySlowPowerup());
        }
        else
        {
            resultText.text = "Wrong answer!\nNo powerup this time.";
            resultText.color = Color.red;
        }

        yield return new WaitForSecondsRealtime(2f);
        resultPanel.SetActive(false);
    }

    IEnumerator ApplySlowPowerup()
    {
        if (powerupPanel != null)
        {
            powerupPanel.SetActive(true);
            if (powerupText != null)
                powerupText.text = "Enemies slowed for 5 seconds!";
        }

        EnemyMoveToTarget[] enemies = FindObjectsByType<EnemyMoveToTarget>(FindObjectsSortMode.None);
        float originalSpeed = 0.007f;
        foreach (var e in enemies)
            e.moveSpeed = originalSpeed * 0.3f;

        yield return new WaitForSecondsRealtime(5f);

        enemies = FindObjectsByType<EnemyMoveToTarget>(FindObjectsSortMode.None);
        foreach (var e in enemies)
            e.moveSpeed = originalSpeed;

        if (powerupPanel != null)
            powerupPanel.SetActive(false);
    }
}
