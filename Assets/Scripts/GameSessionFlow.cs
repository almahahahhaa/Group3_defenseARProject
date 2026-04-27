using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.Interaction.Toolkit.Samples.ARStarterAssets;
using UnityEngine.XR.Interaction.Toolkit.Samples.StarterAssets;
using System.Collections;

public static class GameSessionFlow
{
    public static void PrepareForGameplayStart()
    {
        Time.timeScale = 1f;
        PowerupManager.Instance?.ResetForNewSession();
    }

    public static void PrepareForMainMenuReturn()
    {
        Time.timeScale = 1f;
        PowerupManager.Instance?.ResetForNewSession();
        ShutdownARStateInScene();
    }

    public static void ResetARStateInScene()
    {
        ARSession session = Object.FindFirstObjectByType<ARSession>();
        if (session != null)
        {
            session.enabled = true;
        }

        ARPlaneManager planeManager = Object.FindFirstObjectByType<ARPlaneManager>();
        if (planeManager != null)
        {
            planeManager.enabled = true;
            foreach (ARPlane plane in planeManager.trackables)
            {
                if (plane != null)
                {
                    plane.gameObject.SetActive(true);
                }
            }
        }

        ObjectSpawner objectSpawner = Object.FindFirstObjectByType<ObjectSpawner>();
        if (objectSpawner != null)
        {
            objectSpawner.enabled = true;
            objectSpawner.spawnOptionIndex = 0;
            objectSpawner.isFieldSpawner = false;

            ARInteractorSpawnTrigger spawnTrigger = objectSpawner.GetComponent<ARInteractorSpawnTrigger>();
            if (spawnTrigger != null)
            {
                spawnTrigger.enabled = true;
            }
        }
    }

    public static IEnumerator EnsureCameraBackgroundReady()
    {
        ARCameraBackground background = Object.FindFirstObjectByType<ARCameraBackground>();
        ARCameraManager cameraManager = Object.FindFirstObjectByType<ARCameraManager>();

        if (background == null || cameraManager == null)
        {
            yield break;
        }

        float timeout = Time.realtimeSinceStartup + 1.5f;
        while (Time.realtimeSinceStartup < timeout)
        {
            if (background.backgroundRenderingEnabled)
            {
                yield break;
            }

            yield return null;
        }

        background.enabled = false;
        cameraManager.enabled = false;
        yield return null;
        cameraManager.enabled = true;
        background.enabled = true;

        timeout = Time.realtimeSinceStartup + 1.0f;
        while (Time.realtimeSinceStartup < timeout)
        {
            if (background.backgroundRenderingEnabled)
            {
                yield break;
            }

            yield return null;
        }

        ARSession session = Object.FindFirstObjectByType<ARSession>();
        if (session != null)
        {
            session.Reset();
        }
    }

    static void ShutdownARStateInScene()
    {
        ARCameraBackground background = Object.FindFirstObjectByType<ARCameraBackground>();
        if (background != null)
        {
            background.enabled = false;
        }

        ARCameraManager cameraManager = Object.FindFirstObjectByType<ARCameraManager>();
        if (cameraManager != null)
        {
            cameraManager.enabled = false;
        }

        ARPlaneManager planeManager = Object.FindFirstObjectByType<ARPlaneManager>();
        if (planeManager != null)
        {
            planeManager.enabled = false;
            foreach (ARPlane plane in planeManager.trackables)
            {
                if (plane != null)
                {
                    plane.gameObject.SetActive(false);
                }
            }
        }

        ObjectSpawner objectSpawner = Object.FindFirstObjectByType<ObjectSpawner>();
        if (objectSpawner != null)
        {
            objectSpawner.enabled = false;

            ARInteractorSpawnTrigger spawnTrigger = objectSpawner.GetComponent<ARInteractorSpawnTrigger>();
            if (spawnTrigger != null)
            {
                spawnTrigger.enabled = false;
            }
        }

        ARSession session = Object.FindFirstObjectByType<ARSession>();
        if (session != null)
        {
            session.enabled = false;
        }
    }

}
