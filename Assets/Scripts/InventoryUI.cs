using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

public class InventoryUI : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private WizardController wizard;
    [SerializeField] private ItemDatabase itemDatabase;

    [Header("Layout")]
    [SerializeField] private Transform slotsParent;   // Grid parent
    [SerializeField] private HotbarSlotUI slotPrefab; // reuse the same slot prefab
    [SerializeField] private int expectedSlots = 27;

    [Header("Open/Close")]
    [Tooltip("Panel GameObject to show/hide. If left blank, this GameObject is used.")]
    [SerializeField] private GameObject rootPanel;    // Panel to show/hide
    [SerializeField] private Key toggleKey = Key.Tab; // Local toggle key if this component is on an active host

    [Header("Global toggle")]
    [Tooltip("If true, pressing Tab anywhere will toggle this inventory (works even if this panel starts inactive).")]
    [SerializeField] private bool isPrimaryForGlobalToggle = true;

    private HotbarSlotUI[] _slots;
    private bool _subscribed;

    // -------------- Lifecycle --------------
    void Awake()
    {
        if (!rootPanel) rootPanel = gameObject;
        BuildSlotsIfPossible(); // ok to run even if panel inactive
    }

    void OnEnable()
    {
        Subscribe();
        Refresh();
    }

    void OnDisable()
    {
        Unsubscribe();
    }

    void Update()
    {
        // This only runs if THIS component is on an active object (e.g., Canvas).
        // If it's on the inactive panel, our global watcher handles the key instead.
        if (Keyboard.current != null && Keyboard.current[toggleKey].wasPressedThisFrame)
            Toggle();
    }

    // -------------- Public API --------------
    public void Toggle()
    {
        if (!rootPanel) rootPanel = gameObject;

        bool opening = !rootPanel.activeSelf;

        if (opening)
        {
            // Make sure we can build and refresh even if Awake wasn't called yet (panel was inactive).
            LazyInitIfPossible();
        }

        rootPanel.SetActive(opening);

        // If we live on an always-active host, refresh now.
        // If this component is on the panel, OnEnable() will run right after SetActive(true) and call Refresh() anyway.
        if (opening && _slots != null) Refresh();

        // Remember for global toggler
        if (opening) _lastToggled = this;
    }

    public void Open()
    {
        if (!IsOpen) Toggle();
    }

    public void Close()
    {
        if (IsOpen) Toggle();
    }

    public bool IsOpen => rootPanel && rootPanel.activeSelf;

    // -------------- Internal --------------
    private void Subscribe()
    {
        if (!_subscribed && wizard != null)
        {
            wizard.onInventoryChanged.AddListener(Refresh);
            _subscribed = true;
        }
    }

    private void Unsubscribe()
    {
        if (_subscribed && wizard != null)
        {
            wizard.onInventoryChanged.RemoveListener(Refresh);
            _subscribed = false;
        }
    }

    private void LazyInitIfPossible()
    {
        // Find wizard if missing
        if (!wizard)
        {
            var p = GameObject.FindWithTag("Player");
            if (p) wizard = p.GetComponent<WizardController>();
        }

        // Find slotsParent if missing (look for a GridLayoutGroup under rootPanel)
        if (!slotsParent && rootPanel)
        {
            var grid = rootPanel.GetComponentInChildren<GridLayoutGroup>(true);
            if (grid) slotsParent = grid.transform;
        }

        // (Re)build slots if needed
        if (_slots == null && slotsParent != null && slotPrefab != null)
        {
            BuildSlots();
        }

        // Make sure inventory change events will update us
        if (isActiveAndEnabled) Subscribe();
    }

    private void BuildSlotsIfPossible()
    {
        // Called in Awake (may be on active host or on the panel itself)
        if (!wizard)
        {
            var p = GameObject.FindWithTag("Player");
            if (p) wizard = p.GetComponent<WizardController>();
        }
        if (!rootPanel) rootPanel = gameObject;
        if (!slotsParent && rootPanel)
        {
            var grid = rootPanel.GetComponentInChildren<GridLayoutGroup>(true);
            if (grid) slotsParent = grid.transform;
        }
        if (slotsParent && slotPrefab)
        {
            BuildSlots();
        }
    }

    private void BuildSlots()
    {
        if (!slotsParent || !slotPrefab) return;

        // Clear old
        for (int i = slotsParent.childCount - 1; i >= 0; i--)
            Destroy(slotsParent.GetChild(i).gameObject);

        int count = wizard ? wizard.BackpackCount : expectedSlots;
        _slots = new HotbarSlotUI[count];
        for (int i = 0; i < count; i++)
        {
            var s = Instantiate(slotPrefab, slotsParent);
            s.name = $"Bag_{i+1}";
            s.Bind(wizard, itemDatabase, WizardController.SlotContainer.Backpack, i);
            _slots[i] = s;
        }
    }

    public void Refresh()
    {
        if (wizard == null || _slots == null) return;

        int n = Mathf.Min(_slots.Length, wizard.BackpackCount);
        for (int i = 0; i < n; i++)
        {
            wizard.TryGetBackpackSlot(i, out var id, out var amt);
            var icon = itemDatabase ? itemDatabase.GetIcon(id) : null;
            _slots[i].SetData(icon, amt, false);
        }
    }

    // -------------- Global toggle support --------------
    private static InventoryUI _lastToggled;  // prefer the one the player used last

    internal static void ToggleAny()
    {
        // Prefer last toggled if still valid
        if (_lastToggled != null)
        {
            _lastToggled.Toggle();
            return;
        }

        // Otherwise find a primary inventory in the scene (include inactive)
#if UNITY_2023_1_OR_NEWER
        var all = Object.FindObjectsByType<InventoryUI>(FindObjectsSortMode.None);
        // FindObjectsByType doesn't include inactive; fall back to classic API below.
        var list = new List<InventoryUI>(all);
        if (list.Count == 0)
        {
            var includeInactive = Object.FindObjectsOfType<InventoryUI>(true);
            foreach (var ui in includeInactive) list.Add(ui);
        }
#else
        var includeInactive = Object.FindObjectsOfType<InventoryUI>(true);
        var list = new List<InventoryUI>(includeInactive);
#endif

        InventoryUI primary = null;
        foreach (var ui in list)
        {
            if (ui.isPrimaryForGlobalToggle) { primary = ui; break; }
        }
        if (primary == null && list.Count > 0) primary = list[0];
        if (primary != null)
        {
            _lastToggled = primary;
            primary.Toggle();
        }
    }
}

// This tiny watcher is auto-created at runtime and listens for Tab globally.
// It lets you keep InventoryUI on an inactive panel OR on an active Canvas—both work.
public class InventoryUIWatcher : MonoBehaviour
{
    public Key globalKey = Key.Tab;

    void Update()
    {
        var kbd = Keyboard.current;
        if (kbd != null && kbd[globalKey].wasPressedThisFrame)
        {
            InventoryUI.ToggleAny();
        }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void AutoCreate()
    {
        var go = new GameObject("[InventoryUIWatcher]");
        DontDestroyOnLoad(go);
        go.AddComponent<InventoryUIWatcher>();
    }
}
