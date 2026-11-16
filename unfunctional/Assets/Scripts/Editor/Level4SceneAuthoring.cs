using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>One-time migration to editable scene objects. Existing Level4_Art is never regenerated.</summary>
public static class Level4SceneAuthoring
{
    const string LevelPath="Assets/Scenes/LEVEL4.unity";
    const string Materials="Assets/Art/Department/Level4";
    [MenuItem("Unfunctional/Scene Art/Install Editable Level 4 and Repair Pause")]
    public static void Install()
    {
        if(EditorApplication.isPlaying){Debug.LogWarning("Stop Play mode before editing scene art.");return;}
        if(!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())return;
        var scene=SceneManager.GetSceneByPath(LevelPath);
        if(!scene.IsValid()||!scene.isLoaded)scene=EditorSceneManager.OpenScene(LevelPath,OpenSceneMode.Additive);
        SceneManager.SetActiveScene(scene);
        var existing=scene.GetRootGameObjects().FirstOrDefault(g=>g.name=="Level4_Art");
        if(existing!=null){RepairPause();Selection.activeGameObject=existing;Debug.Log("[SceneArt] Existing Level4_Art preserved. No positions or materials regenerated.");return;}

        var props=DepartmentProps.Create(null,"Level4_Art");
        SceneManager.MoveGameObjectToScene(props.gameObject,scene);
        BuildSupport(props);
        WallFinish(props);
        Directory.CreateDirectory(Materials);AssetDatabase.Refresh();
        props.Teal.name="SupportTeal";props.Paper.name="OldPaper";props.Ink.name="TelephoneBlack";
        props.Brass.name="TarnishedBrass";props.Red.name="StampRed";
        foreach(var asset in props.ReleaseGeneratedAssets())
        {
            string name=asset.name.Replace('/','_');
            AssetDatabase.CreateAsset(asset,AssetDatabase.GenerateUniqueAssetPath(Materials+"/"+name+".mat"));
        }
        var root=props.gameObject;
        Object.DestroyImmediate(props); // Saved scene contains ordinary objects, not a procedural builder.
        Undo.RegisterCreatedObjectUndo(root,"Install editable Level 4 art");
        EditorApplication.QueuePlayerLoopUpdate();SceneView.RepaintAll();
        // Let TextMesh rebuild bounds before baking the final fitted lettering scale.
        EditorApplication.delayCall+=()=>Finish(root,scene);
    }
    static void Finish(GameObject root,Scene scene)
    {
        if(root==null||!scene.isLoaded)return;
        foreach(var fit in root.GetComponentsInChildren<DepartmentLabelFit>(true))
        {
            var bounds=fit.GetComponent<Renderer>().localBounds.size;
            if(bounds.x>0&&bounds.y>0)fit.transform.localScale=Vector3.one*Mathf.Min(1,fit.width/bounds.x,fit.height/bounds.y);
            Object.DestroyImmediate(fit);
        }
        var furniture=Folder(root,"Furniture");var signs=Folder(root,"Signs");var trim=Folder(root,"Wall Trim");
        var papers=Folder(root,"Floor Mail");var lights=Folder(root,"Lighting");int cabinet=0,envelope=0;
        foreach(var child in root.transform.Cast<Transform>().ToArray())
        {
            if(child==furniture||child==signs||child==trim||child==papers||child==lights)continue;
            Transform parent=signs;
            if(child.name=="AbandonedSupportCounter"||child.name=="FilingCabinet")parent=furniture;
            if(child.name=="FilingCabinet")child.name="Filing Cabinet "+(++cabinet);
            if(child.name=="UnansweredMail"){parent=papers;child.name="Envelope "+(++envelope);}
            if(child.name=="WornTealWainscot"||child.name=="DadoRail")parent=trim;
            if(child.GetComponent<Light>()!=null)parent=lights;
            child.SetParent(parent,true);
        }
        AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
        RepairPause();Selection.activeGameObject=root;SceneView.RepaintAll();
        Debug.Log("[SceneArt] Saved editable Level4_Art in LEVEL4.unity; no runtime spawn, positioning or label fitting remains.");
    }
    static Transform Folder(GameObject root,string name)
    {
        var t=new GameObject(name).transform;t.SetParent(root.transform,false);return t;
    }
    static void WallFinish(DepartmentProps p)
    {
        var worn=p.Mat(new Color(.18f,.29f,.27f));var lib=GameArtLibrary.Load();
        if(lib!=null&&lib.concrete!=null){worn.CopyPropertiesFromMaterial(lib.concrete);worn.color=new Color(.36f,.49f,.46f);worn.mainTextureScale=new Vector2(3,1);}
        worn.name="WornTealPaint";
        p.Box("WornTealWainscot",new Vector3(3.965f,.62f,0),new Vector3(.04f,1.18f,7.8f),worn);
        p.Box("DadoRail",new Vector3(3.93f,1.24f,0),new Vector3(.04f,.055f,7.8f),p.Brass);
    }
    static void Lamp(DepartmentProps p,Vector3 at,Color color)
    {
        var t=p.Group("CounterLight",at);var light=t.gameObject.AddComponent<Light>();
        light.type=LightType.Point;light.range=7;light.intensity=2.2f;light.color=color;light.shadows=LightShadows.None;
    }
    static void RepairPause()
    {
        const string path="Assets/Scenes/GLOBAL.unity";
        var scene=SceneManager.GetSceneByPath(path);bool opened=!scene.IsValid()||!scene.isLoaded;
        if(opened)scene=EditorSceneManager.OpenScene(path,OpenSceneMode.Additive);
        var pause=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<GamePauseMenu>(true)).Single();
        PlaceButton(pause.resumeButton,.46f,.55f);
        PlaceButton(pause.restartButton,.33f,.42f);
        PlaceButton(pause.quitButton,.20f,.29f);
        if(pause.debugLevelsButton==null)
        {
            pause.debugLevelsButton=GameScreenUI.Button(pause.pausePanel.transform,"DebugLevelsButton","LEVEL SELECT (F1)",new Vector2(.35f,.07f),new Vector2(.65f,.16f),()=>{});
        }
        // Pause dominates all ordinary HUD/store canvases, and sits below debug/transition overlays.
        pause.GetComponent<Canvas>().sortingOrder=800;
        EditorUtility.SetDirty(pause);EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
        if(opened)EditorSceneManager.CloseScene(scene,true);
        AssetDatabase.SaveAssets();
    }
    static void PlaceButton(Button button,float bottom,float top)
    {
        var rect=button.GetComponent<RectTransform>();rect.anchorMin=new Vector2(.35f,bottom);rect.anchorMax=new Vector2(.65f,top);
        rect.offsetMin=rect.offsetMax=Vector2.zero;EditorUtility.SetDirty(rect);
    }
    static void BuildSupport(DepartmentProps p)
    {
        p.Sign("SupportHeader","RODNEY'S SUPPORT ANNEX",new Vector3(0,4.6f,3.81f),new Vector2(6.8f,.6f),p.Teal,p.Paper,.053f);
        var desk=p.Group("AbandonedSupportCounter",new Vector3(2.7f,0,-1.4f));
        p.Box("CounterCabinet",new Vector3(0,.52f,0),new Vector3(2.1f,1.04f,.8f),p.Teal,desk);
        p.Box("ChippedLaminateTop",new Vector3(0,1.08f,0),new Vector3(2.25f,.12f,.96f),p.Paper,desk);
        p.Box("FrontKickPlate",new Vector3(0,.12f,-.41f),new Vector3(2.1f,.16f,.02f),p.Ink,desk);
        p.Phone(desk,new Vector3(-.56f,1.2f,0));p.PaperStack(desk,new Vector3(.58f,1.15f,.06f),15);
        p.Text("BACK IN 60 SECONDS\n(since 1998)",new Vector3(0,.68f,-.412f),.032f,p.Paper,desk);
        p.Cabinet(new Vector3(3.45f,0,1.1f),90);p.Cabinet(new Vector3(3.45f,0,2.3f),90);
        p.Sign("NoticeBoard","PLEASE EMAIL YOUR COMPLAINT\n\nPHONE SUPPORT: OUT OF SERVICE",new Vector3(3.78f,3.4f,.4f),new Vector2(2.35f,1.1f),p.Wood,p.Ink,.033f,90);
        p.Sign("TicketSign","NOW SERVING\n000",new Vector3(3.78f,2.7f,-2.8f),new Vector2(1.3f,.85f),p.Ink,p.Paper,.045f,90);
        // Separate old unanswered correspondence from the existing clue notes.
        for(int i=0;i<5;i++){var r=p.Group("UnansweredMail",new Vector3(2.7f+i*.12f,.02f,-1.9f-i*.09f),i*19);p.Box("Envelope",Vector3.zero,new Vector3(.24f,.01f,.14f),p.Paper,r);}
        Lamp(p,new Vector3(1.7f,2.8f,.8f),new Color(1,.82f,.55f));
    }

}
