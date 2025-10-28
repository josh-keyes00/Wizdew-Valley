using System;
using System.Collections.Generic;
using UnityEngine;

public class KillCounter : MonoBehaviour
{
    public static KillCounter Instance { get; private set; }
    public static string Normalize(string s) => string.IsNullOrEmpty(s) ? "" : s.Trim().ToLowerInvariant();

    public event Action<string,int> OnKill;

    // Case-insensitive keys
    private readonly Dictionary<string,int> totals = new Dictionary<string,int>(StringComparer.OrdinalIgnoreCase);

    private void Awake()
    {
        // Make sure we're root so DontDestroyOnLoad always works
        if (transform.parent != null) transform.SetParent(null);

        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        DontDestroyOnLoad(gameObject);
    }

    public void RegisterKill(string enemyId, int amount = 1)
    {
        var key = Normalize(enemyId);
        if (string.IsNullOrEmpty(key) || amount <= 0) return;

        totals.TryGetValue(key, out var t);
        t += amount;
        totals[key] = t;

        OnKill?.Invoke(key, t);
    }

    public int GetTotal(string enemyId)
    {
        var key = Normalize(enemyId);
        return totals.TryGetValue(key, out var t) ? t : 0;
    }

    public void ResetEnemy(string enemyId) => totals.Remove(Normalize(enemyId));
    public void ResetAll() => totals.Clear();
}
