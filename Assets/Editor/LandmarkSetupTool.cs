using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;
using UnityEngine.XR.Interaction.Toolkit.Samples.StarterAssets;

/// <summary>
/// Run via  Tools > Setup All Landmarks
/// Creates Burj Al Arab and Dubai Frame prefabs, wires the ObjectSpawner,
/// adds LandmarkManager to the scene, and builds the placement instruction UI.
/// </summary>
public class LandmarkSetupTool
{
    private const string PrefabFolder       = "Assets/Prefabs";
    private const string BurjAlArabFBX      = "Assets/WDiiAssets/LowPolyDubaiPack/Models/burjAlArab.FBX";
    private const string DubaiFrameFBX      = "Assets/WDiiAssets/LowPolyDubaiPack/Models/dubaiFrame.FBX";
    private const string BurjAlArabPrefab   = "Assets/Prefabs/BurjAlArab.prefab";
    private const string DubaiFramePrefab   = "Assets/Prefabs/DubaiFrame.prefab";

    private const string BattlefieldPlatformPrefab = "Assets/Prefabs/BattlefieldPlatform.prefab";

    [MenuItem("Tools/Fix HP Bars")]
    static void FixHPBars()
    {
        // Add/replace HPCanvas on BattlefieldPlatform at Y=1.0 above tower
        AddHPCanvasToBattlefieldPlatform();

        // Fix Y height on BAA and DubaiFrame (0.3 → 1.0)
        FixCanvasHeight(BurjAlArabPrefab);
        FixCanvasHeight(DubaiFramePrefab);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("=== HP Bar fix complete! ===");
    }

    static void AddHPCanvasToBattlefieldPlatform()
    {
        GameObject prefabAsset = AssetDatabase.LoadAssetAtPath<GameObject>(BattlefieldPlatformPrefab);
        if (prefabAsset == null)
        {
            Debug.LogError("BattlefieldPlatform prefab not found at " + BattlefieldPlatformPrefab);
            return;
        }

        // Edit in place
        string assetPath = AssetDatabase.GetAssetPath(prefabAsset);
        GameObject root = PrefabUtility.LoadPrefabContents(assetPath);

        // Remove old HPCanvas if already present
        Transform existing = root.transform.Find("HPCanvas");
        if (existing != null) Object.DestroyImmediate(existing.gameObject);

        // Build new HP canvas and parent to root
        GameObject canvas = BuildHPCanvas(root.transform);
        canvas.transform.localPosition = new Vector3(0f, 1.0f, 0f);
        canvas.SetActive(false); // BurjKhalifaHealth.Start() activates it

        PrefabUtility.SaveAsPrefabAsset(root, assetPath);
        PrefabUtility.UnloadPrefabContents(root);

        Debug.Log("Added HPCanvas to BattlefieldPlatform at Y=1.0");
    }

    static void FixCanvasHeight(string prefabPath)
    {
        GameObject prefabAsset = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        if (prefabAsset == null) { Debug.LogError("Prefab not found: " + prefabPath); return; }

        string assetPath = AssetDatabase.GetAssetPath(prefabAsset);
        GameObject root = PrefabUtility.LoadPrefabContents(assetPath);

        Transform canvas = root.transform.Find("HPCanvas");
        if (canvas != null)
        {
            canvas.localPosition = new Vector3(0f, 1.0f, 0f);
            Debug.Log($"Fixed HPCanvas Y on {prefabAsset.name}");
        }
        else
            Debug.LogWarning($"HPCanvas not found on {prefabAsset.name}");

        PrefabUtility.SaveAsPrefabAsset(root, assetPath);
        PrefabUtility.UnloadPrefabContents(root);
    }

    [MenuItem("Tools/Setup All Landmarks")]
    static void SetupAll()
    {
        EnsurePrefabFolder();

        GameObject baaPrefab = CreateOrUpdateLandmarkPrefab(
            "BurjAlArab", BurjAlArabFBX, "Burj Al Arab", BurjAlArabPrefab);

        GameObject dfPrefab = CreateOrUpdateLandmarkPrefab(
            "DubaiFrame", DubaiFrameFBX, "Dubai Frame", DubaiFramePrefab);

        if (baaPrefab == null || dfPrefab == null)
        {
            Debug.LogError("Prefab creation failed. Aborting scene setup.");
            return;
        }

        SetupScene(baaPrefab, dfPrefab);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("=== Landmark setup complete! Open ARScene and press Play. ===");
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Prefab creation
    // ─────────────────────────────────────────────────────────────────────────

    static GameObject CreateOrUpdateLandmarkPrefab(
        string goName, string fbxPath, string landmarkName, string savePath)
    {
        // Root
        GameObject root = new GameObject(goName);

        // 3-D model
        GameObject modelAsset = AssetDatabase.LoadAssetAtPath<GameObject>(fbxPath);
        if (modelAsset == null)
        {
            Debug.LogError($"FBX not found at {fbxPath}");
            Object.DestroyImmediate(root);
            return null;
        }
        GameObject model = (GameObject)PrefabUtility.InstantiatePrefab(modelAsset);
        model.name = "Model";
        model.transform.SetParent(root.transform, false);
        model.transform.localPosition = Vector3.zero;
        model.transform.localRotation = Quaternion.identity;
        model.transform.localScale    = Vector3.one;

        // Collider on root so enemies can reach it
        root.AddComponent<BoxCollider>();

        // Health component
        LandmarkHealth health       = root.AddComponent<LandmarkHealth>();
        health.landmarkName         = landmarkName;
        health.maxHP                = 100;

        // HP canvas (World Space, child of landmark so it moves with it)
        GameObject canvasGO = BuildHPCanvas(root.transform);

        // Save
        canvasGO.SetActive(false); // starts hidden; LandmarkHealth.Start() activates it

        GameObject saved = PrefabUtility.SaveAsPrefabAsset(root, savePath);
        Object.DestroyImmediate(root);

        Debug.Log($"Saved prefab: {savePath}");
        return saved;
    }

    static GameObject BuildHPCanvas(Transform parent)
    {
        // ── Canvas ────────────────────────────────────────────────────────────
        GameObject canvasGO = new GameObject("HPCanvas");
        canvasGO.transform.SetParent(parent, false);
        canvasGO.transform.localPosition = new Vector3(0f, 0.3f, 0f);
        canvasGO.transform.localRotation = Quaternion.identity;
        canvasGO.transform.localScale    = new Vector3(0.002f, 0.002f, 0.002f);

        Canvas canvas       = canvasGO.AddComponent<Canvas>();
        canvas.renderMode   = RenderMode.WorldSpace;
        canvasGO.AddComponent<CanvasScaler>();
        canvasGO.AddComponent<GraphicRaycaster>();

        RectTransform crt = canvasGO.GetComponent<RectTransform>();
        crt.sizeDelta = new Vector2(300f, 60f);

        // Camera-facing component
        canvasGO.AddComponent<LandmarkHPCanvasFollower>();

        // ── Background panel ──────────────────────────────────────────────────
        GameObject bgGO    = new GameObject("Background");
        bgGO.transform.SetParent(canvasGO.transform, false);
        Image bgImg        = bgGO.AddComponent<Image>();
        bgImg.color        = new Color(0f, 0f, 0f, 0.55f);
        RectTransform bgRT = bgGO.GetComponent<RectTransform>();
        bgRT.anchorMin     = Vector2.zero;
        bgRT.anchorMax     = Vector2.one;
        bgRT.offsetMin     = Vector2.zero;
        bgRT.offsetMax     = Vector2.zero;

        // ── Landmark name label ───────────────────────────────────────────────
        // (optional but helpful in AR)
        // skipped to keep canvas compact

        // ── Slider (HP bar) ───────────────────────────────────────────────────
        var res = new DefaultControls.Resources();
        res.background  = MakeWhiteTexture();
        res.standard    = MakeWhiteTexture();
        res.knob        = MakeWhiteTexture();

        GameObject sliderGO = DefaultControls.CreateSlider(res);
        sliderGO.name       = "HPSlider";
        sliderGO.transform.SetParent(canvasGO.transform, false);
        RectTransform srt = sliderGO.GetComponent<RectTransform>();
        srt.anchorMin      = new Vector2(0.02f, 0.25f);
        srt.anchorMax      = new Vector2(0.75f, 0.75f);
        srt.offsetMin      = Vector2.zero;
        srt.offsetMax      = Vector2.zero;

        Slider slider       = sliderGO.GetComponent<Slider>();
        slider.minValue     = 0;
        slider.maxValue     = 100;
        slider.value        = 100;
        slider.interactable = false;

        // Colour the fill green and hide the handle
        ColorFill(sliderGO, new Color(0.15f, 0.85f, 0.25f));
        ColorBackground(sliderGO, new Color(0.25f, 0.25f, 0.25f, 0.8f));
        HideHandle(sliderGO);

        // Attach our HP slider controller
        LandmarkHPSlider hpSlider = sliderGO.AddComponent<LandmarkHPSlider>();

        // ── HP number text ────────────────────────────────────────────────────
        GameObject textGO  = new GameObject("HPText");
        textGO.transform.SetParent(canvasGO.transform, false);
        TextMeshProUGUI tmp = textGO.AddComponent<TextMeshProUGUI>();
        tmp.text            = "100";
        tmp.fontSize        = 22f;
        tmp.alignment       = TextAlignmentOptions.MidlineLeft;
        tmp.color           = Color.white;
        RectTransform trt   = textGO.GetComponent<RectTransform>();
        trt.anchorMin       = new Vector2(0.77f, 0.2f);
        trt.anchorMax       = new Vector2(1f,    0.8f);
        trt.offsetMin       = Vector2.zero;
        trt.offsetMax       = Vector2.zero;

        hpSlider.hpText = tmp;

        return canvasGO;
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Scene setup
    // ─────────────────────────────────────────────────────────────────────────

    static void SetupScene(GameObject baaPrefab, GameObject dfPrefab)
    {
        // ── ObjectSpawner: ensure 3 entries ──────────────────────────────────
        ObjectSpawner spawner = Object.FindFirstObjectByType<ObjectSpawner>();
        if (spawner == null)
        {
            Debug.LogWarning("ObjectSpawner not found in the active scene. " +
                             "Open ARScene before running this tool, or assign prefabs manually.");
        }
        else
        {
            var list = spawner.objectPrefabs;
            // Keep index 0 (Burj Khalifa) untouched
            if (list.Count < 2) list.Add(baaPrefab);
            else                list[1] = baaPrefab;

            if (list.Count < 3) list.Add(dfPrefab);
            else                list[2] = dfPrefab;

            spawner.spawnOptionIndex = 0;
            spawner.isFieldSpawner   = false; // ensure unlocked at startup
            EditorUtility.SetDirty(spawner);
            Debug.Log("ObjectSpawner updated: 3 landmark prefabs assigned.");
        }

        // ── LandmarkManager ──────────────────────────────────────────────────
        LandmarkManager mgr = Object.FindFirstObjectByType<LandmarkManager>();
        if (mgr == null)
        {
            GameObject mgrGO = new GameObject("LandmarkManager");
            mgr = mgrGO.AddComponent<LandmarkManager>();
            Debug.Log("Created LandmarkManager in scene.");
        }

        // Wire the ObjectSpawner reference via SerializedObject so it survives
        if (spawner != null)
        {
            SerializedObject so = new SerializedObject(mgr);
            so.FindProperty("objectSpawner").objectReferenceValue = spawner;
            so.ApplyModifiedProperties();
        }

        // ── Instruction UI ────────────────────────────────────────────────────
        Canvas mainCanvas = FindMainCanvas();
        if (mainCanvas == null)
        {
            Debug.LogWarning("No Screen Space Canvas found. " +
                             "Create an Instruction Panel manually and assign it to LandmarkManager.");
            return;
        }

        // Reuse existing panel if already present
        Transform existing = mainCanvas.transform.Find("LandmarkInstructionPanel");
        GameObject panel = existing != null
            ? existing.gameObject
            : BuildInstructionPanel(mainCanvas.transform);

        // Wire into LandmarkManager
        SerializedObject mgrSO = new SerializedObject(mgr);
        mgrSO.FindProperty("instructionPanel").objectReferenceValue = panel;

        TextMeshProUGUI instrText = panel.GetComponentInChildren<TextMeshProUGUI>();
        mgrSO.FindProperty("instructionText").objectReferenceValue = instrText;
        mgrSO.ApplyModifiedProperties();

        EditorUtility.SetDirty(mgr.gameObject);
        UnityEditor.SceneManagement.EditorSceneManager.MarkAllScenesDirty();
        Debug.Log("LandmarkManager wired with ObjectSpawner + instruction panel.");
    }

    static Canvas FindMainCanvas()
    {
        foreach (Canvas c in Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None))
            if (c.renderMode == RenderMode.ScreenSpaceOverlay ||
                c.renderMode == RenderMode.ScreenSpaceCamera)
                return c;
        return null;
    }

    static GameObject BuildInstructionPanel(Transform canvasParent)
    {
        // Panel root
        GameObject panel   = new GameObject("LandmarkInstructionPanel");
        panel.transform.SetParent(canvasParent, false);
        Image bg           = panel.AddComponent<Image>();
        bg.color           = new Color(0f, 0f, 0f, 0.65f);
        RectTransform prt  = panel.GetComponent<RectTransform>();
        prt.anchorMin      = new Vector2(0.1f, 0.05f);
        prt.anchorMax      = new Vector2(0.9f, 0.22f);
        prt.offsetMin      = Vector2.zero;
        prt.offsetMax      = Vector2.zero;

        // Text
        GameObject textGO  = new GameObject("InstructionText");
        textGO.transform.SetParent(panel.transform, false);
        TextMeshProUGUI tmp = textGO.AddComponent<TextMeshProUGUI>();
        tmp.text            = "Place Burj Khalifa";
        tmp.fontSize        = 36f;
        tmp.alignment       = TextAlignmentOptions.Center;
        tmp.color           = Color.white;
        RectTransform trt   = textGO.GetComponent<RectTransform>();
        trt.anchorMin       = Vector2.zero;
        trt.anchorMax       = Vector2.one;
        trt.offsetMin       = new Vector2(10, 5);
        trt.offsetMax       = new Vector2(-10, -5);

        return panel;
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Helpers
    // ─────────────────────────────────────────────────────────────────────────

    static void EnsurePrefabFolder()
    {
        if (!AssetDatabase.IsValidFolder(PrefabFolder))
            AssetDatabase.CreateFolder("Assets", "Prefabs");
    }

    static Sprite MakeWhiteTexture()
    {
        Texture2D tex = new Texture2D(1, 1);
        tex.SetPixel(0, 0, Color.white);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f));
    }

    static void ColorFill(GameObject sliderGO, Color color)
    {
        Transform fillArea = sliderGO.transform.Find("Fill Area");
        if (fillArea == null) return;
        Transform fill = fillArea.Find("Fill");
        if (fill == null) return;
        Image img = fill.GetComponent<Image>();
        if (img != null) img.color = color;
    }

    static void ColorBackground(GameObject sliderGO, Color color)
    {
        Transform bg = sliderGO.transform.Find("Background");
        if (bg == null) return;
        Image img = bg.GetComponent<Image>();
        if (img != null) img.color = color;
    }

    static void HideHandle(GameObject sliderGO)
    {
        Transform handleArea = sliderGO.transform.Find("Handle Slide Area");
        if (handleArea != null) handleArea.gameObject.SetActive(false);
    }
}
