using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public class Checkpoint : MonoBehaviour
{
    [Tooltip("Safe position for the player's centre, above the platform and outside hazards.")]
    [SerializeField] private Transform respawnPoint;

    private void Reset()
    {
        GetComponent<BoxCollider2D>().isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        var player = other.GetComponentInParent<PlayerRespawn>();
        if (player != null && respawnPoint != null)
            player.SetCheckpoint(respawnPoint.position);
    }
}
