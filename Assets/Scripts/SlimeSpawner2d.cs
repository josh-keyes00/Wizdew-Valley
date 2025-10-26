using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

[DisallowMultipleComponent]
[RequireComponent(typeof(Collider2D))]
public class SlimeSpawner2D : MonoBehaviour
{
    [Header("Spawn Settings")]
    [SerializeField] private GameObject slimePrefab;
    [SerializeField, Tooltip("Rectangle the slimes spawn in.")]
    private Vector2 areaSize = new Vector2(6f, 4f);

    [SerializeField, Tooltip("If true, the spawn area ignores this object's rotation/scale (world-aligned).")]
    private bool areaInWorldSpace = false;

    [SerializeField, Tooltip("Seconds between waves: random between X..Y.")]
    private Vector2 cooldownRange = new Vector2(5f, 9f);

    [SerializeField, Tooltip("Slimes per wave: random inclusive between Min..Max.")]
    private Vector2Int spawnCountRange = new Vector2Int(1, 3);

    [SerializeField, Tooltip("Cap the number of spawned slimes alive at once.")]
    private int maxAlive = 20;

    [SerializeField, Tooltip("Delay before first wave.")]
    private float startDelay = 0f;

    [Header("Placement Safety")]
    [SerializeField, Tooltip("Clear radius around spawn point that must be free of colliders.")]
    private float spawnClearRadius = 0.35f;

    [SerializeField, Tooltip("Layers considered blocking for spawn clearance.")]
    private LayerMask preventSpawnMask = ~0;

    [SerializeField, Tooltip("Attempts to find a clear point per slime.")]
    private int maxPointAttempts = 16;

    [Header("Parenting (optional)")]
    [SerializeField, Tooltip("If true, parent spawned slimes (but keep world pose).")]
    private bool parentSpawned = false;
    [SerializeField, Tooltip("Parent to use if parentSpawned is true; if null, uses this spawner transform.")]
    private Transform spawnedParentOverride;

    [Header("Health")]
    [SerializeField] private float maxHealth = 10f;
    public UnityEvent onDeath;

    [Header("Gizmos")]
    [SerializeField] private Color areaColor = new Color(0f, 1f, 0.6f, 0.18f);
    [SerializeField] private Color areaOutline = new Color(0f, 1f, 0.6f, 0.9f);

    private float _hp;
    private Coroutine _runner;
    private Collider2D _col;

    // We track alive slimes without parenting
    private readonly List<GameObject> _alive = new List<GameObject>();

    void Awake()
    {
        _hp = Mathf.Max(1f, maxHealth);
        _col = GetComponent<Collider2D>();
        _col.isTrigger = false; // solid obstacle
    }

    void OnEnable()
    {
        if (_runner == null) _runner = StartCoroutine(SpawnLoop());
    }

    void OnDisable()
    {
        if (_runner != null) { StopCoroutine(_runner); _runner = null; }
    }

    IEnumerator SpawnLoop()
    {
        if (startDelay > 0f)
            yield return new WaitForSeconds(startDelay);

        while (true)
        {
            CleanupDead();

            int alive = _alive.Count;
            if (alive < maxAlive)
            {
                int canSpawn = Mathf.Max(0, maxAlive - alive);
                int want = Random.Range(spawnCountRange.x, spawnCountRange.y + 1);
                int toSpawn = Mathf.Clamp(want, 0, canSpawn);

                for (int i = 0; i < toSpawn; i++)
                {
                    if (TryGetSpawnPoint(out Vector2 pos))
                    {
                        // Instantiate WITHOUT a parent so it doesn't inherit transform
                        var go = Instantiate(slimePrefab, pos, Quaternion.identity);
                        _alive.Add(go);

                        // Add a token so we get notified when it dies
                        var token = go.AddComponent<SpawnedToken>();
                        token.owner = this;

                        // Optional: parent AFTER spawn but KEEP world pose so it won't inherit rotation/scale visually
                        if (parentSpawned)
                        {
                            var p = spawnedParentOverride ? spawnedParentOverride : this.transform;
                            go.transform.SetParent(p, worldPositionStays: true);
                        }
                    }
                }
            }

            float wait = Mathf.Max(0.05f, Random.Range(cooldownRange.x, cooldownRange.y));
            yield return new WaitForSeconds(wait);
        }
    }

    void CleanupDead()
    {
        for (int i = _alive.Count - 1; i >= 0; i--)
        {
            if (_alive[i] == null) _alive.RemoveAt(i);
        }
    }

    bool TryGetSpawnPoint(out Vector2 world)
    {
        Vector2 half = areaSize * 0.5f;

        for (int attempt = 0; attempt < maxPointAttempts; attempt++)
        {
            // Pick a point inside the rectangle (local or world-aligned)
            Vector2 localRectPoint = new Vector2(Random.Range(-half.x, half.x), Random.Range(-half.y, half.y));

            Vector2 p;
            if (areaInWorldSpace)
            {
                // World-aligned rectangle: ignore this transform's rotation/scale
                p = (Vector2)transform.position + localRectPoint;
            }
            else
            {
                // Local-to-world: respects this transform's rotation/scale
                p = transform.TransformPoint(localRectPoint);
            }

            if (spawnClearRadius > 0f && Physics2D.OverlapCircle(p, spawnClearRadius, preventSpawnMask))
                continue;

            world = p;
            return true;
        }

        world = default;
        return false;
    }

    // -------------------- Health --------------------
    public void TakeDamage(float amount)
    {
        if (amount <= 0f) return;
        _hp = Mathf.Max(0f, _hp - amount);
        if (_hp <= 0f) Die();
    }

    private void Die()
    {
        onDeath?.Invoke();
        Destroy(gameObject);
    }

    // -------------------- Editor helpers --------------------
    void Reset()
    {
        var box = GetComponent<BoxCollider2D>();
        if (!box)
        {
            box = gameObject.AddComponent<BoxCollider2D>();
        }
        box.size = new Vector2(1f, 1f);
        box.offset = Vector2.zero;
        box.isTrigger = false;
    }

    void OnDrawGizmosSelected()
    {
        // Draw rectangle either world-aligned or using this transform's basis
        Gizmos.color = areaColor;

        Vector3 c = transform.position;
        Vector2 half = areaSize * 0.5f;

        Vector3 a, b, d, e;
        if (areaInWorldSpace)
        {
            // World aligned
            a = c + new Vector3(-half.x, -half.y, 0f);
            b = c + new Vector3( half.x, -half.y, 0f);
            d = c + new Vector3( half.x,  half.y, 0f);
            e = c + new Vector3(-half.x,  half.y, 0f);
            // simple fill
            Gizmos.DrawCube(c, new Vector3(areaSize.x, areaSize.y, 0.01f));
        }
        else
        {
            // Respect transform rotation/scale
            Vector3 right = transform.right * areaSize.x * 0.5f;
            Vector3 up    = transform.up    * areaSize.y * 0.5f;
            a = c + (-right - up);
            b = c + ( right - up);
            d = c + ( right + up);
            e = c + (-right + up);

            // approximate fill
            Gizmos.matrix = Matrix4x4.TRS(transform.position, transform.rotation, new Vector3(1f,1f,1f));
            Gizmos.DrawCube(Vector3.zero, new Vector3(areaSize.x, areaSize.y, 0.01f));
            Gizmos.matrix = Matrix4x4.identity;
        }

        // outline
        Gizmos.color = areaOutline;
        Gizmos.DrawLine(a, b); Gizmos.DrawLine(b, d);
        Gizmos.DrawLine(d, e); Gizmos.DrawLine(e, a);
    }

    // Helper component to track despawn without parenting
    public class SpawnedToken : MonoBehaviour
    {
        public SlimeSpawner2D owner;
        void OnDestroy()
        {
            if (!owner) return;
            // Remove nulls; owner will clean up on next loop too
            owner._alive.Remove(gameObject);
        }
    }
}
