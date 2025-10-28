using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Wizdew/Seeds/Seed Database", fileName = "SeedDatabase")]
public class SeedDatabase : ScriptableObject
{
    public List<SeedItem> seeds = new List<SeedItem>();

    private Dictionary<string, SeedItem> _map;

    private void OnEnable()
    {
        _map = new Dictionary<string, SeedItem>();
        foreach (var s in seeds)
        {
            if (!s) continue;
            if (string.IsNullOrEmpty(s.id)) continue;
            if (!_map.ContainsKey(s.id)) _map[s.id] = s;
        }
    }

    public bool TryGetById(string id, out SeedItem seed)
    {
        if (_map == null) OnEnable();
        return _map.TryGetValue(id, out seed);
    }
}
