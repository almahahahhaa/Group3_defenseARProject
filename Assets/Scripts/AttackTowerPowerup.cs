using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

[DefaultExecutionOrder(-61)]
public class AttackTowerPowerup : MonoBehaviour
{
    public enum PlacementState
    {
        Idle,
        PlacingTower,
        Active
    }

    const float ActiveDuration = 12f;
    static readonly Color AmberGold = new Color(1f, 0.72156864f, 0f, 1f);
    static readonly Color DarkNavy = new Color(0.051f, 0.051f, 0.169f, 1f);

    public static AttackTowerPowerup Instance;

    public PlacementState CurrentState => _state;
    public bool IsPlacingTower => _state == PlacementState.PlacingTower;
    public bool IsTowerActive => _state == PlacementState.Active;
    public float TimeRemaining { get; private set; }

    private PlacementState _state = PlacementState.Idle;
    private Camera _arCamera;
    private Transform _battlefieldRoot;
    private Renderer _battlefieldRenderer;
    private GameObject _towerInstance;
    private Material[] _runtimeMaterials;
    private GameObject _instructionBanner;
    private TextMeshProUGUI _instructionText;
    private GameObject _countdownBadge;
    private TextMeshProUGUI _countdownText;
    private Coroutine _countdownCoroutine;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }

        Instance = this;
    }

    void Start()
    {
        _arCamera = Camera.main;
        _battlefieldRoot = ResolveBattlefieldRoot();
        CacheBattlefieldRenderer();
    }

    void Update()
    {
        if (_arCamera == null)
        {
            _arCamera = Camera.main;
        }

        if (_battlefieldRoot == null)
        {
            _battlefieldRoot = ResolveBattlefieldRoot();
            CacheBattlefieldRenderer();
        }

        if (_state == PlacementState.PlacingTower)
        {
            HandlePlacementTap();
        }
    }

    public void ActivatePlacementMode()
    {
        if (_state != PlacementState.Idle)
        {
            return;
        }

        if (PowerupManager.Instance == null || !PowerupManager.Instance.ConsumeTowerCharge())
        {
            return;
        }

        _state = PlacementState.PlacingTower;
        Time.timeScale = 0f;

        if (_instructionBanner == null)
        {
            BuildInstructionBanner();
        }

        ShowInstructionBanner("TAP BATTLEFIELD TO PLACE TOWER");
        HUDManager.Instance?.RefreshTowerButton();
    }

    public void PlaceTower(Vector3 position)
    {
        if (_state != PlacementState.PlacingTower)
        {
            return;
        }

        GameObject prefab = TowerPlacer.Instance != null ? TowerPlacer.Instance.defenseTowerPrefab : null;
        if (prefab == null)
        {
            return;
        }

        _towerInstance = Instantiate(prefab, position, Quaternion.identity);
        if (_battlefieldRoot != null)
        {
            _towerInstance.transform.SetParent(_battlefieldRoot, true);
        }

        SnapObjectToBattlefield(_towerInstance, position);

        ApplyTowerMaterial(_towerInstance);
        SpawnPlacementBurst(position);
        PowerupAudioManager.Instance?.PlaySpawn(position);
        StartCoroutine(SpawnPunchCoroutine(_towerInstance.transform, _towerInstance.transform.localScale));

        HideInstructionBanner();
        Time.timeScale = 1f;
        _state = PlacementState.Active;
        TimeRemaining = ActiveDuration;

        if (PowerupManager.Instance != null)
        {
            PowerupManager.Instance.isTowerActive = true;
        }

        GameEvents.TowerPlaced();
        HUDManager.Instance?.RefreshTowerButton();

        if (_countdownCoroutine != null)
        {
            StopCoroutine(_countdownCoroutine);
        }

        _countdownCoroutine = StartCoroutine(CountdownCoroutine());
    }

    public void DeactivateTower()
    {
        bool wasPlacing = _state == PlacementState.PlacingTower;

        if (_countdownCoroutine != null)
        {
            StopCoroutine(_countdownCoroutine);
            _countdownCoroutine = null;
        }

        if (wasPlacing)
        {
            Time.timeScale = 1f;
        }

        HideInstructionBanner();
        HideCountdownBadge();

        if (_towerInstance != null)
        {
            SpawnExpiryBurst(_towerInstance.transform.position);
            StartCoroutine(FadeOutCoroutine());
            return;
        }

        FinalizeTowerState();
    }

    void HandlePlacementTap()
    {
        Vector2 screenPosition = default;
        bool hasTap = false;

        var touchscreen = Touchscreen.current;
        if (touchscreen != null && touchscreen.primaryTouch.press.wasPressedThisFrame)
        {
            screenPosition = touchscreen.primaryTouch.position.ReadValue();
            hasTap = true;
        }

        if (!hasTap && Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            screenPosition = Mouse.current.position.ReadValue();
            hasTap = true;
        }

        if (!hasTap || _arCamera == null)
        {
            return;
        }

        EnemyTapDestroyer.LastTapFrame = Time.frameCount;

        Ray ray = _arCamera.ScreenPointToRay(screenPosition);
        if (!TryGetBattlefieldHit(ray, out RaycastHit hit))
        {
            return;
        }

        PlaceTower(hit.point);
    }

    bool TryGetBattlefieldHit(Ray ray, out RaycastHit battlefieldHit)
    {
        battlefieldHit = default;
        RaycastHit[] hits = Physics.RaycastAll(ray, 100f);
        float closestDistance = float.MaxValue;

        foreach (RaycastHit hit in hits)
        {
            if (!IsBattlefieldTransform(hit.collider.transform))
            {
                continue;
            }

            if (hit.distance < closestDistance)
            {
                closestDistance = hit.distance;
                battlefieldHit = hit;
            }
        }

        if (closestDistance < float.MaxValue)
        {
            return true;
        }

        return TryGetPlanePlacementHit(ray, out battlefieldHit);
    }

    bool TryGetPlanePlacementHit(Ray ray, out RaycastHit battlefieldHit)
    {
        battlefieldHit = default;

        ResolvePlacementPlane(out Vector3 planeNormal, out Vector3 planePoint);

        Plane floor = new Plane(planeNormal, planePoint);
        if (!floor.Raycast(ray, out float enter))
        {
            return false;
        }

        Vector3 point = ray.GetPoint(enter);

        battlefieldHit.point = point;
        battlefieldHit.distance = enter;
        return true;
    }

    bool IsBattlefieldTransform(Transform candidate)
    {
        Transform current = candidate;
        while (current != null)
        {
            if (current.name == "BattlefieldPlatform")
            {
                return true;
            }

            current = current.parent;
        }

        return false;
    }

    Transform ResolveBattlefieldRoot()
    {
        if (EnemySpawner.Instance != null && EnemySpawner.Instance.transform.parent != null)
        {
            return EnemySpawner.Instance.transform.parent;
        }

        GameObject battlefield = GameObject.Find("BattlefieldPlatform");
        return battlefield != null ? battlefield.transform : null;
    }

    void CacheBattlefieldRenderer()
    {
        if (_battlefieldRoot == null)
        {
            _battlefieldRenderer = null;
            return;
        }

        _battlefieldRenderer = _battlefieldRoot.GetComponent<Renderer>();
        if (_battlefieldRenderer == null)
        {
            _battlefieldRenderer = _battlefieldRoot.GetComponentInChildren<Renderer>();
        }
    }

    void ResolvePlacementPlane(out Vector3 planeNormal, out Vector3 planePoint)
    {
        planeNormal = _battlefieldRoot != null ? _battlefieldRoot.up : Vector3.up;
        planePoint = _battlefieldRoot != null ? _battlefieldRoot.position : Vector3.zero;

        if (LandmarkManager.Instance == null)
        {
            return;
        }

        var landmarks = LandmarkManager.Instance.GetAllLandmarks();
        if (landmarks == null || landmarks.Count == 0)
        {
            return;
        }

        Vector3 sum = Vector3.zero;
        int count = 0;
        foreach (GameObject landmark in landmarks)
        {
            if (landmark == null)
            {
                continue;
            }

            sum += landmark.transform.position;
            count++;
        }

        if (count > 0)
        {
            planePoint = sum / count;
        }
    }

    void SnapObjectToBattlefield(GameObject instance, Vector3 placementPoint)
    {
        if (instance == null)
        {
            return;
        }

        Vector3 surfaceNormal = _battlefieldRoot != null ? _battlefieldRoot.up : Vector3.up;
        Renderer[] renderers = instance.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0)
        {
            instance.transform.position = placementPoint;
            return;
        }

        float minProjection = float.MaxValue;
        foreach (Renderer renderer in renderers)
        {
            Bounds bounds = renderer.bounds;
            foreach (Vector3 corner in GetBoundsCorners(bounds))
            {
                float projection = Vector3.Dot(surfaceNormal, corner);
                if (projection < minProjection)
                {
                    minProjection = projection;
                }
            }
        }

        float targetProjection = Vector3.Dot(surfaceNormal, placementPoint) + 0.0025f;
        float delta = targetProjection - minProjection;
        instance.transform.position += surfaceNormal * delta;
    }

    static Vector3[] GetBoundsCorners(Bounds bounds)
    {
        Vector3 min = bounds.min;
        Vector3 max = bounds.max;

        return new[]
        {
            new Vector3(min.x, min.y, min.z),
            new Vector3(min.x, min.y, max.z),
            new Vector3(min.x, max.y, min.z),
            new Vector3(min.x, max.y, max.z),
            new Vector3(max.x, min.y, min.z),
            new Vector3(max.x, min.y, max.z),
            new Vector3(max.x, max.y, min.z),
            new Vector3(max.x, max.y, max.z)
        };
    }

    void ApplyTowerMaterial(GameObject tower)
    {
        Renderer[] renderers = tower.GetComponentsInChildren<Renderer>(true);
        _runtimeMaterials = new Material[renderers.Length];

        for (int i = 0; i < renderers.Length; i++)
        {
            Material source = renderers[i].sharedMaterial;
            Material runtimeMaterial;

            if (source != null)
            {
                runtimeMaterial = new Material(source);
            }
            else
            {
                Shader shader = Shader.Find("Universal Render Pipeline/Lit");
                if (shader == null)
                {
                    shader = Shader.Find("Standard");
                }

                runtimeMaterial = new Material(shader);
            }

            renderers[i].material = runtimeMaterial;
            _runtimeMaterials[i] = runtimeMaterial;
        }
    }

    IEnumerator CountdownCoroutine()
    {
        BuildCountdownBadge();

        while (TimeRemaining > 0f)
        {
            TimeRemaining -= Time.deltaTime;
            if (_countdownText != null)
            {
                _countdownText.text = $"ATTACK TOWER {Mathf.CeilToInt(Mathf.Max(0f, TimeRemaining))}s";
            }

            yield return null;
        }

        DeactivateTower();
    }

    public IEnumerator SpawnPunchCoroutine(Transform target, Vector3 finalScale)
    {
        target.localScale = Vector3.zero;
        float elapsed = 0f;
        const float duration = 0.3f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            target.localScale = finalScale * Mathf.Clamp01(elapsed / duration);
            yield return null;
        }

        target.localScale = finalScale;
    }

    public IEnumerator FadeOutCoroutine()
    {
        float elapsed = 0f;
        const float duration = 0.4f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float intensity = Mathf.Lerp(0.5f, 0f, elapsed / duration);
            Color emission = AmberGold * intensity;

            if (_runtimeMaterials != null)
            {
                foreach (Material material in _runtimeMaterials)
                {
                    if (material == null)
                    {
                        continue;
                    }

                    if (material.HasProperty("_EmissionColor"))
                    {
                        material.SetColor("_EmissionColor", emission);
                    }

                    if (material.HasProperty("_EmissiveColor"))
                    {
                        material.SetColor("_EmissiveColor", emission);
                    }
                }
            }

            yield return null;
        }

        if (_towerInstance != null)
        {
            Destroy(_towerInstance);
            _towerInstance = null;
        }

        FinalizeTowerState();
    }

    void FinalizeTowerState()
    {
        HideInstructionBanner();
        HideCountdownBadge();
        TimeRemaining = 0f;
        _runtimeMaterials = null;
        _state = PlacementState.Idle;

        if (PowerupManager.Instance != null)
        {
            PowerupManager.Instance.isTowerActive = false;
        }

        HUDManager.Instance?.RefreshTowerButton();
    }

    void BuildInstructionBanner()
    {
        Canvas canvas = FindHUDCanvas();
        if (canvas == null)
        {
            return;
        }

        _instructionBanner = new GameObject("TowerInstructionBanner");
        _instructionBanner.transform.SetParent(canvas.transform, false);

        RectTransform rect = _instructionBanner.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.anchoredPosition = new Vector2(0f, -124f);
        rect.sizeDelta = new Vector2(0f, 48f);

        Image background = _instructionBanner.AddComponent<Image>();
        background.color = AmberGold;

        GameObject textObject = new GameObject("Text");
        textObject.transform.SetParent(_instructionBanner.transform, false);

        RectTransform textRect = textObject.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(8f, 2f);
        textRect.offsetMax = new Vector2(-8f, -2f);

        _instructionText = textObject.AddComponent<TextMeshProUGUI>();
        _instructionText.fontSize = 24f;
        _instructionText.color = DarkNavy;
        _instructionText.alignment = TextAlignmentOptions.Center;
        _instructionText.fontStyle = FontStyles.Bold;

        _instructionBanner.SetActive(false);
    }

    void ShowInstructionBanner(string text)
    {
        if (_instructionBanner == null)
        {
            BuildInstructionBanner();
        }

        if (_instructionText != null)
        {
            _instructionText.text = text;
        }

        if (_instructionBanner != null)
        {
            _instructionBanner.SetActive(true);
        }
    }

    void HideInstructionBanner()
    {
        if (_instructionBanner != null)
        {
            _instructionBanner.SetActive(false);
        }
    }

    void BuildCountdownBadge()
    {
        HideCountdownBadge();

        Canvas canvas = FindHUDCanvas();
        if (canvas == null)
        {
            return;
        }

        _countdownBadge = new GameObject("TowerCountdownBadge");
        _countdownBadge.transform.SetParent(canvas.transform, false);

        RectTransform rect = _countdownBadge.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.28f, 1f);
        rect.anchorMax = new Vector2(0.72f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.anchoredPosition = new Vector2(0f, -174f);
        rect.sizeDelta = new Vector2(0f, 40f);

        Image background = _countdownBadge.AddComponent<Image>();
        background.color = new Color(AmberGold.r, AmberGold.g, AmberGold.b, 0.9f);

        GameObject textObject = new GameObject("Text");
        textObject.transform.SetParent(_countdownBadge.transform, false);

        RectTransform textRect = textObject.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(4f, 2f);
        textRect.offsetMax = new Vector2(-4f, -2f);

        _countdownText = textObject.AddComponent<TextMeshProUGUI>();
        _countdownText.fontSize = 20f;
        _countdownText.color = DarkNavy;
        _countdownText.alignment = TextAlignmentOptions.Center;
        _countdownText.fontStyle = FontStyles.Bold;
        _countdownText.text = "ATTACK TOWER 12s";
    }

    void HideCountdownBadge()
    {
        if (_countdownBadge != null)
        {
            Destroy(_countdownBadge);
            _countdownBadge = null;
            _countdownText = null;
        }
    }

    Canvas FindHUDCanvas()
    {
        foreach (Canvas canvas in FindObjectsByType<Canvas>(FindObjectsSortMode.None))
        {
            if (canvas.renderMode == RenderMode.ScreenSpaceOverlay && canvas.sortingOrder == 10)
            {
                return canvas;
            }
        }

        foreach (Canvas canvas in FindObjectsByType<Canvas>(FindObjectsSortMode.None))
        {
            if (canvas.renderMode == RenderMode.ScreenSpaceOverlay)
            {
                return canvas;
            }
        }

        return null;
    }

    void SpawnPlacementBurst(Vector3 position)
    {
        GameObject particleObject = new GameObject("TowerPlaceBurst");
        particleObject.transform.position = position;

        ParticleSystem particles = particleObject.AddComponent<ParticleSystem>();
        var main = particles.main;
        main.startColor = AmberGold;
        main.startSize = 0.025f;
        main.startLifetime = 0.6f;
        main.startSpeed = 0.4f;
        main.loop = false;
        main.playOnAwake = false;
        main.simulationSpace = ParticleSystemSimulationSpace.World;

        var emission = particles.emission;
        emission.enabled = false;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 25) });
        emission.enabled = true;

        var shape = particles.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = 0.3f;

        particles.GetComponent<ParticleSystemRenderer>().material = ParticleMaterial();
        particles.Play();
        Destroy(particleObject, 1.5f);
    }

    void SpawnExpiryBurst(Vector3 position)
    {
        GameObject particleObject = new GameObject("TowerExpiryBurst");
        particleObject.transform.position = position;

        ParticleSystem particles = particleObject.AddComponent<ParticleSystem>();
        var main = particles.main;
        main.startColor = Color.white;
        main.startSize = 0.03f;
        main.startLifetime = 0.8f;
        main.startSpeed = 0.3f;
        main.loop = false;
        main.playOnAwake = false;

        var emission = particles.emission;
        emission.enabled = false;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 15) });
        emission.enabled = true;

        var shape = particles.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.1f;

        var colorOverLifetime = particles.colorOverLifetime;
        colorOverLifetime.enabled = true;
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
            new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) });
        colorOverLifetime.color = gradient;

        particles.GetComponent<ParticleSystemRenderer>().material = ParticleMaterial();
        particles.Play();
        Destroy(particleObject, 2f);
    }

    static Material ParticleMaterial()
    {
        Shader shader = Shader.Find("Particles/Standard Unlit")
            ?? Shader.Find("Legacy Shaders/Particles/Additive")
            ?? Shader.Find("Sprites/Default");
        return shader != null ? new Material(shader) : new Material(Shader.Find("Standard"));
    }
}
