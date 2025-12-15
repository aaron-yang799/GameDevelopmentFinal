using UnityEngine;

/// <summary>
/// Controls player movement on grid with snappy rotation and input buffering.
/// Players continue moving in current direction even if new direction is blocked.
/// </summary>
public class PlayerController : MonoBehaviour
{
    [Header("Player Settings")]
    public int playerNumber = 1;
    public float moveSpeed = 5f;

    [Header("Input Keys")]
    public KeyCode upKey = KeyCode.W;
    public KeyCode downKey = KeyCode.S;
    public KeyCode leftKey = KeyCode.A;
    public KeyCode rightKey = KeyCode.D;
    public KeyCode swapKey = KeyCode.E;

    // Grid movement
    private Vector2Int currentGridPos;
    private Vector2Int targetGridPos;
    private Vector3 targetWorldPos;
    private bool isMoving = false;

    // Current and buffered direction
    private Vector2Int currentDirection = Vector2Int.zero;
    private Vector2Int bufferedDirection = Vector2Int.zero;

    // Spawn position for respawning
    private Vector2Int spawnGridPos;

    // Death state
    public bool isAlive = true;

    void Start()
    {
        // Set initial grid position
        currentGridPos = GridManager.Instance.WorldToGrid(transform.position);
        spawnGridPos = currentGridPos;
        targetGridPos = currentGridPos;
        targetWorldPos = transform.position;

        Debug.Log($"Player {playerNumber} spawned at grid {currentGridPos}, world {transform.position}");
    }

    void Update()
    {
        // ALWAYS allow swap input, even when dead (for last-man-standing)
        HandleSwapInput();

        // Movement only when alive
        if (!isAlive) return;

        // Handle movement input
        HandleInput();

        // Move if we're currently moving
        if (isMoving)
        {
            Move();
        }
        else
        {
            // Reached a cell - try buffered direction first, then current direction

            // First, try to turn in the buffered direction
            if (bufferedDirection != Vector2Int.zero && TryMove(bufferedDirection))
            {
                // Successfully turned to buffered direction
                currentDirection = bufferedDirection;
                bufferedDirection = Vector2Int.zero;
            }
            // If buffered direction failed or no buffered input, continue in current direction
            else if (currentDirection != Vector2Int.zero)
            {
                TryMove(currentDirection);
            }
        }
    }

    /// <summary>
    /// Handles player input and buffers it.
    /// </summary>
    /// <summary>
    /// Handles swap input (works even when dead).
    /// </summary>
    void HandleSwapInput()
    {
        if (Input.GetKeyDown(swapKey))
        {
            GameManager.Instance?.InitiateSwap(playerNumber);
        }
    }

    /// <summary>
    /// Handles movement input (only when alive).
    /// </summary>
    void HandleInput()
    {
        // Check for new input
        if (Input.GetKey(upKey))
        {
            bufferedDirection = new Vector2Int(0, 1);
        }
        else if (Input.GetKey(downKey))
        {
            bufferedDirection = new Vector2Int(0, -1);
        }
        else if (Input.GetKey(leftKey))
        {
            bufferedDirection = new Vector2Int(-1, 0);
        }
        else if (Input.GetKey(rightKey))
        {
            bufferedDirection = new Vector2Int(1, 0);
        }
    }

    /// <summary>
    /// Attempts to move in the given direction.
    /// Returns true if movement started, false if blocked.
    /// </summary>
    bool TryMove(Vector2Int direction)
    {
        if (direction == Vector2Int.zero) return false;

        Vector2Int nextPos = currentGridPos + direction;

        // Check if next position is walkable
        if (GridManager.Instance.IsWalkable(nextPos))
        {
            targetGridPos = nextPos;
            targetWorldPos = GridManager.Instance.GridToWorld(nextPos);
            isMoving = true;
            return true;
        }

        return false;
    }

    /// <summary>
    /// Moves player towards target position and rotates to face movement direction.
    /// </summary>
    void Move()
    {
        transform.position = Vector3.MoveTowards(transform.position, targetWorldPos, moveSpeed * Time.deltaTime);

        // Rotate cube to face movement direction (snappy, with 90° offset)
        RotateTowardsMovementDirection();

        if (Vector3.Distance(transform.position, targetWorldPos) < 0.01f)
        {
            transform.position = targetWorldPos;
            currentGridPos = targetGridPos;
            isMoving = false;
        }
    }

    /// <summary>
    /// Rotates player cube instantly to face the direction they're moving.
    /// Angles adjusted 90° clockwise for correct sprite orientation.
    /// </summary>
    void RotateTowardsMovementDirection()
    {
        // Calculate movement direction
        Vector3 direction = (targetWorldPos - transform.position).normalized;

        // Don't rotate if not moving
        if (direction.sqrMagnitude < 0.01f) return;

        // Calculate angle based on movement direction (rotated 90° clockwise)
        float angle = 0f;

        if (Mathf.Abs(direction.x) > Mathf.Abs(direction.z))
        {
            // Moving horizontally
            if (direction.x > 0)
            {
                angle = 180f;  // Moving right
            }
            else
            {
                angle = 0f;    // Moving left
            }
        }
        else
        {
            // Moving vertically
            if (direction.z > 0)
            {
                angle = 90f;   // Moving forward/up
            }
            else
            {
                angle = -90f;  // Moving backward/down
            }
        }

        // Apply rotation instantly (snappy Pac-Man style)
        transform.rotation = Quaternion.Euler(0f, angle, 0f);
    }

    /// <summary>
    /// Gets the current grid position.
    /// </summary>
    public Vector2Int GetGridPosition()
    {
        return currentGridPos;
    }

    /// <summary>
    /// Sets the grid position (used for spawning and swapping).
    /// </summary>
    public void SetGridPosition(Vector2Int gridPos, bool preserveMovement = false)
    {
        currentGridPos = gridPos;
        targetGridPos = gridPos;
        targetWorldPos = GridManager.Instance.GridToWorld(gridPos);
        transform.position = targetWorldPos;
        isMoving = false;

        // Only clear direction if NOT preserving movement (e.g., for respawn/swap)
        if (!preserveMovement)
        {
            currentDirection = Vector2Int.zero;
            bufferedDirection = Vector2Int.zero;
        }
    }

    /// <summary>
    /// Respawns the player at their spawn position.
    /// </summary>
    public void Respawn()
    {
        SetGridPosition(spawnGridPos);
        isAlive = true;
        currentDirection = Vector2Int.zero;
        bufferedDirection = Vector2Int.zero;

        // Make visible
        Renderer rend = GetComponent<Renderer>();
        if (rend != null)
        {
            rend.enabled = true;
        }
    }

    /// <summary>
    /// Kills the player (permanent death until respawn).
    /// </summary>
    public void Die()
    {
        isAlive = false;
        isMoving = false;
        currentDirection = Vector2Int.zero;
        bufferedDirection = Vector2Int.zero;

        // Make invisible
        Renderer rend = GetComponent<Renderer>();
        if (rend != null)
        {
            rend.enabled = false;
        }
    }

    /// <summary>
    /// Collision detection for pellets and ghosts.
    /// </summary>
    void OnTriggerEnter(Collider other)
    {
        if (!isAlive) return;

        // Pellet collection handled by Pellet script
    }
}