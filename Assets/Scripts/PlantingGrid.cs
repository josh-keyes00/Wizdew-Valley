using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;     // New Input System
using UnityEngine.Tilemaps;

public class PlantingGrid : MonoBehaviour
{
    [Header("Refs")]
    public Tilemap tilledSoil;          // your pre-tilled tilemap
    public GameObject plantPrefab;      // Plant prefab (has Plant.cs)
    public SeedDatabase seedDatabase;   // to resolve hotbar itemId -> SeedItem
    public WizardController wizard;     // player (auto-find if null)
    public Camera worldCamera;          // aim camera (auto-fill Camera.main)

    [Header("Controls")]
    // FIX: use Input System's Key instead of old KeyCode
    public Key plantKey = Key.F;        // press to plant selected seed at mouse cell

    private readonly HashSet<Vector3Int> occupiedCells = new HashSet<Vector3Int>();

    void Awake()
    {
        if (!wizard)
        {
            var p = GameObject.FindWithTag("Player");
            if (p) wizard = p.GetComponent<WizardController>();
        }
        if (!worldCamera) worldCamera = Camera.main;
    }

    void Update()
    {
        if (Keyboard.current == null) return;

        // FIX: index with Input System Key
        if (Keyboard.current[plantKey].wasPressedThisFrame)
            TryPlantAtMouse();
    }

    void TryPlantAtMouse()
    {
        if (!wizard || !tilledSoil || !plantPrefab || seedDatabase == null) return;
        if (worldCamera == null) worldCamera = Camera.main;
        if (Mouse.current == null) return;

        var stack = wizard.SelectedHotbarStack;
        if (stack == null || stack.IsEmpty) return;

        if (!seedDatabase.TryGetById(stack.itemId, out var seed)) return; // not a seed

        Vector2 mouseScreen = Mouse.current.position.ReadValue();
        Vector3 world = worldCamera.ScreenToWorldPoint(new Vector3(mouseScreen.x, mouseScreen.y, 0f));
        Vector3Int cell = tilledSoil.WorldToCell(world);

        // Must be on the tilled tilemap and unoccupied
        if (!tilledSoil.HasTile(cell)) return;
        if (occupiedCells.Contains(cell)) return;

        // place plant at cell center
        Vector3 placePos = tilledSoil.GetCellCenterWorld(cell);
        var go = Instantiate(plantPrefab, placePos, Quaternion.identity);
        var plant = go.GetComponent<Plant>();
        if (plant) plant.Init(seed, cell, this);

        occupiedCells.Add(cell);

        // consume one seed from inventory
        wizard.RemoveItem(seed.id, 1);
    }

    public void FreeCell(Vector3Int cell)
    {
        occupiedCells.Remove(cell);
    }
}
