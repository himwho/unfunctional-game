using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// LEVEL 12: Over-achiever level. Constant achievement popups for the most 
/// trivial actions. The player must collect a certain number of "real" achievements
/// while being bombarded by fake ones for breathing, blinking, existing, etc.
/// 
/// This is a placeholder scaffold - full implementation when level design is finalized.
/// Attach to a root GameObject in the LEVEL12 scene.
/// </summary>
public class Level12_Achievements : LevelManager
{
    [Header("Level 12 - Achievement Abuse")]
    public Canvas achievementCanvas;
    public GameObject achievementPopupPrefab;   // Template for achievement popup
    public Transform achievementPopupParent;    // Container for popups

    [Header("Settings")]
    public float trivialAchievementInterval = 3f;
    public int realAchievementsNeeded = 5;

    [Header("Trivial Achievements")]
    public string[] trivialAchievements = new string[]
    {
        "Achievement Unlocked: You Opened Your Eyes",
        "Achievement Unlocked: Breathing Manually",
        "Achievement Unlocked: Standing Still",
        "Achievement Unlocked: Existing",
        "Achievement Unlocked: Reading This Achievement",
        "Achievement Unlocked: Moving Your Mouse",
        "Achievement Unlocked: Pressing A Key",
        "Achievement Unlocked: Having A Computer",
        "Achievement Unlocked: Gravity Is Working",
        "Achievement Unlocked: Achievement Achievement",
        "Achievement Unlocked: Looking At Nothing",
        "Achievement Unlocked: Wasting Time",
        "Achievement Unlocked: Achievement Overload",
        "Achievement Unlocked: Still Playing",
        "Achievement Unlocked: Patience",
        "Achievement Unlocked: You Blinked",
        "Achievement Unlocked: Oxygen Intake Nominal",
        "Achievement Unlocked: Thinking About Achievements",
        "Achievement Unlocked: Not Quitting Yet",
        "Achievement Unlocked: Screen Starer"
    };

    private float trivialTimer;
    private int realAchievementsCollected = 0;
    private int trivialAchievementIndex = 0;

    protected override void Start()
    {
        base.Start();
        levelDisplayName = "Achievement Hunter";
        levelDescription = "Collect the real achievements. Ignore the rest. Good luck telling them apart.";

        trivialTimer = trivialAchievementInterval;
    }

    private void Update()
    {
        if (levelComplete) return;

        // Spam trivial achievements
        trivialTimer -= Time.deltaTime;
        if (trivialTimer <= 0f)
        {
            ShowTrivialAchievement();
            trivialTimer = trivialAchievementInterval + Random.Range(-1f, 1f);

            // Speed up over time for maximum annoyance
            if (trivialAchievementInterval > 0.5f)
                trivialAchievementInterval *= 0.95f;
        }

        // Check completion
        if (realAchievementsCollected >= realAchievementsNeeded)
        {
            CompleteLevel();
        }
    }

    private void ShowTrivialAchievement()
    {
        if (trivialAchievements.Length == 0) return;

        string achievement = trivialAchievements[trivialAchievementIndex % trivialAchievements.Length];
        trivialAchievementIndex++;

        ShowAchievementPopup(achievement, false);
    }

    public void UnlockRealAchievement(string achievementName)
    {
        realAchievementsCollected++;
        ShowAchievementPopup($"REAL Achievement: {achievementName}", true);
        Debug.Log($"[Level12] Real achievement unlocked: {achievementName} ({realAchievementsCollected}/{realAchievementsNeeded})");
    }

    private void ShowAchievementPopup(string text, bool isReal)
    {
        Debug.Log($"[Level12] {text}");

        // If we have a prefab and parent, instantiate a popup
        if (achievementPopupPrefab != null && achievementPopupParent != null)
        {
            GameObject popup = Instantiate(achievementPopupPrefab, achievementPopupParent);
            Text popupText = popup.GetComponentInChildren<Text>();
            if (popupText != null)
                popupText.text = text;

            // Color code: real = gold, trivial = grey
            Image bg = popup.GetComponent<Image>();
            if (bg != null)
                bg.color = isReal ? new Color(1f, 0.84f, 0f, 0.9f) : new Color(0.3f, 0.3f, 0.3f, 0.7f);

            // Auto-destroy after a few seconds
            Destroy(popup, 3f);
        }
    }
}
