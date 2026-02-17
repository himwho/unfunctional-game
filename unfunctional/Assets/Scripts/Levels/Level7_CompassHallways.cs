using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// LEVEL 7: Compass following task level. A labyrinth of identical hallways with
/// a compass that always points to the exit. The joke is the hallways are one
/// straight path with fake branches that loop back -- the compass is completely
/// redundant. Near the end the compass goes haywire to create false panic.
///
/// Builds the hallway maze and compass HUD at runtime.
/// Attach to a root GameObject in the LEVEL7 scene.
/// </summary>
public class Level7_CompassHallways : LevelManager
{
    [Header("Hallway Generation")]
    public int segmentCount = 16;
    public float segmentLength = 8f;
    public float hallwayWidth = 4f;
    public float hallwayHeight = 3.5f;
    public float wallThickness = 0.2f;

    [Header("Door")]
    public DoorController doorController;

    [Header("Compass Behavior")]
    public float erraticStartDistance = 20f;
    public float erraticIntensity = 180f;

    // Runtime references
    private Canvas compassCanvas;
    private RectTransform compassNeedle;
    private Text compassLabel;
    private Text distanceText;
    private Image compassBg;

    private Transform exitPoint;
    private List<Vector3> segmentCenters = new List<Vector3>();
    private bool reachedEnd = false;

    // Hallway directions
    private Vector3[] directions = new Vector3[]
    {
        Vector3.forward,
        Vector3.right,
        Vector3.forward,
        Vector3.left,
        Vector3.forward,
        Vector3.forward,
        Vector3.right,
        Vector3.forward,
        Vector3.left,
        Vector3.forward,
        Vector3.right,
        Vector3.right,
        Vector3.forward,
        Vector3.left,
        Vector3.forward,
        Vector3.forward
    };

    protected override void Start()
    {
        base.Start();
        levelDisplayName = "The Compass Level";
        levelDescription = "Follow the compass. It knows the way. Probably.";
        needsPlayer = true;
        wantsCursorLocked = true;

        GenerateHallways();
        CreateCompassHUD();
        PlaceDoorAtEnd();
        PlaceHallwayLights();
        CreateDecoyBranches();
    }

    private void Update()
    {
        if (levelComplete) return;
        UpdateCompass();
        CheckExitProximity();
    }

    // =========================================================================
    // Hallway Generation
    // =========================================================================

    private void GenerateHallways()
    {
        Vector3 currentPos = Vector3.zero;
        Vector3 currentDir = Vector3.forward;

        // Make sure segmentCount does not exceed directions array
        int count = Mathf.Min(segmentCount, directions.Length);

        Material wallMat = CreateMaterial(new Color(0.45f, 0.45f, 0.42f));
        Material floorMat = CreateMaterial(new Color(0.3f, 0.3f, 0.32f));
        Material ceilingMat = CreateMaterial(new Color(0.5f, 0.5f, 0.48f));

        for (int i = 0; i < count; i++)
        {
            currentDir = directions[i];
            Vector3 segCenter = currentPos + currentDir * (segmentLength / 2f);
            segmentCenters.Add(segCenter);

            CreateHallwaySegment(i, segCenter, currentDir, wallMat, floorMat, ceilingMat);

            currentPos += currentDir * segmentLength;
        }

        // Create player spawn point
        GameObject spawnPoint = new GameObject("PlayerSpawnPoint");
        spawnPoint.transform.position = new Vector3(0f, 1f, -segmentLength * 0.3f);
        spawnPoint.transform.rotation = Quaternion.LookRotation(Vector3.forward);
        playerSpawnPoint = spawnPoint.transform;

        // Mark exit
        GameObject exitObj = new GameObject("ExitPoint");
        exitObj.transform.position = currentPos;
        exitPoint = exitObj.transform;

        // Exit trigger zone
        GameObject exitZone = new GameObject("ExitZone");
        exitZone.transform.position = currentPos;
        BoxCollider exitCol = exitZone.AddComponent<BoxCollider>();
        exitCol.size = new Vector3(hallwayWidth, hallwayHeight, 2f);
        exitCol.isTrigger = true;
        exitZone.AddComponent<Level7ExitTrigger>().levelManager = this;
    }

    private void CreateHallwaySegment(int index, Vector3 center, Vector3 direction,
        Material wallMat, Material floorMat, Material ceilingMat)
    {
        // Determine orientation
        bool isZAxis = Mathf.Abs(direction.z) > 0.5f;
        float lengthX = isZAxis ? hallwayWidth : segmentLength;
        float lengthZ = isZAxis ? segmentLength : hallwayWidth;

        string prefix = $"Seg{index}";

        // Floor
        GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
        floor.name = $"{prefix}_Floor";
        floor.transform.position = center + Vector3.down * (wallThickness / 2f);
        floor.transform.localScale = new Vector3(lengthX + wallThickness * 2, wallThickness, lengthZ + wallThickness * 2);
        floor.GetComponent<Renderer>().sharedMaterial = floorMat;

        // Ceiling
        GameObject ceiling = GameObject.CreatePrimitive(PrimitiveType.Cube);
        ceiling.name = $"{prefix}_Ceiling";
        ceiling.transform.position = center + Vector3.up * (hallwayHeight + wallThickness / 2f);
        ceiling.transform.localScale = new Vector3(lengthX + wallThickness * 2, wallThickness, lengthZ + wallThickness * 2);
        ceiling.GetComponent<Renderer>().sharedMaterial = ceilingMat;

        // Left wall
        Vector3 leftDir = Vector3.Cross(Vector3.up, direction).normalized;
        GameObject leftWall = GameObject.CreatePrimitive(PrimitiveType.Cube);
        leftWall.name = $"{prefix}_WallLeft";
        leftWall.transform.position = center + leftDir * (hallwayWidth / 2f) + Vector3.up * (hallwayHeight / 2f);
        if (isZAxis)
            leftWall.transform.localScale = new Vector3(wallThickness, hallwayHeight, segmentLength);
        else
            leftWall.transform.localScale = new Vector3(segmentLength, hallwayHeight, wallThickness);
        leftWall.GetComponent<Renderer>().sharedMaterial = wallMat;

        // Right wall
        GameObject rightWall = GameObject.CreatePrimitive(PrimitiveType.Cube);
        rightWall.name = $"{prefix}_WallRight";
        rightWall.transform.position = center - leftDir * (hallwayWidth / 2f) + Vector3.up * (hallwayHeight / 2f);
        rightWall.transform.localScale = leftWall.transform.localScale;
        rightWall.GetComponent<Renderer>().sharedMaterial = wallMat;
    }

    private void PlaceHallwayLights()
    {
        for (int i = 0; i < segmentCenters.Count; i++)
        {
            if (i % 2 != 0) continue; // Every other segment

            GameObject lightObj = new GameObject($"HallwayLight_{i}");
            lightObj.transform.position = segmentCenters[i] + Vector3.up * (hallwayHeight - 0.3f);
            Light light = lightObj.AddComponent<Light>();
            light.type = LightType.Point;
            light.range = segmentLength * 1.2f;
            light.intensity = 0.8f;
            light.color = new Color(1f, 0.95f, 0.85f);
        }
    }

    private void CreateDecoyBranches()
    {
        Material decoyMat = CreateMaterial(new Color(0.4f, 0.4f, 0.38f));

        // Place a few fake branches that are dead-ends (short alcoves)
        int[] decoySegments = { 3, 7, 11 };
        foreach (int seg in decoySegments)
        {
            if (seg >= segmentCenters.Count) continue;

            Vector3 center = segmentCenters[seg];
            Vector3 dir = directions[seg];
            Vector3 leftDir = Vector3.Cross(Vector3.up, dir).normalized;

            // Create a short alcove to the right
            Vector3 alcoveStart = center + leftDir * (hallwayWidth / 2f + 1f);
            Vector3 alcoveCenter = alcoveStart + leftDir * 2f;

            GameObject alcoveFloor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            alcoveFloor.name = $"DecoyFloor_{seg}";
            alcoveFloor.transform.position = alcoveCenter + Vector3.down * (wallThickness / 2f);
            alcoveFloor.transform.localScale = new Vector3(4f, wallThickness, 3f);
            alcoveFloor.GetComponent<Renderer>().sharedMaterial = decoyMat;

            // Dead-end wall
            GameObject deadEnd = GameObject.CreatePrimitive(PrimitiveType.Cube);
            deadEnd.name = $"DeadEnd_{seg}";
            deadEnd.transform.position = alcoveCenter + leftDir * 2.5f + Vector3.up * (hallwayHeight / 2f);
            deadEnd.transform.localScale = new Vector3(wallThickness, hallwayHeight, 3f);
            deadEnd.GetComponent<Renderer>().sharedMaterial = decoyMat;
        }
    }

    private void PlaceDoorAtEnd()
    {
        if (exitPoint == null) return;

        // If a door controller is already wired (from scene setup), just reposition it
        if (doorController != null)
        {
            doorController.transform.position = exitPoint.position;
            doorController.RecalculatePositions();
            return;
        }

        // Try to find one in the scene
        doorController = FindAnyObjectByType<DoorController>();
        if (doorController != null)
        {
            doorController.transform.position = exitPoint.position;
            doorController.RecalculatePositions();
        }
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

        // Compass background circle
        GameObject bgObj = new GameObject("CompassBG");
        bgObj.transform.SetParent(canvasObj.transform, false);
        compassBg = bgObj.AddComponent<Image>();
        compassBg.color = new Color(0.1f, 0.1f, 0.15f, 0.8f);
        RectTransform bgRect = bgObj.GetComponent<RectTransform>();
        bgRect.anchorMin = new Vector2(0.85f, 0.75f);
        bgRect.anchorMax = new Vector2(0.98f, 0.95f);
        bgRect.offsetMin = Vector2.zero;
        bgRect.offsetMax = Vector2.zero;

        // Compass needle (a thin colored bar)
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

        // Near the end, add erratic behavior
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

    // =========================================================================
    // Helpers
    // =========================================================================

    private Material CreateMaterial(Color color)
    {
        Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        if (mat.shader == null || mat.shader.name == "Hidden/InternalErrorShader")
            mat = new Material(Shader.Find("Standard"));
        mat.color = color;
        return mat;
    }
}

/// <summary>
/// Simple trigger for the exit zone.
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
