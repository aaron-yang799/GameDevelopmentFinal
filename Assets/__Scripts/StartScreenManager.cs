using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Manages the start screen and loads the game scene.
/// </summary>
public class StartScreenManager : MonoBehaviour
{
    public string gameSceneName = "GameScene"; // Name of your main game scene
    
    /// <summary>
    /// Loads the main game scene.
    /// Called by the Start Button.
    /// </summary>
    public void StartGame()
    {
        Debug.Log("Loading game scene...");
        SceneManager.LoadScene(gameSceneName);
    }
    
    /// <summary>
    /// Quits the application.
    /// Optional - for a Quit button.
    /// </summary>
    public void QuitGame()
    {
        Debug.Log("Quitting game...");
        Application.Quit();
        
        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #endif
    }
}