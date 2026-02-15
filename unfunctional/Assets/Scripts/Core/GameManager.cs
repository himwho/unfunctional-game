using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Singleton GameManager that persists across all scenes.
/// Lives in the GLOBAL scene which is always loaded.
/// Tracks game state, current level, and coordinates between systems.
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Level Configuration")]
    [Tooltip("Scene names in order. Index 0 = LEVEL1, etc.")]
    public string[] levelSceneNames = new string[]
    {
        "LEVEL1",
        "LEVEL2",
        "LEVEL3",
        "LEVEL4",
        "LEVEL5",
        "LEVEL6"
    };

    [Header("Transition")]
    public float fadeOutDuration = 0.5f;
    public float fadeInDuration = 0.5f;
    public Color fadeColor = Color.black;

    [Header("State")]
    [SerializeField] private int currentLevelIndex = -1; // -1 means no level loaded yet
    [SerializeField] private GameState currentState = GameState.Boot;

    public int CurrentLevelIndex => currentLevelIndex;
    public GameState CurrentState => currentState;

    public enum GameState
    {
        Boot,       // Initial state, nothing loaded
        MainMenu,   // LEVEL1 - the confusing main menu
        Playing,    // Active gameplay in a level
        Paused,     // Game is paused
        Transition  // Loading between levels
    }

    // Events for other systems to hook into
    public delegate void GameStateChanged(GameState newState);
    public event GameStateChanged OnGameStateChanged;

    public delegate void LevelLoaded(int levelIndex, string levelName);
    public event LevelLoaded OnLevelLoaded;

    // Transition overlay (created at runtime)
    private Canvas transitionCanvas;
    private Image fadeOverlay;
    private CanvasGroup fadeCanvasGroup;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        CreateTransitionOverlay();
    }

    private void Start()
    {
        // On boot, load LEVEL1 (the main menu) additively
        LoadLevel(0);
    }

    /// <summary>
    /// Load a level by index (0-based, maps to levelSceneNames array).
    /// Unloads the previous level scene first with a fade transition.
    /// </summary>
    public void LoadLevel(int levelIndex)
    {
        if (levelIndex < 0 || levelIndex >= levelSceneNames.Length)
        {
            Debug.LogError($"[GameManager] Invalid level index: {levelIndex}");
            return;
        }

        SetState(GameState.Transition);
        StartCoroutine(LoadLevelRoutine(levelIndex));
    }

    /// <summary>
    /// Advance to the next level in sequence.
    /// </summary>
    public void LoadNextLevel()
    {
        int nextIndex = currentLevelIndex + 1;
        if (nextIndex >= levelSceneNames.Length)
        {
            Debug.Log("[GameManager] All levels completed!");
            return;
        }
        LoadLevel(nextIndex);
    }

    /// <summary>
    /// Reload the current level.
    /// </summary>
    public void ReloadCurrentLevel()
    {
        if (currentLevelIndex >= 0)
        {
            LoadLevel(currentLevelIndex);
        }
    }

    public void SetState(GameState newState)
    {
        currentState = newState;
        OnGameStateChanged?.Invoke(newState);

        if (newState == GameState.Paused)
        {
            Time.timeScale = 0f;
        }
        else if (newState == GameState.Playing || newState == GameState.MainMenu)
        {
            Time.timeScale = 1f;
        }
    }

    public void TogglePause()
    {
        if (currentState == GameState.Playing)
        {
            SetState(GameState.Paused);
        }
        else if (currentState == GameState.Paused)
        {
            SetState(GameState.Playing);
        }
    }

    private IEnumerator LoadLevelRoutine(int levelIndex)
    {
        // Fade out
        yield return StartCoroutine(Fade(0f, 1f, fadeOutDuration));

        // Unload current level if one is loaded
        if (currentLevelIndex >= 0 && currentLevelIndex < levelSceneNames.Length)
        {
            string currentSceneName = levelSceneNames[currentLevelIndex];
            Scene currentScene = SceneManager.GetSceneByName(currentSceneName);
            if (currentScene.isLoaded)
            {
                AsyncOperation unload = SceneManager.UnloadSceneAsync(currentScene);
                if (unload != null)
                {
                    while (!unload.isDone)
                        yield return null;
                }
            }
        }

        // Load new level additively (GLOBAL stays loaded)
        string newSceneName = levelSceneNames[levelIndex];
        AsyncOperation load = SceneManager.LoadSceneAsync(newSceneName, LoadSceneMode.Additive);
        if (load != null)
        {
            while (!load.isDone)
                yield return null;
        }

        currentLevelIndex = levelIndex;

        // Set the loaded level as the active scene (for lighting, etc.)
        Scene loadedScene = SceneManager.GetSceneByName(newSceneName);
        if (loadedScene.IsValid())
        {
            SceneManager.SetActiveScene(loadedScene);
        }

        // Determine state based on level
        if (levelIndex == 0)
        {
            SetState(GameState.MainMenu);
        }
        else
        {
            SetState(GameState.Playing);
        }

        OnLevelLoaded?.Invoke(levelIndex, newSceneName);
        Debug.Log($"[GameManager] Loaded level: {newSceneName} (index {levelIndex})");

        // Fade in
        yield return StartCoroutine(Fade(1f, 0f, fadeInDuration));
    }

    // =========================================================================
    // Transition Overlay
    // =========================================================================

    private void CreateTransitionOverlay()
    {
        // Create a canvas that renders on top of everything for fade transitions
        GameObject canvasObj = new GameObject("TransitionCanvas");
        canvasObj.transform.SetParent(transform);

        transitionCanvas = canvasObj.AddComponent<Canvas>();
        transitionCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        transitionCanvas.sortingOrder = 999; // Above everything

        canvasObj.AddComponent<CanvasScaler>();

        // Fade overlay image
        GameObject overlayObj = new GameObject("FadeOverlay");
        overlayObj.transform.SetParent(canvasObj.transform, false);

        fadeOverlay = overlayObj.AddComponent<Image>();
        fadeOverlay.color = fadeColor;
        fadeOverlay.raycastTarget = false;

        RectTransform overlayRect = overlayObj.GetComponent<RectTransform>();
        overlayRect.anchorMin = Vector2.zero;
        overlayRect.anchorMax = Vector2.one;
        overlayRect.offsetMin = Vector2.zero;
        overlayRect.offsetMax = Vector2.zero;

        // CanvasGroup for alpha control
        fadeCanvasGroup = overlayObj.AddComponent<CanvasGroup>();
        fadeCanvasGroup.alpha = 0f;
        fadeCanvasGroup.blocksRaycasts = false;
        fadeCanvasGroup.interactable = false;
    }

    private IEnumerator Fade(float from, float to, float duration)
    {
        if (fadeCanvasGroup == null) yield break;

        fadeCanvasGroup.blocksRaycasts = (to > 0f);
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime; // Use unscaled so it works even when paused
            float t = Mathf.Clamp01(elapsed / duration);
            fadeCanvasGroup.alpha = Mathf.Lerp(from, to, t);
            yield return null;
        }

        fadeCanvasGroup.alpha = to;
        fadeCanvasGroup.blocksRaycasts = (to > 0.01f);
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }
}
