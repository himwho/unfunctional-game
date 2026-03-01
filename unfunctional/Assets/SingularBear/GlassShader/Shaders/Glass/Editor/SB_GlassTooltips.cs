// ============================================================
// SingularBear Glass Shader V1 - Complete Tooltips Database
// Didactic tooltips for ALL shader properties
// Copyright (c) SingularBear - All Rights Reserved
// ============================================================
using System.Collections.Generic;

namespace SingularBear.Shaders
{
    /// <summary>
    /// Centralized tooltip database for SB Glass Shader.
    /// Each tooltip explains WHAT the parameter does and HOW to use it effectively.
    /// </summary>
    public static class SB_GlassTooltips
    {
        private static readonly Dictionary<string, string> tooltips = new Dictionary<string, string>()
        {
            // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
            // QUALITY
            // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
            {"_SB_Quality", 
                "Global performance preset that controls shader complexity.\n\n" +
                "â€¢ Low: Fastest rendering, disables expensive features (blur, caustics)\n" +
                "â€¢ Medium: Balanced quality and performance\n" +
                "â€¢ High: Maximum quality, all features enabled\n\n" +
                "Tip: Start with High, then reduce if needed for mobile."},
            
            // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
            // MAIN SURFACE
            // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
            {"_Color", 
                "Base glass tint color.\n\n" +
                "â€¢ RGB: The color of the glass itself\n" +
                "â€¢ Alpha: Base opacity (combines with other opacity settings)\n\n" +
                "Tip: Use subtle tints (near white) for realistic glass.\n" +
                "Use saturated colors for stained/stylized glass."},
            
            {"_MainTex", 
                "Optional albedo texture for the glass surface.\n\n" +
                "â€¢ RGB: Surface color/pattern\n" +
                "â€¢ Alpha: Can control opacity variation\n\n" +
                "Use for: Stained glass, frosted patterns, dirt overlays.\n" +
                "Leave empty for plain colored glass."},
            
            {"_MainTint", 
                "Controls how much the albedo texture affects the final color.\n\n" +
                "â€¢ 0 = Pure glass color (texture ignored)\n" +
                "â€¢ 1 = Full texture influence\n\n" +
                "Useful to fade texture in/out without changing other settings."},
            
            {"_Metallic", 
                "Metallic response of the glass surface.\n\n" +
                "â€¢ 0 = Dielectric (normal glass behavior)\n" +
                "â€¢ 1 = Metallic (mirror-like, colored reflections)\n\n" +
                "Real glass is ALWAYS 0.\n" +
                "Use higher values only for stylized chrome/mirror effects."},
            
            {"_Smoothness", 
                "Surface smoothness - controls reflection sharpness.\n\n" +
                "â€¢ 0 = Rough surface (frosted/matte glass)\n" +
                "â€¢ 0.5 = Semi-rough (dirty window)\n" +
                "â€¢ 1 = Perfectly smooth (clean mirror)\n\n" +
                "Most clean glass: 0.9-1.0\n" +
                "Frosted glass: 0.3-0.6"},
            
            {"_Saturation", 
                "Adjusts color saturation of the glass.\n\n" +
                "â€¢ 0 = Grayscale (no color)\n" +
                "â€¢ 1 = Normal color\n" +
                "â€¢ 2 = Boosted/vivid colors\n\n" +
                "Useful for stylized looks or desaturated environments."},
            
            {"_Brightness", 
                "Overall brightness multiplier for the base color.\n\n" +
                "â€¢ <1 = Darker glass\n" +
                "â€¢ 1 = Normal\n" +
                "â€¢ >1 = Brighter glass\n\n" +
                "Does not affect reflections or refraction."},
            
            {"_MetallicGlossMap", 
                "Packed texture for surface variation:\n\n" +
                "â€¢ Red channel = Metallic\n" +
                "â€¢ Alpha channel = Smoothness\n\n" +
                "Allows different areas to have different properties.\n" +
                "Example: Fingerprint areas = less smooth."},
            
            {"_UseMetallicMap", 
                "Enable the Metallic/Smoothness map.\n\n" +
                "When ON: Uses texture for surface variation.\n" +
                "When OFF: Uses global Metallic/Smoothness sliders."},
            
            // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
            // NORMAL MAPPING
            // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
            {"_BumpMap", 
                "Normal map texture for surface detail.\n\n" +
                "Adds visual depth without extra geometry:\n" +
                "â€¢ Scratches, bumps, glass patterns\n" +
                "â€¢ Affects both lighting AND refraction distortion\n\n" +
                "Use tangent-space normal maps (blue-ish color)."},
            
            {"_BumpScale", 
                "Normal map intensity.\n\n" +
                "â€¢ 0 = Flat (normal map disabled)\n" +
                "â€¢ 1 = Normal strength\n" +
                "â€¢ >1 = Exaggerated bumps\n\n" +
                "Higher values = stronger refraction distortion."},
            
            {"_UseNormalMap", 
                "Enable/disable the normal map.\n\n" +
                "Turn OFF to save performance if you don't need surface detail."},
            
            // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
            // DETAIL MAPS
            // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
            {"_UseDetailAlbedo", 
                "Enable a secondary detail albedo layer.\n\n" +
                "Adds fine color variation that tiles independently.\n" +
                "Great for: dust, grime, subtle patterns."},
            
            {"_DetailAlbedoMap", 
                "Secondary albedo texture for micro-detail.\n\n" +
                "â€¢ Tiles independently from main texture\n" +
                "â€¢ Blends over the base color\n\n" +
                "Use for fine surface variation like dust or scratches."},
            
            {"_DetailColor", 
                "Tint color applied to the detail albedo.\n\n" +
                "Multiplies with the detail texture color."},
            
            {"_DetailTiling", 
                "Tiling multiplier for detail ALBEDO only.\n\n" +
                "â€¢ Higher = More repetition, finer detail\n" +
                "â€¢ Lower = Larger pattern\n\n" +
                "Typical values: 2-10 for subtle detail."},
            
            {"_DetailAlbedoIntensity", 
                "Blend strength of the detail albedo.\n\n" +
                "â€¢ 0 = No detail visible\n" +
                "â€¢ 0.5 = Subtle blend\n" +
                "â€¢ 1 = Full detail overlay\n\n" +
                "Keep low (0.1-0.3) for subtle effects."},
            
            {"_UseDetailNormal", 
                "Enable a secondary detail normal map.\n\n" +
                "Adds micro-surface detail that tiles independently.\n" +
                "Perfect for: scratches, imperfections, glass textures."},
            
            {"_DetailNormalMap", 
                "Secondary normal map for micro-surface detail.\n\n" +
                "â€¢ Combines with main normal map\n" +
                "â€¢ Has its own independent tiling\n" +
                "â€¢ Can use triplanar mapping separately\n\n" +
                "Great for small-scale scratches or patterns."},
            
            {"_DetailNormalScale", 
                "Detail normal map intensity.\n\n" +
                "â€¢ 0 = No detail normals\n" +
                "â€¢ 1 = Full strength\n" +
                "â€¢ >1 = Exaggerated\n\n" +
                "Adds to the main normal map effect."},
            
            {"_DetailNormalTiling", 
                "Tiling for detail NORMAL (independent from albedo).\n\n" +
                "â€¢ Higher = More repetition, finer scratches\n" +
                "â€¢ Lower = Larger patterns\n\n" +
                "Can be different from Detail Albedo tiling!"},
            
            {"_UseDetailNormalTriplanar", 
                "Enable triplanar mapping for detail normal ONLY.\n\n" +
                "â€¢ Projects detail normal from 3 world axes\n" +
                "â€¢ Eliminates UV stretching on curved surfaces\n" +
                "â€¢ Works independently from global triplanar\n\n" +
                "Perfect for seamless scratches on bottles, spheres, etc."},
            
            {"_DetailNormalTriplanarScale", 
                "World-space scale for detail normal triplanar.\n\n" +
                "â€¢ Smaller values = Larger patterns\n" +
                "â€¢ Higher values = Finer detail\n\n" +
                "Typical values: 0.5-5.0"},
            
            {"_DetailNormalTriplanarSharpness", 
                "Controls triplanar blend sharpness.\n\n" +
                "â€¢ 1 = Soft blend (may show stretching)\n" +
                "â€¢ 4-6 = Good balance\n" +
                "â€¢ 10 = Sharp transitions\n\n" +
                "Higher = cleaner but may show seams."},
            
            // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
            // TRANSPARENCY
            // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
            {"_Opacity", 
                "Base opacity of the glass.\n\n" +
                "â€¢ 0 = Fully transparent\n" +
                "â€¢ 0.5 = Semi-transparent (typical glass)\n" +
                "â€¢ 1 = Fully opaque\n\n" +
                "Combines with Fresnel and other opacity effects."},
            
            {"_UseAlphaClip", 
                "Enable alpha cutout mode.\n\n" +
                "Pixels below threshold become fully transparent.\n" +
                "Useful for: holes, cutouts, masked patterns.\n\n" +
                "More performant than smooth transparency for hard edges."},
            
            {"_AlphaClip", 
                "Alpha clip threshold (when Alpha Clip is enabled).\n\n" +
                "â€¢ Pixels with alpha < threshold = invisible\n" +
                "â€¢ Pixels with alpha >= threshold = visible\n\n" +
                "Use with a mask texture for cutout effects."},
            
            {"_UseFalloffOpacity", 
                "Enable view-angle opacity falloff.\n\n" +
                "Makes glass more/less transparent based on view angle.\n" +
                "Similar to Fresnel but affects opacity only."},
            
            {"_FalloffOpacityIntensity", 
                "Strength of the opacity falloff effect.\n\n" +
                "â€¢ 0 = No effect\n" +
                "â€¢ 1 = Full effect\n\n" +
                "Higher = more dramatic opacity change at edges."},
            
            {"_FalloffOpacityPower", 
                "Falloff curve sharpness.\n\n" +
                "â€¢ Low (1-2) = Gradual change\n" +
                "â€¢ High (5-10) = Sharp edge effect\n\n" +
                "Controls how quickly opacity changes with angle."},
            
            {"_FalloffOpacityInvert", 
                "Invert the opacity falloff direction.\n\n" +
                "â€¢ OFF = Edges more opaque (normal)\n" +
                "â€¢ ON = Center more opaque (inverted)\n\n" +
                "Inverted can create interesting portal effects."},
            
            // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
            // SPECULAR
            // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
            {"_UseSpecular", 
                "Enable custom specular highlights.\n\n" +
                "Adds bright spots where light reflects directly.\n" +
                "Essential for making glass look shiny and reactive to light."},
            
            {"_SpecularColor", 
                "Color of the specular highlight.\n\n" +
                "â€¢ White = Natural light color\n" +
                "â€¢ Colored = Stylized highlights\n\n" +
                "Real glass typically uses white."},
            
            {"_SpecularIntensity", 
                "Brightness of specular highlights.\n\n" +
                "â€¢ 0 = No highlights\n" +
                "â€¢ 1 = Normal brightness\n" +
                "â€¢ >1 = Intense, eye-catching highlights\n\n" +
                "Higher values = shinier appearance."},
            
            {"_SpecularSize", 
                "Size of the specular highlight spot.\n\n" +
                "â€¢ Small (0.1) = Tiny, sharp highlight\n" +
                "â€¢ Large (0.8) = Broad, soft highlight\n\n" +
                "Smoother surfaces = smaller highlights."},
            
            {"_SpecularSmoothness", 
                "Smoothness of the specular falloff.\n\n" +
                "â€¢ 0 = Rough, diffuse highlight\n" +
                "â€¢ 1 = Sharp, focused highlight\n\n" +
                "Works with Specular Size for final look."},
            
            {"_SpecularHardness", 
                "Edge hardness of the specular highlight.\n\n" +
                "â€¢ 0 = Soft, gradient edges\n" +
                "â€¢ 1 = Hard, sharp edges (toon-like)\n\n" +
                "Use higher values for stylized looks."},
            
            {"_SpecularToon", 
                "Enable toon/cel-shaded specular.\n\n" +
                "Creates stepped, cartoon-style highlights.\n" +
                "Great for stylized or anime-inspired visuals."},
            
            {"_SpecularSteps", 
                "Number of steps in toon specular (when Toon Mode is ON).\n\n" +
                "â€¢ 2 = Simple on/off highlight\n" +
                "â€¢ 5-10 = More gradient steps\n\n" +
                "More steps = smoother but less stylized."},
            
            {"_SpecularThreshold", 
                "Threshold for toon specular steps.\n\n" +
                "Controls where the steps occur.\n" +
                "Adjust to fine-tune the toon look."},
            
            {"_SpecularFresnel", 
                "Apply Fresnel to specular intensity.\n\n" +
                "â€¢ ON = Stronger highlights at glancing angles\n" +
                "â€¢ OFF = Uniform highlight strength\n\n" +
                "More physically accurate when ON."},
            
            {"_SpecularAnisotropy", 
                "Anisotropic specular stretching.\n\n" +
                "â€¢ -1 = Stretched vertically\n" +
                "â€¢ 0 = Circular (isotropic)\n" +
                "â€¢ +1 = Stretched horizontally\n\n" +
                "Simulates brushed or directional surfaces."},
            
            {"_DiffuseIntensity", 
                "Amount of diffuse (matte) lighting on the glass.\n\n" +
                "â€¢ 0 = Pure specular (very reflective)\n" +
                "â€¢ 1 = More matte appearance\n\n" +
                "Lower values = more glass-like."},
            
            // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
            // REFRACTION
            // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
            {"_UseRefraction", 
                "Enable see-through refraction.\n\n" +
                "â€¢ ON = See through the glass with distortion\n" +
                "â€¢ OFF = Solid color (opaque-looking)\n\n" +
                "This is what makes glass look like glass!"},
            
            {"_Distortion", 
                "How much the normal map distorts the view through the glass.\n\n" +
                "â€¢ 0 = No distortion (flat glass)\n" +
                "â€¢ 0.1 = Subtle waviness\n" +
                "â€¢ 0.5 = Very distorted view\n\n" +
                "Requires a normal map to have effect."},
            
            {"_FlipRefraction", 
                "Flip the refraction direction.\n\n" +
                "Useful if refraction appears inverted on your mesh.\n" +
                "Try toggling if things look 'wrong'."},
            
            {"_UseIOR", 
                "Enable physically-based Index of Refraction.\n\n" +
                "Creates realistic light bending based on:\n" +
                "â€¢ View angle\n" +
                "â€¢ Material IOR value\n\n" +
                "More accurate but slightly more expensive."},
            
            {"_IndexOfRefraction", 
                "IOR blend strength.\n\n" +
                "â€¢ 0 = No physical IOR effect\n" +
                "â€¢ 1 = Full physical refraction\n\n" +
                "Controls how much the IOR affects the image."},
            
            {"_IOROriginPreset", 
                "Preset for the origin medium (what's outside the glass).\n\n" +
                "â€¢ Air (IOR 1.0) - Normal environment\n" +
                "â€¢ Water (IOR 1.33) - Underwater glass\n" +
                "â€¢ Glass (IOR 1.5) - Glass touching glass"},
            
            {"_IOROrigin", 
                "Custom Index of Refraction value.\n\n" +
                "Real-world values:\n" +
                "â€¢ Air = 1.0\n" +
                "â€¢ Water = 1.33\n" +
                "â€¢ Glass = 1.5\n" +
                "â€¢ Diamond = 2.4\n\n" +
                "Higher = more bending/distortion."},
            
            {"_UseChromatic", 
                "Enable chromatic aberration (color fringing).\n\n" +
                "Simulates how glass splits white light into colors.\n" +
                "Creates rainbow edges, especially at steep angles."},
            
            {"_ChromaticAberration", 
                "Strength of chromatic dispersion.\n\n" +
                "â€¢ 0 = No color separation\n" +
                "â€¢ 1-2 = Subtle, realistic\n" +
                "â€¢ 5-10 = Dramatic rainbow effect\n\n" +
                "Higher = more visible RGB separation."},
            
            // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
            // BLUR
            // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
            {"_UseBlur", 
                "Enable refraction blur for frosted glass.\n\n" +
                "Blurs what's seen through the glass.\n" +
                "Works with OR without Refraction enabled.\n\n" +
                "Note: Has performance cost."},
            
            {"_BlurStrength", 
                "Blur intensity.\n\n" +
                "â€¢ 0 = Crystal clear\n" +
                "â€¢ 0.5 = Moderately blurred\n" +
                "â€¢ 1 = Maximum blur\n\n" +
                "Use with BlurRadius for frosted glass effect."},
            
            {"_BlurRadius", 
                "Blur sample radius in screen space.\n\n" +
                "â€¢ Small = Tight, subtle blur\n" +
                "â€¢ Large = Wide, soft blur\n\n" +
                "Larger radius + more samples = softer result."},
            
            {"_BlurQuality", 
                "Number of blur samples.\n\n" +
                "â€¢ 4 = Fast, grainy\n" +
                "â€¢ 8 = Good for mobile\n" +
                "â€¢ 16 = High quality\n" +
                "â€¢ 32 = Very smooth (expensive)\n\n" +
                "More samples = smoother but slower."},
            
            // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
            // REFLECTION
            // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
            {"_UseReflection", 
                "Enable environment reflections.\n\n" +
                "Uses scene reflection probes by default.\n" +
                "Can use a custom cubemap instead."},
            
            {"_ReflectionColor", 
                "Tint color for reflections.\n\n" +
                "â€¢ White = Natural reflection color\n" +
                "â€¢ Colored = Tinted reflections\n\n" +
                "Useful for stylized or colored glass."},
            
            {"_ReflectionIntensity", 
                "Reflection strength.\n\n" +
                "â€¢ 0 = No reflections\n" +
                "â€¢ 1 = Full reflections\n" +
                "â€¢ >1 = Boosted (brighter)\n\n" +
                "Real glass: 0.3-0.5 at normal angles."},
            
            {"_ReflectionBlur", 
                "Blur/roughness of reflections.\n\n" +
                "â€¢ 0 = Sharp mirror reflection\n" +
                "â€¢ 1 = Fully blurred/diffuse\n\n" +
                "Simulates surface roughness without affecting refraction."},
            
            {"_UseCubemap", 
                "Use a custom cubemap instead of scene probes.\n\n" +
                "Useful for:\n" +
                "â€¢ Consistent reflections regardless of scene\n" +
                "â€¢ Stylized/artistic reflections\n" +
                "â€¢ When no reflection probes exist"},
            
            {"_ReflectionCube", 
                "Custom reflection cubemap texture.\n\n" +
                "Only used when 'Use Cubemap' is ON.\n" +
                "Overrides scene reflection probes."},
            
            // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
            // IRIDESCENCE
            // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
            {"_UseIridescence", 
                "Enable iridescent color shifting.\n\n" +
                "Creates soap bubble or oil-slick rainbow effects.\n" +
                "Colors change based on view angle."},
            
            {"_IridescenceColor", 
                "Base tint for the iridescence effect.\n\n" +
                "Multiplies with the rainbow gradient.\n" +
                "White = full rainbow, Colored = tinted rainbow."},
            
            {"_IridescenceStrength", 
                "Intensity of the iridescent colors.\n\n" +
                "â€¢ 0 = No iridescence\n" +
                "â€¢ 1 = Normal strength\n" +
                "â€¢ 2 = Very strong rainbow\n\n" +
                "Start low (0.3-0.5) for subtle effects."},
            
            {"_IridescenceScale", 
                "Frequency of color bands.\n\n" +
                "â€¢ Low (1-2) = Wide color bands\n" +
                "â€¢ High (5-10) = Tight, narrow bands\n\n" +
                "Higher = more complex color patterns."},
            
            {"_IridescenceShift", 
                "Hue rotation offset (0-1).\n\n" +
                "Shifts which colors appear at which angles.\n" +
                "Use to pick starting colors in the rainbow."},
            
            {"_IridescenceSpeed", 
                "Animation speed for shifting colors.\n\n" +
                "â€¢ 0 = Static iridescence\n" +
                "â€¢ >0 = Animated rainbow shifting\n\n" +
                "Creates dynamic, living effect."},
            
            // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
            // FRESNEL
            // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
            {"_UseFresnel", 
                "Enable Fresnel effect.\n\n" +
                "Makes edges more visible/reflective than center.\n" +
                "ESSENTIAL for realistic glass look!\n\n" +
                "Real glass always has Fresnel."},
            
            {"_FresnelColor", 
                "Fresnel tint color.\n\n" +
                "â€¢ White = Natural edge brightening\n" +
                "â€¢ Colored = Stylized edge glow\n\n" +
                "Blue tints are common for glass."},
            
            {"_FresnelPower", 
                "Fresnel curve sharpness.\n\n" +
                "â€¢ Low (1-2) = Wide, soft rim\n" +
                "â€¢ Medium (3-5) = Realistic glass\n" +
                "â€¢ High (8-10) = Sharp edge line\n\n" +
                "Real glass is around 5.0."},
            
            {"_FresnelIntensity", 
                "Overall Fresnel effect strength.\n\n" +
                "â€¢ 0 = No Fresnel\n" +
                "â€¢ 1 = Normal\n" +
                "â€¢ 2+ = Exaggerated edges\n\n" +
                "Keep around 1.0 for realism."},
            
            {"_FresnelMin", 
                "Minimum Fresnel value (at center/front view).\n\n" +
                "â€¢ 0 = Fully transparent center\n" +
                "â€¢ >0 = Some visibility even at center\n\n" +
                "Increase for more uniform appearance."},
            
            {"_FresnelMax", 
                "Maximum Fresnel value (at edges/grazing angles).\n\n" +
                "â€¢ 1 = Full effect at edges\n" +
                "â€¢ <1 = Reduced edge effect\n\n" +
                "Decrease for subtler edge brightening."},
            
            {"_FresnelInvert", 
                "Invert the Fresnel direction.\n\n" +
                "â€¢ OFF = Edges bright (normal)\n" +
                "â€¢ ON = Center bright (inverted)\n\n" +
                "Inverted creates unusual effects."},
            
            {"_FresnelAffectAlpha", 
                "Let Fresnel modify transparency.\n\n" +
                "â€¢ ON = Edges become more opaque\n" +
                "â€¢ OFF = Alpha stays uniform\n\n" +
                "ON is more physically correct."},
            
            {"_FresnelAffectReflection", 
                "Let Fresnel boost reflections at edges.\n\n" +
                "â€¢ ON = More reflection at grazing angles\n" +
                "â€¢ OFF = Uniform reflection strength\n\n" +
                "ON is physically correct for glass."},
            
            // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
            // SURFACE NOISE
            // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
            {"_UseSurfaceNoise", 
                "Enable procedural surface noise.\n\n" +
                "Adds animated noise to the surface.\n" +
                "Creates organic, living glass appearance."},
            
            {"_SurfaceNoiseScale", 
                "Scale of the noise pattern.\n\n" +
                "â€¢ Low (10-50) = Large, blobby noise\n" +
                "â€¢ High (100-500) = Fine, grainy noise\n\n" +
                "Adjust to match your glass size."},
            
            {"_SurfaceNoiseStrength", 
                "Intensity of the noise effect on color.\n\n" +
                "â€¢ 0 = No visible noise\n" +
                "â€¢ 0.1-0.5 = Subtle variation\n" +
                "â€¢ >1 = Very noisy\n\n" +
                "Keep low for subtle organic feel."},
            
            {"_SurfaceNoiseDistortion", 
                "How much noise distorts refraction.\n\n" +
                "â€¢ 0 = No refraction distortion\n" +
                "â€¢ >0 = Wavy, distorted view through glass\n\n" +
                "Creates heat haze-like effect."},
            
            {"_SurfaceNoiseSpeed", 
                "Animation speed of the noise.\n\n" +
                "â€¢ 0 = Static noise\n" +
                "â€¢ 0.1-0.5 = Slow, subtle movement\n" +
                "â€¢ >1 = Fast, obvious animation\n\n" +
                "Slow speeds look more natural."},
            
            // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
            // TINT TEXTURE
            // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
            {"_UseTintTexture", 
                "Enable a tint texture overlay.\n\n" +
                "Adds color variation across the surface.\n" +
                "Great for: stained glass, gradients, patterns."},
            
            {"_TintTexture", 
                "Texture used to tint the glass.\n\n" +
                "RGB = Color tint applied to glass.\n" +
                "Can use gradients or patterns."},
            
            {"_TintTextureColor", 
                "Color multiplier for the tint texture.\n\n" +
                "Allows adjusting the tint without changing texture."},
            
            {"_TintTextureStrength", 
                "Blend strength of the tint.\n\n" +
                "â€¢ 0 = No tint visible\n" +
                "â€¢ 1 = Full tint effect\n\n" +
                "Use to fade the tint in/out."},
            
            {"_TintTextureBlend", 
                "Alpha blending mode for tint.\n\n" +
                "â€¢ 0 = Multiply blend\n" +
                "â€¢ 1 = Alpha/overlay blend\n\n" +
                "Different looks for different use cases."},
            
            {"_TintDistortionAmount", 
                "How much the tint affects refraction.\n\n" +
                "â€¢ 0 = Tint is visual only\n" +
                "â€¢ >0 = Tint also distorts view\n\n" +
                "Creates stained-glass-like refraction."},
            
            // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
            // EDGE DARKENING
            // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
            {"_UseEdgeDarkening", 
                "Enable edge darkening effect.\n\n" +
                "Darkens the glass near mesh edges.\n" +
                "Adds depth and definition to thin glass."},
            
            {"_EdgeDarkeningStrength", 
                "Intensity of edge darkening.\n\n" +
                "â€¢ 0 = No darkening\n" +
                "â€¢ 1 = Full darkening at edges\n\n" +
                "Adjust based on your lighting."},
            
            {"_EdgeDarkeningDistance", 
                "How far the darkening extends from edges.\n\n" +
                "â€¢ Small = Thin dark line at edges\n" +
                "â€¢ Large = Wide gradient from edges\n\n" +
                "Adjust based on mesh size."},
            
            {"_EdgeDarkeningPower", 
                "Falloff curve of the darkening.\n\n" +
                "â€¢ Low (1-2) = Soft gradient\n" +
                "â€¢ High (5-10) = Sharp edge line\n\n" +
                "Higher = more defined edges."},
            
            // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
            // INNER GLOW
            // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
            {"_UseInnerGlow", 
                "Enable inner glow effect.\n\n" +
                "Creates a soft glow from inside the glass.\n" +
                "Great for: magic effects, energy, sci-fi."},
            
            {"_InnerGlowColor", 
                "Color of the inner glow.\n\n" +
                "Use HDR colors for bloom-compatible glow.\n" +
                "Alpha controls base opacity."},
            
            {"_InnerGlowStrength", 
                "Intensity of the glow.\n\n" +
                "â€¢ 0 = No glow\n" +
                "â€¢ 1 = Normal glow\n" +
                "â€¢ >1 = Bright, intense glow\n\n" +
                "Use with HDR for bloom effects."},
            
            {"_InnerGlowPower", 
                "Falloff curve of the glow.\n\n" +
                "â€¢ Low (1-2) = Soft, spread glow\n" +
                "â€¢ High (5-10) = Concentrated center glow\n\n" +
                "Controls glow distribution."},
            
            {"_InnerGlowFalloff", 
                "How quickly glow fades toward edges.\n\n" +
                "â€¢ 0 = Uniform glow\n" +
                "â€¢ 1 = Strong edge falloff\n\n" +
                "Higher = more center-focused glow."},
            
            // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
            // THICKNESS
            // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
            {"_UseThickness", 
                "Enable thickness-based effects.\n\n" +
                "Uses a texture to simulate varying glass thickness.\n" +
                "Thicker areas = more color/distortion."},
            
            {"_ThicknessMap", 
                "Grayscale texture representing glass thickness.\n\n" +
                "â€¢ Black = Thin glass\n" +
                "â€¢ White = Thick glass\n\n" +
                "Affects color absorption and distortion."},
            
            {"_ThicknessMin", 
                "Minimum thickness value.\n\n" +
                "Remaps black in texture to this thickness.\n" +
                "Use for uniform minimum thickness."},
            
            {"_ThicknessMax", 
                "Maximum thickness value.\n\n" +
                "Remaps white in texture to this thickness.\n" +
                "Controls the range of thickness variation."},
            
            {"_ThicknessAffectsColor", 
                "How much thickness affects color absorption.\n\n" +
                "â€¢ 0 = Uniform color\n" +
                "â€¢ 1 = Thick areas much darker/more saturated\n\n" +
                "Creates realistic thick glass appearance."},
            
            {"_ThicknessAffectsDistortion", 
                "How much thickness affects refraction.\n\n" +
                "â€¢ 0 = Uniform distortion\n" +
                "â€¢ 1 = Thick areas distort more\n\n" +
                "Creates lens-like effects in thick areas."},
            
            // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
            // DEPTH FADE
            // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
            {"_UseDepthFade", 
                "Enable depth-based fading.\n\n" +
                "Fades glass where it intersects other geometry.\n" +
                "Prevents hard edges at intersections."},
            
            {"_DepthFadeDistance", 
                "Distance over which the fade occurs.\n\n" +
                "â€¢ Small = Sharp fade near intersection\n" +
                "â€¢ Large = Gradual fade over distance\n\n" +
                "Adjust based on your scene scale."},
            
            {"_DepthFadeColor", 
                "Color to fade toward at intersections.\n\n" +
                "Usually matches the environment or is transparent.\n" +
                "Alpha controls final opacity at intersection."},
            
            // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
            // RAIN
            // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
            {"_UseRain", 
                "Enable rain/water droplet effects.\n\n" +
                "Adds animated water droplets to the surface.\n" +
                "Uses a normal map for droplet shapes."},
            
            {"_RainTexture", 
                "Normal map texture for rain droplets.\n\n" +
                "Should contain multiple droplet shapes.\n" +
                "Animated by scrolling UVs."},
            
            {"_RainIntensity", 
                "Overall visibility of rain droplets.\n\n" +
                "â€¢ 0 = No visible rain\n" +
                "â€¢ 1 = Full rain effect\n\n" +
                "Blend between dry and wet."},
            
            {"_RainTiling", 
                "Tiling of the rain texture.\n\n" +
                "â€¢ XY = Tiling amounts\n\n" +
                "Higher = more, smaller droplets."},
            
            {"_RainOffset", 
                "UV offset for the rain texture.\n\n" +
                "Use to adjust droplet positions."},
            
            {"_RainRotation", 
                "Rotation of the rain pattern (degrees).\n\n" +
                "Aligns droplets with gravity direction.\n" +
                "0 = Droplets flow down in UV space."},
            
            {"_RainSpeed", 
                "Animation speed of rain droplets.\n\n" +
                "â€¢ XY = Scroll speed in each direction\n\n" +
                "Use negative Y for falling droplets."},
            
            {"_RainNormalStrength", 
                "Strength of droplet normal perturbation.\n\n" +
                "â€¢ 0 = Flat droplets (no bump)\n" +
                "â€¢ 1 = Normal strength\n" +
                "â€¢ >1 = Exaggerated bumps\n\n" +
                "Affects lighting on droplets."},
            
            {"_RainDistortion", 
                "How much droplets distort refraction.\n\n" +
                "â€¢ 0 = Visual droplets only\n" +
                "â€¢ 1 = Strong refraction through droplets\n\n" +
                "Creates realistic water lens effect."},
            
            {"_RainWetness", 
                "Surface wetness around droplets.\n\n" +
                "â€¢ 0 = Dry surface\n" +
                "â€¢ 1 = Wet/shiny surface\n\n" +
                "Increases smoothness in rain areas."},
            
            // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
            // RIM LIGHTING
            // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
            {"_UseRim", 
                "Enable rim lighting effect.\n\n" +
                "Adds a bright edge/outline around the glass.\n" +
                "Great for highlighting objects, sci-fi effects."},
            
            {"_RimColor", 
                "Color of the rim light.\n\n" +
                "Use HDR colors for bloom-compatible glow.\n" +
                "Creates colored edge highlights."},
            
            {"_RimPower", 
                "Sharpness of the rim light falloff.\n\n" +
                "â€¢ Low (1-2) = Wide, soft rim\n" +
                "â€¢ High (5-10) = Thin, sharp edge line\n\n" +
                "Similar to Fresnel Power."},
            
            {"_RimIntensity", 
                "Brightness of the rim light.\n\n" +
                "â€¢ 0 = No rim\n" +
                "â€¢ 1 = Normal brightness\n" +
                "â€¢ >1 = Bright glow (use with HDR/bloom)\n\n" +
                "Multiplied with Rim Color."},
            
            {"_RimMin", 
                "Minimum rim value.\n\n" +
                "Sets a floor for the rim effect.\n" +
                "Use to ensure some rim is always visible."},
            
            {"_RimMax", 
                "Maximum rim value.\n\n" +
                "Caps the rim effect intensity.\n" +
                "Use to prevent overblown edges."},
            
            // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
            // TRANSLUCENCY
            // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
            {"_UseTranslucent", 
                "Enable translucency (subsurface scattering).\n\n" +
                "Light passes through the glass and scatters.\n" +
                "Creates soft, glowing appearance from behind."},
            
            {"_TranslucentColor", 
                "Color of transmitted light.\n\n" +
                "Light passing through is tinted this color.\n" +
                "Warm colors (orange/red) look natural."},
            
            {"_TranslucentIntensity", 
                "Strength of the translucency effect.\n\n" +
                "â€¢ 0 = No translucency\n" +
                "â€¢ 1-3 = Normal\n" +
                "â€¢ >3 = Very strong glow-through\n\n" +
                "Higher = more light passes through."},
            
            {"_TranslucentPower", 
                "Falloff curve of translucency.\n\n" +
                "â€¢ Low (1-2) = Soft, spread light\n" +
                "â€¢ High (8-16) = Concentrated around light\n\n" +
                "Controls how focused the effect is."},
            
            {"_TranslucentDistortion", 
                "How much the normal distorts translucency.\n\n" +
                "â€¢ 0 = Uniform translucency\n" +
                "â€¢ Positive = Normal bends light forward\n" +
                "â€¢ Negative = Normal bends light backward\n\n" +
                "Adds surface detail to the effect."},
            
            {"_TranslucentScale", 
                "Overall scale of the translucency effect.\n\n" +
                "Multiplier for the final result.\n" +
                "Use for quick intensity adjustment."},
            
            // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
            // OCCLUSION
            // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
            {"_UseOcclusion", 
                "Enable ambient occlusion map.\n\n" +
                "Darkens crevices and corners.\n" +
                "Adds depth and contact shadows."},
            
            {"_OcclusionMap", 
                "Grayscale ambient occlusion texture.\n\n" +
                "â€¢ White = No occlusion (fully lit)\n" +
                "â€¢ Black = Full occlusion (shadowed)\n\n" +
                "Usually baked from 3D software."},
            
            {"_OcclusionStrength", 
                "Intensity of the occlusion effect.\n\n" +
                "â€¢ 0 = No occlusion darkening\n" +
                "â€¢ 1 = Full occlusion effect\n\n" +
                "Reduce if occlusion looks too dark."},
            
            // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
            // EMISSION
            // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
            {"_UseEmission", 
                "Enable emissive lighting.\n\n" +
                "Makes the glass glow and emit light.\n" +
                "Works with bloom post-processing."},
            
            {"_UseEmissionMap", 
                "Use an emission texture.\n\n" +
                "â€¢ OFF = Uniform emission color\n" +
                "â€¢ ON = Emission controlled by texture\n\n" +
                "Allows patterned glowing areas."},
            
            {"_EmissionMap", 
                "Emission mask texture.\n\n" +
                "â€¢ RGB = Emission color/pattern\n" +
                "â€¢ Brightness = Emission intensity\n\n" +
                "Black areas = no emission."},
            
            {"_EmissionColor", 
                "Emission color (HDR).\n\n" +
                "Use HDR colors (intensity > 1) for bloom.\n" +
                "Multiplied with emission map if used."},
            
            {"_EmissionIntensity", 
                "Emission brightness multiplier.\n\n" +
                "â€¢ 0 = No emission\n" +
                "â€¢ 1 = Normal\n" +
                "â€¢ >1 = Brighter glow\n\n" +
                "Use with HDR color for bloom effects."},
            
            // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
            // TRIPLANAR
            // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
            {"_UseTriplanar", 
                "Enable triplanar texture mapping.\n\n" +
                "Projects textures from 3 world axes.\n" +
                "Eliminates UV stretching on any surface.\n\n" +
                "Perfect for: procedural objects, terrain glass."},
            
            {"_TriplanarScale", 
                "World-space scale for triplanar projection.\n\n" +
                "â€¢ Smaller = Larger texture\n" +
                "â€¢ Larger = Finer detail\n\n" +
                "Adjust to match your world scale."},
            
            {"_TriplanarSharpness", 
                "Blend sharpness between the 3 projections.\n\n" +
                "â€¢ 1 = Soft blend (may show stretching)\n" +
                "â€¢ 8 = Good balance\n" +
                "â€¢ 20 = Sharp transitions\n\n" +
                "Higher = cleaner but harder edges."},
            
            // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
            // ABSORPTION
            // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
            {"_UseAbsorption", 
                "Enable light absorption (beer's law).\n\n" +
                "Light gets absorbed as it travels through glass.\n" +
                "Thicker glass = darker/more colored."},
            
            {"_AbsorptionColor", 
                "Color of absorbed light.\n\n" +
                "The glass tints toward this color as thickness increases.\n" +
                "Green/blue = bottle glass, Brown = beer bottle."},
            
            {"_AbsorptionDensity", 
                "Absorption rate.\n\n" +
                "â€¢ 0 = No absorption\n" +
                "â€¢ 1 = Normal density\n" +
                "â€¢ >1 = Dense, dark glass\n\n" +
                "Higher = quicker color saturation."},
            
            {"_AbsorptionFalloff", 
                "How absorption changes with depth.\n\n" +
                "â€¢ 0.5 = Linear falloff\n" +
                "â€¢ 1 = Natural exponential\n" +
                "â€¢ >1 = Rapid absorption near surface\n\n" +
                "Controls the absorption curve."},
            
            // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
            // CAUSTICS
            // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
            {"_UseCaustics", 
                "Enable caustic light patterns.\n\n" +
                "Simulates focused light patterns from refraction.\n" +
                "Like light through water or textured glass."},
            
            {"_UseCausticsProcedural", 
                "Use procedural caustics instead of texture.\n\n" +
                "â€¢ ON = Animated procedural pattern\n" +
                "â€¢ OFF = Use caustics texture\n\n" +
                "Procedural is animated and tileable."},
            
            {"_CausticsTexture", 
                "Caustics pattern texture (when not procedural).\n\n" +
                "Grayscale texture of light patterns.\n" +
                "Should be tileable for best results."},
            
            {"_CausticsColor", 
                "Color tint for caustics.\n\n" +
                "Usually white or slightly warm.\n" +
                "Colored caustics for stylized looks."},
            
            {"_CausticsIntensity", 
                "Brightness of caustic patterns.\n\n" +
                "â€¢ 0 = No caustics visible\n" +
                "â€¢ 1 = Normal brightness\n" +
                "â€¢ >1 = Bright, prominent caustics\n\n" +
                "Adds sparkle and life to glass."},
            
            {"_CausticsScale", 
                "Size of caustic patterns.\n\n" +
                "â€¢ Small (0.5) = Large patterns\n" +
                "â€¢ Large (5) = Fine, detailed patterns\n\n" +
                "Adjust based on glass size."},
            
            {"_CausticsSpeed", 
                "Animation speed of caustics.\n\n" +
                "â€¢ 0 = Static caustics\n" +
                "â€¢ 0.5 = Gentle movement\n" +
                "â€¢ >1 = Fast, active animation\n\n" +
                "Slow speeds look more natural."},
            
            {"_CausticsDistortion", 
                "How much caustics warp with the surface.\n\n" +
                "â€¢ 0 = Caustics ignore surface normals\n" +
                "â€¢ 1 = Caustics follow surface shape\n\n" +
                "Higher = more organic, surface-following pattern."},
            
            // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
            // TOTAL INTERNAL REFLECTION
            // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
            {"_UseTIR", 
                "Enable Total Internal Reflection.\n\n" +
                "At steep angles, light reflects inside instead of passing through.\n" +
                "Creates realistic glass behavior at edges."},
            
            {"_TIRIntensity", 
                "Strength of internal reflection.\n\n" +
                "â€¢ 0 = No TIR effect\n" +
                "â€¢ 1 = Full physical TIR\n\n" +
                "Higher = more visible internal reflections."},
            
            {"_TIRCriticalAngle", 
                "Angle threshold for TIR to occur.\n\n" +
                "Real glass: ~42Â° (IOR 1.5)\n" +
                "Lower = TIR happens at more angles.\n\n" +
                "Adjust for artistic control."},
            
            {"_TIRSharpness", 
                "Transition sharpness at critical angle.\n\n" +
                "â€¢ Low = Gradual transition\n" +
                "â€¢ High = Sharp cutoff\n\n" +
                "Higher = more dramatic TIR edge."},
            
            // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
            // SPARKLE
            // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
            {"_UseSparkle", 
                "Enable sparkle/glitter effect.\n\n" +
                "Adds tiny bright spots that shimmer.\n" +
                "Great for: crystal, frost, magical glass."},
            
            {"_SparkleColor", 
                "Color of sparkle points.\n\n" +
                "Use HDR colors for bloom-compatible sparkles.\n" +
                "White = natural, Colored = stylized."},
            
            {"_SparkleIntensity", 
                "Brightness of sparkles.\n\n" +
                "â€¢ 0 = No sparkles visible\n" +
                "â€¢ 1 = Normal brightness\n" +
                "â€¢ >1 = Bright, prominent sparkles\n\n" +
                "Use with HDR for bloom."},
            
            {"_SparkleScale", 
                "Size of the sparkle pattern.\n\n" +
                "â€¢ Small = Large sparkle cells\n" +
                "â€¢ Large = Many small sparkles\n\n" +
                "Affects sparkle density."},
            
            {"_SparkleSize", 
                "Size of individual sparkle points.\n\n" +
                "â€¢ Small = Tiny pinpoint sparkles\n" +
                "â€¢ Large = Bigger star-like sparkles\n\n" +
                "Controls each sparkle's footprint."},
            
            {"_SparkleDensity", 
                "How many sparkles appear.\n\n" +
                "â€¢ Low = Sparse, few sparkles\n" +
                "â€¢ High = Dense, many sparkles\n\n" +
                "Controls sparkle frequency."},
            
            {"_SparkleSpeed", 
                "Animation speed of sparkle shimmer.\n\n" +
                "â€¢ 0 = Static sparkles\n" +
                "â€¢ >0 = Twinkling animation\n\n" +
                "Creates dynamic, living sparkle."},
            
            // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
            // DIRT/MOSS
            // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
            {"_UseDust", 
                "Enable Dirt/Moss effect.\n\n" +
                "Adds organic growth or dirt accumulation.\n" +
                "â€¢ Moss growing from bottom\n" +
                "â€¢ Grime dripping from top\n" +
                "â€¢ Dust on horizontal surfaces\n\n" +
                "FIXED TO OBJECT: Dirt follows the object when it moves.\n\n" +
                "Dirt hides ALL glass effects:\n" +
                "Reflections, Fresnel, Specular, Sparkle, etc."},
            
            {"_DirtDirection", 
                "Direction of dirt/moss growth.\n\n" +
                "â€¢ Bottom Up: Moss grows upward from below Height Level\n" +
                "â€¢ Top Down: Grime drips down from above Height Level\n" +
                "â€¢ Normal Based: Settles on horizontal surfaces\n\n" +
                "Uses local object Y axis."},
            
            {"_DirtHeight", 
                "Height Level (local Y) for the moss/dirt edge.\n\n" +
                "â€¢ Bottom Up: Top edge of moss (moss below this)\n" +
                "â€¢ Top Down: Bottom edge of dirt (dirt above this)\n\n" +
                "Example for sphere (radius ~0.5):\n" +
                "â€¢ Height = 0: Moss covers bottom half\n" +
                "â€¢ Height = -0.3: Moss covers lower third"},
            
            {"_DirtSpread", 
                "Size of the transition zone.\n\n" +
                "â€¢ Small (0.1): Sharp edge\n" +
                "â€¢ Large (1.0): Very gradual fade\n\n" +
                "This is the vertical distance over which\n" +
                "the moss fades from 100% to 0%."},
            
            {"_DirtSoftness", 
                "Additional softness for the edge.\n\n" +
                "Smooths out the final transition.\n" +
                "â€¢ 0 = Sharp (just Spread)\n" +
                "â€¢ 1 = Extra smooth"},
            
            {"_DustTexture", 
                "Pattern texture for dirt/moss.\n\n" +
                "Grayscale: White = covered, Black = clean.\n" +
                "Use moss, dirt, or grunge textures."},
            
            {"_DustTiling", 
                "Texture tiling/repetition.\n\n" +
                "Higher = more repetition, finer pattern.\n" +
                "Only used when Triplanar is OFF."},
            
            {"_DustColor", 
                "Main color of dirt/moss.\n\n" +
                "â€¢ Green for moss\n" +
                "â€¢ Brown for dirt\n" +
                "â€¢ Gray for grime"},
            
            {"_DirtColorVariation", 
                "Secondary color for variation.\n\n" +
                "Blends with main color for organic look.\n" +
                "Use a darker or lighter shade."},
            
            {"_DirtVariationScale", 
                "Scale of color variation noise.\n\n" +
                "â€¢ Small = Large color patches\n" +
                "â€¢ Large = Fine color variation"},
            
            {"_DustIntensity", 
                "Overall amount of dirt/moss.\n\n" +
                "â€¢ 0 = Clean glass\n" +
                "â€¢ 1 = Maximum coverage\n\n" +
                "Main control for the effect."},
            
            {"_DustCoverage", 
                "How much texture affects the pattern.\n\n" +
                "â€¢ 0 = Full texture influence (patchy)\n" +
                "â€¢ 1 = Ignore texture (solid coverage)\n\n" +
                "Use 1 for solid moss, 0 for patchy look."},
            
            {"_DirtFullOpacity", 
                "Opacity of covered areas.\n\n" +
                "â€¢ 1 = Fully opaque (blocks view & effects)\n" +
                "â€¢ <1 = Semi-transparent\n\n" +
                "Also controls how much glass effects are hidden."},
            
            {"_DustRoughness", 
                "Surface roughness of dirty areas.\n\n" +
                "â€¢ 0 = Smooth (wet)\n" +
                "â€¢ 1 = Matte (dry)\n\n" +
                "Moss/dirt are usually rough."},
            
            {"_DustNormalBlend", 
                "How much dirt flattens the surface normal.\n\n" +
                "Creates matte, diffuse appearance."},
            
            {"_DirtUseEdgeNoise", 
                "Add procedural noise to edges.\n\n" +
                "Creates organic, irregular borders.\n" +
                "Essential for natural look."},
            
            {"_DirtEdgeNoiseScale", 
                "Scale of edge noise pattern.\n\n" +
                "â€¢ Small = Large irregular chunks\n" +
                "â€¢ Large = Fine edge detail"},
            
            {"_DirtEdgeNoiseStrength", 
                "Strength of edge noise.\n\n" +
                "â€¢ 0 = Smooth edges\n" +
                "â€¢ 1 = Very irregular edges"},
            
            {"_DustEdgeFalloff", 
                "Fresnel falloff for dirt.\n\n" +
                "Less dirt at grazing angles.\n" +
                "Usually OFF for moss."},
            
            {"_DustEdgePower", 
                "Power of fresnel falloff.\n\n" +
                "Higher = more edge fade."},
            
            {"_UseDustTriplanar", 
                "Use triplanar texture projection.\n\n" +
                "Projects from 3 LOCAL axes (follows object).\n" +
                "Better for complex shapes without UVs."},
            
            {"_DustTriplanarScale", 
                "Scale for triplanar projection.\n\n" +
                "Pattern size in local/object units."},
            
            {"_DustTriplanarSharpness", 
                "Blend sharpness between axes.\n\n" +
                "Higher = sharper transitions."},
            
            {"_DustTriplanarRotation", 
                "Rotation angle for triplanar projection.\n\n" +
                "Use to hide visible seams on your mesh.\n" +
                "â€¢ 0Â° = Default orientation\n" +
                "â€¢ 45Â° = Diagonal pattern\n" +
                "â€¢ 90Â° = Perpendicular\n\n" +
                "Rotates around local Y axis (up)."},
            
            // DAMAGE
            {"_UseDamage", 
                "Enable Damage / Cracks system.\n\n" +
                "Adds cracks to glass surface.\n" +
                "Procedural mode: Voronoi-based, no texture needed.\n" +
                "Texture mode: Use a damage mask for custom patterns.\n\n" +
                "Cracks affect normal, refraction, and add emission."},
            
            {"_DamageProgression", 
                "How damaged the glass is.\n\n" +
                "0 = No visible damage\n" +
                "0.5 = Moderate cracking\n" +
                "1 = Fully cracked\n\n" +
                "Animate this for breaking glass effect."},
            
            {"_ProceduralCrackDensity", 
                "Number of Voronoi cells.\n\n" +
                "Low (2-5) = Few large cracks\n" +
                "High (10-20) = Many small cracks\n\n" +
                "Controls the overall crack pattern density."},
            
            {"_ProceduralCrackSeed", 
                "Randomize the crack pattern.\n\n" +
                "Different values produce different crack layouts.\n" +
                "Useful for variation across multiple objects."},
            
            {"_CrackWidth", 
                "Width of the crack lines.\n\n" +
                "Low (0.01-0.2) = Hair-thin fractures\n" +
                "Medium (0.3-0.7) = Visible cracks\n" +
                "High (1-2) = Wide shattered look\n\n" +
                "Works with Sharpness for full control."},
            
            {"_CrackSharpness", 
                "Edge falloff of crack lines.\n\n" +
                "Low (0.1-0.5) = Soft, blurry edges\n" +
                "Medium (1) = Natural look\n" +
                "High (2-5) = Razor sharp edges\n\n" +
                "Combine with Width for different styles."},
            
            {"_CrackColor", 
                "Color of the crack lines.\n\n" +
                "Dark colors for realistic cracks.\n" +
                "Bright colors for stylized/energy effects.\n" +
                "Also used for crack edge emission tint."},
            
            {"_CrackDepth", 
                "Normal perturbation strength along cracks.\n\n" +
                "0 = Flat cracks (painted look)\n" +
                "1 = Visible depth in cracks\n" +
                "2 = Exaggerated depth\n\n" +
                "Adds 3D feel to the crack pattern."},
            
            {"_CrackEmission", 
                "Glow intensity along crack edges.\n\n" +
                "0 = No glow\n" +
                "1 = Subtle edge light\n" +
                "3+ = Strong glow effect\n\n" +
                "Great for energy/magic glass effects."},
            
            {"_ShatterDistortion", 
                "Refraction distortion from cracks.\n\n" +
                "0 = No distortion\n" +
                "0.5 = Moderate shatter look\n" +
                "1 = Heavy distortion\n\n" +
                "Each cracked cell offsets refraction slightly."},
            
            {"_DamageMask", 
                "Spatial mask controlling WHERE cracks appear.\n\n" +
                "R channel = Crack zone (white = cracked)\n" +
                "White = Cracks visible here\n" +
                "Black = No cracks in this zone\n\n" +
                "Supports tiling/offset for custom patterns."},
            
            {"_CrackNormalMap", 
                "Normal map for texture-based cracks.\n\n" +
                "Blended on top of procedural Voronoi normal.\n" +
                "Adds surface detail to the crack pattern."},
            
            // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
            // DECALS
            // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
            {"_UseDecals", 
                "Enable decal system.\n\n" +
                "Up to 4 decal slots for stickers, labels, logos.\n" +
                "Each decal can be positioned, rotated, and scaled."},
            
            {"_DecalTexture1", 
                "Decal 1 texture.\n\n" +
                "â€¢ RGB = Decal color\n" +
                "â€¢ Alpha = Decal shape/mask\n\n" +
                "Transparent areas show glass through."},
            
            {"_DecalPosition1", 
                "UV position of decal 1.\n\n" +
                "â€¢ XY = Center position (0-1 UV space)\n" +
                "â€¢ (0.5, 0.5) = Center of surface\n\n" +
                "Drag to position the decal."},
            
            {"_DecalSize1", 
                "Size of decal 1.\n\n" +
                "â€¢ Small = Tiny decal\n" +
                "â€¢ Large = Big decal\n\n" +
                "Uniform scaling of the decal."},
            
            {"_DecalRotation1", 
                "Rotation of decal 1 (degrees).\n\n" +
                "â€¢ 0 = Original orientation\n" +
                "â€¢ 90, 180, 270 = Rotated\n\n" +
                "Rotates around decal center."},
            
            {"_DecalIntensity1", 
                "Visibility/opacity of decal 1.\n\n" +
                "â€¢ 0 = Invisible\n" +
                "â€¢ 1 = Fully visible\n\n" +
                "Fade decals in/out."},
            
            // Decal 2
            {"_UseDecal2", "Enable second decal slot."},
            {"_DecalTexture2", "Decal 2 texture (RGBA)."},
            {"_DecalPosition2", "UV position of decal 2."},
            {"_DecalSize2", "Size of decal 2."},
            {"_DecalRotation2", "Rotation of decal 2 (degrees)."},
            {"_DecalIntensity2", "Visibility of decal 2."},
            
            // Decal 3
            {"_UseDecal3", "Enable third decal slot."},
            {"_DecalTexture3", "Decal 3 texture (RGBA)."},
            {"_DecalPosition3", "UV position of decal 3."},
            {"_DecalSize3", "Size of decal 3."},
            {"_DecalRotation3", "Rotation of decal 3 (degrees)."},
            {"_DecalIntensity3", "Visibility of decal 3."},
            
            // Decal 4
            {"_UseDecal4", "Enable fourth decal slot."},
            {"_DecalTexture4", "Decal 4 texture (RGBA)."},
            {"_DecalPosition4", "UV position of decal 4."},
            {"_DecalSize4", "Size of decal 4."},
            {"_DecalRotation4", "Rotation of decal 4 (degrees)."},
            {"_DecalIntensity4", "Visibility of decal 4."},
            
            // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
            // FINGERPRINTS
            // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
            {"_UseFingerprints", 
                "Enable fingerprint/smudge system.\n\n" +
                "Up to 4 fingerprint slots using textures.\n" +
                "Adds realistic touch marks to glass surfaces."},
            
            {"_FingerprintTint", 
                "Global tint color for all fingerprints.\n\n" +
                "â€¢ RGB = Smudge color (usually warm/skin tone)\n" +
                "â€¢ Alpha = Tint strength\n\n" +
                "Affects all fingerprint slots uniformly."},
            
            // Fingerprint Slot 1
            {"_FingerprintTexture1", 
                "Fingerprint 1 texture.\n\n" +
                "â€¢ R channel = Fingerprint mask\n" +
                "â€¢ White = Visible smudge\n" +
                "â€¢ Black = Clean glass\n\n" +
                "Use fingerprint photos or patterns."},
            
            {"_FingerprintMapping1", 
                "Mapping mode for fingerprint 1.\n\n" +
                "â€¢ UV: Uses mesh UV coordinates\n" +
                "â€¢ World: Uses 3D world position (tangent projection)\n" +
                "â€¢ Triplanar: World position + triplanar sampling\n\n" +
                "Triplanar is best for curved surfaces without UV stretching.\n" +
                "Both World and Triplanar use World Position + Radius."},
            
            {"_FingerprintPos1", 
                "UV position of fingerprint 1.\n\n" +
                "â€¢ XY = Center position (0-1 UV space)\n\n" +
                "Only used in UV mapping mode."},
            
            {"_FingerprintScale1", 
                "UV scale of fingerprint 1.\n\n" +
                "â€¢ XY = Size in UV space\n\n" +
                "Larger values = smaller fingerprint."},
            
            {"_FingerprintWorldPos1", 
                "Local position of fingerprint 1.\n\n" +
                "â€¢ XYZ = Position in object space\n\n" +
                "Position relative to object pivot.\n" +
                "Fingerprint follows the object when moved."},
            
            {"_FingerprintWorldRadius1", 
                "Radius of fingerprint 1.\n\n" +
                "Size of the fingerprint area.\n" +
                "Larger = bigger fingerprint zone."},
            
            {"_FingerprintTriplanarScale1", 
                "Scale for triplanar fingerprint projection.\n\n" +
                "â€¢ Small (<1) = Fingerprint fills more of the radius\n" +
                "â€¢ 1 = Fingerprint matches world radius\n" +
                "â€¢ Large (>1) = Smaller fingerprint within radius\n\n" +
                "Adjust to fit your fingerprint texture to the radius."},
            
            {"_FingerprintRotation1", 
                "Rotation of fingerprint 1 (degrees).\n\n" +
                "â€¢ 0-360 = Rotation angle\n\n" +
                "Rotates around fingerprint center."},
            
            {"_FingerprintIntensity1", 
                "Visibility of fingerprint 1.\n\n" +
                "â€¢ 0 = Clean (no fingerprint)\n" +
                "â€¢ 1 = Fully visible smudge\n\n" +
                "Main control for fingerprint visibility."},
            
            {"_FingerprintRoughness1", 
                "Roughness added by fingerprint 1.\n\n" +
                "â€¢ 0 = Fingerprint doesn't affect smoothness\n" +
                "â€¢ 1 = Fingerprint areas become matte\n\n" +
                "Real fingerprints reduce glass clarity."},
            
            {"_FingerprintFalloff1", 
                "Edge softness of fingerprint 1.\n\n" +
                "â€¢ 0 = Hard edges\n" +
                "â€¢ 1 = Soft, feathered edges\n\n" +
                "Higher = more natural blending."},
            
            // Fingerprint Slots 2-4 (simplified)
            {"_UseSlot2", "Enable second fingerprint slot."},
            {"_FingerprintTexture2", "Fingerprint 2 texture (R=mask)."},
            {"_FingerprintMapping2", "Mapping mode: UV, World, or Triplanar."},
            {"_FingerprintPos2", "UV position of fingerprint 2."},
            {"_FingerprintScale2", "UV scale of fingerprint 2."},
            {"_FingerprintWorldPos2", "Local position of fingerprint 2 (object space)."},
            {"_FingerprintWorldRadius2", "Radius of fingerprint 2."},
            {"_FingerprintTriplanarScale2", "Triplanar scale for fingerprint 2."},
            {"_FingerprintRotation2", "Rotation of fingerprint 2."},
            {"_FingerprintIntensity2", "Visibility of fingerprint 2."},
            {"_FingerprintRoughness2", "Roughness added by fingerprint 2."},
            {"_FingerprintFalloff2", "Edge softness of fingerprint 2."},
            
            {"_UseSlot3", "Enable third fingerprint slot."},
            {"_FingerprintTexture3", "Fingerprint 3 texture (R=mask)."},
            {"_FingerprintMapping3", "Mapping mode: UV, World, or Triplanar."},
            {"_FingerprintPos3", "UV position of fingerprint 3."},
            {"_FingerprintScale3", "UV scale of fingerprint 3."},
            {"_FingerprintWorldPos3", "Local position of fingerprint 3 (object space)."},
            {"_FingerprintWorldRadius3", "Radius of fingerprint 3."},
            {"_FingerprintTriplanarScale3", "Triplanar scale for fingerprint 3."},
            {"_FingerprintRotation3", "Rotation of fingerprint 3."},
            {"_FingerprintIntensity3", "Visibility of fingerprint 3."},
            {"_FingerprintRoughness3", "Roughness added by fingerprint 3."},
            {"_FingerprintFalloff3", "Edge softness of fingerprint 3."},
            
            {"_UseSlot4", "Enable fourth fingerprint slot."},
            {"_FingerprintTexture4", "Fingerprint 4 texture (R=mask)."},
            {"_FingerprintMapping4", "Mapping mode: UV, World, or Triplanar."},
            {"_FingerprintPos4", "UV position of fingerprint 4."},
            {"_FingerprintScale4", "UV scale of fingerprint 4."},
            {"_FingerprintWorldPos4", "Local position of fingerprint 4 (object space)."},
            {"_FingerprintWorldRadius4", "Radius of fingerprint 4."},
            {"_FingerprintTriplanarScale4", "Triplanar scale for fingerprint 4."},
            {"_FingerprintRotation4", "Rotation of fingerprint 4."},
            {"_FingerprintIntensity4", "Visibility of fingerprint 4."},
            {"_FingerprintRoughness4", "Roughness added by fingerprint 4."},
            {"_FingerprintFalloff4", "Edge softness of fingerprint 4."},
            
            // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
            // DISTORTION FX
            // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
            {"_UseDistortion", 
                "Master toggle for distortion effects.\n\n" +
                "Enables various screen-space distortion effects.\n" +
                "Each effect can be enabled independently below."},
            
            // Magnify
            {"_UseMagnify", 
                "Enable magnifying lens effect.\n\n" +
                "Makes area behind glass appear larger/smaller.\n" +
                "Like looking through a magnifying glass."},
            
            {"_MagnifyStrength", 
                "Magnification power.\n\n" +
                "â€¢ >1 = Zoom in (magnify)\n" +
                "â€¢ <1 = Zoom out (minify)\n" +
                "â€¢ 1 = No effect\n\n" +
                "2.0 = 2x magnification."},
            
            {"_MagnifyCenter", 
                "Center point of magnification.\n\n" +
                "â€¢ XY = Screen UV (0.5, 0.5 = center)\n\n" +
                "Magnification radiates from this point."},
            
            {"_MagnifyRadius", 
                "Radius of magnification area.\n\n" +
                "â€¢ Small = Tight lens effect\n" +
                "â€¢ Large = Wide area affected\n\n" +
                "Controls magnification zone size."},
            
            {"_MagnifyFalloff", 
                "Edge falloff of magnification.\n\n" +
                "â€¢ 0 = Hard edge\n" +
                "â€¢ 1 = Soft, gradual transition\n\n" +
                "Smooths the magnification boundary."},
            
            // Barrel
            {"_UseBarrel", 
                "Enable barrel/pincushion distortion.\n\n" +
                "Simulates wide-angle or fisheye lens.\n" +
                "Bends straight lines into curves."},
            
            {"_BarrelStrength", 
                "Distortion strength.\n\n" +
                "â€¢ Positive = Barrel (bulging out)\n" +
                "â€¢ Negative = Pincushion (pinching in)\n" +
                "â€¢ 0 = No distortion\n\n" +
                "Higher absolute value = stronger effect."},
            
            // Waves
            {"_UseWaves", 
                "Enable wave distortion.\n\n" +
                "Creates rippling, water-like effect.\n" +
                "Animated waves distort the view."},
            
            {"_WaveFrequency", 
                "Wave frequency/density.\n\n" +
                "â€¢ Low = Long, gentle waves\n" +
                "â€¢ High = Many short waves\n\n" +
                "Controls wave spacing."},
            
            {"_WaveAmplitude", 
                "Wave height/strength.\n\n" +
                "â€¢ 0 = No waves\n" +
                "â€¢ Higher = Stronger distortion\n\n" +
                "Controls wave intensity."},
            
            {"_WaveSpeed", 
                "Wave animation speed.\n\n" +
                "â€¢ 0 = Static waves\n" +
                "â€¢ Higher = Faster animation\n\n" +
                "Negative = Reverse direction."},
            
            // Ripple
            {"_UseRipple", 
                "Enable ripple distortion.\n\n" +
                "Concentric circles emanating from a point.\n" +
                "Like dropping a stone in water."},
            
            {"_RippleCenter", 
                "Ripple origin point.\n\n" +
                "â€¢ XY = UV coordinates (0.5, 0.5 = center)\n\n" +
                "Ripples expand from this point."},
            
            {"_RippleFrequency", 
                "Number of ripple rings.\n\n" +
                "â€¢ Low = Few wide rings\n" +
                "â€¢ High = Many tight rings\n\n" +
                "Controls ripple density."},
            
            {"_RippleAmplitude", 
                "Ripple strength.\n\n" +
                "â€¢ 0 = No ripple\n" +
                "â€¢ Higher = Stronger distortion\n\n" +
                "Controls ripple intensity."},
            
            {"_RippleSpeed", 
                "Ripple expansion speed.\n\n" +
                "â€¢ 0 = Static ripples\n" +
                "â€¢ Higher = Faster expansion\n\n" +
                "Negative = Contracting ripples."},
            
            {"_RippleDecay", 
                "How quickly ripples fade with distance.\n\n" +
                "â€¢ Low = Ripples extend far\n" +
                "â€¢ High = Ripples fade quickly\n\n" +
                "Controls ripple range."},
            
            // Swirl
            {"_UseSwirl", 
                "Enable swirl/vortex distortion.\n\n" +
                "Rotates the image around a center point.\n" +
                "Creates spiral/vortex effect."},
            
            {"_SwirlStrength", 
                "Swirl rotation amount.\n\n" +
                "â€¢ Positive = Clockwise\n" +
                "â€¢ Negative = Counter-clockwise\n\n" +
                "Higher = more rotation."},
            
            {"_SwirlRadius", 
                "Swirl area radius.\n\n" +
                "â€¢ Small = Tight vortex\n" +
                "â€¢ Large = Wide swirl area\n\n" +
                "Controls affected region."},
            
            {"_SwirlCenter", 
                "Swirl center point.\n\n" +
                "â€¢ XY = UV coordinates\n\n" +
                "Rotation happens around this point."},
            
            {"_SwirlSpeed", 
                "Swirl animation speed.\n\n" +
                "â€¢ 0 = Static swirl\n" +
                "â€¢ >0 = Animated rotation\n\n" +
                "Creates spinning vortex effect."},
            
            // Heat Haze
            {"_UseHeatHaze", 
                "Enable heat haze effect.\n\n" +
                "Shimmering distortion like hot air.\n" +
                "Creates wavy, organic distortion."},
            
            {"_HeatHazeStrength", 
                "Haze distortion intensity.\n\n" +
                "â€¢ 0 = No haze\n" +
                "â€¢ Higher = Stronger shimmer\n\n" +
                "Controls distortion amount."},
            
            {"_HeatHazeSpeed", 
                "Shimmer animation speed.\n\n" +
                "â€¢ 0 = Static haze\n" +
                "â€¢ Higher = Faster shimmer\n\n" +
                "Creates animated heat effect."},
            
            {"_HeatHazeScale", 
                "Scale of haze pattern.\n\n" +
                "â€¢ Small = Large, blobby haze\n" +
                "â€¢ Large = Fine, detailed shimmer\n\n" +
                "Controls pattern size."},
            
            // Pixelate
            {"_UsePixelate", 
                "Enable pixelation effect.\n\n" +
                "Reduces resolution of view through glass.\n" +
                "Creates retro/digital effect."},
            
            {"_PixelateSize", 
                "Pixel block size.\n\n" +
                "â€¢ Small = Fine pixelation\n" +
                "â€¢ Large = Big, blocky pixels\n\n" +
                "Higher = more obvious effect."},
            
            // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
            // SHADOWS
            // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
            {"_ReceiveShadows", 
                "Enable shadow receiving.\n\n" +
                "Glass will show shadows from other objects.\n" +
                "Turn off for purely transparent glass."},
            
            {"_ShadowIntensity", 
                "Shadow darkness on the glass.\n\n" +
                "â€¢ 0 = No shadows visible\n" +
                "â€¢ 1 = Full shadow darkness\n\n" +
                "Reduce for subtle shadows."},
            
            // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
            // RENDERING
            // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
            {"_Surface", 
                "Surface rendering type.\n\n" +
                "â€¢ Opaque = No transparency\n" +
                "â€¢ Transparent = Alpha blending enabled\n\n" +
                "Glass should usually be Transparent."},
            
            {"_BlendMode", 
                "Blend mode for transparency.\n\n" +
                "â€¢ Alpha = Standard alpha blending\n" +
                "â€¢ Premultiply = Pre-multiplied alpha\n" +
                "â€¢ Additive = Adds to background\n" +
                "â€¢ Multiply = Darkens background\n\n" +
                "Alpha is most common for glass."},
            
            {"_Cull", 
                "Face culling mode.\n\n" +
                "â€¢ Off = Double-sided (see both sides)\n" +
                "â€¢ Front = Only see back faces\n" +
                "â€¢ Back = Only see front faces (default)\n\n" +
                "Use Off for thin glass panes."},
            
            {"_ZWrite", 
                "Depth buffer writing.\n\n" +
                "â€¢ On = Writes to depth (blocks objects behind)\n" +
                "â€¢ Off = Transparent (typical for glass)\n\n" +
                "Usually Off for transparent glass."},
            
            {"_QueueOffset", 
                "Render queue offset.\n\n" +
                "Adjusts rendering order relative to other transparent objects.\n" +
                "â€¢ Positive = Render later (in front)\n" +
                "â€¢ Negative = Render earlier (behind)\n\n" +
                "Use to fix sorting issues between glass objects."},
        };

        /// <summary>
        /// Get tooltip for a shader property by name.
        /// Returns null if no tooltip is defined.
        /// </summary>
        public static string GetTooltip(string propertyName)
        {
            if (tooltips.TryGetValue(propertyName, out string tip))
                return tip;
            return null;
        }
        
        /// <summary>
        /// Check if a tooltip exists for the given property.
        /// </summary>
        public static bool HasTooltip(string propertyName)
        {
            return tooltips.ContainsKey(propertyName);
        }
        
        /// <summary>
        /// Get all tooltips (for debugging or export).
        /// </summary>
        public static IReadOnlyDictionary<string, string> GetAllTooltips()
        {
            return tooltips;
        }
    }
}
