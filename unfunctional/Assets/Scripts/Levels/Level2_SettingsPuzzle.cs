using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// LEVEL 2: An obscene multi-step audio/video calibration wizard.
/// Each step is a full-screen settings page with its own puzzle mechanic.
/// The player must complete all 10 steps to proceed.
///
/// Steps:
///  1. Brightness  - NEXT button only visible at correct brightness
///  2. Contrast    - going too far resets you to step 1
///  3. L/T/B edges - NEXT hidden as overflow, shift content to find it
///  4. R edge      - same concept, right side
///  5. Left audio  - set left channel to max (annoying tone)
///  6. Right audio - set right channel to max (annoying tone)
///  7. Input gain  - yell into mic; 10% of loudest 3s avg = output volume
///  8. Language     - "read" words in 7 languages via mic detection
///  9. Compression  - unreadable text, slider raises resolution
/// 10. Particles    - obscene particles; text says "set at 22%"
///
/// Creates all UI at runtime. Only needs a Canvas (finds or creates one).
/// </summary>
public class Level2_SettingsPuzzle : LevelManager
{
    private const int TOTAL_STEPS = 10;

    [Header("Optional - will create if null")]
    public Canvas settingsCanvas;

    // =====================================================================
    // Runtime state
    // =====================================================================
    private Font defaultFont;
    private GameObject[] stepPanels;
    private int currentStep = -1;

    // Persistent across steps
    private float outputVolume = 1f;

    // -- Step 1 --
    private Slider step1Slider;
    private Image step1Overlay;
    private Button step1Next;
    private Image step1NextImage;
    private readonly float step1Target = 0.72f;

    // -- Step 2 --
    private Slider step2Slider;
    private Image step2Overlay;
    private Button step2Next;
    private Image step2NextImage;
    private Text step2Warning;
    private readonly float step2MaxSafe = 0.78f;
    private readonly float step2VisibleMin = 0.03f; // NEXT visible from 3%
    private readonly float step2VisibleMax = 0.11f; // NEXT visible to 11%
    private bool step2Warned = false;

    // -- Step 3 --
    private Slider step3Left, step3Top, step3Bottom;
    private RectTransform step3Content;
    private Button step3Next;

    // -- Step 4 --
    private Slider step4Right;
    private RectTransform step4Content;
    private Button step4Next;

    // -- Step 5 --
    private Slider step5Slider;
    private AudioSource step5Audio;
    private Button step5Next;
    private Text step5VolLabel;

    // -- Step 6 --
    private Slider step6Slider;
    private AudioSource step6Audio;
    private Button step6Next;
    private Text step6VolLabel;

    // -- Step 7 --
    private Image step7BarFill;
    private Text step7LevelText;
    private Button step7Next;
    private Button step7Skip;
    private AudioClip micClip;
    private string micDevice;
    private List<float> micHistory = new List<float>();
    private float micSampleTimer;
    private bool micAvailable;
    private float micPeakAvg;
    private float step7Timer;

    // -- Step 8 --
    private Text step8LangName;
    private Text step8Words;
    private Image step8Dot;
    private Text step8Prompt;
    private Button step8Next;
    private Button step8Skip;
    private int langIdx;
    private float langSpeakTimer;
    private bool langWordDone;
    private string[] langNames;
    private string[][] langWords;
    private int langTotalPairs; // 7 languages * 2 words = 14

    // -- Step 9 --
    private Slider step9Slider;
    private Text step9HiddenText;
    private List<Image> compBlocks = new List<Image>();
    private Button step9Next;

    // -- Step 10 --
    private Slider step10Slider;
    private Text step10HiddenText;
    private List<RectTransform> particles = new List<RectTransform>();
    private Button step10Next;

    // -- Persistent camera overlays (brightness + contrast persist across all later steps) --
    private Image persistentBrightnessOverlay;
    private Image persistentContrastOverlay;

    // -- Saved step values for summary --
    private float savedStep1;  // brightness
    private float savedStep2;  // contrast
    private float savedStep3L, savedStep3T, savedStep3B; // edges L/T/B
    private float savedStep4;  // edge R
    private float savedStep5;  // left audio
    private float savedStep6;  // right audio
    private float savedStep7;  // mic gain (micPeakAvg)
    private float savedStep9;  // compression
    private float savedStep10; // particle density

    // -- Syllable detection for Step 8 --
    private int[] expectedSyllablesPerLang;
    private int detectedSyllables;
    private float syllableThreshold = 0.06f;
    private bool wasAboveSyllableThreshold;
    private float syllableCooldown;
    private const float SYLLABLE_COOLDOWN_TIME = 0.12f;

    // Audio clips
    private AudioClip leftTone;
    private AudioClip rightTone;

    // =====================================================================
    // Lifecycle
    // =====================================================================

    protected override void Start()
    {
        wantsCursorLocked = false; // UI level
        base.Start(); // calls ApplyCursorState()
        levelDisplayName = "Audio/Video Calibration";
        levelDescription = "Please configure your display and audio settings to proceed.";

        defaultFont = UIHelper.GetDefaultFont();
        leftTone = GenerateTone(440f, 10f);
        rightTone = GenerateTone(554.37f, 10f);

        langNames = new string[] {
            "Arabic", "English", "Spanish", "French",
            "German", "Chinese (Mandarin)", "Chinese (Fuzhou)"
        };
        langWords = new string[][] {
            new[] { "\u0645\u0631\u062D\u0628\u0627", "\u0639\u0627\u0644\u0645" },
            new[] { "Hello", "World" },
            new[] { "Hola", "Mundo" },
            new[] { "Bonjour", "Monde" },
            new[] { "Hallo", "Welt" },
            new[] { "\u4F60\u597D", "\u4E16\u754C" },
            new[] { "\u6C5D\u597D", "\u4E16\u754C" }
        };
        langTotalPairs = langNames.Length;

        // Expected syllable counts per language (both words combined)
        expectedSyllablesPerLang = new int[] { 5, 3, 4, 3, 3, 4, 4 };
        // Arabic: mar-ha-ba(3) + a-lam(2) = 5
        // English: hel-lo(2) + world(1) = 3
        // Spanish: ho-la(2) + mun-do(2) = 4
        // French: bon-jour(2) + monde(1) = 3
        // German: hal-lo(2) + welt(1) = 3
        // Mandarin: ni-hao(2) + shi-jie(2) = 4
        // Fuzhou: ru-ho(2) + se-gai(2) = 4

        EnsureCanvas();
        BuildAllSteps();
        CreatePersistentOverlays();
        GoToStep(0);
    }

    private void Update()
    {
        if (levelComplete) return;
        UpdateCurrentStep();
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        StopMic();
    }

    // =====================================================================
    // Canvas
    // =====================================================================

    private void EnsureCanvas()
    {
        if (settingsCanvas != null) return;

        settingsCanvas = GetComponentInChildren<Canvas>();
        if (settingsCanvas != null) return;

        GameObject obj = new GameObject("SettingsCanvas");
        obj.transform.SetParent(transform);
        settingsCanvas = obj.AddComponent<Canvas>();
        UIHelper.ConfigureCanvas(settingsCanvas, sortingOrder: 10);
        CanvasScaler scaler = obj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        obj.AddComponent<GraphicRaycaster>();
    }

    // =====================================================================
    // Persistent Camera Overlays
    // =====================================================================

    /// <summary>
    /// Creates two full-screen overlay images on the canvas that persist
    /// across all steps — simulating brightness/contrast applied at the
    /// camera level so subsequent steps are affected.
    /// </summary>
    private void CreatePersistentOverlays()
    {
        // Brightness overlay
        GameObject bObj = new GameObject("PersistentBrightnessOverlay");
        bObj.transform.SetParent(settingsCanvas.transform, false);
        RectTransform brt = bObj.AddComponent<RectTransform>();
        brt.anchorMin = Vector2.zero;
        brt.anchorMax = Vector2.one;
        brt.offsetMin = Vector2.zero;
        brt.offsetMax = Vector2.zero;
        persistentBrightnessOverlay = bObj.AddComponent<Image>();
        persistentBrightnessOverlay.color = new Color(0, 0, 0, 0);
        persistentBrightnessOverlay.raycastTarget = false;
        bObj.SetActive(false);

        // Contrast overlay
        GameObject cObj = new GameObject("PersistentContrastOverlay");
        cObj.transform.SetParent(settingsCanvas.transform, false);
        RectTransform crt = cObj.AddComponent<RectTransform>();
        crt.anchorMin = Vector2.zero;
        crt.anchorMax = Vector2.one;
        crt.offsetMin = Vector2.zero;
        crt.offsetMax = Vector2.zero;
        persistentContrastOverlay = cObj.AddComponent<Image>();
        persistentContrastOverlay.color = new Color(0, 0, 0, 0);
        persistentContrastOverlay.raycastTarget = false;
        cObj.SetActive(false);
    }

    private void SaveStepValue(int step)
    {
        switch (step)
        {
            case 0: savedStep1 = step1Slider != null ? step1Slider.value : 0; break;
            case 1: savedStep2 = step2Slider != null ? step2Slider.value : 0; break;
            case 2:
                savedStep3L = step3Left != null ? step3Left.value : 0;
                savedStep3T = step3Top != null ? step3Top.value : 0;
                savedStep3B = step3Bottom != null ? step3Bottom.value : 0;
                break;
            case 3: savedStep4 = step4Right != null ? step4Right.value : 0; break;
            case 4: savedStep5 = step5Slider != null ? step5Slider.value : 0; break;
            case 5: savedStep6 = step6Slider != null ? step6Slider.value : 0; break;
            case 6: savedStep7 = micPeakAvg; break;
            // step 7 = language (no slider value)
            case 8: savedStep9 = step9Slider != null ? step9Slider.value : 0; break;
            case 9: savedStep10 = step10Slider != null ? step10Slider.value : 0; break;
        }
    }

    private void ApplyPersistentBrightness()
    {
        if (persistentBrightnessOverlay == null) return;

        float v = savedStep1;
        persistentBrightnessOverlay.gameObject.SetActive(true);

        // Same colour logic as step 1's overlay
        if (v <= 0.5f)
        {
            float darkAlpha = 1f - (v / 0.5f);
            persistentBrightnessOverlay.color = new Color(0f, 0f, 0f, darkAlpha);
        }
        else
        {
            float lightAlpha = (v - 0.5f) / 0.5f;
            persistentBrightnessOverlay.color = new Color(1f, 1f, 1f, lightAlpha);
        }

        // Keep overlays on top
        persistentBrightnessOverlay.transform.SetAsLastSibling();
        if (persistentContrastOverlay != null && persistentContrastOverlay.gameObject.activeSelf)
            persistentContrastOverlay.transform.SetAsLastSibling();
    }

    private void ApplyPersistentContrast()
    {
        if (persistentContrastOverlay == null) return;

        float v = savedStep2;
        persistentContrastOverlay.gameObject.SetActive(true);

        if (v < 0.5f)
        {
            float alpha = (0.5f - v) * 1.2f;
            persistentContrastOverlay.color = new Color(0.5f, 0.5f, 0.5f, alpha);
        }
        else
        {
            float alpha = (v - 0.5f) * 0.6f;
            persistentContrastOverlay.color = new Color(0f, 0f, 0f, alpha);
        }

        persistentContrastOverlay.transform.SetAsLastSibling();
    }

    // =====================================================================
    // Step Management
    // =====================================================================

    private void GoToStep(int step)
    {
        // Tear down current step
        if (currentStep == 4) { if (step5Audio) step5Audio.Stop(); }
        if (currentStep == 5) { if (step6Audio) step6Audio.Stop(); }
        if (currentStep == 6) StopMic();
        if (currentStep == 7) StopMic();

        // Save the slider value from the step we're leaving
        SaveStepValue(currentStep);

        // Apply persistent camera-level overlays when leaving brightness / contrast
        if (currentStep == 0 && step > 0) ApplyPersistentBrightness();
        if (currentStep == 1 && step > 1) ApplyPersistentContrast();

        // If we're being sent back to step 0 or 1, disable the persistent overlays
        // (player is re-doing those settings)
        if (step <= 0 && persistentBrightnessOverlay != null)
            persistentBrightnessOverlay.gameObject.SetActive(false);
        if (step <= 1 && persistentContrastOverlay != null)
            persistentContrastOverlay.gameObject.SetActive(false);

        for (int i = 0; i < TOTAL_STEPS; i++)
            if (stepPanels[i] != null) stepPanels[i].SetActive(false);

        currentStep = step;
        if (currentStep >= TOTAL_STEPS)
        {
            StartCoroutine(FinishLevel());
            return;
        }

        stepPanels[currentStep].SetActive(true);

        // Ensure persistent overlays render on top of step panels
        if (persistentBrightnessOverlay != null && persistentBrightnessOverlay.gameObject.activeSelf)
            persistentBrightnessOverlay.transform.SetAsLastSibling();
        if (persistentContrastOverlay != null && persistentContrastOverlay.gameObject.activeSelf)
            persistentContrastOverlay.transform.SetAsLastSibling();

        SetupCurrentStep();
    }

    private void SetupCurrentStep()
    {
        switch (currentStep)
        {
            case 0: SetupStep1(); break;
            case 1: SetupStep2(); break;
            case 2: SetupStep3(); break;
            case 3: SetupStep4(); break;
            case 4: SetupStep5(); break;
            case 5: SetupStep6(); break;
            case 6: SetupStep7(); break;
            case 7: SetupStep8(); break;
            case 8: SetupStep9(); break;
            case 9: SetupStep10(); break;
        }
    }

    private void UpdateCurrentStep()
    {
        switch (currentStep)
        {
            case 0: UpdateStep1(); break;
            case 1: UpdateStep2(); break;
            case 2: UpdateStep3(); break;
            case 3: UpdateStep4(); break;
            case 4: UpdateStep5(); break;
            case 5: UpdateStep6(); break;
            case 6: UpdateStep7(); break;
            case 7: UpdateStep8(); break;
            case 8: UpdateStep9(); break;
            case 9: UpdateStep10(); break;
        }
    }

    private IEnumerator FinishLevel()
    {
        // Apply the output volume set by step 7
        AudioListener.volume = outputVolume;
        Debug.Log($"[Level2] All settings done. Output volume = {outputVolume:P0}");

        // Save step 10 value (we just left it)
        SaveStepValue(9);

        // ── Build summary panel ──────────────────────────────────────────
        GameObject summaryPanel = MakeChild(settingsCanvas.gameObject, "SummaryPanel");
        Pos(summaryPanel, Vector2.zero, Vector2.one);
        summaryPanel.transform.SetAsLastSibling(); // on top of persistent overlays
        Image summaryBg = summaryPanel.AddComponent<Image>();
        summaryBg.color = new Color(0.04f, 0.04f, 0.08f, 0.95f);

        MakeText(summaryPanel, "SETTINGS SUMMARY", 36, TextAnchor.MiddleCenter,
            new Color(0.9f, 0.9f, 0.3f),
            new Vector2(0.1f, 0.88f), new Vector2(0.9f, 0.95f));

        MakeText(summaryPanel, "Your calibration results:", 18, TextAnchor.MiddleCenter,
            new Color(0.6f, 0.6f, 0.6f),
            new Vector2(0.2f, 0.83f), new Vector2(0.8f, 0.88f));

        string[] labels = {
            "Brightness", "Contrast",
            "Left Edge", "Top Edge", "Bottom Edge", "Right Edge",
            "Left Audio", "Right Audio",
            "Mic Input Gain", "Language",
            "Resolution Quality", "Particle Density"
        };
        float[] values = {
            savedStep1, savedStep2,
            savedStep3L, savedStep3T, savedStep3B, savedStep4,
            savedStep5, savedStep6,
            savedStep7, 1f, // language always "complete"
            savedStep9, savedStep10
        };

        float startY = 0.81f;
        float rowH = 0.048f;

        for (int i = 0; i < labels.Length; i++)
        {
            float y = startY - i * rowH;

            // Label
            MakeText(summaryPanel, labels[i], 15, TextAnchor.MiddleRight,
                new Color(0.7f, 0.7f, 0.7f),
                new Vector2(0.05f, y - rowH * 0.45f), new Vector2(0.33f, y + rowH * 0.45f));

            // Bar background
            GameObject barBg = MakeChild(summaryPanel, $"SumBar_{i}");
            Pos(barBg, new Vector2(0.35f, y - rowH * 0.3f), new Vector2(0.78f, y + rowH * 0.3f));
            barBg.AddComponent<Image>().color = new Color(0.12f, 0.12f, 0.18f);

            // Bar fill
            GameObject barFill = MakeChild(barBg, "Fill");
            RectTransform fillRt = barFill.GetComponent<RectTransform>();
            fillRt.anchorMin = Vector2.zero;
            fillRt.anchorMax = new Vector2(Mathf.Clamp01(values[i]), 1f);
            fillRt.offsetMin = Vector2.zero;
            fillRt.offsetMax = Vector2.zero;
            Image fillImg = barFill.AddComponent<Image>();
            fillImg.color = (i == 9)
                ? new Color(0.3f, 0.8f, 0.3f)    // green for language
                : new Color(0.3f, 0.5f, 0.8f, 0.8f);

            // Percentage text
            string valStr = (i == 9)
                ? "\u2713 Complete"
                : $"{Mathf.RoundToInt(values[i] * 100)}%";
            MakeText(summaryPanel, valStr, 15, TextAnchor.MiddleLeft,
                Color.white,
                new Vector2(0.80f, y - rowH * 0.45f), new Vector2(0.96f, y + rowH * 0.45f));
        }

        // "Apply Settings" button
        bool applied = false;
        Button applyBtn = MakeButton(summaryPanel, "APPLY SETTINGS",
            new Vector2(0.5f, 0.06f), new Vector2(240, 50),
            new Color(0.2f, 0.5f, 0.2f, 1f));
        applyBtn.onClick.AddListener(() => applied = true);

        // Wait for the player to click
        while (!applied)
            yield return null;

        // Flash transition
        GameObject flashObj = MakeFullOverlay(summaryPanel, "Flash", Color.white);
        flashObj.transform.SetAsLastSibling();
        yield return new WaitForSeconds(0.15f);
        flashObj.GetComponent<Image>().color = Color.black;
        yield return new WaitForSeconds(0.5f);

        Destroy(summaryPanel);
        yield return new WaitForSeconds(0.3f);
        CompleteLevel();
    }

    // =====================================================================
    // BUILD ALL STEPS
    // =====================================================================

    private void BuildAllSteps()
    {
        stepPanels = new GameObject[TOTAL_STEPS];
        BuildStep1();
        BuildStep2();
        BuildStep3();
        BuildStep4();
        BuildStep5();
        BuildStep6();
        BuildStep7();
        BuildStep8();
        BuildStep9();
        BuildStep10();

        for (int i = 0; i < TOTAL_STEPS; i++)
            stepPanels[i].SetActive(false);
    }

    // ----- Step 1: Brightness -----
    private void BuildStep1()
    {
        GameObject panel = MakeStepPanel(0, "BRIGHTNESS ADJUSTMENT",
            "Adjust the brightness slider until you can find the NEXT button.");

        step1Slider = MakeSlider(panel, "Brightness", 0.42f);

        // Full-screen dark overlay (child of canvas so it covers everything)
        GameObject ovlObj = MakeFullOverlay(panel, "BrightnessOverlay", Color.black);
        step1Overlay = ovlObj.GetComponent<Image>();
        step1Overlay.raycastTarget = false;

        // NEXT button -- grey tone that blends with the overlay at most brightness levels
        step1Next = MakeButton(panel, "NEXT", new Vector2(0.5f, 0.15f), new Vector2(180, 50),
            new Color(0.15f, 0.15f, 0.15f, 1f));
        step1NextImage = step1Next.GetComponent<Image>();
        // Make the button text also grey so it's truly hidden
        Text step1BtnText = step1Next.GetComponentInChildren<Text>();
        if (step1BtnText != null) step1BtnText.color = new Color(0.15f, 0.15f, 0.15f, 1f);
        step1Next.onClick.AddListener(() => { if (CanAdvanceStep1()) GoToStep(1); });
    }

    // ----- Step 2: Contrast -----
    private void BuildStep2()
    {
        GameObject panel = MakeStepPanel(1, "CONTRAST ADJUSTMENT",
            "Fine-tune the contrast. Be careful not to go too high.");

        step2Slider = MakeSlider(panel, "Contrast", 0.42f);

        GameObject ovlObj = MakeFullOverlay(panel, "ContrastOverlay",
            new Color(0.5f, 0.5f, 0.5f, 0f));
        step2Overlay = ovlObj.GetComponent<Image>();
        step2Overlay.raycastTarget = false;

        step2Warning = MakeText(panel, "", 28, TextAnchor.MiddleCenter,
            new Color(1f, 0.3f, 0.3f), new Vector2(0.15f, 0.18f), new Vector2(0.85f, 0.28f));
        step2Warning.gameObject.SetActive(false);

        step2Next = MakeButton(panel, "NEXT", new Vector2(0.5f, 0.1f), new Vector2(180, 50),
            new Color(0.2f, 0.45f, 0.2f, 0f)); // start fully transparent
        step2NextImage = step2Next.GetComponent<Image>();
        // Hide button text initially too
        Text step2BtnText = step2Next.GetComponentInChildren<Text>();
        if (step2BtnText != null) step2BtnText.color = new Color(1f, 1f, 1f, 0f);
        step2Next.onClick.AddListener(() => { if (CanAdvanceStep2()) GoToStep(2); });
    }

    // ----- Step 3: Left / Top / Bottom Edge -----
    private void BuildStep3()
    {
        GameObject panel = MakeStepPanel(2, "EDGE CALIBRATION (L / T / B)",
            "Adjust the screen edges to reveal the NEXT button.");

        step3Left = MakeSlider(panel, "Left Edge", 0.55f);
        step3Top = MakeSlider(panel, "Top Edge", 0.45f);
        step3Bottom = MakeSlider(panel, "Bottom Edge", 0.35f);

        // Masked viewport area
        GameObject maskObj = MakeChild(panel, "MaskArea");
        Pos(maskObj, new Vector2(0.15f, 0.05f), new Vector2(0.85f, 0.28f));
        Image maskBg = maskObj.AddComponent<Image>();
        maskBg.color = new Color(0.1f, 0.1f, 0.15f, 1f);
        maskObj.AddComponent<RectMask2D>();

        // Content inside the mask - larger than the mask, shifted so NEXT is hidden
        GameObject content = MakeChild(maskObj, "Content");
        step3Content = content.GetComponent<RectTransform>();
        step3Content.anchorMin = Vector2.zero;
        step3Content.anchorMax = Vector2.one;
        // Start offset so button is out of view (bottom-left)
        step3Content.offsetMin = new Vector2(-200, -150);
        step3Content.offsetMax = new Vector2(200, 150);

        MakeText(content, "The button is around here somewhere...", 18,
            TextAnchor.MiddleCenter, new Color(0.5f, 0.5f, 0.5f),
            new Vector2(0.1f, 0.4f), new Vector2(0.9f, 0.7f));

        // NEXT button placed at the far bottom-left of the content
        step3Next = MakeButton(content, "NEXT", Vector2.zero, new Vector2(120, 40),
            new Color(0.2f, 0.45f, 0.2f, 1f));
        RectTransform btn3Rect = step3Next.GetComponent<RectTransform>();
        btn3Rect.anchorMin = new Vector2(0f, 0f);
        btn3Rect.anchorMax = new Vector2(0f, 0f);
        btn3Rect.anchoredPosition = new Vector2(40, 20);
        step3Next.onClick.AddListener(() => GoToStep(3));
    }

    // ----- Step 4: Right Edge -----
    private void BuildStep4()
    {
        GameObject panel = MakeStepPanel(3, "EDGE CALIBRATION (RIGHT)",
            "Adjust the right screen edge.");

        step4Right = MakeSlider(panel, "Right Edge", 0.45f);

        GameObject maskObj = MakeChild(panel, "MaskArea");
        Pos(maskObj, new Vector2(0.15f, 0.1f), new Vector2(0.85f, 0.35f));
        Image maskBg = maskObj.AddComponent<Image>();
        maskBg.color = new Color(0.1f, 0.1f, 0.15f, 1f);
        maskObj.AddComponent<RectMask2D>();

        GameObject content = MakeChild(maskObj, "Content");
        step4Content = content.GetComponent<RectTransform>();
        step4Content.anchorMin = Vector2.zero;
        step4Content.anchorMax = Vector2.one;
        step4Content.offsetMin = new Vector2(-200, -50);
        step4Content.offsetMax = new Vector2(200, 50);

        MakeText(content, "Almost there...", 18,
            TextAnchor.MiddleCenter, new Color(0.5f, 0.5f, 0.5f),
            new Vector2(0.1f, 0.3f), new Vector2(0.9f, 0.7f));

        // NEXT button at the far right of the content
        step4Next = MakeButton(content, "NEXT", Vector2.zero, new Vector2(120, 40),
            new Color(0.2f, 0.45f, 0.2f, 1f));
        RectTransform btn4Rect = step4Next.GetComponent<RectTransform>();
        btn4Rect.anchorMin = new Vector2(1f, 0.5f);
        btn4Rect.anchorMax = new Vector2(1f, 0.5f);
        btn4Rect.anchoredPosition = new Vector2(-40, 0);
        step4Next.onClick.AddListener(() => GoToStep(4));
    }

    // ----- Step 5: Left Audio Channel -----
    private void BuildStep5()
    {
        GameObject panel = MakeStepPanel(4, "LEFT AUDIO CHANNEL",
            "Set the left audio channel gain to maximum.");

        step5Slider = MakeSlider(panel, "Left Channel Gain", 0.45f);

        step5VolLabel = MakeText(panel, "Volume: 0%", 24, TextAnchor.MiddleCenter,
            Color.white, new Vector2(0.3f, 0.32f), new Vector2(0.7f, 0.38f));

        step5Next = MakeButton(panel, "NEXT", new Vector2(0.5f, 0.12f), new Vector2(180, 50),
            new Color(0.2f, 0.45f, 0.2f, 1f));
        step5Next.gameObject.SetActive(false);
        step5Next.onClick.AddListener(() => GoToStep(5));

        // Audio source
        step5Audio = MakeAudioSource("LeftChannel", leftTone);
        step5Audio.panStereo = -1f;
    }

    // ----- Step 6: Right Audio Channel -----
    private void BuildStep6()
    {
        GameObject panel = MakeStepPanel(5, "RIGHT AUDIO CHANNEL",
            "Set the right audio channel gain to maximum.");

        step6Slider = MakeSlider(panel, "Right Channel Gain", 0.45f);

        step6VolLabel = MakeText(panel, "Volume: 0%", 24, TextAnchor.MiddleCenter,
            Color.white, new Vector2(0.3f, 0.32f), new Vector2(0.7f, 0.38f));

        step6Next = MakeButton(panel, "NEXT", new Vector2(0.5f, 0.12f), new Vector2(180, 50),
            new Color(0.2f, 0.45f, 0.2f, 1f));
        step6Next.gameObject.SetActive(false);
        step6Next.onClick.AddListener(() => GoToStep(6));

        step6Audio = MakeAudioSource("RightChannel", rightTone);
        step6Audio.panStereo = 1f;
    }

    // ----- Step 7: Input Gain (Microphone) -----
    private void BuildStep7()
    {
        GameObject panel = MakeStepPanel(6, "INPUT GAIN CALIBRATION",
            "We need to calibrate your microphone.\n" +
            "Please produce a sustained loud sound for 3 seconds.\n" +
            "10% of the loudest 3-second average will become the output volume.");

        step7LevelText = MakeText(panel, "Mic Level: --", 22, TextAnchor.MiddleCenter,
            Color.white, new Vector2(0.25f, 0.42f), new Vector2(0.75f, 0.48f));

        // Mic level bar
        GameObject barBg = MakeChild(panel, "MicBarBg");
        Pos(barBg, new Vector2(0.2f, 0.34f), new Vector2(0.8f, 0.4f));
        barBg.AddComponent<Image>().color = new Color(0.15f, 0.15f, 0.2f);

        GameObject barFill = MakeChild(barBg, "Fill");
        step7BarFill = barFill.AddComponent<Image>();
        step7BarFill.color = new Color(0.3f, 0.7f, 0.3f);
        RectTransform fillRect = barFill.GetComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = new Vector2(0f, 1f);
        fillRect.offsetMin = Vector2.zero;
        fillRect.offsetMax = Vector2.zero;

        step7Next = MakeButton(panel, "NEXT", new Vector2(0.5f, 0.12f), new Vector2(180, 50),
            new Color(0.2f, 0.45f, 0.2f, 1f));
        step7Next.gameObject.SetActive(false);
        step7Next.onClick.AddListener(() =>
        {
            outputVolume = Mathf.Clamp01(micPeakAvg * 0.1f);
            AudioListener.volume = outputVolume;
            GoToStep(7);
        });

        // Skip button for no-mic scenarios
        step7Skip = MakeButton(panel, "Skip (no mic)", new Vector2(0.5f, 0.05f),
            new Vector2(160, 35), new Color(0.4f, 0.2f, 0.2f, 1f));
        step7Skip.GetComponentInChildren<Text>().fontSize = 16;
        step7Skip.gameObject.SetActive(false);
        step7Skip.onClick.AddListener(() =>
        {
            outputVolume = 0.05f; // Punishingly quiet
            AudioListener.volume = outputVolume;
            GoToStep(7);
        });
    }

    // ----- Step 8: Language / TTS -----
    private void BuildStep8()
    {
        GameObject panel = MakeStepPanel(7, "LANGUAGE CONFIGURATION",
            "To set your language, please read the following words aloud.\n" +
            "Speak clearly into your microphone.");

        step8LangName = MakeText(panel, "", 28, TextAnchor.MiddleCenter,
            new Color(0.6f, 0.8f, 1f), new Vector2(0.2f, 0.52f), new Vector2(0.8f, 0.58f));

        step8Words = MakeText(panel, "", 48, TextAnchor.MiddleCenter,
            Color.white, new Vector2(0.1f, 0.38f), new Vector2(0.9f, 0.52f));

        step8Prompt = MakeText(panel, "", 20, TextAnchor.MiddleCenter,
            new Color(0.5f, 0.5f, 0.5f), new Vector2(0.25f, 0.3f), new Vector2(0.75f, 0.36f));

        // Recording dot
        GameObject dotObj = MakeChild(panel, "RecDot");
        Pos(dotObj, new Vector2(0.48f, 0.26f), new Vector2(0.52f, 0.3f));
        step8Dot = dotObj.AddComponent<Image>();
        step8Dot.color = new Color(0.8f, 0.1f, 0.1f, 0f);

        step8Next = MakeButton(panel, "NEXT", new Vector2(0.5f, 0.1f), new Vector2(180, 50),
            new Color(0.2f, 0.45f, 0.2f, 1f));
        step8Next.gameObject.SetActive(false);
        step8Next.onClick.AddListener(() => GoToStep(8));

        step8Skip = MakeButton(panel, "Skip (no mic)", new Vector2(0.5f, 0.04f),
            new Vector2(160, 35), new Color(0.4f, 0.2f, 0.2f, 1f));
        step8Skip.GetComponentInChildren<Text>().fontSize = 16;
        step8Skip.gameObject.SetActive(false);
        step8Skip.onClick.AddListener(() => GoToStep(8));
    }

    // ----- Step 9: Visual Compression -----
    private void BuildStep9()
    {
        GameObject panel = MakeStepPanel(8, "VISUAL COMPRESSION",
            "Your display compression is too high.\nAdjust the slider to raise resolution.");

        step9Slider = MakeSlider(panel, "Resolution Quality", 0.5f);
        step9Slider.value = 0f;

        // Hidden instruction text (obscured by compression blocks)
        step9HiddenText = MakeText(panel, "Set slider to 85% and press NEXT", 30,
            TextAnchor.MiddleCenter, new Color(0.9f, 0.9f, 0.3f),
            new Vector2(0.15f, 0.2f), new Vector2(0.85f, 0.38f));

        // Compression blocks parent
        GameObject blocksObj = MakeChild(panel, "CompBlocks");
        Pos(blocksObj, new Vector2(0.1f, 0.15f), new Vector2(0.9f, 0.45f));

        // Create a grid of colored blocks that obscure the text
        int cols = 20;
        int rows = 8;
        for (int r = 0; r < rows; r++)
        {
            for (int c = 0; c < cols; c++)
            {
                GameObject block = MakeChild(blocksObj, $"B_{r}_{c}");
                float x0 = (float)c / cols;
                float x1 = (float)(c + 1) / cols;
                float y0 = (float)r / rows;
                float y1 = (float)(r + 1) / rows;
                Pos(block, new Vector2(x0, y0), new Vector2(x1, y1));
                Image img = block.AddComponent<Image>();
                img.color = new Color(
                    Random.Range(0.05f, 0.3f),
                    Random.Range(0.05f, 0.3f),
                    Random.Range(0.1f, 0.35f),
                    1f);
                img.raycastTarget = false;
                compBlocks.Add(img);
            }
        }

        step9Next = MakeButton(panel, "NEXT", new Vector2(0.5f, 0.08f), new Vector2(180, 50),
            new Color(0.2f, 0.45f, 0.2f, 1f));
        step9Next.onClick.AddListener(() =>
        {
            if (step9Slider.value >= 0.83f && step9Slider.value <= 0.87f)
                GoToStep(9);
        });
    }

    // ----- Step 10: Particles -----
    private void BuildStep10()
    {
        GameObject panel = MakeStepPanel(9, "PARTICLE EFFECTS TEST",
            "Please set the particle density to the recommended level.");

        step10Slider = MakeSlider(panel, "Particle Density", 0.5f);
        step10Slider.value = 1f; // Start at max particles

        step10HiddenText = MakeText(panel, "Set slider to 22% to continue", 28,
            TextAnchor.MiddleCenter, new Color(0.9f, 0.9f, 0.3f),
            new Vector2(0.15f, 0.18f), new Vector2(0.85f, 0.35f));

        // Particles parent
        GameObject partObj = MakeChild(panel, "Particles");
        Pos(partObj, new Vector2(0f, 0f), new Vector2(1f, 0.75f));

        // Create many fake particle images
        for (int i = 0; i < 200; i++)
        {
            GameObject p = MakeChild(partObj, $"P_{i}");
            RectTransform prt = p.GetComponent<RectTransform>();
            prt.anchorMin = new Vector2(Random.Range(0f, 1f), Random.Range(0f, 1f));
            prt.anchorMax = prt.anchorMin;
            float size = Random.Range(15f, 60f);
            prt.sizeDelta = new Vector2(size, size);
            Image img = p.AddComponent<Image>();
            img.color = new Color(
                Random.Range(0.2f, 1f),
                Random.Range(0.2f, 1f),
                Random.Range(0.2f, 1f),
                Random.Range(0.5f, 0.9f));
            img.raycastTarget = false;
            particles.Add(prt);
        }

        step10Next = MakeButton(panel, "NEXT", new Vector2(0.5f, 0.08f), new Vector2(180, 50),
            new Color(0.2f, 0.45f, 0.2f, 1f));
        step10Next.onClick.AddListener(() =>
        {
            if (step10Slider.value >= 0.20f && step10Slider.value <= 0.24f)
            {
                GoToStep(10); // triggers FinishLevel
            }
        });
    }

    // =====================================================================
    // STEP SETUP (called when entering a step)
    // =====================================================================

    private void SetupStep1()
    {
        step1Slider.value = 0.3f;
    }

    private void SetupStep2()
    {
        step2Slider.value = 0.5f;
        step2Warned = false;
        step2Warning.gameObject.SetActive(false);
    }

    private void SetupStep3()
    {
        step3Left.value = 0.5f;
        step3Top.value = 0.5f;
        step3Bottom.value = 0.5f;
        step3Content.offsetMin = new Vector2(-200, -150);
        step3Content.offsetMax = new Vector2(200, 150);
    }

    private void SetupStep4()
    {
        step4Right.value = 0.5f;
        step4Content.offsetMin = new Vector2(-200, -50);
        step4Content.offsetMax = new Vector2(200, 50);
    }

    private void SetupStep5()
    {
        step5Slider.value = 0f;
        step5Next.gameObject.SetActive(false);
        step5Audio.volume = 0f;
        step5Audio.Play();
    }

    private void SetupStep6()
    {
        step6Slider.value = 0f;
        step6Next.gameObject.SetActive(false);
        step6Audio.volume = 0f;
        step6Audio.Play();
    }

    private void SetupStep7()
    {
        micHistory.Clear();
        micPeakAvg = 0f;
        micSampleTimer = 0f;
        step7Timer = 0f;
        step7Next.gameObject.SetActive(false);
        step7Skip.gameObject.SetActive(false);
        StartMic();
    }

    private void SetupStep8()
    {
        langIdx = 0;
        langSpeakTimer = 0f;
        langWordDone = false;
        step7Timer = 0f; // reset skip timer
        detectedSyllables = 0;
        wasAboveSyllableThreshold = false;
        syllableCooldown = 0f;
        step8Next.gameObject.SetActive(false);
        step8Skip.gameObject.SetActive(false);
        StartMic();
        ShowCurrentLanguage();
    }

    private void SetupStep9()
    {
        step9Slider.value = 0f;
        foreach (Image block in compBlocks)
            if (block != null) block.gameObject.SetActive(true);
    }

    private void SetupStep10()
    {
        step10Slider.value = 1f;
        foreach (RectTransform p in particles)
            if (p != null) p.gameObject.SetActive(true);
    }

    // =====================================================================
    // STEP UPDATES (called each frame)
    // =====================================================================

    private void UpdateStep1()
    {
        float v = step1Slider.value;

        // Overlay goes from completely dark (v=0) to completely light (v=1).
        // At v=0 the screen is pitch black; at v=1 it's blinding white.
        if (v <= 0.5f)
        {
            // Dark overlay fades out as brightness increases
            float darkAlpha = 1f - (v / 0.5f); // 1 at v=0, 0 at v=0.5
            step1Overlay.color = new Color(0f, 0f, 0f, darkAlpha);
        }
        else
        {
            // White overlay fades in as brightness goes past midpoint
            float lightAlpha = (v - 0.5f) / 0.5f; // 0 at v=0.5, 1 at v=1
            step1Overlay.color = new Color(1f, 1f, 1f, lightAlpha);
        }

        // NEXT button is grey and blends into the background at most brightness.
        // It only becomes distinguishable near the target (~85%).
        // At 85% the overlay is white with alpha ~0.7, so the button needs to
        // match that exact shade to be clickable but nearly invisible elsewhere.
        float dist = Mathf.Abs(v - step1Target);
        float visibility = 1f - Mathf.Clamp01(dist / 0.04f); // visible within ±4% of target

        // Background at ~85%: mix of panel dark + white overlay ≈ bright grey
        // Button transitions from dark grey (invisible when dark) to matching bright grey
        float grey = Mathf.Lerp(0.12f, 0.55f, visibility);
        step1NextImage.color = new Color(grey, grey, grey, 0.3f + visibility * 0.7f);

        // Also adjust the button text visibility
        Text btnText = step1Next.GetComponentInChildren<Text>();
        if (btnText != null)
        {
            float textGrey = Mathf.Lerp(0.12f, 0.9f, visibility);
            btnText.color = new Color(textGrey, textGrey, textGrey, visibility);
        }
    }

    private bool CanAdvanceStep1()
    {
        return Mathf.Abs(step1Slider.value - step1Target) < 0.04f;
    }

    private void UpdateStep2()
    {
        float v = step2Slider.value;
        // Contrast overlay
        float alpha;
        Color c;
        if (v < 0.5f)
        {
            alpha = (0.5f - v) * 1.2f;
            c = new Color(0.5f, 0.5f, 0.5f, alpha); // washed out
        }
        else
        {
            alpha = (v - 0.5f) * 0.6f;
            c = new Color(0f, 0f, 0f, alpha); // over-contrasted
        }
        step2Overlay.color = c;

        // NEXT button only visible when contrast is in 3%-11% range
        float visibility = 0f;
        if (v >= step2VisibleMin && v <= step2VisibleMax)
        {
            // Full visibility when in the sweet spot center
            float center = (step2VisibleMin + step2VisibleMax) / 2f;
            float halfRange = (step2VisibleMax - step2VisibleMin) / 2f;
            float dist = Mathf.Abs(v - center);
            visibility = 1f - Mathf.Clamp01(dist / halfRange * 0.5f); // softer falloff within range
        }

        if (step2NextImage != null)
            step2NextImage.color = new Color(0.2f, 0.45f, 0.2f, visibility);

        Text btnText = step2Next.GetComponentInChildren<Text>();
        if (btnText != null)
            btnText.color = new Color(1f, 1f, 1f, visibility);

        // If too high, warn and reset
        if (v > step2MaxSafe && !step2Warned)
        {
            step2Warned = true;
            step2Warning.gameObject.SetActive(true);
            step2Warning.text = "CONTRAST TOO HIGH!\nRecalibrating brightness...";
            StartCoroutine(ResetToStep1());
        }
    }

    private bool CanAdvanceStep2()
    {
        float v = step2Slider.value;
        return v >= step2VisibleMin && v <= step2VisibleMax;
    }

    private IEnumerator ResetToStep1()
    {
        yield return new WaitForSeconds(2f);
        GoToStep(0);
    }

    private void UpdateStep3()
    {
        // Shift content based on sliders to reveal NEXT button
        float lx = (step3Left.value - 0.5f) * 400f;     // left shifts content right
        float ty = (step3Top.value - 0.5f) * -300f;      // top shifts content down
        float by = (step3Bottom.value - 0.5f) * 300f;    // bottom shifts content up

        step3Content.offsetMin = new Vector2(-200 + lx, -150 + by);
        step3Content.offsetMax = new Vector2(200 + lx, 150 + ty);
    }

    private void UpdateStep4()
    {
        float rx = (step4Right.value - 0.5f) * -400f; // right shifts content left
        step4Content.offsetMin = new Vector2(-200 + rx, -50);
        step4Content.offsetMax = new Vector2(200 + rx, 50);
    }

    private void UpdateStep5()
    {
        float v = step5Slider.value;
        step5Audio.volume = v;
        step5VolLabel.text = $"Volume: {Mathf.RoundToInt(v * 100)}%";

        bool atMax = v >= 0.98f;
        if (atMax && !step5Next.gameObject.activeSelf)
            step5Next.gameObject.SetActive(true);
    }

    private void UpdateStep6()
    {
        float v = step6Slider.value;
        step6Audio.volume = v;
        step6VolLabel.text = $"Volume: {Mathf.RoundToInt(v * 100)}%";

        bool atMax = v >= 0.98f;
        if (atMax && !step6Next.gameObject.activeSelf)
            step6Next.gameObject.SetActive(true);
    }

    private void UpdateStep7()
    {
        step7Timer += Time.deltaTime;

        // Show skip after 10 seconds
        //if (step7Timer > 10f && !step7Skip.gameObject.activeSelf)
        //    step7Skip.gameObject.SetActive(true);

        if (!micAvailable) return;

        float level = GetMicLevel();
        step7LevelText.text = $"Mic Level: {Mathf.RoundToInt(level * 100)}%";

        // Update bar fill
        RectTransform fillRect = step7BarFill.GetComponent<RectTransform>();
        fillRect.anchorMax = new Vector2(Mathf.Clamp01(level), 1f);

        // Sample every 0.1 seconds into history
        micSampleTimer += Time.deltaTime;
        if (micSampleTimer >= 0.1f)
        {
            micSampleTimer = 0f;
            micHistory.Add(level);

            // Keep last 3 seconds (30 samples at 0.1s each)
            if (micHistory.Count > 30)
                micHistory.RemoveAt(0);

            // Calculate average of current 3-second window
            if (micHistory.Count >= 30)
            {
                float avg = 0f;
                foreach (float l in micHistory) avg += l;
                avg /= micHistory.Count;

                if (avg > micPeakAvg)
                    micPeakAvg = avg;

                // NEXT only appears once mic level average hits ~70%
                if (!step7Next.gameObject.activeSelf && micPeakAvg >= 0.70f)
                    step7Next.gameObject.SetActive(true);
            }
        }
    }

    private void UpdateStep8()
    {
        step7Timer += Time.deltaTime; // reuse timer for skip

        if (step7Timer > 15f && !step8Skip.gameObject.activeSelf)
            step8Skip.gameObject.SetActive(true);

        if (langIdx >= langTotalPairs)
        {
            // All languages done
            if (!step8Next.gameObject.activeSelf)
            {
                step8Prompt.text = "Language calibration complete.";
                step8Next.gameObject.SetActive(true);
            }
            return;
        }

        if (!micAvailable)
        {
            // Auto-advance without mic after a pause (fallback)
            langSpeakTimer += Time.deltaTime;
            if (langSpeakTimer > 3f)
                AdvanceLanguage();
            return;
        }

        float level = GetMicLevel();

        // Pulsing record dot
        float dotAlpha = (Mathf.Sin(Time.time * 4f) * 0.3f + 0.5f);
        step8Dot.color = new Color(0.9f, 0.1f, 0.1f, dotAlpha);

        // ── Syllable detection via mic gain peaks ──
        int expected = (langIdx < expectedSyllablesPerLang.Length)
            ? expectedSyllablesPerLang[langIdx] : 3;

        syllableCooldown -= Time.deltaTime;

        if (level > syllableThreshold && !wasAboveSyllableThreshold && syllableCooldown <= 0f)
        {
            // Rising edge — voice onset
            wasAboveSyllableThreshold = true;
        }
        else if (level < syllableThreshold * 0.6f && wasAboveSyllableThreshold)
        {
            // Falling edge — syllable ended
            wasAboveSyllableThreshold = false;
            detectedSyllables++;
            syllableCooldown = SYLLABLE_COOLDOWN_TIME;
        }

        // Visual feedback
        if (level > syllableThreshold)
            step8Prompt.text = $"Hearing you... syllables: {detectedSyllables} / {expected}";
        else
            step8Prompt.text = $"Speak the words now.  ({detectedSyllables} / {expected} syllables)";

        // Advance when all syllables detected
        if (detectedSyllables >= expected)
        {
            AdvanceLanguage();
        }
    }

    private void ShowCurrentLanguage()
    {
        if (langIdx >= langTotalPairs)
        {
            step8LangName.text = "";
            step8Words.text = "All done!";
            return;
        }

        int expected = (langIdx < expectedSyllablesPerLang.Length)
            ? expectedSyllablesPerLang[langIdx] : 3;

        step8LangName.text = $"Language {langIdx + 1} of {langTotalPairs}: {langNames[langIdx]}";
        step8Words.text = $"{langWords[langIdx][0]}  {langWords[langIdx][1]}";
        step8Prompt.text = $"Speak the words now.  ({expected} syllables)";
        langSpeakTimer = 0f;
    }

    private void AdvanceLanguage()
    {
        langIdx++;
        langSpeakTimer = 0f;
        detectedSyllables = 0;
        wasAboveSyllableThreshold = false;
        syllableCooldown = 0f;
        ShowCurrentLanguage();
    }

    private void UpdateStep9()
    {
        float v = step9Slider.value;
        // Hide/show compression blocks based on slider
        int totalBlocks = compBlocks.Count;
        int visibleBlocks = Mathf.RoundToInt((1f - v) * totalBlocks);
        for (int i = 0; i < totalBlocks; i++)
        {
            if (compBlocks[i] != null)
            {
                compBlocks[i].gameObject.SetActive(i < visibleBlocks);
                // Fade effect for blocks near the threshold
                if (i < visibleBlocks)
                {
                    Color bc = compBlocks[i].color;
                    bc.a = (i < visibleBlocks - 10) ? 1f : 0.5f;
                    compBlocks[i].color = bc;
                }
            }
        }
    }

    private void UpdateStep10()
    {
        float v = step10Slider.value;
        // Control particle visibility based on slider
        int total = particles.Count;
        int visible = Mathf.RoundToInt(v * total);
        for (int i = 0; i < total; i++)
        {
            if (particles[i] != null)
                particles[i].gameObject.SetActive(i < visible);
        }

        // Animate visible particles (drift)
        for (int i = 0; i < visible && i < total; i++)
        {
            if (particles[i] == null) continue;
            Vector2 a = particles[i].anchorMin;
            a.x += Mathf.Sin(Time.time * (1f + i * 0.1f)) * 0.0005f;
            a.y += Mathf.Cos(Time.time * (0.7f + i * 0.07f)) * 0.0005f;
            a.x = Mathf.Repeat(a.x, 1f);
            a.y = Mathf.Repeat(a.y, 1f);
            particles[i].anchorMin = a;
            particles[i].anchorMax = a;
        }
    }

    // =====================================================================
    // MICROPHONE
    // =====================================================================

    private void StartMic()
    {
        micAvailable = false;

#if !UNITY_WEBGL
        if (Microphone.devices.Length == 0) return;

        micDevice = Microphone.devices[0];
        micClip = Microphone.Start(micDevice, true, 10, 44100);
        micAvailable = true;
#endif
    }

    private void StopMic()
    {
#if !UNITY_WEBGL
        if (micDevice != null && Microphone.IsRecording(micDevice))
            Microphone.End(micDevice);
#endif
        micAvailable = false;
    }

    private float GetMicLevel()
    {
#if !UNITY_WEBGL
        if (micClip == null || micDevice == null) return 0f;
        int pos = Microphone.GetPosition(micDevice);
        if (pos <= 0) return 0f;

        int sampleSize = 256;
        float[] samples = new float[sampleSize];
        int start = Mathf.Max(0, pos - sampleSize);
        micClip.GetData(samples, start);

        float sum = 0f;
        for (int i = 0; i < sampleSize; i++)
            sum += samples[i] * samples[i];

        return Mathf.Sqrt(sum / sampleSize); // RMS
#else
        return 0f;
#endif
    }

    // =====================================================================
    // AUDIO GENERATION
    // =====================================================================

    private AudioClip GenerateTone(float freq, float duration)
    {
        int sampleRate = 44100;
        int samples = (int)(sampleRate * duration);
        float[] data = new float[samples];
        for (int i = 0; i < samples; i++)
            data[i] = Mathf.Sin(2f * Mathf.PI * freq * i / sampleRate) * 0.5f;

        AudioClip clip = AudioClip.Create($"tone_{freq}", samples, 1, sampleRate, false);
        clip.SetData(data, 0);
        return clip;
    }

    private AudioSource MakeAudioSource(string name, AudioClip clip)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(transform);
        AudioSource src = obj.AddComponent<AudioSource>();
        src.clip = clip;
        src.loop = true;
        src.playOnAwake = false;
        src.volume = 0f;
        return src;
    }

    // =====================================================================
    // UI BUILDER HELPERS
    // =====================================================================

    private GameObject MakeStepPanel(int index, string title, string description)
    {
        GameObject panel = MakeChild(settingsCanvas.gameObject, $"Step{index + 1}Panel");
        Pos(panel, Vector2.zero, Vector2.one);
        Image bg = panel.AddComponent<Image>();
        bg.color = new Color(0.06f, 0.06f, 0.1f, 1f);

        // Step counter
        MakeText(panel, $"STEP {index + 1} OF {TOTAL_STEPS}", 18, TextAnchor.MiddleCenter,
            new Color(0.5f, 0.5f, 0.5f), new Vector2(0.3f, 0.93f), new Vector2(0.7f, 0.97f));

        // Title
        MakeText(panel, title, 34, TextAnchor.MiddleCenter,
            new Color(0.9f, 0.9f, 0.9f), new Vector2(0.05f, 0.85f), new Vector2(0.95f, 0.93f));

        // Description
        MakeText(panel, description, 18, TextAnchor.MiddleCenter,
            new Color(0.6f, 0.6f, 0.6f), new Vector2(0.1f, 0.7f), new Vector2(0.9f, 0.85f));

        stepPanels[index] = panel;
        return panel;
    }

    private Slider MakeSlider(GameObject parent, string label, float yCenter)
    {
        float h = 0.06f;
        // Label
        MakeText(parent, label, 16, TextAnchor.LowerLeft, new Color(0.75f, 0.75f, 0.75f),
            new Vector2(0.15f, yCenter + h * 0.2f), new Vector2(0.85f, yCenter + h + 0.02f));

        // Slider root
        GameObject sliderObj = MakeChild(parent, $"Slider_{label.Replace(" ", "")}");
        Pos(sliderObj, new Vector2(0.15f, yCenter - h), new Vector2(0.85f, yCenter + h * 0.2f));
        sliderObj.AddComponent<Image>().color = new Color(0.12f, 0.12f, 0.18f);

        // Track
        GameObject track = MakeChild(sliderObj, "Track");
        RectTransform trackRect = track.GetComponent<RectTransform>();
        trackRect.anchorMin = new Vector2(0f, 0.2f);
        trackRect.anchorMax = new Vector2(1f, 0.8f);
        trackRect.offsetMin = new Vector2(8, 0);
        trackRect.offsetMax = new Vector2(-8, 0);
        track.AddComponent<Image>().color = new Color(0.22f, 0.22f, 0.28f);

        // Fill area
        GameObject fillArea = MakeChild(sliderObj, "FillArea");
        RectTransform faRect = fillArea.GetComponent<RectTransform>();
        faRect.anchorMin = new Vector2(0f, 0.2f);
        faRect.anchorMax = new Vector2(1f, 0.8f);
        faRect.offsetMin = new Vector2(8, 0);
        faRect.offsetMax = new Vector2(-8, 0);

        GameObject fill = MakeChild(fillArea, "Fill");
        RectTransform fillRect = fill.GetComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = new Vector2(0f, 1f);
        fillRect.offsetMin = Vector2.zero;
        fillRect.offsetMax = Vector2.zero;
        fill.AddComponent<Image>().color = new Color(0.3f, 0.5f, 0.8f, 0.7f);

        // Handle slide area
        GameObject handleArea = MakeChild(sliderObj, "HandleSlideArea");
        RectTransform haRect = handleArea.GetComponent<RectTransform>();
        haRect.anchorMin = Vector2.zero;
        haRect.anchorMax = Vector2.one;
        haRect.offsetMin = new Vector2(10, 0);
        haRect.offsetMax = new Vector2(-10, 0);

        GameObject handle = MakeChild(handleArea, "Handle");
        RectTransform hRect = handle.GetComponent<RectTransform>();
        hRect.anchorMin = new Vector2(0f, 0f);
        hRect.anchorMax = new Vector2(0f, 1f);
        hRect.pivot = new Vector2(0.5f, 0.5f);
        hRect.sizeDelta = new Vector2(20, 0);
        Image handleImg = handle.AddComponent<Image>();
        handleImg.color = new Color(0.85f, 0.85f, 0.95f);

        Slider slider = sliderObj.AddComponent<Slider>();
        slider.fillRect = fillRect;
        slider.handleRect = hRect;
        slider.targetGraphic = handleImg;
        slider.direction = Slider.Direction.LeftToRight;
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.value = 0.5f;

        return slider;
    }

    private Button MakeButton(GameObject parent, string label, Vector2 anchorCenter,
        Vector2 size, Color bgColor)
    {
        GameObject obj = MakeChild(parent, $"Btn_{label.Replace(" ", "")}");
        RectTransform rt = obj.GetComponent<RectTransform>();
        rt.anchorMin = anchorCenter;
        rt.anchorMax = anchorCenter;
        rt.sizeDelta = size;
        rt.anchoredPosition = Vector2.zero;

        Image bg = obj.AddComponent<Image>();
        bg.color = bgColor;

        Button btn = obj.AddComponent<Button>();
        ColorBlock cb = btn.colors;
        cb.highlightedColor = new Color(
            Mathf.Min(bgColor.r + 0.15f, 1f),
            Mathf.Min(bgColor.g + 0.15f, 1f),
            Mathf.Min(bgColor.b + 0.15f, 1f), 1f);
        cb.pressedColor = new Color(bgColor.r * 0.7f, bgColor.g * 0.7f, bgColor.b * 0.7f, 1f);
        btn.colors = cb;

        GameObject textObj = MakeChild(obj, "Text");
        Pos(textObj, Vector2.zero, Vector2.one);
        Text text = textObj.AddComponent<Text>();
        text.text = label;
        text.font = defaultFont;
        text.fontSize = 22;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = Color.white;

        return btn;
    }

    private Text MakeText(GameObject parent, string content, int fontSize,
        TextAnchor anchor, Color color, Vector2 ancMin, Vector2 ancMax)
    {
        GameObject obj = MakeChild(parent, $"Txt_{content.GetHashCode():X8}");
        Pos(obj, ancMin, ancMax);
        Text text = obj.AddComponent<Text>();
        text.text = content;
        text.font = defaultFont;
        text.fontSize = fontSize;
        text.alignment = anchor;
        text.color = color;
        text.raycastTarget = false;
        return text;
    }

    private GameObject MakeFullOverlay(GameObject parent, string name, Color color)
    {
        GameObject obj = MakeChild(parent, name);
        Pos(obj, Vector2.zero, Vector2.one);
        Image img = obj.AddComponent<Image>();
        img.color = color;
        return obj;
    }

    private GameObject MakeChild(GameObject parent, string name)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent.transform, false);
        obj.AddComponent<RectTransform>();
        return obj;
    }

    private void Pos(GameObject obj, Vector2 ancMin, Vector2 ancMax)
    {
        RectTransform rt = obj.GetComponent<RectTransform>();
        if (rt == null) rt = obj.AddComponent<RectTransform>();
        rt.anchorMin = ancMin;
        rt.anchorMax = ancMax;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }
}
