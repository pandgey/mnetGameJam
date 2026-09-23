using System.Collections;
using UnityEngine;

[RequireComponent(typeof(PlayerMovement), typeof(Rigidbody2D))]
public class PlayerRespawn : MonoBehaviour
{
    [SerializeField] private float fallDeathY = -6f;

    [Header("Audio")]
    [SerializeField] private AudioClip deathSound;
    [SerializeField, Range(0f, 1f)] private float deathSoundVolume = 1f;

    public int DeathCount { get; private set; }

    private PlayerMovement movement;
    private Rigidbody2D body;
    private AudioSource audioSource;
    private Vector2 respawnPosition;
    private bool isRespawning;

    private void Awake()
    {
        movement = GetComponent<PlayerMovement>();
        body = GetComponent<Rigidbody2D>();
        audioSource = PlayerAudio.GetOrAddSource(gameObject);
        respawnPosition = body.position;
    }

    private void FixedUpdate()
    {
        if (body.position.y < fallDeathY)
            Die();
    }

    public void SetCheckpoint(Vector2 position)
    {
        if (!isRespawning)
            respawnPosition = position;
    }

    public void Die()
    {
        if (isRespawning || !isActiveAndEnabled)
            return;

        isRespawning = true;
        DeathCount++;
        PlayerAudio.Play(audioSource, deathSound, deathSoundVolume);
        StartCoroutine(Respawn());
    }

    private IEnumerator Respawn()
    {
        movement.enabled = false;
        body.simulated = false;
        movement.ResetForRespawn();
        // Keep the Transform in sync while the Rigidbody is outside simulation.
        transform.position = new Vector3(respawnPosition.x, respawnPosition.y, transform.position.z);
        body.position = respawnPosition;

        // Let queued contacts and the current input frame finish before resuming.
        yield return null;
        yield return new WaitForFixedUpdate();

        movement.ResetForRespawn();
        body.simulated = true;
        movement.enabled = true;
        isRespawning = false;
    }
}
