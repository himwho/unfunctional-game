using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// LEVEL 13 – 2nd-Person Shooter
///
/// The core gimmick: the rendering camera sits on the SHOULDER of whichever NPC
/// is closest to the player's crosshair. The player still controls their own
/// character (WASD + mouse) but sees themselves from the enemy's perspective.
///
/// Features:
///   • Wave-based NPC spawning with escalating difficulty
///   • Dynamic camera switching (closest NPC to aim ray)
///   • Player shooting via raycast from their own character
///   • Full HUD: crosshair, health bar, ammo, kills, wave counter
///   • Simple arena generated at runtime (can be replaced by scene geometry)
///
/// Attach to a root GameObject in the LEVEL13 scene.
/// </summary>
public class Level13_SecondPersonShooter : LevelManager
{
    // =========================================================================
    // Inspector — tweakable in the Unity Editor
    // =========================================================================

    [Header("Level 13 — 2nd Person Shooter")]
    [Tooltip("Base multiplier for NPC spawn counts. Wave 1 spawns 1×, 2×, 3× this value per sub-wave.")]
    public int npcWaveBaseMultiplier = 1;
    public int totalWaves = 1;
    [Tooltip("Assign a BoxCollider to define the NPC spawn region. If empty, spawns around the player using the radius fields below.")]
    public BoxCollider spawnZone;
    public float spawnRadius = 28f;
    public float minSpawnDistance = 12f;

    [Header("Player Combat")]
    public int playerMaxHealth = 100;
    public int playerDamage = 25;
    public int maxAmmo = 30;
    public float fireRate = 6f;       // shots per second
    public float reloadTime = 1.8f;

    [Header("NPC")]
    [Tooltip("Optional NPC body prefab. If empty, a capsule+sphere is generated at runtime.")]
    public GameObject npcPrefab;
    public int npcHealth = 80;
    public float npcMoveSpeed = 3f;
    public float npcDetectionRange = 30f;
    public float npcAttackRange = 18f;
    public float npcFireRate = 0.6f;
    public int npcDamage = 8;

    [Header("Gun Model")]
    [Tooltip("Assign the AK47 prefab (or any gun model) here.")]
    public GameObject gunPrefab;
    public Vector3 gunPositionOffset = new Vector3(0.3f, 0.3f, 0.35f);
    public Vector3 gunRotationOffset = Vector3.zero;
    public float gunScale = 1f;
    [Tooltip("Local offset from the gun model's pivot to the barrel tip. X=right, Y=up, Z=forward.")]
    public Vector3 muzzleTipOffset = new Vector3(0f, 0f, 1.5f);

    [Header("Camera")]
    [Tooltip("Shoulder cam offset relative to each NPC. Adjust live in play mode.")]
    public Vector3 shoulderCamOffset = new Vector3(0.55f, 1.6f, -0.4f);
    [Tooltip("How fast the 2nd-person camera lerps to the new shoulder.")]
    public float cameraSmoothSpeed = 14f;
    public float cameraLookSmooth = 12f;
    [Tooltip("Height above the arena center for the cinematic overview camera.")]
    public float cinematicHeight = 20f;
    [Tooltip("How long the cinematic overview lasts before NPCs spawn (seconds).")]
    public float cinematicDuration = 3f;

    // =========================================================================
    // Runtime state
    // =========================================================================

    // Player refs
    private PlayerController playerController;
    private Transform playerTransform;
    private Camera playerCamera;           // disabled; its transform is still used for aim
    private Transform playerCamTransform;  // shorthand for playerCamera.transform

    // 2nd-person camera (cloned from the player camera to inherit all URP settings)
    private Camera secondPersonCam;

    // All gun model instances (player + NPCs) so inspector tweaks apply live
    private List<Transform> allGuns = new List<Transform>();
    private Transform playerGun;
    private Transform playerMuzzlePoint;

    // NPCs
    private List<SecondPersonNPC> activeNPCs = new List<SecondPersonNPC>();
    public List<SecondPersonNPC> ActiveNPCs => activeNPCs;
    private SecondPersonNPC currentViewNPC;

    // Player combat
    private int playerHealth;
    private int currentAmmo;
    private int kills;
    private int currentWave;
    private int subWave;           // 0, 1, 2 within each wave (spawns 1, 2, 3 × multiplier)
    private int waveMultiplier;    // doubles each wave: 1, 2, 4, 8...
    private float fireCooldown;
    private bool isReloading;
    private bool gameOver;
    private bool cinematicMode;
    private float cinematicTimer;

    // Shot visual
    private LineRenderer playerShotLine;

    // HUD
    private Canvas hudCanvas;
    private Image crosshairDot;
    private Image crosshairH, crosshairV;
    private Image healthBarFill;
    private Text healthText;
    private Text ammoText;
    private Text killText;
    private Text waveText;
    private Text modeLabel;
    private Text centerMsg;       // reload / game over / wave clear
    private Image damageFlash;
    private Image hitMarkerImg;
    private Text debugNPCStateText;

    // Arena objects (so we can clean up)
    private List<GameObject> arenaObjects = new List<GameObject>();

    // Player CharacterController — level-specific overrides, restored on destroy
    private CharacterController playerCC;
    private float originalCCHeight;
    private float originalCCRadius;
    private Vector3 originalCCCenter;

    // =========================================================================
    // Lifecycle
    // =========================================================================

    protected override void Start()
    {
        needsPlayer = true;
        wantsCursorLocked = true;
        base.Start();

        levelDisplayName = "2nd Person Shooter";
        levelDescription = "See yourself from their eyes.";

        StartCoroutine(SetupAfterSpawn());
    }

    private IEnumerator SetupAfterSpawn()
    {
        // Wait for GameManager to spawn the player
        float timeout = 5f;
        float elapsed = 0f;
        while (elapsed < timeout)
        {
            playerController = FindAnyObjectByType<PlayerController>();
            if (playerController != null) break;
            elapsed += Time.deltaTime;
            yield return null;
        }

        if (playerController == null)
        {
            Debug.LogError("[Level13] PlayerController not found!");
            yield break;
        }

        playerTransform = playerController.transform;
        playerCamera = playerController.GetComponentInChildren<Camera>();
        playerCamTransform = playerCamera != null ? playerCamera.transform : playerTransform;

        Debug.Log($"[Level13] Player found at {playerTransform.position}, " +
                  $"camera={(playerCamera != null ? playerCamera.name : "NULL")}");

        // Tag the player so NPC hit-detection can find them
        playerTransform.gameObject.tag = "Player";

        // Adjust CharacterController for this level to prevent floating
        playerCC = playerController.GetComponent<CharacterController>();
        if (playerCC != null)
        {
            originalCCHeight = playerCC.height;
            originalCCRadius = playerCC.radius;
            originalCCCenter = playerCC.center;
            playerCC.height = 2f;
            playerCC.radius = 0.4f;
            playerCC.center = Vector3.up;
        }

        // Initialize state
        playerHealth = playerMaxHealth;
        currentAmmo = maxAmmo;
        kills = 0;
        currentWave = 0;

        // If the scene has no floor, create a simple arena
        if (!Physics.Raycast(playerTransform.position + Vector3.up * 5f, Vector3.down, 50f))
        {
            CreateArena();
        }

        SetupSecondPersonCamera();
        AttachGunToPlayer();

        try { BuildHUD(); }
        catch (System.Exception e) { Debug.LogError($"[Level13] HUD setup failed: {e}"); }

        StartCoroutine(CinematicThenSpawn(true));
    }

    protected override void OnDestroy()
    {
        if (playerCamera != null) playerCamera.enabled = true;
        if (secondPersonCam != null) Destroy(secondPersonCam.gameObject);
        if (playerGun != null) Destroy(playerGun.gameObject);

        // Restore player CharacterController to original settings
        if (playerCC != null)
        {
            playerCC.height = originalCCHeight;
            playerCC.radius = originalCCRadius;
            playerCC.center = originalCCCenter;
        }

        foreach (var obj in arenaObjects)
        {
            if (obj != null) Destroy(obj);
        }

        base.OnDestroy();
    }

    // =========================================================================
    // Update
    // =========================================================================

    private void Update()
    {
        if (gameOver || levelComplete) return;
        if (playerTransform == null) return;

        UpdateCameraSwitch();
        if (!cinematicMode)
        {
            HandleShooting();
            HandleReload();
        }
        UpdateHUD();
    }

    private void LateUpdate()
    {
        // Apply inspector gun values live so they can be tweaked at runtime
        for (int i = allGuns.Count - 1; i >= 0; i--)
        {
            if (allGuns[i] == null) { allGuns.RemoveAt(i); continue; }

            Transform gun = allGuns[i];
            gun.localPosition = gunPositionOffset;
            gun.localScale = Vector3.one * gunScale;

            // NPC guns inherit body rotation; only apply the static offset
            if (gun != playerGun)
                gun.localRotation = Quaternion.Euler(gunRotationOffset);
        }

        // Player gun overrides rotation to follow the aim direction
        if (playerGun != null && playerCamTransform != null)
        {
            playerGun.rotation = Quaternion.LookRotation(playerCamTransform.forward, Vector3.up)
                                 * Quaternion.Euler(gunRotationOffset);
        }
    }

    // =========================================================================
    // 2nd-Person Camera System
    // =========================================================================

    private void SetupSecondPersonCamera()
    {
        if (playerCamera == null)
        {
            Debug.LogError("[Level13] Player has no camera — cannot set up 2nd-person view.");
            return;
        }

        Debug.Log($"[Level13] Player camera found: {playerCamera.name}, " +
                  $"enabled={playerCamera.enabled}, depth={playerCamera.depth}");

        // Clone the player's camera object so the copy inherits all URP /
        // UniversalAdditionalCameraData / renderer settings automatically.
        GameObject camClone = Instantiate(playerCamera.gameObject);
        camClone.name = "SecondPersonCamera";
        camClone.transform.SetParent(null);

        // Remove AudioListener from clone to avoid duplicate warnings
        AudioListener al = camClone.GetComponent<AudioListener>();
        if (al != null) Destroy(al);

        secondPersonCam = camClone.GetComponent<Camera>();
        secondPersonCam.enabled = true;
        secondPersonCam.depth = playerCamera.depth + 1;
        secondPersonCam.fieldOfView = 65f;

        // Start behind the player at head height
        Vector3 startPos = playerTransform.position
                           + Vector3.up * 1.8f
                           - playerTransform.forward * 2f;
        camClone.transform.position = startPos;
        camClone.transform.LookAt(playerTransform.position + Vector3.up * 1.2f);

        // Disable the player's own camera (keep its transform for aim direction;
        // PlayerController.HandleLook still rotates it even when disabled).
        playerCamera.enabled = false;

        Debug.Log($"[Level13] 2nd-person camera set up at {startPos}, " +
                  $"depth={secondPersonCam.depth}");
    }

    private void AttachGunToPlayer()
    {
        if (gunPrefab == null || playerTransform == null) return;

        GameObject gun = AttachGunModel(playerTransform, gunPositionOffset);
        if (gun != null)
        {
            playerGun = gun.transform;
            Transform mp = gun.transform.Find("MuzzlePoint");
            if (mp != null) playerMuzzlePoint = mp;
        }
    }

    /// <summary>
    /// Instantiates the gun prefab as a child of <paramref name="parent"/>
    /// at the given local offset, applying the inspector scale and rotation.
    /// </summary>
    private GameObject AttachGunModel(Transform parent, Vector3 localPos)
    {
        if (gunPrefab == null) return null;

        GameObject gun = Instantiate(gunPrefab, parent);
        gun.name = "GunModel";
        gun.transform.localPosition = localPos;
        gun.transform.localRotation = Quaternion.Euler(gunRotationOffset);
        gun.transform.localScale = Vector3.one * gunScale;

        foreach (Collider col in gun.GetComponentsInChildren<Collider>())
            Destroy(col);

        allGuns.Add(gun.transform);
        return gun;
    }

    /// <summary>
    /// Each frame: cast a ray from the player's aim direction, find the NPC
    /// whose world position is closest to that ray, and smoothly move the
    /// rendering camera to that NPC's shoulder.
    /// </summary>
    private void UpdateCameraSwitch()
    {
        if (secondPersonCam == null) return;

        // Cinematic overview mode — orbit above the arena
        if (cinematicMode)
        {
            cinematicTimer += Time.deltaTime;

            Vector3 center = playerTransform.position;
            if (spawnZone != null) center = spawnZone.bounds.center;

            float orbitAngle = cinematicTimer * 30f; // 30 degrees per second
            float orbitRadius = spawnZone != null
                ? Mathf.Max(spawnZone.bounds.extents.x, spawnZone.bounds.extents.z) * 0.6f
                : spawnRadius * 0.4f;

            Vector3 offset = new Vector3(
                Mathf.Sin(orbitAngle * Mathf.Deg2Rad) * orbitRadius,
                cinematicHeight,
                Mathf.Cos(orbitAngle * Mathf.Deg2Rad) * orbitRadius
            );

            Transform cam = secondPersonCam.transform;
            cam.position = Vector3.Lerp(cam.position, center + offset, Time.deltaTime * 3f);
            cam.LookAt(center);
            return;
        }

        // Build the aim ray from the player's (disabled) camera transform
        Ray aimRay = new Ray(
            playerCamTransform.position,
            playerCamTransform.forward
        );

        // Find closest NPC to the ray
        SecondPersonNPC best = null;
        float bestScore = float.MaxValue;

        for (int i = activeNPCs.Count - 1; i >= 0; i--)
        {
            SecondPersonNPC npc = activeNPCs[i];
            if (npc == null || npc.isDead)
            {
                activeNPCs.RemoveAt(i);
                continue;
            }

            float rayDist = DistancePointToRay(aimRay, npc.transform.position);
            float worldDist = Vector3.Distance(aimRay.origin, npc.transform.position);

            // Combined score: perpendicular distance is dominant, with a tiny
            // preference for physically closer NPCs when ray-distances tie.
            float score = rayDist + worldDist * 0.01f;

            if (score < bestScore)
            {
                bestScore = score;
                best = npc;
            }
        }

        if (best != null && best != currentViewNPC)
        {
            // Show the NPC we're leaving
            if (currentViewNPC != null && !currentViewNPC.isDead)
                currentViewNPC.SetVisible(true);

            currentViewNPC = best;

            // Hide the NPC we're now viewing from
            currentViewNPC.SetVisible(false);

            // Being looked at triggers aggro immediately
            currentViewNPC.AlertToPlayer();
        }

        // Smoothly follow the shoulder cam
        if (currentViewNPC != null && currentViewNPC.shoulderCamPoint != null)
        {
            Transform cam = secondPersonCam.transform;
            Transform shoulder = currentViewNPC.shoulderCamPoint;

            cam.position = Vector3.Lerp(cam.position, shoulder.position,
                                        Time.deltaTime * cameraSmoothSpeed);

            Vector3 lookTarget = playerTransform.position + Vector3.up * 1.2f;
            Quaternion targetRot = Quaternion.LookRotation(lookTarget - cam.position);
            cam.rotation = Quaternion.Slerp(cam.rotation, targetRot,
                                            Time.deltaTime * cameraLookSmooth);
        }
        else
        {
            // Fallback: stay close behind the player at head height so the
            // camera doesn't end up outside enclosed arenas.
            Vector3 fallback = playerTransform.position
                               - playerTransform.forward * 2.5f
                               + Vector3.up * 1.8f;
            secondPersonCam.transform.position = Vector3.Lerp(
                secondPersonCam.transform.position, fallback, Time.deltaTime * 5f);
            secondPersonCam.transform.LookAt(playerTransform.position + Vector3.up * 1.2f);
        }
    }

    private static float DistancePointToRay(Ray ray, Vector3 point)
    {
        Vector3 toPoint = point - ray.origin;
        float dot = Vector3.Dot(toPoint, ray.direction);
        if (dot < 0f) return toPoint.magnitude;           // behind the ray
        Vector3 closest = ray.origin + ray.direction * dot;
        return Vector3.Distance(point, closest);
    }

    // =========================================================================
    // Player Combat
    // =========================================================================

    private void HandleShooting()
    {
        fireCooldown -= Time.deltaTime;

        if (Input.GetMouseButton(0) && fireCooldown <= 0f && currentAmmo > 0 && !isReloading)
        {
            fireCooldown = 1f / Mathf.Max(fireRate, 0.1f);
            currentAmmo--;
            PlayerShoot();

            if (currentAmmo <= 0)
                StartCoroutine(DoReload());
        }
    }

    private void PlayerShoot()
    {
        Vector3 origin = playerCamTransform.position;
        Vector3 dir = playerCamTransform.forward;

        Vector3 endPoint = origin + dir * 100f;
        bool hitNPC = false;

        if (SecondPersonNPC.RaycastIgnoringWindows(origin, dir, out RaycastHit hit, 200f))
        {
            endPoint = hit.point;

            SecondPersonNPC npc = hit.collider.GetComponentInParent<SecondPersonNPC>();
            if (npc != null && !npc.isDead)
            {
                npc.TakeDamage(playerDamage, dir);
                hitNPC = true;
            }
        }

        Vector3 muzzle;
        if (playerMuzzlePoint != null)
            muzzle = playerMuzzlePoint.position;
        else if (playerGun != null)
            muzzle = playerGun.TransformPoint(muzzleTipOffset);
        else
            muzzle = playerTransform.position + Vector3.up * 1.3f
                     + playerTransform.forward * 0.4f
                     + playerTransform.right * 0.25f;

        ShowPlayerShotLine(muzzle, endPoint);

        if (hitNPC)
            ShowHitMarker();
    }

    private void HandleReload()
    {
        if (Input.GetKeyDown(KeyCode.R) && currentAmmo < maxAmmo && !isReloading)
            StartCoroutine(DoReload());
    }

    private IEnumerator DoReload()
    {
        isReloading = true;
        if (centerMsg != null) { centerMsg.text = "RELOADING..."; centerMsg.gameObject.SetActive(true); }

        yield return new WaitForSeconds(reloadTime);

        currentAmmo = maxAmmo;
        isReloading = false;
        if (centerMsg != null) centerMsg.gameObject.SetActive(false);
    }

    /// <summary>Called by <see cref="SecondPersonNPC"/> when it hits the player.</summary>
    public void DamagePlayer(int dmg)
    {
        if (gameOver || levelComplete) return;

        playerHealth = Mathf.Max(0, playerHealth - dmg);
        StartCoroutine(FlashDamageOverlay());

        if (playerHealth <= 0)
            GameOver(false);
    }

    // =========================================================================
    // NPC Management
    // =========================================================================

    /// <summary>Called by <see cref="SecondPersonNPC"/> on death.</summary>
    public void OnNPCKilled(SecondPersonNPC npc)
    {
        kills++;
        activeNPCs.Remove(npc);

        if (currentViewNPC == npc)
        {
            npc.SetVisible(true);
            currentViewNPC = null;
        }

        int alive = 0;
        foreach (var n in activeNPCs)
            if (n != null && !n.isDead) alive++;

        if (alive == 0)
        {
            if (subWave < 2)
                StartNextSubWave();
            else
                StartCoroutine(OnWaveCleared());
        }
    }

    private void StartNextSubWave()
    {
        subWave++;
        int count = (subWave + 1) * waveMultiplier * npcWaveBaseMultiplier;
        for (int i = 0; i < count; i++)
            SpawnNPC();
    }

    private IEnumerator OnWaveCleared()
    {
        playerHealth = playerMaxHealth;

        if (centerMsg != null) { centerMsg.text = "WAVE CLEAR!"; centerMsg.gameObject.SetActive(true); }

        yield return new WaitForSeconds(2f);

        if (centerMsg != null) centerMsg.gameObject.SetActive(false);

        if (currentWave >= totalWaves)
        {
            GameOver(true);
        }
        else
        {
            StartCoroutine(CinematicThenSpawn(false));
        }
    }

    private IEnumerator CinematicThenSpawn(bool isFirstWave)
    {
        cinematicMode = true;
        cinematicTimer = 0f;

        if (centerMsg != null)
        {
            centerMsg.text = isFirstWave ? "SPAWNING ENEMIES..." : "NEXT WAVE INCOMING...";
            centerMsg.color = Color.white;
            centerMsg.gameObject.SetActive(true);
        }

        yield return new WaitForSeconds(cinematicDuration);

        cinematicMode = false;
        if (centerMsg != null) centerMsg.gameObject.SetActive(false);

        StartNextWave();
    }

    private void StartNextWave()
    {
        currentWave++;
        if (currentWave == 1)
            waveMultiplier = 1;
        else
            waveMultiplier *= 2;

        subWave = -1; // StartNextSubWave increments to 0
        StartNextSubWave();

        if (waveText != null)
            waveText.text = $"WAVE  {currentWave} / {totalWaves}";
    }

    private void SpawnNPC()
    {
        Vector3 spawnPos = FindValidSpawnPosition();

        GameObject npcObj;
        Renderer bodyRenderer;

        if (npcPrefab != null)
        {
            npcObj = Instantiate(npcPrefab, spawnPos, Quaternion.identity);
            npcObj.name = "NPC_Enemy";

            // Strip player-specific components immediately so they can't
            // process input or render even for a single frame.
            foreach (var pc in npcObj.GetComponentsInChildren<PlayerController>())
                DestroyImmediate(pc);
            foreach (var cam in npcObj.GetComponentsInChildren<Camera>())
            {
                cam.enabled = false;
                DestroyImmediate(cam.gameObject);
            }
            foreach (var al in npcObj.GetComponentsInChildren<AudioListener>())
                DestroyImmediate(al);

            bodyRenderer = npcObj.GetComponentInChildren<Renderer>();

            if (gunPrefab != null)
                AttachGunModel(npcObj.transform, gunPositionOffset);
        }
        else
        {
            npcObj = CreateFallbackNPCVisual(spawnPos);
            bodyRenderer = npcObj.GetComponent<Renderer>();
        }

        // Reuse existing CharacterController if the prefab has one (e.g. player
        // prefab), otherwise add a new one. Never destroy + re-add since
        // Destroy is deferred and AddComponent would fail on duplicates.
        CharacterController cc = npcObj.GetComponent<CharacterController>();
        if (cc == null) cc = npcObj.AddComponent<CharacterController>();
        cc.height = 2.2f;
        cc.radius = 0.6f;
        cc.center = Vector3.up * 1.1f;
        cc.slopeLimit = 45f;
        cc.stepOffset = 0.4f;

        SecondPersonNPC npc = npcObj.AddComponent<SecondPersonNPC>();
        npc.SetBodyRenderer(bodyRenderer);

        int waveBonus = currentWave - 1;
        npc.maxHealth       = npcHealth + waveBonus * 10;
        npc.moveSpeed       = npcMoveSpeed + waveBonus * 0.3f;
        npc.detectionRange  = npcDetectionRange;
        npc.attackRange     = npcAttackRange;
        npc.fireRate        = npcFireRate + waveBonus * 0.1f;
        npc.damage          = npcDamage + waveBonus * 2;
        npc.accuracy        = Mathf.Clamp01(0.35f + waveBonus * 0.1f);

        npc.Initialize(playerTransform, this);
        activeNPCs.Add(npc);
    }

    /// <summary>
    /// Picks a random point around the player and validates that it has arena
    /// floor beneath it (downward raycast). Retries up to 30 times with
    /// shrinking radius, then falls back to a position near the player.
    /// </summary>
    private const float MIN_NPC_SPACING = 4f;

    private Vector3 FindValidSpawnPosition()
    {
        for (int attempt = 0; attempt < 30; attempt++)
        {
            Vector3 candidate;

            {
                float radius = Random.Range(minSpawnDistance, spawnRadius);
                if (attempt > 10) radius *= 0.5f;

                Vector2 circle = Random.insideUnitCircle.normalized * radius;
                candidate = playerTransform.position + new Vector3(circle.x, 0, circle.y);

                // If a spawn zone is defined, clamp the candidate inside its bounds
                if (spawnZone != null)
                {
                    Bounds b = spawnZone.bounds;
                    candidate.x = Mathf.Clamp(candidate.x, b.min.x, b.max.x);
                    candidate.y = b.center.y;
                    candidate.z = Mathf.Clamp(candidate.z, b.min.z, b.max.z);
                }
            }

            // Must have floor beneath
            if (!Physics.Raycast(candidate + Vector3.up * 10f, Vector3.down, out RaycastHit ground, 30f))
                continue;

            candidate.y = ground.point.y;

            // Reject positions too close to existing NPCs
            bool tooClose = false;
            foreach (var npc in activeNPCs)
            {
                if (npc == null) continue;
                if (Vector3.Distance(candidate, npc.transform.position) < MIN_NPC_SPACING)
                {
                    tooClose = true;
                    break;
                }
            }
            if (tooClose) continue;

            return candidate;
        }

        Debug.LogWarning("[Level13] Could not find valid NPC spawn position, spawning near player.");
        return playerTransform.position + playerTransform.forward * 3f;
    }

    private GameObject CreateFallbackNPCVisual(Vector3 position)
    {
        GameObject body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        body.name = "NPC_Enemy";
        body.transform.position = position + Vector3.up * 1f;

        Color npcColor = new Color(
            Random.Range(0.5f, 0.9f),
            Random.Range(0.1f, 0.3f),
            Random.Range(0.1f, 0.3f));
        SetMaterialColor(body.GetComponent<Renderer>(), npcColor);

        GameObject head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        head.name = "Head";
        head.transform.SetParent(body.transform);
        head.transform.localPosition = new Vector3(0f, 0.85f, 0f);
        head.transform.localScale = new Vector3(0.55f, 0.55f, 0.55f);
        SetMaterialColor(head.GetComponent<Renderer>(), npcColor * 0.8f);
        Destroy(head.GetComponent<Collider>());

        if (gunPrefab != null)
        {
            AttachGunModel(body.transform, gunPositionOffset);
        }
        else
        {
            GameObject gun = GameObject.CreatePrimitive(PrimitiveType.Cube);
            gun.name = "Gun";
            gun.transform.SetParent(body.transform);
            gun.transform.localPosition = new Vector3(0.4f, 0.3f, 0.35f);
            gun.transform.localScale = new Vector3(0.08f, 0.08f, 0.45f);
            SetMaterialColor(gun.GetComponent<Renderer>(), new Color(0.25f, 0.25f, 0.28f));
            Destroy(gun.GetComponent<Collider>());
        }

        return body;
    }

    // =========================================================================
    // Game Over / Win
    // =========================================================================

    private void GameOver(bool won)
    {
        gameOver = true;

        if (centerMsg != null)
        {
            centerMsg.gameObject.SetActive(true);
            if (won)
            {
                centerMsg.text = "ALL WAVES CLEARED!\n\nLevel Complete.";
                centerMsg.color = Color.green;
            }
            else
            {
                centerMsg.text = "YOU DIED\n\nPress [R] to restart";
                centerMsg.color = Color.red;
            }
        }

        if (won)
        {
            StartCoroutine(WinAfterDelay(3f));
        }
        else
        {
            StartCoroutine(ListenForRestart());
        }
    }

    private IEnumerator WinAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);

        if (playerCamera != null) playerCamera.enabled = true;
        if (secondPersonCam != null) Destroy(secondPersonCam.gameObject);

        CompleteLevel();
    }

    private IEnumerator ListenForRestart()
    {
        while (true)
        {
            if (Input.GetKeyDown(KeyCode.R))
            {
                if (GameManager.Instance != null)
                    GameManager.Instance.ReloadCurrentLevel();
                yield break;
            }
            yield return null;
        }
    }

    // =========================================================================
    // HUD (built at runtime)
    // =========================================================================

    private void BuildHUD()
    {
        // Canvas
        GameObject canvasObj = new GameObject("ShooterHUD");
        canvasObj.transform.SetParent(transform);
        hudCanvas = canvasObj.AddComponent<Canvas>();
        UIHelper.ConfigureCanvas(hudCanvas, sortingOrder: 30);
        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        canvasObj.AddComponent<GraphicRaycaster>();

        // -- "2ND PERSON MODE" comedy label (top-left) --
        modeLabel = MakeText(canvasObj, "ModeLabel",
            "<b>2ND PERSON MODE</b>",
            new Vector2(0.01f, 0.93f), new Vector2(0.25f, 0.99f),
            20, new Color(1f, 0.4f, 0.4f, 0.9f), TextAnchor.UpperLeft);
        modeLabel.supportRichText = true;

        // -- Wave counter (top-center) --
        waveText = MakeText(canvasObj, "WaveText",
            "",
            new Vector2(0.35f, 0.93f), new Vector2(0.65f, 0.99f),
            24, new Color(0.9f, 0.9f, 0.5f), TextAnchor.MiddleCenter);

        // -- Kills (top-right) --
        killText = MakeText(canvasObj, "KillText",
            "KILLS: 0",
            new Vector2(0.78f, 0.93f), new Vector2(0.99f, 0.99f),
            20, Color.white, TextAnchor.UpperRight);

        // -- Crosshair (center) -- disabled for now
        // crosshairDot = MakeImage(canvasObj, "CrosshairDot",
        //     new Vector2(0.498f, 0.494f), new Vector2(0.502f, 0.506f),
        //     Color.white);
        //
        // crosshairH = MakeImage(canvasObj, "CrosshairH",
        //     new Vector2(0.485f, 0.499f), new Vector2(0.515f, 0.501f),
        //     new Color(1, 1, 1, 0.7f));
        //
        // crosshairV = MakeImage(canvasObj, "CrosshairV",
        //     new Vector2(0.4995f, 0.48f), new Vector2(0.5005f, 0.52f),
        //     new Color(1, 1, 1, 0.7f));

        // -- Hit marker (flashes over crosshair) --
        hitMarkerImg = MakeImage(canvasObj, "HitMarker",
            new Vector2(0.49f, 0.485f), new Vector2(0.51f, 0.515f),
            new Color(1f, 0.2f, 0.2f, 0f));

        // -- Health bar (bottom-left) --
        MakeText(canvasObj, "HealthLabel", "HP",
            new Vector2(0.02f, 0.04f), new Vector2(0.06f, 0.08f),
            16, Color.white, TextAnchor.MiddleLeft);

        Image hpBg = MakeImage(canvasObj, "HealthBarBG",
            new Vector2(0.06f, 0.045f), new Vector2(0.26f, 0.075f),
            new Color(0.15f, 0.15f, 0.15f, 0.8f));

        GameObject hpFillObj = new GameObject("HealthBarFill");
        hpFillObj.transform.SetParent(hpBg.transform, false);
        healthBarFill = hpFillObj.AddComponent<Image>();
        healthBarFill.color = new Color(0.2f, 0.8f, 0.3f);
        healthBarFill.type = Image.Type.Filled;
        healthBarFill.fillMethod = Image.FillMethod.Horizontal;
        healthBarFill.fillAmount = 1f;
        RectTransform hpFillRt = hpFillObj.GetComponent<RectTransform>();
        hpFillRt.anchorMin = Vector2.zero;
        hpFillRt.anchorMax = Vector2.one;
        hpFillRt.offsetMin = Vector2.zero;
        hpFillRt.offsetMax = Vector2.zero;

        healthText = MakeText(canvasObj, "HealthText",
            $"{playerMaxHealth}",
            new Vector2(0.06f, 0.04f), new Vector2(0.26f, 0.08f),
            14, Color.white, TextAnchor.MiddleCenter);

        // -- Ammo (bottom-right) --
        ammoText = MakeText(canvasObj, "AmmoText",
            $"{maxAmmo} / {maxAmmo}",
            new Vector2(0.8f, 0.04f), new Vector2(0.98f, 0.08f),
            20, Color.white, TextAnchor.MiddleRight);

        // -- Center message (reload / wave clear / game over) --
        centerMsg = MakeText(canvasObj, "CenterMsg", "",
            new Vector2(0.2f, 0.35f), new Vector2(0.8f, 0.65f),
            40, Color.white, TextAnchor.MiddleCenter);
        centerMsg.gameObject.SetActive(false);

        // -- Debug: NPC state (bottom-center) --
        debugNPCStateText = MakeText(canvasObj, "DebugNPCState", "",
            new Vector2(0.3f, 0.01f), new Vector2(0.7f, 0.06f),
            18, Color.yellow, TextAnchor.MiddleCenter);

        // -- Full-screen damage flash --
        damageFlash = MakeImage(canvasObj, "DamageFlash",
            Vector2.zero, Vector2.one,
            new Color(0.8f, 0.1f, 0.05f, 0f));
        damageFlash.raycastTarget = false;
    }

    private void UpdateHUD()
    {
        if (healthBarFill != null)
        {
            float hpPct = (float)playerHealth / playerMaxHealth;
            healthBarFill.fillAmount = hpPct;
            healthBarFill.color = Color.Lerp(Color.red, new Color(0.2f, 0.8f, 0.3f), hpPct);
        }
        if (healthText != null)
            healthText.text = $"{playerHealth}";

        if (ammoText != null)
            ammoText.text = isReloading ? "..." : $"{currentAmmo} / {maxAmmo}";

        if (killText != null)
            killText.text = $"KILLS: {kills}";

        if (debugNPCStateText != null)
        {
            if (currentViewNPC != null && !currentViewNPC.isDead)
                debugNPCStateText.text = $"NPC: {currentViewNPC.CurrentStateName} | HP: {currentViewNPC.currentHealth}/{currentViewNPC.maxHealth}";
            else
                debugNPCStateText.text = "NPC: ---";
        }
    }

    // =========================================================================
    // Visual Effects
    // =========================================================================

    private void ShowPlayerShotLine(Vector3 from, Vector3 to)
    {
        if (playerShotLine == null)
        {
            GameObject lineObj = new GameObject("PlayerShotLine");
            lineObj.transform.SetParent(playerTransform);
            playerShotLine = lineObj.AddComponent<LineRenderer>();
            playerShotLine.startWidth = 0.02f;
            playerShotLine.endWidth = 0.015f;
            playerShotLine.material = new Material(Shader.Find("Sprites/Default"));
            playerShotLine.startColor = new Color(1f, 0.9f, 0.3f);
            playerShotLine.endColor = new Color(1f, 0.5f, 0.1f, 0.2f);
            playerShotLine.positionCount = 2;
        }

        playerShotLine.enabled = true;
        playerShotLine.SetPosition(0, from);
        playerShotLine.SetPosition(1, to);
        StartCoroutine(HideLine(playerShotLine, 0.05f));
    }

    private IEnumerator HideLine(LineRenderer lr, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (lr != null) lr.enabled = false;
    }

    private void ShowHitMarker()
    {
        if (hitMarkerImg != null)
            StartCoroutine(FlashHitMarker());
    }

    private IEnumerator FlashHitMarker()
    {
        hitMarkerImg.color = new Color(1f, 0.2f, 0.2f, 0.9f);
        float t = 0;
        while (t < 0.15f)
        {
            t += Time.deltaTime;
            float a = Mathf.Lerp(0.9f, 0f, t / 0.15f);
            hitMarkerImg.color = new Color(1f, 0.2f, 0.2f, a);
            yield return null;
        }
        hitMarkerImg.color = new Color(1f, 0.2f, 0.2f, 0f);
    }

    private IEnumerator FlashDamageOverlay()
    {
        if (damageFlash == null) yield break;
        damageFlash.color = new Color(0.8f, 0.1f, 0.05f, 0.35f);
        float t = 0;
        while (t < 0.3f)
        {
            t += Time.deltaTime;
            float a = Mathf.Lerp(0.35f, 0f, t / 0.3f);
            damageFlash.color = new Color(0.8f, 0.1f, 0.05f, a);
            yield return null;
        }
        damageFlash.color = new Color(0.8f, 0.1f, 0.05f, 0f);
    }

    // =========================================================================
    // Arena Generation (fallback if scene has no geometry)
    // =========================================================================

    private void CreateArena()
    {
        Vector3 center = playerTransform.position - Vector3.up * 0.5f;

        // Floor
        GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
        floor.name = "Arena_Floor";
        floor.transform.position = center;
        floor.transform.localScale = new Vector3(8, 1, 8); // 80 × 80 units
        SetMaterialColor(floor.GetComponent<Renderer>(), new Color(0.28f, 0.3f, 0.32f));
        arenaObjects.Add(floor);

        // Perimeter walls (4 sides)
        float wallDist = 38f;
        float wallHeight = 6f;
        Vector3[] wallDirs = { Vector3.forward, Vector3.back, Vector3.left, Vector3.right };
        foreach (var d in wallDirs)
        {
            GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.name = "Arena_Wall";
            wall.transform.position = center + d * wallDist + Vector3.up * wallHeight * 0.5f;
            bool xAligned = Mathf.Abs(d.x) > 0.5f;
            wall.transform.localScale = xAligned
                ? new Vector3(1f, wallHeight, wallDist * 2)
                : new Vector3(wallDist * 2, wallHeight, 1f);
            SetMaterialColor(wall.GetComponent<Renderer>(), new Color(0.22f, 0.22f, 0.24f));
            arenaObjects.Add(wall);
        }

        // Scatter cover objects
        for (int i = 0; i < 12; i++)
        {
            float angle = i * Mathf.PI * 2f / 12f + Random.Range(-0.2f, 0.2f);
            float radius = Random.Range(8f, 22f);
            Vector3 pos = center + new Vector3(Mathf.Cos(angle) * radius, 0, Mathf.Sin(angle) * radius);
            pos.y = center.y + 0.5f;

            GameObject cover = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cover.name = $"Cover_{i}";
            cover.transform.position = pos + Vector3.up * Random.Range(0.5f, 1.5f);
            cover.transform.localScale = new Vector3(
                Random.Range(1.5f, 3f),
                Random.Range(1.5f, 3.5f),
                Random.Range(1.5f, 3f));
            cover.transform.rotation = Quaternion.Euler(0, Random.Range(0, 360), 0);
            SetMaterialColor(cover.GetComponent<Renderer>(), new Color(
                Random.Range(0.3f, 0.45f),
                Random.Range(0.28f, 0.38f),
                Random.Range(0.25f, 0.35f)));
            arenaObjects.Add(cover);
        }

        Debug.Log("[Level13] Simple arena created at runtime.");
    }

    // =========================================================================
    // UI Helpers
    // =========================================================================

    private Text MakeText(GameObject parent, string name, string content,
        Vector2 anchorMin, Vector2 anchorMax,
        int fontSize, Color color, TextAnchor alignment)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent.transform, false);
        RectTransform rt = obj.AddComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        Text txt = obj.AddComponent<Text>();
        txt.font = UIHelper.GetDefaultFont();
        txt.fontSize = fontSize;
        txt.alignment = alignment;
        txt.color = color;
        txt.text = content;
        txt.raycastTarget = false;
        return txt;
    }

    private Image MakeImage(GameObject parent, string name,
        Vector2 anchorMin, Vector2 anchorMax, Color color)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent.transform, false);
        RectTransform rt = obj.AddComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        Image img = obj.AddComponent<Image>();
        img.color = color;
        img.raycastTarget = false;
        return img;
    }

    // =========================================================================
    // Material Helper (URP-safe)
    // =========================================================================

    private static void SetMaterialColor(Renderer r, Color c)
    {
        if (r == null) return;
        Material mat = r.material;
        mat.color = c;
        if (mat.HasProperty("_BaseColor"))
            mat.SetColor("_BaseColor", c);
    }
}
