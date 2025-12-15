using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class GhostAI : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 4f;
    public float scaredSpeed = 2.5f;

    [Header("AI Settings")]
    public float pathRecalculateInterval = 0.3f;
    public int maxPathLength = 100;

    [Header("Random Behavior (Adjust Per Ghost)")]
    [Tooltip("How often ghost makes a random decision (seconds)")]
    public float randomDecisionInterval = 3f;
    [Tooltip("Chance ghost moves randomly (0-1). Higher = easier")]
    [Range(0f, 1f)]
    public float randomDecisionChance = 0.5f;

    [Header("Ghost House Settings")]
    public int ghostHouseMinX = 14;
    public int ghostHouseMaxX = 18;
    public int ghostHouseMinZ = 13;
    public int ghostHouseMaxZ = 17;
    public Vector2Int ghostHouseExitTarget = new Vector2Int(16, 18);

    [Header("Respawn Settings")]
    public float respawnDelay = 6f;

    // Grid movement
    private Vector2Int currentGridPos;
    private Vector2Int targetGridPos;
    private Vector3 targetWorldPos;
    private bool isMoving = false;
    private Vector3 initialWorldPosition;

    // AI state
    private bool isScared = false;
    private float pathRecalculateTimer = 0f;
    private float randomDecisionTimer = 0f;
    private bool shouldMoveRandomly = false;
    private bool hasLeftGhostHouse = false;

    // Pathfinding
    private Queue<Vector2Int> currentPath = new Queue<Vector2Int>();
    private Vector2Int lastTargetPlayerPos;

    // Spawn
    private Vector2Int spawnGridPos;
    private bool isRespawning = false;

    // Material
    private Renderer ghostRenderer;
    private Color normalColor = Color.red;
    private Color scaredColor = Color.blue;
    private Color respawnColor = new Color(0.5f, 0.5f, 0.5f, 0.5f);

    void Awake()
    {
        ghostRenderer = GetComponent<Renderer>();
        initialWorldPosition = transform.position;

        Debug.Log($"Ghost '{gameObject.name}' Awake - stored position: {initialWorldPosition}");
    }

    void Start()
    {
        if (GridManager.Instance == null)
        {
            Debug.LogError($"Ghost '{gameObject.name}': GridManager not ready!");
            return;
        }

        currentGridPos = GridManager.Instance.WorldToGrid(initialWorldPosition);
        spawnGridPos = currentGridPos;
        targetGridPos = currentGridPos;
        targetWorldPos = GridManager.Instance.GridToWorld(currentGridPos);

        transform.position = targetWorldPos;

        CheckIfOutsideGhostHouse();

        pathRecalculateTimer = pathRecalculateInterval;
        randomDecisionTimer = Random.Range(0f, randomDecisionInterval);

        Debug.Log($"Ghost '{gameObject.name}' initialized at grid {currentGridPos}, in house: {!hasLeftGhostHouse}");
    }

    void Update()
    {
        if (GameManager.Instance == null) return;
        if (isRespawning) return;

        if (!hasLeftGhostHouse)
        {
            CheckIfOutsideGhostHouse();
        }

        randomDecisionTimer -= Time.deltaTime;
        if (randomDecisionTimer <= 0f)
        {
            randomDecisionTimer = randomDecisionInterval;

            if (Random.value < randomDecisionChance)
            {
                shouldMoveRandomly = true;
            }
        }

        pathRecalculateTimer += Time.deltaTime;
        if (pathRecalculateTimer >= pathRecalculateInterval)
        {
            pathRecalculateTimer = 0f;
            CalculateNewPath();
        }

        if (!isMoving)
        {
            FollowPath();
        }
        else
        {
            Move();
        }
    }

    void CheckIfOutsideGhostHouse()
    {
        if (!IsInsideGhostHouse(currentGridPos))
        {
            hasLeftGhostHouse = true;
        }
    }

    bool IsInsideGhostHouse(Vector2Int pos)
    {
        return pos.x >= ghostHouseMinX && pos.x <= ghostHouseMaxX &&
               pos.y >= ghostHouseMinZ && pos.y <= ghostHouseMaxZ;
    }

    void CalculateNewPath()
    {
        if (shouldMoveRandomly)
        {
            currentPath.Clear();
            shouldMoveRandomly = false;
            return;
        }

        if (!hasLeftGhostHouse)
        {
            currentPath = FindPath(currentGridPos, ghostHouseExitTarget);
            if (currentPath.Count == 0)
            {
                MoveTowardsTarget(ghostHouseExitTarget);
            }
            return;
        }

        Vector2Int targetPos = GetTargetPosition();

        if (targetPos != lastTargetPlayerPos || currentPath.Count == 0)
        {
            lastTargetPlayerPos = targetPos;

            if (isScared)
            {
                MoveAwayFromTarget(targetPos);
            }
            else
            {
                currentPath = FindPath(currentGridPos, targetPos);

                if (currentPath.Count == 0)
                {
                    MoveTowardsTarget(targetPos);
                }
            }
        }
    }

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

    void FollowPath()
    {
        if (currentPath.Count == 0)
        {
            MoveRandomly();
            return;
        }

        Vector2Int nextPos = currentPath.Dequeue();

        if (IsAdjacent(currentGridPos, nextPos) && GridManager.Instance.IsWalkable(nextPos))
        {
            StartMovingToCell(nextPos);
        }
        else
        {
            currentPath.Clear();
            pathRecalculateTimer = pathRecalculateInterval;
        }
    }

    bool IsAdjacent(Vector2Int pos1, Vector2Int pos2)
    {
        int dx = Mathf.Abs(pos1.x - pos2.x);
        int dy = Mathf.Abs(pos1.y - pos2.y);

        return (dx == 1 && dy == 0) || (dx == 0 && dy == 1);
    }

    void MoveRandomly()
    {
        List<Vector2Int> neighbors = GridManager.Instance.GetWalkableNeighbors(currentGridPos);

        if (neighbors.Count > 0)
        {
            Vector2Int randomNeighbor = neighbors[Random.Range(0, neighbors.Count)];
            StartMovingToCell(randomNeighbor);
        }
    }

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

    void StartMovingToCell(Vector2Int gridPos)
    {
        targetGridPos = gridPos;
        targetWorldPos = GridManager.Instance.GridToWorld(gridPos);
        isMoving = true;
    }

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

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            PlayerController player = other.GetComponent<PlayerController>();

            if (player != null && player.isAlive)
            {
                if (isScared)
                {
                    GameManager.Instance?.GhostEaten(this, player.playerNumber);
                    StartRespawn();
                }
                else
                {
                    if (!isRespawning)
                    {
                        GameManager.Instance?.PlayerHitByGhost(player);
                    }
                }
            }
        }
    }

    public void SetScared(bool scared)
    {
        isScared = scared;

        if (isRespawning) return;

        if (ghostRenderer != null)
        {
            ghostRenderer.material.color = scared ? scaredColor : normalColor;
        }

        currentPath.Clear();
        pathRecalculateTimer = pathRecalculateInterval;
    }

    public void StartRespawn()
    {
        StartCoroutine(RespawnCoroutine());
    }

    IEnumerator RespawnCoroutine()
    {
        isRespawning = true;
        isMoving = false;
        currentPath.Clear();
        hasLeftGhostHouse = false;

        if (ghostRenderer != null)
        {
            ghostRenderer.material.color = respawnColor;
        }

        currentGridPos = spawnGridPos;
        targetGridPos = spawnGridPos;
        targetWorldPos = GridManager.Instance.GridToWorld(spawnGridPos);
        transform.position = targetWorldPos;

        yield return new WaitForSeconds(respawnDelay);

        isRespawning = false;
        SetScared(false);
        pathRecalculateTimer = 0f;
    }

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
        hasLeftGhostHouse = false;
        SetScared(false);

        CheckIfOutsideGhostHouse();

        Debug.Log("Ghost respawned (immediate)");
    }

    void OnDrawGizmos()
    {
        if (currentPath != null && currentPath.Count > 0 && GridManager.Instance != null)
        {
            if (!hasLeftGhostHouse)
            {
                Gizmos.color = Color.yellow;
            }
            else if (isScared)
            {
                Gizmos.color = Color.cyan;
            }
            else
            {
                Gizmos.color = Color.red;
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