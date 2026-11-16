using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Idempotent asset preparation; never rebuilds authored levels or overwrites artist-edited assets.</summary>
public static class DepartmentArtBaker
{
    const string Folder="Assets/Resources/GameArt/";
    [MenuItem("Unfunctional/Prepare Department Art and Crafting")]
    public static void Prepare()
    {
        if(EditorApplication.isPlaying){Debug.LogWarning("Stop Play mode before preparing art.");return;}
        CampaignArtBaker.Prepare();
        var lib=AssetDatabase.LoadAssetAtPath<GameArtLibrary>(Folder+"Library.asset");
        BakeSprite("ButtonBezel",128,64,false,-1);BakeSprite("SliderKnob",32,64,true,-1);
        for(int i=0;i<6;i++)BakeSprite("Award"+i,128,128,false,i);
        lib.shovelLowerShaft=MeshAsset("ShovelLowerShaft",()=>Shaft(false));
        lib.shovelUpperShaft=MeshAsset("ShovelUpperShaft",()=>Shaft(true));
        lib.soilClod=MeshAsset("SoilClod",Clod);
        lib.achievementVariants=new AudioClip[5];
        for(int i=0;i<5;i++)lib.achievementVariants[i]=BakeSound("AwardSound"+i,i,false);
        lib.shovelSnap=BakeSound("ShovelSnap",7,true);
        var atlas=AssetImporter.GetAtPath(Folder+"ShopProducts.png") as TextureImporter;
        if(atlas!=null){atlas.textureType=TextureImporterType.Default;atlas.mipmapEnabled=false;atlas.wrapMode=TextureWrapMode.Clamp;atlas.maxTextureSize=2048;atlas.textureCompression=TextureImporterCompression.Uncompressed;atlas.SaveAndReimport();}
        EditorUtility.SetDirty(lib);AssetDatabase.SaveAssets();
        CreateCraftingScene();
        Debug.Log("[DepartmentArt] Ready: three room treatments, twenty product images, custom widgets, fracture meshes, awards, audio and crafting scene.");
    }
    static Mesh MeshAsset(string name,Func<Mesh> create)
    {
        string path=Folder+name+".asset";var found=AssetDatabase.LoadAssetAtPath<Mesh>(path);if(found!=null)return found;
        var m=create();m.name=name;AssetDatabase.CreateAsset(m,path);return m;
    }
    static void BakeSprite(string name,int w,int h,bool knob,int award)
    {
        string path=Folder+name+".png";
        if(!File.Exists(path)) {
            var tex=new Texture2D(w,h,TextureFormat.RGBA32,false);var pixels=new Color32[w*h];
            for(int y=0;y<h;y++)for(int x=0;x<w;x++) {
                int edge=Math.Min(Math.Min(x,w-1-x),Math.Min(y,h-1-y));float v=.79f;
                if(edge<2)v=.12f;else if(edge<5)v=(x<5||y>h-6)?.96f:.30f;else if(edge<7)v=.47f;
                if(knob&&x>9&&x<w-9&&y>12&&y<h-12&&y%6<2)v=.28f;
                if(!knob&&award<0&&((x<14||x>w-15)&&(y<14||y>h-15))) {float dx=x<(w/2)?x-10:x-(w-11),dy=y<h/2?y-10:y-(h-11);if(dx*dx+dy*dy<7)v=.18f;}
                v-=((x*19+y*31)%137==0)?.12f:0;
                Color c=new Color(v,v,v,1);
                if(award>=0) {
                    c=edge<6?new Color(.66f,.47f,.16f):new Color(.08f,.17f,.16f);
                    float a=x/128f,b=y/128f;bool ink=false;
                    switch(award) {
                        case 0:ink=(a>.22f&&a<.76f&&b>.32f&&b<.47f)||(a>.27f&&a<.45f&&b>.44f&&b<.72f);break;
                        case 1:ink=(Mathf.Abs(a-.5f)<.055f&&b>.25f&&b<.7f)||(b>.56f&&b<.82f&&Mathf.Abs(a-.5f)<(.82f-b)*.9f+.03f);break;
                        case 2:ink=b>.26f&&b<.75f&&((a>.28f&&a<.43f)||(a>.57f&&a<.72f));break;
                        case 3:ink=a>.2f&&a<.8f&&Mathf.Abs(a-.5f)>.025f&&b>.3f+Mathf.Abs(a-.5f)*.25f&&b<.72f+Mathf.Abs(a-.5f)*.1f;break;
                        case 4:ink=(a-.5f)*(a-.5f)+(b-.5f)*(b-.5f)<.075f;break;
                        default:ink=(b>.43f&&b<.74f&&Mathf.Abs(a-.5f)<(b-.3f)*.55f)||(Mathf.Abs(a-.5f)<.04f&&b>.27f&&b<.5f)||(a>.32f&&a<.68f&&b>.23f&&b<.3f);break;
                    }
                    if(ink)c=new Color(.92f,.72f,.29f);
                }
                pixels[y*w+x]=c;
            }
            tex.SetPixels32(pixels);tex.Apply();File.WriteAllBytes(path,tex.EncodeToPNG());UnityEngine.Object.DestroyImmediate(tex);AssetDatabase.ImportAsset(path);
        }
        var imp=AssetImporter.GetAtPath(path) as TextureImporter;imp.textureType=TextureImporterType.Sprite;imp.spriteImportMode=SpriteImportMode.Single;
        imp.spriteBorder=award<0?new Vector4(8,8,8,8):Vector4.zero;imp.mipmapEnabled=false;imp.textureCompression=TextureImporterCompression.Uncompressed;imp.SaveAndReimport();
    }
    static Mesh Shaft(bool upper)
    {
        const int count=12;var vertices=new List<Vector3>();var tris=new List<int>();var uv=new List<Vector2>();
        for(int row=0;row<2;row++)for(int i=0;i<count;i++) {
            float a=i*Mathf.PI*2/count;float jag=.02f+((i*7)%5-2)*.016f;
            float y=upper?(row==0?jag:.45f):(row==0?-.41f:jag);
            vertices.Add(new Vector3(Mathf.Cos(a)*.027f,y,Mathf.Sin(a)*.027f));uv.Add(new Vector2(i/(float)count,row));
        }
        for(int i=0;i<count;i++){int j=(i+1)%count;tris.AddRange(new[]{i,j,count+j,i,count+j,count+i});}
        for(int row=0;row<2;row++)for(int i=1;i<count-1;i++) {
            int o=row*count;tris.AddRange(row==0?new[]{o,o+i+1,o+i}:new[]{o,o+i,o+i+1});
        }
        var mesh=new Mesh();mesh.SetVertices(vertices);mesh.SetUVs(0,uv);mesh.SetTriangles(tris,0);mesh.RecalculateNormals();mesh.RecalculateBounds();return mesh;
    }
    static Mesh Clod()
    {
        var mesh=new Mesh();mesh.vertices=new[]{new Vector3(0,1,0),new Vector3(1,0,0),new Vector3(0,0,.8f),new Vector3(-.8f,0,0),new Vector3(0,0,-1),new Vector3(.1f,-.7f,0)};
        mesh.triangles=new[]{0,2,1,0,3,2,0,4,3,0,1,4,5,1,2,5,2,3,5,3,4,5,4,1};mesh.RecalculateNormals();return mesh;
    }
    static AudioClip BakeSound(string name,int variant,bool snap)
    {
        string path=Folder+name+".wav";
        if(!File.Exists(path)) {
            int rate=22050,count=(int)(rate*(snap?.38f:.62f));var random=new System.Random(420+variant);
            using(var stream=new BinaryWriter(File.Create(path))) {
                stream.Write(System.Text.Encoding.ASCII.GetBytes("RIFF"));stream.Write(36+count*2);stream.Write(System.Text.Encoding.ASCII.GetBytes("WAVEfmt "));
                stream.Write(16);stream.Write((short)1);stream.Write((short)1);stream.Write(rate);stream.Write(rate*2);stream.Write((short)2);stream.Write((short)16);
                stream.Write(System.Text.Encoding.ASCII.GetBytes("data"));stream.Write(count*2);
                for(int i=0;i<count;i++) {
                    float t=i/(float)rate,noise=(float)random.NextDouble()*2-1;
                    float signal=snap?noise*(Mathf.Exp(-t*23)+.45f*Mathf.Exp(-Mathf.Abs(t-.09f)*90)):
                        Mathf.Sin(2*Mathf.PI*(520+variant*110)*t)*Mathf.Exp(-t*8)*.35f+Mathf.Sin(2*Mathf.PI*(780+variant*140)*t)*Mathf.Exp(-t*11)*.18f+noise*Mathf.Exp(-t*60)*.22f;
                    stream.Write((short)(Mathf.Clamp(signal,-1,1)*22000));
                }
            }
            AssetDatabase.ImportAsset(path);
        }
        return AssetDatabase.LoadAssetAtPath<AudioClip>(path);
    }
    static void CreateCraftingScene()
    {
        const string path="Assets/Scenes/LEVEL_CRAFTING.unity";
        if(!File.Exists(path)) {
            var original=SceneManager.GetActiveScene();var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);
            var root=new GameObject("CraftingManager");SceneManager.MoveGameObjectToScene(root,scene);
            var level=root.AddComponent<LevelCrafting_Mandatory>();level.needsPlayer=true;level.wantsCursorLocked=true;level.levelDisplayName="Some Assembly Required";
            EditorSceneManager.SaveScene(scene,path);EditorSceneManager.CloseScene(scene,true);SceneManager.SetActiveScene(original);
        }
        // Synchronize only the campaign list, leaving every authored room intact.
        var global=SceneManager.GetSceneByName("GLOBAL");bool opened=!global.IsValid()||!global.isLoaded;
        if(opened)global=EditorSceneManager.OpenScene("Assets/Scenes/GLOBAL.unity",OpenSceneMode.Additive);
        var gm=global.GetRootGameObjects().SelectMany(x=>x.GetComponentsInChildren<GameManager>(true)).Single();
        var names=gm.levelSceneNames.ToList();if(!names.Contains("LEVEL_CRAFTING")){names.Insert(names.IndexOf("LEVEL12"),"LEVEL_CRAFTING");gm.levelSceneNames=names.ToArray();EditorUtility.SetDirty(gm);EditorSceneManager.MarkSceneDirty(global);EditorSceneManager.SaveScene(global);}
        if(opened)EditorSceneManager.CloseScene(global,true);SceneSetupEditor.RegisterScenesInBuildSettings();
    }
}
