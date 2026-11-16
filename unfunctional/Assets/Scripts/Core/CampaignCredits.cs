using UnityEngine;
using UnityEngine.UI;

/// <summary>Readable, skippable credits. Attribution text comes from checked-in notices and package metadata.</summary>
public class CampaignCredits : MonoBehaviour
{
    private static readonly string[] Pages =
    {
        "UNFUNCTIONAL\n\nA game by The Unfunctional Team\n\nBuilt with Unity\nUniversal Render Pipeline • TextMesh Pro • Polybrush\n\nThank you to everyone who made it through the doors.",
        "ENVIRONMENT & TOOLS\n\nJungle – Tropical Vegetation / SeedMesh\nSeedMesh vegetation shaders\nModern LED Lighting Pack / PolyKebap\nKeypad FREE\n8K Skybox Pack Free • FREE Skyboxes – Space\n\nOriginal project art and level design: The Unfunctional Team",
        "SHADERS & TYPE\n\nVHS UI Effect / Turishader\nMirrors and reflections for VR / Fragilem17\nPro Glass Shader / Singular Bear\n\nLiberation Sans • Caveat • Staatliches • Open Sans • Roboto\nSee Third-party notices for bundled license texts.",
        "THANK YOU FOR PLAYING\n\nNo real purchase was needed to open the DLC door.\nNo trophy improves the durability of a shovel.\n\nYour time was the real achievement.\n\nYou can leave these credits at any time."
    };
    public static CampaignCredits Instance { get; private set; }
    private GameObject root, returnScreen, previousSelection;
    private Text body, pageLabel;
    private int page;
    private bool notices;
    public void Show(Transform parent, GameObject endingScreen)
    {
        Instance = this;
        returnScreen = endingScreen;
        previousSelection = UnityEngine.EventSystems.EventSystem.current != null
            ? UnityEngine.EventSystems.EventSystem.current.currentSelectedGameObject : null;
        if (returnScreen != null) returnScreen.SetActive(false);
        var canvas = GameScreenUI.Canvas(parent, "CreditsCanvas", 1000);
        root = canvas.gameObject;
        GameScreenUI.Panel(root.transform, "Background", new Color(0.035f, 0.045f, 0.05f), Vector2.zero, Vector2.one);
        body = GameScreenUI.Text(root.transform, "CreditsBody", "", 32, new Vector2(0.12f, 0.24f), new Vector2(0.88f, 0.91f));
        pageLabel = GameScreenUI.Text(root.transform, "CreditPage", "", 22, new Vector2(0.35f, 0.17f), new Vector2(0.65f, 0.22f));
        GameScreenUI.Button(root.transform, "PreviousCredit", "PREVIOUS", new Vector2(0.1f, 0.08f), new Vector2(0.28f, 0.15f), () => { page = Mathf.Max(0, page - 1); Refresh(); });
        GameScreenUI.Button(root.transform, "NextCredit", "NEXT", new Vector2(0.30f, 0.08f), new Vector2(0.48f, 0.15f), () => { page = Mathf.Min(PageCount - 1, page + 1); Refresh(); });
        GameScreenUI.Button(root.transform, "ThirdPartyNotices", "LICENSE NOTICES", new Vector2(0.50f, 0.08f), new Vector2(0.73f, 0.15f), () => { notices = !notices; page = 0; Refresh(); });
        var back = GameScreenUI.Button(root.transform, "CloseCredits", "BACK", new Vector2(0.76f, 0.08f), new Vector2(0.90f, 0.15f), Close);
        if (UnityEngine.EventSystems.EventSystem.current != null)
            UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(back.gameObject);
        Refresh();
    }
    private string[] NoticePages
    {
        get
        {
            var asset = Resources.Load<TextAsset>("GameArt/ThirdPartyNotices");
            return asset != null ? asset.text.Split(new[] { "\n---PAGE---\n" }, System.StringSplitOptions.None) : new[] { "Bundled notices unavailable." };
        }
    }
    private int PageCount => notices ? NoticePages.Length : Pages.Length;
    private void Refresh()
    {
        body.fontSize = notices ? 21 : 32;
        body.alignment = notices ? TextAnchor.UpperLeft : TextAnchor.MiddleCenter;
        body.text = notices ? NoticePages[page] : Pages[page];
        pageLabel.text = $"{page + 1} / {PageCount}";
    }
    public void Close() { Destroy(gameObject); }
    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
        if (root != null) Destroy(root);
        if (returnScreen != null)
        {
            returnScreen.SetActive(true);
            if (UnityEngine.EventSystems.EventSystem.current != null)
                UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(previousSelection);
        }
    }
}
