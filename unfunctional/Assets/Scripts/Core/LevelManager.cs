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

    [Header("Level Complete")]
    [SerializeField] protected bool levelComplete = false;

    public bool IsLevelComplete => levelComplete;

    protected virtual void Start()
    {
        Debug.Log($"[LevelManager] Initialized: {levelDisplayName}");
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
