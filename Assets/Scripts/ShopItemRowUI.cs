using UnityEngine;
using UnityEngine.UI;

public class ShopItemRowUI : MonoBehaviour
{
    [Header("UI")]
    public Image iconImage;
    public Text nameText;
    public Text buyPriceText;
    public Text sellPriceText;
    public Button buyButton;
    public Button sellButton;

    // runtime
    private string _itemId;
    private int _buyPrice;
    private int _sellPrice;
    private Shopkeeper _owner;
    private ShopUI _ui;

    public void Setup(string itemId, string displayName, Sprite icon,
                      int buyPrice, int sellPrice,
                      Shopkeeper owner, ShopUI ui)
    {
        _itemId = itemId;
        _buyPrice = buyPrice;
        _sellPrice = sellPrice;
        _owner = owner;
        _ui = ui;

        if (iconImage) iconImage.sprite = icon;
        if (nameText) nameText.text = displayName;
        if (buyPriceText) buyPriceText.text = buyPrice.ToString();
        if (sellPriceText) sellPriceText.text = sellPrice.ToString();

        // Wire buttons
        if (buyButton)
        {
            buyButton.onClick.RemoveAllListeners();
            buyButton.onClick.AddListener(() => _ui.RequestBuyOne(_itemId));
        }
        if (sellButton)
        {
            sellButton.onClick.RemoveAllListeners();
            sellButton.onClick.AddListener(() => _ui.RequestSellOne(_itemId));
        }

        // Initial interactivity based on coins/items
        UpdateInteractivity();
    }

    private void UpdateInteractivity()
    {
        if (_owner == null) return;

        bool canBuy = _owner.GetPlayerCoinCount() >= _buyPrice;
        bool canSell = _owner.GetPlayerItemCount(_itemId) > 0 && _itemId != _owner.CoinId;

        if (buyButton) buyButton.interactable = canBuy;
        if (sellButton) sellButton.interactable = canSell;
    }
}
