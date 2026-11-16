#if UNITY_EDITOR
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

public partial class CampaignSmokeDriver : MonoBehaviour
{
    [Serializable] private class Report
    {
        public bool passed;
        public string unityVersion;
        public List<string> checks = new List<string>();
        public List<string> errors = new List<string>();
    }

    private readonly Report report = new Report();
    private bool finished;
    private float startTime;
    private string output;
    private string originalStorageKey;
    private CampaignProgress.Checkpoint originalCalibration;

    private void Awake()
    {
        originalStorageKey = CampaignProgress.StorageKey;
        CampaignProgress.StorageKey = "Unfunctional.Smoke." + Guid.NewGuid().ToString("N");
        originalCalibration = new CampaignProgress.Checkpoint
        {
            calibrationKept = Level2_SettingsPuzzle.SettingsPersisted,
            brightness = Level2_SettingsPuzzle.PersistentBrightness,
            contrast = Level2_SettingsPuzzle.PersistentContrast,
            volume = Level2_SettingsPuzzle.PersistentVolume
        };
        Level2_SettingsPuzzle.RestoreCalibration(false);
    }

    private void OnDestroy()
    {
        if (originalStorageKey == null) return;
        CampaignProgress.Clear();
        CampaignProgress.StorageKey = originalStorageKey;
        Level2_SettingsPuzzle.RestoreCalibration(originalCalibration.calibrationKept,
            originalCalibration.brightness, originalCalibration.contrast, originalCalibration.volume);
        originalStorageKey = null;
    }

    private IEnumerator Start()
    {
        startTime = Time.realtimeSinceStartup;
        report.unityVersion = Application.unityVersion;
        bool focused=SessionState.GetBool("Unfunctional.SceneInteractions",false);
        SessionState.SetBool("Unfunctional.SceneInteractions",false);
        output = Path.GetFullPath(focused?"TestResults/SceneInteractions":"TestResults/CampaignSmoke");
        string[] args = Environment.GetCommandLineArgs();
        for (int i = 0; i < args.Length - 1; i++) if (args[i] == "-campaignResults") output = args[i + 1];
        Directory.CreateDirectory(output);
        Application.logMessageReceived += CaptureError;
        // Flatten iterators to turn assertion failures into a report and a failing exit code.
        var stack = new Stack<IEnumerator>();
        stack.Push(focused?ExerciseSceneInteractions():ExerciseCampaign());
        while (stack.Count > 0 && !finished)
        {
            bool next = false;
            object current = null;
            try
            {
                next = stack.Peek().MoveNext();
                if (next) current = stack.Peek().Current;
            }
            catch (Exception ex) { report.errors.Add(ex.ToString()); Finish(); yield break; }
            if (!next) { stack.Pop(); continue; }
            if (current is IEnumerator nested) stack.Push(nested);
            else yield return current;
        }
        Finish();
    }

    private void Update()
    {
        if (!finished && startTime > 0 && Time.realtimeSinceStartup - startTime > 240f)
        {
            report.errors.Add("Timed out after 240 seconds.");
            Finish();
        }
    }

    private IEnumerator ExerciseCampaign()
    {
        yield return WaitForLevel(0);
        var gm = GameManager.Instance;
        Check(GameObject.Find("RealOptionsCanvas") == null, "Independent F3 settings overlay removed");
        Check(!gm.CanContinueCampaign, "Fresh profile has no Continue option");
        foreach (string invalid in new[] { "not json", "{}", "{\"version\":99,\"scene\":\"LEVEL4\"}",
            "{\"version\":1,\"scene\":\"REMOVED_SCENE\"}", "{\"version\":1,\"scene\":\"LEVEL1\"}" })
        {
            PlayerPrefs.SetString(CampaignProgress.StorageKey, invalid);
            Check(!gm.CanContinueCampaign, "Invalid checkpoint safely ignored: " + invalid);
        }
        CampaignProgress.Clear();
        Check(gm.levelSceneNames.Length == 14, "All 14 campaign stages registered");
        Check(gm.CurrentState == GameManager.GameState.MainMenu, "GLOBAL boots into the confusing menu");
        Check(FindAnyObjectByType<GamePauseMenu>() != null, "Persistent pause menu exists");
        yield return ExerciseDepartmentArt(FindAnyObjectByType<LevelManager>());
        yield return ExerciseDevelopmentMenus();
        for (int i = 1; i < gm.levelSceneNames.Length; i++)
        {
            gm.LoadLevel(i);
            gm.LoadLevel(0); // A simultaneous request must be ignored.
            yield return WaitForLevel(i);
            Check(CampaignProgress.TryRead(gm.levelSceneNames, out var saved) && saved.scene == gm.levelSceneNames[i],
                "Checkpoint saved after loading " + gm.levelSceneNames[i]);
            yield return new WaitForSecondsRealtime(0.2f);
            var manager = FindAnyObjectByType<LevelManager>();
            Check(manager != null && manager.gameObject.scene.name == gm.levelSceneNames[i], "Load " + gm.levelSceneNames[i]);
            Check(!manager.needsPlayer || gm.CurrentPlayer != null, "Player ready in " + gm.levelSceneNames[i]);
            Check(SceneManager.sceneCount == 2, "No orphan scenes after " + gm.levelSceneNames[i]);
            Check(FindObjectsByType<UnityEngine.EventSystems.EventSystem>(FindObjectsSortMode.None).Length == 1,
                "One EventSystem in " + gm.levelSceneNames[i]);

            yield return ExerciseAuthoredRules(manager);
            yield return ExerciseDepartmentArt(manager);
            yield return ExerciseSceneAuthoredArt(manager);
            yield return ExerciseEditableDepartments(manager);

            if (manager is Level2_SettingsPuzzle settings)
            {
                // No microphone is opened: force the unavailable-device branch directly.
                Set(settings, "micAvailable", false);
                Invoke(settings, "UpdateStep7");
                Check(Get<Button>(settings, "step7Skip").gameObject.activeSelf, "Microphone fallback remains available");
            }
            if (manager is Level4_KeypadPuzzle keypad)
            {
                Invoke(keypad, "GenerateOfflineCode");
                string code = Get<string>(keypad, "currentValidCode");
                Check(keypad.offlineMode && code.Length == 9 && code.All(char.IsDigit), "Local Rodney reply produces a nine-digit code");
                yield return null;
                Check(Get<Text>(keypad, "localReplyText").text.Contains(code), "Local code visible in player HUD");
                Set(keypad, "codeExpiryTime", Time.unscaledTime - 1);
                Invoke(keypad, "HandleCodeSubmitted", code);
                Check(!Get<bool>(keypad, "doorOpening"), "Expired local code rejected");
                Invoke(keypad, "GenerateOfflineCode");
                Invoke(keypad, "HandleCodeSubmitted", Get<string>(keypad, "currentValidCode"));
                Check(Get<bool>(keypad, "doorOpening"), "Fresh local code opens the door");
            }
            if (manager is Level8_DLCDoor store)
            {
                Invoke(store, "OpenShop");
                gm.TogglePause();
                gm.TogglePause();
                Check(Cursor.lockState == CursorLockMode.None, "Shop retains its cursor after pause/resume");
                Check(store.TryCloseModal(), "Escape routing closes the store before pausing");
                Check(gm.CurrentState == GameManager.GameState.Playing, "Closing a modal does not also pause");
            }
            if (manager is Level10_ItemDegradation workshop) yield return ExerciseWorkshop(workshop);
            if (manager is Level13_SecondPersonShooter shooter)
            {
                yield return new WaitForSecondsRealtime(0.5f);
                Check(Get<Camera>(shooter, "secondPersonCam") != null, "Second-person camera initialized");
            }
        }

        var finale = FindAnyObjectByType<Level12_Achievements>();
        Check(finale != null && !finale.ExitUnlocked, "Finale starts with a locked exit");
        finale.UnlockRealAchievement("read");
        finale.UnlockRealAchievement("read");
        finale.UnlockRealAchievement("made-up");
        Check(finale.RealAchievementsCollected == 1, "Duplicate and unknown achievements cannot advance progress");
        gm.TogglePause();
        Check(Time.timeScale == 0, "Pause stops simulation");
        var gameplay = typeof(LevelManager).GetProperty("CanProcessGameplay", BindingFlags.Instance | BindingFlags.NonPublic);
        Check(!(bool)gameplay.GetValue(finale), "Paused level rejects gameplay input");
        gm.ReloadCurrentLevel();
        yield return WaitForLevel(System.Array.IndexOf(gm.levelSceneNames, "LEVEL12"));
        Check(Time.timeScale == 1, "Restart from pause restores simulation");
        finale = FindAnyObjectByType<Level12_Achievements>();
        Check(finale.RealAchievementsCollected == 0, "Restart clears achievement progress");
        CheckFinaleTargets();
        yield return Capture("achievements-room.png");
        foreach (string id in new[] { "walk", "jump", "wait", "read", "button" }) finale.UnlockRealAchievement(id);
        Check(finale.ExitUnlocked && !finale.IsLevelComplete, "Five unique objectives unlock exit without auto-finishing");
        finale.CompleteLevel();
        yield return null;
        Check(gm.CurrentState == GameManager.GameState.Completed, "Finale reaches campaign Completed state");
        Check(FindAnyObjectByType<CampaignEnding>() != null && Cursor.lockState == CursorLockMode.None, "Ending visible with free cursor");
        Check(!gm.CanContinueCampaign, "Completed campaign does not offer a stale Continue");
        yield return Capture("campaign-ending.png");
        yield return ExerciseCredits();
        gm.LoadLevel(0);
        yield return WaitForLevel(0);
        Check(Time.timeScale == 1 && FindAnyObjectByType<CampaignEnding>() == null, "Ending returns to playable menu");
        gm.LoadLevel(3);
        yield return WaitForLevel(3);
        Level2_SettingsPuzzle.RestoreCalibration(true, 0.5f, 0.5f, 0.4f);
        CampaignProgress.Save("LEVEL4");
        Level2_SettingsPuzzle.RestoreCalibration(false);
        gm.LoadLevel(0);
        yield return WaitForLevel(0);
        Check(gm.CanContinueCampaign && GameObject.Find("ContinueCampaign") != null, "Returning to menu preserves checkpoint and shows Continue");
        gm.ContinueCampaign();
        yield return WaitForLevel(3);
        Check(Level2_SettingsPuzzle.SettingsPersisted && Mathf.Approximately(AudioListener.volume, 0.4f),
            "Continue loads stable scene name and restores calibration");
        gm.StartNewCampaign();
        yield return WaitForLevel(0);
        Check(!gm.CanContinueCampaign && !Level2_SettingsPuzzle.SettingsPersisted && AudioListener.volume == 1f,
            "Explicit new run clears checkpoint and restores default calibration");
        yield return ExerciseOriginalRNG();
    }

    private IEnumerator ExerciseWorkshop(Level10_ItemDegradation workshop)
    {
        var tasks = Get<List<Level10_ItemDegradation.Task>>(workshop, "tasks");
        Check(tasks.Count == 5 && tasks.Any(t => t.toolName == "Shovel"), "Workshop has all five tasks");
        Check(workshop.maxDurability == 3, "Workshop respects authored durability setting");
        Check(GameObject.Find("ToolRack_Shovel") != null && GameObject.Find("Station_Shovel") != null, "Dig station and supply crates created");
        int shovelIndex = tasks.FindIndex(t => t.toolName == "Shovel");
        while (!tasks[shovelIndex].completed)
        {
            Invoke(workshop, "DispenseReplacementTool", "Shovel");
            Check(Get<GameObject>(workshop, "currentToolVisual") != null, "Shovel has a held model");
            Invoke(workshop, "SwitchToSlot", 1);
            Invoke(workshop, "SwitchToSlot", 0);
            Check(Get<GameObject>(workshop, "currentToolVisual") != null, "Shovel survives inventory switching");
            while (Get<int>(workshop, "currentToolDurability") > 0 && !tasks[shovelIndex].completed)
                Invoke(workshop, "UseTool", shovelIndex);
            yield return new WaitForSecondsRealtime(0.7f);
        }
        Check(!Get<bool>(workshop, "allTasksComplete"), "Digging alone cannot open workshop exit");
        // Exhaust more tools than the authored room contains, then replace them.
        foreach (string tool in new[] { "Hammer", "Saw", "Wrench", "Broom" })
        {
            for (int n = 0; n < 5; n++)
            {
                Invoke(workshop, "DispenseReplacementTool", tool);
                Check(Get<string>(workshop, "currentToolName") == tool, "Replacement supplied: " + tool);
                Invoke(workshop, "BreakTool");
            }
            Invoke(workshop, "DispenseReplacementTool", tool);
            yield return new WaitForSecondsRealtime(1f);
            Check(Get<string>(workshop, "currentToolName") == tool, "Delayed break does not detach replacement: " + tool);
            Invoke(workshop, "DropCurrentItem");
        }
        // Test task/door gating separately from the visual alignment minigames.
        foreach (var task in tasks) task.completed = true;
        Invoke(workshop, "CheckAllTasksComplete");
        Check(Get<bool>(workshop, "allTasksComplete") && !workshop.IsLevelComplete, "All tasks enable door interaction, not an automatic skip");
        yield return Capture("workshop-completed.png");
    }

    private IEnumerator WaitForLevel(int index)
    {
        float deadline = Time.realtimeSinceStartup + 45f;
        while (GameManager.Instance == null || GameManager.Instance.IsLoading || GameManager.Instance.CurrentLevelIndex != index)
        {
            if (Time.realtimeSinceStartup > deadline) throw new TimeoutException("Waiting for campaign index " + index);
            yield return null;
        }
    }

    private IEnumerator Capture(string filename)
    {
        if (SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Null) yield break;
        yield return new WaitForEndOfFrame();
        var screenshot = ScreenCapture.CaptureScreenshotAsTexture();
        File.WriteAllBytes(Path.Combine(output, filename), screenshot.EncodeToPNG());
        Destroy(screenshot);
        yield return null;
    }

    private static FieldInfo Field(object target, string name) => target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
    private static T Get<T>(object target, string name) => (T)Field(target, name).GetValue(target);
    private static void Set(object target, string name, object value) => Field(target, name).SetValue(target, value);
    private static object Invoke(object target, string name, params object[] args) =>
        target.GetType().GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(target, args);

    private void Check(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
        report.checks.Add(message);
        Debug.Log("[CampaignSmoke] PASS: " + message);
    }

    private void CaptureError(string message, string trace, LogType type)
    {
        if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert)
            report.errors.Add(message + "\n" + trace);
    }

    private void Finish()
    {
        if (finished) return;
        finished = true;
        Application.logMessageReceived -= CaptureError;
        report.passed = report.errors.Count == 0;
        File.WriteAllText(Path.Combine(output, "results.json"), JsonUtility.ToJson(report, true));
        Debug.Log($"[CampaignSmoke] {(report.passed ? "PASS" : "FAIL")}: {report.checks.Count} checks, {report.errors.Count} errors. Results: {output}");
        Time.timeScale = 1;
        OnDestroy(); // Also restore the test storage key before a batch process exits.
        if (Application.isBatchMode) EditorApplication.Exit(report.passed ? 0 : 1);
        else EditorApplication.isPlaying = false;
    }
}

#endif
