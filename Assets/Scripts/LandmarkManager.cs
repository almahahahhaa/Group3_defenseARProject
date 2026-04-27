using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.Interaction.Toolkit.Samples.StarterAssets;
using UnityEngine.XR.Interaction.Toolkit.Samples.ARStarterAssets;
using TMPro;

public class LandmarkManager : MonoBehaviour
{
    public static LandmarkManager Instance;

    [Header("AR Spawner")]
    [SerializeField] private ObjectSpawner objectSpawner;

    [Header("Placement UI")]
    [SerializeField] private GameObject instructionPanel;
    [SerializeField] private TextMeshProUGUI instructionText;

    private readonly string[] placementMessages =
    {
        "Place Burj Khalifa",
        "Place Burj Al Arab",
        "Place Dubai Frame"
    };

    private int placementStep = 0;
    private readonly List<GameObject> placedLandmarks = new List<GameObject>();

    void Awake()
    {
        Instance = this;
        // Auto-find if not wired in the Inspector
        if (objectSpawner == null)
            objectSpawner = FindFirstObjectByType<ObjectSpawner>();
        if (objectSpawner == null)
            Debug.LogError("[LandmarkManager] No ObjectSpawner found in scene!");
    }

    void OnEnable()
    {
        if (objectSpawner != null)
            objectSpawner.objectSpawned += OnLandmarkPlaced;
    }

    void OnDisable()
    {
        if (objectSpawner != null)
            objectSpawner.objectSpawned -= OnLandmarkPlaced;
    }

    IEnumerator Start()
    {
        GameSessionFlow.ResetARStateInScene();

        if (objectSpawner != null)
        {
            objectSpawner.spawnOptionIndex = 0;
            objectSpawner.isFieldSpawner   = false;
        }

        if (instructionText != null)
        {
            instructionText.fontSize = 34f;
            instructionText.fontStyle = FontStyles.Bold;
            instructionText.alignment = TextAlignmentOptions.Center;
        }

        ShowInstruction();

        yield return StartCoroutine(GameSessionFlow.EnsureCameraBackgroundReady());
    }

    // Tags must match entries in TagManager (Burj_Khalifa, Burj_Al_Arab, Dubai_Frame)
    private static readonly string[] LandmarkTags = { "Burj_Khalifa", "Burj_Al_Arab", "Dubai_Frame" };

    void OnLandmarkPlaced(GameObject spawnedObject)
    {
        // Assign tag and tap handler before incrementing step
        int index = placedLandmarks.Count; // 0=BK, 1=BAA, 2=DF
        if (index < LandmarkTags.Length)
        {
            try { spawnedObject.tag = LandmarkTags[index]; }
            catch { /* tag not registered — skip */ }
        }

        // Add the tap handler to the root's collider. All landmark prefabs have an enabled
        // collider directly on their root GameObject.
        var col = spawnedObject.GetComponent<Collider>();
        if (col == null) col = spawnedObject.GetComponentInChildren<Collider>();
        GameObject tapTarget = col != null ? col.gameObject : spawnedObject;
        var tapHandler = tapTarget.GetComponent<LandmarkTapHandler>();
        if (tapHandler == null) tapHandler = tapTarget.AddComponent<LandmarkTapHandler>();
        tapHandler.landmarkRoot = spawnedObject.transform;

        placedLandmarks.Add(spawnedObject);
        placementStep++;

        if (placementStep < placementMessages.Length)
        {
            if (objectSpawner != null)
                objectSpawner.spawnOptionIndex = placementStep;

            // IMPORTANT: ObjectSpawner.TrySpawnObject sets isFieldSpawner = true
            // on the line immediately AFTER firing objectSpawned, so any reset
            // we do inside this handler gets stomped. Defer to the next frame.
            StartCoroutine(UnlockSpawnerNextFrame());

            ShowInstruction();
        }
        else
        {
            if (instructionPanel != null)
                instructionPanel.SetActive(false);

            if (objectSpawner != null)
            {
                objectSpawner.enabled = false;
                // Prevent stray "Could not spawn object" warnings during gameplay
                var spawnTrigger = objectSpawner.GetComponent<ARInteractorSpawnTrigger>();
                if (spawnTrigger != null) spawnTrigger.enabled = false;
            }

            HidePlanes();

            if (GameHUD.Instance != null)
                GameHUD.Instance.ShowHUD();

            if (FactCardSystem.Instance != null)
                FactCardSystem.Instance.TriggerForWave(1);
            else if (EnemySpawner.Instance != null)
                EnemySpawner.Instance.StartNextWave();
            else
                Debug.LogError("[LandmarkManager] EnemySpawner.Instance is null — waves won't start.");
        }
    }

    void ShowInstruction()
    {
        if (instructionPanel != null)
            instructionPanel.SetActive(true);
        if (instructionText != null)
            instructionText.text = placementMessages[placementStep];
    }

    // Waits one frame so TrySpawnObject finishes setting isFieldSpawner = true,
    // then resets it and restores AR planes for the next placement.
    IEnumerator UnlockSpawnerNextFrame()
    {
        yield return null;

        if (objectSpawner != null)
            objectSpawner.isFieldSpawner = false;

        ARPlaneManager pm = FindFirstObjectByType<ARPlaneManager>();
        if (pm == null) yield break;
        pm.enabled = true;
        foreach (var plane in pm.trackables)
            plane.gameObject.SetActive(true);
    }

    void HidePlanes()
    {
        ARPlaneHider hider = FindFirstObjectByType<ARPlaneHider>();
        if (hider != null) { hider.HidePlanes(); return; }

        ARPlaneManager pm = FindFirstObjectByType<ARPlaneManager>();
        if (pm == null) return;
        foreach (var plane in pm.trackables)
            plane.gameObject.SetActive(false);
        pm.enabled = false;
    }

    public GameObject GetRandomLandmark()
    {
        if (placedLandmarks.Count == 0) return null;
        return placedLandmarks[Random.Range(0, placedLandmarks.Count)];
    }

    public List<GameObject> GetAllLandmarks() => placedLandmarks;
}
