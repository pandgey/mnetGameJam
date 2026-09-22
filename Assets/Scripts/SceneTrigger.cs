using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

[RequireComponent (typeof (BoxCollider2D))]
public class SceneTrigger : MonoBehaviour {

    [Header ("Destination")]
    [SerializeField] private string sceneName = "HumanErrorReport";

    [Header ("Activation")]
    [Tooltip ("Off: the scene loads the moment the player walks in. On: the player has to press the key below while standing inside.")]
    [SerializeField] private bool requireKeyPress = false;
    [SerializeField] private Key interactKey = Key.E;

    private bool _playerInside;
    private bool _loading;

    void Reset () {
        // A door the player can walk through, not a wall they bump into.
        GetComponent<BoxCollider2D> ().isTrigger = true;
    }

    void OnTriggerEnter2D (Collider2D other) {
        // The player is untagged, so look the movement script up instead.
        if (other.GetComponentInParent<PlayerMovement> () == null)
            return;

        _playerInside = true;

        if (!requireKeyPress)
            Load ();
    }

    void OnTriggerExit2D (Collider2D other) {
        if (other.GetComponentInParent<PlayerMovement> () != null)
            _playerInside = false;
    }

    void Update () {
        if (!_playerInside || !requireKeyPress)
            return;

        Keyboard keyboard = Keyboard.current;
        if (keyboard != null && keyboard[interactKey].wasPressedThisFrame)
            Load ();
    }

    private void Load () {
        // The load finishes at the end of the frame, so without this guard a second trigger frame could queue the scene twice.
        if (_loading)
            return;

        if (string.IsNullOrWhiteSpace (sceneName)) {
            Debug.LogWarning ($"{name}: no destination scene set.", this);
            return;
        }

        _loading = true;
        SceneManager.LoadScene (sceneName);
    }

    void OnDrawGizmos () {
        // Draw the doorway in the editor; the collider itself is invisible in play mode.
        BoxCollider2D box = GetComponent<BoxCollider2D> ();
        if (box == null)
            return;

        Gizmos.color = new Color (0.3f, 0.8f, 1f, 0.35f);
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.DrawCube (box.offset, box.size);
    }
}
