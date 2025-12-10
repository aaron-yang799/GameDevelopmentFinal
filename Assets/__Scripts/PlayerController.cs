using UnityEngine;

/// <summary>
/// Controls player movement on grid using WASD (Player 1) or Arrow Keys (Player 2).
/// Handles input, grid-based movement, and swap key detection.
/// </summary>
public class PlayerController : MonoBehaviour
{
    [Header("Player Settings")]
    public int playerNumber = 1; // 1 or 2
    public float moveSpeed = 5f;
    
    [Header("Status")]
    public bool isAlive = true;
    
    // Grid movement
    private Vector2Int currentGridPos;
    private Vector2Int targetGridPos;
    private Vector3 targetWorldPos;
    private bool isMoving = false;
    
    // Direction tracking
    private Vector2Int currentDirection = Vector2Int.zero;
    private Vector2Int nextDirection = Vector2Int.zero;
    
    // Input keys
    private KeyCode upKey, downKey, leftKey, rightKey, swapKey;
    
    // Spawn position
    private Vector2Int spawnGridPos;
    
    void Start()
    {
        SetupControls();
        
        // Set initial grid position based on world position
        currentGridPos = GridManager.Instance.WorldToGrid(transform.position);
        spawnGridPos = currentGridPos;
        targetGridPos = currentGridPos;
        targetWorldPos = transform.position;
        
        Debug.Log($"Player {playerNumber} spawned at grid {currentGridPos}, world {transform.position}");
    }
    
    /// <summary>
    /// Sets up input keys based on player number.
    /// Player 1: WASD + E
    /// Player 2: Arrows + /
    /// </summary>
    void SetupControls()
    {
        if (playerNumber == 1)
        {
            upKey = KeyCode.W;
            downKey = KeyCode.S;
            leftKey = KeyCode.A;
            rightKey = KeyCode.D;
            swapKey = KeyCode.E;
        }
        else // Player 2
        {
            upKey = KeyCode.UpArrow;
            downKey = KeyCode.DownArrow;
            leftKey = KeyCode.LeftArrow;
            rightKey = KeyCode.RightArrow;
            swapKey = KeyCode.Slash;
        }
    }
    
    void Update()
    {
        if (!isAlive) return;
        
        HandleInput();
        Move();
    }
    
    /// <summary>
    /// Handles player input for movement and swap.
    /// </summary>
    void HandleInput()
    {
        // Movement input
        if (Input.GetKeyDown(upKey))
            nextDirection = Vector2Int.up;
        else if (Input.GetKeyDown(downKey))
            nextDirection = Vector2Int.down;
        else if (Input.GetKeyDown(leftKey))
            nextDirection = Vector2Int.left;
        else if (Input.GetKeyDown(rightKey))
            nextDirection = Vector2Int.right;
        
        // Swap input
        if (Input.GetKeyDown(swapKey))
        {
            GameManager.Instance?.InitiateSwap(playerNumber);
        }
    }
    
    /// <summary>
    /// Handles grid-based movement.
    /// Players move one cell at a time and snap to grid positions.
    /// </summary>
    void Move()
    {
        if (!isMoving)
        {
            // Try to move in the next direction (buffered input)
            if (nextDirection != Vector2Int.zero)
            {
                Vector2Int newGridPos = currentGridPos + nextDirection;
                if (GridManager.Instance.IsWalkable(newGridPos))
                {
                    currentDirection = nextDirection;
                    StartMovingToCell(newGridPos);
                    return;
                }
            }
            
            // Continue in current direction if no turn
            if (currentDirection != Vector2Int.zero)
            {
                Vector2Int newGridPos = currentGridPos + currentDirection;
                if (GridManager.Instance.IsWalkable(newGridPos))
                {
                    StartMovingToCell(newGridPos);
                }
                else
                {
                    // Hit a wall, stop moving
                    currentDirection = Vector2Int.zero;
                }
            }
        }
        else
        {
            // Move towards target cell
            transform.position = Vector3.MoveTowards(transform.position, targetWorldPos, moveSpeed * Time.deltaTime);
            
            // Check if reached target
            if (Vector3.Distance(transform.position, targetWorldPos) < 0.01f)
            {
                transform.position = targetWorldPos;
                currentGridPos = targetGridPos;
                isMoving = false;
            }
        }
    }
    
    /// <summary>
    /// Starts movement to a new grid cell.
    /// </summary>
    void StartMovingToCell(Vector2Int gridPos)
    {
        targetGridPos = gridPos;
        targetWorldPos = GridManager.Instance.GridToWorld(gridPos);
        isMoving = true;
    }
    
    /// <summary>
    /// Respawns player at spawn position with full health.
    /// </summary>
    public void Respawn()
    {
        currentGridPos = spawnGridPos;
        targetGridPos = spawnGridPos;
        targetWorldPos = GridManager.Instance.GridToWorld(spawnGridPos);
        transform.position = targetWorldPos;
        isMoving = false;
        currentDirection = Vector2Int.zero;
        nextDirection = Vector2Int.zero;
        isAlive = true;
        gameObject.SetActive(true);
        
        Debug.Log($"Player {playerNumber} respawned");
    }
    
    /// <summary>
    /// Kills the player and hides them.
    /// </summary>
    public void Die()
    {
        isAlive = false;
        gameObject.SetActive(false);
        Debug.Log($"Player {playerNumber} died");
    }
    
    /// <summary>
    /// Gets current grid position.
    /// </summary>
    public Vector2Int GetGridPosition()
    {
        return currentGridPos;
    }
    
    /// <summary>
    /// Sets grid position (used for swap mechanic).
    /// </summary>
    public void SetGridPosition(Vector2Int gridPos)
    {
        currentGridPos = gridPos;
        targetGridPos = gridPos;
        targetWorldPos = GridManager.Instance.GridToWorld(gridPos);
        transform.position = targetWorldPos;
        isMoving = false;
        
        Debug.Log($"Player {playerNumber} moved to grid {gridPos}");
    }
    
    /// <summary>
    /// Handles collision with pellets (detected by Pellet script).
    /// </summary>
    void OnTriggerEnter(Collider other)
    {
        // Pellet collision is handled by Pellet.cs
        // This is here for potential future use
    }
}
