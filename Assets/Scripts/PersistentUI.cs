using UnityEngine;
public class UIRootPersistence : MonoBehaviour
{
    void Awake() { DontDestroyOnLoad(gameObject); }
}
