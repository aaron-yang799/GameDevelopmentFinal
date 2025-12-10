using UnityEngine;
using TMPro;
using UnityEngine.UI;

/// <summary>
/// Manages all UI elements in the game.
/// Updates scores, lives, swap UI, power-up indicator, and game over screen.
/// </summary>
public class UIManager : MonoBehaviour
{
    [Header("Score Display")]
    public TextMeshProUGUI player1ScoreText;
    public TextMeshProUGUI player2ScoreText;
    public TextMeshProUGUI totalScoreText;
    
    [Header("Lives Display")]
    public TextMeshProUGUI livesText;
    
    [Header("Swap UI")]
    public GameObject swapPanel;
    public TextMeshProUGUI swapTimerText;
    
    [Header("Power-Up UI")]
    public TextMeshProUGUI powerUpText;
    
    [Header("Game Over UI")]
    public GameObject gameOverPanel;
    public TextMeshProUGUI gameOverText;
    public Button restartButton;
    
    void Start()
    {
        // Connect restart button to GameManager
        if (restartButton != null)
        {
            restartButton.onClick.AddListener(() => GameManager.Instance?.RestartGame());
        }
        
        // Make sure panels are hidden at start
        if (swapPanel != null) swapPanel.SetActive(false);
        if (powerUpText != null) powerUpText.gameObject.SetActive(false);
        if (gameOverPanel != null) gameOverPanel.SetActive(false);
    }
    
    /// <summary>
    /// Updates score displays for both players and total.
    /// </summary>
    public void UpdateScores(int p1Score, int p2Score)
    {
        if (player1ScoreText != null)
        {
            player1ScoreText.text = $"P1: {p1Score}";
        }
        
        if (player2ScoreText != null)
        {
            player2ScoreText.text = $"P2: {p2Score}";
        }
        
        if (totalScoreText != null)
        {
            totalScoreText.text = $"SCORE: {p1Score + p2Score}";
        }
    }
    
    /// <summary>
    /// Updates lives display.
    /// </summary>
    public void UpdateLives(int lives)
    {
        if (livesText != null)
        {
            livesText.text = $"LIVES: {lives}";
        }
    }
    
    /// <summary>
    /// Shows or hides the swap window.
    /// </summary>
    public void ShowSwapWindow(bool show)
    {
        if (swapPanel != null)
        {
            swapPanel.SetActive(show);
        }
    }
    
    /// <summary>
    /// Updates the swap countdown timer.
    /// </summary>
    public void UpdateSwapTimer(float time)
    {
        if (swapTimerText != null)
        {
            swapTimerText.text = time.ToString("F1");
        }
    }
    
    /// <summary>
    /// Shows or hides power-up active indicator.
    /// </summary>
    public void ShowPowerUpActive(bool active)
    {
        if (powerUpText != null)
        {
            powerUpText.gameObject.SetActive(active);
        }
    }
    
    /// <summary>
    /// Shows game over screen.
    /// </summary>
    /// <param name="won">True if players won, false if they lost</param>
    public void ShowGameOver(bool won)
    {
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(true);
        }
        
        if (gameOverText != null)
        {
            gameOverText.text = won ? "YOU WIN!" : "GAME OVER";
            gameOverText.color = won ? Color.green : Color.red;
        }
    }
}
