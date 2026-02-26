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

    [Header("Hammer Mesh")]
    [SerializeField] private Vector3 hammerHeldPosition = new Vector3(0.5f, -0.35f, 0.8f);
    [SerializeField] private Vector3 hammerHeldRotation = new Vector3(0f, 180f, -30f);
    [SerializeField] private Vector3 hammerHeldScale = new Vector3(1f, 1f, 1f);
    [SerializeField] private float hammerHitDistance = 0.5f;
    [SerializeField] private float hammerHeadLaunchForce = 8f;
    [SerializeField] private float hammerHeadTorque = 10f;

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

    // Real hammer mesh references
    private GameObject hammerSceneObject;
    private Transform hammerHeadTransform;
    private Transform hammerHandleTransform;
    private bool holdingRealHammer = false;
    private bool isSwinging = false;
    private Coroutine activeBreakMessage;

    // Nail
    private Transform nailTransform;
    private Vector3 nailStartPos;
    [SerializeField] private float nailTotalDrop = 0.0763f;

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
        CreateHUD();
        UpdateTaskList();
        InitHammer();
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
            new Task { name = "Dig a Hole", toolName = "Shovel", requiredUses = 5 },
            new Task { name = "Hammer a Nail", toolName = "Hammer", requiredUses = 4 },
            new Task { name = "Saw a Plank", toolName = "Saw", requiredUses = 6 },
            new Task { name = "Turn a Bolt", toolName = "Wrench", requiredUses = 3 },
            new Task { name = "Sweep the Floor", toolName = "Broom", requiredUses = 5 },
        };

        foreach (var task in tasks)
        {
            string stationName = "Station_" + task.toolName;
            GameObject station = GameObject.Find(stationName);
            if (station != null)
            {
                task.stationObject = station;
                task.stationPosition = station.transform.position;
                Debug.Log($"[Level10] Found station '{stationName}'");
            }
            else
            {
                Debug.LogWarning($"[Level10] Station not found: '{stationName}' — create a GameObject with this name in the scene");
            }
        }

        GameObject nailObj = GameObject.Find("Metal Nail");
        if (nailObj != null)
        {
            nailTransform = nailObj.transform;
            nailStartPos = nailTransform.position;
            Debug.Log("[Level10] Found 'Metal Nail'");
        }
        else
        {
            Debug.LogWarning("[Level10] 'Metal Nail' not found in scene");
        }
    }

    private void InitHammer()
    {
        hammerSceneObject = GameObject.Find("hammer");
        if (hammerSceneObject == null)
        {
            Debug.LogWarning("[Level10] Could not find 'hammer' object in scene");
            return;
        }

        hammerHeadTransform = hammerSceneObject.transform.Find("hammerhead");
        hammerHandleTransform = hammerSceneObject.transform.Find("hammerhandle");

        if (hammerHeadTransform == null || hammerHandleTransform == null)
            Debug.LogWarning("[Level10] Hammer children not found (expected 'hammerhead' and 'hammerhandle')");

        foreach (var meshFilter in hammerSceneObject.GetComponentsInChildren<MeshFilter>())
        {
            if (meshFilter.GetComponent<Collider>() == null)
            {
                var mc = meshFilter.gameObject.AddComponent<MeshCollider>();
                mc.convex = true;
            }
        }

        Debug.Log("[Level10] Hammer initialized from scene object");
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

            // Check if looking at the scene hammer
            if (hammerSceneObject != null && !holdingRealHammer &&
                (hit.collider.gameObject == hammerSceneObject ||
                 hit.collider.transform.IsChildOf(hammerSceneObject.transform)))
            {
                showPrompt = true;
                promptText.text = "Press [E] to pick up Hammer";

                if (Input.GetKeyDown(KeyCode.E))
                    PickUpHammer();
            }
            // Check if looking at tool rack
            else if (hitName.Contains("ToolRack") || hitName.Contains("Tool_"))
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
                            promptText.text = holdingRealHammer
                                ? ""
                                : $"Left Click to use {currentToolName} ({currentToolDurability} uses left)";

                            if (Input.GetMouseButtonDown(0) && !isSwinging)
                                StartCoroutine(SwingAndUseTool(i));
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
            if (!holdingRealHammer && !string.IsNullOrEmpty(currentToolName))
                promptText.text = $"Holding: {currentToolName} ({currentToolDurability} uses left)";
            else if (!holdingRealHammer)
                promptText.text = "";
        }

        // Update tool display
        if (toolText != null)
        {
            if (holdingRealHammer)
                toolText.text = "";
            else if (string.IsNullOrEmpty(currentToolName))
                toolText.text = "No tool equipped";
            else
                toolText.text = $"{currentToolName} [{currentToolDurability}/{maxDurability}]";
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
        if (holdingRealHammer)
            DropRealHammer();

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

        if (task.toolName == "Hammer" && nailTransform != null)
        {
            float dropPerSwing = nailTotalDrop / task.requiredUses;
            Vector3 targetPos = nailStartPos + Vector3.down * dropPerSwing * task.currentUses;
            StartCoroutine(PushNailDown(targetPos));
        }

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

        StartBreakMessage(msg);

        if (holdingRealHammer)
        {
            BreakHammer();
        }
        else if (currentToolVisual != null)
        {
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

    private IEnumerator SwingAndUseTool(int taskIndex)
    {
        isSwinging = true;

        Transform swingTarget = holdingRealHammer && hammerSceneObject != null
            ? hammerSceneObject.transform
            : currentToolVisual != null ? currentToolVisual.transform : null;

        if (swingTarget != null)
        {
            Quaternion startRot = swingTarget.localRotation;
            Quaternion windUp = startRot * Quaternion.Euler(0f, 0f, 20f);
            Quaternion swingDown = startRot * Quaternion.Euler(0f, 0f, -60f);

            float windUpTime = 0.1f;
            float swingTime = 0.12f;
            float returnTime = 0.2f;
            float t = 0f;

            bool isHammerTask = tasks[taskIndex].toolName == "Hammer" && nailTransform != null;

            while (t < windUpTime)
            {
                t += Time.deltaTime;
                swingTarget.localRotation = Quaternion.Slerp(startRot, windUp, t / windUpTime);
                yield return null;
            }

            if (!isHammerTask)
                UseTool(taskIndex);

            bool hammerHitNail = false;

            t = 0f;
            while (t < swingTime)
            {
                t += Time.deltaTime;
                swingTarget.localRotation = Quaternion.Slerp(windUp, swingDown, t / swingTime);

                if (isHammerTask && !hammerHitNail && hammerHeadTransform != null)
                {
                    float dist = Vector3.Distance(hammerHeadTransform.position, nailTransform.position);
                    if (dist <= hammerHitDistance)
                        hammerHitNail = true;
                }

                yield return null;
            }

            if (isHammerTask)
            {
                if (hammerHitNail)
                {
                    UseTool(taskIndex);
                    StartBreakMessage("Hit!", new Color(0.3f, 1f, 0.3f, 1f));
                }
                else
                    StartBreakMessage("Swing missed! Try again.");
            }

            t = 0f;
            while (t < returnTime)
            {
                t += Time.deltaTime;
                swingTarget.localRotation = Quaternion.Slerp(swingDown, startRot, t / returnTime);
                yield return null;
            }

            swingTarget.localRotation = startRot;
        }
        else
        {
            UseTool(taskIndex);
        }

        isSwinging = false;
    }

    private IEnumerator PushNailDown(Vector3 targetPos)
    {
        Vector3 from = nailTransform.position;
        float duration = 0.15f;
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            nailTransform.position = Vector3.Lerp(from, targetPos, t / duration);
            yield return null;
        }
        nailTransform.position = targetPos;
    }

    private void StartBreakMessage(string msg)
    {
        StartBreakMessage(msg, new Color(1f, 0.3f, 0.3f, 1f));
    }

    private void StartBreakMessage(string msg, Color color)
    {
        if (activeBreakMessage != null)
            StopCoroutine(activeBreakMessage);
        activeBreakMessage = StartCoroutine(ShowBreakMessage(msg, color));
    }

    private IEnumerator ShowBreakMessage(string msg, Color color)
    {
        if (breakText == null) yield break;

        breakText.text = msg;
        breakText.color = color;

        float fadeTime = 0.75f;
        float elapsed = 0f;
        while (elapsed < fadeTime)
        {
            elapsed += Time.deltaTime;
            breakText.color = new Color(color.r, color.g, color.b, 1f - (elapsed / fadeTime));
            yield return null;
        }
        breakText.text = "";
    }

    // =========================================================================
    // Hammer Mesh Handling
    // =========================================================================

    private void PickUpHammer()
    {
        if (currentToolVisual != null)
            Destroy(currentToolVisual);

        currentToolName = "Hammer";
        currentToolDurability = maxDurability;
        holdingRealHammer = true;

        Camera cam = Camera.main;
        if (cam != null && hammerSceneObject != null)
        {
            hammerSceneObject.transform.SetParent(cam.transform);
            hammerSceneObject.transform.localPosition = hammerHeldPosition;
            hammerSceneObject.transform.localRotation = Quaternion.Euler(hammerHeldRotation);
            hammerSceneObject.transform.localScale = hammerHeldScale;

            foreach (var col in hammerSceneObject.GetComponentsInChildren<Collider>())
                col.enabled = false;
        }

        Task hammerTask = tasks.Find(t => t.toolName == "Hammer");
        Transform stationTransform = hammerTask?.stationObject != null ? hammerTask.stationObject.transform : null;
        StartCoroutine(ShowPromptUntilNearby("Hammer in the nail on the workbench.", stationTransform, interactRange * 0.6f, 1f));
        Debug.Log($"[Level10] Picked up real Hammer with {currentToolDurability} durability");
    }

    private IEnumerator ShowTimedPrompt(string msg, float holdTime, float fadeTime)
    {
        if (promptText == null) yield break;
        promptText.text = msg;
        Color startColor = promptText.color;
        promptText.color = new Color(startColor.r, startColor.g, startColor.b, 1f);

        yield return new WaitForSeconds(holdTime);

        if (promptText.text != msg) yield break;

        float elapsed = 0f;
        while (elapsed < fadeTime)
        {
            elapsed += Time.deltaTime;
            promptText.color = new Color(startColor.r, startColor.g, startColor.b, 1f - (elapsed / fadeTime));
            yield return null;
        }

        if (promptText.text == msg)
            promptText.text = "";
        promptText.color = new Color(startColor.r, startColor.g, startColor.b, 1f);
    }

    private IEnumerator ShowPromptUntilNearby(string msg, Transform target, float range, float fadeTime)
    {
        if (promptText == null) yield break;
        promptText.text = msg;
        Color startColor = promptText.color;
        promptText.color = new Color(startColor.r, startColor.g, startColor.b, 1f);

        List<Renderer> rendererList = new List<Renderer>();
        GameObject workBench = GameObject.Find("Work Bench");
        if (workBench != null) rendererList.AddRange(workBench.GetComponentsInChildren<Renderer>());
        if (target != null) rendererList.AddRange(target.GetComponentsInChildren<Renderer>());
        if (nailTransform != null) rendererList.AddRange(nailTransform.GetComponentsInChildren<Renderer>());
        Renderer[] renderers = rendererList.Count > 0 ? rendererList.ToArray() : null;
        Dictionary<Renderer, Color> originalColors = new Dictionary<Renderer, Color>();
        Color highlightColor = new Color(1f, 0.9f, 0.4f);

        if (renderers != null)
        {
            foreach (var r in renderers)
            {
                originalColors[r] = r.material.color;
                r.material.EnableKeyword("_EMISSION");
            }
        }

        Camera cam = Camera.main;
        while (target != null && cam != null)
        {
            float dist = Vector3.Distance(cam.transform.position, target.position);
            if (dist <= range)
                break;

            if (renderers != null)
            {
                float pulse = (Mathf.Sin(Time.time * 3f) + 1f) * 0.5f;
                Color glow = Color.Lerp(new Color(1f, 0.7f, 0.1f), new Color(1f, 1f, 0.5f), pulse) * (0.3f + pulse * 0.4f);
                foreach (var r in renderers)
                {
                    if (r != null)
                        r.material.SetColor("_EmissionColor", glow);
                }
            }

            yield return null;
        }

        if (renderers != null)
        {
            foreach (var r in renderers)
            {
                if (r == null) continue;
                r.material.SetColor("_EmissionColor", Color.black);
            }
        }

        if (promptText.text != msg) yield break;

        float elapsed = 0f;
        while (elapsed < fadeTime)
        {
            elapsed += Time.deltaTime;
            promptText.color = new Color(startColor.r, startColor.g, startColor.b, 1f - (elapsed / fadeTime));
            yield return null;
        }

        if (promptText.text == msg)
            promptText.text = "";
        promptText.color = new Color(startColor.r, startColor.g, startColor.b, 1f);
    }

    private void BreakHammer()
    {
        holdingRealHammer = false;
        Camera cam = Camera.main;

        if (hammerHeadTransform != null)
        {
            hammerHeadTransform.SetParent(null);

            foreach (var col in hammerHeadTransform.GetComponentsInChildren<Collider>())
                col.enabled = true;
            if (hammerHeadTransform.GetComponent<Collider>() == null)
                hammerHeadTransform.gameObject.AddComponent<BoxCollider>();

            Rigidbody rb = hammerHeadTransform.gameObject.AddComponent<Rigidbody>();

            Vector3 launchDir = cam != null
                ? cam.transform.forward + Vector3.up * 0.5f
                : Vector3.forward + Vector3.up * 0.5f;
            rb.AddForce(launchDir.normalized * hammerHeadLaunchForce, ForceMode.Impulse);
            rb.AddTorque(Random.insideUnitSphere * hammerHeadTorque, ForceMode.Impulse);

            Destroy(hammerHeadTransform.gameObject, 5f);
            hammerHeadTransform = null;
        }

        if (hammerSceneObject != null)
            StartCoroutine(DropHandleAfterDelay(0.8f));
    }

    private IEnumerator DropHandleAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);

        if (hammerSceneObject != null)
        {
            hammerSceneObject.transform.SetParent(null);

            foreach (var col in hammerSceneObject.GetComponentsInChildren<Collider>())
                col.enabled = true;
            if (hammerSceneObject.GetComponent<Collider>() == null)
                hammerSceneObject.AddComponent<BoxCollider>();

            hammerSceneObject.AddComponent<Rigidbody>();

            Destroy(hammerSceneObject, 5f);
            hammerSceneObject = null;
            hammerHandleTransform = null;
        }
    }

    private void DropRealHammer()
    {
        holdingRealHammer = false;

        if (hammerSceneObject != null)
        {
            hammerSceneObject.transform.SetParent(null);

            foreach (var col in hammerSceneObject.GetComponentsInChildren<Collider>())
                col.enabled = true;

            hammerSceneObject.AddComponent<Rigidbody>();

            Destroy(hammerSceneObject, 5f);
            hammerSceneObject = null;
            hammerHeadTransform = null;
            hammerHandleTransform = null;
        }
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
