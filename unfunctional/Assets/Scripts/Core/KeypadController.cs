using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Reusable keypad interaction controller.
/// Lives on the LEVEL_DOOR prefab alongside DoorController.
///
/// Handles:
///  - Creating the screen-space keypad UI overlay at runtime
///  - Digit entry, display, clear, submit
///  - Timer display
///  - "Request Code" button (togglable per level)
///
/// Does NOT handle code validation. Instead it fires events that the
/// level script subscribes to:
///   OnCodeSubmitted(string code)  — player pressed OK with a complete code
///   OnCodeRequested()             — player pressed the "Request Code" button
///
/// The level script calls back into this controller to report results:
///   AcceptCode() / RejectCode(message) / SetStatus(text, color) / etc.
///
/// Usage from a level script:
///   keypad.codeLength = 9;
///   keypad.keypadTitle = "DOOR ACCESS KEYPAD";
///   keypad.showRequestCodeButton = true;
///   keypad.requestCodeLabel = "Email Rodney for Code";
///   keypad.OnCodeSubmitted += HandleSubmit;
///   keypad.OnCodeRequested += HandleRequestCode;
///   keypad.Open();
/// </summary>
public class KeypadController : MonoBehaviour
{
    // =========================================================================
    // Configuration (set by level scripts before Open())
    // =========================================================================

    [Header("Keypad Settings")]
    [Tooltip("Number of digits the player must enter.")]
    public int codeLength = 9;

    [Tooltip("Title shown at the top of the keypad overlay.")]
    public string keypadTitle = "DOOR ACCESS KEYPAD";

    [Tooltip("Hint text shown below the title (e.g. sticky note contents).")]
    [TextArea(2, 4)]
    public string hintText = "";

    [Header("Request Code Button")]
    [Tooltip("Show a button that lets the player request a code (e.g. email, NPC).")]
    public bool showRequestCodeButton = false;

    [Tooltip("Label for the request code button.")]
    public string requestCodeLabel = "Request Code";

    // =========================================================================
    // Events — subscribe from your level script
    // =========================================================================

    /// <summary>Fired when the player presses OK with a complete code.</summary>
    public event Action<string> OnCodeSubmitted;

    /// <summary>Fired when the player presses the "Request Code" button.</summary>
    public event Action OnCodeRequested;

    /// <summary>Fired when the keypad is opened.</summary>
    public event Action OnKeypadOpened;

    /// <summary>Fired when the keypad is closed (by X button or Escape).</summary>
    public event Action OnKeypadClosed;

    // =========================================================================
    // Public state
    // =========================================================================

    public bool IsOpen { get; private set; }
    public string EnteredCode => enteredCode;

    // =========================================================================
    // Runtime UI references (created lazily)
    // =========================================================================

    private Canvas keypadCanvas;
    private GameObject keypadPanel;
    private Text codeDisplayText;
    private Text timerText;
    private Text statusText;
    private Text hintTextUI;
    private Text titleTextUI;
    private Button[] digitButtons = new Button[10];
    private Button clearButton;
    private Button submitButton;
    private Button requestCodeButton;
    private Button closeKeypadButton;
    private Font defaultFont;
    private bool uiBuilt = false;

    // State
    private string enteredCode = "";

    // =========================================================================
    // Lifecycle
    // =========================================================================

    private void OnDestroy()
    {
        // Unsubscribe safety
        OnCodeSubmitted = null;
        OnCodeRequested = null;
        OnKeypadOpened = null;
        OnKeypadClosed = null;
    }

    // =========================================================================
    // Public API — called by level scripts
    // =========================================================================

    /// <summary>Opens the keypad overlay. Unlocks cursor, disables player.</summary>
    public void Open()
    {
        if (IsOpen) return;
        EnsureUI();

        IsOpen = true;
        keypadPanel.SetActive(true);
        enteredCode = "";
        UpdateCodeDisplay();
        SetStatus("Enter the " + codeLength + "-digit code", Color.white);

        // Refresh configurable elements
        if (titleTextUI != null) titleTextUI.text = keypadTitle;
        if (hintTextUI != null)
        {
            hintTextUI.text = hintText;
            hintTextUI.gameObject.SetActive(!string.IsNullOrEmpty(hintText));
        }
        if (requestCodeButton != null)
        {
            requestCodeButton.gameObject.SetActive(showRequestCodeButton);
            Text reqLabel = requestCodeButton.GetComponentInChildren<Text>();
            if (reqLabel != null) reqLabel.text = requestCodeLabel;
        }

        // Unlock cursor
        if (InputManager.Instance != null)
            InputManager.Instance.UnlockCursor();

        // Disable player movement
        PlayerController pc = FindAnyObjectByType<PlayerController>();
        if (pc != null) pc.enabled = false;

        OnKeypadOpened?.Invoke();
    }

    /// <summary>Closes the keypad overlay. Re-locks cursor, enables player.</summary>
    public void Close()
    {
        if (!IsOpen) return;
        IsOpen = false;

        if (keypadPanel != null)
            keypadPanel.SetActive(false);

        // Re-lock cursor for FPS
        if (InputManager.Instance != null)
            InputManager.Instance.LockCursor();

        // Re-enable player
        PlayerController pc = FindAnyObjectByType<PlayerController>();
        if (pc != null) pc.enabled = true;

        OnKeypadClosed?.Invoke();
    }

    /// <summary>Call from your level script when the entered code is correct.</summary>
    public void AcceptCode(string message = "ACCESS GRANTED")
    {
        SetStatus(message, new Color(0.2f, 1f, 0.2f));
    }

    /// <summary>Call from your level script when the entered code is wrong.</summary>
    public void RejectCode(string message = "WRONG CODE")
    {
        SetStatus(message, new Color(1f, 0.2f, 0.2f));
        enteredCode = "";
        UpdateCodeDisplay();
    }

    /// <summary>Set the status line text and color.</summary>
    public void SetStatus(string text, Color color)
    {
        if (statusText != null)
        {
            statusText.text = text;
            statusText.color = color;
        }
    }

    /// <summary>Set the timer text (shown below the code display).</summary>
    public void SetTimer(string text, Color color)
    {
        if (timerText != null)
        {
            timerText.text = text;
            timerText.color = color;
        }
    }

    /// <summary>Clears the entered code and updates the display.</summary>
    public void ClearInput()
    {
        enteredCode = "";
        UpdateCodeDisplay();
    }

    /// <summary>Update the hint text dynamically (e.g. after NPC gives code).</summary>
    public void SetHint(string hint)
    {
        hintText = hint;
        if (hintTextUI != null)
        {
            hintTextUI.text = hint;
            hintTextUI.gameObject.SetActive(!string.IsNullOrEmpty(hint));
        }
    }

    // =========================================================================
    // Internal — input handlers
    // =========================================================================

    private void Update()
    {
        if (!IsOpen) return;

        // Close with Escape
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Close();
            return;
        }

        // Allow keyboard digit entry while keypad is open
        for (int i = 0; i <= 9; i++)
        {
            if (Input.GetKeyDown(KeyCode.Alpha0 + i) || Input.GetKeyDown(KeyCode.Keypad0 + i))
            {
                OnDigitPressed(i);
                break;
            }
        }

        // Backspace to clear last digit
        if (Input.GetKeyDown(KeyCode.Backspace) && enteredCode.Length > 0)
        {
            enteredCode = enteredCode.Substring(0, enteredCode.Length - 1);
            UpdateCodeDisplay();
        }

        // Enter to submit
        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
        {
            OnSubmitPressed();
        }
    }

    private void OnDigitPressed(int digit)
    {
        if (enteredCode.Length >= codeLength) return;
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
        if (enteredCode.Length != codeLength)
        {
            SetStatus("Enter all " + codeLength + " digits", new Color(1f, 0.6f, 0.2f));
            return;
        }

        OnCodeSubmitted?.Invoke(enteredCode);
    }

    private void OnRequestCodeClicked()
    {
        OnCodeRequested?.Invoke();
    }

    // =========================================================================
    // Display
    // =========================================================================

    private void UpdateCodeDisplay()
    {
        if (codeDisplayText == null) return;

        string display = "";
        int groupSize = codeLength <= 6 ? 3 : 3; // always group by 3
        for (int i = 0; i < codeLength; i++)
        {
            if (i > 0 && i % groupSize == 0) display += " ";
            display += (i < enteredCode.Length) ? enteredCode[i].ToString() : "_";
        }
        codeDisplayText.text = display;
    }

    // =========================================================================
    // UI Building (done once, lazily)
    // =========================================================================

    private void EnsureUI()
    {
        if (uiBuilt) return;
        uiBuilt = true;

        defaultFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        // Screen-space overlay canvas
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
        GameObject dimObj = MakeChild(canvasObj, "DimBackground");
        Image dimImg = dimObj.AddComponent<Image>();
        dimImg.color = new Color(0, 0, 0, 0.7f);
        dimImg.raycastTarget = true;
        Stretch(dimObj);

        // Main keypad panel
        keypadPanel = MakeChild(canvasObj, "KeypadPanel");
        Image panelBg = keypadPanel.AddComponent<Image>();
        panelBg.color = new Color(0.08f, 0.08f, 0.12f, 0.95f);
        RectTransform panelRect = keypadPanel.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.3f, 0.1f);
        panelRect.anchorMax = new Vector2(0.7f, 0.9f);
        panelRect.offsetMin = panelRect.offsetMax = Vector2.zero;

        // Title
        titleTextUI = MakeText(keypadPanel, "KeypadTitle", keypadTitle,
            new Vector2(0.05f, 0.9f), new Vector2(0.95f, 0.97f),
            36, new Color(0.9f, 0.9f, 0.9f));

        // Hint text (sticky note info etc.)
        hintTextUI = MakeText(keypadPanel, "HintText", hintText,
            new Vector2(0.05f, 0.82f), new Vector2(0.95f, 0.9f),
            18, new Color(1f, 1f, 0.6f));
        hintTextUI.fontStyle = FontStyle.Italic;
        hintTextUI.gameObject.SetActive(!string.IsNullOrEmpty(hintText));

        // Code display
        codeDisplayText = MakeText(keypadPanel, "CodeDisplay", "",
            new Vector2(0.1f, 0.72f), new Vector2(0.9f, 0.82f),
            48, Color.white);

        // Timer
        timerText = MakeText(keypadPanel, "TimerText", "",
            new Vector2(0.1f, 0.67f), new Vector2(0.9f, 0.73f),
            20, new Color(1f, 0.9f, 0.3f));

        // Status
        statusText = MakeText(keypadPanel, "StatusText", "Enter the " + codeLength + "-digit code",
            new Vector2(0.05f, 0.62f), new Vector2(0.95f, 0.68f),
            18, new Color(0.7f, 0.7f, 0.7f));

        // ── Number pad grid (3×4: digits 1-9, then CLR / 0 / OK) ──
        float gridLeft = 0.15f, gridRight = 0.85f;
        float gridTop = 0.58f, gridBottom = 0.2f;
        float cellW = (gridRight - gridLeft) / 3f;
        float cellH = (gridTop - gridBottom) / 4f;
        float pad = 0.008f;

        for (int row = 0; row < 3; row++)
        {
            for (int col = 0; col < 3; col++)
            {
                int digit = row * 3 + col + 1;
                float x0 = gridLeft + col * cellW + pad;
                float x1 = gridLeft + (col + 1) * cellW - pad;
                float y1 = gridTop - row * cellH - pad;
                float y0 = gridTop - (row + 1) * cellH + pad;

                digitButtons[digit] = MakeButton(keypadPanel, "Digit" + digit, digit.ToString(),
                    new Vector2(x0, y0), new Vector2(x1, y1),
                    new Color(0.25f, 0.25f, 0.3f), 28);

                int d = digit;
                digitButtons[d].onClick.AddListener(() => OnDigitPressed(d));
            }
        }

        // Row 4
        float r4y0 = gridTop - 4 * cellH + pad;
        float r4y1 = gridTop - 3 * cellH - pad;

        clearButton = MakeButton(keypadPanel, "Clear", "CLR",
            new Vector2(gridLeft + pad, r4y0), new Vector2(gridLeft + cellW - pad, r4y1),
            new Color(0.5f, 0.2f, 0.2f), 22);
        clearButton.onClick.AddListener(OnClearPressed);

        digitButtons[0] = MakeButton(keypadPanel, "Digit0", "0",
            new Vector2(gridLeft + cellW + pad, r4y0), new Vector2(gridLeft + 2 * cellW - pad, r4y1),
            new Color(0.25f, 0.25f, 0.3f), 28);
        digitButtons[0].onClick.AddListener(() => OnDigitPressed(0));

        submitButton = MakeButton(keypadPanel, "Submit", "OK",
            new Vector2(gridLeft + 2 * cellW + pad, r4y0), new Vector2(gridRight - pad, r4y1),
            new Color(0.2f, 0.45f, 0.2f), 22);
        submitButton.onClick.AddListener(OnSubmitPressed);

        // Request code button
        requestCodeButton = MakeButton(keypadPanel, "RequestCode", requestCodeLabel,
            new Vector2(0.1f, 0.1f), new Vector2(0.65f, 0.18f),
            new Color(0.2f, 0.3f, 0.5f), 18);
        requestCodeButton.onClick.AddListener(OnRequestCodeClicked);
        requestCodeButton.gameObject.SetActive(showRequestCodeButton);

        // Close button
        closeKeypadButton = MakeButton(keypadPanel, "CloseKeypad", "X",
            new Vector2(0.9f, 0.93f), new Vector2(0.98f, 0.99f),
            new Color(0.5f, 0.15f, 0.15f), 20);
        closeKeypadButton.onClick.AddListener(Close);

        // Start hidden
        keypadPanel.SetActive(false);
    }

    // =========================================================================
    // UI Helpers
    // =========================================================================

    private GameObject MakeChild(GameObject parent, string name)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent.transform, false);
        obj.AddComponent<RectTransform>();
        return obj;
    }

    private void Stretch(GameObject obj)
    {
        RectTransform rt = obj.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
    }

    private Text MakeText(GameObject parent, string name, string content,
        Vector2 ancMin, Vector2 ancMax, int fontSize, Color color)
    {
        GameObject obj = MakeChild(parent, name);
        RectTransform rt = obj.GetComponent<RectTransform>();
        rt.anchorMin = ancMin;
        rt.anchorMax = ancMax;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        Text txt = obj.AddComponent<Text>();
        txt.text = content;
        txt.font = defaultFont;
        txt.fontSize = fontSize;
        txt.alignment = TextAnchor.MiddleCenter;
        txt.color = color;
        txt.raycastTarget = false;
        return txt;
    }

    private Button MakeButton(GameObject parent, string name, string label,
        Vector2 ancMin, Vector2 ancMax, Color bgColor, int fontSize)
    {
        GameObject obj = MakeChild(parent, name);
        RectTransform rt = obj.GetComponent<RectTransform>();
        rt.anchorMin = ancMin;
        rt.anchorMax = ancMax;
        rt.offsetMin = rt.offsetMax = Vector2.zero;

        Image bg = obj.AddComponent<Image>();
        bg.color = bgColor;

        Button btn = obj.AddComponent<Button>();
        ColorBlock cb = btn.colors;
        cb.highlightedColor = new Color(
            Mathf.Min(bgColor.r + 0.15f, 1f),
            Mathf.Min(bgColor.g + 0.15f, 1f),
            Mathf.Min(bgColor.b + 0.15f, 1f), 1f);
        cb.pressedColor = new Color(bgColor.r * 0.6f, bgColor.g * 0.6f, bgColor.b * 0.6f, 1f);
        btn.colors = cb;

        GameObject textObj = MakeChild(obj, "Text");
        Stretch(textObj);
        Text txt = textObj.AddComponent<Text>();
        txt.text = label;
        txt.font = defaultFont;
        txt.fontSize = fontSize;
        txt.alignment = TextAnchor.MiddleCenter;
        txt.color = Color.white;

        return btn;
    }
}
