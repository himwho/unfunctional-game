using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

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

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        // On boot, load LEVEL1 (the main menu) additively
        LoadLevel(0);
    }

    /// <summary>
    /// Load a level by index (0-based, maps to levelSceneNames array).
    /// Unloads the previous level scene first.
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
            // Could loop back or show credits
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
        else if (newState == GameState.Playing)
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

    private System.Collections.IEnumerator LoadLevelRoutine(int levelIndex)
    {
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
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }
}
