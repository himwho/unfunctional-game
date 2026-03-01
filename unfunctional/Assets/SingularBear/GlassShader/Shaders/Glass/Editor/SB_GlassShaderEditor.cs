// ============================================================
// SingularBear Glass Shader V1 - Custom ShaderGUI
// Material Editor with Search, Categories, Foldouts & Presets
// Copyright (c) SingularBear - All Rights Reserved
// ============================================================
using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using SingularBear.Glass;

namespace SingularBear.Shaders
{
    /// <summary>
    /// Custom ShaderGUI for SingularBear Glass Shader V1
    /// Features: Search Bar, Category Tabs, Foldout Sections, Auto Keywords, Preset System.
    /// </summary>
    public class SB_GlassShaderEditor : ShaderGUI
    {
        // ========================================
        // CACHED GUI CONTENT (Avoid GC allocations)
        // ========================================
        private static readonly GUIContent s_copyAllContent = new GUIContent("\U0001F4CB Copy All", "Copy all material properties");
        private static readonly GUIContent s_pasteAllContent = new GUIContent("\U0001F4C4 Paste All", "Paste copied properties");
        private static readonly GUIContent s_copyContent = new GUIContent("C", "Copy this section");
        private static readonly GUIContent s_pasteContent = new GUIContent("P", "Paste section");
        private static readonly GUIContent s_createContent = new GUIContent("Create", "Create preset from material");
        private static readonly GUIContent s_browseContent = new GUIContent("Browse", "Open presets folder");
        private static readonly GUIContent s_applyContent = new GUIContent("Apply Preset", "Apply selected preset");
        private static readonly GUIContent s_searchContent = new GUIContent("Search:");
        
        // Cached property organization (reused each frame to avoid GC)
        private static Dictionary<string, List<MaterialProperty>> s_organizedProps = null;
        private static bool s_organizedPropsInitialized = false;
        
        // ========================================
        // COPY/PASTE SYSTEM
        // ========================================
        private static Dictionary<string, object> s_copiedProperties = null;
        private static Dictionary<string, Dictionary<string, object>> s_copiedSections = new Dictionary<string, Dictionary<string, object>>();
        
        // ========================================
        // PRESET SYSTEM
        // ========================================
        private static SB_GlassPreset s_selectedPreset = null;
        private static bool s_showPresetSection = true;
        private const string PRESET_FOLDER = "Assets/SingularBear/GlassShader/Presets";
        
        // ========================================
        // CATEGORIES
        // ========================================
        private enum Category { All, Base, Optical, Surface, Effects, Rendering }
        private enum ExpandType { All, Active, Collapse, Keep }

        private static Texture2D s_gradientTex;

        private static readonly Dictionary<string, Category> sectionCategories = new Dictionary<string, Category>()
        {
            // Base
            {"Main Surface", Category.Base},
            {"Normal Mapping", Category.Base},
            {"Detail Maps", Category.Base},
            {"Quality", Category.Base},
            {"Transparency", Category.Base},
            
            // Optical
            {"Refraction", Category.Optical},
            {"Blur", Category.Optical},
            {"Reflection", Category.Optical},
            {"Fresnel", Category.Optical},
            {"Iridescence", Category.Optical},
            
            // Surface
            {"Surface Noise", Category.Surface},
            {"Tint Texture", Category.Surface},
            {"Edge Darkening", Category.Surface},
            {"Inner Glow", Category.Surface},
            {"Thickness", Category.Surface},
            {"Depth Fade", Category.Surface},
            {"Rain", Category.Surface},
            {"Mapping Mode", Category.Surface},              // Rain sub-header
            {"Tiling and Direction", Category.Surface}, // Rain sub-header
            {"Animation", Category.Surface},            // Rain sub-header
            {"Effect Strength", Category.Surface},      // Rain sub-header
            {"Rim Lighting", Category.Surface},
            {"Specular", Category.Surface},
            {"Shape", Category.Surface},                // Specular sub-header
            {"Stylized", Category.Surface},             // Specular sub-header
            {"Specular Options", Category.Surface},     // Specular sub-header
            
            // Effects
            {"Translucency", Category.Effects},
            {"Occlusion", Category.Effects},
            {"Emission", Category.Effects},
            {"Triplanar", Category.Effects},
            {"Advanced", Category.Effects},     // Triplanar sub-header
            {"Absorption", Category.Effects},
            {"Caustics", Category.Effects},
            {"Total Internal Reflection", Category.Effects},
            {"Sparkle", Category.Effects},
            {"Dirt/Moss", Category.Effects},
            {"Damage", Category.Effects},
            {"Decals", Category.Effects},
            {"Decal 1", Category.Effects},
            {"Decal 2", Category.Effects},
            {"Decal 3", Category.Effects},
            {"Decal 4", Category.Effects},
            {"Fingerprints", Category.Effects},
            {"Slot 1", Category.Effects},
            {"Slot 2", Category.Effects},
            {"Slot 3", Category.Effects},
            {"Slot 4", Category.Effects},
            {"Distortion FX", Category.Effects},
            {"Magnify", Category.Effects},
            {"Barrel", Category.Effects},
            {"Waves", Category.Effects},
            {"Ripple", Category.Effects},
            {"Swirl", Category.Effects},
            {"Heat Haze", Category.Effects},
            {"Pixelate", Category.Effects},
            
            // Rendering
            {"Shadows", Category.Rendering},
            {"Rendering", Category.Rendering},
        };

        // ========================================
        // KEYWORD MANAGEMENT
        // ========================================
        private static readonly List<(string keyword, string propName)> autoKeywordProps = new List<(string, string)>()
        {
            // Main
            ("_SB_METALLICGLOSS_MAP", "_UseMetallicMap"),
            ("_SB_NORMALMAP", "_UseNormalMap"),
            ("_SB_DETAIL_ALBEDO", "_UseDetailAlbedo"),
            ("_SB_DETAIL_NORMAL", "_UseDetailNormal"),
            ("_SB_DETAIL_NORMAL_TRIPLANAR", "_UseDetailNormalTriplanar"),
            
            // Refraction
            ("_SB_REFRACTION", "_UseRefraction"),
            ("_SB_IOR", "_UseIOR"),
            ("_SB_CHROMATIC_ABERRATION", "_UseChromatic"),
            ("_SB_BLUR", "_UseBlur"),
            
            // Reflection
            ("_SB_REFLECTION", "_UseReflection"),
            ("_SB_REFLECTION_CUBEMAP", "_UseCubemap"),
            
            // Effects
            ("_SB_IRIDESCENCE", "_UseIridescence"),
            ("_SB_FRESNEL", "_UseFresnel"),
            ("_SB_SPECULAR", "_UseSpecular"),
            ("_SB_RIM", "_UseRim"),
            ("_SB_TRANSLUCENT", "_UseTranslucent"),
            ("_SB_OCCLUSION_MAP", "_UseOcclusion"),
            ("_SB_EMISSION", "_UseEmission"),
            ("_SB_EMISSION_MAP", "_UseEmissionMap"),
            ("_SB_ALPHA_CLIP", "_UseAlphaClip"),
            ("_SB_FALLOFF_OPACITY", "_UseFalloffOpacity"),
            
            // Surface
            ("_SB_SURFACE_NOISE", "_UseSurfaceNoise"),
            ("_SB_TINT_TEXTURE", "_UseTintTexture"),
            ("_SB_EDGE_DARKENING", "_UseEdgeDarkening"),
            ("_SB_INNER_GLOW", "_UseInnerGlow"),
            ("_SB_THICKNESS_MAP", "_UseThicknessMap"),
            ("_SB_DEPTH_FADE", "_UseDepthFade"),
            ("_SB_RAIN", "_UseRain"),
            ("_SB_RAIN_TRIPLANAR", "_UseRainTriplanar"),
            ("_SB_TRIPLANAR", "_UseTriplanar"),
            
            // Advanced
            ("_SB_ABSORPTION", "_UseAbsorption"),
            ("_SB_CAUSTICS", "_UseCaustics"),
            ("_SB_CAUSTICS_PROCEDURAL", "_UseCausticsProcedural"),
            ("_SB_TIR", "_UseTIR"),
            ("_SB_SPARKLE", "_UseSparkle"),
            ("_SB_DUST", "_UseDust"),
            ("_SB_DUST_TRIPLANAR", "_UseDustTriplanar"),
            
            // Damage
            ("_SB_DAMAGE", "_UseDamage"),
            
            // Decals
            ("_SB_DECALS", "_UseDecals"),
            
            // Fingerprints
            ("_SB_FINGERPRINTS", "_UseFingerprints"),
            // Note: _SB_FINGERPRINTS_TRIPLANAR is handled manually based on Mapping Mode
            
            // Distortion FX
            ("_SB_DISTORTION", "_UseDistortion"),
            ("_SB_MAGNIFY", "_UseMagnify"),
            ("_SB_BARREL", "_UseBarrel"),
            ("_SB_WAVES", "_UseWaves"),
            ("_SB_RIPPLE", "_UseRipple"),
            ("_SB_SWIRL", "_UseSwirl"),
            ("_SB_HEAT_HAZE", "_UseHeatHaze"),
            ("_SB_PIXELATE", "_UsePixelate"),
            
            // Shadows
            ("_SB_RECEIVE_SHADOWS", "_ReceiveShadows"),
        };

        // ========================================
        // PROPERTY MAPPING
        // ========================================
        private static readonly Dictionary<string, (string header, string feature)> propertyMapping = new Dictionary<string, (string, string)>()
        {
            // ============ QUALITY ============
            {"_SB_Quality", ("Quality", null)},
            
            // ============ MAIN SURFACE ============
            {"_Color", ("Main Surface", null)},
            {"_MainTex", ("Main Surface", null)},
            {"_MainTint", ("Main Surface", null)},
            {"_Metallic", ("Main Surface", null)},
            {"_Smoothness", ("Main Surface", null)},
            {"_Saturation", ("Main Surface", null)},
            {"_Brightness", ("Main Surface", null)},
            {"_UseMetallicMap", ("Main Surface", null)},
            {"_MetallicGlossMap", ("Main Surface", "_UseMetallicMap")},
            
            // ============ NORMAL MAPPING ============
            {"_UseNormalMap", ("Normal Mapping", null)},
            {"_BumpMap", ("Normal Mapping", "_UseNormalMap")},
            {"_BumpScale", ("Normal Mapping", "_UseNormalMap")},
            
            // ============ DETAIL MAPS ============
            {"_UseDetailAlbedo", ("Detail Maps", null)},
            {"_DetailAlbedoMap", ("Detail Maps", "_UseDetailAlbedo")},
            {"_DetailColor", ("Detail Maps", "_UseDetailAlbedo")},
            {"_DetailTiling", ("Detail Maps", "_UseDetailAlbedo")},
            {"_DetailAlbedoIntensity", ("Detail Maps", "_UseDetailAlbedo")},
            {"_UseDetailNormal", ("Detail Maps", null)},
            {"_DetailNormalMap", ("Detail Maps", "_UseDetailNormal")},
            {"_DetailNormalScale", ("Detail Maps", "_UseDetailNormal")},
            {"_DetailNormalTiling", ("Detail Maps", "_UseDetailNormal")},
            {"_UseDetailNormalTriplanar", ("Detail Maps", "_UseDetailNormal")},
            {"_DetailNormalTriplanarScale", ("Detail Maps", "_UseDetailNormalTriplanar")},
            {"_DetailNormalTriplanarSharpness", ("Detail Maps", "_UseDetailNormalTriplanar")},
            
            // ============ REFRACTION ============
            {"_UseRefraction", ("Refraction", null)},
            {"_Distortion", ("Refraction", "_UseRefraction")},
            {"_FlipRefraction", ("Refraction", "_UseRefraction")},
            {"_UseIOR", ("Refraction", "_UseRefraction")},
            {"_IndexOfRefraction", ("Refraction", "_UseIOR")},
            {"_IOROrigin", ("Refraction", "_UseIOR")},
            {"_IOROriginPreset", ("Refraction", "_UseIOR")},
            {"_UseChromatic", ("Refraction", "_UseRefraction")},
            {"_ChromaticAberration", ("Refraction", "_UseChromatic")},
            {"_UseBlur", ("Blur", null)},
            {"_BlurStrength", ("Blur", "_UseBlur")},
            {"_BlurRadius", ("Blur", "_UseBlur")},
            {"_BlurQuality", ("Blur", "_UseBlur")},
            
            // ============ REFLECTION ============
            {"_UseReflection", ("Reflection", null)},
            {"_ReflectionColor", ("Reflection", "_UseReflection")},
            {"_ReflectionIntensity", ("Reflection", "_UseReflection")},
            {"_ReflectionBlur", ("Reflection", "_UseReflection")},
            {"_FlipReflection", ("Reflection", "_UseReflection")},
            {"_UseCubemap", ("Reflection", "_UseReflection")},
            {"_ReflectionCube", ("Reflection", "_UseCubemap")},
            
            // ============ IRIDESCENCE ============
            {"_UseIridescence", ("Iridescence", null)},
            {"_IridescenceColor", ("Iridescence", "_UseIridescence")},
            {"_IridescenceStrength", ("Iridescence", "_UseIridescence")},
            {"_IridescenceScale", ("Iridescence", "_UseIridescence")},
            {"_IridescenceShift", ("Iridescence", "_UseIridescence")},
            {"_IridescenceSpeed", ("Iridescence", "_UseIridescence")},
            
            // ============ FRESNEL ============
            {"_UseFresnel", ("Fresnel", null)},
            {"_FresnelColor", ("Fresnel", "_UseFresnel")},
            {"_FresnelPower", ("Fresnel", "_UseFresnel")},
            {"_FresnelIntensity", ("Fresnel", "_UseFresnel")},
            {"_FresnelMin", ("Fresnel", "_UseFresnel")},
            {"_FresnelMax", ("Fresnel", "_UseFresnel")},
            {"_FresnelInvert", ("Fresnel", "_UseFresnel")},
            {"_FresnelAffectAlpha", ("Fresnel", "_UseFresnel")},
            {"_FresnelAffectReflection", ("Fresnel", "_UseFresnel")},
            
            // ============ RIM LIGHTING ============
            {"_UseRim", ("Rim Lighting", null)},
            {"_RimColor", ("Rim Lighting", "_UseRim")},
            {"_RimPower", ("Rim Lighting", "_UseRim")},
            {"_RimIntensity", ("Rim Lighting", "_UseRim")},
            {"_RimMin", ("Rim Lighting", "_UseRim")},
            {"_RimMax", ("Rim Lighting", "_UseRim")},
            
            // ============ SPECULAR ============
            {"_UseSpecular", ("Specular", null)},
            {"_SpecularColor", ("Specular", "_UseSpecular")},
            {"_SpecularIntensity", ("Specular", "_UseSpecular")},
            {"_SpecularSize", ("Specular", "_UseSpecular")},
            {"_SpecularSmoothness", ("Specular", "_UseSpecular")},
            {"_SpecularHardness", ("Specular", "_UseSpecular")},
            {"_SpecularToon", ("Specular", "_UseSpecular")},
            {"_SpecularSteps", ("Specular", "_UseSpecular")},
            {"_SpecularThreshold", ("Specular", "_UseSpecular")},
            {"_SpecularFresnel", ("Specular", "_UseSpecular")},
            {"_SpecularAnisotropy", ("Specular", "_UseSpecular")},
            {"_DiffuseIntensity", ("Specular", null)},
            
            // ============ TRANSLUCENCY ============
            {"_UseTranslucent", ("Translucency", null)},
            {"_TranslucentColor", ("Translucency", "_UseTranslucent")},
            {"_TranslucentIntensity", ("Translucency", "_UseTranslucent")},
            {"_TranslucentPower", ("Translucency", "_UseTranslucent")},
            {"_TranslucentDistortion", ("Translucency", "_UseTranslucent")},
            {"_TranslucentScale", ("Translucency", "_UseTranslucent")},
            
            // ============ OCCLUSION ============
            {"_UseOcclusion", ("Occlusion", null)},
            {"_OcclusionMap", ("Occlusion", "_UseOcclusion")},
            {"_OcclusionStrength", ("Occlusion", "_UseOcclusion")},
            
            // ============ EMISSION ============
            {"_UseEmission", ("Emission", null)},
            {"_EmissionColor", ("Emission", "_UseEmission")},
            {"_UseEmissionMap", ("Emission", "_UseEmission")},
            {"_EmissionMap", ("Emission", "_UseEmissionMap")},
            {"_EmissionIntensity", ("Emission", "_UseEmission")},
            
            // ============ TRANSPARENCY ============
            {"_Opacity", ("Transparency", null)},
            {"_UseAlphaClip", ("Transparency", null)},
            {"_AlphaClip", ("Transparency", "_UseAlphaClip")},
            {"_UseFalloffOpacity", ("Transparency", null)},
            {"_FalloffOpacityIntensity", ("Transparency", "_UseFalloffOpacity")},
            {"_FalloffOpacityPower", ("Transparency", "_UseFalloffOpacity")},
            {"_FalloffOpacityInvert", ("Transparency", "_UseFalloffOpacity")},
            
            // ============ SURFACE NOISE ============
            {"_UseSurfaceNoise", ("Surface Noise", null)},
            {"_SurfaceNoiseScale", ("Surface Noise", "_UseSurfaceNoise")},
            {"_SurfaceNoiseStrength", ("Surface Noise", "_UseSurfaceNoise")},
            {"_SurfaceNoiseDistortion", ("Surface Noise", "_UseSurfaceNoise")},
            {"_SurfaceNoiseSpeed", ("Surface Noise", "_UseSurfaceNoise")},
            
            // ============ TINT TEXTURE ============
            {"_UseTintTexture", ("Tint Texture", null)},
            {"_TintTexture", ("Tint Texture", "_UseTintTexture")},
            {"_TintTextureColor", ("Tint Texture", "_UseTintTexture")},
            {"_TintTextureStrength", ("Tint Texture", "_UseTintTexture")},
            {"_TintTextureBlend", ("Tint Texture", "_UseTintTexture")},
            {"_TintDistortionAmount", ("Tint Texture", "_UseTintTexture")},
            
            // ============ EDGE DARKENING ============
            {"_UseEdgeDarkening", ("Edge Darkening", null)},
            {"_EdgeDarkeningStrength", ("Edge Darkening", "_UseEdgeDarkening")},
            {"_EdgeDarkeningPower", ("Edge Darkening", "_UseEdgeDarkening")},
            {"_EdgeDarkeningDistance", ("Edge Darkening", "_UseEdgeDarkening")},
            
            // ============ INNER GLOW ============
            {"_UseInnerGlow", ("Inner Glow", null)},
            {"_InnerGlowColor", ("Inner Glow", "_UseInnerGlow")},
            {"_InnerGlowStrength", ("Inner Glow", "_UseInnerGlow")},
            {"_InnerGlowPower", ("Inner Glow", "_UseInnerGlow")},
            {"_InnerGlowFalloff", ("Inner Glow", "_UseInnerGlow")},
            
            // ============ THICKNESS ============
            {"_UseThicknessMap", ("Thickness", null)},
            {"_ThicknessMap", ("Thickness", "_UseThicknessMap")},
            {"_ThicknessMin", ("Thickness", "_UseThicknessMap")},
            {"_ThicknessMax", ("Thickness", "_UseThicknessMap")},
            {"_ThicknessAffectsColor", ("Thickness", "_UseThicknessMap")},
            {"_ThicknessAffectsDistortion", ("Thickness", "_UseThicknessMap")},
            
            // ============ DEPTH FADE ============
            {"_UseDepthFade", ("Depth Fade", null)},
            {"_DepthFadeDistance", ("Depth Fade", "_UseDepthFade")},
            {"_DepthFadeColor", ("Depth Fade", "_UseDepthFade")},
            
            // ============ TRIPLANAR ============
            {"_UseTriplanar", ("Triplanar", null)},
            {"_TriplanarScale", ("Triplanar", "_UseTriplanar")},
            {"_TriplanarSharpness", ("Triplanar", "_UseTriplanar")},
            
            // ============ ABSORPTION ============
            {"_UseAbsorption", ("Absorption", null)},
            {"_AbsorptionColor", ("Absorption", "_UseAbsorption")},
            {"_AbsorptionDensity", ("Absorption", "_UseAbsorption")},
            {"_AbsorptionFalloff", ("Absorption", "_UseAbsorption")},
            
            // ============ CAUSTICS ============
            {"_UseCaustics", ("Caustics", null)},
            {"_UseCausticsProcedural", ("Caustics", "_UseCaustics")},
            {"_CausticsTexture", ("Caustics", "_UseCaustics")},
            {"_CausticsColor", ("Caustics", "_UseCaustics")},
            {"_CausticsIntensity", ("Caustics", "_UseCaustics")},
            {"_CausticsScale", ("Caustics", "_UseCaustics")},
            {"_CausticsSpeed", ("Caustics", "_UseCaustics")},
            {"_CausticsDistortion", ("Caustics", "_UseCaustics")},
            
            // ============ TIR ============
            {"_UseTIR", ("Total Internal Reflection", null)},
            {"_TIRIntensity", ("Total Internal Reflection", "_UseTIR")},
            {"_TIRCriticalAngle", ("Total Internal Reflection", "_UseTIR")},
            {"_TIRSharpness", ("Total Internal Reflection", "_UseTIR")},
            
            // ============ SPARKLE ============
            {"_UseSparkle", ("Sparkle", null)},
            {"_SparkleColor", ("Sparkle", "_UseSparkle")},
            {"_SparkleIntensity", ("Sparkle", "_UseSparkle")},
            {"_SparkleScale", ("Sparkle", "_UseSparkle")},
            {"_SparkleSpeed", ("Sparkle", "_UseSparkle")},
            {"_SparkleDensity", ("Sparkle", "_UseSparkle")},
            {"_SparkleSize", ("Sparkle", "_UseSparkle")},
            
            // ============ DIRT/MOSS ============
            {"_UseDust", ("Dirt/Moss", null)},
            {"_DirtDirection", ("Dirt/Moss", "_UseDust")},
            {"_DirtHeight", ("Dirt/Moss", "_UseDust")},
            {"_DirtSpread", ("Dirt/Moss", "_UseDust")},
            {"_DirtSoftness", ("Dirt/Moss", "_UseDust")},
            {"_DustTexture", ("Dirt/Moss", "_UseDust")},
            {"_DustTiling", ("Dirt/Moss", "_UseDust")},
            {"_DustColor", ("Dirt/Moss", "_UseDust")},
            {"_DirtColorVariation", ("Dirt/Moss", "_UseDust")},
            {"_DirtVariationScale", ("Dirt/Moss", "_UseDust")},
            {"_DustIntensity", ("Dirt/Moss", "_UseDust")},
            {"_DustCoverage", ("Dirt/Moss", "_UseDust")},
            {"_DirtFullOpacity", ("Dirt/Moss", "_UseDust")},
            {"_DustRoughness", ("Dirt/Moss", "_UseDust")},
            {"_DustNormalBlend", ("Dirt/Moss", "_UseDust")},
            {"_DirtUseEdgeNoise", ("Dirt/Moss", "_UseDust")},
            {"_DirtEdgeNoiseScale", ("Dirt/Moss", "_DirtUseEdgeNoise")},
            {"_DirtEdgeNoiseStrength", ("Dirt/Moss", "_DirtUseEdgeNoise")},
            {"_DustEdgeFalloff", ("Dirt/Moss", "_UseDust")},
            {"_DustEdgePower", ("Dirt/Moss", "_DustEdgeFalloff")},
            {"_UseDustTriplanar", ("Dirt/Moss", "_UseDust")},
            {"_DustTriplanarScale", ("Dirt/Moss", "_UseDustTriplanar")},
            {"_DustTriplanarSharpness", ("Dirt/Moss", "_UseDustTriplanar")},
            {"_DustTriplanarRotation", ("Dirt/Moss", "_UseDustTriplanar")},
            
            // ============ DAMAGE ============
            {"_UseDamage", ("Damage", null)},
            {"_DamageProgression", ("Damage", "_UseDamage")},
            {"_ProceduralCrackDensity", ("Damage", "_UseDamage")},
            {"_ProceduralCrackSeed", ("Damage", "_UseDamage")},
            {"_CrackWidth", ("Damage", "_UseDamage")},
            {"_CrackSharpness", ("Damage", "_UseDamage")},
            {"_CrackColor", ("Damage", "_UseDamage")},
            {"_CrackDepth", ("Damage", "_UseDamage")},
            {"_CrackEmission", ("Damage", "_UseDamage")},
            {"_ShatterDistortion", ("Damage", "_UseDamage")},
            {"_DamageMask", ("Damage", "_UseDamage")},
            {"_CrackNormalMap", ("Damage", "_UseDamage")},
            
            // ============ RAIN ============
            {"_UseRain", ("Rain", null)},
            {"_RainTexture", ("Rain", "_UseRain")},
            {"_RainIntensity", ("Rain", "_UseRain")},
            {"_RainTiling", ("Rain", "_UseRain")},
            {"_RainOffset", ("Rain", "_UseRain")},
            {"_RainRotation", ("Rain", "_UseRain")},
            {"_RainSpeed", ("Rain", "_UseRain")},
            {"_RainNormalStrength", ("Rain", "_UseRain")},
            {"_RainDistortion", ("Rain", "_UseRain")},
            {"_RainWetness", ("Rain", "_UseRain")},
            
            // ============ DECALS ============
            {"_UseDecals", ("Decals", null)},
            {"_DecalTexture1", ("Decal 1", "_UseDecals")},
            {"_DecalPosition1", ("Decal 1", "_UseDecals")},
            {"_DecalSize1", ("Decal 1", "_UseDecals")},
            {"_DecalRotation1", ("Decal 1", "_UseDecals")},
            {"_DecalIntensity1", ("Decal 1", "_UseDecals")},
            {"_DecalTint1", ("Decal 1", "_UseDecals")},
            {"_UseDecal2", ("Decal 2", "_UseDecals")},
            {"_DecalTexture2", ("Decal 2", "_UseDecal2")},
            {"_DecalPosition2", ("Decal 2", "_UseDecal2")},
            {"_DecalSize2", ("Decal 2", "_UseDecal2")},
            {"_DecalRotation2", ("Decal 2", "_UseDecal2")},
            {"_DecalIntensity2", ("Decal 2", "_UseDecal2")},
            {"_DecalTint2", ("Decal 2", "_UseDecal2")},
            {"_UseDecal3", ("Decal 3", "_UseDecals")},
            {"_DecalTexture3", ("Decal 3", "_UseDecal3")},
            {"_DecalPosition3", ("Decal 3", "_UseDecal3")},
            {"_DecalSize3", ("Decal 3", "_UseDecal3")},
            {"_DecalRotation3", ("Decal 3", "_UseDecal3")},
            {"_DecalIntensity3", ("Decal 3", "_UseDecal3")},
            {"_DecalTint3", ("Decal 3", "_UseDecal3")},
            {"_UseDecal4", ("Decal 4", "_UseDecals")},
            {"_DecalTexture4", ("Decal 4", "_UseDecal4")},
            {"_DecalPosition4", ("Decal 4", "_UseDecal4")},
            {"_DecalSize4", ("Decal 4", "_UseDecal4")},
            {"_DecalRotation4", ("Decal 4", "_UseDecal4")},
            {"_DecalIntensity4", ("Decal 4", "_UseDecal4")},
            {"_DecalTint4", ("Decal 4", "_UseDecal4")},
            
            // ============ FINGERPRINTS ============
            {"_UseFingerprints", ("Fingerprints", null)},
            {"_FingerprintTint", ("Fingerprints", "_UseFingerprints")},
            // Slot 1
            {"_FingerprintTexture1", ("Slot 1", "_UseFingerprints")},
            {"_FingerprintMapping1", ("Slot 1", "_UseFingerprints")},
            {"_FingerprintPos1", ("Slot 1", "_UseFingerprints")},
            {"_FingerprintScale1", ("Slot 1", "_UseFingerprints")},
            {"_FingerprintWorldPos1", ("Slot 1", "_UseFingerprints")},
            {"_FingerprintWorldRadius1", ("Slot 1", "_UseFingerprints")},
            {"_FingerprintTriplanarScale1", ("Slot 1", "_UseFingerprints")},
            {"_FingerprintRotation1", ("Slot 1", "_UseFingerprints")},
            {"_FingerprintIntensity1", ("Slot 1", "_UseFingerprints")},
            {"_FingerprintRoughness1", ("Slot 1", "_UseFingerprints")},
            {"_FingerprintFalloff1", ("Slot 1", "_UseFingerprints")},
            // Slot 2
            {"_UseSlot2", ("Slot 2", "_UseFingerprints")},
            {"_FingerprintTexture2", ("Slot 2", "_UseSlot2")},
            {"_FingerprintMapping2", ("Slot 2", "_UseSlot2")},
            {"_FingerprintPos2", ("Slot 2", "_UseSlot2")},
            {"_FingerprintScale2", ("Slot 2", "_UseSlot2")},
            {"_FingerprintWorldPos2", ("Slot 2", "_UseSlot2")},
            {"_FingerprintWorldRadius2", ("Slot 2", "_UseSlot2")},
            {"_FingerprintTriplanarScale2", ("Slot 2", "_UseSlot2")},
            {"_FingerprintRotation2", ("Slot 2", "_UseSlot2")},
            {"_FingerprintIntensity2", ("Slot 2", "_UseSlot2")},
            {"_FingerprintRoughness2", ("Slot 2", "_UseSlot2")},
            {"_FingerprintFalloff2", ("Slot 2", "_UseSlot2")},
            // Slot 3
            {"_UseSlot3", ("Slot 3", "_UseFingerprints")},
            {"_FingerprintTexture3", ("Slot 3", "_UseSlot3")},
            {"_FingerprintMapping3", ("Slot 3", "_UseSlot3")},
            {"_FingerprintPos3", ("Slot 3", "_UseSlot3")},
            {"_FingerprintScale3", ("Slot 3", "_UseSlot3")},
            {"_FingerprintWorldPos3", ("Slot 3", "_UseSlot3")},
            {"_FingerprintWorldRadius3", ("Slot 3", "_UseSlot3")},
            {"_FingerprintTriplanarScale3", ("Slot 3", "_UseSlot3")},
            {"_FingerprintRotation3", ("Slot 3", "_UseSlot3")},
            {"_FingerprintIntensity3", ("Slot 3", "_UseSlot3")},
            {"_FingerprintRoughness3", ("Slot 3", "_UseSlot3")},
            {"_FingerprintFalloff3", ("Slot 3", "_UseSlot3")},
            // Slot 4
            {"_UseSlot4", ("Slot 4", "_UseFingerprints")},
            {"_FingerprintTexture4", ("Slot 4", "_UseSlot4")},
            {"_FingerprintMapping4", ("Slot 4", "_UseSlot4")},
            {"_FingerprintPos4", ("Slot 4", "_UseSlot4")},
            {"_FingerprintScale4", ("Slot 4", "_UseSlot4")},
            {"_FingerprintWorldPos4", ("Slot 4", "_UseSlot4")},
            {"_FingerprintWorldRadius4", ("Slot 4", "_UseSlot4")},
            {"_FingerprintTriplanarScale4", ("Slot 4", "_UseSlot4")},
            {"_FingerprintRotation4", ("Slot 4", "_UseSlot4")},
            {"_FingerprintIntensity4", ("Slot 4", "_UseSlot4")},
            {"_FingerprintRoughness4", ("Slot 4", "_UseSlot4")},
            {"_FingerprintFalloff4", ("Slot 4", "_UseSlot4")},
            
            // ============ DISTORTION FX ============
            {"_UseDistortion", ("Distortion FX", null)},
            
            // Magnify
            {"_UseMagnify", ("Magnify", "_UseDistortion")},
            {"_MagnifyStrength", ("Magnify", "_UseMagnify")},
            {"_MagnifyCenter", ("Magnify", "_UseMagnify")},
            {"_MagnifyRadius", ("Magnify", "_UseMagnify")},
            {"_MagnifyFalloff", ("Magnify", "_UseMagnify")},
            
            // Barrel
            {"_UseBarrel", ("Barrel", "_UseDistortion")},
            {"_BarrelStrength", ("Barrel", "_UseBarrel")},
            
            // Waves
            {"_UseWaves", ("Waves", "_UseDistortion")},
            {"_WaveAmplitude", ("Waves", "_UseWaves")},
            {"_WaveFrequency", ("Waves", "_UseWaves")},
            {"_WaveSpeed", ("Waves", "_UseWaves")},
            {"_WaveRadial", ("Waves", "_UseWaves")},
            
            // Ripple
            {"_UseRipple", ("Ripple", "_UseDistortion")},
            {"_RippleCenter", ("Ripple", "_UseRipple")},
            {"_RippleAmplitude", ("Ripple", "_UseRipple")},
            {"_RippleFrequency", ("Ripple", "_UseRipple")},
            {"_RippleSpeed", ("Ripple", "_UseRipple")},
            {"_RippleDecay", ("Ripple", "_UseRipple")},
            
            // Swirl
            {"_UseSwirl", ("Swirl", "_UseDistortion")},
            {"_SwirlCenter", ("Swirl", "_UseSwirl")},
            {"_SwirlStrength", ("Swirl", "_UseSwirl")},
            {"_SwirlRadius", ("Swirl", "_UseSwirl")},
            {"_SwirlSpeed", ("Swirl", "_UseSwirl")},
            
            // Heat Haze
            {"_UseHeatHaze", ("Heat Haze", "_UseDistortion")},
            {"_HeatHazeStrength", ("Heat Haze", "_UseHeatHaze")},
            {"_HeatHazeSpeed", ("Heat Haze", "_UseHeatHaze")},
            {"_HeatHazeScale", ("Heat Haze", "_UseHeatHaze")},
            
            // Pixelate
            {"_UsePixelate", ("Pixelate", "_UseDistortion")},
            {"_PixelateSize", ("Pixelate", "_UsePixelate")},
            
            // ============ SHADOWS ============
            {"_ReceiveShadows", ("Shadows", null)},
            {"_ShadowIntensity", ("Shadows", "_ReceiveShadows")},
            
            // ============ RENDERING ============
            {"_BlendMode", ("Rendering", null)},
            {"_Cull", ("Rendering", null)},
            {"_ZWrite", ("Rendering", null)},
            {"_ZTest", ("Rendering", null)},
        };

        // ========================================
        // HEADER ORDER
        // ========================================
        private static readonly List<string> headerOrder = new List<string>()
        {
            // === BASE ===
            "Quality",
            "Main Surface",
            "Transparency",
            "Normal Mapping",
            "Detail Maps",
            
            // === OPTICAL ===
            "Refraction",
            "Blur",
            "Reflection",
            "Fresnel",
            "Iridescence",
            
            // === SURFACE ===
            "Surface Noise",
            "Tint Texture",
            "Edge Darkening",
            "Inner Glow",
            "Thickness",
            "Depth Fade",
            "Rain",
            
            // === ADVANCED ===
            "Rim Lighting",
            "Specular",
            "Translucency",
            "Occlusion",
            "Emission",
            "Triplanar",
            "Absorption",
            "Caustics",
            "Total Internal Reflection",
            "Sparkle",
            "Dirt/Moss",
            "Damage",
            
            // === DECALS ===
            "Decals",
            "Decal 1",
            "Decal 2",
            "Decal 3",
            "Decal 4",
            
            // === FINGERPRINTS ===
            "Fingerprints",
            "Slot 1",
            "Slot 2",
            "Slot 3",
            "Slot 4",
            
            // === EFFECTS ===
            "Distortion FX",
            "Magnify",
            "Barrel",
            "Waves",
            "Ripple",
            "Swirl",
            "Heat Haze",
            "Pixelate",
            
            // === RENDERING ===
            "Shadows",
            "Rendering",
        };

        // Family mapping for section titles
        private static string GetFamilyForSection(string sectionName)
        {
            if (!sectionCategories.ContainsKey(sectionName))
                return null;

            var cat = sectionCategories[sectionName];
            switch (cat)
            {
                case Category.Base: return "BASE";
                case Category.Optical: return "OPTICAL";
                case Category.Surface: return "SURFACE";
                case Category.Effects: return "EFFECTS";
                case Category.Rendering: return "RENDERING";
                default: return null;
            }
        }

        // ========================================
        // STATE
        // ========================================
        private Category currentCategory = Category.All;
        private string searchQuery = "";
        private ExpandType forceExpand = ExpandType.Keep;
        private bool hideUnusedSections = false;
        private const string PREF_PREFIX = "SBGlass_";
        
        // ========================================
        // PER-MATERIAL STATE STORAGE (Persistent via EditorPrefs)
        // ========================================
        private static string s_currentMaterialGUID = null;
        private const string STATE_PREFIX = "SBGlass_State_";
        
        private string GetMaterialGUID(Material mat)
        {
            if (mat == null) return null;
            string path = AssetDatabase.GetAssetPath(mat);
            if (string.IsNullOrEmpty(path)) return mat.GetInstanceID().ToString(); // Runtime material
            return AssetDatabase.AssetPathToGUID(path);
        }
        
        private void SaveMaterialState(Material mat)
        {
            string guid = GetMaterialGUID(mat);
            if (string.IsNullOrEmpty(guid)) return;
            
            // Save UI state to EditorPrefs
            EditorPrefs.SetInt(STATE_PREFIX + guid + "_category", (int)currentCategory);
            EditorPrefs.SetBool(STATE_PREFIX + guid + "_hideUnused", hideUnusedSections);
            EditorPrefs.SetBool(STATE_PREFIX + guid + "_showPreset", s_showPresetSection);
        }
        
        private void LoadMaterialState(Material mat)
        {
            string guid = GetMaterialGUID(mat);
            if (string.IsNullOrEmpty(guid)) return;
            
            // Check if material changed
            if (s_currentMaterialGUID == guid) return;
            
            s_currentMaterialGUID = guid;
            
            // Load state from EditorPrefs
            if (EditorPrefs.HasKey(STATE_PREFIX + guid + "_category"))
            {
                currentCategory = (Category)EditorPrefs.GetInt(STATE_PREFIX + guid + "_category", 0);
                hideUnusedSections = EditorPrefs.GetBool(STATE_PREFIX + guid + "_hideUnused", false);
                s_showPresetSection = EditorPrefs.GetBool(STATE_PREFIX + guid + "_showPreset", true);
            }
            else
            {
                // New material - use defaults
                currentCategory = Category.All;
                hideUnusedSections = false;
                s_showPresetSection = true;
            }
        }
        
        // Get/Set foldout state per material
        private bool GetFoldoutState(string headerName, string materialGUID)
        {
            return EditorPrefs.GetBool(STATE_PREFIX + materialGUID + "_foldout_" + headerName, false);
        }
        
        private void SetFoldoutState(string headerName, string materialGUID, bool state)
        {
            EditorPrefs.SetBool(STATE_PREFIX + materialGUID + "_foldout_" + headerName, state);
        }

        // Effect toggles for active count
        private static readonly HashSet<string> s_effectToggles = new HashSet<string>()
        {
            "_UseMetallicMap", "_UseNormalMap", "_UseDetailAlbedo", "_UseDetailNormal",
            "_UseIOR", "_UseChromatic", "_UseBlur", "_UseReflection", "_UseCubemap",
            "_UseIridescence", "_UseRim", "_UseTranslucent", "_UseOcclusion", "_UseEmission",
            "_UseEmissionMap", "_UseAlphaClip", "_UseSurfaceNoise", "_UseTintTexture",
            "_UseEdgeDarkening", "_UseInnerGlow", "_UseThicknessMap", "_UseDepthFade", "_UseRain",
            "_UseTriplanar", "_UseAbsorption", "_UseCaustics", "_UseTIR", "_UseSparkle",
            "_UseDust", "_UseDecals", "_UseFingerprints", "_UseDistortion", "_UseMagnify",
            "_UseBarrel", "_UseWaves", "_UseRipple", "_UseSwirl", "_UseHeatHaze", "_UsePixelate"
        };

        // Cached GUIStyles
        private static GUIStyle s_headerStyle;
        private static GUIStyle s_familyStyle;
        private static GUIStyle s_centeredWhiteStyle;

        private static GUIStyle HeaderStyle
        {
            get
            {
                if (s_headerStyle == null)
                {
                    s_headerStyle = new GUIStyle(EditorStyles.boldLabel)
                    {
                        fontSize = 16,
                        alignment = TextAnchor.MiddleCenter
                    };
                }
                return s_headerStyle;
            }
        }

        private static GUIStyle FamilyStyle
        {
            get
            {
                if (s_familyStyle == null)
                {
                    s_familyStyle = new GUIStyle(EditorStyles.boldLabel)
                    {
                        fontSize = 13,
                        alignment = TextAnchor.MiddleCenter,
                        normal = { textColor = new Color(0.7f, 0.7f, 0.7f, 1f) }
                    };
                }
                return s_familyStyle;
            }
        }

        private static GUIStyle CenteredWhiteStyle
        {
            get
            {
                if (s_centeredWhiteStyle == null)
                {
                    s_centeredWhiteStyle = new GUIStyle(EditorStyles.boldLabel)
                    {
                        alignment = TextAnchor.MiddleCenter,
                        normal = { textColor = Color.white }
                    };
                }
                return s_centeredWhiteStyle;
            }
        }

        // ========================================
        // MAIN GUI
        // ========================================
        public override void OnGUI(MaterialEditor materialEditor, MaterialProperty[] properties)
        {
            Material mat = materialEditor.target as Material;
            if (mat == null) return;

            // ============ LOAD PER-MATERIAL STATE ============
            LoadMaterialState(mat);

            // ============ BLEND MODE INITIALIZATION ============
            // Ensure blend values are correctly set on first load
            InitializeBlendMode(mat);

            // Track changes to refresh UI when toggles change
            EditorGUI.BeginChangeCheck();

            // ============ HEADER ============
            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("SingularBear Glass Shader V1", HeaderStyle);
            EditorGUILayout.LabelField("Physically-Based Glass \u2022 URP", EditorStyles.centeredGreyMiniLabel);
            EditorGUILayout.Space(5);

            // ============ DOCUMENTATION BUTTONS ============
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Documentation"))
            {
                string[] guids = AssetDatabase.FindAssets("GlassShader_ReadMe");
                if (guids.Length > 0)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                    Object asset = AssetDatabase.LoadAssetAtPath<Object>(path);
                    if (asset != null)
                    {
                        EditorGUIUtility.PingObject(asset);
                        Selection.activeObject = asset;
                    }
                }
                else
                {
                    EditorApplication.delayCall += () => 
                        EditorUtility.DisplayDialog("Documentation", "Look for 'GlassShader_ReadMe' in your project or check the Notion page.", "OK");
                }
            }
            if (GUILayout.Button("Notion Page"))
            {
                Application.OpenURL("https://www.notion.so/Glass-Shader-2df4315cbb4d8098bebfd86f3cc3994c");
            }
            EditorGUILayout.EndHorizontal();
            GUILayout.Space(3);

            // ============ PRESET SYSTEM ============
            DrawPresetSection(materialEditor, mat);
            GUILayout.Space(5);

            // ============ SEARCH BAR ============
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Search:", GUILayout.Width(50));
            searchQuery = EditorGUILayout.TextField(searchQuery, GUILayout.ExpandWidth(true));
            if (GUILayout.Button("\u2715", GUILayout.Width(22)))
                searchQuery = "";
            EditorGUILayout.EndHorizontal();
            GUILayout.Space(3);

            // ============ CATEGORY TABS ============
            EditorGUILayout.BeginHorizontal();
            DrawCategoryTab("All", Category.All);
            DrawCategoryTab("Base", Category.Base);
            DrawCategoryTab("Optical", Category.Optical);
            DrawCategoryTab("Surface", Category.Surface);
            DrawCategoryTab("Effects", Category.Effects);
            DrawCategoryTab("Render", Category.Rendering);
            EditorGUILayout.EndHorizontal();
            GUILayout.Space(3);

            // ============ EXPAND/COLLAPSE BUTTONS ============
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("+ Expand All"))
            {
                forceExpand = ExpandType.All;
                UpdateAllHeaderStates(true, mat);
            }
            if (GUILayout.Button("Expand Active"))
            {
                forceExpand = ExpandType.Active;
                UpdateActiveHeaderStates(mat);
            }
            if (GUILayout.Button("- Collapse All"))
            {
                forceExpand = ExpandType.Collapse;
                UpdateAllHeaderStates(false, mat);
            }
            if (GUILayout.Button(hideUnusedSections ? "Show All" : "Hide Unused"))
            {
                hideUnusedSections = !hideUnusedSections;
            }
            EditorGUILayout.EndHorizontal();;
            
            // ============ COPY / PASTE BUTTONS ============
            EditorGUILayout.BeginHorizontal();
            GUI.backgroundColor = new Color(0.7f, 0.85f, 1f);
            if (GUILayout.Button(new GUIContent("\U0001F4CB Copy All", "Copy all material properties")))
            {
                CopyAllProperties(mat, properties);
                Debug.Log("[SB Glass] All properties copied!");
            }
            
            bool hasClipboard = s_copiedProperties != null && s_copiedProperties.Count > 0;
            GUI.backgroundColor = hasClipboard ? new Color(0.7f, 1f, 0.7f) : new Color(0.5f, 0.5f, 0.5f);
            GUI.enabled = hasClipboard;
            if (GUILayout.Button(new GUIContent("\U0001F4C4 Paste All", hasClipboard ? $"Paste {s_copiedProperties.Count} properties" : "Nothing copied")))
            {
                Undo.RecordObject(mat, "Paste All Properties");
                PasteAllProperties(mat);
                Debug.Log("[SB Glass] All properties pasted!");
            }
            GUI.enabled = true;
            GUI.backgroundColor = Color.white;
            
            // Show copied status
            if (s_copiedProperties != null && s_copiedProperties.Count > 0)
            {
                GUILayout.Label($"({s_copiedProperties.Count} props)", EditorStyles.miniLabel, GUILayout.Width(70));
            }
            EditorGUILayout.EndHorizontal();
            GUILayout.Space(5);

            // ============ ACTIVE FEATURES COUNTER ============
            int activeCount = CountActiveFeatures(mat);
            string impactText = GetPerformanceImpact(activeCount);
            Color barColor = GetImpactColor(activeCount);

            Rect barRect = EditorGUILayout.GetControlRect(false, 22);
            EditorGUI.DrawRect(barRect, new Color(0.15f, 0.15f, 0.15f, 1f));
            Rect fillRect = new Rect(barRect.x, barRect.y, barRect.width * Mathf.Clamp01(activeCount / 20f), barRect.height);
            EditorGUI.DrawRect(fillRect, barColor);
            EditorGUI.LabelField(barRect, $"Active Features: {activeCount} | {impactText}", CenteredWhiteStyle);
            GUILayout.Space(5);

            // ============ SECTIONS ============
            var organizedProps = OrganizePropertiesByHeader(properties);
            string lastFamily = null;

            for (int i = 0; i < headerOrder.Count; i++)
            {
                string headerName = headerOrder[i];

                // Category filter
                if (currentCategory != Category.All)
                {
                    if (!sectionCategories.ContainsKey(headerName) || sectionCategories[headerName] != currentCategory)
                        continue;
                }

                // Search filter
                if (!string.IsNullOrEmpty(searchQuery))
                {
                    bool matchFound = headerName.ToLower().Contains(searchQuery.ToLower());
                    if (!matchFound && organizedProps.ContainsKey(headerName))
                    {
                        var props = organizedProps[headerName];
                        for (int p = 0; p < props.Count; p++)
                        {
                            if (props[p].displayName.ToLower().Contains(searchQuery.ToLower()))
                            {
                                matchFound = true;
                                break;
                            }
                        }
                    }
                    if (!matchFound) continue;
                }

                // Hide unused sections
                if (hideUnusedSections && !IsSectionEnabled(headerName, mat))
                    continue;
                
                // Hide sections that shouldn't be visible at all
                if (!IsSectionVisible(headerName, mat))
                    continue;

                // Family title
                string family = GetFamilyForSection(headerName);
                if (family != null && family != lastFamily)
                {
                    GUILayout.Space(20);
                    EditorGUILayout.LabelField(family, FamilyStyle);
                    lastFamily = family;
                }

                // Section foldout
                bool isEnabled = IsSectionEnabled(headerName, mat);
                bool isExpanded = GetHeaderState(headerName, mat);

                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                Rect headerRect = EditorGUILayout.GetControlRect(false, 22);

                // Grey Background
                EditorGUI.DrawRect(headerRect, new Color(0.2f, 0.2f, 0.2f, 0.5f));

                // Gradient if active
                if (isEnabled)
                {
                    float gradientWidth = 0.4f;
                    Rect gradientRect = new Rect(headerRect.x, headerRect.y, headerRect.width * gradientWidth, headerRect.height);
                    GUI.DrawTexture(gradientRect, GetGradientTexture(), ScaleMode.StretchToFill);
                }

                string arrow = isExpanded ? "\u25BC" : "\u25B6";
                string enabledMark = isEnabled ? "\u25CF" : "\u25CB";

                // Calculate button rects
                float buttonWidth = 22f;
                float buttonSpacing = 2f;
                float buttonsWidth = buttonWidth * 2 + buttonSpacing;
                
                Rect foldoutRect = new Rect(headerRect.x, headerRect.y, headerRect.width - buttonsWidth - 5, headerRect.height);
                Rect copyRect = new Rect(headerRect.xMax - buttonsWidth, headerRect.y + 2, buttonWidth, headerRect.height - 4);
                Rect pasteRect = new Rect(copyRect.xMax + buttonSpacing, headerRect.y + 2, buttonWidth, headerRect.height - 4);

                // Foldout button
                if (GUI.Button(foldoutRect, $"{arrow} {enabledMark} {headerName}", EditorStyles.boldLabel))
                {
                    SetHeaderState(headerName, !isExpanded, mat);
                }
                
                // Copy button
                GUI.backgroundColor = new Color(0.7f, 0.85f, 1f);
                if (GUI.Button(copyRect, new GUIContent("C", "Copy this section")))
                {
                    CopySectionProperties(mat, headerName, organizedProps);
                    Debug.Log($"[SB Glass] Section '{headerName}' copied!");
                }
                
                // Paste button
                bool hasCopiedSection = s_copiedSections.ContainsKey(headerName) && s_copiedSections[headerName].Count > 0;
                string pasteTooltip = hasCopiedSection ? $"Paste {s_copiedSections[headerName].Count} properties" : "Nothing copied for this section";
                GUI.backgroundColor = hasCopiedSection ? new Color(0.7f, 1f, 0.7f) : new Color(0.5f, 0.5f, 0.5f);
                GUI.enabled = hasCopiedSection;
                if (GUI.Button(pasteRect, new GUIContent("P", pasteTooltip)))
                {
                    Undo.RecordObject(mat, $"Paste Section {headerName}");
                    PasteSectionProperties(mat, headerName);
                    Debug.Log($"[SB Glass] Section '{headerName}' pasted!");
                }
                GUI.enabled = true;
                GUI.backgroundColor = Color.white;

                if (isExpanded && organizedProps.ContainsKey(headerName))
                {
                    EditorGUI.indentLevel++;
                    var props = organizedProps[headerName];

                    for (int p = 0; p < props.Count; p++)
                    {
                        var prop = props[p];

                        // Check feature dependency
                        if (propertyMapping.ContainsKey(prop.name))
                        {
                            var mapping = propertyMapping[prop.name];
                            if (mapping.feature != null)
                            {
                                // Standard toggle dependency
                                if (!mat.HasProperty(mapping.feature)) continue;
                                if (mat.GetFloat(mapping.feature) != 1.0f) continue;
                            }
                        }
                        
                        // ============ FINGERPRINT MAPPING MODE VISIBILITY ============
                        // Only show properties relevant to the current mapping mode
                        // Mode: 0=UV, 1=World, 2=Triplanar (uses World pos/radius + triplanar sampling)
                        if (IsFingerprintModeProperty(prop.name))
                        {
                            int slotNum = GetFingerprintSlotNumber(prop.name);
                            if (slotNum > 0)
                            {
                                string mappingProp = "_FingerprintMapping" + slotNum;
                                if (mat.HasProperty(mappingProp))
                                {
                                    int mode = (int)mat.GetFloat(mappingProp);
                                    
                                    // UV mode (0): Show Pos, Scale, Falloff
                                    // World mode (1): Show WorldPos, WorldRadius, Falloff
                                    // Triplanar mode (2): Show WorldPos, WorldRadius, TriplanarScale, Falloff
                                    
                                    bool isUVProp = prop.name.Contains("Pos" + slotNum) && !prop.name.Contains("World");
                                    isUVProp = isUVProp || (prop.name.Contains("Scale" + slotNum) && !prop.name.Contains("Triplanar"));
                                    
                                    bool isWorldProp = prop.name.Contains("WorldPos" + slotNum) || prop.name.Contains("WorldRadius" + slotNum);
                                    bool isTriplanarProp = prop.name.Contains("TriplanarScale" + slotNum);
                                    
                                    // Skip if property doesn't match current mode
                                    if (mode == 0 && (isWorldProp || isTriplanarProp)) continue;
                                    if (mode == 1 && (isUVProp || isTriplanarProp)) continue;
                                    if (mode == 2 && isUVProp) continue; // Triplanar uses World + TriplanarScale
                                }
                            }
                        }

                        // Get tooltip
                        string tooltip = SB_GlassTooltips.GetTooltip(prop.name);
                        GUIContent label = tooltip != null 
                            ? new GUIContent(prop.displayName, tooltip) 
                            : new GUIContent(prop.displayName);

                        // --- BLEND MODE SPECIAL HANDLING ---
                        if (prop.name == "_BlendMode")
                        {
                            DrawBlendModeWithLogic(materialEditor, prop, mat);
                        }
                        else
                        {
                            // Draw standard property
                            materialEditor.ShaderProperty(prop, label);
                        }
                    }
                    EditorGUI.indentLevel--;
                }

                EditorGUILayout.EndVertical();
            }

            // ============ AUTO KEYWORDS - Only when properties change ============
            // Force UI refresh when properties change
            if (EditorGUI.EndChangeCheck())
            {
                // Only manage keywords when something actually changed
                AutoManageKeywords(mat);
                
                EditorUtility.SetDirty(mat);
                if (materialEditor != null)
                {
                    materialEditor.Repaint();
                }
            }
            
            // ============ SAVE PER-MATERIAL STATE ============
            SaveMaterialState(mat);
        }

        // ========================================
        // HELPER METHODS
        // ========================================
        private void DrawCategoryTab(string name, Category cat)
        {
            bool isActive = currentCategory == cat;
            GUI.backgroundColor = isActive ? new Color(0.4f, 0.8f, 1f) : Color.white;
            if (GUILayout.Button(name, GUILayout.Height(22)))
                currentCategory = cat;
            GUI.backgroundColor = Color.white;
        }

        private int CountActiveFeatures(Material mat)
        {
            int count = 0;
            foreach (string toggle in s_effectToggles)
            {
                if (mat.HasProperty(toggle) && mat.GetFloat(toggle) == 1.0f)
                    count++;
            }
            return count;
        }
        
        // ========================================
        // FINGERPRINT MODE HELPERS
        // ========================================
        private static readonly string[] s_fingerprintModeProps = new string[]
        {
            "_FingerprintPos", "_FingerprintScale", 
            "_FingerprintWorldPos", "_FingerprintWorldRadius",
            "_FingerprintTriplanarScale"
            // Note: Falloff is shown for all modes now
        };
        
        private bool IsFingerprintModeProperty(string propName)
        {
            foreach (var prefix in s_fingerprintModeProps)
            {
                if (propName.StartsWith(prefix)) return true;
            }
            return false;
        }
        
        private int GetFingerprintSlotNumber(string propName)
        {
            // Extract slot number from property name (1-4)
            for (int i = 1; i <= 4; i++)
            {
                if (propName.EndsWith(i.ToString())) return i;
            }
            return 0;
        }

        private string GetPerformanceImpact(int count)
        {
            if (count <= 5) return "Excellent";
            if (count <= 10) return "Good";
            if (count <= 15) return "Moderate";
            if (count <= 20) return "Heavy";
            return "Extreme";
        }

        private Color GetImpactColor(int count)
        {
            if (count <= 5) return new Color(0.2f, 0.7f, 0.2f, 1f);
            if (count <= 10) return new Color(0.4f, 0.7f, 0.2f, 1f);
            if (count <= 15) return new Color(0.7f, 0.7f, 0.2f, 1f);
            if (count <= 20) return new Color(0.7f, 0.4f, 0.2f, 1f);
            return new Color(0.7f, 0.2f, 0.2f, 1f);
        }

        private bool IsSectionEnabled(string headerName, Material mat)
        {
            // Always enabled sections
            if (headerName == "Main Surface" || headerName == "Transparency" || headerName == "Quality" || headerName == "Rendering")
                return true;
            
            // Detail Maps: enabled if either Detail Albedo OR Detail Normal is active
            if (headerName == "Detail Maps")
            {
                bool hasAlbedo = mat.HasProperty("_UseDetailAlbedo") && mat.GetFloat("_UseDetailAlbedo") == 1.0f;
                bool hasNormal = mat.HasProperty("_UseDetailNormal") && mat.GetFloat("_UseDetailNormal") == 1.0f;
                return hasAlbedo || hasNormal;
            }
            
            // Refraction, Fresnel and Specular now have toggles
            if (headerName == "Refraction") return mat.HasProperty("_UseRefraction") && mat.GetFloat("_UseRefraction") == 1.0f;
            if (headerName == "Fresnel") return mat.HasProperty("_UseFresnel") && mat.GetFloat("_UseFresnel") == 1.0f;
            if (headerName == "Specular") return mat.HasProperty("_UseSpecular") && mat.GetFloat("_UseSpecular") == 1.0f;

            // Sub-sections (Decal 1-4, Slot 1-4)
            if (headerName == "Decal 1") return mat.HasProperty("_UseDecals") && mat.GetFloat("_UseDecals") == 1.0f;
            if (headerName == "Decal 2") return mat.HasProperty("_UseDecal2") && mat.GetFloat("_UseDecal2") == 1.0f;
            if (headerName == "Decal 3") return mat.HasProperty("_UseDecal3") && mat.GetFloat("_UseDecal3") == 1.0f;
            if (headerName == "Decal 4") return mat.HasProperty("_UseDecal4") && mat.GetFloat("_UseDecal4") == 1.0f;
            if (headerName == "Slot 1") return mat.HasProperty("_UseFingerprints") && mat.GetFloat("_UseFingerprints") == 1.0f;
            if (headerName == "Slot 2") return mat.HasProperty("_UseSlot2") && mat.GetFloat("_UseSlot2") == 1.0f;
            if (headerName == "Slot 3") return mat.HasProperty("_UseSlot3") && mat.GetFloat("_UseSlot3") == 1.0f;
            if (headerName == "Slot 4") return mat.HasProperty("_UseSlot4") && mat.GetFloat("_UseSlot4") == 1.0f;
            
            // Distortion sub-sections
            if (headerName == "Magnify") return mat.HasProperty("_UseMagnify") && mat.GetFloat("_UseMagnify") == 1.0f;
            if (headerName == "Barrel") return mat.HasProperty("_UseBarrel") && mat.GetFloat("_UseBarrel") == 1.0f;
            if (headerName == "Waves") return mat.HasProperty("_UseWaves") && mat.GetFloat("_UseWaves") == 1.0f;
            if (headerName == "Ripple") return mat.HasProperty("_UseRipple") && mat.GetFloat("_UseRipple") == 1.0f;
            if (headerName == "Swirl") return mat.HasProperty("_UseSwirl") && mat.GetFloat("_UseSwirl") == 1.0f;
            if (headerName == "Heat Haze") return mat.HasProperty("_UseHeatHaze") && mat.GetFloat("_UseHeatHaze") == 1.0f;
            if (headerName == "Pixelate") return mat.HasProperty("_UsePixelate") && mat.GetFloat("_UsePixelate") == 1.0f;

            foreach (var kvp in propertyMapping)
            {
                if (kvp.Value.header == headerName && kvp.Value.feature == null)
                {
                    if (mat.HasProperty(kvp.Key))
                    {
                        if (kvp.Key.Contains("Use") || kvp.Key.StartsWith("_Use"))
                            return mat.GetFloat(kvp.Key) == 1.0f;
                    }
                }
            }
            return false;
        }
        
        private bool IsSectionVisible(string headerName, Material mat)
        {
            // Decal sub-sections only visible when Decals is enabled
            if (headerName == "Decal 1" || headerName == "Decal 2" || headerName == "Decal 3" || headerName == "Decal 4")
            {
                return mat.HasProperty("_UseDecals") && mat.GetFloat("_UseDecals") == 1.0f;
            }
            
            // Fingerprint slots only visible when Fingerprints is enabled
            if (headerName == "Slot 1" || headerName == "Slot 2" || headerName == "Slot 3" || headerName == "Slot 4")
            {
                return mat.HasProperty("_UseFingerprints") && mat.GetFloat("_UseFingerprints") == 1.0f;
            }
            
            // Distortion sub-sections only visible when Distortion FX is enabled
            if (headerName == "Magnify" || headerName == "Barrel" || headerName == "Waves" || 
                headerName == "Ripple" || headerName == "Swirl" || headerName == "Heat Haze" || headerName == "Pixelate")
            {
                return mat.HasProperty("_UseDistortion") && mat.GetFloat("_UseDistortion") == 1.0f;
            }
            
            return true;
        }

        private static Texture2D GetGradientTexture()
        {
            if (s_gradientTex == null)
            {
                s_gradientTex = new Texture2D(64, 1, TextureFormat.RGBA32, false);
                s_gradientTex.wrapMode = TextureWrapMode.Clamp;
                for (int x = 0; x < 64; x++)
                {
                    float t = (float)x / 63f;
                    float alpha = 1f - t;
                    s_gradientTex.SetPixel(x, 0, new Color(0.2f, 0.5f, 0.6f, alpha));
                }
                s_gradientTex.Apply();
            }
            return s_gradientTex;
        }

        private bool GetHeaderState(string headerName, Material mat)
        {
            if (forceExpand == ExpandType.All) return true;
            if (forceExpand == ExpandType.Collapse) return false;
            if (forceExpand == ExpandType.Active) return IsSectionEnabled(headerName, mat);

            // Use per-material foldout state from EditorPrefs
            string guid = GetMaterialGUID(mat);
            if (!string.IsNullOrEmpty(guid))
            {
                return GetFoldoutState(headerName, guid);
            }
            
            return false; // Default collapsed
        }

        private void SetHeaderState(string headerName, bool state, Material mat)
        {
            forceExpand = ExpandType.Keep;
            
            // Store in EditorPrefs per material
            string guid = GetMaterialGUID(mat);
            if (!string.IsNullOrEmpty(guid))
            {
                SetFoldoutState(headerName, guid, state);
            }
        }

        private void UpdateAllHeaderStates(bool state, Material mat)
        {
            string guid = GetMaterialGUID(mat);
            if (string.IsNullOrEmpty(guid)) return;
            
            for (int i = 0; i < headerOrder.Count; i++)
            {
                SetFoldoutState(headerOrder[i], guid, state);
            }
        }

        private void UpdateActiveHeaderStates(Material mat)
        {
            string guid = GetMaterialGUID(mat);
            if (string.IsNullOrEmpty(guid)) return;
            
            for (int i = 0; i < headerOrder.Count; i++)
            {
                SetFoldoutState(headerOrder[i], guid, IsSectionEnabled(headerOrder[i], mat));
            }
        }

        private Dictionary<string, List<MaterialProperty>> OrganizePropertiesByHeader(MaterialProperty[] properties)
        {
            // Initialize cached dictionary once
            if (!s_organizedPropsInitialized || s_organizedProps == null)
            {
                s_organizedProps = new Dictionary<string, List<MaterialProperty>>(headerOrder.Count);
                for (int i = 0; i < headerOrder.Count; i++)
                {
                    s_organizedProps[headerOrder[i]] = new List<MaterialProperty>(32); // Pre-allocate capacity
                }
                s_organizedPropsInitialized = true;
            }
            
            // Clear lists instead of recreating them
            foreach (var kvp in s_organizedProps)
            {
                kvp.Value.Clear();
            }

            // Populate
            for (int i = 0; i < properties.Length; i++)
            {
                var prop = properties[i];
                if (propertyMapping.TryGetValue(prop.name, out var mapping))
                {
                    if (s_organizedProps.TryGetValue(mapping.header, out var list))
                    {
                        list.Add(prop);
                    }
                }
            }
            return s_organizedProps;
        }

        private void AutoManageKeywords(Material mat)
        {
            // Only modify keywords if their state actually needs to change
            // This avoids unnecessary internal allocations in Unity 6+
            for (int i = 0; i < autoKeywordProps.Count; i++)
            {
                var kvp = autoKeywordProps[i];
                if (!mat.HasProperty(kvp.propName)) continue;
                
                bool shouldBeEnabled = mat.GetFloat(kvp.propName) == 1.0f;
                bool isCurrentlyEnabled = mat.IsKeywordEnabled(kvp.keyword);
                
                if (shouldBeEnabled && !isCurrentlyEnabled)
                    mat.EnableKeyword(kvp.keyword);
                else if (!shouldBeEnabled && isCurrentlyEnabled)
                    mat.DisableKeyword(kvp.keyword);
            }
            
            // Protection: Disable child features when parent is OFF
            // Only disable if currently enabled
            if (mat.HasProperty("_UseDecals") && mat.GetFloat("_UseDecals") != 1.0f)
            {
                if (mat.IsKeywordEnabled("_SB_DECAL2")) mat.DisableKeyword("_SB_DECAL2");
                if (mat.IsKeywordEnabled("_SB_DECAL3")) mat.DisableKeyword("_SB_DECAL3");
                if (mat.IsKeywordEnabled("_SB_DECAL4")) mat.DisableKeyword("_SB_DECAL4");
            }
            
            if (mat.HasProperty("_UseFingerprints") && mat.GetFloat("_UseFingerprints") != 1.0f)
            {
                if (mat.IsKeywordEnabled("_SB_FINGERPRINTS_SLOT2")) mat.DisableKeyword("_SB_FINGERPRINTS_SLOT2");
                if (mat.IsKeywordEnabled("_SB_FINGERPRINTS_SLOT3")) mat.DisableKeyword("_SB_FINGERPRINTS_SLOT3");
                if (mat.IsKeywordEnabled("_SB_FINGERPRINTS_SLOT4")) mat.DisableKeyword("_SB_FINGERPRINTS_SLOT4");
            }
            
            if (mat.HasProperty("_UseDistortion") && mat.GetFloat("_UseDistortion") != 1.0f)
            {
                if (mat.IsKeywordEnabled("_SB_MAGNIFY")) mat.DisableKeyword("_SB_MAGNIFY");
                if (mat.IsKeywordEnabled("_SB_BARREL")) mat.DisableKeyword("_SB_BARREL");
                if (mat.IsKeywordEnabled("_SB_WAVES")) mat.DisableKeyword("_SB_WAVES");
                if (mat.IsKeywordEnabled("_SB_RIPPLE")) mat.DisableKeyword("_SB_RIPPLE");
                if (mat.IsKeywordEnabled("_SB_SWIRL")) mat.DisableKeyword("_SB_SWIRL");
                if (mat.IsKeywordEnabled("_SB_HEAT_HAZE")) mat.DisableKeyword("_SB_HEAT_HAZE");
                if (mat.IsKeywordEnabled("_SB_PIXELATE")) mat.DisableKeyword("_SB_PIXELATE");
            }
            
            if (mat.HasProperty("_UseReflection") && mat.GetFloat("_UseReflection") != 1.0f)
            {
                if (mat.IsKeywordEnabled("_SB_REFLECTION_CUBEMAP")) mat.DisableKeyword("_SB_REFLECTION_CUBEMAP");
            }
            
            if (mat.HasProperty("_UseEmission") && mat.GetFloat("_UseEmission") != 1.0f)
            {
                if (mat.IsKeywordEnabled("_SB_EMISSION_MAP")) mat.DisableKeyword("_SB_EMISSION_MAP");
            }
            
            if (mat.HasProperty("_UseCaustics") && mat.GetFloat("_UseCaustics") != 1.0f)
            {
                if (mat.IsKeywordEnabled("_SB_CAUSTICS_PROCEDURAL")) mat.DisableKeyword("_SB_CAUSTICS_PROCEDURAL");
            }
            
            if (mat.HasProperty("_UseRain") && mat.GetFloat("_UseRain") != 1.0f)
            {
                if (mat.IsKeywordEnabled("_SB_RAIN_TRIPLANAR")) mat.DisableKeyword("_SB_RAIN_TRIPLANAR");
            }
        }

        // ========================================
        // COPY/PASTE SYSTEM
        // ========================================
        private void CopyAllProperties(Material mat, MaterialProperty[] properties)
        {
            s_copiedProperties = new Dictionary<string, object>();
            for (int i = 0; i < properties.Length; i++)
            {
                var prop = properties[i];
                s_copiedProperties[prop.name] = GetPropertyValue(mat, prop);
            }
        }

        private void PasteAllProperties(Material mat)
        {
            if (s_copiedProperties == null) return;
            foreach (var kvp in s_copiedProperties)
            {
                SetPropertyValue(mat, kvp.Key, kvp.Value);
            }
        }

        private void CopySectionProperties(Material mat, string headerName, Dictionary<string, List<MaterialProperty>> organizedProps)
        {
            if (!organizedProps.ContainsKey(headerName)) return;
            
            var sectionData = new Dictionary<string, object>();
            var props = organizedProps[headerName];
            for (int i = 0; i < props.Count; i++)
            {
                var prop = props[i];
                sectionData[prop.name] = GetPropertyValue(mat, prop);
            }
            s_copiedSections[headerName] = sectionData;
        }

        private void PasteSectionProperties(Material mat, string headerName)
        {
            if (!s_copiedSections.ContainsKey(headerName)) return;
            var sectionData = s_copiedSections[headerName];
            foreach (var kvp in sectionData)
            {
                SetPropertyValue(mat, kvp.Key, kvp.Value);
            }
        }

        private object GetPropertyValue(Material mat, MaterialProperty prop)
        {
            switch (prop.propertyType)
            {
                case UnityEngine.Rendering.ShaderPropertyType.Color:
                    return mat.GetColor(prop.name);
                case UnityEngine.Rendering.ShaderPropertyType.Vector:
                    return mat.GetVector(prop.name);
                case UnityEngine.Rendering.ShaderPropertyType.Float:
                case UnityEngine.Rendering.ShaderPropertyType.Range:
                    return mat.GetFloat(prop.name);
                case UnityEngine.Rendering.ShaderPropertyType.Texture:
                    return new TextureData
                    {
                        texture = mat.GetTexture(prop.name),
                        scale = mat.GetTextureScale(prop.name),
                        offset = mat.GetTextureOffset(prop.name)
                    };
                default:
                    return null;
            }
        }

        private void SetPropertyValue(Material mat, string propName, object value)
        {
            if (value == null || !mat.HasProperty(propName)) return;
            
            if (value is Color colorVal)
                mat.SetColor(propName, colorVal);
            else if (value is Vector4 vecVal)
                mat.SetVector(propName, vecVal);
            else if (value is float floatVal)
                mat.SetFloat(propName, floatVal);
            else if (value is int intVal)
                mat.SetInt(propName, intVal);
            else if (value is TextureData texData)
            {
                mat.SetTexture(propName, texData.texture);
                mat.SetTextureScale(propName, texData.scale);
                mat.SetTextureOffset(propName, texData.offset);
            }
        }

        // ============================================================
        // PRESET SYSTEM UI
        // ============================================================
        private void DrawPresetSection(MaterialEditor materialEditor, Material mat)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            // Header bar - same style as other categories
            Rect headerRect = EditorGUILayout.GetControlRect(false, 22);
            
            // Grey Background
            EditorGUI.DrawRect(headerRect, new Color(0.2f, 0.2f, 0.2f, 0.5f));
            
            // Gradient (always show for Preset section)
            float gradientWidth = 0.4f;
            Rect gradientRect = new Rect(headerRect.x, headerRect.y, headerRect.width * gradientWidth, headerRect.height);
            GUI.DrawTexture(gradientRect, GetGradientTexture(), ScaleMode.StretchToFill);
            
            string arrow = s_showPresetSection ? "\u25BC" : "\u25B6";
            
            // Calculate button rects - Create + Browse on the right
            float createWidth = 80f;
            float browseWidth = 80f;
            float buttonSpacing = 4f;
            float buttonsWidth = createWidth + browseWidth + buttonSpacing;
            
            Rect foldoutRect = new Rect(headerRect.x, headerRect.y, headerRect.width - buttonsWidth - 10, headerRect.height);
            Rect createRect = new Rect(headerRect.xMax - buttonsWidth, headerRect.y + 2, createWidth, headerRect.height - 4);
            Rect browseRect = new Rect(createRect.xMax + buttonSpacing, headerRect.y + 2, browseWidth, headerRect.height - 4);
            
            // Foldout button
            if (GUI.Button(foldoutRect, $"{arrow} \u25CF Preset System", EditorStyles.boldLabel))
            {
                s_showPresetSection = !s_showPresetSection;
            }
            
            // Create Preset button - use delayCall to avoid GUI recursion
            GUI.backgroundColor = new Color(0.5f, 1.5f, 0.5f);
            if (GUI.Button(createRect, "Create"))
            {
                Material matCopy = mat; // Capture for lambda
                EditorApplication.delayCall += () => CreatePresetFromMaterial(matCopy);
            }
            
            // Browse button
            GUI.backgroundColor = new Color(0.5f, 1.2f, 1.5f);
            if (GUI.Button(browseRect, "Browse"))
            {
                EditorApplication.delayCall += () => OpenOrCreatePresetFolder();
            }
            GUI.backgroundColor = Color.white;
            
            if (s_showPresetSection)
            {
                EditorGUILayout.Space(5);
                
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Apply Preset:", GUILayout.Width(80));
                
                SB_GlassPreset newPreset = (SB_GlassPreset)EditorGUILayout.ObjectField(
                    s_selectedPreset, 
                    typeof(SB_GlassPreset), 
                    false
                );
                
                if (newPreset != s_selectedPreset)
                {
                    s_selectedPreset = newPreset;
                }
                
                EditorGUILayout.EndHorizontal();
                
                if (s_selectedPreset != null)
                {
                    EditorGUILayout.Space(5);
                    
                    EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                    
                    EditorGUILayout.BeginHorizontal();
                    
                    if (s_selectedPreset.thumbnail != null)
                    {
                        GUILayout.Label(s_selectedPreset.thumbnail, GUILayout.Width(64), GUILayout.Height(64));
                    }
                    
                    EditorGUILayout.BeginVertical();
                    EditorGUILayout.LabelField(s_selectedPreset.presetName, EditorStyles.boldLabel);
                    if (!string.IsNullOrEmpty(s_selectedPreset.category))
                    {
                        EditorGUILayout.LabelField($"Category: {s_selectedPreset.category}", EditorStyles.miniLabel);
                    }
                    if (!string.IsNullOrEmpty(s_selectedPreset.description))
                    {
                        EditorGUILayout.LabelField(s_selectedPreset.description, EditorStyles.wordWrappedMiniLabel);
                    }
                    
                    var features = s_selectedPreset.GetEnabledFeatures();
                    if (features.Count > 0)
                    {
                        EditorGUILayout.LabelField($"Features: {features.Count} enabled", EditorStyles.miniLabel);
                    }
                    EditorGUILayout.EndVertical();
                    
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.EndVertical();
                    
                    EditorGUILayout.Space(3);
                    EditorGUILayout.BeginHorizontal();
                    GUILayout.FlexibleSpace();
                    
                    GUI.backgroundColor = new Color(0.4f, 0.8f, 0.4f);
                    if (GUILayout.Button("Apply Preset", GUILayout.Width(100), GUILayout.Height(22)))
                    {
                        SB_GlassPreset presetCopy = s_selectedPreset; // Capture for lambda
                        Material matCopy = mat;
                        string matGuid = GetMaterialGUID(mat);
                        EditorApplication.delayCall += () =>
                        {
                            if (EditorUtility.DisplayDialog("Apply Preset", 
                                $"Apply preset '{presetCopy.presetName}' to this material?\n\nThis will override all current settings.",
                                "Apply", "Cancel"))
                            {
                                // Apply material properties
                                presetCopy.ApplyToMaterial(matCopy);
                                
                                // Apply editor state if saved in preset
                                if (presetCopy.saveEditorState && !string.IsNullOrEmpty(matGuid))
                                {
                                    // Restore category tab
                                    EditorPrefs.SetInt(STATE_PREFIX + matGuid + "_category", presetCopy.editorCategory);
                                    EditorPrefs.SetBool(STATE_PREFIX + matGuid + "_hideUnused", presetCopy.editorHideUnused);
                                    
                                    // Restore foldout states - collapse all first, then expand listed ones
                                    foreach (string header in headerOrder)
                                    {
                                        bool shouldExpand = presetCopy.expandedSections.Contains(header);
                                        EditorPrefs.SetBool(STATE_PREFIX + matGuid + "_foldout_" + header, shouldExpand);
                                    }
                                    
                                    // Force reload on next OnGUI
                                    s_currentMaterialGUID = null;
                                }
                                
                                Debug.Log($"[SB Glass] Applied preset: {presetCopy.presetName}");
                            }
                        };
                    }
                    GUI.backgroundColor = Color.white;
                    
                    GUILayout.FlexibleSpace();
                    EditorGUILayout.EndHorizontal();
                }
            }
            
            EditorGUILayout.EndVertical();
        }
        
        private void CreatePresetFromMaterial(Material mat)
        {
            if (!AssetDatabase.IsValidFolder(PRESET_FOLDER))
            {
                CreatePresetFolderStructure();
            }
            
            string defaultName = $"Preset_{mat.name}";
            string path = EditorUtility.SaveFilePanelInProject(
                "Save Glass Preset",
                defaultName,
                "asset",
                "Choose a name for the preset",
                PRESET_FOLDER
            );
            
            if (string.IsNullOrEmpty(path)) return;
            
            SB_GlassPreset preset = ScriptableObject.CreateInstance<SB_GlassPreset>();
            preset.presetName = System.IO.Path.GetFileNameWithoutExtension(path);
            preset.SaveFromMaterial(mat);
            
            // Save clean editor state: Hide Unused + Collapse All
            preset.saveEditorState = true;
            preset.editorCategory = 0; // All tab
            preset.editorHideUnused = true; // Hide unused sections
            preset.expandedSections.Clear(); // Collapse all
            
            AssetDatabase.CreateAsset(preset, path);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            
            s_selectedPreset = preset;
            EditorGUIUtility.PingObject(preset);
            Selection.activeObject = preset;
            
            Debug.Log($"[SB Glass] Created preset: {path}");
        }
        
        private void OpenOrCreatePresetFolder()
        {
            if (!AssetDatabase.IsValidFolder(PRESET_FOLDER))
            {
                CreatePresetFolderStructure();
            }
            
            Object folder = AssetDatabase.LoadAssetAtPath<Object>(PRESET_FOLDER);
            if (folder != null)
            {
                EditorGUIUtility.PingObject(folder);
                Selection.activeObject = folder;
            }
        }
        
        private void CreatePresetFolderStructure()
        {
            if (!AssetDatabase.IsValidFolder("Assets/SingularBear"))
            {
                AssetDatabase.CreateFolder("Assets", "SingularBear");
            }
            if (!AssetDatabase.IsValidFolder("Assets/SingularBear/GlassShader"))
            {
                AssetDatabase.CreateFolder("Assets/SingularBear", "GlassShader");
            }
            if (!AssetDatabase.IsValidFolder(PRESET_FOLDER))
            {
                AssetDatabase.CreateFolder("Assets/SingularBear/GlassShader", "Presets");
            }
            AssetDatabase.Refresh();
        }

        // Helper struct for texture data
        private struct TextureData
        {
            public Texture texture;
            public Vector2 scale;
            public Vector2 offset;
        }
        
        // ========================================
        // BLEND MODE INITIALIZATION
        // ========================================
        /// <summary>
        /// Ensures blend values are correctly set based on _BlendMode.
        /// Called once at the start of OnGUI to fix dark materials.
        /// </summary>
        private void InitializeBlendMode(Material mat)
        {
            if (!mat.HasProperty("_BlendMode")) return;
            
            int mode = (int)mat.GetFloat("_BlendMode");
            int currentSrc = mat.HasProperty("_SrcBlend") ? (int)mat.GetFloat("_SrcBlend") : -1;
            int currentDst = mat.HasProperty("_DstBlend") ? (int)mat.GetFloat("_DstBlend") : -1;
            
            int expectedSrc, expectedDst;
            switch (mode)
            {
                case 0: // Standard (Alpha Blend)
                    expectedSrc = (int)UnityEngine.Rendering.BlendMode.SrcAlpha;
                    expectedDst = (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha;
                    break;
                case 1: // Additive
                    expectedSrc = (int)UnityEngine.Rendering.BlendMode.SrcAlpha;
                    expectedDst = (int)UnityEngine.Rendering.BlendMode.One;
                    break;
                case 2: // Soft (Premultiply)
                    expectedSrc = (int)UnityEngine.Rendering.BlendMode.One;
                    expectedDst = (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha;
                    break;
                default:
                    expectedSrc = (int)UnityEngine.Rendering.BlendMode.SrcAlpha;
                    expectedDst = (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha;
                    break;
            }
            
            // Only update if values are incorrect (avoid constant dirty marking)
            if (currentSrc != expectedSrc || currentDst != expectedDst)
            {
                mat.SetInt("_SrcBlend", expectedSrc);
                mat.SetInt("_DstBlend", expectedDst);
                EditorUtility.SetDirty(mat);
            }
        }
        
        /// <summary>
        /// Force fix dark material by resetting blend mode and lighting values.
        /// Call this when material appears darker than expected.
        /// </summary>
        private void FixDarkMaterial(Material mat)
        {
            // Reset to Standard Alpha Blend
            mat.SetFloat("_BlendMode", 0);
            mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            
            // Reset ZWrite for transparency
            mat.SetFloat("_ZWrite", 0);
            
            // Fix lighting intensity (too high makes glass dark)
            if (mat.HasProperty("_DiffuseIntensity"))
            {
                float diffuse = mat.GetFloat("_DiffuseIntensity");
                if (diffuse > 0.5f)
                {
                    mat.SetFloat("_DiffuseIntensity", 0.3f);
                }
            }
            
            // Ensure reasonable opacity
            if (mat.HasProperty("_Opacity"))
            {
                float opacity = mat.GetFloat("_Opacity");
                if (opacity < 0.05f)
                {
                    mat.SetFloat("_Opacity", 0.5f);
                }
            }
            
            // Ensure color alpha is not zero
            if (mat.HasProperty("_Color"))
            {
                Color col = mat.GetColor("_Color");
                if (col.a < 0.05f)
                {
                    col.a = 0.1f;
                    mat.SetColor("_Color", col);
                }
            }
            
            EditorUtility.SetDirty(mat);
        }

        // ========================================
        // BLEND MODE LOGIC
        // ========================================
        /// <summary>
        /// Draws the BlendMode dropdown and updates _SrcBlend/_DstBlend accordingly.
        /// Modes: 0=Standard (Alpha), 1=Additive (Glow), 2=Soft (Premultiply)
        /// </summary>
        private void DrawBlendModeWithLogic(MaterialEditor materialEditor, MaterialProperty prop, Material mat)
        {
            EditorGUI.BeginChangeCheck();
            
            // Draw the dropdown, monitoring for changes
            string tooltip = SB_GlassTooltips.GetTooltip(prop.name);
            GUIContent label = tooltip != null 
                ? new GUIContent(prop.displayName, tooltip) 
                : new GUIContent(prop.displayName);
            materialEditor.ShaderProperty(prop, label);
            
            // If user changed the mode, update blend math immediately
            if (EditorGUI.EndChangeCheck())
            {
                int mode = (int)prop.floatValue;
                
                // Configure blend factors (SrcBlend / DstBlend)
                // Matches: [KeywordEnum(Standard, Additive, Soft)] in Shader
                switch (mode)
                {
                    case 0: // Standard (Alpha Blend)
                        mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                        mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                        break;
                        
                    case 1: // Additive (Glow / Hologram)
                        mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                        mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.One);
                        break;
                        
                    case 2: // Soft (Premultiplied Alpha - Best for realistic glass)
                        mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
                        mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                        break;
                }
                
                EditorUtility.SetDirty(mat);
            }
        }
    }
} // namespace SingularBear.Shaders
