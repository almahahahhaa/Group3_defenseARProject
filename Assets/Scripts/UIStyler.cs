using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// Applies a cohesive dark tactical-AR theme to every UI panel at startup.
/// Attach this to the UI Canvas root (alongside FactCardSystem).
[DefaultExecutionOrder(-50)]
public class UIStyler : MonoBehaviour
{
    // ── Palette ───────────────────────────────────────────────────────────────
    static readonly Color BgPanel      = new Color(0.05f, 0.08f, 0.14f, 0.95f);
    static readonly Color BgPanelRed   = new Color(0.10f, 0.03f, 0.04f, 0.96f);
    static readonly Color BgHeader     = new Color(0.04f, 0.40f, 0.62f, 1.00f);
    static readonly Color BgHeaderGold = new Color(0.50f, 0.36f, 0.02f, 1.00f);
    static readonly Color BgStatBox    = new Color(0.08f, 0.14f, 0.26f, 1.00f);
    static readonly Color BgButton     = new Color(0.06f, 0.40f, 0.68f, 1.00f);
    static readonly Color BgButtonHov  = new Color(0.10f, 0.60f, 0.90f, 1.00f);
    static readonly Color BgButtonPrs  = new Color(0.02f, 0.28f, 0.50f, 1.00f);
    static readonly Color BgButtonAlt  = new Color(0.12f, 0.14f, 0.24f, 1.00f);
    static readonly Color BgButtonAltH = new Color(0.20f, 0.22f, 0.36f, 1.00f);
    static readonly Color BgOption     = new Color(0.07f, 0.16f, 0.30f, 1.00f);
    static readonly Color BgOptionHov  = new Color(0.10f, 0.40f, 0.70f, 1.00f);

    static readonly Color AccentCyan   = new Color(0.00f, 0.84f, 0.98f, 1.00f);
    static readonly Color AccentGold   = new Color(1.00f, 0.82f, 0.12f, 1.00f);
    static readonly Color AccentGreen  = new Color(0.14f, 0.84f, 0.42f, 1.00f);
    static readonly Color AccentRed    = new Color(1.00f, 0.25f, 0.25f, 1.00f);
    static readonly Color AccentAmber  = new Color(1.00f, 0.60f, 0.08f, 1.00f);

    static readonly Color TxtWhite     = new Color(0.95f, 0.98f, 1.00f, 1.00f);
    static readonly Color TxtSoft      = new Color(0.68f, 0.80f, 0.92f, 1.00f);
    static readonly Color Transparent  = new Color(0f, 0f, 0f, 0f);

    // ── Entry point ───────────────────────────────────────────────────────────
    void Awake() => StyleAll();

    void StyleAll()
    {
        EnsureCanvasScaler();
        StyleWaveHUD();
        StyleLandmarkPanel();
        StyleFactCard();
        StyleQuestionCard();
        StyleResultPanel();
        StylePowerupPanel();
        StyleWaveClearedPanel();
        StyleGameOverPanel();
    }

    // Ensures this canvas uses ScaleWithScreenSize so all panels scale
    // correctly across different device resolutions.
    void EnsureCanvasScaler()
    {
        var canvas = GetComponent<Canvas>() ?? GetComponentInParent<Canvas>();
        if (canvas == null) return;
        var scaler = canvas.GetComponent<CanvasScaler>();
        if (scaler == null) scaler = canvas.gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080f, 1920f);
        scaler.matchWidthOrHeight  = 0.5f;
    }

    // ── Helpers ───────────────────────────────────────────────────────────────
    Transform UI => transform;
    Transform Find(string path) => UI.Find(path);

    static void Img(Transform t, Color c)
    {
        if (!t) return;
        var i = t.GetComponent<Image>(); if (i) i.color = c;
    }

    static void Txt(Transform t, Color c, float sz = -1,
                    FontStyles style = FontStyles.Normal,
                    TextAlignmentOptions align = TextAlignmentOptions.Center,
                    bool autoSize = false)
    {
        if (!t) return;
        var tmp = t.GetComponent<TextMeshProUGUI>(); if (!tmp) return;
        tmp.color = c;
        if (sz > 0) tmp.fontSize = sz;
        tmp.fontStyle = style;
        tmp.alignment = align;
        tmp.enableAutoSizing = autoSize;
        if (autoSize) { tmp.fontSizeMin = 9; tmp.fontSizeMax = sz > 0 ? sz : 32; }
    }

    static void Btn(Transform t, Color normal, Color hover, Color press)
    {
        if (!t) return;
        var b = t.GetComponent<Button>(); if (!b) return;
        var cb = b.colors;
        cb.normalColor      = normal;
        cb.highlightedColor = hover;
        cb.pressedColor     = press;
        cb.selectedColor    = normal;
        cb.disabledColor    = new Color(normal.r, normal.g, normal.b, 0.35f);
        cb.fadeDuration     = 0.05f;
        cb.colorMultiplier  = 1f;
        b.colors = cb;
        Img(t, normal);
    }

    static void Anchor(Transform t, Vector2 min, Vector2 max, Vector2 offMin, Vector2 offMax)
    {
        if (!t) return;
        var rt = t.GetComponent<RectTransform>(); if (!rt) return;
        rt.anchorMin = min; rt.anchorMax = max;
        rt.offsetMin = offMin; rt.offsetMax = offMax;
    }

    static void BtnTxt(Transform btn, Color c, float sz, FontStyles style = FontStyles.Bold)
    {
        if (!btn) return;
        var tmp = btn.GetComponentInChildren<TextMeshProUGUI>(); if (!tmp) return;
        tmp.color = c; tmp.fontSize = sz; tmp.fontStyle = style;
        tmp.alignment = TextAlignmentOptions.Center;
    }

    // ── WaveHUD ───────────────────────────────────────────────────────────────
    void StyleWaveHUD()
    {
        var hud = Find("WaveHUD");
        if (!hud) return;

        // WaveHUD is a pure TMP object — style the text only (can't add Image).
        Txt(hud, AccentCyan, 22, FontStyles.Bold, TextAlignmentOptions.Center);

        Anchor(hud,
            new Vector2(0.18f, 0.90f), new Vector2(0.82f, 1.00f),
            Vector2.zero, new Vector2(0, -2));
    }

    // ── Tower Placement UI ────────────────────────────────────────────────────
    // Note: position/size of TowerPlacementUI is handled by GameHUD.BuildStrip().
    // ── Landmark Instruction Panel ────────────────────────────────────────────
    void StyleLandmarkPanel()
    {
        var panel = Find("LandmarkInstructionPanel");
        if (!panel) return;

        Img(panel, BgHeader);
        Anchor(panel,
            new Vector2(0.08f, 0.83f), new Vector2(0.92f, 0.95f),
            Vector2.zero, Vector2.zero);

        var txt = panel.Find("InstructionText");
        Txt(txt, TxtWhite, 34, FontStyles.Bold);
    }

    // ── Fact Card ─────────────────────────────────────────────────────────────
    void StyleFactCard()
    {
        var panel = Find("FactCardPanel");
        if (!panel) return;

        Img(panel, BgPanel);
        Anchor(panel,
            new Vector2(0.04f, 0.25f), new Vector2(0.96f, 0.75f),
            Vector2.zero, Vector2.zero);

        // "Did You Know?" header badge
        var tag = panel.Find("DidYouKnowTag");
        Img(tag, BgHeaderGold);
        Anchor(tag,
            new Vector2(0.05f, 0.86f), new Vector2(0.40f, 0.97f),
            Vector2.zero, Vector2.zero);
        if (tag)
        {
            var tagTmp = tag.GetComponentInChildren<TextMeshProUGUI>();
            if (tagTmp) { tagTmp.color = TxtWhite; tagTmp.fontStyle = FontStyles.Bold; tagTmp.fontSize = 24; tagTmp.alignment = TextAlignmentOptions.Center; }
        }

        // Fact body text
        var factTxt = panel.Find("FactText");
        Txt(factTxt, TxtWhite, 34, FontStyles.Normal,
            TextAlignmentOptions.Center, autoSize: true);
        Anchor(factTxt,
            new Vector2(0.10f, 0.22f), new Vector2(0.86f, 0.72f),
            Vector2.zero, Vector2.zero);

        var sdgImage = panel.Find("SDGGoalImage");
        if (sdgImage != null)
        {
            Anchor(sdgImage,
                new Vector2(1f, 1f), new Vector2(1f, 1f),
                new Vector2(-226f, -226f), new Vector2(-26f, -18f));
        }

        // Timer slider
        StyleSlider(panel.Find("FactTimerSlider"), AccentGold,
            new Color(0.06f, 0.10f, 0.20f, 1f));
    }

    // ── Question Card ─────────────────────────────────────────────────────────
    void StyleQuestionCard()
    {
        var panel = Find("QuestionCardPanel");
        if (!panel) return;

        Img(panel, BgPanel);
        Anchor(panel,
            new Vector2(0.03f, 0.12f), new Vector2(0.97f, 0.88f),
            Vector2.zero, Vector2.zero);

        // "Question" header badge
        var tag = panel.Find("QuestionTag");
        Img(tag, BgHeader);
        Anchor(tag,
            new Vector2(0.02f, 0.93f), new Vector2(0.30f, 0.985f),
            Vector2.zero, Vector2.zero);
        if (tag)
        {
            var tagTmp = tag.GetComponentInChildren<TextMeshProUGUI>();
            if (tagTmp) { tagTmp.color = TxtWhite; tagTmp.fontStyle = FontStyles.Bold; tagTmp.enableAutoSizing = true; tagTmp.fontSizeMin = 16; tagTmp.fontSizeMax = 24; tagTmp.alignment = TextAlignmentOptions.Center; }
        }

        var questionText = panel.Find("QuestionText");
        Txt(questionText, TxtWhite, 32, FontStyles.Bold,
            TextAlignmentOptions.Center, autoSize: true);
        Anchor(questionText,
            new Vector2(0.10f, 0.72f), new Vector2(0.90f, 0.89f),
            Vector2.zero, Vector2.zero);
        var questionTmp = questionText != null ? questionText.GetComponent<TextMeshProUGUI>() : null;
        if (questionTmp != null)
        {
            questionTmp.fontSizeMin = 22f;
            questionTmp.fontSizeMax = 32f;
        }

        var divider = panel.Find("Divider") ?? panel.Find("Divider (1)");
        Img(divider, AccentCyan);
        Anchor(divider,
            new Vector2(0.00f, 0.665f), new Vector2(1.00f, 0.665f),
            new Vector2(0f, 0f), new Vector2(0f, 3f));

        // Option buttons
        string[] opts   = { "OptionA", "OptionB", "OptionC", "OptionD" };
        string[] labels = { "A",       "B",       "C",       "D"       };
        Color[]  badge  = { new Color(0.70f, 0.14f, 0.14f, 1f),
                            new Color(0.14f, 0.48f, 0.18f, 1f),
                            new Color(0.14f, 0.28f, 0.70f, 1f),
                            new Color(0.50f, 0.26f, 0.02f, 1f) };
        float[] topAnchors = { 0.62f, 0.49f, 0.36f, 0.23f };
        const float rowHeight = 0.095f;

        for (int i = 0; i < opts.Length; i++)
        {
            var opt = panel.Find(opts[i]);
            if (!opt) continue;

            Btn(opt, BgOption, BgOptionHov, BgButtonPrs);
            Anchor(opt,
                new Vector2(0.09f, topAnchors[i] - rowHeight), new Vector2(0.91f, topAnchors[i]),
                Vector2.zero, Vector2.zero);

            // Letter badge (child Image named "A", "B", etc.)
            var badgeObj = opt.Find(labels[i]);
            Img(badgeObj, badge[i]);
            if (badgeObj)
            {
                Anchor(badgeObj,
                    new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                    new Vector2(12f, -28f), new Vector2(68f, 28f));

                var badgeTmp = badgeObj.GetComponentInChildren<TextMeshProUGUI>();
                if (badgeTmp) { badgeTmp.color = TxtWhite; badgeTmp.fontStyle = FontStyles.Bold; badgeTmp.fontSize = 28; badgeTmp.alignment = TextAlignmentOptions.Center; }
            }

            // Option answer text
            var optTxt = opt.Find($"Option{labels[i]}Text");
            Txt(optTxt, TxtWhite, 30, FontStyles.Normal,
                TextAlignmentOptions.Left, autoSize: true);
            Anchor(optTxt,
                new Vector2(0f, 0f), new Vector2(1f, 1f),
                new Vector2(76f, 10f), new Vector2(-18f, -10f));
            var optionTmp = optTxt != null ? optTxt.GetComponent<TextMeshProUGUI>() : null;
            if (optionTmp != null)
            {
                optionTmp.fontSizeMin = 20f;
                optionTmp.fontSizeMax = 30f;
                optionTmp.textWrappingMode = TextWrappingModes.Normal;
            }
        }

        // Timer slider
        var timer = panel.Find("QuestionTimerSlider");
        Anchor(timer,
            new Vector2(0.02f, 0.02f), new Vector2(0.98f, 0.05f),
            Vector2.zero, Vector2.zero);
        StyleSlider(timer, AccentCyan,
            new Color(0.06f, 0.10f, 0.20f, 1f));
    }

    // ── Result Panel ──────────────────────────────────────────────────────────
    void StyleResultPanel()
    {
        var panel = Find("ResultPanel");
        if (!panel) return;

        Img(panel, BgPanel);
        Anchor(panel,
            new Vector2(0.08f, 0.38f), new Vector2(0.92f, 0.62f),
            Vector2.zero, Vector2.zero);

        Txt(panel.Find("ResultText"), TxtWhite, 34, FontStyles.Bold);
    }

    // ── Powerup Panel ─────────────────────────────────────────────────────────
    void StylePowerupPanel()
    {
        var panel = Find("PowerupPanel");
        if (!panel) return;

        Img(panel, new Color(0.02f, 0.32f, 0.52f, 0.96f));
        Anchor(panel,
            new Vector2(0.06f, 0.76f), new Vector2(0.94f, 0.90f),
            Vector2.zero, Vector2.zero);

        Txt(panel.Find("PowerupText"), AccentCyan, 30, FontStyles.Bold);
    }

    // ── Wave Cleared Panel ────────────────────────────────────────────────────
    void StyleWaveClearedPanel()
    {
        var panel = Find("WaveClearedPanel");
        if (!panel) return;

        Img(panel, BgPanel);

        // Check mark icon ring
        var check = panel.Find("Check");
        Img(check, AccentGreen);
        Img(check?.Find("Image"), new Color(0.08f, 0.55f, 0.28f, 1f));

        // Title
        Txt(panel.Find("WaveCleared"), AccentGold, 44, FontStyles.Bold,
            TextAlignmentOptions.Center, autoSize: true);

        // Subtitle
        var subtitle = panel.Find("Text (TMP)");
        Txt(subtitle, TxtSoft, 34, autoSize: true);

        // Divider
        Img(panel.Find("Divider (1)"), AccentCyan);

        // Stats row background
        var statsRow = panel.Find("StatsRow");
        Img(statsRow, new Color(0.04f, 0.08f, 0.18f, 0.80f));

        StyleStatBox(statsRow?.Find("EnemiesBox"), AccentAmber);
        StyleStatBox(statsRow?.Find("HPBox"),      AccentGreen);

        // Wave credits box
        var wave = panel.Find("Wave");
        Img(wave, BgStatBox);
        Txt(wave?.Find("WaveLabel"), TxtSoft,   28, FontStyles.Normal);
        Txt(wave?.Find("WaveText"),  AccentCyan, 32, FontStyles.Bold);

        // Next Wave button
        var nextBtn = panel.Find("NextWaveButton");
        Btn(nextBtn, BgButton, BgButtonHov, BgButtonPrs);
        BtnTxt(nextBtn, TxtWhite, 30);

        // Main Menu button
        var menuBtn = panel.Find("MainMenuButton");
        Btn(menuBtn, BgButtonAlt, BgButtonAltH,
            new Color(0.06f, 0.07f, 0.14f, 1f));
        BtnTxt(menuBtn, TxtSoft, 26, FontStyles.Normal);
    }

    // ── Game Over Panel ───────────────────────────────────────────────────────
    void StyleGameOverPanel()
    {
        var panel = Find("GameOverPanel");
        if (!panel) return;

        Img(panel, BgPanelRed);

        // Cross icon ring
        var cross = panel.Find("Cross");
        Img(cross, AccentRed);
        Img(cross?.Find("Image"), new Color(0.60f, 0.08f, 0.08f, 1f));

        // Title
        Txt(panel.Find("WaveCleared"), AccentRed, 44, FontStyles.Bold,
            TextAlignmentOptions.Center, autoSize: true);

        // Subtitle
        Txt(panel.Find("Text (TMP)"), TxtSoft, 34, autoSize: true);

        // Divider
        Img(panel.Find("Divider (1)"), AccentRed);

        // Stats row
        var statsRow = panel.Find("StatsRow");
        Img(statsRow, new Color(0.15f, 0.04f, 0.04f, 0.80f));

        StyleStatBox(statsRow?.Find("WavesBox"),   AccentAmber);
        StyleStatBox(statsRow?.Find("EnemiesBox"), AccentRed);

        // Health / waves summary box
        var health = panel.Find("Health");
        Img(health, new Color(0.18f, 0.05f, 0.05f, 0.90f));
        Txt(health?.Find("WaveLabel"), TxtSoft,  28, FontStyles.Normal);
        Txt(health?.Find("WaveText"),  AccentRed, 32, FontStyles.Bold);

        // Try Again button
        var tryBtn = panel.Find("NextWaveButton");
        Btn(tryBtn, AccentRed,
            new Color(1.0f, 0.45f, 0.45f, 1f),
            new Color(0.60f, 0.10f, 0.10f, 1f));
        BtnTxt(tryBtn, TxtWhite, 30);

        // Main Menu button
        var menuBtn = panel.Find("MainMenuButton");
        Btn(menuBtn,
            new Color(0.16f, 0.06f, 0.06f, 1f),
            new Color(0.26f, 0.10f, 0.10f, 1f),
            new Color(0.08f, 0.03f, 0.03f, 1f));
        BtnTxt(menuBtn, TxtSoft, 26, FontStyles.Normal);
    }

    // ── Shared helpers ────────────────────────────────────────────────────────
    static void StyleStatBox(Transform box, Color valueColor)
    {
        if (!box) return;
        Img(box, BgStatBox);
        // Find label & value by component order (label first, value second)
        var tmps = box.GetComponentsInChildren<TextMeshProUGUI>(true);
        if (tmps.Length > 0) { tmps[0].color = TxtSoft; tmps[0].fontSize = 30; tmps[0].fontStyle = FontStyles.Normal; tmps[0].enableAutoSizing = false; }
        if (tmps.Length > 1) { tmps[1].color = valueColor; tmps[1].fontSize = 32; tmps[1].fontStyle = FontStyles.Bold; tmps[1].enableAutoSizing = false; }
    }

    static void StyleSlider(Transform sliderT, Color fillColor, Color bgColor)
    {
        if (!sliderT) return;
        var bg   = sliderT.Find("Background");
        var fill = sliderT.Find("Fill Area/Fill") ?? sliderT.Find("Fill Area");
        Img(bg,   bgColor);
        Img(fill, fillColor);

        // Hide the handle if it exists
        var handle = sliderT.Find("Handle Slide Area/Handle") ?? sliderT.Find("Handle Slide Area");
        Img(handle, Transparent);
    }
}
