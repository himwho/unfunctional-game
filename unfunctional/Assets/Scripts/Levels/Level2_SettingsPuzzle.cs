using UnityEngine;
using UnityEngine.UI;
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
    public Image screenOverlay;             // For brightness/contrast simulation
    public RectTransform[] tearPanels;      // Panels that simulate screen tear
    public AudioSource leftAudioSource;
    public AudioSource rightAudioSource;
    public ParticleSystem fogParticles;

    [Header("Annoyance")]
    public float sliderDriftSpeed = 0.01f;  // Sliders slowly drift from their set position
    public float driftInterval = 3f;        // How often a random slider drifts
    public bool enableSliderDrift = true;
    public Text feedbackText;               // Shows vague unhelpful feedback

    private Dictionary<Slider, float> sliderTargets = new Dictionary<Slider, float>();
    private float driftTimer;
    private int correctCount;

    protected override void Start()
    {
        base.Start();
        levelDisplayName = "Audio/Video Calibration";
        levelDescription = "Please calibrate your display and audio settings to proceed.";

        // Unlock cursor
        if (InputManager.Instance != null)
            InputManager.Instance.UnlockCursor();

        SetupSliders();
        driftTimer = driftInterval;
    }

    private void Update()
    {
        if (levelComplete) return;

        ApplyVisualEffects();

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

        // Check if all sliders are correct
        CheckCompletion();
    }

    private void SetupSliders()
    {
        RegisterSlider(brightnessSlider, targetBrightness);
        RegisterSlider(contrastSlider, targetContrast);
        RegisterSlider(leftScreenTearSlider, targetLeftTear);
        RegisterSlider(rightScreenTearSlider, targetRightTear);
        RegisterSlider(topScreenTearSlider, targetTopTear);
        RegisterSlider(bottomScreenTearSlider, targetBottomTear);
        RegisterSlider(leftChannelAudioSlider, targetLeftAudio);
        RegisterSlider(rightChannelAudioSlider, targetRightAudio);
        RegisterSlider(compressionThresholdSlider, targetCompression);
        RegisterSlider(particleFogSlider, targetParticleFog);
    }

    private void RegisterSlider(Slider slider, float target)
    {
        if (slider == null) return;

        sliderTargets[slider] = target;

        // Set random starting value (never correct)
        float startValue;
        do
        {
            startValue = Random.Range(0f, 1f);
        }
        while (Mathf.Abs(startValue - target) < tolerance * 2f);

        slider.value = startValue;
        slider.onValueChanged.AddListener((val) => OnSliderChanged(slider, val));
    }

    private void OnSliderChanged(Slider slider, float value)
    {
        // Show vague feedback
        if (feedbackText != null)
        {
            float target = sliderTargets[slider];
            float diff = Mathf.Abs(value - target);

            if (diff < tolerance)
            {
                feedbackText.text = "Hmm, that seems... acceptable?";
            }
            else if (diff < 0.1f)
            {
                feedbackText.text = "Getting warmer... maybe.";
            }
            else if (diff < 0.3f)
            {
                feedbackText.text = "Not quite right.";
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
                    "Bold move."
                };
                feedbackText.text = vagueFeedback[Random.Range(0, vagueFeedback.Length)];
            }
        }
    }

    private void ApplyVisualEffects()
    {
        // Brightness/contrast overlay
        if (screenOverlay != null)
        {
            float brightness = brightnessSlider != null ? brightnessSlider.value : 0.5f;
            float contrast = contrastSlider != null ? contrastSlider.value : 0.5f;

            // Map slider values to visual overlay alpha
            float overlayAlpha = Mathf.Clamp01(1f - brightness) * 0.8f;
            Color overlayColor = screenOverlay.color;
            overlayColor.a = overlayAlpha;
            screenOverlay.color = overlayColor;
        }

        // Audio balance
        if (leftAudioSource != null && leftChannelAudioSlider != null)
            leftAudioSource.volume = leftChannelAudioSlider.value;
        if (rightAudioSource != null && rightChannelAudioSlider != null)
            rightAudioSource.volume = rightChannelAudioSlider.value;

        // Fog particles
        if (fogParticles != null && particleFogSlider != null)
        {
            var emission = fogParticles.emission;
            emission.rateOverTime = particleFogSlider.value * 50f;
        }
    }

    private void DriftRandomSlider()
    {
        List<Slider> sliders = new List<Slider>(sliderTargets.Keys);
        if (sliders.Count == 0) return;

        Slider randomSlider = sliders[Random.Range(0, sliders.Count)];
        if (randomSlider != null)
        {
            float drift = Random.Range(-sliderDriftSpeed, sliderDriftSpeed);
            randomSlider.value = Mathf.Clamp01(randomSlider.value + drift);
        }
    }

    private void CheckCompletion()
    {
        correctCount = 0;
        int totalSliders = 0;

        foreach (var kvp in sliderTargets)
        {
            if (kvp.Key == null) continue;
            totalSliders++;

            float diff = Mathf.Abs(kvp.Key.value - kvp.Value);
            if (diff <= tolerance)
            {
                correctCount++;
            }
        }

        if (totalSliders > 0 && correctCount >= totalSliders)
        {
            Debug.Log("[Level2] All settings correct! Level complete.");
            if (feedbackText != null)
                feedbackText.text = "Settings calibrated. Congratulations. That only took forever.";
            CompleteLevel();
        }
    }
}
