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

    private RectTransform realButtonRect;
    private bool realButtonFound = false;
    private float subMenuTimer;

    protected override void Start()
    {
        base.Start();
        levelDisplayName = "Main Menu";
        levelDescription = "Find the Start Game button. Good luck.";

        // Unlock cursor for menu interaction
        if (InputManager.Instance != null)
        {
            InputManager.Instance.UnlockCursor();
        }
        else
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        SetupFakeButtons();
        SetupRealButton();
        SetupSubMenus();

        subMenuTimer = subMenuPopupInterval;
    }

    private void Update()
    {
        if (levelComplete) return;

        // Slowly rotate the entire menu for maximum confusion
        if (enableMenuDrift && mainPanel != null)
        {
            mainPanel.Rotate(0, 0, Mathf.Sin(Time.time * 0.5f) * menuRotateSpeed * Time.deltaTime);
        }

        // Make the real start button flee from the mouse cursor
        if (enableButtonFlee && realButtonRect != null && !realButtonFound)
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

        // Do something annoying:
        // - Change the button text to something taunting
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
        if (levelComplete) return;

        Debug.Log("[Level1] Real start button found! Completing level.");
        realButtonFound = true;

        // Brief celebration, then move on
        StartCoroutine(CompleteLevelAfterDelay(0.5f));
    }

    private IEnumerator CompleteLevelAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        CompleteLevel();
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

        // Close all first
        foreach (GameObject panel in subMenuPanels)
        {
            if (panel != null)
                panel.SetActive(false);
        }

        // Open a random one
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
