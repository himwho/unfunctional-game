using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// LEVEL 8 – DLC Door
///
/// A room with a locked door that says "BUY DLC TO UNLOCK". Interacting opens a
/// full-screen fake in-game store with absurd microtransaction items. The player
/// must browse enough unique items (8+) before the game "rewards" them with free
/// credits to "purchase" the DOOR UNLOCK DLC, which reveals a randomly generated
/// 4-digit keypad code. The player then walks to the door's keypad and enters
/// the code to proceed.
///
/// Shop features: scrollable item list with ratings & reviews, "VIEWED" tags,
/// browse counter, flash sale timer, loading spinner, upsell pop-ups, and a
/// purchase receipt screen.
///
/// Builds all UI at runtime. Attach to root GameObject in LEVEL8 scene.
/// </summary>
public class Level8_DLCDoor : LevelManager
{
    [Header("Level 8 – DLC Door")]
    public DoorController doorController;

    [Header("Shop Settings")]
    public int requiredBrowseCount = 8;
    public int freeCreditsAmount = 99999;
    public int doorDLCPrice = 49999;

    // ── Runtime UI ──────────────────────────────────────────────────────────
    private Canvas shopCanvas;
    private Canvas hudCanvas;
    private Text hudPromptText;
    private Text creditText;
    private Text browseCountText;

    private GameObject shopPanel;
    private GameObject itemDetailPanel;
    private GameObject confirmPanel;
    private GameObject upsellPanel;
    private GameObject codeRevealPanel;
    private GameObject loadingPanel;
    private Text detailTitle;
    private Text detailDesc;
    private Text detailPrice;
    private Text detailReview;
    private Text confirmText;
    private Text upsellText;
    private Text codeRevealCodeText;

    // ── State ───────────────────────────────────────────────────────────────
    private int playerCredits = 0;
    private int itemsBrowsed = 0;
    private bool shopOpen = false;
    private bool dlcPurchased = false;
    private bool rewardGiven = false;
    private bool codeRevealed = false;
    private int currentViewItem = -1;
    private HashSet<int> viewedItems = new HashSet<int>();
    private List<GameObject> viewedTags = new List<GameObject>();
    private GameObject fallbackDoor;

    // ── Keypad / door code ──────────────────────────────────────────────────
    private KeypadController keypad;
    private string doorCode = "";

    // ── Shop catalogue ──────────────────────────────────────────────────────
    // Each item: { name, description, price, review, rating }
    private readonly string[][] shopItems = new string[][]
    {
        new[] { "Golden Shovel Skin",       "Makes your shovel 200% more golden.\nDoes not improve digging.",                "$14.99",  "\"My shovel is now golden but I don't have a shovel.\" – GoldenBoy42",            "★★★☆☆" },
        new[] { "Premium Air DLC",          "Breathe premium air!\nNow with 3% more oxygen.",                               "$9.99",   "\"I feel 3% more alive.\" – BreathingFan",                                        "★★☆☆☆" },
        new[] { "Extra Gravity Pack",       "Feel heavier!\nPerfect for that grounded experience.",                          "$7.99",   "\"I can no longer jump. 10/10.\" – PhysicsNerd",                                  "★★★★☆" },
        new[] { "Door Opening Sound FX",    "A satisfying 'click' when doors open.\nJust the sound. No door included.",      "$4.99",   "\"The click is nice but I still can't open doors.\" – DoorLover",                 "★★★☆☆" },
        new[] { "Invisible Hat",            "You can't see it, but it's there.\nTrust us.",                                  "$19.99",  "\"I can't see it but I FEEL fashionable.\" – InvisiFan",                          "★★★★★" },
        new[] { "Speed Boost (0.01%)",      "Barely noticeable.\nTechnically faster.",                                       "$12.99",  "\"After 400 hours I think I notice the difference.\" – SpeedRunner",              "★☆☆☆☆" },
        new[] { "HD Texture: Single Brick", "One brick, ultra high resolution.\nThe rest stay blurry.",                      "$2.99",   "\"That one brick looks AMAZING.\" – TextureEnjoyer",                              "★★★☆☆" },
        new[] { "NPC Emotion Pack",         "NPCs now have 2 emotions instead of 1.\nIncludes: Happy and Slightly Less Happy.", "$8.99", "\"GORP smiled at me once. Worth every penny.\" – NPCWhisperer",                  "★★★★☆" },
        new[] { "Loot Box (Empty)",         "Guaranteed to contain nothing.\nCollector's edition!",                          "$3.99",   "\"The nothing inside is very high quality.\" – LootGoblin",                       "★★★★★" },
        new[] { "Premium Loading Screen",   "Watch a fancier loading animation.\nNow with spinning dots!",                  "$6.99",   "\"I intentionally load screens just to watch it.\" – LoadingFan",                 "★★★☆☆" },
        new[] { "Day/Night Cycle",          "The sky changes color sometimes.\nRevolutionary game design.",                  "$24.99",  "\"It got dark and I couldn't see anything. Perfect.\" – SkyWatcher",              "★★☆☆☆" },
        new[] { "Extended Credits",         "See 40% more names in the credits.\nDiscover who made this mess!",              "$1.99",   "\"So many names. So much blame.\" – CreditRoller",                                "★★★☆☆" },
        new[] { "VIP Queue Skip",           "Skip the queue that doesn't exist.\nSave 0 seconds!",                          "$15.99",  "\"I skipped nothing faster than everyone.\" – VIPGamer",                          "★★★★★" },
        new[] { "Companion Rock",           "A rock follows you around.\nIt doesn't do anything.",                           "$11.99",  "\"I named him Rocky. He's my best friend.\" – PetRock99",                         "★★★★★" },
        new[] { "Battle Pass: Season 0",    "Pre-season content.\nThere was nothing before this.",                           "$29.99",  "\"I paid for emptiness and it delivered.\" – EarlyAdopter",                       "★★☆☆☆" },
        new[] { "Font Change DLC",          "Changes the game font.\nOnce. To Comic Sans.",                                 "$5.99",   "\"Comic Sans makes everything better.\" – FontFanatic",                           "★★★★☆" },
        new[] { "Cloud Save Unlock",        "Your saves, stored somewhere\nyou can't access.",                               "$9.99",   "\"My saves are in the cloud. Like my money.\" – SaveScummer",                     "★☆☆☆☆" },
        new[] { "FOV Slider DLC",           "Lets you see a tiny bit more.\nFor a premium price.",                           "$11.99",  "\"5 extra degrees of peripheral vision!\" – PeripheralPro",                       "★★★☆☆" },
        new[] { "Character Name Change",    "Rename yourself.\nWe'll still call you Player.",                                "$4.99",   "\"Changed my name to 'Not Player'. Still called Player.\" – NotPlayer",           "★★☆☆☆" },
        new[] { "DOOR UNLOCK DLC ★",        "Reveals the 4-digit code to proceed.\nThe actual useful one.",                  "$49,999", "\"This is literally the only item worth buying.\" – EveryPlayer",                 "★★★★★" },
    };

    // =====================================================================
    // Lifecycle
    // =====================================================================

    protected override void Start()
    {
        base.Start();
        levelDisplayName = "The DLC Door";
        levelDescription = "Surely you can afford to continue.";
        needsPlayer = true;
        wantsCursorLocked = true;

        // Generate a unique 4-digit code for this level load
        doorCode = Random.Range(1000, 10000).ToString();
        Debug.Log($"[Level8] Generated door code: {doorCode}");

        EnsureSpawnPoint();
        SetupDoor();
        CreateShopUI();
        CreateHUD();
    }

    private void Update()
    {
        if (levelComplete) return;

        // Don't run interaction while the keypad overlay is active
        if (keypad != null && keypad.IsOpen) return;

        if (!shopOpen)
        {
            UpdateInteraction();
        }
        else
        {
            if (Input.GetKeyDown(KeyCode.Escape))
                CloseShop();
        }
    }

    // =====================================================================
    // Gaze / Interaction
    // =====================================================================

    private void UpdateInteraction()
    {
        Camera cam = Camera.main;
        if (cam == null) return;

        bool lookingAtDoor = false;
        bool lookingAtKeypad = false;

        Ray ray = new Ray(cam.transform.position, cam.transform.forward);
        if (Physics.Raycast(ray, out RaycastHit hit, 5f, ~0, QueryTriggerInteraction.Collide))
        {
            Transform root = hit.collider.transform.root;
            string hitName = hit.collider.gameObject.name;

            if (doorController != null && root == doorController.transform)
            {
                // Distinguish between keypad sub-object and the door itself
                if (hitName.Contains("Keypad") || hitName.Contains("keypad"))
                    lookingAtKeypad = true;
                else
                    lookingAtDoor = true;
            }
            else if (hitName.Contains("Door") || hitName.Contains("DLC"))
            {
                lookingAtDoor = true;
            }
        }

        bool lookingAtAnyDoorPart = lookingAtDoor || lookingAtKeypad;

        if (lookingAtAnyDoorPart)
        {
            if (dlcPurchased && codeRevealed)
            {
                // After purchase — any part of the door opens the keypad
                hudPromptText.text = "Press [E] to enter door code";
                if (Input.GetKeyDown(KeyCode.E) && keypad != null)
                    keypad.Open();
            }
            else if (dlcPurchased && !codeRevealed)
            {
                hudPromptText.text = "Press [E] to view purchase receipt";
                if (Input.GetKeyDown(KeyCode.E))
                    ShowCodeReveal();
            }
            else
            {
                hudPromptText.text = "Press [E] to open STORE";
                if (Input.GetKeyDown(KeyCode.E))
                    OpenShop();
            }
            hudCanvas.gameObject.SetActive(true);
        }
        else
        {
            hudCanvas.gameObject.SetActive(false);
        }
    }

    // =====================================================================
    // Shop – open / close
    // =====================================================================

    private void OpenShop()
    {
        shopOpen = true;
        shopPanel.SetActive(true);
        shopCanvas.gameObject.SetActive(true);

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        PlayerController pc = FindAnyObjectByType<PlayerController>();
        if (pc != null) pc.enabled = false;

        UpdateCreditDisplay();
        UpdateBrowseCount();
    }

    private void CloseShop()
    {
        shopOpen = false;
        shopCanvas.gameObject.SetActive(false);
        if (shopPanel != null) shopPanel.SetActive(false);
        if (itemDetailPanel != null) itemDetailPanel.SetActive(false);
        if (confirmPanel != null) confirmPanel.SetActive(false);
        if (upsellPanel != null) upsellPanel.SetActive(false);
        if (codeRevealPanel != null) codeRevealPanel.SetActive(false);
        if (loadingPanel != null) loadingPanel.SetActive(false);

        ApplyCursorState();

        PlayerController pc = FindAnyObjectByType<PlayerController>();
        if (pc != null) pc.enabled = true;
    }

    // =====================================================================
    // Shop UI – build at runtime
    // =====================================================================

    private void CreateShopUI()
    {
        // -- Root Canvas --
        GameObject canvasObj = new GameObject("ShopCanvas");
        canvasObj.transform.SetParent(transform);
        shopCanvas = canvasObj.AddComponent<Canvas>();
        UIHelper.ConfigureCanvas(shopCanvas, sortingOrder: 50);

        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        canvasObj.AddComponent<GraphicRaycaster>();

        // -- Full-screen shop panel --
        shopPanel = CreatePanel(canvasObj.transform, "ShopPanel",
            Vector2.zero, Vector2.one, new Color(0.05f, 0.05f, 0.08f, 0.95f));

        // ── Header bar ──
        CreatePanel(shopPanel.transform, "HeaderBar",
            new Vector2(0, 0.885f), new Vector2(1, 1),
            new Color(0.08f, 0.06f, 0.12f));

        CreateText(shopPanel.transform, "ShopTitle",
            "UNFUNCTIONAL PREMIUM STORE™",
            new Vector2(0.03f, 0.91f), new Vector2(0.55f, 0.99f),
            36, new Color(1f, 0.84f, 0f));

        CreateText(shopPanel.transform, "ShopSubtitle",
            "\"Where every purchase is a mistake you can't refund\"",
            new Vector2(0.03f, 0.885f), new Vector2(0.55f, 0.915f),
            14, new Color(0.4f, 0.4f, 0.4f));

        creditText = CreateText(shopPanel.transform, "Credits",
            $"Credits: {playerCredits}",
            new Vector2(0.58f, 0.92f), new Vector2(0.82f, 0.99f),
            28, new Color(0.3f, 1f, 0.3f));

        browseCountText = CreateText(shopPanel.transform, "BrowseCount",
            $"Browsed: 0/{requiredBrowseCount}",
            new Vector2(0.58f, 0.885f), new Vector2(0.82f, 0.925f),
            16, new Color(0.6f, 0.6f, 0.6f));

        CreateButton(shopPanel.transform, "CloseBtn", "X  CLOSE",
            new Vector2(0.92f, 0.95f), new Vector2(130, 38),
            new Color(0.5f, 0.1f, 0.1f), () => CloseShop());

        // ── Fake sale timer ──
        Text saleTimer = CreateText(shopPanel.transform, "SaleTimer",
            "FLASH SALE ENDS IN: 99:59:59",
            new Vector2(0.25f, 0.855f), new Vector2(0.75f, 0.885f),
            16, new Color(1f, 0.3f, 0.3f));
        saleTimer.alignment = TextAnchor.MiddleCenter;
        StartCoroutine(AnimateSaleTimer(saleTimer));

        // ── Scrollable item list ──
        GameObject scrollArea = CreatePanel(shopPanel.transform, "ScrollArea",
            new Vector2(0.02f, 0.02f), new Vector2(0.98f, 0.85f),
            new Color(0.06f, 0.06f, 0.09f, 0.8f));

        ScrollRect scrollRect = scrollArea.AddComponent<ScrollRect>();
        scrollRect.horizontal = false;
        scrollRect.vertical = true;
        scrollRect.movementType = ScrollRect.MovementType.Clamped;
        scrollRect.scrollSensitivity = 40f;

        scrollArea.AddComponent<Mask>().showMaskGraphic = true;

        // Content container (anchored at top)
        GameObject content = new GameObject("Content");
        content.transform.SetParent(scrollArea.transform, false);
        RectTransform contentRT = content.AddComponent<RectTransform>();
        contentRT.anchorMin = new Vector2(0, 1);
        contentRT.anchorMax = new Vector2(1, 1);
        contentRT.pivot = new Vector2(0.5f, 1);

        float rowH = 78f;
        float gap = 4f;
        float totalH = shopItems.Length * (rowH + gap);
        contentRT.sizeDelta = new Vector2(0, totalH);
        contentRT.anchoredPosition = Vector2.zero;
        scrollRect.content = contentRT;

        Font font = UIHelper.GetDefaultFont();

        for (int i = 0; i < shopItems.Length; i++)
        {
            float yTop = -(i * (rowH + gap));
            int idx = i;
            bool isDLC = (i == shopItems.Length - 1);

            // Row container
            GameObject row = new GameObject($"ItemRow_{i}");
            row.transform.SetParent(content.transform, false);
            RectTransform rowRT = row.AddComponent<RectTransform>();
            rowRT.anchorMin = new Vector2(0, 1);
            rowRT.anchorMax = new Vector2(1, 1);
            rowRT.pivot = new Vector2(0.5f, 1);
            rowRT.anchoredPosition = new Vector2(0, yTop);
            rowRT.sizeDelta = new Vector2(-16, rowH);

            Image rowBg = row.AddComponent<Image>();
            rowBg.color = isDLC
                ? new Color(0.15f, 0.08f, 0.08f)
                : (i % 2 == 0 ? new Color(0.10f, 0.10f, 0.13f) : new Color(0.08f, 0.08f, 0.11f));

            // Name
            MakeRowText(row.transform, "Name", shopItems[i][0], font, 20,
                isDLC ? new Color(1f, 0.84f, 0f) : Color.white,
                new Vector2(0.02f, 0.38f), new Vector2(0.44f, 0.95f),
                TextAnchor.MiddleLeft);

            // Rating
            MakeRowText(row.transform, "Rating", shopItems[i][4], font, 14,
                new Color(1f, 0.8f, 0.2f),
                new Vector2(0.02f, 0.05f), new Vector2(0.44f, 0.38f),
                TextAnchor.MiddleLeft);

            // Price
            MakeRowText(row.transform, "Price", shopItems[i][2], font, 22,
                isDLC ? new Color(1f, 0.3f, 0.3f) : new Color(0.3f, 1f, 0.3f),
                new Vector2(0.72f, 0.15f), new Vector2(0.98f, 0.85f),
                TextAnchor.MiddleRight);

            // VIEW button
            MakeRowButton(row.transform, "ViewBtn", isDLC ? "VIEW DLC" : "VIEW",
                new Vector2(0.54f, 0.5f), new Vector2(105, 38),
                isDLC ? new Color(0.5f, 0.2f, 0.2f) : new Color(0.2f, 0.3f, 0.5f),
                font, () => OnViewItem(idx));

            // Badges on select items
            if (i == 0 || i == 4 || i == 8 || i == 14 || isDLC)
            {
                string badge = isDLC ? "HOT" : (i == 0 ? "NEW!" : "SALE!");
                Color bc = isDLC ? new Color(1f, 0.2f, 0.1f) : new Color(0.1f, 0.6f, 0.1f);

                GameObject badgeObj = new GameObject("Badge");
                badgeObj.transform.SetParent(row.transform, false);
                Image badgeImg = badgeObj.AddComponent<Image>();
                badgeImg.color = bc;
                badgeImg.raycastTarget = false;
                RectTransform brt = badgeObj.GetComponent<RectTransform>();
                brt.anchorMin = new Vector2(0.455f, 0.6f);
                brt.anchorMax = new Vector2(0.525f, 0.95f);
                brt.offsetMin = brt.offsetMax = Vector2.zero;

                MakeRowText(badgeObj.transform, "BT", badge, font, 10,
                    Color.white, Vector2.zero, Vector2.one, TextAnchor.MiddleCenter);
            }

            // "VIEWED" marker (hidden initially)
            GameObject viewedObj = new GameObject("ViewedTag");
            viewedObj.transform.SetParent(row.transform, false);
            Text vt = viewedObj.AddComponent<Text>();
            vt.font = font;
            vt.fontSize = 12;
            vt.text = "✓ VIEWED";
            vt.color = new Color(0.3f, 0.8f, 0.3f);
            vt.alignment = TextAnchor.MiddleLeft;
            vt.raycastTarget = false;
            RectTransform vrt = viewedObj.GetComponent<RectTransform>();
            vrt.anchorMin = new Vector2(0.54f, 0.05f);
            vrt.anchorMax = new Vector2(0.71f, 0.3f);
            vrt.offsetMin = vrt.offsetMax = Vector2.zero;
            viewedObj.SetActive(false);
            viewedTags.Add(viewedObj);
        }

        // ── Item Detail Panel ──
        itemDetailPanel = CreatePanel(canvasObj.transform, "ItemDetailPanel",
            new Vector2(0.2f, 0.12f), new Vector2(0.8f, 0.88f),
            new Color(0.08f, 0.08f, 0.12f, 0.98f));

        CreatePanel(itemDetailPanel.transform, "DetailHdr",
            new Vector2(0, 0.87f), new Vector2(1, 1),
            new Color(0.1f, 0.08f, 0.15f));

        detailTitle = CreateText(itemDetailPanel.transform, "DetailTitle", "",
            new Vector2(0.05f, 0.88f), new Vector2(0.85f, 0.97f),
            30, new Color(1f, 0.84f, 0f));

        detailDesc = CreateText(itemDetailPanel.transform, "DetailDesc", "",
            new Vector2(0.05f, 0.55f), new Vector2(0.95f, 0.84f),
            20, new Color(0.8f, 0.8f, 0.8f));

        detailPrice = CreateText(itemDetailPanel.transform, "DetailPrice", "",
            new Vector2(0.05f, 0.42f), new Vector2(0.95f, 0.55f),
            28, new Color(0.3f, 1f, 0.3f));

        detailReview = CreateText(itemDetailPanel.transform, "DetailReview", "",
            new Vector2(0.05f, 0.26f), new Vector2(0.95f, 0.42f),
            16, new Color(0.6f, 0.6f, 0.5f));
        detailReview.fontStyle = FontStyle.Italic;

        CreateButton(itemDetailPanel.transform, "BuyBtn", "BUY NOW",
            new Vector2(0.35f, 0.1f), new Vector2(200, 50),
            new Color(0.2f, 0.5f, 0.2f), () => OnTryBuy());

        CreateButton(itemDetailPanel.transform, "BackBtn", "< BACK TO STORE",
            new Vector2(0.65f, 0.1f), new Vector2(200, 50),
            new Color(0.3f, 0.3f, 0.35f), () => itemDetailPanel.SetActive(false));

        CreateButton(itemDetailPanel.transform, "DetailX", "X",
            new Vector2(0.95f, 0.94f), new Vector2(36, 36),
            new Color(0.5f, 0.15f, 0.15f), () => itemDetailPanel.SetActive(false));

        itemDetailPanel.SetActive(false);

        // ── Confirm Panel ──
        confirmPanel = CreatePanel(canvasObj.transform, "ConfirmPanel",
            new Vector2(0.25f, 0.25f), new Vector2(0.75f, 0.75f),
            new Color(0.06f, 0.06f, 0.1f, 0.98f));

        confirmText = CreateText(confirmPanel.transform, "ConfirmText", "",
            new Vector2(0.05f, 0.3f), new Vector2(0.95f, 0.9f), 22, Color.white);
        confirmText.alignment = TextAnchor.MiddleCenter;

        CreateButton(confirmPanel.transform, "ConfirmYes", "YES, I'M SURE",
            new Vector2(0.3f, 0.08f), new Vector2(180, 45),
            new Color(0.2f, 0.5f, 0.2f), () => OnConfirmBuy());

        CreateButton(confirmPanel.transform, "ConfirmNo", "NO, WAIT",
            new Vector2(0.7f, 0.08f), new Vector2(180, 45),
            new Color(0.5f, 0.2f, 0.2f), () => confirmPanel.SetActive(false));

        confirmPanel.SetActive(false);

        // ── Upsell Panel ──
        upsellPanel = CreatePanel(canvasObj.transform, "UpsellPanel",
            new Vector2(0.2f, 0.2f), new Vector2(0.8f, 0.8f),
            new Color(0.1f, 0.05f, 0.05f, 0.98f));

        upsellText = CreateText(upsellPanel.transform, "UpsellText",
            "Are you SURE you don't want to buy anything?\n\n" +
            "These deals won't last forever!\n" +
            "(They will, actually. They're fake.)\n\n" +
            "But what if they weren't fake?\n" +
            "You'd regret not buying, right?",
            new Vector2(0.05f, 0.25f), new Vector2(0.95f, 0.9f),
            22, new Color(1f, 0.7f, 0.7f));
        upsellText.alignment = TextAnchor.MiddleCenter;

        CreateButton(upsellPanel.transform, "UpsellOK", "FINE, I'LL KEEP BROWSING",
            new Vector2(0.5f, 0.08f), new Vector2(340, 50),
            new Color(0.3f, 0.3f, 0.5f), () => upsellPanel.SetActive(false));

        upsellPanel.SetActive(false);

        // ── Code Reveal / Receipt Panel (shown after DLC purchase) ──
        codeRevealPanel = CreatePanel(canvasObj.transform, "CodeRevealPanel",
            new Vector2(0.2f, 0.15f), new Vector2(0.8f, 0.85f),
            new Color(0.05f, 0.08f, 0.05f, 0.98f));

        Text rcptTitle = CreateText(codeRevealPanel.transform, "ReceiptTitle",
            "PURCHASE RECEIPT",
            new Vector2(0.05f, 0.84f), new Vector2(0.95f, 0.96f),
            34, new Color(0.3f, 1f, 0.3f));
        rcptTitle.alignment = TextAnchor.MiddleCenter;

        Text rcptItem = CreateText(codeRevealPanel.transform, "ReceiptItem",
            "DOOR UNLOCK DLC ★",
            new Vector2(0.05f, 0.72f), new Vector2(0.95f, 0.84f),
            24, new Color(1f, 0.84f, 0f));
        rcptItem.alignment = TextAnchor.MiddleCenter;

        Text rcptLabel = CreateText(codeRevealPanel.transform, "ReceiptLabel",
            "Your door access code:",
            new Vector2(0.05f, 0.58f), new Vector2(0.95f, 0.70f),
            20, new Color(0.7f, 0.7f, 0.7f));
        rcptLabel.alignment = TextAnchor.MiddleCenter;

        codeRevealCodeText = CreateText(codeRevealPanel.transform, "CodeText",
            doorCode,
            new Vector2(0.15f, 0.38f), new Vector2(0.85f, 0.58f),
            64, Color.white);
        codeRevealCodeText.alignment = TextAnchor.MiddleCenter;

        Text rcptHint = CreateText(codeRevealPanel.transform, "ReceiptHint",
            "Enter this code on the keypad next to the door.\n" +
            "(Yes, you still have to walk over there and type it in.)",
            new Vector2(0.05f, 0.2f), new Vector2(0.95f, 0.38f),
            16, new Color(0.5f, 0.5f, 0.5f));
        rcptHint.alignment = TextAnchor.MiddleCenter;

        CreateButton(codeRevealPanel.transform, "ReceiptClose", "GOT IT",
            new Vector2(0.5f, 0.07f), new Vector2(200, 50),
            new Color(0.2f, 0.5f, 0.2f),
            () => { codeRevealPanel.SetActive(false); CloseShop(); });

        codeRevealPanel.SetActive(false);

        // ── Fake loading spinner panel ──
        loadingPanel = CreatePanel(canvasObj.transform, "LoadingPanel",
            new Vector2(0.3f, 0.35f), new Vector2(0.7f, 0.65f),
            new Color(0.05f, 0.05f, 0.08f, 0.98f));

        Text loadTxt = CreateText(loadingPanel.transform, "LoadingText",
            "Processing your purchase...",
            new Vector2(0.05f, 0.3f), new Vector2(0.95f, 0.9f),
            22, new Color(0.7f, 0.7f, 0.7f));
        loadTxt.alignment = TextAnchor.MiddleCenter;
        loadingPanel.SetActive(false);

        // Hide everything until player opens the shop
        shopCanvas.gameObject.SetActive(false);
    }

    // =====================================================================
    // Shop – item callbacks
    // =====================================================================

    private void OnViewItem(int index)
    {
        currentViewItem = index;

        bool isNew = viewedItems.Add(index);
        itemsBrowsed = viewedItems.Count;

        // Show "VIEWED" tag on that row
        if (isNew && index < viewedTags.Count)
            viewedTags[index].SetActive(true);

        bool isDLC = (index == shopItems.Length - 1);

        detailTitle.text = shopItems[index][0];
        detailDesc.text = shopItems[index][1];
        detailPrice.text = shopItems[index][2];
        detailReview.text = shopItems[index][3] + "\n" + shopItems[index][4];
        detailPrice.color = isDLC
            ? new Color(1f, 0.3f, 0.3f) : new Color(0.3f, 1f, 0.3f);

        itemDetailPanel.SetActive(true);
        confirmPanel.SetActive(false);

        UpdateBrowseCount();

        // Reward free credits once enough items are browsed
        if (itemsBrowsed >= requiredBrowseCount && !rewardGiven)
        {
            rewardGiven = true;
            StartCoroutine(GiveFreeCredits());
        }
    }

    private void OnTryBuy()
    {
        if (currentViewItem == shopItems.Length - 1) // Door DLC
        {
            if (playerCredits >= doorDLCPrice)
            {
                confirmText.text =
                    "Purchase DOOR UNLOCK DLC for 49,999 credits?\n\n" +
                    "(This reveals a 4-digit code for the door keypad.)";
                confirmPanel.SetActive(true);
            }
            else
            {
                confirmText.text =
                    "NOT ENOUGH CREDITS!\n\nKeep browsing items to earn free credits.\n" +
                    $"(You have {playerCredits:N0}, need {doorDLCPrice:N0})\n\n" +
                    $"Items browsed: {itemsBrowsed}/{requiredBrowseCount}";
                confirmPanel.SetActive(true);
            }
        }
        else
        {
            // Non-DLC item → upsell nag
            upsellPanel.SetActive(true);
            itemDetailPanel.SetActive(false);
        }
    }

    private void OnConfirmBuy()
    {
        if (currentViewItem == shopItems.Length - 1 && playerCredits >= doorDLCPrice)
        {
            playerCredits -= doorDLCPrice;
            dlcPurchased = true;
            UpdateCreditDisplay();

            confirmPanel.SetActive(false);
            itemDetailPanel.SetActive(false);

            StartCoroutine(PurchaseSequence());
        }
        else
        {
            confirmPanel.SetActive(false);
        }
    }

    private IEnumerator GiveFreeCredits()
    {
        yield return new WaitForSeconds(0.5f);

        int target = freeCreditsAmount;
        float duration = 2.5f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            playerCredits = (int)Mathf.Lerp(0, target, elapsed / duration);
            UpdateCreditDisplay();
            yield return null;
        }

        playerCredits = target;
        UpdateCreditDisplay();
    }

    // =====================================================================
    // Purchase → Code Reveal
    // =====================================================================

    private IEnumerator PurchaseSequence()
    {
        // Fake loading spinner
        loadingPanel.SetActive(true);
        yield return new WaitForSeconds(2f);
        loadingPanel.SetActive(false);

        // Show the receipt with the door code
        ShowCodeReveal();
    }

    private void ShowCodeReveal()
    {
        codeRevealed = true;

        // If player triggered this from the HUD rather than the shop, we need
        // to open the overlay canvas so the receipt is visible.
        if (!shopOpen)
        {
            shopCanvas.gameObject.SetActive(true);
            shopPanel.SetActive(false);
            shopOpen = true;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            PlayerController pc = FindAnyObjectByType<PlayerController>();
            if (pc != null) pc.enabled = false;
        }

        codeRevealPanel.SetActive(true);
    }

    private IEnumerator AnimateSaleTimer(Text timerTxt)
    {
        int h = 99, m = 59, s = 59;
        while (true)
        {
            timerTxt.text = $"FLASH SALE ENDS IN: {h:D2}:{m:D2}:{s:D2}";
            yield return new WaitForSecondsRealtime(1f);
            s--;
            if (s < 0) { s = 59; m--; }
            if (m < 0) { m = 59; h--; }
            if (h < 0) { h = 99; m = 59; s = 59; }
        }
    }

    // =====================================================================
    // Keypad validation
    // =====================================================================

    private void HandleCodeSubmitted(string code)
    {
        if (code == doorCode)
        {
            keypad.AcceptCode("ACCESS GRANTED");
            StartCoroutine(DoorOpenSequence());
        }
        else
        {
            keypad.RejectCode("WRONG CODE – TRY AGAIN");
            if (doorController != null)
                doorController.ShakeDoor();
        }
    }

    private IEnumerator DoorOpenSequence()
    {
        yield return new WaitForSeconds(1f);

        if (keypad != null && keypad.IsOpen)
            keypad.Close();

        yield return new WaitForSeconds(0.5f);

        if (doorController != null)
        {
            doorController.OpenDoor();
            yield return new WaitForSeconds(2f);
        }
        else if (fallbackDoor != null)
        {
            float elapsed = 0f;
            Vector3 startPos = fallbackDoor.transform.position;
            Vector3 endPos = startPos + Vector3.up * 3.5f;
            while (elapsed < 1.5f)
            {
                elapsed += Time.deltaTime;
                fallbackDoor.transform.position =
                    Vector3.Lerp(startPos, endPos, elapsed / 1.5f);
                yield return null;
            }
            yield return new WaitForSeconds(0.5f);
        }

        CompleteLevel();
    }

    // =====================================================================
    // HUD helpers
    // =====================================================================

    private void UpdateCreditDisplay()
    {
        if (creditText != null)
            creditText.text = $"Credits: {playerCredits:N0}";
    }

    private void UpdateBrowseCount()
    {
        if (browseCountText != null)
        {
            browseCountText.text = $"Browsed: {itemsBrowsed}/{requiredBrowseCount}";
            browseCountText.color = itemsBrowsed >= requiredBrowseCount
                ? new Color(0.3f, 1f, 0.3f)
                : new Color(0.6f, 0.6f, 0.6f);
        }
    }

    // =====================================================================
    // Room Geometry & Door
    // =====================================================================

    private void BuildRoom()
    {
        float roomW = 10f, roomH = 4f, roomD = 10f, wt = 0.2f;
        float doorGapW = 2.2f, doorGapH = 3f;

        Material wallMat  = CreateMat(new Color(0.45f, 0.45f, 0.48f));
        Material floorMat = CreateMat(new Color(0.3f, 0.3f, 0.32f));
        Material accentMat = CreateMat(new Color(0.25f, 0.2f, 0.35f));
        Material redMat   = CreateMat(new Color(0.8f, 0.15f, 0.15f));

        // Floor & Ceiling
        CreateBox("Floor",   new Vector3(0, -wt / 2f, 0),          new Vector3(roomW, wt, roomD), floorMat);
        CreateBox("Ceiling", new Vector3(0, roomH + wt / 2f, 0),   new Vector3(roomW, wt, roomD), wallMat);

        // South, West, East walls
        CreateBox("WallSouth", new Vector3(0, roomH / 2f, -roomD / 2f), new Vector3(roomW, roomH, wt), wallMat);
        CreateBox("WallWest",  new Vector3(-roomW / 2f, roomH / 2f, 0), new Vector3(wt, roomH, roomD), wallMat);
        CreateBox("WallEast",  new Vector3(roomW / 2f, roomH / 2f, 0),  new Vector3(wt, roomH, roomD), wallMat);

        // North wall – split for door opening
        float sideW = (roomW - doorGapW) / 2f;
        CreateBox("WallNorthL",
            new Vector3(-(doorGapW + sideW) / 2f, roomH / 2f, roomD / 2f),
            new Vector3(sideW, roomH, wt), wallMat);
        CreateBox("WallNorthR",
            new Vector3((doorGapW + sideW) / 2f, roomH / 2f, roomD / 2f),
            new Vector3(sideW, roomH, wt), wallMat);
        float topH = roomH - doorGapH;
        CreateBox("WallNorthTop",
            new Vector3(0, doorGapH + topH / 2f, roomD / 2f),
            new Vector3(doorGapW + wt, topH, wt), wallMat);

        // Door frame jambs
        Material frameMat = CreateMat(new Color(0.55f, 0.5f, 0.6f));
        CreateBox("DoorJambL", new Vector3(-doorGapW / 2f, doorGapH / 2f, roomD / 2f),
            new Vector3(0.08f, doorGapH, wt + 0.04f), frameMat);
        CreateBox("DoorJambR", new Vector3(doorGapW / 2f, doorGapH / 2f, roomD / 2f),
            new Vector3(0.08f, doorGapH, wt + 0.04f), frameMat);

        // DLC banner sign above door
        GameObject sign = GameObject.CreatePrimitive(PrimitiveType.Quad);
        sign.name = "DLCSign";
        sign.transform.position = new Vector3(0, doorGapH + 0.2f, roomD / 2f - 0.12f);
        sign.transform.localScale = new Vector3(4f, 0.7f, 1f);
        sign.GetComponent<Renderer>().sharedMaterial = redMat;
        Destroy(sign.GetComponent<Collider>());

        // Display pedestals along walls
        for (int i = 0; i < 3; i++)
        {
            float z = -3f + i * 2.5f;
            CreateBox($"PedestalW{i}", new Vector3(-roomW / 2f + 1.2f, 0.35f, z),
                new Vector3(0.7f, 0.7f, 0.7f), accentMat);
            CreateBox($"ProductW{i}", new Vector3(-roomW / 2f + 1.2f, 0.9f, z),
                new Vector3(0.3f, 0.3f, 0.3f), redMat);
            CreateBox($"PedestalE{i}", new Vector3(roomW / 2f - 1.2f, 0.35f, z),
                new Vector3(0.7f, 0.7f, 0.7f), accentMat);
            CreateBox($"ProductE{i}", new Vector3(roomW / 2f - 1.2f, 0.9f, z),
                new Vector3(0.3f, 0.3f, 0.3f), redMat);
        }

        // Reception counter
        CreateBox("Counter",    new Vector3(0, 0.5f, -1.5f),  new Vector3(3.5f, 1f, 1f), accentMat);
        CreateBox("CounterTop", new Vector3(0, 1.05f, -1.5f), new Vector3(3.7f, 0.1f, 1.1f),
            CreateMat(new Color(0.35f, 0.28f, 0.45f)));

        // Spawn point
        GameObject sp = new GameObject("PlayerSpawnPoint");
        sp.transform.position = new Vector3(0, 1f, -3.5f);
        sp.transform.rotation = Quaternion.LookRotation(Vector3.forward);
        playerSpawnPoint = sp.transform;

        // Lighting
        CreatePointLight("MainLight", new Vector3(0, 3.5f, 0),
            new Color(1f, 0.95f, 0.85f), 16f, 1.2f);
        CreatePointLight("AccentW", new Vector3(-roomW / 2f + 1f, 2.5f, 0),
            new Color(0.5f, 0.2f, 0.8f), 7f, 0.5f);
        CreatePointLight("AccentE", new Vector3(roomW / 2f - 1f, 2.5f, 0),
            new Color(0.2f, 0.4f, 0.8f), 7f, 0.5f);
        CreatePointLight("DoorSpot", new Vector3(0, 3f, roomD / 2f - 1f),
            new Color(1f, 0.3f, 0.3f), 5f, 0.8f);
    }

    /// <summary>
    /// Creates a spawn point only if one was not assigned in the inspector.
    /// Call this instead of BuildRoom() when the scene is already built manually.
    /// </summary>
    private void EnsureSpawnPoint()
    {
        if (playerSpawnPoint != null) return;

        GameObject sp = new GameObject("PlayerSpawnPoint");
        sp.transform.position = new Vector3(0f, 1f, -1.5f);
        sp.transform.rotation = Quaternion.LookRotation(Vector3.forward);
        playerSpawnPoint = sp.transform;
        Debug.Log("[Level8] Created runtime spawn point at (0, 1, -1.5).");
    }

    /// <summary>
    /// Wires the DoorController and KeypadController without moving
    /// the door — respects wherever it was placed in the scene.
    /// </summary>
    private void SetupDoor()
    {
        if (doorController == null)
            doorController = FindAnyObjectByType<DoorController>();

        if (doorController != null)
        {
            // Use Keypad unlock for this level (don't reposition the door)
            doorController.unlockMethod = DoorController.UnlockMethod.Keypad;
            doorController.ApplyUnlockMethod();
            doorController.RecalculatePositions();

            // Wire keypad events
            keypad = doorController.keypadController;
            if (keypad == null)
                keypad = FindAnyObjectByType<KeypadController>();

            if (keypad != null)
            {
                keypad.codeLength = 4;
                keypad.keypadTitle = "DLC DOOR KEYPAD";
                keypad.hintText = "Enter your purchased access code";
                keypad.showRequestCodeButton = false;
                keypad.OnCodeSubmitted += HandleCodeSubmitted;
            }

            Debug.Log($"[Level8] Door wired (kept scene position). Keypad ready. Code: {doorCode}");
        }
        else
        {
            Debug.LogWarning("[Level8] No DoorController found. Creating fallback door.");
            CreateFallbackDoor();
        }
    }

    private void CreateFallbackDoor()
    {
        Material doorMat = CreateMat(new Color(0.35f, 0.22f, 0.12f));
        fallbackDoor = GameObject.CreatePrimitive(PrimitiveType.Cube);
        fallbackDoor.name = "DLCDoor";
        fallbackDoor.transform.position = new Vector3(0, 1.5f, 4.9f);
        fallbackDoor.transform.localScale = new Vector3(1.8f, 3f, 0.12f);
        fallbackDoor.GetComponent<Renderer>().sharedMaterial = doorMat;

        Material lockMat = CreateMat(new Color(0.7f, 0.6f, 0.1f));
        GameObject lockVis = GameObject.CreatePrimitive(PrimitiveType.Cube);
        lockVis.name = "DoorLock";
        lockVis.transform.SetParent(fallbackDoor.transform);
        lockVis.transform.localPosition = new Vector3(0.35f, 0f, -0.6f);
        lockVis.transform.localScale = new Vector3(0.08f, 0.12f, 0.08f);
        lockVis.GetComponent<Renderer>().sharedMaterial = lockMat;
    }

    private void CreatePointLight(string name, Vector3 pos, Color color,
        float range, float intensity)
    {
        GameObject obj = new GameObject(name);
        Light l = obj.AddComponent<Light>();
        l.type = LightType.Point;
        l.range = range;
        l.intensity = intensity;
        l.color = color;
        obj.transform.position = pos;
    }

    // =====================================================================
    // HUD
    // =====================================================================

    private void CreateHUD()
    {
        GameObject canvasObj = new GameObject("InteractHUD");
        canvasObj.transform.SetParent(transform);
        hudCanvas = canvasObj.AddComponent<Canvas>();
        UIHelper.ConfigureCanvas(hudCanvas, sortingOrder: 15);

        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);

        GameObject textObj = new GameObject("PromptText");
        textObj.transform.SetParent(canvasObj.transform, false);
        hudPromptText = textObj.AddComponent<Text>();
        hudPromptText.font = UIHelper.GetDefaultFont();
        hudPromptText.fontSize = 24;
        hudPromptText.alignment = TextAnchor.MiddleCenter;
        hudPromptText.color = Color.white;
        hudPromptText.raycastTarget = false;

        RectTransform rect = textObj.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.3f, 0.45f);
        rect.anchorMax = new Vector2(0.7f, 0.55f);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        hudCanvas.gameObject.SetActive(false);
    }

    // =====================================================================
    // UI Helpers
    // =====================================================================

    private GameObject CreatePanel(Transform parent, string name,
        Vector2 anchorMin, Vector2 anchorMax, Color color)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent, false);
        RectTransform rect = obj.AddComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        Image img = obj.AddComponent<Image>();
        img.color = color;
        return obj;
    }

    private Text CreateText(Transform parent, string name, string content,
        Vector2 anchorMin, Vector2 anchorMax, int fontSize, Color color)
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
        txt.alignment = TextAnchor.MiddleLeft;
        txt.color = color;
        txt.text = content;
        txt.raycastTarget = false;
        return txt;
    }

    private void CreateButton(Transform parent, string name, string label,
        Vector2 anchorCenter, Vector2 size, Color bgColor, System.Action onClick)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent, false);
        RectTransform rect = obj.AddComponent<RectTransform>();
        rect.anchorMin = anchorCenter;
        rect.anchorMax = anchorCenter;
        rect.sizeDelta = size;
        rect.anchoredPosition = Vector2.zero;

        Image bg = obj.AddComponent<Image>();
        bg.color = bgColor;

        Button btn = obj.AddComponent<Button>();
        ColorBlock cb = btn.colors;
        cb.highlightedColor = new Color(
            Mathf.Min(bgColor.r + 0.15f, 1f),
            Mathf.Min(bgColor.g + 0.15f, 1f),
            Mathf.Min(bgColor.b + 0.15f, 1f), 1f);
        cb.pressedColor = new Color(bgColor.r * 0.6f, bgColor.g * 0.6f, bgColor.b * 0.6f, 1f);
        btn.colors = cb;
        btn.onClick.AddListener(() => onClick());

        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(obj.transform, false);
        RectTransform textRect = textObj.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;
        Text txt = textObj.AddComponent<Text>();
        txt.font = UIHelper.GetDefaultFont();
        txt.fontSize = 16;
        txt.alignment = TextAnchor.MiddleCenter;
        txt.color = Color.white;
        txt.text = label;
    }

    /// <summary>Non-interactive text element placed inside a scroll row.</summary>
    private Text MakeRowText(Transform parent, string name, string content,
        Font font, int fontSize, Color color,
        Vector2 ancMin, Vector2 ancMax, TextAnchor alignment)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent, false);
        RectTransform rt = obj.AddComponent<RectTransform>();
        rt.anchorMin = ancMin;
        rt.anchorMax = ancMax;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        Text txt = obj.AddComponent<Text>();
        txt.font = font;
        txt.fontSize = fontSize;
        txt.text = content;
        txt.color = color;
        txt.alignment = alignment;
        txt.raycastTarget = false;
        return txt;
    }

    /// <summary>Clickable button placed inside a scroll row.</summary>
    private void MakeRowButton(Transform parent, string name, string label,
        Vector2 anchorCenter, Vector2 size, Color bgColor,
        Font font, System.Action onClick)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent, false);
        RectTransform rect = obj.AddComponent<RectTransform>();
        rect.anchorMin = anchorCenter;
        rect.anchorMax = anchorCenter;
        rect.sizeDelta = size;
        rect.anchoredPosition = Vector2.zero;

        Image bg = obj.AddComponent<Image>();
        bg.color = bgColor;

        Button btn = obj.AddComponent<Button>();
        ColorBlock cb = btn.colors;
        cb.highlightedColor = new Color(
            Mathf.Min(bgColor.r + 0.15f, 1f),
            Mathf.Min(bgColor.g + 0.15f, 1f),
            Mathf.Min(bgColor.b + 0.15f, 1f), 1f);
        cb.pressedColor = new Color(bgColor.r * 0.6f, bgColor.g * 0.6f, bgColor.b * 0.6f, 1f);
        btn.colors = cb;
        btn.onClick.AddListener(() => onClick());

        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(obj.transform, false);
        RectTransform textRT = textObj.AddComponent<RectTransform>();
        textRT.anchorMin = Vector2.zero;
        textRT.anchorMax = Vector2.one;
        textRT.offsetMin = textRT.offsetMax = Vector2.zero;
        Text txt = textObj.AddComponent<Text>();
        txt.font = font;
        txt.fontSize = 15;
        txt.alignment = TextAnchor.MiddleCenter;
        txt.color = Color.white;
        txt.text = label;
    }

    // =====================================================================
    // Geometry Helpers
    // =====================================================================

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
