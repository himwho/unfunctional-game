using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// LEVEL 10: Item degradation level. A workshop with tasks that each need a
/// tool, but every tool breaks almost immediately. Tasks: dig a hole, hammer
/// a nail, saw a plank, turn a bolt, sweep the floor. Tools break after 1-3
/// uses and the player must grab replacements from a tool rack. After all 5
/// tasks are done, the exit door unlocks.
///
/// Builds workshop geometry, tools, task stations, and HUD at runtime.
/// Attach to root GameObject in LEVEL10 scene.
/// </summary>
public class Level10_ItemDegradation : LevelManager
{
    [Header("Level 10 - Item Degradation")]
    public DoorController doorController;

    [Header("Tool Settings")]
    public int maxDurability = 3;           // Uses before tool breaks
    public float interactRange = 3.5f;

    // Task definitions
    [System.Serializable]
    public class Task
    {
        public string name;
        public string toolName;
        public int requiredUses;            // How many successful uses to complete
        public int currentUses;
        public bool completed;
        public Vector3 stationPosition;
        public GameObject stationObject;
        public GameObject progressIndicator;
    }

    private List<Task> tasks = new List<Task>();

    // Current held tool
    private string currentToolName = "";
    private int currentToolDurability = 0;
    private GameObject currentToolVisual;

    // HUD
    private Canvas hudCanvas;
    private Text promptText;
    private Text toolText;
    private Text taskListText;
    private Text breakText;

    // Tool rack
    private Vector3 toolRackPosition;
    private Dictionary<string, Color> toolColors = new Dictionary<string, Color>();

    // Break messages
    private readonly string[] breakMessages = new string[]
    {
        "SNAP! The {0} broke!",
        "CRACK! There goes another {0}.",
        "The {0} couldn't handle the pressure.",
        "*crunch* ...that {0} is done for.",
        "The {0} disintegrated in your hands.",
        "Well, that {0} lasted about as long as expected.",
    };

    protected override void Start()
    {
        base.Start();
        levelDisplayName = "Tool Trouble";
        levelDescription = "Complete the tasks. The tools have other plans.";
        needsPlayer = true;
        wantsCursorLocked = true;

        InitToolColors();
        InitTasks();
        BuildWorkshop();
        CreateTaskStations();
        CreateToolRack();
        CreateHUD();
        UpdateTaskList();
    }

    private void Update()
    {
        if (levelComplete) return;

        UpdateInteraction();
    }

    // =========================================================================
    // Initialization
    // =========================================================================

    private void InitToolColors()
    {
        toolColors["Shovel"] = new Color(0.5f, 0.4f, 0.3f);
        toolColors["Hammer"] = new Color(0.6f, 0.3f, 0.2f);
        toolColors["Saw"] = new Color(0.7f, 0.7f, 0.7f);
        toolColors["Wrench"] = new Color(0.3f, 0.3f, 0.6f);
        toolColors["Broom"] = new Color(0.6f, 0.5f, 0.3f);
    }

    private void InitTasks()
    {
        tasks = new List<Task>
        {
            new Task { name = "Dig a Hole", toolName = "Shovel", requiredUses = 5, stationPosition = new Vector3(-3f, 0f, 2f) },
            new Task { name = "Hammer a Nail", toolName = "Hammer", requiredUses = 4, stationPosition = new Vector3(-1f, 0f, 3.5f) },
            new Task { name = "Saw a Plank", toolName = "Saw", requiredUses = 6, stationPosition = new Vector3(1f, 0f, 3.5f) },
            new Task { name = "Turn a Bolt", toolName = "Wrench", requiredUses = 3, stationPosition = new Vector3(3f, 0f, 2f) },
            new Task { name = "Sweep the Floor", toolName = "Broom", requiredUses = 5, stationPosition = new Vector3(0f, 0f, 1f) },
        };
    }

    // =========================================================================
    // Interaction
    // =========================================================================

    private void UpdateInteraction()
    {
        Camera cam = Camera.main;
        if (cam == null) return;

        Ray ray = new Ray(cam.transform.position, cam.transform.forward);
        bool showPrompt = false;

        if (Physics.Raycast(ray, out RaycastHit hit, interactRange, ~0, QueryTriggerInteraction.Collide))
        {
            string hitName = hit.collider.gameObject.name;

            // Check if looking at tool rack
            if (hitName.Contains("ToolRack") || hitName.Contains("Tool_"))
            {
                showPrompt = true;

                // Determine which tool based on what's closest
                string toolName = GetToolFromHit(hit);
                promptText.text = $"Press [E] to pick up {toolName}";

                if (Input.GetKeyDown(KeyCode.E))
                    PickUpTool(toolName);
            }
            // Check if looking at a task station
            else
            {
                for (int i = 0; i < tasks.Count; i++)
                {
                    if (tasks[i].stationObject != null &&
                        (hit.collider.gameObject == tasks[i].stationObject ||
                         hit.collider.transform.IsChildOf(tasks[i].stationObject.transform)))
                    {
                        showPrompt = true;

                        if (tasks[i].completed)
                        {
                            promptText.text = $"{tasks[i].name} - DONE";
                        }
                        else if (currentToolName == tasks[i].toolName)
                        {
                            promptText.text = $"Press [E] to use {currentToolName} ({currentToolDurability} uses left)";

                            if (Input.GetKeyDown(KeyCode.E))
                                UseTool(i);
                        }
                        else if (string.IsNullOrEmpty(currentToolName))
                        {
                            promptText.text = $"Need: {tasks[i].toolName} (pick one up from the rack)";
                        }
                        else
                        {
                            promptText.text = $"Wrong tool! Need: {tasks[i].toolName}, have: {currentToolName}";
                        }
                        break;
                    }
                }
            }
        }

        if (!showPrompt)
        {
            if (!string.IsNullOrEmpty(currentToolName))
                promptText.text = $"Holding: {currentToolName} ({currentToolDurability} uses left)";
            else
                promptText.text = "";
        }

        // Update tool display
        if (toolText != null)
        {
            toolText.text = string.IsNullOrEmpty(currentToolName)
                ? "No tool equipped"
                : $"{currentToolName} [{currentToolDurability}/{maxDurability}]";
        }
    }

    private string GetToolFromHit(RaycastHit hit)
    {
        string hitName = hit.collider.gameObject.name;
        foreach (var kvp in toolColors)
        {
            if (hitName.Contains(kvp.Key))
                return kvp.Key;
        }
        // Default based on position
        float x = hit.point.x;
        string[] tools = { "Shovel", "Hammer", "Saw", "Wrench", "Broom" };
        int index = Mathf.Clamp((int)((x + 2.5f) / 1.2f), 0, tools.Length - 1);
        return tools[index];
    }

    private void PickUpTool(string toolName)
    {
        // Drop current tool visual
        if (currentToolVisual != null)
            Destroy(currentToolVisual);

        currentToolName = toolName;
        currentToolDurability = Random.Range(1, maxDurability + 1); // 1-3 uses

        // Create a small visual indicator attached to camera
        Camera cam = Camera.main;
        if (cam != null)
        {
            currentToolVisual = GameObject.CreatePrimitive(PrimitiveType.Cube);
            currentToolVisual.name = $"HeldTool_{toolName}";
            currentToolVisual.transform.SetParent(cam.transform);
            currentToolVisual.transform.localPosition = new Vector3(0.4f, -0.3f, 0.5f);
            currentToolVisual.transform.localScale = new Vector3(0.1f, 0.1f, 0.5f);
            currentToolVisual.transform.localRotation = Quaternion.Euler(0, 0, 30);

            Color toolColor = toolColors.ContainsKey(toolName) ? toolColors[toolName] : Color.grey;
            currentToolVisual.GetComponent<Renderer>().material.color = toolColor;

            // Disable collider on held tool
            Destroy(currentToolVisual.GetComponent<Collider>());
        }

        Debug.Log($"[Level10] Picked up {toolName} with {currentToolDurability} durability");
    }

    private void UseTool(int taskIndex)
    {
        Task task = tasks[taskIndex];

        if (currentToolDurability <= 0 || task.completed) return;

        // Use the tool
        task.currentUses++;
        currentToolDurability--;

        Debug.Log($"[Level10] Used {currentToolName} on '{task.name}' ({task.currentUses}/{task.requiredUses}), durability: {currentToolDurability}");

        // Check if tool broke
        if (currentToolDurability <= 0)
        {
            BreakTool();
        }

        // Check if task complete
        if (task.currentUses >= task.requiredUses)
        {
            task.completed = true;
            Debug.Log($"[Level10] Task '{task.name}' completed!");

            // Visual feedback on station
            if (task.progressIndicator != null)
            {
                task.progressIndicator.GetComponent<Renderer>().material.color = Color.green;
            }

            CheckAllTasksComplete();
        }
        else
        {
            // Update progress indicator
            if (task.progressIndicator != null)
            {
                float progress = (float)task.currentUses / task.requiredUses;
                task.progressIndicator.transform.localScale = new Vector3(
                    progress * 0.8f, 0.1f, 0.1f);
            }
        }

        UpdateTaskList();
    }

    private void BreakTool()
    {
        string msg = breakMessages[Random.Range(0, breakMessages.Length)];
        msg = msg.Replace("{0}", currentToolName);

        Debug.Log($"[Level10] {msg}");

        // Show break message
        StartCoroutine(ShowBreakMessage(msg));

        // Destroy tool visual
        if (currentToolVisual != null)
        {
            // Shake and destroy
            StartCoroutine(BreakAnimation(currentToolVisual));
        }

        currentToolName = "";
        currentToolDurability = 0;
    }

    private IEnumerator BreakAnimation(GameObject tool)
    {
        // Quick shake
        Vector3 originalPos = tool.transform.localPosition;
        for (int i = 0; i < 8; i++)
        {
            tool.transform.localPosition = originalPos + Random.insideUnitSphere * 0.05f;
            yield return new WaitForSeconds(0.03f);
        }
        Destroy(tool);
    }

    private IEnumerator ShowBreakMessage(string msg)
    {
        if (breakText == null) yield break;

        breakText.text = msg;
        breakText.color = new Color(1f, 0.3f, 0.3f, 1f);

        yield return new WaitForSeconds(2f);

        float fadeTime = 1f;
        float elapsed = 0f;
        while (elapsed < fadeTime)
        {
            elapsed += Time.deltaTime;
            breakText.color = new Color(1f, 0.3f, 0.3f, 1f - (elapsed / fadeTime));
            yield return null;
        }
        breakText.text = "";
    }

    private void CheckAllTasksComplete()
    {
        foreach (var task in tasks)
        {
            if (!task.completed) return;
        }

        // All tasks done!
        Debug.Log("[Level10] All tasks complete! Opening door.");

        if (doorController != null)
        {
            doorController.OpenDoor();
            StartCoroutine(CompleteLevelDelay());
        }
        else
        {
            CompleteLevel();
        }
    }

    private IEnumerator CompleteLevelDelay()
    {
        yield return new WaitForSeconds(2f);
        CompleteLevel();
    }

    private void UpdateTaskList()
    {
        if (taskListText == null) return;

        string text = "TASKS:\n";
        foreach (var task in tasks)
        {
            string status = task.completed ? "[DONE]" : $"[{task.currentUses}/{task.requiredUses}]";
            text += $"  {status} {task.name} ({task.toolName})\n";
        }
        taskListText.text = text;
    }

    // =========================================================================
    // Workshop Geometry
    // =========================================================================

    private void BuildWorkshop()
    {
        float roomW = 12f, roomH = 4f, roomD = 10f, wt = 0.2f;
        Material wallMat = CreateMat(new Color(0.5f, 0.45f, 0.4f));
        Material floorMat = CreateMat(new Color(0.35f, 0.3f, 0.28f));

        CreateBox("Floor", new Vector3(0, -wt / 2f, 0), new Vector3(roomW, wt, roomD), floorMat);
        CreateBox("Ceiling", new Vector3(0, roomH + wt / 2f, 0), new Vector3(roomW, wt, roomD), wallMat);
        CreateBox("WallSouth", new Vector3(0, roomH / 2f, -roomD / 2f), new Vector3(roomW, roomH, wt), wallMat);
        CreateBox("WallNorth", new Vector3(0, roomH / 2f, roomD / 2f), new Vector3(roomW, roomH, wt), wallMat);
        CreateBox("WallWest", new Vector3(-roomW / 2f, roomH / 2f, 0), new Vector3(wt, roomH, roomD), wallMat);
        CreateBox("WallEast", new Vector3(roomW / 2f, roomH / 2f, 0), new Vector3(wt, roomH, roomD), wallMat);

        // Light
        GameObject lightObj = new GameObject("WorkshopLight");
        Light light = lightObj.AddComponent<Light>();
        light.type = LightType.Point;
        light.range = 18f;
        light.intensity = 1.3f;
        light.color = new Color(1f, 0.92f, 0.8f);
        lightObj.transform.position = new Vector3(0, 3.5f, 0);

        // Second light
        GameObject light2Obj = new GameObject("WorkshopLight2");
        Light light2 = light2Obj.AddComponent<Light>();
        light2.type = LightType.Point;
        light2.range = 10f;
        light2.intensity = 0.6f;
        light2.color = new Color(0.9f, 0.95f, 1f);
        light2Obj.transform.position = new Vector3(0, 3.5f, -3f);

        // Spawn
        GameObject sp = new GameObject("PlayerSpawnPoint");
        sp.transform.position = new Vector3(0, 1f, -4f);
        sp.transform.rotation = Quaternion.LookRotation(Vector3.forward);
        playerSpawnPoint = sp.transform;
    }

    private void CreateTaskStations()
    {
        Material stationMat = CreateMat(new Color(0.4f, 0.35f, 0.3f));

        foreach (var task in tasks)
        {
            // Workbench
            GameObject station = new GameObject(task.name.Replace(" ", ""));
            station.transform.position = task.stationPosition;

            GameObject bench = GameObject.CreatePrimitive(PrimitiveType.Cube);
            bench.name = $"{task.name}_Bench";
            bench.transform.SetParent(station.transform);
            bench.transform.localPosition = new Vector3(0, 0.5f, 0);
            bench.transform.localScale = new Vector3(1.2f, 1f, 0.8f);
            bench.GetComponent<Renderer>().sharedMaterial = stationMat;

            // Task label (small quad on top)
            GameObject label = GameObject.CreatePrimitive(PrimitiveType.Quad);
            label.name = $"{task.name}_Label";
            label.transform.SetParent(station.transform);
            label.transform.localPosition = new Vector3(0, 1.05f, 0);
            label.transform.localScale = new Vector3(0.8f, 0.3f, 1f);
            label.transform.rotation = Quaternion.Euler(90, 0, 0);
            label.GetComponent<Renderer>().sharedMaterial = CreateMat(new Color(0.9f, 0.85f, 0.7f));

            // Progress indicator
            GameObject progress = GameObject.CreatePrimitive(PrimitiveType.Cube);
            progress.name = $"{task.name}_Progress";
            progress.transform.SetParent(station.transform);
            progress.transform.localPosition = new Vector3(0, 1.1f, -0.3f);
            progress.transform.localScale = new Vector3(0.01f, 0.1f, 0.1f);
            progress.GetComponent<Renderer>().sharedMaterial = CreateMat(new Color(0.8f, 0.6f, 0.2f));

            task.stationObject = station;
            task.progressIndicator = progress;
        }
    }

    private void CreateToolRack()
    {
        toolRackPosition = new Vector3(0, 0, -4.5f);

        Material rackMat = CreateMat(new Color(0.35f, 0.25f, 0.2f));

        // Rack frame
        CreateBox("ToolRack_Frame", toolRackPosition + new Vector3(0, 1.5f, 0),
            new Vector3(5f, 2.5f, 0.3f), rackMat);

        // Individual tool slots
        string[] toolNames = { "Shovel", "Hammer", "Saw", "Wrench", "Broom" };
        float startX = -2f;
        float spacing = 1f;

        for (int i = 0; i < toolNames.Length; i++)
        {
            float x = startX + i * spacing;
            Color toolColor = toolColors.ContainsKey(toolNames[i]) ? toolColors[toolNames[i]] : Color.grey;

            // Tool visual on the rack
            GameObject tool = GameObject.CreatePrimitive(PrimitiveType.Cube);
            tool.name = $"Tool_{toolNames[i]}";
            tool.transform.position = toolRackPosition + new Vector3(x, 1.5f, 0.2f);
            tool.transform.localScale = new Vector3(0.15f, 0.8f, 0.15f);
            tool.GetComponent<Renderer>().sharedMaterial = CreateMat(toolColor);

            // Label
            GameObject labelObj = GameObject.CreatePrimitive(PrimitiveType.Quad);
            labelObj.name = $"ToolLabel_{toolNames[i]}";
            labelObj.transform.position = toolRackPosition + new Vector3(x, 0.5f, 0.16f);
            labelObj.transform.localScale = new Vector3(0.6f, 0.2f, 1f);
            labelObj.GetComponent<Renderer>().sharedMaterial = CreateMat(new Color(0.9f, 0.85f, 0.7f));
        }
    }

    // =========================================================================
    // HUD
    // =========================================================================

    private void CreateHUD()
    {
        GameObject canvasObj = new GameObject("ItemDegradationHUD");
        canvasObj.transform.SetParent(transform);
        hudCanvas = canvasObj.AddComponent<Canvas>();
        UIHelper.ConfigureCanvas(hudCanvas, sortingOrder: 15);

        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);

        // Prompt
        promptText = MakeText(canvasObj.transform, "PromptText", "",
            new Vector2(0.2f, 0.4f), new Vector2(0.8f, 0.5f),
            22, Color.white, TextAnchor.MiddleCenter);

        // Tool display
        toolText = MakeText(canvasObj.transform, "ToolText", "No tool equipped",
            new Vector2(0.02f, 0.88f), new Vector2(0.3f, 0.93f),
            18, new Color(0.7f, 0.7f, 0.5f), TextAnchor.MiddleLeft);

        // Task list
        taskListText = MakeText(canvasObj.transform, "TaskList", "",
            new Vector2(0.72f, 0.55f), new Vector2(0.98f, 0.95f),
            14, new Color(0.7f, 0.8f, 0.7f), TextAnchor.UpperLeft);

        // Break message
        breakText = MakeText(canvasObj.transform, "BreakText", "",
            new Vector2(0.15f, 0.55f), new Vector2(0.85f, 0.65f),
            28, new Color(1f, 0.3f, 0.3f, 0f), TextAnchor.MiddleCenter);
        breakText.fontStyle = FontStyle.Bold;
    }

    private Text MakeText(Transform parent, string name, string content,
        Vector2 anchorMin, Vector2 anchorMax, int fontSize, Color color, TextAnchor anchor)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent, false);
        RectTransform rect = obj.AddComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        Text txt = obj.AddComponent<Text>();
        txt.font = UIHelper.GetDefaultFont();
        txt.fontSize = fontSize;
        txt.alignment = anchor;
        txt.color = color;
        txt.text = content;
        txt.raycastTarget = false;
        return txt;
    }

    // =========================================================================
    // Helpers
    // =========================================================================

    private void CreateBox(string name, Vector3 pos, Vector3 scale, Material mat)
    {
        GameObject obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
        obj.name = name;
        obj.transform.position = pos;
        obj.transform.localScale = scale;
        obj.GetComponent<Renderer>().sharedMaterial = mat;
    }

    private Material CreateMat(Color color)
    {
        Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        if (mat.shader == null || mat.shader.name == "Hidden/InternalErrorShader")
            mat = new Material(Shader.Find("Standard"));
        mat.color = color;
        return mat;
    }
}
