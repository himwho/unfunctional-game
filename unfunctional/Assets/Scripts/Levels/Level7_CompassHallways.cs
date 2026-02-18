using UnityEngine;
using UnityEngine.UI;
using System.Collections;

/// <summary>
/// LEVEL 7: Compass following task level. A labyrinth of identical hallways with
/// a compass that always points to the exit. The joke is the hallways are one
/// straight path with fake branches that loop back -- the compass is completely
/// redundant. Near the end the compass goes haywire to create false panic.
///
/// The hallways are built manually in the scene. This script creates the compass
/// HUD at runtime and drives the needle toward the assigned exit point.
/// Attach to a root GameObject in the LEVEL7 scene.
/// </summary>
public class Level7_CompassHallways : LevelManager
{
    [Header("Door")]
    public DoorController doorController;

    [Header("Exit")]
    [Tooltip("Transform marking the exit location. The compass needle points here.")]
    public Transform exitPoint;

    [Header("Compass Behavior")]
    public float erraticStartDistance = 20f;
    public float erraticIntensity = 180f;

    // Runtime HUD references
    private Canvas compassCanvas;
    private RectTransform compassNeedle;
    private Text compassLabel;
    private Text distanceText;
    private Image compassBg;

    private bool reachedEnd = false;

    protected override void Start()
    {
        base.Start();
        levelDisplayName = "The Compass Level";
        levelDescription = "Follow the compass. It knows the way. Probably.";
        needsPlayer = true;
        wantsCursorLocked = true;

        CreateCompassHUD();
    }

    private void Update()
    {
        if (levelComplete) return;
        UpdateCompass();
        CheckExitProximity();
    }

    // =========================================================================
    // Compass HUD
    // =========================================================================

    private void CreateCompassHUD()
    {
        GameObject canvasObj = new GameObject("CompassHUD");
        canvasObj.transform.SetParent(transform);
        compassCanvas = canvasObj.AddComponent<Canvas>();
        compassCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        compassCanvas.sortingOrder = 15;

        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);

        // Compass background
        GameObject bgObj = new GameObject("CompassBG");
        bgObj.transform.SetParent(canvasObj.transform, false);
        compassBg = bgObj.AddComponent<Image>();
        compassBg.color = new Color(0.1f, 0.1f, 0.15f, 0.8f);
        RectTransform bgRect = bgObj.GetComponent<RectTransform>();
        bgRect.anchorMin = new Vector2(0.85f, 0.75f);
        bgRect.anchorMax = new Vector2(0.98f, 0.95f);
        bgRect.offsetMin = Vector2.zero;
        bgRect.offsetMax = Vector2.zero;

        // Compass needle (a thin colored bar that rotates)
        GameObject needleObj = new GameObject("CompassNeedle");
        needleObj.transform.SetParent(bgObj.transform, false);
        Image needleImage = needleObj.AddComponent<Image>();
        needleImage.color = new Color(0.9f, 0.2f, 0.2f);
        needleImage.raycastTarget = false;
        compassNeedle = needleObj.GetComponent<RectTransform>();
        compassNeedle.anchorMin = new Vector2(0.45f, 0.2f);
        compassNeedle.anchorMax = new Vector2(0.55f, 0.8f);
        compassNeedle.offsetMin = Vector2.zero;
        compassNeedle.offsetMax = Vector2.zero;
        compassNeedle.pivot = new Vector2(0.5f, 0.5f);

        // Label
        GameObject labelObj = new GameObject("CompassLabel");
        labelObj.transform.SetParent(canvasObj.transform, false);
        compassLabel = labelObj.AddComponent<Text>();
        compassLabel.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        compassLabel.fontSize = 16;
        compassLabel.alignment = TextAnchor.MiddleCenter;
        compassLabel.color = new Color(0.7f, 0.7f, 0.8f);
        compassLabel.text = "COMPASS";
        compassLabel.raycastTarget = false;
        RectTransform labelRect = labelObj.GetComponent<RectTransform>();
        labelRect.anchorMin = new Vector2(0.85f, 0.72f);
        labelRect.anchorMax = new Vector2(0.98f, 0.76f);
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;

        // Distance indicator
        GameObject distObj = new GameObject("DistanceText");
        distObj.transform.SetParent(canvasObj.transform, false);
        distanceText = distObj.AddComponent<Text>();
        distanceText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        distanceText.fontSize = 14;
        distanceText.alignment = TextAnchor.MiddleCenter;
        distanceText.color = new Color(0.5f, 0.5f, 0.6f);
        distanceText.raycastTarget = false;
        RectTransform distRect = distObj.GetComponent<RectTransform>();
        distRect.anchorMin = new Vector2(0.85f, 0.68f);
        distRect.anchorMax = new Vector2(0.98f, 0.72f);
        distRect.offsetMin = Vector2.zero;
        distRect.offsetMax = Vector2.zero;
    }

    private void UpdateCompass()
    {
        Camera cam = Camera.main;
        if (cam == null || exitPoint == null || compassNeedle == null) return;

        Vector3 toExit = exitPoint.position - cam.transform.position;
        toExit.y = 0;
        float dist = toExit.magnitude;

        // Base angle: direction to exit relative to camera forward
        float targetAngle = 0f;
        if (toExit.sqrMagnitude > 0.01f)
        {
            float worldAngle = Mathf.Atan2(toExit.x, toExit.z) * Mathf.Rad2Deg;
            float camAngle = cam.transform.eulerAngles.y;
            targetAngle = worldAngle - camAngle;
        }

        // Near the end, add erratic behavior to cause false panic
        if (dist < erraticStartDistance)
        {
            float erraticAmount = 1f - (dist / erraticStartDistance);
            targetAngle += Mathf.Sin(Time.time * 8f) * erraticIntensity * erraticAmount;
            targetAngle += Mathf.Sin(Time.time * 13f) * erraticIntensity * erraticAmount * 0.5f;

            compassBg.color = Color.Lerp(
                new Color(0.1f, 0.1f, 0.15f, 0.8f),
                new Color(0.3f, 0.1f, 0.1f, 0.9f),
                erraticAmount);
        }

        compassNeedle.localRotation = Quaternion.Euler(0, 0, -targetAngle);

        if (distanceText != null)
            distanceText.text = $"{dist:F0}m to exit";
    }

    // =========================================================================
    // Exit Detection
    // =========================================================================

    private void CheckExitProximity()
    {
        Camera cam = Camera.main;
        if (cam == null || exitPoint == null) return;

        float dist = Vector3.Distance(cam.transform.position, exitPoint.position);
        if (dist < 3f && !reachedEnd)
        {
            reachedEnd = true;
            OnReachedEnd();
        }
    }

    public void OnReachedEnd()
    {
        if (levelComplete) return;

        if (doorController != null)
        {
            doorController.OpenDoor();
            StartCoroutine(CompleteAfterDelay(2f));
        }
        else
        {
            CompleteLevel();
        }
    }

    private IEnumerator CompleteAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        CompleteLevel();
    }
}

/// <summary>
/// Trigger collider for the exit zone. Place on a GameObject with a trigger
/// collider near the exit point in the scene.
/// </summary>
public class Level7ExitTrigger : MonoBehaviour
{
    public Level7_CompassHallways levelManager;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") || other.GetComponent<CharacterController>() != null)
        {
            if (levelManager != null)
                levelManager.OnReachedEnd();
        }
    }
}
