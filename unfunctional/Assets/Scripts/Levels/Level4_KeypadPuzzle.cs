using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;

/// <summary>
/// LEVEL 4: A room with a locked door and a keypad. Above the keypad are sticky
/// notes with an email address (rodney@please.nyc) and a warning that "this guy
/// always changes the code". The player is expected to alt-tab out of the game,
/// send an email to that address, receive a 9-digit code that expires in 15
/// seconds, then type it into the keypad.
///
/// The backend is a simple Node.js service that auto-replies with a fresh code.
/// For offline/testing, a debug mode generates codes locally.
///
/// This script no longer builds its own keypad UI. Instead it configures and
/// subscribes to the reusable KeypadController on the LEVEL_DOOR prefab.
/// </summary>
public class Level4_KeypadPuzzle : LevelManager
{
    [Header("Level 4 - Keypad Puzzle")]
    [Tooltip("Reference to the door prefab root (for door shake / interaction checks).")]
    public GameObject doorObject;

    [Tooltip("The visual keypad panel on the wall (raycast target for 'Use Keypad').")]
    public GameObject keypadObject;

    [Tooltip("Transform near the sticky notes (for 'Read Notes' interaction range).")]
    public Transform stickyNotePoint;

    [Tooltip("Where the player spawns in this level.")]
    public Transform playerSpawnTransform;

    [Tooltip("DoorController on the LEVEL_DOOR prefab.")]
    public DoorController doorController;

    [Header("Keypad Settings")]
    public float codeValiditySeconds = 15f;
    public float interactRange = 3f;

    [Header("Server")]
    [Tooltip("Base URL of the email support server, e.g. http://your-ec2:3000. " +
             "Endpoints: POST /api/request-code, POST /api/validate. " +
             "Leave empty to use offline/debug mode.")]
    public string codeServerUrl = "";

    [Header("Debug")]
    [Tooltip("When true (or when server URL is empty), generate codes locally.")]
    public bool offlineMode = true;

    // =========================================================================
    // Runtime references
    // =========================================================================

    private KeypadController keypad; // from doorController

    // HUD
    private Canvas hudCanvas;
    private Text interactPromptText;
    private Text narrationText;
    private Image crosshairImage;
    private CanvasGroup narrationCanvasGroup;

    // State
    private string currentValidCode = "";
    private float codeExpiryTime = -1f;
    private bool codeRequested = false;
    private bool doorOpening = false;
    private int failedAttempts = 0;
    private Coroutine narrationFadeCoroutine;

    // Narration lines
    private static readonly string[] stickyNoteNarration = new string[]
    {
        "There's an email address on a sticky note: rodney@please.nyc",
        "Another note says: \"this guy always changes the code\"",
        "Looks like you'll need to email Rodney for the door code.",
    };

    private static readonly string[] failNarration = new string[]
    {
        "Wrong code. Rodney's codes expire fast.",
        "Nope. Did you type it in time?",
        "Still wrong. Maybe email Rodney again?",
        "The code changes every time. You need a fresh one.",
        "This is the point where you alt-tab and send an email.",
    };

    // =========================================================================
    // Lifecycle
    // =========================================================================

    protected override void Start()
    {
        wantsCursorLocked = true;
        needsPlayer = true;
        base.Start();
        levelDisplayName = "The Keypad";
        levelDescription = "A door. A keypad. An email address.";

        // Get the KeypadController from the door prefab
        if (doorController != null)
            keypad = doorController.keypadController;

        if (keypad == null)
            keypad = FindAnyObjectByType<KeypadController>();

        if (keypad != null)
        {
            // Configure the keypad for this level
            keypad.codeLength = 9;
            keypad.keypadTitle = "DOOR ACCESS KEYPAD";
            keypad.hintText = "Sticky Note: \"rodney@please.nyc\"\n\"this guy always changes the code\"";
            keypad.showRequestCodeButton = true;
            keypad.requestCodeLabel = "Email Rodney for Code";

            // Subscribe to events
            keypad.OnCodeSubmitted += HandleCodeSubmitted;
            keypad.OnCodeRequested += HandleCodeRequested;
        }
        else
        {
            Debug.LogWarning("[Level4] No KeypadController found! Add one to the LEVEL_DOOR prefab.");
        }

        CreateHUD();
        ShowNarration("Another room. This time, there's a keypad.", 4f);
    }

    protected override void OnDestroy()
    {
        // Unsubscribe
        if (keypad != null)
        {
            keypad.OnCodeSubmitted -= HandleCodeSubmitted;
            keypad.OnCodeRequested -= HandleCodeRequested;
        }
        base.OnDestroy();
    }

    private void Update()
    {
        if (levelComplete || doorOpening) return;

        if (crosshairImage != null)
            crosshairImage.enabled = keypad != null && keypad.IsOpen;

        UpdateInteractPrompt();
        CheckInteraction();
        UpdateKeypadTimer();
    }

    // =========================================================================
    // Interaction (raycasting in 3D world)
    // =========================================================================

    private void CheckInteraction()
    {
        if (keypad != null && keypad.IsOpen) return;
        if (InputManager.Instance == null || !InputManager.Instance.InteractPressed) return;

        Camera cam = Camera.main;
        if (cam == null) return;

        Ray ray = cam.ScreenPointToRay(new Vector3(Screen.width / 2f, Screen.height / 2f, 0f));
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, interactRange, ~0, QueryTriggerInteraction.Collide))
        {
            // Check if the player is looking at the keypad
            if (IsHitOnKeypad(hit))
            {
                if (keypad != null) keypad.Open();
                else ShowNarration("The keypad seems broken...", 2f);
            }
            // Check if looking at the door itself
            else if (IsHitOnDoor(hit))
            {
                ShowNarration("The door is locked. Use the keypad.", 2.5f);
                if (doorController != null) doorController.ShakeDoor();
            }
            // Check if looking at sticky notes
            else if (stickyNotePoint != null &&
                     Vector3.Distance(hit.point, stickyNotePoint.position) < 1.5f)
            {
                ShowStickyNoteInfo();
            }
        }
    }

    private void UpdateInteractPrompt()
    {
        if (interactPromptText == null) return;
        if (keypad != null && keypad.IsOpen)
        {
            interactPromptText.enabled = false;
            return;
        }

        Camera cam = Camera.main;
        if (cam == null)
        {
            interactPromptText.enabled = false;
            return;
        }

        Ray ray = cam.ScreenPointToRay(new Vector3(Screen.width / 2f, Screen.height / 2f, 0f));
        RaycastHit hit;

        bool show = false;
        string prompt = "[E] Interact";

        if (Physics.Raycast(ray, out hit, interactRange, ~0, QueryTriggerInteraction.Collide))
        {
            if (IsHitOnKeypad(hit))
            {
                show = true;
                prompt = "[E] Use Keypad";
            }
            else if (IsHitOnDoor(hit))
            {
                show = true;
                prompt = "[E] Try Door";
            }
            else if (stickyNotePoint != null &&
                     Vector3.Distance(hit.point, stickyNotePoint.position) < 1.5f)
            {
                show = true;
                prompt = "[E] Read Notes";
            }
        }

        interactPromptText.enabled = show;
        if (show) interactPromptText.text = prompt;
    }

    // =========================================================================
    // Hit detection helpers (works with LEVEL_DOOR prefab hierarchy)
    // =========================================================================

    private bool IsHitOnKeypad(RaycastHit hit)
    {
        // Check the explicit keypadObject reference
        if (keypadObject != null && hit.collider.transform.IsChildOf(keypadObject.transform))
            return true;

        // Also check DoorController's keypad children
        if (doorController != null)
        {
            if (doorController.keypadMount != null &&
                hit.collider.transform.IsChildOf(doorController.keypadMount.transform))
                return true;
            if (doorController.keypadPanel != null &&
                hit.collider.transform.IsChildOf(doorController.keypadPanel.transform))
                return true;
        }

        return false;
    }

    private bool IsHitOnDoor(RaycastHit hit)
    {
        if (doorObject != null && hit.collider.transform.IsChildOf(doorObject.transform))
            return true;
        if (doorController != null && hit.collider.transform.IsChildOf(doorController.transform))
            return true;
        return false;
    }

    // =========================================================================
    // Sticky note interaction
    // =========================================================================

    private int stickyNoteReadIndex = 0;

    private void ShowStickyNoteInfo()
    {
        if (stickyNoteReadIndex < stickyNoteNarration.Length)
        {
            ShowNarration(stickyNoteNarration[stickyNoteReadIndex], 4f);
            stickyNoteReadIndex++;
        }
        else
        {
            ShowNarration("rodney@please.nyc -- email him for the code.\nHurry, it only lasts 15 seconds.", 4f);
        }
    }

    // =========================================================================
    // KeypadController event handlers
    // =========================================================================

    private void HandleCodeSubmitted(string code)
    {
        // Server mode
        if (!string.IsNullOrEmpty(codeServerUrl) && !offlineMode)
        {
            StartCoroutine(ValidateCodeOnServer(code));
            return;
        }

        // Offline validation
        if (string.IsNullOrEmpty(currentValidCode))
        {
            if (keypad != null) keypad.RejectCode("No code requested yet. Email Rodney first!");
            failedAttempts++;
            ShowFailNarration();
            return;
        }

        if (Time.time > codeExpiryTime)
        {
            if (keypad != null) keypad.RejectCode("Code expired! Request a new one.");
            currentValidCode = "";
            failedAttempts++;
            ShowFailNarration();
            return;
        }

        if (code == currentValidCode)
        {
            OnCodeAccepted();
        }
        else
        {
            OnCodeRejected("WRONG CODE");
        }
    }

    private void HandleCodeRequested()
    {
        if (!string.IsNullOrEmpty(codeServerUrl) && !offlineMode)
        {
            StartCoroutine(RequestCodeFromServer());
        }
        else
        {
            GenerateOfflineCode();
        }
    }

    // =========================================================================
    // Code validation results
    // =========================================================================

    private void OnCodeAccepted()
    {
        if (keypad != null) keypad.AcceptCode("ACCESS GRANTED");
        ShowNarration("The code worked. Wait, how did you get that so fast?", 3f);
        StartCoroutine(DoorOpenSequence());
    }

    private void OnCodeRejected(string reason)
    {
        if (keypad != null) keypad.RejectCode(reason);
        failedAttempts++;
        ShowFailNarration();
    }

    private void ShowFailNarration()
    {
        int idx = Mathf.Min(failedAttempts - 1, failNarration.Length - 1);
        ShowNarration(failNarration[idx], 3f);
    }

    // =========================================================================
    // Code generation / server interaction
    // =========================================================================

    private void GenerateOfflineCode()
    {
        currentValidCode = "";
        for (int i = 0; i < 9; i++)
            currentValidCode += Random.Range(0, 10).ToString();

        codeExpiryTime = Time.time + codeValiditySeconds;
        codeRequested = true;

        if (keypad != null)
            keypad.SetStatus("Code sent! Check console.\nExpires in " + codeValiditySeconds + "s",
                new Color(1f, 0.9f, 0.3f));

        Debug.Log($"[Level4 DEBUG] Rodney's code: {currentValidCode} (expires in {codeValiditySeconds}s)");

        ShowNarration(
            "DEBUG MODE: Code logged to console.\n" +
            "In the real game, you'd email rodney@please.nyc and alt-tab back.", 5f);
    }

    private IEnumerator RequestCodeFromServer()
    {
        if (keypad != null) keypad.SetStatus("Contacting Rodney...", Color.white);

        string url = codeServerUrl.TrimEnd('/') + "/api/request-code";
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes("{}");

        UnityWebRequest req = new UnityWebRequest(url, "POST");
        req.uploadHandler = new UploadHandlerRaw(bodyRaw);
        req.downloadHandler = new DownloadHandlerBuffer();
        req.SetRequestHeader("Content-Type", "application/json");
        req.timeout = 10;

        yield return req.SendWebRequest();

        if (req.result == UnityWebRequest.Result.Success)
        {
            string response = req.downloadHandler.text;
            string code = ParseJsonStringField(response, "code");
            string message = ParseJsonStringField(response, "message");

            codeExpiryTime = Time.time + codeValiditySeconds;
            codeRequested = true;

            if (!string.IsNullOrEmpty(code) && code.Length == 9)
            {
                currentValidCode = code;
                if (keypad != null)
                    keypad.SetStatus(
                        "Code: " + code.Substring(0, 3) + " " + code.Substring(3, 3) + " " +
                        code.Substring(6, 3) + " -- expires in " + codeValiditySeconds + "s",
                        new Color(0.3f, 1f, 0.3f));
                Debug.Log($"[Level4] Server debug code: {code}");
                ShowNarration("Rodney sent the code. It's on screen (debug mode).", 3f);
            }
            else
            {
                if (keypad != null)
                    keypad.SetStatus(message ?? "Rodney replied! Code expires in " + codeValiditySeconds + "s",
                        new Color(0.3f, 1f, 0.3f));
                ShowNarration("Check your email. Rodney sent the code.", 3f);
            }
        }
        else
        {
            if (keypad != null)
                keypad.SetStatus("Rodney isn't responding.", new Color(1f, 0.4f, 0.2f));
            ShowNarration("Can't reach the server. Generating code locally...", 3f);
            GenerateOfflineCode();
        }
    }

    private IEnumerator ValidateCodeOnServer(string code)
    {
        if (keypad != null) keypad.SetStatus("Validating...", Color.white);

        string url = codeServerUrl.TrimEnd('/') + "/api/validate";
        string jsonBody = "{\"code\":\"" + code + "\"}";
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonBody);

        UnityWebRequest req = new UnityWebRequest(url, "POST");
        req.uploadHandler = new UploadHandlerRaw(bodyRaw);
        req.downloadHandler = new DownloadHandlerBuffer();
        req.SetRequestHeader("Content-Type", "application/json");
        req.timeout = 10;

        yield return req.SendWebRequest();

        if (req.result == UnityWebRequest.Result.Success)
        {
            string response = req.downloadHandler.text;
            bool valid = response.Contains("\"valid\":true") ||
                         response.Contains("\"valid\": true");

            if (valid)
            {
                OnCodeAccepted();
            }
            else
            {
                string msg = ParseJsonStringField(response, "message");
                OnCodeRejected(string.IsNullOrEmpty(msg) ? "WRONG CODE" : msg);
            }
        }
        else
        {
            Debug.LogWarning($"[Level4] Server validation failed: {req.error}");
            if (keypad != null) keypad.SetStatus("Server unreachable. Trying offline...", Color.yellow);

            yield return new WaitForSeconds(0.5f);

            if (!string.IsNullOrEmpty(currentValidCode) &&
                Time.time <= codeExpiryTime &&
                code == currentValidCode)
            {
                OnCodeAccepted();
            }
            else
            {
                OnCodeRejected("Could not validate. Try again.");
            }
        }
    }

    private string ParseJsonStringField(string json, string fieldName)
    {
        string key = "\"" + fieldName + "\"";
        int idx = json.IndexOf(key);
        if (idx >= 0)
        {
            int colonIdx = json.IndexOf(':', idx + key.Length);
            if (colonIdx >= 0)
            {
                int quoteStart = json.IndexOf('"', colonIdx + 1);
                int quoteEnd = json.IndexOf('"', quoteStart + 1);
                if (quoteStart >= 0 && quoteEnd > quoteStart)
                    return json.Substring(quoteStart + 1, quoteEnd - quoteStart - 1);
            }
        }
        return null;
    }

    // =========================================================================
    // Timer display (driven from this level script, displayed on keypad)
    // =========================================================================

    private void UpdateKeypadTimer()
    {
        if (keypad == null) return;

        if (codeRequested && codeExpiryTime > 0f)
        {
            float remaining = codeExpiryTime - Time.time;
            if (remaining > 0f)
            {
                Color timerColor = remaining < 5f
                    ? new Color(1f, 0.3f, 0.3f)
                    : new Color(1f, 0.9f, 0.3f);
                keypad.SetTimer("Code expires: " + remaining.ToString("F1") + "s", timerColor);
            }
            else
            {
                keypad.SetTimer("Code EXPIRED", new Color(0.5f, 0.2f, 0.2f));
                currentValidCode = "";
                codeRequested = false;
            }
        }
        else
        {
            keypad.SetTimer("", Color.white);
        }
    }

    // =========================================================================
    // Door Opening
    // =========================================================================

    private IEnumerator DoorOpenSequence()
    {
        doorOpening = true;

        yield return new WaitForSeconds(0.5f);
        if (keypad != null) keypad.Close();

        // Slide door up using DoorController if available
        if (doorController != null)
        {
            doorController.OpenDoor();
            while (doorController.IsAnimating)
                yield return null;
        }

        yield return new WaitForSeconds(1f);
        ShowNarration("Well done. Rodney says hi.", 3f);
        yield return new WaitForSeconds(2f);
        CompleteLevel();
    }

    // =========================================================================
    // HUD Creation (crosshair, interact prompt, narration — NOT the keypad)
    // =========================================================================

    private void CreateHUD()
    {
        Font font = UIHelper.GetDefaultFont();

        GameObject canvasObj = new GameObject("Level4HUD");
        canvasObj.transform.SetParent(transform);
        hudCanvas = canvasObj.AddComponent<Canvas>();
        UIHelper.ConfigureCanvas(hudCanvas, sortingOrder: 20);

        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        canvasObj.AddComponent<GraphicRaycaster>();

        // Crosshair
        GameObject crossObj = new GameObject("Crosshair");
        crossObj.transform.SetParent(canvasObj.transform, false);
        crosshairImage = crossObj.AddComponent<Image>();
        crosshairImage.color = new Color(1f, 1f, 1f, 0.6f);
        crosshairImage.raycastTarget = false;
        RectTransform crossRect = crossObj.GetComponent<RectTransform>();
        crossRect.anchorMin = crossRect.anchorMax = new Vector2(0.5f, 0.5f);
        crossRect.sizeDelta = new Vector2(4, 4);
        crossRect.anchoredPosition = Vector2.zero;

        // Interact prompt
        GameObject promptObj = new GameObject("InteractPrompt");
        promptObj.transform.SetParent(canvasObj.transform, false);
        interactPromptText = promptObj.AddComponent<Text>();
        interactPromptText.font = font;
        interactPromptText.fontSize = 22;
        interactPromptText.alignment = TextAnchor.MiddleCenter;
        interactPromptText.color = new Color(1f, 1f, 1f, 0.85f);
        interactPromptText.raycastTarget = false;
        interactPromptText.enabled = false;
        RectTransform promptRect = promptObj.GetComponent<RectTransform>();
        promptRect.anchorMin = new Vector2(0.35f, 0.4f);
        promptRect.anchorMax = new Vector2(0.65f, 0.46f);
        promptRect.offsetMin = promptRect.offsetMax = Vector2.zero;

        // Narration text
        GameObject narObj = new GameObject("NarrationText");
        narObj.transform.SetParent(canvasObj.transform, false);
        narrationCanvasGroup = narObj.AddComponent<CanvasGroup>();
        narrationCanvasGroup.alpha = 0f;
        narrationText = narObj.AddComponent<Text>();
        narrationText.font = font;
        narrationText.fontSize = 24;
        narrationText.alignment = TextAnchor.MiddleCenter;
        narrationText.color = new Color(0.75f, 0.85f, 1f, 1f);
        narrationText.fontStyle = FontStyle.Italic;
        narrationText.raycastTarget = false;
        RectTransform narRect = narObj.GetComponent<RectTransform>();
        narRect.anchorMin = new Vector2(0.1f, 0.05f);
        narRect.anchorMax = new Vector2(0.9f, 0.14f);
        narRect.offsetMin = narRect.offsetMax = Vector2.zero;
    }

    // =========================================================================
    // Display helpers
    // =========================================================================

    private void ShowNarration(string text, float duration)
    {
        Debug.Log($"[Level4 Narration] {text}");
        if (narrationText == null || narrationCanvasGroup == null) return;
        narrationText.text = text;
        if (narrationFadeCoroutine != null) StopCoroutine(narrationFadeCoroutine);
        narrationFadeCoroutine = StartCoroutine(FadeCanvasGroup(narrationCanvasGroup, duration));
    }

    private IEnumerator FadeCanvasGroup(CanvasGroup cg, float holdDuration)
    {
        float fadeIn = 0.4f;
        float fadeOut = 1f;

        float t = 0f;
        while (t < fadeIn)
        {
            t += Time.deltaTime;
            cg.alpha = Mathf.Clamp01(t / fadeIn);
            yield return null;
        }
        cg.alpha = 1f;

        yield return new WaitForSeconds(holdDuration);

        t = 0f;
        while (t < fadeOut)
        {
            t += Time.deltaTime;
            cg.alpha = 1f - Mathf.Clamp01(t / fadeOut);
            yield return null;
        }
        cg.alpha = 0f;
    }
}

