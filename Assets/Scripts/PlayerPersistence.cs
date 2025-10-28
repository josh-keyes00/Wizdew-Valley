using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

[DefaultExecutionOrder(-1000)]
public class PlayerPersistence : MonoBehaviour
{
    public static PlayerPersistence Instance;

    [Header("Spawn")]
    [Tooltip("If true, log spawn decisions (helpful while wiring scenes).")]
    public bool debugLogs = true;
    [Tooltip("Try to find the spawn point for up to this many frames after a scene load.")]
    public int spawnSearchFrames = 15; // ~0.25s at 60fps

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            if (debugLogs) Debug.Log("[PlayerPersistence] Duplicate player found. Destroying this one.");
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            Instance = null;
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // kick off a small coroutine to let the scene finish spawning objects
        StartCoroutine(WarpToSpawnRoutine(TransitionData.NextSpawnId));
    }

    private IEnumerator WarpToSpawnRoutine(string requestedId)
    {
        // Clear once consumed so we don’t reuse it accidentally next transition
        TransitionData.NextSpawnId = null;

        // Wait one frame so new scene’s objects are awake
        yield return null;

        SpawnPoint found = null;
        // Try for a handful of frames in case SpawnPoints are created by scripts
        for (int i = 0; i < Mathf.Max(1, spawnSearchFrames) && found == null; i++)
        {
            found = FindSpawnById(requestedId);
            if (!found && i < spawnSearchFrames - 1)
                yield return null;
        }

        if (found == null)
        {
            // Fallbacks: a SpawnPoint named "Default", then first SpawnPoint in the scene
            found = FindSpawnById("Default") ?? FindAnySpawn();
        }

        if (found != null)
        {
            if (debugLogs)
            {
                Debug.Log($"[PlayerPersistence] Spawning at '{found.id}' ({found.transform.position})");
            }

            transform.position = found.transform.position;

            var rb2d = GetComponent<Rigidbody2D>();
            if (rb2d)
            {
                rb2d.linearVelocity = Vector2.zero;
                rb2d.angularVelocity = 0f;
            }
        }
        else
        {
            if (debugLogs)
            {
                Debug.LogWarning("[PlayerPersistence] No SpawnPoint found. Leaving player at previous position.");
            }
        }
    }

    private SpawnPoint FindSpawnById(string id)
    {
        if (string.IsNullOrEmpty(id)) return null;

        foreach (var sp in EnumerateSceneSpawnPoints())
        {
            if (sp != null && sp.id == id) return sp;
        }
        return null;
    }

    private SpawnPoint FindAnySpawn()
    {
        foreach (var sp in EnumerateSceneSpawnPoints())
            if (sp != null) return sp;
        return null;
    }

    // Enumerate SpawnPoints in the currently loaded scene(s), including inactive ones.
    private IEnumerable<SpawnPoint> EnumerateSceneSpawnPoints()
    {
#if UNITY_2020_1_OR_NEWER
        var list = Object.FindObjectsOfType<SpawnPoint>(true); // includeInactive
        foreach (var sp in list)
        {
            // Filter out prefab assets (not in a loaded scene)
            if (sp.gameObject.scene.IsValid() && sp.gameObject.scene.isLoaded)
                yield return sp;
        }
#else
        // Older Unity fallback
        var list = Resources.FindObjectsOfTypeAll<SpawnPoint>();
        foreach (var sp in list)
        {
            if ((sp.hideFlags & HideFlags.HideInHierarchy) == 0 &&
                sp.gameObject.scene.IsValid() && sp.gameObject.scene.isLoaded)
                yield return sp;
        }
#endif
    }
}
