using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.XR.ARFoundation;

public class ARRestartDiagnostics : MonoBehaviour
{
    static bool s_Initialized;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Initialize()
    {
        if (s_Initialized)
        {
            return;
        }

        s_Initialized = true;
        SceneManager.sceneLoaded += OnSceneLoaded;
        ARSession.stateChanged += OnARSessionStateChanged;
        Debug.Log("[ARRestartDiagnostics] Initialized");
    }

    static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Debug.Log($"[ARRestartDiagnostics] sceneLoaded name={scene.name} mode={mode}");
        CreateRunner().StartCoroutine(LogSceneState(scene.name, "sceneLoaded+0.5s", 0.5f));
        CreateRunner().StartCoroutine(LogSceneState(scene.name, "sceneLoaded+1.5s", 1.5f));
    }

    static void OnARSessionStateChanged(ARSessionStateChangedEventArgs args)
    {
        Debug.Log($"[ARRestartDiagnostics] arSessionState={args.state}");
    }

    static ARRestartDiagnostics CreateRunner()
    {
        var go = new GameObject("ARRestartDiagnosticsRunner");
        Object.DontDestroyOnLoad(go);
        return go.AddComponent<ARRestartDiagnostics>();
    }

    static IEnumerator LogSceneState(string sceneName, string label, float delay)
    {
        yield return new WaitForSecondsRealtime(delay);

        Camera[] cameras = Object.FindObjectsByType<Camera>(FindObjectsSortMode.None);
        ARSession session = Object.FindFirstObjectByType<ARSession>();
        ARCameraManager cameraManager = Object.FindFirstObjectByType<ARCameraManager>();
        ARCameraBackground background = Object.FindFirstObjectByType<ARCameraBackground>();
        ARPlaneManager planeManager = Object.FindFirstObjectByType<ARPlaneManager>();

        string camNames = cameras.Length == 0 ? "none" : string.Join(",", System.Array.ConvertAll(cameras, c => $"{c.name}:{c.enabled}:{c.gameObject.activeInHierarchy}:{c.tag}"));

        Debug.Log(
            $"[ARRestartDiagnostics] label={label} scene={sceneName} activeScene={SceneManager.GetActiveScene().name} " +
            $"cams={cameras.Length} [{camNames}] " +
            $"arState={ARSession.state} session={(session != null ? session.enabled.ToString() : "null")} " +
            $"camMgr={(cameraManager != null ? cameraManager.enabled.ToString() : "null")} " +
            $"perm={(cameraManager != null ? cameraManager.permissionGranted.ToString() : "null")} " +
            $"bg={(background != null ? background.enabled.ToString() : "null")} " +
            $"bgRender={(background != null ? background.backgroundRenderingEnabled.ToString() : "null")} " +
            $"planes={(planeManager != null ? planeManager.enabled.ToString() : "null")}");
    }
}
