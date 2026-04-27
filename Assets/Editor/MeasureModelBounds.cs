using UnityEngine;
using UnityEditor;

public static class MeasureModelBounds
{
    [MenuItem("Tools/Measure Landmark Bounds")]
    public static void Measure()
    {
        string[] paths = {
            "Assets/WDiiAssets/LowPolyDubaiPack/Models/burjKhalifa.FBX",
            "Assets/WDiiAssets/LowPolyDubaiPack/Models/burjAlArab.FBX",
            "Assets/WDiiAssets/LowPolyDubaiPack/Models/dubaiFrame.FBX"
        };

        foreach (var path in paths)
        {
            var fbx = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (fbx == null) { Debug.Log(path + ": NOT FOUND"); continue; }

            var go = Object.Instantiate(fbx);
            go.transform.localScale = Vector3.one;

            Bounds b = new Bounds();
            bool first = true;
            foreach (var r in go.GetComponentsInChildren<Renderer>())
            {
                if (first) { b = r.bounds; first = false; }
                else b.Encapsulate(r.bounds);
            }

            Debug.Log(System.IO.Path.GetFileName(path)
                + "  size=" + b.size.ToString("F3")
                + "  height=" + b.size.y.ToString("F3"));

            Object.DestroyImmediate(go);
        }
    }
}
