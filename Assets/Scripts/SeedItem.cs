using UnityEngine;

[CreateAssetMenu(menuName = "Wizdew/Seeds/Seed Item", fileName = "SeedItem")]
public class SeedItem : ScriptableObject
{
    [Header("Identity")]
    public string id;                 // inventory id used by WizardController (e.g., "seed_slime")
    public string displayName = "Seed";
    public Sprite icon;

    [Header("Prices")]
    public int buyPrice = 10;         // shop buy price
    public int sellPrice = 15;        // price for 1 harvest item

    [Header("Growth by Kills")]
    public string requiredEnemyId = "Slime";
    [Min(1)] public int stages = 3;       // total stages to become harvestable
    [Min(1)] public int killsPerStage = 3;// kills needed PER stage

    [Header("Harvest Result")]
    public string harvestItemId = "produce_slime";
    [Min(1)] public int harvestYield = 1; // how many items you get on harvest
}
