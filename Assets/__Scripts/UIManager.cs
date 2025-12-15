using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    [Header("Score Display")]
    public TextMeshProUGUI player1ScoreText;
    public TextMeshProUGUI player2ScoreText;
    public TextMeshProUGUI totalScoreText;
    public TextMeshProUGUI highScoreText;

    [Header("Level Display")]
    public TextMeshProUGUI levelText;

    [Header("Lives Display")]
    public TextMeshProUGUI livesText;

    [Header("Swap UI")]
    public GameObject swapPanel;
    public TextMeshProUGUI swapTimerText;

    [Header("Power-Up UI")]
    public TextMeshProUGUI powerUpText;

    [Header("Level Complete UI")]
    public GameObject levelCompletePanel;
    public TextMeshProUGUI levelCompleteText;

    [Header("Game Over UI")]
    public GameObject gameOverPanel;
    public TextMeshProUGUI gameOverText;
    public TextMeshProUGUI finalScoreText;
    public TextMeshProUGUI finalLevelText;
    public Button restartButton;

    void Start()
    {
        if (restartButton != null)
        {
            restartButton.onClick.AddListener(() => GameManager.Instance?.RestartGame());
        }

        if (swapPanel != null) swapPanel.SetActive(false);
        if (powerUpText != null) powerUpText.gameObject.SetActive(false);
        if (levelCompletePanel != null) levelCompletePanel.SetActive(false);
        if (gameOverPanel != null) gameOverPanel.SetActive(false);
    }

    public void UpdateScores(int p1Score, int p2Score, int highScore)
    {
        if (player1ScoreText != null)
        {
            player1ScoreText.text = $"P1: {p1Score}";
        }

        if (player2ScoreText != null)
        {
            player2ScoreText.text = $"P2: {p2Score}";
        }

        int totalScore = p1Score + p2Score;

        if (totalScoreText != null)
        {
            totalScoreText.text = $"SCORE: {totalScore}";
        }

        if (highScoreText != null)
        {
            highScoreText.text = $"HIGH SCORE: {highScore}";
        }
    }

    public void UpdateLevel(int level)
    {
        if (levelText != null)
        {
            levelText.text = $"LEVEL {level}";
        }
    }

    public void UpdateLives(int lives)
    {
        if (livesText != null)
        {
            livesText.text = $"LIVES: {lives}";
        }
    }

    public void ShowSwapWindow(bool show)
    {
        if (swapPanel != null)
        {
            swapPanel.SetActive(show);
        }
    }

    public void UpdateSwapTimer(float time)
    {
        if (swapTimerText != null)
        {
            swapTimerText.text = time.ToString("F1");
        }
    }

    public void ShowPowerUpActive(bool active)
    {
        if (powerUpText != null)
        {
            powerUpText.gameObject.SetActive(active);
        }
    }

    public void ShowLevelComplete(int level)
    {
        if (levelCompletePanel != null)
        {
            levelCompletePanel.SetActive(true);
        }

        if (levelCompleteText != null)
        {
            levelCompleteText.text = $"LEVEL {level} COMPLETE!";
        }
    }

    public void HideLevelComplete()
    {
        if (levelCompletePanel != null)
        {
            levelCompletePanel.SetActive(false);
        }
    }

    public void ShowGameOver(int finalLevel, int finalScore, int highScore)
    {
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(true);
        }

        if (gameOverText != null)
        {
            gameOverText.text = "GAME OVER";
            gameOverText.color = Color.red;
        }

        if (finalLevelText != null)
        {
            finalLevelText.text = $"Reached Level {finalLevel}";
        }

        if (finalScoreText != null)
        {
            if (finalScore >= highScore)
            {
                finalScoreText.text = $"NEW HIGH SCORE: {finalScore}!";
                finalScoreText.color = Color.yellow;
            }
            else
            {
                finalScoreText.text = $"Score: {finalScore}\nHigh Score: {highScore}";
                finalScoreText.color = Color.white;
            }
        }
    }
}