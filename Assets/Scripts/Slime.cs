using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
public class Slime : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private Transform player;
    [SerializeField] private string playerTag = "Player";

    [Header("Aggro (units)")]
    [SerializeField] private float aggroRadius = 6f;
    [SerializeField] private float deAggroRadius = 8f;

    [Header("Hop Timing (seconds)")]
    [SerializeField] private Vector2 idleHopInterval = new Vector2(0.8f, 1.4f);
    [SerializeField] private Vector2 chaseHopInterval = new Vector2(0.35f, 0.7f);
    [SerializeField, Range(0.05f, 0.6f)] private float hopDuration = 0.22f;

    [Header("Hop Speed (units/sec during hop)")]
    [SerializeField] private float idleHopSpeed = 3.0f;
    [SerializeField] private float chaseHopSpeed = 5.0f;

    [Header("Obstacles")]
    [SerializeField] private float obstacleCheckDistance = 0.6f;
    [SerializeField] private LayerMask obstacleMask = ~0; // set to your Walls physics layer

    [Header("Contact Damage")]
    [SerializeField] private float contactDamage = 1f;
    [SerializeField, Tooltip("Minimum time between hits on the same target")]
    private float contactCooldown = 0.5f;

    [Header("Knockback (Impulse)")]
    [SerializeField, Tooltip("Min/Max impulse applied to player on hit")]
    private Vector2 knockbackImpulseRange = new Vector2(4f, 7f);

    [Header("Misc")]
    [SerializeField] private bool faceDirection = true;
    [SerializeField] private float maxSpeed = 7f;

    // ---------- HEALTH ----------
    [Header("Health")]
    [SerializeField] private float maxHealth = 3f;
    [SerializeField, Tooltip("Brief i-frames after taking damage (seconds)")]
    private float hitInvulnerability = 0.05f;
    public UnityEvent onDeath; // optional hook for VFX/SFX/loot

    private float _hp;
    private float _lastDamageTime = -999f;

    private Rigidbody2D rb;
    private bool isChasing;
    private float intervalTimer;
    private float hopTimeLeft;
    private Vector2 moveDir = Vector2.zero;
    private Vector2 lastDir = Vector2.right;

    // Tracks last time we damaged a given target (by instanceID)
    private readonly Dictionary<int, float> lastHitTime = new Dictionary<int, float>();

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0f;                       // top-down: no gravity
        rb.freezeRotation = true;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        _hp = Mathf.Max(1f, maxHealth);
    }

    void Start()
    {
        if (!player)
        {
            var p = GameObject.FindGameObjectWithTag(playerTag);
            if (p) player = p.transform;
        }
        ScheduleNextHop(true);
    }

    void Update()
    {
        UpdateAggro();

        // Active hop window
        if (hopTimeLeft > 0f)
        {
            hopTimeLeft -= Time.deltaTime;
            float spd = isChasing ? chaseHopSpeed : idleHopSpeed;
            rb.linearVelocity = moveDir * spd; // FIX

            if (faceDirection && moveDir.sqrMagnitude > 1e-4f)
            {
                var s = transform.localScale;
                if (Mathf.Abs(moveDir.x) > 0.01f) s.x = Mathf.Sign(moveDir.x) * Mathf.Abs(s.x);
                transform.localScale = s;
            }

            if (hopTimeLeft <= 0f)
            {
                rb.linearVelocity = Vector2.zero; // FIX
                ScheduleNextHop(!isChasing);
            }

            ClampSpeed();
            return;
        }

        // Waiting for next hop
        intervalTimer -= Time.deltaTime;
        if (intervalTimer <= 0f)
        {
            Vector2 dir;

            if (isChasing && player)
            {
                dir = (player.position - transform.position);
                if (dir.sqrMagnitude < 1e-4f) dir = lastDir;
                else dir.Normalize();
            }
            else
            {
                // random wander with slight bias toward last direction
                dir = Random.insideUnitCircle.normalized;
                dir = (dir * 0.5f + lastDir * 0.5f).normalized;
            }

            // Avoid walls: try a side step, else reverse
            if (Physics2D.CircleCast(transform.position, 0.2f, dir, obstacleCheckDistance, obstacleMask))
            {
                var side = Vector2.Perpendicular(dir);
                if (Physics2D.CircleCast(transform.position, 0.2f, side, obstacleCheckDistance, obstacleMask))
                    dir = -dir;
                else
                    dir = side;
            }

            lastDir = dir;
            moveDir = dir;
            hopTimeLeft = hopDuration;
        }

        ClampSpeed();
    }

    void UpdateAggro()
    {
        if (!player) { isChasing = false; return; }
        float d = Vector2.Distance(transform.position, player.position);
        if (!isChasing && d <= aggroRadius) isChasing = true;
        else if (isChasing && d >= deAggroRadius) isChasing = false;
    }

    void ScheduleNextHop(bool idle)
    {
        var r = idle ? idleHopInterval : chaseHopInterval;
        intervalTimer = Random.Range(r.x, r.y);
    }

    void ClampSpeed()
    {
        rb.linearVelocity = Vector2.ClampMagnitude(rb.linearVelocity, maxSpeed); // FIX
    }

    void Reset()
    {
        if (!GetComponent<Collider2D>()) gameObject.AddComponent<CircleCollider2D>();
        if (!GetComponent<Rigidbody2D>()) gameObject.AddComponent<Rigidbody2D>();
    }

    // ---- CONTACT DAMAGE / KNOCKBACK TO PLAYER ----
    void TryDealContact(GameObject target)
    {
        if (!target.CompareTag(playerTag)) return;

        int id = target.GetInstanceID();
        if (lastHitTime.TryGetValue(id, out var last))
        {
            if (Time.time - last < contactCooldown) return; // still on cooldown
        }
        lastHitTime[id] = Time.time;

        // Damage via message (no interface required)
        target.SendMessage("TakeDamage", contactDamage, SendMessageOptions.DontRequireReceiver);

        // Ask the target to apply knockback in its own controller (works with MovePosition)
        Vector2 dir = ((Vector2)target.transform.position - (Vector2)transform.position).normalized;
        float force = Random.Range(knockbackImpulseRange.x, knockbackImpulseRange.y);
        target.SendMessage("ApplyKnockback", dir * force, SendMessageOptions.DontRequireReceiver);
    }

    void OnCollisionEnter2D(Collision2D col)  { TryDealContact(col.gameObject); }
    void OnCollisionStay2D(Collision2D col)   { TryDealContact(col.gameObject); }
    void OnTriggerEnter2D(Collider2D other)   { TryDealContact(other.gameObject); }
    void OnTriggerStay2D(Collider2D other)    { TryDealContact(other.gameObject); }

    // ---------- DAMAGE / DEATH ----------
    public void TakeDamage(float amount)
    {
        if (amount <= 0f) return;
        if (Time.time - _lastDamageTime < hitInvulnerability) return;

        _lastDamageTime = Time.time;
        _hp = Mathf.Max(0f, _hp - amount);

        if (_hp <= 0f)
        {
            Die();
        }
        else
        {
            // (optional) brief feedback hook (flash, sound) can go here
        }
    }

    public void ApplyKnockback(Vector2 impulse)
    {
        if (impulse.sqrMagnitude <= 0f || rb == null) return;
        rb.AddForce(impulse, ForceMode2D.Impulse);
    }

    private void Die()
    {
        onDeath?.Invoke();
        KillCounter.Instance?.RegisterKill("Slime");
        Destroy(gameObject);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green; Gizmos.DrawWireSphere(transform.position, aggroRadius);
        Gizmos.color = new Color(1f, 0.6f, 0f); Gizmos.DrawWireSphere(transform.position, deAggroRadius);
        Gizmos.color = Color.red; Gizmos.DrawLine(transform.position, transform.position + (Vector3)(lastDir.normalized * obstacleCheckDistance));
    }
}
