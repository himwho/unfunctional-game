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

    [Header("Saw Mesh")]
    [SerializeField] private Vector3 sawHeldPosition = new Vector3(0.5f, -0.35f, 0.8f);
    [SerializeField] private Vector3 sawHeldRotation = new Vector3(0f, 180f, -30f);
    [SerializeField] private Vector3 sawHeldScale = new Vector3(1f, 1f, 1f);
    [SerializeField] private float sawAlignDistance = 0.5f;
    [SerializeField] private float sawBladeLaunchForce = 8f;
    [SerializeField] private float sawBladeTorque = 10f;

    [Header("Broom Mesh")]
    [SerializeField] private Vector3 broomHeldPosition = new Vector3(0.5f, -0.35f, 0.8f);
    [SerializeField] private Vector3 broomHeldRotation = new Vector3(0f, 180f, -30f);
    [SerializeField] private Vector3 broomHeldScale = new Vector3(1f, 1f, 1f);
    [SerializeField] private float broomHeadLaunchForce = 8f;
    [SerializeField] private float broomHeadTorque = 10f;
    [SerializeField, Range(0f, 1f)] private float broomPitchFollowFactor = 0.3f;
    [SerializeField] private float sweepAngle = 30f;
    [SerializeField] private float sweepStrokeTime = 0.2f;
    [SerializeField] private int sweepStrokes = 4;

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
    private class HammerInfo
    {
        public GameObject gameObject;
        public Transform headTransform;
        public Transform handleTransform;
    }

    private List<HammerInfo> availableHammers = new List<HammerInfo>();
    private GameObject hammerSceneObject;
    private Transform hammerHeadTransform;
    private Transform hammerHandleTransform;
    private bool holdingRealHammer = false;

    // Real saw mesh references
    private class SawInfo
    {
        public GameObject gameObject;
        public Transform bladeTransform;
        public Transform handleTransform;
    }

    private List<SawInfo> availableSaws = new List<SawInfo>();
    private GameObject sawSceneObject;
    private Transform sawBladeTransform;
    private Transform sawHandleTransform;
    private bool holdingRealSaw = false;

    // Real broom mesh references
    private class BroomInfo
    {
        public GameObject gameObject;
        public Transform headTransform;
        public Transform handleTransform;
    }

    private List<BroomInfo> availableBrooms = new List<BroomInfo>();
    private GameObject broomSceneObject;
    private Transform broomHeadTransform;
    private Transform broomHandleTransform;
    private bool holdingRealBroom = false;

    private bool isSwinging = false;
    private Coroutine activeBreakMessage;

    // Nail
    private Transform nailTransform;
    private Vector3 nailStartPos;
    [SerializeField] private float nailTotalDrop = 0.0763f;

    // Wood Plank (two child halves under "Wood Plank")
    private Transform plankTransform;
    private Transform plankChildA;
    private Transform plankChildB;
    private bool plankSplit = false;
    [SerializeField] private float plankSplitForce = 2f;
    [SerializeField] private float sawingAmplitude = 0.08f;

    [Header("Saw Cut Line")]
    [SerializeField] private Vector3 sawCutLineOffset = Vector3.zero;
    [SerializeField] private Vector3 sawCutLineRotation = Vector3.zero;
    [SerializeField] private Vector3 sawCutLineScale = new Vector3(0.3f, 0.05f, 1f);
    [SerializeField] private Color sawCutLineColor = new Color(1f, 0.7f, 0.1f);
    [SerializeField] private float sawCutLineGlowIntensity = 2f;
    [SerializeField] private float sawCutLinePulseSpeed = 3f;

    private GameObject sawCutLine;
    private Material sawCutLineMaterial;
    private float sawCutProgress = 0f;

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
        InitSaw();
        InitBroom();
        InitPlank();
    }

    private void Update()
    {
        if (levelComplete) return;

        UpdateInteraction();
        UpdateSawGuideGlow();
        UpdateBroomPitchDamp();
    }

    private void UpdateSawGuideGlow()
    {
        if (sawCutLineMaterial == null || sawCutLine == null) return;

        float pulse = (Mathf.Sin(Time.time * sawCutLinePulseSpeed) + 1f) * 0.5f;
        Color baseColor = Color.Lerp(sawCutLineColor, Color.red, sawCutProgress);
        Color dimmed = baseColor * 0.6f;
        Color glow = Color.Lerp(dimmed, baseColor, pulse);
        sawCutLineMaterial.color = glow;
        sawCutLineMaterial.SetColor("_EmissionColor", glow * (sawCutLineGlowIntensity * (0.7f + pulse * 0.3f)));
    }

    private void UpdateBroomPitchDamp()
    {
        if (!holdingRealBroom || broomSceneObject == null || isSwinging) return;

        Camera cam = Camera.main;
        if (cam == null) return;

        float cameraPitch = cam.transform.eulerAngles.x;
        if (cameraPitch > 180f) cameraPitch -= 360f;

        float pitchCorrection = -cameraPitch * (1f - broomPitchFollowFactor);
        Quaternion heldRot = Quaternion.Euler(broomHeldRotation);
        broomSceneObject.transform.localRotation = Quaternion.Euler(pitchCorrection, 0f, 0f) * heldRot;
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
            new Task { name = "Hammer a Nail", toolName = "Hammer", requiredUses = 8 },
            new Task { name = "Saw a Plank", toolName = "Saw", requiredUses = 8 },
            new Task { name = "Turn a Bolt", toolName = "Wrench", requiredUses = 3 },
            new Task { name = "Sweep the Floor", toolName = "Broom", requiredUses = 6 },
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
        string[] hammerNames = { "hammer", "hammer (1)", "hammer (2)" };

        foreach (string hammerName in hammerNames)
        {
            GameObject hammerObj = GameObject.Find(hammerName);
            if (hammerObj == null)
            {
                Debug.LogWarning($"[Level10] Could not find '{hammerName}' object in scene");
                continue;
            }

            var info = new HammerInfo
            {
                gameObject = hammerObj,
                headTransform = hammerObj.transform.Find("hammerhead"),
                handleTransform = hammerObj.transform.Find("hammerhandle")
            };

            if (info.headTransform == null || info.handleTransform == null)
                Debug.LogWarning($"[Level10] Hammer '{hammerName}' children not found (expected 'hammerhead' and 'hammerhandle')");

            foreach (var meshFilter in hammerObj.GetComponentsInChildren<MeshFilter>())
            {
                if (meshFilter.GetComponent<Collider>() == null)
                {
                    var mc = meshFilter.gameObject.AddComponent<MeshCollider>();
                    mc.convex = true;
                    mc.isTrigger = true;
                }
                else
                {
                    meshFilter.GetComponent<Collider>().isTrigger = true;
                }
            }

            availableHammers.Add(info);
            Debug.Log($"[Level10] Hammer '{hammerName}' initialized from scene object");
        }

        if (availableHammers.Count == 0)
            Debug.LogWarning("[Level10] No hammers found in scene");
    }

    private void InitSaw()
    {
        string[] sawNames = { "saw", "saw (1)", "saw (2)" };

        foreach (string sawName in sawNames)
        {
            GameObject sawObj = GameObject.Find(sawName);
            if (sawObj == null)
            {
                Debug.LogWarning($"[Level10] Could not find '{sawName}' object in scene");
                continue;
            }

            var info = new SawInfo
            {
                gameObject = sawObj,
                bladeTransform = sawObj.transform.Find("sawblade"),
                handleTransform = sawObj.transform.Find("sawhandle")
            };

            if (info.bladeTransform == null || info.handleTransform == null)
                Debug.LogWarning($"[Level10] Saw '{sawName}' children not found (expected 'sawblade' and 'sawhandle')");

            foreach (var meshFilter in sawObj.GetComponentsInChildren<MeshFilter>())
            {
                if (meshFilter.GetComponent<Collider>() == null)
                {
                    var mc = meshFilter.gameObject.AddComponent<MeshCollider>();
                    mc.convex = true;
                    mc.isTrigger = true;
                }
                else
                {
                    meshFilter.GetComponent<Collider>().isTrigger = true;
                }
            }

            availableSaws.Add(info);
            Debug.Log($"[Level10] Saw '{sawName}' initialized from scene object");
        }

        if (availableSaws.Count == 0)
            Debug.LogWarning("[Level10] No saws found in scene");
    }

    private void InitBroom()
    {
        string[] broomNames = { "broomstick", "broomstick (1)" };

        foreach (string broomName in broomNames)
        {
            GameObject broomObj = GameObject.Find(broomName);
            if (broomObj == null)
            {
                Debug.LogWarning($"[Level10] Could not find '{broomName}' object in scene");
                continue;
            }

            var info = new BroomInfo
            {
                gameObject = broomObj,
                headTransform = broomObj.transform.Find("broomhead"),
                handleTransform = broomObj.transform.Find("broomhandle")
            };

            if (info.headTransform == null || info.handleTransform == null)
                Debug.LogWarning($"[Level10] Broom '{broomName}' children not found (expected 'broomhead' and 'broomhandle')");

            foreach (var meshFilter in broomObj.GetComponentsInChildren<MeshFilter>())
            {
                if (meshFilter.GetComponent<Collider>() == null)
                {
                    var mc = meshFilter.gameObject.AddComponent<MeshCollider>();
                    mc.convex = true;
                    mc.isTrigger = true;
                }
                else
                {
                    meshFilter.GetComponent<Collider>().isTrigger = true;
                }
            }

            availableBrooms.Add(info);
            Debug.Log($"[Level10] Broom '{broomName}' initialized from scene object");
        }

        if (availableBrooms.Count == 0)
            Debug.LogWarning("[Level10] No brooms found in scene");
    }

    private void InitPlank()
    {
        GameObject plankObj = GameObject.Find("Wood Plank");
        if (plankObj == null)
        {
            Debug.LogWarning("[Level10] 'Wood Plank' not found in scene");
            return;
        }

        plankTransform = plankObj.transform;

        if (plankTransform.childCount >= 2)
        {
            plankChildA = plankTransform.GetChild(0);
            plankChildB = plankTransform.GetChild(1);
            Debug.Log($"[Level10] Found 'Wood Plank' with children '{plankChildA.name}' and '{plankChildB.name}'");
        }
        else
        {
            Debug.LogWarning("[Level10] 'Wood Plank' needs at least 2 child objects (the two halves)");
        }

        CreateSawCutLine();
    }

    private void CreateSawCutLine()
    {
        if (plankChildA == null || plankChildB == null) return;

        Vector3 seamCenter = (plankChildA.position + plankChildB.position) * 0.5f;

        sawCutLine = GameObject.CreatePrimitive(PrimitiveType.Cube);
        sawCutLine.name = "SawGuide";
        Destroy(sawCutLine.GetComponent<Collider>());

        sawCutLine.transform.SetParent(plankTransform);
        sawCutLine.transform.position = seamCenter + plankTransform.TransformDirection(sawCutLineOffset);
        sawCutLine.transform.rotation = plankTransform.rotation * Quaternion.Euler(sawCutLineRotation);
        sawCutLine.transform.localScale = sawCutLineScale;

        var renderer = sawCutLine.GetComponent<Renderer>();
        Material mat = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
        if (mat.shader == null || mat.shader.name == "Hidden/InternalErrorShader")
            mat = new Material(Shader.Find("Unlit/Color"));
        mat.color = sawCutLineColor;
        mat.EnableKeyword("_EMISSION");
        mat.SetColor("_EmissionColor", sawCutLineColor * sawCutLineGlowIntensity);
        renderer.material = mat;
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

        sawCutLineMaterial = mat;

        sawCutLine.SetActive(false);
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

            // Check if looking at any available scene hammer
            HammerInfo hitHammer = null;
            if (!holdingRealHammer)
            {
                foreach (var h in availableHammers)
                {
                    if (h.gameObject != null &&
                        (hit.collider.gameObject == h.gameObject ||
                         hit.collider.transform.IsChildOf(h.gameObject.transform)))
                    {
                        hitHammer = h;
                        break;
                    }
                }
            }

            // Check if looking at any available scene saw
            SawInfo hitSaw = null;
            if (!holdingRealSaw)
            {
                foreach (var s in availableSaws)
                {
                    if (s.gameObject != null &&
                        (hit.collider.gameObject == s.gameObject ||
                         hit.collider.transform.IsChildOf(s.gameObject.transform)))
                    {
                        hitSaw = s;
                        break;
                    }
                }
            }

            // Check if looking at any available scene broom
            BroomInfo hitBroom = null;
            if (!holdingRealBroom)
            {
                foreach (var b in availableBrooms)
                {
                    if (b.gameObject != null &&
                        (hit.collider.gameObject == b.gameObject ||
                         hit.collider.transform.IsChildOf(b.gameObject.transform)))
                    {
                        hitBroom = b;
                        break;
                    }
                }
            }

            if (hitHammer != null)
            {
                showPrompt = true;
                promptText.text = "Press [E] to pick up Hammer";

                if (Input.GetKeyDown(KeyCode.E))
                    PickUpHammer(hitHammer);
            }
            else if (hitSaw != null)
            {
                showPrompt = true;
                promptText.text = "Press [E] to pick up Saw";

                if (Input.GetKeyDown(KeyCode.E))
                    PickUpSaw(hitSaw);
            }
            else if (hitBroom != null)
            {
                showPrompt = true;
                promptText.text = "Press [E] to pick up Broom";

                if (Input.GetKeyDown(KeyCode.E))
                    PickUpBroom(hitBroom);
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
                    bool hitStation = tasks[i].stationObject != null &&
                        (hit.collider.gameObject == tasks[i].stationObject ||
                         hit.collider.transform.IsChildOf(tasks[i].stationObject.transform));

                    if (!hitStation && tasks[i].toolName == "Saw" && plankTransform != null)
                    {
                        hitStation = hit.collider.gameObject == plankTransform.gameObject ||
                                     hit.collider.transform.IsChildOf(plankTransform);
                    }

                    if (hitStation)
                    {
                        showPrompt = true;

                        bool isHammerStation = tasks[i].toolName == "Hammer";
                        bool isSawStation = tasks[i].toolName == "Saw";
                        bool isBroomStation = tasks[i].toolName == "Broom";
                        bool isRealToolStation = isHammerStation || isSawStation || isBroomStation;

                        if (tasks[i].completed)
                        {
                            if (!isRealToolStation)
                                promptText.text = $"{tasks[i].name} - DONE";
                        }
                        else                         if (currentToolName == tasks[i].toolName)
                        {
                            if (holdingRealHammer || holdingRealSaw || holdingRealBroom)
                                promptText.text = "";
                            else
                                promptText.text = $"Left Click to use {currentToolName} ({currentToolDurability} uses left)";

                            if (Input.GetMouseButtonDown(0) && !isSwinging)
                                StartCoroutine(SwingAndUseTool(i));
                        }
                        else if (string.IsNullOrEmpty(currentToolName))
                        {
                            if (!isRealToolStation)
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

        // Broom can sweep anywhere with left click
        if (holdingRealBroom && !isSwinging && Input.GetMouseButtonDown(0))
        {
            int broomTaskIndex = tasks.FindIndex(t => t.toolName == "Broom");
            if (broomTaskIndex >= 0 && !tasks[broomTaskIndex].completed)
                StartCoroutine(SwingAndUseTool(broomTaskIndex));
        }

        bool holdingRealTool = holdingRealHammer || holdingRealSaw || holdingRealBroom;

        if (!showPrompt)
        {
            if (!holdingRealTool && !string.IsNullOrEmpty(currentToolName))
                promptText.text = $"Holding: {currentToolName} ({currentToolDurability} uses left)";
            else if (!holdingRealTool)
                promptText.text = "";
        }

        // Update tool display
        if (toolText != null)
        {
            if (holdingRealTool)
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
        if (holdingRealSaw)
            DropRealSaw();
        if (holdingRealBroom)
            DropRealBroom();

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

        if (task.toolName == "Saw" && sawCutLine != null && !plankSplit)
        {
            UpdateSawCutProgress(task);
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

            if (holdingRealHammer && task.toolName == "Hammer")
            {
                DropRealHammer();
                currentToolName = "";
                currentToolDurability = 0;
            }
            else if (holdingRealSaw && task.toolName == "Saw")
            {
                DropRealSaw();
                currentToolName = "";
                currentToolDurability = 0;
            }
            else if (holdingRealBroom && task.toolName == "Broom")
            {
                DropRealBroom();
                currentToolName = "";
                currentToolDurability = 0;
            }

            if (task.toolName == "Saw" && !plankSplit)
            {
                SplitPlank();
            }

            // Visual feedback on station
            if (task.progressIndicator != null)
            {
                task.progressIndicator.GetComponent<Renderer>().material.color = Color.green;
            }

            StartBreakMessage($"'{task.name}' complete!", new Color(0.3f, 1f, 0.3f, 1f), 3f);
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
        else if (holdingRealSaw)
        {
            BreakSaw();
        }
        else if (holdingRealBroom)
        {
            BreakBroom();
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

        bool isSawTask = tasks[taskIndex].toolName == "Saw" && plankTransform != null;
        bool isBroomTask = tasks[taskIndex].toolName == "Broom";

        if (isSawTask)
        {
            yield return StartCoroutine(SawAndUseTool(taskIndex));
            isSwinging = false;
            yield break;
        }

        if (isBroomTask)
        {
            yield return StartCoroutine(SweepAndUseTool(taskIndex));
            isSwinging = false;
            yield break;
        }

        Transform swingTarget = null;
        if (holdingRealHammer && hammerSceneObject != null)
            swingTarget = hammerSceneObject.transform;
        else if (holdingRealSaw && sawSceneObject != null)
            swingTarget = sawSceneObject.transform;
        else if (holdingRealBroom && broomSceneObject != null)
            swingTarget = broomSceneObject.transform;
        else if (currentToolVisual != null)
            swingTarget = currentToolVisual.transform;

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
                    int durabilityBefore = currentToolDurability;
                    UseTool(taskIndex);
                    bool toolBroke = durabilityBefore > 0 && currentToolDurability <= 0;
                    if (!toolBroke && !tasks[taskIndex].completed)
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

    private IEnumerator SawAndUseTool(int taskIndex)
    {
        Transform swingTarget = null;
        if (holdingRealSaw && sawSceneObject != null)
            swingTarget = sawSceneObject.transform;
        else if (currentToolVisual != null)
            swingTarget = currentToolVisual.transform;

        if (swingTarget == null)
        {
            UseTool(taskIndex);
            yield break;
        }

        Vector3 startPos = swingTarget.localPosition;
        Quaternion startRot = swingTarget.localRotation;

        // Tilt saw slightly forward to meet the plank
        Quaternion sawAngle = startRot * Quaternion.Euler(15f, 0f, 0f);
        float tiltTime = 0.1f;
        float t = 0f;
        while (t < tiltTime)
        {
            t += Time.deltaTime;
            swingTarget.localRotation = Quaternion.Slerp(startRot, sawAngle, t / tiltTime);
            yield return null;
        }

        // Back-and-forth sawing strokes, checking alignment during each stroke
        int strokes = 4;
        float strokeTime = 0.15f;
        bool sawHitCutLine = false;

        Transform bladetip = sawBladeTransform != null ? sawBladeTransform : swingTarget;

        for (int i = 0; i < strokes; i++)
        {
            float target = (i % 2 == 0) ? sawingAmplitude : -sawingAmplitude;
            float elapsed = 0f;
            Vector3 from = swingTarget.localPosition;
            Vector3 to = startPos + swingTarget.localRotation * new Vector3(0f, 0f, target);

            while (elapsed < strokeTime)
            {
                elapsed += Time.deltaTime;
                swingTarget.localPosition = Vector3.Lerp(from, to, elapsed / strokeTime);

                if (!sawHitCutLine && sawCutLine != null)
                {
                    float dist = Vector3.Distance(bladetip.position, sawCutLine.transform.position);
                    if (dist <= sawAlignDistance)
                        sawHitCutLine = true;
                }

                yield return null;
            }
        }

        if (sawHitCutLine)
        {
            int durabilityBefore = currentToolDurability;
            UseTool(taskIndex);
            bool toolBroke = durabilityBefore > 0 && currentToolDurability <= 0;

            if (!toolBroke && !tasks[taskIndex].completed)
                StartBreakMessage("Sawing...", new Color(1f, 0.85f, 0.4f, 1f));
        }
        else
        {
            StartBreakMessage("Line up the saw with the cut line!", new Color(1f, 1f, 0.3f, 1f));
        }

        // Return to rest position
        t = 0f;
        float returnTime = 0.15f;
        Vector3 currentPos = swingTarget.localPosition;
        Quaternion currentRot = swingTarget.localRotation;
        while (t < returnTime)
        {
            t += Time.deltaTime;
            float frac = t / returnTime;
            swingTarget.localPosition = Vector3.Lerp(currentPos, startPos, frac);
            swingTarget.localRotation = Quaternion.Slerp(currentRot, startRot, frac);
            yield return null;
        }
        swingTarget.localPosition = startPos;
        swingTarget.localRotation = startRot;
    }

    private IEnumerator SweepAndUseTool(int taskIndex)
    {
        Transform swingTarget = null;
        if (holdingRealBroom && broomSceneObject != null)
            swingTarget = broomSceneObject.transform;
        else if (currentToolVisual != null)
            swingTarget = currentToolVisual.transform;

        if (swingTarget == null)
        {
            UseTool(taskIndex);
            yield break;
        }

        Quaternion startRot = swingTarget.localRotation;

        for (int i = 0; i < sweepStrokes; i++)
        {
            float angle = (i % 2 == 0) ? sweepAngle : -sweepAngle;
            float elapsed = 0f;
            Quaternion from = swingTarget.localRotation;
            Quaternion to = startRot * Quaternion.Euler(angle, 0f, 0f);

            while (elapsed < sweepStrokeTime)
            {
                elapsed += Time.deltaTime;
                swingTarget.localRotation = Quaternion.Slerp(from, to, elapsed / sweepStrokeTime);
                yield return null;
            }
        }

        bool nearStation = false;
        Camera sweepCam = Camera.main;
        Task broomTask = tasks[taskIndex];
        if (sweepCam != null && broomTask.stationObject != null)
        {
            float dist = Vector3.Distance(sweepCam.transform.position, broomTask.stationObject.transform.position);
            nearStation = dist <= interactRange;
        }

        if (nearStation)
        {
            int durabilityBefore = currentToolDurability;
            UseTool(taskIndex);
            bool toolBroke = durabilityBefore > 0 && currentToolDurability <= 0;

            if (!toolBroke && !broomTask.completed)
                StartBreakMessage("Sweeping...", new Color(0.6f, 0.9f, 0.5f, 1f));
        }

        float returnTime = 0.15f;
        float t = 0f;
        Quaternion currentRot = swingTarget != null ? swingTarget.localRotation : startRot;
        while (t < returnTime && swingTarget != null)
        {
            t += Time.deltaTime;
            swingTarget.localRotation = Quaternion.Slerp(currentRot, startRot, t / returnTime);
            yield return null;
        }
        if (swingTarget != null)
            swingTarget.localRotation = startRot;
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

    private void StartBreakMessage(string msg, Color color, float holdTime)
    {
        if (activeBreakMessage != null)
            StopCoroutine(activeBreakMessage);
        activeBreakMessage = StartCoroutine(ShowBreakMessage(msg, color, holdTime));
    }

    private IEnumerator ShowBreakMessage(string msg, Color color, float holdTime = 0f)
    {
        if (breakText == null) yield break;

        breakText.text = msg;
        breakText.color = color;

        if (holdTime > 0f)
            yield return new WaitForSeconds(holdTime);

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

    private void PickUpHammer(HammerInfo info)
    {
        if (holdingRealSaw)
            DropRealSaw();
        if (currentToolVisual != null)
            Destroy(currentToolVisual);

        availableHammers.Remove(info);
        hammerSceneObject = info.gameObject;
        hammerHeadTransform = info.headTransform;
        hammerHandleTransform = info.handleTransform;

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
        if (plankTransform != null)
        {
            Renderer sawGuideRenderer = sawCutLine != null ? sawCutLine.GetComponent<Renderer>() : null;
            foreach (var r in plankTransform.GetComponentsInChildren<Renderer>())
            {
                if (r != sawGuideRenderer)
                    rendererList.Add(r);
            }
        }
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

    private void IgnorePlayerCollision(GameObject obj)
    {
        if (obj == null) return;
        Collider[] playerColliders = null;
        Camera cam = Camera.main;
        if (cam != null)
        {
            Transform root = cam.transform.root;
            playerColliders = root.GetComponentsInChildren<Collider>();
        }
        if (playerColliders == null || playerColliders.Length == 0) return;

        foreach (var hammerCol in obj.GetComponentsInChildren<Collider>())
            foreach (var playerCol in playerColliders)
                Physics.IgnoreCollision(hammerCol, playerCol, true);
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

            IgnorePlayerCollision(hammerHeadTransform.gameObject);

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

            IgnorePlayerCollision(hammerSceneObject);

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

            IgnorePlayerCollision(hammerSceneObject);

            hammerSceneObject.AddComponent<Rigidbody>();

            Destroy(hammerSceneObject, 5f);
            hammerSceneObject = null;
            hammerHeadTransform = null;
            hammerHandleTransform = null;
        }
    }

    // =========================================================================
    // Saw Mesh Handling
    // =========================================================================

    private void PickUpSaw(SawInfo info)
    {
        if (holdingRealHammer)
            DropRealHammer();
        if (currentToolVisual != null)
            Destroy(currentToolVisual);

        availableSaws.Remove(info);
        sawSceneObject = info.gameObject;
        sawBladeTransform = info.bladeTransform;
        sawHandleTransform = info.handleTransform;

        currentToolName = "Saw";
        currentToolDurability = maxDurability;
        holdingRealSaw = true;

        if (sawCutLine != null && !plankSplit)
            sawCutLine.SetActive(true);

        Camera cam = Camera.main;
        if (cam != null && sawSceneObject != null)
        {
            sawSceneObject.transform.SetParent(cam.transform);
            sawSceneObject.transform.localPosition = sawHeldPosition;
            sawSceneObject.transform.localRotation = Quaternion.Euler(sawHeldRotation);
            sawSceneObject.transform.localScale = sawHeldScale;

            foreach (var col in sawSceneObject.GetComponentsInChildren<Collider>())
                col.enabled = false;
        }

        Task sawTask = tasks.Find(t => t.toolName == "Saw");
        Transform stationTransform = sawTask?.stationObject != null ? sawTask.stationObject.transform : null;
        StartCoroutine(ShowPromptUntilNearby("Saw the plank at the workbench.", stationTransform, interactRange * 0.6f, 1f));
        Debug.Log($"[Level10] Picked up real Saw with {currentToolDurability} durability");
    }

    private void BreakSaw()
    {
        holdingRealSaw = false;
        if (sawCutLine != null)
            sawCutLine.SetActive(false);
        Camera cam = Camera.main;

        if (sawBladeTransform != null)
        {
            sawBladeTransform.SetParent(null);

            foreach (var col in sawBladeTransform.GetComponentsInChildren<Collider>())
                col.enabled = true;
            if (sawBladeTransform.GetComponent<Collider>() == null)
                sawBladeTransform.gameObject.AddComponent<BoxCollider>();

            IgnorePlayerCollision(sawBladeTransform.gameObject);

            Rigidbody rb = sawBladeTransform.gameObject.AddComponent<Rigidbody>();

            Vector3 launchDir = cam != null
                ? cam.transform.forward + Vector3.up * 0.5f
                : Vector3.forward + Vector3.up * 0.5f;
            rb.AddForce(launchDir.normalized * sawBladeLaunchForce, ForceMode.Impulse);
            rb.AddTorque(Random.insideUnitSphere * sawBladeTorque, ForceMode.Impulse);

            Destroy(sawBladeTransform.gameObject, 5f);
            sawBladeTransform = null;
        }

        if (sawSceneObject != null)
            StartCoroutine(DropSawHandleAfterDelay(0.8f));
    }

    private IEnumerator DropSawHandleAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);

        if (sawSceneObject != null)
        {
            sawSceneObject.transform.SetParent(null);

            foreach (var col in sawSceneObject.GetComponentsInChildren<Collider>())
                col.enabled = true;
            if (sawSceneObject.GetComponent<Collider>() == null)
                sawSceneObject.AddComponent<BoxCollider>();

            IgnorePlayerCollision(sawSceneObject);

            sawSceneObject.AddComponent<Rigidbody>();

            Destroy(sawSceneObject, 5f);
            sawSceneObject = null;
            sawHandleTransform = null;
        }
    }

    private void DropRealSaw()
    {
        holdingRealSaw = false;
        if (sawCutLine != null)
            sawCutLine.SetActive(false);

        if (sawSceneObject != null)
        {
            sawSceneObject.transform.SetParent(null);

            foreach (var col in sawSceneObject.GetComponentsInChildren<Collider>())
                col.enabled = true;

            IgnorePlayerCollision(sawSceneObject);

            sawSceneObject.AddComponent<Rigidbody>();

            Destroy(sawSceneObject, 5f);
            sawSceneObject = null;
            sawBladeTransform = null;
            sawHandleTransform = null;
        }
    }

    // =========================================================================
    // Broom Mesh Handling
    // =========================================================================

    private void PickUpBroom(BroomInfo info)
    {
        if (holdingRealHammer)
            DropRealHammer();
        if (holdingRealSaw)
            DropRealSaw();
        if (currentToolVisual != null)
            Destroy(currentToolVisual);

        availableBrooms.Remove(info);
        broomSceneObject = info.gameObject;
        broomHeadTransform = info.headTransform;
        broomHandleTransform = info.handleTransform;

        currentToolName = "Broom";
        currentToolDurability = maxDurability;
        holdingRealBroom = true;

        Camera cam = Camera.main;
        if (cam != null && broomSceneObject != null)
        {
            broomSceneObject.transform.SetParent(cam.transform);
            broomSceneObject.transform.localPosition = broomHeldPosition;
            broomSceneObject.transform.localRotation = Quaternion.Euler(broomHeldRotation);
            broomSceneObject.transform.localScale = broomHeldScale;

            foreach (var col in broomSceneObject.GetComponentsInChildren<Collider>())
                col.enabled = false;
        }

        Task broomTask = tasks.Find(t => t.toolName == "Broom");
        Transform stationTransform = broomTask?.stationObject != null ? broomTask.stationObject.transform : null;
        StartCoroutine(ShowPromptUntilNearby("Sweep the floor at the station.", stationTransform, interactRange * 0.6f, 1f));
        Debug.Log($"[Level10] Picked up real Broom with {currentToolDurability} durability");
    }

    private void BreakBroom()
    {
        holdingRealBroom = false;

        if (broomSceneObject == null) return;

        Camera cam = Camera.main;
        Vector3 baseDir = cam != null
            ? cam.transform.forward + Vector3.up * 0.5f
            : Vector3.forward + Vector3.up * 0.5f;

        broomSceneObject.transform.SetParent(null);

        List<Transform> children = new List<Transform>();
        foreach (Transform child in broomSceneObject.transform)
            children.Add(child);

        foreach (Transform child in children)
        {
            child.SetParent(null);

            foreach (var col in child.GetComponentsInChildren<Collider>())
            {
                col.enabled = true;
                col.isTrigger = false;
            }
            if (child.GetComponentsInChildren<Collider>().Length == 0)
                child.gameObject.AddComponent<BoxCollider>();

            IgnorePlayerCollision(child.gameObject);

            Rigidbody rb = child.gameObject.AddComponent<Rigidbody>();
            rb.AddForce(Random.insideUnitSphere * 0.5f, ForceMode.Impulse);
            rb.AddTorque(Random.insideUnitSphere * 2f, ForceMode.Impulse);

            Destroy(child.gameObject, 5f);
        }

        Destroy(broomSceneObject);
        broomSceneObject = null;
        broomHeadTransform = null;
        broomHandleTransform = null;
    }

    private void DropRealBroom()
    {
        holdingRealBroom = false;

        if (broomSceneObject != null)
        {
            broomSceneObject.transform.SetParent(null);

            foreach (var col in broomSceneObject.GetComponentsInChildren<Collider>())
                col.enabled = true;

            IgnorePlayerCollision(broomSceneObject);

            broomSceneObject.AddComponent<Rigidbody>();

            Destroy(broomSceneObject, 5f);
            broomSceneObject = null;
            broomHeadTransform = null;
            broomHandleTransform = null;
        }
    }

    // =========================================================================
    // Wood Plank / Sawing
    // =========================================================================

    private void UpdateSawCutProgress(Task task)
    {
        if (plankChildA == null || plankChildB == null) return;

        float progress = (float)task.currentUses / task.requiredUses;
        sawCutProgress = progress;

        Vector3 dirAway = (plankChildA.position - plankChildB.position).normalized;
        if (dirAway.sqrMagnitude < 0.001f)
            dirAway = plankTransform != null ? plankTransform.right : Vector3.right;

        float separation = progress * 0.003f;
        plankChildA.localPosition += dirAway * separation;
        plankChildB.localPosition -= dirAway * separation;
    }

    private void SplitPlank()
    {
        if (plankSplit) return;
        plankSplit = true;

        Debug.Log("[Level10] Plank split!");

        if (sawCutLine != null)
            Destroy(sawCutLine);
        sawCutLineMaterial = null;

        if (plankChildA != null)
            StartCoroutine(ReleasePlankHalf(plankChildA, 1f));
        if (plankChildB != null)
            StartCoroutine(ReleasePlankHalf(plankChildB, -1f));
    }

    private IEnumerator ReleasePlankHalf(Transform half, float sideSign)
    {
        half.SetParent(null);

        foreach (var existingCollider in half.GetComponentsInChildren<Collider>())
        {
            existingCollider.isTrigger = false;
            existingCollider.enabled = true;
        }

        bool hasCollider = half.GetComponentsInChildren<Collider>().Length > 0;
        if (!hasCollider)
        {
            foreach (var mf in half.GetComponentsInChildren<MeshFilter>())
            {
                if (mf.sharedMesh != null)
                {
                    var mc = mf.gameObject.AddComponent<MeshCollider>();
                    mc.convex = true;
                }
            }

            if (half.GetComponentsInChildren<Collider>().Length == 0)
                half.gameObject.AddComponent<BoxCollider>();
        }

        IgnorePlayerCollision(half.gameObject);

        Rigidbody rb = half.gameObject.AddComponent<Rigidbody>();
        rb.mass = 2f;

        yield return new WaitForFixedUpdate();

        Vector3 pushDir = Vector3.zero;
        if (plankTransform != null)
            pushDir = plankTransform.right * sideSign;
        else
            pushDir = Vector3.right * sideSign;

        rb.AddForce((pushDir + Vector3.down * 0.3f) * plankSplitForce, ForceMode.Impulse);
        rb.AddTorque(Random.insideUnitSphere * 1.5f, ForceMode.Impulse);
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
