using UnityEngine;
using UnityEngine.SceneManagement;

[DefaultExecutionOrder(-1002)]
public class UIRootSingleton : MonoBehaviour
{
    private static UIRootSingleton _instance;

    [Tooltip("If Screen Space - Camera, we will rebind worldCamera to Camera.main on each scene load.")]
    public bool rebindCanvasCameraOnLoad = true;

    private Canvas _canvas;

    void Awake()
    {
        // UI must be a root object for DontDestroyOnLoad
        if (transform.parent != null)
        {
            Debug.LogWarning("[UIRootSingleton] Moving UI to scene root for persistence.");
            transform.SetParent(null, false);
        }

        if (_instance != null && _instance != this)
        {
            // Another UI already persisted → kill this new duplicate
            Destroy(gameObject);
            return;
        }

        _instance = this;
        DontDestroyOnLoad(gameObject);

        _canvas = GetComponent<Canvas>();

        if (rebindCanvasCameraOnLoad)
            SceneManager.sceneLoaded += OnSceneLoaded;

        // initial bind (in case the first scene’s MainCamera wasn’t assigned)
        RebindCameraIfNeeded();
    }

    void OnDestroy()
    {
        if (_instance == this)
        {
            if (rebindCanvasCameraOnLoad)
                SceneManager.sceneLoaded -= OnSceneLoaded;
            _instance = null;
        }
    }

    private void OnSceneLoaded(Scene s, LoadSceneMode m)
    {
        RebindCameraIfNeeded();
    }

    private void RebindCameraIfNeeded()
    {
        if (_canvas && _canvas.renderMode == RenderMode.ScreenSpaceCamera)
        {
            if (_canvas.worldCamera == null || _canvas.worldCamera != Camera.main)
                _canvas.worldCamera = Camera.main;
        }
    }
}
