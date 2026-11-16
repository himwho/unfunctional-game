using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Pause menu overlay. Listens to GameManager state changes to show/hide.
/// Create a Canvas with this script in the GLOBAL scene with a Panel + buttons.
/// Named GamePauseMenu to avoid collision with SampleScenes/Menu/Scripts/PauseMenu.cs
/// </summary>
public class GamePauseMenu : MonoBehaviour
{
    [Header("UI References")]
    public GameObject pausePanel;
    public Button resumeButton;
    public Button restartButton;
    public Button quitButton;
    public Button debugLevelsButton;

    private void Start()
    {
        if (pausePanel != null)
            pausePanel.SetActive(false);

        // Wire up buttons
        if (resumeButton != null)
            resumeButton.onClick.AddListener(OnResumeClicked);
        if (restartButton != null)
            restartButton.onClick.AddListener(OnRestartClicked);
        if (quitButton != null)
            quitButton.onClick.AddListener(OnQuitClicked);

        if (debugLevelsButton != null)
        {
            debugLevelsButton.gameObject.SetActive(DebugPanel.Instance != null && DebugPanel.Instance.IsAvailable);
            debugLevelsButton.onClick.AddListener(() => DebugPanel.Instance?.Open());
        }
        Subscribe();
    }

    private GameManager subscribedManager;

    private void Subscribe()
    {
        if (subscribedManager != null || GameManager.Instance == null) return;
        subscribedManager = GameManager.Instance;
        subscribedManager.OnGameStateChanged += HandleStateChanged;
    }

    private void OnEnable()
    {
        Subscribe();
    }

    private void OnDisable()
    {
        if (subscribedManager != null)
        {
            subscribedManager.OnGameStateChanged -= HandleStateChanged;
            subscribedManager = null;
        }
    }

    private void HandleStateChanged(GameManager.GameState newState)
    {
        if (pausePanel == null) return;

        bool showPause = newState == GameManager.GameState.Paused && !(DebugPanel.Instance != null && DebugPanel.Instance.IsOpen);
        pausePanel.SetActive(showPause);

        if (showPause)
        {
            // Unlock cursor so the player can click pause menu buttons
            if (InputManager.Instance != null)
                InputManager.Instance.UnlockCursor();
        }
        else if (newState == GameManager.GameState.Playing ||
                 newState == GameManager.GameState.MainMenu)
        {
            // Restore the active level's cursor preference instead of
            // blindly locking. UI levels need the cursor free; 3D levels
            // need it locked. The LevelManager knows which.
            LevelManager lm = FindAnyObjectByType<LevelManager>();
            if (lm != null)
            {
                lm.ApplyCursorState();
            }
        }
    }

    private void OnResumeClicked()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.TogglePause();
        }
    }

    private void OnRestartClicked()
    {
        // Unpause first
        Time.timeScale = 1f;

        if (GameManager.Instance != null)
        {
            GameManager.Instance.ReloadCurrentLevel();
        }
    }

    private void OnQuitClicked()
    {
        Debug.Log("[GamePauseMenu] Quit requested");
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    /// <summary>
    /// Show or hide the restart button. Used by levels that want to prevent
    /// restarting (e.g., Level 11's RNG punishment mechanic).
    /// </summary>
    public void SetRestartButtonEnabled(bool enabled)
    {
        if (restartButton != null)
            restartButton.gameObject.SetActive(enabled);
    }
}
