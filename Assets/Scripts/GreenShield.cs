using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GreenShield : MonoBehaviour
{
    static readonly List<GreenShield> ActiveShields = new List<GreenShield>();
    static readonly Color EmeraldGreen = new Color(0f, 0.784f, 0.325f, 1f);
    static readonly Color GlowColor = new Color(0f, 1f, 0.416f, 1f);
    static readonly Color DarkNavy = new Color(0.051f, 0.051f, 0.169f, 1f);

    public float TimeRemaining { get; private set; }

    private Transform _landmark;
    private float _duration;
    private LandmarkHealth _lh;
    private GameObject _dome;
    private GameObject _glow;
    private Material _domeMat;
    private Material _glowMat;
    private GameObject _countdownBadge;
    private TextMeshProUGUI _countdownText;
    private bool _isSubscribedToWaveStart;

    public void Init(Transform landmark, float duration)
    {
        _landmark = landmark;
        _duration = duration;
        TimeRemaining = duration;

        _lh = landmark.GetComponentInChildren<LandmarkHealth>();

        SetShielded(true);
        BuildDome();
        BuildEdgeGlow();
        PlaySpawnPunch();
        PlayActivationBurst();
        RegisterActiveShield();
        BuildCountdownBadge();
        RefreshCountdownLabel();
        StartCoroutine(ShieldCountdown());
        StartCoroutine(PulseDome());
    }

    void SetShielded(bool state)
    {
        if (_lh != null)
        {
            _lh.SetShielded(state);
        }
    }

    IEnumerator ShieldCountdown()
    {
        EnemySpawner.OnWaveStarted += OnWaveStarted;
        _isSubscribedToWaveStart = true;

        while (TimeRemaining > 0f)
        {
            TimeRemaining -= Time.unscaledDeltaTime;
            RefreshCountdownLabel();
            yield return null;
        }

        UnsubscribeFromWaveStart();
        Expire();
    }

    void OnWaveStarted(int _)
    {
        UnsubscribeFromWaveStart();
        StopAllCoroutines();
        TimeRemaining = 0f;
        CleanupVisuals();
        SetShielded(false);
        DestroyCountdownBadge();
        Destroy(gameObject);
    }

    void Expire()
    {
        SetShielded(false);
        CleanupVisuals();
        PlayExpiryBurst();
        DestroyCountdownBadge();
        Destroy(gameObject);
    }

    void BuildDome()
    {
        _dome = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        Destroy(_dome.GetComponent<Collider>());
        _dome.name = "ShieldDome";
        _dome.transform.SetParent(_landmark, false);

        var (scale, localCenter) = ComputeCapsuleScaleAndCenter();
        _dome.transform.localScale = scale;
        _dome.transform.localPosition = localCenter;

        _domeMat = new Material(PickShader());
        ApplyTransparentSettings(_domeMat, EmeraldGreen, 0.15f, 0.25f);
        _dome.GetComponent<Renderer>().material = _domeMat;
    }

    void BuildEdgeGlow()
    {
        _glow = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        Destroy(_glow.GetComponent<Collider>());
        _glow.name = "ShieldGlow";
        _glow.transform.SetParent(_landmark, false);
        _glow.transform.localPosition = _dome.transform.localPosition;
        _glow.transform.localScale = _dome.transform.localScale * 1.04f;

        _glowMat = new Material(PickShader());
        ApplyTransparentSettings(_glowMat, EmeraldGreen, 0f, 0.7f);
        _glow.GetComponent<Renderer>().material = _glowMat;
    }

    (Vector3 scale, Vector3 localCenter) ComputeCapsuleScaleAndCenter()
    {
        Renderer[] all = _landmark.GetComponentsInChildren<Renderer>();

        Bounds? b = null;
        foreach (var r in all)
        {
            string n = r.gameObject.name;
            if (n == "ShieldDome" || n == "ShieldGlow")
            {
                continue;
            }

            if (b == null)
            {
                b = r.bounds;
            }
            else
            {
                Bounds tmp = b.Value;
                tmp.Encapsulate(r.bounds);
                b = tmp;
            }
        }

        if (b == null)
        {
            return (new Vector3(2f, 1.5f, 2f), Vector3.zero);
        }

        Bounds bounds = b.Value;
        const float pad = 1.12f;
        float worldWidth = Mathf.Max(bounds.size.x, bounds.size.z) * pad;
        float worldHeight = bounds.size.y * pad;

        Vector3 ls = _landmark.lossyScale;

        float lsX = ls.x > 0.0001f ? worldWidth / ls.x : 2f;
        float lsY = ls.y > 0.0001f ? worldHeight / (2f * ls.y) : 1.5f;
        float lsZ = ls.z > 0.0001f ? worldWidth / ls.z : 2f;
        Vector3 localCenter = _landmark.InverseTransformPoint(bounds.center);

        return (new Vector3(lsX, lsY, lsZ), localCenter);
    }

    IEnumerator PulseDome()
    {
        float cycle = 1.5f;
        while (true)
        {
            float t = Mathf.PingPong(Time.time, cycle) / cycle;
            float a = Mathf.Lerp(0.08f, 0.22f, Mathf.Sin(t * Mathf.PI));

            if (_domeMat != null)
            {
                Color c = _domeMat.color;
                _domeMat.color = new Color(c.r, c.g, c.b, a);
                _domeMat.SetColor("_BaseColor", new Color(c.r, c.g, c.b, a));
            }

            if (_glowMat != null)
            {
                float ei = Mathf.Lerp(0.4f, 0.9f, Mathf.Sin(t * Mathf.PI));
                _glowMat.SetColor("_EmissionColor", GlowColor * ei);
            }

            yield return null;
        }
    }

    void PlaySpawnPunch()
    {
        if (_dome != null)
        {
            StartCoroutine(ScalePunchCoroutine(_dome.transform, _dome.transform.localScale));
        }

        if (_glow != null)
        {
            StartCoroutine(ScalePunchCoroutine(_glow.transform, _glow.transform.localScale));
        }
    }

    IEnumerator ScalePunchCoroutine(Transform target, Vector3 finalScale)
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

    void PlayActivationBurst()
    {
        SpawnBurst(_landmark.position, 40, GlowColor, 0.05f, 0.8f, 0.3f, 2f);
    }

    void PlayExpiryBurst()
    {
        SpawnBurst(_landmark.position, 15, Color.white, 0.05f, 0.8f, 0.3f, 2f);
    }

    void SpawnBurst(Vector3 pos, int count, Color color, float size, float lifetime, float speed, float destroyAfter)
    {
        GameObject psGO = new GameObject("ShieldParticles");
        psGO.transform.position = pos;
        var ps = psGO.AddComponent<ParticleSystem>();

        float domeRadius = _dome != null ? _dome.transform.lossyScale.x * 0.5f : 0.5f;

        var main = ps.main;
        main.startColor = color;
        main.startSize = size;
        main.startLifetime = lifetime;
        main.startSpeed = speed;
        main.loop = false;
        main.playOnAwake = false;

        var emission = ps.emission;
        emission.enabled = false;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, count) });
        emission.enabled = true;

        var shape = ps.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = domeRadius;

        var renderer = ps.GetComponent<ParticleSystemRenderer>();
        renderer.material = new Material(Shader.Find("Particles/Standard Unlit") ?? Shader.Find("Legacy Shaders/Particles/Additive"));

        ps.Play();
        Destroy(psGO, destroyAfter);
    }

    void RegisterActiveShield()
    {
        if (!ActiveShields.Contains(this))
        {
            ActiveShields.Add(this);
        }
    }

    void BuildCountdownBadge()
    {
        Canvas hudCanvas = FindHUDCanvas();
        if (hudCanvas == null)
        {
            return;
        }

        _countdownBadge = new GameObject("ShieldCountdownBadge_" + _landmark.name);
        _countdownBadge.transform.SetParent(hudCanvas.transform, false);

        var rt = _countdownBadge.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.22f, 1f);
        rt.anchorMax = new Vector2(0.78f, 1f);
        rt.pivot = new Vector2(0.5f, 1f);
        rt.sizeDelta = new Vector2(0f, 40f);

        var bg = _countdownBadge.AddComponent<Image>();
        bg.color = new Color(EmeraldGreen.r, EmeraldGreen.g, EmeraldGreen.b, 0.9f);

        var textGO = new GameObject("Text");
        textGO.transform.SetParent(_countdownBadge.transform, false);
        var trt = textGO.AddComponent<RectTransform>();
        trt.anchorMin = Vector2.zero;
        trt.anchorMax = Vector2.one;
        trt.offsetMin = new Vector2(4f, 2f);
        trt.offsetMax = new Vector2(-4f, -2f);

        _countdownText = textGO.AddComponent<TextMeshProUGUI>();
        _countdownText.text = "GREEN SHIELD 12s";
        _countdownText.fontSize = 20f;
        _countdownText.color = DarkNavy;
        _countdownText.alignment = TextAlignmentOptions.Center;
        _countdownText.fontStyle = FontStyles.Bold;

        RefreshBadgeLayout();
    }

    void RefreshCountdownLabel()
    {
        if (_countdownText == null)
        {
            return;
        }

        int secs = Mathf.CeilToInt(Mathf.Max(0f, TimeRemaining));
        _countdownText.text = $"GREEN SHIELD {secs}s";
    }

    static void RefreshBadgeLayout()
    {
        for (int i = 0; i < ActiveShields.Count; i++)
        {
            if (ActiveShields[i] == null || ActiveShields[i]._countdownBadge == null)
            {
                continue;
            }

            var rect = ActiveShields[i]._countdownBadge.GetComponent<RectTransform>();
            if (rect != null)
            {
                rect.anchoredPosition = new Vector2(0f, -174f - (48f * i));
            }
        }
    }

    void DestroyCountdownBadge()
    {
        ActiveShields.Remove(this);

        if (_countdownBadge != null)
        {
            Destroy(_countdownBadge);
        }

        _countdownBadge = null;
        _countdownText = null;
        RefreshBadgeLayout();
    }

    Canvas FindHUDCanvas()
    {
        foreach (Canvas c in FindObjectsByType<Canvas>(FindObjectsSortMode.None))
        {
            if (c.renderMode == RenderMode.ScreenSpaceOverlay && c.sortingOrder == 10)
            {
                return c;
            }
        }

        foreach (Canvas c in FindObjectsByType<Canvas>(FindObjectsSortMode.None))
        {
            if (c.renderMode == RenderMode.ScreenSpaceOverlay)
            {
                return c;
            }
        }

        return null;
    }

    void CleanupVisuals()
    {
        if (_dome != null)
        {
            Destroy(_dome);
        }

        if (_glow != null)
        {
            Destroy(_glow);
        }
    }

    void OnDestroy()
    {
        UnsubscribeFromWaveStart();
        DestroyCountdownBadge();
    }

    void UnsubscribeFromWaveStart()
    {
        if (!_isSubscribedToWaveStart)
        {
            return;
        }

        EnemySpawner.OnWaveStarted -= OnWaveStarted;
        _isSubscribedToWaveStart = false;
    }

    static Shader PickShader()
    {
        Shader s = Shader.Find("Universal Render Pipeline/Lit");
        if (s != null)
        {
            return s;
        }

        s = Shader.Find("Standard");
        if (s != null)
        {
            return s;
        }

        return Shader.Find("Legacy Shaders/Transparent/Diffuse");
    }

    static void ApplyTransparentSettings(Material mat, Color baseColor, float alpha, float emission)
    {
        bool isURP = mat.shader.name.StartsWith("Universal Render Pipeline");

        if (isURP)
        {
            mat.SetFloat("_Surface", 1f);
            mat.SetFloat("_Blend", 0f);
            mat.SetFloat("_AlphaClip", 0f);
            mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            mat.SetInt("_ZWrite", 0);
            mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        }
        else
        {
            mat.SetFloat("_Mode", 3f);
            mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            mat.SetInt("_ZWrite", 0);
            mat.DisableKeyword("_ALPHATEST_ON");
            mat.EnableKeyword("_ALPHABLEND_ON");
            mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        }

        mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;

        Color c = baseColor;
        c.a = alpha;
        mat.color = c;
        mat.SetColor("_BaseColor", c);

        mat.EnableKeyword("_EMISSION");
        Color ec = new Color(0f, 1f, 0.416f, 1f) * emission;
        mat.SetColor("_EmissionColor", ec);
        mat.SetColor("_EmissiveColor", ec);
    }
}
