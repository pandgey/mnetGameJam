using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuController : MonoBehaviour {

    [SerializeField] private string gameSceneName = "Lab";

    private void Awake () {
        LevelResult.Clear ();
    }

    public void PlayGame () {
        LevelResult.Clear ();
        SceneManager.LoadScene (gameSceneName);
    }

    public void QuitGame () {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit ();
#endif
    }
}
