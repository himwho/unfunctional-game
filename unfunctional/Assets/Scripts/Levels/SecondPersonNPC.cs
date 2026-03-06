using UnityEngine;
using System.Collections;

/// <summary>
/// NPC for the 2nd-person shooter level. Each NPC has:
///   • A simple patrol / chase / attack AI state machine
///   • A shoulder camera point the level camera can attach to
///   • Health, damage flash, death animation
///   • Shooting logic aimed at the player with configurable accuracy
///
/// Created and configured at runtime by Level13_SecondPersonShooter.
/// </summary>
public class SecondPersonNPC : MonoBehaviour
{
    // =========================================================================
    // Stats (set by the spawner before Initialize)
    // =========================================================================

    [Header("Stats")]
    public int maxHealth = 80;
    public float moveSpeed = 3.5f;
    public float rotationSpeed = 5f;

    [Header("Combat")]
    public float detectionRange = 30f;
    public float attackRange = 18f;
    public float fireRate = 0.6f;     // accurate (damage) shots per second
    public float visualFireRate = 5f; // cosmetic shots per second (no damage)
    public int damage = 8;
    public float accuracy = 0.5f;     // 0 = wild, 1 = aimbot

    [Header("Patrol")]
    public float patrolRadius = 15f;
    public float waypointReachDist = 1.5f;

    // =========================================================================
    // Runtime (set by Initialize)
    // =========================================================================

    [HideInInspector] public int currentHealth;
    [HideInInspector] public Transform shoulderCamPoint;
    [HideInInspector] public bool isDead;

    private Transform player;
    private Level13_SecondPersonShooter levelManager;
    private Renderer bodyRenderer;
    private CharacterController controller;
    private Transform gunModelTransform;
    private Transform muzzlePoint;

    // AI
    private enum AIState { Idle, Patrol, Chase, Attack, Dead }
    private AIState state = AIState.Idle;
    private Vector3 spawnPosition;
    private Vector3 patrolTarget;
    private float stateTimer;
    private float idleDuration;
    private float fireCooldown;
    private float visualFireCooldown;

    // Physics
    private float verticalVelocity;
    private const float GRAVITY = -20f;
    private const float OBSTACLE_CHECK_DIST = 1.5f;
    private const float AVOIDANCE_ANGLE = 50f;

    private bool hasEngagedPlayer;

    // Visuals
    private LineRenderer shotLine;
    private Color originalColor;

    // =========================================================================
    // Public API
    // =========================================================================

    public void Initialize(Transform playerTransform, Level13_SecondPersonShooter manager)
    {
        player = playerTransform;
        levelManager = manager;
        currentHealth = maxHealth;
        isDead = false;
        spawnPosition = transform.position;

        controller = GetComponent<CharacterController>();

        Transform gm = transform.Find("GunModel");
        if (gm != null)
        {
            gunModelTransform = gm;
            Transform mp = gm.Find("MuzzlePoint");
            if (mp != null) muzzlePoint = mp;
        }

        GameObject shoulder = new GameObject("ShoulderCamPoint");
        shoulder.transform.SetParent(transform);
        shoulder.transform.localPosition = manager.shoulderCamOffset;
        shoulderCamPoint = shoulder.transform;

        // Cache body renderer
        bodyRenderer = GetComponent<Renderer>();
        if (bodyRenderer != null)
            originalColor = bodyRenderer.material.color;

        // Start with a brief idle
        state = AIState.Idle;
        idleDuration = Random.Range(0.5f, 2f);
        stateTimer = 0f;
        PickNewPatrolTarget();
    }

    public void SetBodyRenderer(Renderer r)
    {
        bodyRenderer = r;
        if (r != null) originalColor = r.material.color;
    }

    public void SetVisible(bool visible)
    {
        Transform gunModel = transform.Find("GunModel");
        foreach (Renderer r in GetComponentsInChildren<Renderer>())
        {
            if (gunModel != null && r.transform.IsChildOf(gunModel))
                continue;
            r.enabled = visible;
        }
    }

    public string CurrentStateName => state.ToString();

    public void AlertToPlayer()
    {
        if (isDead) return;
        hasEngagedPlayer = true;
        if (state == AIState.Idle || state == AIState.Patrol)
            state = AIState.Chase;
    }

    public void TakeDamage(int dmg, Vector3 hitDirection)
    {
        if (isDead) return;
        currentHealth -= dmg;

        // Flash red
        if (bodyRenderer != null)
            StartCoroutine(DamageFlash());

        // Alert to player
        if (state == AIState.Patrol || state == AIState.Idle)
            state = AIState.Chase;

        if (currentHealth <= 0)
            Die(hitDirection);
    }

    // =========================================================================
    // Update — AI state machine
    // =========================================================================

    private void Update()
    {
        if (isDead || player == null) return;

        float distToPlayer = Vector3.Distance(transform.position, player.position);

        if (shoulderCamPoint != null)
        {
            if (levelManager != null)
                shoulderCamPoint.localPosition = levelManager.shoulderCamOffset;

            shoulderCamPoint.LookAt(player.position + Vector3.up * 1.2f);
        }

        ApplyGravity();
        SeparateFromOtherNPCs();

        switch (state)
        {
            case AIState.Idle:    UpdateIdle(distToPlayer);   break;
            case AIState.Patrol:  UpdatePatrol(distToPlayer); break;
            case AIState.Chase:   UpdateChase(distToPlayer);  break;
            case AIState.Attack:  UpdateAttack(distToPlayer); break;
        }

        fireCooldown -= Time.deltaTime;
        visualFireCooldown -= Time.deltaTime;
    }

    // ── Physics ──────────────────────────────────────────────────────────────

    private void ApplyGravity()
    {
        if (controller == null) return;

        if (controller.isGrounded)
            verticalVelocity = -2f;
        else
            verticalVelocity += GRAVITY * Time.deltaTime;

        controller.Move(new Vector3(0f, verticalVelocity * Time.deltaTime, 0f));
    }

    /// <summary>
    /// Move toward a direction using the CharacterController, with obstacle
    /// avoidance via raycasts. Falls back to direct position if no CC.
    /// </summary>
    private void MoveToward(Vector3 desiredDir, float speed)
    {
        desiredDir.y = 0;
        if (desiredDir.sqrMagnitude < 0.001f) return;
        desiredDir.Normalize();

        Vector3 moveDir = desiredDir;

        if (controller != null)
        {
            Vector3 origin = transform.position + Vector3.up * 0.5f;
            if (Physics.Raycast(origin, desiredDir, OBSTACLE_CHECK_DIST))
            {
                // Try steering left and right to find a clear path
                Vector3 left = Quaternion.Euler(0, -AVOIDANCE_ANGLE, 0) * desiredDir;
                Vector3 right = Quaternion.Euler(0, AVOIDANCE_ANGLE, 0) * desiredDir;

                bool leftClear = !Physics.Raycast(origin, left, OBSTACLE_CHECK_DIST);
                bool rightClear = !Physics.Raycast(origin, right, OBSTACLE_CHECK_DIST);

                if (leftClear && !rightClear) moveDir = left;
                else if (rightClear && !leftClear) moveDir = right;
                else if (leftClear && rightClear) moveDir = (Random.value > 0.5f) ? left : right;
                else moveDir = -desiredDir; // back away if totally blocked
            }

            controller.Move(moveDir * speed * Time.deltaTime);
        }
        else
        {
            transform.position += moveDir * speed * Time.deltaTime;
        }

        RotateToward(moveDir);
    }

    // ── Idle ─────────────────────────────────────────────────────────────────

    private void UpdateIdle(float distToPlayer)
    {
        if (hasEngagedPlayer || distToPlayer < detectionRange)
        {
            hasEngagedPlayer = true;
            state = AIState.Chase;
            return;
        }

        stateTimer += Time.deltaTime;
        if (stateTimer >= idleDuration)
        {
            state = AIState.Patrol;
            PickNewPatrolTarget();
        }
    }

    // ── Patrol ───────────────────────────────────────────────────────────────

    private void UpdatePatrol(float distToPlayer)
    {
        if (hasEngagedPlayer || distToPlayer < detectionRange)
        {
            hasEngagedPlayer = true;
            state = AIState.Chase;
            return;
        }

        Vector3 dir = patrolTarget - transform.position;
        dir.y = 0;

        if (dir.magnitude < waypointReachDist)
        {
            state = AIState.Idle;
            stateTimer = 0f;
            idleDuration = Random.Range(1f, 3f);
            return;
        }

        MoveToward(dir, moveSpeed * 0.5f);
    }

    // ── Chase ────────────────────────────────────────────────────────────────

    private void UpdateChase(float distToPlayer)
    {
        hasEngagedPlayer = true;

        if (distToPlayer <= attackRange && HasLineOfSight())
        {
            state = AIState.Attack;
            return;
        }

        // Always close the distance and navigate around obstacles
        Vector3 dir = player.position - transform.position;
        MoveToward(dir, moveSpeed);
    }

    // ── Attack ───────────────────────────────────────────────────────────────

    private void UpdateAttack(float distToPlayer)
    {
        // If too far or lost line of sight, reposition
        if (distToPlayer > attackRange * 1.3f || !HasLineOfSight())
        {
            state = AIState.Chase;
            return;
        }

        Vector3 dir = (player.position - transform.position);
        dir.y = 0;
        if (dir.sqrMagnitude > 0.01f)
            RotateToward(dir.normalized);

        // Strafe using CharacterController
        float strafe = Mathf.Sin(Time.time * 1.5f + GetInstanceID()) * 0.5f;
        Vector3 strafeMove = transform.right * (strafe * moveSpeed * 0.3f * Time.deltaTime);
        if (controller != null)
            controller.Move(strafeMove);
        else
            transform.position += strafeMove;

        // Real damage shot at fireRate
        if (fireCooldown <= 0f)
        {
            Shoot(true);
            fireCooldown = 1f / Mathf.Max(fireRate, 0.1f);
            visualFireCooldown = 1f / Mathf.Max(visualFireRate, 0.1f);
        }
        // Visual-only suppressive fire between real shots
        else if (visualFireCooldown <= 0f)
        {
            Shoot(false);
            visualFireCooldown = 1f / Mathf.Max(visualFireRate, 0.1f);
        }
    }

    // =========================================================================
    // Combat
    // =========================================================================

    private void Shoot(bool canDamage)
    {
        if (player == null) return;

        Vector3 muzzlePos;
        if (muzzlePoint != null)
            muzzlePos = muzzlePoint.position;
        else if (gunModelTransform != null)
        {
            Vector3 tipOffset = (levelManager != null) ? levelManager.muzzleTipOffset : new Vector3(0f, 0f, 1.5f);
            muzzlePos = gunModelTransform.TransformPoint(tipOffset);
        }
        else
            muzzlePos = transform.position + Vector3.up * 1.3f + transform.forward * 0.5f;
        Vector3 targetPos = player.position + Vector3.up * 1f;
        Vector3 losDir = (targetPos - muzzlePos).normalized;
        float losDist = Vector3.Distance(muzzlePos, targetPos);

        // Line-of-sight check — don't fire if a solid wall is between us and the player
        if (RaycastIgnoringWindows(muzzlePos, losDir, out RaycastHit losHit, losDist))
        {
            if (losHit.collider.GetComponentInParent<PlayerController>() == null)
                return;
        }

        Vector3 shotDir = losDir;

        // Visual shots get extra spread so they miss; real shots use normal accuracy
        float spread = canDamage
            ? (1f - Mathf.Clamp01(accuracy)) * 0.15f
            : Random.Range(0.12f, 0.25f);
        shotDir += new Vector3(
            Random.Range(-spread, spread),
            Random.Range(-spread, spread),
            Random.Range(-spread, spread));
        shotDir.Normalize();

        float maxRange = Mathf.Min(attackRange, 30f);
        Vector3 endPoint = muzzlePos + shotDir * maxRange;

        if (RaycastIgnoringWindows(muzzlePos, shotDir, out RaycastHit hit, attackRange * 1.5f))
        {
            endPoint = hit.point;
            if (canDamage)
            {
                PlayerController pc = hit.collider.GetComponentInParent<PlayerController>();
                if (pc != null && levelManager != null)
                    levelManager.DamagePlayer(damage);
            }
        }

        ShowShotLine(muzzlePos, endPoint);
    }

    // =========================================================================
    // Death
    // =========================================================================

    private void Die(Vector3 hitDirection)
    {
        isDead = true;
        state = AIState.Dead;

        if (levelManager != null)
            levelManager.OnNPCKilled(this);

        // Disable CharacterController and any other collider so it stops blocking
        if (controller != null) controller.enabled = false;
        Collider col = GetComponent<Collider>();
        if (col != null && col != controller) col.enabled = false;

        StartCoroutine(DeathAnimation(hitDirection));
    }

    private IEnumerator DeathAnimation(Vector3 hitDir)
    {
        Vector3 fallDir = hitDir;
        fallDir.y = 0;
        if (fallDir.sqrMagnitude < 0.01f) fallDir = -transform.forward;
        fallDir.Normalize();

        Quaternion startRot = transform.rotation;
        Quaternion endRot = Quaternion.LookRotation(fallDir) * Quaternion.Euler(80, 0, 0);
        float elapsed = 0f;

        while (elapsed < 0.6f)
        {
            elapsed += Time.deltaTime;
            transform.rotation = Quaternion.Slerp(startRot, endRot, elapsed / 0.6f);
            transform.position += Vector3.down * (Time.deltaTime * 0.4f);
            yield return null;
        }

        yield return new WaitForSeconds(3f);
        Destroy(gameObject);
    }

    // =========================================================================
    // Visuals
    // =========================================================================

    private void ShowShotLine(Vector3 from, Vector3 to)
    {
        if (shotLine == null)
        {
            GameObject lineObj = new GameObject("ShotLine");
            lineObj.transform.SetParent(transform);
            shotLine = lineObj.AddComponent<LineRenderer>();
            shotLine.startWidth = 0.025f;
            shotLine.endWidth = 0.01f;
            shotLine.material = new Material(Shader.Find("Sprites/Default"));
            shotLine.startColor = Color.yellow;
            shotLine.endColor = new Color(1f, 0.6f, 0.1f, 0.3f);
            shotLine.positionCount = 2;
        }

        shotLine.enabled = true;
        shotLine.SetPosition(0, from);
        shotLine.SetPosition(1, to);
        StartCoroutine(HideShotLine());
    }

    private IEnumerator HideShotLine()
    {
        yield return new WaitForSeconds(0.06f);
        if (shotLine != null) shotLine.enabled = false;
    }

    private IEnumerator DamageFlash()
    {
        if (bodyRenderer == null) yield break;
        SetMaterialColor(bodyRenderer, Color.red);
        yield return new WaitForSeconds(0.1f);
        if (bodyRenderer != null)
            SetMaterialColor(bodyRenderer, originalColor);
    }

    // =========================================================================
    // Helpers
    // =========================================================================

    private const float SEPARATION_RADIUS = 3f;
    private const float SEPARATION_FORCE = 4f;

    private void SeparateFromOtherNPCs()
    {
        if (levelManager == null) return;

        Vector3 push = Vector3.zero;
        foreach (var other in levelManager.ActiveNPCs)
        {
            if (other == null || other == this || other.isDead) continue;

            Vector3 diff = transform.position - other.transform.position;
            diff.y = 0;
            float dist = diff.magnitude;

            if (dist < SEPARATION_RADIUS && dist > 0.01f)
                push += diff.normalized * (1f - dist / SEPARATION_RADIUS);
        }

        if (push.sqrMagnitude > 0.001f)
        {
            Vector3 sepMove = push.normalized * SEPARATION_FORCE * Time.deltaTime;
            if (controller != null)
                controller.Move(sepMove);
            else
                transform.position += sepMove;
        }
    }

    private bool HasLineOfSight()
    {
        if (player == null) return false;
        Vector3 eyePos = transform.position + Vector3.up * 1.5f;
        Vector3 targetPos = player.position + Vector3.up * 1f;
        Vector3 dir = targetPos - eyePos;
        float dist = dir.magnitude;

        return !RaycastIgnoringWindows(eyePos, dir.normalized, out RaycastHit hit, dist)
               || hit.collider.GetComponentInParent<PlayerController>() != null;
    }

    /// <summary>
    /// Casts a ray that passes through any collider tagged "Window".
    /// Returns true + the first non-window hit, or false if nothing solid was hit.
    /// </summary>
    public static bool RaycastIgnoringWindows(Vector3 origin, Vector3 dir, out RaycastHit solidHit, float maxDist)
    {
        RaycastHit[] hits = Physics.RaycastAll(origin, dir, maxDist);
        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        solidHit = default;
        foreach (var h in hits)
        {
            if (h.collider.gameObject.name.Contains("WindowGlass"))
                continue;
            solidHit = h;
            return true;
        }
        return false;
    }

    private void RotateToward(Vector3 dir)
    {
        if (dir.sqrMagnitude < 0.001f) return;
        Quaternion target = Quaternion.LookRotation(dir);
        transform.rotation = Quaternion.Slerp(transform.rotation, target, rotationSpeed * Time.deltaTime);
    }

    private void PickNewPatrolTarget()
    {
        Vector2 rnd = Random.insideUnitCircle * patrolRadius;
        patrolTarget = spawnPosition + new Vector3(rnd.x, 0, rnd.y);
    }

    private static void SetMaterialColor(Renderer r, Color c)
    {
        Material mat = r.material;
        mat.color = c;
        if (mat.HasProperty("_BaseColor"))
            mat.SetColor("_BaseColor", c);
    }
}
