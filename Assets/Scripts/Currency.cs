using UnityEngine;

public class Currency : MonoBehaviour
{
    public static Currency Instance { get; private set; }
    [Min(0)] public int gold = 100; // start money

    void Awake()
    {
        if (Instance && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public bool Spend(int amount)
    {
        if (amount <= 0) return true;
        if (gold < amount) return false;
        gold -= amount;
        return true;
    }

    public void Add(int amount)
    {
        if (amount > 0) gold += amount;
    }
}
