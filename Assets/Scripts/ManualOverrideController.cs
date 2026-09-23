using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

// One attempt per opening. Gameplay and report integration belong to the caller.
public class ManualOverrideController : MonoBehaviour
{
    [Header("UI references")]
    [SerializeField] private GameObject overlay;
    [SerializeField] private RectTransform marker;
    [SerializeField] private RectTransform successZone;
    [SerializeField] private TMP_Text titleLabel;
    [SerializeField] private TMP_Text instructionLabel;
    [SerializeField] private TMP_Text statusLabel;

    [Header("Timing")]
    [SerializeField] private Key actionKey = Key.E;
    [SerializeField, Min(0f)] private float preparationDuration = 0.35f;
    [SerializeField, Min(0.1f)] private float passDuration = 1.25f;
    [SerializeField, Range(0f, 1f)] private float targetCentre = 0.65f;
    [SerializeField, Range(0.01f, 1f)] private float targetWidth = 0.125f;
    [SerializeField, Min(0.1f)] private float resultDuration = 1.5f;
    [SerializeField, Min(0.1f)] private float assistanceMessageDuration = 2f;

    [Header("Wording")]
    [SerializeField] private string title = "MANUAL OVERRIDE";
    [SerializeField] private string successText = "OVERRIDE ACCEPTED";
    [SerializeField, TextArea] private string failureText =
        "MANUAL OVERRIDE FAILED\nHUMAN ERROR RECORDED\nRETRY REQUIRED";

    // Fired once, after the result has been displayed and the overlay closes.
    public event Action<bool> Completed;
    // A genuine timing result, before its display delay. Cancellation never fires this.
    public event Action<bool> Resolved;
    public bool HasResult { get; private set; }
    public bool LastSucceeded { get; private set; }
    public bool IsRunning => phase != Phase.Idle;

    private enum Phase { Idle, WaitingForRelease, Preparing, Moving, Result, Assistance }
    private Phase phase;
    private float elapsed;
    private float position;
    private float zoneStart;
    private float zoneEnd;
    private Color normalStatusColour;
    private bool showAssistance;
    private readonly Color failureColour = new Color32(255, 93, 82, 255);

    private void Awake()
    {
        if (overlay == null || marker == null || successZone == null ||
            titleLabel == null || instructionLabel == null || statusLabel == null)
        {
            Debug.LogError("Manual Override: assign all UI references.", this);
            enabled = false;
            return;
        }

        normalStatusColour = statusLabel.color;
        overlay.SetActive(false);
    }

    public void StartCheck()
    {
        if (!isActiveAndEnabled || IsRunning)
            return;

        HasResult = false;
        LastSucceeded = false;
        showAssistance = false;
        elapsed = 0f;
        SetMarkerPosition(0f);

        float width = Mathf.Clamp(targetWidth, 0.01f, 1f);
        float centre = Mathf.Clamp(targetCentre, width / 2f, 1f - width / 2f);
        zoneStart = centre - width / 2f;
        zoneEnd = centre + width / 2f;
        successZone.anchorMin = new Vector2(zoneStart, 0f);
        successZone.anchorMax = new Vector2(zoneEnd, 1f);
        successZone.offsetMin = Vector2.zero;
        successZone.offsetMax = Vector2.zero;

        titleLabel.text = title;
        instructionLabel.text = $"PRESS [{actionKey}] WHEN THE MARKER IS INSIDE THE BAND";
        statusLabel.color = normalStatusColour;
        statusLabel.text = $"RELEASE [{actionKey}] TO PREPARE";
        phase = Phase.WaitingForRelease;
        overlay.SetActive(true);
        if (EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(null);
    }

    private void Update()
    {
        if (phase == Phase.Idle)
            return;

        var keyboard = Keyboard.current;
        bool held = keyboard != null && keyboard[actionKey].isPressed;
        bool pressed = keyboard != null && keyboard[actionKey].wasPressedThisFrame;

        switch (phase)
        {
            case Phase.WaitingForRelease:
                // Also exclude a press/release that both arrived in this frame.
                if (keyboard != null && !held && !pressed)
                {
                    elapsed = 0f;
                    statusLabel.text = "PREPARE";
                    phase = Phase.Preparing;
                }
                break;

            case Phase.Preparing:
                if (held || pressed)
                {
                    elapsed = 0f;
                    statusLabel.text = $"RELEASE [{actionKey}] TO PREPARE";
                    phase = Phase.WaitingForRelease;
                    break;
                }
                elapsed += Time.unscaledDeltaTime;
                if (elapsed >= preparationDuration)
                {
                    elapsed = 0f;
                    statusLabel.text = "AWAITING MANUAL INPUT";
                    phase = Phase.Moving;
                }
                break;

            case Phase.Moving:
                // Judge the position already drawn, before advancing this frame.
                if (pressed)
                {
                    Resolve(position >= zoneStart && position <= zoneEnd);
                    break;
                }
                elapsed += Time.unscaledDeltaTime;
                SetMarkerPosition(Mathf.Clamp01(elapsed / Mathf.Max(0.1f, passDuration)));
                if (position >= 1f)
                    Resolve(false);
                break;

            case Phase.Result:
                elapsed += Time.unscaledDeltaTime;
                if (elapsed >= Mathf.Max(0.1f, resultDuration))
                {
                    if (showAssistance)
                    {
                        elapsed = 0f;
                        phase = Phase.Assistance;
                        statusLabel.text = "CORRECTIVE ASSISTANCE ENABLED\nOPERATOR PERFORMANCE BELOW REQUIRED STANDARD";
                    }
                    else CloseResult();
                }
                break;

            case Phase.Assistance:
                elapsed += Time.unscaledDeltaTime;
                if (elapsed >= assistanceMessageDuration) CloseResult();
                break;
        }
    }

    public void ConfigureDifficulty(float seconds, float width)
    {
        if (IsRunning) return;
        passDuration = Mathf.Max(0.1f, seconds);
        targetWidth = Mathf.Clamp(width, 0.01f, 1f);
    }

    public void ShowCorrectiveAssistance() => showAssistance = true;

    private void CloseResult()
    {
        phase = Phase.Idle;
        overlay.SetActive(false);
        Completed?.Invoke(LastSucceeded);
    }

    private void SetMarkerPosition(float value)
    {
        position = value;
        marker.anchorMin = new Vector2(position, 0.5f);
        marker.anchorMax = marker.anchorMin;
        marker.anchoredPosition = Vector2.zero;
    }

    private void Resolve(bool success)
    {
        HasResult = true;
        LastSucceeded = success;
        statusLabel.text = success ? successText : failureText;
        statusLabel.color = success ? normalStatusColour : failureColour;
        elapsed = 0f;
        phase = Phase.Result;
        Resolved?.Invoke(success);
    }

    private void OnDisable()
    {
        // Cancelling an interrupted check does not report a fabricated failure.
        phase = Phase.Idle;
        if (overlay != null)
            overlay.SetActive(false);
    }
}
