using UnityEngine;
using UnityEngine.UI;

public class HotbarSlotUI : MonoBehaviour
{
    [Header("UI Refs")]
    public Image slotBackground;
    public Image iconImage;
    public Text countText;
    public GameObject selectedHighlight;

    [Header("Empty State")]
    public Color emptyBgColor = new Color(0,0,0,0.35f);
    public Color filledBgColor = new Color(0,0,0,0.6f);

    public void SetData(Sprite icon, int count, bool selected)
    {
        bool hasItem = icon != null && count > 0;

        if (iconImage)
        {
            iconImage.sprite = hasItem ? icon : null;
            iconImage.enabled = hasItem;
        }

        if (countText)
        {
            // show count only if > 1 (common UX)
            countText.text = (hasItem && count > 1) ? count.ToString() : "";
        }

        if (slotBackground)
        {
            slotBackground.color = hasItem ? filledBgColor : emptyBgColor;
        }

        if (selectedHighlight)
        {
            selectedHighlight.SetActive(selected);
        }
    }
}
