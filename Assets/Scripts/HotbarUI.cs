using UnityEngine;
using UnityEngine.InputSystem;   // New Input System
using UnityEngine.UI;

public class HotbarUI : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private WizardController wizard;
    [SerializeField] private ItemDatabase itemDatabase;

    [Header("Layout")]
    [SerializeField] private Transform slotsParent;    // parent with 9 HotbarSlotUI children
    [SerializeField] private HotbarSlotUI slotPrefab;  // optional: if you want to auto-instantiate
    [SerializeField] private int expectedSlots = 9;

    private HotbarSlotUI[] _slots;
    private float _refreshTimer;

    void Awake()
    {
        if (!wizard)
        {
            var p = GameObject.FindWithTag("Player");
            if (p) wizard = p.GetComponent<WizardController>();
        }

        // Gather/create slots
        if (slotsParent)
        {
            _slots = slotsParent.GetComponentsInChildren<HotbarSlotUI>(includeInactive: true);
        }

        if ((_slots == null || _slots.Length == 0) && slotPrefab && slotsParent)
        {
            _slots = new HotbarSlotUI[expectedSlots];
            for (int i = 0; i < expectedSlots; i++)
            {
                var s = Instantiate(slotPrefab, slotsParent);
                s.name = $"Slot_{i+1}";
                _slots[i] = s;
            }
        }
    }

    void Update()
    {
        // 1–9 number keys to select slots
        if (Keyboard.current != null)
        {
            if (Keyboard.current.digit1Key.wasPressedThisFrame) wizard?.SelectHotbarIndex(0);
            if (Keyboard.current.digit2Key.wasPressedThisFrame) wizard?.SelectHotbarIndex(1);
            if (Keyboard.current.digit3Key.wasPressedThisFrame) wizard?.SelectHotbarIndex(2);
            if (Keyboard.current.digit4Key.wasPressedThisFrame) wizard?.SelectHotbarIndex(3);
            if (Keyboard.current.digit5Key.wasPressedThisFrame) wizard?.SelectHotbarIndex(4);
            if (Keyboard.current.digit6Key.wasPressedThisFrame) wizard?.SelectHotbarIndex(5);
            if (Keyboard.current.digit7Key.wasPressedThisFrame) wizard?.SelectHotbarIndex(6);
            if (Keyboard.current.digit8Key.wasPressedThisFrame) wizard?.SelectHotbarIndex(7);
            if (Keyboard.current.digit9Key.wasPressedThisFrame) wizard?.SelectHotbarIndex(8);
        }

        // Light polling refresh (10x/sec) — simple and robust
        _refreshTimer -= Time.unscaledDeltaTime;
        if (_refreshTimer <= 0f)
        {
            _refreshTimer = 0.1f;
            Refresh();
        }
    }

    public void Refresh()
    {
        if (wizard == null || _slots == null) return;

        int n = Mathf.Min(_slots.Length, wizard.HotbarCount);
        for (int i = 0; i < n; i++)
        {
            string id; int amt;
            wizard.TryGetHotbarSlot(i, out id, out amt);
            var icon = (itemDatabase != null) ? itemDatabase.GetIcon(id) : null;
            bool selected = (i == wizard.SelectedHotbarIndex);
            _slots[i].SetData(icon, amt, selected);
        }

        // If UI has more slots than wizard, clear the extras
        for (int i = n; i < (_slots?.Length ?? 0); i++)
        {
            _slots[i].SetData(null, 0, false);
        }
    }
}
