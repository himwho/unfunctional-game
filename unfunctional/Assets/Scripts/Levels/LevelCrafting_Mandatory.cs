using UnityEngine;
using UnityEngine.UI;

/// <summary>Mandatory crafting: nine transactions to recreate the handle already on display.</summary>
public sealed class LevelCrafting_Mandatory : LevelManager
{
    public const int RecipeLength=9;
    static readonly string[] Stations={"depot","grinder","grinder","press","foundry","stamp","packing","packing","exit"};
    static readonly string[] Items={"NOTHING","ALLOY BILLET","METAL FILINGS","ARTISANAL METAL POWDER","ALLOY BILLET (AGAIN)","UNAPPROVED HANDLE","NOTARIZED HANDLE","PACKAGED HANDLE","UNPACKAGED NOTARIZED HANDLE","INSTALLED HANDLE"};
    static readonly string[] Orders={"Collect one billet at MATERIALS.","Grind the billet into filings.","Grind the filings into finer filings.","Press the powder back into a billet.","Cast a door handle at the FOUNDRY.","Have the handle NOTARIZED.","Package the handle at DISPATCH.","Unpackage the handle at DISPATCH.","Install your handle at the EXIT."};
    DepartmentProps art;
    Text ledger,prompt,remark;
    Transform carried,installed,display;
    AudioSource machineAudio;
    int step;
    public int CraftingStep=>step;
    public string InventoryItem=>Items[Mathf.Clamp(step,0,RecipeLength)];
    public bool HandleInstalled=>step==RecipeLength;
    protected override void Start()
    {
        levelDisplayName="Some Assembly Required";levelDescription="The handle was here all along.";
        needsPlayer=true;wantsCursorLocked=true;base.Start();BuildRoom();BuildHUD();Refresh();
    }
    void Update()
    {
        if(!CanProcessGameplay||Camera.main==null)return;
        prompt.text="[E] Use workstation   /   Follow the production order";
        if(!Physics.Raycast(Camera.main.transform.position,Camera.main.transform.forward,out var hit,3.2f,~0,QueryTriggerInteraction.Ignore))return;
        var target=hit.collider.GetComponent<CraftingStation>();if(target==null)return;
        prompt.text=target.stationId=="display"?"[E] Examine the finished handle":target.stationId=="exit"?"[E] Install handle and leave":"[E] Use "+target.displayName;
        if(Input.GetKeyDown(KeyCode.E))TryProcessStation(target.stationId);
    }
    /// <summary>One ordered item, no consumable shortages; invalid stations cannot consume progress.</summary>
    public bool TryProcessStation(string station)
    {
        if(!CanProcessGameplay||step>=RecipeLength)return false;
        if(station=="display") {remark.text="DISPLAY ONLY. A fully functional handle. Touching it would invalidate the demonstration.";return false;}
        if(station!=Stations[step]) {remark.text=station=="exit"?"The door requires a handle with a complete administrative history.":"WRONG DEPARTMENT. Please follow the production order.";return false;}
        step++;RefreshItem();Refresh();
        var library=GameArtLibrary.Load();
        if(machineAudio!=null&&library!=null&&library.achievementVariants!=null&&library.achievementVariants.Length>0)
            machineAudio.PlayOneShot(library.achievementVariants[(step-1)%library.achievementVariants.Length],.6f);
        remark.text=step==4?"A billet. Exactly what you started with. Excellent craftsmanship.":step==8?"Packaging successfully removed. The handle remains a handle.":"Production record updated. No shortcuts have occurred.";
        if(HandleInstalled){installed.gameObject.SetActive(true);CompleteLevel();}
        return true;
    }
    void Refresh()
    {
        if(ledger==null)return;
        ledger.text="MANDATORY CRAFTING\nPRODUCTION ORDER 00-DOOR\n\n"+Mathf.Min(step+1,RecipeLength)+" / "+RecipeLength+"\n\nIN HAND:\n"+InventoryItem+"\n\nNEXT:\n"+(step<RecipeLength?Orders[step]:"Production complete.");
    }
    void BuildHUD()
    {
        var c=GameScreenUI.Canvas(transform,"CraftingHUD",20);
        var bg=GameScreenUI.Panel(c.transform,"OrderSheet",new Color(.06f,.10f,.10f,.91f),new Vector2(.02f,.49f),new Vector2(.28f,.95f));bg.raycastTarget=false;
        ledger=GameScreenUI.Text(c.transform,"RecipeLedger","",21,new Vector2(.035f,.51f),new Vector2(.265f,.93f),TextAnchor.UpperLeft);
        prompt=GameScreenUI.Text(c.transform,"Prompt","",24,new Vector2(.15f,.08f),new Vector2(.85f,.15f));
        remark=GameScreenUI.Text(c.transform,"Remark","The finished handle is for display purposes only. Please manufacture your own.",23,new Vector2(.1f,.015f),new Vector2(.9f,.075f));
        GameScreenUI.Text(c.transform,"Crosshair","+",24,new Vector2(.48f,.48f),new Vector2(.52f,.52f));
    }
    void BuildRoom()
    {
        art=DepartmentProps.Create(transform,"CraftingDepartmentArt");var library=GameArtLibrary.Load();
        Material tile=library!=null?library.tile:art.Paper,concrete=library!=null?library.concrete:art.Teal;
        art.Box("Floor",new Vector3(0,-.2f,0),new Vector3(14,.4f,18),tile,null,true);
        art.Box("Ceiling",new Vector3(0,4.9f,0),new Vector3(14,.3f,18),concrete,null,true);
        foreach(int side in new[]{-1,1}) {
            art.Box("SideWall",new Vector3(side*7,2.4f,0),new Vector3(.3f,4.8f,18),concrete,null,true);
            art.Box("EndWall",new Vector3(0,2.4f,side*9),new Vector3(14,4.8f,.3f),concrete,null,true);
            art.Box("TileDado",new Vector3(side*6.81f,.7f,0),new Vector3(.05f,1.4f,18),tile);
        }
        art.Sign("DepartmentHeader","DEPARTMENT OF MANDATORY MANUFACTURE",new Vector3(0,3.9f,8.7f),new Vector2(12,.8f),art.Teal,art.Paper,.065f);
        art.Sign("SafetyNotice","DO NOT USE THE EXISTING HANDLE\nALL HANDLES MUST BE CRAFTED ON SITE",new Vector3(0,2.9f,-8.7f),new Vector2(6,1),art.Paper,art.Red,.05f,180);
        Station("depot","MATERIALS",new Vector3(-4,0,-4),"01 / TAKE A BILLET");
        Station("grinder","GRINDER",new Vector3(-4,0,-.7f),"02 + 03 / MAKE IT SMALLER");
        Station("press","COMPACTOR",new Vector3(4,0,-.7f),"04 / MAKE IT BIG AGAIN");
        Station("foundry","FOUNDRY",new Vector3(-4,0,2.7f),"05 / CAST THE OBVIOUS");
        Station("stamp","NOTARY",new Vector3(4,0,2.7f),"06 / VALIDATE THE HANDLE");
        Station("packing","DISPATCH",new Vector3(4,0,-4),"07 + 08 / PACK / UNPACK");
        display=art.Group("DisplayOnlyHandle",new Vector3(2.4f,1.25f,7.3f));
        art.Box("ExhibitionPedestal",new Vector3(2.4f,.55f,7.3f),new Vector3(1.2f,1.1f,1.2f),art.Teal,null,true);
        var displayTarget=art.Box("DisplayTarget",new Vector3(0,.14f,0),new Vector3(.85f,.6f,.3f),art.Ink,display,true);
        Target(displayTarget,"display","DISPLAY ONLY");art.Handle(display,new Vector3(-.17f,.2f,-.18f),art.Brass);
        art.Sign("DisplayOnlyNotice","FINISHED DOOR HANDLE\nDISPLAY ONLY",new Vector3(2.4f,1.96f,7.3f),new Vector2(2,.55f),art.Paper,art.Red,.034f);
        art.Sign("DisplayInsult","FUNCTIONAL SINCE ARRIVAL",new Vector3(2.4f,.65f,6.68f),new Vector2(1.1f,.22f),art.Brass,art.Ink,.018f);
        var door=art.Box("HandlelessExit",new Vector3(0,1.55f,8.5f),new Vector3(1.8f,3.1f,.2f),art.Paper,null,true);Target(door,"exit","EXIT");
        foreach(int side in new[]{-1,1})art.Box("ExitJamb",new Vector3(side*.99f,1.6f,8.43f),new Vector3(.15f,3.2f,.3f),art.Ink);
        for(int i=0;i<3;i++)art.Box("DoorRecess",new Vector3(0,.55f+i*.85f,8.385f),new Vector3(1.35f,.58f,.025f),art.Teal);
        installed=art.Group("InstalledHandle",new Vector3(.55f,1.35f,8.33f));art.Handle(installed,Vector3.zero,art.Brass);installed.gameObject.SetActive(false);
        art.Sign("ExitLabel","EXIT / HANDLE NOT INCLUDED",new Vector3(0,3.35f,8.4f),new Vector2(2.7f,.35f),art.Teal,art.Paper,.03f);
        carried=art.Group("CurrentCraftingItem",new Vector3(0,0,0));
        var spawn=new GameObject("PlayerSpawnPoint").transform;spawn.SetParent(transform,false);spawn.position=new Vector3(0,1.5f,-6.5f);playerSpawnPoint=spawn;
        for(int i=0;i<3;i++) {
            var l=art.Group("Fluorescent",new Vector3(0,4.3f,-5+i*5));var light=l.gameObject.AddComponent<Light>();light.type=LightType.Point;light.intensity=3.5f;light.range=11;
            art.Box("LightHousing",new Vector3(0,4.65f,-5+i*5),new Vector3(3,.15f,.35f),art.Ink);
            art.Box("Diffuser",new Vector3(0,4.55f,-5+i*5),new Vector3(2.8f,.04f,.25f),art.Paper);
        }
        machineAudio=gameObject.AddComponent<AudioSource>();machineAudio.playOnAwake=false;
    }
    static void Target(GameObject go,string id,string label){var s=go.AddComponent<CraftingStation>();s.stationId=id;s.displayName=label;}
    void Station(string id,string label,Vector3 position,string instruction)
    {
        var r=art.Group("Workstation_"+id,position);
        var volume=r.gameObject.AddComponent<BoxCollider>();volume.center=new Vector3(0,1.1f,0);volume.size=new Vector3(2.1f,2.2f,1.25f);Target(r.gameObject,id,label);
        var desk=art.Box("InteractionCounter",new Vector3(0,.53f,0),new Vector3(2,1.06f,1.15f),art.Teal,r,true);Target(desk,id,label);
        art.Box("Worktop",new Vector3(0,1.09f,0),new Vector3(2.1f,.1f,1.25f),art.Wood,r);
        art.Text(label,new Vector3(0,.75f,-.585f),.06f,art.Paper,r);
        art.Text(instruction,new Vector3(0,.38f,-.585f),.028f,art.Paper,r);
        art.Sign("StationHeader",label,new Vector3(position.x,2.3f,position.z+.35f),new Vector2(2.15f,.43f),art.Paper,art.Ink,.045f);
        if(id=="depot")for(int i=0;i<4;i++)art.Box("AlloyBillet",new Vector3((i%2)*.5f-.25f,1.19f+(i/2)*.14f,0),new Vector3(.4f,.12f,.6f),art.Steel,r);
        if(id=="grinder") {
            art.Box("GrinderHousing",new Vector3(0,1.5f,.18f),new Vector3(1.05f,.7f,.55f),art.Ink,r);
            for(int i=0;i<2;i++){var roller=art.Shape("GrindingWheel",PrimitiveType.Cylinder,new Vector3(i==0?-.28f:.28f,1.6f,-.15f),new Vector3(.45f,.11f,.45f),art.Steel,r);roller.transform.localRotation=Quaternion.Euler(90,0,0);}
            art.Box("PowderTray",new Vector3(0,1.18f,-.32f),new Vector3(.85f,.04f,.4f),art.Brass,r);
        }
        if(id=="press") {
            foreach(int s in new[]{-1,1})art.Rod("PressColumn",new Vector3(s*.5f,1.14f,.1f),new Vector3(s*.5f,2,.1f),.06f,art.Steel,r);
            art.Box("PressHead",new Vector3(0,1.95f,.1f),new Vector3(1.2f,.2f,.5f),art.Red,r);
            art.Rod("Ram",new Vector3(0,1.95f,.1f),new Vector3(0,1.5f,.1f),.09f,art.Steel,r);art.Box("Die",new Vector3(0,1.4f,.1f),new Vector3(.65f,.15f,.5f),art.Ink,r);
        }
        if(id=="foundry") {
            art.Box("ColdFurnace",new Vector3(-.35f,1.5f,.15f),new Vector3(.8f,.7f,.65f),art.Ink,r);
            art.Box("HeatingDoor",new Vector3(-.35f,1.5f,-.19f),new Vector3(.6f,.48f,.03f),art.Red,r);
            art.Box("HandleMold",new Vector3(.48f,1.18f,0),new Vector3(.65f,.08f,.7f),art.Steel,r);art.Handle(r,new Vector3(.3f,1.25f,0),art.Brass);
        }
        if(id=="stamp") {art.PaperStack(r,new Vector3(-.4f,1.16f,0),12);art.Box("StampPad",new Vector3(.35f,1.19f,0),new Vector3(.35f,.1f,.25f),art.Red,r);art.Rod("RubberStamp",new Vector3(.35f,1.25f,0),new Vector3(.35f,1.48f,0),.06f,art.Wood,r);}
        if(id=="packing") {art.Box("PointlessPackage",new Vector3(0,1.37f,0),new Vector3(.85f,.45f,.65f),art.Paper,r);art.Box("PackingTape",new Vector3(0,1.6f,0),new Vector3(.14f,.01f,.65f),art.Brass,r);art.Text("HANDLE WITH HANDLE",new Vector3(0,1.4f,-.33f),.021f,art.Ink,r);}
    }
    void RefreshItem()
    {
        if(carried==null)return;foreach(Transform child in carried)Destroy(child.gameObject);
        if(Camera.main==null||HandleInstalled)return;
        carried.SetParent(Camera.main.transform,false);carried.localPosition=new Vector3(.45f,-.38f,.85f);carried.localRotation=Quaternion.Euler(5,-25,0);
        if(step>=5&&step!=7)art.Handle(carried,Vector3.zero,art.Brass);
        else if(step==7)art.Box("WrappedHandle",Vector3.zero,new Vector3(.4f,.22f,.27f),art.Paper,carried);
        else if(step==2||step==3)for(int i=0;i<8;i++)art.Shape("Filings",PrimitiveType.Sphere,new Vector3((i%3)*.055f,(i/3)*.03f,0),Vector3.one*(step==2?.04f:.022f),art.Brass,carried);
        else art.Box("Billet",Vector3.zero,new Vector3(.28f,.12f,.18f),art.Steel,carried);
    }
    protected override void OnDestroy(){if(carried!=null)Destroy(carried.gameObject);base.OnDestroy();}
}
