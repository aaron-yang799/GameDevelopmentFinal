using UnityEngine;

/// <summary>
/// Handles pellet collection logic.
/// Each pellet is assigned to a player number and can be either regular or power pellet.
/// </summary>
public class Pellet : MonoBehaviour
{
    [Header("Pellet Settings")]
    public int playerNumber = 1; // 1 or 2 - which player can eat this pellet
    public bool isPowerPellet = false;
    public int points = 10;
    
    void Start()
    {
        // Power pellets are worth more points
        if (isPowerPellet)
        {
            points = 50;
        }
    }
    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            PlayerController player = other.GetComponent<PlayerController>();

            if (player != null && player.playerNumber == playerNumber && player.isAlive)
            {
                // Play appropriate sound
                if (isPowerPellet)
                {
                    AudioManager.Instance?.PlayPowerPelletEaten();
                }
                else
                {
                    AudioManager.Instance?.PlayPelletEaten();
                }

                // Notify game manager
                GameManager.Instance?.CollectPellet(this, player.playerNumber);

                // Destroy pellet
                Destroy(gameObject);
            }
        }
    }
}
