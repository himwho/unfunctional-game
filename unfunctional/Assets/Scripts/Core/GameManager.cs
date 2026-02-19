using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
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
        "LEVEL6",
        "LEVEL7",
        "LEVEL8",
        "LEVEL9",
        "LEVEL10",
        "LEVEL11"
    };

    [Header("Player")]
    [Tooltip("Player prefab to instantiate for 3D levels. Loaded from Resources/Player if null.")]
    public GameObject playerPrefab;

    [Header("Transition")]
    public float fadeOutDuration = 0.5f;
    public float fadeInDuration = 0.5f;
    public Color fadeColor = Color.black;

    [Header("State")]
    [SerializeField] private int currentLevelIndex = -1; // -1 means no level loaded yet
    [SerializeField] private GameState currentState = GameState.Boot;

    public int CurrentLevelIndex => currentLevelIndex;
    public GameState CurrentState => currentState;
    public GameObject CurrentPlayer => currentPlayerInstance;

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

    // Player instance management
    private GameObject currentPlayerInstance;

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
        LoadPlayerPrefab();
        EnsureDebugPanel();
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

        // Despawn current player if it exists
        DespawnPlayer();

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

        // Remove duplicate EventSystems that came in with the additive scene.
        // The GLOBAL scene already has one; extras cause input conflicts.
        CleanupDuplicateEventSystems();

        // Strip orphan CanvasRenderer components from non-Canvas TextMeshPro
        // objects to silence URP / TMP warnings.
        CleanupOrphanCanvasRenderers();

        // Find the level manager and handle player spawning
        // Wait one frame so all Start() methods have run
        yield return null;

        LevelManager lm = FindAnyObjectByType<LevelManager>();
        if (lm != null && lm.needsPlayer)
        {
            SpawnPlayer(lm.GetSpawnPosition(), lm.GetSpawnRotation());
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
    // Player Spawning
    // =========================================================================

    private void LoadPlayerPrefab()
    {
        if (playerPrefab != null) return;

        // Try to load from Resources folder
        playerPrefab = Resources.Load<GameObject>("Player");

        if (playerPrefab == null)
        {
            Debug.LogWarning("[GameManager] No player prefab assigned and none found in Resources/. " +
                "3D levels will have no player. Assign the prefab on the GameManager or place it in Assets/Resources/.");
        }
    }

    private void SpawnPlayer(Vector3 position, Quaternion rotation)
    {
        if (playerPrefab == null)
        {
            Debug.LogError("[GameManager] Cannot spawn player: no prefab assigned.");
            return;
        }

        if (currentPlayerInstance != null)
        {
            Debug.LogWarning("[GameManager] Player already exists, despawning old one first.");
            DespawnPlayer();
        }

        currentPlayerInstance = Instantiate(playerPrefab, position, rotation);
        currentPlayerInstance.name = "Player";

        // In URP, having two Base cameras active simultaneously (BackgroundCamera +
        // PlayerCamera) can cause Screen Space - Overlay canvases to become invisible.
        // The intermediate render texture blit from the second camera overwrites the
        // overlay compositing from the first camera. Disabling BackgroundCamera when
        // the PlayerCamera is active avoids this entirely.
        SetBackgroundCamera(false);

        Debug.Log($"[GameManager] Player spawned at {position}");
    }

    private void DespawnPlayer()
    {
        if (currentPlayerInstance != null)
        {
            Destroy(currentPlayerInstance);
            currentPlayerInstance = null;
            Debug.Log("[GameManager] Player despawned.");
        }

        // Re-enable BackgroundCamera for UI-only levels (no player camera)
        SetBackgroundCamera(true);
    }

    /// <summary>
    /// Enable or disable the BackgroundCamera in the GLOBAL scene.
    /// Must be disabled when a PlayerCamera is active to prevent URP
    /// multi-Base-camera issues with Screen Space - Overlay canvases.
    /// </summary>
    private void SetBackgroundCamera(bool enabled)
    {
        // The BackgroundCamera lives as a sibling in the GLOBAL scene root
        GameObject camObj = GameObject.Find("BackgroundCamera");
        if (camObj == null)
        {
            // Also check children of this transform (in case it was parented)
            Transform child = transform.parent != null
                ? transform.parent.Find("BackgroundCamera")
                : null;
            if (child != null) camObj = child.gameObject;
        }

        if (camObj != null)
        {
            Camera cam = camObj.GetComponent<Camera>();
            if (cam != null)
            {
                cam.enabled = enabled;
                Debug.Log($"[GameManager] BackgroundCamera enabled={enabled}");
            }
        }
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

        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);

        UIHelper.ConfigureCanvas(transitionCanvas, sortingOrder: 999);

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

    // =========================================================================
    // Debug Panel
    // =========================================================================

    private void EnsureDebugPanel()
    {
        if (GetComponent<DebugPanel>() == null)
        {
            gameObject.AddComponent<DebugPanel>();
        }
    }

    // =========================================================================
    // Scene Cleanup Helpers
    // =========================================================================

    /// <summary>
    /// After an additive scene load, destroy every EventSystem except the
    /// first one found (which lives in the persistent GLOBAL scene).
    /// This silences the "There can be only one active Event System" warning
    /// and prevents input conflicts between duplicate EventSystems.
    /// </summary>
    private void CleanupDuplicateEventSystems()
    {
        EventSystem[] all = FindObjectsByType<EventSystem>(FindObjectsSortMode.None);
        if (all.Length <= 1) return;

        // Keep the very first one (GLOBAL's), destroy the rest
        bool kept = false;
        foreach (var es in all)
        {
            if (!kept)
            {
                kept = true;
                continue;
            }
            Debug.Log($"[GameManager] Destroying duplicate EventSystem on '{es.gameObject.name}'.");
            Destroy(es.gameObject);
        }
    }

    /// <summary>
    /// Removes stray CanvasRenderer components left on GameObjects that have a
    /// TextMeshPro (world-space) component but no parent Canvas. URP + TMP
    /// in Unity 6 logs warnings about these.
    /// </summary>
    private void CleanupOrphanCanvasRenderers()
    {
        var tmps = FindObjectsByType<TMPro.TextMeshPro>(FindObjectsSortMode.None);
        foreach (var tmp in tmps)
        {
            var cr = tmp.GetComponent<CanvasRenderer>();
            if (cr != null && tmp.GetComponentInParent<Canvas>() == null)
            {
                Debug.Log($"[GameManager] Removing orphan CanvasRenderer from '{tmp.gameObject.name}'.");
                Destroy(cr);
            }
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }
}
