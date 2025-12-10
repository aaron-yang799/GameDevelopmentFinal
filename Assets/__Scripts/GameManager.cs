using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

/// <summary>
/// Main game manager that handles:
/// - Player lives and scoring
/// - Power-up system (10 seconds, all ghosts vulnerable)
/// - Swap mechanic (3 second window, 10 second cooldown)
/// - Win/lose conditions
/// - Last-man-standing mode (can swap when out of lives)
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance;
    
    [Header("Player References")]
    public PlayerController player1;
    public PlayerController player2;
    
    [Header("Ghost References")]
    public GhostAI[] ghosts;
    
    [Header("Game Settings")]
    public int startingLives = 3;
    public float powerUpDuration = 10f;
    public float swapWindowDuration = 3f;
    public float swapCooldown = 10f;
    
    [Header("UI")]
    public UIManager uiManager;
    
    // Game state
    private int lives;
    private int player1Score = 0;
    private int player2Score = 0;
    private int totalPellets = 0;
    private int pelletsCollected = 0;
    
    // Power-up state
    private bool isPowerUpActive = false;
    private float powerUpTimer = 0f;
    
    // Swap mechanic state
    private bool isSwapWindowActive = false;
    private int swapInitiator = 0; // 1 or 2
    private float swapWindowTimer = 0f;
    private bool canSwap = true;
    private float swapCooldownTimer = 0f;
    
    // Win/lose state
    private bool gameOver = false;
    private bool gameWon = false;
    
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
    }
    
    void Start()
    {
        lives = startingLives;
        
        // Spawn all pellets
        PelletSpawner spawner = GetComponent<PelletSpawner>();
        if (spawner != null)
        {
            spawner.SpawnPellets();
        }
        else
        {
            Debug.LogError("PelletSpawner component not found!");
        }
        
        // Count total pellets after spawning
        StartCoroutine(CountPelletsAfterSpawn());
        
        UpdateUI();
    }
    
    /// <summary>
    /// Counts pellets after a brief delay to ensure they're all spawned.
    /// </summary>
    IEnumerator CountPelletsAfterSpawn()
    {
        yield return new WaitForEndOfFrame();
        
        totalPellets = GameObject.FindGameObjectsWithTag("PelletYellow").Length +
                       GameObject.FindGameObjectsWithTag("PelletPurple").Length +
                       GameObject.FindGameObjectsWithTag("PowerPelletYellow").Length +
                       GameObject.FindGameObjectsWithTag("PowerPelletPurple").Length;
        
        Debug.Log($"Total pellets in game: {totalPellets}");
    }
    
    void Update()
    {
        if (gameOver || gameWon) return;
        
        // Power-up timer
        if (isPowerUpActive)
        {
            powerUpTimer -= Time.deltaTime;
            if (powerUpTimer <= 0f)
            {
                EndPowerUp();
            }
        }
        
        // Swap window timer
        if (isSwapWindowActive)
        {
            swapWindowTimer -= Time.deltaTime;
            uiManager?.UpdateSwapTimer(swapWindowTimer);
            
            if (swapWindowTimer <= 0f)
            {
                CancelSwap();
            }
        }
        
        // Swap cooldown timer
        if (!canSwap)
        {
            swapCooldownTimer -= Time.deltaTime;
            if (swapCooldownTimer <= 0f)
            {
                canSwap = true;
                Debug.Log("Swap cooldown ended");
            }
        }
    }
    
    /// <summary>
    /// Called when a player collects a pellet.
    /// </summary>
    public void CollectPellet(Pellet pellet, int playerNumber)
    {
        pelletsCollected++;
        
        // Add score to appropriate player
        if (playerNumber == 1)
        {
            player1Score += pellet.points;
        }
        else
        {
            player2Score += pellet.points;
        }
        
        UpdateUI();
        
        // Activate power-up if it was a power pellet
        if (pellet.isPowerPellet)
        {
            ActivatePowerUp();
        }
        
        Debug.Log($"Player {playerNumber} collected pellet. {pelletsCollected}/{totalPellets}");
        
        // Check win condition
        if (pelletsCollected >= totalPellets)
        {
            WinGame();
        }
    }
    
    /// <summary>
    /// Called when a ghost touches a player.
    /// </summary>
    public void PlayerHitByGhost(PlayerController player)
    {
        if (lives > 0)
        {
            lives--;
            player.Respawn();
            UpdateUI();
            
            Debug.Log($"Player {player.playerNumber} hit by ghost! Lives remaining: {lives}");
            
            if (lives == 0)
            {
                Debug.Log("No lives left! Entering last-man-standing mode");
            }
        }
        else
        {
            // No lives left - player dies permanently
            player.Die();
            
            Debug.Log($"Player {player.playerNumber} died permanently!");
            
            // Check if both players are dead
            if (!player1.isAlive && !player2.isAlive)
            {
                LoseGame();
            }
        }
    }
    
    /// <summary>
    /// Called when a ghost is eaten during power-up.
    /// </summary>
    public void GhostEaten(GhostAI ghost, int playerNumber)
    {
        int points = 200;
        
        if (playerNumber == 1)
        {
            player1Score += points;
        }
        else
        {
            player2Score += points;
        }
        
        UpdateUI();
        
        Debug.Log($"Player {playerNumber} ate a ghost! +{points} points");
    }
    
    /// <summary>
    /// Activates power-up mode for both players.
    /// All ghosts become vulnerable for 10 seconds.
    /// Resets swap cooldown.
    /// </summary>
    void ActivatePowerUp()
    {
        isPowerUpActive = true;
        powerUpTimer = powerUpDuration;
        
        // Make all ghosts scared
        foreach (GhostAI ghost in ghosts)
        {
            ghost.SetScared(true);
        }
        
        // Reset swap cooldown
        canSwap = true;
        swapCooldownTimer = 0f;
        
        uiManager?.ShowPowerUpActive(true);
        
        Debug.Log("Power-up activated! Ghosts are vulnerable!");
    }
    
    /// <summary>
    /// Ends power-up mode.
    /// </summary>
    void EndPowerUp()
    {
        isPowerUpActive = false;
        
        // Make all ghosts normal again
        foreach (GhostAI ghost in ghosts)
        {
            ghost.SetScared(false);
        }
        
        uiManager?.ShowPowerUpActive(false);
        
        Debug.Log("Power-up ended");
    }
    
    /// <summary>
    /// Initiates the swap sequence.
    /// First player presses their swap key to start 3-second window.
    /// Second player must press their swap key within window to execute swap.
    /// </summary>
    public void InitiateSwap(int playerNumber)
    {
        if (!canSwap)
        {
            Debug.Log("Swap on cooldown!");
            return;
        }
        
        if (!isSwapWindowActive)
        {
            // Start swap window
            isSwapWindowActive = true;
            swapInitiator = playerNumber;
            swapWindowTimer = swapWindowDuration;
            uiManager?.ShowSwapWindow(true);
            
            Debug.Log($"Player {playerNumber} initiated swap! Other player has {swapWindowDuration} seconds to accept.");
        }
        else
        {
            // Check if other player is accepting the swap
            if (playerNumber != swapInitiator)
            {
                ExecuteSwap();
            }
            else
            {
                Debug.Log("Same player pressed swap key again - waiting for other player");
            }
        }
    }
    
    /// <summary>
    /// Executes the position swap between players.
    /// </summary>
    void ExecuteSwap()
    {
        // Get current positions
        Vector2Int pos1 = player1.GetGridPosition();
        Vector2Int pos2 = player2.GetGridPosition();
        
        // Swap positions
        player1.SetGridPosition(pos2);
        player2.SetGridPosition(pos1);
        
        // End swap window
        isSwapWindowActive = false;
        uiManager?.ShowSwapWindow(false);
        
        // Start cooldown
        canSwap = false;
        swapCooldownTimer = swapCooldown;
        
        Debug.Log($"Players swapped positions! Cooldown: {swapCooldown} seconds");
    }
    
    /// <summary>
    /// Cancels the swap if time runs out.
    /// </summary>
    void CancelSwap()
    {
        isSwapWindowActive = false;
        swapInitiator = 0;
        uiManager?.ShowSwapWindow(false);
        
        Debug.Log("Swap cancelled - time expired");
    }
    
    /// <summary>
    /// Updates all UI elements.
    /// </summary>
    void UpdateUI()
    {
        uiManager?.UpdateScores(player1Score, player2Score);
        uiManager?.UpdateLives(lives);
    }
    
    /// <summary>
    /// Called when all pellets are collected.
    /// </summary>
    void WinGame()
    {
        gameWon = true;
        uiManager?.ShowGameOver(true);
        
        Debug.Log("===== YOU WIN! =====");
        Debug.Log($"Final Score - P1: {player1Score}, P2: {player2Score}, Total: {player1Score + player2Score}");
    }
    
    /// <summary>
    /// Called when both players are dead.
    /// </summary>
    void LoseGame()
    {
        gameOver = true;
        uiManager?.ShowGameOver(false);
        
        Debug.Log("===== GAME OVER =====");
        Debug.Log($"Final Score - P1: {player1Score}, P2: {player2Score}, Total: {player1Score + player2Score}");
    }
    
    /// <summary>
    /// Restarts the game (called by UI button).
    /// </summary>
    public void RestartGame()
    {
        Debug.Log("Restarting game...");
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}
