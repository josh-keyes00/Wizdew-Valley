using UnityEngine;
using UnityEngine.UI;

public class ShopItemRowUI : MonoBehaviour
{
    [Header("UI")]
    public Image icon;
    public Text nameText;
    public Text priceBuyText;
    public Text priceSellText;
    public Button buy1Button;
    public Button sellAllButton;

    private SeedItem _seed;
    private WizardController _player;
    private ShopUI _shop;

    public void Setup(SeedItem seed, WizardController player, ShopUI shop)
    {
        _seed = seed; _player = player; _shop = shop;

        if (icon) icon.sprite = seed.icon;
        if (nameText) nameText.text = seed.displayName;
        if (priceBuyText) priceBuyText.text = $"Buy: {seed.buyPrice}";
        if (priceSellText) priceSellText.text = $"Sell: {seed.sellPrice}";

        buy1Button.onClick.AddListener(() =>
        {
            if (Currency.Instance != null && Currency.Instance.Spend(seed.buyPrice))
            {
                _player.TryAddItem(seed.id, 1);
            }
        });

        sellAllButton.onClick.AddListener(() =>
        {
            int have = _player.GetTotalCount(seed.harvestItemId);
            if (have > 0)
            {
                _player.RemoveItem(seed.harvestItemId, have);
                Currency.Instance?.Add(have * seed.sellPrice);
            }
        });
    }
}
