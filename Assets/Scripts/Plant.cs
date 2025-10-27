using UnityEngine;

public class Plant : MonoBehaviour
{
    [Header("Visuals")]
    public SpriteRenderer spriteRenderer;
    public Sprite[] stageSprites; // optional: size == seed.stages (can be empty)

    private SeedItem _seed;
    private Vector3Int _cell;
    private PlantingGrid _grid;

    private int _stage = 0;          // 0..(stages-1)
    private int _baselineKills = 0;  // kills at the moment of planting

    public bool IsMature => _stage >= (_seed ? _seed.stages : 1) - 1;

    public void Init(SeedItem seed, Vector3Int cell, PlantingGrid grid)
    {
        _seed = seed; _cell = cell; _grid = grid;

        // subscribe to global kills
        KillCounter.Instance.OnKill += HandleKill;

        // baseline when planted
        _baselineKills = KillCounter.Instance.GetTotal(_seed.requiredEnemyId);

        UpdateVisual();
    }

    void OnDestroy()
    {
        if (KillCounter.Instance != null)
            KillCounter.Instance.OnKill -= HandleKill;
    }

    void HandleKill(string enemyId, int totalNow)
    {
        if (_seed == null || enemyId != _seed.requiredEnemyId) return;

        int gainedSincePlant = totalNow - _baselineKills;
        int newStage = Mathf.Clamp(gainedSincePlant / _seed.killsPerStage, 0, _seed.stages - 1);
        if (newStage != _stage)
        {
            _stage = newStage;
            UpdateVisual();
        }
    }

    void UpdateVisual()
    {
        if (spriteRenderer && stageSprites != null && stageSprites.Length > 0)
        {
            int idx = Mathf.Clamp(_stage, 0, stageSprites.Length - 1);
            if (stageSprites[idx]) spriteRenderer.sprite = stageSprites[idx];
        }
    }

    // Simple harvest: call via trigger/key or OnMouseDown for now
    private void OnMouseDown()
    {
        TryHarvest();
    }

    public bool TryHarvest()
    {
        if (_seed == null || !IsMature) return false;

        var player = FindObjectOfType<WizardController>();
        if (!player) return false;

        player.TryAddItem(_seed.harvestItemId, _seed.harvestYield);

        // free cell and destroy plant
        _grid?.FreeCell(_cell);
        Destroy(gameObject);
        return true;
    }
}
