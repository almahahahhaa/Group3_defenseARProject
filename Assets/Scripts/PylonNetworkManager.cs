using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using TMPro;

[DefaultExecutionOrder(-60)]
public class PylonNetworkManager : MonoBehaviour
{
    const string BeamTipAnchorName = "Affector";
    const float TargetPylonHeight = 0.16f;

    public static PylonNetworkManager Instance;

    public enum PlacementState { Idle, PlacingPylon1, PlacingPylon2, Active }

    // ── Inspector ─────────────────────────────────────────────────────────────
    public GameObject pylonPrefab;

    // ── State ─────────────────────────────────────────────────────────────────
    public PlacementState CurrentState => _state;
    public bool           isActive     => _state == PlacementState.Active;
    public float          timeRemaining { get; private set; }
    public GameObject     Pylon1        { get; private set; }
    public GameObject     Pylon2        { get; private set; }

    private PlacementState _state = PlacementState.Idle;

    // ── Beam ──────────────────────────────────────────────────────────────────
    private LineRenderer _primaryBeam;
    private LineRenderer _secondaryArc;
    private Material     _beamMat;
    private Material     _arcMat;

    // ── Coroutines ────────────────────────────────────────────────────────────
    private Coroutine _beamPulseCoroutine;
    private Coroutine _arcJitterCoroutine;
    private Coroutine _countdownCoroutine;

    // ── UI ────────────────────────────────────────────────────────────────────
    private GameObject     _instructionBanner;
    private TextMeshProUGUI _instructionText;
    private GameObject     _countdownBadge;
    private TextMeshProUGUI _countdownText;

    // ── Camera ────────────────────────────────────────────────────────────────
    private Camera _arCamera;
    private Transform _battlefieldRoot;
    private Renderer _battlefieldRenderer;

    // ── Colors ────────────────────────────────────────────────────────────────
    static readonly Color ElectricCyan = new Color(0f,     1f,     0.898f, 1f);
    static readonly Color DarkNavy     = new Color(0.051f, 0.051f, 0.169f, 1f);

    // ═════════════════════════════════════════════════════════════════════════
    // LIFECYCLE
    // ═════════════════════════════════════════════════════════════════════════

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(this); return; }
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
        if (_arCamera == null) _arCamera = Camera.main;
        if (_battlefieldRoot == null)
        {
            _battlefieldRoot = ResolveBattlefieldRoot();
            CacheBattlefieldRenderer();
        }

        if (_state == PlacementState.PlacingPylon1 || _state == PlacementState.PlacingPylon2)
            HandlePlacementTap();

        if (_state == PlacementState.Active && Pylon1 != null && Pylon2 != null)
            CheckBeamCollisions();
    }

    // ═════════════════════════════════════════════════════════════════════════
    // PUBLIC API
    // ═════════════════════════════════════════════════════════════════════════

    public void ActivatePlacementMode()
    {
        if (_state != PlacementState.Idle) return;
        if (PowerupManager.Instance == null || !PowerupManager.Instance.ConsumePylonCharge()) return;

        _state = PlacementState.PlacingPylon1;
        Time.timeScale = 0f;

        if (_instructionBanner == null) BuildInstructionBanner();
        ShowInstructionBanner("TAP BATTLEFIELD TO PLACE PYLON 1");

        HUDManager.Instance?.RefreshPylonButton();
    }

    public void PlacePylon(Vector3 position)
    {
        if (_state == PlacementState.PlacingPylon1)
        {
            Pylon1 = SpawnPylon(position);
            _state = PlacementState.PlacingPylon2;
            ShowInstructionBanner("TAP BATTLEFIELD TO PLACE PYLON 2");
        }
        else if (_state == PlacementState.PlacingPylon2)
        {
            Pylon2 = SpawnPylon(position);
            HideInstructionBanner();
            Time.timeScale = 1f;
            _state = PlacementState.Active;
            ActivateBeam();
            _countdownCoroutine = StartCoroutine(CountdownCoroutine());
            HUDManager.Instance?.RefreshPylonButton();
        }
    }

    public void DeactivateNetwork()
    {
        if (_state == PlacementState.PlacingPylon1 || _state == PlacementState.PlacingPylon2)
            Time.timeScale = 1f;

        StopAllBeamCoroutines();
        if (_countdownCoroutine != null) { StopCoroutine(_countdownCoroutine); _countdownCoroutine = null; }

        HideInstructionBanner();
        HideCountdownBadge();
        DestroyBeam();
        DestroyPylons();

        _state = PlacementState.Idle;
        HUDManager.Instance?.RefreshPylonButton();
    }

    // ═════════════════════════════════════════════════════════════════════════
    // TAP HANDLING
    // ═════════════════════════════════════════════════════════════════════════

    void HandlePlacementTap()
    {
        Vector2 screenPos = default;
        bool    hasTap    = false;

        var ts = Touchscreen.current;
        if (ts != null && ts.primaryTouch.press.wasPressedThisFrame)
        {
            screenPos = ts.primaryTouch.position.ReadValue();
            hasTap    = true;
        }

        if (!hasTap && Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            screenPos = Mouse.current.position.ReadValue();
            hasTap    = true;
        }

        if (!hasTap) return;

        // Claim this tap so EnemyTapDestroyer and XRObjectTapDestroyer skip it
        EnemyTapDestroyer.LastTapFrame = Time.frameCount;

        if (_arCamera == null) return;

        Ray ray = _arCamera.ScreenPointToRay(screenPos);
        ResolvePlacementPlane(out Vector3 planeNormal, out Vector3 planePoint);
        var floor = new Plane(planeNormal, planePoint);

        if (!floor.Raycast(ray, out float enter)) return;

        Vector3 placementPoint = ray.GetPoint(enter);
        PlacePylon(placementPoint);
    }

    // ═════════════════════════════════════════════════════════════════════════
    // PYLON SPAWNING
    // ═════════════════════════════════════════════════════════════════════════

    GameObject SpawnPylon(Vector3 position)
    {
        GameObject pylonGO;
        bool usedFallback = false;

        if (pylonPrefab != null)
        {
            pylonGO = Instantiate(pylonPrefab, position, Quaternion.identity);
            StripPylonGameplayComponents(pylonGO);
        }
        else
        {
            // Fallback cylinder when no prefab is assigned
            pylonGO = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            Destroy(pylonGO.GetComponent<Collider>());
            pylonGO.transform.position   = position;
            pylonGO.transform.localScale = new Vector3(0.03f, 0.08f, 0.03f);
            usedFallback = true;
        }

        // Parent to battlefield so it moves with the AR surface
        if (EnemySpawner.Instance != null && EnemySpawner.Instance.transform.parent != null)
            pylonGO.transform.SetParent(EnemySpawner.Instance.transform.parent);

        if (usedFallback)
            ApplyFallbackPylonMaterial(pylonGO);
        else
            FitPylonToBattlefield(pylonGO, position);

        Vector3 naturalScale = pylonGO.transform.localScale;
        SpawnPlacementBurst(position);
        PowerupAudioManager.Instance?.PlaySpawn(position);
        StartCoroutine(ScalePunchCoroutine(pylonGO.transform, naturalScale));

        return pylonGO;
    }

    void StripPylonGameplayComponents(GameObject pylonGO)
    {
        foreach (var currencyAffector in pylonGO.GetComponentsInChildren<TowerDefense.Affectors.CurrencyAffector>(true))
        {
            currencyAffector.enabled = false;
            Destroy(currencyAffector);
        }
    }

    void ApplyFallbackPylonMaterial(GameObject pylonGO)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("Standard");
        if (shader == null) return;

        var mat = new Material(shader);

        // Opaque URP/Lit settings
        mat.SetFloat("_Surface",   0f);
        mat.SetFloat("_Blend",     0f);
        mat.SetFloat("_AlphaClip", 0f);

        Color cyan = ElectricCyan;
        mat.color = cyan;
        mat.SetColor("_BaseColor", cyan);

        mat.EnableKeyword("_EMISSION");
        Color emColor = cyan * 0.6f;
        mat.SetColor("_EmissionColor", emColor);
        if (mat.HasProperty("_EmissiveColor")) mat.SetColor("_EmissiveColor", emColor);

        foreach (var r in pylonGO.GetComponentsInChildren<Renderer>())
            r.material = mat;
    }

    IEnumerator ScalePunchCoroutine(Transform t, Vector3 targetScale)
    {
        t.localScale = Vector3.zero;
        float elapsed  = 0f;
        float duration = 0.3f;

        while (elapsed < duration)
        {
            elapsed     += Time.unscaledDeltaTime;
            t.localScale = targetScale * Mathf.Clamp01(elapsed / duration);
            yield return null;
        }
        t.localScale = targetScale;
    }

    // ═════════════════════════════════════════════════════════════════════════
    // ELECTRIC ARC BEAM
    // ═════════════════════════════════════════════════════════════════════════

    void ActivateBeam()
    {
        // Root for both LineRenderers — parented to the battlefield
        var beamRoot = new GameObject("PylonBeamRoot");
        if (EnemySpawner.Instance != null && EnemySpawner.Instance.transform.parent != null)
            beamRoot.transform.SetParent(EnemySpawner.Instance.transform.parent);

        // Zigzag arc beam
        var arcGO = new GameObject("PylonArcBeam");
        arcGO.transform.SetParent(beamRoot.transform, false);
        _secondaryArc = arcGO.AddComponent<LineRenderer>();
        _secondaryArc.positionCount = 10;
        SetupLineRenderer(_secondaryArc, 0.03f, out _arcMat, centerBright: true);

        _arcJitterCoroutine = StartCoroutine(SecondaryArcJitterCoroutine());
    }

    void SetupLineRenderer(LineRenderer lr, float width, out Material mat, bool centerBright)
    {
        lr.useWorldSpace  = false;
        lr.positionCount  = 2;
        lr.startWidth     = width;
        lr.endWidth       = width;

        Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        if (shader == null) shader = Shader.Find("Sprites/Default");
        if (shader == null) shader = Shader.Find("Unlit/Color");

        mat = new Material(shader);
        mat.color = ElectricCyan;
        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", ElectricCyan);
        lr.material = mat;

        var grad = new Gradient();
        if (centerBright)
        {
            grad.SetKeys(
                new[] { new GradientColorKey(ElectricCyan, 0f), new GradientColorKey(ElectricCyan, 1f) },
                new[] { new GradientAlphaKey(0.4f, 0f), new GradientAlphaKey(1f, 0.5f), new GradientAlphaKey(0.4f, 1f) }
            );
        }
        else
        {
            grad.SetKeys(
                new[] { new GradientColorKey(ElectricCyan, 0f), new GradientColorKey(ElectricCyan, 1f) },
                new[] { new GradientAlphaKey(0.3f, 0f), new GradientAlphaKey(0.5f, 0.5f), new GradientAlphaKey(0.3f, 1f) }
            );
        }
        lr.colorGradient = grad;
    }

    void UpdateBeamPositions()
    {
        if (_secondaryArc == null || Pylon1 == null || Pylon2 == null) return;

        Vector3 p1w = GetPylonBeamOrigin(Pylon1);
        Vector3 p2w = GetPylonBeamOrigin(Pylon2);

        _secondaryArc.SetPosition(0, _secondaryArc.transform.InverseTransformPoint(p1w));
        _secondaryArc.SetPosition(_secondaryArc.positionCount - 1, _secondaryArc.transform.InverseTransformPoint(p2w));
    }

    Vector3 GetPylonBeamOrigin(GameObject pylon)
    {
        Transform anchor = FindChildRecursive(pylon.transform, BeamTipAnchorName);
        if (anchor != null)
            return anchor.position;

        float topY = GetPylonTopY(pylon);
        return new Vector3(pylon.transform.position.x, topY, pylon.transform.position.z);
    }

    Transform FindChildRecursive(Transform root, string childName)
    {
        if (root.name == childName)
            return root;

        for (int i = 0; i < root.childCount; i++)
        {
            Transform match = FindChildRecursive(root.GetChild(i), childName);
            if (match != null)
                return match;
        }

        return null;
    }

    float GetPylonTopY(GameObject pylon)
    {
        float maxY = pylon.transform.position.y;
        foreach (var r in pylon.GetComponentsInChildren<Renderer>())
            maxY = Mathf.Max(maxY, r.bounds.max.y);
        return maxY;
    }

    IEnumerator BeamPulseCoroutine()
    {
        while (_secondaryArc != null)
        {
            // Use realtimeSinceStartup so pulse works even if timeScale changes
            float sin   = Mathf.Sin(Time.realtimeSinceStartup * Mathf.PI * 2f / 0.6f);
            float t     = (sin + 1f) * 0.5f;
            float width = Mathf.Lerp(0.02f, 0.045f, t);
            float alpha = Mathf.Lerp(0.6f,  1.0f,  t);

            _secondaryArc.startWidth = width;
            _secondaryArc.endWidth   = width;

            if (_arcMat != null)
            {
                Color c = new Color(ElectricCyan.r, ElectricCyan.g, ElectricCyan.b, alpha);
                _arcMat.color = c;
                if (_arcMat.HasProperty("_BaseColor")) _arcMat.SetColor("_BaseColor", c);
            }

            yield return null;
        }
    }

    Transform ResolveBattlefieldRoot()
    {
        if (EnemySpawner.Instance != null && EnemySpawner.Instance.transform.parent != null)
            return EnemySpawner.Instance.transform.parent;

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
            _battlefieldRenderer = _battlefieldRoot.GetComponentInChildren<Renderer>();
    }

    void ResolvePlacementPlane(out Vector3 planeNormal, out Vector3 planePoint)
    {
        planeNormal = _battlefieldRoot != null ? _battlefieldRoot.up : Vector3.up;
        planePoint = _battlefieldRoot != null ? _battlefieldRoot.position : Vector3.zero;

        if (LandmarkManager.Instance == null)
            return;

        var landmarks = LandmarkManager.Instance.GetAllLandmarks();
        if (landmarks == null || landmarks.Count == 0)
            return;

        Vector3 sum = Vector3.zero;
        int count = 0;
        foreach (GameObject landmark in landmarks)
        {
            if (landmark == null)
                continue;

            sum += landmark.transform.position;
            count++;
        }

        if (count > 0)
            planePoint = sum / count;
    }

    void FitPylonToBattlefield(GameObject pylonGO, Vector3 placementPoint)
    {
        Renderer[] renderers = pylonGO.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0)
            return;

        Bounds combinedBounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
            combinedBounds.Encapsulate(renderers[i].bounds);

        Vector3 surfaceNormal = _battlefieldRoot != null ? _battlefieldRoot.up : Vector3.up;
        GetProjectionRange(renderers, surfaceNormal, out float minProjection, out float maxProjection);
        float currentHeight = Mathf.Max(0.0001f, maxProjection - minProjection);
        float scaleFactor = TargetPylonHeight / currentHeight;
        pylonGO.transform.localScale *= scaleFactor;

        renderers = pylonGO.GetComponentsInChildren<Renderer>(true);
        SnapRenderersToSurface(pylonGO.transform, renderers, placementPoint, surfaceNormal);
    }

    IEnumerator SecondaryArcJitterCoroutine()
    {
        while (_secondaryArc != null && Pylon1 != null && Pylon2 != null)
        {
            float sin = Mathf.Sin(Time.realtimeSinceStartup * Mathf.PI * 2f / 0.6f);
            float pulse = (sin + 1f) * 0.5f;
            float width = Mathf.Lerp(0.02f, 0.045f, pulse);
            float alpha = Mathf.Lerp(0.6f, 1.0f, pulse);
            _secondaryArc.startWidth = width;
            _secondaryArc.endWidth = width;
            if (_arcMat != null)
            {
                Color c = new Color(ElectricCyan.r, ElectricCyan.g, ElectricCyan.b, alpha);
                _arcMat.color = c;
                if (_arcMat.HasProperty("_BaseColor")) _arcMat.SetColor("_BaseColor", c);
            }

            Vector3 p1w = GetPylonBeamOrigin(Pylon1);
            Vector3 p2w = GetPylonBeamOrigin(Pylon2);

            _secondaryArc.positionCount = 10;
            for (int i = 0; i < 10; i++)
            {
                Vector3 world = Vector3.Lerp(p1w, p2w, i / 9f);
                if (i > 0 && i < 9)
                {
                    world.x += Random.Range(-0.05f, 0.05f);
                    world.z += Random.Range(-0.05f, 0.05f);
                }
                _secondaryArc.SetPosition(i, _secondaryArc.transform.InverseTransformPoint(world));
            }

            yield return null;
        }
    }

    // ═════════════════════════════════════════════════════════════════════════
    // COUNTDOWN & EXPIRY
    // ═════════════════════════════════════════════════════════════════════════

    IEnumerator CountdownCoroutine()
    {
        timeRemaining = 12f;
        BuildCountdownBadge();

        while (timeRemaining > 0f)
        {
            timeRemaining -= Time.deltaTime;
            if (_countdownText != null)
                _countdownText.text = $"PYLON NETWORK {Mathf.CeilToInt(Mathf.Max(0f, timeRemaining))}s";
            yield return null;
        }

        yield return StartCoroutine(ExpireCoroutine());
    }

    IEnumerator ExpireCoroutine()
    {
        if (Pylon1 != null) SpawnExpiryBurst(Pylon1.transform.position);
        if (Pylon2 != null) SpawnExpiryBurst(Pylon2.transform.position);

        yield return StartCoroutine(FadePylonsOut());

        StopAllBeamCoroutines();
        DestroyBeam();
        DestroyPylons();
        HideCountdownBadge();

        _state = PlacementState.Idle;
        HUDManager.Instance?.RefreshPylonButton();
    }

    IEnumerator FadePylonsOut()
    {
        float elapsed  = 0f;
        float duration = 0.4f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float intensity = Mathf.Lerp(0.6f, 0f, elapsed / duration);
            Color emColor   = ElectricCyan * intensity;
            foreach (var p in new[] { Pylon1, Pylon2 })
            {
                if (p == null) continue;
                foreach (var r in p.GetComponentsInChildren<Renderer>())
                    if (r.material.HasProperty("_EmissionColor"))
                        r.material.SetColor("_EmissionColor", emColor);
            }
            yield return null;
        }
    }

    // ═════════════════════════════════════════════════════════════════════════
    // BEAM COLLISION
    // ═════════════════════════════════════════════════════════════════════════

    void CheckBeamCollisions()
    {
        if (Pylon1 == null || Pylon2 == null || EnemySpawner.Instance == null) return;

        Vector3 p1 = GetPylonBeamOrigin(Pylon1);
        Vector3 p2 = GetPylonBeamOrigin(Pylon2);

        // Enemies are parented to EnemySpawner — no FindObjectsByType needed
        var enemies   = EnemySpawner.Instance.GetComponentsInChildren<EnemyMoveToTarget>();
        var toDestroy = new List<EnemyMoveToTarget>(4);

        foreach (var enemy in enemies)
        {
            if (enemy == null) continue;
            if (PointToSegmentDist(enemy.transform.position, p1, p2) < 0.12f)
                toDestroy.Add(enemy);
        }

        foreach (var enemy in toDestroy)
        {
            if (enemy == null) continue;
            SpawnKillBurst(enemy.transform.position);
            GameEvents.EnemyDestroyed();
            EnemySpawner.Instance?.OnEnemyTapped();
            Destroy(enemy.gameObject);
        }
    }

    static float PointToSegmentDist(Vector3 pt, Vector3 a, Vector3 b)
    {
        Vector3 ab = b - a;
        if (ab.sqrMagnitude < 0.0001f) return Vector3.Distance(pt, a);
        float t = Mathf.Clamp01(Vector3.Dot(pt - a, ab) / ab.sqrMagnitude);
        return Vector3.Distance(pt, a + t * ab);
    }

    // ═════════════════════════════════════════════════════════════════════════
    // PARTICLES
    // ═════════════════════════════════════════════════════════════════════════

    void SpawnPlacementBurst(Vector3 pos)
    {
        var psGO = new GameObject("PylonPlaceBurst");
        psGO.transform.position = pos;
        var ps = psGO.AddComponent<ParticleSystem>();

        var main = ps.main;
        main.startColor    = ElectricCyan;
        main.startSize     = 0.02f;
        main.startLifetime = 0.6f;
        main.startSpeed    = 0.4f;
        main.loop          = false;
        main.playOnAwake   = false;
        main.simulationSpace = ParticleSystemSimulationSpace.World;

        var emission = ps.emission;
        emission.enabled = false;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 25) });
        emission.enabled = true;

        var shape = ps.shape;
        shape.enabled   = true;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius    = 0.3f;

        var rend = ps.GetComponent<ParticleSystemRenderer>();
        rend.material = ParticleMaterial();

        ps.Play();
        Destroy(psGO, 1.5f);
    }

    void SpawnExpiryBurst(Vector3 pos)
    {
        var psGO = new GameObject("PylonExpiryBurst");
        psGO.transform.position = pos;
        var ps = psGO.AddComponent<ParticleSystem>();

        var main = ps.main;
        main.startColor    = Color.white;
        main.startSize     = 0.03f;
        main.startLifetime = 0.8f;
        main.startSpeed    = 0.3f;
        main.loop          = false;
        main.playOnAwake   = false;

        var emission = ps.emission;
        emission.enabled = false;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 15) });
        emission.enabled = true;

        var shape = ps.shape;
        shape.enabled   = true;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius    = 0.1f;

        ps.GetComponent<ParticleSystemRenderer>().material = ParticleMaterial();
        ps.Play();
        Destroy(psGO, 2f);
    }

    void SpawnKillBurst(Vector3 pos)
    {
        var psGO = new GameObject("PylonKillBurst");
        psGO.transform.position = pos;
        var ps = psGO.AddComponent<ParticleSystem>();

        var main = ps.main;
        main.startColor    = ElectricCyan;
        main.startSize     = 0.015f;
        main.startLifetime = 0.4f;
        main.startSpeed    = 0.3f;
        main.loop          = false;
        main.playOnAwake   = false;

        var emission = ps.emission;
        emission.enabled = false;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 10) });
        emission.enabled = true;

        var shape = ps.shape;
        shape.enabled   = true;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius    = 0.05f;

        ps.GetComponent<ParticleSystemRenderer>().material = ParticleMaterial();
        ps.Play();
        Destroy(psGO, 1f);
    }

    static Material ParticleMaterial()
    {
        Shader s = Shader.Find("Particles/Standard Unlit")
                ?? Shader.Find("Legacy Shaders/Particles/Additive")
                ?? Shader.Find("Sprites/Default");
        return s != null ? new Material(s) : new Material(Shader.Find("Standard"));
    }

    // ═════════════════════════════════════════════════════════════════════════
    // HUD ELEMENTS
    // ═════════════════════════════════════════════════════════════════════════

    void BuildInstructionBanner()
    {
        Canvas canvas = FindHUDCanvas();
        if (canvas == null) return;

        _instructionBanner = new GameObject("PylonInstructionBanner");
        _instructionBanner.transform.SetParent(canvas.transform, false);

        var rt = _instructionBanner.AddComponent<RectTransform>();
        rt.anchorMin        = new Vector2(0f, 1f);
        rt.anchorMax        = new Vector2(1f, 1f);
        rt.pivot            = new Vector2(0.5f, 1f);
        rt.anchoredPosition = new Vector2(0f, -124f); // flush below the 120px top bar
        rt.sizeDelta        = new Vector2(0f, 48f);

        _instructionBanner.AddComponent<Image>().color = ElectricCyan;

        var textGO = new GameObject("Text");
        textGO.transform.SetParent(_instructionBanner.transform, false);
        var trt = textGO.AddComponent<RectTransform>();
        trt.anchorMin = Vector2.zero; trt.anchorMax = Vector2.one;
        trt.offsetMin = new Vector2(8, 2); trt.offsetMax = new Vector2(-8, -2);

        _instructionText = textGO.AddComponent<TextMeshProUGUI>();
        _instructionText.text      = "";
        _instructionText.fontSize  = 24f;
        _instructionText.color     = DarkNavy;
        _instructionText.alignment = TextAlignmentOptions.Center;
        _instructionText.fontStyle = FontStyles.Bold;

        _instructionBanner.SetActive(false);
    }

    void ShowInstructionBanner(string text)
    {
        if (_instructionBanner == null) BuildInstructionBanner();
        if (_instructionText   != null) _instructionText.text = text;
        if (_instructionBanner != null) _instructionBanner.SetActive(true);
    }

    void HideInstructionBanner()
    {
        if (_instructionBanner != null) _instructionBanner.SetActive(false);
    }

    void BuildCountdownBadge()
    {
        Canvas canvas = FindHUDCanvas();
        if (canvas == null) return;

        _countdownBadge = new GameObject("PylonCountdownBadge");
        _countdownBadge.transform.SetParent(canvas.transform, false);

        var rt = _countdownBadge.AddComponent<RectTransform>();
        rt.anchorMin        = new Vector2(0.2f, 1f);
        rt.anchorMax        = new Vector2(0.8f, 1f);
        rt.pivot            = new Vector2(0.5f, 1f);
        rt.anchoredPosition = new Vector2(0f, -174f); // below instruction banner position
        rt.sizeDelta        = new Vector2(0f, 44f);

        var bg = _countdownBadge.AddComponent<Image>();
        bg.color = new Color(ElectricCyan.r, ElectricCyan.g, ElectricCyan.b, 0.85f);

        var textGO = new GameObject("Text");
        textGO.transform.SetParent(_countdownBadge.transform, false);
        var trt = textGO.AddComponent<RectTransform>();
        trt.anchorMin = Vector2.zero; trt.anchorMax = Vector2.one;
        trt.offsetMin = new Vector2(4, 2); trt.offsetMax = new Vector2(-4, -2);

        _countdownText = textGO.AddComponent<TextMeshProUGUI>();
        _countdownText.text      = "PYLON NETWORK 12s";
        _countdownText.fontSize  = 20f;
        _countdownText.color     = DarkNavy;
        _countdownText.alignment = TextAlignmentOptions.Center;
        _countdownText.fontStyle = FontStyles.Bold;

        _countdownBadge.SetActive(true);
    }

    void HideCountdownBadge()
    {
        if (_countdownBadge == null) return;
        Destroy(_countdownBadge);
        _countdownBadge = null;
        _countdownText  = null;
    }

    Canvas FindHUDCanvas()
    {
        foreach (var c in FindObjectsByType<Canvas>(FindObjectsSortMode.None))
            if (c.renderMode == RenderMode.ScreenSpaceOverlay && c.sortingOrder == 10)
                return c;
        foreach (var c in FindObjectsByType<Canvas>(FindObjectsSortMode.None))
            if (c.renderMode == RenderMode.ScreenSpaceOverlay)
                return c;
        return null;
    }

    // ═════════════════════════════════════════════════════════════════════════
    // CLEANUP
    // ═════════════════════════════════════════════════════════════════════════

    void StopAllBeamCoroutines()
    {
        if (_beamPulseCoroutine != null) { StopCoroutine(_beamPulseCoroutine); _beamPulseCoroutine = null; }
        if (_arcJitterCoroutine  != null) { StopCoroutine(_arcJitterCoroutine);  _arcJitterCoroutine  = null; }
    }

    void DestroyBeam()
    {
        StopAllBeamCoroutines();
        Transform beamRoot = null;

        if (_primaryBeam != null)
        {
            beamRoot = _primaryBeam.transform.parent != null
                ? _primaryBeam.transform.parent
                : _primaryBeam.transform;
        }
        else if (_secondaryArc != null)
        {
            beamRoot = _secondaryArc.transform.parent != null
                ? _secondaryArc.transform.parent
                : _secondaryArc.transform;
        }

        if (beamRoot != null)
            Destroy(beamRoot.gameObject);

        _primaryBeam  = null;
        _secondaryArc = null;
    }

    void DestroyPylons()
    {
        if (Pylon1 != null) { Destroy(Pylon1); Pylon1 = null; }
        if (Pylon2 != null) { Destroy(Pylon2); Pylon2 = null; }
    }

    static void SnapRenderersToSurface(Transform target, Renderer[] renderers, Vector3 placementPoint, Vector3 surfaceNormal)
    {
        GetProjectionRange(renderers, surfaceNormal, out float minProjection, out _);
        float targetProjection = Vector3.Dot(surfaceNormal, placementPoint) + 0.0025f;
        float delta = targetProjection - minProjection;
        target.position += surfaceNormal * delta;
    }

    static void GetProjectionRange(Renderer[] renderers, Vector3 direction, out float minProjection, out float maxProjection)
    {
        minProjection = float.MaxValue;
        maxProjection = float.MinValue;

        foreach (Renderer renderer in renderers)
        {
            foreach (Vector3 corner in GetBoundsCorners(renderer.bounds))
            {
                float projection = Vector3.Dot(direction, corner);
                if (projection < minProjection) minProjection = projection;
                if (projection > maxProjection) maxProjection = projection;
            }
        }
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
}
