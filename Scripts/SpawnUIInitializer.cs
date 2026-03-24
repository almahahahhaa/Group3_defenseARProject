using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Samples.StarterAssets;

public class SpawnUIInitializer : MonoBehaviour
{
    [SerializeField] private Burj_KhalifaHPSlider hpSliderUI;

    private ObjectSpawner objectSpawner;

    void Awake()
    {
        objectSpawner = GetComponent<ObjectSpawner>();
    }

    void OnEnable()
    {
        if (objectSpawner != null)
            objectSpawner.objectSpawned += OnObjectSpawned;
    }

    void OnDisable()
    {
        if (objectSpawner != null)
            objectSpawner.objectSpawned -= OnObjectSpawned;
    }

    void OnObjectSpawned(GameObject spawnedObject)
    {
        var health = spawnedObject.GetComponentInChildren<BurjKhalifaHealth>();

        if (health == null)
        {
            Debug.LogError("BurjKhalifaHealth not found on spawned object or its children.");
            return;
        }

        if (hpSliderUI == null)
        {
            Debug.LogError("HP Slider UI not assigned in SpawnUIInitializer.");
            return;
        }

        hpSliderUI.gameObject.SetActive(true);
        hpSliderUI.Initialize(health.maxHP);
    }
}