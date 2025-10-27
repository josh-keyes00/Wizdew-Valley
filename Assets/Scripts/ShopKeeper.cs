using UnityEngine;
using UnityEngine.InputSystem; // New Input System

public class Shopkeeper : MonoBehaviour
{
    [Header("Shop")]
    [SerializeField] private ShopCatalog catalog;
    [SerializeField] private GameObject shopUIPrefab;   // must have ShopUI on its root

    [Header("Interaction")]
    [SerializeField] private float interactRadius = 1.7f;
    [SerializeField] private Key openKey = Key.E;       // press to open/toggle
    [SerializeField] private Key closeKey = Key.Escape; // press to close
    [SerializeField] private string playerTag = "Player";

    private Transform _player;
    private ShopUI _ui;

    void Start()
    {
        var p = GameObject.FindGameObjectWithTag(playerTag);
        if (p) _player = p.transform;
    }

    void Update()
    {
        if (Keyboard.current == null) return;

        // re-find player if it got reloaded
        if (_player == null)
        {
            var p = GameObject.FindGameObjectWithTag(playerTag);
            if (p) _player = p.transform;
        }

        // Open/toggle when in range
        if (_player != null && Keyboard.current[openKey].wasPressedThisFrame && InRange())
        {
            OpenOrToggle();
        }

        // Close with key
        if (_ui != null && _ui.IsOpen && Keyboard.current[closeKey].wasPressedThisFrame)
        {
            _ui.Close();
        }
    }

    private bool InRange()
    {
        return Vector2.Distance(_player.position, transform.position) <= interactRadius;
    }

    private void OpenOrToggle()
    {
        if (shopUIPrefab == null)
        {
            Debug.LogError("Shopkeeper: 'shopUIPrefab' is not assigned.", this);
            return;
        }

        if (_ui == null)
        {
            var go = Instantiate(shopUIPrefab);
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

    public void CloseShopFromUI()
    {
        _ui?.Close();
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.2f, 0.9f, 1f, 0.6f);
        Gizmos.DrawWireSphere(transform.position, interactRadius);
    }
}
