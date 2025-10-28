using UnityEngine;
using UnityEngine.InputSystem; // New Input System

public class Shopkeeper : MonoBehaviour
{
    [Header("Databases")]
    [SerializeField] private ItemDatabase itemDatabase;
    [SerializeField] private ShopCatalog catalog;

    [Header("Currency")]
    [Tooltip("Item id used as currency (must exist in ItemDatabase).")]
    [SerializeField] private string coinId = "coin";

    [Header("UI")]
    [SerializeField] private GameObject shopUIPrefab;   // must have ShopUI on its root

    [Header("Interaction")]
    [SerializeField] private float interactRadius = 1.7f;
    [SerializeField] private Key openKey = Key.E;       // press to open/toggle
    [SerializeField] private Key closeKey = Key.Escape; // press to close
    [SerializeField] private string playerTag = "Player";

    private Transform _player;
    private WizardController _wizard;
    private ShopUI _ui;

    // ---------- Unity ----------
    void Start() => ReacquirePlayer();

    void Update()
    {
        if (Keyboard.current == null) return;

        if (_player == null || _wizard == null)
            ReacquirePlayer();

        if (_player != null && Keyboard.current[openKey].wasPressedThisFrame && InRange())
            OpenOrToggle();

        if (_ui != null && _ui.IsOpen && Keyboard.current[closeKey].wasPressedThisFrame)
            _ui.Close();
    }

    // ---------- Player ----------
    private void ReacquirePlayer()
    {
        var p = GameObject.FindGameObjectWithTag(playerTag);
        _player = p ? p.transform : null;
        _wizard = p ? p.GetComponent<WizardController>() : null;
    }

    private bool InRange() =>
        _player && Vector2.Distance(_player.position, transform.position) <= interactRadius;

    // ---------- UI ----------
    private void OpenOrToggle()
    {
        if (!shopUIPrefab)
        {
            Debug.LogError("Shopkeeper: 'shopUIPrefab' is not assigned.", this);
            return;
        }

        if (_ui == null)
        {
            var go = Instantiate(shopUIPrefab);

            // Ensure in a Canvas
            var canvas = go.GetComponentInParent<Canvas>();
            if (canvas == null)
            {
                var targetCanvas = FindObjectOfType<Canvas>(true);
                if (targetCanvas != null)
                    go.transform.SetParent(targetCanvas.transform, false);
            }

            // Stretch
            var rt = go.GetComponent<RectTransform>();
            if (rt)
            {
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.anchoredPosition = Vector2.zero;
                rt.offsetMin = Vector2.zero;
                rt.offsetMax = Vector2.zero;
                rt.localScale = Vector3.one;
            }

            _ui = go.GetComponent<ShopUI>();
            if (_ui == null)
            {
                Debug.LogError("Shopkeeper: shopUIPrefab is missing a ShopUI component on its ROOT object.", go);
                Destroy(go);
                return;
            }
        }

        if (_ui.IsOpen) _ui.Close();
        else _ui.Open(this, catalog);
    }

    // ---------- Exposed to UI ----------
    public ItemDatabase ItemDB => itemDatabase;
    public string CoinId => coinId;
    public Sprite GetIcon(string itemId) => itemDatabase ? itemDatabase.GetIcon(itemId) : null;
    public int GetPlayerCoinCount() => _wizard ? _wizard.GetTotalCount(coinId) : 0;
    public int GetPlayerItemCount(string itemId) => _wizard ? _wizard.GetTotalCount(itemId) : 0;

    // ---------- Core transactions (no stock; one at a time) ----------
    public bool TryBuyOne(string itemId)
    {
        if (_wizard == null || catalog == null) return false;
        if (string.IsNullOrEmpty(itemId)) return false;

        var entry = catalog.Find(itemId);
        if (entry == null) { Debug.LogWarning($"[Shop] '{itemId}' not in catalog."); return false; }

        int cost = entry.buyPrice;
        if (GetPlayerCoinCount() < cost) return false;

        // Try add item first so we don't charge if full
        if (!_wizard.TryAddItem(itemId, 1)) return false;

        // Charge coins; rollback if failed (rare)
        int removed = _wizard.RemoveItem(coinId, cost);
        if (removed != cost)
        {
            _wizard.RemoveItem(itemId, 1);
            return false;
        }
        return true;
    }

    public bool TrySellOne(string itemId)
    {
        if (_wizard == null || catalog == null) return false;
        if (string.IsNullOrEmpty(itemId)) return false;
        if (itemId == coinId) return false; // don't sell currency

        var entry = catalog.Find(itemId);
        if (entry == null) { Debug.LogWarning($"[Shop] '{itemId}' not in catalog."); return false; }

        if (GetPlayerItemCount(itemId) <= 0) return false;

        int removed = _wizard.RemoveItem(itemId, 1);
        if (removed != 1) return false;

        if (!_wizard.TryAddItem(coinId, entry.sellPrice))
        {
            // rollback if couldn't pay coins (shouldn't happen if coin exists)
            _wizard.TryAddItem(itemId, 1);
            return false;
        }
        return true;
    }
}
