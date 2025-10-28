using UnityEngine;
using UnityEngine.SceneManagement;

public class Gateway : MonoBehaviour
{
    [Header("Trigger Settings")]
    [SerializeField] private string playerTag = "Player";
    [SerializeField, Min(0.05f)] private float requiredStayTime = 1.0f;

    [Header("Destination")]
    [SerializeField] private string sceneName = "";
    [SerializeField] private string spawnId = "Default";

#if UNITY_EDITOR
    [SerializeField] private UnityEditor.SceneAsset sceneAsset;
    private void OnValidate()
    {
        if (sceneAsset != null) sceneName = sceneAsset.name;
    }
#endif

    [Header("Optional: UI Fill (0..1)")]
    [SerializeField] private UnityEngine.UI.Image progressFill;

    float timer;
    bool playerInside;
    bool loading;

    void Update()
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

                // Set the target spawn **before** loading
                TransitionData.NextSpawnId = spawnId;

                SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);
            }
        }
        else
        {
            if (timer > 0f) timer = 0f;
            if (progressFill) progressFill.fillAmount = 0f;
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag(playerTag)) playerInside = true;
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag(playerTag))
        {
            playerInside = false;
            timer = 0f;
        }
    }
}
