using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

public class ARPlacementSystem : MonoBehaviour
{
    private ARRaycastManager arRaycastManager;
    private ARPlaneManager arPlaneManager;  // Add this

    public void Awake()
    {
        arRaycastManager = FindAnyObjectByType<ARRaycastManager>();
        arPlaneManager = FindAnyObjectByType<ARPlaneManager>();  // Add this
    }

    // Call this method after the player confirms placement
    public void HidePlanesAfterPlacement()
    {
        if (arPlaneManager == null) return;

        // Hide all existing plane visuals
        foreach (var plane in arPlaneManager.trackables)
        {
            plane.gameObject.SetActive(false);
        }

        // Disable the manager so no new planes are detected/shown
        arPlaneManager.enabled = false;
    }
}