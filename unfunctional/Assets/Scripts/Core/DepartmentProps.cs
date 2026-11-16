using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Scene-owned decorative models. Decorations never add raycast targets or consume gameplay RNG.</summary>
public sealed class DepartmentProps : MonoBehaviour
{
    readonly List<Object> owned = new List<Object>();
    public Material Teal, Paper, Ink, Brass, Red, Wood, Steel;
    public static DepartmentProps Create(Transform parent, string name)
    {
        var go = new GameObject(name); go.transform.SetParent(parent, false);
        var p = go.AddComponent<DepartmentProps>(); p.Init(); return p;
    }
    void Init()
    {
        var lib = GameArtLibrary.Load();
        Teal = Mat(new Color(.17f,.31f,.30f)); Paper = Mat(new Color(.82f,.77f,.59f));
        Ink = Mat(new Color(.065f,.085f,.08f)); Brass = Mat(new Color(.63f,.43f,.12f), .65f);
        Red = Mat(new Color(.48f,.075f,.045f));
        Wood = lib != null ? lib.wood : Paper; Steel = lib != null ? lib.steel : Ink;
    }
    public Material Mat(Color c, float metal = 0)
    {
        var m = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        m.color=c; m.SetFloat("_Metallic",metal); m.SetFloat("_Smoothness",.28f); owned.Add(m); return m;
    }
    public Transform Group(string name, Vector3 p, float yaw=0)
    {
        var g=new GameObject(name).transform; g.SetParent(transform,false); g.localPosition=p;
        g.localRotation=Quaternion.Euler(0,yaw,0); return g;
    }
    public GameObject Shape(string name, PrimitiveType type, Vector3 p, Vector3 scale, Material m, Transform parent=null, bool solid=false)
    {
        var g=GameObject.CreatePrimitive(type); g.name=name; g.transform.SetParent(parent!=null?parent:transform,false);
        g.transform.localPosition=p; g.transform.localScale=scale; g.GetComponent<Renderer>().sharedMaterial=m;
        if(!solid) { g.GetComponent<Collider>().enabled=false; if(Application.isPlaying) Destroy(g.GetComponent<Collider>()); else DestroyImmediate(g.GetComponent<Collider>()); g.layer=2; }
        return g;
    }
    public GameObject Box(string n, Vector3 p, Vector3 size, Material m, Transform parent=null, bool solid=false)
        =>Shape(n,PrimitiveType.Cube,p,size,m,parent,solid);
    public GameObject Rod(string n, Vector3 a, Vector3 b, float radius, Material m, Transform parent=null)
    {
        var g=Shape(n,PrimitiveType.Cylinder,(a+b)*.5f,new Vector3(radius*2,(b-a).magnitude*.5f,radius*2),m,parent);
        g.transform.localRotation=Quaternion.FromToRotation(Vector3.up,b-a); return g;
    }
    public TextMesh Text(string text, Vector3 p, float size, Material color=null, Transform parent=null)
    {
        var g=new GameObject("Lettering",typeof(TextMesh)); g.transform.SetParent(parent!=null?parent:transform,false);
        g.transform.localPosition=p; var t=g.GetComponent<TextMesh>(); t.text=text;
        t.font=UIHelper.GetDefaultFont(); t.fontSize=64; t.characterSize=size;
        t.anchor=TextAnchor.MiddleCenter;t.alignment=TextAlignment.Center;t.color=(color!=null?color:Paper).color;
        g.GetComponent<Renderer>().sharedMaterial=t.font.material;return t;
    }
    public Transform Sign(string name,string text,Vector3 p,Vector2 size,Material bg,Material ink,float letters=.045f,float yaw=0)
    {
        var r=Group(name,p,yaw);Box("EnamelBacking",Vector3.zero,new Vector3(size.x,size.y,.06f),bg,r);
        var lettering=Text(text,new Vector3(0,0,-.036f),letters,ink,r);
        var fit=lettering.gameObject.AddComponent<DepartmentLabelFit>();fit.width=size.x*.87f;fit.height=size.y*.82f;
        foreach(float x in new[]{-1f,1f}) foreach(float y in new[]{-1f,1f})
            Shape("Rivet",PrimitiveType.Sphere,new Vector3(x*(size.x*.5f-.06f),y*(size.y*.5f-.06f),-.04f),Vector3.one*.025f,Brass,r);
        return r;
    }
    public void PaperStack(Transform r,Vector3 p,int count=8)
    {
        for(int i=0;i<count;i++) {
            var sheet=Box("UnprocessedForm",p+Vector3.up*(i*.012f),new Vector3(.28f,.009f,.38f),Paper,r);
            sheet.transform.localRotation=Quaternion.Euler(0,(i*17%13)-6,0);
        }
    }
    public void Cabinet(Vector3 p,float yaw=0)
    {
        var r=Group("FilingCabinet",p,yaw);Box("Case",new Vector3(0,.85f,0),new Vector3(.8f,1.7f,.65f),Teal,r);
        for(int i=0;i<4;i++) {
            float y=.23f+i*.4f;Box("Drawer",new Vector3(0,y,-.345f),new Vector3(.72f,.35f,.06f),Paper,r);
            Box("Pull",new Vector3(0,y-.06f,-.40f),new Vector3(.22f,.035f,.06f),Ink,r);
            Box("IndexSlot",new Vector3(0,y+.07f,-.38f),new Vector3(.2f,.065f,.01f),Brass,r);
        }
    }
    public void Phone(Transform r,Vector3 p)
    {
        Box("TelephoneBase",p,new Vector3(.36f,.11f,.29f),Ink,r);
        for(int i=0;i<9;i++) Box("PhoneKey",p+new Vector3((i%3-1)*.055f,.062f,(i/3-1)*.055f),new Vector3(.04f,.015f,.035f),Paper,r);
        Rod("Handset",p+new Vector3(-.2f,.17f,.09f),p+new Vector3(.2f,.17f,.09f),.045f,Ink,r);
        for(int i=0;i<2;i++) Box("Receiver",p+new Vector3(i==0?-.18f:.18f,.15f,.09f),new Vector3(.12f,.09f,.13f),Ink,r);
        Vector3 last=p+new Vector3(.2f,.1f,.1f);
        for(int i=1;i<=32;i++) {float t=i/32f;Vector3 next=p+new Vector3(.2f+t*.45f,.07f+Mathf.Sin(t*40)*.026f,.1f+Mathf.Cos(t*40)*.03f);Rod("CoiledCable",last,next,.009f,Ink,r);last=next;}
    }
    public Transform Crate(Vector3 p,string label,Material stripe,Transform parent=null)
    {
        var r=Group("SlattedReplacementCrate_"+label,p);if(parent!=null)r.SetParent(parent,false);
        Box("CrateFloor",new Vector3(0,-.44f,0),new Vector3(1.55f,.12f,1.18f),Wood,r);
        for(int row=0;row<4;row++) foreach(int side in new[]{-1,1}) {
            Box("HorizontalSlat",new Vector3(0,-.32f+row*.21f,side*.56f),new Vector3(1.56f,.16f,.07f),Wood,r);
            Box("SideSlat",new Vector3(side*.76f,-.32f+row*.21f,0),new Vector3(.07f,.16f,1.12f),Wood,r);
        }
        foreach(int side in new[]{-1,1}) foreach(int z in new[]{-1,1}) {
            Box("SteelCorner",new Vector3(side*.70f,0,z*.61f),new Vector3(.085f,.94f,.04f),Steel,r);
            for(int row=0;row<3;row++) Shape("Nail",PrimitiveType.Sphere,new Vector3(side*.7f,-.3f+row*.3f,z*.64f),Vector3.one*.035f,Ink,r);
        }
        Box("StencilPlate",new Vector3(0,.02f,-.62f),new Vector3(.99f,.43f,.035f),stripe,r);
        var caption=Text(label+"\nGRADE: DISPOSABLE",new Vector3(0,.02f,-.646f),.026f,Paper,r);
        var fit=caption.gameObject.AddComponent<DepartmentLabelFit>();fit.width=.9f;fit.height=.35f;
        return r;
    }
    public void Rope(Vector3 a,Vector3 b)
    {
        foreach(var p in new[]{a,b}) {Shape("StanchionBase",PrimitiveType.Cylinder,p+Vector3.up*.045f,new Vector3(.35f,.045f,.35f),Brass);Rod("Stanchion",p,p+Vector3.up*.85f,.035f,Brass);Shape("Finial",PrimitiveType.Sphere,p+Vector3.up*.9f,Vector3.one*.13f,Brass);}
        Vector3 last=a+Vector3.up*.85f;
        for(int i=1;i<=12;i++){float t=i/12f;var next=Vector3.Lerp(a,b,t)+Vector3.up*(.85f-Mathf.Sin(t*Mathf.PI)*.2f);Rod("VelvetRope",last,next,.027f,Red);last=next;}
    }
    public void Handle(Transform r,Vector3 p,Material m)
    {
        var plate=Shape("HandleRosette",PrimitiveType.Cylinder,p,new Vector3(.18f,.025f,.18f),m,r);plate.transform.localRotation=Quaternion.Euler(90,0,0);
        Rod("HandleSpindle",p,p+Vector3.back*.15f,.035f,m,r);Rod("HandleLever",p+Vector3.back*.15f,p+new Vector3(.32f,0,-.15f),.04f,m,r);
    }
    public void Trophy(Vector3 p,int kind)
    {
        var r=Group("Trophy_"+kind,p);Box("MarbleBase",new Vector3(0,.09f,0),new Vector3(.65f,.18f,.5f),Ink,r);
        Box("Nameplate",new Vector3(0,.10f,-.26f),new Vector3(.48f,.09f,.02f),Brass,r);
        Rod("Stem",new Vector3(0,.18f,0),new Vector3(0,.45f,0),.055f,Brass,r);
        if(kind==0) {Box("GoldenShoe",new Vector3(.05f,.51f,0),new Vector3(.46f,.14f,.22f),Brass,r);Box("Ankle",new Vector3(-.1f,.64f,0),new Vector3(.17f,.22f,.2f),Brass,r);}
        else if(kind==1) {Rod("ArrowShaft",new Vector3(0,.4f,0),new Vector3(0,.9f,0),.045f,Brass,r);Rod("ArrowWing",new Vector3(0,.9f,0),new Vector3(-.2f,.7f,0),.045f,Brass,r);Rod("ArrowWing",new Vector3(0,.9f,0),new Vector3(.2f,.7f,0),.045f,Brass,r);}
        else if(kind==2) {for(int i=0;i<2;i++)Box("PauseBar",new Vector3(i==0?-.1f:.1f,.68f,0),new Vector3(.12f,.46f,.15f),Brass,r);}
        else if(kind==3) {for(int i=0;i<2;i++){var page=Box("OpenBook",new Vector3(i==0?-.12f:.12f,.66f,0),new Vector3(.23f,.4f,.05f),Brass,r);page.transform.localRotation=Quaternion.Euler(0,i==0?-18:18,0);}}
        else {Shape("ButtonAward",PrimitiveType.Cylinder,new Vector3(0,.57f,0),new Vector3(.42f,.1f,.42f),Brass,r);}
    }
    public void Certificate(Vector3 p,string title,float yaw=0)
    {
        var r=Sign("FramedCertificate",title+"\n\nCERTIFIED ORDINARY\n\nDepartment of Achievements",p,new Vector2(1.35f,1.65f),Paper,Ink,.025f,yaw);
        foreach(int side in new[]{-1,1}) {Box("GoldBorder",new Vector3(side*.65f,0,-.04f),new Vector3(.045f,1.65f,.04f),Brass,r);Box("GoldBorder",new Vector3(0,side*.8f,-.04f),new Vector3(1.35f,.045f,.04f),Brass,r);}
        var seal=Shape("WaxSeal",PrimitiveType.Cylinder,new Vector3(.36f,-.55f,-.05f),new Vector3(.22f,.012f,.22f),Red,r);seal.transform.localRotation=Quaternion.Euler(90,0,0);
        for(int i=0;i<2;i++){var ribbon=Box("SealRibbon",new Vector3(.32f+i*.08f,-.69f,-.04f),new Vector3(.055f,.22f,.01f),Brass,r);ribbon.transform.localRotation=Quaternion.Euler(0,0,i==0?-12:12);}
    }
    // Editor authoring transfers generated materials into assets before removing this builder.
    public List<Object> ReleaseGeneratedAssets()
    {
        var assets = new List<Object>(owned); owned.Clear(); return assets;
    }
    void OnDestroy(){foreach(var o in owned)if(o!=null){if(Application.isPlaying)Destroy(o);else DestroyImmediate(o);}}
}
