using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Achievement-abuse finale: five distinct actions unlock the exit.
/// LEVEL12 is a historical asset name; this scene follows LEVEL_CRAFTING.
/// Uses the project material library and authored door, with five interactive exhibits.
/// </summary>
public class Level12_Achievements : LevelManager
{
    public const int RequiredCount = 5;
    private static readonly string[] ObjectiveIds = { "walk", "jump", "wait", "read", "button" };
    private static readonly string[] ObjectiveLabels =
    {
        "Walk 8 metres. Revolutionary.", "Jump once. Defy the floor.",
        "Stand still for 3 seconds.", "Read the plaque on the left [E].",
        "Press the red button on the right [E]."
    };
    private static readonly string[] TrivialAchievements =
    {
        "You have a computer", "Breathing manually", "Gravity still works",
        "Read an achievement", "Continued existing", "Achievement achievement",
        "Another second well spent", "Premium oxygen acquired"
    };

    [Min(0.5f)] public float trivialAchievementInterval = 3f;
    [Min(1f)] public float interactRange = 3.5f;

    private readonly HashSet<string> unlocked = new HashSet<string>();
    private readonly Queue<GameObject> popups = new Queue<GameObject>();
    private readonly List<Material> materials = new List<Material>();
    private Text checklist, prompt, narration;
    private AudioSource achievementAudio;
    private Transform popupParent;
    private GameObject plaque, button, exit;
    private Renderer exitRenderer;
    private PlayerController player;
    private CharacterController character;
    private Vector3 previousPosition;
    private float distanceWalked, stillTime, groundedHeight, popupTimer, elapsed;
    private bool trackedGround;
    private int trivialIndex;
    private int soundIndex;

    public int RealAchievementsCollected => unlocked.Count;
    public bool ExitUnlocked => unlocked.Count == RequiredCount;

    protected override void Start()
    {
        levelDisplayName = "Over-achiever";
        levelDescription = "Five real achievements. An unreasonable number of notifications.";
        needsPlayer = true;
        wantsCursorLocked = true;
        base.Start();
        BuildRoom();
        BuildHUD();
        RefreshChecklist();
        popupTimer = trivialAchievementInterval;
    }

    private void Update()
    {
        if (!CanProcessGameplay) return;
        if (player == null)
        {
            player = FindAnyObjectByType<PlayerController>();
            if (player == null) return;
            character = player.GetComponent<CharacterController>();
            previousPosition = player.transform.position;
        }

        Vector3 position = player.transform.position;
        Vector3 movement = position - previousPosition;
        float horizontal = new Vector2(movement.x, movement.z).magnitude;
        if (horizontal < 1.5f) distanceWalked += horizontal; // Ignore teleports.
        if (distanceWalked >= 8f) UnlockRealAchievement("walk");
        stillTime = horizontal < 0.005f && character.isGrounded ? stillTime + Time.deltaTime : 0f;
        if (stillTime >= 3f) UnlockRealAchievement("wait");
        if (character.isGrounded)
        {
            groundedHeight = position.y;
            trackedGround = true;
        }
        else if (trackedGround && position.y > groundedHeight + 0.25f)
            UnlockRealAchievement("jump");
        previousPosition = position;

        elapsed += Time.deltaTime;
        popupTimer -= Time.deltaTime;
        if (popupTimer <= 0f)
        {
            ShowPopup(TrivialAchievements[trivialIndex++ % TrivialAchievements.Length], false);
            popupTimer = Mathf.Max(0.75f, trivialAchievementInterval - elapsed * 0.025f);
        }
        UpdateInteraction();
    }

    /// <summary>Only known IDs count, and each can be awarded once.</summary>
    public void UnlockRealAchievement(string achievementName)
    {
        int index = System.Array.IndexOf(ObjectiveIds, achievementName);
        if (levelComplete || index < 0 || !unlocked.Add(achievementName)) return;
        ShowPopup(ObjectiveLabels[index], true);
        if (achievementAudio != null)
        {
            var art = GameArtLibrary.Load();
            if (art != null && art.achievementVariants != null && art.achievementVariants.Length > 0)
                achievementAudio.PlayOneShot(art.achievementVariants[index % art.achievementVariants.Length]);
            else if (achievementAudio.clip != null) achievementAudio.Play();
        }
        if (narration != null) narration.text = ExitUnlocked
            ? "Five certificates. One exit. Please leave your ambition at reception."
            : "Excellent. Another entirely normal action, formally recognized.";
        RefreshChecklist();
        if (ExitUnlocked && exitRenderer != null)
        {
            exitRenderer.sharedMaterial.color = new Color(0.2f, 0.7f, 0.4f);
            ShowPopup("The exit is unlocked. You may stop achieving.", true);
        }
    }

    private void UpdateInteraction()
    {
        prompt.text = ExitUnlocked ? "All five earned. Approach the exit and press [E]." : "Find five real achievements.";
        var cam = Camera.main;
        if (cam == null || !Physics.Raycast(cam.transform.position, cam.transform.forward,
            out RaycastHit hit, interactRange, ~0, QueryTriggerInteraction.Ignore)) return;
        if (hit.collider.gameObject == plaque)
        {
            prompt.text = "[E] Read: This plaque commemorates your ability to read plaques.";
            if (Input.GetKeyDown(KeyCode.E)) UnlockRealAchievement("read");
        }
        else if (hit.collider.gameObject == button)
        {
            prompt.text = "[E] Press the completely necessary button";
            if (Input.GetKeyDown(KeyCode.E)) UnlockRealAchievement("button");
        }
        else if ((hit.collider.gameObject == exit || hit.collider.transform.IsChildOf(exit.transform)))
        {
            prompt.text = ExitUnlocked ? "[E] Finish the game" : $"EXIT LOCKED: {unlocked.Count}/{RequiredCount} real achievements";
            if (ExitUnlocked && Input.GetKeyDown(KeyCode.E)) CompleteLevel();
        }
    }

    private void RefreshChecklist()
    {
        if (checklist == null) return;
        checklist.text = $"REAL ACHIEVEMENTS  {unlocked.Count}/{RequiredCount}\n\n";
        for (int i = 0; i < ObjectiveIds.Length; i++)
            checklist.text += (unlocked.Contains(ObjectiveIds[i]) ? "[DONE] " : "[  ] ") + ObjectiveLabels[i] + "\n\n";
    }

    private void ShowPopup(string message, bool real)
    {
        if (popupParent == null) return;
        while (popups.Count > 0 && popups.Peek() == null) popups.Dequeue();
        while (popups.Count >= 3) Destroy(popups.Dequeue());
        var panel = GameScreenUI.Panel(popupParent, "AchievementToast",
            real ? new Color(0.35f, 0.27f, 0.07f, 0.95f) : new Color(0.13f, 0.15f, 0.17f, 0.95f),
            Vector2.zero, Vector2.one);
        panel.raycastTarget = false;
        panel.gameObject.AddComponent<LayoutElement>().preferredHeight = 100;
        int iconIndex = real ? System.Array.IndexOf(ObjectiveLabels, message) : 5;
        var icon = new GameObject("AwardIcon", typeof(RectTransform), typeof(Image));
        icon.transform.SetParent(panel.transform, false);
        GameScreenUI.Place(icon.GetComponent<RectTransform>(), new Vector2(.025f,.18f), new Vector2(.16f,.82f));
        var graphic = icon.GetComponent<Image>(); graphic.raycastTarget=false; graphic.preserveAspect=true;
        graphic.sprite=Resources.Load<Sprite>("GameArt/Award" + (iconIndex < 0 ? 5 : iconIndex));
        if (!real && achievementAudio != null)
        {
            var art=GameArtLibrary.Load();
            if(art!=null && art.achievementVariants!=null && art.achievementVariants.Length>0)
                achievementAudio.PlayOneShot(art.achievementVariants[soundIndex++ % art.achievementVariants.Length], .25f);
        }
        GameScreenUI.Text(panel.transform, "Message",
            (real ? "REAL ACHIEVEMENT\n" : "ACHIEVEMENT UNLOCKED\n") + message, 22,
            new Vector2(0.19f, 0.06f), new Vector2(0.97f, 0.94f), TextAnchor.MiddleLeft);
        popups.Enqueue(panel.gameObject);
        Destroy(panel.gameObject, 3f);
    }

    private void BuildHUD()
    {
        var canvas = GameScreenUI.Canvas(transform, "AchievementsHUD", 20);
        var background = GameScreenUI.Panel(canvas.transform, "ChecklistBackground", new Color(0, 0, 0, 0.7f),
            new Vector2(0.02f, 0.53f), new Vector2(0.32f, 0.95f));
        background.raycastTarget = false;
        checklist = GameScreenUI.Text(canvas.transform, "Checklist", "", 23,
            new Vector2(0.035f, 0.55f), new Vector2(0.305f, 0.93f), TextAnchor.UpperLeft);
        prompt = GameScreenUI.Text(canvas.transform, "Prompt", "", 25,
            new Vector2(0.12f, 0.06f), new Vector2(0.88f, 0.16f));
        GameScreenUI.Text(canvas.transform, "Crosshair", "+", 25,
            new Vector2(0.48f, 0.48f), new Vector2(0.52f, 0.52f));
        narration = GameScreenUI.Text(canvas.transform, "NarrationText",
            "Welcome to the Department of Achievements. Participation is mandatory.", 25,
            new Vector2(0.12f, 0.01f), new Vector2(0.88f, 0.065f));
        var holder = new GameObject("Toasts", typeof(RectTransform), typeof(VerticalLayoutGroup));
        holder.transform.SetParent(canvas.transform, false);
        GameScreenUI.Place(holder.GetComponent<RectTransform>(), new Vector2(0.68f, 0.59f), new Vector2(0.98f, 0.95f));
        var group = holder.GetComponent<VerticalLayoutGroup>();
        group.spacing = 12;
        group.childControlWidth = group.childControlHeight = true;
        group.childForceExpandHeight = false;
        popupParent = holder.transform;
    }

    private void BuildRoom()
    {
        var art = GameArtLibrary.Load();
        if (art == null) Debug.LogError("[Finale] Missing GameArt library. Run Prepare Finale and Workshop Art.");
        var tile = art != null ? CopyMaterial(art.tile, new Vector2(5, 6)) : MakeMaterial(new Color(0.6f, 0.57f, 0.47f));
        var concrete = art != null ? CopyMaterial(art.concrete, new Vector2(3, 1)) : MakeMaterial(new Color(0.5f, 0.5f, 0.45f));
        var dark = MakeMaterial(new Color(0.10f, 0.13f, 0.13f));
        var brass = MakeMaterial(new Color(0.48f, 0.35f, 0.12f));
        brass.SetFloat("_Metallic", 0.7f);
        brass.SetFloat("_Smoothness", 0.45f);
        var paper = MakeMaterial(new Color(0.86f, 0.82f, 0.66f));
        Box("TiledFloor", new Vector3(0, -0.2f, 0), new Vector3(14, 0.4f, 18), tile);
        Box("Ceiling", new Vector3(0, 4.9f, 0), new Vector3(14, 0.3f, 18), concrete);
        Box("BackWall", new Vector3(0, 2.4f, -9), new Vector3(14, 4.8f, 0.4f), concrete);
        Box("FrontWall", new Vector3(0, 2.4f, 9), new Vector3(14, 4.8f, 0.4f), concrete);
        foreach (int side in new[] { -1, 1 })
        {
            Box("TiledDado", new Vector3(side * 7, 0.8f, 0), new Vector3(0.4f, 1.6f, 18), tile);
            Box("ConcreteWall", new Vector3(side * 7, 3.2f, 0), new Vector3(0.4f, 3.2f, 18), concrete);
            Box("BrassRail", new Vector3(side * 6.77f, 1.65f, 0), new Vector3(0.08f, 0.07f, 18), brass);
            Box("Skirting", new Vector3(side * 6.75f, 0.14f, 0), new Vector3(0.1f, 0.28f, 18), dark);
        }
        Box("ReceptionHeader", new Vector3(0, 3.8f, 8.7f), new Vector3(10, 0.85f, 0.18f), dark);
        WorldText("DEPARTMENT OF ACHIEVEMENTS", new Vector3(0, 3.93f, 8.57f), 0.065f, Color.white);
        WorldText("FINAL INSPECTION  /  PLEASE LOWER YOUR EXPECTATIONS", new Vector3(0, 3.57f, 8.56f), 0.025f, new Color(0.8f, 0.7f, 0.45f));
        // Framed, legible certificate rather than an unlabelled placeholder cube.
        Box("CertificateFrame", new Vector3(-4, 1.65f, 1), new Vector3(2.1f, 1.8f, 0.22f), brass);
        plaque = Box("ReadingPlaque", new Vector3(-4, 1.65f, 0.85f), new Vector3(1.85f, 1.55f, 0.12f), paper);
        Box("CertificateStand", new Vector3(-4, 0.45f, 1.1f), new Vector3(0.16f, 0.9f, 0.16f), dark);
        Box("CertificateBase", new Vector3(-4, 0.07f, 1.1f), new Vector3(1.1f, 0.14f, 0.7f), dark);
        WorldText("CERTIFICATE OF\nADEQUATE PARTICIPATION", new Vector3(-4, 1.92f, 0.77f), 0.026f, new Color(0.13f, 0.16f, 0.15f));
        WorldText("This certifies that you can read.\nPlease press E to make it official.", new Vector3(-4, 1.45f, 0.77f), 0.019f, new Color(0.13f, 0.16f, 0.15f));
        Box("ButtonPedestal", new Vector3(4, 0.55f, 2), new Vector3(1.25f, 1.1f, 1.25f), concrete);
        Box("ButtonMount", new Vector3(4, 1.13f, 2), new Vector3(1.05f, 0.1f, 1.05f), brass);
        button = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        button.name = "RedButton"; button.transform.SetParent(transform);
        button.transform.position = new Vector3(4, 1.27f, 2);
        button.transform.localScale = new Vector3(0.62f, 0.11f, 0.62f);
        button.GetComponent<Renderer>().sharedMaterial = MakeMaterial(new Color(0.65f, 0.06f, 0.035f));
        WorldText("OFFICIAL BUTTON\nONE PRESS REQUIRED", new Vector3(4, 0.72f, 1.36f), 0.025f, Color.white);
        exit = new GameObject("AchievementExit"); exit.transform.SetParent(transform);
        exit.transform.position = new Vector3(0, 1.6f, 8.45f);
        var exitCollider = exit.AddComponent<BoxCollider>(); exitCollider.size = new Vector3(2.2f, 3.2f, 0.3f);
        if (art != null && art.door != null)
        {
            var door = Instantiate(art.door, exit.transform, false);
            var renderers = door.GetComponentsInChildren<Renderer>();
            var bounds = renderers[0].bounds;
            foreach (var renderer in renderers) bounds.Encapsulate(renderer.bounds);
            door.transform.localScale *= 3.2f / Mathf.Max(bounds.size.y, 0.01f);
            bounds = renderers[0].bounds;
            foreach (var renderer in renderers) bounds.Encapsulate(renderer.bounds);
            door.transform.position += exit.transform.position - bounds.center;
        }
        else Box("ExitLeaf", exit.transform.position, new Vector3(2, 3.2f, 0.2f), concrete);
        Box("ExitFrameLeft", new Vector3(-1.2f, 1.7f, 8.35f), new Vector3(0.18f, 3.4f, 0.4f), dark);
        Box("ExitFrameRight", new Vector3(1.2f, 1.7f, 8.35f), new Vector3(0.18f, 3.4f, 0.4f), dark);
        exitRenderer = Box("ExitIndicator", new Vector3(1.65f, 1.8f, 8.6f), new Vector3(0.25f, 0.55f, 0.12f), MakeMaterial(new Color(0.65f, 0.12f, 0.05f))).GetComponent<Renderer>();
        WorldText("EXIT", new Vector3(0, 3.28f, 8.16f), 0.05f, Color.white);
        WorldText("5 CERTIFICATES\nNO EXCEPTIONS", new Vector3(2.5f, 1.8f, 8.5f), 0.025f, Color.white);
        var glow = MakeMaterial(new Color(0.9f, 0.86f, 0.67f));
        glow.EnableKeyword("_EMISSION"); glow.SetColor("_EmissionColor", new Color(1f, 0.9f, 0.65f) * 2f);
        for (int z = -5; z <= 5; z += 5)
        {
            Box("CeilingFixture", new Vector3(0, 4.7f, z), new Vector3(3.6f, 0.18f, 0.65f), dark);
            Box("Diffuser", new Vector3(0, 4.58f, z), new Vector3(3.2f, 0.06f, 0.5f), glow);
            var fixture = new GameObject("SteadyWarmLight", typeof(Light)); fixture.transform.SetParent(transform);
            fixture.transform.position = new Vector3(0, 4.25f, z);
            var light = fixture.GetComponent<Light>(); light.type = LightType.Point; light.range = 10f;
            light.intensity = 2.5f; light.color = new Color(1f, 0.91f, 0.74f);
        }
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.4f, 0.42f, 0.4f);
        var spawn = new GameObject("PlayerSpawnPoint"); spawn.transform.SetParent(transform);
        spawn.transform.position = new Vector3(0, 1.5f, -6); playerSpawnPoint = spawn.transform;
        achievementAudio = gameObject.AddComponent<AudioSource>();
        achievementAudio.playOnAwake = false; achievementAudio.volume = 0.25f;
        if (art != null) achievementAudio.clip = art.achievementSound;
    }

    private Material CopyMaterial(Material source, Vector2 tiling)
    {
        var copy = new Material(source); copy.mainTextureScale = tiling;
        materials.Add(copy); return copy;
    }
    private void WorldText(string content, Vector3 position, float size, Color color)
    {
        var go = new GameObject("ExhibitLabel", typeof(TextMesh)); go.transform.SetParent(transform);
        go.transform.position = position;
        var text = go.GetComponent<TextMesh>(); text.text = content; text.characterSize = size;
        text.fontSize = 64; text.anchor = TextAnchor.MiddleCenter; text.alignment = TextAlignment.Center;
        text.font = UIHelper.GetDefaultFont(); text.color = color;
        go.GetComponent<Renderer>().sharedMaterial = text.font.material;
    }

    private Material MakeMaterial(Color color)
    {
        var material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        material.color = color;
        materials.Add(material);
        return material;
    }

    private GameObject Box(string name, Vector3 position, Vector3 size, Material material)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        go.transform.SetParent(transform);
        go.transform.position = position;
        go.transform.localScale = size;
        go.GetComponent<Renderer>().sharedMaterial = material;
        return go;
    }

    private void Label(string content, Vector3 position)
    {
        var go = new GameObject("Sign", typeof(TextMesh));
        go.transform.SetParent(transform);
        go.transform.position = position;
        var text = go.GetComponent<TextMesh>();
        text.text = content;
        text.characterSize = 0.08f;
        text.fontSize = 48;
        text.anchor = TextAnchor.MiddleCenter;
        text.font = UIHelper.GetDefaultFont();
        go.GetComponent<Renderer>().sharedMaterial = text.font.material;
    }

    protected override void OnDestroy()
    {
        foreach (var material in materials) if (material != null) Destroy(material);
        base.OnDestroy();
    }
}
