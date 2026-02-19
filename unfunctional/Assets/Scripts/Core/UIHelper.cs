using UnityEngine;

/// <summary>
/// Shared UI utilities.
///
/// - Robust font loading across Unity versions (LegacyRuntime.ttf vs Arial.ttf).
/// - URP-safe canvas configuration (Screen Space - Overlay with correct sortingOrder).
/// </summary>
public static class UIHelper
{
    private static Font cachedFont;

    // =====================================================================
    // Font helpers
    // =====================================================================

    /// <summary>
    /// Returns a built-in font that works in the current Unity version.
    /// Tries "LegacyRuntime.ttf" first (Unity 6+), then "Arial.ttf" (older),
    /// and finally falls back to any font found in Resources.
    /// The result is cached after the first successful load.
    /// </summary>
    public static Font GetDefaultFont()
    {
        if (cachedFont != null) return cachedFont;

        cachedFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (cachedFont != null) return cachedFont;

        cachedFont = Resources.GetBuiltinResource<Font>("Arial.ttf");
        if (cachedFont != null) return cachedFont;

        Font[] allFonts = Resources.FindObjectsOfTypeAll<Font>();
        if (allFonts.Length > 0)
        {
            cachedFont = allFonts[0];
            Debug.LogWarning($"[UIHelper] Using fallback font: {cachedFont.name}");
            return cachedFont;
        }

        Debug.LogError("[UIHelper] Could not find any font! UI text will be invisible.");
        return null;
    }

    // =====================================================================
    // Canvas helpers
    // =====================================================================

    /// <summary>
    /// Configures a Canvas for rendering.
    /// Uses Screen Space - Overlay mode so canvases render after all cameras.
    /// </summary>
    /// <param name="canvas">The Canvas to configure.</param>
    /// <param name="sortingOrder">Sorting order (higher = on top).</param>
    public static void ConfigureCanvas(Canvas canvas, int sortingOrder = 0)
    {
        if (canvas == null) return;

        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = sortingOrder;
    }
}
