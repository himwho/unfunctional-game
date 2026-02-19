using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// LEVEL 1: A stupidly confusing MainMenu UI system.
/// The "Start Game" button is deliberately hidden, moving, 
/// mislabeled, or buried in nonsensical sub-menus.
/// 
/// Attach to a root GameObject in LEVEL1 scene with a Canvas child.
/// </summary>
public class Level1_ConfusingMenu : LevelManager
{
    [Header("Level 1 - Confusing Menu")]
    public Canvas menuCanvas;
    public RectTransform mainPanel;

    [Header("Fake Buttons")]
    public Button[] fakeStartButtons;      // Buttons that look like Start but aren't
    public Button[] decoyButtons;          // Random useless buttons
    public Button realStartButton;         // The actual start button (tiny, hidden, mislabeled)

    [Header("Annoyance Settings")]
    public float buttonMoveSpeed = 100f;
    public float buttonMoveInterval = 2f;
    public float menuRotateSpeed = 5f;
    public bool enableMenuDrift = true;
    public bool enableButtonFlee = true;   // Real button runs away from cursor

    [Header("Sub-Menu Trap")]
    public GameObject[] subMenuPanels;     // Fake sub-menus that open for no reason
    public float subMenuPopupInterval = 8f;

    [Header("Completion Feedback")]
    public Text completionText;            // Optional: assigned in editor or created at runtime

    private RectTransform realButtonRect;
    private bool realButtonFound = false;
    private float subMenuTimer;

    protected override void Start()
    {
        wantsCursorLocked = false; // UI level
        base.Start(); // calls ApplyCursorState()
        levelDisplayName = "Main Menu";
        levelDescription = "Find the Start Game button. Good luck.";

        // Create an oversized background that covers the screen even when
        // the panel rotates. Placed behind all children so buttons are unaffected.
        CreateOversizedBackground();

        SetupFakeButtons();
        SetupRealButton();
        SetupSubMenus();

        subMenuTimer = subMenuPopupInterval;
    }

    /// <summary>
    /// Adds a large background image inside the MainPanel that extends well
    /// beyond the screen edges. When the panel rotates/drifts the background
    /// still fills the viewport, hiding the black void behind it.
    /// Placed as the first sibling so it renders behind every other child.
    /// </summary>
    private void CreateOversizedBackground()
    {
        if (mainPanel == null) return;

        GameObject bg = new GameObject("OversizedBackground");
        bg.transform.SetParent(mainPanel, false);
        bg.transform.SetAsFirstSibling(); // render behind everything

        RectTransform bgRT = bg.AddComponent<RectTransform>();
        bgRT.anchorMin = new Vector2(0.5f, 0.5f);
        bgRT.anchorMax = new Vector2(0.5f, 0.5f);
        bgRT.pivot = new Vector2(0.5f, 0.5f);
        bgRT.sizeDelta = new Vector2(4000, 4000); // ~2x diagonal of 1920×1080
        bgRT.anchoredPosition = Vector2.zero;

        Image bgImg = bg.AddComponent<Image>();
        bgImg.raycastTarget = false; // don't block button clicks

        // Copy the panel's own visual so it looks seamless
        Image panelImg = mainPanel.GetComponent<Image>();
        if (panelImg != null)
        {
            bgImg.color = panelImg.color;
            bgImg.sprite = panelImg.sprite;
            bgImg.material = panelImg.material;
            bgImg.type = panelImg.type;
        }
        else
        {
            // Fallback: dark menu-style background
            bgImg.color = new Color(0.12f, 0.12f, 0.15f, 1f);
        }
    }

    private void Update()
    {
        if (levelComplete || realButtonFound) return;

        // Slowly rotate the entire menu for maximum confusion
        if (enableMenuDrift && mainPanel != null)
        {
            mainPanel.Rotate(0, 0, Mathf.Sin(Time.time * 0.5f) * menuRotateSpeed * Time.deltaTime);
        }

        // Make the real start button flee from the mouse cursor
        if (enableButtonFlee && realButtonRect != null)
        {
            FleeFromCursor();
        }

        // Random sub-menu popups
        subMenuTimer -= Time.deltaTime;
        if (subMenuTimer <= 0f)
        {
            PopRandomSubMenu();
            subMenuTimer = subMenuPopupInterval + Random.Range(-2f, 4f);
        }
    }

    private void SetupFakeButtons()
    {
        if (fakeStartButtons == null) return;

        foreach (Button btn in fakeStartButtons)
        {
            if (btn == null) continue;
            btn.onClick.AddListener(() => OnFakeStartClicked(btn));
        }
    }

    private void SetupRealButton()
    {
        if (realStartButton == null) return;

        realButtonRect = realStartButton.GetComponent<RectTransform>();
        realStartButton.onClick.AddListener(OnRealStartClicked);
    }

    private void SetupSubMenus()
    {
        if (subMenuPanels == null) return;

        foreach (GameObject panel in subMenuPanels)
        {
            if (panel != null)
                panel.SetActive(false);
        }
    }

    private void OnFakeStartClicked(Button btn)
    {
        Debug.Log("[Level1] Fake start button clicked! Nice try.");

        // Change the button text to something taunting
        Text btnText = btn.GetComponentInChildren<Text>();
        if (btnText != null)
        {
            string[] taunts = new string[]
            {
                "Nope!",
                "Try again!",
                "Not this one!",
                "Haha!",
                "So close!",
                "Wrong!",
                "Keep looking!",
                "Almost!",
                "Really?",
                "That's not it!"
            };
            btnText.text = taunts[Random.Range(0, taunts.Length)];
        }

        // Move the real button to a new random position
        if (realButtonRect != null && mainPanel != null)
        {
            Vector2 randomPos = new Vector2(
                Random.Range(-300f, 300f),
                Random.Range(-200f, 200f)
            );
            realButtonRect.anchoredPosition = randomPos;
        }

        // Maybe pop open a sub-menu
        if (Random.value > 0.5f)
        {
            PopRandomSubMenu();
        }
    }

    private void OnRealStartClicked()
    {
        if (levelComplete || realButtonFound) return;

        Debug.Log("[Level1] Real start button found! Completing level.");
        realButtonFound = true;

        // Disable all interactable buttons so nothing else can be clicked
        DisableAllButtons();

        // Close any open sub-menus
        CloseAllSubMenus();

        // Show completion feedback
        StartCoroutine(ShowCompletionAndAdvance());
    }

    private IEnumerator ShowCompletionAndAdvance()
    {
        // Flash the real button green to confirm the click
        if (realStartButton != null)
        {
            Image btnImage = realStartButton.GetComponent<Image>();
            if (btnImage != null)
            {
                btnImage.color = new Color(0.1f, 0.8f, 0.1f, 1f);
            }
            Text btnText = realStartButton.GetComponentInChildren<Text>();
            if (btnText != null)
            {
                btnText.text = "FOUND IT!";
                btnText.fontSize = 14;
                btnText.color = Color.white;
            }
            // Grow the button so it's actually visible
            RectTransform rt = realStartButton.GetComponent<RectTransform>();
            if (rt != null)
            {
                rt.sizeDelta = new Vector2(200, 50);
            }
        }

        yield return new WaitForSeconds(0.3f);

        // Show a centered "GAME STARTING..." message
        if (completionText != null)
        {
            completionText.gameObject.SetActive(true);
            completionText.text = "GAME STARTING...";
        }
        else if (mainPanel != null)
        {
            // Create one on the fly if not assigned
            GameObject msgObj = new GameObject("CompletionMsg");
            msgObj.transform.SetParent(mainPanel, false);
            RectTransform msgRect = msgObj.AddComponent<RectTransform>();
            msgRect.anchorMin = new Vector2(0.2f, 0.4f);
            msgRect.anchorMax = new Vector2(0.8f, 0.6f);
            msgRect.offsetMin = Vector2.zero;
            msgRect.offsetMax = Vector2.zero;

            Text msgText = msgObj.AddComponent<Text>();
            msgText.text = "GAME STARTING...";
            msgText.fontSize = 56;
            msgText.font = UIHelper.GetDefaultFont();
            msgText.alignment = TextAnchor.MiddleCenter;
            msgText.color = new Color(0.2f, 1f, 0.2f, 1f);
        }

        // Stop the menu rotation
        enableMenuDrift = false;

        // Brief pause for the player to read the message,
        // then the GameManager fade-to-black + scene unload handles the rest
        yield return new WaitForSeconds(1.0f);

        CompleteLevel();
    }

    private void DisableAllButtons()
    {
        if (fakeStartButtons != null)
        {
            foreach (Button btn in fakeStartButtons)
            {
                if (btn != null) btn.interactable = false;
            }
        }
        if (decoyButtons != null)
        {
            foreach (Button btn in decoyButtons)
            {
                if (btn != null) btn.interactable = false;
            }
        }
        // Don't disable the real button -- it just got clicked and should look "activated"
    }

    private void CloseAllSubMenus()
    {
        if (subMenuPanels == null) return;
        foreach (GameObject panel in subMenuPanels)
        {
            if (panel != null)
                panel.SetActive(false);
        }
    }

    private void FleeFromCursor()
    {
        if (realButtonRect == null || menuCanvas == null) return;

        Vector2 mousePos;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            mainPanel, Input.mousePosition, menuCanvas.worldCamera, out mousePos);

        Vector2 buttonPos = realButtonRect.anchoredPosition;
        float dist = Vector2.Distance(mousePos, buttonPos);

        // If cursor gets close, run away
        if (dist < 150f)
        {
            Vector2 awayDir = (buttonPos - mousePos).normalized;
            Vector2 newPos = buttonPos + awayDir * buttonMoveSpeed * Time.deltaTime;

            // Clamp within panel bounds (roughly)
            newPos.x = Mathf.Clamp(newPos.x, -400f, 400f);
            newPos.y = Mathf.Clamp(newPos.y, -250f, 250f);

            realButtonRect.anchoredPosition = newPos;
        }
    }

    private void PopRandomSubMenu()
    {
        if (subMenuPanels == null || subMenuPanels.Length == 0) return;

        CloseAllSubMenus();

        int idx = Random.Range(0, subMenuPanels.Length);
        if (subMenuPanels[idx] != null)
        {
            subMenuPanels[idx].SetActive(true);
            StartCoroutine(CloseSubMenuAfterDelay(subMenuPanels[idx], Random.Range(2f, 5f)));
        }
    }

    private IEnumerator CloseSubMenuAfterDelay(GameObject panel, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (panel != null)
            panel.SetActive(false);
    }
}
