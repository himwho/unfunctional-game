using UnityEngine;

// Editor-only composition recipes. Saved scene objects are the source of truth at runtime.
public static class DepartmentSceneComposition
{
    public static DepartmentProps Create(LevelManager level)
    {
        var p=DepartmentProps.Create(null,"Level"+(level is Level8_DLCDoor?"8":level is Level11_BadRNG?"11":"12")+"_Art");
        if(level is Level8_DLCDoor){WallFinish(p,4,4);Showroom(p);}
        else if(level is Level11_BadRNG){WallFinish(p,6,6);Probability(p);}
        else Finale(p);
        return p;
    }
    static void WallFinish(DepartmentProps p,float halfWidth,float halfDepth)
    {
        var library=GameArtLibrary.Load();var worn=p.Mat(new Color(.18f,.29f,.27f));
        if(library!=null&&library.concrete!=null){worn.CopyPropertiesFromMaterial(library.concrete);worn.color=new Color(.36f,.49f,.46f);worn.mainTextureScale=new Vector2(3,1);}
        // Low wall panels leave all doors, keypads, windows and clue notes exposed.
        foreach(int side in new[]{1}) {
            p.Box("WornTealWainscot",new Vector3(side*(halfWidth-.035f),.62f,0),new Vector3(.04f,1.18f,halfDepth*2-.2f),worn);
            p.Box("DadoRail",new Vector3(side*(halfWidth-.07f),1.24f,0),new Vector3(.04f,.055f,halfDepth*2-.2f),p.Brass);
        }
    }
    static void Lamp(DepartmentProps p,Vector3 at,Color color,float range=7)
    {
        var g=p.Group("LocalExhibitLight",at);var l=g.gameObject.AddComponent<Light>();
        l.type=LightType.Point;l.range=range;l.intensity=2.2f;l.color=color;l.shadows=LightShadows.None;
    }
    static void Showroom(DepartmentProps p)
    {
        p.Sign("PremiumHeader","UNFUNCTIONAL  /  PREMIUM OUTLET",new Vector3(0,4.6f,3.79f),new Vector2(6.8f,.6f),p.Ink,p.Brass,.048f);
        p.Sign("ExitPrice","EXIT EXPERIENCE\n49,999 CREDITS",new Vector3(2.2f,2.4f,3.76f),new Vector2(1.8f,.95f),p.Red,p.Paper,.043f);
        p.Box("WornRedRunner",new Vector3(0,.016f,1.7f),new Vector3(1.55f,.015f,4.1f),p.Red);
        string[] names={"INVISIBLE HAT","PREMIUM AIR","NOTHING / LIMITED EDITION","VIP QUEUE SKIP"};
        for(int i=0;i<4;i++) {
            float x=i%2==0?-2.75f:2.75f,z=i<2?1.35f:-1.55f;
            p.Box("GildedEmptyPlinth",new Vector3(x,.5f,z),new Vector3(.95f,1,.95f),p.Brass);
            p.Box("BlackVelvetTop",new Vector3(x,1.02f,z),new Vector3(.88f,.05f,.88f),p.Ink);
            p.Sign("ProductLabel",names[i],new Vector3(x,.64f,z-.49f),new Vector2(.84f,.28f),p.Ink,p.Paper,.019f);
            p.Rope(new Vector3(x-.58f,0,z-.8f),new Vector3(x+.58f,0,z-.8f));
        }
        Poster(p,4,new Vector3(-3.78f,2.45f,.3f),-90,"100% LESS VISIBLE\n100% FULL PRICE");
        Poster(p,19,new Vector3(3.78f,2.45f,.3f),90,"BUY YOUR WAY OUT!\nDOOR SOLD SEPARATELY");
        p.Sign("SaleSign","SALE!\nWAS: EXPENSIVE\nNOW: ALSO EXPENSIVE",new Vector3(-2.6f,3.55f,3.78f),new Vector2(1.9f,1.1f),p.Red,p.Paper,.032f);
        Lamp(p,new Vector3(-2,3,1.3f),new Color(1,.77f,.45f));Lamp(p,new Vector3(2,3,-1),new Color(.7f,.85f,1));
    }
    static void Poster(DepartmentProps p,int item,Vector3 pos,float yaw,string slogan)
    {
        var r=p.Sign("UglyAdvertisement",slogan,pos,new Vector2(1.55f,2.05f),p.Brass,p.Ink,.029f,yaw);
        r.GetComponentInChildren<TextMesh>().transform.localPosition=new Vector3(0,-.73f,-.045f);
        r.GetComponentInChildren<DepartmentLabelFit>().height=.43f;
        var tex=Resources.Load<Texture2D>("GameArt/ShopProducts");if(tex==null)return;
        var mat=p.Mat(Color.white);mat.mainTexture=tex;mat.mainTextureScale=new Vector2(.25f,.2f);
        mat.mainTextureOffset=new Vector2((item%4)*.25f,(4-item/4)*.2f);
        p.Box("ProductPrint",new Vector3(0,.24f,-.04f),new Vector3(1.4f,1.4f,.015f),mat,r);
    }
    static void Probability(DepartmentProps p)
    {
        p.Sign("RNGHeader","DEPARTMENT OF PROBABILITY",new Vector3(0,5.2f,5.9f),new Vector2(9,.75f),p.Teal,p.Paper,.07f);
        p.Sign("PolicyNotice","75% SUCCESS RATE\n25% COMPLETE ADMINISTRATIVE RESET",new Vector3(-2.5f,4.35f,5.91f),new Vector2(3.8f,.95f),p.Paper,p.Ink,.036f);
        p.Sign("Disclaimer","YOUR PREVIOUS EFFORT\nDOES NOT AFFECT THE ODDS",new Vector3(0,3.7f,5.88f),new Vector2(1.45f,1.15f),p.Red,p.Paper,.027f);
        p.Cabinet(new Vector3(-5,0,4.8f));p.Cabinet(new Vector3(-4.05f,0,4.8f));
        p.Cabinet(new Vector3(4.3f,0,4.8f));p.Cabinet(new Vector3(5.25f,0,4.8f));
        var d=p.Group("ClaimsDesk",new Vector3(0,0,4.8f));
        p.Box("DeskTop",new Vector3(0,1.02f,0),new Vector3(1.65f,.12f,.75f),p.Wood,d);
        foreach(int s in new[]{-1,1})p.Box("DeskLeg",new Vector3(s*.67f,.5f,0),new Vector3(.08f,1,.6f),p.Ink,d);
        p.PaperStack(d,new Vector3(-.45f,1.1f,0),20);p.Phone(d,new Vector3(.42f,1.13f,0));
        // Folded tarp on a display cabinet beside option B; the existing button remains exposed.
        var tarp=p.Mat(new Color(.22f,.25f,.28f));
        for(int i=0;i<12;i++)p.Box("FoldedComingSoonDrape",new Vector3(4.7f+(i-5.5f)*.12f,1.25f,2.7f+Mathf.Sin(i*2)*.035f),new Vector3(.14f,2.5f,.18f),tarp);
        p.Sign("DLCNotice","DIFFICULTY DEPARTMENT\nCOMING SOON",new Vector3(4.7f,1.6f,2.55f),new Vector2(1.8f,.7f),p.Paper,p.Ink,.029f);
        p.Sign("ResetBin","REJECTED CAMPAIGNS",new Vector3(-4.7f,1.45f,2.7f),new Vector2(1.8f,.45f),p.Red,p.Paper,.03f);
        p.Crate(new Vector3(-4.7f,.5f,3),"REJECTED",p.Red);
        Lamp(p,new Vector3(0,3.4f,3),new Color(.75f,.88f,.82f),11);
    }
    static void Finale(DepartmentProps p)
    {
        string[] names={"WALKING","JUMPING","WAITING","READING","BUTTON PRESSING"};
        for(int i=0;i<5;i++) {
            float x=new[]{-5.4f,-3.5f,-1.6f,3.2f,5.2f}[i];
            p.Box("TrophyPlinth",new Vector3(x,.58f,5.8f),new Vector3(.9f,1.16f,.85f),p.Teal);
            p.Trophy(new Vector3(x,1.16f,5.8f),i);
            p.Sign("TrophyCaption",names[i],new Vector3(x,.83f,5.36f),new Vector2(.78f,.23f),p.Brass,p.Ink,.02f);
            if(i!=2)p.Certificate(new Vector3(x,2.1f,8.72f),names[i]+" AWARD");
        }
        Lamp(p,new Vector3(-3.4f,3.2f,5.3f),new Color(1,.87f,.64f));
        Lamp(p,new Vector3(4.3f,3.2f,5.3f),new Color(1,.87f,.64f));
        // Richer framing for the existing readable plaque, without a competing collider.
        p.Sign("PlaqueFootnote","APPROVED BY THE COMMITTEE\nFOR THE RECOGNITION OF COMMITTEES",new Vector3(-4,1.03f,.76f),new Vector2(1.6f,.2f),p.Paper,p.Ink,.014f);
        p.Certificate(new Vector3(-6.72f,2.6f,-2),"CERTIFICATE OF EXISTENCE",-90);
        p.Certificate(new Vector3(6.72f,2.6f,-2),"OUTSTANDING ADEQUACY",90);
    }
}
