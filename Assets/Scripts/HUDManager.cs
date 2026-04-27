using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.XR.ARFoundation;

[DefaultExecutionOrder(-48)]
public class HUDManager : MonoBehaviour
{
    private static int SessionScore;

    private sealed class PowerupButtonRefs
    {
        public GameObject Root;
        public Button Button;
        public Image Background;
        public Image Icon;
        public Image BadgeBackground;
        public TextMeshProUGUI BadgeText;
    }

    public static HUDManager Instance;

    public TextMeshProUGUI WaveCounter;
    public TextMeshProUGUI PlayTimer;
    public TextMeshProUGUI ScoreCounter;
    public Button ScanSurfaceButton;
    public GameObject WaveAnnouncementOverlay;
    public GameObject LearningBreakBadge;

    [Header("Pylon Network")]
    public GameObject pylonPrefab;

    static readonly Color Navy = new Color(0.051f, 0.051f, 0.169f, 1.00f);
    static readonly Color Cyan = new Color(0.000f, 1.000f, 0.898f, 1.00f);
    static readonly Color GreenAccent = new Color(0.224f, 1.000f, 0.078f, 1.00f);
    static readonly Color Gold = new Color(1.000f, 0.722f, 0.000f, 1.00f);
    static readonly Color PanelBg = new Color(0.102f, 0.039f, 0.227f, 0.85f);
    static readonly Color White = new Color(0.950f, 0.980f, 1.000f, 1.00f);
    static readonly Color FadedWhite = new Color(0.800f, 0.850f, 0.900f, 0.70f);
    static readonly Color GrayDisabled = new Color(0.266f, 0.266f, 0.400f, 1f);
    static readonly Color ShieldGreen = new Color(0f, 0.784f, 0.325f, 1f);
    static readonly Color ActiveIconTint = Color.white;

    static readonly string[] WaveFlavors =
    {
        "Pollution spreads. Defend the UAE.",
        "The Oil Slick Crawlers have arrived.",
        "Smog thickens. Hold the line.",
        "The assault intensifies. Stand firm.",
        "Green energy. Stronger defenses.",
        "The enemy adapts. So must you.",
        "Critical systems under threat.",
        "Reinforce! The tide turns now.",
        "Final push approaches. Endure.",
        "The last wave. Make it count."
    };

    private bool _hudVisible;
    private bool _timerRunning;
    private bool _timerFrozen;
    private float _elapsed;
    private int _score;

    private Canvas _canvas;
    private GameObject _shieldInstructionBanner;
    private TextMeshProUGUI _waveBigNumber;
    private TextMeshProUGUI _waveFlavorText;
    private TextMeshProUGUI _waveIncomingLine;
    private Button _pauseButton;
    private GameObject _pauseOverlay;
    private bool _pauseMenuOpen;

    private PowerupButtonRefs _shieldButton;
    private PowerupButtonRefs _pylonButton;
    private PowerupButtonRefs _towerButton;
    private Sprite _shieldIconSprite;
    private Sprite _pylonIconSprite;
    private Sprite _towerIconSprite;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        SessionScore = 0;
        LoadPowerupIcons();
        BuildCanvas();
        SetHUDVisible(false);
        ApplyPowerupIcons();
        SyncScoreLabel();

        EnsureManager<PowerupManager>("PowerupManager");
        EnsureManager<AttackTowerPowerup>("AttackTowerPowerup");

        if (PylonNetworkManager.Instance == null)
        {
            var pylonObject = new GameObject("PylonNetworkManager");
            var manager = pylonObject.AddComponent<PylonNetworkManager>();
            if (pylonPrefab != null)
            {
                manager.pylonPrefab = pylonPrefab;
            }
        }
        else if (pylonPrefab != null && PylonNetworkManager.Instance.pylonPrefab == null)
        {
            PylonNetworkManager.Instance.pylonPrefab = pylonPrefab;
        }
    }

    void Start()
    {
        RefreshPowerupButtons();
    }

    void OnEnable()
    {
        GameEvents.OnEnemyDestroyed += HandleEnemyDestroyed;
        GameEvents.OnWaveChanged += HandleWaveChanged;
        GameEvents.OnShieldChargesChanged += HandleShieldChargesChanged;
        GameEvents.OnPylonChargesChanged += HandlePylonChargesChanged;
        GameEvents.OnTowerChargesChanged += HandleTowerChargesChanged;
    }

    void OnDisable()
    {
        GameEvents.OnEnemyDestroyed -= HandleEnemyDestroyed;
        GameEvents.OnWaveChanged -= HandleWaveChanged;
        GameEvents.OnShieldChargesChanged -= HandleShieldChargesChanged;
        GameEvents.OnPylonChargesChanged -= HandlePylonChargesChanged;
        GameEvents.OnTowerChargesChanged -= HandleTowerChargesChanged;
    }

    void Update()
    {
        if (!_timerRunning || !_hudVisible || _timerFrozen)
        {
            return;
        }

        _elapsed += Time.deltaTime;
        RefreshTimer();
    }

    public void ShowHUD()
    {
        _hudVisible = true;
        _timerRunning = true;
        SyncScoreLabel();
        SetHUDVisible(true);
    }

    public void ShowLearningBreakBadge(bool show)
    {
        if (LearningBreakBadge != null)
        {
            StartCoroutine(CoFadeBadge(show));
        }
    }

    void HandleEnemyDestroyed()
    {
        SessionScore++;
        SyncScoreLabel();
    }

    void HandleWaveChanged(int wave)
    {
        if (WaveCounter != null)
        {
            WaveCounter.text = $"{wave}/{EnemySpawner.TotalWaves}";
        }

        if (_hudVisible)
        {
            StartCoroutine(CoShowWaveAnnouncement(wave));
        }
    }

    void SyncScoreLabel()
    {
        _score = SessionScore;
        if (ScoreCounter != null)
        {
            ScoreCounter.text = _score.ToString();
        }
    }

    void HandleShieldChargesChanged(int count)
    {
        RefreshShieldButton();
    }

    void HandlePylonChargesChanged(int count)
    {
        RefreshPylonButton();
    }

    void HandleTowerChargesChanged(int count)
    {
        RefreshTowerButton();
    }

    public void RefreshPowerupButtons()
    {
        RefreshShieldButton();
        RefreshPylonButton();
        RefreshTowerButton();
    }

    public void RefreshShieldButton()
    {
        int charges = PowerupManager.Instance != null ? PowerupManager.Instance.GetCharges() : 0;
        bool placementMode = PowerupManager.Instance != null && PowerupManager.Instance.ShieldPlacementMode;

        if (_shieldButton == null)
        {
            return;
        }

        if (placementMode)
        {
            ConfigurePowerupButton(_shieldButton, ShieldGreen, charges, false, true, false);
            return;
        }

        if (charges > 0)
        {
            ConfigurePowerupButton(_shieldButton, ShieldGreen, charges, true, true, false);
        }
        else
        {
            ConfigurePowerupButton(_shieldButton, ShieldGreen, 0, false, false, true);
        }
    }

    public void RefreshPylonButton()
    {
        int charges = PowerupManager.Instance != null ? PowerupManager.Instance.GetPylonCharges() : 0;
        var manager = PylonNetworkManager.Instance;
        bool isBusy = manager != null && manager.CurrentState != PylonNetworkManager.PlacementState.Idle;
        bool isActive = manager != null && manager.CurrentState == PylonNetworkManager.PlacementState.Active;
        bool isPlacing = manager != null
            && (manager.CurrentState == PylonNetworkManager.PlacementState.PlacingPylon1
                || manager.CurrentState == PylonNetworkManager.PlacementState.PlacingPylon2);

        if (_pylonButton == null)
        {
            return;
        }

        if (isPlacing)
        {
            ConfigurePowerupButton(_pylonButton, Cyan, charges, false, true, false);
            return;
        }

        if (isActive)
        {
            ConfigurePowerupButton(_pylonButton, Cyan, charges, false, true, false);
            return;
        }

        if (charges > 0 && !isBusy)
        {
            ConfigurePowerupButton(_pylonButton, Cyan, charges, true, true, false);
        }
        else
        {
            ConfigurePowerupButton(_pylonButton, Cyan, charges, false, false, true);
        }
    }

    public void RefreshTowerButton()
    {
        int charges = PowerupManager.Instance != null ? PowerupManager.Instance.GetTowerCharges() : 0;
        bool isActive = PowerupManager.Instance != null && PowerupManager.Instance.isTowerActive;
        bool isPlacing = AttackTowerPowerup.Instance != null && AttackTowerPowerup.Instance.IsPlacingTower;

        if (_towerButton == null)
        {
            return;
        }

        if (isPlacing)
        {
            ConfigurePowerupButton(_towerButton, Gold, charges, false, true, false);
            return;
        }

        if (isActive)
        {
            ConfigurePowerupButton(_towerButton, Gold, charges, false, true, false);
            return;
        }

        if (charges > 0)
        {
            ConfigurePowerupButton(_towerButton, Gold, charges, true, true, false);
        }
        else
        {
            ConfigurePowerupButton(_towerButton, Gold, 0, false, false, true);
        }
    }

    public void OnScanSurfacePressed()
    {
        var planeManager = FindFirstObjectByType<ARPlaneManager>();
        if (planeManager == null)
        {
            return;
        }

        planeManager.enabled = true;
        foreach (var plane in planeManager.trackables)
        {
            plane.gameObject.SetActive(true);
        }
    }

    void OnShieldPowerupPressed()
    {
        if (PowerupManager.Instance == null || PowerupManager.Instance.GetCharges() <= 0)
        {
            return;
        }

        PowerupManager.Instance.ShieldPlacementMode = true;
        Time.timeScale = 0f;

        if (_shieldInstructionBanner != null)
        {
            _shieldInstructionBanner.SetActive(true);
        }

        RefreshShieldButton();
        StartCoroutine(CoWatchShieldPlacement());
        StartCoroutine(CoPunchScaleButton(_shieldButton.Root.transform));
    }

    void OnPylonPowerupPressed()
    {
        if (PylonNetworkManager.Instance == null)
        {
            return;
        }

        PylonNetworkManager.Instance.ActivatePlacementMode();
        if (_pylonButton != null)
        {
            StartCoroutine(CoPunchScaleButton(_pylonButton.Root.transform));
        }
    }

    void OnTowerPowerupPressed()
    {
        if (AttackTowerPowerup.Instance == null)
        {
            return;
        }

        AttackTowerPowerup.Instance.ActivatePlacementMode();
        if (_towerButton != null)
        {
            StartCoroutine(CoPunchScaleButton(_towerButton.Root.transform));
        }
    }

    void RefreshTimer()
    {
        if (PlayTimer == null)
        {
            return;
        }

        int mins = Mathf.FloorToInt(_elapsed / 60f);
        int secs = Mathf.FloorToInt(_elapsed % 60f);
        PlayTimer.text = $"{mins:00}:{secs:00}";
    }

    void SetHUDVisible(bool show)
    {
        if (_canvas != null)
        {
            _canvas.gameObject.SetActive(show);
        }
    }

    IEnumerator CoShowWaveAnnouncement(int wave)
    {
        if (WaveAnnouncementOverlay == null)
        {
            yield break;
        }

        if (_waveBigNumber != null)
        {
            _waveBigNumber.text = wave.ToString();
        }

        if (_waveFlavorText != null)
        {
            int index = Mathf.Clamp(wave - 1, 0, WaveFlavors.Length - 1);
            _waveFlavorText.text = WaveFlavors[index];
        }

        var overlayImage = WaveAnnouncementOverlay.GetComponent<Image>();
        if (overlayImage != null)
        {
            overlayImage.color = new Color(0f, 0f, 0f, 0.70f);
        }

        SetAnnouncementAlpha(1f);
        if (_waveBigNumber != null)
        {
            _waveBigNumber.transform.localScale = Vector3.zero;
        }

        _timerFrozen = true;
        WaveAnnouncementOverlay.SetActive(true);

        if (_waveBigNumber != null)
        {
            yield return StartCoroutine(CoPunchScale(_waveBigNumber.transform, 0f, 1f, 0.35f));
        }

        yield return new WaitForSecondsRealtime(2.1f);

        float elapsed = 0f;
        while (elapsed < 0.4f)
        {
            elapsed += Time.unscaledDeltaTime;
            float alpha = 1f - (elapsed / 0.4f);
            if (overlayImage != null)
            {
                overlayImage.color = new Color(0f, 0f, 0f, 0.70f * alpha);
            }

            SetAnnouncementAlpha(alpha);
            yield return null;
        }

        WaveAnnouncementOverlay.SetActive(false);
        SetAnnouncementAlpha(1f);
        _timerFrozen = false;
    }

    void SetAnnouncementAlpha(float alpha)
    {
        SetTMPAlpha(_waveIncomingLine, alpha);
        SetTMPAlpha(_waveBigNumber, alpha);
        SetTMPAlpha(_waveFlavorText, alpha);
    }

    IEnumerator CoFadeBadge(bool show)
    {
        if (LearningBreakBadge == null)
        {
            yield break;
        }

        var image = LearningBreakBadge.GetComponent<Image>();
        var labels = LearningBreakBadge.GetComponentsInChildren<TextMeshProUGUI>();

        if (show)
        {
            LearningBreakBadge.SetActive(true);
        }

        float from = show ? 0f : 1f;
        float to = show ? 1f : 0f;
        float elapsed = 0f;

        while (elapsed < 0.2f)
        {
            elapsed += Time.unscaledDeltaTime;
            float alpha = Mathf.Lerp(from, to, elapsed / 0.2f);
            if (image != null)
            {
                image.color = new Color(Gold.r, Gold.g, Gold.b, alpha);
            }

            foreach (var label in labels)
            {
                SetTMPAlpha(label, alpha);
            }

            yield return null;
        }

        if (!show)
        {
            LearningBreakBadge.SetActive(false);
        }
    }

    IEnumerator CoPunchScale(Transform target, float from, float to, float duration)
    {
        float peak = to * 1.18f;
        float half = duration * 0.5f;
        float elapsed = 0f;

        while (elapsed < half)
        {
            elapsed += Time.unscaledDeltaTime;
            target.localScale = Vector3.one * Mathf.Lerp(from, peak, elapsed / half);
            yield return null;
        }

        elapsed = 0f;
        while (elapsed < half)
        {
            elapsed += Time.unscaledDeltaTime;
            target.localScale = Vector3.one * Mathf.Lerp(peak, to, elapsed / half);
            yield return null;
        }

        target.localScale = Vector3.one * to;
    }

    IEnumerator CoWatchShieldPlacement()
    {
        while (PowerupManager.Instance != null && PowerupManager.Instance.ShieldPlacementMode)
        {
            yield return null;
        }

        Time.timeScale = 1f;
        if (_shieldInstructionBanner != null)
        {
            _shieldInstructionBanner.SetActive(false);
        }

        RefreshShieldButton();
    }

    IEnumerator CoPunchScaleButton(Transform target)
    {
        float half = 0.075f;
        float elapsed = 0f;
        while (elapsed < half)
        {
            elapsed += Time.unscaledDeltaTime;
            target.localScale = Vector3.one * Mathf.Lerp(1f, 0.95f, elapsed / half);
            yield return null;
        }

        elapsed = 0f;
        while (elapsed < half)
        {
            elapsed += Time.unscaledDeltaTime;
            target.localScale = Vector3.one * Mathf.Lerp(0.95f, 1f, elapsed / half);
            yield return null;
        }

        target.localScale = Vector3.one;
    }

    static void SetTMPAlpha(TextMeshProUGUI tmp, float alpha)
    {
        if (tmp == null)
        {
            return;
        }

        var color = tmp.color;
        tmp.color = new Color(color.r, color.g, color.b, alpha);
    }

    void BuildCanvas()
    {
        var canvasObject = new GameObject("GameHUD_Canvas");

        _canvas = canvasObject.AddComponent<Canvas>();
        _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        _canvas.sortingOrder = 10;

        var scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080f, 1920f);
        scaler.matchWidthOrHeight = 0.5f;

        canvasObject.AddComponent<GraphicRaycaster>();

        BuildTopBar(canvasObject.transform);
        BuildBottomBar(canvasObject.transform);
        BuildWaveAnnouncement(canvasObject.transform);
        BuildLearningBreakBadge(canvasObject.transform);
        BuildPauseOverlay(canvasObject.transform);
    }

    void BuildTopBar(Transform root)
    {
        var bar = Panel(root, "TopBar",
            new Vector2(0f, 1f), new Vector2(1f, 1f),
            new Vector2(0f, -120f), Vector2.zero, PanelBg);

        Img(bar, "BottomBorder",
            new Vector2(0f, 0f), new Vector2(1f, 0f),
            Vector2.zero, new Vector2(0f, 4f), Cyan);

        var waveCell = Cell(bar, "WaveCell", new Vector2(0.02f, 0.12f), new Vector2(0.28f, 0.88f));
        TMP(waveCell.transform, "WaveLabel", "WAVE", Cyan, 22f, TextAlignmentOptions.Center,
            new Vector2(0f, 0.58f), new Vector2(1f, 1f)).fontStyle = FontStyles.SmallCaps;
        WaveCounter = TMP(waveCell.transform, "WaveCounter", "0/10", Gold, 42f, TextAlignmentOptions.Center,
            new Vector2(0f, 0f), new Vector2(1f, 0.7f));
        WaveCounter.fontStyle = FontStyles.Bold;

        var scoreCell = Cell(bar, "ScoreCell", new Vector2(0.30f, 0.08f), new Vector2(0.70f, 0.92f));
        TMP(scoreCell.transform, "ScoreLabel", "SCORE", Cyan, 24f, TextAlignmentOptions.Center,
            new Vector2(0f, 0.58f), new Vector2(1f, 1f)).fontStyle = FontStyles.SmallCaps;
        ScoreCounter = TMP(scoreCell.transform, "ScoreCounter", "0", GreenAccent, 50f, TextAlignmentOptions.Center,
            new Vector2(0f, 0f), new Vector2(1f, 0.72f));
        ScoreCounter.fontStyle = FontStyles.Bold;

        var timeCell = Cell(bar, "TimeCell", new Vector2(0.72f, 0.12f), new Vector2(0.90f, 0.88f));
        TMP(timeCell.transform, "TimeLabel", "TIME", Cyan, 22f, TextAlignmentOptions.Center,
            new Vector2(0f, 0.58f), new Vector2(1f, 1f)).fontStyle = FontStyles.SmallCaps;
        PlayTimer = TMP(timeCell.transform, "PlayTimer", "00:00", White, 42f, TextAlignmentOptions.Center,
            new Vector2(0f, 0f), new Vector2(1f, 0.7f));
        PlayTimer.fontStyle = FontStyles.Bold;

        var pauseCell = Cell(bar, "PauseCell", new Vector2(0.915f, 0.18f), new Vector2(0.985f, 0.82f));
        _pauseButton = BuildActionButton(pauseCell.transform, "PauseButton", "II", PanelBg, White, OnPauseButtonPressed);
        var pauseBg = _pauseButton.GetComponent<Image>();
        if (pauseBg != null)
        {
            pauseBg.color = new Color(Navy.r, Navy.g, Navy.b, 0.9f);
        }
    }

    void BuildBottomBar(Transform root)
    {
        var bar = new GameObject("BottomBar");
        bar.transform.SetParent(root, false);
        var barRect = bar.AddComponent<RectTransform>();
        barRect.anchorMin = new Vector2(0f, 0f);
        barRect.anchorMax = new Vector2(1f, 0f);
        barRect.offsetMin = Vector2.zero;
        barRect.offsetMax = new Vector2(0f, 220f);

        var powerupDock = Cell(bar.transform, "PowerupDock", new Vector2(0.06f, 0.18f), new Vector2(0.78f, 0.9f));
        _shieldButton = BuildPowerupButton(powerupDock.transform, "ShieldPowerupButton",
            new Vector2(0f, 0.16f), new Vector2(0.31f, 0.84f), ShieldGreen, _shieldIconSprite, OnShieldPowerupPressed);
        _pylonButton = BuildPowerupButton(powerupDock.transform, "PylonPowerupButton",
            new Vector2(0.345f, 0.16f), new Vector2(0.655f, 0.84f), Cyan, _pylonIconSprite, OnPylonPowerupPressed);
        _towerButton = BuildPowerupButton(powerupDock.transform, "TowerPowerupButton",
            new Vector2(0.69f, 0.16f), new Vector2(1f, 0.84f), Gold, _towerIconSprite, OnTowerPowerupPressed);
        ScanSurfaceButton = null;

        _shieldInstructionBanner = new GameObject("ShieldInstructionBanner");
        _shieldInstructionBanner.transform.SetParent(root, false);
        var instructionRect = _shieldInstructionBanner.AddComponent<RectTransform>();
        instructionRect.anchorMin = new Vector2(0f, 1f);
        instructionRect.anchorMax = new Vector2(1f, 1f);
        instructionRect.pivot = new Vector2(0.5f, 1f);
        instructionRect.anchoredPosition = new Vector2(0f, -124f);
        instructionRect.sizeDelta = new Vector2(0f, 48f);

        var instructionBg = _shieldInstructionBanner.AddComponent<Image>();
        instructionBg.color = new Color(ShieldGreen.r, ShieldGreen.g, ShieldGreen.b, 0.92f);

        var instructionText = TMP(_shieldInstructionBanner.transform, "ShieldInstructionText",
            "TAP A LANDMARK TO SHIELD IT",
            Navy, 24f, TextAlignmentOptions.Center,
            new Vector2(0.04f, 0.1f), new Vector2(0.96f, 0.9f));
        instructionText.fontStyle = FontStyles.Bold;
        instructionText.raycastTarget = false;
        _shieldInstructionBanner.SetActive(false);
    }

    PowerupButtonRefs BuildPowerupButton(Transform parent, string name,
        Vector2 anchorMin, Vector2 anchorMax, Color fillColor, Sprite iconSprite, UnityEngine.Events.UnityAction callback)
    {
        var slot = new GameObject(name);
        slot.transform.SetParent(parent, false);
        var slotRect = slot.AddComponent<RectTransform>();
        slotRect.anchorMin = anchorMin;
        slotRect.anchorMax = anchorMax;
        slotRect.offsetMin = Vector2.zero;
        slotRect.offsetMax = Vector2.zero;

        var root = new GameObject("Face");
        root.transform.SetParent(slot.transform, false);
        var rootRect = root.AddComponent<RectTransform>();
        rootRect.anchorMin = new Vector2(0.5f, 0.5f);
        rootRect.anchorMax = new Vector2(0.5f, 0.5f);
        rootRect.pivot = new Vector2(0.5f, 0.5f);
        rootRect.sizeDelta = new Vector2(126f, 126f);
        rootRect.anchoredPosition = Vector2.zero;

        var background = root.AddComponent<Image>();
        background.sprite = MakeCircleSprite(256, Color.white);
        background.color = fillColor;
        background.preserveAspect = true;

        var button = root.AddComponent<Button>();
        button.targetGraphic = background;
        button.onClick.AddListener(callback);

        var colors = button.colors;
        colors.normalColor = fillColor;
        colors.highlightedColor = Color.Lerp(fillColor, Color.white, 0.18f);
        colors.pressedColor = Color.Lerp(fillColor, Color.black, 0.18f);
        colors.disabledColor = new Color(fillColor.r, fillColor.g, fillColor.b, 0.55f);
        colors.colorMultiplier = 1f;
        button.colors = colors;

        var iconObject = new GameObject("Icon");
        iconObject.transform.SetParent(root.transform, false);
        var iconRect = iconObject.AddComponent<RectTransform>();
        iconRect.anchorMin = new Vector2(0.2f, 0.2f);
        iconRect.anchorMax = new Vector2(0.8f, 0.8f);
        iconRect.offsetMin = Vector2.zero;
        iconRect.offsetMax = Vector2.zero;
        var iconImage = iconObject.AddComponent<Image>();
        iconImage.sprite = iconSprite;
        iconImage.color = ActiveIconTint;
        iconImage.preserveAspect = true;
        iconImage.raycastTarget = false;

        var badge = new GameObject("Badge");
        badge.transform.SetParent(root.transform, false);
        var badgeRect = badge.AddComponent<RectTransform>();
        badgeRect.anchorMin = new Vector2(0.68f, 0.64f);
        badgeRect.anchorMax = new Vector2(1.02f, 0.98f);
        badgeRect.offsetMin = Vector2.zero;
        badgeRect.offsetMax = Vector2.zero;

        var badgeBg = badge.AddComponent<Image>();
        badgeBg.color = Navy;
        badgeBg.sprite = MakeCircleSprite(128, Color.white);
        badgeBg.preserveAspect = true;

        var badgeText = TMP(badge.transform, "BadgeText", "0", fillColor, 24f, TextAlignmentOptions.Center,
            Vector2.zero, Vector2.one);
        badgeText.fontStyle = FontStyles.Bold;

        return new PowerupButtonRefs
        {
            Root = root,
            Button = button,
            Background = background,
            Icon = iconImage,
            BadgeBackground = badgeBg,
            BadgeText = badgeText
        };
    }

    void ApplyPowerupIcons()
    {
        AssignButtonIcon(_shieldButton, _shieldIconSprite);
        AssignButtonIcon(_pylonButton, _pylonIconSprite);
        AssignButtonIcon(_towerButton, _towerIconSprite);
    }

    Button BuildActionButton(Transform parent, string name, string label, Color backgroundColor, Color textColor, UnityEngine.Events.UnityAction callback)
    {
        var buttonObject = new GameObject(name);
        buttonObject.transform.SetParent(parent, false);

        var rect = buttonObject.AddComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        var image = buttonObject.AddComponent<Image>();
        image.color = backgroundColor;

        var button = buttonObject.AddComponent<Button>();
        button.targetGraphic = image;
        button.onClick.AddListener(callback);

        var colors = button.colors;
        colors.normalColor = image.color;
        colors.highlightedColor = Color.Lerp(image.color, Color.white, 0.15f);
        colors.pressedColor = Color.Lerp(image.color, Color.black, 0.12f);
        colors.disabledColor = new Color(image.color.r, image.color.g, image.color.b, 0.4f);
        colors.colorMultiplier = 1f;
        button.colors = colors;

        var labelText = TMP(buttonObject.transform, "Label", label, textColor, 24f, TextAlignmentOptions.Center, Vector2.zero, Vector2.one);
        labelText.fontStyle = FontStyles.Bold;
        labelText.raycastTarget = false;
        return button;
    }

    static void AssignButtonIcon(PowerupButtonRefs button, Sprite iconSprite)
    {
        if (button == null || button.Icon == null)
        {
            return;
        }

        button.Icon.sprite = iconSprite;
        button.Icon.enabled = iconSprite != null;
    }

    void ConfigurePowerupButton(PowerupButtonRefs button, Color accentColor, int count, bool interactable, bool showBadge, bool locked)
    {
        if (button == null || button.Root == null)
        {
            return;
        }

        button.Root.SetActive(true);
        button.Button.interactable = interactable;
        float backgroundAlpha = locked ? 0.28f : 1f;
        float iconAlpha = locked ? 0.38f : (interactable ? 1f : 0.72f);
        button.Background.color = new Color(accentColor.r, accentColor.g, accentColor.b, backgroundAlpha);
        if (button.Icon != null)
        {
            button.Icon.color = new Color(ActiveIconTint.r, ActiveIconTint.g, ActiveIconTint.b, iconAlpha);
        }

        var colors = button.Button.colors;
        colors.normalColor = button.Background.color;
        colors.highlightedColor = locked ? button.Background.color : Color.Lerp(button.Background.color, Color.white, 0.15f);
        colors.pressedColor = locked ? button.Background.color : Color.Lerp(button.Background.color, Color.black, 0.15f);
        colors.disabledColor = button.Background.color;
        button.Button.colors = colors;

        if (button.BadgeBackground != null)
        {
            button.BadgeBackground.gameObject.SetActive(showBadge && count > 0);
        }

        if (button.BadgeText != null)
        {
            button.BadgeText.text = Mathf.Max(0, count).ToString();
            button.BadgeText.color = accentColor;
            button.BadgeText.gameObject.SetActive(showBadge && count > 0);
        }
    }

    void BuildWaveAnnouncement(Transform root)
    {
        var overlay = new GameObject("WaveAnnouncementOverlay");
        overlay.transform.SetParent(root, false);
        var rect = overlay.AddComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        var background = overlay.AddComponent<Image>();
        background.color = new Color(0f, 0f, 0f, 0.70f);

        _waveIncomingLine = TMP(overlay.transform, "WaveIncoming", "WAVE INCOMING",
            Cyan, 28f, TextAlignmentOptions.Center,
            new Vector2(0.1f, 0.60f), new Vector2(0.9f, 0.68f));
        _waveIncomingLine.characterSpacing = 6f;

        _waveBigNumber = TMP(overlay.transform, "WaveBigNumber", "1",
            Gold, 96f, TextAlignmentOptions.Center,
            new Vector2(0.2f, 0.42f), new Vector2(0.8f, 0.62f));
        _waveBigNumber.fontStyle = FontStyles.Bold;
        _waveBigNumber.transform.localScale = Vector3.zero;

        _waveFlavorText = TMP(overlay.transform, "FlavorText", string.Empty,
            new Color(White.r, White.g, White.b, 0.80f), 26f, TextAlignmentOptions.Center,
            new Vector2(0.1f, 0.32f), new Vector2(0.9f, 0.42f));

        WaveAnnouncementOverlay = overlay;
        overlay.SetActive(false);
    }

    void BuildPauseOverlay(Transform root)
    {
        _pauseOverlay = new GameObject("PauseOverlay");
        _pauseOverlay.transform.SetParent(root, false);

        var rect = _pauseOverlay.AddComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        var bg = _pauseOverlay.AddComponent<Image>();
        bg.color = new Color(0f, 0f, 0f, 0.62f);

        var modal = Panel(_pauseOverlay.transform, "PauseModal",
            new Vector2(0.18f, 0.34f), new Vector2(0.82f, 0.66f),
            Vector2.zero, Vector2.zero, new Color(Navy.r, Navy.g, Navy.b, 0.96f));

        TMP(modal.transform, "PauseTitle", "PAUSED", White, 42f, TextAlignmentOptions.Center,
            new Vector2(0.15f, 0.72f), new Vector2(0.85f, 0.92f)).fontStyle = FontStyles.Bold;

        var resumeCell = Cell(modal.transform, "ResumeCell", new Vector2(0.17f, 0.42f), new Vector2(0.83f, 0.60f));
        var resumeButton = BuildActionButton(resumeCell.transform, "ResumeButton", "RESUME", Cyan, Navy, OnResumeButtonPressed);
        var resumeLabel = resumeButton.GetComponentInChildren<TextMeshProUGUI>();
        if (resumeLabel != null)
        {
            resumeLabel.fontSize = 28f;
        }

        var menuCell = Cell(modal.transform, "MainMenuCell", new Vector2(0.17f, 0.16f), new Vector2(0.83f, 0.34f));
        var menuButton = BuildActionButton(menuCell.transform, "PauseMainMenuButton", "MAIN MENU", Gold, Navy, OnPauseMainMenuPressed);
        var menuLabel = menuButton.GetComponentInChildren<TextMeshProUGUI>();
        if (menuLabel != null)
        {
            menuLabel.fontSize = 28f;
        }

        _pauseOverlay.SetActive(false);
    }

    void BuildLearningBreakBadge(Transform root)
    {
        var badge = new GameObject("LearningBreakBadge");
        badge.transform.SetParent(root, false);
        var rect = badge.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.2f, 1f);
        rect.anchorMax = new Vector2(0.8f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.anchoredPosition = new Vector2(0f, -124f);
        rect.sizeDelta = new Vector2(0f, 44f);
        var background = badge.AddComponent<Image>();
        background.color = new Color(Gold.r, Gold.g, Gold.b, 0f);

        var label = TMP(badge.transform, "BadgeLabel", "LEARNING BREAK",
            Navy, 18f, TextAlignmentOptions.Center, Vector2.zero, Vector2.one);
        label.fontStyle = FontStyles.Bold;

        LearningBreakBadge = badge;
        badge.SetActive(false);
    }

    void LoadPowerupIcons()
    {
        Sprite[] sprites = Resources.LoadAll<Sprite>("Sprite Assets/InlineIcons");
        _towerIconSprite = FindSpriteByName(sprites, "ebc_piratefortress");
        _pylonIconSprite = FindSpriteByName(sprites, "InlineIcons_158");
        _shieldIconSprite = FindSpriteByName(sprites, "InlineIcons_122");
    }

    void OnPauseButtonPressed()
    {
        if (_pauseMenuOpen)
        {
            ClosePauseMenu();
            return;
        }

        if (Time.timeScale <= 0f)
        {
            return;
        }

        OpenPauseMenu();
    }

    void OnResumeButtonPressed()
    {
        ClosePauseMenu();
    }

    void OnPauseMainMenuPressed()
    {
        _pauseMenuOpen = false;
        GameSessionFlow.PrepareForMainMenuReturn();
        SceneManager.LoadScene("Home");
    }

    void OpenPauseMenu()
    {
        _pauseMenuOpen = true;
        _timerFrozen = true;
        Time.timeScale = 0f;
        if (_pauseOverlay != null)
        {
            _pauseOverlay.SetActive(true);
        }
    }

    void ClosePauseMenu()
    {
        _pauseMenuOpen = false;
        _timerFrozen = false;
        Time.timeScale = 1f;
        if (_pauseOverlay != null)
        {
            _pauseOverlay.SetActive(false);
        }
    }

    static Sprite FindSpriteByName(Sprite[] sprites, string spriteName)
    {
        if (sprites == null)
        {
            return null;
        }

        foreach (Sprite sprite in sprites)
        {
            if (sprite != null && sprite.name == spriteName)
            {
                return sprite;
            }
        }

        return null;
    }

    void EnsureManager<T>(string objectName) where T : Component
    {
        if (FindFirstObjectByType<T>() != null)
        {
            return;
        }

        var managerObject = new GameObject(objectName);
        managerObject.AddComponent<T>();
    }

    static GameObject Panel(Transform parent, string name,
        Vector2 anchorMin, Vector2 anchorMax,
        Vector2 offsetMin, Vector2 offsetMax, Color color)
    {
        var obj = new GameObject(name);
        obj.transform.SetParent(parent, false);
        var rect = obj.AddComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = offsetMin;
        rect.offsetMax = offsetMax;
        obj.AddComponent<Image>().color = color;
        return obj;
    }

    static void Img(GameObject parent, string name,
        Vector2 anchorMin, Vector2 anchorMax,
        Vector2 offsetMin, Vector2 offsetMax, Color color)
    {
        var obj = new GameObject(name);
        obj.transform.SetParent(parent.transform, false);
        var rect = obj.AddComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = offsetMin;
        rect.offsetMax = offsetMax;
        obj.AddComponent<Image>().color = color;
    }

    static GameObject Cell(GameObject parent, string name, Vector2 anchorMin, Vector2 anchorMax)
    {
        return Cell(parent.transform, name, anchorMin, anchorMax);
    }

    static GameObject Cell(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax)
    {
        var obj = new GameObject(name);
        obj.transform.SetParent(parent, false);
        var rect = obj.AddComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        return obj;
    }

    static TextMeshProUGUI TMP(GameObject parent, string name, string text,
        Color color, float fontSize, TextAlignmentOptions alignment,
        Vector2 anchorMin, Vector2 anchorMax)
    {
        return TMP(parent.transform, name, text, color, fontSize, alignment, anchorMin, anchorMax);
    }

    static TextMeshProUGUI TMP(Transform parent, string name, string text,
        Color color, float fontSize, TextAlignmentOptions alignment,
        Vector2 anchorMin, Vector2 anchorMax)
    {
        var obj = new GameObject(name);
        obj.transform.SetParent(parent, false);
        var rect = obj.AddComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        var tmp = obj.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.color = color;
        tmp.fontSize = fontSize;
        tmp.alignment = alignment;
        tmp.fontStyle = FontStyles.Bold;
        tmp.enableAutoSizing = false;
        tmp.overflowMode = TextOverflowModes.Overflow;
        return tmp;
    }

    static Sprite MakeCircleSprite(int size, Color color)
    {
        var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        var pixels = new Color32[size * size];
        float centerX = size * 0.5f;
        float centerY = size * 0.5f;
        float radius = size * 0.5f - 1f;
        byte red = (byte)(color.r * 255);
        byte green = (byte)(color.g * 255);
        byte blue = (byte)(color.b * 255);

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = x - centerX;
                float dy = y - centerY;
                float distance = Mathf.Sqrt(dx * dx + dy * dy);
                float alpha = Mathf.Clamp01(radius - distance + 1f);
                pixels[y * size + x] = new Color32(red, green, blue, (byte)(alpha * 255));
            }
        }

        texture.SetPixels32(pixels);
        texture.Apply();
        return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
    }
}
