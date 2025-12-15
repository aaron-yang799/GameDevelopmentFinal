using UnityEngine;

public class WrapAroundTeleport : MonoBehaviour
{
    [Header("Teleport Settings")]
    public WrapAroundTeleport pairedTeleporter;
    public float teleportCooldown = 0.5f;

    [HideInInspector]
    public float lastTeleportTime = -999f;

    void OnTriggerEnter(Collider other)
    {
        if (pairedTeleporter == null)
        {
            Debug.LogError($"{gameObject.name} has no paired teleporter!");
            return;
        }

        if (Time.time - lastTeleportTime < teleportCooldown ||
            Time.time - pairedTeleporter.lastTeleportTime < teleportCooldown)
        {
            return;
        }

        if (other.CompareTag("Player"))
        {
            PlayerController player = other.GetComponent<PlayerController>();
            if (player != null && player.isAlive)
            {
                TeleportPlayer(player);
            }
        }
        else if (other.CompareTag("Ghost"))
        {
            TeleportGhost(other.transform);
        }
    }

    void TeleportPlayer(PlayerController player)
    {
        Vector2Int destGridPos = GridManager.Instance.WorldToGrid(pairedTeleporter.transform.position);

        player.SetGridPosition(destGridPos);

        lastTeleportTime = Time.time;
        pairedTeleporter.lastTeleportTime = Time.time;
    }

    void TeleportGhost(Transform ghostTransform)
    {
        ghostTransform.position = pairedTeleporter.transform.position;

        lastTeleportTime = Time.time;
        pairedTeleporter.lastTeleportTime = Time.time;
    }

    void OnDrawGizmos()
    {
        if (pairedTeleporter != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(transform.position, pairedTeleporter.transform.position);

            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, 0.5f);

            Gizmos.color = Color.magenta;
            Gizmos.DrawWireSphere(pairedTeleporter.transform.position, 0.5f);
        }
        else
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, 0.5f);
        }
    }
}