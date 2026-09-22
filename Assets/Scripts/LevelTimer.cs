using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LevelTimer : MonoBehaviour {

    [Header ("Display")]
    [Tooltip ("Leave both empty and the timer builds its own label in the top centre of the screen.")]
    [SerializeField] private TMP_Text tmpLabel;
    [SerializeField] private Text uiLabel;
    [SerializeField] private bool showHundredths = false;

    [Header ("Behaviour")]
    [SerializeField] private bool startOnLoad = true;

    public float Elapsed => _elapsed;
    public bool IsRunning => _running;

    private float _elapsed;
    private bool _running;

    void Start () {
        if (tmpLabel == null && uiLabel == null)
            tmpLabel = BuildLabel ();

        _running = startOnLoad;
        Draw ();
    }

    void Update () {
        if (!_running)
            return;

        _elapsed += Time.deltaTime;
        Draw ();
    }

    // Public so buttons, triggers and other scripts can drive the clock.
    public void StartTimer () => _running = true;

    public void StopTimer () => _running = false;

    public void ResetTimer () {
        _elapsed = 0f;
        Draw ();
    }

    public string FormattedTime => Format (_elapsed);

    private string Format (float seconds) {
        int minutes = (int) (seconds / 60f);
        float rest = seconds - minutes * 60f;

        return showHundredths
            ? $"{minutes}:{rest:00.00}"
            : $"{minutes}:{(int) rest:00}";
    }

    private void Draw () {
        string text = Format (_elapsed);

        if (tmpLabel != null)
            tmpLabel.text = text;
        if (uiLabel != null)
            uiLabel.text = text;
    }

    private TMP_Text BuildLabel () {
        GameObject canvasObject = new GameObject ("Timer Canvas", typeof (Canvas), typeof (CanvasScaler));
        canvasObject.transform.SetParent (transform, false);

        Canvas canvas = canvasObject.GetComponent<Canvas> ();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        // Sit above anything else the scene happens to draw.
        canvas.sortingOrder = 100;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler> ();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2 (1920f, 1080f);

        GameObject labelObject = new GameObject ("Timer Label", typeof (RectTransform), typeof (TextMeshProUGUI));
        labelObject.transform.SetParent (canvasObject.transform, false);

        RectTransform rect = labelObject.GetComponent<RectTransform> ();
        rect.anchorMin = rect.anchorMax = new Vector2 (0.5f, 1f);
        rect.pivot = new Vector2 (0.5f, 1f);
        rect.anchoredPosition = new Vector2 (0f, -24f);
        rect.sizeDelta = new Vector2 (400f, 80f);

        TextMeshProUGUI text = labelObject.GetComponent<TextMeshProUGUI> ();
        text.alignment = TextAlignmentOptions.Top;
        text.fontSize = 56f;
        text.color = Color.white;

        return text;
    }
}
