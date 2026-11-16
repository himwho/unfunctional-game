#if UNITY_EDITOR
using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public partial class CampaignSmokeDriver
{
    IEnumerator ExerciseDevelopmentMenus()
    {
        var gm=GameManager.Instance;var debug=DebugPanel.Instance;var pause=FindAnyObjectByType<GamePauseMenu>();
        Check(debug!=null&&debug.IsAvailable,"Development level selector is available in the Editor");
        debug.Open();
        Check(debug.IsOpen&&gm.CurrentState==GameManager.GameState.Paused&&Time.timeScale==0&&Cursor.lockState==CursorLockMode.None,"F1 selector pauses gameplay and releases the cursor");
        Check(!pause.pausePanel.activeSelf,"Debug selector suppresses the ordinary pause overlay");
        var rows=debug.GetComponentsInChildren<Button>(true).Where(b=>b.name.StartsWith("Jump_")).ToArray();
        Check(rows.Length==gm.levelSceneNames.Length&&rows.Select(b=>b.name).Distinct().Count()==rows.Length,"Every campaign stage has its own debug button");
        debug.Close();
        Check(gm.CurrentState==GameManager.GameState.MainMenu&&!pause.pausePanel.activeSelf,"Closing debug from the main menu restores the main-menu state");
        debug.Open();rows.Single(b=>b.name=="Jump_LEVEL4").onClick.Invoke();yield return WaitForLevel(3);
        Check(!debug.IsOpen&&gm.CurrentState==GameManager.GameState.Playing&&Cursor.lockState==CursorLockMode.Locked,"Debug jump loads Level 4 and restores FPS cursor state");
        gm.TogglePause();Canvas.ForceUpdateCanvases();
        Check(pause.pausePanel.activeSelf,"Ordinary pause menu is visible");
        var buttons=new[]{pause.resumeButton,pause.restartButton,pause.quitButton,pause.debugLevelsButton};
        for(int i=0;i<buttons.Length;i++)for(int j=i+1;j<buttons.Length;j++)
            Check(!ButtonRect(buttons[i]).Overlaps(ButtonRect(buttons[j])),"Pause buttons do not overlap: "+buttons[i].name+" / "+buttons[j].name);
        Check(pause.GetComponent<Canvas>().sortingOrder==800,"Pause renders above ordinary HUD and shop canvases");
        yield return Capture("pause-menu-fixed.png");
        pause.debugLevelsButton.onClick.Invoke();
        Check(debug.IsOpen&&!pause.pausePanel.activeSelf,"Pause-menu level-select button opens the same exclusive debug overlay");
        debug.Close();
        Check(gm.CurrentState==GameManager.GameState.Paused&&pause.pausePanel.activeSelf&&Time.timeScale==0,"Closing debug opened from pause returns to pause");
        debug.Open();rows.Single(b=>b.name=="Jump_LEVEL_CRAFTING").onClick.Invoke();
        yield return WaitForLevel(System.Array.IndexOf(gm.levelSceneNames,"LEVEL_CRAFTING"));
        Check(FindAnyObjectByType<LevelCrafting_Mandatory>()!=null&&!debug.IsOpen,"Debug can jump directly to the new crafting stage");
        debug.Open();rows.Single(b=>b.name=="Jump_LEVEL12").onClick.Invoke();
        yield return WaitForLevel(System.Array.IndexOf(gm.levelSceneNames,"LEVEL12"));
        Check(FindAnyObjectByType<Level12_Achievements>()!=null,"Debug can reach the last stage in the scrollable list");
        debug.Open();rows.Single(b=>b.name=="Jump_LEVEL1").onClick.Invoke();yield return WaitForLevel(0);
        Check(Cursor.lockState==CursorLockMode.None&&Time.timeScale==1,"Debug jump back to the UI menu restores free cursor and timescale");
        debug.Open();
        yield return null; yield return null; // Allow newly enabled canvas geometry to reach the renderer.
        var debugCanvas=debug.transform.Find("DebugLevelCanvas").GetComponent<Canvas>();
        var continueCanvas=GameObject.Find("ContinueCampaignCanvas").GetComponent<Canvas>();
        Check(debugCanvas.sortingOrder>continueCanvas.sortingOrder,"Development selector renders above the saved-game Continue controls");
        var pointer=new UnityEngine.EventSystems.PointerEventData(UnityEngine.EventSystems.EventSystem.current);
        pointer.position=ButtonRect(rows.Single(b=>b.name=="Jump_LEVEL1")).center;
        var hits=new System.Collections.Generic.List<UnityEngine.EventSystems.RaycastResult>();
        UnityEngine.EventSystems.EventSystem.current.RaycastAll(pointer,hits);
        Check(hits.Count>0&&hits[0].gameObject.name=="Jump_LEVEL1","Visible debug rows receive pointer input ahead of underlying menus");
        yield return Capture("debug-level-selector.png");
        debug.Close();
    }
    static Rect ButtonRect(Button button)
    {
        var corners=new Vector3[4];button.GetComponent<RectTransform>().GetWorldCorners(corners);
        return Rect.MinMaxRect(corners[0].x,corners[0].y,corners[2].x,corners[2].y);
    }
    IEnumerator ExerciseSceneAuthoredArt(LevelManager level)
    {
        if(level is Level4_KeypadPuzzle)
        {
            var roots=level.gameObject.scene.GetRootGameObjects();
            var art=roots.SingleOrDefault(g=>g.name=="Level4_Art");
            Check(art!=null&&art.transform.Find("Furniture")!=null&&art.transform.Find("Signs")!=null,"Level 4 loads editable scene-authored art with named groups");
            Check(art.GetComponentsInChildren<DepartmentProps>(true).Length==0&&art.GetComponentsInChildren<DepartmentLabelFit>(true).Length==0,"Level 4 props have no runtime builder or repositioning components");
            var original=art.transform.Find("Furniture/AbandonedSupportCounter").position;
            CampaignRoomArt.Dress(level);CampaignRoomArt.Dress(level);
            Check(level.gameObject.scene.GetRootGameObjects().Count(g=>g.name=="Level4_Art")==1&&level.transform.Find("DepartmentArt")==null,"Level 4 is never respawned by runtime dressing");
            Check(art.transform.Find("Furniture/AbandonedSupportCounter").position==original,"Runtime initialization preserves authored prop transforms");
            Check(art.GetComponentsInChildren<Renderer>().Where(r=>r.GetComponent<TextMesh>()==null).All(r=>r.sharedMaterial!=null&&!string.IsNullOrEmpty(UnityEditor.AssetDatabase.GetAssetPath(r.sharedMaterial))),"Baked Level 4 mesh materials survive scene serialization");
            Check(!art.GetComponentsInChildren<Collider>().Any(c=>c.enabled),"Baked Level 4 dressing leaves original interaction colliders intact");
            var signage=art.transform.Find("Signs").GetComponentsInChildren<Renderer>();
            var notes=roots.Where(g=>g.name.StartsWith("StickyNote")).SelectMany(g=>g.GetComponentsInChildren<Renderer>()).ToArray();
            Check(!signage.Any(a=>notes.Any(b=>a.bounds.Intersects(b.bounds))),"New Level 4 signage does not intersect the existing clue notes");
            yield return ArtView("level4-editable-art.png",new Vector3(-2.4f,2,-2.8f),new Vector3(1.1f,1.9f,1.5f));
        }
        if(level is Level8_DLCDoor store)
        {
            Invoke(store,"OpenShop");DebugPanel.Instance.Open();DebugPanel.Instance.Close();
            Check(Cursor.lockState==CursorLockMode.None&&GameManager.Instance.CurrentState==GameManager.GameState.Playing,"Debug close restores an open shop's cursor preference");
            store.TryCloseModal();
        }
    }
}
#endif
