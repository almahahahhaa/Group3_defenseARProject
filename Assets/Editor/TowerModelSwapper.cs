using UnityEngine;
using UnityEditor;

public static class TowerModelSwapper
{
    [MenuItem("Tools/SwapTowerModels")]
    public static void Run()
    {
        SwapTower();
        SwapProjectile();
        WireTurretPivot();
        AssetDatabase.Refresh();
        Debug.Log("TowerModelSwapper: Done.");
    }

    static void WireTurretPivot()
    {
        const string path = "Assets/Prefabs/DefenseTower.prefab";
        GameObject root = PrefabUtility.LoadPrefabContents(path);
        DefenseTower script = root.GetComponent<DefenseTower>();
        Transform turret = root.transform.Find("TowerTurret");
        if (script != null && turret != null)
        {
            script.turretPivot = turret;
            PrefabUtility.SaveAsPrefabAsset(root, path);
            Debug.Log("TowerModelSwapper: turretPivot wired.");
        }
        PrefabUtility.UnloadPrefabContents(root);
    }

    static void SwapTower()
    {
        const string prefabPath  = "Assets/Prefabs/DefenseTower.prefab";
        const string basePath    = "Assets/UnityTechnologies/TowerDefenseTemplate/Models/Towers/Rocket/Base_RocketTower_L01.fbx";
        const string turretPath  = "Assets/UnityTechnologies/TowerDefenseTemplate/Models/Towers/Rocket/Turret_RocketTower_L01.fbx";

        GameObject baseModel   = AssetDatabase.LoadAssetAtPath<GameObject>(basePath);
        GameObject turretModel = AssetDatabase.LoadAssetAtPath<GameObject>(turretPath);
        if (baseModel == null || turretModel == null)
        {
            Debug.LogError("TowerModelSwapper: Could not load rocket tower FBX files.");
            return;
        }

        MeshFilter   baseMF   = baseModel.GetComponentInChildren<MeshFilter>();
        MeshRenderer baseMR   = baseModel.GetComponentInChildren<MeshRenderer>();
        MeshFilter   turretMF = turretModel.GetComponentInChildren<MeshFilter>();
        MeshRenderer turretMR = turretModel.GetComponentInChildren<MeshRenderer>();

        Debug.Log($"TowerModelSwapper: base mesh={baseMF?.sharedMesh?.name}, turret mesh={turretMF?.sharedMesh?.name}");

        GameObject root = PrefabUtility.LoadPrefabContents(prefabPath);

        // Rename TowerBody -> TowerBase and replace with rocket base mesh
        Transform towerBody = root.transform.Find("TowerBody");
        if (towerBody != null)
        {
            towerBody.gameObject.name = "TowerBase";
            MeshFilter mf   = towerBody.GetComponent<MeshFilter>();
            MeshRenderer mr = towerBody.GetComponent<MeshRenderer>();
            if (mf != null && baseMF != null) mf.sharedMesh       = baseMF.sharedMesh;
            if (mr != null && baseMR != null) mr.sharedMaterials   = baseMR.sharedMaterials;
            towerBody.localPosition = Vector3.zero;
            towerBody.localScale    = Vector3.one * 0.15f;

            CapsuleCollider cc = towerBody.GetComponent<CapsuleCollider>();
            if (cc != null) Object.DestroyImmediate(cc);
            BoxCollider bc = towerBody.gameObject.AddComponent<BoxCollider>();
            bc.center = new Vector3(0f, 0.5f, 0f);
            bc.size   = new Vector3(0.5f, 1f, 0.5f);
        }

        // Remove old TowerTurret child if present, then add fresh one
        Transform oldTurret = root.transform.Find("TowerTurret");
        if (oldTurret != null) Object.DestroyImmediate(oldTurret.gameObject);

        GameObject turretGO  = new GameObject("TowerTurret");
        turretGO.transform.SetParent(root.transform, false);
        turretGO.transform.localPosition = Vector3.zero;
        turretGO.transform.localScale    = Vector3.one * 0.15f;
        MeshFilter   tMF = turretGO.AddComponent<MeshFilter>();
        MeshRenderer tMR = turretGO.AddComponent<MeshRenderer>();
        if (turretMF != null) tMF.sharedMesh      = turretMF.sharedMesh;
        if (turretMR != null) tMR.sharedMaterials = turretMR.sharedMaterials;

        // Move FirePoint to the muzzle area of the turret
        // FirePoint: at the muzzle of the turret. Scale 0.15 * ~2 unit model height = ~0.3m.
        // Keep it at the top of the tower so projectiles visually come from the gun barrel.
        Transform fp = root.transform.Find("FirePoint");
        if (fp != null) fp.localPosition = new Vector3(0f, 0.28f, 0.06f);

        PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
        PrefabUtility.UnloadPrefabContents(root);
        Debug.Log("TowerModelSwapper: Tower prefab saved.");
    }

    static void SwapProjectile()
    {
        const string prefabPath = "Assets/Prefabs/DefenseProjectile.prefab";
        const string modelPath  = "Assets/UnityTechnologies/TowerDefenseTemplate/Models/Projectiles/Rocket_Projectile.fbx";

        GameObject projModel = AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);
        if (projModel == null)
        {
            Debug.LogError("TowerModelSwapper: Could not load Rocket_Projectile.fbx.");
            return;
        }

        MeshFilter   projMF = projModel.GetComponentInChildren<MeshFilter>();
        MeshRenderer projMR = projModel.GetComponentInChildren<MeshRenderer>();

        Debug.Log($"TowerModelSwapper: rocket projectile mesh={projMF?.sharedMesh?.name}");

        GameObject root   = PrefabUtility.LoadPrefabContents(prefabPath);
        Transform visual  = root.transform.Find("ProjectileVisual");
        if (visual != null)
        {
            MeshFilter   mf = visual.GetComponent<MeshFilter>();
            MeshRenderer mr = visual.GetComponent<MeshRenderer>();
            if (mf != null && projMF != null) mf.sharedMesh      = projMF.sharedMesh;
            if (mr != null && projMR != null) mr.sharedMaterials = projMR.sharedMaterials;
            // Scale to be clearly visible as a projectile
            visual.localScale    = Vector3.one * 0.03f;
            // Rotate so rocket nose points in +Z (forward travel direction)
            visual.localRotation = Quaternion.Euler(90f, 0f, 0f);
        }

        PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
        PrefabUtility.UnloadPrefabContents(root);
        Debug.Log("TowerModelSwapper: Projectile prefab saved.");
    }
}
