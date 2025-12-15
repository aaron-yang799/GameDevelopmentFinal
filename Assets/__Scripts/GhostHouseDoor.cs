using UnityEngine;

/// <summary>
/// One-way door for the ghost house.
/// Blocks players but allows ghosts to pass through.
/// Works with both trigger and solid colliders.
/// </summary>
public class GhostHouseDoor : MonoBehaviour
{
    [Header("Door Settings")]
    public bool allowGhosts = true;
    public bool blockPlayers = true;

    void Start()
    {
        // Set layer
        gameObject.layer = LayerMask.NameToLayer("GhostHouseDoor");

        // Make sure we have both a solid and trigger collider
        BoxCollider[] colliders = GetComponents<BoxCollider>();

        if (colliders.Length == 0)
        {
            Debug.LogError("GhostHouseDoor needs at least one Box Collider!");
        }

        Debug.Log("Ghost House Door initialized");
    }

    void OnTriggerEnter(Collider other)
    {
        // Block players from entering
        if (blockPlayers && other.CompareTag("Player"))
        {
            PlayerController player = other.GetComponent<PlayerController>();
            if (player != null && player.isAlive)
            {
                // Don't let players enter - reset to just outside
                Vector3 pushDirection = (other.transform.position - transform.position).normalized;
                pushDirection.y = 0;

                // Teleport player back outside
                other.transform.position += pushDirection * 1f;

                Debug.Log("Player blocked from ghost house");
            }
        }
    }

    void OnTriggerStay(Collider other)
    {
        // Continuously push players out if they somehow get in
        if (blockPlayers && other.CompareTag("Player"))
        {
            PlayerController player = other.GetComponent<PlayerController>();
            if (player != null && player.isAlive)
            {
                Vector3 pushDirection = (other.transform.position - transform.position).normalized;
                pushDirection.y = 0;

                other.transform.position += pushDirection * 3f * Time.deltaTime;
            }
        }
    }
}