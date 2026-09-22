using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Acknowledges the standalone report; level transitions are not connected yet.
[RequireComponent(typeof(Button))]
public class HumanErrorReportContinue : MonoBehaviour
{
    [SerializeField] private string acknowledgementText = "ACKNOWLEDGED";

    private Button continueButton;
    private TMP_Text buttonLabel;

    private void Awake()
    {
        continueButton = GetComponent<Button>();
        buttonLabel = GetComponentInChildren<TMP_Text>();
        continueButton.onClick.AddListener(AcknowledgeReport);
    }

    private void AcknowledgeReport()
    {
        if (buttonLabel != null)
            buttonLabel.text = acknowledgementText;

        continueButton.interactable = false;
    }

    private void OnDestroy()
    {
        if (continueButton != null)
            continueButton.onClick.RemoveListener(AcknowledgeReport);
    }
}
