using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Inventory/Item Database", fileName = "ItemDatabase")]
public class ItemDatabase : ScriptableObject
{
    [System.Serializable]
    public class Entry
    {
        public string id;
        public Sprite icon;
    }

    public Sprite defaultIcon;
    public List<Entry> entries = new List<Entry>();

    private Dictionary<string, Sprite> _map;

    private void OnEnable()
    {
        _map = new Dictionary<string, Sprite>();
        foreach (var e in entries)
        {
            if (!string.IsNullOrEmpty(e.id) && e.icon && !_map.ContainsKey(e.id))
                _map[e.id] = e.icon;
        }
    }

    public Sprite GetIcon(string id)
    {
        if (string.IsNullOrEmpty(id)) return null;
        if (_map == null) OnEnable();
        return _map.TryGetValue(id, out var s) ? s : defaultIcon;
    }
}
