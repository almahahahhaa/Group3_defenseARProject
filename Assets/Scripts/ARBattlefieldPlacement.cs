using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

public class ARPlacementSystem : MonoBehaviour
{
    private ARRaycastManager ARRaycastManager;

    public void Awake()
    {
        ARRaycastManager = FindAnyObjectByType<ARRaycastManager>();

        if (ARRaycastManager == null)
        {
            Debug.LogError("No ARRaycastManager found in the scene.");
        }
        else
        {
            Debug.Log("ARRaycastManager found successfully.");
            ARRaycastManager.enabled = false;
        }
    }
}