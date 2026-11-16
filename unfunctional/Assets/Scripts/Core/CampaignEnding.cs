using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>Visible, navigable terminal state for the campaign.</summary>
public class CampaignEnding : MonoBehaviour
{
    public void Show(GameManager manager)
    {
        var canvas = GameScreenUI.Canvas(transform, "CampaignComplete", 950);
        GameScreenUI.Panel(canvas.transform, "Background", new Color(0.035f, 0.045f, 0.05f), Vector2.zero, Vector2.one);
        GameScreenUI.Text(canvas.transform, "Title", "UNFUNCTIONAL", 64,
            new Vector2(0.1f, 0.7f), new Vector2(0.9f, 0.85f));
        GameScreenUI.Text(canvas.transform, "Ending",
            "Achievement unlocked: finished the game.\n\nYou survived every terrible idea.\nThank you for playing.", 30,
            new Vector2(0.15f, 0.4f), new Vector2(0.85f, 0.68f));
        var replay = GameScreenUI.Button(canvas.transform, "BackToMenu", "BACK TO THE BEGINNING",
            new Vector2(0.32f, 0.30f), new Vector2(0.68f, 0.37f), manager.StartNewCampaign);
        GameScreenUI.Button(canvas.transform, "Credits", "CREDITS & THANKS",
            new Vector2(0.32f, 0.20f), new Vector2(0.68f, 0.27f), () =>
            {
                if (GetComponentInChildren<CampaignCredits>() != null) return;
                var credits = new GameObject("CampaignCredits");
                credits.transform.SetParent(transform, false);
                credits.AddComponent<CampaignCredits>().Show(credits.transform, canvas.gameObject);
            });
        GameScreenUI.Button(canvas.transform, "Quit", "QUIT WHILE YOU'RE AHEAD",
            new Vector2(0.32f, 0.10f), new Vector2(0.68f, 0.17f), Quit);
        if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(replay.gameObject);
    }

    private static void Quit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
