using UnityEngine;
using UnityEngine.EventSystems;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
#endif

public static class EventSystemBootstrap
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Ensure()
    {
        if (Object.FindObjectOfType<EventSystem>(true)) return;

        var go = new GameObject("[EventSystem]");
        var es = go.AddComponent<EventSystem>();
#if ENABLE_INPUT_SYSTEM
        go.AddComponent<InputSystemUIInputModule>();
#else
        go.AddComponent<StandaloneInputModule>();
#endif
        Object.DontDestroyOnLoad(go);
        go.AddComponent<EventSystemPersistence>(); // unify behavior with the script above
    }
}
