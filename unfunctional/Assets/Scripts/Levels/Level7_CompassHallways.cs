using UnityEngine;
using UnityEngine.UI;
using System.Collections;

/// <summary>
/// LEVEL 7: Compass following task level. A labyrinth of identical hallways with
/// a compass that always points to the exit. The joke is the hallways are one
/// straight path with fake branches that loop back -- the compass is completely
/// redundant. Near the end the compass goes haywire to create false panic.
///
/// The hallways are built manually in the scene. This script creates the compass
/// HUD at runtime and drives the needle toward the assigned exit point.
/// Attach to a root GameObject in the LEVEL7 scene.
/// </summary>
public class Level7_CompassHallways : LevelManager
{
    [Header("Door")]
    public DoorController doorController;

    [Header("NPC")]
    public GameObject npcObject;
    public float interactRange = 5f;
    public string npcName = "Gorp";

    [Header("Exit")]
    [Tooltip("Transform marking the exit location. The compass needle points here.")]
    public Transform exitPoint;

    [Header("Compass Behavior")]
    public float erraticStartDistance = 20f;
    public float erraticIntensity = 180f;

    private static readonly Color FaceNormal = new Color(0.95f, 0.93f, 0.9f);
    private static readonly Color FaceErratic = new Color(0.6f, 0.25f, 0.2f);

    private Canvas compassCanvas;
    private RectTransform compassNeedle;
    private Image compassFace;
    private bool reachedEnd = false;

    // NPC interaction UI
    private Canvas interactPromptCanvas;
    private Text interactPromptText;
    private Canvas dialogueCanvas;
    private Text dialogueNameText;
    private Text dialogueBodyText;
    private Text dialogueDismissText;
    private bool inDialogue = false;
    private bool hasSpokenToNpc = false;
    private Coroutine typingCoroutine;

    protected override void Start()
    {
        base.Start();
        levelDisplayName = "The Compass Level";
        levelDescription = "Follow the compass. It knows the way. Probably.";
        needsPlayer = true;
        wantsCursorLocked = true;

        CreateCompassHUD();
        CreateInteractPrompt();
        CreateDialogueHUD();

        interactPromptCanvas.gameObject.SetActive(false);
        dialogueCanvas.gameObject.SetActive(false);
    }

    private void Update()
    {
        if (levelComplete) return;
        UpdateCompass();
        CheckExitProximity();
        UpdateNpcInteraction();
    }

    // =========================================================================
    // Compass HUD
    // =========================================================================

    private void CreateCompassHUD()
    {
        // Canvas
        GameObject canvasObj = new GameObject("CompassHUD");
        canvasObj.transform.SetParent(transform);
        compassCanvas = canvasObj.AddComponent<Canvas>();
        compassCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        compassCanvas.sortingOrder = 15;

        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);

        // Outer bezel (gray metallic border)
        GameObject borderObj = new GameObject("CompassBorder");
        borderObj.transform.SetParent(canvasObj.transform, false);
        Image borderImg = borderObj.AddComponent<Image>();
        borderImg.color = new Color(0.45f, 0.47f, 0.5f);
        borderImg.raycastTarget = false;
        RectTransform borderRect = borderObj.GetComponent<RectTransform>();
        borderRect.anchorMin = new Vector2(0.5f, 0f);
        borderRect.anchorMax = new Vector2(0.5f, 0f);
        borderRect.pivot = new Vector2(0.5f, 0f);
        borderRect.anchoredPosition = new Vector2(0f, 20f);
        borderRect.sizeDelta = new Vector2(160f, 160f);

        // Inner bezel ring (slightly darker than face)
        GameObject innerRingObj = new GameObject("InnerRing");
        innerRingObj.transform.SetParent(borderObj.transform, false);
        Image innerRingImg = innerRingObj.AddComponent<Image>();
        innerRingImg.color = new Color(0.55f, 0.56f, 0.58f);
        innerRingImg.raycastTarget = false;
        RectTransform innerRingRect = innerRingObj.GetComponent<RectTransform>();
        innerRingRect.anchorMin = new Vector2(0.05f, 0.05f);
        innerRingRect.anchorMax = new Vector2(0.95f, 0.95f);
        innerRingRect.offsetMin = Vector2.zero;
        innerRingRect.offsetMax = Vector2.zero;

        // Compass face (off-white center)
        GameObject faceObj = new GameObject("CompassFace");
        faceObj.transform.SetParent(innerRingObj.transform, false);
        compassFace = faceObj.AddComponent<Image>();
        compassFace.color = FaceNormal;
        compassFace.raycastTarget = false;
        RectTransform faceRect = faceObj.GetComponent<RectTransform>();
        faceRect.anchorMin = new Vector2(0.04f, 0.04f);
        faceRect.anchorMax = new Vector2(0.96f, 0.96f);
        faceRect.offsetMin = Vector2.zero;
        faceRect.offsetMax = Vector2.zero;

        // Tick marks around the edge
        Color tickColor = new Color(0.25f, 0.25f, 0.25f);
        for (int angle = 0; angle < 360; angle += 15)
        {
            bool cardinal = angle % 90 == 0;
            bool major = angle % 30 == 0;
            float length = cardinal ? 12f : (major ? 8f : 5f);
            float width = cardinal ? 2.5f : (major ? 2f : 1f);
            CreateRadialLine(faceObj.transform, angle, 58f, length, width, tickColor, false);
        }

        // Compass rose: thin decorative diagonals behind the arrow
        Color roseColor = new Color(0.6f, 0.58f, 0.55f);
        for (int a = 45; a < 360; a += 90)
            CreateRadialLine(faceObj.transform, a, 6f, 24f, 1f, roseColor, true);

        // Arrow container (rotates toward exit)
        GameObject needleObj = new GameObject("ArrowContainer");
        needleObj.AddComponent<RectTransform>();
        needleObj.transform.SetParent(faceObj.transform, false);
        compassNeedle = needleObj.GetComponent<RectTransform>();
        compassNeedle.anchorMin = new Vector2(0.5f, 0.5f);
        compassNeedle.anchorMax = new Vector2(0.5f, 0.5f);
        compassNeedle.sizeDelta = Vector2.zero;

        Color arrowColor = new Color(0.85f, 0.2f, 0.15f);

        // Arrow shaft (extends upward from center)
        CreateArrowPart(needleObj.transform, "Shaft", arrowColor,
            new Vector2(4f, 42f), new Vector2(0.5f, 0f), Vector2.zero, 0f);

        // Arrow tail (short stub below center)
        CreateArrowPart(needleObj.transform, "Tail", new Color(0.4f, 0.4f, 0.42f),
            new Vector2(3f, 16f), new Vector2(0.5f, 1f), Vector2.zero, 0f);

        // Arrowhead left arm
        CreateArrowPart(needleObj.transform, "HeadLeft", arrowColor,
            new Vector2(3f, 16f), new Vector2(0.5f, 1f), new Vector2(0f, 40f), 35f);

        // Arrowhead right arm
        CreateArrowPart(needleObj.transform, "HeadRight", arrowColor,
            new Vector2(3f, 16f), new Vector2(0.5f, 1f), new Vector2(0f, 40f), -35f);

        // Center pin
        GameObject pinObj = new GameObject("CenterPin");
        pinObj.transform.SetParent(faceObj.transform, false);
        Image pinImg = pinObj.AddComponent<Image>();
        pinImg.color = new Color(0.35f, 0.28f, 0.22f);
        pinImg.raycastTarget = false;
        RectTransform pinRect = pinObj.GetComponent<RectTransform>();
        pinRect.anchorMin = new Vector2(0.5f, 0.5f);
        pinRect.anchorMax = new Vector2(0.5f, 0.5f);
        pinRect.sizeDelta = new Vector2(10f, 10f);
    }

    private void CreateRadialLine(Transform parent, float angleDeg, float distFromCenter,
        float length, float width, Color color, bool extendsOutward)
    {
        GameObject container = new GameObject($"Line_{angleDeg}");
        container.AddComponent<RectTransform>();
        container.transform.SetParent(parent, false);
        RectTransform cRect = container.GetComponent<RectTransform>();
        cRect.anchorMin = new Vector2(0.5f, 0.5f);
        cRect.anchorMax = new Vector2(0.5f, 0.5f);
        cRect.sizeDelta = Vector2.zero;
        cRect.localRotation = Quaternion.Euler(0, 0, -angleDeg);

        GameObject mark = new GameObject("Mark");
        mark.transform.SetParent(container.transform, false);
        Image img = mark.AddComponent<Image>();
        img.color = color;
        img.raycastTarget = false;
        RectTransform mRect = mark.GetComponent<RectTransform>();
        mRect.anchorMin = new Vector2(0.5f, 0.5f);
        mRect.anchorMax = new Vector2(0.5f, 0.5f);
        mRect.pivot = extendsOutward ? new Vector2(0.5f, 0f) : new Vector2(0.5f, 1f);
        mRect.sizeDelta = new Vector2(width, length);
        mRect.anchoredPosition = new Vector2(0, distFromCenter);
    }

    private void CreateArrowPart(Transform parent, string name, Color color,
        Vector2 size, Vector2 pivot, Vector2 position, float rotationZ)
    {
        GameObject obj = new GameObject($"Arrow_{name}");
        obj.transform.SetParent(parent, false);
        Image img = obj.AddComponent<Image>();
        img.color = color;
        img.raycastTarget = false;
        RectTransform rect = obj.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = pivot;
        rect.sizeDelta = size;
        rect.anchoredPosition = position;
        rect.localRotation = Quaternion.Euler(0f, 0f, rotationZ);
    }

    // =========================================================================
    // Compass Update
    // =========================================================================

    private void UpdateCompass()
    {
        Camera cam = Camera.main;
        if (cam == null || exitPoint == null || compassNeedle == null) return;

        Vector3 toExit = exitPoint.position - cam.transform.position;
        toExit.y = 0;
        float dist = toExit.magnitude;

        float targetAngle = 0f;
        if (toExit.sqrMagnitude > 0.01f)
        {
            float worldAngle = Mathf.Atan2(toExit.x, toExit.z) * Mathf.Rad2Deg;
            float camAngle = cam.transform.eulerAngles.y;
            targetAngle = worldAngle - camAngle;
        }

        if (dist < erraticStartDistance)
        {
            float erraticAmount = 1f - (dist / erraticStartDistance);
            targetAngle += Mathf.Sin(Time.time * 8f) * erraticIntensity * erraticAmount;
            targetAngle += Mathf.Sin(Time.time * 13f) * erraticIntensity * erraticAmount * 0.5f;

            compassFace.color = Color.Lerp(FaceNormal, FaceErratic, erraticAmount);
        }

        compassNeedle.localRotation = Quaternion.Euler(0, 0, -targetAngle);
    }

    // =========================================================================
    // NPC Interaction
    // =========================================================================

    private void UpdateNpcInteraction()
    {
        if (npcObject == null) return;

        Camera cam = Camera.main;
        if (cam == null) return;

        float dist = Vector3.Distance(cam.transform.position, npcObject.transform.position);
        bool nearNpc = dist <= interactRange;
        bool ePressed = Input.GetKeyDown(KeyCode.E);

        if (inDialogue)
        {
            interactPromptCanvas.gameObject.SetActive(false);

            if (ePressed)
                DismissDialogue();

            return;
        }

        interactPromptCanvas.gameObject.SetActive(nearNpc);

        if (nearNpc && ePressed)
            ShowDialogue();
    }

    private void ShowDialogue()
    {
        inDialogue = true;
        hasSpokenToNpc = true;
        interactPromptCanvas.gameObject.SetActive(false);
        dialogueCanvas.gameObject.SetActive(true);

        dialogueNameText.text = npcName;
        dialogueDismissText.gameObject.SetActive(false);

        if (typingCoroutine != null)
            StopCoroutine(typingCoroutine);
        typingCoroutine = StartCoroutine(TypeDialogueLine("Follow the arrow... it will show you the way."));
    }

    private IEnumerator TypeDialogueLine(string text)
    {
        dialogueBodyText.text = "";
        foreach (char c in text)
        {
            dialogueBodyText.text += c;
            yield return new WaitForSeconds(0.04f);
        }

        dialogueDismissText.gameObject.SetActive(true);
        dialogueDismissText.text = "[E] Dismiss";
    }

    private void DismissDialogue()
    {
        if (typingCoroutine != null)
            StopCoroutine(typingCoroutine);
        typingCoroutine = null;

        inDialogue = false;
        dialogueCanvas.gameObject.SetActive(false);
    }

    // =========================================================================
    // NPC Interact Prompt HUD
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
    // NPC Dialogue HUD
    // =========================================================================

    private void CreateDialogueHUD()
    {
        GameObject canvasObj = new GameObject("DialogueHUD");
        canvasObj.transform.SetParent(transform);
        dialogueCanvas = canvasObj.AddComponent<Canvas>();
        dialogueCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        dialogueCanvas.sortingOrder = 25;

        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);

        // NPC name
        GameObject nameObj = new GameObject("NpcNameText");
        nameObj.transform.SetParent(canvasObj.transform, false);
        dialogueNameText = nameObj.AddComponent<Text>();
        dialogueNameText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        dialogueNameText.fontSize = 24;
        dialogueNameText.fontStyle = FontStyle.BoldAndItalic;
        dialogueNameText.alignment = TextAnchor.MiddleCenter;
        dialogueNameText.color = new Color(0.8f, 0.8f, 0.5f, 1f);
        dialogueNameText.raycastTarget = false;

        RectTransform nameRect = nameObj.GetComponent<RectTransform>();
        nameRect.anchorMin = new Vector2(0.1f, 0.18f);
        nameRect.anchorMax = new Vector2(0.9f, 0.22f);
        nameRect.offsetMin = Vector2.zero;
        nameRect.offsetMax = Vector2.zero;

        // Dialogue body
        GameObject bodyObj = new GameObject("DialogueText");
        bodyObj.transform.SetParent(canvasObj.transform, false);
        dialogueBodyText = bodyObj.AddComponent<Text>();
        dialogueBodyText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        dialogueBodyText.fontSize = 24;
        dialogueBodyText.fontStyle = FontStyle.Italic;
        dialogueBodyText.alignment = TextAnchor.MiddleCenter;
        dialogueBodyText.color = new Color(0.75f, 0.85f, 1f, 1f);
        dialogueBodyText.raycastTarget = false;
        dialogueBodyText.horizontalOverflow = HorizontalWrapMode.Wrap;
        dialogueBodyText.verticalOverflow = VerticalWrapMode.Overflow;

        RectTransform bodyRect = bodyObj.GetComponent<RectTransform>();
        bodyRect.anchorMin = new Vector2(0.1f, 0.10f);
        bodyRect.anchorMax = new Vector2(0.9f, 0.18f);
        bodyRect.offsetMin = Vector2.zero;
        bodyRect.offsetMax = Vector2.zero;

        // Dismiss prompt
        GameObject dismissObj = new GameObject("DismissText");
        dismissObj.transform.SetParent(canvasObj.transform, false);
        dialogueDismissText = dismissObj.AddComponent<Text>();
        dialogueDismissText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        dialogueDismissText.fontSize = 20;
        dialogueDismissText.fontStyle = FontStyle.Italic;
        dialogueDismissText.alignment = TextAnchor.MiddleCenter;
        dialogueDismissText.color = new Color(0.75f, 0.85f, 1f, 0.7f);
        dialogueDismissText.raycastTarget = false;

        RectTransform dismissRect = dismissObj.GetComponent<RectTransform>();
        dismissRect.anchorMin = new Vector2(0.1f, 0.06f);
        dismissRect.anchorMax = new Vector2(0.9f, 0.10f);
        dismissRect.offsetMin = Vector2.zero;
        dismissRect.offsetMax = Vector2.zero;
    }

    // =========================================================================
    // Exit Detection
    // =========================================================================

    private void CheckExitProximity()
    {
        Camera cam = Camera.main;
        if (cam == null || exitPoint == null) return;

        float dist = Vector3.Distance(cam.transform.position, exitPoint.position);
        if (dist < 3f && !reachedEnd)
        {
            reachedEnd = true;
            OnReachedEnd();
        }
    }

    public void OnReachedEnd()
    {
        if (levelComplete) return;

        if (doorController != null)
        {
            doorController.OpenDoor();
            StartCoroutine(CompleteAfterDelay(2f));
        }
        else
        {
            CompleteLevel();
        }
    }

    private IEnumerator CompleteAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        CompleteLevel();
    }
}

/// <summary>
/// Trigger collider for the exit zone. Place on a GameObject with a trigger
/// collider near the exit point in the scene.
/// </summary>
public class Level7ExitTrigger : MonoBehaviour
{
    public Level7_CompassHallways levelManager;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") || other.GetComponent<CharacterController>() != null)
        {
            if (levelManager != null)
                levelManager.OnReachedEnd();
        }
    }
}
