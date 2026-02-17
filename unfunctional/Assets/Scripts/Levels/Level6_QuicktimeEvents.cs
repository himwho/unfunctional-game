using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// LEVEL 6: Quicktime event level. Forces the player to do timed key-based 
/// quicktime events in a long sequence. Display key may differ from actual required key.
/// 
/// Builds its own HUD at runtime. Attach to a root GameObject in the LEVEL6 scene.
/// </summary>
public class Level6_QuicktimeEvents : LevelManager
{
    [Header("Level 6 - Quicktime Events")]
    public Canvas qteCanvas;

    [Header("QTE Settings")]
    public float timePerEvent = 2.0f;       // Seconds to respond
    public float timeBetweenEvents = 0.8f;  // Pause between events
    public int totalEvents = 20;            // How many QTEs in the sequence
    public bool enableMislabeling = true;   // Show wrong key on display

    [Header("Fail Penalty")]
    public bool resetOnFail = true;         // Go back to start on failure
    public int failsBeforeReset = 3;        // Or allow some failures

    [System.Serializable]
    public class QTEEvent
    {
        public KeyCode correctKey;          // The actual key to press
        public KeyCode displayKey;          // The key shown on screen (may differ)
    }

    // Runtime-built UI references
    private Text promptText;
    private Text timerText;
    private Image progressBarFill;
    private Text feedbackText;
    private Text counterText;
    private Image timerBarFill;

    private List<QTEEvent> eventSequence = new List<QTEEvent>();
    private int currentEventIndex = 0;
    private float currentTimer = 0f;
    private bool waitingForInput = false;
    private int failCount = 0;
    private bool sequenceActive = false;

    // All possible keys for QTEs
    private KeyCode[] possibleKeys = new KeyCode[]
    {
        KeyCode.Q, KeyCode.W, KeyCode.E, KeyCode.R, KeyCode.T,
        KeyCode.A, KeyCode.S, KeyCode.D, KeyCode.F, KeyCode.G,
        KeyCode.Z, KeyCode.X, KeyCode.C, KeyCode.V, KeyCode.B,
        KeyCode.Space, KeyCode.LeftShift
    };

    protected override void Start()
    {
        base.Start();
        levelDisplayName = "Quicktime Gauntlet";
        levelDescription = "Press the buttons. ALL of them. Fast.";

        BuildHUD();
        GenerateSequence();
        StartCoroutine(BeginSequenceAfterDelay(2f));
    }

    private void Update()
    {
        if (levelComplete || !sequenceActive) return;

        if (waitingForInput)
        {
            currentTimer -= Time.deltaTime;

            // Update timer display
            if (timerText != null)
                timerText.text = currentTimer.ToString("F1") + "s";

            // Update timer bar
            if (timerBarFill != null)
                timerBarFill.fillAmount = currentTimer / timePerEvent;

            // Update progress bar
            if (progressBarFill != null)
                progressBarFill.fillAmount = (float)currentEventIndex / totalEvents;

            // Update counter
            if (counterText != null)
                counterText.text = $"{currentEventIndex}/{totalEvents}";

            // Time ran out
            if (currentTimer <= 0f)
            {
                OnEventFailed("TOO SLOW!");
                return;
            }

            // Check for any key press
            foreach (KeyCode key in possibleKeys)
            {
                if (Input.GetKeyDown(key))
                {
                    CheckInput(key);
                    return;
                }
            }
        }
    }

    // =========================================================================
    // HUD (built at runtime)
    // =========================================================================

    private void BuildHUD()
    {
        // Use existing canvas from scene setup or create one
        Canvas canvas = qteCanvas;
        GameObject canvasObj;

        if (canvas == null)
        {
            canvasObj = new GameObject("QTECanvas");
            canvasObj.transform.SetParent(transform);
            canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 20;
            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            canvasObj.AddComponent<GraphicRaycaster>();
        }
        else
        {
            canvasObj = canvas.gameObject;
        }

        // -- Prompt text (big center display: "Press [X]!") --
        promptText = CreateText(canvasObj.transform, "PromptText", "GET READY...",
            new Vector2(0.2f, 0.45f), new Vector2(0.8f, 0.65f),
            64, Color.white, TextAnchor.MiddleCenter);
        promptText.fontStyle = FontStyle.Bold;

        // -- Timer text (below prompt) --
        timerText = CreateText(canvasObj.transform, "TimerText", "",
            new Vector2(0.4f, 0.35f), new Vector2(0.6f, 0.45f),
            28, new Color(0.8f, 0.8f, 0.5f), TextAnchor.MiddleCenter);

        // -- Timer bar background --
        GameObject timerBg = new GameObject("TimerBarBG");
        timerBg.transform.SetParent(canvasObj.transform, false);
        Image timerBgImg = timerBg.AddComponent<Image>();
        timerBgImg.color = new Color(0.15f, 0.15f, 0.15f, 0.8f);
        timerBgImg.raycastTarget = false;
        RectTransform timerBgRect = timerBg.GetComponent<RectTransform>();
        timerBgRect.anchorMin = new Vector2(0.3f, 0.3f);
        timerBgRect.anchorMax = new Vector2(0.7f, 0.33f);
        timerBgRect.offsetMin = Vector2.zero;
        timerBgRect.offsetMax = Vector2.zero;

        // Timer bar fill
        GameObject timerFillObj = new GameObject("TimerBarFill");
        timerFillObj.transform.SetParent(timerBg.transform, false);
        timerBarFill = timerFillObj.AddComponent<Image>();
        timerBarFill.color = new Color(0.9f, 0.7f, 0.2f);
        timerBarFill.type = Image.Type.Filled;
        timerBarFill.fillMethod = Image.FillMethod.Horizontal;
        timerBarFill.fillAmount = 1f;
        timerBarFill.raycastTarget = false;
        RectTransform timerFillRect = timerFillObj.GetComponent<RectTransform>();
        timerFillRect.anchorMin = Vector2.zero;
        timerFillRect.anchorMax = Vector2.one;
        timerFillRect.offsetMin = Vector2.zero;
        timerFillRect.offsetMax = Vector2.zero;

        // -- Progress bar background (top of screen) --
        GameObject progressBg = new GameObject("ProgressBarBG");
        progressBg.transform.SetParent(canvasObj.transform, false);
        Image progressBgImg = progressBg.AddComponent<Image>();
        progressBgImg.color = new Color(0.15f, 0.15f, 0.15f, 0.8f);
        progressBgImg.raycastTarget = false;
        RectTransform progressBgRect = progressBg.GetComponent<RectTransform>();
        progressBgRect.anchorMin = new Vector2(0.1f, 0.93f);
        progressBgRect.anchorMax = new Vector2(0.9f, 0.96f);
        progressBgRect.offsetMin = Vector2.zero;
        progressBgRect.offsetMax = Vector2.zero;

        // Progress bar fill
        GameObject progressFillObj = new GameObject("ProgressBarFill");
        progressFillObj.transform.SetParent(progressBg.transform, false);
        progressBarFill = progressFillObj.AddComponent<Image>();
        progressBarFill.color = new Color(0.3f, 0.8f, 0.3f);
        progressBarFill.type = Image.Type.Filled;
        progressBarFill.fillMethod = Image.FillMethod.Horizontal;
        progressBarFill.fillAmount = 0f;
        progressBarFill.raycastTarget = false;
        RectTransform progressFillRect = progressFillObj.GetComponent<RectTransform>();
        progressFillRect.anchorMin = Vector2.zero;
        progressFillRect.anchorMax = Vector2.one;
        progressFillRect.offsetMin = Vector2.zero;
        progressFillRect.offsetMax = Vector2.zero;

        // -- Counter text (top right) --
        counterText = CreateText(canvasObj.transform, "CounterText", "0/" + totalEvents,
            new Vector2(0.85f, 0.92f), new Vector2(0.98f, 0.97f),
            20, new Color(0.7f, 0.7f, 0.8f), TextAnchor.MiddleRight);

        // -- Feedback text (below timer, shows SUCCESS/FAILED) --
        feedbackText = CreateText(canvasObj.transform, "FeedbackText", "",
            new Vector2(0.2f, 0.2f), new Vector2(0.8f, 0.3f),
            36, Color.green, TextAnchor.MiddleCenter);
        feedbackText.fontStyle = FontStyle.Bold;

        // -- Fail counter (top left) --
        CreateText(canvasObj.transform, "FailLabel", "",
            new Vector2(0.02f, 0.92f), new Vector2(0.15f, 0.97f),
            16, new Color(0.8f, 0.4f, 0.4f), TextAnchor.MiddleLeft);

        // -- Mislabel warning (subtle) --
        CreateText(canvasObj.transform, "MislabelWarning",
            enableMislabeling ? "(Hint: the displayed key might not be the correct one...)" : "",
            new Vector2(0.2f, 0.12f), new Vector2(0.8f, 0.18f),
            14, new Color(0.5f, 0.5f, 0.5f, 0.6f), TextAnchor.MiddleCenter);
    }

    private Text CreateText(Transform parent, string name, string content,
        Vector2 anchorMin, Vector2 anchorMax, int fontSize, Color color, TextAnchor anchor)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent, false);
        RectTransform rect = obj.AddComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        Text txt = obj.AddComponent<Text>();
        txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        txt.fontSize = fontSize;
        txt.alignment = anchor;
        txt.color = color;
        txt.text = content;
        txt.raycastTarget = false;
        return txt;
    }

    // =========================================================================
    // QTE Sequence
    // =========================================================================

    private void GenerateSequence()
    {
        eventSequence.Clear();

        for (int i = 0; i < totalEvents; i++)
        {
            QTEEvent evt = new QTEEvent();
            evt.correctKey = possibleKeys[Random.Range(0, possibleKeys.Length)];

            if (enableMislabeling && Random.value > 0.6f)
            {
                // Show a different key than the correct one (evil)
                do
                {
                    evt.displayKey = possibleKeys[Random.Range(0, possibleKeys.Length)];
                }
                while (evt.displayKey == evt.correctKey);
            }
            else
            {
                evt.displayKey = evt.correctKey;
            }

            eventSequence.Add(evt);
        }
    }

    private IEnumerator BeginSequenceAfterDelay(float delay)
    {
        if (promptText != null)
            promptText.text = "GET READY...";

        yield return new WaitForSeconds(delay);

        sequenceActive = true;
        currentEventIndex = 0;
        failCount = 0;
        ShowCurrentEvent();
    }

    private void ShowCurrentEvent()
    {
        if (currentEventIndex >= eventSequence.Count)
        {
            OnSequenceComplete();
            return;
        }

        QTEEvent evt = eventSequence[currentEventIndex];
        currentTimer = timePerEvent;
        waitingForInput = true;

        if (promptText != null)
        {
            string keyName = evt.displayKey.ToString();
            // Make some keys show confusing names
            if (evt.displayKey == KeyCode.Space)
                keyName = "SPACEBAR";
            else if (evt.displayKey == KeyCode.LeftShift)
                keyName = "SHIFT";

            promptText.text = $"Press [{keyName}]!";
            promptText.color = Color.white;
        }

        if (feedbackText != null)
            feedbackText.text = "";
    }

    private void CheckInput(KeyCode pressedKey)
    {
        QTEEvent evt = eventSequence[currentEventIndex];

        if (pressedKey == evt.correctKey)
        {
            OnEventSuccess();
        }
        else
        {
            OnEventFailed("WRONG KEY!");
        }
    }

    private void OnEventSuccess()
    {
        waitingForInput = false;

        if (feedbackText != null)
        {
            feedbackText.text = "SUCCESS!";
            feedbackText.color = Color.green;
        }

        if (promptText != null)
            promptText.color = Color.green;

        currentEventIndex++;
        StartCoroutine(NextEventAfterDelay(timeBetweenEvents));
    }

    private void OnEventFailed(string reason)
    {
        waitingForInput = false;
        failCount++;

        if (feedbackText != null)
        {
            feedbackText.text = reason;
            feedbackText.color = Color.red;
        }

        if (promptText != null)
            promptText.color = Color.red;

        if (resetOnFail && failCount >= failsBeforeReset)
        {
            // Reset the entire sequence
            StartCoroutine(ResetSequence());
        }
        else
        {
            currentEventIndex++;
            StartCoroutine(NextEventAfterDelay(timeBetweenEvents * 1.5f));
        }
    }

    private IEnumerator NextEventAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        ShowCurrentEvent();
    }

    private IEnumerator ResetSequence()
    {
        if (promptText != null)
            promptText.text = "SEQUENCE FAILED!\nRestarting...";

        if (feedbackText != null)
        {
            feedbackText.text = $"({failCount} failures - sequence reset!)";
            feedbackText.color = new Color(1f, 0.5f, 0.3f);
        }

        yield return new WaitForSeconds(2f);

        currentEventIndex = 0;
        failCount = 0;

        // Regenerate with potentially different mislabeling
        GenerateSequence();

        ShowCurrentEvent();
    }

    private void OnSequenceComplete()
    {
        sequenceActive = false;
        waitingForInput = false;

        if (promptText != null)
        {
            promptText.text = "SEQUENCE COMPLETE!";
            promptText.color = Color.green;
        }

        if (progressBarFill != null)
            progressBarFill.fillAmount = 1f;

        Debug.Log("[Level6] QTE sequence complete!");
        StartCoroutine(CompleteLevelAfterDelay(1.5f));
    }

    private IEnumerator CompleteLevelAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        CompleteLevel();
    }
}
