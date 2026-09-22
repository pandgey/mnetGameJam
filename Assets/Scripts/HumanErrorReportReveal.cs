using System.Collections;
using System.Globalization;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// Presentation only: the report's Continue component still owns acknowledgement.
[DisallowMultipleComponent]
public class HumanErrorReportReveal : MonoBehaviour
{
    [Header("Existing report objects (assigned when added to Canvas)")]
    [SerializeField] private Graphic[] header;
    [SerializeField] private TMP_Text title;
    [SerializeField] private Graphic[] completionRow;
    [SerializeField] private Graphic[] fatalRow;
    [SerializeField] private Graphic[] systemRow;
    [SerializeField] private TMP_Text errorIndex;
    [SerializeField] private Graphic[] warning;
    [SerializeField] private TMP_Text disclaimer;
    [SerializeField] private Button continueButton;

    [Header("Reveal times (seconds from opening)")]
    [Min(0)] [SerializeField] private float headerTime = 0.20f;
    [Min(0)] [SerializeField] private float titleTime = 0.32f;
    [Min(0)] [SerializeField] private float completionTime = 0.60f;
    [Min(0)] [SerializeField] private float fatalTime = 0.90f;
    [Min(0)] [SerializeField] private float systemTime = 1.20f;
    [Min(0)] [SerializeField] private float processingTime = 1.40f;
    [Min(0)] [SerializeField] private float countTime = 1.80f;
    [Min(0.01f)] [SerializeField] private float countDuration = 0.65f;
    [Min(0)] [SerializeField] private float warningTime = 2.55f;
    [Min(0)] [SerializeField] private float footerTime = 2.80f;
    [Min(0.01f)] [SerializeField] private float fadeDuration = 0.12f;
    [Min(0.01f)] [SerializeField] private float footerFadeDuration = 0.20f;

    [Header("Mock result")]
    [Range(0, 100)] [SerializeField] private float finalHumanErrorIndex = 27.4f;

    private CanvasGroup[][] sections;
    private TMP_Text processingLabel;
    private string indexTemplate;
    private string finalIndexText;

    // Unity calls Reset when this component is added in the Inspector.
    // References are then saved in the scene; no runtime name searches are needed.
    private void Reset()
    {
        header = FindGraphics("TopRule", "AuthorityHeader");
        title = FindText("ReportTitle");
        completionRow = FindGraphics("CompletionTimeLabel", "CompletionTimeValue", "CompletionDivider");
        fatalRow = FindGraphics("FatalErrorsLabel", "FatalErrorsValue", "FatalErrorsDivider");
        systemRow = FindGraphics("SystemFaultLabel", "SystemFaultValue");
        errorIndex = FindText("ErrorIndex");
        warning = FindGraphics("Assessment", "WarningRule");
        disclaimer = FindText("ReportDisclaimer");
        continueButton = transform.Find("ContinueButton")?.GetComponent<Button>();
    }

    private Graphic[] FindGraphics(params string[] names)
    {
        var result = new Graphic[names.Length];
        for (int i = 0; i < names.Length; i++)
            result[i] = transform.Find(names[i])?.GetComponent<Graphic>();
        return result;
    }

    private TMP_Text FindText(string objectName)
    {
        return transform.Find(objectName)?.GetComponent<TMP_Text>();
    }

    private void Awake()
    {
        if (!title || !errorIndex || !disclaimer || !continueButton ||
            !HasReferences(header) || !HasReferences(completionRow) ||
            !HasReferences(fatalRow) || !HasReferences(systemRow) || !HasReferences(warning))
        {
            Debug.LogError("Report reveal has missing Inspector references. Check the Canvas component.", this);
            enabled = false;
            return;
        }

        // Preserve the approved rich-text sizes, colours, spacing and target line.
        // Only the number immediately before the smaller percent sign is replaced.
        var number = Regex.Match(errorIndex.text, @"\d+(?:\.\d+)?(?=<size=88>%)");
        if (!number.Success)
        {
            Debug.LogError("Report reveal could not find the percentage in ErrorIndex's rich text.", this);
            enabled = false;
            return;
        }
        indexTemplate = errorIndex.text.Remove(number.Index, number.Length).Insert(number.Index, "{INDEX}");
        finalIndexText = IndexText(finalHumanErrorIndex);

        sections = new[]
        {
            MakeGroups(header), MakeGroups(title), MakeGroups(completionRow),
            MakeGroups(fatalRow), MakeGroups(systemRow), MakeGroups(errorIndex),
            MakeGroups(warning), MakeGroups(disclaimer, continueButton.image)
        };
        foreach (var section in sections) SetAlpha(section, 0);
        continueButton.interactable = false;
        SetButtonRaycasts(false);

        // A runtime-only copy borrows the existing IBM Plex label style.
        // It never changes the saved hierarchy or the final report layout.
        processingLabel = Instantiate(completionRow[0].GetComponent<TMP_Text>(), transform);
        processingLabel.name = "ProcessingLabel (runtime)";
        processingLabel.rectTransform.anchorMin = errorIndex.rectTransform.anchorMin;
        processingLabel.rectTransform.anchorMax = errorIndex.rectTransform.anchorMax;
        processingLabel.rectTransform.pivot = errorIndex.rectTransform.pivot;
        processingLabel.rectTransform.anchoredPosition = errorIndex.rectTransform.anchoredPosition;
        processingLabel.rectTransform.sizeDelta = errorIndex.rectTransform.sizeDelta;
        processingLabel.text = "<align=left><size=20>CALCULATING HUMAN ERROR INDEX...</size></align>";
        processingLabel.raycastTarget = false;
        processingLabel.GetComponent<CanvasGroup>().alpha = 1;
        processingLabel.gameObject.SetActive(false);
    }

    private static bool HasReferences(Graphic[] graphics)
    {
        if (graphics == null || graphics.Length == 0) return false;
        foreach (var graphic in graphics) if (!graphic) return false;
        return true;
    }

    private static CanvasGroup[] MakeGroups(params Graphic[] graphics)
    {
        var groups = new CanvasGroup[graphics.Length];
        for (int i = 0; i < graphics.Length; i++)
        {
            groups[i] = graphics[i].GetComponent<CanvasGroup>();
            if (!groups[i]) groups[i] = graphics[i].gameObject.AddComponent<CanvasGroup>();
        }
        return groups;
    }

    private void Start()
    {
        StartCoroutine(RevealReport());
    }

    private IEnumerator RevealReport()
    {
        // Keep the verdict after calculation even if timings are adjusted out of order.
        float countStart = Mathf.Max(processingTime, countTime);
        float countEnd = countStart + Mathf.Max(0.01f, countDuration);
        float verdictStart = Mathf.Max(warningTime, countEnd);
        float footerStart = Mathf.Max(footerTime, verdictStart + fadeDuration);
        float finishTime = footerStart + Mathf.Max(0.01f, footerFadeDuration);
        float elapsed = 0;
        while (elapsed < finishTime)
        {
            Fade(sections[0], elapsed, headerTime, fadeDuration);
            Fade(sections[1], elapsed, titleTime, fadeDuration);
            Fade(sections[2], elapsed, completionTime, fadeDuration);
            Fade(sections[3], elapsed, fatalTime, fadeDuration);
            Fade(sections[4], elapsed, systemTime, fadeDuration);
            processingLabel.gameObject.SetActive(elapsed >= processingTime && elapsed < countStart);

            if (elapsed >= countStart)
            {
                SetAlpha(sections[5], 1);
                float progress = Mathf.Clamp01((elapsed - countStart) / Mathf.Max(0.01f, countDuration));
                string text = IndexText(Mathf.SmoothStep(0, finalHumanErrorIndex, progress));
                // Transparent target retains exactly the same line spacing while counting.
                errorIndex.text = progress < 1 ? text.Replace("<color=#ECEEE6>", "<color=#ECEEE600>") : finalIndexText;
            }

            Fade(sections[6], elapsed, verdictStart, fadeDuration);
            Fade(sections[7], elapsed, footerStart, footerFadeDuration);
            yield return null;
            elapsed += Time.unscaledDeltaTime;
        }
        yield return FinishReport();
    }

    private string IndexText(float value)
    {
        return indexTemplate.Replace("{INDEX}", value.ToString("0.0", CultureInfo.InvariantCulture));
    }

    private static void Fade(CanvasGroup[] groups, float elapsed, float start, float duration)
    {
        SetAlpha(groups, Mathf.Clamp01((elapsed - start) / Mathf.Max(0.01f, duration)));
    }

    private static void SetAlpha(CanvasGroup[] groups, float alpha)
    {
        foreach (var group in groups) group.alpha = alpha;
    }

    private IEnumerator FinishReport()
    {
        processingLabel.gameObject.SetActive(false);
        errorIndex.text = finalIndexText;
        foreach (var section in sections) SetAlpha(section, 1);
        if (EventSystem.current) EventSystem.current.SetSelectedGameObject(null);

        // Wait for release AND a neutral frame so a held submit/click cannot acknowledge.
        do
        {
            while (InputHeld()) yield return null;
            yield return null;
        } while (InputHeld());

        SetButtonRaycasts(true);
        continueButton.interactable = true;
        // No further writes to interactable: Continue now owns its disabled state.
    }

    private static bool InputHeld()
    {
        return (Keyboard.current != null && Keyboard.current.anyKey.isPressed) ||
               (Mouse.current != null && Mouse.current.leftButton.isPressed) ||
               (Gamepad.current != null && Gamepad.current.buttonSouth.isPressed);
    }

    private void SetButtonRaycasts(bool enabledRaycasts)
    {
        continueButton.GetComponent<CanvasGroup>().blocksRaycasts = enabledRaycasts;
    }

    private void OnDisable()
    {
        StopAllCoroutines();
    }
}
