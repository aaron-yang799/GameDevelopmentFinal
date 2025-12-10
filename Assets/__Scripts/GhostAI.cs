using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Ghost AI that chases the nearest alive player.
/// Uses simple pathfinding to move towards players on the grid.
/// Can be scared (vulnerable) when power pellets are active.
/// </summary>
public class GhostAI : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 3f;
    public float scaredSpeed = 2f;
    
    [Header("AI Settings")]
    public float directionChangeInterval = 0.5f;
    
    // Grid movement
    private Vector2Int currentGridPos;
    private Vector2Int targetGridPos;
    private Vector3 targetWorldPos;
    private bool isMoving = false;
    
    // AI state
    private bool isScared = false;
    private float directionChangeTimer = 0f;
    
    // Spawn position for respawning
    private Vector2Int spawnGridPos;
    
    // Material for color changes
    private Renderer ghostRenderer;
    private Color normalColor = Color.red;
    private Color scaredColor = Color.blue;
    
    void Start()
    {
        ghostRenderer = GetComponent<Renderer>();
        
        // Set initial grid position
        currentGridPos = GridManager.Instance.WorldToGrid(transform.position);
        spawnGridPos = currentGridPos;
        targetGridPos = currentGridPos;
        targetWorldPos = transform.position;
        
        Debug.Log($"Ghost spawned at grid {currentGridPos}");
    }
    
    void Update()
    {
        if (GameManager.Instance == null) return;
        
        if (!isMoving)
        {
            // Wait before choosing new direction
            directionChangeTimer += Time.deltaTime;
            if (directionChangeTimer >= directionChangeInterval)
            {
                directionChangeTimer = 0f;
                ChooseNextDirection();
            }
        }
        else
        {
            Move();
        }
    }
    
    /// <summary>
    /// Decides which direction to move based on scared state.
    /// </summary>
    void ChooseNextDirection()
    {
        if (isScared)
        {
            MoveAwayFromPlayers();
        }
        else
        {
            MoveTowardsNearestPlayer();
        }
    }
    
    /// <summary>
    /// Moves towards the nearest alive player (chase mode).
    /// </summary>
    void MoveTowardsNearestPlayer()
    {
        PlayerController player1 = GameManager.Instance.player1;
        PlayerController player2 = GameManager.Instance.player2;
        
        // Find nearest alive player
        PlayerController targetPlayer = null;
        float minDistance = float.MaxValue;
        
        if (player1.isAlive)
        {
            float dist = Vector2Int.Distance(currentGridPos, player1.GetGridPosition());
            if (dist < minDistance)
            {
                minDistance = dist;
                targetPlayer = player1;
            }
        }
        
        if (player2.isAlive)
        {
            float dist = Vector2Int.Distance(currentGridPos, player2.GetGridPosition());
            if (dist < minDistance)
            {
                targetPlayer = player2;
            }
        }
        
        // Move towards target player
        if (targetPlayer != null)
        {
            Vector2Int targetPos = targetPlayer.GetGridPosition();
            Vector2Int bestDirection = GetBestDirection(targetPos);
            
            if (bestDirection != Vector2Int.zero)
            {
                StartMovingToCell(currentGridPos + bestDirection);
            }
        }
    }
    
    /// <summary>
    /// Moves away from nearest player (scared mode).
    /// </summary>
    void MoveAwayFromPlayers()
    {
        PlayerController player1 = GameManager.Instance.player1;
        PlayerController player2 = GameManager.Instance.player2;
        
        // Find nearest player (even if we're running away)
        Vector2Int nearestPlayerPos = currentGridPos;
        float minDistance = float.MaxValue;
        
        if (player1.isAlive)
        {
            float dist = Vector2Int.Distance(currentGridPos, player1.GetGridPosition());
            if (dist < minDistance)
            {
                minDistance = dist;
                nearestPlayerPos = player1.GetGridPosition();
            }
        }
        
        if (player2.isAlive)
        {
            float dist = Vector2Int.Distance(currentGridPos, player2.GetGridPosition());
            if (dist < minDistance)
            {
                nearestPlayerPos = player2.GetGridPosition();
            }
        }
        
        // Move away from nearest player
        Vector2Int fleeDirection = GetWorstDirection(nearestPlayerPos);
        
        if (fleeDirection != Vector2Int.zero)
        {
            StartMovingToCell(currentGridPos + fleeDirection);
        }
    }
    
    /// <summary>
    /// Gets the direction that moves closest to target (for chasing).
    /// </summary>
    Vector2Int GetBestDirection(Vector2Int targetPos)
    {
        List<Vector2Int> walkableNeighbors = GridManager.Instance.GetWalkableNeighbors(currentGridPos);
        
        if (walkableNeighbors.Count == 0) return Vector2Int.zero;
        
        Vector2Int bestDir = Vector2Int.zero;
        float bestDistance = float.MaxValue;
        
        foreach (Vector2Int neighbor in walkableNeighbors)
        {
            float distance = Vector2Int.Distance(neighbor, targetPos);
            if (distance < bestDistance)
            {
                bestDistance = distance;
                bestDir = neighbor - currentGridPos;
            }
        }
        
        return bestDir;
    }
    
    /// <summary>
    /// Gets the direction that moves farthest from target (for fleeing).
    /// </summary>
    Vector2Int GetWorstDirection(Vector2Int targetPos)
    {
        List<Vector2Int> walkableNeighbors = GridManager.Instance.GetWalkableNeighbors(currentGridPos);
        
        if (walkableNeighbors.Count == 0) return Vector2Int.zero;
        
        Vector2Int worstDir = Vector2Int.zero;
        float worstDistance = 0f;
        
        foreach (Vector2Int neighbor in walkableNeighbors)
        {
            float distance = Vector2Int.Distance(neighbor, targetPos);
            if (distance > worstDistance)
            {
                worstDistance = distance;
                worstDir = neighbor - currentGridPos;
            }
        }
        
        return worstDir;
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
        
        // Check if reached target
        if (Vector3.Distance(transform.position, targetWorldPos) < 0.01f)
        {
            transform.position = targetWorldPos;
            currentGridPos = targetGridPos;
            isMoving = false;
        }
    }
    
    /// <summary>
    /// Detects collision with players.
    /// If scared: ghost gets eaten.
    /// If normal: player loses a life.
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
                    Respawn();
                }
                else
                {
                    // Ghost catches player
                    GameManager.Instance?.PlayerHitByGhost(player);
                }
            }
        }
    }
    
    /// <summary>
    /// Sets whether ghost is scared (vulnerable to being eaten).
    /// </summary>
    public void SetScared(bool scared)
    {
        isScared = scared;
        
        // Change color to indicate scared state
        if (ghostRenderer != null)
        {
            ghostRenderer.material.color = scared ? scaredColor : normalColor;
        }
    }
    
    /// <summary>
    /// Respawns ghost at spawn position.
    /// </summary>
    public void Respawn()
    {
        currentGridPos = spawnGridPos;
        targetGridPos = spawnGridPos;
        targetWorldPos = GridManager.Instance.GridToWorld(spawnGridPos);
        transform.position = targetWorldPos;
        isMoving = false;
        SetScared(false);
        
        Debug.Log("Ghost respawned");
    }
}
