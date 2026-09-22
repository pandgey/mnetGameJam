using UnityEngine;

[RequireComponent (typeof (Camera))]
public class CameraFollow : MonoBehaviour {

    [Header ("Target")]
    [SerializeField] private Transform target;

    [Header ("Framing")]
    [SerializeField] private Vector2 offset = Vector2.zero;
    [SerializeField] private bool followX = true;
    [SerializeField] private bool followY = true;

    [Header ("Smoothing")]
    [SerializeField, Range (0f, 1f)] private float smoothTime = 0.15f;

    private float _depth;
    private Vector3 _velocity;

    void Awake () {
        if (target == null) {
            // The player is untagged, so look the movement script up instead.
            PlayerMovement player = FindFirstObjectByType<PlayerMovement> ();
            if (player != null)
                target = player.transform;
        }

        // A 2D camera has to stay in front of the sprites it renders.
        _depth = transform.position.z;
    }

    void Start () {
        // Frame the target immediately so the first frame does not pan into place.
        if (target != null)
            transform.position = DesiredPosition ();
    }

    void LateUpdate () {
        // LateUpdate runs after movement, and the body interpolates, so the target has already settled for this frame.
        if (target == null)
            return;

        transform.position = smoothTime > 0f
            ? Vector3.SmoothDamp (transform.position, DesiredPosition (), ref _velocity, smoothTime)
            : DesiredPosition ();
    }

    private Vector3 DesiredPosition () {
        Vector3 current = transform.position;
        Vector2 wanted = (Vector2) target.position + offset;

        return new Vector3 (
            followX ? wanted.x : current.x,
            followY ? wanted.y : current.y,
            _depth);
    }
}
