using System.Collections;
using NavKeypad;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;
using UnityEngine.Networking;

/// <summary>
/// LEVEL 4: A room with a locked door and a keypad. Above the keypad are sticky
/// notes with an email address (rodney@premiumdoorcodes.com) and a warning that "this guy
/// always changes the code". The player is expected to alt-tab out of the game,
/// send an email to that address asking for the door code, receive a 9-digit
/// code that expires in 60 seconds, then alt-tab back and type it into the keypad.
///
/// The server only replies with a code if the email contains a trigger keyword
/// ("door", "code", or "please"). Otherwise Rodney sends an annoyed non-code reply.
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

    [Header("Player Prop")]
    [Tooltip("Assign the Finger_Animated prefab here to equip it to the player in Level 4.")]
    public GameObject fingerAnimatedPrefab;

    [Tooltip("Local position of the equipped finger relative to the player camera.")]
    public Vector3 fingerHeldLocalPosition = new Vector3(0.28f, -0.32f, 0.55f);

    [Tooltip("Local rotation of the equipped finger relative to the player camera.")]
    public Vector3 fingerHeldLocalRotation = new Vector3(8f, -92f, 18f);

    [Tooltip("Local scale of the equipped finger relative to the player camera.")]
    public Vector3 fingerHeldLocalScale = Vector3.one;

    [Tooltip("Animator trigger fired when the player left-clicks.")]
    public string fingerClickTriggerName = "Straighten";

    [Tooltip("Delay before a keypad button press is registered, so it lines up with the finger reaching full extension.")]
    public float fingerKeypadPressDelay = 0.12f;

    [Tooltip("Child object on the finger prefab whose trigger collider determines which keypad button gets pressed.")]
    public string fingerTipChildName = "FingerTip";

    [Tooltip("DoorController on the LEVEL_DOOR prefab.")]
    public DoorController doorController;

    [Header("Keypad Settings")]
    public float codeValiditySeconds = 60f;
    public float interactRange = 3f;
    public float keypadButtonAimRadius = 0.08f;
    [Tooltip("Horizontal screen-space offset in pixels used when aiming keypad buttons. Positive values aim farther right.")]
    public float keypadAimOffsetX = 0f;
    [Tooltip("Vertical screen-space offset in pixels used when aiming keypad buttons. Positive values aim up, negative values aim down.")]
    public float keypadAimOffsetY = 0f;

    [Header("Server")]
    [Tooltip("Base URL of the email support server. " +
             "Endpoints: POST /api/request-code, POST /api/validate. " +
             "Server requires trigger keywords (door/code/please) in email. " +
             "Leave empty to use offline/debug mode.")]
    public string codeServerUrl = "https://premiumdoorcodes.com";

    [Header("Debug")]
    [Tooltip("When true (or when server URL is empty), generate codes locally.")]
    public bool offlineMode = true;
    public bool debugFingerTipPressLogging = true;

    // =========================================================================
    // Runtime references
    // =========================================================================

    private KeypadController keypad; // from doorController

    // HUD
    private Canvas hudCanvas;
    private Text interactPromptText;
    private Text narrationText;
    private CanvasGroup narrationCanvasGroup;

    // State
    private string currentValidCode = "";
    private float codeExpiryTime = -1f;
    private bool codeRequested = false;
    private bool doorOpening = false;
    private int failedAttempts = 0;
    private Coroutine narrationFadeCoroutine;
    private GameObject equippedFingerInstance;
    private Animator equippedFingerAnimator;
    private WorldKeypadButton hoveredKeypadButton;
    private Transform physicalKeypadRoot;
    private Coroutine pendingFingerPressCoroutine;
    private FingerTipKeypadDetector fingerTipDetector;
    private Transform fingerTipTransform;
    private SphereCollider fingerTipSphereCollider;
    private Camera fingerViewModelCamera;
    private Camera fingerBaseCamera;
    private int fingerBaseCameraOriginalCullingMask;
    private bool fingerBaseCameraMaskCaptured;

    private const int FingerViewModelLayer = 30;

    // Narration lines
    private static readonly string[] stickyNoteNarration = new string[]
    {
        "There's an email address on a sticky note: rodney@premiumdoorcodes.com",
        "Another note says: \"this guy always changes the code\"",
        "A third note: \"just ask him for the door code -- he's picky about wording\"",
        "Looks like you'll need to email Rodney and ask for the door code.",
    };

    private static readonly string[] failNarration = new string[]
    {
        "Wrong code. Rodney's codes expire after 60 seconds.",
        "Nope. Did you type it in time?",
        "Still wrong. Maybe email Rodney again?",
        "The code changes every time. You need a fresh one.",
        "This is the point where you alt-tab and send an email.",
        "Make sure you actually ask for the door code. Rodney's picky.",
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
            keypad.hintText = "Sticky Note: \"rodney@premiumdoorcodes.com\"\n\"this guy always changes the code\"\n\"just ask him for the door code\"";
            keypad.showRequestCodeButton = false;

            // Subscribe to events
            keypad.OnCodeSubmitted += HandleCodeSubmitted;
            keypad.OnCodeRequested += HandleCodeRequested;
        }
        else
        {
            Debug.LogWarning("[Level4] No KeypadController found! Add one to the LEVEL_DOOR prefab.");
        }

        if (doorController != null)
        {
            GameObject panel = doorController.doorPanel;
            if (panel == null)
                panel = doorController.gameObject;
            if (panel.GetComponent<Collider>() == null)
                panel.AddComponent<BoxCollider>();
        }

        CreateHUD();
        SetupPhysicalKeypad();
        StartCoroutine(EquipFingerWhenPlayerReady());
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

        if (equippedFingerInstance != null)
            Destroy(equippedFingerInstance);

        CleanupFingerViewModelCamera();

        if (keypad != null)
            keypad.UnregisterExternalDisplay();

        base.OnDestroy();
    }

    private void Update()
    {
        if (levelComplete || doorOpening) return;

        UpdateEquippedFingerTransform();
        hoveredKeypadButton = GetHoveredKeypadButton();

        HandleFingerClickAnimation();
        UpdateInteraction();
        UpdateKeypadTimer();
    }

    // =========================================================================
    // Interaction (raycasting in 3D world)
    // =========================================================================

    private void UpdateInteraction()
    {
        if (interactPromptText == null) return;

        Camera cam = Camera.main;
        if (cam == null) return;

        bool lookingAtDoor = false;
        bool lookingAtStickyNotes = false;

        Ray ray = new Ray(cam.transform.position, cam.transform.forward);
        if (Physics.Raycast(ray, out RaycastHit hit, 5f, ~0, QueryTriggerInteraction.Collide))
        {
            if (IsHitOnKeypad(hit))
            {
                // looking at keypad — no prompt, finger handles it
            }
            else if (IsHitOnDoor(hit))
            {
                lookingAtDoor = true;
            }
            else if (stickyNotePoint != null &&
                     Vector3.Distance(hit.point, stickyNotePoint.position) < 1.5f)
            {
                lookingAtStickyNotes = true;
            }
        }

        if (lookingAtDoor)
        {
            interactPromptText.text = "Press [E] to open";
            interactPromptText.enabled = true;
            if (InputManager.Instance != null && InputManager.Instance.InteractPressed)
            {
                if (doorController != null)
                    doorController.ShakeDoor();
                ShowNarration("Did you e-mail Rodney for the code?", 3f);
            }
        }
        else if (lookingAtStickyNotes)
        {
            interactPromptText.text = "[E] Read Notes";
            interactPromptText.enabled = true;
            if (InputManager.Instance != null && InputManager.Instance.InteractPressed)
            {
                ShowStickyNoteInfo();
            }
        }
        else
        {
            interactPromptText.enabled = false;
        }
    }

    // =========================================================================
    // Hit detection helpers (works with LEVEL_DOOR prefab hierarchy)
    // =========================================================================

    private bool IsHitOnKeypad(RaycastHit hit)
    {
        if (keypadObject != null && hit.collider.transform.IsChildOf(keypadObject.transform))
            return true;

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
        // Keypad children are part of the door hierarchy but should never
        // register as a "door" hit — they're handled by IsHitOnKeypad.
        if (IsHitOnKeypad(hit)) return false;

        if (doorObject != null && hit.collider.transform.IsChildOf(doorObject.transform))
            return true;
        if (doorController != null && hit.collider.transform.IsChildOf(doorController.transform))
            return true;
        return false;
    }

    // =========================================================================
    // Physical keypad setup
    // =========================================================================

    private void SetupPhysicalKeypad()
    {
        if (keypad == null || doorController == null)
            return;

        physicalKeypadRoot = ResolvePhysicalKeypadRoot();
        if (physicalKeypadRoot == null)
        {
            Debug.LogWarning("[Level4] Could not resolve the physical keypad root.");
            return;
        }

        DisableLegacyPhysicalKeypadBehavior();
        SetupWorldKeypadDisplay();
        SetupWorldKeypadButtons();
        keypad.ClearInput();
        keypad.SetStatus("Enter the " + keypad.codeLength + "-digit code", Color.white);
        keypad.SetTimer("", Color.white);
    }

    private void DisableLegacyPhysicalKeypadBehavior()
    {
        if (physicalKeypadRoot == null) return;

        Keypad[] legacyKeypads = physicalKeypadRoot.GetComponentsInParent<Keypad>(true);
        for (int i = 0; i < legacyKeypads.Length; i++)
            legacyKeypads[i].enabled = false;

        KeypadButton[] legacyButtons = physicalKeypadRoot.GetComponentsInChildren<KeypadButton>(true);
        for (int i = 0; i < legacyButtons.Length; i++)
            legacyButtons[i].enabled = false;
    }

    private void SetupWorldKeypadDisplay()
    {
        Canvas displayCanvas = physicalKeypadRoot != null
            ? physicalKeypadRoot.GetComponentInChildren<Canvas>(true)
            : null;
        if (displayCanvas == null)
        {
            Debug.LogWarning("[Level4] No world-space keypad display canvas found.");
            return;
        }

        TMP_Text legacyDisplayText = null;
        Transform legacyDisplayTextTransform = FindNamedDescendant(displayCanvas.transform, "DisplayText");
        if (legacyDisplayTextTransform != null)
            legacyDisplayText = legacyDisplayTextTransform.GetComponent<TMP_Text>();

        if (legacyDisplayText != null)
            keypad.RegisterExternalDisplay(legacyDisplayText);

        Font font = UIHelper.GetDefaultFont();
        Text timerDisplay = GetOrCreateWorldDisplayText(
            displayCanvas.transform,
            "Level4TimerDisplay",
            font,
            14,
            new Color(1f, 0.9f, 0.3f),
            new Vector2(0.02f, 0.18f),
            new Vector2(0.98f, 0.42f),
            TextAnchor.MiddleCenter);

        Text statusDisplay = GetOrCreateWorldDisplayText(
            displayCanvas.transform,
            "Level4StatusDisplay",
            font,
            12,
            new Color(0.8f, 0.8f, 0.8f),
            new Vector2(0.02f, 0.02f),
            new Vector2(0.98f, 0.18f),
            TextAnchor.MiddleCenter);

        keypad.RegisterExternalDisplay(null, timerDisplay, statusDisplay);
    }

    private Text GetOrCreateWorldDisplayText(
        Transform parent,
        string name,
        Font font,
        int fontSize,
        Color color,
        Vector2 anchorMin,
        Vector2 anchorMax,
        TextAnchor alignment)
    {
        Transform existing = parent.Find(name);
        GameObject textObj = existing != null ? existing.gameObject : new GameObject(name);
        if (existing == null)
        {
            textObj.transform.SetParent(parent, false);
            textObj.AddComponent<RectTransform>();
        }

        RectTransform rect = textObj.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        Text text = textObj.GetComponent<Text>();
        if (text == null)
            text = textObj.AddComponent<Text>();

        text.font = font;
        text.fontSize = fontSize;
        text.alignment = alignment;
        text.color = color;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        text.raycastTarget = false;
        return text;
    }

    private void SetupWorldKeypadButtons()
    {
        Transform keypadRoot = physicalKeypadRoot;

        ConfigureExistingKeypadDigit(keypadRoot, "bttn0", 0);
        ConfigureExistingKeypadDigit(keypadRoot, "bttn1", 1);
        ConfigureExistingKeypadDigit(keypadRoot, "bttn2", 2);
        ConfigureExistingKeypadDigit(keypadRoot, "bttn3", 3);
        ConfigureExistingKeypadDigit(keypadRoot, "bttn4", 4);
        ConfigureExistingKeypadDigit(keypadRoot, "bttn5", 5);
        ConfigureExistingKeypadDigit(keypadRoot, "bttn6", 6);
        ConfigureExistingKeypadDigit(keypadRoot, "bttn7", 7);
        ConfigureExistingKeypadDigit(keypadRoot, "bttn8", 8);
        ConfigureExistingKeypadDigit(keypadRoot, "bttn9", 9);
        ConfigureExistingKeypadAction(keypadRoot, "bttnEnter", WorldKeypadButton.ButtonAction.Submit, "OK");
    }

    private void ConfigureExistingKeypadDigit(Transform keypadRoot, string buttonName, int digit)
    {
        Transform buttonTransform = FindNamedDescendant(keypadRoot, buttonName);
        if (buttonTransform == null)
        {
            Debug.LogWarning($"[Level4] Keypad button '{buttonName}' not found.");
            return;
        }

        WorldKeypadButton button = GetOrAddWorldKeypadButton(buttonTransform);
        button.ConfigureDigit(digit);
    }

    private void ConfigureExistingKeypadAction(
        Transform keypadRoot,
        string buttonName,
        WorldKeypadButton.ButtonAction action,
        string promptLabel)
    {
        Transform buttonTransform = FindNamedDescendant(keypadRoot, buttonName);
        if (buttonTransform == null)
        {
            Debug.LogWarning($"[Level4] Keypad button '{buttonName}' not found.");
            return;
        }

        WorldKeypadButton button = GetOrAddWorldKeypadButton(buttonTransform);
        button.ConfigureAction(action, promptLabel);
    }

    private WorldKeypadButton GetOrAddWorldKeypadButton(Transform buttonTransform)
    {
        WorldKeypadButton button = buttonTransform.GetComponent<WorldKeypadButton>();
        if (button == null)
            button = buttonTransform.gameObject.AddComponent<WorldKeypadButton>();

        Collider col = buttonTransform.GetComponent<Collider>();
        if (col != null)
            col.enabled = true;

        return button;
    }

    private Transform FindNamedDescendant(Transform root, string objectName)
    {
        if (root == null) return null;

        Transform[] children = root.GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < children.Length; i++)
        {
            if (children[i].name == objectName)
                return children[i];
        }

        return null;
    }

    private Transform ResolvePhysicalKeypadRoot()
    {
        if (doorController == null) return null;

        Transform buttons = FindNamedDescendant(doorController.transform, "Buttons");
        if (buttons != null && buttons.parent != null)
            return buttons.parent;

        Transform displayCanvas = FindNamedDescendant(doorController.transform, "DisplayCanvas");
        if (displayCanvas != null && displayCanvas.parent != null)
            return displayCanvas.parent;

        if (doorController.keypadMount != null)
            return doorController.keypadMount.transform;

        return null;
    }

    private WorldKeypadButton GetHoveredKeypadButton()
    {
        Camera cam = Camera.main;
        if (cam == null || physicalKeypadRoot == null)
            return null;

        float maxDistance = interactRange;
        Vector3 screenPoint = GetKeypadAimScreenPoint();
        Ray ray = cam.ScreenPointToRay(screenPoint);
        RaycastHit[] directHits = Physics.RaycastAll(ray, maxDistance, ~0, QueryTriggerInteraction.Collide);
        WorldKeypadButton directButton = FindClosestKeypadButton(directHits);
        if (directButton != null)
            return directButton;

        RaycastHit[] hits = Physics.SphereCastAll(
            ray,
            keypadButtonAimRadius,
            maxDistance,
            ~0,
            QueryTriggerInteraction.Collide);

        return FindClosestKeypadButton(hits);
    }

    private Vector3 GetKeypadAimScreenPoint()
    {
        return new Vector3(
            (Screen.width * 0.5f) + keypadAimOffsetX,
            (Screen.height * 0.5f) + keypadAimOffsetY,
            0f);
    }

    private WorldKeypadButton FindClosestKeypadButton(RaycastHit[] hits)
    {
        float closestDistance = float.MaxValue;
        WorldKeypadButton closestButton = null;
        for (int i = 0; i < hits.Length; i++)
        {
            WorldKeypadButton button = hits[i].collider.GetComponentInParent<WorldKeypadButton>();
            if (button == null) continue;
            if (!button.transform.IsChildOf(physicalKeypadRoot)) continue;
            if (hits[i].distance >= closestDistance) continue;

            closestDistance = hits[i].distance;
            closestButton = button;
        }

        return closestButton;
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
            ShowNarration("rodney@premiumdoorcodes.com -- ask him for the door code.\nYou have 60 seconds once he sends it.", 4f);
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
        if (keypad != null) keypad.FlashRejectCode();
        if (doorController != null) doorController.ShakeDoor();
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
            "In the real game, you'd email rodney@premiumdoorcodes.com and alt-tab back.", 5f);
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
    // Player Prop
    // =========================================================================

    private IEnumerator EquipFingerWhenPlayerReady()
    {
        if (fingerAnimatedPrefab == null)
        {
            Debug.LogWarning("[Level4] No Finger_Animated prefab assigned on Level4Manager.");
            yield break;
        }

        const float timeoutSeconds = 5f;
        float elapsed = 0f;

        while (elapsed < timeoutSeconds)
        {
            Transform attachPoint = GetFingerAttachPoint();
            if (attachPoint != null)
            {
                EquipFingerPrefab(attachPoint, fingerAnimatedPrefab);
                yield break;
            }

            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        Debug.LogWarning("[Level4] Could not find the player camera to equip Finger_Animated.");
    }

    private Transform GetFingerAttachPoint()
    {
        Camera mainCam = Camera.main;
        if (mainCam != null)
            return mainCam.transform;

        PlayerController playerController = FindAnyObjectByType<PlayerController>();
        if (playerController != null && playerController.cameraTransform != null)
            return playerController.cameraTransform;

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            Camera playerCamera = player.GetComponentInChildren<Camera>();
            if (playerCamera != null)
                return playerCamera.transform;
        }

        return null;
    }

    private void EquipFingerPrefab(Transform attachPoint, GameObject prefab)
    {
        Transform existing = attachPoint.Find("Finger_Animated_Equipped");
        if (existing != null)
        {
            equippedFingerInstance = existing.gameObject;
            CacheFingerAnimator();
            CacheFingerTipDetector();
            SetupFingerViewModelRendering(attachPoint);
            UpdateEquippedFingerTransform();
            return;
        }

        equippedFingerInstance = Instantiate(prefab, attachPoint);
        equippedFingerInstance.name = "Finger_Animated_Equipped";
        CacheFingerAnimator();
        CacheFingerTipDetector();
        SetupFingerViewModelRendering(attachPoint);
        UpdateEquippedFingerTransform();

        foreach (Collider col in equippedFingerInstance.GetComponentsInChildren<Collider>(true))
        {
            bool isFingerTipCollider = fingerTipDetector != null &&
                col.transform.IsChildOf(fingerTipDetector.transform);
            col.enabled = isFingerTipCollider;
        }

        foreach (Rigidbody rb in equippedFingerInstance.GetComponentsInChildren<Rigidbody>(true))
        {
            rb.isKinematic = true;
            rb.useGravity = false;
        }
    }

    private void CacheFingerAnimator()
    {
        equippedFingerAnimator = equippedFingerInstance != null
            ? equippedFingerInstance.GetComponentInChildren<Animator>(true)
            : null;
    }

    private void CacheFingerTipDetector()
    {
        fingerTipDetector = null;
        fingerTipTransform = null;
        fingerTipSphereCollider = null;
        if (equippedFingerInstance == null || string.IsNullOrWhiteSpace(fingerTipChildName))
            return;

        fingerTipTransform = FindNamedDescendant(equippedFingerInstance.transform, fingerTipChildName);
        if (fingerTipTransform == null)
        {
            Debug.LogWarning($"[Level4] Could not find fingertip child '{fingerTipChildName}' on equipped finger.");
            return;
        }

        fingerTipSphereCollider = fingerTipTransform.GetComponent<SphereCollider>();
        if (fingerTipSphereCollider == null)
            Debug.LogWarning($"[Level4] Fingertip child '{fingerTipChildName}' should have a SphereCollider for keypad overlap checks.");

        fingerTipDetector = fingerTipTransform.GetComponent<FingerTipKeypadDetector>();
        if (fingerTipDetector == null)
            fingerTipDetector = fingerTipTransform.gameObject.AddComponent<FingerTipKeypadDetector>();
    }

    private void SetupFingerViewModelRendering(Transform attachPoint)
    {
        if (equippedFingerInstance == null || attachPoint == null) return;

        Camera baseCamera = attachPoint.GetComponent<Camera>();
        if (baseCamera == null)
            baseCamera = attachPoint.GetComponentInChildren<Camera>();
        if (baseCamera == null)
            return;

        fingerBaseCamera = baseCamera;
        if (!fingerBaseCameraMaskCaptured)
        {
            fingerBaseCameraOriginalCullingMask = fingerBaseCamera.cullingMask;
            fingerBaseCameraMaskCaptured = true;
        }

        SetLayerRecursively(equippedFingerInstance, FingerViewModelLayer);
        if (fingerTipDetector != null)
            SetLayerRecursively(fingerTipDetector.gameObject, 0);
        fingerBaseCamera.cullingMask &= ~(1 << FingerViewModelLayer);

        if (fingerViewModelCamera == null)
        {
            Transform existingCamera = attachPoint.Find("FingerViewModelCamera");
            if (existingCamera != null)
                fingerViewModelCamera = existingCamera.GetComponent<Camera>();
        }

        if (fingerViewModelCamera == null)
        {
            GameObject cameraObject = new GameObject("FingerViewModelCamera");
            cameraObject.transform.SetParent(attachPoint, false);
            fingerViewModelCamera = cameraObject.AddComponent<Camera>();
        }

        fingerViewModelCamera.transform.localPosition = Vector3.zero;
        fingerViewModelCamera.transform.localRotation = Quaternion.identity;
        fingerViewModelCamera.nearClipPlane = 0.01f;
        fingerViewModelCamera.farClipPlane = 10f;
        fingerViewModelCamera.fieldOfView = fingerBaseCamera.fieldOfView;
        fingerViewModelCamera.cullingMask = 1 << FingerViewModelLayer;
        fingerViewModelCamera.allowHDR = fingerBaseCamera.allowHDR;
        fingerViewModelCamera.allowMSAA = fingerBaseCamera.allowMSAA;
        fingerViewModelCamera.enabled = true;

        UniversalAdditionalCameraData baseData = fingerBaseCamera.GetComponent<UniversalAdditionalCameraData>();
        UniversalAdditionalCameraData viewModelData = fingerViewModelCamera.GetComponent<UniversalAdditionalCameraData>();
        if (viewModelData == null)
            viewModelData = fingerViewModelCamera.gameObject.AddComponent<UniversalAdditionalCameraData>();

        if (baseData != null)
        {
            viewModelData.renderType = CameraRenderType.Overlay;
            if (!baseData.cameraStack.Contains(fingerViewModelCamera))
                baseData.cameraStack.Add(fingerViewModelCamera);
        }
        else
        {
            fingerViewModelCamera.clearFlags = CameraClearFlags.Depth;
            fingerViewModelCamera.depth = fingerBaseCamera.depth + 1f;
        }
    }

    private void CleanupFingerViewModelCamera()
    {
        if (fingerBaseCamera != null && fingerBaseCameraMaskCaptured)
            fingerBaseCamera.cullingMask = fingerBaseCameraOriginalCullingMask;

        if (fingerBaseCamera != null)
        {
            UniversalAdditionalCameraData baseData = fingerBaseCamera.GetComponent<UniversalAdditionalCameraData>();
            if (baseData != null && fingerViewModelCamera != null)
                baseData.cameraStack.Remove(fingerViewModelCamera);
        }

        if (fingerViewModelCamera != null)
            Destroy(fingerViewModelCamera.gameObject);

        fingerViewModelCamera = null;
        fingerBaseCamera = null;
        fingerBaseCameraMaskCaptured = false;
    }

    private void SetLayerRecursively(GameObject obj, int layer)
    {
        if (obj == null) return;

        obj.layer = layer;
        Transform[] children = obj.GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < children.Length; i++)
            children[i].gameObject.layer = layer;
    }

    private void UpdateEquippedFingerTransform()
    {
        if (equippedFingerInstance == null) return;

        equippedFingerInstance.transform.localPosition = fingerHeldLocalPosition;
        equippedFingerInstance.transform.localRotation = Quaternion.Euler(fingerHeldLocalRotation);
        equippedFingerInstance.transform.localScale = fingerHeldLocalScale;
    }

    private void HandleFingerClickAnimation()
    {
        if (!Input.GetMouseButtonDown(0)) return;

        WorldKeypadButton targetButton = hoveredKeypadButton != null
            ? hoveredKeypadButton
            : GetHoveredKeypadButton();

        if (equippedFingerAnimator != null && !string.IsNullOrWhiteSpace(fingerClickTriggerName))
        {
            equippedFingerAnimator.ResetTrigger(fingerClickTriggerName);
            equippedFingerAnimator.SetTrigger(fingerClickTriggerName);
        }

        if (keypad != null)
        {
            if (pendingFingerPressCoroutine != null)
                StopCoroutine(pendingFingerPressCoroutine);

            pendingFingerPressCoroutine = StartCoroutine(PressKeypadButtonAfterDelay(
                fingerKeypadPressDelay,
                targetButton));
        }
    }

    private IEnumerator PressKeypadButtonAfterDelay(float delay, WorldKeypadButton targetButton)
    {
        if (delay > 0f)
            yield return new WaitForSeconds(delay);

        if (targetButton != null && keypad != null)
            targetButton.Press(keypad);

        pendingFingerPressCoroutine = null;
    }

    private Vector3 GetFingerTipWorldCenter()
    {
        if (fingerTipTransform == null)
            return Vector3.zero;

        if (fingerTipSphereCollider != null)
            return fingerTipTransform.TransformPoint(fingerTipSphereCollider.center);

        return fingerTipTransform.position;
    }

    private WorldKeypadButton GetFingerTipTouchedButton()
    {
        if (fingerTipTransform == null || fingerTipSphereCollider == null)
        {
            if (debugFingerTipPressLogging)
                Debug.LogWarning("[Level4] FingerTip overlap skipped: fingertip transform or sphere collider missing.");
            return fingerTipDetector != null ? fingerTipDetector.CurrentTouchedButton : null;
        }

        Vector3 worldCenter = GetFingerTipWorldCenter();
        float maxScale = Mathf.Max(
            Mathf.Abs(fingerTipTransform.lossyScale.x),
            Mathf.Abs(fingerTipTransform.lossyScale.y),
            Mathf.Abs(fingerTipTransform.lossyScale.z));
        float worldRadius = fingerTipSphereCollider.radius * maxScale;

        Collider[] overlaps = Physics.OverlapSphere(
            worldCenter,
            worldRadius,
            ~0,
            QueryTriggerInteraction.Collide);

        if (debugFingerTipPressLogging)
            Debug.Log($"[Level4] FingerTip overlap check at {worldCenter} radius {worldRadius:F4}. Hits: {overlaps.Length}");

        float closestDistance = float.MaxValue;
        WorldKeypadButton closestButton = null;

        for (int i = 0; i < overlaps.Length; i++)
        {
            if (overlaps[i] != null && fingerTipTransform != null && overlaps[i].transform.IsChildOf(fingerTipTransform))
                continue;

            WorldKeypadButton button = overlaps[i].GetComponentInParent<WorldKeypadButton>();
            if (debugFingerTipPressLogging)
            {
                string buttonName = button != null ? button.name : "none";
                Debug.Log($"[Level4] FingerTip overlap hit '{overlaps[i].name}' -> button '{buttonName}'");
            }

            if (button == null) continue;
            if (physicalKeypadRoot != null && !button.transform.IsChildOf(physicalKeypadRoot)) continue;

            float distance = Vector3.Distance(worldCenter, overlaps[i].ClosestPoint(worldCenter));
            if (distance >= closestDistance) continue;

            closestDistance = distance;
            closestButton = button;
        }

        if (debugFingerTipPressLogging)
        {
            string result = closestButton != null ? closestButton.name : "none";
            Debug.Log($"[Level4] FingerTip selected keypad button: {result}");
        }

        return closestButton;
    }

    // =========================================================================
    // Door Opening
    // =========================================================================

    private IEnumerator DoorOpenSequence()
    {
        doorOpening = true;

        yield return new WaitForSeconds(0.5f);
        if (keypad != null) keypad.Close();

        if (doorController != null && doorController.doorPanel != null)
        {
            GameObject panel = doorController.doorPanel;
            panel.transform.SetParent(null);

            Rigidbody rb = panel.GetComponent<Rigidbody>();
            if (rb == null)
                rb = panel.AddComponent<Rigidbody>();

            rb.isKinematic = false;
            rb.mass = 40f;
            rb.useGravity = true;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode.Continuous;

            if (panel.GetComponent<Collider>() == null)
                panel.AddComponent<BoxCollider>();

            Vector3 topOfDoor = panel.transform.position + Vector3.up * 1.4f;
            Vector3 pushDir = -panel.transform.forward;
            rb.AddForceAtPosition(pushDir * 120f, topOfDoor, ForceMode.Impulse);

            yield return new WaitForSeconds(3f);
        }

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

