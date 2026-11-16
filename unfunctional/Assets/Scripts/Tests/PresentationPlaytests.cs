#if UNITY_EDITOR
using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Preserve the deliberately hostile rules while checking presentation and campaign wiring.</summary>
public partial class CampaignSmokeDriver
{
    private IEnumerator ExerciseAuthoredRules(LevelManager level)
    {
        if (level is Level6_QuicktimeEvents qte)
            Check(Mathf.Approximately(qte.timePerEvent, 1f) && Mathf.Approximately(qte.codeTimeLimit, 1.5f),
                "Authored QTE prompt and code time limits preserved");
        if (level is Level9_WalkingSimulator walking)
            Check(Mathf.Approximately(walking.staminaDrainRate, 40f) && Mathf.Approximately(walking.staminaRegenRate, 8f)
                && Mathf.Approximately(walking.staminaRegenDelay, 1.5f) && Mathf.Approximately(walking.sprintMultiplier, 1.8f),
                "Authored tedious stamina profile preserved");
        if (level is Level10_ItemDegradation workshop)
        {
            var tasks = Get<System.Collections.Generic.List<Level10_ItemDegradation.Task>>(workshop, "tasks");
            Check(tasks.Where(t => t.toolName != "Broom").Sum(t => t.requiredUses) == 140,
                "Original 140 counted workshop uses restored");
            var art = GameArtLibrary.Load();
            Check(art != null && art.shovel != null && art.tile != null && art.concrete != null, "Finale materials and shaped shovel retained");
            Invoke(workshop, "DispenseReplacementTool", "Shovel");
            var held = Get<GameObject>(workshop, "currentToolVisual");
            Check(held != null && held.transform.Find("StampedSteelBlade") != null && held.transform.Find("DGrip") != null, "Shaped shovel retained in inventory");
            yield return Capture("shovel-art.png");
            Invoke(workshop, "DropCurrentItem");
        }
        if (level is Level11_BadRNG rng)
        {
            Check(Mathf.Approximately(rng.doorSpawnChance, 0.75f), "Authored RNG probability preserved");
            var pause = FindAnyObjectByType<GamePauseMenu>();
            Check(pause.restartButton != null && !pause.restartButton.gameObject.activeSelf, "RNG level still forbids level restart");
        }
    }

    private IEnumerator ExerciseOriginalRNG()
    {
        var gm = GameManager.Instance;
        gm.LoadLevel(10);
        yield return WaitForLevel(10);
        var rng = FindAnyObjectByType<Level11_BadRNG>();
        yield return new WaitForSecondsRealtime(0.2f);
        rng.doorSpawnChance = -1f; // Deterministically exercise the original failure branch.
        rng.tauntDisplayTime = 0.1f;
        Level2_SettingsPuzzle.RestoreCalibration(true, 0.5f, 0.5f, 0.4f);
        Invoke(rng, "OnChooseOptionA");
        yield return WaitForLevel(0);
        Check(!gm.CanContinueCampaign && !Level2_SettingsPuzzle.SettingsPersisted, "RNG failure clears the campaign and calibration without a forgiving alternative");
    }

    private IEnumerator ExerciseCredits()
    {
        var ending = FindAnyObjectByType<CampaignEnding>();
        var endingCanvas = ending.transform.Find("CampaignComplete").gameObject;
        endingCanvas.transform.Find("Credits").GetComponent<Button>().onClick.Invoke();
        var credits = CampaignCredits.Instance;
        Check(credits != null && !endingCanvas.activeSelf, "Credits disable ending buttons behind the overlay");
        Check(UnityEngine.EventSystems.EventSystem.current.currentSelectedGameObject.name == "CloseCredits", "Credits move keyboard focus to their own controls");
        yield return Capture("credits.png");
        var canvas = credits.transform.Find("CreditsCanvas");
        canvas.Find("NextCredit").GetComponent<Button>().onClick.Invoke();
        Check(canvas.Find("CreditPage").GetComponent<Text>().text == "2 / 4", "Credit pages navigate");
        canvas.Find("ThirdPartyNotices").GetComponent<Button>().onClick.Invoke();
        Check(canvas.Find("CreditsBody").GetComponent<Text>().text.Contains("Caveat"), "Bundled attribution notices load");
        yield return Capture("license-notices.png");
        credits.Close();
        yield return null;
        Check(endingCanvas.activeSelf && CampaignCredits.Instance == null, "Closing credits restores the ending");
    }

    private void CheckFinaleTargets()
    {
        CheckTarget("ReadingPlaque", new Vector3(-4, 1.65f, -1f), new Vector3(-4, 1.65f, 0.85f));
        CheckTarget("RedButton", new Vector3(4, 2.2f, 0.5f), new Vector3(4, 1.27f, 2));
        CheckTarget("AchievementExit", new Vector3(0, 1.6f, 6), new Vector3(0, 1.6f, 8.45f));
    }
    private void CheckTarget(string name, Vector3 origin, Vector3 target)
    {
        Physics.SyncTransforms();
        bool hitTarget = Physics.Raycast(origin, (target - origin).normalized, out var hit, 3.5f, ~0, QueryTriggerInteraction.Ignore)
            && (hit.collider.name == name || hit.transform.IsChildOf(GameObject.Find(name).transform));
        Check(hitTarget, "Finale interaction is reachable by raycast: " + name);
    }

}

#endif
