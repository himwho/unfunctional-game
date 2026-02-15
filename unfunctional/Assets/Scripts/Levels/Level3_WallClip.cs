using UnityEngine;

/// <summary>
/// LEVEL 3: A simple room where the door doesn't open. 
/// The player must discover they can clip through a specific wall section
/// by exploiting a deliberately broken collider.
/// 
/// Attach to a root GameObject in the LEVEL3 scene.
/// </summary>
public class Level3_WallClip : LevelManager
{
    [Header("Level 3 - Wall Clip")]
    public GameObject normalDoor;             // The door that won't open (for show)
    public GameObject clippableWallSection;   // The wall section with broken collision
    public Collider clippableCollider;        // The collider to disable/make trigger
    public Transform exitTriggerPoint;        // Where the player ends up after clipping
    public BoxCollider exitZoneTrigger;       // Trigger zone on the other side of the wall

    [Header("Door Interaction")]
    public string doorLockedMessage = "The door appears to be stuck. Permanently.";
    public float doorInteractRange = 3f;

    [Header("Hints")]
    public float hintDelay = 30f;            // Seconds before showing first hint
    public string[] hints = new string[]
    {
        "Maybe the door isn't the only way...",
        "Some walls in older games aren't as solid as they look.",
        "Try walking into different wall sections.",
        "Look for where the textures don't quite line up.",
        "Just clip through the wall. Yes, really."
    };

    [Header("Visual")]
    public Material normalWallMaterial;
    public Material glitchyWallMaterial;     // Slightly different texture to hint at clippable section
    public float wallFlickerInterval = 5f;

    private int currentHintIndex = 0;
    private float hintTimer;
    private float flickerTimer;
    private bool playerInExitZone = false;
    private MeshRenderer clippableRenderer;

    protected override void Start()
    {
        base.Start();
        levelDisplayName = "The Room";
        levelDescription = "A simple room. Find a way through.";

        hintTimer = hintDelay;
        flickerTimer = wallFlickerInterval;

        // Set up the clippable wall - make its collider a trigger so player can walk through
        if (clippableCollider != null)
        {
            clippableCollider.isTrigger = true;
        }

        if (clippableWallSection != null)
        {
            clippableRenderer = clippableWallSection.GetComponent<MeshRenderer>();
        }

        // Set up exit trigger
        if (exitZoneTrigger != null)
        {
            exitZoneTrigger.isTrigger = true;
            // Ensure there's a trigger handler
            ExitZoneHandler handler = exitZoneTrigger.GetComponent<ExitZoneHandler>();
            if (handler == null)
            {
                handler = exitZoneTrigger.gameObject.AddComponent<ExitZoneHandler>();
            }
            handler.levelManager = this;
        }
    }

    private void Update()
    {
        if (levelComplete) return;

        // Hint timer
        hintTimer -= Time.deltaTime;
        if (hintTimer <= 0f && currentHintIndex < hints.Length)
        {
            ShowHint(hints[currentHintIndex]);
            currentHintIndex++;
            hintTimer = hintDelay * 0.7f; // Hints come faster over time
        }

        // Wall flicker to subtly hint at clippable section
        flickerTimer -= Time.deltaTime;
        if (flickerTimer <= 0f && clippableRenderer != null)
        {
            FlickerWall();
            flickerTimer = wallFlickerInterval + Random.Range(-1f, 2f);
        }

        // Door interaction
        CheckDoorInteraction();
    }

    private void CheckDoorInteraction()
    {
        if (InputManager.Instance == null || !InputManager.Instance.InteractPressed) return;

        // Raycast from camera to check if looking at door
        Camera cam = Camera.main;
        if (cam == null) return;

        Ray ray = cam.ScreenPointToRay(new Vector3(Screen.width / 2f, Screen.height / 2f, 0f));
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, doorInteractRange))
        {
            if (normalDoor != null && hit.collider.gameObject == normalDoor)
            {
                ShowMessage(doorLockedMessage);
            }
        }
    }

    private void FlickerWall()
    {
        if (clippableRenderer == null) return;

        // Briefly swap to glitchy material and back
        if (glitchyWallMaterial != null)
        {
            StartCoroutine(FlickerRoutine());
        }
    }

    private System.Collections.IEnumerator FlickerRoutine()
    {
        if (clippableRenderer == null || glitchyWallMaterial == null) yield break;

        Material original = clippableRenderer.material;
        clippableRenderer.material = glitchyWallMaterial;
        yield return new WaitForSeconds(0.1f);
        clippableRenderer.material = original;
        yield return new WaitForSeconds(0.05f);
        clippableRenderer.material = glitchyWallMaterial;
        yield return new WaitForSeconds(0.05f);
        clippableRenderer.material = original;
    }

    public void OnPlayerReachedExit()
    {
        if (levelComplete) return;

        Debug.Log("[Level3] Player clipped through the wall! Level complete.");
        ShowMessage("You found the way through! Just like the old days.");
        CompleteLevel();
    }

    private void ShowHint(string hint)
    {
        Debug.Log($"[Level3 Hint] {hint}");
        // TODO: Display on screen via UI text
    }

    private void ShowMessage(string message)
    {
        Debug.Log($"[Level3] {message}");
        // TODO: Display on screen via UI text
    }
}

/// <summary>
/// Simple trigger handler for the exit zone behind the clippable wall.
/// </summary>
public class ExitZoneHandler : MonoBehaviour
{
    [HideInInspector]
    public Level3_WallClip levelManager;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") || other.GetComponent<PlayerController>() != null)
        {
            if (levelManager != null)
            {
                levelManager.OnPlayerReachedExit();
            }
        }
    }
}
