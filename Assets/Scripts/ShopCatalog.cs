using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Wizdew/Shop/Shop Catalog", fileName = "ShopCatalog")]
public class ShopCatalog : ScriptableObject
{
    public List<SeedItem> seeds = new List<SeedItem>();
}
