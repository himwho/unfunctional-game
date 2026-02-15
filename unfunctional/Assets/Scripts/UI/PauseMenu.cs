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

        // Listen for state changes
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnGameStateChanged += HandleStateChanged;
        }
    }

    private void OnEnable()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnGameStateChanged += HandleStateChanged;
        }
    }

    private void OnDisable()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnGameStateChanged -= HandleStateChanged;
        }
    }

    private void HandleStateChanged(GameManager.GameState newState)
    {
        if (pausePanel == null) return;

        bool showPause = (newState == GameManager.GameState.Paused);
        pausePanel.SetActive(showPause);

        if (showPause)
        {
            if (InputManager.Instance != null)
                InputManager.Instance.UnlockCursor();
        }
        else
        {
            // Re-lock cursor if going back to gameplay (not main menu)
            if (newState == GameManager.GameState.Playing && InputManager.Instance != null)
                InputManager.Instance.LockCursor();
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
}
