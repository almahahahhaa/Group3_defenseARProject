using UnityEngine;

public class XRTargetMover : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 0.2f;

    [Header("Target")]
    public string targetTag = "Burj_Khalifa";

    private Transform target;
    private Camera arCamera;

    void Start()
    {
        arCamera = Camera.main;
        FindTarget();
    }

    void Update()
    {
        MoveTowardsTarget();
        HandleTouch();
    }

    void FindTarget()
    {
        GameObject obj = GameObject.FindGameObjectWithTag(targetTag);

        if (obj != null)
            target = obj.transform;
        else
            Debug.LogWarning("Target not found: " + targetTag);
    }

    void MoveTowardsTarget()
    {
        if (target == null) return;

        Vector3 dir = (target.position - transform.position).normalized;
        transform.position += dir * moveSpeed * Time.deltaTime;

        transform.LookAt(target);
    }

    void HandleTouch()
    {
        if (Input.touchCount == 0) return;

        Touch touch = Input.GetTouch(0);

        if (touch.phase != TouchPhase.Began) return;

        Ray ray = arCamera.ScreenPointToRay(touch.position);

        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            if (hit.transform == transform)
            {
                Debug.Log("Enemy Destroyed");

                Destroy(gameObject);
            }
        }
    }
}