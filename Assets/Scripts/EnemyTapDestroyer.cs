using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;

public class EnemyTapDestroyer : MonoBehaviour
{
    private Camera arCamera;

    void OnEnable()
    {
        EnhancedTouchSupport.Enable();
    }

    void OnDisable()
    {
        EnhancedTouchSupport.Disable();
    }

    void Start()
    {
        arCamera = Camera.main;
    }

    void Update()
    {
        // Handle touch on mobile
        foreach (var touch in Touch.activeTouches)
        {
            if (touch.phase == UnityEngine.InputSystem.TouchPhase.Began)
            {
                TryHit(touch.screenPosition);
            }
        }

        // Handle mouse click in editor (for testing)
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            TryHit(Mouse.current.position.ReadValue());
        }
    }

    void TryHit(Vector2 screenPosition)
    {
        if (arCamera == null) return;

        Ray ray = arCamera.ScreenPointToRay(screenPosition);

        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            if (hit.transform == this.transform ||
                hit.transform.IsChildOf(this.transform))
            {
                Debug.Log("Enemy tapped: " + gameObject.name);
                Destroy(gameObject);
            }
        }
    }
}