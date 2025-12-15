using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Ghost AI with pathfinding, random decision-making, and ghost house exit behavior.
/// Ghosts prioritize leaving the ghost house before chasing players.
/// Random decisions are configurable per ghost for difficulty tuning.
/// </summary>
public class GhostAI : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 4f;
    public float scaredSpeed = 2.5f;

    [Header("AI Settings")]
    public float pathRecalculateInterval = 0.3f;
    public int maxPathLength = 100;

    [Header("Random Behavior (Adjust Per Ghost for Difficulty)")]
    [Tooltip("How often ghost makes a random decision (seconds). Lower = more random")]
    public float randomDecisionInterval = 3f;
    [Tooltip("Chance (0-1) ghost moves randomly when interval triggers. Higher = easier for players")]
    [Range(0f, 1f)]
    public float randomDecisionChance = 0.5f;

    [Header("Ghost House Settings")]
    [Tooltip("Ghost house area to escape from")]
    public int ghostHouseMinX = 14;
    public int ghostHouseMaxX = 18;
    public int ghostHouseMinZ = 13;
    public int ghostHouseMaxZ = 17;
    [Tooltip("Target position outside ghost house (usually the exit)")]
    public Vector2Int ghostHouseExitTarget = new Vector2Int(16, 18);

    [Header("Respawn Settings")]
    public float respawnDelay = 6f;

    // Grid movement
    private Vector2Int currentGridPos;
    private Vector2Int targetGridPos;
    private Vector3 targetWorldPos;
    private bool isMoving = false;

    // AI state
    private bool isScared = false;
    private float pathRecalculateTimer = 0f;
    private float randomDecisionTimer = 0f;
    private bool shouldMoveRandomly = false;
    private bool hasLeftGhostHouse = false; // Tracks if ghost has escaped

    // Pathfinding
    private Queue<Vector2Int> currentPath = new Queue<Vector2Int>();
    private Vector2Int lastTargetPlayerPos;

    // Spawn position
    private Vector2Int spawnGridPos;

    // Respawn state
    private bool isRespawning = false;

    // Material for color changes
    private Renderer ghostRenderer;
    private Color normalColor = Color.red;
    private Color scaredColor = Color.blue;
    private Color respawnColor = new Color(0.5f, 0.5f, 0.5f, 0.5f);

    void Start()
    {
        ghostRenderer = GetComponent<Renderer>();

        // Set initial grid position
        currentGridPos = GridManager.Instance.WorldToGrid(transform.position);
        spawnGridPos = currentGridPos;
        targetGridPos = currentGridPos;
        targetWorldPos = transform.position;

        // Check if spawned outside ghost house
        CheckIfOutsideGhostHouse();

        // Start pathfinding immediately
        pathRecalculateTimer = pathRecalculateInterval;

        // Randomize initial decision timer so ghosts don't sync
        randomDecisionTimer = Random.Range(0f, randomDecisionInterval);

        Debug.Log($"Ghost spawned at grid {currentGridPos}, in ghost house: {!hasLeftGhostHouse}");
    }

    void Update()
    {
        if (GameManager.Instance == null) return;

        // Don't do anything while respawning
        if (isRespawning) return;

        // Check if we've left the ghost house
        if (!hasLeftGhostHouse)
        {
            CheckIfOutsideGhostHouse();
        }

        // Random decision timer
        randomDecisionTimer -= Time.deltaTime;
        if (randomDecisionTimer <= 0f)
        {
            randomDecisionTimer = randomDecisionInterval;

            // Roll the dice - should we move randomly?
            if (Random.value < randomDecisionChance)
            {
                shouldMoveRandomly = true;
            }
        }

        // Update path recalculation timer
        pathRecalculateTimer += Time.deltaTime;
        if (pathRecalculateTimer >= pathRecalculateInterval)
        {
            pathRecalculateTimer = 0f;
            CalculateNewPath();
        }

        // Movement logic
        if (!isMoving)
        {
            FollowPath();
        }
        else
        {
            Move();
        }
    }

    /// <summary>
    /// Checks if ghost is outside the ghost house.
    /// </summary>
    void CheckIfOutsideGhostHouse()
    {
        if (!IsInsideGhostHouse(currentGridPos))
        {
            hasLeftGhostHouse = true;
        }
    }

    /// <summary>
    /// Checks if a position is inside the ghost house.
    /// </summary>
    bool IsInsideGhostHouse(Vector2Int pos)
    {
        return pos.x >= ghostHouseMinX && pos.x <= ghostHouseMaxX &&
               pos.y >= ghostHouseMinZ && pos.y <= ghostHouseMaxZ;
    }

    /// <summary>
    /// Calculates a new path.
    /// Priority: 1) Exit ghost house, 2) Chase/flee from players
    /// </summary>
    void CalculateNewPath()
    {
        // If we should move randomly, clear path
        if (shouldMoveRandomly)
        {
            currentPath.Clear();
            shouldMoveRandomly = false;
            return;
        }

        // PRIORITY 1: If still in ghost house, path to exit
        if (!hasLeftGhostHouse)
        {
            currentPath = FindPath(currentGridPos, ghostHouseExitTarget);
            if (currentPath.Count == 0)
            {
                // Can't find path to exit, try moving towards it greedily
                MoveTowardsTarget(ghostHouseExitTarget);
            }
            return;
        }

        // PRIORITY 2: Normal chase/flee behavior
        Vector2Int targetPos = GetTargetPosition();

        // Recalculate if target moved or we have no path
        if (targetPos != lastTargetPlayerPos || currentPath.Count == 0)
        {
            lastTargetPlayerPos = targetPos;

            if (isScared)
            {
                // Scared: move away from players
                MoveAwayFromTarget(targetPos);
            }
            else
            {
                // Normal: chase the player
                currentPath = FindPath(currentGridPos, targetPos);

                if (currentPath.Count == 0)
                {
                    // No path found, try greedy approach
                    MoveTowardsTarget(targetPos);
                }
            }
        }
    }

    /// <summary>
    /// Gets the target position (nearest alive player).
    /// </summary>
    Vector2Int GetTargetPosition()
    {
        PlayerController player1 = GameManager.Instance.player1;
        PlayerController player2 = GameManager.Instance.player2;

        Vector2Int targetPos = currentGridPos;
        float minDistance = float.MaxValue;

        if (player1 != null && player1.isAlive)
        {
            float dist = Vector2Int.Distance(currentGridPos, player1.GetGridPosition());
            if (dist < minDistance)
            {
                minDistance = dist;
                targetPos = player1.GetGridPosition();
            }
        }

        if (player2 != null && player2.isAlive)
        {
            float dist = Vector2Int.Distance(currentGridPos, player2.GetGridPosition());
            if (dist < minDistance)
            {
                targetPos = player2.GetGridPosition();
            }
        }

        return targetPos;
    }

    /// <summary>
    /// Moves towards target greedily.
    /// </summary>
    void MoveTowardsTarget(Vector2Int targetPos)
    {
        List<Vector2Int> neighbors = GridManager.Instance.GetWalkableNeighbors(currentGridPos);

        if (neighbors.Count == 0) return;

        Vector2Int bestNeighbor = neighbors[0];
        float bestDistance = Vector2Int.Distance(bestNeighbor, targetPos);

        foreach (Vector2Int neighbor in neighbors)
        {
            float distance = Vector2Int.Distance(neighbor, targetPos);
            if (distance < bestDistance)
            {
                bestDistance = distance;
                bestNeighbor = neighbor;
            }
        }

        currentPath.Clear();
        currentPath.Enqueue(bestNeighbor);
    }

    /// <summary>
    /// Moves away from target (scared mode).
    /// </summary>
    void MoveAwayFromTarget(Vector2Int targetPos)
    {
        List<Vector2Int> neighbors = GridManager.Instance.GetWalkableNeighbors(currentGridPos);

        if (neighbors.Count == 0) return;

        Vector2Int bestNeighbor = neighbors[0];
        float bestDistance = Vector2Int.Distance(bestNeighbor, targetPos);

        foreach (Vector2Int neighbor in neighbors)
        {
            float distance = Vector2Int.Distance(neighbor, targetPos);
            if (distance > bestDistance)
            {
                bestDistance = distance;
                bestNeighbor = neighbor;
            }
        }

        currentPath.Clear();
        currentPath.Enqueue(bestNeighbor);
    }

    /// <summary>
    /// Follows the calculated path.
    /// </summary>
    void FollowPath()
    {
        // If path is empty, move randomly
        if (currentPath.Count == 0)
        {
            MoveRandomly();
            return;
        }

        Vector2Int nextPos = currentPath.Dequeue();

        // Check if next position is adjacent and walkable
        if (IsAdjacent(currentGridPos, nextPos) && GridManager.Instance.IsWalkable(nextPos))
        {
            StartMovingToCell(nextPos);
        }
        else
        {
            // Not adjacent or blocked
            currentPath.Clear();
            pathRecalculateTimer = pathRecalculateInterval;
        }
    }

    /// <summary>
    /// Checks if two positions are adjacent (4-directional).
    /// </summary>
    bool IsAdjacent(Vector2Int pos1, Vector2Int pos2)
    {
        int dx = Mathf.Abs(pos1.x - pos2.x);
        int dy = Mathf.Abs(pos1.y - pos2.y);

        return (dx == 1 && dy == 0) || (dx == 0 && dy == 1);
    }

    /// <summary>
    /// Moves to a random adjacent walkable cell.
    /// </summary>
    void MoveRandomly()
    {
        List<Vector2Int> neighbors = GridManager.Instance.GetWalkableNeighbors(currentGridPos);

        if (neighbors.Count > 0)
        {
            Vector2Int randomNeighbor = neighbors[Random.Range(0, neighbors.Count)];
            StartMovingToCell(randomNeighbor);
        }
    }

    /// <summary>
    /// Finds a path using BFS.
    /// </summary>
    Queue<Vector2Int> FindPath(Vector2Int start, Vector2Int goal)
    {
        Queue<Vector2Int> path = new Queue<Vector2Int>();

        if (start == goal || Vector2Int.Distance(start, goal) < 1.5f)
        {
            return path;
        }

        Queue<Vector2Int> frontier = new Queue<Vector2Int>();
        Dictionary<Vector2Int, Vector2Int> cameFrom = new Dictionary<Vector2Int, Vector2Int>();

        frontier.Enqueue(start);
        cameFrom[start] = start;

        int iterations = 0;
        bool foundGoal = false;

        while (frontier.Count > 0 && iterations < maxPathLength)
        {
            iterations++;
            Vector2Int current = frontier.Dequeue();

            if (current == goal || Vector2Int.Distance(current, goal) < 1.5f)
            {
                foundGoal = true;
                goal = current;
                break;
            }

            List<Vector2Int> neighbors = GridManager.Instance.GetWalkableNeighbors(current);

            foreach (Vector2Int neighbor in neighbors)
            {
                if (!cameFrom.ContainsKey(neighbor))
                {
                    frontier.Enqueue(neighbor);
                    cameFrom[neighbor] = current;
                }
            }
        }

        if (foundGoal && cameFrom.ContainsKey(goal))
        {
            Vector2Int current = goal;
            List<Vector2Int> pathList = new List<Vector2Int>();

            int maxSteps = 100;
            int steps = 0;

            while (current != start && steps < maxSteps)
            {
                pathList.Add(current);

                if (cameFrom.ContainsKey(current))
                {
                    current = cameFrom[current];
                }
                else
                {
                    break;
                }

                steps++;
            }

            pathList.Reverse();

            foreach (Vector2Int pos in pathList)
            {
                path.Enqueue(pos);
            }
        }

        return path;
    }

    /// <summary>
    /// Starts movement to a target grid cell.
    /// </summary>
    void StartMovingToCell(Vector2Int gridPos)
    {
        targetGridPos = gridPos;
        targetWorldPos = GridManager.Instance.GridToWorld(gridPos);
        isMoving = true;
    }

    /// <summary>
    /// Moves ghost towards target position.
    /// </summary>
    void Move()
    {
        float speed = isScared ? scaredSpeed : moveSpeed;
        transform.position = Vector3.MoveTowards(transform.position, targetWorldPos, speed * Time.deltaTime);

        if (Vector3.Distance(transform.position, targetWorldPos) < 0.01f)
        {
            transform.position = targetWorldPos;
            currentGridPos = targetGridPos;
            isMoving = false;
        }
    }

    /// <summary>
    /// Detects collision with players.
    /// </summary>
    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            PlayerController player = other.GetComponent<PlayerController>();

            if (player != null && player.isAlive)
            {
                if (isScared)
                {
                    // Player eats ghost
                    GameManager.Instance?.GhostEaten(this, player.playerNumber);
                    StartRespawn();
                }
                else
                {
                    // Ghost catches player (only if not respawning)
                    if (!isRespawning)
                    {
                        GameManager.Instance?.PlayerHitByGhost(player);
                    }
                }
            }
        }
    }

    /// <summary>
    /// Sets whether ghost is scared.
    /// </summary>
    public void SetScared(bool scared)
    {
        isScared = scared;

        // Don't change color if respawning
        if (isRespawning) return;

        // Change color
        if (ghostRenderer != null)
        {
            ghostRenderer.material.color = scared ? scaredColor : normalColor;
        }

        // Clear path
        currentPath.Clear();
        pathRecalculateTimer = pathRecalculateInterval;
    }

    /// <summary>
    /// Initiates respawn sequence.
    /// </summary>
    public void StartRespawn()
    {
        StartCoroutine(RespawnCoroutine());
    }

    /// <summary>
    /// Respawn coroutine with delay.
    /// </summary>
    IEnumerator RespawnCoroutine()
    {
        isRespawning = true;
        isMoving = false;
        currentPath.Clear();
        hasLeftGhostHouse = false; // Reset ghost house flag

        // Visual feedback
        if (ghostRenderer != null)
        {
            ghostRenderer.material.color = respawnColor;
        }

        // Move to spawn immediately
        currentGridPos = spawnGridPos;
        targetGridPos = spawnGridPos;
        targetWorldPos = GridManager.Instance.GridToWorld(spawnGridPos);
        transform.position = targetWorldPos;

        Debug.Log($"Ghost eaten! Respawning in {respawnDelay} seconds...");

        // Wait
        yield return new WaitForSeconds(respawnDelay);

        // Respawn complete
        isRespawning = false;
        SetScared(false);
        pathRecalculateTimer = 0f;

        Debug.Log("Ghost respawned and active!");
    }

    /// <summary>
    /// Immediate respawn (used for game start/reset).
    /// </summary>
    public void Respawn()
    {
        StopAllCoroutines();

        isRespawning = false;
        currentGridPos = spawnGridPos;
        targetGridPos = spawnGridPos;
        targetWorldPos = GridManager.Instance.GridToWorld(spawnGridPos);
        transform.position = targetWorldPos;
        isMoving = false;
        currentPath.Clear();
        pathRecalculateTimer = 0f;
        hasLeftGhostHouse = false; // Reset ghost house flag
        SetScared(false);

        // Check if spawned outside ghost house
        CheckIfOutsideGhostHouse();

        Debug.Log("Ghost respawned (immediate)");
    }

    /// <summary>
    /// Debug visualization.
    /// </summary>
    void OnDrawGizmos()
    {
        if (currentPath != null && currentPath.Count > 0 && GridManager.Instance != null)
        {
            // Path color based on state
            if (!hasLeftGhostHouse)
            {
                Gizmos.color = Color.yellow; // Exiting ghost house
            }
            else if (isScared)
            {
                Gizmos.color = Color.cyan; // Fleeing
            }
            else
            {
                Gizmos.color = Color.red; // Chasing
            }

            Vector2Int[] pathArray = currentPath.ToArray();
            Vector3 currentPos = transform.position;

            for (int i = 0; i < pathArray.Length; i++)
            {
                Vector3 pos = GridManager.Instance.GridToWorld(pathArray[i]);
                Gizmos.DrawWireSphere(pos, 0.2f);
                Gizmos.DrawLine(currentPos, pos);
                currentPos = pos;
            }
        }
    }
}