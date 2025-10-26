using UnityEngine;
using UnityEngine.SceneManagement;

public class Gateway : MonoBehaviour
{
    [Header("Trigger Settings")]
    [SerializeField] private string playerTag = "Player";
    [SerializeField, Min(0.1f)] private float requiredStayTime = 1.5f;

    [Header("Scene To Load")]
    [SerializeField] private string sceneName = "";   // Fallback used at runtime

#if UNITY_EDITOR
    // Assign a SceneAsset in the Inspector; we copy its name into sceneName.
    [SerializeField] private UnityEditor.SceneAsset sceneAsset;
    private void OnValidate()
    {
        if (sceneAsset != null)
            sceneName = sceneAsset.name;
    }
#endif

    [Header("Optional: UI Fill (0..1)")]
    [SerializeField] private UnityEngine.UI.Image progressFill; // leave empty if not using UI

    private float timer;
    private bool playerInside;
    private bool loading;

    private void Reset()
    {
        // Ensure we behave like a trigger in 2D.
        var col = GetComponent<Collider2D>();
        if (col == null) col = gameObject.AddComponent<BoxCollider2D>();
        col.isTrigger = true;

        // Having any Rigidbody2D on either object helps consistent trigger messages.
        var rb = GetComponent<Rigidbody2D>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Kinematic;
        }
    }

    private void Update()
    {
        if (loading || string.IsNullOrEmpty(sceneName)) return;

        if (playerInside)
        {
            timer += Time.deltaTime;
            if (progressFill) progressFill.fillAmount = Mathf.Clamp01(timer / requiredStayTime);

            if (timer >= requiredStayTime)
            {
                loading = true;
                if (progressFill) progressFill.fillAmount = 1f;
                // Async load to avoid a hitch.
                SceneManager.LoadSceneAsync(sceneName);
            }
        }
        else
        {
            if (timer > 0f) timer = 0f;
            if (progressFill) progressFill.fillAmount = 0f;
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag(playerTag))
            playerInside = true;
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag(playerTag))
        {
            playerInside = false;
            timer = 0f;
        }
    }
}
