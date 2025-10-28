using UnityEngine;
using UnityEngine.UI;

public class ShopUI : MonoBehaviour
{
    [Header("Wiring")]
    public Transform rowsParent;          // container for rows
    public ShopItemRowUI rowPrefab;       // row prefab with ShopItemRowUI on root
    public GameObject rootPanel;          // panel we show/hide

    [Header("HUD")]
    public Text coinText;                 // optional coin display

    private ShopCatalog _catalog;
    private Shopkeeper _owner;
    private WizardController _player;

    public bool IsOpen => rootPanel && rootPanel.activeSelf;

    public void Open(Shopkeeper owner, ShopCatalog catalog)
    {
        _owner = owner;
        _catalog = catalog;

        if (_player == null)
        {
            var p = GameObject.FindGameObjectWithTag("Player");
            if (p) _player = p.GetComponent<WizardController>();
        }

        Refresh();
        if (rootPanel) rootPanel.SetActive(true);
    }

    public void Close()
    {
        if (rootPanel) rootPanel.SetActive(false);
    }

    public void Refresh()
    {
        BuildRows();
        UpdateCoinText();
    }

    private void UpdateCoinText()
    {
        if (coinText && _owner != null)
            coinText.text = _owner.GetPlayerCoinCount().ToString();
    }

    private void BuildRows()
    {
        if (rowsParent)
        {
            for (int i = rowsParent.childCount - 1; i >= 0; i--)
                Destroy(rowsParent.GetChild(i).gameObject);
        }

        if (_catalog == null || _owner == null || rowPrefab == null || rowsParent == null) return;

        foreach (var entry in _catalog.items)
        {
            if (entry == null || string.IsNullOrEmpty(entry.itemId)) continue;

            var row = Instantiate(rowPrefab, rowsParent);
            var icon = _owner.GetIcon(entry.itemId);
            string display = string.IsNullOrEmpty(entry.displayName) ? entry.itemId : entry.displayName;

            row.Setup(
                itemId: entry.itemId,
                displayName: display,
                icon: icon,
                buyPrice: entry.buyPrice,
                sellPrice: entry.sellPrice,
                owner: _owner,
                ui: this
            );
        }
    }

    // called by row
    public void RequestBuyOne(string itemId)
    {
        if (_owner != null && _owner.TryBuyOne(itemId))
            Refresh();
        else
            UpdateCoinText();
    }

    public void RequestSellOne(string itemId)
    {
        if (_owner != null && _owner.TrySellOne(itemId))
            Refresh();
        else
            UpdateCoinText();
    }
}
