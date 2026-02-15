using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEditor;
using UnityEditor.SceneManagement;

/// <summary>
/// Editor utility to set up the game scene hierarchies and create prefabs.
/// Run from the Unity menu: Unfunctional > Setup All Scenes
/// </summary>
public class SceneSetupEditor : EditorWindow
{
    [MenuItem("Unfunctional/Setup All Scenes")]
    public static void SetupAllScenes()
    {
        if (!EditorUtility.DisplayDialog(
            "Setup All Scenes",
            "This will:\n" +
            "1. Create the Player prefab in Assets/Prefabs/\n" +
            "2. Set up the GLOBAL scene with GameManager, InputManager, PauseMenu Canvas, EventSystem\n" +
            "3. Set up LEVEL1 scene with confusing menu UI\n\n" +
            "Existing objects in those scenes will be preserved. Continue?",
            "Yes, set it up", "Cancel"))
        {
            return;
        }

        CreatePlayerPrefab();
        SetupGlobalScene();
        SetupLevel1Scene();

        Debug.Log("[SceneSetup] All scenes set up successfully!");
        EditorUtility.DisplayDialog("Done", "All scenes and prefabs have been set up.", "OK");
    }

    [MenuItem("Unfunctional/1. Create Player Prefab")]
    public static void CreatePlayerPrefab()
    {
        // Ensure Prefabs directory exists
        if (!AssetDatabase.IsValidFolder("Assets/Prefabs"))
        {
            AssetDatabase.CreateFolder("Assets", "Prefabs");
        }

        // Create a capsule as the player body
        GameObject player = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        player.name = "Player";
        player.tag = "Player";
        player.layer = 0;

        // Position at origin
        player.transform.position = Vector3.zero;

        // Remove the default CapsuleCollider (CharacterController replaces it)
        Object.DestroyImmediate(player.GetComponent<CapsuleCollider>());

        // Add CharacterController
        CharacterController cc = player.AddComponent<CharacterController>();
        cc.height = 2f;
        cc.radius = 0.5f;
        cc.center = new Vector3(0, 0, 0);

        // Add PlayerController script
        player.AddComponent<PlayerController>();

        // Create camera as child at head height
        GameObject camObj = new GameObject("PlayerCamera");
        camObj.transform.SetParent(player.transform);
        camObj.transform.localPosition = new Vector3(0, 0.8f, 0); // Roughly eye level on a 2-unit capsule
        camObj.transform.localRotation = Quaternion.identity;

        Camera cam = camObj.AddComponent<Camera>();
        cam.fieldOfView = 70;
        cam.nearClipPlane = 0.1f;
        cam.farClipPlane = 1000f;
        cam.tag = "MainCamera";
        camObj.tag = "MainCamera";

        AudioListener listener = camObj.AddComponent<AudioListener>();

        // Wire the camera reference on PlayerController
        PlayerController pc = player.GetComponent<PlayerController>();
        pc.cameraTransform = camObj.transform;

        // Save as prefab
        string prefabPath = "Assets/Prefabs/Player.prefab";
        PrefabUtility.SaveAsPrefabAsset(player, prefabPath);
        Object.DestroyImmediate(player);

        AssetDatabase.SaveAssets();
        Debug.Log($"[SceneSetup] Player prefab created at {prefabPath}");
    }

    [MenuItem("Unfunctional/2. Setup GLOBAL Scene")]
    public static void SetupGlobalScene()
    {
        // Open the GLOBAL scene
        var scene = EditorSceneManager.OpenScene("Assets/Scenes/GLOBAL.unity", OpenSceneMode.Single);

        // Remove the default Main Camera if present (player prefab has its own)
        GameObject existingCam = GameObject.FindGameObjectWithTag("MainCamera");
        if (existingCam != null && existingCam.name == "Main Camera")
        {
            Object.DestroyImmediate(existingCam);
        }

        // --- GameManager ---
        GameObject gmObj = GameObject.Find("GameManager");
        if (gmObj == null)
        {
            gmObj = new GameObject("GameManager");
        }
        if (gmObj.GetComponent<GameManager>() == null)
        {
            gmObj.AddComponent<GameManager>();
        }

        // --- InputManager ---
        GameObject imObj = GameObject.Find("InputManager");
        if (imObj == null)
        {
            imObj = new GameObject("InputManager");
        }
        if (imObj.GetComponent<InputManager>() == null)
        {
            imObj.AddComponent<InputManager>();
        }

        // --- EventSystem ---
        if (Object.FindAnyObjectByType<EventSystem>() == null)
        {
            GameObject esObj = new GameObject("EventSystem");
            esObj.AddComponent<EventSystem>();
            esObj.AddComponent<StandaloneInputModule>();
        }

        // --- Pause Menu Canvas ---
        GameObject pauseCanvasObj = GameObject.Find("PauseCanvas");
        if (pauseCanvasObj == null)
        {
            pauseCanvasObj = CreatePauseMenuCanvas();
        }

        // Mark scene dirty and save
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("[SceneSetup] GLOBAL scene set up with GameManager, InputManager, EventSystem, PauseCanvas");
    }

    [MenuItem("Unfunctional/3. Setup LEVEL1 Scene")]
    public static void SetupLevel1Scene()
    {
        // Open LEVEL1 scene
        var scene = EditorSceneManager.OpenScene("Assets/Scenes/LEVEL1.unity", OpenSceneMode.Single);

        // Remove default Camera/Light if not needed (the GLOBAL scene provides persistent objects)
        // Keep the Directional Light for level-specific lighting

        // --- Level Manager Root ---
        GameObject levelRoot = GameObject.Find("Level1Manager");
        if (levelRoot == null)
        {
            levelRoot = new GameObject("Level1Manager");
        }
        Level1_ConfusingMenu menuScript = levelRoot.GetComponent<Level1_ConfusingMenu>();
        if (menuScript == null)
        {
            menuScript = levelRoot.AddComponent<Level1_ConfusingMenu>();
        }

        // --- Menu Canvas ---
        GameObject canvasObj = GameObject.Find("MenuCanvas");
        Canvas canvas;
        if (canvasObj == null)
        {
            canvasObj = new GameObject("MenuCanvas");
            canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10;
            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            canvasObj.AddComponent<GraphicRaycaster>();
        }
        else
        {
            canvas = canvasObj.GetComponent<Canvas>();
        }

        // Wire canvas reference
        menuScript.menuCanvas = canvas;

        // --- Main Panel (slightly rotated for confusion) ---
        GameObject mainPanel = FindOrCreateChild(canvasObj, "MainPanel");
        RectTransform mainPanelRect = EnsureRectTransform(mainPanel);
        mainPanelRect.anchorMin = Vector2.zero;
        mainPanelRect.anchorMax = Vector2.one;
        mainPanelRect.offsetMin = Vector2.zero;
        mainPanelRect.offsetMax = Vector2.zero;

        // Add a subtle background
        Image mainPanelBg = mainPanel.GetComponent<Image>();
        if (mainPanelBg == null)
        {
            mainPanelBg = mainPanel.AddComponent<Image>();
        }
        mainPanelBg.color = new Color(0.08f, 0.08f, 0.12f, 1f);

        menuScript.mainPanel = mainPanelRect;

        // --- Title Text ---
        GameObject titleObj = FindOrCreateChild(mainPanel, "TitleText");
        Text titleText = titleObj.GetComponent<Text>();
        if (titleText == null)
        {
            titleText = titleObj.AddComponent<Text>();
        }
        titleText.text = "UNFUNCTIONAL";
        titleText.fontSize = 72;
        titleText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        titleText.alignment = TextAnchor.MiddleCenter;
        titleText.color = new Color(0.9f, 0.9f, 0.9f, 1f);

        RectTransform titleRect = titleObj.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0.1f, 0.75f);
        titleRect.anchorMax = new Vector2(0.9f, 0.95f);
        titleRect.offsetMin = Vector2.zero;
        titleRect.offsetMax = Vector2.zero;

        // --- Subtitle ---
        GameObject subtitleObj = FindOrCreateChild(mainPanel, "SubtitleText");
        Text subtitleText = subtitleObj.GetComponent<Text>();
        if (subtitleText == null)
        {
            subtitleText = subtitleObj.AddComponent<Text>();
        }
        subtitleText.text = "The Worst Game Ever Made (on purpose)";
        subtitleText.fontSize = 24;
        subtitleText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        subtitleText.alignment = TextAnchor.MiddleCenter;
        subtitleText.color = new Color(0.6f, 0.6f, 0.6f, 1f);

        RectTransform subtitleRect = subtitleObj.GetComponent<RectTransform>();
        subtitleRect.anchorMin = new Vector2(0.15f, 0.68f);
        subtitleRect.anchorMax = new Vector2(0.85f, 0.76f);
        subtitleRect.offsetMin = Vector2.zero;
        subtitleRect.offsetMax = Vector2.zero;

        // --- Create Fake Start Buttons ---
        string[] fakeLabels = new string[]
        {
            "Start Game",
            "Begin",
            "Play",
            "Start",
            "New Game",
            "Launch"
        };

        Vector2[] fakePositions = new Vector2[]
        {
            new Vector2(0.5f, 0.55f),
            new Vector2(0.3f, 0.45f),
            new Vector2(0.7f, 0.45f),
            new Vector2(0.2f, 0.35f),
            new Vector2(0.8f, 0.35f),
            new Vector2(0.5f, 0.25f)
        };

        Button[] fakeButtons = new Button[fakeLabels.Length];
        for (int i = 0; i < fakeLabels.Length; i++)
        {
            string btnName = $"FakeStartButton_{i}";
            GameObject btnObj = FindOrCreateChild(mainPanel, btnName);
            fakeButtons[i] = SetupButton(btnObj, fakeLabels[i],
                fakePositions[i], new Vector2(180, 45),
                new Color(0.2f, 0.5f, 0.2f, 1f), 22);
        }
        menuScript.fakeStartButtons = fakeButtons;

        // --- Create Decoy Buttons (useless) ---
        string[] decoyLabels = new string[]
        {
            "Options",
            "Credits",
            "Help",
            "Extra",
            "More Options",
            "Quit?",
            "Settings",
            "About"
        };

        Vector2[] decoyPositions = new Vector2[]
        {
            new Vector2(0.15f, 0.55f),
            new Vector2(0.85f, 0.55f),
            new Vector2(0.1f, 0.15f),
            new Vector2(0.9f, 0.15f),
            new Vector2(0.4f, 0.12f),
            new Vector2(0.6f, 0.12f),
            new Vector2(0.15f, 0.25f),
            new Vector2(0.85f, 0.25f)
        };

        Button[] decoyButtons = new Button[decoyLabels.Length];
        for (int i = 0; i < decoyLabels.Length; i++)
        {
            string btnName = $"DecoyButton_{i}";
            GameObject btnObj = FindOrCreateChild(mainPanel, btnName);
            decoyButtons[i] = SetupButton(btnObj, decoyLabels[i],
                decoyPositions[i], new Vector2(130, 35),
                new Color(0.3f, 0.3f, 0.3f, 1f), 16);
        }
        menuScript.decoyButtons = decoyButtons;

        // --- The REAL Start Button (tiny, mislabeled, hidden) ---
        GameObject realBtnObj = FindOrCreateChild(mainPanel, "RealStartButton");
        Button realBtn = SetupButton(realBtnObj, "Terms of Service",
            new Vector2(0.92f, 0.03f), new Vector2(100, 20),
            new Color(0.15f, 0.15f, 0.15f, 0.6f), 9);
        menuScript.realStartButton = realBtn;

        // --- Sub-Menu Panels (popup traps) ---
        string[] subMenuTitles = new string[]
        {
            "COOKIE POLICY\n\nWe use cookies.\nActually, we use the whole bakery.\n\nAccept?",
            "SYSTEM CHECK\n\nChecking system...\nYour system is... a computer.\nProbably.\n\nOK",
            "TERMS OF SERVICE\n\nBy reading this you agree\nto not read this.\n\nAgree / Also Agree",
            "UPDATE AVAILABLE\n\nVersion 0.0.0.0.1 is ready.\nChangelog: Fixed nothing.\n\nInstall Later"
        };

        GameObject[] subMenuPanels = new GameObject[subMenuTitles.Length];
        for (int i = 0; i < subMenuTitles.Length; i++)
        {
            string panelName = $"SubMenuPanel_{i}";
            GameObject panel = FindOrCreateChild(mainPanel, panelName);

            RectTransform panelRect = EnsureRectTransform(panel);
            panelRect.anchorMin = new Vector2(0.25f, 0.2f);
            panelRect.anchorMax = new Vector2(0.75f, 0.8f);
            panelRect.offsetMin = Vector2.zero;
            panelRect.offsetMax = Vector2.zero;

            Image panelBg = panel.GetComponent<Image>();
            if (panelBg == null) panelBg = panel.AddComponent<Image>();
            panelBg.color = new Color(0.12f, 0.12f, 0.18f, 0.95f);

            // Panel text
            GameObject textObj = FindOrCreateChild(panel, "PanelText");
            Text panelText = textObj.GetComponent<Text>();
            if (panelText == null) panelText = textObj.AddComponent<Text>();
            panelText.text = subMenuTitles[i];
            panelText.fontSize = 22;
            panelText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            panelText.alignment = TextAnchor.MiddleCenter;
            panelText.color = Color.white;

            RectTransform textRect = textObj.GetComponent<RectTransform>();
            textRect.anchorMin = new Vector2(0.05f, 0.05f);
            textRect.anchorMax = new Vector2(0.95f, 0.95f);
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            // Close button
            GameObject closeBtnObj = FindOrCreateChild(panel, "CloseBtn");
            Button closeBtn = SetupButton(closeBtnObj, "X",
                new Vector2(0.95f, 0.95f), new Vector2(30, 30),
                new Color(0.6f, 0.1f, 0.1f, 1f), 14);

            panel.SetActive(false);
            subMenuPanels[i] = panel;
        }
        menuScript.subMenuPanels = subMenuPanels;

        // --- EventSystem for this scene (in case loaded standalone for testing) ---
        if (Object.FindAnyObjectByType<EventSystem>() == null)
        {
            GameObject esObj = new GameObject("EventSystem");
            esObj.AddComponent<EventSystem>();
            esObj.AddComponent<StandaloneInputModule>();
        }

        // Save
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("[SceneSetup] LEVEL1 scene set up with confusing menu UI");
    }

    // =========================================================================
    // Helper Methods
    // =========================================================================

    private static GameObject CreatePauseMenuCanvas()
    {
        // Canvas
        GameObject canvasObj = new GameObject("PauseCanvas");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100; // Always on top

        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);

        canvasObj.AddComponent<GraphicRaycaster>();

        // GamePauseMenu component
        GamePauseMenu pauseMenu = canvasObj.AddComponent<GamePauseMenu>();

        // --- Pause Panel (the actual overlay) ---
        GameObject panelObj = new GameObject("PausePanel");
        panelObj.transform.SetParent(canvasObj.transform, false);

        RectTransform panelRect = panelObj.AddComponent<RectTransform>();
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;

        Image panelBg = panelObj.AddComponent<Image>();
        panelBg.color = new Color(0, 0, 0, 0.75f);

        pauseMenu.pausePanel = panelObj;

        // --- Pause Title ---
        GameObject titleObj = new GameObject("PauseTitle");
        titleObj.transform.SetParent(panelObj.transform, false);

        RectTransform titleRect = titleObj.AddComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0.3f, 0.65f);
        titleRect.anchorMax = new Vector2(0.7f, 0.8f);
        titleRect.offsetMin = Vector2.zero;
        titleRect.offsetMax = Vector2.zero;

        Text titleText = titleObj.AddComponent<Text>();
        titleText.text = "PAUSED";
        titleText.fontSize = 48;
        titleText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        titleText.alignment = TextAnchor.MiddleCenter;
        titleText.color = Color.white;

        // --- Resume Button ---
        GameObject resumeBtnObj = CreateUIButton(panelObj, "ResumeButton", "Resume",
            new Vector2(0.35f, 0.45f), new Vector2(0.65f, 0.55f),
            new Color(0.2f, 0.5f, 0.2f, 1f));
        pauseMenu.resumeButton = resumeBtnObj.GetComponent<Button>();

        // --- Restart Button ---
        GameObject restartBtnObj = CreateUIButton(panelObj, "RestartButton", "Restart Level",
            new Vector2(0.35f, 0.32f), new Vector2(0.65f, 0.42f),
            new Color(0.5f, 0.5f, 0.2f, 1f));
        pauseMenu.restartButton = restartBtnObj.GetComponent<Button>();

        // --- Quit Button ---
        GameObject quitBtnObj = CreateUIButton(panelObj, "QuitButton", "Quit",
            new Vector2(0.35f, 0.19f), new Vector2(0.65f, 0.29f),
            new Color(0.5f, 0.2f, 0.2f, 1f));
        pauseMenu.quitButton = quitBtnObj.GetComponent<Button>();

        // Start hidden
        panelObj.SetActive(false);

        return canvasObj;
    }

    private static GameObject CreateUIButton(GameObject parent, string name, string label,
        Vector2 anchorMin, Vector2 anchorMax, Color bgColor)
    {
        GameObject btnObj = new GameObject(name);
        btnObj.transform.SetParent(parent.transform, false);

        RectTransform btnRect = btnObj.AddComponent<RectTransform>();
        btnRect.anchorMin = anchorMin;
        btnRect.anchorMax = anchorMax;
        btnRect.offsetMin = Vector2.zero;
        btnRect.offsetMax = Vector2.zero;

        Image btnBg = btnObj.AddComponent<Image>();
        btnBg.color = bgColor;

        Button btn = btnObj.AddComponent<Button>();
        ColorBlock colors = btn.colors;
        colors.highlightedColor = new Color(bgColor.r + 0.15f, bgColor.g + 0.15f, bgColor.b + 0.15f, 1f);
        colors.pressedColor = new Color(bgColor.r - 0.1f, bgColor.g - 0.1f, bgColor.b - 0.1f, 1f);
        btn.colors = colors;

        // Text child
        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(btnObj.transform, false);

        RectTransform textRect = textObj.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        Text text = textObj.AddComponent<Text>();
        text.text = label;
        text.fontSize = 28;
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.alignment = TextAnchor.MiddleCenter;
        text.color = Color.white;

        return btnObj;
    }

    private static Button SetupButton(GameObject btnObj, string label, Vector2 anchorCenter,
        Vector2 size, Color bgColor, int fontSize)
    {
        RectTransform btnRect = EnsureRectTransform(btnObj);

        // Convert center anchor + size to pixel offsets
        btnRect.anchorMin = anchorCenter;
        btnRect.anchorMax = anchorCenter;
        btnRect.sizeDelta = size;
        btnRect.anchoredPosition = Vector2.zero;

        Image btnBg = btnObj.GetComponent<Image>();
        if (btnBg == null) btnBg = btnObj.AddComponent<Image>();
        btnBg.color = bgColor;

        Button btn = btnObj.GetComponent<Button>();
        if (btn == null) btn = btnObj.AddComponent<Button>();

        ColorBlock colors = btn.colors;
        colors.highlightedColor = new Color(
            Mathf.Min(bgColor.r + 0.2f, 1f),
            Mathf.Min(bgColor.g + 0.2f, 1f),
            Mathf.Min(bgColor.b + 0.2f, 1f), 1f);
        colors.pressedColor = new Color(bgColor.r * 0.7f, bgColor.g * 0.7f, bgColor.b * 0.7f, 1f);
        btn.colors = colors;

        // Text child
        GameObject textObj = FindOrCreateChild(btnObj, "Text");
        Text text = textObj.GetComponent<Text>();
        if (text == null) text = textObj.AddComponent<Text>();

        RectTransform textRect = textObj.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        text.text = label;
        text.fontSize = fontSize;
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.alignment = TextAnchor.MiddleCenter;
        text.color = Color.white;

        return btn;
    }

    private static GameObject FindOrCreateChild(GameObject parent, string name)
    {
        Transform existing = parent.transform.Find(name);
        if (existing != null)
        {
            return existing.gameObject;
        }

        GameObject child = new GameObject(name);
        child.transform.SetParent(parent.transform, false);
        EnsureRectTransform(child);
        return child;
    }

    private static RectTransform EnsureRectTransform(GameObject obj)
    {
        RectTransform rect = obj.GetComponent<RectTransform>();
        if (rect == null)
        {
            rect = obj.AddComponent<RectTransform>();
        }
        return rect;
    }
}
