using UnityEngine;

/// <summary>
/// Developer debug panel for quickly skipping between levels.
/// Toggle with F1. Uses IMGUI so it requires no scene/canvas setup —
/// just attach to the GameManager GameObject in the GLOBAL scene.
/// 
/// Automatically stripped from release builds unless ENABLE_DEBUG_PANEL
/// is defined. Always available in the Editor and Development Builds.
/// </summary>
public class DebugPanel : MonoBehaviour
{
    [Header("Settings")]
    [Tooltip("Key to toggle the debug panel")]
    public KeyCode toggleKey = KeyCode.F1;

    [Tooltip("Panel will only function in Editor / Development Builds unless this is true")]
    public bool allowInReleaseBuild = false;

    private bool isVisible = false;
    private bool isAllowed = false;

    private Vector2 scrollPos;
    private Rect windowRect = new Rect(10, 10, 280, 0); // height auto-calculated

    private GUIStyle headerStyle;
    private GUIStyle levelButtonStyle;
    private GUIStyle currentLevelStyle;
    private bool stylesInitialized = false;

    private void Awake()
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        isAllowed = true;
#else
        isAllowed = allowInReleaseBuild;
#endif
    }

    private void Update()
    {
        if (!isAllowed) return;

        if (Input.GetKeyDown(toggleKey))
        {
            isVisible = !isVisible;
        }
    }

    private void InitStyles()
    {
        if (stylesInitialized) return;

        headerStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 16,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter
        };
        headerStyle.normal.textColor = Color.white;

        levelButtonStyle = new GUIStyle(GUI.skin.button)
        {
            fontSize = 13,
            fixedHeight = 30,
            alignment = TextAnchor.MiddleLeft,
            padding = new RectOffset(10, 10, 4, 4)
        };

        currentLevelStyle = new GUIStyle(GUI.skin.button)
        {
            fontSize = 13,
            fontStyle = FontStyle.Bold,
            fixedHeight = 30,
            alignment = TextAnchor.MiddleLeft,
            padding = new RectOffset(10, 10, 4, 4)
        };
        currentLevelStyle.normal.textColor = Color.green;
        currentLevelStyle.hover.textColor = Color.green;

        stylesInitialized = true;
    }

    private void OnGUI()
    {
        if (!isAllowed || !isVisible) return;

        InitStyles();

        GameManager gm = GameManager.Instance;
        if (gm == null)
        {
            GUI.Label(new Rect(10, 10, 300, 30), "Debug Panel: GameManager not found");
            return;
        }

        // Calculate window height based on content
        int levelCount = gm.levelSceneNames != null ? gm.levelSceneNames.Length : 0;
        float windowHeight = 110 + (levelCount * 35) + 80; // header + levels + extras
        windowRect.height = windowHeight;

        // Clamp to screen
        windowRect.x = Mathf.Clamp(windowRect.x, 0, Screen.width - windowRect.width);
        windowRect.y = Mathf.Clamp(windowRect.y, 0, Screen.height - windowRect.height);

        windowRect = GUI.Window(9999, windowRect, DrawWindow, "");
    }

    private void DrawWindow(int windowID)
    {
        GameManager gm = GameManager.Instance;

        // Header
        GUILayout.Space(4);
        GUILayout.Label("DEBUG PANEL", headerStyle);
        GUILayout.Space(2);

        // Current state info
        string stateName = gm.CurrentState.ToString();
        string currentLevel = gm.CurrentLevelIndex >= 0 && gm.CurrentLevelIndex < gm.levelSceneNames.Length
            ? gm.levelSceneNames[gm.CurrentLevelIndex]
            : "None";
        GUILayout.Label($"State: {stateName}  |  Level: {currentLevel} ({gm.CurrentLevelIndex})");

        GUILayout.Space(6);

        // Divider
        GUILayout.Box("", GUILayout.ExpandWidth(true), GUILayout.Height(1));

        GUILayout.Label("Jump to Level:");

        // Level buttons
        if (gm.levelSceneNames != null)
        {
            for (int i = 0; i < gm.levelSceneNames.Length; i++)
            {
                bool isCurrent = (i == gm.CurrentLevelIndex);
                string label = isCurrent
                    ? $"► {i}: {gm.levelSceneNames[i]}  (current)"
                    : $"   {i}: {gm.levelSceneNames[i]}";

                GUIStyle style = isCurrent ? currentLevelStyle : levelButtonStyle;

                if (GUILayout.Button(label, style))
                {
                    if (gm.CurrentState == GameManager.GameState.Paused)
                    {
                        Time.timeScale = 1f;
                    }
                    gm.LoadLevel(i);
                }
            }
        }

        GUILayout.Space(6);
        GUILayout.Box("", GUILayout.ExpandWidth(true), GUILayout.Height(1));

        // Utility buttons
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Reload Current", GUILayout.Height(28)))
        {
            if (gm.CurrentState == GameManager.GameState.Paused)
            {
                Time.timeScale = 1f;
            }
            gm.ReloadCurrentLevel();
        }
        if (GUILayout.Button("Next Level", GUILayout.Height(28)))
        {
            if (gm.CurrentState == GameManager.GameState.Paused)
            {
                Time.timeScale = 1f;
            }
            gm.LoadNextLevel();
        }
        GUILayout.EndHorizontal();

        // Complete current level button
        if (GUILayout.Button("Force Complete Level", GUILayout.Height(28)))
        {
            LevelManager lm = FindAnyObjectByType<LevelManager>();
            if (lm != null)
            {
                lm.CompleteLevel();
            }
        }

        GUILayout.Space(4);
        GUILayout.Label($"<size=10>Press {toggleKey} to close</size>", new GUIStyle(GUI.skin.label) { richText = true, alignment = TextAnchor.MiddleCenter });

        // Make window draggable
        GUI.DragWindow();
    }
}
