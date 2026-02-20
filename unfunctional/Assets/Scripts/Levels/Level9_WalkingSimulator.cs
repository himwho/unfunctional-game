using UnityEngine;
using UnityEngine.UI;
using System.Collections;

/// <summary>
/// LEVEL 9: Walking simulator door. A very long hallway referencing every
/// walking simulator ever made. The player must walk/sprint an absurd distance.
/// Stamina bar depletes quickly and regens slowly, making sprinting tedious.
/// Progress bar moves deceptively slowly at first. "Scenic" set pieces along
/// the way parody the genre.
///
/// Builds geometry, HUD, and scenic pieces at runtime.
/// Attach to root GameObject in LEVEL9 scene.
/// </summary>
public class Level9_WalkingSimulator : LevelManager
{
    [Header("Level 9 - Walking Simulator")]
    public DoorController doorController;

    [Header("Hallway")]
    public float hallwayLength = 250f;
    public float hallwayWidth = 5f;
    public float hallwayHeight = 4f;

    [Header("Stamina")]
    public float maxStamina = 100f;
    public float staminaDrainRate = 40f;   // Per second while sprinting
    public float staminaRegenRate = 8f;    // Per second while walking
    public float sprintMultiplier = 1.8f;
    public float staminaRegenDelay = 1.5f; // Delay after sprinting before regen starts

    // Runtime UI
    private Canvas hudCanvas;
    private Image staminaBarFill;
    private Image progressBarFill;
    private Text progressText;
    private Text scenicText;

    private float currentStamina;
    private float staminaRegenTimer = 0f;
    private bool isSprinting = false;
    private float playerStartZ;
    private bool doorPlaced = false;

    // Scenic set pieces
    private readonly string[] scenicMessages = new string[]
    {
        "\"The journey is the destination.\" - Every walking simulator",
        "A lone chair sits in the hallway. It has no purpose.",
        "\"Keep walking. Something meaningful will happen eventually.\"",
        "The lighting changes slightly. This is considered gameplay.",
        "A note on the ground reads: \"Why are you still walking?\"",
        "The hallway gets slightly wider for no reason.",
        "\"Press Shift to sprint. Or don't. Nothing matters.\"",
        "A potted plant. In a hallway. Underground. Don't ask.",
        "\"You are 40% through. The other 60% is identical.\"",
        "A painting of another, nicer hallway hangs on the wall.",
        "\"Fun fact: this hallway is longer than most RPG main quests.\"",
        "The floor squeaks. This is the highlight of the level.",
        "\"Almost there!\" (This is a lie.)",
        "You hear distant footsteps. It's your echo. You're alone.",
        "A vending machine. It's out of order. Of course it is.",
    };

    protected override void Start()
    {
        base.Start();
        levelDisplayName = "The Walking Simulator";
        levelDescription = "Walk. That's it. That's the game.";
        needsPlayer = true;
        wantsCursorLocked = true;

        currentStamina = maxStamina;

        CreateHUD();
    }

    private void Update()
    {
        if (levelComplete) return;

        UpdateStamina();
        UpdateSprintModifier();
        UpdateProgressBar();
        CheckFinish();
    }

    // =========================================================================
    // Stamina System
    // =========================================================================

    private void UpdateStamina()
    {
        bool wantsSprint = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);

        if (wantsSprint && currentStamina > 0f)
        {
            isSprinting = true;
            currentStamina -= staminaDrainRate * Time.deltaTime;
            currentStamina = Mathf.Max(0f, currentStamina);
            staminaRegenTimer = staminaRegenDelay;
        }
        else
        {
            isSprinting = false;

            if (staminaRegenTimer > 0f)
            {
                staminaRegenTimer -= Time.deltaTime;
            }
            else
            {
                currentStamina += staminaRegenRate * Time.deltaTime;
                currentStamina = Mathf.Min(maxStamina, currentStamina);
            }
        }

        // Update stamina bar
        if (staminaBarFill != null)
        {
            staminaBarFill.fillAmount = currentStamina / maxStamina;

            // Color: green when full, yellow when medium, red when low
            float ratio = currentStamina / maxStamina;
            if (ratio > 0.5f)
                staminaBarFill.color = Color.Lerp(Color.yellow, Color.green, (ratio - 0.5f) * 2f);
            else
                staminaBarFill.color = Color.Lerp(Color.red, Color.yellow, ratio * 2f);
        }
    }

    private void UpdateSprintModifier()
    {
        PlayerController pc = FindAnyObjectByType<PlayerController>();
        if (pc == null) return;

        // Modify the player's walk/run speed based on sprint state
        if (isSprinting)
        {
            pc.walkSpeed = 5f * sprintMultiplier;
            pc.runSpeed = 8f * sprintMultiplier;
        }
        else
        {
            pc.walkSpeed = 5f;
            pc.runSpeed = 8f;
        }
    }

    private void UpdateProgressBar()
    {
        Camera cam = Camera.main;
        if (cam == null || progressBarFill == null) return;

        float currentZ = cam.transform.position.z;
        float rawProgress = Mathf.Clamp01((currentZ - playerStartZ) / hallwayLength);

        // Deceptive progress: slow at first, speeds up near end
        // Use a power curve that makes early progress feel glacial
        float displayProgress = Mathf.Pow(rawProgress, 0.4f) * 0.6f + rawProgress * 0.4f;
        // Actually make it worse: the first 80% of real distance only shows 40% progress
        if (rawProgress < 0.8f)
            displayProgress = rawProgress * 0.5f;
        else
            displayProgress = 0.4f + (rawProgress - 0.8f) * 3f; // Rushes to 100%

        displayProgress = Mathf.Clamp01(displayProgress);
        progressBarFill.fillAmount = displayProgress;

        if (progressText != null)
            progressText.text = $"{(displayProgress * 100f):F0}%";
    }

    private void CheckFinish()
    {
        Camera cam = Camera.main;
        if (cam == null) return;

        float currentZ = cam.transform.position.z;
        if (currentZ >= hallwayLength - 3f)
        {
            if (doorController != null && !doorController.IsOpen)
            {
                doorController.OpenDoor();
                StartCoroutine(CompleteLevelAfterDelay(2f));
            }
            else if (doorController == null)
            {
                CompleteLevel();
            }
        }
    }

    private IEnumerator CompleteLevelAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        CompleteLevel();
    }

    public void ShowScenicText(string message)
    {
        if (scenicText != null)
        {
            scenicText.text = message;
            StopCoroutine("FadeScenicText");
            StartCoroutine(FadeScenicText());
        }
    }

    private IEnumerator FadeScenicText()
    {
        scenicText.color = new Color(0.8f, 0.8f, 0.6f, 1f);
        yield return new WaitForSeconds(3f);

        float fadeTime = 1.5f;
        float elapsed = 0f;
        while (elapsed < fadeTime)
        {
            elapsed += Time.deltaTime;
            float a = 1f - (elapsed / fadeTime);
            scenicText.color = new Color(0.8f, 0.8f, 0.6f, a);
            yield return null;
        }
        scenicText.text = "";
    }

    // =========================================================================
    // HUD
    // =========================================================================

    private void CreateHUD()
    {
        GameObject canvasObj = new GameObject("WalkSimHUD");
        canvasObj.transform.SetParent(transform);
        hudCanvas = canvasObj.AddComponent<Canvas>();
        UIHelper.ConfigureCanvas(hudCanvas, sortingOrder: 15);

        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);

        // -- Stamina Bar --
        GameObject staminaBg = new GameObject("StaminaBG");
        staminaBg.transform.SetParent(canvasObj.transform, false);
        Image staminaBgImg = staminaBg.AddComponent<Image>();
        staminaBgImg.color = new Color(0.15f, 0.15f, 0.15f, 0.8f);
        staminaBgImg.raycastTarget = false;
        RectTransform staminaBgRect = staminaBg.GetComponent<RectTransform>();
        staminaBgRect.anchorMin = new Vector2(0.3f, 0.04f);
        staminaBgRect.anchorMax = new Vector2(0.7f, 0.06f);
        staminaBgRect.offsetMin = Vector2.zero;
        staminaBgRect.offsetMax = Vector2.zero;

        GameObject staminaFill = new GameObject("StaminaFill");
        staminaFill.transform.SetParent(staminaBg.transform, false);
        staminaBarFill = staminaFill.AddComponent<Image>();
        staminaBarFill.color = Color.green;
        staminaBarFill.type = Image.Type.Filled;
        staminaBarFill.fillMethod = Image.FillMethod.Horizontal;
        staminaBarFill.fillAmount = 1f;
        staminaBarFill.raycastTarget = false;
        RectTransform fillRect = staminaFill.GetComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.offsetMin = Vector2.zero;
        fillRect.offsetMax = Vector2.zero;

        // Stamina label
        CreateHUDText(canvasObj.transform, "StaminaLabel", "STAMINA",
            new Vector2(0.3f, 0.06f), new Vector2(0.7f, 0.09f),
            14, new Color(0.6f, 0.6f, 0.6f));

        // -- Progress Bar --
        GameObject progressBg = new GameObject("ProgressBG");
        progressBg.transform.SetParent(canvasObj.transform, false);
        Image progressBgImg = progressBg.AddComponent<Image>();
        progressBgImg.color = new Color(0.15f, 0.15f, 0.15f, 0.8f);
        progressBgImg.raycastTarget = false;
        RectTransform progressBgRect = progressBg.GetComponent<RectTransform>();
        progressBgRect.anchorMin = new Vector2(0.1f, 0.96f);
        progressBgRect.anchorMax = new Vector2(0.9f, 0.98f);
        progressBgRect.offsetMin = Vector2.zero;
        progressBgRect.offsetMax = Vector2.zero;

        GameObject progressFillObj = new GameObject("ProgressFill");
        progressFillObj.transform.SetParent(progressBg.transform, false);
        progressBarFill = progressFillObj.AddComponent<Image>();
        progressBarFill.color = new Color(0.3f, 0.5f, 0.8f);
        progressBarFill.type = Image.Type.Filled;
        progressBarFill.fillMethod = Image.FillMethod.Horizontal;
        progressBarFill.fillAmount = 0f;
        progressBarFill.raycastTarget = false;
        RectTransform progressFillRect = progressFillObj.GetComponent<RectTransform>();
        progressFillRect.anchorMin = Vector2.zero;
        progressFillRect.anchorMax = Vector2.one;
        progressFillRect.offsetMin = Vector2.zero;
        progressFillRect.offsetMax = Vector2.zero;

        // Progress label
        progressText = CreateHUDText(canvasObj.transform, "ProgressLabel", "0%",
            new Vector2(0.42f, 0.92f), new Vector2(0.58f, 0.96f),
            16, new Color(0.7f, 0.7f, 0.8f));

        // -- Scenic Text --
        scenicText = CreateHUDText(canvasObj.transform, "ScenicText", "",
            new Vector2(0.1f, 0.4f), new Vector2(0.9f, 0.5f),
            22, new Color(0.8f, 0.8f, 0.6f, 0f));
        scenicText.fontStyle = FontStyle.Italic;
        scenicText.alignment = TextAnchor.MiddleCenter;
    }

    private Text CreateHUDText(Transform parent, string name, string content,
        Vector2 anchorMin, Vector2 anchorMax, int fontSize, Color color)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent, false);
        RectTransform rect = obj.AddComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        Text txt = obj.AddComponent<Text>();
        txt.font = UIHelper.GetDefaultFont();
        txt.fontSize = fontSize;
        txt.alignment = TextAnchor.MiddleCenter;
        txt.color = color;
        txt.text = content;
        txt.raycastTarget = false;
        return txt;
    }

    // =========================================================================
    // Helpers
    // =========================================================================

    private void CreateBox(string name, Vector3 pos, Vector3 scale, Material mat)
    {
        GameObject obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
        obj.name = name;
        obj.transform.position = pos;
        obj.transform.localScale = scale;
        obj.GetComponent<Renderer>().sharedMaterial = mat;
    }

    private Material CreateMat(Color color)
    {
        Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        if (mat.shader == null || mat.shader.name == "Hidden/InternalErrorShader")
            mat = new Material(Shader.Find("Standard"));
        mat.color = color;
        return mat;
    }
}

/// <summary>
/// Trigger for scenic text along the hallway.
/// </summary>
public class Level9ScenicTrigger : MonoBehaviour
{
    public string message;
    public Level9_WalkingSimulator levelManager;
    private bool triggered = false;

    private void OnTriggerEnter(Collider other)
    {
        if (triggered) return;
        if (other.CompareTag("Player") || other.GetComponent<CharacterController>() != null)
        {
            triggered = true;
            if (levelManager != null)
                levelManager.ShowScenicText(message);
        }
    }
}
