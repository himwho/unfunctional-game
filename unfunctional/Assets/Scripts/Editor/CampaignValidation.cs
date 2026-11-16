using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Non-destructive scene checks, usable from the menu or Unity -executeMethod.</summary>
public static class CampaignValidation
{
    public const string FinalePath = "Assets/Scenes/LEVEL12.unity";

    [MenuItem("Unfunctional/Create Missing Achievements Scene")]
    public static void CreateMissingFinale()
    {
        if (System.IO.File.Exists(FinalePath)) return;
        if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        var setup = EditorSceneManager.GetSceneManagerSetup();
        try
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var manager = new GameObject("Level12Manager").AddComponent<Level12_Achievements>();
            manager.needsPlayer = true;
            manager.wantsCursorLocked = true;
            manager.levelDisplayName = "Over-achiever";
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.45f, 0.48f, 0.5f);
            EditorSceneManager.SaveScene(scene, FinalePath);
        }
        finally
        {
            if (setup.Any(s => s.isLoaded && s.isActive && !string.IsNullOrEmpty(s.path)))
                EditorSceneManager.RestoreSceneManagerSetup(setup);
        }
    }

    // Batch setup only creates the missing scene. It never rebuilds authored levels.
    public static void PrepareAndValidate()
    {
        CreateMissingFinale();
        SceneSetupEditor.RegisterScenesInBuildSettings();
        Validate();
    }

    [MenuItem("Unfunctional/Validate Campaign")]
    public static void Validate()
    {
        if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        var setup = EditorSceneManager.GetSceneManagerSetup();
        try
        {
            var build = EditorBuildSettings.scenes.Where(s => s.enabled).ToArray();
            Require(build.Length > 1 && build[0].path == "Assets/Scenes/GLOBAL.unity", "GLOBAL must be first in the build.");
            var global = EditorSceneManager.OpenScene(build[0].path, OpenSceneMode.Single);
            var gm = Roots<GameManager>(global).Single();
            string[] campaign = gm.levelSceneNames;
            Require(campaign.Distinct().Count() == campaign.Length, "Campaign contains duplicate scenes.");
            Require(campaign.SequenceEqual(build.Skip(1).Select(s => System.IO.Path.GetFileNameWithoutExtension(s.path))),
                "GLOBAL campaign order and enabled Build Settings differ.");
            Require(gm.playerPrefab != null || Resources.Load<GameObject>("Player") != null, "Player prefab missing.");
            CheckScripts(global);
            Require(Roots<InputManager>(global).Length == 1, "GLOBAL needs one InputManager.");
            Require(Roots<GamePauseMenu>(global).Length == 1, "GLOBAL needs one pause menu.");
            foreach (var entry in build.Skip(1))
            {
                Require(System.IO.File.Exists(entry.path), "Scene missing: " + entry.path);
                var scene = EditorSceneManager.OpenScene(entry.path, OpenSceneMode.Single);
                CheckScripts(scene);
                CheckPolybrushSources(scene);
                Require(Roots<LevelManager>(scene).Length == 1, scene.name + " must have one LevelManager.");
            }
            Debug.Log($"[CampaignValidation] PASS: {campaign.Length} campaign scenes, bootstrap, build order, player and script references.");
        }
        finally
        {
            if (setup.Any(s => s.isLoaded && s.isActive && !string.IsNullOrEmpty(s.path)))
                EditorSceneManager.RestoreSceneManagerSetup(setup);
        }
    }

    private static T[] Roots<T>(Scene scene) where T : Component =>
        scene.GetRootGameObjects().SelectMany(go => go.GetComponentsInChildren<T>(true)).ToArray();

    private static void CheckScripts(Scene scene)
    {
        foreach (var root in scene.GetRootGameObjects())
            foreach (var child in root.GetComponentsInChildren<Transform>(true))
                Require(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(child.gameObject) == 0,
                    $"Missing script in {scene.name}: {child.name}");
    }

    private static void CheckPolybrushSources(Scene scene)
    {
        foreach (var mesh in PolybrushImportRepair.SourceMeshes(scene))
        {
            if (mesh == null) continue;
            var path = AssetDatabase.GetAssetPath(mesh);
            var importer = AssetImporter.GetAtPath(path) as ModelImporter;
            Require(importer == null || importer.isReadable,
                $"{scene.name}: Polybrush needs Read/Write on {path}. Run Repair Campaign Polybrush Imports.");
        }
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException("[CampaignValidation] " + message);
    }
}
