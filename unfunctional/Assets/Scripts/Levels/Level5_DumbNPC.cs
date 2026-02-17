using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// LEVEL 5: Dumb NPC with an unnecessarily long conversation.
/// Press E near the NPC to start talking. Press E to advance each line.
/// The NPC says nothing useful but the player must exhaust all dialogue to proceed.
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

    [Header("Typing Effect")]
    public float typingSpeed = 0.04f;
    public bool enableTypingEffect = true;

    [Header("Dialogue Lines")]
    public List<string> dialogueLines = new List<string>();

    // Runtime UI references (built in code)
    private Canvas dialogueCanvas;
    private Text npcNameText;
    private Text dialogueText;
    private Text promptText;
    private CanvasGroup dialogueCanvasGroup;

    // Interact prompt (shown when near NPC but not yet talking)
    private Canvas interactPromptCanvas;
    private Text interactPromptText;

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

        if (dialogueLines.Count == 0)
            BuildDefaultDialogue();

        CreateDialogueHUD();
        CreateInteractPrompt();

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

    private void Update()
    {
        if (levelComplete) return;

        if (inputCooldown > 0f)
            inputCooldown -= Time.deltaTime;

        bool ePressed = Input.GetKeyDown(KeyCode.E);

        if (!inDialogue)
        {
            bool nearNpc = IsPlayerNearNPC();
            interactPromptCanvas.gameObject.SetActive(nearNpc);

            if (npcAnimator != null)
            {
                if (wasPlayerNear && !nearNpc && !isReversing)
                {
                    isReversing = true;
                    npcAnimator.SetFloat("AnimSpeed", 1f);
                    npcAnimator.SetTrigger("Reverse");
                    StartCoroutine(ResetAnimatorAfterReverse());
                }
                else if (!isReversing)
                {
                    npcAnimator.SetFloat("AnimSpeed", nearNpc ? 1f : 0f);
                }
            }

            wasPlayerNear = nearNpc;

            if (ePressed)
                TryStartDialogue();
        }
        else if (ePressed && waitingForInput && !isTyping && inputCooldown <= 0f)
        {
            AdvanceDialogue();
        }

        if (inDialogue)
        {
            UpdateDialogueFontSize();
        }
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

    private bool IsPlayerNearNPC()
    {
        Camera cam = Camera.main;
        if (cam == null || npcObject == null) return false;

        float dist = Vector3.Distance(cam.transform.position, npcObject.transform.position);
        return dist <= interactRange;
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

        dialogueCanvas.gameObject.SetActive(false);

        Debug.Log($"[Level5] Dialogue ended after {dialogueLines.Count} lines.");
        CompleteLevel();
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
    // Default Dialogue
    // =========================================================================

    private void BuildDefaultDialogue()
    {
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
            "Bottle cap number 2 was also a Coca-Cola cap from 1987. Also red. Bottle cap number 3— you know what, this might take a while.",
            "Anyway, you probably want to know about the door, right? Everyone always asks about the door.",
            "Here's the thing about the door: it's a door. It has hinges. And a handle. You push it or pull it. Actually, I forget which one.",
            "Maybe it slides? No wait, I think you have to say a password. The password is... hmm. I wrote it down somewhere.",
            "On my hand I think. Let me check. No, that's my grocery list. Eggs, milk, more spoons...",
            "Oh! I remember now! The password is 'please'. Or 'open sesame'. Or 'Gerald is the best'. One of those three. Try all of them.",
            "Actually, the door might not even be locked. I honestly don't remember.",
            "Anyway, it was lovely chatting with you. If you ever want to hear about my spoons in more detail, you know where to find me. Actually, you don't. I move around a lot. Goodbye!"
        };
    }
}
