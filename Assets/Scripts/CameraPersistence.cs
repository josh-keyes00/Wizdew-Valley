using UnityEngine;
using UnityEngine.SceneManagement;

[DefaultExecutionOrder(-1001)]
public class CameraPersistence : MonoBehaviour
{
    public static CameraPersistence Instance;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDestroy()
    {
        if (Instance == this) SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void OnSceneLoaded(Scene s, LoadSceneMode m)
    {
        // Disable any other active cameras so they can’t overwrite the frame
        var cams = FindObjectsOfType<Camera>(true);
        foreach (var c in cams)
        {
            if (c.gameObject != this.gameObject)
                c.enabled = false;
        }
    }
}
