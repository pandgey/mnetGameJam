using UnityEngine;

// Runs after CameraFollow so the layer reads the camera's final position for this frame and does not jitter.
[DefaultExecutionOrder (100)]
public class ParallaxLayer : MonoBehaviour {

    [Header ("Camera")]
    [SerializeField] private Transform cameraTransform;

    [Header ("Parallax")]
    [Tooltip ("0 = fixed in the world, 1 = moves with the camera (appears infinitely far away).")]
    [SerializeField, Range (0f, 1f)] private float followX = 0.8f;
    [SerializeField, Range (0f, 1f)] private float followY = 0.8f;

    private Vector3 _lastCameraPosition;

    void Awake () {
        if (cameraTransform == null && Camera.main != null)
            cameraTransform = Camera.main.transform;
    }

    void Start () {
        // CameraFollow snaps to the player in Start, so sample the camera after that.
        if (cameraTransform != null)
            _lastCameraPosition = cameraTransform.position;
    }

    void LateUpdate () {
        if (cameraTransform == null)
            return;

        Vector3 cameraDelta = cameraTransform.position - _lastCameraPosition;
        transform.position += new Vector3 (cameraDelta.x * followX, cameraDelta.y * followY, 0f);
        _lastCameraPosition = cameraTransform.position;
    }
}
