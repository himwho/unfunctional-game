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
/// Builds its own HUD and keypad UI at runtime.
/// </summary>
public class Level4_KeypadPuzzle : LevelManager
{
    [Header("Level 4 - Keypad Puzzle")]
    public GameObject doorObject;
    public GameObject keypadObject;     // The visual keypad panel on the wall
    public Transform stickyNotePoint;   // Where the sticky notes are (for interact prompt)
    public Transform playerSpawnTransform; // re-exposed for clarity (base class has playerSpawnPoint)

    [Header("Keypad Settings")]
    public float codeValiditySeconds = 15f;
    public float doorOpenSpeed = 2f;
    public float interactRange = 3f;

    [Header("Server")]
    [Tooltip("URL to request a code. POST to this endpoint to simulate emailing Rodney. " +
             "Leave empty to use offline/debug mode.")]
    public string codeServerUrl = "";

    [Header("Debug")]
    [Tooltip("When true (or when server URL is empty), generate codes locally for testing.")]
    public bool offlineMode = true;

    // Runtime UI
    private Canvas hudCanvas;
    private Text interactPromptText;
    private Text messageText;
    private Text narrationText;
    private Image crosshairImage;
    private CanvasGroup messageCanvasGroup;
    private CanvasGroup narrationCanvasGroup;

    // Keypad UI (screen-space overlay that appears when interacting)
    private Canvas keypadCanvas;
    private GameObject keypadPanel;
    private Text codeDisplayText;
    private Text timerText;
    private Text statusText;
    private Button[] digitButtons = new Button[10];
    private Button clearButton;
    private Button submitButton;
    private Button requestCodeButton;
    private Button closeKeypadButton;

    // State
    private string enteredCode = "";
    private string currentValidCode = "";
    private float codeExpiryTime = -1f;
    private bool keypadOpen = false;
    private bool doorOpening = false;
    private int failedAttempts = 0;
    private Coroutine messageFadeCoroutine;
    private Coroutine narrationFadeCoroutine;
    private Vector3 doorClosedPos;
    private Vector3 doorOpenPos;

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

    protected override void Start()
    {
        wantsCursorLocked = true;
        needsPlayer = true;
        base.Start();
        levelDisplayName = "The Keypad";
        levelDescription = "A door. A keypad. An email address.";

        if (doorObject != null)
        {
            doorClosedPos = doorObject.transform.position;
            doorOpenPos = doorClosedPos + Vector3.up * 3f; // Slide door up to open
        }

        CreateHUD();
        CreateKeypadUI();

        ShowNarration("Another room. This time, there's a keypad.", 4f);
    }

    private void Update()
    {
        if (levelComplete || doorOpening) return;

        UpdateInteractPrompt();
        CheckInteraction();
        UpdateKeypadTimer();

        // Close keypad with Escape (but not if pausing)
        if (keypadOpen && Input.GetKeyDown(KeyCode.Escape))
        {
            // Only close keypad, don't trigger pause
            CloseKeypad();
        }
    }

    // =========================================================================
    // Interaction
    // =========================================================================

    private void CheckInteraction()
    {
        if (keypadOpen) return;
        if (InputManager.Instance == null || !InputManager.Instance.InteractPressed) return;

        Camera cam = Camera.main;
        if (cam == null) return;

        Ray ray = cam.ScreenPointToRay(new Vector3(Screen.width / 2f, Screen.height / 2f, 0f));
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, interactRange))
        {
            if (keypadObject != null && hit.collider.gameObject == keypadObject)
            {
                OpenKeypad();
            }
            else if (doorObject != null && hit.collider.gameObject == doorObject)
            {
                ShowNarration("The door is locked. Use the keypad.", 2.5f);
            }
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
        if (keypadOpen)
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

        if (Physics.Raycast(ray, out hit, interactRange))
        {
            if (keypadObject != null && hit.collider.gameObject == keypadObject)
            {
                show = true;
                prompt = "[E] Use Keypad";
            }
            else if (doorObject != null && hit.collider.gameObject == doorObject)
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
    // Keypad Logic
    // =========================================================================

    private void OpenKeypad()
    {
        keypadOpen = true;
        keypadPanel.SetActive(true);
        enteredCode = "";
        UpdateCodeDisplay();

        // Unlock cursor so the player can click buttons
        if (InputManager.Instance != null)
            InputManager.Instance.UnlockCursor();

        // Disable player look/movement
        PlayerController pc = FindAnyObjectByType<PlayerController>();
        if (pc != null) pc.enabled = false;
    }

    private void CloseKeypad()
    {
        keypadOpen = false;
        keypadPanel.SetActive(false);

        // Re-lock cursor for FPS
        if (InputManager.Instance != null)
            InputManager.Instance.LockCursor();

        // Re-enable player
        PlayerController pc = FindAnyObjectByType<PlayerController>();
        if (pc != null) pc.enabled = true;
    }

    private void OnDigitPressed(int digit)
    {
        if (enteredCode.Length >= 9) return;
        enteredCode += digit.ToString();
        UpdateCodeDisplay();
    }

    private void OnClearPressed()
    {
        enteredCode = "";
        UpdateCodeDisplay();
    }

    private void OnSubmitPressed()
    {
        if (enteredCode.Length != 9)
        {
            SetKeypadStatus("Enter all 9 digits");
            return;
        }

        // Check if there's a valid code and it hasn't expired
        if (string.IsNullOrEmpty(currentValidCode))
        {
            SetKeypadStatus("No code requested yet. Email Rodney first!");
            failedAttempts++;
            ShowFailNarration();
            return;
        }

        if (Time.time > codeExpiryTime)
        {
            SetKeypadStatus("Code expired! Request a new one.");
            currentValidCode = "";
            failedAttempts++;
            ShowFailNarration();
            return;
        }

        if (enteredCode == currentValidCode)
        {
            // Success
            SetKeypadStatus("ACCESS GRANTED");
            if (statusText != null) statusText.color = new Color(0.2f, 1f, 0.2f);
            ShowNarration("The code worked. Wait, how did you get that so fast?", 3f);
            StartCoroutine(DoorOpenSequence());
        }
        else
        {
            SetKeypadStatus("WRONG CODE");
            if (statusText != null) statusText.color = new Color(1f, 0.2f, 0.2f);
            failedAttempts++;
            ShowFailNarration();
            enteredCode = "";
            UpdateCodeDisplay();
        }
    }

    private void ShowFailNarration()
    {
        int idx = Mathf.Min(failedAttempts - 1, failNarration.Length - 1);
        ShowNarration(failNarration[idx], 3f);
    }

    private void OnRequestCodePressed()
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

    private void GenerateOfflineCode()
    {
        // Generate a random 9-digit code
        currentValidCode = "";
        for (int i = 0; i < 9; i++)
        {
            currentValidCode += Random.Range(0, 10).ToString();
        }

        codeExpiryTime = Time.time + codeValiditySeconds;

        SetKeypadStatus($"Code sent! Check console.\nExpires in {codeValiditySeconds}s");
        if (statusText != null) statusText.color = new Color(1f, 0.9f, 0.3f);

        // In offline mode, log the code to console (simulates receiving the email)
        Debug.Log($"[Level4 DEBUG] Rodney's code: {currentValidCode} (expires in {codeValiditySeconds}s)");

        ShowNarration(
            "DEBUG MODE: Code logged to console.\n" +
            "In the real game, you'd email rodney@please.nyc and alt-tab back.",
            5f);
    }

    private IEnumerator RequestCodeFromServer()
    {
        SetKeypadStatus("Contacting Rodney...");

        UnityWebRequest req = UnityWebRequest.PostWwwForm(codeServerUrl, "{}");
        req.SetRequestHeader("Content-Type", "application/json");
        req.timeout = 10;

        yield return req.SendWebRequest();

        if (req.result == UnityWebRequest.Result.Success)
        {
            // Expect the response body to be a JSON like {"code":"123456789"}
            // or just the plain 9-digit string
            string response = req.downloadHandler.text;

            // Try to parse as JSON
            string code = ParseCodeFromResponse(response);
            if (!string.IsNullOrEmpty(code) && code.Length == 9)
            {
                currentValidCode = code;
                codeExpiryTime = Time.time + codeValiditySeconds;
                SetKeypadStatus($"Rodney replied! Code expires in {codeValiditySeconds}s");
                if (statusText != null) statusText.color = new Color(0.3f, 1f, 0.3f);
                ShowNarration("Check your email. Rodney sent the code.", 3f);
            }
            else
            {
                SetKeypadStatus("Rodney's reply was... unhelpful.");
                ShowNarration("Something went wrong with Rodney's response.", 3f);
            }
        }
        else
        {
            SetKeypadStatus("Rodney isn't responding.");
            ShowNarration("Can't reach the server. Try offline mode or check the URL.", 3f);

            // Fallback to offline
            GenerateOfflineCode();
        }
    }

    private string ParseCodeFromResponse(string json)
    {
        // Minimal JSON parsing for {"code":"123456789"}
        string key = "\"code\"";
        int idx = json.IndexOf(key);
        if (idx >= 0)
        {
            int colonIdx = json.IndexOf(':', idx + key.Length);
            if (colonIdx >= 0)
            {
                int quoteStart = json.IndexOf('"', colonIdx + 1);
                int quoteEnd = json.IndexOf('"', quoteStart + 1);
                if (quoteStart >= 0 && quoteEnd > quoteStart)
                {
                    return json.Substring(quoteStart + 1, quoteEnd - quoteStart - 1);
                }
            }
        }

        // Maybe it's just the raw digits
        string trimmed = json.Trim().Trim('"');
        if (trimmed.Length == 9 && long.TryParse(trimmed, out _))
        {
            return trimmed;
        }

        return null;
    }

    private void UpdateKeypadTimer()
    {
        if (timerText == null) return;

        if (!string.IsNullOrEmpty(currentValidCode) && codeExpiryTime > 0f)
        {
            float remaining = codeExpiryTime - Time.time;
            if (remaining > 0f)
            {
                timerText.text = $"Code expires: {remaining:F1}s";
                timerText.color = remaining < 5f ?
                    new Color(1f, 0.3f, 0.3f) :
                    new Color(1f, 0.9f, 0.3f);
            }
            else
            {
                timerText.text = "Code EXPIRED";
                timerText.color = new Color(0.5f, 0.2f, 0.2f);
                currentValidCode = "";
            }
        }
        else
        {
            timerText.text = "";
        }
    }

    private void UpdateCodeDisplay()
    {
        if (codeDisplayText == null) return;

        // Show entered digits with underscores for remaining
        string display = "";
        for (int i = 0; i < 9; i++)
        {
            if (i > 0 && i % 3 == 0) display += " ";
            display += (i < enteredCode.Length) ? enteredCode[i].ToString() : "_";
        }
        codeDisplayText.text = display;
    }

    private void SetKeypadStatus(string text)
    {
        if (statusText != null)
        {
            statusText.text = text;
            statusText.color = Color.white;
        }
    }

    // =========================================================================
    // Door Opening
    // =========================================================================

    private IEnumerator DoorOpenSequence()
    {
        doorOpening = true;

        yield return new WaitForSeconds(0.5f);
        CloseKeypad();

        // Slide door up
        if (doorObject != null)
        {
            float t = 0f;
            while (t < 1f)
            {
                t += Time.deltaTime * doorOpenSpeed;
                doorObject.transform.position = Vector3.Lerp(doorClosedPos, doorOpenPos, Mathf.SmoothStep(0f, 1f, t));
                yield return null;
            }
        }

        yield return new WaitForSeconds(1f);
        ShowNarration("Well done. Rodney says hi.", 3f);
        yield return new WaitForSeconds(2f);
        CompleteLevel();
    }

    // =========================================================================
    // HUD Creation
    // =========================================================================

    private void CreateHUD()
    {
        GameObject canvasObj = new GameObject("Level4HUD");
        canvasObj.transform.SetParent(transform);
        hudCanvas = canvasObj.AddComponent<Canvas>();
        hudCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        hudCanvas.sortingOrder = 20;

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
        interactPromptText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        interactPromptText.fontSize = 22;
        interactPromptText.alignment = TextAnchor.MiddleCenter;
        interactPromptText.color = new Color(1f, 1f, 1f, 0.85f);
        interactPromptText.raycastTarget = false;
        interactPromptText.enabled = false;
        RectTransform promptRect = promptObj.GetComponent<RectTransform>();
        promptRect.anchorMin = new Vector2(0.35f, 0.4f);
        promptRect.anchorMax = new Vector2(0.65f, 0.46f);
        promptRect.offsetMin = promptRect.offsetMax = Vector2.zero;

        // Message text
        GameObject msgObj = new GameObject("MessageText");
        msgObj.transform.SetParent(canvasObj.transform, false);
        messageCanvasGroup = msgObj.AddComponent<CanvasGroup>();
        messageCanvasGroup.alpha = 0f;
        messageText = msgObj.AddComponent<Text>();
        messageText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        messageText.fontSize = 32;
        messageText.alignment = TextAnchor.MiddleCenter;
        messageText.color = Color.white;
        messageText.raycastTarget = false;
        RectTransform msgRect = msgObj.GetComponent<RectTransform>();
        msgRect.anchorMin = new Vector2(0.15f, 0.55f);
        msgRect.anchorMax = new Vector2(0.85f, 0.65f);
        msgRect.offsetMin = msgRect.offsetMax = Vector2.zero;

        // Narration text
        GameObject narObj = new GameObject("NarrationText");
        narObj.transform.SetParent(canvasObj.transform, false);
        narrationCanvasGroup = narObj.AddComponent<CanvasGroup>();
        narrationCanvasGroup.alpha = 0f;
        narrationText = narObj.AddComponent<Text>();
        narrationText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
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
    // Keypad UI Creation
    // =========================================================================

    private void CreateKeypadUI()
    {
        // Screen-space overlay canvas for the keypad popup
        GameObject canvasObj = new GameObject("KeypadCanvas");
        canvasObj.transform.SetParent(transform);
        keypadCanvas = canvasObj.AddComponent<Canvas>();
        keypadCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        keypadCanvas.sortingOrder = 50;

        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        canvasObj.AddComponent<GraphicRaycaster>();

        // Dimmed background
        GameObject dimObj = new GameObject("DimBackground");
        dimObj.transform.SetParent(canvasObj.transform, false);
        Image dimImg = dimObj.AddComponent<Image>();
        dimImg.color = new Color(0, 0, 0, 0.7f);
        dimImg.raycastTarget = true;
        RectTransform dimRect = dimObj.GetComponent<RectTransform>();
        dimRect.anchorMin = Vector2.zero;
        dimRect.anchorMax = Vector2.one;
        dimRect.offsetMin = dimRect.offsetMax = Vector2.zero;

        // Main keypad panel
        keypadPanel = new GameObject("KeypadPanel");
        keypadPanel.transform.SetParent(canvasObj.transform, false);
        Image panelBg = keypadPanel.AddComponent<Image>();
        panelBg.color = new Color(0.08f, 0.08f, 0.12f, 0.95f);
        RectTransform panelRect = keypadPanel.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.3f, 0.1f);
        panelRect.anchorMax = new Vector2(0.7f, 0.9f);
        panelRect.offsetMin = panelRect.offsetMax = Vector2.zero;

        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        // Title
        CreateText(keypadPanel, "KeypadTitle", "DOOR ACCESS KEYPAD",
            new Vector2(0.05f, 0.9f), new Vector2(0.95f, 0.97f),
            36, new Color(0.9f, 0.9f, 0.9f), font);

        // Sticky note text (email hint)
        Text stickyText = CreateText(keypadPanel, "StickyNote",
            "Sticky Note: \"rodney@please.nyc\"\n\"this guy always changes the code\"",
            new Vector2(0.05f, 0.82f), new Vector2(0.95f, 0.9f),
            18, new Color(1f, 1f, 0.6f), font);
        stickyText.fontStyle = FontStyle.Italic;

        // Code display
        codeDisplayText = CreateText(keypadPanel, "CodeDisplay", "___ ___ ___",
            new Vector2(0.1f, 0.72f), new Vector2(0.9f, 0.82f),
            48, Color.white, font);

        // Timer
        timerText = CreateText(keypadPanel, "TimerText", "",
            new Vector2(0.1f, 0.67f), new Vector2(0.9f, 0.73f),
            20, new Color(1f, 0.9f, 0.3f), font);

        // Status
        statusText = CreateText(keypadPanel, "StatusText", "Enter the 9-digit code",
            new Vector2(0.05f, 0.62f), new Vector2(0.95f, 0.68f),
            18, new Color(0.7f, 0.7f, 0.7f), font);

        // Number pad (3x4 grid: 1-9, then clear/0/submit)
        float gridLeft = 0.15f;
        float gridRight = 0.85f;
        float gridTop = 0.58f;
        float gridBottom = 0.2f;
        float cellW = (gridRight - gridLeft) / 3f;
        float cellH = (gridTop - gridBottom) / 4f;
        float pad = 0.008f;

        // Rows 1-3 (digits 1-9)
        for (int row = 0; row < 3; row++)
        {
            for (int col = 0; col < 3; col++)
            {
                int digit = row * 3 + col + 1;
                float x0 = gridLeft + col * cellW + pad;
                float x1 = gridLeft + (col + 1) * cellW - pad;
                float y1 = gridTop - row * cellH - pad;
                float y0 = gridTop - (row + 1) * cellH + pad;

                digitButtons[digit] = CreateKeypadButton(keypadPanel, digit.ToString(),
                    digit.ToString(), new Vector2(x0, y0), new Vector2(x1, y1),
                    new Color(0.25f, 0.25f, 0.3f), font, 28);

                int d = digit; // capture for closure
                digitButtons[d].onClick.AddListener(() => OnDigitPressed(d));
            }
        }

        // Row 4: Clear, 0, Submit
        float r4y0 = gridTop - 4 * cellH + pad;
        float r4y1 = gridTop - 3 * cellH - pad;

        clearButton = CreateKeypadButton(keypadPanel, "Clear", "CLR",
            new Vector2(gridLeft + pad, r4y0), new Vector2(gridLeft + cellW - pad, r4y1),
            new Color(0.5f, 0.2f, 0.2f), font, 22);
        clearButton.onClick.AddListener(OnClearPressed);

        digitButtons[0] = CreateKeypadButton(keypadPanel, "Digit0", "0",
            new Vector2(gridLeft + cellW + pad, r4y0), new Vector2(gridLeft + 2 * cellW - pad, r4y1),
            new Color(0.25f, 0.25f, 0.3f), font, 28);
        digitButtons[0].onClick.AddListener(() => OnDigitPressed(0));

        submitButton = CreateKeypadButton(keypadPanel, "Submit", "OK",
            new Vector2(gridLeft + 2 * cellW + pad, r4y0), new Vector2(gridRight - pad, r4y1),
            new Color(0.2f, 0.45f, 0.2f), font, 22);
        submitButton.onClick.AddListener(OnSubmitPressed);

        // Request code button (simulates emailing Rodney)
        requestCodeButton = CreateKeypadButton(keypadPanel, "RequestCode",
            "Email Rodney for Code",
            new Vector2(0.1f, 0.1f), new Vector2(0.65f, 0.18f),
            new Color(0.2f, 0.3f, 0.5f), font, 18);
        requestCodeButton.onClick.AddListener(OnRequestCodePressed);

        // Close button
        closeKeypadButton = CreateKeypadButton(keypadPanel, "CloseKeypad", "X",
            new Vector2(0.9f, 0.93f), new Vector2(0.98f, 0.99f),
            new Color(0.5f, 0.15f, 0.15f), font, 20);
        closeKeypadButton.onClick.AddListener(CloseKeypad);

        // Start hidden
        keypadPanel.SetActive(false);
    }

    private Button CreateKeypadButton(GameObject parent, string name, string label,
        Vector2 anchorMin, Vector2 anchorMax, Color bgColor, Font font, int fontSize)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent.transform, false);

        Image bg = obj.AddComponent<Image>();
        bg.color = bgColor;

        Button btn = obj.AddComponent<Button>();
        ColorBlock colors = btn.colors;
        colors.highlightedColor = new Color(
            Mathf.Min(bgColor.r + 0.15f, 1f),
            Mathf.Min(bgColor.g + 0.15f, 1f),
            Mathf.Min(bgColor.b + 0.15f, 1f), 1f);
        colors.pressedColor = new Color(bgColor.r * 0.6f, bgColor.g * 0.6f, bgColor.b * 0.6f, 1f);
        btn.colors = colors;

        RectTransform rect = obj.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = rect.offsetMax = Vector2.zero;

        // Label
        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(obj.transform, false);
        Text txt = textObj.AddComponent<Text>();
        txt.text = label;
        txt.font = font;
        txt.fontSize = fontSize;
        txt.alignment = TextAnchor.MiddleCenter;
        txt.color = Color.white;
        RectTransform textRect = textObj.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = textRect.offsetMax = Vector2.zero;

        return btn;
    }

    private Text CreateText(GameObject parent, string name, string content,
        Vector2 anchorMin, Vector2 anchorMax, int fontSize, Color color, Font font)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent.transform, false);
        Text txt = obj.AddComponent<Text>();
        txt.text = content;
        txt.font = font;
        txt.fontSize = fontSize;
        txt.alignment = TextAnchor.MiddleCenter;
        txt.color = color;
        txt.raycastTarget = false;
        RectTransform rect = obj.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
        return txt;
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

    private void ShowMessage(string message)
    {
        Debug.Log($"[Level4] {message}");
        if (messageText == null || messageCanvasGroup == null) return;
        messageText.text = message;
        if (messageFadeCoroutine != null) StopCoroutine(messageFadeCoroutine);
        messageFadeCoroutine = StartCoroutine(FadeCanvasGroup(messageCanvasGroup, 3f));
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
