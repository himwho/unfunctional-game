using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Keep CPU mesh data only for models used by campaign Polybrush components.</summary>
public static class PolybrushImportRepair
{
    [MenuItem("Unfunctional/Repair Campaign Polybrush Imports")]
    public static void Repair()
    {
        if (EditorApplication.isPlaying) return;
        if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        var setup = EditorSceneManager.GetSceneManagerSetup();
        var paths = new HashSet<string>();
        try
        {
            foreach (var entry in EditorBuildSettings.scenes.Where(s => s.enabled))
            {
                var scene = EditorSceneManager.OpenScene(entry.path, OpenSceneMode.Single);
                foreach (var mesh in SourceMeshes(scene)) AddModel(mesh, paths);
            }
            // Release scene instances before reimporting their models.
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            int repaired = 0;
            foreach (string path in paths.OrderBy(p => p))
            {
                var importer = AssetImporter.GetAtPath(path) as ModelImporter;
                if (importer == null || importer.isReadable) continue;
                importer.isReadable = true;
                importer.SaveAndReimport();
                Debug.Log("[PolybrushImportRepair] Enabled Read/Write: " + path);
                repaired++;
            }
            Debug.Log($"[PolybrushImportRepair] Repaired {repaired} model imports. Authored mesh data and scenes preserved.");
        }
        finally
        {
            if (setup.Any(s => s.isLoaded && s.isActive && !string.IsNullOrEmpty(s.path)))
                EditorSceneManager.RestoreSceneManagerSetup(setup);
        }
    }

    // Polybrush keeps its component internal; use Unity's serialized editor API.
    public static IEnumerable<Mesh> SourceMeshes(Scene scene)
    {
        foreach (var root in scene.GetRootGameObjects())
            foreach (var component in root.GetComponentsInChildren<MonoBehaviour>(true))
            {
                if (component == null || component.GetType().FullName != "UnityEngine.Polybrush.PolybrushMesh") continue;
                using (var serialized = new SerializedObject(component))
                {
                    var source = serialized.FindProperty("m_OriginalMeshObject");
                    if (source?.objectReferenceValue is Mesh original) yield return original;
                }
                var filter = component.GetComponent<MeshFilter>();
                if (filter != null && filter.sharedMesh != null) yield return filter.sharedMesh;
            }
    }

    private static void AddModel(Mesh mesh, HashSet<string> paths)
    {
        if (mesh == null) return;
        string path = AssetDatabase.GetAssetPath(mesh);
        if (AssetImporter.GetAtPath(path) is ModelImporter) paths.Add(path);
    }
}
