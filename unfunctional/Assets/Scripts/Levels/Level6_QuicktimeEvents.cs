using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// LEVEL 6: Quicktime event level. The player cannot move freely -- every
/// successful QTE takes one step forward down the hallway toward the door.
/// Once they reach the door, a second QTE burst is required to open it.
/// Mislabeled keys and resets keep it frustrating/funny.
///
/// Builds its own HUD at runtime. Attach to a root GameObject in the LEVEL6 scene.
/// </summary>
public class Level6_QuicktimeEvents : LevelManager
{
    [Header("Level 6 - Quicktime Events")]
    public Canvas qteCanvas;

    [Header("Door")]
    public DoorController doorController;

    [Header("QTE Settings")]
    public float timePerEvent = 2.0f;       // Seconds to respond
    public float timeBetweenEvents = 0.8f;  // Pause between events
    public int walkEvents = 20;             // QTEs needed to reach the door
    public int doorEvents = 8;              // QTEs needed to open the door
    public bool enableMislabeling = true;   // Show wrong key on display

    [Header("Step Movement")]
    [Tooltip("How long the smooth step animation takes (seconds).")]
    public float stepDuration = 0.35f;

    [Header("Fail Penalty")]
    public bool resetOnFail = true;         // Go back to start on failure
    public int failsBeforeReset = 3;        // Failures allowed before reset

    [System.Serializable]
    public class QTEEvent
    {
        public KeyCode correctKey;          // The actual key to press
        public KeyCode displayKey;          // The key shown on screen (may differ)
    }

    // =========================================================================
    // Phases
    // =========================================================================

    private enum Phase { Intro, WalkingToDoor, ReachedDoor, OpeningDoor, Complete }
    private Phase currentPhase = Phase.Intro;

    // =========================================================================
    // Player movement control
    // =========================================================================

    private PlayerController playerController;
    private CharacterController characterController;
    private float savedWalkSpeed;
    private float savedRunSpeed;
    private float savedJumpForce;

    // Step geometry
    private Vector3 startPosition;
    private Vector3 doorPosition;
    private Vector3 stepDirection;  // Normalised direction toward door (flat)
    private float totalDistance;
    private float stepDistance;     // Distance per successful walk QTE
    private int stepsCompleted = 0;
    private bool isStepping = false;

    // =========================================================================
    // Runtime-built UI references
    // =========================================================================

    private Text promptText;
    private Text timerText;
    private Image progressBarFill;
    private Text feedbackText;
    private Text counterText;
    private Image timerBarFill;
    private Text phaseText;       // Shows "WALK TO DOOR" or "OPEN THE DOOR"

    // =========================================================================
    // QTE state
    // =========================================================================

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

    // =========================================================================
    // Lifecycle
    // =========================================================================

    protected override void Start()
    {
        base.Start();
        levelDisplayName = "Quicktime Gauntlet";
        levelDescription = "Press the buttons. ALL of them. Fast.";

        // Grab the spawned player and freeze movement (look still works)
        StartCoroutine(SetupAfterFrame());
    }

    /// <summary>
    /// Poll until the player is spawned by GameManager, then freeze movement.
    /// Also auto-finds the DoorController if not wired via the inspector.
    /// </summary>
    private IEnumerator SetupAfterFrame()
    {
        // Poll for the player — GameManager spawns it in a coroutine so
        // it may not exist until a few frames after the scene loads.
        float timeout = 5f;
        float elapsed = 0f;

        while (elapsed < timeout)
        {
            playerController = FindObjectOfType<PlayerController>();
            if (playerController != null) break;
            elapsed += Time.deltaTime;
            yield return null;
        }

        if (playerController != null)
        {
            characterController = playerController.GetComponent<CharacterController>();
            savedWalkSpeed = playerController.walkSpeed;
            savedRunSpeed = playerController.runSpeed;
            savedJumpForce = playerController.jumpForce;
            FreezePlayerMovement();

            startPosition = playerController.transform.position;
            Debug.Log($"[Level6] Player found and frozen. walkSpeed was {savedWalkSpeed}, now 0.");
        }
        else
        {
            Debug.LogWarning("[Level6] PlayerController not found after timeout. Steps will be skipped.");
            startPosition = playerSpawnPoint != null ? playerSpawnPoint.position : Vector3.zero;
        }

        // Auto-find DoorController if not wired via inspector
        if (doorController == null)
        {
            doorController = FindObjectOfType<DoorController>();
        }

        // Door target position
        if (doorController != null)
        {
            doorPosition = doorController.transform.position;
            doorPosition.y = startPosition.y; // keep same height
        }
        else
        {
            // Fallback: step direction from spawn rotation
            Vector3 forward = (playerSpawnPoint != null)
                ? playerSpawnPoint.forward
                : Vector3.right;
            doorPosition = startPosition + forward * 36f;
            Debug.LogWarning("[Level6] DoorController not found, using fallback door position.");
        }

        // Compute step geometry
        Vector3 towardDoor = doorPosition - startPosition;
        towardDoor.y = 0;
        totalDistance = towardDoor.magnitude;
        stepDirection = towardDoor.normalized;

        // Leave a small gap so the player ends up a couple metres from the door
        float effectiveDistance = Mathf.Max(totalDistance - 2.5f, totalDistance * 0.9f);
        stepDistance = effectiveDistance / walkEvents;

        BuildHUD();
        GenerateSequence(walkEvents);
        StartCoroutine(BeginSequenceAfterDelay(2f));
    }

    protected override void OnDestroy()
    {
        RestorePlayerMovement();
        base.OnDestroy();
    }

    private void FreezePlayerMovement()
    {
        if (playerController != null)
        {
            playerController.walkSpeed = 0f;
            playerController.runSpeed = 0f;
            playerController.jumpForce = 0f;
        }
    }

    private void RestorePlayerMovement()
    {
        if (playerController != null)
        {
            playerController.walkSpeed = savedWalkSpeed;
            playerController.runSpeed = savedRunSpeed;
            playerController.jumpForce = savedJumpForce;
        }
    }

    // =========================================================================
    // Update
    // =========================================================================

    private void Update()
    {
        if (levelComplete || !sequenceActive || isStepping) return;

        if (waitingForInput)
        {
            currentTimer -= Time.deltaTime;

            // Timer display
            if (timerText != null)
                timerText.text = currentTimer.ToString("F1") + "s";

            // Timer bar fill
            if (timerBarFill != null)
                timerBarFill.fillAmount = currentTimer / timePerEvent;

            // Progress bar (context-dependent)
            UpdateProgressBar();

            // Counter
            if (counterText != null)
            {
                int total = currentPhase == Phase.OpeningDoor ? doorEvents : walkEvents;
                counterText.text = $"{currentEventIndex}/{total}";
            }

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

    private void UpdateProgressBar()
    {
        if (progressBarFill == null) return;

        if (currentPhase == Phase.WalkingToDoor)
        {
            progressBarFill.fillAmount = (float)stepsCompleted / walkEvents;
            progressBarFill.color = new Color(0.3f, 0.8f, 0.3f);
        }
        else if (currentPhase == Phase.OpeningDoor)
        {
            progressBarFill.fillAmount = (float)currentEventIndex / doorEvents;
            progressBarFill.color = new Color(0.8f, 0.6f, 0.2f);
        }
    }

    // =========================================================================
    // HUD (built at runtime)
    // =========================================================================

    private void BuildHUD()
    {
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

        // -- Phase text (top center: "WALK TO DOOR" / "OPEN THE DOOR") --
        phaseText = CreateText(canvasObj.transform, "PhaseText", "WALK TO THE DOOR",
            new Vector2(0.2f, 0.82f), new Vector2(0.8f, 0.9f),
            32, new Color(0.9f, 0.9f, 0.5f), TextAnchor.MiddleCenter);
        phaseText.fontStyle = FontStyle.Bold;

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
        counterText = CreateText(canvasObj.transform, "CounterText", "0/" + walkEvents,
            new Vector2(0.85f, 0.92f), new Vector2(0.98f, 0.97f),
            20, new Color(0.7f, 0.7f, 0.8f), TextAnchor.MiddleRight);

        // -- Feedback text (below timer, shows SUCCESS/FAILED) --
        feedbackText = CreateText(canvasObj.transform, "FeedbackText", "",
            new Vector2(0.2f, 0.2f), new Vector2(0.8f, 0.3f),
            36, Color.green, TextAnchor.MiddleCenter);
        feedbackText.fontStyle = FontStyle.Bold;

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
    // QTE Sequence generation
    // =========================================================================

    private void GenerateSequence(int count)
    {
        eventSequence.Clear();

        for (int i = 0; i < count; i++)
        {
            QTEEvent evt = new QTEEvent();
            evt.correctKey = possibleKeys[Random.Range(0, possibleKeys.Length)];

            if (enableMislabeling && Random.value > 0.6f)
            {
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

        currentPhase = Phase.WalkingToDoor;
        sequenceActive = true;
        currentEventIndex = 0;
        failCount = 0;
        stepsCompleted = 0;

        if (phaseText != null)
            phaseText.text = "WALK TO THE DOOR";

        ShowCurrentEvent();
    }

    // =========================================================================
    // Event display & input
    // =========================================================================

    private void ShowCurrentEvent()
    {
        int totalForPhase = (currentPhase == Phase.OpeningDoor) ? doorEvents : walkEvents;

        if (currentEventIndex >= eventSequence.Count)
        {
            // Phase completed
            if (currentPhase == Phase.WalkingToDoor)
            {
                OnReachedDoor();
            }
            else if (currentPhase == Phase.OpeningDoor)
            {
                OnDoorSequenceComplete();
            }
            return;
        }

        QTEEvent evt = eventSequence[currentEventIndex];
        currentTimer = timePerEvent;
        waitingForInput = true;

        if (promptText != null)
        {
            string keyName = GetKeyDisplayName(evt.displayKey);
            promptText.text = $"Press [{keyName}]!";
            promptText.color = Color.white;
        }

        if (feedbackText != null)
            feedbackText.text = "";
    }

    private string GetKeyDisplayName(KeyCode key)
    {
        switch (key)
        {
            case KeyCode.Space: return "SPACEBAR";
            case KeyCode.LeftShift: return "SHIFT";
            default: return key.ToString();
        }
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

    // =========================================================================
    // Success / failure
    // =========================================================================

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

        if (currentPhase == Phase.WalkingToDoor)
        {
            // Take a step forward, then show next event
            stepsCompleted++;
            StartCoroutine(StepForwardThenContinue());
        }
        else
        {
            // Door phase: just advance
            StartCoroutine(NextEventAfterDelay(timeBetweenEvents));
        }
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
            StartCoroutine(ResetSequence());
        }
        else
        {
            // Skip this event (no step forward) and move to next
            currentEventIndex++;
            StartCoroutine(NextEventAfterDelay(timeBetweenEvents * 1.5f));
        }
    }

    // =========================================================================
    // Step movement (Phase: WalkingToDoor)
    // =========================================================================

    /// <summary>
    /// Smoothly slides the player one step forward, then shows the next QTE.
    /// </summary>
    private IEnumerator StepForwardThenContinue()
    {
        isStepping = true;

        if (characterController != null)
        {
            Vector3 stepDelta = stepDirection * stepDistance;
            float elapsed = 0f;
            float moved = 0f;

            while (elapsed < stepDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / stepDuration);
                // Ease out for a satisfying step feel
                float easedT = 1f - (1f - t) * (1f - t);
                float targetMoved = stepDistance * easedT;
                float frameDelta = targetMoved - moved;
                moved = targetMoved;

                characterController.Move(stepDirection * frameDelta);
                yield return null;
            }

            // Ensure exact step
            float remaining = stepDistance - moved;
            if (remaining > 0.001f)
                characterController.Move(stepDirection * remaining);
        }

        isStepping = false;
        yield return new WaitForSeconds(timeBetweenEvents * 0.5f);
        ShowCurrentEvent();
    }

    // =========================================================================
    // Phase transitions
    // =========================================================================

    /// <summary>
    /// Called when all walk QTEs are done. Transition to the door-opening phase.
    /// </summary>
    private void OnReachedDoor()
    {
        currentPhase = Phase.ReachedDoor;
        sequenceActive = false;
        waitingForInput = false;

        if (phaseText != null)
            phaseText.text = "YOU REACHED THE DOOR!";
        if (promptText != null)
        {
            promptText.text = "Now open it...";
            promptText.color = new Color(0.9f, 0.8f, 0.3f);
        }
        if (feedbackText != null)
            feedbackText.text = "";

        StartCoroutine(BeginDoorPhaseAfterDelay(2f));
    }

    private IEnumerator BeginDoorPhaseAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);

        currentPhase = Phase.OpeningDoor;
        sequenceActive = true;
        currentEventIndex = 0;
        failCount = 0;

        GenerateSequence(doorEvents);

        if (phaseText != null)
        {
            phaseText.text = "OPEN THE DOOR!";
            phaseText.color = new Color(0.9f, 0.5f, 0.2f);
        }

        if (progressBarFill != null)
            progressBarFill.fillAmount = 0f;

        ShowCurrentEvent();
    }

    private void OnDoorSequenceComplete()
    {
        currentPhase = Phase.Complete;
        sequenceActive = false;
        waitingForInput = false;

        if (promptText != null)
        {
            promptText.text = "DOOR UNLOCKED!";
            promptText.color = Color.green;
        }
        if (phaseText != null)
            phaseText.text = "WELL DONE!";

        if (progressBarFill != null)
            progressBarFill.fillAmount = 1f;

        // Open the door and complete the level
        if (doorController != null)
        {
            doorController.OpenDoor();
        }

        Debug.Log("[Level6] QTE gauntlet complete! Door opened.");
        RestorePlayerMovement();
        StartCoroutine(CompleteLevelAfterDelay(2f));
    }

    // =========================================================================
    // Reset (on too many failures)
    // =========================================================================

    private IEnumerator ResetSequence()
    {
        sequenceActive = false;
        waitingForInput = false;

        if (currentPhase == Phase.WalkingToDoor)
        {
            if (promptText != null)
                promptText.text = "SEQUENCE FAILED!\nBack to the start...";
            if (feedbackText != null)
            {
                feedbackText.text = $"({failCount} failures - reset!)";
                feedbackText.color = new Color(1f, 0.5f, 0.3f);
            }

            yield return new WaitForSeconds(2f);

            // Teleport player back to start
            if (playerController != null)
            {
                playerController.TeleportTo(startPosition);
            }
            stepsCompleted = 0;
        }
        else if (currentPhase == Phase.OpeningDoor)
        {
            if (promptText != null)
                promptText.text = "NOPE!\nTry again...";
            if (feedbackText != null)
            {
                feedbackText.text = $"({failCount} failures on the door)";
                feedbackText.color = new Color(1f, 0.5f, 0.3f);
            }

            yield return new WaitForSeconds(2f);
        }

        // Regenerate and restart the current phase
        currentEventIndex = 0;
        failCount = 0;

        int count = (currentPhase == Phase.OpeningDoor) ? doorEvents : walkEvents;
        GenerateSequence(count);

        if (progressBarFill != null)
            progressBarFill.fillAmount = 0f;

        sequenceActive = true;
        ShowCurrentEvent();
    }

    // =========================================================================
    // Helpers
    // =========================================================================

    private IEnumerator NextEventAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        ShowCurrentEvent();
    }

    private IEnumerator CompleteLevelAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        CompleteLevel();
    }
}
