using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>One-time scene migration. Existing art roots and authored positions are preserved.</summary>
public static class DepartmentSceneAuthoring
{
    static readonly List<Scene> pending=new List<Scene>();
    static SceneSetup[] previousSetup;
    [MenuItem("Unfunctional/Scene Art/Install Editable Departments")]
    public static void Install()
    {
        if(EditorApplication.isPlaying || !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())return;
        previousSetup=EditorSceneManager.GetSceneManagerSetup();pending.Clear();
        foreach(string name in new[]{"LEVEL4","LEVEL8","LEVEL11","LEVEL12"})
        {
            string path="Assets/Scenes/"+name+".unity";
            var scene=SceneManager.GetSceneByPath(path);
            if(!scene.IsValid()||!scene.isLoaded)scene=EditorSceneManager.OpenScene(path,OpenSceneMode.Additive);
            SceneManager.SetActiveScene(scene);pending.Add(scene);
            var level=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<LevelManager>(true)).Single();
            if(name!="LEVEL4")
            {
                string rootName=name.Replace("LEVEL","Level")+"_Art";
                if(!scene.GetRootGameObjects().Any(g=>g.name==rootName))
                {
                    var props=DepartmentSceneComposition.Create(level);
                    SceneManager.MoveGameObjectToScene(props.gameObject,scene);
                    SaveMaterials(props.ReleaseGeneratedAssets(),name);
                    var root=props.gameObject;Object.DestroyImmediate(props);
                    GroupDecoration(root);
                    if(level is Level12_Achievements finale)
                    {
                        var existing=level.transform.Cast<Transform>().ToArray();
                        finale.BuildAuthoredRoom();
                        var room=Folder(root,"Room and Interactions");
                        foreach(var child in level.transform.Cast<Transform>().Except(existing).ToArray())child.SetParent(room,true);
                        SaveMaterials(finale.ReleaseAuthoredMaterials().Cast<Object>(),name);
                        EditorUtility.SetDirty(finale);
                    }
                    Undo.RegisterCreatedObjectUndo(root,"Install editable department art");
                }
            }
            if(level is Level4_KeypadPuzzle support && Mathf.Approximately(support.interactRange,1f))
            {
                // The scaled player cannot bring its camera within one metre of the lower keys.
                support.interactRange=1.5f;EditorUtility.SetDirty(support);
            }
            if(level is Level8_DLCDoor)
            {
                var art=scene.GetRootGameObjects().Single(g=>g.name=="Level8_Art");
                foreach(var t in art.GetComponentsInChildren<Transform>())
                {
                    if(t.name=="PremiumHeader" && Vector3.Distance(t.position,new Vector3(0,3.55f,3.79f))<.001f)t.position=new Vector3(0,4.6f,3.79f);
                    if(t.name=="SaleSign" && Vector3.Distance(t.position,new Vector3(-2.6f,2.7f,3.78f))<.001f)t.position=new Vector3(-2.6f,3.55f,3.78f);
                }
            }
            if(level is Level12_Achievements)FitExistingFinaleDoor(scene);
        }
        EditorApplication.QueuePlayerLoopUpdate();SceneView.RepaintAll();EditorApplication.delayCall+=Finish;
    }
    static void Finish()
    {
        foreach(var scene in pending)
        {
            foreach(var fit in scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<DepartmentLabelFit>(true)))
            {
                var bounds=fit.GetComponent<Renderer>().localBounds.size;
                if(bounds.x>0 && bounds.y>0)fit.transform.localScale=Vector3.one*Mathf.Min(1,fit.width/bounds.x,fit.height/bounds.y);
                Object.DestroyImmediate(fit);
            }
            EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
        }
        AssetDatabase.SaveAssets();pending.Clear();EditorSceneManager.RestoreSceneManagerSetup(previousSetup);
        Debug.Log("[SceneArt] Saved editable LEVEL8, LEVEL11 and LEVEL12 art. Existing art roots preserved.");
    }
    static void SaveMaterials(IEnumerable<Object> objects,string name)
    {
        string folder="Assets/Art/Department/"+name.Replace("LEVEL","Level");Directory.CreateDirectory(folder);AssetDatabase.Refresh();
        int index=0;
        foreach(var obj in objects)
        {
            if(obj==null || !string.IsNullOrEmpty(AssetDatabase.GetAssetPath(obj)))continue;
            obj.name="DepartmentMaterial_"+(++index);
            AssetDatabase.CreateAsset(obj,AssetDatabase.GenerateUniqueAssetPath(folder+"/"+obj.name+".mat"));
        }
    }
    static Transform Folder(GameObject root,string name)
    {var t=new GameObject(name).transform;t.SetParent(root.transform,false);return t;}
    static void GroupDecoration(GameObject root)
    {
        var children=root.transform.Cast<Transform>().ToArray();
        var furniture=Folder(root,"Furniture and Displays");var signs=Folder(root,"Signs and Certificates");
        var trim=Folder(root,"Wall and Floor Dressing");var lights=Folder(root,"Lighting");
        foreach(var child in children)
        {
            Transform parent=furniture;
            if(child.GetComponentInChildren<TextMesh>()!=null && child.name!="FilingCabinet" && child.name!="ClaimsDesk")parent=signs;
            if(child.name=="WornTealWainscot"||child.name=="DadoRail"||child.name=="WornRedRunner")parent=trim;
            if(child.GetComponent<Light>()!=null)parent=lights;
            child.SetParent(parent,true);
        }
    }
    static void FitExistingFinaleDoor(Scene scene)
    {
        var exit=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>()).Single(t=>t.name=="AchievementExit");
        if(exit.Find("DoorVisual")!=null || exit.childCount==0)return;
        var children=exit.Cast<Transform>().ToArray();var renderers=exit.GetComponentsInChildren<Renderer>();
        if(renderers.Length==0)return;
        var bounds=renderers[0].bounds;foreach(var r in renderers)bounds.Encapsulate(r.bounds);
        var visual=new GameObject("DoorVisual").transform;visual.SetParent(exit,false);
        foreach(var child in children)child.SetParent(visual,true);
        visual.localScale=new Vector3(2.4f/bounds.size.x,3.8f/bounds.size.y,3.8f/bounds.size.y);
    }
}
