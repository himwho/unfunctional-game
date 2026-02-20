using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// LEVEL 11: Bad RNG door. The player enters a room with two big buttons.
/// Option A spawns an "Easy Room" (75% chance of a door, 25% no door -- forcing
/// restart). Option B says "Difficult Room" but is "COMING SOON - Requires
/// Premium DLC" and is not interactable. The player must gamble on Option A
/// repeatedly. Each fail taunts them. When a door spawns, it auto-opens.
///
/// Builds the room, buttons, HUD, and spawned rooms at runtime.
/// Attach to root GameObject in LEVEL11 scene.
/// </summary>
public class Level11_BadRNG : LevelManager
{
    [Header("Level 11 - Bad RNG")]
    public DoorController doorController;

    [Header("RNG Settings")]
    [Range(0f, 1f)]
    public float doorSpawnChance = 0.75f;

    // Runtime
    private Canvas hudCanvas;
    private Text statusText;
    private Text attemptText;
    private Text tauntText;
    private Text optionALabel;
    private Text optionBLabel;
    private GameObject choiceRoom;
    private GameObject spawnedRoom;
    private GameObject optionAButton;
    private GameObject optionBButton;
    private GameObject dlcSignObj;

    private int attemptCount = 0;
    private bool isChoosing = true;
    private bool roomSpawned = false;

    // Taunt messages
    private readonly string[] tauntMessages = new string[]
    {
        "No door this time! Better luck next time!",
        "The RNG gods frown upon you.",
        "NOPE. Try again.",
        "So close! (Not really.)",
        "The door was in the other room. Just kidding, there is no other room.",
        "75% chance and you STILL missed? Incredible.",
        "Maybe the door is a metaphor. Nah, it's just bad luck.",
        "Door machine broke. Understandable, have a nice day.",
        "You'd think 75% would be generous. You'd be wrong.",
        "Fun fact: you have a better chance of this working than a gacha pull.",
        "The door sends its regards. From somewhere else.",
        "Try turning it off and on again. Oh wait.",
    };

    protected override void Start()
    {
        base.Start();
        levelDisplayName = "RNG Casino";
        levelDescription = "Pick a door. Or don't. Your odds are... concerning.";
        needsPlayer = true;
        wantsCursorLocked = true;

        CreateHUD();
        ShowChoicePhase();
    }

    private void Update()
    {
        if (levelComplete) return;

        if (isChoosing)
        {
            UpdateChoiceInteraction();
        }
    }

    // =========================================================================
    // Choice Phase
    // =========================================================================

    private void UpdateChoiceInteraction()
    {
        Camera cam = Camera.main;
        if (cam == null) return;

        Ray ray = new Ray(cam.transform.position, cam.transform.forward);
        bool lookingAtA = false;
        bool lookingAtB = false;

        if (Physics.Raycast(ray, out RaycastHit hit, 5f, ~0, QueryTriggerInteraction.Collide))
        {
            if (hit.collider.gameObject == optionAButton ||
                hit.collider.transform.IsChildOf(optionAButton.transform))
            {
                lookingAtA = true;
            }
            else if (hit.collider.gameObject == optionBButton ||
                     hit.collider.transform.IsChildOf(optionBButton.transform))
            {
                lookingAtB = true;
            }
        }

        if (lookingAtA)
        {
            statusText.text = "Press [E] to choose EASY ROOM (75% chance of door)";
            if (Input.GetKeyDown(KeyCode.E))
                OnChooseOptionA();
        }
        else if (lookingAtB)
        {
            statusText.text = "COMING SOON - Requires Premium DLC ($49.99)";
        }
        else
        {
            statusText.text = "Look at a button and press [E]";
        }
    }

    private void OnChooseOptionA()
    {
        isChoosing = false;
        attemptCount++;

        bool hasDoor = Random.value <= doorSpawnChance;

        Debug.Log($"[Level11] Attempt #{attemptCount}: Door spawned = {hasDoor}");
    }

    private void MovePlayerTo(Vector3 position, Quaternion rotation)
    {
        if (GameManager.Instance != null && GameManager.Instance.CurrentPlayer != null)
        {
            CharacterController cc = GameManager.Instance.CurrentPlayer.GetComponent<CharacterController>();
            if (cc != null) cc.enabled = false;
            GameManager.Instance.CurrentPlayer.transform.position = position;
            GameManager.Instance.CurrentPlayer.transform.rotation = rotation;
            if (cc != null) cc.enabled = true;
        }
    }

    private void ShowChoicePhase()
    {
        isChoosing = true;
        UpdateAttemptDisplay();
    }

    private void UpdateAttemptDisplay()
    {
        if (attemptText != null)
            attemptText.text = $"Attempts: {attemptCount}";
    }

    // =========================================================================
    // HUD
    // =========================================================================

    private void CreateHUD()
    {
        GameObject canvasObj = new GameObject("RNG_HUD");
        canvasObj.transform.SetParent(transform);
        hudCanvas = canvasObj.AddComponent<Canvas>();
        UIHelper.ConfigureCanvas(hudCanvas, sortingOrder: 15);

        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);

        // Status text (center)
        statusText = MakeText(canvasObj.transform, "StatusText", "",
            new Vector2(0.15f, 0.4f), new Vector2(0.85f, 0.5f),
            24, Color.white, TextAnchor.MiddleCenter);

        // Attempt counter
        attemptText = MakeText(canvasObj.transform, "AttemptText", "Attempts: 0",
            new Vector2(0.02f, 0.92f), new Vector2(0.2f, 0.97f),
            20, new Color(0.7f, 0.7f, 0.8f), TextAnchor.MiddleLeft);

        // Taunt text
        tauntText = MakeText(canvasObj.transform, "TauntText", "",
            new Vector2(0.1f, 0.55f), new Vector2(0.9f, 0.65f),
            22, new Color(1f, 0.5f, 0.5f), TextAnchor.MiddleCenter);
        tauntText.fontStyle = FontStyle.Italic;

        // Option labels (screen-space, since we can't use TextMesh easily)
        optionALabel = MakeText(canvasObj.transform, "OptionALabel",
            "OPTION A\nEasy Room\n75% chance of door",
            new Vector2(0.15f, 0.7f), new Vector2(0.45f, 0.88f),
            18, new Color(0.3f, 0.8f, 0.3f), TextAnchor.MiddleCenter);

        optionBLabel = MakeText(canvasObj.transform, "OptionBLabel",
            "OPTION B\nDifficult Room\nCOMING SOON\n(Requires Premium DLC)",
            new Vector2(0.55f, 0.7f), new Vector2(0.85f, 0.88f),
            18, new Color(0.6f, 0.4f, 0.4f), TextAnchor.MiddleCenter);
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

    private void CreateBoxChild(GameObject parent, string name, Vector3 pos, Vector3 scale, Material mat)
    {
        GameObject obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
        obj.name = name;
        obj.transform.SetParent(parent.transform);
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
