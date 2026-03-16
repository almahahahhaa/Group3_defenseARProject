using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

public class ARPlacementSystem : MonoBehaviour
{
    [Header("AR References")]
    public ARRaycastManager raycastManager;
    public ARPlaneManager planeManager;
    public Camera arCamera;

    [Header("Prefabs")]
    public GameObject battlefieldPrefab;
    public GameObject hitMarkerPrefab;

    [Header("Debug Ray")]
    public float rayLength = 5f;
    public float rayWidth = 0.01f;

    private LineRenderer rayRenderer;

    private GameObject spawnedBattlefield;
    private GameObject currentMarker;

    static List<ARRaycastHit> hits = new List<ARRaycastHit>();


    void Start()
    {
        CreateRayRenderer();
    }

    void Update()
    {
        DrawRay();

        if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began)
        {
            TryPlaceBattlefield(Input.GetTouch(0).position);
        }
    }


    void CreateRayRenderer()
    {
        GameObject rayObject = new GameObject("AR_Ray_Debug");
        rayRenderer = rayObject.AddComponent<LineRenderer>();

        rayRenderer.material = new Material(Shader.Find("Unlit/Color"));
        rayRenderer.material.color = Color.red;

        rayRenderer.startWidth = rayWidth;
        rayRenderer.endWidth = rayWidth;

        rayRenderer.positionCount = 2;
    }


    void DrawRay()
    {
        Ray ray = new Ray(arCamera.transform.position, arCamera.transform.forward);

        rayRenderer.SetPosition(0, ray.origin);
        rayRenderer.SetPosition(1, ray.origin + ray.direction * rayLength);
    }


    void TryPlaceBattlefield(Vector2 screenPosition)
    {
        if (spawnedBattlefield != null)
            return;

        bool hit = raycastManager.Raycast(screenPosition, hits, TrackableType.PlaneWithinPolygon);

        if (!hit)
        {
            Debug.Log("No AR plane hit.");
            return;
        }

        Pose hitPose = hits[0].pose;

        Debug.Log("Plane hit at: " + hitPose.position);

        SpawnHitMarker(hitPose.position);

        spawnedBattlefield = Instantiate(
            battlefieldPrefab,
            hitPose.position,
            hitPose.rotation
        );

        StopPlaneDetection();
    }


    void SpawnHitMarker(Vector3 position)
    {
        if (hitMarkerPrefab == null)
            return;

        if (currentMarker != null)
            Destroy(currentMarker);

        currentMarker = Instantiate(hitMarkerPrefab, position, Quaternion.identity);
    }


    void StopPlaneDetection()
    {
        planeManager.enabled = false;

        foreach (var plane in planeManager.trackables)
        {
            plane.gameObject.SetActive(false);
        }
    }
}