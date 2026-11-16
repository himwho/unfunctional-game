using UnityEngine;

/// <summary>
/// Base class for level-specific managers. Each level scene should have one of these
/// (or a derived class) on a root GameObject to handle level init/cleanup.
/// </summary>
public class LevelManager : MonoBehaviour
{
    [Header("Level Info")]
    public string levelDisplayName = "Unnamed Level";
    public string levelDescription = "";
    public int levelBuildIndex = -1;

    [Header("Cursor")]
    [Tooltip("True for 3D/FPS levels, false for UI-based levels")]
    public bool wantsCursorLocked = false;

    [Header("Player")]
    [Tooltip("True for 3D levels that need the Player prefab spawned")]
    public bool needsPlayer = false;
    [Tooltip("Where to spawn the player. If null, spawns at origin.")]
    public Transform playerSpawnPoint;

    [Header("Level Complete")]
    [SerializeField] protected bool levelComplete = false;

    public bool IsLevelComplete => levelComplete;

    // Time.timeScale does not stop Update or Input.GetKeyDown. Gate level actions too.
    protected bool CanProcessGameplay => !levelComplete &&
        (GameManager.Instance == null || (!GameManager.Instance.IsLoading &&
         (GameManager.Instance.CurrentState == GameManager.GameState.Playing ||
          GameManager.Instance.CurrentState == GameManager.GameState.MainMenu)));

    protected virtual void Start()
    {
        Debug.Log($"[LevelManager] Initialized: {levelDisplayName}");
        ApplyCursorState();
    }

    /// <summary>
    /// Applies this level's cursor preference. Called on Start and when
    /// resuming from pause so the correct cursor state is restored.
    /// </summary>
    public virtual void ApplyCursorState()
    {
        bool shouldLock = wantsCursorLocked;
        foreach (var keypad in FindObjectsByType<KeypadController>(FindObjectsSortMode.None))
            if (keypad.IsOpen) shouldLock = false;
        if (InputManager.Instance != null)
        {
            if (shouldLock)
                InputManager.Instance.LockCursor();
            else
                InputManager.Instance.UnlockCursor();
        }
        else
        {
            Cursor.lockState = shouldLock ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !shouldLock;
        }
    }

    /// <summary>Esc closes a modal before the shared input system opens pause.</summary>
    public virtual bool TryCloseModal()
    {
        foreach (var keypad in FindObjectsByType<KeypadController>(FindObjectsSortMode.None))
        {
            if (!keypad.IsOpen) continue;
            keypad.Close();
            return true;
        }
        return false;
    }

    /// <summary>
    /// Returns the world position where the player should spawn.
    /// </summary>
    public Vector3 GetSpawnPosition()
    {
        if (playerSpawnPoint != null)
            return playerSpawnPoint.position;
        return new Vector3(0f, 1f, 0f); // Default: slightly above origin
    }

    /// <summary>
    /// Returns the spawn rotation.
    /// </summary>
    public Quaternion GetSpawnRotation()
    {
        if (playerSpawnPoint != null)
            return playerSpawnPoint.rotation;
        return Quaternion.identity;
    }

    /// <summary>
    /// Call this when the player completes the level objective.
    /// Override in derived classes for custom completion behavior.
    /// </summary>
    public virtual void CompleteLevel()
    {
        if (levelComplete) return;

        levelComplete = true;
        Debug.Log($"[LevelManager] Level complete: {levelDisplayName}");

        // Notify the GameManager to move to the next level
        if (GameManager.Instance != null)
        {
            GameManager.Instance.LoadNextLevel();
        }
    }

    /// <summary>
    /// Override to handle any cleanup before the level is unloaded.
    /// </summary>
    protected virtual void OnDestroy()
    {
        Debug.Log($"[LevelManager] Cleaning up: {levelDisplayName}");
    }
}
