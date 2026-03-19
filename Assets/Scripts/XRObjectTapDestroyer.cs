using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

public class XRObjectTapDestroyer : MonoBehaviour
{
    public XRRayInteractor rayInteractor;

    private bool haveRayInteractor = false;
    public void Awake()
    {
        if (rayInteractor == null)
        {
            rayInteractor = FindAnyObjectByType<XRRayInteractor>();
            if (rayInteractor == null)
            {
                Debug.LogError("XRRayInteractor component not found on the GameObject. Please assign it in the inspector.");
            }
            haveRayInteractor= rayInteractor != null;
        }
    }
    void Update()
    {
        if (!haveRayInteractor) return;

        // Check if user tapped (same trigger used for spawn)
        if (rayInteractor.logicalSelectState.wasPerformedThisFrame)
        {
            if (rayInteractor.TryGetCurrent3DRaycastHit(out RaycastHit hit))
            {
                Debug.Log("Hit Object: " + hit.transform.name);

                // Check if it's an enemy
                if (hit.transform.CompareTag("Enemy"))
                {
                    Debug.Log("Enemy Destroyed via XR");

                    Destroy(hit.transform.gameObject);
                }
            }
        }
    }
}