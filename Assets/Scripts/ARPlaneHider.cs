using UnityEngine;
using UnityEngine.XR.ARFoundation;

public class ARPlaneHider : MonoBehaviour
{
    private ARPlaneManager arPlaneManager;

    void Awake()
    {
        arPlaneManager = FindAnyObjectByType<ARPlaneManager>();
    }

    public void HidePlanes()
    {
        if (arPlaneManager == null) return;

        foreach (var plane in arPlaneManager.trackables)
            plane.gameObject.SetActive(false);

        arPlaneManager.enabled = false;
    }
}