using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using TMPro;

public class PlaneDetectionDebugger : MonoBehaviour
{
    public ARPlaneManager planeManager;
    public TextMeshProUGUI debugText;

    void OnEnable()
    {
        planeManager.planesChanged += OnPlanesChanged;
    }

    void OnDisable()
    {
        planeManager.planesChanged -= OnPlanesChanged;
    }

    void Update()
    {
        int planeCount = planeManager.trackables.count;

        debugText.text =
            "AR DEBUG\n" +
            "Planes Detected: " + planeCount + "\n" +
            "Move phone slowly over floor or table";
    }

    void OnPlanesChanged(ARPlanesChangedEventArgs args)
    {
        foreach (var plane in args.added)
        {
            Debug.Log("New Plane Detected at: " + plane.center);
        }

        foreach (var plane in args.updated)
        {
            Debug.Log("Plane Updated: " + plane.trackableId);
        }

        foreach (var plane in args.removed)
        {
            Debug.Log("Plane Removed");
        }
    }
}