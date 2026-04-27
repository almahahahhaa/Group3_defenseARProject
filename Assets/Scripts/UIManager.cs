using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class UIManager : MonoBehaviour
{
    const string HomeSceneName = "Home";
    const string GameSceneName = "ARScene";
    const string GameBootstrapSceneName = "GameBootstrap";
    const string OilSlickSpritePath = "Oil_Slick_Image_Cropped";
    [Header("Panels")]
    public GameObject settingsPanel;
    public GameObject creditsPanel;
    public GameObject helpPanel;
    public GameObject enemyDirectoryPanel;

    [Header("Settings Buttons")]
    public Button musicButton;
    public Button musicToggleButton;
    public GameObject musicOnVisual;
    public GameObject musicOffVisual;
    public Button soundButton;
    public Button soundToggleButton;
    public GameObject soundOnVisual;
    public GameObject soundOffVisual;
    public GameObject cameraButton;

    [Header("Audio")]
    public AudioClip backgroundMusic;
    public AudioClip buttonClickSound;
    public AudioSource musicSource;
    public AudioSource sfxSource;

    bool m_IsInitializing;

    void Awake()
    {
        AutoWireSceneReferences();
        EnsureAudioSources();
    }

    void Start()
    {
        m_IsInitializing = true;

        SetupEnemyDirectory();
        CloseAllPanels();
        HideCameraSetting();
        BindCoreButtons();

        bool musicOn = AudioSettingsStore.IsMusicEnabled();
        bool soundOn = AudioSettingsStore.IsSoundEnabled();

        ApplyMusic(musicOn);
        ApplySound(soundOn);
        UpdateSettingVisuals(musicOn, soundOn);

        m_IsInitializing = false;
    }

    void AutoWireSceneReferences()
    {
        if (settingsPanel == null) settingsPanel = FindChildGameObject("Settings");
        if (creditsPanel == null) creditsPanel = FindChildGameObject("Credits");
        if (helpPanel == null) helpPanel = FindChildGameObject("Help");
        if (enemyDirectoryPanel == null) enemyDirectoryPanel = FindChildGameObject("EnemyDirectory");

        if (musicButton == null) musicButton = FindChildComponent<Button>("Settings/Popup/Buttons/Music_Button");
        if (musicToggleButton == null) musicToggleButton = FindChildComponent<Button>("Settings/Popup/Buttons/Music_Button/Toggle");
        if (musicOnVisual == null) musicOnVisual = FindChildGameObject("Settings/Popup/Buttons/Music_Button/Toggle/ON");
        if (musicOffVisual == null) musicOffVisual = FindChildGameObject("Settings/Popup/Buttons/Music_Button/Toggle/OFF");

        if (soundButton == null) soundButton = FindChildComponent<Button>("Settings/Popup/Buttons/Sounds_Button (2)");
        if (soundToggleButton == null) soundToggleButton = FindChildComponent<Button>("Settings/Popup/Buttons/Sounds_Button (2)/Toggle");
        if (soundOnVisual == null) soundOnVisual = FindChildGameObject("Settings/Popup/Buttons/Sounds_Button (2)/Toggle/ON");
        if (soundOffVisual == null) soundOffVisual = FindChildGameObject("Settings/Popup/Buttons/Sounds_Button (2)/Toggle/OFF");

        if (cameraButton == null) cameraButton = FindChildGameObject("Settings/Popup/Buttons/Camera_Button");
    }

    void EnsureAudioSources()
    {
        if (musicSource == null)
        {
            musicSource = gameObject.AddComponent<AudioSource>();
        }

        if (sfxSource == null)
        {
            sfxSource = gameObject.AddComponent<AudioSource>();
        }

        musicSource.playOnAwake = false;
        musicSource.loop = true;
        musicSource.spatialBlend = 0f;
        musicSource.clip = backgroundMusic;

        sfxSource.playOnAwake = false;
        sfxSource.loop = false;
        sfxSource.spatialBlend = 0f;
    }

    void CloseAllPanels()
    {
        if (settingsPanel != null) settingsPanel.SetActive(false);
        if (creditsPanel != null) creditsPanel.SetActive(false);
        if (helpPanel != null) helpPanel.SetActive(false);
        if (enemyDirectoryPanel != null) enemyDirectoryPanel.SetActive(false);
    }

    void BindCoreButtons()
    {
        AddButtonHandler("Home/Play_Button", OnPlayButtonPressed, true);
        AddButtonHandler("Home/Settings", OpenSettings, true);
        AddButtonHandler("Home/Credits", OpenCredits, true);
        AddButtonHandler("Home/Help", OpenHelp, true);
        AddButtonHandler("Home/EnemyDirectory", OpenEnemyDirectory, true);
        AddButtonHandler("Home/Coins", PlayButtonSound, true);

        AddButtonHandler("Settings/Popup/Back", CloseSettings, true);
        AddButtonHandler("Credits/Popup/Back", CloseCredits, true);
        AddButtonHandler("Help/Popup/Back", CloseHelp, true);
        AddButtonHandler("EnemyDirectory/Popup/Back", CloseEnemyDirectory, true);

        BindSettingToggle(musicButton, ToggleMusicSetting);
        BindSettingToggle(musicToggleButton, ToggleMusicSetting);
        BindSettingToggle(soundButton, ToggleSoundSetting);
        BindSettingToggle(soundToggleButton, ToggleSoundSetting);
    }

    void BindSettingToggle(Button button, UnityAction toggleAction)
    {
        if (button == null)
        {
            return;
        }

        button.onClick.RemoveListener(toggleAction);
        button.onClick.AddListener(toggleAction);
    }

    void AddButtonHandler(string path, UnityAction action, bool includeClickSound)
    {
        Button button = FindChildComponent<Button>(path);
        if (button == null)
        {
            return;
        }

        if (includeClickSound)
        {
            button.onClick.RemoveListener(PlayButtonSound);
            button.onClick.AddListener(PlayButtonSound);
        }

        if (action != null && button.onClick.GetPersistentEventCount() == 0)
        {
            button.onClick.RemoveListener(action);
            button.onClick.AddListener(action);
        }
    }

    Transform FindChild(string path)
    {
        return transform.Find(path);
    }

    GameObject FindChildGameObject(string path)
    {
        Transform target = FindChild(path);
        return target != null ? target.gameObject : null;
    }

    T FindChildComponent<T>(string path) where T : Component
    {
        Transform target = FindChild(path);
        return target != null ? target.GetComponent<T>() : null;
    }

    void HideCameraSetting()
    {
        if (cameraButton != null)
        {
            cameraButton.SetActive(false);
        }
    }

    public void OnPlayButtonPressed()
    {
        GameSessionFlow.PrepareForGameplayStart();
        SceneManager.LoadScene(GameBootstrapSceneName);
    }

    public void OpenSettings()
    {
        if (settingsPanel != null) settingsPanel.SetActive(true);
    }

    public void CloseSettings()
    {
        if (settingsPanel != null) settingsPanel.SetActive(false);
    }

    public void OpenCredits()
    {
        if (creditsPanel != null) creditsPanel.SetActive(true);
    }

    public void CloseCredits()
    {
        if (creditsPanel != null) creditsPanel.SetActive(false);
    }

    public void OpenHelp()
    {
        if (helpPanel != null) helpPanel.SetActive(true);
    }

    public void CloseHelp()
    {
        if (helpPanel != null) helpPanel.SetActive(false);
    }

    public void OpenEnemyDirectory()
    {
        if (enemyDirectoryPanel != null) enemyDirectoryPanel.SetActive(true);
    }

    public void CloseEnemyDirectory()
    {
        if (enemyDirectoryPanel != null) enemyDirectoryPanel.SetActive(false);
    }

    void ToggleMusicSetting()
    {
        bool musicOn = !IsMusicEnabled();
        SetMusicEnabled(musicOn);
    }

    void ToggleSoundSetting()
    {
        bool wasSoundEnabled = IsSoundEnabled();
        bool soundOn = !wasSoundEnabled;

        if (wasSoundEnabled)
        {
            PlayButtonSound();
        }

        SetSoundEnabled(soundOn);

        if (!wasSoundEnabled)
        {
            PlayButtonSound();
        }
    }

    bool IsMusicEnabled()
    {
        return AudioSettingsStore.IsMusicEnabled();
    }

    bool IsSoundEnabled()
    {
        return AudioSettingsStore.IsSoundEnabled();
    }

    void SetMusicEnabled(bool isOn)
    {
        AudioSettingsStore.SetMusicEnabled(isOn);
        ApplyMusic(isOn);
        UpdateMusicVisual(isOn);
    }

    void SetSoundEnabled(bool isOn)
    {
        AudioSettingsStore.SetSoundEnabled(isOn);
        ApplySound(isOn);
        UpdateSoundVisual(isOn);
    }

    void ApplyMusic(bool isOn)
    {
        if (musicSource == null)
        {
            return;
        }

        musicSource.clip = backgroundMusic;
        musicSource.mute = !isOn || backgroundMusic == null;

        if (!isOn || backgroundMusic == null)
        {
            if (musicSource.isPlaying)
            {
                musicSource.Stop();
            }

            return;
        }

        if (musicSource.clip != null && !musicSource.isPlaying)
        {
            musicSource.Play();
        }
    }

    void ApplySound(bool isOn)
    {
        if (sfxSource != null)
        {
            sfxSource.mute = !isOn;
        }
    }

    void UpdateSettingVisuals(bool musicOn, bool soundOn)
    {
        UpdateMusicVisual(musicOn);
        UpdateSoundVisual(soundOn);
    }

    void UpdateMusicVisual(bool isOn)
    {
        SetToggleVisual(musicOnVisual, musicOffVisual, isOn);
    }

    void UpdateSoundVisual(bool isOn)
    {
        SetToggleVisual(soundOnVisual, soundOffVisual, isOn);
    }

    void SetToggleVisual(GameObject onVisual, GameObject offVisual, bool isOn)
    {
        if (onVisual != null)
        {
            onVisual.SetActive(isOn);
        }

        if (offVisual != null)
        {
            offVisual.SetActive(!isOn);
        }
    }

    public void PlayButtonSound()
    {
        if (m_IsInitializing || !IsSoundEnabled())
        {
            return;
        }

        if (sfxSource == null || buttonClickSound == null)
        {
            return;
        }

        sfxSource.PlayOneShot(buttonClickSound);
    }

    void SetupEnemyDirectory()
    {
        Transform popup = FindChild("EnemyDirectory/Popup");
        Transform contentRoot = FindChild("EnemyDirectory/Popup/Image");
        Transform topPanel = FindChild("EnemyDirectory/Popup/TopPanel");

        if (popup == null || contentRoot == null || topPanel == null)
        {
            return;
        }

        EnemyDirectoryNavigator navigator = popup.GetComponent<EnemyDirectoryNavigator>();
        if (navigator == null)
        {
            navigator = popup.gameObject.AddComponent<EnemyDirectoryNavigator>();
        }

        GameObject pollutionPage = EnsurePollutionCloudPage(contentRoot);
        if (pollutionPage == null)
        {
            return;
        }

        GameObject oilSlickPage = EnsureOilSlickPage(contentRoot, pollutionPage);
        if (oilSlickPage == null)
        {
            return;
        }

        Button prevButton = EnsureDirectoryNavButton(popup, "PrevButton", "<", new Vector2(-96f, 40f));
        Button nextButton = EnsureDirectoryNavButton(popup, "NextButton", ">", new Vector2(96f, 40f));
        Text pageIndicator = EnsureDirectoryPageIndicator(popup, new Vector2(0f, 40f));

        prevButton.onClick.RemoveListener(PlayButtonSound);
        prevButton.onClick.AddListener(PlayButtonSound);
        nextButton.onClick.RemoveListener(PlayButtonSound);
        nextButton.onClick.AddListener(PlayButtonSound);

        navigator.pages = new[] { pollutionPage, oilSlickPage };
        navigator.pageIndicator = null;
        navigator.pageIndicatorText = pageIndicator;
        navigator.prevButton = prevButton;
        navigator.nextButton = nextButton;

        prevButton.onClick.RemoveListener(navigator.Prev);
        prevButton.onClick.AddListener(navigator.Prev);
        nextButton.onClick.RemoveListener(navigator.Next);
        nextButton.onClick.AddListener(navigator.Next);

        UpdateDirectoryIndicator(pageIndicator, 0, navigator.pages.Length);
    }

    GameObject EnsurePollutionCloudPage(Transform contentRoot)
    {
        Transform existingPage = contentRoot.Find("Page1_PollutionCloud");
        if (existingPage != null)
        {
            return existingPage.gameObject;
        }

        GameObject page = new GameObject("Page1_PollutionCloud", typeof(RectTransform));
        RectTransform pageRect = page.GetComponent<RectTransform>();
        page.transform.SetParent(contentRoot, false);
        StretchRect(pageRect);

        Transform[] children = new Transform[contentRoot.childCount - 1];
        int index = 0;
        foreach (Transform child in contentRoot)
        {
            if (child == page.transform)
            {
                continue;
            }

            children[index++] = child;
        }

        for (int i = 0; i < index; i++)
        {
            children[i].SetParent(page.transform, false);
        }

        return page;
    }

    GameObject EnsureOilSlickPage(Transform contentRoot, GameObject pollutionPage)
    {
        Transform existingPage = contentRoot.Find("Page2_OilSlick");
        if (existingPage != null)
        {
            ConfigureOilSlickPage(existingPage.gameObject);
            return existingPage.gameObject;
        }

        GameObject page = Instantiate(pollutionPage, contentRoot);
        page.name = "Page2_OilSlick";
        page.SetActive(false);

        ConfigureOilSlickPage(page);
        return page;
    }

    void ConfigureOilSlickPage(GameObject page)
    {
        TextMeshProUGUI title = FindText(page.transform, "PollutionCloud");
        TextMeshProUGUI description = FindText(page.transform, "Description");
        TextMeshProUGUI damage = FindText(page.transform, "DamageLabel/Text (TMP)");
        TextMeshProUGUI destroy = FindText(page.transform, "DestroyLabel/Text (TMP)");
        TextMeshProUGUI speed = FindText(page.transform, "SpeedLabel/Text (TMP)");
        TextMeshProUGUI threatValue = FindText(page.transform, "Low");
        Slider threatSlider = page.transform.Find("Slider")?.GetComponent<Slider>();

        if (title != null)
        {
            title.text = "Oil Slick";
        }

        if (description != null)
        {
            description.text = "A slow-moving oil mass that deals 10 damage if it reaches a landmark. Tap it before impact and it splits into two Mini Oil Slicks, each dealing 5 damage.";
            description.fontSize = 27f;
        }

        if (speed != null)
        {
            speed.text = "Slow speed";
        }

        if (damage != null)
        {
            damage.text = "10 dmg / hit";
        }

        if (destroy != null)
        {
            destroy.text = "Tap splits it";
        }

        if (threatValue != null)
        {
            threatValue.text = "High";
        }

        if (threatSlider != null)
        {
            threatSlider.normalizedValue = 0.85f;
        }

        Transform imageFrame = page.transform.Find("Image");
        Image mainPortrait = imageFrame != null ? imageFrame.Find("Image")?.GetComponent<Image>() : null;
        Sprite oilSprite = LoadResourceSprite(OilSlickSpritePath);
        if (mainPortrait != null && oilSprite != null)
        {
            mainPortrait.sprite = oilSprite;
            mainPortrait.preserveAspect = true;
        }

        ConfigureOilSlickImageFrame(imageFrame);
    }

    void ConfigureOilSlickImageFrame(Transform imageFrame)
    {
        if (imageFrame == null)
        {
            return;
        }

        RemoveIfExists(imageFrame.parent, "MiniOilSlickBox");
    }

    Button EnsureDirectoryNavButton(Transform parent, string name, string label, Vector2 anchoredPosition)
    {
        Transform existing = parent.Find(name);
        Button button = existing != null ? existing.GetComponent<Button>() : null;
        if (button == null)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);

            button = go.GetComponent<Button>();
            button.targetGraphic = go.GetComponent<Image>();
        }

        RectTransform rect = button.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0f);
        rect.anchorMax = new Vector2(0.5f, 0f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = new Vector2(78f, 44f);

        Image image = button.GetComponent<Image>();
        image.color = new Color(0.21f, 0.16f, 0.46f, 1f);

        Transform labelTransform = button.transform.Find("Label");
        Text text = labelTransform != null ? labelTransform.GetComponent<Text>() : null;
        if (text == null)
        {
            GameObject labelObject = new GameObject("Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            labelObject.transform.SetParent(button.transform, false);
            RectTransform labelRect = labelObject.GetComponent<RectTransform>();
            StretchRect(labelRect);

            text = labelObject.GetComponent<Text>();
        }

        text.text = label;
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = 28;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = Color.white;

        button.transform.SetAsLastSibling();

        return button;
    }

    Text EnsureDirectoryPageIndicator(Transform parent, Vector2 anchoredPosition)
    {
        Transform existing = parent.Find("PageIndicator");
        Text indicator = existing != null ? existing.GetComponent<Text>() : null;
        if (indicator == null)
        {
            GameObject go = new GameObject("PageIndicator", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            go.transform.SetParent(parent, false);

            indicator = go.GetComponent<Text>();
        }

        RectTransform rect = indicator.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0f);
        rect.anchorMax = new Vector2(0.5f, 0f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = new Vector2(132f, 34f);

        indicator.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        indicator.fontSize = 22;
        indicator.alignment = TextAnchor.MiddleCenter;
        indicator.color = new Color(0.86f, 0.86f, 0.95f, 0.95f);

        indicator.transform.SetAsLastSibling();

        return indicator;
    }

    Sprite LoadResourceSprite(string resourcePath)
    {
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

        return Sprite.Create(
            texture,
            new Rect(0f, 0f, texture.width, texture.height),
            new Vector2(0.5f, 0.5f),
            100f);
    }

    void UpdateDirectoryIndicator(Text indicator, int currentIndex, int pageCount)
    {
        if (indicator != null)
        {
            indicator.text = string.Format("{0} / {1}", currentIndex + 1, pageCount);
        }
    }

    TextMeshProUGUI FindText(Transform root, string path)
    {
        Transform target = root.Find(path);
        return target != null ? target.GetComponent<TextMeshProUGUI>() : null;
    }

    void StretchRect(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = Vector2.zero;
    }

    void RemoveIfExists(Transform parent, string childName)
    {
        if (parent == null)
        {
            return;
        }

        Transform child = parent.Find(childName);
        if (child != null)
        {
            Destroy(child.gameObject);
        }
    }
}
