using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

/// <summary>
/// Main game manager with infinite level progression.
/// Each level increases ghost speed by 0.4.
/// Tracks high score across sessions.
/// Awards bonus life for completing levels.
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

    [Header("Level Progression")]
    public float baseGhostSpeed = 3f;
    public float speedIncreasePerLevel = 0.4f;
    public float levelTransitionDelay = 2f;
    public int maxLives = 9; // Maximum lives cap

    [Header("UI")]
    public UIManager uiManager;

    // Game state
    private int lives;
    private int currentLevel = 1;
    private int currentScore = 0;
    private int player1Score = 0;
    private int player2Score = 0;
    private int highScore = 0;
    private int totalPellets = 0;
    private int pelletsCollected = 0;

    // Power-up state
    private bool isPowerUpActive = false;
    private float powerUpTimer = 0f;

    // Swap mechanic state
    private bool isSwapWindowActive = false;
    private int swapInitiator = 0;
    private float swapWindowTimer = 0f;
    private bool canSwap = true;
    private float swapCooldownTimer = 0f;

    // Win/lose state
    private bool gameOver = false;
    private bool isTransitioningLevel = false;

    private const string HIGH_SCORE_KEY = "PacManHighScore";

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
        highScore = PlayerPrefs.GetInt(HIGH_SCORE_KEY, 0);
        StartLevel();
    }

    /// <summary>
    /// Starts a new level.
    /// </summary>
    void StartLevel()
    {
        Debug.Log($"===== STARTING LEVEL {currentLevel} =====");

        float currentGhostSpeed = baseGhostSpeed + (speedIncreasePerLevel * (currentLevel - 1));
        foreach (GhostAI ghost in ghosts)
        {
            ghost.moveSpeed = currentGhostSpeed;
            ghost.Respawn();
        }

        Debug.Log($"Ghost speed for level {currentLevel}: {currentGhostSpeed}");

        PelletSpawner spawner = GetComponent<PelletSpawner>();
        if (spawner != null)
        {
            spawner.SpawnPellets();
        }
        else
        {
            Debug.LogError("PelletSpawner component not found!");
        }

        CountPelletsNow();
        UpdateUI();
    }

    /// <summary>
    /// Counts pellets immediately (synchronous).
    /// CRITICAL FIX: Prevents race condition where pellets are collected before counting finishes.
    /// </summary>
    void CountPelletsNow()
    {
        totalPellets = GameObject.FindGameObjectsWithTag("PelletYellow").Length +
                       GameObject.FindGameObjectsWithTag("PelletPurple").Length +
                       GameObject.FindGameObjectsWithTag("PowerPelletYellow").Length +
                       GameObject.FindGameObjectsWithTag("PowerPelletPurple").Length;

        pelletsCollected = 0;

        Debug.Log($"Total pellets in game: {totalPellets}");

        if (totalPellets == 0)
        {
            Debug.LogError("CRITICAL: No pellets spawned! Check PelletSpawner configuration!");
        }
    }

    void Update()
    {
        if (gameOver || isTransitioningLevel) return;

        if (isPowerUpActive)
        {
            powerUpTimer -= Time.deltaTime;
            if (powerUpTimer <= 0f)
            {
                EndPowerUp();
            }
        }

        if (isSwapWindowActive)
        {
            swapWindowTimer -= Time.deltaTime;
            uiManager?.UpdateSwapTimer(swapWindowTimer);

            if (swapWindowTimer <= 0f)
            {
                CancelSwap();
            }
        }

        if (!canSwap)
        {
            swapCooldownTimer -= Time.deltaTime;
            if (swapCooldownTimer <= 0f)
            {
                canSwap = true;
            }
        }
    }

    /// <summary>
    /// Called when a player collects a pellet.
    /// </summary>
    public void CollectPellet(Pellet pellet, int playerNumber)
    {
        if (totalPellets == 0)
        {
            Debug.LogWarning("Pellet collected before counting finished");
            Destroy(pellet.gameObject);
            return;
        }

        pelletsCollected++;

        if (playerNumber == 1)
        {
            player1Score += pellet.points;
        }
        else
        {
            player2Score += pellet.points;
        }

        currentScore = player1Score + player2Score;

        if (currentScore > highScore)
        {
            highScore = currentScore;
            PlayerPrefs.SetInt(HIGH_SCORE_KEY, highScore);
            PlayerPrefs.Save();
        }

        UpdateUI();

        if (pellet.isPowerPellet)
        {
            ActivatePowerUp();
        }

        if (pelletsCollected >= totalPellets)
        {
            CompleteLevel();
        }
    }

    /// <summary>
    /// Called when all pellets are collected.
    /// Advances to next level instead of ending game.
    /// </summary>
    void CompleteLevel()
    {
        if (totalPellets <= 0)
        {
            Debug.LogError("Cannot complete level - no pellets spawned!");
            return;
        }

        Debug.Log($"===== LEVEL {currentLevel} COMPLETE! =====");

        AudioManager.Instance?.PlayLevelComplete();

        StartCoroutine(LevelTransition());
    }

    /// <summary>
    /// Handles transition to next level.
    /// Awards bonus life for completing level.
    /// </summary>
    IEnumerator LevelTransition()
    {
        isTransitioningLevel = true;

        player1.enabled = false;
        player2.enabled = false;

        foreach (GhostAI ghost in ghosts)
        {
            ghost.enabled = false;
        }

        uiManager?.ShowLevelComplete(currentLevel);

        yield return new WaitForSeconds(levelTransitionDelay);

        uiManager?.HideLevelComplete();

        ClearAllPellets();

        // Award bonus life for completing level
        if (lives < maxLives)
        {
            lives++;
            Debug.Log($"Bonus life awarded! Lives: {lives}");
        }
        else
        {
            Debug.Log($"Already at max lives ({maxLives})");
        }

        currentLevel++;

        player1.enabled = true;
        player2.enabled = true;

        foreach (GhostAI ghost in ghosts)
        {
            ghost.enabled = true;
        }

        isTransitioningLevel = false;

        StartLevel();
    }

    /// <summary>
    /// Clears all remaining pellets from the scene.
    /// </summary>
    void ClearAllPellets()
    {
        GameObject[] allPellets = GameObject.FindGameObjectsWithTag("PelletYellow");
        foreach (GameObject pellet in allPellets) Destroy(pellet);

        allPellets = GameObject.FindGameObjectsWithTag("PelletPurple");
        foreach (GameObject pellet in allPellets) Destroy(pellet);

        allPellets = GameObject.FindGameObjectsWithTag("PowerPelletYellow");
        foreach (GameObject pellet in allPellets) Destroy(pellet);

        allPellets = GameObject.FindGameObjectsWithTag("PowerPelletPurple");
        foreach (GameObject pellet in allPellets) Destroy(pellet);
    }

    /// <summary>
    /// Called when a ghost touches a player.
    /// </summary>
    public void PlayerHitByGhost(PlayerController player)
    {
        if (lives > 0)
        {
            lives--;

            AudioManager.Instance?.PlayPlayerDeath();

            player.Respawn();
            UpdateUI();

            if (lives == 0)
            {
                Debug.Log("No lives left! Last-man-standing mode");
            }
        }
        else
        {
            AudioManager.Instance?.PlayPlayerDeath();

            player.Die();

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

        currentScore = player1Score + player2Score;

        if (currentScore > highScore)
        {
            highScore = currentScore;
            PlayerPrefs.SetInt(HIGH_SCORE_KEY, highScore);
            PlayerPrefs.Save();
        }

        AudioManager.Instance?.PlayGhostEaten();

        UpdateUI();
    }

    /// <summary>
    /// Activates power-up mode for both players.
    /// </summary>
    void ActivatePowerUp()
    {
        isPowerUpActive = true;
        powerUpTimer = powerUpDuration;

        foreach (GhostAI ghost in ghosts)
        {
            ghost.SetScared(true);
        }

        canSwap = true;
        swapCooldownTimer = 0f;

        uiManager?.ShowPowerUpActive(true);
    }

    /// <summary>
    /// Ends power-up mode.
    /// </summary>
    void EndPowerUp()
    {
        isPowerUpActive = false;

        foreach (GhostAI ghost in ghosts)
        {
            ghost.SetScared(false);
        }

        uiManager?.ShowPowerUpActive(false);
    }

    /// <summary>
    /// Initiates the swap sequence.
    /// </summary>
    public void InitiateSwap(int playerNumber)
    {
        if (!canSwap) return;

        if (!isSwapWindowActive)
        {
            isSwapWindowActive = true;
            swapInitiator = playerNumber;
            swapWindowTimer = swapWindowDuration;
            uiManager?.ShowSwapWindow(true);
        }
        else
        {
            if (playerNumber != swapInitiator)
            {
                ExecuteSwap();
            }
        }
    }

    /// <summary>
    /// Executes the position swap between players.
    /// During last-man-standing (one player dead), swaps life state too.
    /// </summary>
    void ExecuteSwap()
    {
        // Check if we're in last-man-standing mode (one player dead, one alive)
        bool isLastManStanding = (!player1.isAlive && player2.isAlive) || (player1.isAlive && !player2.isAlive);

        if (isLastManStanding)
        {
            // Last-man-standing: Swap life states
            if (!player1.isAlive && player2.isAlive)
            {
                // Player 1 is dead, Player 2 is alive
                Vector2Int player2Pos = player2.GetGridPosition();

                player2.Die();
                player1.isAlive = true;
                player1.SetGridPosition(player2Pos, preserveMovement: false);

                Renderer rend1 = player1.GetComponent<Renderer>();
                if (rend1 != null)
                {
                    rend1.enabled = true;
                }

                Debug.Log("Life swap: Player 1 now alive, Player 2 now dead");
            }
            else if (player1.isAlive && !player2.isAlive)
            {
                // Player 1 is alive, Player 2 is dead
                Vector2Int player1Pos = player1.GetGridPosition();

                player1.Die();
                player2.isAlive = true;
                player2.SetGridPosition(player1Pos, preserveMovement: false);

                Renderer rend2 = player2.GetComponent<Renderer>();
                if (rend2 != null)
                {
                    rend2.enabled = true;
                }

                Debug.Log("Life swap: Player 2 now alive, Player 1 now dead");
            }
        }
        else
        {
            // Normal swap: Both alive, just swap positions
            Vector2Int pos1 = player1.GetGridPosition();
            Vector2Int pos2 = player2.GetGridPosition();

            player1.SetGridPosition(pos2, preserveMovement: false);
            player2.SetGridPosition(pos1, preserveMovement: false);

            Debug.Log("Normal position swap");
        }

        isSwapWindowActive = false;
        uiManager?.ShowSwapWindow(false);

        canSwap = false;
        swapCooldownTimer = swapCooldown;
    }

    /// <summary>
    /// Cancels the swap if time runs out.
    /// </summary>
    void CancelSwap()
    {
        isSwapWindowActive = false;
        swapInitiator = 0;
        uiManager?.ShowSwapWindow(false);
    }

    /// <summary>
    /// Updates all UI elements.
    /// </summary>
    void UpdateUI()
    {
        uiManager?.UpdateScores(player1Score, player2Score, highScore);
        uiManager?.UpdateLives(lives);
        uiManager?.UpdateLevel(currentLevel);
    }

    /// <summary>
    /// Called when both players are dead.
    /// </summary>
    void LoseGame()
    {
        gameOver = true;

        player1.enabled = false;
        player2.enabled = false;

        foreach (GhostAI ghost in ghosts)
        {
            ghost.enabled = false;
        }

        AudioManager.Instance?.PlayGameOver();

        uiManager?.ShowGameOver(currentLevel, currentScore, highScore);

        Debug.Log("===== GAME OVER =====");
        Debug.Log($"Final Level: {currentLevel}");
        Debug.Log($"Final Score: {currentScore}");
        Debug.Log($"High Score: {highScore}");
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