using UnityEngine;
using UnityEngine.UI;
using System.Collections;

/// <summary>
/// LEVEL 11: Bad RNG door. Two buttons: Option A has 75% chance of spawning
/// a door, Option B is "DLC locked" and never works. If Option A fails, the
/// player must restart the ENTIRE GAME (no level restart allowed).
///
/// Expects "Button1" and "Button2" parent GameObjects in the scene, each with
/// "ButtonTop" and "ButtonBase" children. The press animation targets "ButtonTop".
/// </summary>
public class Level11_BadRNG : LevelManager
{
    [Header("Level 11 - Bad RNG")]
    [Tooltip("Option A door (left) - 75% chance to open")]
    public DoorController optionADoor;

    [Tooltip("Option B door (right) - DLC locked, never opens")]
    public DoorController optionBDoor;

    [Header("Buttons (scene objects — parent with ButtonTop / ButtonBase children)")]
    [Tooltip("Option A button parent in scene")]
    public GameObject buttonA;

    [Tooltip("Option B button parent in scene")]
    public GameObject buttonB;

    private Transform buttonATop;
    private Transform buttonBTop;

    [Header("RNG Settings")]
    [Range(0f, 1f)]
    public float doorSpawnChance = 0.75f;

    [Header("Timing")]
    public float tauntDisplayTime = 2.5f;

    // Runtime UI
    private Canvas hudCanvas;
    private Text statusText;
    private Text tauntText;
    private Text attemptText;

    // Front wall collider (disabled when door opens so player can walk through)
    private BoxCollider frontWallCollider;

    // State
    private int attemptCount = 0;
    private bool hasChosen = false;
    private bool doorOpened = false;

    // Billboard labels
    private Transform[] labelTransforms = new Transform[2];
    private int labelCount = 0;

    private readonly string[] tauntMessages = new string[]
    {
        "No door this time! Better luck next time!",
        "The RNG gods frown upon you.",
        "NOPE. Try again... from the beginning.",
        "So close! (Not really.)",
        "75% chance and you STILL missed? Incredible.",
        "Maybe the door is a metaphor. Nah, it's just bad luck.",
        "Door machine broke. Understandable, have a nice day.",
        "You'd think 75% would be generous. You'd be wrong.",
        "The door sends its regards. From somewhere else.",
        "Have you tried being luckier?",
        "Error 404: Door not found.",
        "You're in the 25%. Congratulations?",
        "The universe has spoken. It said 'no'.",
        "Back to the start with you!",
        "This is why we can't have nice things.",
        "Your luck stat is clearly a dump stat.",
    };

    protected override void Start()
    {
        base.Start();
        levelDisplayName = "RNG Casino";
        levelDescription = "Pick a door. Your odds are... concerning.";
        needsPlayer = true;
        wantsCursorLocked = true;

        FindFrontWall();
        ActivateDoors();
        DisableRestartButton();
        SetupButtons();
        CreateHUD();
        EnsureSpawnPoint();
    }

    private void Update()
    {
        if (levelComplete) return;

        if (!hasChosen)
        {
            UpdateButtonInteraction();
        }
        else if (doorOpened)
        {
            CheckLevelCompletion();
        }
    }

    // =========================================================================
    // Setup
    // =========================================================================

    private void FindFrontWall()
    {
        foreach (GameObject root in gameObject.scene.GetRootGameObjects())
        {
            if (root.name == "Wall (2)")
            {
                frontWallCollider = root.GetComponent<BoxCollider>();
                break;
            }
        }
    }

    private void ActivateDoors()
    {
        if (optionADoor != null)
        {
            optionADoor.gameObject.SetActive(true);
            optionADoor.unlockMethod = DoorController.UnlockMethod.None;
            optionADoor.ApplyUnlockMethod();
        }

        if (optionBDoor != null)
        {
            optionBDoor.gameObject.SetActive(true);
            optionBDoor.unlockMethod = DoorController.UnlockMethod.None;
            optionBDoor.ApplyUnlockMethod();
        }
    }

    private void EnsureSpawnPoint()
    {
        if (playerSpawnPoint == null)
        {
            GameObject sp = new GameObject("PlayerSpawnPoint");
            sp.transform.position = new Vector3(0f, 1f, -2f);
            sp.transform.rotation = Quaternion.LookRotation(Vector3.forward);
            playerSpawnPoint = sp.transform;
        }
    }

    private void DisableRestartButton()
    {
        GamePauseMenu pauseMenu = FindAnyObjectByType<GamePauseMenu>();
        if (pauseMenu != null)
        {
            pauseMenu.SetRestartButtonEnabled(false);
            Debug.Log("[Level11] Restart button disabled - no mercy!");
        }
    }

    private void ReenableRestartButton()
    {
        GamePauseMenu pauseMenu = FindAnyObjectByType<GamePauseMenu>();
        if (pauseMenu != null)
            pauseMenu.SetRestartButtonEnabled(true);
    }

    // =========================================================================
    // Buttons
    // =========================================================================

    private void SetupButtons()
    {
        if (buttonA != null)
        {
            buttonATop = buttonA.transform.Find("ButtonTop");
            SpawnLabel(buttonA, "OPTION A\nEASY ROOM\n75% Chance");
        }

        if (buttonB != null)
        {
            buttonBTop = buttonB.transform.Find("ButtonTop");
            SpawnLabel(buttonB, "OPTION B\nDIFFICULT ROOM\nCOMING SOON");
        }
    }

    private void SpawnLabel(GameObject button, string label)
    {
        if (button.transform.Find("Label") != null) return;

        GameObject labelObj = new GameObject("Label");
        labelObj.transform.SetParent(button.transform, true);
        labelObj.transform.position = button.transform.position + Vector3.up * 1.3f;
        TextMesh tm = labelObj.AddComponent<TextMesh>();
        tm.text = label;
        tm.fontSize = 28;
        tm.characterSize = 0.08f;
        tm.alignment = TextAlignment.Center;
        tm.anchor = TextAnchor.LowerCenter;
        tm.color = Color.white;
        Font font = UIHelper.GetDefaultFont();
        if (font != null)
        {
            tm.font = font;
            MeshRenderer mr = labelObj.GetComponent<MeshRenderer>();
            if (mr != null && font.material != null)
                mr.material = font.material;
        }

        if (labelCount < labelTransforms.Length)
            labelTransforms[labelCount++] = labelObj.transform;
    }

    private void LateUpdate()
    {
        Camera cam = Camera.main;
        if (cam == null) return;

        for (int i = 0; i < labelCount; i++)
        {
            if (labelTransforms[i] == null) continue;
            Vector3 dir = labelTransforms[i].position - cam.transform.position;
            dir.y = 0f;
            if (dir.sqrMagnitude > 0.001f)
                labelTransforms[i].rotation = Quaternion.LookRotation(dir);
        }
    }

    // =========================================================================
    // Interaction
    // =========================================================================

    private void UpdateButtonInteraction()
    {
        Camera cam = Camera.main;
        if (cam == null) return;

        Ray ray = new Ray(cam.transform.position, cam.transform.forward);
        bool lookingAtA = false;
        bool lookingAtB = false;

        if (Physics.Raycast(ray, out RaycastHit hit, 5f))
        {
            Transform hitTransform = hit.collider.transform;

            if (buttonA != null && hitTransform.IsChildOf(buttonA.transform))
                lookingAtA = true;
            else if (buttonB != null && hitTransform.IsChildOf(buttonB.transform))
                lookingAtB = true;
        }

        if (lookingAtA)
        {
            statusText.text = "Press [E] - EASY ROOM (75% chance)";
            statusText.color = new Color(0.5f, 1f, 0.5f);

            if (Input.GetKeyDown(KeyCode.E))
                OnChooseOptionA();
        }
        else if (lookingAtB)
        {
            statusText.text = "COMING SOON - Premium DLC ($49.99)";
            statusText.color = new Color(1f, 0.5f, 0.5f);

            if (Input.GetKeyDown(KeyCode.E))
                OnChooseOptionB();
        }
        else
        {
            statusText.text = "Choose a button and press [E]";
            statusText.color = Color.white;
        }
    }

    private void OnChooseOptionA()
    {
        hasChosen = true;
        attemptCount++;
        UpdateAttemptDisplay();

        if (buttonATop != null)
            StartCoroutine(ButtonPressAnimation(buttonATop));

        bool success = Random.value <= doorSpawnChance;
        Debug.Log($"[Level11] Attempt #{attemptCount}: Success = {success}");

        if (success)
        {
            doorOpened = true;
            statusText.text = "The door opens! Walk through!";
            statusText.color = new Color(0.3f, 1f, 0.5f);
            tauntText.text = "";

            StartCoroutine(DoorFallSequence());
        }
        else
        {
            statusText.text = "FAILURE!";
            statusText.color = new Color(1f, 0.3f, 0.3f);
            tauntText.text = tauntMessages[Random.Range(0, tauntMessages.Length)];

            if (optionADoor != null)
                optionADoor.ShakeDoor(0.5f, 0.05f);

            StartCoroutine(RestartGameSequence());
        }
    }

    private void OnChooseOptionB()
    {
        statusText.text = "This content requires PREMIUM DLC!";
        statusText.color = new Color(1f, 0.6f, 0.2f);
        tauntText.text = "Purchase for only $49.99! (Not really available)";

        if (buttonBTop != null)
            StartCoroutine(ButtonPressAnimation(buttonBTop));

        if (optionBDoor != null)
            optionBDoor.ShakeDoor(0.3f, 0.02f);

        StartCoroutine(ResetAfterDLCTaunt());
    }

    private IEnumerator ButtonPressAnimation(Transform button)
    {
        if (button == null) yield break;

        Vector3 originalPos = button.localPosition;
        Vector3 pressedPos = originalPos + Vector3.down * 0.05f;

        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime * 8f;
            button.localPosition = Vector3.Lerp(originalPos, pressedPos, t);
            yield return null;
        }

        yield return new WaitForSeconds(0.1f);

        t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime * 4f;
            button.localPosition = Vector3.Lerp(pressedPos, originalPos, t);
            yield return null;
        }

        button.localPosition = originalPos;
    }

    private IEnumerator DoorFallSequence()
    {
        yield return new WaitForSeconds(0.5f);

        if (optionADoor != null && optionADoor.doorPanel != null)
        {
            GameObject panel = optionADoor.doorPanel;
            panel.transform.SetParent(null);

            Rigidbody rb = panel.GetComponent<Rigidbody>();
            if (rb == null)
                rb = panel.AddComponent<Rigidbody>();

            rb.isKinematic = false;
            rb.mass = 40f;
            rb.useGravity = true;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode.Continuous;

            if (panel.GetComponent<Collider>() == null)
                panel.AddComponent<BoxCollider>();

            Vector3 topOfDoor = panel.transform.position + Vector3.up * 1.4f;
            Vector3 pushDir = -panel.transform.forward;
            rb.AddForceAtPosition(pushDir * 120f, topOfDoor, ForceMode.Impulse);
        }

        if (frontWallCollider != null)
            frontWallCollider.enabled = false;
    }

    private IEnumerator ResetAfterDLCTaunt()
    {
        yield return new WaitForSeconds(1.5f);
        tauntText.text = "";
    }

    private IEnumerator RestartGameSequence()
    {
        yield return new WaitForSeconds(tauntDisplayTime);

        statusText.text = "Restarting game...";
        tauntText.text = "No restarts allowed. Back to the beginning!";

        yield return new WaitForSeconds(1f);

        if (GameManager.Instance != null)
            GameManager.Instance.LoadLevel(0);
    }

    // =========================================================================
    // Level Completion
    // =========================================================================

    private void CheckLevelCompletion()
    {
        if (GameManager.Instance == null || GameManager.Instance.CurrentPlayer == null)
            return;

        Vector3 playerPos = GameManager.Instance.CurrentPlayer.transform.position;
        float doorZ = optionADoor != null ? optionADoor.transform.position.z : 3.8f;

        if (playerPos.z > doorZ + 0.5f)
            CompleteLevel();
    }

    public override void CompleteLevel()
    {
        if (levelComplete) return;

        statusText.text = "LEVEL COMPLETE!";
        statusText.color = new Color(0.3f, 1f, 0.5f);
        tauntText.text = $"Cleared in {attemptCount} attempt{(attemptCount == 1 ? "" : "s")}!";

        base.CompleteLevel();
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

        statusText = CreateText(canvasObj.transform, "StatusText",
            "Choose a button and press [E]",
            new Vector2(0.1f, 0.35f), new Vector2(0.9f, 0.45f),
            28, Color.white, TextAnchor.MiddleCenter);

        attemptText = CreateText(canvasObj.transform, "AttemptText", "Attempts: 0",
            new Vector2(0.02f, 0.92f), new Vector2(0.25f, 0.98f),
            22, new Color(0.7f, 0.7f, 0.8f), TextAnchor.MiddleLeft);

        tauntText = CreateText(canvasObj.transform, "TauntText", "",
            new Vector2(0.1f, 0.2f), new Vector2(0.9f, 0.32f),
            22, new Color(1f, 0.6f, 0.6f), TextAnchor.MiddleCenter);
        tauntText.fontStyle = FontStyle.Italic;
    }

    private void UpdateAttemptDisplay()
    {
        if (attemptText != null)
            attemptText.text = $"Attempts: {attemptCount}";
    }

    private Text CreateText(Transform parent, string name, string content,
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
    // Cleanup
    // =========================================================================

    protected override void OnDestroy()
    {
        ReenableRestartButton();
        base.OnDestroy();
    }
}
