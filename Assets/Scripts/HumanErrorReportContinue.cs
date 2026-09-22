using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Acknowledges the report, then moves the player on to the next level.
[RequireComponent(typeof(Button))]
public class HumanErrorReportContinue : MonoBehaviour
{
    [SerializeField] private string acknowledgementText = "ACKNOWLEDGED";

    [Header("Destination")]
    [SerializeField] private string nextSceneName = "Rooftop";
    [Tooltip("Seconds the acknowledgement stays on screen before the next scene loads.")]
    [Min(0)] [SerializeField] private float loadDelay = 0.45f;

    private Button continueButton;
    private TMP_Text buttonLabel;
    private bool loading;

    private void Awake()
    {
        continueButton = GetComponent<Button>();
        buttonLabel = GetComponentInChildren<TMP_Text>();
        continueButton.onClick.AddListener(AcknowledgeReport);
    }

    private void AcknowledgeReport()
    {
        if (loading)
            return;

        if (buttonLabel != null)
            buttonLabel.text = acknowledgementText;

        continueButton.interactable = false;

        if (string.IsNullOrWhiteSpace(nextSceneName))
        {
            Debug.LogWarning($"{name}: no destination scene set, staying on the report.", this);
            return;
        }

        loading = true;
        StartCoroutine(LoadNextScene());
    }

    private IEnumerator LoadNextScene()
    {
        // Unscaled, to match the reveal: the report runs while the game is paused.
        yield return new WaitForSecondsRealtime(loadDelay);
        SceneManager.LoadScene(nextSceneName);
    }

    private void OnDestroy()
    {
        if (continueButton != null)
            continueButton.onClick.RemoveListener(AcknowledgeReport);
    }
}
