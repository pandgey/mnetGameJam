using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

[RequireComponent (typeof (BoxCollider2D))]
public class SceneTrigger : MonoBehaviour {

    [Header ("Destination")]
    [SerializeField] private string sceneName = "HumanErrorReport";

    [Header ("Level result (optional)")]
    [SerializeField] private bool captureLevelResult = false;
    [SerializeField] private LevelTimer levelTimer;
    [SerializeField] private string nextSceneAfterReport = "Rooftop";
    [SerializeField] private bool isFinalReport = false;

    [Header ("Activation")]
    [Tooltip ("Off: the scene loads the moment the player walks in. On: the player has to press the key below while standing inside.")]
    [SerializeField] private bool requireKeyPress = false;
    [SerializeField] private Key interactKey = Key.E;

    private bool _playerInside;
    private bool _loading;
    private PlayerRespawn _enteringPlayer;

    void Reset () {
        // A door the player can walk through, not a wall they bump into.
        GetComponent<BoxCollider2D> ().isTrigger = true;
    }

    void OnTriggerEnter2D (Collider2D other) {
        // The player is untagged, so look the movement script up instead.
        if (other.GetComponentInParent<PlayerMovement> () == null)
            return;

        _playerInside = true;
        _enteringPlayer = other.GetComponentInParent<PlayerRespawn> ();

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

        if (captureLevelResult) {
            if (levelTimer == null)
                levelTimer = FindFirstObjectByType<LevelTimer> ();

            if (levelTimer == null || _enteringPlayer == null || string.IsNullOrWhiteSpace (nextSceneAfterReport)) {
                Debug.LogError ($"{name}: level completion needs a LevelTimer, PlayerRespawn and report destination.", this);
                return;
            }
        }

        _loading = true;
        if (captureLevelResult) {
            levelTimer.StopTimer ();
            LevelResult.Store (levelTimer.Elapsed, _enteringPlayer.DeathCount,
                gameObject.scene.name, nextSceneAfterReport, isFinalReport);
        }
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
