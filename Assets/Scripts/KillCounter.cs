using System;
using System.Collections.Generic;
using UnityEngine;

public class KillCounter : MonoBehaviour
{
    public static KillCounter Instance { get; private set; }

    private readonly Dictionary<string, int> _counts = new Dictionary<string, int>();

    public event Action<string,int> OnKill; // enemyId, newTotal

    void Awake()
    {
        if (Instance && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void RegisterKill(string enemyId)
    {
        if (string.IsNullOrEmpty(enemyId)) return;
        _counts.TryGetValue(enemyId, out int c);
        c++;
        _counts[enemyId] = c;
        OnKill?.Invoke(enemyId, c);
    }

    public int GetTotal(string enemyId)
    {
        return _counts.TryGetValue(enemyId, out int c) ? c : 0;
    }
}
