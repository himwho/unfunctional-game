using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

/// <summary>
/// Integration tests against the actual scenes and player. Run in a separate editor:
/// -batchmode -projectPath ... -executeMethod CampaignSmokeTests.Run
/// Do not pass -quit: the runner exits after play-mode tests finish.
/// </summary>
[InitializeOnLoad]
public static class CampaignSmokeTests
{
    private const string PendingKey = "Unfunctional.CampaignSmoke.Pending";
    static CampaignSmokeTests()
    {
        EditorApplication.playModeStateChanged += state =>
        {
            if (state != PlayModeStateChange.EnteredPlayMode || !SessionState.GetBool(PendingKey, false)) return;
            SessionState.SetBool(PendingKey, false);
            Object.DontDestroyOnLoad(new GameObject("CampaignSmokeDriver", typeof(CampaignSmokeDriver)));
        };
    }

    [MenuItem("Unfunctional/Run Campaign Smoke Tests")]
    public static void Run() => Begin(false);

    [MenuItem("Unfunctional/Run Scene Interaction Tests")]
    public static void RunSceneInteractions() => Begin(true);

    private static void Begin(bool focused)
    {
        if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        CampaignValidation.Validate();
        EditorSceneManager.OpenScene("Assets/Scenes/GLOBAL.unity", OpenSceneMode.Single);
        SessionState.SetBool("Unfunctional.SceneInteractions",focused);
        SessionState.SetBool(PendingKey, true);
        EditorApplication.isPlaying = true;
    }
}

