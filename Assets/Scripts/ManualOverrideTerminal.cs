using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

// Lab's single-use terminal. The existing controller owns the timing check itself.
[RequireComponent(typeof(BoxCollider2D))]
public class ManualOverrideTerminal : MonoBehaviour
{
    [SerializeField] private ManualOverrideController skillCheck;
    [SerializeField] private GameObject skillCheckOverlay;
    [SerializeField] private GameObject prompt;
    [SerializeField] private Collider2D doorCollider;
    [SerializeField] private GameObject doorVisual;

    public bool HasCompleted { get; private set; }
    public bool OverrideFailed { get; private set; }

    private PlayerMovement player;
    private PlayerRespawn respawn;
    private Rigidbody2D body;
    private Collider2D playerCollider;
    private bool nearby, interactionArmed, running, playerLocked;
    private bool movementWasEnabled, respawnWasEnabled;
    private RigidbodyConstraints2D previousConstraints;
    private readonly RaycastHit2D[] groundHits = new RaycastHit2D[8];

    private void Awake()
    {
        if (prompt) prompt.SetActive(false);
        if (!skillCheck || !skillCheckOverlay || !prompt || !doorCollider || !doorVisual)
        {
            Debug.LogError("Manual Override terminal: assign the UI, prompt and door references.", this);
            enabled = false;
        }
    }

    private void OnEnable()
    {
        if (skillCheck) skillCheck.Completed += OnCompleted;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (HasCompleted || playerLocked) return;
        var movement = other.GetComponentInParent<PlayerMovement>();
        if (!movement) return;
        player = movement;
        body = player.GetComponent<Rigidbody2D>();
        respawn = player.GetComponent<PlayerRespawn>();
        playerCollider = player.GetComponent<Collider2D>();
        nearby = true;
        interactionArmed = false;
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.GetComponentInParent<PlayerMovement>() != player) return;
        nearby = false;
        interactionArmed = false;
        if (prompt) prompt.SetActive(false);
    }

    private void Update()
    {
        if (running && (!skillCheck || !skillCheck.isActiveAndEnabled ||
            !skillCheck.IsRunning || !skillCheckOverlay || !skillCheckOverlay.activeInHierarchy))
        {
            FailOpen();
            return;
        }

        if (HasCompleted || running || playerLocked) return;
        bool available = nearby && player && player.isActiveAndEnabled && body && body.simulated && respawn;
        prompt.SetActive(available);
        if (!available) { interactionArmed = false; return; }

        var keyboard = Keyboard.current;
        if (keyboard == null) return;
        if (!keyboard.eKey.isPressed && !keyboard.eKey.wasPressedThisFrame)
            interactionArmed = true;

        if (interactionArmed && keyboard.eKey.wasPressedThisFrame)
        {
            interactionArmed = false;
            if (IsStanding()) BeginCheck();
        }
    }

    private bool IsStanding()
    {
        if (!playerCollider || Mathf.Abs(body.linearVelocity.y) > 0.1f) return false;
        var filter = ContactFilter2D.noFilter;
        filter.useTriggers = false;
        int hits = playerCollider.Cast(Vector2.down, filter, groundHits, 0.05f);
        for (int i = 0; i < hits; i++)
            if (groundHits[i].normal.y >= 0.5f) return true;
        return false;
    }

    private void BeginCheck()
    {
        if (!skillCheck || !skillCheck.isActiveAndEnabled || skillCheck.IsRunning)
        {
            FailOpen();
            return;
        }

        movementWasEnabled = player.enabled;
        respawnWasEnabled = respawn.enabled;
        previousConstraints = body.constraints;
        playerLocked = true;
        player.enabled = false;
        player.ResetForRespawn();
        // Keep simulation/collisions active; only freeze this body, never the clock.
        body.constraints = RigidbodyConstraints2D.FreezeAll;
        respawn.enabled = false;
        prompt.SetActive(false);
        running = true;
        skillCheck.StartCheck();
    }

    private void OnCompleted(bool success)
    {
        if (!running || HasCompleted) return;
        OverrideFailed = !success;
        running = false;
        OpenDoor();
        StartCoroutine(RestoreAfterInputRelease());
    }

    private IEnumerator RestoreAfterInputRelease()
    {
        // A held movement/jump key must be released before normal input resumes.
        do
        {
            while (MovementInputHeld()) yield return null;
            yield return null;
        } while (MovementInputHeld());
        RestorePlayer();
    }

    private static bool MovementInputHeld()
    {
        var keyboard = Keyboard.current;
        if (keyboard == null) return false;
        return keyboard.aKey.isPressed || keyboard.dKey.isPressed ||
            keyboard.leftArrowKey.isPressed || keyboard.rightArrowKey.isPressed ||
            keyboard.spaceKey.isPressed || keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed ||
            keyboard.spaceKey.wasPressedThisFrame || keyboard.wKey.wasPressedThisFrame || keyboard.upArrowKey.wasPressedThisFrame;
    }

    private void OpenDoor()
    {
        HasCompleted = true;
        if (doorCollider) doorCollider.enabled = false;
        if (doorVisual) doorVisual.SetActive(false);
        if (prompt) prompt.SetActive(false);
    }

    private void RestorePlayer()
    {
        if (!playerLocked) return;
        playerLocked = false;
        if (player) player.ResetForRespawn();
        if (body) body.constraints = previousConstraints;
        if (respawn) respawn.enabled = respawnWasEnabled;
        if (player) player.enabled = movementWasEnabled;
    }

    private void FailOpen()
    {
        running = false;
        // OnDisable cancels the existing controller without producing a result.
        if (skillCheck && skillCheck.IsRunning)
        {
            bool wasEnabled = skillCheck.enabled;
            skillCheck.enabled = false;
            skillCheck.enabled = wasEnabled;
        }
        OpenDoor();
        StopAllCoroutines();
        RestorePlayer();
    }

    private void OnDisable()
    {
        if (skillCheck) skillCheck.Completed -= OnCompleted;
        // Also handles a terminal being disabled while waiting for input release.
        FailOpen();
    }
}
