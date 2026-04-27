using UnityEngine;
using UnityEditor;

public static class OilSlickPrefabCreator
{
    [MenuItem("Tools/Create Oil Slick Prefabs")]
    public static void CreatePrefabs()
    {
        CreateOilSlick();
        CreateMiniOilSlick();
        WireUpBattlefieldPlatform();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[OilSlickPrefabCreator] Done — prefabs created and BattlefieldPlatform wired.");
    }

    static GameObject CreateOilSlick()
    {
        string modelPath = AssetDatabase.GUIDToAssetPath("75bbfccc509428f43ba5c41435c16a78");
        GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);
        if (model == null) { Debug.LogError("Oil_Slick.glb not found at " + modelPath); return null; }

        GameObject go = (GameObject)PrefabUtility.InstantiatePrefab(model);
        go.name = "OilSlick";
        go.transform.localScale = new Vector3(0.05f, 0.05f, 0.05f);
        go.tag = "Enemy";

        var mover = go.AddComponent<EnemyMoveToTarget>();
        mover.damageOnHit    = 10;
        mover.moveSpeed      = 0.05f;
        mover.damageDistance = 0.15f;

        go.AddComponent<OilSlickTapHandler>();

        var col = go.AddComponent<BoxCollider>();
        col.isTrigger = true;

        GameObject saved = PrefabUtility.SaveAsPrefabAsset(go, "Assets/Prefabs/OilSlick.prefab");
        Object.DestroyImmediate(go);
        Debug.Log("Created: Assets/Prefabs/OilSlick.prefab");
        return saved;
    }

    static GameObject CreateMiniOilSlick()
    {
        string modelPath = AssetDatabase.GUIDToAssetPath("a3d796f0eb699e24c8acc5b2bd7adcf2");
        GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);
        if (model == null) { Debug.LogError("Mini_Oil_Slick.glb not found at " + modelPath); return null; }

        GameObject go = (GameObject)PrefabUtility.InstantiatePrefab(model);
        go.name = "MiniOilSlick";
        go.transform.localScale = new Vector3(0.03f, 0.03f, 0.03f);
        go.tag = "Enemy";

        var mover = go.AddComponent<EnemyMoveToTarget>();
        mover.damageOnHit    = 5;
        mover.moveSpeed      = 0.05f;
        mover.damageDistance = 0.15f;

        go.AddComponent<EnemyTapDestroyer>();

        var col = go.AddComponent<BoxCollider>();
        col.isTrigger = true;

        GameObject saved = PrefabUtility.SaveAsPrefabAsset(go, "Assets/Prefabs/MiniOilSlick.prefab");
        Object.DestroyImmediate(go);
        Debug.Log("Created: Assets/Prefabs/MiniOilSlick.prefab");
        return saved;
    }

    static void WireUpBattlefieldPlatform()
    {
        string bfPath = "Assets/Prefabs/BattlefieldPlatform.prefab";
        GameObject bfPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(bfPath);
        if (bfPrefab == null) { Debug.LogError("BattlefieldPlatform.prefab not found."); return; }

        GameObject oilSlick = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/OilSlick.prefab");
        GameObject miniSlick = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/MiniOilSlick.prefab");

        using (var scope = new PrefabUtility.EditPrefabContentsScope(bfPath))
        {
            var root    = scope.prefabContentsRoot;
            var spawner = root.GetComponentInChildren<EnemySpawner>();
            if (spawner == null) { Debug.LogError("EnemySpawner not found in BattlefieldPlatform."); return; }

            spawner.oilSlickPrefab     = oilSlick;
            spawner.miniOilSlickPrefab = miniSlick;
            Debug.Log("Wired oilSlickPrefab and miniOilSlickPrefab on EnemySpawner.");
        }
    }
}
