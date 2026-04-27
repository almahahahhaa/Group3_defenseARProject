using UnityEngine;
using UnityEditor;

public static class CreateBurjKhalifaPrefab
{
    [MenuItem("Tools/Create BurjKhalifa Prefab")]
    public static void Create()
    {
        const string baaPath = "Assets/Prefabs/BurjAlArab.prefab";
        const string bkfbxPath = "Assets/WDiiAssets/LowPolyDubaiPack/Models/burjKhalifa.FBX";
        const string outPath  = "Assets/Prefabs/BurjKhalifa.prefab";

        // Load BurjAlArab and open an editable copy in memory
        GameObject root = PrefabUtility.LoadPrefabContents(baaPath);
        if (root == null) { Debug.LogError("Could not load BurjAlArab.prefab"); return; }

        // Rename root
        root.name = "BurjKhalifa";

        // Update LandmarkHealth
        var lh = root.GetComponent<LandmarkHealth>();
        if (lh != null) lh.landmarkName = "Burj Khalifa";

        // Remove the BurjAlArab model child (child 0 is always the 3-D model)
        if (root.transform.childCount > 0)
            Object.DestroyImmediate(root.transform.GetChild(0).gameObject);

        // Insert BurjKhalifa FBX as child 0
        var fbx = AssetDatabase.LoadAssetAtPath<GameObject>(bkfbxPath);
        if (fbx == null)
        {
            PrefabUtility.UnloadPrefabContents(root);
            Debug.LogError("burjKhalifa.FBX not found at: " + bkfbxPath);
            return;
        }

        var model = (GameObject)PrefabUtility.InstantiatePrefab(fbx, root.transform);
        model.name  = "BurjKhalifaModel";
        model.transform.SetSiblingIndex(0);
        model.transform.localPosition = Vector3.zero;
        model.transform.localRotation = Quaternion.identity;
        // BurjKhalifa FBX native height = 8.294 units.
        // BurjAlArab (height 2.637) at scale 0.3 displays as ~0.79 m.
        // DubaiFrame  (height 2.924) at scale 0.27 displays as ~0.79 m.
        // Scale BK to ~0.95 m (≈1.2× others) so it is visibly the tallest without being abnormal.
        model.transform.localScale = new Vector3(0.115f, 0.115f, 0.115f);

        // Adjust BoxCollider to cover the model (world size ≈ 0.31 × 0.95 × 0.31 m)
        var col = root.GetComponent<BoxCollider>();
        if (col != null)
        {
            col.size   = new Vector3(0.5f, 1.1f, 0.5f);
            col.center = new Vector3(0f, 0.5f, 0f);
        }

        // Match BAA/DubaiFrame: anchoredPosition y=1 places the canvas 1 unit above the root.
        if (root.transform.childCount > 1)
        {
            var canvasRT = root.transform.GetChild(1).GetComponent<RectTransform>();
            if (canvasRT != null) canvasRT.anchoredPosition = new Vector2(0f, 1f);
        }

        // Save as a brand-new prefab asset
        bool ok;
        PrefabUtility.SaveAsPrefabAsset(root, outPath, out ok);
        PrefabUtility.UnloadPrefabContents(root);

        AssetDatabase.Refresh();
        Debug.Log("[CreateBurjKhalifaPrefab] " + (ok ? "Created " + outPath : "FAILED to save prefab"));
    }
}
