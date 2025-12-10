using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Spawns pellets across the map at walkable grid positions.
/// Power pellets spawn at the 4 corners.
/// Regular pellets spawn randomly, excluding the ghost room.
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
    
    [Header("Ghost Room Exclusion")]
    public Vector2Int ghostRoomCenter = new Vector2Int(14, 13); // Adjust based on YOUR ghost room
    public int ghostRoomRadius = 4; // Exclude cells within this radius
    
    [Header("Corner Positions")]
    public Vector2Int topLeftCorner = new Vector2Int(1, 29);
    public Vector2Int topRightCorner = new Vector2Int(26, 29);
    public Vector2Int bottomLeftCorner = new Vector2Int(1, 1);
    public Vector2Int bottomRightCorner = new Vector2Int(26, 1);
    
    private List<Vector2Int> availablePositions = new List<Vector2Int>();
    
    /// <summary>
    /// Spawns all pellets. Call this from GameManager.Start()
    /// </summary>
    public void SpawnPellets()
    {
        FindAvailablePositions();
        
        // Spawn power pellets at corners FIRST
        SpawnPowerPelletsAtCorners();
        
        // Then spawn regular pellets randomly
        SpawnPelletType(pelletYellowPrefab, regularPelletsPerPlayer);
        SpawnPelletType(pelletPurplePrefab, regularPelletsPerPlayer);
        
        Debug.Log($"Spawned pellets: {regularPelletsPerPlayer * 2} regular + power pellets at corners");
    }
    
    /// <summary>
    /// Finds all walkable grid positions, excluding the ghost room.
    /// </summary>
    void FindAvailablePositions()
    {
        availablePositions.Clear();
        
        for (int x = 0; x < GridManager.Instance.gridWidth; x++)
        {
            for (int y = 0; y < GridManager.Instance.gridHeight; y++)
            {
                Vector2Int gridPos = new Vector2Int(x, y);
                
                // Check if walkable
                if (GridManager.Instance.IsWalkable(gridPos))
                {
                    // Check if inside ghost room (exclude it)
                    if (IsInsideGhostRoom(gridPos))
                    {
                        continue; // Skip this position
                    }
                    
                    availablePositions.Add(gridPos);
                }
            }
        }
        
        Debug.Log($"Found {availablePositions.Count} walkable positions (excluding ghost room)");
    }
    
    /// <summary>
    /// Checks if a grid position is inside the ghost room.
    /// </summary>
    bool IsInsideGhostRoom(Vector2Int gridPos)
    {
        // Calculate distance from ghost room center
        int distanceX = Mathf.Abs(gridPos.x - ghostRoomCenter.x);
        int distanceY = Mathf.Abs(gridPos.y - ghostRoomCenter.y);
        
        // Check if within radius (using Manhattan distance for rectangular exclusion)
        return (distanceX <= ghostRoomRadius && distanceY <= ghostRoomRadius);
    }
    
    /// <summary>
    /// Spawns power pellets at the 4 corners if they're walkable.
    /// Each corner gets 1 yellow + 1 purple power pellet = 8 total (4 per player).
    /// </summary>
    void SpawnPowerPelletsAtCorners()
    {
        Vector2Int[] corners = new Vector2Int[]
        {
            topLeftCorner,
            topRightCorner,
            bottomLeftCorner,
            bottomRightCorner
        };
        
        int spawnedPowerPellets = 0;
        
        foreach (Vector2Int corner in corners)
        {
            // Check if this corner position is walkable
            if (!GridManager.Instance.IsWalkable(corner))
            {
                Debug.LogWarning($"Corner {corner} is not walkable! Power pellets will spawn at nearest walkable cell.");
                
                // Try to find nearest walkable position near this corner
                Vector2Int nearestWalkable = FindNearestWalkablePosition(corner);
                if (nearestWalkable != corner)
                {
                    SpawnPowerPelletPair(nearestWalkable);
                    spawnedPowerPellets += 2;
                }
                continue;
            }
            
            // Spawn power pellet pair at this corner
            SpawnPowerPelletPair(corner);
            spawnedPowerPellets += 2;
            
            // Remove this position from available positions
            availablePositions.Remove(corner);
        }
        
        Debug.Log($"Spawned {spawnedPowerPellets} power pellets at corners (4 per player)");
    }
    
    /// <summary>
    /// Spawns one yellow and one purple power pellet at a position.
    /// </summary>
    void SpawnPowerPelletPair(Vector2Int gridPos)
    {
        Vector3 worldPos = GridManager.Instance.GridToWorld(gridPos);
        
        // Spawn slightly offset so they don't overlap completely
        Instantiate(powerPelletYellowPrefab, worldPos + Vector3.left * 0.2f, Quaternion.identity, transform);
        Instantiate(powerPelletPurplePrefab, worldPos + Vector3.right * 0.2f, Quaternion.identity, transform);
    }
    
    /// <summary>
    /// Finds the nearest walkable position to a target grid position.
    /// </summary>
    Vector2Int FindNearestWalkablePosition(Vector2Int target)
    {
        // Search in expanding radius
        for (int radius = 1; radius <= 5; radius++)
        {
            for (int dx = -radius; dx <= radius; dx++)
            {
                for (int dy = -radius; dy <= radius; dy++)
                {
                    Vector2Int testPos = new Vector2Int(target.x + dx, target.y + dy);
                    if (GridManager.Instance.IsWalkable(testPos) && !IsInsideGhostRoom(testPos))
                    {
                        return testPos;
                    }
                }
            }
        }
        
        return target; // Fallback
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