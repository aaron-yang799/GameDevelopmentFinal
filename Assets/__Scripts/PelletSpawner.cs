using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Spawns pellets across the map at walkable grid positions.
/// Power pellets spawn at the 4 corners (classic Pac-Man style).
/// Regular pellets spawn randomly in remaining walkable spaces.
/// </summary>
public class PelletSpawner : MonoBehaviour
{
    [Header("Pellet Prefabs")]
    public GameObject pelletYellowPrefab;
    public GameObject pelletPurplePrefab;
    public GameObject powerPelletYellowPrefab;
    public GameObject powerPelletPurplePrefab;
    
    [Header("Spawn Settings")]
    public int regularPelletsPerPlayer = 100;
    public int powerPelletsPerPlayer = 2; // 4 corners total, 2 per player
    
    private List<Vector2Int> availablePositions = new List<Vector2Int>();
    
    /// <summary>
    /// Spawns all pellets. Call this from GameManager.Start()
    /// </summary>
    public void SpawnPellets()
    {
        FindAvailablePositions();
        
        // Spawn power pellets at fixed corners first
        SpawnPowerPelletsAtCorners();
        
        // Spawn regular pellets randomly
        SpawnPelletType(pelletYellowPrefab, regularPelletsPerPlayer);
        SpawnPelletType(pelletPurplePrefab, regularPelletsPerPlayer);
        
        Debug.Log($"Spawned pellets: {regularPelletsPerPlayer * 2} regular + {powerPelletsPerPlayer * 2} power");
    }
    
    /// <summary>
    /// Finds all walkable grid positions where pellets can spawn.
    /// </summary>
    void FindAvailablePositions()
    {
        availablePositions.Clear();
        
        for (int x = 0; x < GridManager.Instance.gridWidth; x++)
        {
            for (int y = 0; y < GridManager.Instance.gridHeight; y++)
            {
                Vector2Int gridPos = new Vector2Int(x, y);
                if (GridManager.Instance.IsWalkable(gridPos))
                {
                    availablePositions.Add(gridPos);
                }
            }
        }
        
        Debug.Log($"Found {availablePositions.Count} walkable positions");
    }
    
    /// <summary>
    /// Spawns power pellets at the 4 corners (classic Pac-Man positions).
    /// Each corner gets one yellow and one purple power pellet.
    /// </summary>
    void SpawnPowerPelletsAtCorners()
    {
        // Classic Pac-Man power pellet corners
        Vector2Int[] corners = new Vector2Int[]
        {
            new Vector2Int(3, 26),   // Top-left
            new Vector2Int(24, 26),  // Top-right
            new Vector2Int(3, 4),    // Bottom-left
            new Vector2Int(24, 4)    // Bottom-right
        };
        
        foreach (Vector2Int corner in corners)
        {
            Vector3 worldPos = GridManager.Instance.GridToWorld(corner);
            
            // Spawn one of each color at each corner (offset slightly so they don't overlap)
            Instantiate(powerPelletYellowPrefab, worldPos, Quaternion.identity, transform);
            Instantiate(powerPelletPurplePrefab, worldPos + Vector3.right * 0.3f, Quaternion.identity, transform);
            
            // Remove this position from available positions so regular pellets don't spawn here
            availablePositions.Remove(corner);
        }
        
        Debug.Log("Spawned power pellets at 4 corners");
    }
    
    /// <summary>
    /// Spawns a specific type of pellet randomly across available positions.
    /// </summary>
    void SpawnPelletType(GameObject prefab, int count)
    {
        int spawned = 0;
        
        for (int i = 0; i < count && availablePositions.Count > 0; i++)
        {
            // Pick random available position
            int randomIndex = Random.Range(0, availablePositions.Count);
            Vector2Int gridPos = availablePositions[randomIndex];
            availablePositions.RemoveAt(randomIndex);
            
            // Spawn pellet at this position
            Vector3 worldPos = GridManager.Instance.GridToWorld(gridPos);
            Instantiate(prefab, worldPos, Quaternion.identity, transform);
            spawned++;
        }
        
        if (spawned < count)
        {
            Debug.LogWarning($"Only spawned {spawned}/{count} pellets - not enough walkable space!");
        }
    }
}
