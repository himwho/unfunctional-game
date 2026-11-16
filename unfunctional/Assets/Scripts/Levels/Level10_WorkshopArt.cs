using System.Collections;
using UnityEngine;

public partial class Level10_ItemDegradation
{
    private DepartmentProps workshopArt;
    private void DressWorkshop()
    {
        workshopArt=DepartmentProps.Create(transform,"WorkshopArt");
        // The authored manager has a 175-degree rotation; these dressing coordinates are world space.
        workshopArt.transform.SetPositionAndRotation(Vector3.zero,Quaternion.identity);
        string[] names={"Shovel","Hammer","Saw","Wrench","Broom"};
        foreach(var name in names) {
            var rack=transform.Find("ToolRack_"+name);if(rack==null)continue;
            rack.GetComponent<Renderer>().enabled=false; // Keep the exact original supply hitbox.
            var crate=workshopArt.Crate(rack.position,name.ToUpperInvariant(),SupplyMaterial(toolColors[name]));
            crate.rotation=rack.rotation*Quaternion.Euler(0,180,0);
        }
        if(digSoil==null)return;
        var lib=GameArtLibrary.Load();var dirt=workshopArt.Mat(new Color(.22f,.13f,.065f));
        var stones=workshopArt.Mat(new Color(.34f,.30f,.24f));
        var root=workshopArt.Group("UnevenSoilSurface",Vector3.zero);root.SetParent(digSoil,false);
        for(int i=0;i<58;i++) {
            float x=Mathf.Sin(i*17.13f)*.45f,z=Mathf.Cos(i*9.71f)*.45f;
            var g=new GameObject("SoilClod",typeof(MeshFilter),typeof(MeshRenderer));g.transform.SetParent(root,false);
            g.transform.localPosition=new Vector3(x,.51f+Mathf.Sin(i*3.2f)*.12f,z);
            g.transform.localScale=new Vector3(.035f+(i%5)*.008f,.18f+(i%3)*.1f,.04f+(i%4)*.01f);
            g.GetComponent<MeshFilter>().sharedMesh=lib!=null?lib.soilClod:null;g.GetComponent<Renderer>().sharedMaterial=i%6==0?stones:dirt;g.layer=2;
        }
        for(int i=0;i<12;i++) {
            var g=workshopArt.Box("SoilDebris",new Vector3(-7+Mathf.Sin(i*2.3f)*1.65f,.035f,-6+Mathf.Cos(i*2.3f)*1.65f),new Vector3(.12f,.06f,.2f),i%3==0?workshopArt.Wood:dirt);
            g.transform.localRotation=Quaternion.Euler(0,i*31,0);
        }
    }
    /// <summary>Two fractured wood ends, flex, separation and independent falling pieces.</summary>
    private IEnumerator SnapShovel(GameObject tool)
    {
        var lib=GameArtLibrary.Load();
        if(lib==null||lib.shovelLowerShaft==null||lib.shovelUpperShaft==null){yield return BreakAnimation(tool);yield break;}
        var lower=new GameObject("BrokenShovel_Blade").transform;lower.SetParent(tool.transform,false);
        var upper=new GameObject("BrokenShovel_Grip").transform;upper.SetParent(tool.transform,false);
        var shaft=tool.transform.Find("WoodenShaft");if(shaft!=null)shaft.gameObject.SetActive(false);
        var pieces=new System.Collections.Generic.List<Transform>();
        foreach(Transform part in tool.transform) pieces.Add(part);
        foreach(Transform part in pieces) {
            if(part==lower||part==upper||part==shaft)continue;
            part.SetParent(part.name.Contains("Grip")?upper:lower,true);
        }
        AddFractureShaft(lower,lib.shovelLowerShaft,lib.wood);AddFractureShaft(upper,lib.shovelUpperShaft,lib.wood);
        float elapsed=0;
        while(elapsed<.18f) {
            elapsed+=Time.deltaTime;float flex=Mathf.SmoothStep(0,1,elapsed/.18f);
            lower.localRotation=Quaternion.Euler(0,0,-16*flex);upper.localRotation=Quaternion.Euler(0,0,8*flex);yield return null;
        }
        if(lib.shovelSnap!=null)AudioSource.PlayClipAtPoint(lib.shovelSnap,tool.transform.position,.7f);
        ReleaseShovelPart(lower,new Vector3(0,-.4f,0),new Vector3(.35f,.88f,.15f),new Vector3(.8f,.3f,1.1f),new Vector3(2,3,-5));
        yield return new WaitForSeconds(.27f);
        ReleaseShovelPart(upper,new Vector3(0,.32f,0),new Vector3(.28f,.68f,.15f),new Vector3(-.3f,.1f,.35f),new Vector3(-3,1,2));
        // The authored swing still owns the root for .42s; it must survive its return animation.
        Destroy(tool,1f);
    }
    private static void AddFractureShaft(Transform parent,Mesh mesh,Material material)
    {
        var go=new GameObject("SplinteredWood",typeof(MeshFilter),typeof(MeshRenderer));go.transform.SetParent(parent,false);
        go.GetComponent<MeshFilter>().sharedMesh=mesh;go.GetComponent<Renderer>().sharedMaterial=material;
    }
    private void ReleaseShovelPart(Transform part,Vector3 center,Vector3 size,Vector3 force,Vector3 torque)
    {
        part.SetParent(transform,true);
        var collider=part.gameObject.AddComponent<BoxCollider>();collider.center=center;collider.size=size;
        IgnorePlayerCollision(part.gameObject);
        var rb=part.gameObject.AddComponent<Rigidbody>();rb.mass=.3f;rb.collisionDetectionMode=CollisionDetectionMode.ContinuousDynamic;
        rb.AddForce(Camera.main!=null?Camera.main.transform.TransformDirection(force):force,ForceMode.Impulse);rb.AddTorque(torque,ForceMode.Impulse);
        Destroy(part.gameObject,20f);
    }
}
