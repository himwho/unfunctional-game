using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

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
    public float interactRange = 5f;
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

    // Runtime UI references (built in code)
    private Canvas dialogueCanvas;
    private Text npcNameText;
    private Text dialogueText;
    private Text promptText;
    private CanvasGroup dialogueCanvasGroup;

    // Interact prompt (shown when near NPC but not yet talking)
    private Canvas interactPromptCanvas;
    private Text interactPromptText;

    // Door interaction HUD (crosshair + prompt)
    private Canvas doorHudCanvas;
    private Text doorInteractPromptText;
    private Image crosshairImage;

    private const int IDLE_ANIM_COUNT = 7;

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
            }
        }

        if (dialogueLines.Count == 0)
            BuildDefaultDialogue();

        CreateDialogueHUD();
        CreateInteractPrompt();
        CreateDoorHUD();

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
            keypad.OnCodeSubmitted -= HandleCodeSubmitted;
        base.OnDestroy();
    }

    private void Update()
    {
        if (levelComplete || doorOpening) return;

        if (crosshairImage != null)
            crosshairImage.enabled = keypad != null && keypad.IsOpen;

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
        else if (keypad != null && keypad.IsOpen)
        {
            // Keypad is open -- let KeypadController handle input, hide prompts
            interactPromptCanvas.gameObject.SetActive(false);
            doorInteractPromptText.enabled = false;
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
    /// what the player is looking at: NPC, keypad, or door. Shows the appropriate
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
        RaycastHit hit;
        bool hasHit = Physics.Raycast(ray, out hit, Mathf.Max(interactRange, doorInteractRange), ~0, QueryTriggerInteraction.Collide);

        GazeTarget target = GazeTarget.None;
        float hitDist = hasHit ? hit.distance : float.MaxValue;

        if (hasHit)
        {
            if (hitDist <= doorInteractRange && IsHitOnKeypad(hit))
                target = GazeTarget.Keypad;
            else if (hitDist <= doorInteractRange && IsHitOnDoor(hit))
                target = GazeTarget.Door;
            else if (hitDist <= interactRange && IsHitOnNPC(hit))
                target = GazeTarget.NPC;
        }

        // If not looking at NPC via raycast, fall back to proximity check
        // (only before dialogue is completed -- afterwards require gaze)
        if (target == GazeTarget.None && !dialogueCompleted && IsPlayerNearNPC() && npcReadyToInteract)
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

            case GazeTarget.Keypad:
                interactPromptCanvas.gameObject.SetActive(false);
                doorInteractPromptText.enabled = true;
                doorInteractPromptText.text = "[E] Use Keypad";
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

                case GazeTarget.Keypad:
                    if (keypad != null)
                        keypad.Open();
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

    private enum GazeTarget { None, NPC, Keypad, Door }

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
        if (doorController != null && hit.collider.transform.IsChildOf(doorController.transform))
            return true;
        return false;
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
            keypad.RejectCode("WRONG CODE");

            if (!dialogueCompleted)
                keypad.SetStatus("Maybe talk to " + npcName + " first?", new Color(1f, 0.8f, 0.3f));
        }
    }

    private IEnumerator OpenDoorSequence()
    {
        doorOpening = true;

        yield return new WaitForSeconds(0.8f);

        if (keypad != null) keypad.Close();

        yield return new WaitForSeconds(0.3f);

        if (doorController != null)
            doorController.OpenDoor();

        yield return new WaitForSeconds(1.5f);

        CompleteLevel();
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
    // Interact Prompt (shown when near NPC, before dialogue starts)
    // =========================================================================

    private void CreateInteractPrompt()
    {
        GameObject canvasObj = new GameObject("InteractPromptHUD");
        canvasObj.transform.SetParent(transform);
        interactPromptCanvas = canvasObj.AddComponent<Canvas>();
        interactPromptCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        interactPromptCanvas.sortingOrder = 20;

        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);

        canvasObj.AddComponent<GraphicRaycaster>();

        GameObject textObj = new GameObject("InteractPromptText");
        textObj.transform.SetParent(canvasObj.transform, false);

        interactPromptText = textObj.AddComponent<Text>();
        interactPromptText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
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
    // Door Interaction HUD (crosshair + interact prompt for door/keypad)
    // =========================================================================

    private void CreateDoorHUD()
    {
        GameObject canvasObj = new GameObject("DoorHUD");
        canvasObj.transform.SetParent(transform);
        doorHudCanvas = canvasObj.AddComponent<Canvas>();
        doorHudCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        doorHudCanvas.sortingOrder = 15;

        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);

        canvasObj.AddComponent<GraphicRaycaster>();

        // Crosshair
        GameObject crossObj = new GameObject("Crosshair");
        crossObj.transform.SetParent(canvasObj.transform, false);
        crosshairImage = crossObj.AddComponent<Image>();
        crosshairImage.color = new Color(1f, 1f, 1f, 0.5f);
        crosshairImage.raycastTarget = false;
        RectTransform crossRect = crossObj.GetComponent<RectTransform>();
        crossRect.anchorMin = new Vector2(0.498f, 0.496f);
        crossRect.anchorMax = new Vector2(0.502f, 0.504f);
        crossRect.offsetMin = Vector2.zero;
        crossRect.offsetMax = Vector2.zero;

        // Door interact prompt text
        GameObject promptObj = new GameObject("DoorPromptText");
        promptObj.transform.SetParent(canvasObj.transform, false);
        doorInteractPromptText = promptObj.AddComponent<Text>();
        doorInteractPromptText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
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
        dialogueCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        dialogueCanvas.sortingOrder = 25;

        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);

        canvasObj.AddComponent<GraphicRaycaster>();

        // NPC Name (bottom of screen, above dialogue text)
        GameObject nameObj = new GameObject("NpcNameText");
        nameObj.transform.SetParent(canvasObj.transform, false);

        npcNameText = nameObj.AddComponent<Text>();
        npcNameText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
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
        dialogueText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
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
        promptText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
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
