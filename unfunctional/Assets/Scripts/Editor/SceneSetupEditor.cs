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
            "3. Set up LEVEL1 scene with confusing menu UI\n" +
            "4. Set up LEVEL2 scene with settings puzzle UI\n" +
            "5. Set up LEVEL3 scene with wall-clip room\n" +
            "6. Set up LEVEL4 scene with keypad puzzle room\n\n" +
            "Existing objects in those scenes will be preserved. Continue?",
            "Yes, set it up", "Cancel"))
        {
            return;
        }

        CreatePlayerPrefab();
        SetupGlobalScene();
        SetupLevel1Scene();
        SetupLevel2Scene();
        SetupLevel3Scene();
        SetupLevel4Scene();

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
        cam.depth = 0; // Higher than BackgroundCamera (-100) so it renders on top
        cam.tag = "MainCamera";
        camObj.tag = "MainCamera";

        // No AudioListener here -- the GLOBAL scene's BackgroundCamera
        // already has one and Unity only allows one active AudioListener.

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

        // Keep (or create) a persistent background camera.
        // This camera always renders a solid dark background so the Game view
        // never shows "No cameras rendering". Level-specific cameras (e.g. Player
        // prefab) can render on top with higher depth when 3D levels are loaded.
        GameObject camObj = GameObject.Find("BackgroundCamera");
        if (camObj == null)
        {
            // Reuse the default Main Camera if it exists, just rename it
            GameObject existingCam = GameObject.FindGameObjectWithTag("MainCamera");
            if (existingCam != null && existingCam.name == "Main Camera")
            {
                camObj = existingCam;
                camObj.name = "BackgroundCamera";
            }
            else
            {
                camObj = new GameObject("BackgroundCamera");
            }
        }
        Camera bgCam = camObj.GetComponent<Camera>();
        if (bgCam == null) bgCam = camObj.AddComponent<Camera>();
        bgCam.clearFlags = CameraClearFlags.SolidColor;
        bgCam.backgroundColor = new Color(0.04f, 0.04f, 0.06f, 1f);
        bgCam.depth = -100; // Lowest depth so any other camera renders on top
        bgCam.cullingMask = 0; // Don't render anything, just clear to background color
        camObj.tag = "Untagged"; // Don't tag as MainCamera; player camera will be MainCamera
        // Ensure it has an AudioListener (only one per scene tree)
        if (camObj.GetComponent<AudioListener>() == null)
            camObj.AddComponent<AudioListener>();

        // --- GameManager ---
        GameObject gmObj = GameObject.Find("GameManager");
        if (gmObj == null)
        {
            gmObj = new GameObject("GameManager");
        }
        GameManager gm = gmObj.GetComponent<GameManager>();
        if (gm == null)
        {
            gm = gmObj.AddComponent<GameManager>();
        }

        // Wire up the Player prefab reference so GameManager can spawn it for 3D levels
        GameObject playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Player.prefab");
        if (playerPrefab != null)
        {
            gm.playerPrefab = playerPrefab;
            Debug.Log("[SceneSetup] Player prefab wired on GameManager.");
        }
        else
        {
            Debug.LogWarning("[SceneSetup] Player prefab not found at Assets/Prefabs/Player.prefab. Run 'Create Player Prefab' first.");
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

    [MenuItem("Unfunctional/4. Setup LEVEL2 Scene")]
    public static void SetupLevel2Scene()
    {
        // Open LEVEL2 scene
        var scene = EditorSceneManager.OpenScene("Assets/Scenes/LEVEL2.unity", OpenSceneMode.Single);

        // --- Level Manager Root ---
        // Level2_SettingsPuzzle builds all its UI at runtime (10 step panels,
        // sliders, overlays, mic input, particles, etc.), so the editor setup
        // just creates the root object, a canvas, and an EventSystem.
        GameObject levelRoot = GameObject.Find("Level2Manager");
        if (levelRoot == null)
        {
            levelRoot = new GameObject("Level2Manager");
        }
        Level2_SettingsPuzzle puzzleScript = levelRoot.GetComponent<Level2_SettingsPuzzle>();
        if (puzzleScript == null)
        {
            puzzleScript = levelRoot.AddComponent<Level2_SettingsPuzzle>();
        }

        // --- Settings Canvas (optional; script creates one if missing) ---
        GameObject canvasObj = GameObject.Find("SettingsCanvas");
        if (canvasObj == null)
        {
            canvasObj = new GameObject("SettingsCanvas");
            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10;
            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            canvasObj.AddComponent<GraphicRaycaster>();
        }
        Canvas settingsCanvas = canvasObj.GetComponent<Canvas>();
        puzzleScript.settingsCanvas = settingsCanvas;

        // --- EventSystem for standalone testing ---
        if (Object.FindAnyObjectByType<EventSystem>() == null)
        {
            GameObject esObj = new GameObject("EventSystem");
            esObj.AddComponent<EventSystem>();
            esObj.AddComponent<StandaloneInputModule>();
        }

        // Save
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("[SceneSetup] LEVEL2 scene set up (UI built at runtime by Level2_SettingsPuzzle)");
    }

    [MenuItem("Unfunctional/5. Setup LEVEL3 Scene")]
    public static void SetupLevel3Scene()
    {
        // Open LEVEL3 scene
        var scene = EditorSceneManager.OpenScene("Assets/Scenes/LEVEL3.unity", OpenSceneMode.Single);

        // Clean up default camera if present (player prefab provides the camera)
        GameObject defaultCam = GameObject.FindGameObjectWithTag("MainCamera");
        if (defaultCam != null && defaultCam.name == "Main Camera")
        {
            Object.DestroyImmediate(defaultCam);
        }

        // =====================================================================
        // Lighting
        // =====================================================================

        // Ensure a directional light exists for ambient illumination
        Light dirLight = Object.FindAnyObjectByType<Light>();
        if (dirLight == null)
        {
            GameObject lightObj = new GameObject("Directional Light");
            dirLight = lightObj.AddComponent<Light>();
            dirLight.type = LightType.Directional;
            lightObj.transform.eulerAngles = new Vector3(50f, -30f, 0f);
        }
        dirLight.color = new Color(0.85f, 0.85f, 0.9f, 1f);
        dirLight.intensity = 0.6f;

        // =====================================================================
        // Room Geometry
        // =====================================================================
        // Room is 10x4x10 units. Player starts inside.
        // North wall has a door that won't open. East wall has a clippable section.
        // Exit trigger is outside the east wall.

        float roomW = 10f;  // X
        float roomH = 4f;   // Y
        float roomD = 10f;  // Z
        float wallThickness = 0.2f;

        // --- Floor ---
        GameObject floor = CreateOrFindPrimitive("Floor", PrimitiveType.Cube);
        floor.transform.position = new Vector3(0f, -wallThickness / 2f, 0f);
        floor.transform.localScale = new Vector3(roomW, wallThickness, roomD);
        SetColor(floor, new Color(0.35f, 0.35f, 0.35f));

        // --- Ceiling ---
        GameObject ceiling = CreateOrFindPrimitive("Ceiling", PrimitiveType.Cube);
        ceiling.transform.position = new Vector3(0f, roomH + wallThickness / 2f, 0f);
        ceiling.transform.localScale = new Vector3(roomW, wallThickness, roomD);
        SetColor(ceiling, new Color(0.5f, 0.5f, 0.5f));

        // --- South Wall (behind player start) ---
        GameObject wallSouth = CreateOrFindPrimitive("WallSouth", PrimitiveType.Cube);
        wallSouth.transform.position = new Vector3(0f, roomH / 2f, -roomD / 2f);
        wallSouth.transform.localScale = new Vector3(roomW, roomH, wallThickness);
        SetColor(wallSouth, new Color(0.55f, 0.55f, 0.5f));

        // --- North Wall (has the locked door) ---
        // Left part
        GameObject wallNorthLeft = CreateOrFindPrimitive("WallNorthLeft", PrimitiveType.Cube);
        float doorWidth = 1.5f;
        float northLeftW = (roomW - doorWidth) / 2f;
        wallNorthLeft.transform.position = new Vector3(-(doorWidth / 2f + northLeftW / 2f), roomH / 2f, roomD / 2f);
        wallNorthLeft.transform.localScale = new Vector3(northLeftW, roomH, wallThickness);
        SetColor(wallNorthLeft, new Color(0.55f, 0.55f, 0.5f));

        // Right part
        GameObject wallNorthRight = CreateOrFindPrimitive("WallNorthRight", PrimitiveType.Cube);
        wallNorthRight.transform.position = new Vector3(doorWidth / 2f + northLeftW / 2f, roomH / 2f, roomD / 2f);
        wallNorthRight.transform.localScale = new Vector3(northLeftW, roomH, wallThickness);
        SetColor(wallNorthRight, new Color(0.55f, 0.55f, 0.5f));

        // Door frame top
        GameObject doorFrameTop = CreateOrFindPrimitive("DoorFrameTop", PrimitiveType.Cube);
        float doorHeight = 2.5f;
        doorFrameTop.transform.position = new Vector3(0f, doorHeight + (roomH - doorHeight) / 2f, roomD / 2f);
        doorFrameTop.transform.localScale = new Vector3(doorWidth, roomH - doorHeight, wallThickness);
        SetColor(doorFrameTop, new Color(0.55f, 0.55f, 0.5f));

        // The door itself (visual only, solid collider, won't open)
        GameObject door = CreateOrFindPrimitive("Door", PrimitiveType.Cube);
        door.transform.position = new Vector3(0f, doorHeight / 2f, roomD / 2f);
        door.transform.localScale = new Vector3(doorWidth - 0.1f, doorHeight, wallThickness * 0.5f);
        SetColor(door, new Color(0.4f, 0.25f, 0.15f)); // brown wood color

        // --- West Wall (solid) ---
        GameObject wallWest = CreateOrFindPrimitive("WallWest", PrimitiveType.Cube);
        wallWest.transform.position = new Vector3(-roomW / 2f, roomH / 2f, 0f);
        wallWest.transform.localScale = new Vector3(wallThickness, roomH, roomD);
        SetColor(wallWest, new Color(0.55f, 0.55f, 0.5f));

        // --- East Wall (has clippable section) ---
        // The east wall is split into solid sections + one clippable section
        float clipSectionWidth = 2f;
        float clipSectionHeight = 2.5f;
        float clipSectionZ = 0f; // center of east wall

        // East wall - bottom solid
        // (we tile around the clip hole)

        // Left of clip section
        float eastSolidLeftW = (roomD - clipSectionWidth) / 2f;
        GameObject wallEastLeft = CreateOrFindPrimitive("WallEastLeft", PrimitiveType.Cube);
        wallEastLeft.transform.position = new Vector3(roomW / 2f, roomH / 2f, -(clipSectionWidth / 2f + eastSolidLeftW / 2f));
        wallEastLeft.transform.localScale = new Vector3(wallThickness, roomH, eastSolidLeftW);
        SetColor(wallEastLeft, new Color(0.55f, 0.55f, 0.5f));

        // Right of clip section
        GameObject wallEastRight = CreateOrFindPrimitive("WallEastRight", PrimitiveType.Cube);
        wallEastRight.transform.position = new Vector3(roomW / 2f, roomH / 2f, clipSectionWidth / 2f + eastSolidLeftW / 2f);
        wallEastRight.transform.localScale = new Vector3(wallThickness, roomH, eastSolidLeftW);
        SetColor(wallEastRight, new Color(0.55f, 0.55f, 0.5f));

        // Above clip section
        GameObject wallEastTop = CreateOrFindPrimitive("WallEastTop", PrimitiveType.Cube);
        float aboveClipH = roomH - clipSectionHeight;
        wallEastTop.transform.position = new Vector3(roomW / 2f, clipSectionHeight + aboveClipH / 2f, clipSectionZ);
        wallEastTop.transform.localScale = new Vector3(wallThickness, aboveClipH, clipSectionWidth);
        SetColor(wallEastTop, new Color(0.55f, 0.55f, 0.5f));

        // --- Clippable Wall Section ---
        // Looks like a wall but the collider is set to trigger so player walks through
        GameObject clippableWall = CreateOrFindPrimitive("ClippableWall", PrimitiveType.Cube);
        clippableWall.transform.position = new Vector3(roomW / 2f, clipSectionHeight / 2f, clipSectionZ);
        clippableWall.transform.localScale = new Vector3(wallThickness, clipSectionHeight, clipSectionWidth);
        // Slightly different color to give a subtle visual hint
        SetColor(clippableWall, new Color(0.53f, 0.54f, 0.48f));

        // Make the collider a trigger (the Level3 script also does this, but set it here too)
        BoxCollider clippableCol = clippableWall.GetComponent<BoxCollider>();
        if (clippableCol == null) clippableCol = clippableWall.AddComponent<BoxCollider>();
        clippableCol.isTrigger = true;

        // --- Exit Trigger Zone (outside east wall) ---
        GameObject exitZone = GameObject.Find("ExitZone");
        if (exitZone == null)
        {
            exitZone = new GameObject("ExitZone");
        }
        exitZone.transform.position = new Vector3(roomW / 2f + 1.5f, 1f, clipSectionZ);
        BoxCollider exitCol = exitZone.GetComponent<BoxCollider>();
        if (exitCol == null) exitCol = exitZone.AddComponent<BoxCollider>();
        exitCol.size = new Vector3(2f, 3f, 3f);
        exitCol.isTrigger = true;

        // --- Player Spawn Point ---
        GameObject spawnPoint = GameObject.Find("PlayerSpawnPoint");
        if (spawnPoint == null)
        {
            spawnPoint = new GameObject("PlayerSpawnPoint");
        }
        spawnPoint.transform.position = new Vector3(0f, 1f, -3f); // Center-south of room, facing north
        spawnPoint.transform.rotation = Quaternion.identity; // Looking towards +Z (north wall / door)

        // --- Point Light (inside the room for atmosphere) ---
        GameObject pointLightObj = GameObject.Find("RoomLight");
        if (pointLightObj == null)
        {
            pointLightObj = new GameObject("RoomLight");
        }
        Light pointLight = pointLightObj.GetComponent<Light>();
        if (pointLight == null) pointLight = pointLightObj.AddComponent<Light>();
        pointLight.type = LightType.Point;
        pointLight.range = 15f;
        pointLight.intensity = 1.2f;
        pointLight.color = new Color(1f, 0.95f, 0.8f);
        pointLightObj.transform.position = new Vector3(0f, 3.5f, 0f);

        // =====================================================================
        // Level Manager
        // =====================================================================

        GameObject levelRoot = GameObject.Find("Level3Manager");
        if (levelRoot == null)
        {
            levelRoot = new GameObject("Level3Manager");
        }
        Level3_WallClip wallClipScript = levelRoot.GetComponent<Level3_WallClip>();
        if (wallClipScript == null)
        {
            wallClipScript = levelRoot.AddComponent<Level3_WallClip>();
        }

        // Wire references
        wallClipScript.normalDoor = door;
        wallClipScript.clippableWallSection = clippableWall;
        wallClipScript.clippableCollider = clippableCol;
        wallClipScript.exitTriggerPoint = exitZone.transform;
        wallClipScript.exitZoneTrigger = exitCol;
        wallClipScript.playerSpawnPoint = spawnPoint.transform;
        wallClipScript.needsPlayer = true;
        wallClipScript.wantsCursorLocked = true;

        // --- EventSystem for standalone testing ---
        if (Object.FindAnyObjectByType<EventSystem>() == null)
        {
            GameObject esObj = new GameObject("EventSystem");
            esObj.AddComponent<EventSystem>();
            esObj.AddComponent<StandaloneInputModule>();
        }

        // Save
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("[SceneSetup] LEVEL3 scene set up with room geometry and wall-clip puzzle");
    }

    [MenuItem("Unfunctional/6. Setup LEVEL4 Scene")]
    public static void SetupLevel4Scene()
    {
        // Open LEVEL4 scene
        var scene = EditorSceneManager.OpenScene("Assets/Scenes/LEVEL4.unity", OpenSceneMode.Single);

        // Clean up default camera
        GameObject defaultCam = GameObject.FindGameObjectWithTag("MainCamera");
        if (defaultCam != null && defaultCam.name == "Main Camera")
        {
            Object.DestroyImmediate(defaultCam);
        }

        // =====================================================================
        // Lighting
        // =====================================================================

        Light dirLight = Object.FindAnyObjectByType<Light>();
        if (dirLight == null)
        {
            GameObject lightObj = new GameObject("Directional Light");
            dirLight = lightObj.AddComponent<Light>();
            dirLight.type = LightType.Directional;
            lightObj.transform.eulerAngles = new Vector3(50f, -30f, 0f);
        }
        dirLight.color = new Color(0.75f, 0.8f, 0.9f, 1f);
        dirLight.intensity = 0.4f;

        // =====================================================================
        // Room Geometry
        // =====================================================================
        // Room is 8x4x8. North wall has a sliding door with keypad + sticky notes.

        float roomW = 8f;
        float roomH = 4f;
        float roomD = 8f;
        float wallThickness = 0.2f;

        // Floor
        GameObject floor = CreateOrFindPrimitive("Floor", PrimitiveType.Cube);
        floor.transform.position = new Vector3(0f, -wallThickness / 2f, 0f);
        floor.transform.localScale = new Vector3(roomW, wallThickness, roomD);
        SetColor(floor, new Color(0.3f, 0.3f, 0.32f));

        // Ceiling
        GameObject ceiling = CreateOrFindPrimitive("Ceiling", PrimitiveType.Cube);
        ceiling.transform.position = new Vector3(0f, roomH + wallThickness / 2f, 0f);
        ceiling.transform.localScale = new Vector3(roomW, wallThickness, roomD);
        SetColor(ceiling, new Color(0.45f, 0.45f, 0.48f));

        // South wall (behind player)
        GameObject wallSouth = CreateOrFindPrimitive("WallSouth", PrimitiveType.Cube);
        wallSouth.transform.position = new Vector3(0f, roomH / 2f, -roomD / 2f);
        wallSouth.transform.localScale = new Vector3(roomW, roomH, wallThickness);
        SetColor(wallSouth, new Color(0.5f, 0.5f, 0.52f));

        // West wall
        GameObject wallWest = CreateOrFindPrimitive("WallWest", PrimitiveType.Cube);
        wallWest.transform.position = new Vector3(-roomW / 2f, roomH / 2f, 0f);
        wallWest.transform.localScale = new Vector3(wallThickness, roomH, roomD);
        SetColor(wallWest, new Color(0.5f, 0.5f, 0.52f));

        // East wall
        GameObject wallEast = CreateOrFindPrimitive("WallEast", PrimitiveType.Cube);
        wallEast.transform.position = new Vector3(roomW / 2f, roomH / 2f, 0f);
        wallEast.transform.localScale = new Vector3(wallThickness, roomH, roomD);
        SetColor(wallEast, new Color(0.5f, 0.5f, 0.52f));

        // North wall -- split around the door
        float doorWidth = 1.6f;
        float doorHeight = 2.6f;
        float northSolidW = (roomW - doorWidth) / 2f;

        // North left
        GameObject wallNL = CreateOrFindPrimitive("WallNorthLeft", PrimitiveType.Cube);
        wallNL.transform.position = new Vector3(-(doorWidth / 2f + northSolidW / 2f), roomH / 2f, roomD / 2f);
        wallNL.transform.localScale = new Vector3(northSolidW, roomH, wallThickness);
        SetColor(wallNL, new Color(0.5f, 0.5f, 0.52f));

        // North right
        GameObject wallNR = CreateOrFindPrimitive("WallNorthRight", PrimitiveType.Cube);
        wallNR.transform.position = new Vector3(doorWidth / 2f + northSolidW / 2f, roomH / 2f, roomD / 2f);
        wallNR.transform.localScale = new Vector3(northSolidW, roomH, wallThickness);
        SetColor(wallNR, new Color(0.5f, 0.5f, 0.52f));

        // North above door
        GameObject wallNA = CreateOrFindPrimitive("WallNorthAbove", PrimitiveType.Cube);
        float aboveDoorH = roomH - doorHeight;
        wallNA.transform.position = new Vector3(0f, doorHeight + aboveDoorH / 2f, roomD / 2f);
        wallNA.transform.localScale = new Vector3(doorWidth, aboveDoorH, wallThickness);
        SetColor(wallNA, new Color(0.5f, 0.5f, 0.52f));

        // The door (slides up when unlocked)
        GameObject door = CreateOrFindPrimitive("Door4", PrimitiveType.Cube);
        door.transform.position = new Vector3(0f, doorHeight / 2f, roomD / 2f);
        door.transform.localScale = new Vector3(doorWidth - 0.05f, doorHeight, wallThickness * 0.6f);
        SetColor(door, new Color(0.35f, 0.35f, 0.4f)); // metallic gray

        // =====================================================================
        // Keypad (small box on the wall to the right of the door)
        // =====================================================================
        GameObject keypad = CreateOrFindPrimitive("Keypad", PrimitiveType.Cube);
        float keypadX = doorWidth / 2f + 0.5f;
        keypad.transform.position = new Vector3(keypadX, 1.3f, roomD / 2f - 0.05f);
        keypad.transform.localScale = new Vector3(0.4f, 0.5f, 0.08f);
        SetColor(keypad, new Color(0.15f, 0.15f, 0.2f)); // dark panel

        // =====================================================================
        // Sticky Notes (small quads above the keypad)
        // =====================================================================

        // Sticky note 1: email address
        GameObject sticky1 = CreateOrFindPrimitive("StickyNote_Email", PrimitiveType.Quad);
        sticky1.transform.position = new Vector3(keypadX - 0.15f, 1.85f, roomD / 2f - 0.05f);
        sticky1.transform.localScale = new Vector3(0.35f, 0.2f, 1f);
        sticky1.transform.rotation = Quaternion.Euler(0f, 0f, -5f);
        SetColor(sticky1, new Color(1f, 1f, 0.5f)); // yellow sticky
        // Remove collider from quad (the keypad handles interaction)
        MeshCollider stickyCol1 = sticky1.GetComponent<MeshCollider>();
        if (stickyCol1 != null) Object.DestroyImmediate(stickyCol1);

        // Sticky note 2: warning
        GameObject sticky2 = CreateOrFindPrimitive("StickyNote_Warning", PrimitiveType.Quad);
        sticky2.transform.position = new Vector3(keypadX + 0.18f, 1.9f, roomD / 2f - 0.05f);
        sticky2.transform.localScale = new Vector3(0.32f, 0.18f, 1f);
        sticky2.transform.rotation = Quaternion.Euler(0f, 0f, 3f);
        SetColor(sticky2, new Color(1f, 0.7f, 0.5f)); // orange sticky
        MeshCollider stickyCol2 = sticky2.GetComponent<MeshCollider>();
        if (stickyCol2 != null) Object.DestroyImmediate(stickyCol2);

        // Sticky note area point (for interaction detection)
        GameObject stickyPoint = GameObject.Find("StickyNotePoint");
        if (stickyPoint == null) stickyPoint = new GameObject("StickyNotePoint");
        stickyPoint.transform.position = new Vector3(keypadX, 1.7f, roomD / 2f - 0.1f);

        // =====================================================================
        // Room light
        // =====================================================================
        GameObject pointLightObj = GameObject.Find("RoomLight");
        if (pointLightObj == null) pointLightObj = new GameObject("RoomLight");
        Light pointLight = pointLightObj.GetComponent<Light>();
        if (pointLight == null) pointLight = pointLightObj.AddComponent<Light>();
        pointLight.type = LightType.Point;
        pointLight.range = 12f;
        pointLight.intensity = 1.0f;
        pointLight.color = new Color(0.9f, 0.95f, 1f);
        pointLightObj.transform.position = new Vector3(0f, 3.5f, 0f);

        // A small spot light aimed at the keypad/sticky notes
        GameObject spotObj = GameObject.Find("KeypadSpotlight");
        if (spotObj == null) spotObj = new GameObject("KeypadSpotlight");
        Light spotLight = spotObj.GetComponent<Light>();
        if (spotLight == null) spotLight = spotObj.AddComponent<Light>();
        spotLight.type = LightType.Spot;
        spotLight.range = 5f;
        spotLight.spotAngle = 50f;
        spotLight.intensity = 2f;
        spotLight.color = new Color(1f, 0.95f, 0.85f);
        spotObj.transform.position = new Vector3(keypadX, 2.8f, roomD / 2f - 0.8f);
        spotObj.transform.rotation = Quaternion.LookRotation(
            new Vector3(keypadX, 1.5f, roomD / 2f) - spotObj.transform.position);

        // =====================================================================
        // Spawn point
        // =====================================================================
        GameObject spawnPoint = GameObject.Find("PlayerSpawnPoint");
        if (spawnPoint == null) spawnPoint = new GameObject("PlayerSpawnPoint");
        spawnPoint.transform.position = new Vector3(0f, 1f, -2.5f);
        spawnPoint.transform.rotation = Quaternion.identity;

        // =====================================================================
        // Level Manager
        // =====================================================================
        GameObject levelRoot = GameObject.Find("Level4Manager");
        if (levelRoot == null) levelRoot = new GameObject("Level4Manager");
        Level4_KeypadPuzzle kpScript = levelRoot.GetComponent<Level4_KeypadPuzzle>();
        if (kpScript == null) kpScript = levelRoot.AddComponent<Level4_KeypadPuzzle>();

        // Wire references
        kpScript.doorObject = door;
        kpScript.keypadObject = keypad;
        kpScript.stickyNotePoint = stickyPoint.transform;
        kpScript.playerSpawnPoint = spawnPoint.transform;
        kpScript.needsPlayer = true;
        kpScript.wantsCursorLocked = true;

        // EventSystem
        if (Object.FindAnyObjectByType<EventSystem>() == null)
        {
            GameObject esObj = new GameObject("EventSystem");
            esObj.AddComponent<EventSystem>();
            esObj.AddComponent<StandaloneInputModule>();
        }

        // Save
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("[SceneSetup] LEVEL4 scene set up with keypad puzzle room");
    }

    /// <summary>
    /// Find an existing named GameObject or create a new primitive.
    /// </summary>
    private static GameObject CreateOrFindPrimitive(string name, PrimitiveType type)
    {
        GameObject obj = GameObject.Find(name);
        if (obj != null) return obj;

        obj = GameObject.CreatePrimitive(type);
        obj.name = name;
        return obj;
    }

    /// <summary>
    /// Set a solid color on a renderer via a simple material.
    /// </summary>
    private static void SetColor(GameObject obj, Color color)
    {
        Renderer rend = obj.GetComponent<Renderer>();
        if (rend == null) return;

        // Use the URP Lit shader if available, otherwise Standard
        Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        if (mat.shader == null || mat.shader.name == "Hidden/InternalErrorShader")
        {
            mat = new Material(Shader.Find("Standard"));
        }
        mat.color = color;
        rend.sharedMaterial = mat;
    }

    // =========================================================================
    // Helper Methods
    // =========================================================================

    private static void PositionRect(GameObject obj, Vector2 anchorMin, Vector2 anchorMax)
    {
        RectTransform rect = EnsureRectTransform(obj);
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private static Text EnsureText(GameObject obj)
    {
        Text text = obj.GetComponent<Text>();
        if (text == null) text = obj.AddComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        return text;
    }

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
