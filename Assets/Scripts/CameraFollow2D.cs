using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(Camera))]
public class CameraFollow2D : MonoBehaviour
{
    [Header("Target Acquisition")]
    public string playerTag = "Player";
    public bool autoFindOnStart = true;
    public bool autoFindOnSceneLoad = true;

    [Header("Motion")]
    [Min(0f)] public float smooth = 10f;
    [Tooltip("Hard-lock the camera Z so 2D sprites remain in view.")]
    public bool lockZ = true;
    public float fixedZ = -10f;

    [Header("Camera Setup")]
    public bool forceOrthographic = true;
    public float orthographicSize = 5f;

    private Transform target;
    private Camera cam;
    private float searchTimer;
    public float searchInterval = 0.5f;

    void Awake()
    {
        cam = GetComponent<Camera>();
        if (forceOrthographic) { cam.orthographic = true; cam.orthographicSize = orthographicSize; }

        if (lockZ)
        {
            var p = transform.position;
            p.z = fixedZ;
            transform.position = p;
        }

        if (autoFindOnStart) FindTarget();

        if (autoFindOnSceneLoad)
            SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDestroy()
    {
        if (autoFindOnSceneLoad)
            SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void OnSceneLoaded(Scene s, LoadSceneMode m)
    {
        // reacquire after spawn/scene switch
        searchTimer = 0f;
        FindTarget();
    }

    void Update()
    {
        if (target == null)
        {
            searchTimer -= Time.unscaledDeltaTime;
            if (searchTimer <= 0f)
            {
                searchTimer = searchInterval;
                FindTarget();
            }
        }
    }

    void LateUpdate()
    {
        if (!target) return;

        var pos = transform.position;
        var goal = new Vector3(target.position.x, target.position.y, lockZ ? fixedZ : pos.z);

        // framerate-independent smoothing
        transform.position = Vector3.Lerp(pos, goal, 1f - Mathf.Exp(-smooth * Time.deltaTime));
    }

    public void SetTarget(Transform t) => target = t;

    void FindTarget()
    {
        var go = GameObject.FindWithTag(playerTag);
        if (go) { target = go.transform; return; }

        // Fallbacks if tag isn’t set on the player for some reason
        var wiz = FindObjectOfType<WizardController>(true);
        if (wiz) { target = wiz.transform; return; }

        var pp = FindObjectOfType<PlayerPersistence>(true);
        if (pp) { target = pp.transform; return; }
    }
}
