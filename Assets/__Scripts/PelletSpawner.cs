using UnityEngine;
using System.Collections.Generic;

public class PelletSpawner : MonoBehaviour
{
    [Header("Pellet Prefabs")]
    public GameObject pelletYellowPrefab;
    public GameObject pelletPurplePrefab;
    public GameObject powerPelletYellowPrefab;
    public GameObject powerPelletPurplePrefab;

    [Header("Spawn Settings")]
    public int regularPelletsPerPlayer = 122;

    [Header("Play Area Boundaries")]
    [Tooltip("Minimum X column for spawning (excludes left corridor)")]
    public int minSpawnX = 2;
    [Tooltip("Maximum X column for spawning (excludes right corridor)")]
    public int maxSpawnX = 29;

    [Header("Ghost Room Exclusion")]
    public Vector2Int ghostRoomCenter = new Vector2Int(16, 15);
    public int ghostRoomRadius = 4;

    [Header("Power Pellet Corners")]
    public Vector2Int topLeftCorner = new Vector2Int(3, 29);
    public Vector2Int topRightCorner = new Vector2Int(28, 29);
    public Vector2Int bottomLeftCorner = new Vector2Int(3, 1);
    public Vector2Int bottomRightCorner = new Vector2Int(28, 1);

    private List<Vector2Int> availablePositions = new List<Vector2Int>();

    public void SpawnPellets()
    {
        FindAvailablePositions();

        if (availablePositions.Count == 0)
        {
            Debug.LogError("No valid spawn positions found!");
            return;
        }

        SpawnPowerPelletsAtCorners();

        SpawnPelletType(pelletYellowPrefab, regularPelletsPerPlayer);
        SpawnPelletType(pelletPurplePrefab, regularPelletsPerPlayer);

        Debug.Log($"Spawned pellets: {regularPelletsPerPlayer * 2} regular + 8 power");
    }

    void FindAvailablePositions()
    {
        availablePositions.Clear();

        int validPositions = 0;
        int excludedSideCorridors = 0;
        int excludedWalls = 0;
        int excludedGhostRoom = 0;

        for (int x = 0; x < GridManager.Instance.gridWidth; x++)
        {
            for (int z = 0; z < GridManager.Instance.gridHeight; z++)
            {
                Vector2Int gridPos = new Vector2Int(x, z);

                if (x < minSpawnX || x > maxSpawnX)
                {
                    excludedSideCorridors++;
                    continue;
                }

                if (!GridManager.Instance.IsWalkable(gridPos))
                {
                    excludedWalls++;
                    continue;
                }

                if (IsInsideGhostRoom(gridPos))
                {
                    excludedGhostRoom++;
                    continue;
                }

                availablePositions.Add(gridPos);
                validPositions++;
            }
        }

        Debug.Log($"Pellet spawn analysis:");
        Debug.Log($"   Valid spawn positions: {validPositions}");
        Debug.Log($"   Excluded (side corridors): {excludedSideCorridors}");
        Debug.Log($"   Excluded (walls): {excludedWalls}");
        Debug.Log($"   Excluded (ghost room): {excludedGhostRoom}");
    }

    bool IsInsideGhostRoom(Vector2Int gridPos)
    {
        int distanceX = Mathf.Abs(gridPos.x - ghostRoomCenter.x);
        int distanceZ = Mathf.Abs(gridPos.y - ghostRoomCenter.y);

        return (distanceX <= ghostRoomRadius && distanceZ <= ghostRoomRadius);
    }

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
            if (!GridManager.Instance.IsWalkable(corner))
            {
                Debug.LogWarning($"Corner {corner} is not walkable! Trying nearby position.");
                Vector2Int nearestWalkable = FindNearestWalkablePosition(corner);
                if (GridManager.Instance.IsWalkable(nearestWalkable))
                {
                    SpawnPowerPelletPair(nearestWalkable);
                    spawnedPowerPellets += 2;
                    availablePositions.Remove(nearestWalkable);
                }
                continue;
            }

            SpawnPowerPelletPair(corner);
            spawnedPowerPellets += 2;

            availablePositions.Remove(corner);
        }

        Debug.Log($"Spawned {spawnedPowerPellets} power pellets at corners");
    }

    void SpawnPowerPelletPair(Vector2Int gridPos)
    {
        Vector3 worldPos = GridManager.Instance.GridToWorld(gridPos);

        Instantiate(powerPelletYellowPrefab, worldPos + Vector3.left * 0.2f, Quaternion.identity, transform);
        Instantiate(powerPelletPurplePrefab, worldPos + Vector3.right * 0.2f, Quaternion.identity, transform);
    }

    Vector2Int FindNearestWalkablePosition(Vector2Int target)
    {
        for (int radius = 1; radius <= 5; radius++)
        {
            for (int dx = -radius; dx <= radius; dx++)
            {
                for (int dz = -radius; dz <= radius; dz++)
                {
                    Vector2Int testPos = new Vector2Int(target.x + dx, target.y + dz);

                    if (testPos.x < minSpawnX || testPos.x > maxSpawnX)
                        continue;

                    if (GridManager.Instance.IsWalkable(testPos) && !IsInsideGhostRoom(testPos))
                    {
                        return testPos;
                    }
                }
            }
        }

        return target;
    }

    void SpawnPelletType(GameObject prefab, int count)
    {
        int spawned = 0;

        for (int i = 0; i < count && availablePositions.Count > 0; i++)
        {
            int randomIndex = Random.Range(0, availablePositions.Count);
            Vector2Int gridPos = availablePositions[randomIndex];
            availablePositions.RemoveAt(randomIndex);

            Vector3 worldPos = GridManager.Instance.GridToWorld(gridPos);
            Instantiate(prefab, worldPos, Quaternion.identity, transform);
            spawned++;
        }

        if (spawned < count)
        {
            Debug.LogWarning($"Only spawned {spawned}/{count} pellets - not enough space!");
        }
    }
}