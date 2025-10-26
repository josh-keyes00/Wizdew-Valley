using UnityEngine;
using UnityEngine.InputSystem; // New Input System
using UnityEngine.Events;
using System.Collections;
using UnityEngine.SceneManagement; // for death scene load

#if UNITY_EDITOR
using UnityEditor;
#endif

public class WizardController : MonoBehaviour
{
    // -------------------- Movement / Dash / Shoot --------------------
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 5f;

    [Header("Dash")]
    [SerializeField] private float dashSpeed = 12f;
    [SerializeField] private float dashDuration = 0.18f;
    [SerializeField] private float dashCooldown = 0.8f;

    [Header("Shooting")]
    [SerializeField] private GameObject bulletPrefab;
    [SerializeField] private Transform firePoint;
    [SerializeField] private float fireRate = 6f; // bullets per second
    [SerializeField] private float bulletSpeed = 14f;

    [Header("Aiming")]
    [SerializeField] private bool aimWithMouse = true;
    [SerializeField] private Camera aimCamera; // leave empty to auto-use Camera.main

    private Rigidbody2D _rb;
    private Collider2D _col;

    // Input System handles
    private PlayerInput _pi;
    private InputAction _moveAction, _fireAction, _dashAction;

    private Vector2 _moveInput;
    private Vector2 _lastMoveDir = Vector2.right;

    private Vector2 _aimDir; // normalized direction from player to cursor

    private bool _isDashing;
    private float _dashCdTimer;

    private bool _isFiring;
    private float _nextFireTime;

    // -------------------- HEALTH --------------------
    [Header("Health")]
    [SerializeField] private float maxHealth = 5f;
    [SerializeField, Tooltip("Seconds of invulnerability after taking a hit")]
    private float invulnerabilityTime = 0.3f;
    [SerializeField, Tooltip("Destroy the player object on death (otherwise just disables input)")]
    private bool destroyOnDeath = false;

    [Tooltip("Invoked with (current, max) whenever health changes")]
    public UnityEvent<float, float> onHealthChanged;
    public UnityEvent onDeath;

    private float _currentHealth;
    private float _lastHitTime = -999f;

    public float CurrentHealth => _currentHealth;
    public float MaxHealth => maxHealth;
    public bool IsInvulnerable => Time.time - _lastHitTime < invulnerabilityTime;

    // -------------------- Death / Respawn --------------------
    [Header("Death / Respawn")]
    [SerializeField] private bool loadSceneOnDeath = true;
    [SerializeField] private string deathSceneName = "Home"; // must be in Build Settings
#if UNITY_EDITOR
    // Editor-only helper to select a scene asset; its name is copied to deathSceneName.
    [SerializeField] private UnityEditor.SceneAsset deathSceneAsset;
#endif

    // -------------------- Hit / Knockback (ADDED) --------------------
    [Header("Hit / Knockback")]
    [SerializeField, Tooltip("How quickly knockback decays (units/sec). Higher = shorter shove.")]
    private float knockbackFriction = 18f;

    [SerializeField, Tooltip("Brief input lock after getting hit (seconds).")]
    private float hitStunTime = 0.12f;

    private Vector2 _externalVelocity;   // carries knockback over time
    private float _hitStunTimer;

    // -------------------- INVENTORY --------------------
    [System.Serializable]
    public class ItemStack
    {
        public string itemId;
        public int amount;

        public bool IsEmpty => string.IsNullOrEmpty(itemId) || amount <= 0;
        public void Clear() { itemId = null; amount = 0; }
    }

    [Header("Inventory (27 backpack + 9 hotbar = 36 total)")]
    [SerializeField, Min(1)] private int hotbarSize = 9;
    [SerializeField, Min(1)] private int backpackSize = 27;
    [SerializeField, Min(1)] private int maxStackSize = 99;

    [SerializeField] private ItemStack[] hotbar;
    [SerializeField] private ItemStack[] backpack;

    [SerializeField, Range(0, 8)] private int selectedHotbarIndex = 0;

    public int SelectedHotbarIndex => selectedHotbarIndex;
    public ItemStack SelectedHotbarStack =>
        (hotbar != null && selectedHotbarIndex >= 0 && selectedHotbarIndex < hotbar.Length)
            ? hotbar[selectedHotbarIndex] : null;





// ---- Hotbar read-only accessors (ADD THESE) ----
public int HotbarCount => hotbar != null ? hotbar.Length : 0;

public bool TryGetHotbarSlot(int index, out string itemId, out int amount)
{
    itemId = null; amount = 0;
    if (hotbar == null || index < 0 || index >= hotbar.Length) return false;
    var s = hotbar[index];
    if (s == null || s.IsEmpty) return true; // empty slot: null + 0
    itemId = s.itemId;
    amount = s.amount;
    return true;
}

    // -------------------- LIFECYCLE --------------------
    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _col = GetComponent<Collider2D>();

        // Ensure robust collisions for knockback
        _rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        _rb.freezeRotation = true;

        _pi = GetComponent<PlayerInput>();
        if (_pi != null)
        {
            var actions = _pi.actions;
            _moveAction = actions["Move"];
            _fireAction = actions["Fire"];
            _dashAction = actions["Dash"];
        }
        else
        {
            Debug.LogError("PlayerInput missing on Wizard.");
        }

        if (!aimCamera) aimCamera = Camera.main;
        if (!firePoint) Debug.LogWarning("Assign FirePoint on WizardController.");

        // Health init
        _currentHealth = Mathf.Max(1f, maxHealth);
        onHealthChanged?.Invoke(_currentHealth, maxHealth);

        EnsureInventoryArrays();
    }

    private void OnEnable()
    {
        _dashAction?.Enable();
        _fireAction?.Enable();
        _moveAction?.Enable();
    }

    private void OnDisable()
    {
        _dashAction?.Disable();
        _fireAction?.Disable();
        _moveAction?.Disable();
    }

    private void Update()
    {
        // --- Read inputs ---
        if (_moveAction != null)
            _moveInput = Vector2.ClampMagnitude(_moveAction.ReadValue<Vector2>(), 1f);

        if (_moveInput.sqrMagnitude > 0.0001f && !_isDashing)
            _lastMoveDir = _moveInput;

        _dashCdTimer -= Time.deltaTime;

        if (_fireAction != null)
        {
            bool pressed = _fireAction.IsPressed();
            if (pressed && !_isFiring) _nextFireTime = Time.time; // fire immediately on press
            _isFiring = pressed;
        }

        if (_isFiring && Time.time >= _nextFireTime)
        {
            Fire();
            _nextFireTime = Time.time + (1f / Mathf.Max(0.01f, fireRate));
        }

        if (_dashAction != null && _dashAction.triggered)
            TryDash();

        // --- Mouse aiming ---
        UpdateAimDirectionFromMouse();
        RotateFirePointTowardAim();
    }

    private void FixedUpdate()
    {
        // Decay knockback over time
        _externalVelocity = Vector2.MoveTowards(
            _externalVelocity, Vector2.zero, knockbackFriction * Time.fixedDeltaTime);

        // Optional brief input lock on hit
        if (_hitStunTimer > 0f) _hitStunTimer -= Time.fixedDeltaTime;

        float speed = _isDashing ? dashSpeed : moveSpeed;

        // If stunned, ignore input for a moment (still keep knockback)
        Vector2 inputVel = (_hitStunTimer > 0f) ? Vector2.zero : _moveInput * speed;

        // Combine input with external (knockback) velocity
        Vector2 totalVel = inputVel + _externalVelocity;

        // Move with MovePosition so it stays smooth; include the knockback
        _rb.MovePosition(_rb.position + totalVel * Time.fixedDeltaTime);
    }

    // -------------------- AIM / DASH / FIRE --------------------
    private void UpdateAimDirectionFromMouse()
    {
        if (!aimWithMouse || aimCamera == null || Mouse.current == null) return;

        Vector2 mouseScreen = Mouse.current.position.ReadValue();
        Vector3 mouseWorld3 = aimCamera.ScreenToWorldPoint(new Vector3(mouseScreen.x, mouseScreen.y, 0f));
        Vector2 mouseWorld = (Vector2)mouseWorld3;

        Vector2 dir = mouseWorld - _rb.position;
        _aimDir = dir.sqrMagnitude > 0.000001f ? dir.normalized : _aimDir;
    }

    private void RotateFirePointTowardAim()
    {
        if (!firePoint) return;

        Vector2 dir = _aimDir.sqrMagnitude > 0.0001f ? _aimDir : _lastMoveDir;
        if (dir.sqrMagnitude < 0.0001f) dir = Vector2.right;

        firePoint.right = dir; // point firePoint's +X toward aim
    }

    private void TryDash()
    {
        if (_isDashing || _dashCdTimer > 0f || _lastMoveDir.sqrMagnitude < 0.0001f) return;
        _dashCdTimer = dashCooldown;
        StartCoroutine(DashRoutine());
    }

    private IEnumerator DashRoutine()
    {
        _isDashing = true;
        yield return new WaitForSeconds(dashDuration);
        _isDashing = false;
    }

    private void Fire()
    {
        if (!bulletPrefab || !firePoint) return;

        Vector2 dir = _aimDir.sqrMagnitude > 0.0001f ? _aimDir
                      : (_lastMoveDir.sqrMagnitude > 0.0001f ? _lastMoveDir : Vector2.right);

        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        GameObject b = Instantiate(bulletPrefab, firePoint.position, Quaternion.AngleAxis(angle, Vector3.forward));
        var bullet = b.GetComponent<Bullet>();
        if (bullet) bullet.Init(dir, bulletSpeed, _col);

        Debug.DrawRay(firePoint.position, dir * 0.8f, Color.cyan, 0.1f);
    }

    // -------------------- HEALTH API --------------------
    // NOTE: Slime uses SendMessage("TakeDamage", amount) if interface isn't present.
    public void TakeDamage(float amount)
    {
        if (amount <= 0f) return;
        if (IsInvulnerable) return;

        _lastHitTime = Time.time;
        _currentHealth = Mathf.Max(0f, _currentHealth - amount);
        onHealthChanged?.Invoke(_currentHealth, maxHealth);

        if (_currentHealth <= 0f)
            Die();
        else
            _hitStunTimer = hitStunTime; // small stun on hit (pairs with knockback)
    }

    public void Heal(float amount)
    {
        if (amount <= 0f) return;
        _currentHealth = Mathf.Min(maxHealth, _currentHealth + amount);
        onHealthChanged?.Invoke(_currentHealth, maxHealth);
    }

    public void SetMaxHealth(float newMax, bool fill = true)
    {
        maxHealth = Mathf.Max(1f, newMax);
        if (fill) _currentHealth = maxHealth;
        else _currentHealth = Mathf.Min(_currentHealth, maxHealth);
        onHealthChanged?.Invoke(_currentHealth, maxHealth);
    }

    private void Die()
    {
        onDeath?.Invoke();

        // Stop input/motion immediately
        _moveInput = Vector2.zero;
        _isFiring = false;

        // Load a scene on death if configured
        if (loadSceneOnDeath && !string.IsNullOrEmpty(deathSceneName))
        {
            SceneManager.LoadSceneAsync(deathSceneName, LoadSceneMode.Single);
            return; // scene is switching
        }

        // Fallback behavior if not loading a scene
        if (destroyOnDeath) Destroy(gameObject);
        else
        {
            if (_pi) _pi.enabled = false;
            enabled = false;
        }
    }

    // -------------------- KNOCKBACK HOOK (ADDED) --------------------
    // Called by Slime via SendMessage("ApplyKnockback", impulse)
    public void ApplyKnockback(Vector2 impulse)
    {
        float mass = _rb ? Mathf.Max(0.0001f, _rb.mass) : 1f;
        _externalVelocity += impulse / mass; // convert impulse J to velocity v = J/m
        _hitStunTimer = Mathf.Max(_hitStunTimer, hitStunTime);
    }

    // -------------------- INVENTORY API --------------------
    private void EnsureInventoryArrays()
    {
        if (hotbar == null || hotbar.Length != hotbarSize) hotbar = NewStackArray(hotbarSize);
        if (backpack == null || backpack.Length != backpackSize) backpack = NewStackArray(backpackSize);
        ClampStacksToMax(hotbar);
        ClampStacksToMax(backpack);
        selectedHotbarIndex = Mathf.Clamp(selectedHotbarIndex, 0, Mathf.Max(0, hotbarSize - 1));
    }

    private ItemStack[] NewStackArray(int size)
    {
        var arr = new ItemStack[size];
        for (int i = 0; i < size; i++) arr[i] = new ItemStack();
        return arr;
    }

    private void ClampStacksToMax(ItemStack[] arr)
    {
        if (arr == null) return;
        for (int i = 0; i < arr.Length; i++)
        {
            if (arr[i] == null) arr[i] = new ItemStack();
            if (!arr[i].IsEmpty) arr[i].amount = Mathf.Clamp(arr[i].amount, 0, maxStackSize);
        }
    }

    /// <summary>Try to add items. Returns true if all were added.</summary>
    public bool TryAddItem(string itemId, int amount)
    {
        if (string.IsNullOrEmpty(itemId) || amount <= 0) return true;
        EnsureInventoryArrays();

        int remaining = amount;

        // 1) Merge into existing stacks (hotbar then backpack)
        remaining = MergeIntoExisting(hotbar, itemId, remaining);
        remaining = MergeIntoExisting(backpack, itemId, remaining);

        // 2) Fill empty slots (hotbar then backpack)
        remaining = FillEmptySlots(hotbar, itemId, remaining);
        remaining = FillEmptySlots(backpack, itemId, remaining);

        return remaining == 0;
    }

    /// <summary>Remove up to 'amount' of itemId. Returns removed count.</summary>
    public int RemoveItem(string itemId, int amount)
    {
        if (string.IsNullOrEmpty(itemId) || amount <= 0) return 0;
        EnsureInventoryArrays();

        int toRemove = amount;

        // Remove from hotbar first, then backpack
        toRemove = RemoveFromArray(hotbar, itemId, toRemove);
        toRemove = RemoveFromArray(backpack, itemId, toRemove);

        return amount - toRemove;
    }

    /// <summary>Total count of itemId across hotbar + backpack.</summary>
    public int GetTotalCount(string itemId)
    {
        if (string.IsNullOrEmpty(itemId)) return 0;
        int sum = 0;
        sum += CountInArray(hotbar, itemId);
        sum += CountInArray(backpack, itemId);
        return sum;
    }

    public void SelectHotbarIndex(int index)
    {
        EnsureInventoryArrays();
        selectedHotbarIndex = Mathf.Clamp(index, 0, hotbar.Length - 1);
        // Hook this to UI highlight if you have one.
    }

    /// <summary>Use one item from the selected hotbar slot (placeholder).</summary>
    public bool UseSelectedHotbarItem()
    {
        EnsureInventoryArrays();
        var stack = hotbar[selectedHotbarIndex];
        if (stack.IsEmpty) return false;

        // TODO: trigger your actual item use here (cast spell, consume potion, place tile, etc.)
        // For now we just consume one.
        stack.amount -= 1;
        if (stack.amount <= 0) stack.Clear();
        return true;
    }

    public bool SwapHotbarSlots(int a, int b)
    {
        if (a < 0 || b < 0 || a >= hotbar.Length || b >= hotbar.Length) return false;
        (hotbar[a], hotbar[b]) = (hotbar[b], hotbar[a]);
        return true;
    }

    // ------- Inventory helpers -------
    private int MergeIntoExisting(ItemStack[] arr, string id, int remaining)
    {
        if (arr == null || remaining <= 0) return remaining;
        for (int i = 0; i < arr.Length && remaining > 0; i++)
        {
            var s = arr[i];
            if (!s.IsEmpty && s.itemId == id && s.amount < maxStackSize)
            {
                int space = maxStackSize - s.amount;
                int add = Mathf.Min(space, remaining);
                s.amount += add;
                remaining -= add;
            }
        }
        return remaining;
    }

    private int FillEmptySlots(ItemStack[] arr, string id, int remaining)
    {
        if (arr == null || remaining <= 0) return remaining;
        for (int i = 0; i < arr.Length && remaining > 0; i++)
        {
            var s = arr[i];
            if (s.IsEmpty)
            {
                int add = Mathf.Min(maxStackSize, remaining);
                s.itemId = id;
                s.amount = add;
                remaining -= add;
            }
        }
        return remaining;
    }

    private int RemoveFromArray(ItemStack[] arr, string id, int toRemove)
    {
        if (arr == null || toRemove <= 0) return toRemove;
        for (int i = 0; i < arr.Length && toRemove > 0; i++)
        {
            var s = arr[i];
            if (!s.IsEmpty && s.itemId == id)
            {
                int take = Mathf.Min(s.amount, toRemove);
                s.amount -= take;
                toRemove -= take;
                if (s.amount <= 0) s.Clear();
            }
        }
        return toRemove;
    }

    private int CountInArray(ItemStack[] arr, string id)
    {
        if (arr == null) return 0;
        int c = 0;
        foreach (var s in arr)
            if (s != null && !s.IsEmpty && s.itemId == id) c += s.amount;
        return c;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (maxHealth < 1f) maxHealth = 1f;
        EnsureInventoryArrays();
        // Keep selected index in range if sizes change in Inspector
        if (hotbar != null) selectedHotbarIndex = Mathf.Clamp(selectedHotbarIndex, 0, hotbar.Length - 1);

        // sync picked scene asset to the string name (Editor only)
        if (deathSceneAsset != null)
            deathSceneName = deathSceneAsset.name;
    }
#endif
}
