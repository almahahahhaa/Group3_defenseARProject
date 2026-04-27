using UnityEngine;

// Attach to an HP canvas that is a CHILD of a landmark prefab.
// Handles only camera-facing; position tracking is automatic via parent transform.
public class LandmarkHPCanvasFollower : MonoBehaviour
{
    private Camera mainCamera;

    void Start()
    {
        mainCamera = Camera.main;
    }

    void LateUpdate()
    {
        if (mainCamera == null) return;
        transform.LookAt(
            transform.position + mainCamera.transform.rotation * Vector3.forward,
            mainCamera.transform.rotation * Vector3.up);
    }
}
