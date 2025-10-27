using UnityEngine;

public class ShopUI : MonoBehaviour
{
    [Header("Wiring")]
    public Transform rowsParent;          // container for rows
    public ShopItemRowUI rowPrefab;       // row prefab with ShopItemRowUI on root
    public GameObject rootPanel;          // panel we show/hide

    private ShopCatalog _catalog;
    private Shopkeeper _owner;
    private WizardController _player;

    public bool IsOpen => rootPanel != null && rootPanel.activeSelf;

    public void Open(Shopkeeper owner, ShopCatalog catalog)
    {
        _owner = owner;
        _catalog = catalog;

        if (_player == null)
        {
            var p = GameObject.FindGameObjectWithTag("Player");
            if (p) _player = p.GetComponent<WizardController>();
        }

        BuildRows();
        if (rootPanel) rootPanel.SetActive(true);
    }

    public void Close()
    {
        if (rootPanel) rootPanel.SetActive(false);
    }

    private void BuildRows()
    {
        // clear
        for (int i = rowsParent.childCount - 1; i >= 0; i--)
            Destroy(rowsParent.GetChild(i).gameObject);

        if (_catalog == null || _player == null || rowPrefab == null || rowsParent == null) return;

        foreach (var seed in _catalog.seeds)
        {
            if (!seed) continue;
            var row = Instantiate(rowPrefab, rowsParent);
            row.Setup(seed, _player, this);
        }
    }
}
