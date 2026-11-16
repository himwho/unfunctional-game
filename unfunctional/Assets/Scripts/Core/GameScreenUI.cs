using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

/// <summary>Shared UI factory for the finale and campaign ending.</summary>
public static class GameScreenUI
{
    public static Canvas Canvas(Transform parent, string name, int order)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        go.transform.SetParent(parent, false);
        var canvas = go.GetComponent<Canvas>();
        UIHelper.ConfigureCanvas(canvas, order);
        var scaler = go.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        return canvas;
    }

    public static void Place(RectTransform rect, Vector2 min, Vector2 max)
    {
        rect.anchorMin = min;
        rect.anchorMax = max;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
    }

    public static Text Text(Transform parent, string name, string content, int size,
        Vector2 min, Vector2 max, TextAnchor alignment = TextAnchor.MiddleCenter)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Text));
        go.transform.SetParent(parent, false);
        var text = go.GetComponent<Text>();
        text.font = UIHelper.GetDefaultFont();
        text.text = content;
        text.fontSize = size;
        text.color = Color.white;
        text.alignment = alignment;
        text.raycastTarget = false;
        Place(text.rectTransform, min, max);
        return text;
    }

    public static Image Panel(Transform parent, string name, Color color, Vector2 min, Vector2 max)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        var image = go.GetComponent<Image>();
        image.color = color;
        Place(image.rectTransform, min, max);
        return image;
    }

    public static Button Button(Transform parent, string name, string label, Vector2 min, Vector2 max, UnityAction action)
    {
        var panel = Panel(parent, name, new Color(0.18f, 0.22f, 0.25f), min, max);
        var button = panel.gameObject.AddComponent<Button>();
        button.targetGraphic = panel;
        button.onClick.AddListener(action);
        Text(panel.transform, "Label", label, 26, Vector2.zero, Vector2.one);
        return button;
    }
}
