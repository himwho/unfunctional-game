#if UNITY_EDITOR
using System.Collections;
using System.Linq;
using UnityEngine;

public partial class CampaignSmokeDriver
{
    IEnumerator ExerciseSceneInteractions()
    {
        yield return WaitForLevel(0);
        foreach(int index in new[]{3,7,10,13})
        {
            GameManager.Instance.LoadLevel(index);yield return WaitForLevel(index);
            var level=FindAnyObjectByType<LevelManager>();
            yield return ExerciseDepartmentArt(level);
            yield return ExerciseEditableDepartments(level);
        }
    }
    IEnumerator ExerciseEditableDepartments(LevelManager level)
    {
        if(level is Level8_DLCDoor || level is Level11_BadRNG || level is Level12_Achievements)
        {
            string name=level.gameObject.scene.name.Replace("LEVEL","Level")+"_Art";
            var root=level.gameObject.scene.GetRootGameObjects().SingleOrDefault(g=>g.name==name);
            Check(root!=null,"Editable scene art loaded: "+name);
            Check(root.GetComponentsInChildren<DepartmentProps>(true).Length==0 && root.GetComponentsInChildren<DepartmentLabelFit>(true).Length==0,"No runtime art builders or label fitters: "+name);
            var sample=root.transform.GetChild(0);var position=sample.position;
            sample.position+=Vector3.right*.13f;CampaignRoomArt.Dress(level);
            Check(sample.position==position+Vector3.right*.13f && level.transform.Find("DepartmentArt")==null,"Authored transforms survive runtime initialization: "+name);
            sample.position=position;
            Check(root.GetComponentsInChildren<Renderer>(true).Where(r=>r.GetComponent<TextMesh>()==null && r.name!="ExitIndicator").All(r=>r.sharedMaterials.All(m=>m!=null&&!string.IsNullOrEmpty(UnityEditor.AssetDatabase.GetAssetPath(m)))),"Scene art materials survive serialization: "+name);
            if(level is Level12_Achievements)
            {
                Check(root.transform.Find("Room and Interactions")!=null && Get<GameObject>(level,"plaque").transform.IsChildOf(root.transform),"Finale room and interactive exhibits are authored scene objects");
                var exit=Get<GameObject>(level,"exit").GetComponent<BoxCollider>();
                Check(exit.size.y>=3.7f && exit.size.x>=2.3f,"Finale door is proportioned for the unchanged three-metre player");
            }
            else Check(!root.GetComponentsInChildren<Collider>(true).Any(c=>c.enabled),"Editable decorations do not add interaction blockers: "+name);
        }
        if(level is Level8_DLCDoor)
        {
            var physical=Get<Transform>(level,"physicalKeypadRoot");
            var keys=physical.GetComponentsInChildren<WorldKeypadButton>().Select(b=>b.GetComponent<Collider>().bounds).ToArray();
            var art=level.gameObject.scene.GetRootGameObjects().Single(g=>g.name=="Level8_Art");
            Check(!art.transform.Find("Signs and Certificates").GetComponentsInChildren<Renderer>().Any(r=>keys.Any(b=>r.bounds.Intersects(b))),"Showroom signage leaves every keypad button visually exposed");
        }
        if(level is Level4_KeypadPuzzle || level is Level8_DLCDoor)
            yield return ExercisePhysicalKeypad(level);
        if(level is Level4_KeypadPuzzle || level is Level8_DLCDoor || level is Level11_BadRNG)
            yield return ExerciseDoorPassage(level);
        if(level is Level11_BadRNG rngExit)yield return ExerciseRngExitTraversal(rngExit);
    }
    IEnumerator ExercisePhysicalKeypad(LevelManager level)
    {
        var player=GameManager.Instance.CurrentPlayer;var movement=player.GetComponent<PlayerController>();
        var controller=player.GetComponent<CharacterController>();var cam=Camera.main;
        Vector3 oldPosition=player.transform.position;Quaternion oldRotation=cam.transform.rotation;
        movement.enabled=false;
        var keypad=Get<KeypadController>(level,"keypad");keypad.ClearInput();
        var buttons=Get<Transform>(level,"physicalKeypadRoot").GetComponentsInChildren<WorldKeypadButton>();
        Check(buttons.Length==11,"All ten physical digits and submit target are bound: "+level.name);
        foreach(var button in buttons.OrderBy(b=>b.GetPromptLabel()))
        {
            var target=button.GetComponent<Collider>().bounds.center;
            controller.enabled=false;player.transform.position=new Vector3(target.x,1.52f,target.z-1.0f);controller.enabled=true;
            var point=PhysicalKeypadAim.ScreenPoint(cam,190,-320);
            var direction=cam.ScreenPointToRay(point).direction;
            cam.transform.rotation=Quaternion.FromToRotation(direction,(target-cam.transform.position).normalized)*cam.transform.rotation;
            Physics.SyncTransforms();
            var actual=(WorldKeypadButton)Invoke(level,"GetHoveredKeypadButton");
            Check(actual==button,"Physical aim reaches digit "+button.GetPromptLabel()+" from a standing player: "+level.name+" (distance "+Vector3.Distance(cam.transform.position,target).ToString("F2")+", found "+(actual!=null?actual.name:"none")+")");
            keypad.ClearInput();yield return (IEnumerator)Invoke(level,"PressKeypadButtonAfterDelay",.01f,actual);
            Check(keypad.EnteredCode==(button.GetPromptLabel()=="OK"?"":button.GetPromptLabel()),"Aimed physical press enters digits and rejects incomplete submission: "+level.name);
        }
        Check(PhysicalKeypadAim.ScreenPoint(cam,190,-320).y>cam.pixelRect.yMin,"Keypad interaction ray stays in the current Game view");
        yield return null;yield return Capture(level is Level4_KeypadPuzzle?"keypad-level4.png":"keypad-level8.png");
        keypad.ClearInput();controller.enabled=false;player.transform.position=oldPosition;controller.enabled=true;cam.transform.rotation=oldRotation;movement.enabled=true;
    }
    IEnumerator ExerciseRngExitTraversal(Level11_BadRNG rng)
    {
        var barrier=rng.optionADoor.GetComponent<BoxCollider>();var bounds=barrier.bounds;
        rng.doorSpawnChance=1.1f; // Deterministically exercise success, separate from the existing forced-failure test.
        Invoke(rng,"OnChooseOptionA");yield return new WaitForSecondsRealtime(3f);
        Check(!barrier.enabled,"Winning RNG roll releases the stationary door barrier");
        var player=GameManager.Instance.CurrentPlayer;var movement=player.GetComponent<PlayerController>();
        var cc=player.GetComponent<CharacterController>();movement.enabled=false;
        cc.enabled=false;player.transform.position=new Vector3(bounds.center.x,1.52f,bounds.center.z-1.6f);cc.enabled=true;Physics.SyncTransforms();
        float threshold=rng.optionADoor.transform.position.z+.5f;bool crossed=false;
        for(int i=0;i<120;i++)
        {
            cc.Move(new Vector3(0,-.015f,.04f));
            if(player.transform.position.z>threshold){crossed=true;break;}
            yield return new WaitForFixedUpdate();
        }
        if(!crossed)
        {
            var centre=player.transform.position;
            var hits=Physics.OverlapCapsule(centre+Vector3.up*.75f,centre-Vector3.up*.75f,.85f,~0,QueryTriggerInteraction.Ignore);
            Debug.Log("[DoorProbe] Player="+centre+" contacts="+string.Join("; ",hits.Select(c=>c.name+" "+c.GetType().Name+" "+c.bounds)));
            Debug.Log("[DoorProbe] Leaf="+rng.optionADoor.doorPanel.transform.position+" bounds="+rng.optionADoor.doorPanel.GetComponent<Collider>().bounds);
            yield return ArtView("rng-exit-collision.png",new Vector3(-2.5f,4f,2),new Vector3(-2.5f,1,5.5f));
        }
        Check(crossed,"Full-size player can cross the fallen RNG door and reach the exit threshold");
        movement.enabled=true;yield return WaitForLevel(11);
        Check(GameManager.Instance.CurrentLevelIndex==11,"Walking through successful RNG exit advances to the shooter");
    }
    IEnumerator ExerciseDoorPassage(LevelManager level)
    {
        var player=GameManager.Instance.CurrentPlayer;var move=player.GetComponent<PlayerController>();var cc=player.GetComponent<CharacterController>();
        var position=player.transform.position;move.enabled=false;
        var door=level is Level11_BadRNG rng?rng.optionADoor:level is Level4_KeypadPuzzle support?support.doorController:((Level8_DLCDoor)level).doorController;
        var barrier=door.GetComponent<BoxCollider>();var bounds=barrier.bounds;
        var start=new Vector3(bounds.center.x,1.52f,bounds.center.z-2);
        cc.enabled=false;player.transform.position=start;cc.enabled=true;Physics.SyncTransforms();
        for(int i=0;i<45;i++)cc.Move(Vector3.forward*.08f);
        Check(player.transform.position.z<bounds.center.z,"Closed door still blocks passage: "+level.name);
        bool panelActive=door.doorPanel.activeSelf;door.ReleasePassage();door.doorPanel.SetActive(false);
        cc.enabled=false;player.transform.position=start;cc.enabled=true;Physics.SyncTransforms();
        for(int i=0;i<45;i++)cc.Move(Vector3.forward*.08f);
        Check(player.transform.position.z>bounds.center.z+.6f,"Full-size CharacterController passes the open doorway: "+level.name);
        barrier.enabled=true;door.doorPanel.SetActive(panelActive);
        cc.enabled=false;player.transform.position=position;cc.enabled=true;move.enabled=true;
        yield return null;
    }
}
#endif
