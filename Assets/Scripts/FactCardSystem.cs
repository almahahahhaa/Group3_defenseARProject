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
    public int sdgGoal;
}

public class FactCardSystem : MonoBehaviour
{
    public static FactCardSystem Instance;

    [Header("Timing")]
    public float factDisplayTime    = 3f;
    public float questionDisplayTime = 5f;

    [Header("Fact Card UI")]
    public GameObject factCardPanel;
    public TextMeshProUGUI factText;
    public Slider factTimerSlider;

    [Header("Question Card UI")]
    public GameObject questionCardPanel;
    public TextMeshProUGUI questionText;
    public Slider questionTimerSlider;
    public Button[] optionButtons = new Button[4];
    public TextMeshProUGUI[] optionTexts = new TextMeshProUGUI[4];

    [Header("Result UI")]
    public GameObject resultPanel;
    public TextMeshProUGUI resultText;

    [Header("Powerup UI")]
    public GameObject powerupPanel;
    public TextMeshProUGUI powerupText;

    public bool IsDisplaying { get; private set; }

    private FactCard[] cards;
    private bool answered;
    private bool correct;
    private Image _factSdgImage;

    void Awake()
    {
        Instance = this;
        InitializeCards();
    }

    void Start()
    {
        factCardPanel.SetActive(false);
        questionCardPanel.SetActive(false);
        resultPanel.SetActive(false);
        if (powerupPanel != null) powerupPanel.SetActive(false);
        EnsureFactSdgImage();
    }

    // Called by LandmarkManager (wave 1) and GameManager (waves 2-10)
    // Shows the fact + question for this wave, then starts the wave.
    public void TriggerForWave(int wave)
    {
        int idx = wave - 1;
        if (idx < 0 || idx >= cards.Length)
        {
            // No card for this wave — start immediately
            EnemySpawner.Instance.StartNextWave();
            return;
        }
        StartCoroutine(CardSequence(cards[idx]));
    }

    IEnumerator CardSequence(FactCard card)
    {
        IsDisplaying = true;
        HUDManager.Instance?.ShowLearningBreakBadge(true);

        // Let the player orient themselves before freezing the game
        yield return new WaitForSecondsRealtime(2f);

        Time.timeScale = 0f;

        yield return StartCoroutine(ShowFactCard(card));
        yield return StartCoroutine(ShowQuestionCard(card));
        yield return StartCoroutine(ShowResult());

        Time.timeScale = 1f;
        IsDisplaying = false;
        HUDManager.Instance?.ShowLearningBreakBadge(false);

        EnemySpawner.Instance.StartNextWave();
    }

    IEnumerator ShowFactCard(FactCard card)
    {
        factCardPanel.SetActive(true);
        factText.text = card.fact;
        SetFactCardSdg(card.sdgGoal);

        if (factTimerSlider != null)
        {
            factTimerSlider.maxValue = factDisplayTime;
            factTimerSlider.value    = factDisplayTime;
        }

        float timer = factDisplayTime;
        while (timer > 0f)
        {
            timer -= Time.unscaledDeltaTime;
            if (factTimerSlider != null)
                factTimerSlider.value = timer;
            yield return null;
        }

        factCardPanel.SetActive(false);
    }

    IEnumerator ShowQuestionCard(FactCard card)
    {
        answered = false;
        correct  = false;

        questionCardPanel.SetActive(true);
        questionText.text = card.question;

        for (int i = 0; i < optionButtons.Length; i++)
        {
            int index = i;
            optionTexts[i].text = card.options[i];
            optionButtons[i].interactable = true;
            optionButtons[i].GetComponent<Image>().color = new Color(0.2f, 0.1f, 0.4f);
            optionButtons[i].onClick.RemoveAllListeners();
            optionButtons[i].onClick.AddListener(() => OnAnswerSelected(index, card.correctIndex));
        }

        if (questionTimerSlider != null)
        {
            questionTimerSlider.maxValue = questionDisplayTime;
            questionTimerSlider.value    = questionDisplayTime;
        }

        float timer = questionDisplayTime;
        while (timer > 0f && !answered)
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
        correct  = selectedIndex == correctIndex;

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
            int awardType = Random.Range(0, 3);
            string powerupName;
            int charges;
            int maxC;

            if (awardType == 0)
            {
                if (PowerupManager.Instance != null)
                    PowerupManager.Instance.AwardShieldCharge();
                charges    = PowerupManager.Instance != null ? PowerupManager.Instance.GetCharges() : 0;
                powerupName = "Shield Charge";
                maxC = PowerupManager.Instance != null ? PowerupManager.Instance.maxCharges : 3;
            }
            else if (awardType == 1)
            {
                if (PowerupManager.Instance != null)
                    PowerupManager.Instance.AwardPylonCharge();
                charges    = PowerupManager.Instance != null ? PowerupManager.Instance.GetPylonCharges() : 0;
                powerupName = "Pylon Charge";
                maxC = PowerupManager.Instance != null ? PowerupManager.Instance.maxCharges : 3;
            }
            else
            {
                if (PowerupManager.Instance != null)
                    PowerupManager.Instance.AwardTowerCharge();
                charges    = PowerupManager.Instance != null ? PowerupManager.Instance.GetTowerCharges() : 0;
                powerupName = "Attack Tower";
                maxC = PowerupManager.Instance != null ? PowerupManager.Instance.maxTowerCharges : 3;
            }

            resultText.text  = $"Correct!\nYou earned a {powerupName}! ({charges}/{maxC})";
            resultText.color = Color.green;
            StartCoroutine(ApplySlowPowerup());
        }
        else
        {
            resultText.text  = "Wrong answer!\nNo power-up this time.";
            resultText.color = Color.red;
        }

        yield return new WaitForSecondsRealtime(2f);
        resultPanel.SetActive(false);
    }

    IEnumerator ApplySlowPowerup()
    {
        EnemyMoveToTarget[] enemies = FindObjectsByType<EnemyMoveToTarget>(FindObjectsSortMode.None);
        float[] originalSpeeds = new float[enemies.Length];
        for (int i = 0; i < enemies.Length; i++)
        {
            originalSpeeds[i] = enemies[i].moveSpeed;
            enemies[i].moveSpeed *= 0.3f;
        }

        yield return new WaitForSecondsRealtime(5f);

        for (int i = 0; i < enemies.Length; i++)
        {
            if (enemies[i] != null)
                enemies[i].moveSpeed = originalSpeeds[i];
        }
    }

    void EnsureFactSdgImage()
    {
        if (factCardPanel == null || _factSdgImage != null)
        {
            return;
        }

        Transform existing = factCardPanel.transform.Find("SDGGoalImage");
        if (existing != null)
        {
            _factSdgImage = existing.GetComponent<Image>();
            return;
        }

        GameObject imageObject = new GameObject("SDGGoalImage");
        imageObject.transform.SetParent(factCardPanel.transform, false);

        RectTransform rect = imageObject.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(1f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(1f, 1f);
        rect.anchoredPosition = new Vector2(-26f, -24f);
        rect.sizeDelta = new Vector2(200f, 200f);

        _factSdgImage = imageObject.AddComponent<Image>();
        _factSdgImage.preserveAspect = true;
        _factSdgImage.raycastTarget = false;
    }

    void SetFactCardSdg(int goal)
    {
        EnsureFactSdgImage();
        if (_factSdgImage == null)
        {
            return;
        }

        _factSdgImage.sprite = LoadSdgSprite(goal);
        _factSdgImage.enabled = _factSdgImage.sprite != null;
    }

    static Sprite LoadSdgSprite(int goal)
    {
        if (goal < 1 || goal > 17)
        {
            return null;
        }

        string resourcePath = $"SDG/sdg{goal}";
        Sprite sprite = Resources.Load<Sprite>(resourcePath);
        if (sprite != null)
        {
            return sprite;
        }

        Texture2D texture = Resources.Load<Texture2D>(resourcePath);
        if (texture == null)
        {
            return null;
        }

        return Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f), 100f);
    }

    void InitializeCards()
    {
        cards = new FactCard[]
        {
            // Wave 1
            new FactCard
            {
                fact = "The UAE aims to achieve net-zero emissions by 2050, despite its economy historically depending heavily on oil and gas exports.",
                question = "What is a major challenge the UAE faces in reaching net-zero by 2050?",
                options = new[] { "Limited access to sunlight", "Heavy reliance on oil and gas revenues", "Absence of urban development", "Lack of desalination technology" },
                correctIndex = 1,
                sdgGoal = 13
            },
            // Wave 2
            new FactCard
            {
                fact = "The Mohammed bin Rashid Al Maktoum Solar Park benefits from the UAE's year-round sunshine and intense desert sunlight.",
                question = "Why is large-scale solar deployment particularly viable in the UAE?",
                options = new[] { "Abundant forest cover", "Low temperatures year-round", "Consistent high solar irradiance", "High rainfall" },
                correctIndex = 2,
                sdgGoal = 7
            },
            // Wave 3
            new FactCard
            {
                fact = "The UAE relies on desalination, which produces freshwater but also releases concentrated salt (brine) back into the sea.",
                question = "What is a key environmental drawback of desalination?",
                options = new[] { "Brine discharge harming marine ecosystems", "Soil erosion", "Air pollution from fertilizers", "Deforestation" },
                correctIndex = 0,
                sdgGoal = 14
            },
            // Wave 4
            new FactCard
            {
                fact = "Masdar City in Abu Dhabi focuses on renewable energy use and highly efficient buildings to reduce emissions.",
                question = "Which feature contributes most to Masdar City's sustainability?",
                options = new[] { "Wide highways", "High-rise glass towers without shading", "Car-dependent transport", "Energy-efficient buildings and renewable energy" },
                correctIndex = 3,
                sdgGoal = 11
            },
            // Wave 5
            new FactCard
            {
                fact = "The UAE is investing in green hydrogen, which is produced using clean energy sources like solar power instead of fossil fuels.",
                question = "What makes hydrogen \"green\"?",
                options = new[] { "It is extracted from plants", "It emits green light", "It is produced using renewable energy", "It is naturally green in color" },
                correctIndex = 2,
                sdgGoal = 7
            },
            // Wave 6
            new FactCard
            {
                fact = "Several emirates have banned single-use plastic bags to reduce visible pollution in oceans and deserts.",
                question = "What is the primary goal of banning single-use plastics?",
                options = new[] { "Promote imports", "Reduce marine and land pollution", "Increase manufacturing", "Lower oil prices" },
                correctIndex = 1,
                sdgGoal = 12
            },
            // Wave 7
            new FactCard
            {
                fact = "The UAE has a high ecological footprint per capita, partly due to high energy use, consumption, and lifestyle patterns.",
                question = "What contributes most to a high ecological footprint?",
                options = new[] { "Small population", "Minimal transportation", "Low energy use", "High consumption of resources and energy" },
                correctIndex = 3,
                sdgGoal = 12
            },
            // Wave 8
            new FactCard
            {
                fact = "District cooling systems in UAE cities cool multiple buildings from a central plant, improving overall efficiency compared to individual AC units.",
                question = "Why are district cooling systems more sustainable?",
                options = new[] { "They require more buildings", "They centralize and improve cooling efficiency", "They use more electricity", "They increase water use" },
                correctIndex = 1,
                sdgGoal = 11
            },
            // Wave 9
            new FactCard
            {
                fact = "The Barakah nuclear plant provides electricity with very low carbon emissions during operation, helping reduce reliance on fossil fuels.",
                question = "What is a benefit of nuclear energy in sustainability?",
                options = new[] { "Unlimited fuel supply", "No safety concerns", "Low carbon emissions during operation", "Zero waste production" },
                correctIndex = 2,
                sdgGoal = 7
            },
            // Wave 10
            new FactCard
            {
                fact = "By hosting COP28, the UAE positioned itself as a leader in global discussions on climate change solutions.",
                question = "What is COP primarily focused on?",
                options = new[] { "Tourism promotion", "Climate change negotiations", "Trade agreements", "Oil pricing" },
                correctIndex = 1,
                sdgGoal = 13
            },
        };
    }
}
