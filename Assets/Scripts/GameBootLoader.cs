using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameBootLoader : MonoBehaviour
{
    const string GameSceneName = "ARScene";

    IEnumerator Start()
    {
        Time.timeScale = 1f;

        yield return null;
        yield return Resources.UnloadUnusedAssets();
        System.GC.Collect();
        yield return null;

        SceneManager.LoadScene(GameSceneName);
    }
}
