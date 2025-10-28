using UnityEngine;
using UnityEngine.SceneManagement;

public class Plant : MonoBehaviour
{
    [Header("Visuals")]
    public SpriteRenderer spriteRenderer;
    public Sprite[] stageSprites;

    private SeedItem _seed;
    private Vector3Int _cell;
    private PlantingGrid _grid;

    private int _stage = 0;
    private int _baselineKills = 0;
    private string _sceneName;
    private string _requiredKey; // normalized enemy id

    public int CurrentStage => _stage;
    public bool IsMature => _seed && _stage >= (_seed.stages - 1);

    // New planting
    public void Init(SeedItem seed, Vector3Int cell, PlantingGrid grid)
    {
        _sceneName = SceneManager.GetActiveScene().name;
        _seed = seed; _cell = cell; _grid = grid;
        _requiredKey = KillCounter.Normalize(_seed.requiredEnemyId);

        if (KillCounter.Instance != null)
        {
            KillCounter.Instance.OnKill += HandleKill;
            _baselineKills = KillCounter.Instance.GetTotal(_requiredKey);
        }

        _stage = 0;
        UpdateVisual();
        PlantSave.Instance?.Upsert(_sceneName, _seed.id, _cell, _stage);
    }

    // Restore from save (NO Upsert here!)
    public void InitFromSave(SeedItem seed, Vector3Int cell, PlantingGrid grid, int savedStage)
    {
        _sceneName = SceneManager.GetActiveScene().name;
        _seed = seed; _cell = cell; _grid = grid;
        _requiredKey = KillCounter.Normalize(_seed.requiredEnemyId);

        if (KillCounter.Instance != null)
        {
            KillCounter.Instance.OnKill += HandleKill;
            int totalNow = KillCounter.Instance.GetTotal(_requiredKey);
            _stage = Mathf.Clamp(savedStage, 0, _seed.stages - 1);
            // Rebuild a baseline so further kills advance naturally
            _baselineKills = Mathf.Max(0, totalNow - (_stage * _seed.killsPerStage));
        }

        UpdateVisual();
        // DO NOT Upsert here (we're restoring from an existing record).
    }

    private void OnDestroy()
    {
        if (KillCounter.Instance != null)
            KillCounter.Instance.OnKill -= HandleKill;
    }

    private void HandleKill(string enemyKey, int totalNow)
    {
        if (enemyKey != _requiredKey || _seed == null) return;

        int gained = totalNow - _baselineKills;
        int newStage = Mathf.Clamp(gained / _seed.killsPerStage, 0, _seed.stages - 1);
        if (newStage != _stage)
        {
            _stage = newStage;
            UpdateVisual();
            PlantSave.Instance?.Upsert(_sceneName, _seed.id, _cell, _stage);
        }
    }

    private void UpdateVisual()
    {
        if (spriteRenderer && stageSprites != null && stageSprites.Length > 0)
        {
            int idx = Mathf.Clamp(_stage, 0, stageSprites.Length - 1);
            if (stageSprites[idx]) spriteRenderer.sprite = stageSprites[idx];
        }
    }

    private void OnMouseDown() { TryHarvest(); }

    public bool TryHarvest()
    {
        if (_seed == null || !IsMature) return false;

        var player = FindObjectOfType<WizardController>();
        if (!player) return false;

        player.TryAddItem(_seed.harvestItemId, _seed.harvestYield);

        _grid?.FreeCell(_cell);
        PlantSave.Instance?.Remove(_sceneName, _cell);
        Destroy(gameObject);
        return true;
    }
}
