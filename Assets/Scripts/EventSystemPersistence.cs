using UnityEngine;
using UnityEngine.EventSystems;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;  // New Input System
#endif

[DefaultExecutionOrder(-1001)]
public class EventSystemPersistence : MonoBehaviour
{
    private static EventSystemPersistence _instance;

    void Awake()
    {
        // EventSystem must be a ROOT object for DontDestroyOnLoad to work
        if (transform.parent != null)
        {
            Debug.LogWarning("[EventSystemPersistence] Moving EventSystem to scene root.");
            transform.SetParent(null, worldPositionStays: false);
        }

        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);   // kill duplicates when loading new scenes
            return;
        }
        _instance = this;

        // Ensure an EventSystem component exists
        var es = GetComponent<EventSystem>() ?? gameObject.AddComponent<EventSystem>();

        // Ensure the correct input module is present (favor the new Input System)
#if ENABLE_INPUT_SYSTEM
        if (!GetComponent<InputSystemUIInputModule>())
        {
            var legacy = GetComponent<StandaloneInputModule>();
            if (legacy) Destroy(legacy);
            gameObject.AddComponent<InputSystemUIInputModule>();
        }
#else
        if (!GetComponent<StandaloneInputModule>())
            gameObject.AddComponent<StandaloneInputModule>();
#endif

        DontDestroyOnLoad(gameObject);
    }
}
