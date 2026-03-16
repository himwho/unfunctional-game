using System.Collections;
using System.Collections.Generic;
using NavKeypad;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

/// <summary>
/// LEVEL 5: Dumb NPC with an unnecessarily long conversation.
/// Press E near the NPC to start talking. Press E to advance each line.
/// The NPC says nothing useful but the player must exhaust all dialogue to proceed.
/// 
/// The NPC buries a randomly generated 4-digit door code somewhere deep in the
/// dialogue. The player must remember it, walk to the LEVEL_DOOR, and enter it
/// on the keypad to complete the level.
/// 
/// Builds its own dialogue HUD at runtime (same style as Level 3).
/// Attach to a root GameObject in the LEVEL5 scene.
/// </summary>
public class Level5_DumbNPC : LevelManager
{
    [Header("NPC")]
    public GameObject npcObject;
    public float interactRange = 0.5f;
    public string npcName = "Gorp";

    [Header("Walk-Away Detection")]
    public float maxDialogueRange = 15f;
    public string comeBackLine = "Hey, come back here!";
    public string startOverLine = "Let me start over...";

    [Header("NPC Rotation")]
    [Tooltip("How fast Gorp turns to face the player (degrees/sec).")]
    public float npcTurnSpeed = 90f;

    [Header("Typing Effect")]
    public float typingSpeed = 0.04f;
    public bool enableTypingEffect = true;

    [Header("Dialogue Lines")]
    public List<string> dialogueLines = new List<string>();

    [Header("Door / Keypad")]
    [Tooltip("DoorController on the LEVEL_DOOR prefab in this scene.")]
    public DoorController doorController;
    [Tooltip("How close the player needs to be to interact with door/keypad.")]
    public float doorInteractRange = 3f;

    [Header("Player Prop")]
    [Tooltip("Assign the Finger_Animated prefab here to equip it to the player in Level 5.")]
    public GameObject fingerAnimatedPrefab;
    [Tooltip("Local position of the equipped finger relative to the player camera.")]
    public Vector3 fingerHeldLocalPosition = new Vector3(0.28f, -0.4f, 0.6f);
    [Tooltip("Local rotation of the equipped finger relative to the player camera.")]
    public Vector3 fingerHeldLocalRotation = new Vector3(8f, 90f, 1f);
    [Tooltip("Local scale of the equipped finger relative to the player camera.")]
    public Vector3 fingerHeldLocalScale = new Vector3(0.7f, 0.7f, 0.7f);
    [Tooltip("Animator trigger fired when the player left-clicks.")]
    public string fingerClickTriggerName = "Straighten";
    [Tooltip("Delay before a keypad button press is registered, so it lines up with the finger reaching full extension.")]
    public float fingerKeypadPressDelay = 0.5f;

    [Header("Physical Keypad")]
    public float keypadButtonAimRadius = 0.08f;
    [Tooltip("Horizontal screen-space offset in pixels used when aiming keypad buttons. Positive values aim farther right.")]
    public float keypadAimOffsetX = 190f;
    [Tooltip("Vertical screen-space offset in pixels used when aiming keypad buttons. Positive values aim up, negative values aim down.")]
    public float keypadAimOffsetY = -320f;

    [Header("Spawn Light Flicker")]
    [Tooltip("If enabled, the named ceiling fixtures flicker once when the player spawns into Level 5.")]
    public bool playSpawnLightFlicker = true;
    [Tooltip("Parent object names for the ceiling fixtures that should flicker on spawn.")]
    public string[] spawnFlickerFixtureNames = { "Ceiling Light 1", "Ceiling Light 2" };
    [Tooltip("Small delay before the spawn flicker starts so it is visible after the level fades in.")]
    public float spawnLightFlickerStartDelay = 0.6f;
    [Tooltip("How long the startup flicker sequence lasts.")]
    public float spawnLightFlickerDuration = 3f;
    [Tooltip("Random blackout duration range used during the flicker.")]
    public Vector2 spawnLightOffTimeRange = new Vector2(0.04f, 0.16f);
    [Tooltip("Random lit duration range used during the flicker.")]
    public Vector2 spawnLightOnTimeRange = new Vector2(0.03f, 0.12f);

    // Runtime UI references (built in code)
    private Canvas dialogueCanvas;
    private Text npcNameText;
    private Text dialogueText;
    private Text promptText;
    private CanvasGroup dialogueCanvasGroup;
    private Text narrationText;
    private CanvasGroup narrationCanvasGroup;

    // Interact prompt (shown when near NPC but not yet talking)
    private Canvas interactPromptCanvas;
    private Text interactPromptText;

    // Door interaction HUD (crosshair + prompt)
    private Canvas doorHudCanvas;
    private Text doorInteractPromptText;

    private const int IDLE_ANIM_COUNT = 7;
    private const int FingerViewModelLayer = 30;

    private Animator npcAnimator;
    private int currentLine = 0;
    private bool inDialogue = false;
    private bool isTyping = false;
    private bool waitingForInput = false;
    private float inputCooldown = 0f;
    private Coroutine typingCoroutine;
    private bool wasPlayerNear = false;
    private bool isReversing = false;
    private bool npcReadyToInteract = false;
    private Coroutine reverseCoroutine;
    private bool playerTooFar = false;

    // Door code
    private string generatedCode = "";
    private KeypadController keypad;
    private bool doorOpening = false;
    private bool dialogueCompleted = false;
    private Coroutine pendingWrongCodeHintCoroutine;
    private Coroutine narrationFadeCoroutine;
    private GameObject equippedFingerInstance;
    private Animator equippedFingerAnimator;
    private WorldKeypadButton hoveredKeypadButton;
    private Transform physicalKeypadRoot;
    private Coroutine pendingFingerPressCoroutine;
    private Coroutine spawnLightFlickerCoroutine;
    private Camera fingerViewModelCamera;
    private Camera fingerBaseCamera;
    private int fingerBaseCameraOriginalCullingMask;
    private bool fingerBaseCameraMaskCaptured;


    // Base font sizes (set during HUD creation, used for distance scaling)
    private int baseFontSizeDialogue;
    private int baseFontSizeName;
    private int baseFontSizePrompt;

    // RectTransforms & base anchors for distance-based layout collapsing
    private RectTransform nameRect;
    private RectTransform dialogueRect;
    private RectTransform promptRect;
    private Vector2 baseNameAnchorMin, baseNameAnchorMax;
    private Vector2 baseDialogueAnchorMin, baseDialogueAnchorMax;
    private Vector2 basePromptAnchorMin, basePromptAnchorMax;
    private float anchorCenterY;

    protected override void Start()
    {
        base.Start();
        levelDisplayName = "NPC Conversation";
        levelDescription = "Talk to the NPC. All of it.";

        if (npcObject != null)
        {
            npcAnimator = npcObject.GetComponentInChildren<Animator>();
            EnsureNpcCollider();
        }

        // Generate a random 4-digit code for this level load
        generatedCode = Random.Range(1000, 10000).ToString();
        Debug.Log($"[Level5] Generated door code: {generatedCode}");

        // Find DoorController if not assigned
        if (doorController == null)
            doorController = FindAnyObjectByType<DoorController>();

        // Wire up the keypad
        if (doorController != null)
        {
            keypad = doorController.keypadController;
            if (keypad == null)
                keypad = FindAnyObjectByType<KeypadController>();

            if (keypad != null)
            {
                keypad.codeLength = 4;
                keypad.keypadTitle = "DOOR ACCESS KEYPAD";
                keypad.hintText = "Ask GORP!";
                keypad.showRequestCodeButton = false;

                keypad.OnCodeSubmitted += HandleCodeSubmitted;
                SetupPhysicalKeypad();
            }
        }

        if (dialogueLines.Count == 0)
            BuildDefaultDialogue();

        CreateDialogueHUD();
        CreateInteractPrompt();
        CreateDoorHUD();
        StartCoroutine(EquipFingerWhenPlayerReady());
        spawnLightFlickerCoroutine = StartCoroutine(PlaySpawnLightFlickerWhenPlayerReady());
        ShowNarration("A padded room...creepy. Who is that at the end?", 4f);

        baseFontSizeDialogue = dialogueText.fontSize;
        baseFontSizeName = npcNameText.fontSize;
        baseFontSizePrompt = promptText.fontSize;

        nameRect     = npcNameText.GetComponent<RectTransform>();
        dialogueRect = dialogueText.GetComponent<RectTransform>();
        promptRect   = promptText.GetComponent<RectTransform>();

        baseNameAnchorMin     = nameRect.anchorMin;
        baseNameAnchorMax     = nameRect.anchorMax;
        baseDialogueAnchorMin = dialogueRect.anchorMin;
        baseDialogueAnchorMax = dialogueRect.anchorMax;
        basePromptAnchorMin   = promptRect.anchorMin;
        basePromptAnchorMax   = promptRect.anchorMax;

        // Vertical center of the whole dialogue block (prompt bottom to name top)
        anchorCenterY = (basePromptAnchorMin.y + baseNameAnchorMax.y) * 0.5f;

        dialogueCanvas.gameObject.SetActive(false);
        interactPromptCanvas.gameObject.SetActive(false);
    }

    protected override void OnDestroy()
    {
        if (keypad != null)
        {
            keypad.OnCodeSubmitted -= HandleCodeSubmitted;
            keypad.UnregisterExternalDisplay();
        }

        if (equippedFingerInstance != null)
            Destroy(equippedFingerInstance);

        if (spawnLightFlickerCoroutine != null)
            StopCoroutine(spawnLightFlickerCoroutine);

        CleanupFingerViewModelCamera();
        base.OnDestroy();
    }

    private void Update()
    {
        if (levelComplete || doorOpening) return;

        UpdateEquippedFingerTransform();
        hoveredKeypadButton = GetHoveredKeypadButton();
        HandleFingerClickAnimation();

        if (inputCooldown > 0f)
            inputCooldown -= Time.deltaTime;

        bool ePressed = Input.GetKeyDown(KeyCode.E);

        // Always run NPC animation/proximity logic (stand up, reverse, etc.)
        // but only when not actively in dialogue
        if (!inDialogue)
            UpdateNPCProximity();

        // Smoothly rotate Gorp to face the player whenever they're nearby
        RotateNPCTowardsPlayer();

        if (inDialogue)
        {
            // In dialogue -- only advance lines
            if (ePressed && waitingForInput && !isTyping && !playerTooFar && inputCooldown <= 0f)
            {
                AdvanceDialogue();
            }

            CheckPlayerDistance();
            UpdateDialogueFontSize();
        }
        else
        {
            // Free roam -- use gaze raycast for all interactions
            UpdateGazeInteraction(ePressed);
        }
    }

    /// <summary>
    /// Handles NPC stand-up/reverse animations based on player proximity.
    /// Does NOT manage the interact prompt -- that is handled by gaze logic.
    /// </summary>
    private void UpdateNPCProximity()
    {
        bool nearNpc = IsPlayerNearNPC();

        if (npcAnimator != null)
        {
            if (wasPlayerNear && !nearNpc && !isReversing)
            {
                isReversing = true;
                npcReadyToInteract = false;
                npcAnimator.SetFloat("AnimSpeed", 1f);
                npcAnimator.SetTrigger("Reverse");
                reverseCoroutine = StartCoroutine(ResetAnimatorAfterReverse());
            }
            else if (isReversing && nearNpc)
            {
                if (reverseCoroutine != null)
                    StopCoroutine(reverseCoroutine);
                reverseCoroutine = null;
                isReversing = false;
                npcAnimator.ResetTrigger("Reverse");
                npcAnimator.Play("idle 3", 0);
                npcReadyToInteract = true;
            }
            else if (!isReversing)
            {
                npcAnimator.SetFloat("AnimSpeed", nearNpc ? 1f : 0f);

                if (nearNpc && !wasPlayerNear && !npcReadyToInteract)
                {
                    StartCoroutine(WaitForStandUpAnimation());
                }
            }
        }
        else
        {
            npcReadyToInteract = nearNpc;
        }

        wasPlayerNear = nearNpc;
    }

    // =========================================================================
    // NPC Rotation (face the player)
    // =========================================================================

    /// <summary>
    /// Smoothly rotates Gorp on the Y axis to face the player camera whenever
    /// the player is within interact range. Only rotates horizontally so Gorp
    /// doesn't tilt up/down.
    /// </summary>
    private void RotateNPCTowardsPlayer()
    {
        if (npcObject == null) return;

        Camera cam = Camera.main;
        if (cam == null) return;

        float dist = Vector3.Distance(cam.transform.position, npcObject.transform.position);
        if (dist > interactRange) return;

        // Direction from NPC to player, flattened to horizontal plane
        Vector3 dirToPlayer = cam.transform.position - npcObject.transform.position;
        dirToPlayer.y = 0f;

        if (dirToPlayer.sqrMagnitude < 0.001f) return;

        Quaternion targetRot = Quaternion.LookRotation(dirToPlayer);
        npcObject.transform.rotation = Quaternion.RotateTowards(
            npcObject.transform.rotation,
            targetRot,
            npcTurnSpeed * Time.deltaTime
        );
    }

    // =========================================================================
    // Gaze-Based Interaction (raycast determines what the player looks at)
    // =========================================================================

    /// <summary>
    /// Single unified gaze system. Casts a ray from screen center and determines
    /// what the player is looking at: NPC or door. Shows the appropriate
    /// prompt and handles E-press interaction.
    ///
    /// Before dialogue is completed: NPC is interactable (if near and ready).
    /// After dialogue is completed: door and keypad become interactable; NPC can
    ///   optionally be re-talked to (restarts dialogue to hear the code again).
    /// </summary>
    private void UpdateGazeInteraction(bool ePressed)
    {
        Camera cam = Camera.main;
        if (cam == null)
        {
            interactPromptCanvas.gameObject.SetActive(false);
            doorInteractPromptText.enabled = false;
            return;
        }

        Ray ray = cam.ScreenPointToRay(new Vector3(Screen.width / 2f, Screen.height / 2f, 0f));
        float maxRange = Mathf.Max(interactRange, doorInteractRange);

        // RaycastAll so we can detect the door even if multiple colliders overlap.
        RaycastHit[] hits = Physics.RaycastAll(ray, maxRange, ~0, QueryTriggerInteraction.Collide);

        GazeTarget target = GazeTarget.None;

        bool foundDoor = false;
        bool foundNPC = false;

        foreach (var hit in hits)
        {
            if (!foundDoor && hit.distance <= doorInteractRange && IsHitOnDoor(hit))
                foundDoor = true;
            if (!foundNPC && hit.distance <= interactRange && IsHitOnNPC(hit))
                foundNPC = true;
        }

        if (foundDoor)
            target = GazeTarget.Door;
        else if (foundNPC)
            target = GazeTarget.NPC;

        // Update prompts
        switch (target)
        {
            case GazeTarget.NPC:
                interactPromptCanvas.gameObject.SetActive(npcReadyToInteract);
                interactPromptText.text = dialogueCompleted
                    ? "Press [E] to talk again"
                    : "Press [E] to interact";
                doorInteractPromptText.enabled = false;
                break;

            case GazeTarget.Door:
                interactPromptCanvas.gameObject.SetActive(false);
                doorInteractPromptText.enabled = true;
                doorInteractPromptText.text = "[E] Try Door";
                break;

            default:
                interactPromptCanvas.gameObject.SetActive(false);
                doorInteractPromptText.enabled = false;
                break;
        }

        // Handle E press
        if (ePressed)
        {
            switch (target)
            {
                case GazeTarget.NPC:
                    if (npcReadyToInteract)
                        TryStartDialogue();
                    break;

                case GazeTarget.Door:
                    if (doorController != null)
                    {
                        doorController.ShakeDoor();
                    }
                    break;
            }
        }
    }

    private enum GazeTarget { None, NPC, Door }

    private bool IsHitOnNPC(RaycastHit hit)
    {
        if (npcObject == null) return false;
        return hit.collider.transform.IsChildOf(npcObject.transform)
            || hit.collider.gameObject == npcObject;
    }

    private IEnumerator ResetAnimatorAfterReverse()
    {
        yield return null;

        while (true)
        {
            AnimatorStateInfo state = npcAnimator.GetCurrentAnimatorStateInfo(0);
            if (npcAnimator.IsInTransition(0))
            {
                yield return null;
                continue;
            }
            if (state.normalizedTime < 1f)
            {
                yield return null;
                continue;
            }
            break;
        }

        isReversing = false;
        npcAnimator.Rebind();
        npcAnimator.Update(0f);
    }

    private IEnumerator WaitForStandUpAnimation()
    {
        yield return new WaitForSeconds(3f);

        if (IsPlayerNearNPC() && !isReversing)
            npcReadyToInteract = true;
    }

    private bool IsPlayerNearNPC()
    {
        Camera cam = Camera.main;
        if (cam == null || npcObject == null) return false;

        float dist = Vector3.Distance(cam.transform.position, npcObject.transform.position);
        return dist <= interactRange;
    }

    // =========================================================================
    // Hit Detection Helpers (used by gaze interaction)
    // =========================================================================

    private bool IsHitOnKeypad(RaycastHit hit)
    {
        WorldKeypadButton worldButton = hit.collider.GetComponentInParent<WorldKeypadButton>();
        if (worldButton != null)
            return true;

        if (physicalKeypadRoot != null && hit.collider.transform.IsChildOf(physicalKeypadRoot))
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
        if (IsHitOnKeypad(hit)) return false;
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
            Debug.LogWarning("[Level5] Could not resolve the physical keypad root.");
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
            Debug.LogWarning("[Level5] No world-space keypad display canvas found.");
            return;
        }

        TMP_Text legacyDisplayText = null;
        Transform legacyDisplayTextTransform = FindNamedDescendant(displayCanvas.transform, "DisplayText");
        if (legacyDisplayTextTransform != null)
            legacyDisplayText = legacyDisplayTextTransform.GetComponent<TMP_Text>();

        if (legacyDisplayText != null)
            keypad.RegisterExternalDisplay(legacyDisplayText);

        Font font = UIHelper.GetDefaultFont();
        Text statusDisplay = GetOrCreateWorldDisplayText(
            displayCanvas.transform,
            "Level5StatusDisplay",
            font,
            12,
            new Color(0.8f, 0.8f, 0.8f),
            new Vector2(0.02f, 0.02f),
            new Vector2(0.98f, 0.18f),
            TextAnchor.MiddleCenter);

        keypad.RegisterExternalDisplay(null, null, statusDisplay);
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
            Debug.LogWarning($"[Level5] Keypad button '{buttonName}' not found.");
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
            Debug.LogWarning($"[Level5] Keypad button '{buttonName}' not found.");
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

        float maxDistance = doorInteractRange;
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
    // Keypad Code Handling
    // =========================================================================

    private void HandleCodeSubmitted(string code)
    {
        if (code == generatedCode)
        {
            keypad.AcceptCode("ACCESS GRANTED");
            StartCoroutine(OpenDoorSequence());
        }
        else
        {
            keypad.FlashRejectCode();
            if (doorController != null) doorController.ShakeDoor();

            if (!dialogueCompleted)
            {
                if (pendingWrongCodeHintCoroutine != null)
                    StopCoroutine(pendingWrongCodeHintCoroutine);

                pendingWrongCodeHintCoroutine = StartCoroutine(ShowWrongCodeHintAfterRejectFlash());
            }
        }
    }

    private IEnumerator ShowWrongCodeHintAfterRejectFlash()
    {
        yield return new WaitForSeconds(0.36f);

        if (!dialogueCompleted && keypad != null)
        {
            keypad.SetStatus("Maybe talk to " + npcName + " first?", new Color(1f, 0.8f, 0.3f));
            ShowNarration("Maybe talk to " + npcName + " first?", 3f);
        }

        pendingWrongCodeHintCoroutine = null;
    }

    private IEnumerator OpenDoorSequence()
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

            ShowNarration("Well done. Gorp was useful after all.", 3f);
            yield return new WaitForSeconds(2f);
        }
        CompleteLevel();
    }

    // =========================================================================
    // Finger / physical keypad interaction
    // =========================================================================

    private IEnumerator EquipFingerWhenPlayerReady()
    {
        if (fingerAnimatedPrefab == null)
        {
            Debug.LogWarning("[Level5] No Finger_Animated prefab assigned on Level5 manager.");
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

        Debug.LogWarning("[Level5] Could not find the player camera to equip Finger_Animated.");
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
            SetupFingerViewModelRendering(attachPoint);
            UpdateEquippedFingerTransform();
            return;
        }

        equippedFingerInstance = Instantiate(prefab, attachPoint);
        equippedFingerInstance.name = "Finger_Animated_Equipped";
        CacheFingerAnimator();
        SetupFingerViewModelRendering(attachPoint);
        UpdateEquippedFingerTransform();

        foreach (Collider col in equippedFingerInstance.GetComponentsInChildren<Collider>(true))
            col.enabled = false;

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

    // =========================================================================
    // Dialogue — Walk-Away Detection
    // =========================================================================

    /// <summary>
    /// During dialogue, checks if the player has wandered too far from the NPC.
    /// If so, interrupts the current line and shows a "come back" message.
    /// When the player returns, the interrupted line replays from the start.
    /// </summary>
    private void CheckPlayerDistance()
    {
        Camera cam = Camera.main;
        if (cam == null || npcObject == null) return;

        float dist = Vector3.Distance(cam.transform.position, npcObject.transform.position);

        if (!playerTooFar && dist > maxDialogueRange)
        {
            playerTooFar = true;

            if (typingCoroutine != null)
                StopCoroutine(typingCoroutine);
            isTyping = false;
            waitingForInput = false;

            npcNameText.text = npcName;
            promptText.gameObject.SetActive(false);

            if (enableTypingEffect)
            {
                typingCoroutine = StartCoroutine(TypeComeBackLine());
            }
            else
            {
                dialogueText.text = comeBackLine;
                promptText.gameObject.SetActive(true);
                promptText.text = "(walk back to " + npcName + ")";
            }
        }
        else if (playerTooFar && dist <= maxDialogueRange)
        {
            playerTooFar = false;
            currentLine = 0;
            StartCoroutine(ShowStartOverThenResume());
        }
    }

    private IEnumerator TypeComeBackLine()
    {
        isTyping = true;
        dialogueText.text = "";
        foreach (char c in comeBackLine)
        {
            dialogueText.text += c;
            yield return new WaitForSeconds(typingSpeed);
        }
        isTyping = false;
        promptText.gameObject.SetActive(true);
        promptText.text = "(walk back to " + npcName + ")";
    }

    private IEnumerator ShowStartOverThenResume()
    {
        waitingForInput = false;
        promptText.gameObject.SetActive(false);

        if (enableTypingEffect)
        {
            isTyping = true;
            dialogueText.text = "";
            foreach (char c in startOverLine)
            {
                dialogueText.text += c;
                yield return new WaitForSeconds(typingSpeed);
            }
            isTyping = false;
        }
        else
        {
            dialogueText.text = startOverLine;
        }

        yield return new WaitForSeconds(1.5f);

        ShowCurrentLine();
    }

    private void TryStartDialogue()
    {
        if (IsPlayerNearNPC())
        {
            StartDialogue();
        }
    }

    private void StartDialogue()
    {
        inDialogue = true;
        currentLine = 0;
        playerTooFar = false;

        // Reset font sizes and layout to full when starting a fresh conversation
        dialogueText.fontSize = baseFontSizeDialogue;
        npcNameText.fontSize  = baseFontSizeName;
        promptText.fontSize   = baseFontSizePrompt;

        nameRect.anchorMin     = baseNameAnchorMin;
        nameRect.anchorMax     = baseNameAnchorMax;
        dialogueRect.anchorMin = baseDialogueAnchorMin;
        dialogueRect.anchorMax = baseDialogueAnchorMax;
        promptRect.anchorMin   = basePromptAnchorMin;
        promptRect.anchorMax   = basePromptAnchorMax;

        interactPromptCanvas.gameObject.SetActive(false);
        dialogueCanvas.gameObject.SetActive(true);

        ShowCurrentLine();
    }

    private void ShowCurrentLine()
    {
        if (currentLine >= dialogueLines.Count)
        {
            EndDialogue();
            return;
        }

        waitingForInput = false;
        isTyping = false;

        if (npcAnimator != null)
        {
            int current = npcAnimator.GetInteger("IdleIndex");
            int next = Random.Range(0, IDLE_ANIM_COUNT - 1);
            if (next >= current) next++;
            npcAnimator.SetInteger("IdleIndex", next);
        }

        npcNameText.text = npcName;
        promptText.gameObject.SetActive(false);

        string line = dialogueLines[currentLine];

        if (enableTypingEffect)
        {
            if (typingCoroutine != null)
                StopCoroutine(typingCoroutine);
            typingCoroutine = StartCoroutine(TypeText(line));
        }
        else
        {
            dialogueText.text = line;
            OnLineFinished();
        }
    }

    private IEnumerator TypeText(string text)
    {
        isTyping = true;
        dialogueText.text = "";

        foreach (char c in text)
        {
            dialogueText.text += c;
            yield return new WaitForSeconds(typingSpeed);
        }

        isTyping = false;
        OnLineFinished();
    }

    private void OnLineFinished()
    {
        waitingForInput = true;
        inputCooldown = 0.15f;
        promptText.gameObject.SetActive(true);
        promptText.text = currentLine < dialogueLines.Count - 1
            ? "[E] Continue"
            : "[E] End";
    }

    private void AdvanceDialogue()
    {
        currentLine++;
        ShowCurrentLine();
    }

    private void EndDialogue()
    {
        inDialogue = false;
        waitingForInput = false;
        dialogueCompleted = true;

        dialogueCanvas.gameObject.SetActive(false);

        Debug.Log($"[Level5] Dialogue ended after {dialogueLines.Count} lines. Code was: {generatedCode}");
    }

    // =========================================================================
    // Distance-based Font Scaling
    // =========================================================================

    /// <summary>
    /// Shrinks all dialogue font sizes as the player walks away from the NPC.
    /// At interact range or closer the text is full-size; beyond that it falls
    /// off proportionally so the conversation becomes unreadable from a distance.
    /// </summary>
    private void UpdateDialogueFontSize()
    {
        Camera cam = Camera.main;
        if (cam == null || npcObject == null) return;

        float dist = Vector3.Distance(cam.transform.position, npcObject.transform.position);

        // Within interact range: full size. Beyond: shrinks with inverse distance.
        float scale = dist <= interactRange ? 1f : interactRange / dist;

        dialogueText.fontSize = Mathf.Max(1, Mathf.RoundToInt(baseFontSizeDialogue * scale));
        npcNameText.fontSize  = Mathf.Max(1, Mathf.RoundToInt(baseFontSizeName * scale));
        promptText.fontSize   = Mathf.Max(1, Mathf.RoundToInt(baseFontSizePrompt * scale));

        // Collapse element anchors towards the vertical center so spacing shrinks too
        nameRect.anchorMin     = CollapseAnchor(baseNameAnchorMin, scale);
        nameRect.anchorMax     = CollapseAnchor(baseNameAnchorMax, scale);
        dialogueRect.anchorMin = CollapseAnchor(baseDialogueAnchorMin, scale);
        dialogueRect.anchorMax = CollapseAnchor(baseDialogueAnchorMax, scale);
        promptRect.anchorMin   = CollapseAnchor(basePromptAnchorMin, scale);
        promptRect.anchorMax   = CollapseAnchor(basePromptAnchorMax, scale);
    }

    /// <summary>
    /// Lerps an anchor's Y component towards the shared vertical center.
    /// At scale 1 the anchor is unchanged; as scale approaches 0 everything
    /// converges to a single line so the gaps between elements disappear.
    /// </summary>
    private Vector2 CollapseAnchor(Vector2 baseAnchor, float scale)
    {
        return new Vector2(
            baseAnchor.x,
            Mathf.Lerp(anchorCenterY, baseAnchor.y, scale)
        );
    }

    // =========================================================================
    // NPC Collision
    // =========================================================================

    /// <summary>
    /// Ensures the NPC has a collider so the player's CharacterController
    /// cannot walk through it.
    /// </summary>
    private void EnsureNpcCollider()
    {
        Collider existingCollider = npcObject.GetComponentInChildren<Collider>();
        if (existingCollider != null && !existingCollider.isTrigger)
            return;

        CapsuleCollider col = npcObject.AddComponent<CapsuleCollider>();
        col.center = new Vector3(0f, 1f, 0f);
        col.radius = 0.5f;
        col.height = 2f;
    }

    // =========================================================================
    // Spawn light flicker
    // =========================================================================

    private sealed class SpawnFlickerLightState
    {
        public Light light;
        public bool wasEnabled;
        public float originalIntensity;
        public GameObject fixtureRoot;
        public bool fixtureWasActive;
        public GameObject emissiveVisual;
        public bool emissiveWasActive;
    }

    private IEnumerator PlaySpawnLightFlickerWhenPlayerReady()
    {
        if (!playSpawnLightFlicker)
            yield break;

        const float timeoutSeconds = 8f;
        float elapsed = 0f;
        GameObject player = null;

        while (elapsed < timeoutSeconds)
        {
            player = GameManager.Instance != null
                ? GameManager.Instance.CurrentPlayer
                : GameObject.Find("Player");

            if (player != null)
                break;

            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        if (player == null)
        {
            spawnLightFlickerCoroutine = null;
            yield break;
        }

        List<SpawnFlickerLightState> flickerLights = ResolveSpawnFlickerLights();
        if (flickerLights.Count == 0)
        {
            Debug.LogWarning("[Level5] Spawn light flicker could not find Ceiling Light 1 / Ceiling Light 2.");
            yield break;
        }

        SetSpawnFlickerLightsInstantIntensity(flickerLights, 0f);
        SetSpawnFlickerEmissiveVisible(flickerLights, false);
        SetSpawnFlickerFixtureVisible(flickerLights, false);

        if (spawnLightFlickerStartDelay > 0f)
            yield return new WaitForSeconds(spawnLightFlickerStartDelay);

        float flickerDuration = Mathf.Max(0.1f, spawnLightFlickerDuration);
        float flickerTime = 0f;

        while (flickerTime < flickerDuration)
        {
            bool lightsOn = Random.value > 0.3f;
            float fadeTime = lightsOn
                ? GetRandomPositiveDuration(spawnLightOnTimeRange, 0.06f)
                : GetRandomPositiveDuration(spawnLightOffTimeRange, 0.1f);

            flickerTime += fadeTime;
            yield return StartCoroutine(FadeSpawnFlickerLights(flickerLights, lightsOn, fadeTime));
        }

        RestoreSpawnFlickerLights(flickerLights);
        spawnLightFlickerCoroutine = null;
    }

    private List<SpawnFlickerLightState> ResolveSpawnFlickerLights()
    {
        List<SpawnFlickerLightState> results = new List<SpawnFlickerLightState>();
        HashSet<Light> seenLights = new HashSet<Light>();

        if (spawnFlickerFixtureNames == null || spawnFlickerFixtureNames.Length == 0)
            return results;

        GameObject[] roots = gameObject.scene.GetRootGameObjects();
        for (int i = 0; i < spawnFlickerFixtureNames.Length; i++)
        {
            string fixtureName = spawnFlickerFixtureNames[i];
            if (string.IsNullOrWhiteSpace(fixtureName))
                continue;

            Transform fixtureRoot = null;
            for (int rootIndex = 0; rootIndex < roots.Length; rootIndex++)
            {
                fixtureRoot = FindNamedDescendant(roots[rootIndex].transform, fixtureName);
                if (fixtureRoot != null)
                    break;
            }

            if (fixtureRoot == null)
                continue;

            Transform emissiveVisual = FindNamedDescendant(fixtureRoot, "light_ON");
            Light[] childLights = fixtureRoot.GetComponentsInChildren<Light>(true);
            for (int lightIndex = 0; lightIndex < childLights.Length; lightIndex++)
            {
                Light childLight = childLights[lightIndex];
                if (childLight == null || seenLights.Contains(childLight))
                    continue;

                if (childLight.type != LightType.Point && childLight.type != LightType.Spot)
                    continue;

                seenLights.Add(childLight);
                results.Add(new SpawnFlickerLightState
                {
                    light = childLight,
                    wasEnabled = childLight.enabled,
                    originalIntensity = childLight.intensity,
                    fixtureRoot = fixtureRoot.gameObject,
                    fixtureWasActive = fixtureRoot.gameObject.activeSelf,
                    emissiveVisual = emissiveVisual != null ? emissiveVisual.gameObject : null,
                    emissiveWasActive = emissiveVisual != null && emissiveVisual.gameObject.activeSelf
                });
            }
        }

        return results;
    }

    private IEnumerator FadeSpawnFlickerLights(List<SpawnFlickerLightState> flickerLights, bool lightsOn, float duration)
    {
        float safeDuration = Mathf.Max(0.01f, duration);
        float[] startIntensities = new float[flickerLights.Count];

        if (lightsOn)
            SetSpawnFlickerFixtureVisible(flickerLights, true);

        SetSpawnFlickerEmissiveVisible(flickerLights, lightsOn);

        for (int i = 0; i < flickerLights.Count; i++)
        {
            SpawnFlickerLightState lightState = flickerLights[i];
            if (lightState.light == null)
                continue;

            startIntensities[i] = lightState.light.intensity;

            if (lightState.wasEnabled)
                lightState.light.enabled = true;
        }

        float elapsed = 0f;
        while (elapsed < safeDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / safeDuration);

            for (int i = 0; i < flickerLights.Count; i++)
            {
                SpawnFlickerLightState lightState = flickerLights[i];
                if (lightState.light == null || !lightState.wasEnabled)
                    continue;

                float targetIntensity = lightsOn ? lightState.originalIntensity : 0f;
                lightState.light.intensity = Mathf.Lerp(startIntensities[i], targetIntensity, t);
            }

            yield return null;
        }

        if (!lightsOn)
            SetSpawnFlickerFixtureVisible(flickerLights, false);
    }

    private void SetSpawnFlickerEmissiveVisible(List<SpawnFlickerLightState> flickerLights, bool visible)
    {
        for (int i = 0; i < flickerLights.Count; i++)
        {
            SpawnFlickerLightState lightState = flickerLights[i];
            if (lightState.emissiveVisual == null)
                continue;

            lightState.emissiveVisual.SetActive(lightState.emissiveWasActive && visible);
        }
    }

    private void SetSpawnFlickerFixtureVisible(List<SpawnFlickerLightState> flickerLights, bool visible)
    {
        HashSet<GameObject> seenFixtures = new HashSet<GameObject>();

        for (int i = 0; i < flickerLights.Count; i++)
        {
            SpawnFlickerLightState lightState = flickerLights[i];
            if (lightState.fixtureRoot == null || seenFixtures.Contains(lightState.fixtureRoot))
                continue;

            seenFixtures.Add(lightState.fixtureRoot);
            lightState.fixtureRoot.SetActive(lightState.fixtureWasActive && visible);
        }
    }

    private void SetSpawnFlickerLightsInstantIntensity(List<SpawnFlickerLightState> flickerLights, float intensity)
    {
        float clampedIntensity = Mathf.Max(0f, intensity);

        for (int i = 0; i < flickerLights.Count; i++)
        {
            SpawnFlickerLightState lightState = flickerLights[i];
            if (lightState.light == null)
                continue;

            lightState.light.enabled = lightState.wasEnabled;

            if (lightState.wasEnabled)
                lightState.light.intensity = clampedIntensity;
        }
    }

    private void RestoreSpawnFlickerLights(List<SpawnFlickerLightState> flickerLights)
    {
        for (int i = 0; i < flickerLights.Count; i++)
        {
            SpawnFlickerLightState lightState = flickerLights[i];
            if (lightState.fixtureRoot != null)
                lightState.fixtureRoot.SetActive(lightState.fixtureWasActive);

            if (lightState.light == null)
                continue;

            lightState.light.enabled = lightState.wasEnabled;

            if (lightState.wasEnabled)
                lightState.light.intensity = lightState.originalIntensity;

            if (lightState.emissiveVisual != null)
                lightState.emissiveVisual.SetActive(lightState.emissiveWasActive);
        }
    }

    private float GetRandomPositiveDuration(Vector2 range, float fallbackValue)
    {
        float min = Mathf.Min(range.x, range.y);
        float max = Mathf.Max(range.x, range.y);

        if (max <= 0f)
            return Mathf.Max(0.01f, fallbackValue);

        if (Mathf.Approximately(min, max))
            return Mathf.Max(0.01f, min);

        return Mathf.Max(0.01f, Random.Range(min, max));
    }

    // =========================================================================
    // Interact Prompt (shown when near NPC, before dialogue starts)
    // =========================================================================

    private void CreateInteractPrompt()
    {
        GameObject canvasObj = new GameObject("InteractPromptHUD");
        canvasObj.transform.SetParent(transform);
        interactPromptCanvas = canvasObj.AddComponent<Canvas>();
        UIHelper.ConfigureCanvas(interactPromptCanvas, sortingOrder: 20);

        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);

        canvasObj.AddComponent<GraphicRaycaster>();

        GameObject textObj = new GameObject("InteractPromptText");
        textObj.transform.SetParent(canvasObj.transform, false);

        interactPromptText = textObj.AddComponent<Text>();
        interactPromptText.font = UIHelper.GetDefaultFont();
        interactPromptText.fontSize = 24;
        interactPromptText.fontStyle = FontStyle.BoldAndItalic;
        interactPromptText.alignment = TextAnchor.MiddleCenter;
        interactPromptText.color = new Color(0.8f, 0.8f, 0.5f, 1f);
        interactPromptText.raycastTarget = false;
        interactPromptText.text = "Press [E] to interact";

        RectTransform rect = textObj.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.3f, 0.45f);
        rect.anchorMax = new Vector2(0.7f, 0.55f);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    // =========================================================================
    // Door Interaction HUD (interact prompt for door)
    // =========================================================================

    private void CreateDoorHUD()
    {
        GameObject canvasObj = new GameObject("DoorHUD");
        canvasObj.transform.SetParent(transform);
        doorHudCanvas = canvasObj.AddComponent<Canvas>();
        UIHelper.ConfigureCanvas(doorHudCanvas, sortingOrder: 15);

        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);

        canvasObj.AddComponent<GraphicRaycaster>();

        // Door interact prompt text
        GameObject promptObj = new GameObject("DoorPromptText");
        promptObj.transform.SetParent(canvasObj.transform, false);
        doorInteractPromptText = promptObj.AddComponent<Text>();
        doorInteractPromptText.font = UIHelper.GetDefaultFont();
        doorInteractPromptText.fontSize = 22;
        doorInteractPromptText.fontStyle = FontStyle.Bold;
        doorInteractPromptText.alignment = TextAnchor.MiddleCenter;
        doorInteractPromptText.color = new Color(1f, 1f, 1f, 0.85f);
        doorInteractPromptText.raycastTarget = false;
        doorInteractPromptText.enabled = false;

        RectTransform promptRect = promptObj.GetComponent<RectTransform>();
        promptRect.anchorMin = new Vector2(0.3f, 0.42f);
        promptRect.anchorMax = new Vector2(0.7f, 0.48f);
        promptRect.offsetMin = Vector2.zero;
        promptRect.offsetMax = Vector2.zero;

        GameObject narObj = new GameObject("NarrationText");
        narObj.transform.SetParent(canvasObj.transform, false);
        narrationCanvasGroup = narObj.AddComponent<CanvasGroup>();
        narrationCanvasGroup.alpha = 0f;
        narrationText = narObj.AddComponent<Text>();
        narrationText.font = UIHelper.GetDefaultFont();
        narrationText.fontSize = 24;
        narrationText.alignment = TextAnchor.MiddleCenter;
        narrationText.color = new Color(0.75f, 0.85f, 1f, 1f);
        narrationText.fontStyle = FontStyle.Italic;
        narrationText.raycastTarget = false;

        RectTransform narRect = narObj.GetComponent<RectTransform>();
        narRect.anchorMin = new Vector2(0.1f, 0.05f);
        narRect.anchorMax = new Vector2(0.9f, 0.14f);
        narRect.offsetMin = Vector2.zero;
        narRect.offsetMax = Vector2.zero;
    }

    // =========================================================================
    // HUD Creation (matches Level 3 style)
    // =========================================================================

    private void CreateDialogueHUD()
    {
        // Canvas
        GameObject canvasObj = new GameObject("DialogueHUD");
        canvasObj.transform.SetParent(transform);
        dialogueCanvas = canvasObj.AddComponent<Canvas>();
        UIHelper.ConfigureCanvas(dialogueCanvas, sortingOrder: 25);

        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);

        canvasObj.AddComponent<GraphicRaycaster>();

        // NPC Name (bottom of screen, above dialogue text)
        GameObject nameObj = new GameObject("NpcNameText");
        nameObj.transform.SetParent(canvasObj.transform, false);

        npcNameText = nameObj.AddComponent<Text>();
        npcNameText.font = UIHelper.GetDefaultFont();
        npcNameText.fontSize = 24;
        npcNameText.fontStyle = FontStyle.BoldAndItalic;
        npcNameText.alignment = TextAnchor.MiddleCenter;
        npcNameText.color = new Color(0.8f, 0.8f, 0.5f, 1f);
        npcNameText.raycastTarget = false;

        RectTransform nameRect = nameObj.GetComponent<RectTransform>();
        nameRect.anchorMin = new Vector2(0.1f, 0.18f);
        nameRect.anchorMax = new Vector2(0.9f, 0.22f);
        nameRect.offsetMin = Vector2.zero;
        nameRect.offsetMax = Vector2.zero;

        // Dialogue Text (bottom of screen, italic, like Level 3 narration)
        GameObject textObj = new GameObject("DialogueText");
        textObj.transform.SetParent(canvasObj.transform, false);

        dialogueText = textObj.AddComponent<Text>();
        dialogueText.font = UIHelper.GetDefaultFont();
        dialogueText.fontSize = 24;
        dialogueText.fontStyle = FontStyle.Italic;
        dialogueText.alignment = TextAnchor.MiddleCenter;
        dialogueText.color = new Color(0.75f, 0.85f, 1f, 1f);
        dialogueText.raycastTarget = false;
        dialogueText.horizontalOverflow = HorizontalWrapMode.Wrap;
        dialogueText.verticalOverflow = VerticalWrapMode.Overflow;

        RectTransform textRect = textObj.GetComponent<RectTransform>();
        textRect.anchorMin = new Vector2(0.1f, 0.10f);
        textRect.anchorMax = new Vector2(0.9f, 0.18f);
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        // Prompt Text (below dialogue, only shown on last line)
        GameObject promptObj = new GameObject("PromptText");
        promptObj.transform.SetParent(canvasObj.transform, false);

        promptText = promptObj.AddComponent<Text>();
        promptText.font = UIHelper.GetDefaultFont();
        promptText.fontSize = 20;
        promptText.fontStyle = FontStyle.Italic;
        promptText.alignment = TextAnchor.MiddleCenter;
        promptText.color = new Color(0.75f, 0.85f, 1f, 0.7f);
        promptText.raycastTarget = false;

        RectTransform promptRect = promptObj.GetComponent<RectTransform>();
        promptRect.anchorMin = new Vector2(0.1f, 0.06f);
        promptRect.anchorMax = new Vector2(0.9f, 0.10f);
        promptRect.offsetMin = Vector2.zero;
        promptRect.offsetMax = Vector2.zero;
    }

    private void ShowNarration(string text, float duration)
    {
        if (narrationText == null || narrationCanvasGroup == null) return;

        narrationText.text = text;
        if (narrationFadeCoroutine != null)
            StopCoroutine(narrationFadeCoroutine);

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

    // =========================================================================
    // Default Dialogue (with generated door code buried inside)
    // =========================================================================

    private void BuildDefaultDialogue()
    {
        // Split the code into individual digits for extra obfuscation in the rambling
        char d0 = generatedCode[0];
        char d1 = generatedCode[1];
        char d2 = generatedCode[2];
        char d3 = generatedCode[3];

        dialogueLines = new List<string>
        {
            "Oh! A visitor! I haven't had a visitor in... well, I've never had a visitor actually. This is quite exciting.",

            "Let me tell you about my day. So I woke up this morning and my pillow was slightly to the left of where I usually put it. Can you believe that?",

            "Anyway, then I spent about 45 minutes deciding what to have for breakfast. I went with toast. Actually no, I had cereal. Wait, was it toast?",

            "You know what, I think it was actually a toast-cereal hybrid. I put the cereal on the toast. Revolutionary, right? I should patent that.",

            "But enough about breakfast. Have I told you about my collection of vintage spoons? I have over 300.",

            "My favorite spoon is number 47. It has a slight bend in the handle from when I used it to dig a very small hole in my garden.",

            "I was planting a seed. The seed never grew. I think about that seed sometimes. It was a mystery seed. Found it in my pocket.",

            "Could have been anything. A tree, a flower, a small civilization. We'll never know.",

            "Oh! That reminds me of my uncle. He collected bottle caps. Had 12,000 of them. Bottle cap number 1 was a Coca-Cola cap from 1987. It was red.",

            "Bottle cap number 2 was also a Coca-Cola cap from 1987. Also red. Bottle cap number 3-- you know what, this might take a while.",

            "Anyway, you probably want to know about the door, right? Everyone always asks about the door.",

            "Here's the thing about the door: it's a door. It has hinges. And a handle. And a keypad! I love keypads. So many buttons.",

            // The code is buried here, delivered casually mid-ramble
            $"The code is... let me think. I wrote it on my hand once. First digit is {d0}. Or was it? No, it's definitely {d0}.",

            $"Then there's a {d1}. I remember because that's how many invisible cats I own. {d1} invisible cats. You can't see them but they're there.",

            $"Third digit is {d2}. Same as the number of times I've tried to teach those cats to fetch. {d2} times. None successful.",

            $"And the last one is {d3}. Like the number of working doors in my house. Well, {d3} if you count this one. Which you shouldn't because it's locked.",

            $"So the whole code is {generatedCode}. Write it down or something. Actually, don't write it down. Memorize it. Actually, do whatever you want.",

            "Oh! One more thing. If you get it wrong, don't look at me. I gave you the code fair and square.",

            "Actually, I forget if that was the right code or the code to my spoon cabinet. Only one way to find out, I suppose.",

            "Anyway, it was lovely chatting with you. If you ever want to hear about my spoons in more detail, you know where to find me. Actually, you don't. I move around a lot. Goodbye!"
        };
    }
}
