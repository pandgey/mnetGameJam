using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent (typeof (Rigidbody2D))]
[RequireComponent (typeof (BoxCollider2D))]
public class PlayerMovement : MonoBehaviour {

    [Header ("Movement")]
    [SerializeField] private float moveSpeed = 7f;

    [Header ("Jump")]
    [SerializeField] private float jumpSpeed = 12f;
    [SerializeField] private float coyoteTime = 0.1f;
    [SerializeField] private float jumpBufferTime = 0.1f;
    [SerializeField, Range (0f, 1f)] private float jumpCutMultiplier = 0.5f;

    [Header ("Audio")]
    [SerializeField] private AudioClip jumpSound;
    [SerializeField, Range (0f, 1f)] private float jumpSoundVolume = 1f;

    [Header ("Ground check")]
    [SerializeField] private float groundCheckDistance = 0.05f;
    [SerializeField] private float minGroundNormalY = 0.5f;

    [Header ("Visuals")]
    [Tooltip ("Tick if the sprite sheet frames are drawn facing right.")]
    [SerializeField] private bool artFacesRight = true;

    private Rigidbody2D _body;
    private Collider2D _collider;
    private AudioSource _audioSource;
    private Animator _animator;
    private SpriteRenderer _sprite;
    private ContactFilter2D _groundFilter;
    private readonly RaycastHit2D[] _groundHits = new RaycastHit2D[8];

    private float _moveInput;
    private bool _jumpHeld;
    private bool _rising;
    private bool _grounded;
    private float _timeSinceGrounded = Mathf.Infinity;
    private float _timeSinceJumpPressed = Mathf.Infinity;

    private static readonly int SpeedParam = Animator.StringToHash ("Speed");
    private static readonly int GroundedParam = Animator.StringToHash ("Grounded");
    private static readonly int VelocityYParam = Animator.StringToHash ("VelocityY");

    void Awake () {
        _body = GetComponent<Rigidbody2D> ();
        _collider = GetComponent<Collider2D> ();
        _audioSource = PlayerAudio.GetOrAddSource (gameObject);
        // Visuals may sit on a child object so the art can be offset from the collider.
        _animator = GetComponentInChildren<Animator> ();
        _sprite = GetComponentInChildren<SpriteRenderer> ();

        // Rotation would let the player topple over when it lands on a corner.
        _body.freezeRotation = true;

        _groundFilter = ContactFilter2D.noFilter;
        _groundFilter.useTriggers = false;
    }

    void Update () {
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null)
            return;

        _moveInput = 0f;
        if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed)
            _moveInput -= 1f;
        if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed)
            _moveInput += 1f;

        _jumpHeld = keyboard.spaceKey.isPressed || keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed;

        bool jumpPressed = keyboard.spaceKey.wasPressedThisFrame
            || keyboard.wKey.wasPressedThisFrame
            || keyboard.upArrowKey.wasPressedThisFrame;

        _timeSinceJumpPressed = jumpPressed ? 0f : _timeSinceJumpPressed + Time.deltaTime;

        UpdateVisuals ();
    }

    void FixedUpdate () {
        _grounded = IsGrounded ();
        _timeSinceGrounded = _grounded ? 0f : _timeSinceGrounded + Time.fixedDeltaTime;

        Vector2 velocity = _body.linearVelocity;
        velocity.x = _moveInput * moveSpeed;

        if (_timeSinceJumpPressed <= jumpBufferTime && _timeSinceGrounded <= coyoteTime) {
            velocity.y = jumpSpeed;
            _rising = true;
            PlayerAudio.Play (_audioSource, jumpSound, jumpSoundVolume);

            // Consume both windows so one press cannot trigger a second jump.
            _timeSinceJumpPressed = Mathf.Infinity;
            _timeSinceGrounded = Mathf.Infinity;
        } else if (_rising && !_jumpHeld && velocity.y > 0f) {
            // Releasing the button early gives a shorter hop.
            velocity.y *= jumpCutMultiplier;
            _rising = false;
        }

        if (velocity.y <= 0f)
            _rising = false;

        _body.linearVelocity = velocity;
    }

    public void ResetForRespawn () {
        _moveInput = 0f;
        _jumpHeld = false;
        _rising = false;
        _grounded = false;
        _timeSinceGrounded = Mathf.Infinity;
        _timeSinceJumpPressed = Mathf.Infinity;
        _body.linearVelocity = Vector2.zero;
        _body.angularVelocity = 0f;
    }

    private void UpdateVisuals () {
        // Keep facing the last direction pressed instead of snapping back when idle.
        if (_sprite != null && _moveInput != 0f)
            _sprite.flipX = (_moveInput < 0f) == artFacesRight;

        // Without a controller assigned the Animator would warn about every missing parameter.
        if (_animator == null || _animator.runtimeAnimatorController == null)
            return;

        Vector2 velocity = _body.linearVelocity;
        _animator.SetFloat (SpeedParam, Mathf.Abs (velocity.x));
        _animator.SetBool (GroundedParam, _grounded);
        _animator.SetFloat (VelocityYParam, velocity.y);
    }

    private bool IsGrounded () {
        // Casting the collider ignores itself, so no ground layer setup is needed.
        int hitCount = _collider.Cast (Vector2.down, _groundFilter, _groundHits, groundCheckDistance);

        for (int i = 0; i < hitCount; i++) {
            // A wall we are pressed against also reports a hit; only floors count.
            if (_groundHits[i].normal.y >= minGroundNormalY)
                return true;
        }

        return false;
    }
}
