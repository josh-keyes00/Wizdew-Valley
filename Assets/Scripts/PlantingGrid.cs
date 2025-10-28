using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Tilemaps;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

public class PlantingGrid : MonoBehaviour
{
    [Header("Refs (auto-bound)")]
    [SerializeField] private WizardController wizard;   // auto-find
    [SerializeField] private Camera worldCamera;        // auto-bind to Camera.main

    [Header("Tiles & Prefabs")]
    public Tilemap tilledSoil;          // assign in Inspector
    public GameObject plantPrefab;      // assign in Inspector (has Plant)
    public SeedDatabase seedDatabase;   // assign in Inspector

    [Header("Controls")]
    public Key plantKey = Key.F;

    [Header("Auto-Find Settings")]
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private bool autoBindWizard = true;
    [SerializeField] private bool autoBindCamera = true;

    private readonly HashSet<Vector3Int> occupiedCells = new HashSet<Vector3Int>();
    private float _retryTimer;
    private const float RetryInterval = 0.5f;

    private void Awake()
    {
        ResolveWizard();
        ResolveCamera();
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
        // Also try restoring immediately if this grid is enabled after scene is already loaded
        RestoreFromSave();
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene s, LoadSceneMode m)
    {
        if (autoBindCamera) ResolveCamera(force: true);
        if (autoBindWizard && wizard == null) ResolveWizard(force: true);
        RestoreFromSave(); // <--- restore on every entry
    }

    private void Update()
    {
        _retryTimer -= Time.unscaledDeltaTime;
        if (_retryTimer <= 0f)
        {
            if (autoBindWizard && wizard == null) ResolveWizard();
            if (autoBindCamera && (worldCamera == null || !worldCamera.isActiveAndEnabled)) ResolveCamera();
            _retryTimer = RetryInterval;
        }

        if (Keyboard.current == null) return;
        if (Keyboard.current[plantKey].wasPressedThisFrame)
            TryPlantAtMouse();
    }

    private void TryPlantAtMouse()
    {
        if (!tilledSoil || !plantPrefab || seedDatabase == null) return;
        if (!wizard) { ResolveWizard(); if (!wizard) return; }
        if (!worldCamera) { ResolveCamera(); if (!worldCamera) return; }
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;
        if (Mouse.current == null) return;

        var stack = wizard.SelectedHotbarStack;
        if (stack == null || stack.IsEmpty) return;
        if (!seedDatabase.TryGetById(stack.itemId, out var seed)) return;

        Vector2 mouseScreen = Mouse.current.position.ReadValue();
        Vector3 world = worldCamera.ScreenToWorldPoint(new Vector3(mouseScreen.x, mouseScreen.y, 0f));
        Vector3Int cell = tilledSoil.WorldToCell(world);

        if (!tilledSoil.HasTile(cell)) return;
        if (occupiedCells.Contains(cell)) return;

        Vector3 placePos = tilledSoil.GetCellCenterWorld(cell);
        var go = Instantiate(plantPrefab, placePos, Quaternion.identity);
        var plant = go.GetComponent<Plant>();
        if (plant) plant.Init(seed, cell, this);

        occupiedCells.Add(cell);
        wizard.RemoveItem(seed.id, 1);
    }

    public void FreeCell(Vector3Int cell) => occupiedCells.Remove(cell);

    // -------- Restore from PlantSave --------
// ... (top of file unchanged)

    private void RestoreFromSave()
    {
        occupiedCells.Clear();

        if (tilledSoil == null || plantPrefab == null || seedDatabase == null) return;
        if (PlantSave.Instance == null) return;

        string sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        if (!PlantSave.Instance.TryGetAll(sceneName, out var records)) return;

        // SNAPSHOT records FIRST so we don't modify the dictionary while iterating
        var snapshot = new List<PlantSave.PlantRecord>(records);

        foreach (var rec in snapshot)
        {
            if (!seedDatabase.TryGetById(rec.seedId, out var seed)) continue;
            if (!tilledSoil.HasTile(rec.cell)) continue;
            if (occupiedCells.Contains(rec.cell)) continue;

            Vector3 pos = tilledSoil.GetCellCenterWorld(rec.cell);
            var go = Instantiate(plantPrefab, pos, Quaternion.identity);
            var plant = go.GetComponent<Plant>();
            if (plant) plant.InitFromSave(seed, rec.cell, this, rec.stage);

            occupiedCells.Add(rec.cell);
        }
    }


    // -------- Auto-binding helpers --------
    private void ResolveWizard(bool force = false)
    {
        if (!autoBindWizard && !force) return;
        if (wizard != null && !force) return;

        GameObject p = null;
        if (!string.IsNullOrEmpty(playerTag))
            p = GameObject.FindGameObjectWithTag(playerTag);

        wizard = p ? p.GetComponent<WizardController>() : FindObjectOfType<WizardController>(true);
    }

    private void ResolveCamera(bool force = false)
    {
        if (!autoBindCamera && !force) return;
        if (worldCamera != null && worldCamera.isActiveAndEnabled && !force) return;

        worldCamera = Camera.main;
        if (worldCamera == null)
        {
            foreach (var c in FindObjectsOfType<Camera>(true))
            {
                if (c.isActiveAndEnabled) { worldCamera = c; break; }
            }
        }
    }
    
}
