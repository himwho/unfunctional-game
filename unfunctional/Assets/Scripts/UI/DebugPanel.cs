using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>Development-only modal level selector. Uses the same scene-loading path as the campaign.</summary>
public class DebugPanel : MonoBehaviour
{
    public static DebugPanel Instance { get; private set; }
    public KeyCode toggleKey = KeyCode.F1;
    public bool allowInReleaseBuild = false;
    public bool IsOpen { get; private set; }
    public bool IsAvailable { get; private set; }
    public int SelectedLevel { get; private set; }

    static readonly string[] Names = { "Confusing menu", "Settings puzzle", "Wall clipping", "Rodney's support annex",
        "Gorp dialogue", "Quick-time events", "Compass hallways", "DLC showroom", "Walking simulator",
        "Tool workshop", "Bad RNG", "Second-person shooter", "Mandatory crafting", "Achievement finale" };
    readonly List<Button> rows = new List<Button>();
    Canvas canvas;
    ScrollRect scroll;
    RectTransform content;
    Text currentStage;
    Button nextButton;
    GameManager manager;
    GameManager.GameState returnState;

    void Awake()
    {
        Instance = this;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        IsAvailable = true;
#else
        IsAvailable = allowInReleaseBuild;
#endif
    }
    void Start()
    {
        manager = GameManager.Instance;
        if (manager != null) manager.OnGameStateChanged += StateChanged;
    }
    public void Toggle() { if (IsOpen) Close(); else Open(); }
    public void Open()
    {
        manager = GameManager.Instance;
        if (!IsAvailable || IsOpen || manager == null || manager.IsLoading || CampaignCredits.Instance != null) return;
        if (canvas == null) Build();
        returnState = manager.CurrentState;
        IsOpen = true; // Suppress the ordinary pause panel before broadcasting the paused state.
        manager.SetState(GameManager.GameState.Paused);
        canvas.gameObject.SetActive(true);
        SelectedLevel = Mathf.Clamp(manager.CurrentLevelIndex, 0, rows.Count - 1);
        currentStage.text = "Current: " + manager.levelSceneNames[SelectedLevel] + "   |   Choose a stage to load";
        nextButton.interactable = manager.CurrentLevelIndex + 1 < rows.Count;
        if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);
        UnlockCursor();
        Canvas.ForceUpdateCanvases();
        HighlightSelection();
    }
    public void Close()
    {
        if (!IsOpen) return;
        Hide();
        if (manager != null && !manager.IsLoading)
        {
            manager.SetState(returnState);
            if (returnState == GameManager.GameState.Paused || returnState == GameManager.GameState.Completed) UnlockCursor();
            else
            {
                var level = FindAnyObjectByType<LevelManager>();
                if (level != null) level.ApplyCursorState();
            }
        }
    }
    public bool JumpToLevel(int index)
    {
        if (!IsAvailable || !IsOpen || manager == null || manager.IsLoading || index < 0 || index >= rows.Count) return false;
        if (!Application.CanStreamedLevelBeLoaded(manager.levelSceneNames[index])) return false;
        Hide();
        manager.LoadLevel(index); // Transition restores timescale, spawns the player and applies cursor preference.
        return true;
    }
    void Hide()
    {
        IsOpen = false;
        if (canvas != null) canvas.gameObject.SetActive(false);
        if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);
    }
    void StateChanged(GameManager.GameState state)
    {
        if (IsOpen && state != GameManager.GameState.Paused) Hide();
    }
    static void UnlockCursor()
    {
        if (InputManager.Instance != null) InputManager.Instance.UnlockCursor();
        else { Cursor.lockState = CursorLockMode.None; Cursor.visible = true; }
    }
    void Update()
    {
        if (!IsOpen) return;
        int selection = SelectedLevel;
        if (Input.GetKeyDown(KeyCode.DownArrow)) selection++;
        if (Input.GetKeyDown(KeyCode.UpArrow)) selection--;
        if (Input.GetKeyDown(KeyCode.PageDown)) selection += 5;
        if (Input.GetKeyDown(KeyCode.PageUp)) selection -= 5;
        if (Input.GetKeyDown(KeyCode.Home)) selection = 0;
        if (Input.GetKeyDown(KeyCode.End)) selection = rows.Count - 1;
        if (selection != SelectedLevel) { SelectedLevel = Mathf.Clamp(selection, 0, rows.Count - 1); HighlightSelection(); }
        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter)) JumpToLevel(SelectedLevel);
    }
    void HighlightSelection()
    {
        for (int i = 0; i < rows.Count; i++)
            rows[i].GetComponent<Image>().color = i == SelectedLevel ? new Color(.22f,.43f,.39f) : new Color(.15f,.19f,.20f);
        // Keep the highlighted row visible even at small Game-view sizes.
        float overflow = Mathf.Max(0, content.rect.height - scroll.viewport.rect.height);
        float top = SelectedLevel * 54f;
        float offset = content.anchoredPosition.y;
        if (top < offset) offset = top;
        else if (top + 48 > offset + scroll.viewport.rect.height) offset = top + 48 - scroll.viewport.rect.height;
        content.anchoredPosition = new Vector2(0, Mathf.Clamp(offset, 0, overflow));
    }
    void Build()
    {
        canvas = GameScreenUI.Canvas(transform, "DebugLevelCanvas", 980);
        canvas.GetComponent<CanvasScaler>().referenceResolution = new Vector2(1280,720);
        GameScreenUI.Panel(canvas.transform, "InputBlocker", new Color(.015f,.025f,.03f,.98f), Vector2.zero, Vector2.one);
        var panel = GameScreenUI.Panel(canvas.transform, "LevelSelector", new Color(.06f,.10f,.11f), new Vector2(.08f,.05f), new Vector2(.92f,.95f));
        GameScreenUI.Text(panel.transform,"Title","DEVELOPMENT / LEVEL SELECT",32,new Vector2(.04f,.88f),new Vector2(.96f,.97f),TextAnchor.MiddleLeft);
        currentStage = GameScreenUI.Text(panel.transform,"CurrentStage","",21,new Vector2(.04f,.81f),new Vector2(.96f,.88f),TextAnchor.MiddleLeft);
        var viewport = GameScreenUI.Panel(panel.transform,"StageViewport",new Color(.03f,.05f,.06f),new Vector2(.04f,.21f),new Vector2(.96f,.79f));
        viewport.gameObject.AddComponent<RectMask2D>();
        scroll = viewport.gameObject.AddComponent<ScrollRect>();
        scroll.horizontal = false; scroll.vertical = true; scroll.movementType = ScrollRect.MovementType.Clamped; scroll.scrollSensitivity = 36;
        scroll.viewport = viewport.rectTransform;
        content = new GameObject("Stages",typeof(RectTransform)).GetComponent<RectTransform>();
        content.SetParent(viewport.transform,false); content.anchorMin = new Vector2(0,1); content.anchorMax = Vector2.one;
        content.pivot = new Vector2(.5f,1); content.sizeDelta = new Vector2(0,manager.levelSceneNames.Length*54f);
        content.anchoredPosition = Vector2.zero; scroll.content = content;
        for (int i=0;i<manager.levelSceneNames.Length;i++)
        {
            int index = i; string scene = manager.levelSceneNames[i];
            string title = i < Names.Length ? Names[i] : scene;
            var button = GameScreenUI.Button(content,"Jump_"+scene,$"{i+1:00}   {title}   ({scene})",Vector2.zero,Vector2.one,()=>JumpToLevel(index));
            var rect = button.GetComponent<RectTransform>();rect.anchorMin = new Vector2(0,1);rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(.5f,1);rect.sizeDelta = new Vector2(-12,48);rect.anchoredPosition = new Vector2(0,-i*54f);
            button.navigation = new Navigation { mode = Navigation.Mode.None };
            var label = button.GetComponentInChildren<Text>();label.fontSize=23;label.alignment=TextAnchor.MiddleLeft;
            label.rectTransform.offsetMin = new Vector2(16,0);rows.Add(button);
        }
        GameScreenUI.Text(panel.transform,"Controls","Click a stage  /  Up-Down + Enter  /  F1 or Esc to close",19,new Vector2(.04f,.145f),new Vector2(.96f,.20f));
        GameScreenUI.Button(panel.transform,"CloseDebug","BACK",new Vector2(.04f,.04f),new Vector2(.32f,.125f),Close);
        GameScreenUI.Button(panel.transform,"ReloadStage","RELOAD CURRENT",new Vector2(.35f,.04f),new Vector2(.65f,.125f),()=>JumpToLevel(manager.CurrentLevelIndex));
        nextButton = GameScreenUI.Button(panel.transform,"NextStage","NEXT LEVEL",new Vector2(.68f,.04f),new Vector2(.96f,.125f),()=>JumpToLevel(manager.CurrentLevelIndex+1));
        canvas.gameObject.SetActive(false);
    }
    void OnDestroy()
    {
        if (manager != null) manager.OnGameStateChanged -= StateChanged;
        if (Instance == this) Instance = null;
    }
}
