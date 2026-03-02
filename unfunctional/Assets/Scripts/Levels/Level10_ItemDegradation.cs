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
    public int maxDurability = 15;          // Uses before tool breaks
    public float interactRange = 3.5f;
    [Tooltip("Radius of the interaction sphere cast — larger = easier to target tools")]
    public float interactRadius = 0.15f;

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
    [SerializeField] private float sweepPushRadius = 0.5f;
    [SerializeField] private float sweepPushForce = 15f;

    [Header("Wrench Mesh")]
    [SerializeField] private Vector3 wrenchHeldPosition = new Vector3(0.5f, -0.35f, 0.8f);
    [SerializeField] private Vector3 wrenchHeldRotation = new Vector3(0f, 180f, -30f);
    [SerializeField] private Vector3 wrenchHeldScale = new Vector3(1f, 1f, 1f);
    [SerializeField] private float wrenchTipLaunchForce = 8f;
    [SerializeField] private float wrenchTipTorque = 10f;

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

    // Inventory system — 4 dynamic slots filled in pickup order
    private class InventorySlot
    {
        public string toolName;        // null/empty = slot is free
        public bool hasItem;
        public int durability;
        public int maxDurability;
        public GameObject sceneObject;
        public Transform childA;       // hammerhead / sawblade / broomhead / wrenchtip
        public Transform childB;       // hammerhandle / sawhandle / broomhandle / wrenchhandle
    }

    private InventorySlot[] inventorySlots;
    private int activeSlotIndex = -1;   // -1 = nothing selected

    // Current held tool (derived from active slot)
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

    // Real wrench mesh references
    private class WrenchInfo
    {
        public GameObject gameObject;
        public Transform tipTransform;
        public Transform handleTransform;
    }

    private List<WrenchInfo> availableWrenches = new List<WrenchInfo>();
    private GameObject wrenchSceneObject;
    private Transform wrenchTipTransform;
    private Transform wrenchHandleTransform;
    private bool holdingRealWrench = false;

    // Soda cans, table, and scene props
    private List<Rigidbody> sodaCanBodies = new List<Rigidbody>();
    private Bounds tableBounds;
    private bool hasTable = false;
    private GameObject tableObj;

    private bool isSwinging = false;
    private HashSet<string> shownPickupPrompts = new HashSet<string>();
    private bool shownDropHint = false;
    private Dictionary<GameObject, int> droppedItemDurability = new Dictionary<GameObject, int>();
    private Coroutine activeBreakMessage;
    private bool allTasksComplete = false;

    // Nail
    private Transform nailTransform;
    private Vector3 nailStartPos;
    [SerializeField] private float nailTotalDrop = 0.0763f;

    // Nut (wrench station)
    private Transform nutTransform;
    private Vector3 nutStartPos;
    private float nutStartRotY;
    [SerializeField] private float nutTotalDrop = 0.0656f;   // 1.3967 -> 1.3311
    [SerializeField] private float nutTotalRotation = 1080f; // 3 full rotations over all turns

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

    // Tool slot UI (4 slots)
    private GameObject toolSlotContainer;
    private GameObject[] slotPanels = new GameObject[4];
    private RawImage[] slotIcons = new RawImage[4];
    private Image[] slotBarFills = new Image[4];
    private Image[] slotBorders = new Image[4];
    private Text[] slotKeyLabels = new Text[4];
    private Dictionary<string, Texture2D> toolIcons = new Dictionary<string, Texture2D>();

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

        maxDurability = 15;

        InitToolColors();
        InitInventory();
        InitTasks();
        CreateHUD();
        UpdateTaskList();
        InitHammer();
        InitSaw();
        InitBroom();
        InitWrench();
        InitNut();
        InitPlank();
        InitSodaCans();
        InitScenePropsColliders();
        LoadToolIcons();
    }

    private void Update()
    {
        if (levelComplete) return;

        UpdateInventoryInput();
        UpdateInteraction();
        UpdateDoorInteraction();
        UpdateSawGuideGlow();
        UpdateBroomPitchDamp();
        UpdateWrenchHeldTransform();
        UpdateToolSlotUI();
        CheckSodaCansUnderTable();
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

        broomSceneObject.transform.localPosition = broomHeldPosition;
        broomSceneObject.transform.localScale = broomHeldScale;

        float cameraPitch = cam.transform.eulerAngles.x;
        if (cameraPitch > 180f) cameraPitch -= 360f;

        float pitchCorrection = -cameraPitch * (1f - broomPitchFollowFactor);
        Quaternion heldRot = Quaternion.Euler(broomHeldRotation);
        broomSceneObject.transform.localRotation = Quaternion.Euler(pitchCorrection, 0f, 0f) * heldRot;
    }

    private void UpdateWrenchHeldTransform()
    {
        if (!holdingRealWrench || wrenchSceneObject == null || isSwinging) return;

        wrenchSceneObject.transform.localPosition = wrenchHeldPosition;
        wrenchSceneObject.transform.localRotation = Quaternion.Euler(wrenchHeldRotation);
        wrenchSceneObject.transform.localScale = wrenchHeldScale;
    }

    private void UpdateToolSlotUI()
    {
        if (inventorySlots == null || slotPanels[0] == null) return;

        Color emptySlotColor = new Color(0.1f, 0.1f, 0.1f, 0.75f);
        Color filledSlotColor = new Color(0.15f, 0.15f, 0.15f, 0.85f);
        Color activeSlotColor = new Color(0.25f, 0.25f, 0.3f, 0.95f);

        for (int i = 0; i < 4; i++)
        {
            InventorySlot slot = inventorySlots[i];
            bool isActive = (i == activeSlotIndex);

            // Border/highlight
            Outline outline = slotPanels[i].GetComponent<Outline>();
            if (isActive)
            {
                slotBorders[i].color = activeSlotColor;
                outline.effectColor = new Color(1f, 0.85f, 0.3f, 1f);
                outline.effectDistance = new Vector2(2f, 2f);
            }
            else
            {
                slotBorders[i].color = slot.hasItem ? filledSlotColor : emptySlotColor;
                outline.effectColor = new Color(0.3f, 0.3f, 0.3f, 0.8f);
                outline.effectDistance = new Vector2(1f, 1f);
            }

            // Key label brightness
            slotKeyLabels[i].color = isActive
                ? new Color(1f, 0.9f, 0.4f, 1f)
                : new Color(0.5f, 0.5f, 0.5f, 0.7f);

            // Icon
            if (slot.hasItem && toolIcons.ContainsKey(slot.toolName))
            {
                slotIcons[i].texture = toolIcons[slot.toolName];
                slotIcons[i].enabled = true;
            }
            else
            {
                slotIcons[i].enabled = false;
            }

            // Durability bar
            if (slot.hasItem)
            {
                slotBarFills[i].enabled = true;
                float frac = slot.maxDurability > 0 ? (float)slot.durability / slot.maxDurability : 0f;
                RectTransform fillRect = slotBarFills[i].rectTransform;
                fillRect.anchorMin = Vector2.zero;
                fillRect.anchorMax = new Vector2(frac, 1f);
                fillRect.offsetMin = Vector2.zero;
                fillRect.offsetMax = Vector2.zero;

                Color barColor;
                if (frac > 0.5f)
                    barColor = Color.Lerp(Color.yellow, Color.green, (frac - 0.5f) * 2f);
                else
                    barColor = Color.Lerp(Color.red, Color.yellow, frac * 2f);
                slotBarFills[i].color = barColor;
            }
            else
            {
                slotBarFills[i].enabled = false;
            }
        }
    }

    private void CheckSodaCansUnderTable()
    {
        if (!hasTable || sodaCanBodies.Count == 0) return;

        Task broomTask = tasks.Find(t => t.toolName == "Broom");
        if (broomTask == null || broomTask.completed) return;

        float margin = 0.5f;
        foreach (var rb in sodaCanBodies)
        {
            if (rb == null) continue;
            Vector3 pos = rb.position;
            if (pos.x < tableBounds.min.x - margin || pos.x > tableBounds.max.x + margin ||
                pos.z < tableBounds.min.z - margin || pos.z > tableBounds.max.z + margin ||
                pos.y > tableBounds.max.y)
                return;
        }

        broomTask.completed = true;
        Debug.Log("[Level10] All soda cans swept under the table! Task complete.");

        if (holdingRealBroom)
        {
            DropRealBroom();
            currentToolName = "";
            currentToolDurability = 0;

            if (activeSlotIndex >= 0)
            {
                inventorySlots[activeSlotIndex].toolName = "";
                inventorySlots[activeSlotIndex].hasItem = false;
                inventorySlots[activeSlotIndex].durability = 0;
                inventorySlots[activeSlotIndex].sceneObject = null;
                inventorySlots[activeSlotIndex].childA = null;
                inventorySlots[activeSlotIndex].childB = null;
            }
        }

        StartBreakMessage("'Sweep the Floor' complete!", new Color(0.3f, 1f, 0.3f, 1f), 3f);
        UpdateTaskList();
        CheckAllTasksComplete();
    }

    // =========================================================================
    // Initialization
    // =========================================================================

    private void InitInventory()
    {
        inventorySlots = new InventorySlot[4];
        for (int i = 0; i < 4; i++)
        {
            inventorySlots[i] = new InventorySlot
            {
                toolName = "",
                hasItem = false,
                durability = 0
            };
        }
        activeSlotIndex = 0;
    }

    private void UpdateInventoryInput()
    {
        if (isSwinging) return;

        // Q key to drop currently equipped item
        if (Input.GetKeyDown(KeyCode.Q))
        {
            DropCurrentItem();
            return;
        }

        // Number keys 1-4 to select slot
        for (int i = 0; i < 4; i++)
        {
            if (Input.GetKeyDown(KeyCode.Alpha1 + i))
            {
                SwitchToSlot(i);
                return;
            }
        }

        // Mouse wheel to cycle slots
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (scroll != 0f)
        {
            int dir = scroll > 0f ? -1 : 1;
            CycleSlot(dir);
        }
    }

    private void DropCurrentItem()
    {
        if (activeSlotIndex < 0 || !inventorySlots[activeSlotIndex].hasItem) return;

        InventorySlot slot = inventorySlots[activeSlotIndex];
        string toolName = slot.toolName;
        GameObject droppedObj = slot.sceneObject;
        Transform cA = slot.childA;
        Transform cB = slot.childB;
        int remainingDurability = slot.durability;

        if (toolName == "Hammer") DropRealHammer();
        else if (toolName == "Saw") DropRealSaw();
        else if (toolName == "Broom") DropRealBroom();
        else if (toolName == "Wrench") DropRealWrench();

        if (droppedObj != null)
        {
            droppedItemDurability[droppedObj] = remainingDurability;

            if (toolName == "Hammer")
                availableHammers.Add(new HammerInfo { gameObject = droppedObj, headTransform = cA, handleTransform = cB });
            else if (toolName == "Saw")
                availableSaws.Add(new SawInfo { gameObject = droppedObj, bladeTransform = cA, handleTransform = cB });
            else if (toolName == "Broom")
                availableBrooms.Add(new BroomInfo { gameObject = droppedObj, headTransform = cA, handleTransform = cB });
            else if (toolName == "Wrench")
                availableWrenches.Add(new WrenchInfo { gameObject = droppedObj, tipTransform = cA, handleTransform = cB });
        }

        slot.toolName = "";
        slot.hasItem = false;
        slot.durability = 0;
        slot.maxDurability = 0;
        slot.sceneObject = null;
        slot.childA = null;
        slot.childB = null;

        currentToolName = "";
        currentToolDurability = 0;

        UpdateToolSlotUI();
    }

    private void SwitchToSlot(int slotIndex)
    {
        if (slotIndex == activeSlotIndex) return;

        UnequipCurrentTool();

        activeSlotIndex = slotIndex;

        if (inventorySlots[slotIndex].hasItem)
            EquipFromSlot(slotIndex);
    }

    private void CycleSlot(int direction)
    {
        int start = activeSlotIndex < 0 ? 0 : activeSlotIndex;
        start = (start + direction + 4) % 4;
        SwitchToSlot(start);
    }

    private void UnequipCurrentTool()
    {
        // Save durability back to slot before unequipping
        if (activeSlotIndex >= 0 && inventorySlots[activeSlotIndex].hasItem)
            inventorySlots[activeSlotIndex].durability = currentToolDurability;

        if (holdingRealHammer && hammerSceneObject != null)
        {
            hammerSceneObject.SetActive(false);
            holdingRealHammer = false;
        }
        if (holdingRealSaw && sawSceneObject != null)
        {
            sawSceneObject.SetActive(false);
            holdingRealSaw = false;
            if (sawCutLine != null) sawCutLine.SetActive(false);
        }
        if (holdingRealBroom && broomSceneObject != null)
        {
            broomSceneObject.SetActive(false);
            holdingRealBroom = false;
        }
        if (holdingRealWrench && wrenchSceneObject != null)
        {
            wrenchSceneObject.SetActive(false);
            holdingRealWrench = false;
        }

        if (currentToolVisual != null)
            Destroy(currentToolVisual);

        currentToolName = "";
        currentToolDurability = 0;
        activeSlotIndex = -1;
    }

    private void EquipFromSlot(int slotIndex)
    {
        InventorySlot slot = inventorySlots[slotIndex];
        currentToolName = slot.toolName;
        currentToolDurability = slot.durability;

        if (slot.sceneObject == null) return;

        Camera cam = Camera.main;
        if (cam == null) return;

        // Set the active scene object pointers from the slot
        if (slot.toolName == "Hammer")
        {
            hammerSceneObject = slot.sceneObject;
            hammerHeadTransform = slot.childA;
            hammerHandleTransform = slot.childB;
            hammerSceneObject.SetActive(true);
            hammerSceneObject.transform.SetParent(cam.transform);
            hammerSceneObject.transform.localPosition = hammerHeldPosition;
            hammerSceneObject.transform.localRotation = Quaternion.Euler(hammerHeldRotation);
            hammerSceneObject.transform.localScale = hammerHeldScale;
            foreach (var col in hammerSceneObject.GetComponentsInChildren<Collider>())
                col.enabled = false;
            holdingRealHammer = true;
        }
        else if (slot.toolName == "Saw")
        {
            sawSceneObject = slot.sceneObject;
            sawBladeTransform = slot.childA;
            sawHandleTransform = slot.childB;
            sawSceneObject.SetActive(true);
            sawSceneObject.transform.SetParent(cam.transform);
            sawSceneObject.transform.localPosition = sawHeldPosition;
            sawSceneObject.transform.localRotation = Quaternion.Euler(sawHeldRotation);
            sawSceneObject.transform.localScale = sawHeldScale;
            foreach (var col in sawSceneObject.GetComponentsInChildren<Collider>())
                col.enabled = false;
            holdingRealSaw = true;
            if (sawCutLine != null && !plankSplit)
                sawCutLine.SetActive(true);
        }
        else if (slot.toolName == "Broom")
        {
            broomSceneObject = slot.sceneObject;
            broomHeadTransform = slot.childA;
            broomHandleTransform = slot.childB;
            broomSceneObject.SetActive(true);
            broomSceneObject.transform.SetParent(cam.transform);
            broomSceneObject.transform.localPosition = broomHeldPosition;
            broomSceneObject.transform.localRotation = Quaternion.Euler(broomHeldRotation);
            broomSceneObject.transform.localScale = broomHeldScale;
            foreach (var col in broomSceneObject.GetComponentsInChildren<Collider>())
                col.enabled = false;
            holdingRealBroom = true;
        }
        else if (slot.toolName == "Wrench")
        {
            wrenchSceneObject = slot.sceneObject;
            wrenchTipTransform = slot.childA;
            wrenchHandleTransform = slot.childB;
            wrenchSceneObject.SetActive(true);
            wrenchSceneObject.transform.SetParent(cam.transform);
            wrenchSceneObject.transform.localPosition = wrenchHeldPosition;
            wrenchSceneObject.transform.localRotation = Quaternion.Euler(wrenchHeldRotation);
            wrenchSceneObject.transform.localScale = wrenchHeldScale;
            foreach (var col in wrenchSceneObject.GetComponentsInChildren<Collider>())
                col.enabled = false;
            holdingRealWrench = true;
        }
    }

    private int GetSlotIndexForTool(string toolName)
    {
        for (int i = 0; i < inventorySlots.Length; i++)
            if (inventorySlots[i].hasItem && inventorySlots[i].toolName == toolName) return i;
        return -1;
    }

    private int GetFirstEmptySlot()
    {
        for (int i = 0; i < inventorySlots.Length; i++)
            if (!inventorySlots[i].hasItem) return i;
        return -1;
    }

    private int preferredSlot = -1;

    private bool IsInventoryFull()
    {
        // Check if the preferred slot is empty, or if there's any empty slot
        if (preferredSlot >= 0 && !inventorySlots[preferredSlot].hasItem)
            return false;
        return GetFirstEmptySlot() < 0;
    }

    private int GetOrAssignSlot(string toolName)
    {
        // Prefer the slot the player had selected before pickup
        if (preferredSlot >= 0 && !inventorySlots[preferredSlot].hasItem)
        {
            int slot = preferredSlot;
            preferredSlot = -1;
            return slot;
        }

        preferredSlot = -1;
        return GetFirstEmptySlot();
    }

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
            // TODO: re-enable these two tasks
            // new Task { name = "Dig a Hole", toolName = "Shovel", requiredUses = 5 },
            new Task { name = "Hammer a Nail", toolName = "Hammer", requiredUses = 40 },
            new Task { name = "Saw a Plank", toolName = "Saw", requiredUses = 40 },
            new Task { name = "Turn a Bolt", toolName = "Wrench", requiredUses = 55 },
            new Task { name = "Sweep the Trash Under the Table", toolName = "Broom", requiredUses = 9999 },
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
        string[] broomNames = { "broomstick", "broomstick (1)", "broomstick (2)", "broomstick (3)", "broomstick (4)", "broomstick (5)", "broomstick (6)", "broomstick (7)" };

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

            foreach (var col in broomObj.GetComponentsInChildren<Collider>())
                col.isTrigger = true;

            foreach (var meshFilter in broomObj.GetComponentsInChildren<MeshFilter>())
            {
                if (meshFilter.GetComponent<Collider>() == null)
                {
                    var mc = meshFilter.gameObject.AddComponent<MeshCollider>();
                    mc.convex = true;
                    mc.isTrigger = true;
                }
            }

            BoxCollider pickupCol = broomObj.AddComponent<BoxCollider>();
            pickupCol.isTrigger = true;
            pickupCol.size = new Vector3(0.3f, 1.2f, 0.3f);
            pickupCol.center = new Vector3(0f, 0.5f, 0f);

            availableBrooms.Add(info);
            Debug.Log($"[Level10] Broom '{broomName}' initialized from scene object");
        }

        if (availableBrooms.Count == 0)
            Debug.LogWarning("[Level10] No brooms found in scene");
    }

    private void InitWrench()
    {
        string[] wrenchNames = { "wrench", "wrench (1)", "wrench (2)", "wrench (3)" };

        foreach (string wrenchName in wrenchNames)
        {
            GameObject wrenchObj = GameObject.Find(wrenchName);
            if (wrenchObj == null)
            {
                Debug.LogWarning($"[Level10] Could not find '{wrenchName}' object in scene");
                continue;
            }

            var info = new WrenchInfo
            {
                gameObject = wrenchObj,
                tipTransform = wrenchObj.transform.Find("wrenchtip"),
                handleTransform = wrenchObj.transform.Find("wrenchhandle")
            };

            if (info.tipTransform == null || info.handleTransform == null)
                Debug.LogWarning($"[Level10] Wrench '{wrenchName}' children not found (expected 'wrenchtip' and 'wrenchhandle')");

            foreach (var meshFilter in wrenchObj.GetComponentsInChildren<MeshFilter>())
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

            availableWrenches.Add(info);
            Debug.Log($"[Level10] Wrench '{wrenchName}' initialized from scene object");
        }

        if (availableWrenches.Count == 0)
            Debug.LogWarning("[Level10] No wrenches found in scene");
    }

    private void InitNut()
    {
        GameObject nutObj = GameObject.Find("Nut");
        if (nutObj != null)
        {
            nutTransform = nutObj.transform;
            nutStartPos = nutTransform.position;
            nutStartRotY = nutTransform.eulerAngles.y;
            Debug.Log("[Level10] Found 'Nut'");
        }
        else
        {
            Debug.LogWarning("[Level10] 'Nut' not found in scene");
        }
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

    private void InitSodaCans()
    {
        PhysicsMaterial bouncyMat = new PhysicsMaterial("SodaCanBounce");
        bouncyMat.bounciness = 0.45f;
        bouncyMat.dynamicFriction = 0.15f;
        bouncyMat.staticFriction = 0.15f;
        bouncyMat.frictionCombine = PhysicsMaterialCombine.Minimum;
        bouncyMat.bounceCombine = PhysicsMaterialCombine.Maximum;

        HashSet<string> trashNames = new HashSet<string> {
            "soda can", "soda can (1)", "soda can (2)", "soda can (3)",
            "soda can (4)", "soda can (5)", "soda can (6)",
            "crumpled paper", "crumpled paper (1)", "crumpled paper (2)",
            "crumpled paper (3)", "crumpled paper (4)"
        };

        foreach (GameObject go in Resources.FindObjectsOfTypeAll<GameObject>())
        {
            if (go.scene != gameObject.scene) continue;

            if (trashNames.Contains(go.name))
            {
                trashNames.Remove(go.name);

                foreach (var col in go.GetComponentsInChildren<Collider>())
                {
                    col.isTrigger = false;
                    col.material = bouncyMat;
                }

                if (go.GetComponentsInChildren<Collider>().Length == 0)
                {
                    var bc = go.AddComponent<BoxCollider>();
                    bc.material = bouncyMat;
                }

                Rigidbody rb = go.GetComponent<Rigidbody>();
                if (rb == null)
                    rb = go.AddComponent<Rigidbody>();
                rb.mass = 0.3f;
                rb.linearDamping = 0.5f;
                rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

                go.AddComponent<TrashWallBounce>();

                sodaCanBodies.Add(rb);
            }
            else if (go.name == "Wooden Table" && tableObj == null)
            {
                tableObj = go;
            }
        }

        if (tableObj != null)
        {
            Renderer[] renderers = tableObj.GetComponentsInChildren<Renderer>();
            if (renderers.Length > 0)
            {
                tableBounds = renderers[0].bounds;
                foreach (var r in renderers)
                    tableBounds.Encapsulate(r.bounds);
                hasTable = true;
            }
        }
        else
        {
            Debug.LogWarning("[Level10] 'Wooden Table' not found in scene");
        }

        // Prevent trash objects from colliding with each other so they don't pile up
        for (int i = 0; i < sodaCanBodies.Count; i++)
        {
            for (int j = i + 1; j < sodaCanBodies.Count; j++)
            {
                foreach (var colA in sodaCanBodies[i].GetComponentsInChildren<Collider>())
                    foreach (var colB in sodaCanBodies[j].GetComponentsInChildren<Collider>())
                        Physics.IgnoreCollision(colA, colB, true);
            }
        }

        Debug.Log($"[Level10] Initialized {sodaCanBodies.Count} trash objects with physics");
    }

    private void InitScenePropsColliders()
    {
        EnsureStaticColliders(tableObj, "Wooden Table");

        GameObject pottedPlant = null;
        foreach (GameObject go in Resources.FindObjectsOfTypeAll<GameObject>())
        {
            if (go.scene != gameObject.scene) continue;
            if (go.name == "Potted Plant")
            {
                pottedPlant = go;
                break;
            }
        }
        EnsureStaticColliders(pottedPlant, "Potted Plant");

        IgnoreTrashCollisions(tableObj);
        IgnoreTrashCollisions(pottedPlant);
    }

    private void IgnoreTrashCollisions(GameObject prop)
    {
        if (prop == null) return;

        Collider[] propColliders = prop.GetComponentsInChildren<Collider>();
        foreach (var rb in sodaCanBodies)
        {
            if (rb == null) continue;
            foreach (var trashCol in rb.GetComponentsInChildren<Collider>())
            {
                foreach (var propCol in propColliders)
                    Physics.IgnoreCollision(trashCol, propCol, true);
            }
        }
    }

    private void EnsureStaticColliders(GameObject obj, string label)
    {
        if (obj == null)
        {
            Debug.LogWarning($"[Level10] '{label}' not found — skipping collider setup");
            return;
        }

        int added = 0;
        foreach (var mf in obj.GetComponentsInChildren<MeshFilter>())
        {
            if (mf.GetComponent<Collider>() != null) continue;
            if (mf.sharedMesh == null) continue;

            var mc = mf.gameObject.AddComponent<MeshCollider>();
            mc.sharedMesh = mf.sharedMesh;
            added++;
        }

        if (added == 0 && obj.GetComponentsInChildren<Collider>().Length == 0)
        {
            obj.AddComponent<BoxCollider>();
            added = 1;
        }

        Debug.Log($"[Level10] '{label}' collider setup: {added} collider(s) ensured");
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
        bool stationHandledClick = false;

        if (Physics.SphereCast(ray, interactRadius, out RaycastHit hit, interactRange, ~0, QueryTriggerInteraction.Collide))
        {
            string hitName = hit.collider.gameObject.name;

            // Check if looking at any available scene hammer
            HammerInfo hitHammer = null;
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

            // Check if looking at any available scene saw
            SawInfo hitSaw = null;
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

            // Check if looking at any available scene broom
            BroomInfo hitBroom = null;
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

            // Check if looking at any available scene wrench
            WrenchInfo hitWrench = null;
            foreach (var w in availableWrenches)
            {
                if (w.gameObject != null &&
                    (hit.collider.gameObject == w.gameObject ||
                     hit.collider.transform.IsChildOf(w.gameObject.transform)))
                {
                    hitWrench = w;
                    break;
                }
            }

            if (hitHammer != null)
            {
                showPrompt = true;
                promptText.text = "Press [E] to pick up Hammer";

                if (Input.GetKeyDown(KeyCode.E))
                {
                    PickUpHammer(hitHammer);
                    promptText.text = "";
                }
            }
            else if (hitSaw != null)
            {
                showPrompt = true;
                promptText.text = "Press [E] to pick up Saw";

                if (Input.GetKeyDown(KeyCode.E))
                {
                    PickUpSaw(hitSaw);
                    promptText.text = "";
                }
            }
            else if (hitBroom != null)
            {
                showPrompt = true;
                promptText.text = "Press [E] to pick up Broom";

                if (Input.GetKeyDown(KeyCode.E))
                {
                    PickUpBroom(hitBroom);
                    promptText.text = "";
                }
            }
            else if (hitWrench != null)
            {
                showPrompt = true;
                promptText.text = "Press [E] to pick up Wrench";

                if (Input.GetKeyDown(KeyCode.E))
                {
                    PickUpWrench(hitWrench);
                    promptText.text = "";
                }
            }
            // Check if looking at tool rack
            else if (hitName.Contains("ToolRack") || hitName.Contains("Tool_"))
            {
                showPrompt = true;

                // Determine which tool based on what's closest
                string toolName = GetToolFromHit(hit);
                promptText.text = $"Press [E] to pick up {toolName}";

                if (Input.GetKeyDown(KeyCode.E))
                {
                    PickUpTool(toolName);
                    promptText.text = "";
                }
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

                    if (!hitStation && tasks[i].toolName == "Wrench" && nutTransform != null)
                    {
                        hitStation = hit.collider.gameObject == nutTransform.gameObject ||
                                     hit.collider.transform.IsChildOf(nutTransform);
                    }

                    if (hitStation)
                    {
                        showPrompt = true;

                        bool isHammerStation = tasks[i].toolName == "Hammer";
                        bool isSawStation = tasks[i].toolName == "Saw";
                        bool isBroomStation = tasks[i].toolName == "Broom";
                        bool isWrenchStation = tasks[i].toolName == "Wrench";
                        bool isRealToolStation = isHammerStation || isSawStation || isBroomStation || isWrenchStation;

                        if (tasks[i].completed)
                        {
                            if (!isRealToolStation)
                                promptText.text = $"{tasks[i].name} - DONE";
                        }
                        else                         if (currentToolName == tasks[i].toolName)
                        {
                            if (holdingRealHammer || holdingRealSaw || holdingRealBroom || holdingRealWrench)
                                promptText.text = "";
                            else
                                promptText.text = $"Left Click to use {currentToolName} ({currentToolDurability} uses left)";

                            if (Input.GetMouseButtonDown(0) && !isSwinging)
                            {
                                stationHandledClick = true;
                                StartCoroutine(SwingAndUseTool(i));
                            }
                        }
                        else if (string.IsNullOrEmpty(currentToolName))
                        {
                            if (!isRealToolStation)
                                promptText.text = $"Need: {tasks[i].toolName} (pick one up from the rack)";
                        }
                        else
                        {
                            promptText.text = "";
                        }
                        break;
                    }
                }
            }
        }

        // Tools animate on left click anywhere; only apply durability at stations
        if (!isSwinging && !stationHandledClick && Input.GetMouseButtonDown(0))
        {
            if (holdingRealHammer)
            {
                int idx = tasks.FindIndex(t => t.toolName == "Hammer");
                if (idx >= 0 && !tasks[idx].completed)
                    StartCoroutine(SwingAndUseTool(idx, true));
            }
            else if (holdingRealSaw)
            {
                int idx = tasks.FindIndex(t => t.toolName == "Saw");
                if (idx >= 0 && !tasks[idx].completed)
                    StartCoroutine(SwingAndUseTool(idx, true));
            }
            else if (holdingRealBroom)
            {
                int idx = tasks.FindIndex(t => t.toolName == "Broom");
                if (idx >= 0 && !tasks[idx].completed)
                    StartCoroutine(SwingAndUseTool(idx));
            }
            else if (holdingRealWrench)
            {
                int idx = tasks.FindIndex(t => t.toolName == "Wrench");
                if (idx >= 0 && !tasks[idx].completed)
                    StartCoroutine(SwingAndUseTool(idx, true));
            }
        }

        bool holdingRealTool = holdingRealHammer || holdingRealSaw || holdingRealBroom || holdingRealWrench;

        if (!showPrompt)
        {
            promptText.text = "";
        }

        // Tool display is now handled by UpdateToolSlotUI()
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
        preferredSlot = activeSlotIndex;
        if (IsInventoryFull())
        {
            StartBreakMessage("Inventory Full!", new Color(1f, 0.3f, 0.3f, 1f));
            preferredSlot = -1;
            return;
        }
        UnequipCurrentTool();

        int durability = Random.Range(1, maxDurability + 1);
        currentToolName = toolName;
        currentToolDurability = durability;

        int slotIdx = GetOrAssignSlot(toolName);
        if (slotIdx >= 0)
        {
            inventorySlots[slotIdx].toolName = toolName;
            inventorySlots[slotIdx].hasItem = true;
            inventorySlots[slotIdx].durability = durability;
            inventorySlots[slotIdx].maxDurability = maxDurability;
            activeSlotIndex = slotIdx;
        }

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

        // Sync durability back to inventory slot
        if (activeSlotIndex >= 0)
            inventorySlots[activeSlotIndex].durability = currentToolDurability;

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

        if (task.toolName == "Wrench" && nutTransform != null)
        {
            float dropPerTurn = nutTotalDrop / task.requiredUses;
            float rotPerTurn = nutTotalRotation / task.requiredUses;
            Vector3 targetPos = nutStartPos + Vector3.down * dropPerTurn * task.currentUses;
            float targetYRot = rotPerTurn * task.currentUses;
            StartCoroutine(TurnNutDown(targetPos, targetYRot));
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

            if (activeSlotIndex >= 0)
            {
                inventorySlots[activeSlotIndex].toolName = "";
                inventorySlots[activeSlotIndex].hasItem = false;
                inventorySlots[activeSlotIndex].durability = 0;
                inventorySlots[activeSlotIndex].sceneObject = null;
                inventorySlots[activeSlotIndex].childA = null;
                inventorySlots[activeSlotIndex].childB = null;
            }

            if (holdingRealHammer && task.toolName == "Hammer")
                DropRealHammer();
            else if (holdingRealSaw && task.toolName == "Saw")
                DropRealSaw();
            else if (holdingRealBroom && task.toolName == "Broom")
                DropRealBroom();
            else if (holdingRealWrench && task.toolName == "Wrench")
                DropRealWrench();

            currentToolName = "";
            currentToolDurability = 0;
            activeSlotIndex = -1;

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
        else if (holdingRealWrench)
        {
            BreakWrench();
        }
        else if (currentToolVisual != null)
        {
            StartCoroutine(BreakAnimation(currentToolVisual));
        }

        // Clear the inventory slot
        if (activeSlotIndex >= 0)
        {
            inventorySlots[activeSlotIndex].toolName = "";
            inventorySlots[activeSlotIndex].hasItem = false;
            inventorySlots[activeSlotIndex].durability = 0;
            inventorySlots[activeSlotIndex].sceneObject = null;
            inventorySlots[activeSlotIndex].childA = null;
            inventorySlots[activeSlotIndex].childB = null;
        }
        currentToolName = "";
        currentToolDurability = 0;
    }

    private IEnumerator BreakAnimation(GameObject tool)
    {
        Vector3 originalPos = tool.transform.localPosition;
        for (int i = 0; i < 8; i++)
        {
            tool.transform.localPosition = originalPos + Random.insideUnitSphere * 0.05f;
            yield return new WaitForSeconds(0.03f);
        }

        tool.transform.SetParent(null);
        Rigidbody rb = tool.AddComponent<Rigidbody>();
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        IgnorePlayerCollision(tool);
        if (tool.GetComponent<Collider>() == null)
            tool.AddComponent<BoxCollider>();
        rb.AddForce(Random.insideUnitSphere * 2f, ForceMode.Impulse);
        rb.AddTorque(Random.insideUnitSphere * 3f, ForceMode.Impulse);
    }

    private IEnumerator SwingAndUseTool(int taskIndex, bool animationOnly = false)
    {
        isSwinging = true;

        bool isSawTask = tasks[taskIndex].toolName == "Saw" && plankTransform != null;
        bool isBroomTask = tasks[taskIndex].toolName == "Broom";
        bool isWrenchTask = tasks[taskIndex].toolName == "Wrench";

        if (isSawTask)
        {
            yield return StartCoroutine(SawAndUseTool(taskIndex, animationOnly));
            isSwinging = false;
            yield break;
        }

        if (isBroomTask)
        {
            yield return StartCoroutine(SweepAndUseTool(taskIndex));
            isSwinging = false;
            yield break;
        }

        if (isWrenchTask)
        {
            yield return StartCoroutine(WrenchAndUseTool(taskIndex, animationOnly));
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

            if (!animationOnly && !isHammerTask)
                UseTool(taskIndex);

            bool hammerHitNail = false;

            t = 0f;
            while (t < swingTime)
            {
                t += Time.deltaTime;
                swingTarget.localRotation = Quaternion.Slerp(windUp, swingDown, t / swingTime);

                if (!animationOnly && isHammerTask && !hammerHitNail && hammerHeadTransform != null)
                {
                    float dist = Vector3.Distance(hammerHeadTransform.position, nailTransform.position);
                    if (dist <= hammerHitDistance)
                        hammerHitNail = true;
                }

                yield return null;
            }

            if (!animationOnly && isHammerTask)
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
        else if (!animationOnly)
        {
            UseTool(taskIndex);
        }

        isSwinging = false;
    }

    private IEnumerator SawAndUseTool(int taskIndex, bool animationOnly = false)
    {
        Transform swingTarget = null;
        if (holdingRealSaw && sawSceneObject != null)
            swingTarget = sawSceneObject.transform;
        else if (currentToolVisual != null)
            swingTarget = currentToolVisual.transform;

        if (swingTarget == null)
        {
            if (!animationOnly) UseTool(taskIndex);
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

                if (!animationOnly && !sawHitCutLine && sawCutLine != null)
                {
                    float dist = Vector3.Distance(bladetip.position, sawCutLine.transform.position);
                    if (dist <= sawAlignDistance)
                        sawHitCutLine = true;
                }

                yield return null;
            }
        }

        if (!animationOnly && sawHitCutLine)
        {
            int durabilityBefore = currentToolDurability;
            UseTool(taskIndex);
            bool toolBroke = durabilityBefore > 0 && currentToolDurability <= 0;

            if (!toolBroke && !tasks[taskIndex].completed)
                StartBreakMessage("Sawing...", new Color(1f, 0.85f, 0.4f, 1f));
        }
        else if (!animationOnly && !sawHitCutLine)
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
            float angle = (i % 2 == 0) ? -sweepAngle : sweepAngle;
            float elapsed = 0f;
            Quaternion from = swingTarget.localRotation;
            Quaternion to = startRot * Quaternion.Euler(angle, 0f, 0f);

            while (elapsed < sweepStrokeTime)
            {
                elapsed += Time.deltaTime;
                swingTarget.localRotation = Quaternion.Slerp(from, to, elapsed / sweepStrokeTime);

                Transform tip = broomHeadTransform != null ? broomHeadTransform : swingTarget;
                Vector3 rawDir = Camera.main != null ? Camera.main.transform.forward : swingTarget.forward;
                Vector3 sweepDir = new Vector3(rawDir.x, 0f, rawDir.z).normalized;
                Collider[] nearby = Physics.OverlapSphere(tip.position, sweepPushRadius);
                foreach (var col in nearby)
                {
                    Rigidbody rb = col.attachedRigidbody;
                    if (rb != null && sodaCanBodies.Contains(rb))
                        rb.AddForce(sweepDir * sweepPushForce, ForceMode.Force);
                }

                yield return null;
            }
        }

        Task broomTask = tasks[taskIndex];
        int durabilityBefore = currentToolDurability;
        UseTool(taskIndex);
        bool toolBroke = durabilityBefore > 0 && currentToolDurability <= 0;

        if (!toolBroke && !broomTask.completed)
            StartBreakMessage("Sweeping...", new Color(0.6f, 0.9f, 0.5f, 1f));

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
        preferredSlot = activeSlotIndex;
        if (IsInventoryFull())
        {
            StartBreakMessage("Inventory Full!", new Color(1f, 0.3f, 0.3f, 1f));
            preferredSlot = -1;
            return;
        }
        UnequipCurrentTool();

        availableHammers.Remove(info);

        int slotIdx = GetOrAssignSlot("Hammer");
        if (slotIdx < 0) return;

        int dur = maxDurability;
        if (droppedItemDurability.TryGetValue(info.gameObject, out int saved))
        {
            dur = saved;
            droppedItemDurability.Remove(info.gameObject);
        }
        Rigidbody existingRb = info.gameObject.GetComponent<Rigidbody>();
        if (existingRb != null) Destroy(existingRb);

        inventorySlots[slotIdx].toolName = "Hammer";
        inventorySlots[slotIdx].hasItem = true;
        inventorySlots[slotIdx].durability = dur;
        inventorySlots[slotIdx].maxDurability = maxDurability;
        inventorySlots[slotIdx].sceneObject = info.gameObject;
        inventorySlots[slotIdx].childA = info.headTransform;
        inventorySlots[slotIdx].childB = info.handleTransform;

        activeSlotIndex = slotIdx;
        EquipFromSlot(slotIdx);

        if (shownPickupPrompts.Add("Hammer"))
            StartCoroutine(ShowTimedPrompt("Hammer in the nail on the workbench.", 2f, 0.5f));
        ShowDropHint();
        Debug.Log($"[Level10] Picked up real Hammer with {currentToolDurability} durability");
    }

    private void ShowDropHint()
    {
        if (shownDropHint) return;
        shownDropHint = true;
        StartCoroutine(ShowDropHintDelayed());
    }

    private IEnumerator ShowDropHintDelayed()
    {
        yield return new WaitForSeconds(3f);
        StartBreakMessage("Press [Q] to drop items", new Color(0.8f, 0.8f, 0.8f, 1f), 3f);
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

        Camera cam = Camera.main;
        while (target != null && cam != null)
        {
            float dist = Vector3.Distance(cam.transform.position, target.position);
            if (dist <= range)
                break;

            yield return null;
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
            {
                col.enabled = true;
                col.isTrigger = false;
            }
            if (hammerHeadTransform.GetComponent<Collider>() == null)
                hammerHeadTransform.gameObject.AddComponent<BoxCollider>();

            IgnorePlayerCollision(hammerHeadTransform.gameObject);

            Rigidbody rb = hammerHeadTransform.gameObject.AddComponent<Rigidbody>();
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

            Vector3 launchDir = cam != null
                ? cam.transform.forward + Vector3.up * 0.5f
                : Vector3.forward + Vector3.up * 0.5f;
            rb.AddForce(launchDir.normalized * hammerHeadLaunchForce, ForceMode.Impulse);
            rb.AddTorque(Random.insideUnitSphere * hammerHeadTorque, ForceMode.Impulse);

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
            {
                col.enabled = true;
                col.isTrigger = false;
            }
            if (hammerSceneObject.GetComponentsInChildren<Collider>().Length == 0)
                hammerSceneObject.AddComponent<BoxCollider>();

            IgnorePlayerCollision(hammerSceneObject);

            Rigidbody rb = hammerSceneObject.AddComponent<Rigidbody>();
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

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
            {
                col.enabled = true;
                col.isTrigger = false;
            }

            IgnorePlayerCollision(hammerSceneObject);

            Rigidbody rb = hammerSceneObject.AddComponent<Rigidbody>();
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

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
        preferredSlot = activeSlotIndex;
        if (IsInventoryFull())
        {
            StartBreakMessage("Inventory Full!", new Color(1f, 0.3f, 0.3f, 1f));
            preferredSlot = -1;
            return;
        }
        UnequipCurrentTool();

        availableSaws.Remove(info);

        int slotIdx = GetOrAssignSlot("Saw");
        if (slotIdx < 0) return;

        int dur = maxDurability;
        if (droppedItemDurability.TryGetValue(info.gameObject, out int saved))
        {
            dur = saved;
            droppedItemDurability.Remove(info.gameObject);
        }
        Rigidbody existingRb = info.gameObject.GetComponent<Rigidbody>();
        if (existingRb != null) Destroy(existingRb);

        inventorySlots[slotIdx].toolName = "Saw";
        inventorySlots[slotIdx].hasItem = true;
        inventorySlots[slotIdx].durability = dur;
        inventorySlots[slotIdx].maxDurability = maxDurability;
        inventorySlots[slotIdx].sceneObject = info.gameObject;
        inventorySlots[slotIdx].childA = info.bladeTransform;
        inventorySlots[slotIdx].childB = info.handleTransform;

        activeSlotIndex = slotIdx;
        EquipFromSlot(slotIdx);

        if (shownPickupPrompts.Add("Saw"))
            StartCoroutine(ShowTimedPrompt("Saw the plank at the workbench.", 2f, 0.5f));
        ShowDropHint();
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
            {
                col.enabled = true;
                col.isTrigger = false;
            }
            if (sawBladeTransform.GetComponent<Collider>() == null)
                sawBladeTransform.gameObject.AddComponent<BoxCollider>();

            IgnorePlayerCollision(sawBladeTransform.gameObject);

            Rigidbody rb = sawBladeTransform.gameObject.AddComponent<Rigidbody>();
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

            Vector3 launchDir = cam != null
                ? cam.transform.forward + Vector3.up * 0.5f
                : Vector3.forward + Vector3.up * 0.5f;
            rb.AddForce(launchDir.normalized * sawBladeLaunchForce, ForceMode.Impulse);
            rb.AddTorque(Random.insideUnitSphere * sawBladeTorque, ForceMode.Impulse);

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
            {
                col.enabled = true;
                col.isTrigger = false;
            }
            if (sawSceneObject.GetComponentsInChildren<Collider>().Length == 0)
                sawSceneObject.AddComponent<BoxCollider>();

            IgnorePlayerCollision(sawSceneObject);

            Rigidbody rb = sawSceneObject.AddComponent<Rigidbody>();
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

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
            {
                col.enabled = true;
                col.isTrigger = false;
            }

            IgnorePlayerCollision(sawSceneObject);

            Rigidbody rb = sawSceneObject.AddComponent<Rigidbody>();
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

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
        preferredSlot = activeSlotIndex;
        if (IsInventoryFull())
        {
            StartBreakMessage("Inventory Full!", new Color(1f, 0.3f, 0.3f, 1f));
            preferredSlot = -1;
            return;
        }
        UnequipCurrentTool();

        availableBrooms.Remove(info);

        int slotIdx = GetOrAssignSlot("Broom");
        if (slotIdx < 0) return;

        int dur = 3;
        if (droppedItemDurability.TryGetValue(info.gameObject, out int saved))
        {
            dur = saved;
            droppedItemDurability.Remove(info.gameObject);
        }
        Rigidbody existingRb = info.gameObject.GetComponent<Rigidbody>();
        if (existingRb != null) Destroy(existingRb);

        inventorySlots[slotIdx].toolName = "Broom";
        inventorySlots[slotIdx].hasItem = true;
        inventorySlots[slotIdx].durability = dur;
        inventorySlots[slotIdx].maxDurability = 3;
        inventorySlots[slotIdx].sceneObject = info.gameObject;
        inventorySlots[slotIdx].childA = info.headTransform;
        inventorySlots[slotIdx].childB = info.handleTransform;

        activeSlotIndex = slotIdx;
        currentToolDurability = dur;
        EquipFromSlot(slotIdx);

        if (shownPickupPrompts.Add("Broom"))
            StartCoroutine(ShowTimedPrompt("Sweep the trash under the table.", 2f, 0.5f));
        ShowDropHint();
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

            bool hasNonTriggerCollider = false;
            foreach (var col in child.GetComponentsInChildren<Collider>())
            {
                if (col.enabled && !col.isTrigger)
                {
                    hasNonTriggerCollider = true;
                    break;
                }
            }
            if (!hasNonTriggerCollider)
                child.gameObject.AddComponent<BoxCollider>();

            IgnorePlayerCollision(child.gameObject);

            foreach (var childCol in child.GetComponentsInChildren<Collider>())
            {
                foreach (var trashRb in sodaCanBodies)
                {
                    if (trashRb == null) continue;
                    foreach (var trashCol in trashRb.GetComponentsInChildren<Collider>())
                        Physics.IgnoreCollision(childCol, trashCol, true);
                }
            }

            Rigidbody rb = child.gameObject.AddComponent<Rigidbody>();
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            rb.AddForce(Random.insideUnitSphere * 0.5f, ForceMode.Impulse);
            rb.AddTorque(Random.insideUnitSphere * 2f, ForceMode.Impulse);

        }

        Destroy(broomSceneObject);
        broomSceneObject = null;
        broomHeadTransform = null;
        broomHandleTransform = null;

        Task broomTask = tasks.Find(t => t.toolName == "Broom");
        if (broomTask != null && !broomTask.completed && availableBrooms.Count == 0)
            StartBreakMessage("Oops! You ran out of brooms. Guess you have to restart the level now.", new Color(1f, 0.3f, 0.3f, 1f), 5f);
    }

    private void DropRealBroom()
    {
        holdingRealBroom = false;

        if (broomSceneObject != null)
        {
            broomSceneObject.transform.SetParent(null);

            foreach (var col in broomSceneObject.GetComponentsInChildren<Collider>())
            {
                col.enabled = true;
                col.isTrigger = false;
            }

            IgnorePlayerCollision(broomSceneObject);

            Rigidbody rb = broomSceneObject.AddComponent<Rigidbody>();
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

            broomSceneObject = null;
            broomHeadTransform = null;
            broomHandleTransform = null;
        }
    }

    // =========================================================================
    // Wrench Mesh Handling
    // =========================================================================

    private void PickUpWrench(WrenchInfo info)
    {
        preferredSlot = activeSlotIndex;
        if (IsInventoryFull())
        {
            StartBreakMessage("Inventory Full!", new Color(1f, 0.3f, 0.3f, 1f));
            preferredSlot = -1;
            return;
        }
        UnequipCurrentTool();

        availableWrenches.Remove(info);

        int slotIdx = GetOrAssignSlot("Wrench");
        if (slotIdx < 0) return;

        int dur = maxDurability;
        if (droppedItemDurability.TryGetValue(info.gameObject, out int saved))
        {
            dur = saved;
            droppedItemDurability.Remove(info.gameObject);
        }
        Rigidbody existingRb = info.gameObject.GetComponent<Rigidbody>();
        if (existingRb != null) Destroy(existingRb);

        inventorySlots[slotIdx].toolName = "Wrench";
        inventorySlots[slotIdx].hasItem = true;
        inventorySlots[slotIdx].durability = dur;
        inventorySlots[slotIdx].maxDurability = maxDurability;
        inventorySlots[slotIdx].sceneObject = info.gameObject;
        inventorySlots[slotIdx].childA = info.tipTransform;
        inventorySlots[slotIdx].childB = info.handleTransform;

        activeSlotIndex = slotIdx;
        EquipFromSlot(slotIdx);

        if (shownPickupPrompts.Add("Wrench"))
            StartCoroutine(ShowTimedPrompt("Tighten the nut at the station.", 2f, 0.5f));
        ShowDropHint();
        Debug.Log($"[Level10] Picked up real Wrench with {currentToolDurability} durability");
    }

    private IEnumerator WrenchAndUseTool(int taskIndex, bool animationOnly = false)
    {
        Transform swingTarget = null;
        if (holdingRealWrench && wrenchSceneObject != null)
            swingTarget = wrenchSceneObject.transform;
        else if (currentToolVisual != null)
            swingTarget = currentToolVisual.transform;

        if (swingTarget == null)
        {
            if (!animationOnly) UseTool(taskIndex);
            yield break;
        }

        Camera cam = Camera.main;

        Quaternion startRot = swingTarget.localRotation;
        Vector3 startPos = swingTarget.localPosition;

        // Get the geometric center of the wrenchtip mesh to use as pivot
        Vector3 tipCenterWorld = swingTarget.position;
        if (holdingRealWrench && wrenchTipTransform != null)
        {
            Renderer tipRenderer = wrenchTipTransform.GetComponentInChildren<Renderer>();
            tipCenterWorld = tipRenderer != null ? tipRenderer.bounds.center : wrenchTipTransform.position;
        }

        // Store pivot in camera-local space so player movement doesn't cause drift
        Vector3 tipOffsetLocal = swingTarget.InverseTransformPoint(tipCenterWorld);
        Vector3 tipCenterCamLocal = cam.transform.InverseTransformPoint(tipCenterWorld);

        Quaternion engageRot = Quaternion.Euler(0f, -15f, 0f) * startRot;
        Quaternion turnedRot = Quaternion.Euler(0f, 90f, 0f) * startRot;

        // Wind-up: small rotation in the opposite direction
        float windUpTime = 0.1f;
        float t = 0f;
        while (t < windUpTime)
        {
            t += Time.deltaTime;
            swingTarget.localRotation = Quaternion.Slerp(startRot, engageRot, t / windUpTime);
            CorrectPositionForPivotLocal(swingTarget, tipOffsetLocal, tipCenterCamLocal, cam);
            yield return null;
        }

        // Turning motion — rotate around the wrenchtip center
        float turnTime = 0.25f;
        t = 0f;
        while (t < turnTime)
        {
            t += Time.deltaTime;
            swingTarget.localRotation = Quaternion.Slerp(engageRot, turnedRot, t / turnTime);
            CorrectPositionForPivotLocal(swingTarget, tipOffsetLocal, tipCenterCamLocal, cam);
            yield return null;
        }

        if (!animationOnly)
        {
            int durabilityBefore = currentToolDurability;
            UseTool(taskIndex);
            bool toolBroke = durabilityBefore > 0 && currentToolDurability <= 0;

            if (!toolBroke && !tasks[taskIndex].completed)
                StartBreakMessage("Turning...", new Color(1f, 0.85f, 0.4f, 1f));
        }

        // Return to rest — animate back to original position and rotation
        float returnTime = 0.2f;
        t = 0f;
        Quaternion currentRot = swingTarget != null ? swingTarget.localRotation : startRot;
        Vector3 currentPos = swingTarget != null ? swingTarget.localPosition : startPos;
        while (t < returnTime && swingTarget != null)
        {
            t += Time.deltaTime;
            float frac = t / returnTime;
            swingTarget.localRotation = Quaternion.Slerp(currentRot, startRot, frac);
            swingTarget.localPosition = Vector3.Lerp(currentPos, startPos, frac);
            yield return null;
        }
        if (swingTarget != null)
        {
            swingTarget.localRotation = startRot;
            swingTarget.localPosition = startPos;
        }
    }

    private void CorrectPositionForPivotLocal(Transform wrench, Vector3 tipOffsetLocal, Vector3 originalTipCamLocal, Camera cam)
    {
        Vector3 newTipWorld = wrench.TransformPoint(tipOffsetLocal);
        Vector3 newTipCamLocal = cam.transform.InverseTransformPoint(newTipWorld);
        wrench.localPosition += originalTipCamLocal - newTipCamLocal;
    }

    private IEnumerator TurnNutDown(Vector3 targetPos, float targetYRotation)
    {
        if (nutTransform == null) yield break;

        Vector3 from = nutTransform.position;
        Quaternion fromRot = nutTransform.rotation;
        Quaternion toRot = Quaternion.Euler(
            nutTransform.eulerAngles.x,
            nutStartRotY + targetYRotation,
            nutTransform.eulerAngles.z
        );

        float duration = 0.25f;
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float frac = t / duration;
            nutTransform.position = Vector3.Lerp(from, targetPos, frac);
            nutTransform.rotation = Quaternion.Slerp(fromRot, toRot, frac);
            yield return null;
        }
        nutTransform.position = targetPos;
        nutTransform.rotation = toRot;
    }

    private void BreakWrench()
    {
        holdingRealWrench = false;
        Camera cam = Camera.main;

        if (wrenchTipTransform != null)
        {
            wrenchTipTransform.SetParent(null);

            foreach (var col in wrenchTipTransform.GetComponentsInChildren<Collider>())
            {
                col.enabled = true;
                col.isTrigger = false;
            }
            if (wrenchTipTransform.GetComponent<Collider>() == null)
                wrenchTipTransform.gameObject.AddComponent<BoxCollider>();

            IgnorePlayerCollision(wrenchTipTransform.gameObject);

            Rigidbody rb = wrenchTipTransform.gameObject.AddComponent<Rigidbody>();
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

            Vector3 launchDir = cam != null
                ? cam.transform.forward + Vector3.up * 0.5f
                : Vector3.forward + Vector3.up * 0.5f;
            rb.AddForce(launchDir.normalized * wrenchTipLaunchForce, ForceMode.Impulse);
            rb.AddTorque(Random.insideUnitSphere * wrenchTipTorque, ForceMode.Impulse);

            wrenchTipTransform = null;
        }

        if (wrenchSceneObject != null)
            StartCoroutine(DropWrenchHandleAfterDelay(0.8f));
    }

    private IEnumerator DropWrenchHandleAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);

        if (wrenchSceneObject != null)
        {
            wrenchSceneObject.transform.SetParent(null);

            foreach (var col in wrenchSceneObject.GetComponentsInChildren<Collider>())
            {
                col.enabled = true;
                col.isTrigger = false;
            }
            if (wrenchSceneObject.GetComponentsInChildren<Collider>().Length == 0)
                wrenchSceneObject.AddComponent<BoxCollider>();

            IgnorePlayerCollision(wrenchSceneObject);

            Rigidbody rb = wrenchSceneObject.AddComponent<Rigidbody>();
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

            wrenchSceneObject = null;
            wrenchHandleTransform = null;
        }
    }

    private void DropRealWrench()
    {
        holdingRealWrench = false;

        if (wrenchSceneObject != null)
        {
            wrenchSceneObject.transform.SetParent(null);

            foreach (var col in wrenchSceneObject.GetComponentsInChildren<Collider>())
            {
                col.enabled = true;
                col.isTrigger = false;
            }

            IgnorePlayerCollision(wrenchSceneObject);

            Rigidbody rb = wrenchSceneObject.AddComponent<Rigidbody>();
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

            wrenchSceneObject = null;
            wrenchTipTransform = null;
            wrenchHandleTransform = null;
        }
    }

    // =========================================================================
    // Wood Plank / Sawing
    // =========================================================================

    private void UpdateSawCutProgress(Task task)
    {
        float progress = (float)task.currentUses / task.requiredUses;
        sawCutProgress = progress;
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

        if (allTasksComplete) return;
        allTasksComplete = true;

        if (doorController != null)
        {
            Debug.Log("[Level10] All tasks complete! Walk to the door and press E to open it.");
            StartBreakMessage("All tasks done! Head to the door.", new Color(0.3f, 1f, 0.3f, 1f), 4f);
        }
        else
        {
            Debug.Log("[Level10] All tasks complete! No door controller — completing level.");
            CompleteLevel();
        }
    }

    private bool IsLookingAtDoor()
    {
        if (doorController == null) return false;
        Camera cam = Camera.main;
        if (cam == null) return false;

        Ray ray = new Ray(cam.transform.position, cam.transform.forward);
        if (Physics.SphereCast(ray, interactRadius, out RaycastHit hit, interactRange, ~0, QueryTriggerInteraction.Collide))
        {
            Transform t = hit.transform;
            while (t != null)
            {
                if (t == doorController.transform) return true;
                t = t.parent;
            }
        }
        return false;
    }

    private void UpdateDoorInteraction()
    {
        if (!allTasksComplete || levelComplete) return;
        if (doorController == null || doorController.IsOpen || doorController.IsAnimating) return;

        if (IsLookingAtDoor())
        {
            promptText.text = "Press [E] to open door";

            if (Input.GetKeyDown(KeyCode.E))
            {
                Debug.Log("[Level10] Player opened the door.");
                promptText.text = "";
                StartCoroutine(DoorFallAndComplete());
            }
        }
    }

    private IEnumerator DoorFallAndComplete()
    {
        if (doorController != null && doorController.doorPanel != null)
        {
            GameObject panel = doorController.doorPanel;
            panel.transform.SetParent(null);

            // Disable any colliders on the door frame so they don't block the fall
            if (doorController.frameLeft != null)
                doorController.frameLeft.SetActive(false);
            if (doorController.frameRight != null)
                doorController.frameRight.SetActive(false);
            if (doorController.frameTop != null)
                doorController.frameTop.SetActive(false);

            Rigidbody rb = panel.GetComponent<Rigidbody>();
            if (rb == null)
                rb = panel.AddComponent<Rigidbody>();

            rb.isKinematic = false;
            rb.mass = 40f;
            rb.useGravity = true;
            rb.constraints = RigidbodyConstraints.None;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode.Continuous;

            if (panel.GetComponent<Collider>() == null)
                panel.AddComponent<BoxCollider>();

            yield return new WaitForFixedUpdate();

            Vector3 topOfDoor = panel.transform.position + Vector3.up * 1.4f;
            Vector3 pushDir = -panel.transform.forward;
            rb.AddForceAtPosition(pushDir * 120f, topOfDoor, ForceMode.Impulse);
            rb.AddTorque(panel.transform.right * 80f, ForceMode.Impulse);

            Debug.Log($"[Level10] Door physics: forward={panel.transform.forward}, pushDir={pushDir}, pos={panel.transform.position}");

            yield return new WaitForSeconds(3f);
        }

        CompleteLevel();
    }

    private void UpdateTaskList()
    {
        if (taskListText == null) return;

        string text = "TASKS:\n";
        foreach (var task in tasks)
        {
            if (task.completed)
                text += $"  <color=#88FF88>{task.name} ({task.toolName}) \u2713</color>\n";
            else
                text += $"  {task.name} ({task.toolName})\n";
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
        toolText = MakeText(canvasObj.transform, "ToolText", "",
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

        CreateToolSlotUI(canvasObj.transform);
    }

    private void LoadToolIcons()
    {
        string[] toolNames = { "Hammer", "Saw", "Broom", "Wrench" };
        foreach (string name in toolNames)
        {
            Texture2D tex = Resources.Load<Texture2D>($"Icons/{name}");
            if (tex != null)
                toolIcons[name] = tex;
            else
                Debug.LogWarning($"[Level10] Tool icon not found: Resources/Icons/{name}");
        }
        Debug.Log($"[Level10] Loaded {toolIcons.Count} tool icon(s)");
    }

    private void CreateToolSlotUI(Transform parent)
    {
        float slotWidth = 72f;
        float slotHeight = 90f;
        float slotSpacing = 8f;
        float totalWidth = slotWidth * 4 + slotSpacing * 3;

        // Container anchored to bottom center
        GameObject container = new GameObject("InventoryBar");
        container.transform.SetParent(parent, false);
        RectTransform containerRect = container.AddComponent<RectTransform>();
        containerRect.anchorMin = new Vector2(0.5f, 0f);
        containerRect.anchorMax = new Vector2(0.5f, 0f);
        containerRect.pivot = new Vector2(0.5f, 0f);
        containerRect.anchoredPosition = new Vector2(0f, 12f);
        containerRect.sizeDelta = new Vector2(totalWidth + 16f, slotHeight + 8f);
        toolSlotContainer = container;

        for (int i = 0; i < 4; i++)
        {
            float xPos = -totalWidth / 2f + slotWidth / 2f + i * (slotWidth + slotSpacing);

            GameObject slotObj = new GameObject("Slot_" + (i + 1));
            slotObj.transform.SetParent(container.transform, false);
            RectTransform slotRect = slotObj.AddComponent<RectTransform>();
            slotRect.anchorMin = new Vector2(0.5f, 0.5f);
            slotRect.anchorMax = new Vector2(0.5f, 0.5f);
            slotRect.pivot = new Vector2(0.5f, 0.5f);
            slotRect.anchoredPosition = new Vector2(xPos, 0f);
            slotRect.sizeDelta = new Vector2(slotWidth, slotHeight);

            Image frameBg = slotObj.AddComponent<Image>();
            frameBg.color = new Color(0.1f, 0.1f, 0.1f, 0.75f);
            frameBg.raycastTarget = false;
            slotBorders[i] = frameBg;

            var outline = slotObj.AddComponent<Outline>();
            outline.effectColor = new Color(0.3f, 0.3f, 0.3f, 0.8f);
            outline.effectDistance = new Vector2(2f, 2f);

            // Key label (1-4) at top-left
            slotKeyLabels[i] = MakeText(slotObj.transform, "KeyLabel", (i + 1).ToString(),
                new Vector2(0f, 0.82f), new Vector2(0.35f, 1f),
                10, new Color(0.7f, 0.7f, 0.7f, 0.8f), TextAnchor.MiddleCenter);

            // Tool icon
            GameObject iconObj = new GameObject("Icon");
            iconObj.transform.SetParent(slotObj.transform, false);
            RectTransform iconRect = iconObj.AddComponent<RectTransform>();
            iconRect.anchorMin = new Vector2(0.05f, 0.22f);
            iconRect.anchorMax = new Vector2(0.95f, 0.80f);
            iconRect.offsetMin = Vector2.zero;
            iconRect.offsetMax = Vector2.zero;
            slotIcons[i] = iconObj.AddComponent<RawImage>();
            slotIcons[i].color = Color.white;
            slotIcons[i].raycastTarget = false;
            slotIcons[i].enabled = false;

            // Durability bar background
            GameObject barBgObj = new GameObject("BarBg");
            barBgObj.transform.SetParent(slotObj.transform, false);
            RectTransform barBgRect = barBgObj.AddComponent<RectTransform>();
            barBgRect.anchorMin = new Vector2(0.08f, 0.04f);
            barBgRect.anchorMax = new Vector2(0.92f, 0.18f);
            barBgRect.offsetMin = Vector2.zero;
            barBgRect.offsetMax = Vector2.zero;
            Image barBgImg = barBgObj.AddComponent<Image>();
            barBgImg.color = new Color(0.15f, 0.15f, 0.15f, 1f);
            barBgImg.raycastTarget = false;

            // Durability bar fill
            GameObject barFillObj = new GameObject("BarFill");
            barFillObj.transform.SetParent(barBgObj.transform, false);
            RectTransform barFillRect = barFillObj.AddComponent<RectTransform>();
            barFillRect.anchorMin = Vector2.zero;
            barFillRect.anchorMax = Vector2.one;
            barFillRect.offsetMin = Vector2.zero;
            barFillRect.offsetMax = Vector2.zero;
            slotBarFills[i] = barFillObj.AddComponent<Image>();
            slotBarFills[i].color = Color.green;
            slotBarFills[i].raycastTarget = false;

            slotPanels[i] = slotObj;
        }
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

public class TrashWallBounce : MonoBehaviour
{
    private Rigidbody rb;
    private const float wallNormalThreshold = 0.3f;
    private const float impactForce = 0.5f;
    private const float stayForce = 0.5f;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    void OnCollisionEnter(Collision collision)
    {
        if (collision.rigidbody != null) return;

        foreach (ContactPoint contact in collision.contacts)
        {
            if (Mathf.Abs(contact.normal.y) < wallNormalThreshold)
            {
                rb.AddForce(contact.normal * impactForce, ForceMode.Impulse);
                break;
            }
        }
    }

    void OnCollisionStay(Collision collision)
    {
        if (collision.rigidbody != null) return;

        foreach (ContactPoint contact in collision.contacts)
        {
            if (Mathf.Abs(contact.normal.y) < wallNormalThreshold)
            {
                rb.AddForce(contact.normal * stayForce, ForceMode.Force);
                break;
            }
        }
    }
}
