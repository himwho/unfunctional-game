using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;

public class ToolIconGenerator : EditorWindow
{
    private const int IconWidth = 130;
    private const int IconHeight = 104;
    private const string OutputFolder = "Assets/Resources/Icons";

    private struct ToolDef
    {
        public string name;
        public string[] fbxPaths;
        public string materialPath;
        public bool topDown;
    }

    private static readonly ToolDef[] tools = new ToolDef[]
    {
        new ToolDef
        {
            name = "Hammer",
            fbxPaths = new[]
            {
                "Assets/MeshyImports/Hammer/hammerhead.fbx",
                "Assets/MeshyImports/Hammer/hammerhandle.fbx"
            },
            materialPath = "Assets/MeshyImports/Hammer/hammertexture.mat"
        },
        new ToolDef
        {
            name = "Saw",
            fbxPaths = new[]
            {
                "Assets/MeshyImports/Saw/sawblade.fbx",
                "Assets/MeshyImports/Saw/sawbladehandle.fbx"
            },
            materialPath = "Assets/MeshyImports/Saw/Saw.mat"
        },
        new ToolDef
        {
            name = "Broom",
            fbxPaths = new[]
            {
                "Assets/MeshyImports/Broomstick/broomstickhandle.fbx",
                "Assets/MeshyImports/Broomstick/broomsticktip.fbx"
            },
            materialPath = "Assets/MeshyImports/Broomstick/Broomstick.mat"
        },
        new ToolDef
        {
            name = "Wrench",
            fbxPaths = new[]
            {
                "Assets/MeshyImports/Wrench/wrenchhandle.fbx",
                "Assets/MeshyImports/Wrench/wrenchtip.fbx"
            },
            materialPath = "Assets/MeshyImports/Wrench/Wrench.mat",
            topDown = true
        }
    };

    [MenuItem("Tools/Generate Tool Icons")]
    public static void GenerateIcons()
    {
        if (!Directory.Exists(OutputFolder))
            Directory.CreateDirectory(OutputFolder);

        foreach (var tool in tools)
            RenderToolIcon(tool);

        AssetDatabase.Refresh();
        Debug.Log($"[ToolIconGenerator] All icons generated in {OutputFolder}");
    }

    private static void RenderToolIcon(ToolDef tool)
    {
        var instantiated = new List<GameObject>();

        Material mat = null;
        if (!string.IsNullOrEmpty(tool.materialPath))
            mat = AssetDatabase.LoadAssetAtPath<Material>(tool.materialPath);

        foreach (string fbxPath in tool.fbxPaths)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(fbxPath);
            if (prefab == null)
            {
                Debug.LogWarning($"[ToolIconGenerator] Could not load: {fbxPath}");
                continue;
            }

            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            instance.hideFlags = HideFlags.HideAndDontSave;

            if (mat != null)
            {
                foreach (var renderer in instance.GetComponentsInChildren<Renderer>())
                {
                    var mats = renderer.sharedMaterials;
                    for (int i = 0; i < mats.Length; i++)
                        mats[i] = mat;
                    renderer.sharedMaterials = mats;
                }
            }

            instantiated.Add(instance);
        }

        if (instantiated.Count == 0)
        {
            Debug.LogError($"[ToolIconGenerator] No models loaded for {tool.name}");
            return;
        }

        Bounds combinedBounds = GetCombinedBounds(instantiated);

        var camGO = new GameObject("IconCamera") { hideFlags = HideFlags.HideAndDontSave };
        var cam = camGO.AddComponent<Camera>();
        cam.orthographic = true;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0, 0, 0, 0);
        cam.nearClipPlane = 0.01f;
        cam.farClipPlane = 100f;
        cam.cullingMask = ~0;

        float padding = 1.15f;
        float aspect = (float)IconWidth / IconHeight;

        Vector3 center = combinedBounds.center;

        if (tool.topDown)
        {
            float boundsWidth = combinedBounds.size.x;
            float boundsDepth = combinedBounds.size.z;

            float orthoSize;
            if (boundsWidth / aspect > boundsDepth)
                orthoSize = (boundsWidth / aspect) * 0.5f * padding;
            else
                orthoSize = boundsDepth * 0.5f * padding;

            cam.orthographicSize = orthoSize;
            cam.aspect = aspect;

            float camDistance = combinedBounds.size.y + 5f;
            camGO.transform.position = center + Vector3.up * camDistance;
            camGO.transform.rotation = Quaternion.LookRotation(Vector3.down, Vector3.forward);
        }
        else
        {
            float boundsWidth = combinedBounds.size.x;
            float boundsHeight = combinedBounds.size.y;

            float orthoSize;
            if (boundsWidth / aspect > boundsHeight)
                orthoSize = (boundsWidth / aspect) * 0.5f * padding;
            else
                orthoSize = boundsHeight * 0.5f * padding;

            cam.orthographicSize = orthoSize;
            cam.aspect = aspect;

            float camDistance = combinedBounds.size.z + 5f;
            camGO.transform.position = center + Vector3.forward * camDistance;
            camGO.transform.rotation = Quaternion.LookRotation(-Vector3.forward, Vector3.up);
        }

        var lightGO = new GameObject("IconLight") { hideFlags = HideFlags.HideAndDontSave };
        var light = lightGO.AddComponent<Light>();
        light.type = LightType.Directional;
        light.intensity = 1.5f;
        light.color = Color.white;
        lightGO.transform.rotation = Quaternion.Euler(30f, -30f, 0f);

        var fillLightGO = new GameObject("IconFillLight") { hideFlags = HideFlags.HideAndDontSave };
        var fillLight = fillLightGO.AddComponent<Light>();
        fillLight.type = LightType.Directional;
        fillLight.intensity = 0.5f;
        fillLight.color = Color.white;
        fillLightGO.transform.rotation = Quaternion.Euler(-20f, 150f, 0f);

        var rt = new RenderTexture(IconWidth, IconHeight, 24, RenderTextureFormat.ARGB32);
        rt.antiAliasing = 4;
        rt.Create();

        cam.targetTexture = rt;
        cam.Render();

        RenderTexture.active = rt;
        var tex = new Texture2D(IconWidth, IconHeight, TextureFormat.RGBA32, false);
        tex.ReadPixels(new Rect(0, 0, IconWidth, IconHeight), 0, 0);
        tex.Apply();
        RenderTexture.active = null;

        byte[] pngData = tex.EncodeToPNG();
        string outputPath = Path.Combine(OutputFolder, tool.name + ".png");
        File.WriteAllBytes(outputPath, pngData);
        Debug.Log($"[ToolIconGenerator] Saved: {outputPath} ({IconWidth}x{IconHeight})");

        cam.targetTexture = null;
        rt.Release();
        Object.DestroyImmediate(rt);
        Object.DestroyImmediate(tex);
        Object.DestroyImmediate(camGO);
        Object.DestroyImmediate(lightGO);
        Object.DestroyImmediate(fillLightGO);
        foreach (var go in instantiated)
            Object.DestroyImmediate(go);
    }

    private static Bounds GetCombinedBounds(List<GameObject> objects)
    {
        Bounds bounds = new Bounds();
        bool initialized = false;

        foreach (var go in objects)
        {
            foreach (var renderer in go.GetComponentsInChildren<Renderer>())
            {
                if (!initialized)
                {
                    bounds = renderer.bounds;
                    initialized = true;
                }
                else
                {
                    bounds.Encapsulate(renderer.bounds);
                }
            }
        }

        return bounds;
    }
}
