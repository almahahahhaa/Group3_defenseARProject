using UnityEngine;

public class HPCanvasFollower : MonoBehaviour
{
    public string targetTag = "Burj_Khalifa";
    public Vector3 offset = new Vector3(0, 0.3f, 0);

    private Transform target;
    private Camera mainCamera;

    void Start()
    {
        mainCamera = Camera.main;
    }

    void LateUpdate()
    {
        // Find target if not found yet (spawned at runtime)
        if (target == null)
        {
            GameObject obj = GameObject.FindGameObjectWithTag(targetTag);
            if (obj != null)
                target = obj.transform;
            return;
        }

        // Follow the tower
        transform.position = target.position + offset;

        // Always face camera
        transform.LookAt(transform.position + mainCamera.transform.rotation * Vector3.forward,
                         mainCamera.transform.rotation * Vector3.up);
    }
}