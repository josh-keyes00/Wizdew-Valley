using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Shop/Shop Catalog", fileName = "ShopCatalog")]
public class ShopCatalog : ScriptableObject
{
    [System.Serializable]
    public class Entry
    {
        [Tooltip("Must match an id in ItemDatabase.")]
        public string itemId;

        [Tooltip("Optional pretty name shown in UI.")]
        public string displayName;

        [Min(0)] public int buyPrice = 1;   // coins player pays to buy 1
        [Min(0)] public int sellPrice = 0;  // coins player receives for selling 1
    }

    public List<Entry> items = new List<Entry>();

    public Entry Find(string itemId)
    {
        for (int i = 0; i < items.Count; i++)
            if (items[i] != null && items[i].itemId == itemId)
                return items[i];
        return null;
    }
}
