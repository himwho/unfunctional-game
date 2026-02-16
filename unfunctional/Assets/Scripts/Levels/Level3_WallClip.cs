using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// LEVEL 3: A simple room where the door doesn't open.
/// The player must discover they can clip through a specific wall section
/// by exploiting a deliberately broken collider.
///
/// Builds its own HUD at runtime: crosshair, hint text, interact prompt,
/// message overlay, and narration text.
/// </summary>
public class Level3_WallClip : LevelManager
{
    [Header("Level 3 - Wall Clip")]
    public GameObject normalDoor;
    [Tooltip("Optional reference to the door prefab's DoorController")]
    public DoorController doorController;
    public GameObject clippableWallSection;
    public Collider clippableCollider;
    public Transform exitTriggerPoint;
    public BoxCollider exitZoneTrigger;

    [Header("Door Interaction")]
    public string doorLockedMessage = "The door appears to be stuck. Permanently.";
    public float doorInteractRange = 3f;

    [Header("Hints")]
    public float hintDelay = 30f;
    public string[] hints = new string[]
    {
        "Maybe the door isn't the only way...",
        "Some walls in older games aren't as solid as they look.",
        "Try walking into different wall sections.",
        "Look for where the textures don't quite line up.",
        "Just clip through the wall. Yes, really."
    };

    [Header("Narration")]
    public string[] doorNarration = new string[]
    {
        "The door appears to be stuck. Permanently.",
        "Still stuck. It's almost like it was never meant to open.",
        "Look, the door is NOT going to open. Try something else.",
        "I'm not even sure this is a real door.",
        "Okay, you've tried the door {0} times now. Impressive commitment."
    };

    [Header("Visual")]
    public float wallFlickerInterval = 5f;

    // Runtime UI references
    private Canvas hudCanvas;
    private Text hintText;
    private Text messageText;
    private Text interactPromptText;
    private Text narrationText;
    private Image crosshairImage;
    private CanvasGroup hintCanvasGroup;
    private CanvasGroup messageCanvasGroup;
    private CanvasGroup narrationCanvasGroup;

    // State
    private int currentHintIndex = 0;
    private float hintTimer;
    private float flickerTimer;
    private int doorInteractCount = 0;
    private MeshRenderer clippableRenderer;
    private Color clippableOriginalColor;
    private Color clippableGlitchColor;
    private Coroutine hintFadeCoroutine;
    private Coroutine messageFadeCoroutine;
    private Coroutine narrationFadeCoroutine;

    protected override void Start()
    {
        wantsCursorLocked = true;
        needsPlayer = true;
        base.Start();
        levelDisplayName = "The Room";
        levelDescription = "A simple room. Find a way through.";

        hintTimer = hintDelay;
        flickerTimer = wallFlickerInterval;

        // Set up the clippable wall
        if (clippableCollider != null)
        {
            clippableCollider.isTrigger = true;
        }

        if (clippableWallSection != null)
        {
            clippableRenderer = clippableWallSection.GetComponent<MeshRenderer>();
            if (clippableRenderer != null && clippableRenderer.material != null)
            {
                clippableOriginalColor = clippableRenderer.material.color;
                // Glitch color: slightly shifted green/cyan tint
                clippableGlitchColor = new Color(
                    clippableOriginalColor.r * 0.7f,
                    clippableOriginalColor.g * 1.3f,
                    clippableOriginalColor.b * 0.8f
                );
            }
        }

        // Set up exit trigger
        if (exitZoneTrigger != null)
        {
            exitZoneTrigger.isTrigger = true;
            ExitZoneHandler handler = exitZoneTrigger.GetComponent<ExitZoneHandler>();
            if (handler == null)
            {
                handler = exitZoneTrigger.gameObject.AddComponent<ExitZoneHandler>();
            }
            handler.levelManager = this;
        }

        // Build the HUD
        CreateHUD();

        // Initial narration
        ShowNarration("A room. With a door. Should be simple enough.", 4f);
    }

    private void Update()
    {
        if (levelComplete) return;

        // Hint timer
        hintTimer -= Time.deltaTime;
        if (hintTimer <= 0f && currentHintIndex < hints.Length)
        {
            ShowHint(hints[currentHintIndex]);
            currentHintIndex++;
            hintTimer = hintDelay * 0.7f;
        }

        // Wall flicker
        flickerTimer -= Time.deltaTime;
        if (flickerTimer <= 0f && clippableRenderer != null)
        {
            FlickerWall();
            flickerTimer = wallFlickerInterval + Random.Range(-1f, 2f);
        }

        // Door interaction check
        CheckDoorInteraction();

        // Interact prompt
        UpdateInteractPrompt();
    }

    // =========================================================================
    // Interaction
    // =========================================================================

    private void CheckDoorInteraction()
    {
        if (InputManager.Instance == null || !InputManager.Instance.InteractPressed) return;

        Camera cam = Camera.main;
        if (cam == null) return;

        Ray ray = cam.ScreenPointToRay(new Vector3(Screen.width / 2f, Screen.height / 2f, 0f));
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, doorInteractRange, ~0, QueryTriggerInteraction.Collide))
        {
            if (normalDoor != null && IsHitOnDoor(hit))
            {
                doorInteractCount++;

                if (doorInteractCount <= doorNarration.Length)
                {
                    string msg = string.Format(doorNarration[doorInteractCount - 1], doorInteractCount);
                    ShowNarration(msg, 3f);
                }
                else
                {
                    string msg = string.Format(
                        "Attempt #{0}. The door doesn't care about your persistence.",
                        doorInteractCount);
                    ShowNarration(msg, 2.5f);
                }

                // Shake the door slightly for feedback
                if (doorController != null)
                    doorController.ShakeDoor();
                else
                    StartCoroutine(ShakeDoor());
            }
        }
    }

    private void UpdateInteractPrompt()
    {
        if (interactPromptText == null) return;

        Camera cam = Camera.main;
        if (cam == null)
        {
            interactPromptText.enabled = false;
            return;
        }

        Ray ray = cam.ScreenPointToRay(new Vector3(Screen.width / 2f, Screen.height / 2f, 0f));
        RaycastHit hit;

        bool showPrompt = false;
        if (Physics.Raycast(ray, out hit, doorInteractRange, ~0, QueryTriggerInteraction.Collide))
        {
            if (normalDoor != null && IsHitOnDoor(hit))
            {
                showPrompt = true;
            }
        }

        interactPromptText.enabled = showPrompt;
    }

    /// <summary>
    /// Returns true if the raycast hit the door object or any of its children/parent.
    /// Handles imported meshes (LEVEL_DOOR prefab) where colliders may be on sub-objects.
    /// </summary>
    private bool IsHitOnDoor(RaycastHit hit)
    {
        // Direct match on the assigned normalDoor
        if (hit.collider.transform.IsChildOf(normalDoor.transform))
            return true;

        // Also check if the DoorController root was hit (trigger collider on root)
        if (doorController != null && hit.collider.transform.IsChildOf(doorController.transform))
            return true;

        return false;
    }

    private IEnumerator ShakeDoor()
    {
        if (normalDoor == null) yield break;

        Vector3 originalPos = normalDoor.transform.position;
        float duration = 0.3f;
        float elapsed = 0f;
        float intensity = 0.03f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float x = originalPos.x + Random.Range(-intensity, intensity);
            float z = originalPos.z + Random.Range(-intensity, intensity);
            normalDoor.transform.position = new Vector3(x, originalPos.y, z);
            yield return null;
        }

        normalDoor.transform.position = originalPos;
    }

    // =========================================================================
    // Wall Flicker
    // =========================================================================

    private void FlickerWall()
    {
        if (clippableRenderer == null) return;
        StartCoroutine(FlickerRoutine());
    }

    private IEnumerator FlickerRoutine()
    {
        if (clippableRenderer == null || clippableRenderer.material == null) yield break;

        Material mat = clippableRenderer.material;
        mat.color = clippableGlitchColor;
        yield return new WaitForSeconds(0.1f);
        mat.color = clippableOriginalColor;
        yield return new WaitForSeconds(0.05f);
        mat.color = clippableGlitchColor;
        yield return new WaitForSeconds(0.05f);
        mat.color = clippableOriginalColor;
    }

    // =========================================================================
    // Completion
    // =========================================================================

    public void OnPlayerReachedExit()
    {
        if (levelComplete) return;

        Debug.Log("[Level3] Player clipped through the wall! Level complete.");
        ShowMessage("You found the way through! Just like the old days.");
        ShowNarration("Wait... you weren't supposed to be able to do that.\n...Were you?", 4f);

        StartCoroutine(CompletionSequence());
    }

    private IEnumerator CompletionSequence()
    {
        // Give the player a moment to enjoy the messages
        yield return new WaitForSeconds(2f);
        CompleteLevel();
    }

    // =========================================================================
    // HUD Creation
    // =========================================================================

    private void CreateHUD()
    {
        // Canvas
        GameObject canvasObj = new GameObject("Level3HUD");
        canvasObj.transform.SetParent(transform);
        hudCanvas = canvasObj.AddComponent<Canvas>();
        hudCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        hudCanvas.sortingOrder = 20;

        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);

        canvasObj.AddComponent<GraphicRaycaster>();

        // --- Crosshair ---
        GameObject crosshairObj = new GameObject("Crosshair");
        crosshairObj.transform.SetParent(canvasObj.transform, false);

        crosshairImage = crosshairObj.AddComponent<Image>();
        crosshairImage.color = new Color(1f, 1f, 1f, 0.6f);
        crosshairImage.raycastTarget = false;

        RectTransform crossRect = crosshairObj.GetComponent<RectTransform>();
        crossRect.anchorMin = new Vector2(0.5f, 0.5f);
        crossRect.anchorMax = new Vector2(0.5f, 0.5f);
        crossRect.sizeDelta = new Vector2(4, 4);
        crossRect.anchoredPosition = Vector2.zero;

        // --- Interact Prompt ---
        GameObject promptObj = new GameObject("InteractPrompt");
        promptObj.transform.SetParent(canvasObj.transform, false);

        interactPromptText = promptObj.AddComponent<Text>();
        interactPromptText.text = "[E] Interact";
        interactPromptText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        interactPromptText.fontSize = 22;
        interactPromptText.alignment = TextAnchor.MiddleCenter;
        interactPromptText.color = new Color(1f, 1f, 1f, 0.85f);
        interactPromptText.raycastTarget = false;
        interactPromptText.enabled = false;

        RectTransform promptRect = promptObj.GetComponent<RectTransform>();
        promptRect.anchorMin = new Vector2(0.35f, 0.4f);
        promptRect.anchorMax = new Vector2(0.65f, 0.46f);
        promptRect.offsetMin = Vector2.zero;
        promptRect.offsetMax = Vector2.zero;

        // --- Hint Text (top of screen, fades in/out) ---
        GameObject hintObj = new GameObject("HintText");
        hintObj.transform.SetParent(canvasObj.transform, false);

        hintCanvasGroup = hintObj.AddComponent<CanvasGroup>();
        hintCanvasGroup.alpha = 0f;

        hintText = hintObj.AddComponent<Text>();
        hintText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        hintText.fontSize = 20;
        hintText.alignment = TextAnchor.MiddleCenter;
        hintText.color = new Color(0.8f, 0.8f, 0.5f, 1f);
        hintText.fontStyle = FontStyle.Italic;
        hintText.raycastTarget = false;

        RectTransform hintRect = hintObj.GetComponent<RectTransform>();
        hintRect.anchorMin = new Vector2(0.15f, 0.88f);
        hintRect.anchorMax = new Vector2(0.85f, 0.95f);
        hintRect.offsetMin = Vector2.zero;
        hintRect.offsetMax = Vector2.zero;

        // --- Message Text (center of screen, big, for level complete etc.) ---
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
        msgRect.anchorMin = new Vector2(0.15f, 0.45f);
        msgRect.anchorMax = new Vector2(0.85f, 0.55f);
        msgRect.offsetMin = Vector2.zero;
        msgRect.offsetMax = Vector2.zero;

        // --- Narration Text (bottom of screen, narrator voice) ---
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
        narRect.offsetMin = Vector2.zero;
        narRect.offsetMax = Vector2.zero;
    }

    // =========================================================================
    // Display helpers
    // =========================================================================

    private void ShowHint(string hint)
    {
        Debug.Log($"[Level3 Hint] {hint}");
        if (hintText == null || hintCanvasGroup == null) return;
        hintText.text = hint;
        if (hintFadeCoroutine != null) StopCoroutine(hintFadeCoroutine);
        hintFadeCoroutine = StartCoroutine(FadeCanvasGroup(hintCanvasGroup, 5f));
    }

    private void ShowMessage(string message)
    {
        Debug.Log($"[Level3] {message}");
        if (messageText == null || messageCanvasGroup == null) return;
        messageText.text = message;
        if (messageFadeCoroutine != null) StopCoroutine(messageFadeCoroutine);
        messageFadeCoroutine = StartCoroutine(FadeCanvasGroup(messageCanvasGroup, 4f));
    }

    private void ShowNarration(string text, float duration)
    {
        Debug.Log($"[Level3 Narration] {text}");
        if (narrationText == null || narrationCanvasGroup == null) return;
        narrationText.text = text;
        if (narrationFadeCoroutine != null) StopCoroutine(narrationFadeCoroutine);
        narrationFadeCoroutine = StartCoroutine(FadeCanvasGroup(narrationCanvasGroup, duration));
    }

    /// <summary>
    /// Fades a CanvasGroup in, holds, then fades out.
    /// </summary>
    private IEnumerator FadeCanvasGroup(CanvasGroup cg, float holdDuration)
    {
        float fadeIn = 0.4f;
        float fadeOut = 1f;

        // Fade in
        float t = 0f;
        while (t < fadeIn)
        {
            t += Time.deltaTime;
            cg.alpha = Mathf.Clamp01(t / fadeIn);
            yield return null;
        }
        cg.alpha = 1f;

        // Hold
        yield return new WaitForSeconds(holdDuration);

        // Fade out
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

/// <summary>
/// Simple trigger handler for the exit zone behind the clippable wall.
/// </summary>
public class ExitZoneHandler : MonoBehaviour
{
    [HideInInspector]
    public Level3_WallClip levelManager;

    private void OnTriggerEnter(Collider other)
    {
        // CharacterController triggers OnTriggerEnter on trigger colliders it moves through
        if (other.CompareTag("Player") || other.GetComponent<CharacterController>() != null)
        {
            if (levelManager != null)
            {
                levelManager.OnPlayerReachedExit();
            }
        }
    }
}
