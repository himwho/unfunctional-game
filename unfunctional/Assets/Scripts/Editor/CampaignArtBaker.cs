using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>Creates a reusable shaped spade asset and references existing environment art.</summary>
public static class CampaignArtBaker
{
    private const string Folder = "Assets/Resources/GameArt";
    [MenuItem("Unfunctional/Prepare Finale and Workshop Art")]
    public static void Prepare()
    {
        Directory.CreateDirectory(Folder);
        AssetDatabase.Refresh();
        var library = AssetDatabase.LoadAssetAtPath<GameArtLibrary>(Folder + "/Library.asset");
        if (library == null)
        {
            library = ScriptableObject.CreateInstance<GameArtLibrary>();
            AssetDatabase.CreateAsset(library, Folder + "/Library.asset");
        }
        library.tile = AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/Textures/base-white-tile-unity/base-white-tile.mat");
        library.concrete = AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/Textures/painted-worn-concrete-unity/Painted-Worn-Concrete.mat");
        library.wood = AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/Models/Props/Wood 2x4/Wood2x4.mat");
        library.steel = AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/Models/Props/Metal Nail/MetalNail.mat");
        library.door = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Doors/Prefabs/door.prefab");
        library.achievementSound = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Keypad/Sounds/sfx_keypadGranted.wav");
        if (library.shovel == null)
        {
            var mesh = BladeMesh();
            AssetDatabase.CreateAsset(mesh, Folder + "/ShovelBlade.asset");
            var root = new GameObject("WorkshopShovel");
            var blade = new GameObject("StampedSteelBlade", typeof(MeshFilter), typeof(MeshRenderer));
            blade.transform.SetParent(root.transform, false);
            blade.GetComponent<MeshFilter>().sharedMesh = mesh;
            blade.GetComponent<MeshRenderer>().sharedMaterial = library.steel;
            Cylinder(root.transform, "WoodenShaft", new Vector3(0, 0.02f, 0), 0.027f, 0.86f, library.wood);
            Cylinder(root.transform, "SteelSocket", new Vector3(0, -0.34f, 0), 0.038f, 0.20f, library.steel);
            for (int side = -1; side <= 1; side += 2)
            {
                var arm = Cylinder(root.transform, "GripArm", new Vector3(side * 0.075f, 0.51f, 0), 0.018f, 0.21f, library.steel);
                arm.transform.localRotation = Quaternion.Euler(0, 0, side * -24);
            }
            var grip = Cylinder(root.transform, "DGrip", new Vector3(0, 0.60f, 0), 0.030f, 0.22f, library.wood);
            grip.transform.localRotation = Quaternion.Euler(0, 0, 90);
            for (int i = 0; i < 2; i++)
            {
                var rivet = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                rivet.name = "SocketRivet";
                rivet.transform.SetParent(root.transform, false);
                rivet.transform.localPosition = new Vector3(0, -0.28f - i * 0.1f, -0.035f);
                rivet.transform.localScale = new Vector3(0.022f, 0.022f, 0.012f);
                rivet.GetComponent<Renderer>().sharedMaterial = library.steel;
            }
            foreach (var collider in root.GetComponentsInChildren<Collider>()) Object.DestroyImmediate(collider);
            library.shovel = PrefabUtility.SaveAsPrefabAsset(root, Folder + "/WorkshopShovel.prefab");
            Object.DestroyImmediate(root);
        }
        EditorUtility.SetDirty(library);
        AssetDatabase.SaveAssets();
        Debug.Log("[CampaignArt] Ready: shaped shovel prefab, source materials, door and achievement sound.");
    }

    private static GameObject Cylinder(Transform parent, string name, Vector3 pos, float radius, float length, Material material)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        go.name = name; go.transform.SetParent(parent, false);
        go.transform.localPosition = pos;
        go.transform.localScale = new Vector3(radius * 2, length * 0.5f, radius * 2);
        go.GetComponent<Renderer>().sharedMaterial = material;
        return go;
    }

    // Two curved skins and a closed rim: pointed blade, flared shoulders, stamped central rib.
    private static Mesh BladeMesh()
    {
        float[] heights = { -0.83f, -0.73f, -0.50f, -0.40f };
        float[] widths = { 0.015f, 0.12f, 0.16f, 0.13f };
        var vertices = new List<Vector3>(); var uv = new List<Vector2>(); var triangles = new List<int>();
        for (int skin = 0; skin < 2; skin++)
            for (int row = 0; row < 4; row++)
                for (int column = 0; column < 5; column++)
                {
                    float x = (column - 2) / 2f;
                    float bowl = 0.035f * (1 - x * x);
                    vertices.Add(new Vector3(widths[row] * x, heights[row], bowl + skin * 0.012f));
                    uv.Add(new Vector2(column / 4f, row / 3f));
                }
        for (int skin = 0; skin < 2; skin++)
            for (int row = 0; row < 3; row++)
                for (int col = 0; col < 4; col++)
                {
                    int a = skin * 20 + row * 5 + col;
                    if (skin == 0) Quad(triangles, a, a + 5, a + 6, a + 1);
                    else Quad(triangles, a, a + 1, a + 6, a + 5);
                }
        int[] rim = { 0,1,2,3,4,9,14,19,18,17,16,15,10,5 };
        for (int i = 0; i < rim.Length; i++)
        {
            int a = rim[i], b = rim[(i + 1) % rim.Length];
            Quad(triangles, a, b, b + 20, a + 20);
        }
        var mesh = new Mesh { name = "Stamped shovel blade" };
        mesh.SetVertices(vertices); mesh.SetUVs(0, uv); mesh.SetTriangles(triangles, 0);
        mesh.RecalculateNormals(); mesh.RecalculateBounds(); mesh.RecalculateTangents();
        return mesh;
    }
    private static void Quad(List<int> t, int a, int b, int c, int d) { t.AddRange(new[] { a,b,c,a,c,d }); }
}
