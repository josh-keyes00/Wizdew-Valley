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
            // Instantiate
            var go = Instantiate(shopUIPrefab);

            // Ensure it's inside a Canvas
            var canvas = go.GetComponentInParent<Canvas>();
            if (canvas == null)
            {
                var targetCanvas = FindObjectOfType<Canvas>();
                if (targetCanvas != null)
                    go.transform.SetParent(targetCanvas.transform, worldPositionStays: false);
                else
                    Debug.LogWarning("No Canvas found. Consider putting a Canvas on the ShopUI prefab.", go);
            }

            // Reset/Stretch the RectTransform so it fills the screen
            var rt = go.GetComponent<RectTransform>();
            if (rt != null)
            {
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.anchoredPosition = Vector2.zero;
                rt.offsetMin = Vector2.zero;  // left/bottom
                rt.offsetMax = Vector2.zero;  // right/top
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
}
