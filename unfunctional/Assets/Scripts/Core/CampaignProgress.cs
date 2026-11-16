using System;
using UnityEngine;

/// <summary>Versioned stage-boundary checkpoint. Scene names remain stable if order changes.</summary>
public static class CampaignProgress
{
    [Serializable]
    public class Checkpoint
    {
        public int version = 1;
        public string scene;
        public bool completed;
        public bool calibrationKept;
        public float brightness = -1f, contrast = -1f, volume = -1f;
    }

#if UNITY_EDITOR
    // Editor playtests must never overwrite a player's standalone-build checkpoint.
    public static string StorageKey { get; set; } = "Unfunctional.Campaign.Editor.v1";
#else
    private const string StorageKey = "Unfunctional.Campaign.v1";
#endif

    public static bool TryRead(string[] scenes, out Checkpoint checkpoint)
    {
        checkpoint = null;
        string json = PlayerPrefs.GetString(StorageKey, "");
        if (string.IsNullOrEmpty(json)) return false;
        try
        {
            var data = JsonUtility.FromJson<Checkpoint>(json);
            if (data == null || data.version != 1 || string.IsNullOrEmpty(data.scene) ||
                Array.IndexOf(scenes, data.scene) <= 0 || !ValidSetting(data.brightness) ||
                !ValidSetting(data.contrast) || !ValidSetting(data.volume)) return false;
            checkpoint = data;
            return true;
        }
        catch (ArgumentException) { return false; }
    }

    private static bool ValidSetting(float value) => value == -1f || (value >= 0f && value <= 1f);

    public static void Save(string scene, bool completed = false)
    {
        var data = new Checkpoint
        {
            scene = scene,
            completed = completed,
            calibrationKept = Level2_SettingsPuzzle.SettingsPersisted,
            brightness = Level2_SettingsPuzzle.PersistentBrightness,
            contrast = Level2_SettingsPuzzle.PersistentContrast,
            volume = Level2_SettingsPuzzle.PersistentVolume
        };
        PlayerPrefs.SetString(StorageKey, JsonUtility.ToJson(data));
        PlayerPrefs.Save();
    }

    public static void Clear()
    {
        PlayerPrefs.DeleteKey(StorageKey);
        PlayerPrefs.Save();
    }
}
