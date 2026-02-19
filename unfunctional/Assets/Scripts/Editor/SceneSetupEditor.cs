using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.Collections.Generic;

/// <summary>
/// Editor utility to set up the game scene hierarchies and create prefabs.
/// Run from the Unity menu: Unfunctional > Setup All Scenes
///
/// SAFETY: All methods are idempotent. Running them on an already-configured
/// scene will NOT destroy existing objects. Objects are found-or-created by
/// name. References are only wired when they are currently null.
/// </summary>
public class SceneSetupEditor : EditorWindow
{
    // =========================================================================
    // Prefab paths
    // =========================================================================

    private const string PlayerPrefabPath = "Assets/Prefabs/Player.prefab";
    private const string DoorPrefabPath   = "Assets/Prefabs/LEVEL_DOOR.prefab";

    // =========================================================================
    // Menu: Setup All
    // =========================================================================

    [MenuItem("Unfunctional/Setup All Scenes")]
    public static void SetupAllScenes()
    {
        if (!EditorUtility.DisplayDialog(
            "Setup All Scenes",
            "This will (idempotently):\n" +
            "1. Create the Player prefab (if missing)\n" +
            "2. Set up the GLOBAL scene\n" +
            "3. Set up LEVEL1-LEVEL11 scenes\n\n" +
            "Existing objects will NOT be destroyed. Continue?",
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
        SetupLevel5Scene();
        SetupLevel6Scene();
        SetupLevel7Scene();
        SetupLevel8Scene();
        SetupLevel9Scene();
        SetupLevel10Scene();
        SetupLevel11Scene();
        RegisterScenesInBuildSettings();

        Debug.Log("[SceneSetup] All scenes set up successfully!");
        EditorUtility.DisplayDialog("Done", "All scenes and prefabs have been set up.", "OK");
    }

    // =========================================================================
    // Register Scenes in Build Settings
    // =========================================================================

    [MenuItem("Unfunctional/Register Scenes in Build Settings")]
    public static void RegisterScenesInBuildSettings()
    {
        string[] scenePaths = new string[]
        {
            "Assets/Scenes/GLOBAL.unity",
            "Assets/Scenes/LEVEL1.unity",
            "Assets/Scenes/LEVEL2.unity",
            "Assets/Scenes/LEVEL3.unity",
            "Assets/Scenes/LEVEL4.unity",
            "Assets/Scenes/LEVEL5.unity",
            "Assets/Scenes/LEVEL6.unity",
            "Assets/Scenes/LEVEL7.unity",
            "Assets/Scenes/LEVEL8.unity",
            "Assets/Scenes/LEVEL9.unity",
            "Assets/Scenes/LEVEL10.unity",
            "Assets/Scenes/LEVEL11.unity"
        };

        List<EditorBuildSettingsScene> buildScenes = new List<EditorBuildSettingsScene>();

        foreach (string path in scenePaths)
        {
            if (System.IO.File.Exists(path))
            {
                buildScenes.Add(new EditorBuildSettingsScene(path, true));
            }
            else
            {
                Debug.LogWarning($"[SceneSetup] Scene file not found: {path}");
            }
        }

        EditorBuildSettings.scenes = buildScenes.ToArray();
        Debug.Log($"[SceneSetup] Registered {buildScenes.Count} scenes in Build Settings.");
    }

    // =========================================================================
    // 1. Player Prefab
    // =========================================================================

    [MenuItem("Unfunctional/1. Create Player Prefab")]
    public static void CreatePlayerPrefab()
    {
        // Skip if prefab already exists
        if (AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath) != null)
        {
            Debug.Log("[SceneSetup] Player prefab already exists, skipping.");
            return;
        }

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
        camObj.transform.localPosition = new Vector3(0, 0.8f, 0);
        camObj.transform.localRotation = Quaternion.identity;

        Camera cam = camObj.AddComponent<Camera>();
        cam.fieldOfView = 70;
        cam.nearClipPlane = 0.1f;
        cam.farClipPlane = 1000f;
        cam.depth = 0;
        cam.tag = "MainCamera";
        camObj.tag = "MainCamera";

        // No AudioListener here -- GLOBAL BackgroundCamera already has one.

        // Wire the camera reference on PlayerController
        PlayerController pc = player.GetComponent<PlayerController>();
        pc.cameraTransform = camObj.transform;

        // Save as prefab
        PrefabUtility.SaveAsPrefabAsset(player, PlayerPrefabPath);
        Object.DestroyImmediate(player);

        AssetDatabase.SaveAssets();
        Debug.Log($"[SceneSetup] Player prefab created at {PlayerPrefabPath}");
    }

    // =========================================================================
    // 2. GLOBAL Scene
    // =========================================================================

    [MenuItem("Unfunctional/2. Setup GLOBAL Scene")]
    public static void SetupGlobalScene()
    {
        var scene = EditorSceneManager.OpenScene("Assets/Scenes/GLOBAL.unity", OpenSceneMode.Single);

        // --- Background Camera ---
        GameObject camObj = GameObject.Find("BackgroundCamera");
        if (camObj == null)
        {
            // Reuse default Main Camera if present, otherwise create new
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
        bgCam.depth = -100;
        bgCam.cullingMask = 0;
        camObj.tag = "Untagged";
        if (camObj.GetComponent<AudioListener>() == null)
            camObj.AddComponent<AudioListener>();

        // --- GameManager ---
        GameObject gmObj = FindOrCreateEmpty("GameManager");
        GameManager gm = gmObj.GetComponent<GameManager>();
        if (gm == null) gm = gmObj.AddComponent<GameManager>();

        // Wire Player prefab reference
        if (gm.playerPrefab == null)
        {
            GameObject playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath);
            if (playerPrefab != null)
            {
                gm.playerPrefab = playerPrefab;
                Debug.Log("[SceneSetup] Player prefab wired on GameManager.");
            }
            else
            {
                Debug.LogWarning("[SceneSetup] Player prefab not found. Run 'Create Player Prefab' first.");
            }
        }

        // --- InputManager ---
        GameObject imObj = FindOrCreateEmpty("InputManager");
        if (imObj.GetComponent<InputManager>() == null) imObj.AddComponent<InputManager>();

        // --- EventSystem ---
        EnsureEventSystem();

        // --- Pause Menu Canvas ---
        if (GameObject.Find("PauseCanvas") == null)
        {
            CreatePauseMenuCanvas();
        }

        // --- Debug Panel ---
        if (gmObj.GetComponent<DebugPanel>() == null) gmObj.AddComponent<DebugPanel>();

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("[SceneSetup] GLOBAL scene set up.");
    }

    // =========================================================================
    // 3. LEVEL1 Scene
    // =========================================================================

    [MenuItem("Unfunctional/3. Setup LEVEL1 Scene")]
    public static void SetupLevel1Scene()
    {
        var scene = EditorSceneManager.OpenScene("Assets/Scenes/LEVEL1.unity", OpenSceneMode.Single);

        // --- Level Manager Root ---
        GameObject levelRoot = FindOrCreateEmpty("Level1Manager");
        Level1_ConfusingMenu menuScript = levelRoot.GetComponent<Level1_ConfusingMenu>();
        if (menuScript == null) menuScript = levelRoot.AddComponent<Level1_ConfusingMenu>();

        // --- Menu Canvas ---
        GameObject canvasObj = GameObject.Find("MenuCanvas");
        Canvas canvas;
        if (canvasObj == null)
        {
            canvasObj = new GameObject("MenuCanvas");
            canvas = canvasObj.AddComponent<Canvas>();
            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            canvasObj.AddComponent<GraphicRaycaster>();
            UIHelper.ConfigureCanvas(canvas, sortingOrder: 10);
        }
        else
        {
            canvas = canvasObj.GetComponent<Canvas>();
        }

        if (menuScript.menuCanvas == null) menuScript.menuCanvas = canvas;

        // --- Main Panel ---
        GameObject mainPanel = FindOrCreateChild(canvasObj, "MainPanel");
        RectTransform mainPanelRect = EnsureRectTransform(mainPanel);
        mainPanelRect.anchorMin = Vector2.zero;
        mainPanelRect.anchorMax = Vector2.one;
        mainPanelRect.offsetMin = Vector2.zero;
        mainPanelRect.offsetMax = Vector2.zero;

        Image mainPanelBg = mainPanel.GetComponent<Image>();
        if (mainPanelBg == null) mainPanelBg = mainPanel.AddComponent<Image>();
        mainPanelBg.color = new Color(0.08f, 0.08f, 0.12f, 1f);

        if (menuScript.mainPanel == null) menuScript.mainPanel = mainPanelRect;

        // --- Title Text ---
        GameObject titleObj = FindOrCreateChild(mainPanel, "TitleText");
        Text titleText = titleObj.GetComponent<Text>();
        if (titleText == null)
        {
            titleText = titleObj.AddComponent<Text>();
            titleText.text = "UNFUNCTIONAL";
            titleText.fontSize = 72;
            titleText.font = UIHelper.GetDefaultFont();
            titleText.alignment = TextAnchor.MiddleCenter;
            titleText.color = new Color(0.9f, 0.9f, 0.9f, 1f);
        }

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
            subtitleText.text = "The Worst Game Ever Made (on purpose)";
            subtitleText.fontSize = 24;
            subtitleText.font = UIHelper.GetDefaultFont();
            subtitleText.alignment = TextAnchor.MiddleCenter;
            subtitleText.color = new Color(0.6f, 0.6f, 0.6f, 1f);
        }

        RectTransform subtitleRect = subtitleObj.GetComponent<RectTransform>();
        subtitleRect.anchorMin = new Vector2(0.15f, 0.68f);
        subtitleRect.anchorMax = new Vector2(0.85f, 0.76f);
        subtitleRect.offsetMin = Vector2.zero;
        subtitleRect.offsetMax = Vector2.zero;

        // --- Create Fake Start Buttons ---
        string[] fakeLabels = { "Start Game", "Begin", "Play", "Start", "New Game", "Launch" };

        Vector2[] fakePositions =
        {
            new Vector2(0.5f, 0.55f),
            new Vector2(0.3f, 0.45f),
            new Vector2(0.7f, 0.45f),
            new Vector2(0.2f, 0.35f),
            new Vector2(0.8f, 0.35f),
            new Vector2(0.5f, 0.25f)
        };

        if (menuScript.fakeStartButtons == null || menuScript.fakeStartButtons.Length == 0)
        {
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
        }

        // --- Create Decoy Buttons ---
        string[] decoyLabels = { "Options", "Credits", "Help", "Extra",
                                  "More Options", "Quit?", "Settings", "About" };

        Vector2[] decoyPositions =
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

        if (menuScript.decoyButtons == null || menuScript.decoyButtons.Length == 0)
        {
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
        }

        // --- The REAL Start Button ---
        if (menuScript.realStartButton == null)
        {
            GameObject realBtnObj = FindOrCreateChild(mainPanel, "RealStartButton");
            Button realBtn = SetupButton(realBtnObj, "Terms of Service",
                new Vector2(0.92f, 0.03f), new Vector2(100, 20),
                new Color(0.15f, 0.15f, 0.15f, 0.6f), 9);
            menuScript.realStartButton = realBtn;
        }

        // --- Sub-Menu Panels ---
        string[] subMenuTitles =
        {
            "COOKIE POLICY\n\nWe use cookies.\nActually, we use the whole bakery.\n\nAccept?",
            "SYSTEM CHECK\n\nChecking system...\nYour system is... a computer.\nProbably.\n\nOK",
            "TERMS OF SERVICE\n\nBy reading this you agree\nto not read this.\n\nAgree / Also Agree",
            "UPDATE AVAILABLE\n\nVersion 0.0.0.0.1 is ready.\nChangelog: Fixed nothing.\n\nInstall Later"
        };

        if (menuScript.subMenuPanels == null || menuScript.subMenuPanels.Length == 0)
        {
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
                panelText.font = UIHelper.GetDefaultFont();
                panelText.alignment = TextAnchor.MiddleCenter;
                panelText.color = Color.white;

                RectTransform textRect = textObj.GetComponent<RectTransform>();
                textRect.anchorMin = new Vector2(0.05f, 0.05f);
                textRect.anchorMax = new Vector2(0.95f, 0.95f);
                textRect.offsetMin = Vector2.zero;
                textRect.offsetMax = Vector2.zero;

                // Close button
                GameObject closeBtnObj = FindOrCreateChild(panel, "CloseBtn");
                SetupButton(closeBtnObj, "X",
                    new Vector2(0.95f, 0.95f), new Vector2(30, 30),
                    new Color(0.6f, 0.1f, 0.1f, 1f), 14);

                panel.SetActive(false);
                subMenuPanels[i] = panel;
            }
            menuScript.subMenuPanels = subMenuPanels;
        }

        EnsureEventSystem();

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("[SceneSetup] LEVEL1 scene set up.");
    }

    // =========================================================================
    // 4. LEVEL2 Scene
    // =========================================================================

    [MenuItem("Unfunctional/4. Setup LEVEL2 Scene")]
    public static void SetupLevel2Scene()
    {
        var scene = EditorSceneManager.OpenScene("Assets/Scenes/LEVEL2.unity", OpenSceneMode.Single);

        // Level2_SettingsPuzzle builds all UI at runtime, so the editor setup
        // just creates the root object, a canvas, and an EventSystem.
        GameObject levelRoot = FindOrCreateEmpty("Level2Manager");
        Level2_SettingsPuzzle puzzleScript = levelRoot.GetComponent<Level2_SettingsPuzzle>();
        if (puzzleScript == null) puzzleScript = levelRoot.AddComponent<Level2_SettingsPuzzle>();

        // --- Settings Canvas ---
        GameObject canvasObj = GameObject.Find("SettingsCanvas");
        if (canvasObj == null)
        {
            canvasObj = new GameObject("SettingsCanvas");
            Canvas canvas = canvasObj.AddComponent<Canvas>();
            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            canvasObj.AddComponent<GraphicRaycaster>();
            UIHelper.ConfigureCanvas(canvas, sortingOrder: 10);
        }

        if (puzzleScript.settingsCanvas == null)
        {
            puzzleScript.settingsCanvas = canvasObj.GetComponent<Canvas>();
        }

        EnsureEventSystem();

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("[SceneSetup] LEVEL2 scene set up.");
    }

    // =========================================================================
    // 5. LEVEL3 Scene
    // =========================================================================

    [MenuItem("Unfunctional/5. Setup LEVEL3 Scene")]
    public static void SetupLevel3Scene()
    {
        var scene = EditorSceneManager.OpenScene("Assets/Scenes/LEVEL3.unity", OpenSceneMode.Single);

        // --- Level Manager ---
        GameObject levelRoot = FindOrCreateEmpty("Level3Manager");
        Level3_WallClip wallClipScript = levelRoot.GetComponent<Level3_WallClip>();
        if (wallClipScript == null) wallClipScript = levelRoot.AddComponent<Level3_WallClip>();

        // If already configured with references, just ensure basics and save
        bool alreadyConfigured = (wallClipScript.normalDoor != null &&
                                   wallClipScript.clippableWallSection != null &&
                                   wallClipScript.exitZoneTrigger != null);

        if (alreadyConfigured)
        {
            Debug.Log("[SceneSetup] LEVEL3 already configured, skipping geometry setup.");
            wallClipScript.needsPlayer = true;
            wallClipScript.wantsCursorLocked = true;

            // Ensure spawn point reference
            if (wallClipScript.playerSpawnPoint == null)
            {
                GameObject sp = GameObject.Find("PlayerSpawnPoint");
                if (sp != null) wallClipScript.playerSpawnPoint = sp.transform;
            }

            EnsureEventSystem();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            return;
        }

        // --- First-time setup: create room geometry ---
        float roomW = 10f;
        float roomH = 4f;
        float roomD = 10f;
        float wallThickness = 0.2f;

        // Floor
        GameObject floor = CreateOrFindPrimitive("Floor", PrimitiveType.Cube);
        floor.transform.position = new Vector3(0f, -wallThickness / 2f, 0f);
        floor.transform.localScale = new Vector3(roomW, wallThickness, roomD);
        SetColor(floor, new Color(0.35f, 0.35f, 0.35f));

        // Ceiling
        GameObject ceiling = CreateOrFindPrimitive("Ceiling", PrimitiveType.Cube);
        ceiling.transform.position = new Vector3(0f, roomH + wallThickness / 2f, 0f);
        ceiling.transform.localScale = new Vector3(roomW, wallThickness, roomD);
        SetColor(ceiling, new Color(0.5f, 0.5f, 0.5f));

        // South wall
        GameObject wallSouth = CreateOrFindPrimitive("WallSouth", PrimitiveType.Cube);
        wallSouth.transform.position = new Vector3(0f, roomH / 2f, -roomD / 2f);
        wallSouth.transform.localScale = new Vector3(roomW, roomH, wallThickness);
        SetColor(wallSouth, new Color(0.55f, 0.55f, 0.5f));

        // West wall
        GameObject wallWest = CreateOrFindPrimitive("WallWest", PrimitiveType.Cube);
        wallWest.transform.position = new Vector3(-roomW / 2f, roomH / 2f, 0f);
        wallWest.transform.localScale = new Vector3(wallThickness, roomH, roomD);
        SetColor(wallWest, new Color(0.55f, 0.55f, 0.5f));

        // --- North Wall: Door (LEVEL_DOOR prefab) ---
        DoorController doorCtrl = null;
        GameObject doorInstance = FindDoorInstance();

        if (doorInstance == null)
        {
            doorInstance = InstantiateDoorPrefab("NorthDoor",
                new Vector3(0f, 0f, roomD / 2f), Quaternion.identity);
        }

        if (doorInstance != null)
        {
            doorCtrl = doorInstance.GetComponent<DoorController>();
            if (doorCtrl != null)
            {
                doorCtrl.unlockMethod = DoorController.UnlockMethod.None;
                doorCtrl.ApplyUnlockMethod();
            }
        }

        // --- East Wall (has clippable section) ---
        float clipSectionWidth = 2f;
        float clipSectionHeight = 2.5f;
        float clipSectionZ = 0f;
        float eastSolidLeftW = (roomD - clipSectionWidth) / 2f;

        GameObject wallEastLeft = CreateOrFindPrimitive("WallEastLeft", PrimitiveType.Cube);
        wallEastLeft.transform.position = new Vector3(roomW / 2f, roomH / 2f, -(clipSectionWidth / 2f + eastSolidLeftW / 2f));
        wallEastLeft.transform.localScale = new Vector3(wallThickness, roomH, eastSolidLeftW);
        SetColor(wallEastLeft, new Color(0.55f, 0.55f, 0.5f));

        GameObject wallEastRight = CreateOrFindPrimitive("WallEastRight", PrimitiveType.Cube);
        wallEastRight.transform.position = new Vector3(roomW / 2f, roomH / 2f, clipSectionWidth / 2f + eastSolidLeftW / 2f);
        wallEastRight.transform.localScale = new Vector3(wallThickness, roomH, eastSolidLeftW);
        SetColor(wallEastRight, new Color(0.55f, 0.55f, 0.5f));

        GameObject wallEastTop = CreateOrFindPrimitive("WallEastTop", PrimitiveType.Cube);
        float aboveClipH = roomH - clipSectionHeight;
        wallEastTop.transform.position = new Vector3(roomW / 2f, clipSectionHeight + aboveClipH / 2f, clipSectionZ);
        wallEastTop.transform.localScale = new Vector3(wallThickness, aboveClipH, clipSectionWidth);
        SetColor(wallEastTop, new Color(0.55f, 0.55f, 0.5f));

        // Clippable wall section
        GameObject clippableWall = CreateOrFindPrimitive("ClippableWall", PrimitiveType.Cube);
        clippableWall.transform.position = new Vector3(roomW / 2f, clipSectionHeight / 2f, clipSectionZ);
        clippableWall.transform.localScale = new Vector3(wallThickness, clipSectionHeight, clipSectionWidth);
        SetColor(clippableWall, new Color(0.53f, 0.54f, 0.48f));

        BoxCollider clippableCol = clippableWall.GetComponent<BoxCollider>();
        if (clippableCol == null) clippableCol = clippableWall.AddComponent<BoxCollider>();
        clippableCol.isTrigger = true;

        // Exit trigger zone
        GameObject exitZone = FindOrCreateEmpty("ExitZone");
        exitZone.transform.position = new Vector3(roomW / 2f + 1.5f, 1f, clipSectionZ);
        BoxCollider exitCol = exitZone.GetComponent<BoxCollider>();
        if (exitCol == null) exitCol = exitZone.AddComponent<BoxCollider>();
        exitCol.size = new Vector3(2f, 3f, 3f);
        exitCol.isTrigger = true;

        // Player spawn point
        GameObject spawnPoint = FindOrCreateEmpty("PlayerSpawnPoint");
        spawnPoint.transform.position = new Vector3(0f, 1f, -3f);
        spawnPoint.transform.rotation = Quaternion.identity;

        // Room light
        GameObject pointLightObj = FindOrCreateEmpty("RoomLight");
        Light pointLight = pointLightObj.GetComponent<Light>();
        if (pointLight == null) pointLight = pointLightObj.AddComponent<Light>();
        pointLight.type = LightType.Point;
        pointLight.range = 15f;
        pointLight.intensity = 1.2f;
        pointLight.color = new Color(1f, 0.95f, 0.8f);
        pointLightObj.transform.position = new Vector3(0f, 3.5f, 0f);

        // --- Wire Level Manager references ---
        if (doorCtrl != null && wallClipScript.normalDoor == null)
        {
            wallClipScript.normalDoor = doorCtrl.doorPanel;
            wallClipScript.doorController = doorCtrl;
        }
        if (wallClipScript.clippableWallSection == null)
            wallClipScript.clippableWallSection = clippableWall;
        if (wallClipScript.clippableCollider == null)
            wallClipScript.clippableCollider = clippableCol;
        if (wallClipScript.exitTriggerPoint == null)
            wallClipScript.exitTriggerPoint = exitZone.transform;
        if (wallClipScript.exitZoneTrigger == null)
            wallClipScript.exitZoneTrigger = exitCol;
        if (wallClipScript.playerSpawnPoint == null)
            wallClipScript.playerSpawnPoint = spawnPoint.transform;
        wallClipScript.needsPlayer = true;
        wallClipScript.wantsCursorLocked = true;

        EnsureEventSystem();

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("[SceneSetup] LEVEL3 scene set up.");
    }

    // =========================================================================
    // 6. LEVEL4 Scene
    // =========================================================================

    [MenuItem("Unfunctional/6. Setup LEVEL4 Scene")]
    public static void SetupLevel4Scene()
    {
        var scene = EditorSceneManager.OpenScene("Assets/Scenes/LEVEL4.unity", OpenSceneMode.Single);

        // --- Level Manager ---
        GameObject levelRoot = FindOrCreateEmpty("Level4Manager");
        Level4_KeypadPuzzle kpScript = levelRoot.GetComponent<Level4_KeypadPuzzle>();
        if (kpScript == null) kpScript = levelRoot.AddComponent<Level4_KeypadPuzzle>();

        // If already configured with references, just ensure basics and save
        bool alreadyConfigured = (kpScript.doorObject != null &&
                                   kpScript.keypadObject != null);

        if (alreadyConfigured)
        {
            Debug.Log("[SceneSetup] LEVEL4 already configured, skipping geometry setup.");
            kpScript.needsPlayer = true;
            kpScript.wantsCursorLocked = true;

            if (kpScript.playerSpawnPoint == null)
            {
                GameObject sp = GameObject.Find("PlayerSpawnPoint");
                if (sp != null) kpScript.playerSpawnPoint = sp.transform;
            }

            EnsureEventSystem();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            return;
        }

        // --- Room Geometry ---
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

        // South wall
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

        // --- North Wall: LEVEL_DOOR prefab ---
        DoorController doorCtrl = null;
        GameObject doorInstance = FindDoorInstance();

        if (doorInstance == null)
        {
            doorInstance = InstantiateDoorPrefab("LEVEL_DOOR",
                new Vector3(0f, 0f, roomD / 2f), Quaternion.identity);
        }

        if (doorInstance != null)
        {
            doorCtrl = doorInstance.GetComponent<DoorController>();
            if (doorCtrl != null)
            {
                doorCtrl.unlockMethod = DoorController.UnlockMethod.Keypad;

                // Ensure KeypadController exists on the door for interactive keypad
                KeypadController kc = doorInstance.GetComponent<KeypadController>();
                if (kc == null) kc = doorInstance.AddComponent<KeypadController>();
                if (doorCtrl.keypadController == null)
                    doorCtrl.keypadController = kc;

                doorCtrl.ApplyUnlockMethod();
            }

            // Ensure the door panel has a collider for raycasting
            if (doorCtrl != null && doorCtrl.doorPanel != null)
            {
                if (doorCtrl.doorPanel.GetComponent<Collider>() == null)
                    doorCtrl.doorPanel.AddComponent<BoxCollider>();
            }
        }

        // --- 3D Keypad visuals next to the door ---
        float keypadX = 1.5f; // Right of door center
        float keypadZ = roomD / 2f - 0.08f; // Slightly in front of north wall

        // Keypad box (small dark panel on the wall)
        GameObject keypadBox = CreateOrFindPrimitive("KeypadBox", PrimitiveType.Cube);
        keypadBox.transform.position = new Vector3(keypadX, 1.3f, keypadZ);
        keypadBox.transform.localScale = new Vector3(0.4f, 0.5f, 0.08f);
        SetColor(keypadBox, new Color(0.15f, 0.15f, 0.2f));

        // Sticky note 1: email address (yellow)
        GameObject stickyEmail = CreateOrFindPrimitive("StickyNote_Email", PrimitiveType.Quad);
        stickyEmail.transform.position = new Vector3(keypadX - 0.15f, 1.85f, keypadZ);
        stickyEmail.transform.localScale = new Vector3(0.35f, 0.2f, 1f);
        stickyEmail.transform.localRotation = Quaternion.Euler(0f, 0f, -5f);
        SetColor(stickyEmail, new Color(1f, 1f, 0.5f));
        // Remove mesh collider on quads (they interfere with raycasting)
        MeshCollider mc1 = stickyEmail.GetComponent<MeshCollider>();
        if (mc1 != null) Object.DestroyImmediate(mc1);

        // Sticky note 2: warning (orange)
        GameObject stickyWarn = CreateOrFindPrimitive("StickyNote_Warning", PrimitiveType.Quad);
        stickyWarn.transform.position = new Vector3(keypadX + 0.18f, 1.9f, keypadZ);
        stickyWarn.transform.localScale = new Vector3(0.32f, 0.18f, 1f);
        stickyWarn.transform.localRotation = Quaternion.Euler(0f, 0f, 3f);
        SetColor(stickyWarn, new Color(1f, 0.7f, 0.5f));
        MeshCollider mc2 = stickyWarn.GetComponent<MeshCollider>();
        if (mc2 != null) Object.DestroyImmediate(mc2);

        // Sticky note interaction center point
        GameObject stickyPoint = FindOrCreateEmpty("StickyNotePoint");
        stickyPoint.transform.position = new Vector3(keypadX, 1.7f, keypadZ - 0.1f);

        // --- Lighting ---
        // Ambient point light
        GameObject pointLightObj = FindOrCreateEmpty("RoomLight");
        Light pointLight = pointLightObj.GetComponent<Light>();
        if (pointLight == null) pointLight = pointLightObj.AddComponent<Light>();
        pointLight.type = LightType.Point;
        pointLight.range = 12f;
        pointLight.intensity = 1.0f;
        pointLight.color = new Color(0.9f, 0.95f, 1f);
        pointLightObj.transform.position = new Vector3(0f, 3.5f, 0f);

        // Spot light aimed at keypad / sticky notes
        GameObject spotObj = FindOrCreateEmpty("KeypadSpotlight");
        Light spotLight = spotObj.GetComponent<Light>();
        if (spotLight == null) spotLight = spotObj.AddComponent<Light>();
        spotLight.type = LightType.Spot;
        spotLight.range = 5f;
        spotLight.spotAngle = 50f;
        spotLight.intensity = 2f;
        spotLight.color = new Color(1f, 0.95f, 0.85f);
        spotObj.transform.position = new Vector3(keypadX, 2.8f, keypadZ - 0.8f);
        spotObj.transform.rotation = Quaternion.LookRotation(
            keypadBox.transform.position - spotObj.transform.position);

        // --- Spawn point ---
        GameObject spawnPoint = FindOrCreateEmpty("PlayerSpawnPoint");
        spawnPoint.transform.position = new Vector3(0f, 1f, -2.5f);
        spawnPoint.transform.rotation = Quaternion.LookRotation(Vector3.forward);

        // --- Wire Level Manager references ---
        if (doorCtrl != null)
        {
            if (kpScript.doorObject == null)
                kpScript.doorObject = doorCtrl.doorPanel ?? doorInstance;
            if (kpScript.doorController == null)
                kpScript.doorController = doorCtrl;

            // Use the prefab's keypad visual for the raycast target
            if (kpScript.keypadObject == null)
            {
                if (doorCtrl.keypadPanel != null)
                    kpScript.keypadObject = doorCtrl.keypadPanel;
                else if (doorCtrl.keypadMount != null)
                    kpScript.keypadObject = doorCtrl.keypadMount;
                else
                    kpScript.keypadObject = keypadBox; // fallback to editor-created box
            }

            // Use the prefab's sticky note point if available
            if (kpScript.stickyNotePoint == null)
            {
                if (doorCtrl.stickyNotePoint != null)
                    kpScript.stickyNotePoint = doorCtrl.stickyNotePoint;
                else
                    kpScript.stickyNotePoint = stickyPoint.transform;
            }
        }
        else
        {
            if (kpScript.keypadObject == null)
                kpScript.keypadObject = keypadBox;
            if (kpScript.stickyNotePoint == null)
                kpScript.stickyNotePoint = stickyPoint.transform;
        }
        if (kpScript.playerSpawnPoint == null)
            kpScript.playerSpawnPoint = spawnPoint.transform;

        kpScript.needsPlayer = true;
        kpScript.wantsCursorLocked = true;

        EnsureEventSystem();

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("[SceneSetup] LEVEL4 scene set up with LEVEL_DOOR prefab and keypad puzzle room.");
    }

    // =========================================================================
    // 7. LEVEL5 Scene (Dumb NPC)
    // =========================================================================

    [MenuItem("Unfunctional/7. Setup LEVEL5 Scene")]
    public static void SetupLevel5Scene()
    {
        var scene = EditorSceneManager.OpenScene("Assets/Scenes/LEVEL5.unity", OpenSceneMode.Single);

        GameObject levelRoot = FindOrCreateEmpty("Level5Manager");
        Level5_DumbNPC npcScript = levelRoot.GetComponent<Level5_DumbNPC>();
        if (npcScript == null) npcScript = levelRoot.AddComponent<Level5_DumbNPC>();

        // If already configured, skip
        if (npcScript.npcObject != null)
        {
            Debug.Log("[SceneSetup] LEVEL5 already configured, skipping.");
            npcScript.needsPlayer = true;
            npcScript.wantsCursorLocked = true;
            if (npcScript.playerSpawnPoint == null)
            {
                GameObject sp = GameObject.Find("PlayerSpawnPoint");
                if (sp != null) npcScript.playerSpawnPoint = sp.transform;
            }
            EnsureEventSystem();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            return;
        }

        // --- Room geometry ---
        float roomW = 10f, roomH = 4f, roomD = 10f, wt = 0.2f;

        CreateOrFindPrimitive("Floor", PrimitiveType.Cube);
        GameObject floor = GameObject.Find("Floor");
        floor.transform.position = new Vector3(0, -wt / 2f, 0);
        floor.transform.localScale = new Vector3(roomW, wt, roomD);
        SetColor(floor, new Color(0.35f, 0.35f, 0.35f));

        CreateOrFindPrimitive("Ceiling", PrimitiveType.Cube);
        GameObject ceiling = GameObject.Find("Ceiling");
        ceiling.transform.position = new Vector3(0, roomH + wt / 2f, 0);
        ceiling.transform.localScale = new Vector3(roomW, wt, roomD);
        SetColor(ceiling, new Color(0.5f, 0.5f, 0.5f));

        CreateBoxWalls(roomW, roomH, roomD, wt);

        // --- NPC ---
        GameObject npc = CreateOrFindPrimitive("NPC_Gorp", PrimitiveType.Capsule);
        npc.transform.position = new Vector3(0, 1f, 2.5f);
        npc.transform.rotation = Quaternion.Euler(0, 180, 0);
        SetColor(npc, new Color(0.6f, 0.4f, 0.3f));

        // --- Door (LEVEL_DOOR prefab, unlock = None, NPC script opens it) ---
        DoorController doorCtrl = null;
        GameObject doorInstance = FindDoorInstance();
        if (doorInstance == null)
        {
            doorInstance = InstantiateDoorPrefab("LEVEL_DOOR",
                new Vector3(0, 0, roomD / 2f), Quaternion.identity);
        }
        if (doorInstance != null)
        {
            doorCtrl = doorInstance.GetComponent<DoorController>();
            if (doorCtrl != null)
            {
                doorCtrl.unlockMethod = DoorController.UnlockMethod.None;
                doorCtrl.ApplyUnlockMethod();
            }
        }

        // Spawn
        GameObject spawnPoint = FindOrCreateEmpty("PlayerSpawnPoint");
        spawnPoint.transform.position = new Vector3(0, 1f, -3f);
        spawnPoint.transform.rotation = Quaternion.identity;

        // Light
        GameObject lightObj = FindOrCreateEmpty("RoomLight");
        Light pointLight = lightObj.GetComponent<Light>();
        if (pointLight == null) pointLight = lightObj.AddComponent<Light>();
        pointLight.type = LightType.Point;
        pointLight.range = 15f;
        pointLight.intensity = 1.2f;
        pointLight.color = new Color(1f, 0.95f, 0.8f);
        lightObj.transform.position = new Vector3(0, 3.5f, 0);

        // Wire references
        npcScript.npcObject = npc;
        npcScript.playerSpawnPoint = spawnPoint.transform;
        npcScript.needsPlayer = true;
        npcScript.wantsCursorLocked = true;

        EnsureEventSystem();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("[SceneSetup] LEVEL5 scene set up.");
    }

    // =========================================================================
    // 8. LEVEL6 Scene (Quicktime Events)
    // =========================================================================

    [MenuItem("Unfunctional/8. Setup LEVEL6 Scene")]
    public static void SetupLevel6Scene()
    {
        var scene = EditorSceneManager.OpenScene("Assets/Scenes/LEVEL6.unity", OpenSceneMode.Single);

        GameObject levelRoot = FindOrCreateEmpty("Level6Manager");
        Level6_QuicktimeEvents qteScript = levelRoot.GetComponent<Level6_QuicktimeEvents>();
        if (qteScript == null) qteScript = levelRoot.AddComponent<Level6_QuicktimeEvents>();

        // Wire doorController if missing
        if (qteScript.doorController == null)
        {
            GameObject doorGO = FindDoorInstance();
            if (doorGO != null)
            {
                DoorController dc = doorGO.GetComponent<DoorController>();
                if (dc != null)
                {
                    qteScript.doorController = dc;
                    dc.unlockMethod = DoorController.UnlockMethod.None;
                    dc.ApplyUnlockMethod();
                }
            }
        }

        // If already configured, skip
        if (qteScript.qteCanvas != null)
        {
            Debug.Log("[SceneSetup] LEVEL6 already configured, skipping.");
            qteScript.needsPlayer = true;
            qteScript.wantsCursorLocked = true;
            if (qteScript.playerSpawnPoint == null)
            {
                GameObject sp = GameObject.Find("PlayerSpawnPoint");
                if (sp != null) qteScript.playerSpawnPoint = sp.transform;
            }
            EnsureEventSystem();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            return;
        }

        // --- Hallway geometry (simple corridor) ---
        float hallW = 4f, hallH = 3.5f, hallL = 30f, wt = 0.2f;

        CreateOrFindPrimitive("Floor", PrimitiveType.Cube);
        GameObject floor = GameObject.Find("Floor");
        floor.transform.position = new Vector3(0, -wt / 2f, hallL / 2f);
        floor.transform.localScale = new Vector3(hallW, wt, hallL + 2f);
        SetColor(floor, new Color(0.3f, 0.3f, 0.32f));

        CreateOrFindPrimitive("Ceiling", PrimitiveType.Cube);
        GameObject ceiling = GameObject.Find("Ceiling");
        ceiling.transform.position = new Vector3(0, hallH + wt / 2f, hallL / 2f);
        ceiling.transform.localScale = new Vector3(hallW, wt, hallL + 2f);
        SetColor(ceiling, new Color(0.48f, 0.48f, 0.45f));

        CreateOrFindPrimitive("WallLeft", PrimitiveType.Cube);
        GameObject wallLeft = GameObject.Find("WallLeft");
        wallLeft.transform.position = new Vector3(-hallW / 2f - wt / 2f, hallH / 2f, hallL / 2f);
        wallLeft.transform.localScale = new Vector3(wt, hallH, hallL + 2f);
        SetColor(wallLeft, new Color(0.42f, 0.42f, 0.4f));

        CreateOrFindPrimitive("WallRight", PrimitiveType.Cube);
        GameObject wallRight = GameObject.Find("WallRight");
        wallRight.transform.position = new Vector3(hallW / 2f + wt / 2f, hallH / 2f, hallL / 2f);
        wallRight.transform.localScale = new Vector3(wt, hallH, hallL + 2f);
        SetColor(wallRight, new Color(0.42f, 0.42f, 0.4f));

        // Back wall
        CreateOrFindPrimitive("WallBack", PrimitiveType.Cube);
        GameObject wallBack = GameObject.Find("WallBack");
        wallBack.transform.position = new Vector3(0, hallH / 2f, -1f);
        wallBack.transform.localScale = new Vector3(hallW, hallH, wt);
        SetColor(wallBack, new Color(0.42f, 0.42f, 0.4f));

        // Door at end
        DoorController doorCtrl = null;
        GameObject doorInstance = FindDoorInstance();
        if (doorInstance == null)
        {
            doorInstance = InstantiateDoorPrefab("LEVEL_DOOR",
                new Vector3(0, 0, hallL), Quaternion.identity);
        }
        if (doorInstance != null)
        {
            doorCtrl = doorInstance.GetComponent<DoorController>();
            if (doorCtrl != null)
            {
                doorCtrl.unlockMethod = DoorController.UnlockMethod.None;
                doorCtrl.ApplyUnlockMethod();
            }
        }

        // QTE Canvas
        GameObject canvasObj = FindOrCreateEmpty("QTECanvas");
        Canvas canvas = canvasObj.GetComponent<Canvas>();
        if (canvas == null)
        {
            canvas = canvasObj.AddComponent<Canvas>();
            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            canvasObj.AddComponent<GraphicRaycaster>();
            UIHelper.ConfigureCanvas(canvas, sortingOrder: 20);
        }
        qteScript.qteCanvas = canvas;

        // Spawn
        GameObject spawnPoint = FindOrCreateEmpty("PlayerSpawnPoint");
        spawnPoint.transform.position = new Vector3(0, 1f, 1f);
        spawnPoint.transform.rotation = Quaternion.LookRotation(Vector3.forward);

        // Light
        GameObject lightObj = FindOrCreateEmpty("HallLight");
        Light hallLight = lightObj.GetComponent<Light>();
        if (hallLight == null) hallLight = lightObj.AddComponent<Light>();
        hallLight.type = LightType.Point;
        hallLight.range = 20f;
        hallLight.intensity = 1f;
        hallLight.color = new Color(1f, 0.95f, 0.85f);
        lightObj.transform.position = new Vector3(0, 3f, hallL / 2f);

        qteScript.playerSpawnPoint = spawnPoint.transform;
        qteScript.needsPlayer = true;
        qteScript.wantsCursorLocked = true;

        // Wire door controller
        if (doorCtrl != null && qteScript.doorController == null)
            qteScript.doorController = doorCtrl;

        EnsureEventSystem();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("[SceneSetup] LEVEL6 scene set up.");
    }

    // =========================================================================
    // 9. LEVEL7 Scene (Compass Hallways)
    // =========================================================================

    [MenuItem("Unfunctional/9. Setup LEVEL7 Scene")]
    public static void SetupLevel7Scene()
    {
        var scene = EditorSceneManager.OpenScene("Assets/Scenes/LEVEL7.unity", OpenSceneMode.Single);

        GameObject levelRoot = FindOrCreateEmpty("Level7Manager");
        Level7_CompassHallways compassScript = levelRoot.GetComponent<Level7_CompassHallways>();
        if (compassScript == null) compassScript = levelRoot.AddComponent<Level7_CompassHallways>();

        // Level7 builds all geometry at runtime -- just set flags
        compassScript.needsPlayer = true;
        compassScript.wantsCursorLocked = true;

        // Try to wire door if one exists
        DoorController doorCtrl = null;
        GameObject doorInstance = FindDoorInstance();
        if (doorInstance != null)
        {
            doorCtrl = doorInstance.GetComponent<DoorController>();
            if (compassScript.doorController == null)
                compassScript.doorController = doorCtrl;
        }

        EnsureEventSystem();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("[SceneSetup] LEVEL7 scene set up.");
    }

    // =========================================================================
    // 10. LEVEL8 Scene (DLC Door)
    // =========================================================================

    [MenuItem("Unfunctional/10. Setup LEVEL8 Scene")]
    public static void SetupLevel8Scene()
    {
        var scene = EditorSceneManager.OpenScene("Assets/Scenes/LEVEL8.unity", OpenSceneMode.Single);

        GameObject levelRoot = FindOrCreateEmpty("Level8Manager");
        Level8_DLCDoor dlcScript = levelRoot.GetComponent<Level8_DLCDoor>();
        if (dlcScript == null) dlcScript = levelRoot.AddComponent<Level8_DLCDoor>();

        // Level8 builds all geometry at runtime -- just set flags
        dlcScript.needsPlayer = true;
        dlcScript.wantsCursorLocked = true;

        // Try to wire door if one exists
        DoorController doorCtrl = null;
        GameObject doorInstance = FindDoorInstance();
        if (doorInstance != null)
        {
            doorCtrl = doorInstance.GetComponent<DoorController>();
            if (dlcScript.doorController == null)
                dlcScript.doorController = doorCtrl;
        }

        EnsureEventSystem();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("[SceneSetup] LEVEL8 scene set up.");
    }

    // =========================================================================
    // 11. LEVEL9 Scene (Walking Simulator)
    // =========================================================================

    [MenuItem("Unfunctional/11. Setup LEVEL9 Scene")]
    public static void SetupLevel9Scene()
    {
        var scene = EditorSceneManager.OpenScene("Assets/Scenes/LEVEL9.unity", OpenSceneMode.Single);

        GameObject levelRoot = FindOrCreateEmpty("Level9Manager");
        Level9_WalkingSimulator walkScript = levelRoot.GetComponent<Level9_WalkingSimulator>();
        if (walkScript == null) walkScript = levelRoot.AddComponent<Level9_WalkingSimulator>();

        // Level9 builds all geometry at runtime
        walkScript.needsPlayer = true;
        walkScript.wantsCursorLocked = true;

        DoorController doorCtrl = null;
        GameObject doorInstance = FindDoorInstance();
        if (doorInstance != null)
        {
            doorCtrl = doorInstance.GetComponent<DoorController>();
            if (walkScript.doorController == null)
                walkScript.doorController = doorCtrl;
        }

        EnsureEventSystem();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("[SceneSetup] LEVEL9 scene set up.");
    }

    // =========================================================================
    // 12. LEVEL10 Scene (Item Degradation)
    // =========================================================================

    [MenuItem("Unfunctional/12. Setup LEVEL10 Scene")]
    public static void SetupLevel10Scene()
    {
        var scene = EditorSceneManager.OpenScene("Assets/Scenes/LEVEL10.unity", OpenSceneMode.Single);

        GameObject levelRoot = FindOrCreateEmpty("Level10Manager");
        Level10_ItemDegradation degradeScript = levelRoot.GetComponent<Level10_ItemDegradation>();
        if (degradeScript == null) degradeScript = levelRoot.AddComponent<Level10_ItemDegradation>();

        // Level10 builds all geometry at runtime
        degradeScript.needsPlayer = true;
        degradeScript.wantsCursorLocked = true;

        DoorController doorCtrl = null;
        GameObject doorInstance = FindDoorInstance();
        if (doorInstance != null)
        {
            doorCtrl = doorInstance.GetComponent<DoorController>();
            if (degradeScript.doorController == null)
                degradeScript.doorController = doorCtrl;
        }

        EnsureEventSystem();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("[SceneSetup] LEVEL10 scene set up.");
    }

    // =========================================================================
    // 13. LEVEL11 Scene (Bad RNG)
    // =========================================================================

    [MenuItem("Unfunctional/13. Setup LEVEL11 Scene")]
    public static void SetupLevel11Scene()
    {
        var scene = EditorSceneManager.OpenScene("Assets/Scenes/LEVEL11.unity", OpenSceneMode.Single);

        GameObject levelRoot = FindOrCreateEmpty("Level11Manager");
        Level11_BadRNG rngScript = levelRoot.GetComponent<Level11_BadRNG>();
        if (rngScript == null) rngScript = levelRoot.AddComponent<Level11_BadRNG>();

        // Level11 builds all geometry at runtime
        rngScript.needsPlayer = true;
        rngScript.wantsCursorLocked = true;

        DoorController doorCtrl = null;
        GameObject doorInstance = FindDoorInstance();
        if (doorInstance != null)
        {
            doorCtrl = doorInstance.GetComponent<DoorController>();
            if (rngScript.doorController == null)
                rngScript.doorController = doorCtrl;
        }

        EnsureEventSystem();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("[SceneSetup] LEVEL11 scene set up.");
    }

    // =========================================================================
    // Helper: Door Prefab
    // =========================================================================

    /// <summary>
    /// Find an existing LEVEL_DOOR prefab instance in the scene.
    /// Searches for common names that indicate a door was placed.
    /// </summary>
    private static GameObject FindDoorInstance()
    {
        // Check by the prefab root name (what Unity uses when instantiating)
        string[] doorNames = { "LEVEL_DOOR", "NorthDoor", "Door" };
        foreach (string name in doorNames)
        {
            GameObject found = GameObject.Find(name);
            if (found != null && found.GetComponent<DoorController>() != null)
                return found;
        }

        // Also check if any DoorController exists in the scene
        DoorController dc = Object.FindAnyObjectByType<DoorController>();
        if (dc != null) return dc.gameObject;

        return null;
    }

    /// <summary>
    /// Instantiate the LEVEL_DOOR prefab into the current scene.
    /// Returns null if the prefab asset is not found.
    /// </summary>
    private static GameObject InstantiateDoorPrefab(string instanceName, Vector3 position, Quaternion rotation)
    {
        GameObject doorPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(DoorPrefabPath);
        if (doorPrefab == null)
        {
            Debug.LogWarning($"[SceneSetup] Door prefab not found at {DoorPrefabPath}.");
            return null;
        }

        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(doorPrefab);
        instance.name = instanceName;
        instance.transform.position = position;
        instance.transform.rotation = rotation;
        return instance;
    }

    // =========================================================================
    // Helper: Find or Create
    // =========================================================================

    /// <summary>
    /// Find a GameObject by name in the scene, or create an empty one.
    /// </summary>
    private static GameObject FindOrCreateEmpty(string name)
    {
        GameObject obj = GameObject.Find(name);
        if (obj != null) return obj;
        return new GameObject(name);
    }

    /// <summary>
    /// Find an existing named GameObject or create a new primitive.
    /// If the object already exists, it is returned as-is (not modified).
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

        Shader urpLit = Shader.Find("Universal Render Pipeline/Lit");
        Material mat;
        if (urpLit != null && urpLit.name != "Hidden/InternalErrorShader")
        {
            mat = new Material(urpLit);
            mat.SetColor("_BaseColor", color); // URP uses _BaseColor not _Color
        }
        else
        {
            mat = new Material(Shader.Find("Standard"));
            mat.color = color;
        }
        rend.sharedMaterial = mat;
    }

    /// <summary>
    /// Creates the four walls (N/S/E/W) for a room.
    /// </summary>
    private static void CreateBoxWalls(float roomW, float roomH, float roomD, float wt)
    {
        GameObject wallSouth = CreateOrFindPrimitive("WallSouth", PrimitiveType.Cube);
        wallSouth.transform.position = new Vector3(0, roomH / 2f, -roomD / 2f);
        wallSouth.transform.localScale = new Vector3(roomW, roomH, wt);
        SetColor(wallSouth, new Color(0.55f, 0.55f, 0.5f));

        GameObject wallNorth = CreateOrFindPrimitive("WallNorth", PrimitiveType.Cube);
        wallNorth.transform.position = new Vector3(0, roomH / 2f, roomD / 2f);
        wallNorth.transform.localScale = new Vector3(roomW, roomH, wt);
        SetColor(wallNorth, new Color(0.55f, 0.55f, 0.5f));

        GameObject wallWest = CreateOrFindPrimitive("WallWest", PrimitiveType.Cube);
        wallWest.transform.position = new Vector3(-roomW / 2f, roomH / 2f, 0);
        wallWest.transform.localScale = new Vector3(wt, roomH, roomD);
        SetColor(wallWest, new Color(0.55f, 0.55f, 0.5f));

        GameObject wallEast = CreateOrFindPrimitive("WallEast", PrimitiveType.Cube);
        wallEast.transform.position = new Vector3(roomW / 2f, roomH / 2f, 0);
        wallEast.transform.localScale = new Vector3(wt, roomH, roomD);
        SetColor(wallEast, new Color(0.55f, 0.55f, 0.5f));
    }

    /// <summary>
    /// Ensure an EventSystem exists in the current scene.
    /// </summary>
    private static void EnsureEventSystem()
    {
        if (Object.FindAnyObjectByType<EventSystem>() == null)
        {
            GameObject esObj = new GameObject("EventSystem");
            esObj.AddComponent<EventSystem>();
            esObj.AddComponent<StandaloneInputModule>();
        }
    }

    // =========================================================================
    // Helper: UI
    // =========================================================================

    private static GameObject FindOrCreateChild(GameObject parent, string name)
    {
        Transform existing = parent.transform.Find(name);
        if (existing != null) return existing.gameObject;

        GameObject child = new GameObject(name);
        child.transform.SetParent(parent.transform, false);
        EnsureRectTransform(child);
        return child;
    }

    private static RectTransform EnsureRectTransform(GameObject obj)
    {
        RectTransform rect = obj.GetComponent<RectTransform>();
        if (rect == null) rect = obj.AddComponent<RectTransform>();
        return rect;
    }

    private static Button SetupButton(GameObject btnObj, string label, Vector2 anchorCenter,
        Vector2 size, Color bgColor, int fontSize)
    {
        RectTransform btnRect = EnsureRectTransform(btnObj);
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
        text.font = UIHelper.GetDefaultFont();
        text.alignment = TextAnchor.MiddleCenter;
        text.color = Color.white;

        return btn;
    }

    private static GameObject CreatePauseMenuCanvas()
    {
        // Canvas
        GameObject canvasObj = new GameObject("PauseCanvas");
        Canvas canvas = canvasObj.AddComponent<Canvas>();

        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);

        canvasObj.AddComponent<GraphicRaycaster>();
        UIHelper.ConfigureCanvas(canvas, sortingOrder: 100);

        // GamePauseMenu component
        GamePauseMenu pauseMenu = canvasObj.AddComponent<GamePauseMenu>();

        // --- Pause Panel ---
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
        titleText.font = UIHelper.GetDefaultFont();
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
        text.font = UIHelper.GetDefaultFont();
        text.alignment = TextAnchor.MiddleCenter;
        text.color = Color.white;

        return btnObj;
    }
}
