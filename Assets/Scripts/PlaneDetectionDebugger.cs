using System.Text;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using TMPro;

public class ARPlaneDebugger : MonoBehaviour
{
    [Header("References")]
    public ARPlaneManager planeManager;
    public Camera arCamera;
    public TextMeshProUGUI debugText;

    private StringBuilder builder = new StringBuilder();

    void OnEnable()
    {
        if (planeManager != null)
            planeManager.planesChanged += OnPlanesChanged;
    }

    void OnDisable()
    {
        if (planeManager != null)
            planeManager.planesChanged -= OnPlanesChanged;
    }

    void Update()
    {
        if (planeManager == null || debugText == null)
            return;

        int planeCount = planeManager.trackables.count;

        ARPlane largestPlane = GetLargestPlane();

        builder.Clear();
        builder.AppendLine("AR DEBUG");
        builder.AppendLine("------------------------");
        builder.AppendLine($"Planes Detected: {planeCount}");

        if (largestPlane != null)
        {
            Vector3 camPos = arCamera.transform.position;
            float distance = Vector3.Distance(camPos, largestPlane.center);

            builder.AppendLine($"Largest Plane Size: {largestPlane.size.x:F2} x {largestPlane.size.y:F2}");
            builder.AppendLine($"Plane Alignment: {largestPlane.alignment}");
            builder.AppendLine($"Distance From Camera: {distance:F2}m");
            builder.AppendLine("Surface Ready For Placement");
        }
        else
        {
            builder.AppendLine("Scanning Environment...");
            builder.AppendLine("Move phone slowly over floor or table");
        }

        debugText.text = builder.ToString();
    }

    void OnPlanesChanged(ARPlanesChangedEventArgs args)
    {
        foreach (var plane in args.added)
        {
            Debug.Log($"Plane Added | ID: {plane.trackableId} | Size: {plane.size}");
        }

        foreach (var plane in args.updated)
        {
            Debug.Log($"Plane Updated | ID: {plane.trackableId}");
        }

        foreach (var plane in args.removed)
        {
            Debug.Log("Plane Removed");
        }
    }

    ARPlane GetLargestPlane()
    {
        ARPlane largest = null;
        float maxArea = 0f;

        foreach (var plane in planeManager.trackables)
        {
            float area = plane.size.x * plane.size.y;

            if (area > maxArea)
            {
                maxArea = area;
                largest = plane;
            }
        }

        return largest;
    }
}