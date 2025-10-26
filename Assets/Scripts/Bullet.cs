using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public class Bullet : MonoBehaviour
{
    [Header("Damage")]
    [SerializeField] private float damage = 1f;
    [SerializeField, Tooltip("Apply damage to objects on wallLayers too.")]
    private bool damageWalls = true;
    [SerializeField, Tooltip("Impulse sent via SendMessage(\"ApplyKnockback\", impulse). 0 = no knockback.")]
    private float knockbackImpulse = 0f;

    [Header("Lifetime / Pierce")]
    [SerializeField, Tooltip("Seconds before auto-destroy.")]
    private float maxLifetime = 4f;
    [SerializeField, Tooltip("How many damageable targets this bullet can pass through before destroying. 0 = destroy on first hit.")]
    private int pierceCount = 0;

    [Header("Layer Filtering")]
    [SerializeField, Tooltip("Layers considered damageable (enemies, spawners, destructibles, etc).")]
    private LayerMask damageableLayers = ~0;
    [SerializeField, Tooltip("Layers considered 'walls/obstacles'. Hitting these stops the bullet (and optionally damages them).")]
    private LayerMask wallLayers = 0;

    private Rigidbody2D _rb;
    private Collider2D _col;
    private Collider2D _ownerCol;
    private int _remainingPierce;
    private float _spawnTime;
    private bool _dead;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _col = GetComponent<Collider2D>();

        // 2D projectile best practices
        _rb.gravityScale = 0f;
        _rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        _rb.interpolation = RigidbodyInterpolation2D.Interpolate;

        _remainingPierce = pierceCount;
    }

    /// <summary>Called by the shooter (WizardController.Fire)</summary>
    public void Init(Vector2 dir, float speed, Collider2D ownerCollider)
    {
        _ownerCol = ownerCollider;
        _spawnTime = Time.time;

        if (_col && _ownerCol)
            Physics2D.IgnoreCollision(_col, _ownerCol, true); // ignore the shooter

        if (dir.sqrMagnitude > 0.0001f) dir.Normalize();
        _rb.linearVelocity = dir * speed;
    }

    private void Update()
    {
        if (!_dead && Time.time - _spawnTime > maxLifetime)
            Destroy(gameObject);
    }

    private void OnDestroy()
    {
        // (optional) revert ignore if both still exist
        if (_col && _ownerCol)
            Physics2D.IgnoreCollision(_col, _ownerCol, false);
    }

    private void OnTriggerEnter2D(Collider2D other)   { HandleContact(other.gameObject, other); }
    private void OnCollisionEnter2D(Collision2D col)  { HandleContact(col.gameObject, col.collider); }

    private void HandleContact(GameObject target, Collider2D hitCol)
    {
        if (_dead || target == null) return;

        // ignore the shooter
        if (_ownerCol && hitCol && (hitCol == _ownerCol || target == _ownerCol.gameObject))
            return;

        int layer = target.layer;
        bool isWall        = (wallLayers.value      & (1 << layer)) != 0;
        bool isDamageable  = (damageableLayers.value & (1 << layer)) != 0;

        // Hitting walls/obstacles
        if (isWall)
        {
            if (damageWalls)
            {
                target.SendMessage("TakeDamage", damage, SendMessageOptions.DontRequireReceiver);
                TrySendKnockback(target);
            }
            Kill();
            return;
        }

        // Hitting damageable targets (enemies, spawners, destructibles, etc.)
        if (isDamageable)
        {
            target.SendMessage("TakeDamage", damage, SendMessageOptions.DontRequireReceiver);
            TrySendKnockback(target);

            if (_remainingPierce > 0) { _remainingPierce--; return; }
            Kill();
            return;
        }

        // Anything else (scenery/unfiltered): stop the bullet to avoid stuck projectiles.
        Kill();
    }

    private void TrySendKnockback(GameObject target)
    {
        if (knockbackImpulse <= 0f) return;
        Vector2 dir = (_rb && _rb.linearVelocity.sqrMagnitude > 0.0001f) ? _rb.linearVelocity.normalized : Vector2.right;
        target.SendMessage("ApplyKnockback", dir * knockbackImpulse, SendMessageOptions.DontRequireReceiver);
    }

    private void Kill()
    {
        if (_dead) return;
        _dead = true;
        if (_rb) _rb.linearVelocity = Vector2.zero;
        Destroy(gameObject);
    }

    // Optional setters if you ever want to tweak at runtime
    public void SetDamage(float d) => damage = d;
    public void SetMasks(LayerMask dmg, LayerMask wall) { damageableLayers = dmg; wallLayers = wall; }
}
