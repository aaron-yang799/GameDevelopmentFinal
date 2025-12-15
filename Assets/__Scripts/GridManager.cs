using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Manages the 28x31 grid system for classic Pac-Man layout.
/// Handles conversion between grid coordinates and world positions.
/// Detects walkable vs wall tiles using raycasts.
/// </summary>
public class GridManager : MonoBehaviour
{
    public static GridManager Instance;

    [Header("Grid Settings")]
    public int gridWidth = 28;
    public int gridHeight = 31;
    public float cellSize = 1f;

    private bool[,] walkableGrid; // true = walkable, false = wall

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        InitializeGrid();
    }

    /// Initializes the walkable grid by raycasting to detect walls.
    /// Only detects walls, ignores players and other objects.
    /// </summary>
    void InitializeGrid()
    {
        walkableGrid = new bool[gridWidth, gridHeight];

        // Mark all as walkable initially
        for (int x = 0; x < gridWidth; x++)
        {
            for (int y = 0; y < gridHeight; y++)
            {
                walkableGrid[x, y] = true;
            }
        }

        // Check for walls using raycasts from above
        for (int x = 0; x < gridWidth; x++)
        {
            for (int y = 0; y < gridHeight; y++)
            {
                Vector3 worldPos = GridToWorld(new Vector2Int(x, y));
                RaycastHit hit;

                // Cast ray from above to detect walls
                if (Physics.Raycast(worldPos + Vector3.up * 10, Vector3.down, out hit, 20f))
                {
                    // Only mark as wall if it's NOT a trigger AND NOT a player or ghost
                    if (!hit.collider.isTrigger &&
                        !hit.collider.CompareTag("Player") &&
                        !hit.collider.CompareTag("Ghost"))
                    {
                        walkableGrid[x, y] = false;
                    }
                }
            }
        }

        Debug.Log($"Grid initialized: {gridWidth}x{gridHeight}");
    }

    /// <summary>
    /// Converts grid coordinates to world position.
    /// Grid (0,0) is bottom-left, World (0,0,0) is center of map.
    /// </summary>
    public Vector3 GridToWorld(Vector2Int gridPos)
    {
        return new Vector3(
            (gridPos.x - 15.5f) * cellSize,  // Center at 15.5 for 32-width grid (0-31)
            0,
            (gridPos.y - 15f) * cellSize     // Center at 15 for 31-height grid (0-30)
        );
    }

    /// <summary>
    /// Converts world position to grid coordinates.
    /// For 32x31 grid.
    /// </summary>
    public Vector2Int WorldToGrid(Vector3 worldPos)
    {
        return new Vector2Int(
            Mathf.RoundToInt(worldPos.x / cellSize + 15.5f),
            Mathf.RoundToInt(worldPos.z / cellSize + 15f)
        );
    }

    /// <summary>
    /// Checks if a grid position is walkable (not a wall and within bounds).
    /// </summary>
    public bool IsWalkable(Vector2Int gridPos)
    {
        if (gridPos.x < 0 || gridPos.x >= gridWidth || gridPos.y < 0 || gridPos.y >= gridHeight)
            return false;

        return walkableGrid[gridPos.x, gridPos.y];
    }

    /// <summary>
    /// Gets all walkable neighboring cells (up, down, left, right).
    /// </summary>
    public List<Vector2Int> GetWalkableNeighbors(Vector2Int gridPos)
    {
        List<Vector2Int> neighbors = new List<Vector2Int>();
        Vector2Int[] directions = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };

        foreach (Vector2Int dir in directions)
        {
            Vector2Int neighbor = gridPos + dir;
            if (IsWalkable(neighbor))
            {
                neighbors.Add(neighbor);
            }
        }

        return neighbors;
    }

    /// <summary>
    /// Debug: Visualizes the grid in Scene view.
    /// </summary>
    void OnDrawGizmos()
    {
        if (walkableGrid == null) return;

        for (int x = 0; x < gridWidth; x++)
        {
            for (int y = 0; y < gridHeight; y++)
            {
                Vector3 pos = GridToWorld(new Vector2Int(x, y));
                Gizmos.color = walkableGrid[x, y] ? Color.green : Color.red;
                Gizmos.DrawWireCube(pos, Vector3.one * cellSize * 0.9f);
            }
        }
    }
}
