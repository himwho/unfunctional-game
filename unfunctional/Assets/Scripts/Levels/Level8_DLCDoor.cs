using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// LEVEL 8: DLC Door. A room with a locked door that says "BUY DLC TO UNLOCK".
/// Interacting opens a fake in-game store with absurd microtransaction items.
/// The player must browse enough items (8+) before getting "rewarded" with free
/// credits to "purchase" the DLC door unlock. The shop UI is intentionally
/// terrible with pop-ups, upsells, and confirmations.
///
/// Builds all UI at runtime. Attach to root GameObject in LEVEL8 scene.
/// </summary>
public class Level8_DLCDoor : LevelManager
{
    [Header("Level 8 - DLC Door")]
    public DoorController doorController;

    [Header("Shop Settings")]
    public int requiredBrowseCount = 8;
    public int freeCreditsAmount = 99999;
    public int doorDLCPrice = 49999;

    // Runtime UI
    private Canvas shopCanvas;
    private Canvas hudCanvas;
    private Text hudPromptText;
    private Text creditText;

    private GameObject shopPanel;
    private GameObject itemDetailPanel;
    private GameObject confirmPanel;
    private GameObject upsellPanel;
    private Text detailTitle;
    private Text detailDesc;
    private Text detailPrice;
    private Text confirmText;
    private Text upsellText;

    private int playerCredits = 0;
    private int itemsBrowsed = 0;
    private bool shopOpen = false;
    private bool dlcPurchased = false;
    private bool rewardGiven = false;
    private HashSet<int> viewedItems = new HashSet<int>();

    // Absurd shop items
    private readonly string[][] shopItems = new string[][]
    {
        new[] { "Golden Shovel Skin", "Makes your shovel 200% more golden. Does not improve digging.", "$14.99" },
        new[] { "Premium Air DLC", "Breathe premium air! Now with 3% more oxygen.", "$9.99" },
        new[] { "Extra Gravity Pack", "Feel heavier! Perfect for that grounded experience.", "$7.99" },
        new[] { "Door Opening Sound FX", "A satisfying 'click' when doors open. Just the sound.", "$4.99" },
        new[] { "Invisible Hat", "You can't see it, but it's there. Trust us.", "$19.99" },
        new[] { "Speed Boost (0.01%)", "Barely noticeable. Technically faster.", "$12.99" },
        new[] { "HD Texture: Single Brick", "One brick, ultra high resolution. The rest stay blurry.", "$2.99" },
        new[] { "NPC Emotion Pack", "NPCs now have 2 emotions instead of 1.", "$8.99" },
        new[] { "Loot Box (Empty)", "Guaranteed to contain nothing. Collector's edition!", "$3.99" },
        new[] { "Premium Loading Screen", "Watch a fancier loading animation.", "$6.99" },
        new[] { "Day/Night Cycle", "The sky changes color sometimes. Revolutionary.", "$24.99" },
        new[] { "Extended Credits", "See 40% more names in the credits sequence.", "$1.99" },
        new[] { "VIP Queue Skip", "Skip the queue that doesn't exist. Save 0 seconds.", "$15.99" },
        new[] { "Companion Rock", "A rock follows you around. It doesn't do anything.", "$11.99" },
        new[] { "DOOR UNLOCK DLC", "Unlocks the door to proceed. The actual useful one.", "$49,999" },
    };

    protected override void Start()
    {
        base.Start();
        levelDisplayName = "The DLC Door";
        levelDescription = "Surely you can afford to continue.";
        needsPlayer = true;
        wantsCursorLocked = true;

        BuildRoom();
        CreateShopUI();
        CreateHUD();
    }

    private void Update()
    {
        if (levelComplete) return;

        if (!shopOpen)
        {
            UpdateInteraction();
        }
        else
        {
            // While shop is open, show cursor
            if (Input.GetKeyDown(KeyCode.Escape))
                CloseShop();
        }
    }

    // =========================================================================
    // Interaction
    // =========================================================================

    private void UpdateInteraction()
    {
        Camera cam = Camera.main;
        if (cam == null) return;

        bool lookingAtDoor = false;

        Ray ray = new Ray(cam.transform.position, cam.transform.forward);
        if (Physics.Raycast(ray, out RaycastHit hit, 4f, ~0, QueryTriggerInteraction.Collide))
        {
            // Check if hit is on the door
            Transform root = hit.collider.transform.root;
            if (doorController != null && root == doorController.transform)
                lookingAtDoor = true;
            else if (hit.collider.gameObject.name.Contains("Door"))
                lookingAtDoor = true;
        }

        if (lookingAtDoor)
        {
            if (!dlcPurchased)
            {
                hudPromptText.text = "Press [E] to open STORE";
                if (Input.GetKeyDown(KeyCode.E))
                    OpenShop();
            }
            else
            {
                hudPromptText.text = "Door unlocked!";
            }
            hudCanvas.gameObject.SetActive(true);
        }
        else
        {
            hudCanvas.gameObject.SetActive(false);
        }
    }

    // =========================================================================
    // Shop UI
    // =========================================================================

    private void OpenShop()
    {
        shopOpen = true;
        shopPanel.SetActive(true);
        shopCanvas.gameObject.SetActive(true);

        // Unlock cursor for shop
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // Disable player look
        PlayerController pc = FindAnyObjectByType<PlayerController>();
        if (pc != null) pc.enabled = false;

        UpdateCreditDisplay();
    }

    private void CloseShop()
    {
        shopOpen = false;
        shopCanvas.gameObject.SetActive(false);
        itemDetailPanel.SetActive(false);
        confirmPanel.SetActive(false);
        upsellPanel.SetActive(false);

        // Re-lock cursor
        ApplyCursorState();

        // Re-enable player look
        PlayerController pc = FindAnyObjectByType<PlayerController>();
        if (pc != null) pc.enabled = true;
    }

    private void CreateShopUI()
    {
        // -- Canvas --
        GameObject canvasObj = new GameObject("ShopCanvas");
        canvasObj.transform.SetParent(transform);
        shopCanvas = canvasObj.AddComponent<Canvas>();
        shopCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        shopCanvas.sortingOrder = 50;

        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        canvasObj.AddComponent<GraphicRaycaster>();

        // -- Shop Panel (full screen) --
        shopPanel = CreatePanel(canvasObj.transform, "ShopPanel",
            Vector2.zero, Vector2.one, new Color(0.05f, 0.05f, 0.08f, 0.95f));

        // Title
        CreateText(shopPanel.transform, "ShopTitle", "UNFUNCTIONAL PREMIUM STORE",
            new Vector2(0.05f, 0.88f), new Vector2(0.7f, 0.97f),
            40, new Color(1f, 0.84f, 0f));

        // Subtitle
        CreateText(shopPanel.transform, "ShopSubtitle",
            "\"Where every purchase is a mistake you can't refund\"",
            new Vector2(0.05f, 0.83f), new Vector2(0.7f, 0.88f),
            18, new Color(0.5f, 0.5f, 0.5f));

        // Credits display
        creditText = CreateText(shopPanel.transform, "Credits",
            $"Credits: {playerCredits}",
            new Vector2(0.7f, 0.88f), new Vector2(0.95f, 0.97f),
            28, new Color(0.3f, 1f, 0.3f));

        // Close button
        CreateButton(shopPanel.transform, "CloseBtn", "X",
            new Vector2(0.95f, 0.92f), new Vector2(50, 50),
            new Color(0.6f, 0.1f, 0.1f), () => CloseShop());

        // -- Scrollable item grid --
        float startY = 0.78f;
        float itemHeight = 0.08f;
        float gap = 0.01f;

        for (int i = 0; i < shopItems.Length; i++)
        {
            float yTop = startY - i * (itemHeight + gap);
            float yBot = yTop - itemHeight;
            int itemIndex = i;

            GameObject itemRow = CreatePanel(shopPanel.transform, $"Item_{i}",
                new Vector2(0.05f, yBot), new Vector2(0.95f, yTop),
                i % 2 == 0 ? new Color(0.12f, 0.12f, 0.15f) : new Color(0.1f, 0.1f, 0.13f));

            // Item name
            CreateText(itemRow.transform, "ItemName", shopItems[i][0],
                new Vector2(0.02f, 0f), new Vector2(0.5f, 1f),
                20, Color.white);

            // Item price
            Color priceColor = i == shopItems.Length - 1
                ? new Color(1f, 0.3f, 0.3f) : new Color(0.3f, 1f, 0.3f);
            CreateText(itemRow.transform, "ItemPrice", shopItems[i][2],
                new Vector2(0.75f, 0f), new Vector2(0.98f, 1f),
                20, priceColor);

            // Browse button
            CreateButton(itemRow.transform, "ViewBtn", "VIEW",
                new Vector2(0.6f, 0.5f), new Vector2(80, 30),
                new Color(0.2f, 0.3f, 0.5f),
                () => OnViewItem(itemIndex));
        }

        // -- Item Detail Panel --
        itemDetailPanel = CreatePanel(canvasObj.transform, "ItemDetailPanel",
            new Vector2(0.2f, 0.2f), new Vector2(0.8f, 0.8f),
            new Color(0.08f, 0.08f, 0.12f, 0.98f));

        detailTitle = CreateText(itemDetailPanel.transform, "DetailTitle", "",
            new Vector2(0.05f, 0.75f), new Vector2(0.95f, 0.95f),
            32, new Color(1f, 0.84f, 0f));

        detailDesc = CreateText(itemDetailPanel.transform, "DetailDesc", "",
            new Vector2(0.05f, 0.4f), new Vector2(0.95f, 0.75f),
            20, new Color(0.7f, 0.7f, 0.7f));

        detailPrice = CreateText(itemDetailPanel.transform, "DetailPrice", "",
            new Vector2(0.05f, 0.25f), new Vector2(0.95f, 0.4f),
            28, new Color(0.3f, 1f, 0.3f));

        CreateButton(itemDetailPanel.transform, "BuyBtn", "BUY NOW",
            new Vector2(0.5f, 0.1f), new Vector2(200, 50),
            new Color(0.2f, 0.5f, 0.2f),
            () => OnTryBuy());

        CreateButton(itemDetailPanel.transform, "BackBtn", "BACK",
            new Vector2(0.5f, 0.03f), new Vector2(100, 35),
            new Color(0.3f, 0.3f, 0.3f),
            () => itemDetailPanel.SetActive(false));

        itemDetailPanel.SetActive(false);

        // -- Confirm Panel --
        confirmPanel = CreatePanel(canvasObj.transform, "ConfirmPanel",
            new Vector2(0.25f, 0.3f), new Vector2(0.75f, 0.7f),
            new Color(0.06f, 0.06f, 0.1f, 0.98f));

        confirmText = CreateText(confirmPanel.transform, "ConfirmText", "",
            new Vector2(0.05f, 0.3f), new Vector2(0.95f, 0.9f),
            22, Color.white);

        CreateButton(confirmPanel.transform, "ConfirmYes", "YES, I'M SURE",
            new Vector2(0.3f, 0.08f), new Vector2(160, 40),
            new Color(0.2f, 0.5f, 0.2f),
            () => OnConfirmBuy());

        CreateButton(confirmPanel.transform, "ConfirmNo", "NO, WAIT",
            new Vector2(0.7f, 0.08f), new Vector2(160, 40),
            new Color(0.5f, 0.2f, 0.2f),
            () => confirmPanel.SetActive(false));

        confirmPanel.SetActive(false);

        // -- Upsell Panel --
        upsellPanel = CreatePanel(canvasObj.transform, "UpsellPanel",
            new Vector2(0.2f, 0.25f), new Vector2(0.8f, 0.75f),
            new Color(0.1f, 0.05f, 0.05f, 0.98f));

        upsellText = CreateText(upsellPanel.transform, "UpsellText",
            "Are you SURE you don't want to buy anything?\n\n" +
            "These deals won't last forever!\n" +
            "(They will, actually. They're fake.)",
            new Vector2(0.05f, 0.3f), new Vector2(0.95f, 0.9f),
            22, new Color(1f, 0.7f, 0.7f));

        CreateButton(upsellPanel.transform, "UpsellOK", "FINE, I'LL KEEP BROWSING",
            new Vector2(0.5f, 0.08f), new Vector2(300, 45),
            new Color(0.3f, 0.3f, 0.5f),
            () => upsellPanel.SetActive(false));

        upsellPanel.SetActive(false);

        shopCanvas.gameObject.SetActive(false);
    }

    private int currentViewItem = -1;

    private void OnViewItem(int index)
    {
        currentViewItem = index;
        viewedItems.Add(index);
        itemsBrowsed = viewedItems.Count;

        detailTitle.text = shopItems[index][0];
        detailDesc.text = shopItems[index][1];
        detailPrice.text = shopItems[index][2];

        itemDetailPanel.SetActive(true);
        confirmPanel.SetActive(false);

        // Check if player has browsed enough for free credits
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
                confirmText.text = "Purchase DOOR UNLOCK DLC for 49,999 credits?\n\n" +
                    "(This is literally the only useful item in the store.)";
                confirmPanel.SetActive(true);
            }
            else
            {
                confirmText.text = "NOT ENOUGH CREDITS!\n\nKeep browsing to earn free credits.\n" +
                    $"(You have {playerCredits}, you need {doorDLCPrice})";
                confirmPanel.SetActive(true);
            }
        }
        else
        {
            // Any other item -- show upsell popup
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

        // Dramatic credit counter
        int target = freeCreditsAmount;
        float duration = 2f;
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

    private IEnumerator PurchaseSequence()
    {
        yield return new WaitForSeconds(1f);

        CloseShop();

        yield return new WaitForSeconds(0.5f);

        // Open the door
        if (doorController != null)
        {
            doorController.OpenDoor();
            yield return new WaitForSeconds(2f);
        }

        CompleteLevel();
    }

    private void UpdateCreditDisplay()
    {
        if (creditText != null)
            creditText.text = $"Credits: {playerCredits:N0}";
    }

    // =========================================================================
    // Room Geometry
    // =========================================================================

    private void BuildRoom()
    {
        float roomW = 10f, roomH = 4f, roomD = 10f, wt = 0.2f;
        Material wallMat = CreateMat(new Color(0.45f, 0.45f, 0.48f));
        Material floorMat = CreateMat(new Color(0.3f, 0.3f, 0.32f));

        CreateBox("Floor", new Vector3(0, -wt / 2f, 0), new Vector3(roomW, wt, roomD), floorMat);
        CreateBox("Ceiling", new Vector3(0, roomH + wt / 2f, 0), new Vector3(roomW, wt, roomD), wallMat);
        CreateBox("WallSouth", new Vector3(0, roomH / 2f, -roomD / 2f), new Vector3(roomW, roomH, wt), wallMat);
        CreateBox("WallNorth", new Vector3(0, roomH / 2f, roomD / 2f), new Vector3(roomW, roomH, wt), wallMat);
        CreateBox("WallWest", new Vector3(-roomW / 2f, roomH / 2f, 0), new Vector3(wt, roomH, roomD), wallMat);
        CreateBox("WallEast", new Vector3(roomW / 2f, roomH / 2f, 0), new Vector3(wt, roomH, roomD), wallMat);

        // DLC sign on the north wall
        GameObject sign = GameObject.CreatePrimitive(PrimitiveType.Quad);
        sign.name = "DLCSign";
        sign.transform.position = new Vector3(0, 2.5f, roomD / 2f - 0.11f);
        sign.transform.localScale = new Vector3(3f, 1f, 1f);
        sign.GetComponent<Renderer>().sharedMaterial = CreateMat(new Color(0.8f, 0.2f, 0.2f));

        // Spawn point
        GameObject sp = new GameObject("PlayerSpawnPoint");
        sp.transform.position = new Vector3(0, 1f, -3f);
        sp.transform.rotation = Quaternion.LookRotation(Vector3.forward);
        playerSpawnPoint = sp.transform;

        // Light
        GameObject lightObj = new GameObject("RoomLight");
        Light light = lightObj.AddComponent<Light>();
        light.type = LightType.Point;
        light.range = 15f;
        light.intensity = 1.2f;
        light.color = new Color(1f, 0.95f, 0.8f);
        lightObj.transform.position = new Vector3(0, 3.5f, 0);
    }

    // =========================================================================
    // HUD
    // =========================================================================

    private void CreateHUD()
    {
        GameObject canvasObj = new GameObject("InteractHUD");
        canvasObj.transform.SetParent(transform);
        hudCanvas = canvasObj.AddComponent<Canvas>();
        hudCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        hudCanvas.sortingOrder = 15;

        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);

        GameObject textObj = new GameObject("PromptText");
        textObj.transform.SetParent(canvasObj.transform, false);
        hudPromptText = textObj.AddComponent<Text>();
        hudPromptText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
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

    // =========================================================================
    // UI Helpers
    // =========================================================================

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
        txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
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
        btn.onClick.AddListener(() => onClick());

        // Text child
        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(obj.transform, false);
        RectTransform textRect = textObj.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;
        Text txt = textObj.AddComponent<Text>();
        txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        txt.fontSize = 16;
        txt.alignment = TextAnchor.MiddleCenter;
        txt.color = Color.white;
        txt.text = label;
    }

    // =========================================================================
    // Geometry Helpers
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
