using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// LEVEL 2: An obscene amount of audio/video adjustment steps.
/// Player must adjust absurd settings (brightness, contrast, screen tear, 
/// audio channels, compression, particle fog) to specific values to proceed.
/// The target values are nonsensical and barely distinguishable.
/// 
/// Attach to a root GameObject in LEVEL2 scene with a Canvas child.
/// </summary>
public class Level2_SettingsPuzzle : LevelManager
{
    [Header("Level 2 - Settings Puzzle")]
    public Canvas settingsCanvas;

    [Header("Slider References")]
    public Slider brightnessSlider;
    public Slider contrastSlider;
    public Slider leftScreenTearSlider;
    public Slider rightScreenTearSlider;
    public Slider topScreenTearSlider;
    public Slider bottomScreenTearSlider;
    public Slider leftChannelAudioSlider;
    public Slider rightChannelAudioSlider;
    public Slider compressionThresholdSlider;
    public Slider particleFogSlider;

    [Header("Slider Labels (for correct/incorrect indicators)")]
    public Image[] sliderStatusIcons;       // Green/red dots next to each slider

    [Header("Target Values (what the player needs to hit)")]
    [Range(0f, 1f)] public float targetBrightness = 0.73f;
    [Range(0f, 1f)] public float targetContrast = 0.41f;
    [Range(0f, 1f)] public float targetLeftTear = 0.12f;
    [Range(0f, 1f)] public float targetRightTear = 0.88f;
    [Range(0f, 1f)] public float targetTopTear = 0.55f;
    [Range(0f, 1f)] public float targetBottomTear = 0.33f;
    [Range(0f, 1f)] public float targetLeftAudio = 0.67f;
    [Range(0f, 1f)] public float targetRightAudio = 0.29f;
    [Range(0f, 1f)] public float targetCompression = 0.95f;
    [Range(0f, 1f)] public float targetParticleFog = 0.08f;

    [Header("Tolerance")]
    [Tooltip("How close each slider must be to its target (0-1 range)")]
    public float tolerance = 0.03f;

    [Header("Visual Effects")]
    public Image brightnessOverlay;         // Dark overlay for brightness
    public Image contrastOverlay;           // Contrast simulation overlay
    public RectTransform leftTearPanel;     // Offset panels for screen tear effect
    public RectTransform rightTearPanel;
    public RectTransform topTearPanel;
    public RectTransform bottomTearPanel;
    public AudioSource leftAudioSource;
    public AudioSource rightAudioSource;
    public ParticleSystem fogParticles;

    [Header("UI Feedback")]
    public Text feedbackText;               // Shows vague unhelpful feedback
    public Text progressText;               // "3/10 calibrated"
    public Text titleText;                  // "DISPLAY & AUDIO CALIBRATION"
    public Text instructionText;            // Vague instructions
    public Button confirmButton;            // Only works when all settings correct
    public Text confirmButtonText;

    [Header("Annoyance")]
    public float sliderDriftSpeed = 0.008f; // Sliders slowly drift from their set position
    public float driftInterval = 4f;        // How often a random slider drifts
    public bool enableSliderDrift = true;
    public float instructionChangeInterval = 12f; // Swap to new confusing instruction

    private Dictionary<Slider, float> sliderTargets = new Dictionary<Slider, float>();
    private Dictionary<Slider, int> sliderIndices = new Dictionary<Slider, int>();
    private float driftTimer;
    private float instructionTimer;
    private int correctCount;
    private int totalSliders;
    private bool allCorrect = false;
    private float confirmVisibleTimer = 0f;

    private string[] vagueInstructions = new string[]
    {
        "Adjust all parameters until the output feels... right.",
        "Calibrate each setting to match the reference. (There is no reference.)",
        "If the feedback says 'acceptable', that slider is done. Probably.",
        "Please ensure all values match factory specifications.\n(We lost the factory specifications.)",
        "Fine-tune each slider. You'll know when it's correct.\n(You won't.)",
        "Consult your display manual for optimal settings.\n(Manual not included.)",
        "The green dots indicate a calibrated parameter.\nRed means try harder.",
        "Pro tip: the sliders drift. You're welcome.",
        "Almost there! (This message appears regardless of progress.)",
        "If you're reading this, you've been calibrating for too long."
    };

    protected override void Start()
    {
        base.Start();
        levelDisplayName = "Audio/Video Calibration";
        levelDescription = "Please calibrate your display and audio settings to proceed.";

        // Unlock cursor
        if (InputManager.Instance != null)
            InputManager.Instance.UnlockCursor();
        else
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        SetupSliders();
        SetupConfirmButton();

        driftTimer = driftInterval;
        instructionTimer = instructionChangeInterval;

        if (instructionText != null)
            instructionText.text = vagueInstructions[0];
    }

    private void Update()
    {
        if (levelComplete) return;

        ApplyVisualEffects();
        UpdateSliderStates();

        // Slider drift annoyance
        if (enableSliderDrift)
        {
            driftTimer -= Time.deltaTime;
            if (driftTimer <= 0f)
            {
                DriftRandomSlider();
                driftTimer = driftInterval + Random.Range(-1f, 2f);
            }
        }

        // Cycle confusing instructions
        instructionTimer -= Time.deltaTime;
        if (instructionTimer <= 0f)
        {
            if (instructionText != null)
            {
                instructionText.text = vagueInstructions[Random.Range(0, vagueInstructions.Length)];
            }
            instructionTimer = instructionChangeInterval + Random.Range(-3f, 5f);
        }

        // Show/hide confirm button based on correctness
        UpdateConfirmButton();
    }

    // =========================================================================
    // Slider Setup
    // =========================================================================

    private void SetupSliders()
    {
        int index = 0;
        RegisterSlider(brightnessSlider, targetBrightness, index++);
        RegisterSlider(contrastSlider, targetContrast, index++);
        RegisterSlider(leftScreenTearSlider, targetLeftTear, index++);
        RegisterSlider(rightScreenTearSlider, targetRightTear, index++);
        RegisterSlider(topScreenTearSlider, targetTopTear, index++);
        RegisterSlider(bottomScreenTearSlider, targetBottomTear, index++);
        RegisterSlider(leftChannelAudioSlider, targetLeftAudio, index++);
        RegisterSlider(rightChannelAudioSlider, targetRightAudio, index++);
        RegisterSlider(compressionThresholdSlider, targetCompression, index++);
        RegisterSlider(particleFogSlider, targetParticleFog, index++);
        totalSliders = index;
    }

    private void RegisterSlider(Slider slider, float target, int index)
    {
        if (slider == null) return;

        sliderTargets[slider] = target;
        sliderIndices[slider] = index;

        // Set random starting value (never correct)
        float startValue;
        do
        {
            startValue = Random.Range(0f, 1f);
        }
        while (Mathf.Abs(startValue - target) < tolerance * 3f);

        slider.value = startValue;
        slider.onValueChanged.AddListener((val) => OnSliderChanged(slider, val));
    }

    private void SetupConfirmButton()
    {
        if (confirmButton != null)
        {
            confirmButton.onClick.AddListener(OnConfirmClicked);
            confirmButton.gameObject.SetActive(false);
        }
    }

    // =========================================================================
    // Slider State & Feedback
    // =========================================================================

    private void OnSliderChanged(Slider slider, float value)
    {
        if (feedbackText == null) return;

        float target = sliderTargets[slider];
        float diff = Mathf.Abs(value - target);

        if (diff < tolerance)
        {
            feedbackText.text = "Hmm, that seems... acceptable?";
            feedbackText.color = new Color(0.4f, 0.8f, 0.4f);
        }
        else if (diff < 0.08f)
        {
            feedbackText.text = "Getting warmer... maybe.";
            feedbackText.color = new Color(0.8f, 0.8f, 0.3f);
        }
        else if (diff < 0.2f)
        {
            feedbackText.text = "Not quite right.";
            feedbackText.color = new Color(0.8f, 0.6f, 0.3f);
        }
        else
        {
            string[] vagueFeedback = new string[]
            {
                "That doesn't look right.",
                "The vibes are off.",
                "Try adjusting further.",
                "Almost... no wait, not at all.",
                "Have you tried turning it off and on again?",
                "Interesting choice.",
                "That's certainly a value.",
                "Bold move.",
                "Your display weeps.",
                "Technically a number, yes."
            };
            feedbackText.text = vagueFeedback[Random.Range(0, vagueFeedback.Length)];
            feedbackText.color = new Color(0.7f, 0.3f, 0.3f);
        }
    }

    private void UpdateSliderStates()
    {
        correctCount = 0;

        foreach (var kvp in sliderTargets)
        {
            if (kvp.Key == null) continue;

            float diff = Mathf.Abs(kvp.Key.value - kvp.Value);
            bool isCorrect = diff <= tolerance;

            if (isCorrect) correctCount++;

            // Update status icon color
            if (sliderStatusIcons != null && sliderIndices.ContainsKey(kvp.Key))
            {
                int idx = sliderIndices[kvp.Key];
                if (idx < sliderStatusIcons.Length && sliderStatusIcons[idx] != null)
                {
                    sliderStatusIcons[idx].color = isCorrect
                        ? new Color(0.2f, 0.9f, 0.2f, 1f)   // Green
                        : new Color(0.9f, 0.2f, 0.2f, 0.6f); // Red
                }
            }
        }

        allCorrect = (correctCount >= totalSliders && totalSliders > 0);

        // Update progress text
        if (progressText != null)
        {
            progressText.text = $"{correctCount}/{totalSliders} calibrated";
            progressText.color = allCorrect
                ? new Color(0.2f, 1f, 0.2f)
                : Color.white;
        }
    }

    private void UpdateConfirmButton()
    {
        if (confirmButton == null) return;

        if (allCorrect)
        {
            confirmButton.gameObject.SetActive(true);
            confirmVisibleTimer += Time.deltaTime;

            // The confirm button text changes to be sarcastic
            if (confirmButtonText != null)
            {
                if (confirmVisibleTimer < 1f)
                    confirmButtonText.text = "Apply Settings";
                else if (confirmVisibleTimer < 3f)
                    confirmButtonText.text = "Apply Settings?";
                else
                    confirmButtonText.text = "Are you sure?";
            }
        }
        else
        {
            // If a slider drifts out of range while button is showing, hide it
            if (confirmButton.gameObject.activeSelf)
            {
                confirmButton.gameObject.SetActive(false);
                confirmVisibleTimer = 0f;

                if (feedbackText != null)
                {
                    feedbackText.text = "A setting drifted. You'll have to fix it.";
                    feedbackText.color = new Color(0.8f, 0.3f, 0.3f);
                }
            }
        }
    }

    private void OnConfirmClicked()
    {
        if (!allCorrect || levelComplete) return;

        // One final check -- re-verify all sliders (drift might have moved one)
        UpdateSliderStates();
        if (!allCorrect)
        {
            if (feedbackText != null)
            {
                feedbackText.text = "JUST KIDDING. A slider moved. Check again.";
                feedbackText.color = new Color(1f, 0.2f, 0.2f);
            }
            confirmButton.gameObject.SetActive(false);
            confirmVisibleTimer = 0f;
            return;
        }

        Debug.Log("[Level2] All settings confirmed! Level complete.");
        enableSliderDrift = false; // Stop drifting so it doesn't mess up the victory

        if (feedbackText != null)
        {
            feedbackText.text = "Settings calibrated. Congratulations. That only took forever.";
            feedbackText.color = new Color(0.2f, 1f, 0.2f);
        }

        // Disable all sliders
        foreach (var slider in sliderTargets.Keys)
        {
            if (slider != null) slider.interactable = false;
        }

        confirmButton.interactable = false;

        StartCoroutine(CompleteLevelAfterDelay(1.5f));
    }

    private IEnumerator CompleteLevelAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        CompleteLevel();
    }

    // =========================================================================
    // Visual Effects
    // =========================================================================

    private void ApplyVisualEffects()
    {
        // Brightness overlay: darker when brightness is low
        if (brightnessOverlay != null && brightnessSlider != null)
        {
            float b = brightnessSlider.value;
            // At 0 -> fully dark, at 1 -> transparent, at target -> clear
            float alpha = Mathf.Clamp01((1f - b) * 0.85f);
            Color c = brightnessOverlay.color;
            c.a = alpha;
            brightnessOverlay.color = c;
        }

        // Contrast overlay: desaturate / wash out at extremes
        if (contrastOverlay != null && contrastSlider != null)
        {
            float cont = contrastSlider.value;
            // Low contrast = washed out grey overlay, high = invisible, mid = slight tint
            float alpha = Mathf.Abs(cont - 0.5f) < 0.2f ? 0f : Mathf.Abs(cont - 0.5f) * 0.6f;
            Color c = cont < 0.5f
                ? new Color(0.5f, 0.5f, 0.5f, alpha)  // Washed out
                : new Color(0f, 0f, 0f, alpha * 0.3f); // Over-saturated dark edges
            contrastOverlay.color = c;
        }

        // Screen tear: offset panels based on slider values
        ApplyTearEffect(leftTearPanel, leftScreenTearSlider, new Vector2(-1, 0));
        ApplyTearEffect(rightTearPanel, rightScreenTearSlider, new Vector2(1, 0));
        ApplyTearEffect(topTearPanel, topScreenTearSlider, new Vector2(0, 1));
        ApplyTearEffect(bottomTearPanel, bottomScreenTearSlider, new Vector2(0, -1));

        // Audio balance
        if (leftAudioSource != null && leftChannelAudioSlider != null)
            leftAudioSource.volume = leftChannelAudioSlider.value;
        if (rightAudioSource != null && rightChannelAudioSlider != null)
            rightAudioSource.volume = rightChannelAudioSlider.value;

        // Audio compression: distort pitch based on compression slider
        if (compressionThresholdSlider != null)
        {
            float comp = compressionThresholdSlider.value;
            float pitchDistort = 1f + (comp - 0.5f) * 0.3f; // Range ~0.85 to 1.15
            if (leftAudioSource != null) leftAudioSource.pitch = pitchDistort;
            if (rightAudioSource != null) rightAudioSource.pitch = pitchDistort;
        }

        // Fog particles
        if (fogParticles != null && particleFogSlider != null)
        {
            var emission = fogParticles.emission;
            emission.rateOverTime = particleFogSlider.value * 100f;

            var main = fogParticles.main;
            main.startLifetime = 2f + particleFogSlider.value * 5f;
            main.startSize = 0.5f + particleFogSlider.value * 3f;
        }
    }

    private void ApplyTearEffect(RectTransform tearPanel, Slider slider, Vector2 direction)
    {
        if (tearPanel == null || slider == null) return;

        // The tear offset oscillates based on slider value, creating a "tearing" appearance
        float tearAmount = slider.value;
        float maxOffset = 50f;
        float offset = Mathf.Sin(Time.time * (2f + tearAmount * 8f)) * tearAmount * maxOffset;

        Vector2 pos = tearPanel.anchoredPosition;
        pos += direction * offset * Time.deltaTime * 2f;

        // Clamp the total displacement
        pos.x = Mathf.Clamp(pos.x, -maxOffset, maxOffset);
        pos.y = Mathf.Clamp(pos.y, -maxOffset, maxOffset);

        // Slowly return to zero when slider is near zero
        pos = Vector2.Lerp(pos, Vector2.zero, (1f - tearAmount) * Time.deltaTime * 3f);

        tearPanel.anchoredPosition = pos;
    }

    // =========================================================================
    // Drift Annoyance
    // =========================================================================

    private void DriftRandomSlider()
    {
        List<Slider> sliders = new List<Slider>(sliderTargets.Keys);
        if (sliders.Count == 0) return;

        Slider randomSlider = sliders[Random.Range(0, sliders.Count)];
        if (randomSlider != null && randomSlider.interactable)
        {
            float drift = Random.Range(-sliderDriftSpeed, sliderDriftSpeed);
            randomSlider.value = Mathf.Clamp01(randomSlider.value + drift);
        }
    }
}
