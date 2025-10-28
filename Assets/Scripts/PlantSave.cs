using System;
using System.Collections.Generic;
using UnityEngine;

public class PlantSave : MonoBehaviour
{
    public static PlantSave Instance { get; private set; }

    [Serializable]
    public class PlantRecord
    {
        public string scene;      // scene name
        public string seedId;     // SeedItem.id
        public Vector3Int cell;   // tile cell
        public int stage;         // current growth stage
    }

    // scene -> (cell -> record)
    private readonly Dictionary<string, Dictionary<Vector3Int, PlantRecord>> _byScene
        = new Dictionary<string, Dictionary<Vector3Int, PlantRecord>>();

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void Upsert(string scene, string seedId, Vector3Int cell, int stage)
    {
        if (string.IsNullOrEmpty(scene) || string.IsNullOrEmpty(seedId)) return;
        if (!_byScene.TryGetValue(scene, out var map))
        {
            map = new Dictionary<Vector3Int, PlantRecord>();
            _byScene[scene] = map;
        }
        map[cell] = new PlantRecord { scene = scene, seedId = seedId, cell = cell, stage = stage };
    }

    public void Remove(string scene, Vector3Int cell)
    {
        if (string.IsNullOrEmpty(scene)) return;
        if (_byScene.TryGetValue(scene, out var map)) map.Remove(cell);
    }

    public bool TryGetAll(string scene, out IEnumerable<PlantRecord> records)
    {
        records = null;
        if (string.IsNullOrEmpty(scene)) return false;
        if (_byScene.TryGetValue(scene, out var map))
        {
            records = map.Values;
            return true;
        }
        return false;
    }
}
