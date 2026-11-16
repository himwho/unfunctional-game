#if UNITY_EDITOR
using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public partial class CampaignSmokeDriver
{
    IEnumerator ExerciseDepartmentArt(LevelManager level)
    {
        var gm=GameManager.Instance;
        if(level is Level1_ConfusingMenu menu) {
            Check(menu.realStartButton.GetComponent<Image>().sprite!=null,"Confusing menu uses a custom button bezel without changing its target");
            yield return Capture("menu-custom-buttons.png");
        }
        if(level is Level2_SettingsPuzzle settings) {
            var slider=Get<Slider>(settings,"step1Slider");
            Check(slider.handleRect.GetComponent<Image>().sprite!=null&&slider.transform.Find("Track/CalibrationTicks")!=null,"Calibration has custom hardware skin and non-interactive etched ticks");
            Check(slider.minValue==0&&slider.maxValue==1,"Calibration range unchanged by art");
            yield return Capture("calibration-custom-controls.png");
        }
        var dressing=level.transform.Find("DepartmentArt");
        if(dressing!=null)Check(!dressing.GetComponentsInChildren<Collider>().Any(c=>c.enabled),"Room dressing cannot intercept interaction rays: "+level.gameObject.scene.name);
        if(level is Level4_KeypadPuzzle)yield return ArtView("rodney-support.png",new Vector3(-2.4f,1.9f,-2.8f),new Vector3(.8f,1.7f,2.5f));
        if(level is Level8_DLCDoor store) {
            var product=level.GetComponent<ShopProductArt>();Check(product!=null,"Store product art initialized");
            Check(store.requiredBrowseCount==8&&store.freeCreditsAmount==99999&&store.doorDLCPrice==49999,"DLC browsing and credit rules unchanged");
            var images=Resources.FindObjectsOfTypeAll<Image>().Where(i=>i.gameObject.scene==store.gameObject.scene&&i.name=="ProductImage").ToArray();
            Check(images.Length==20&&images.All(i=>i.sprite!=null&&!i.raycastTarget),"All twenty products have separate non-interactive atlas illustrations");
            yield return ArtView("dlc-showroom.png",new Vector3(-1.6f,1.9f,-3.1f),new Vector3(.4f,1.6f,2.5f));
            Invoke(store,"OpenShop");yield return Capture("dlc-catalogue.png");
            Invoke(store,"OnViewItem",13);yield return Capture("dlc-product-detail.png");
            store.TryCloseModal();store.TryCloseModal();
        }
        if(level is Level11_BadRNG)yield return ArtView("rng-department.png",new Vector3(-.2f,2.1f,-4.7f),new Vector3(0,2,4.2f));
        if(level is Level10_ItemDegradation workshop) {
            var lib=GameArtLibrary.Load();Check(lib.shovelLowerShaft!=null&&lib.shovelUpperShaft!=null&&lib.soilClod!=null&&lib.shovelSnap!=null,"Workshop fracture geometry, soil mesh and snap audio are packaged");
            var racks=GameObject.Find("WorkshopArt");
            Check(racks!=null&&!racks.GetComponentsInChildren<Collider>().Any(c=>c.enabled),"Workshop dressing leaves the original supply targets unchanged");Check(racks!=null&&racks.transform.Cast<Transform>().Count(t=>t.name.StartsWith("SlattedReplacementCrate"))==5,"All five replacement racks use slatted crate models");
            foreach(string name in new[]{"Shovel","Hammer","Saw","Wrench","Broom"}) {
                var target=workshop.transform.Find("ToolRack_"+name);
                var model=racks.transform.Find("SlattedReplacementCrate_"+name.ToUpperInvariant());
                Check(Vector3.Distance(target.position,model.position)<.01f,"Visual replacement crate matches its authored pickup target: "+name);
            }
            Invoke(workshop,"DispenseReplacementTool","Shovel");Invoke(workshop,"BreakTool");
            yield return new WaitForSeconds(.12f);
            yield return Capture("shovel-fracture.png");
            yield return new WaitForSeconds(.6f);
            var blade=GameObject.Find("BrokenShovel_Blade");var grip=GameObject.Find("BrokenShovel_Grip");
            Check(blade!=null&&grip!=null&&blade.GetComponent<Rigidbody>()!=null&&grip.GetComponent<Rigidbody>()!=null,"Shovel snaps into two independently simulated pieces");
            Check(blade.transform.Find("StampedSteelBlade")!=null&&grip.transform.Find("DGrip")!=null,"Broken shovel keeps blade and grip on the correct halves");
            Invoke(workshop,"DispenseReplacementTool","Shovel");yield return new WaitForSeconds(.6f);
            Check(Get<GameObject>(workshop,"currentToolVisual")!=null,"Shovel fracture cleanup cannot detach a replacement");Invoke(workshop,"DropCurrentItem");
            yield return ArtView("workshop-supplies.png",new Vector3(-6,2.2f,5),new Vector3(-.1f,.9f,10));
            yield return ArtView("workshop-soil.png",new Vector3(-8.5f,2.1f,-3.5f),new Vector3(-7,.2f,-6));
        }
        if(level is LevelCrafting_Mandatory crafting) {
            Check(crafting.CraftingStep==0&&!crafting.HandleInstalled,"Crafting starts without an inventory shortcut");
            Check(!crafting.TryProcessStation("display")&&!crafting.TryProcessStation("exit")&&crafting.CraftingStep==0,"Display handle and premature exit cannot skip crafting");
            gm.TogglePause();Check(!crafting.TryProcessStation("depot")&&crafting.CraftingStep==0,"Paused crafting rejects station transactions");gm.TogglePause();
            Check(crafting.TryProcessStation("depot"),"Crafting acquires the initial billet");
            Check(!crafting.TryProcessStation("depot")&&crafting.CraftingStep==1,"Repeated resource pickup cannot duplicate or erase the carried item");
            Check(!crafting.TryProcessStation("foundry")&&crafting.InventoryItem=="ALLOY BILLET","Wrong workstation preserves inventory");
            gm.ReloadCurrentLevel();yield return WaitForLevel(System.Array.IndexOf(gm.levelSceneNames,"LEVEL_CRAFTING"));
            crafting=FindAnyObjectByType<LevelCrafting_Mandatory>();Check(crafting.CraftingStep==0,"Restart rebuilds the crafting recipe cleanly");
            foreach(var station in FindObjectsByType<CraftingStation>(FindObjectsSortMode.None).Where(s=>s.transform.parent!=null&&s.transform.parent.name=="CraftingDepartmentArt")) {
                var target=station.GetComponent<Collider>().bounds.center;var origin=target+Vector3.back*2.5f;
                Physics.SyncTransforms();Check(Physics.Raycast(origin,Vector3.forward,out var hit,3.2f)&&hit.collider.GetComponent<CraftingStation>()!=null,"Crafting station is reachable by interaction ray: "+station.stationId);
            }
            yield return ArtView("mandatory-crafting.png",new Vector3(0,2.1f,-6.7f),new Vector3(0,1.6f,4));
            yield return ArtView("display-only-handle.png",new Vector3(1,1.8f,4.7f),new Vector3(2.4f,1.5f,7.3f));
            foreach(string station in new[]{"depot","grinder","grinder","press","foundry","stamp","packing","packing"})Check(crafting.TryProcessStation(station),"Crafting recipe processes: "+station);
            Check(crafting.CraftingStep==8&&!crafting.IsLevelComplete,"All intermediates are required before handle installation");
            Check(crafting.TryProcessStation("exit"),"Crafted handle installs at the exit");
            yield return WaitForLevel(System.Array.IndexOf(gm.levelSceneNames,"LEVEL12"));
            Check(gm.CurrentState==GameManager.GameState.Playing,"Crafting flows into the achievement finale");
        }
        if(level is Level12_Achievements) {
            var lib=GameArtLibrary.Load();Check(lib.achievementVariants.Length==5&&lib.achievementVariants.All(a=>a!=null),"Five award sound variations packaged");
            Check(Resources.Load<Sprite>("GameArt/Award5")!=null,"Custom achievement icon set packaged");
            yield return ArtView("finale-custom-awards.png",new Vector3(-2,2.1f,2.8f),new Vector3(0,1.7f,6.3f));
        }
    }
    IEnumerator ArtView(string filename,Vector3 position,Vector3 target)
    {
        var camera=Camera.main;if(camera==null)yield break;
        var player=FindAnyObjectByType<PlayerController>();bool enabled=player!=null&&player.enabled;if(player!=null)player.enabled=false;
        var hidden=player!=null?player.GetComponentsInChildren<Renderer>().Where(r=>r.enabled).ToArray():new Renderer[0];
        foreach(var r in hidden)r.enabled=false;
        var oldPosition=camera.transform.position;var oldRotation=camera.transform.rotation;
        camera.transform.position=position;camera.transform.LookAt(target);yield return null;
        yield return Capture(filename);
        if(camera!=null){camera.transform.position=oldPosition;camera.transform.rotation=oldRotation;}
        foreach(var r in hidden)if(r!=null)r.enabled=true;
        if(player!=null)player.enabled=enabled;
    }
}

#endif
