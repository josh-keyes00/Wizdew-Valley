using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class HotbarSlotUI : MonoBehaviour,
    IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler, IPointerClickHandler
{
    [Header("UI Refs")]
    public Image slotBackground;
    public Image iconImage;
    public Text  countText;
    public GameObject selectedHighlight;

    [Header("Empty State")]
    public Color emptyBgColor = new Color(0,0,0,0.35f);
    public Color filledBgColor = new Color(0,0,0,0.6f);

    [Header("Binding")]
    public WizardController wizard;
    public ItemDatabase itemDatabase;
    public WizardController.SlotContainer container;
    public int index;

    // drag ghost (static so there's only ever one)
    private static RectTransform sGhost;
    private static Image sGhostIcon;
    private static Text sGhostCount;
    private static bool sDragging;
    private static WizardController.SlotContainer sFromContainer;
    private static int sFromIndex;
    private static Canvas sCanvas; // cache canvas we build the ghost under

    void Awake()
    {
        // Ensure the slot (parent) receives raycasts, not the children
        if (iconImage) iconImage.raycastTarget = false;
        if (countText) countText.raycastTarget = false;
        if (slotBackground) slotBackground.raycastTarget = true;
    }

    public void Bind(WizardController wiz, ItemDatabase db, WizardController.SlotContainer cont, int idx)
    {
        wizard = wiz;
        itemDatabase = db;
        container = cont;
        index = idx;
    }

    public void SetData(Sprite icon, int count, bool selected)
    {
        bool hasItem = icon != null && count > 0;

        if (iconImage)
        {
            iconImage.sprite = hasItem ? icon : null;
            iconImage.enabled = hasItem;
        }

        if (countText)
            countText.text = (hasItem && count > 1) ? count.ToString() : "";

        if (slotBackground)
            slotBackground.color = hasItem ? filledBgColor : emptyBgColor;

        if (selectedHighlight)
            selectedHighlight.SetActive(selected);
    }

    // ----- Drag & Drop -----
    public void OnBeginDrag(PointerEventData e)
    {
        if (wizard == null) return;
        if (!wizard.TryGetSlot(container, index, out var id, out var amt)) return;
        if (string.IsNullOrEmpty(id) || amt <= 0) return;

        sDragging = true;
        sFromContainer = container;
        sFromIndex = index;

        EnsureGhost();
        if (sGhostIcon) sGhostIcon.sprite = (itemDatabase ? itemDatabase.GetIcon(id) : null);
        if (sGhostCount) sGhostCount.text = amt > 1 ? amt.ToString() : "";
        sGhost.gameObject.SetActive(true);
        SetGhostPosition(e);
    }

    public void OnDrag(PointerEventData e)
    {
        if (!sDragging || sGhost == null) return;
        SetGhostPosition(e);
    }

    public void OnEndDrag(PointerEventData e)
    {
        HideGhost();
        sDragging = false;
    }

    public void OnDrop(PointerEventData e)
    {
        if (!sDragging || wizard == null) return;
        wizard.MoveOrMerge(sFromContainer, sFromIndex, container, index);
        HideGhost();
        sDragging = false;
    }

    void EnsureGhost()
    {
        if (sGhost != null) return;

        // Find the canvas containing this slot (active or inactive)
        sCanvas = GetComponentInParent<Canvas>(true);
        if (!sCanvas) return;

        var go = new GameObject("DragGhost", typeof(RectTransform), typeof(CanvasGroup), typeof(Image));
        go.transform.SetParent(sCanvas.transform, false);
        sGhost = (RectTransform)go.transform;
        sGhost.sizeDelta = new Vector2(40, 40);

        var img = go.GetComponent<Image>();
        img.raycastTarget = false;
        sGhostIcon = img;

        var cg = go.GetComponent<CanvasGroup>();
        cg.blocksRaycasts = false; // IMPORTANT: ghost must not block pointer
        cg.alpha = 0.9f;

        // count label
        var tgo = new GameObject("Count", typeof(RectTransform), typeof(Text));
        tgo.transform.SetParent(go.transform, false);
        var rt = (RectTransform)tgo.transform;
        rt.anchorMin = rt.anchorMax = new Vector2(1, 0);
        rt.anchoredPosition = new Vector2(-6, 6);
        sGhostCount = tgo.GetComponent<Text>();
        sGhostCount.alignment = TextAnchor.LowerRight;
        sGhostCount.fontSize = 18;
        sGhostCount.text = "";
        sGhostCount.raycastTarget = false;

        HideGhost();
    }

    void SetGhostPosition(PointerEventData e)
    {
        if (sGhost == null || sCanvas == null) return;

        var rtCanvas = (RectTransform)sCanvas.transform;

        if (sCanvas.renderMode == RenderMode.ScreenSpaceOverlay)
        {
            // Simple screen position
            sGhost.position = e.position;
        }
        else if (sCanvas.renderMode == RenderMode.ScreenSpaceCamera)
        {
            // Convert to local/anchored position relative to the canvas
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                rtCanvas, e.position, sCanvas.worldCamera, out var local);
            sGhost.anchoredPosition = local;
        }
        else // RenderMode.WorldSpace
        {
            // Place in world space using the canvas plane
            if (RectTransformUtility.ScreenPointToWorldPointInRectangle(
                rtCanvas, e.position, sCanvas.worldCamera, out var world))
            {
                sGhost.position = world;
            }
        }
    }

    void HideGhost()
    {
        if (sGhost != null) sGhost.gameObject.SetActive(false);
    }

    // Click to select hotbar slot
    public void OnPointerClick(PointerEventData eventData)
    {
        if (wizard == null) return;
        if (container == WizardController.SlotContainer.Hotbar)
            wizard.SelectHotbarIndex(index);
    }
}
