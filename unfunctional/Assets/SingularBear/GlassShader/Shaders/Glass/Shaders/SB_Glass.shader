// ============================================================================
// SingularBear Glass Shader V1.0
// ============================================================================
// Compatible: Unity 2021.3+ | Unity 2022 LTS | Unity 6
// Pipeline: URP 12+ (Universal Render Pipeline)
// Platforms: PC, Mobile (Android/iOS), VR, Consoles
// ============================================================================
// Features:
// - SRP Batcher Compatible
// - GPU Instancing
// - VR Single Pass Instanced / Multi-View
// - Mobile Optimized (No GrabPass)
// - Depth Priming Support
// - URP Decal Projectors Compatible
// - Fog & Volumetrics Support
// ============================================================================

Shader "SingularBear/Glass"
{
    Properties
    {
        [Header(Quality)]
        [Space(5)]
        [KeywordEnum(Low, Medium, High)] _SB_Quality("Quality Level", Float) = 1
        
        [Header(Main Surface)]
        [Space(5)]
        _Color("Color", Color) = (1, 1, 1, 0.1)
        _MainTex("Albedo (RGB) Alpha (A)", 2D) = "white" {}
        _MainTint("Albedo Tint Strength", Range(0, 1)) = 0.2
        
        [Space(10)]
        _Metallic("Metallic", Range(0, 1)) = 0.0
        _Smoothness("Smoothness", Range(0, 1)) = 0.95
        _Saturation("Saturation", Range(0, 2)) = 1.0
        _Brightness("Brightness", Range(0.5, 3)) = 1.0
        [Toggle(_SB_METALLICGLOSS_MAP)] _UseMetallicMap("Use Metallic Map", Float) = 0
        _MetallicGlossMap("Metallic (R) Smoothness (A)", 2D) = "white" {}
        
        [Header(Normal Mapping)]
        [Space(5)]
        [Toggle(_SB_NORMALMAP)] _UseNormalMap("Enable Normal Map", Float) = 0
        _BumpMap("Normal Map", 2D) = "bump" {}
        _BumpScale("Normal Scale", Range(0, 2)) = 1.0
        
        [Header(Detail Maps)]
        [Space(5)]
        [Toggle(_SB_DETAIL_ALBEDO)] _UseDetailAlbedo("Enable Detail Albedo", Float) = 0
        _DetailAlbedoMap("Detail Albedo", 2D) = "white" {}
        _DetailColor("Detail Color", Color) = (1, 1, 1, 1)
        _DetailTiling("Detail Albedo Tiling", Float) = 4.0
        _DetailAlbedoIntensity("Detail Albedo Intensity", Range(0, 1)) = 0.5
        [Toggle(_SB_DETAIL_NORMAL)] _UseDetailNormal("Enable Detail Normal", Float) = 0
        _DetailNormalMap("Detail Normal", 2D) = "bump" {}
        _DetailNormalScale("Detail Normal Scale", Range(0, 2)) = 1.0
        _DetailNormalTiling("Detail Normal Tiling", Float) = 4.0
        [Toggle(_SB_DETAIL_NORMAL_TRIPLANAR)] _UseDetailNormalTriplanar("Detail Normal Triplanar", Float) = 0
        _DetailNormalTriplanarScale("Triplanar Scale", Float) = 1.0
        _DetailNormalTriplanarSharpness("Triplanar Sharpness", Range(1, 10)) = 4.0
        
        [Header(Transparency)]
        [Space(5)]
        _Opacity("Opacity", Range(0, 1)) = 0.5
        [Toggle(_SB_ALPHA_CLIP)] _UseAlphaClip("Alpha Clip", Float) = 0
        _AlphaClip("Alpha Clip Threshold", Range(0, 1)) = 0.5
        [Toggle(_SB_FALLOFF_OPACITY)] _UseFalloffOpacity("Falloff Opacity", Float) = 0
        _FalloffOpacityIntensity("Falloff Intensity", Range(0, 1)) = 1.0
        _FalloffOpacityPower("Falloff Power", Range(0.1, 10)) = 2.0
        [Toggle] _FalloffOpacityInvert("Invert Falloff", Float) = 0
        
        [Header(Refraction)]
        [Space(5)]
        [Toggle(_SB_REFRACTION)] _UseRefraction("Enable Refraction", Float) = 1
        _Distortion("Normal Distortion", Range(0, 0.5)) = 0.1
        [Toggle] _FlipRefraction("Flip Refraction", Float) = 0
        [Toggle(_SB_IOR)] _UseIOR("Physical Refraction (IOR)", Float) = 0
        _IndexOfRefraction("IOR Strength", Range(0, 1)) = 0.3
        [KeywordEnum(Air, Water, Glass)] _IOROriginPreset("Origin Medium", Float) = 0
        _IOROrigin("Origin IOR (Custom)", Range(1.0, 2.0)) = 1.0
        [Toggle(_SB_CHROMATIC_ABERRATION)] _UseChromatic("Chromatic Dispersion", Float) = 0
        _ChromaticAberration("Dispersion Amount", Range(0, 10)) = 2.0
        
        [Header(Blur)]
        [Space(5)]
        [Toggle(_SB_BLUR)] _UseBlur("Enable Blur", Float) = 0
        _BlurStrength("Blur Strength", Range(0, 1)) = 0.5
        _BlurRadius("Blur Radius", Range(0, 0.1)) = 0.03
        _BlurQuality("Blur Quality", Range(4, 25)) = 8
        
        [Header(Reflection)]
        [Space(5)]
        [Toggle(_SB_REFLECTION)] _UseReflection("Enable Reflection", Float) = 1
        _ReflectionColor("Reflection Color", Color) = (1, 1, 1, 1)
        _ReflectionIntensity("Reflection Intensity", Range(0, 2)) = 0.5
        _ReflectionBlur("Reflection Blur", Range(0, 1)) = 0.0
        [Toggle] _FlipReflection("Flip Reflection", Float) = 0
        [Toggle(_SB_REFLECTION_CUBEMAP)] _UseCubemap("Use Custom Cubemap", Float) = 0
        _ReflectionCube("Reflection Cubemap", Cube) = "" {}
        
        [Header(Iridescence)]
        [Space(5)]
        [Toggle(_SB_IRIDESCENCE)] _UseIridescence("Enable Iridescence", Float) = 0
        _IridescenceColor("Iridescence Tint", Color) = (1, 1, 1, 1)
        _IridescenceStrength("Strength", Range(0, 2)) = 1.0
        _IridescenceScale("Scale", Range(0.1, 10)) = 2.0
        _IridescenceShift("Hue Shift", Range(0, 1)) = 0.0
        _IridescenceSpeed("Animation Speed", Range(0, 2)) = 0.0
        
        [Header(Fresnel)]
        [Space(5)]
        [Toggle(_SB_FRESNEL)] _UseFresnel("Enable Fresnel", Float) = 1
        _FresnelColor("Fresnel Color", Color) = (1, 1, 1, 1)
        _FresnelPower("Fresnel Power", Range(0.1, 10)) = 3.0
        _FresnelIntensity("Fresnel Intensity", Range(0, 3)) = 1.0
        _FresnelMin("Fresnel Min (Center)", Range(0, 1)) = 0.0
        _FresnelMax("Fresnel Max (Edge)", Range(0, 1)) = 1.0
        [Toggle] _FresnelInvert("Invert Fresnel", Float) = 0
        [Toggle] _FresnelAffectAlpha("Affect Alpha", Float) = 1
        [Toggle] _FresnelAffectReflection("Affect Reflection", Float) = 1
        
        [Header(Surface Noise)]
        [Space(5)]
        [Toggle(_SB_SURFACE_NOISE)] _UseSurfaceNoise("Enable Surface Noise", Float) = 0
        _SurfaceNoiseScale("Noise Scale", Range(1, 500)) = 100.0
        _SurfaceNoiseStrength("Noise Strength", Range(0, 100)) = 0.1
        _SurfaceNoiseDistortion("Normal Distortion", Range(0, 10)) = 0.3
        _SurfaceNoiseSpeed("Animation Speed", Range(0, 2)) = 0.0
        
        [Header(Tint Texture)]
        [Space(5)]
        [Toggle(_SB_TINT_TEXTURE)] _UseTintTexture("Enable Tint Texture", Float) = 0
        _TintTexture("Tint Texture", 2D) = "white" {}
        _TintTextureColor("Tint Color", Color) = (1, 1, 1, 1)
        _TintTextureStrength("Tint Strength", Range(0, 1)) = 1.0
        _TintTextureBlend("Alpha Blend", Range(0, 1)) = 0.0
        _TintDistortionAmount("Distortion Amount", Range(0, 1)) = 0.0
        
        [Header(Edge Darkening)]
        [Space(5)]
        [Toggle(_SB_EDGE_DARKENING)] _UseEdgeDarkening("Enable Edge Darkening", Float) = 0
        _EdgeDarkeningStrength("Strength", Range(0, 1)) = 0.3
        _EdgeDarkeningPower("Power", Range(0.5, 5)) = 2.0
        _EdgeDarkeningDistance("Distance", Range(0, 2)) = 1.0
        
        [Header(Inner Glow)]
        [Space(5)]
        [Toggle(_SB_INNER_GLOW)] _UseInnerGlow("Enable Inner Glow", Float) = 0
        _InnerGlowColor("Glow Color", Color) = (1, 1, 1, 1)
        _InnerGlowStrength("Strength", Range(0, 2)) = 0.5
        _InnerGlowPower("Power", Range(0.5, 5)) = 2.0
        _InnerGlowFalloff("Falloff", Range(0.5, 5)) = 2.0
        
        [Header(Thickness)]
        [Space(5)]
        [Toggle(_SB_THICKNESS_MAP)] _UseThicknessMap("Enable Thickness Map", Float) = 0
        _ThicknessMap("Thickness Map", 2D) = "white" {}
        _ThicknessMin("Thickness Min", Range(0, 1)) = 0.1
        _ThicknessMax("Thickness Max", Range(0, 1)) = 1.0
        _ThicknessAffectsColor("Affects Color", Range(0, 1)) = 0.5
        _ThicknessAffectsDistortion("Affects Distortion", Range(0, 1)) = 0.3
        
        [Header(Depth Fade)]
        [Space(5)]
        [Toggle(_SB_DEPTH_FADE)] _UseDepthFade("Enable Depth Fade", Float) = 0
        _DepthFadeDistance("Fade Distance", Range(0.001, 10)) = 1.0
        _DepthFadeColor("Fade Color", Color) = (1, 1, 1, 1)
        
        [Header(Rain)]
        [Space(5)]
        [Toggle(_SB_RAIN)] _UseRain("Enable Rain", Float) = 0
        _RainTexture("Rain Normal Map", 2D) = "bump" {}
        _RainIntensity("Rain Intensity", Range(0, 1)) = 0.5
        [Space(3)]
        [Header(Mapping Mode)]
        [Toggle(_SB_RAIN_TRIPLANAR)] _UseRainTriplanar("Triplanar (Object Space)", Float) = 0
        _RainTriplanarScale("Triplanar Scale", Float) = 1.0
        _RainTriplanarSharpness("Triplanar Sharpness", Range(1, 10)) = 4.0
        [Space(3)]
        [Header(Tiling and Direction)]
        _RainTiling("Tiling (XY)", Vector) = (4, 4, 0, 0)
        _RainOffset("Offset (XY)", Vector) = (0, 0, 0, 0)
        _RainRotation("Rotation", Range(0, 360)) = 0
        [Space(3)]
        [Header(Animation)]
        _RainSpeed("Speed (XY)", Vector) = (0, 0.1, 0, 0)
        [Space(3)]
        [Header(Effect Strength)]
        _RainNormalStrength("Normal Strength", Range(0, 3)) = 1
        _RainDistortion("Refraction Distortion", Range(0, 1)) = 0.5
        _RainWetness("Surface Wetness", Range(0, 1)) = 0.3
        
        [Header(Rim Lighting)]
        [Space(5)]
        [Toggle(_SB_RIM)] _UseRim("Enable Rim", Float) = 0
        _RimColor("Rim Color", Color) = (1, 1, 1, 1)
        _RimPower("Rim Power", Range(0.1, 10)) = 3.0
        _RimIntensity("Rim Intensity", Range(0, 2)) = 0.5
        _RimMin("Rim Min", Range(0, 1)) = 0.0
        _RimMax("Rim Max", Range(0, 1)) = 1.0
        
        [Header(Specular)]
        [Space(5)]
        [Toggle(_SB_SPECULAR)] _UseSpecular("Enable Specular", Float) = 1
        _SpecularColor("Specular Color", Color) = (1, 1, 1, 1)
        _SpecularIntensity("Specular Intensity", Range(0, 10)) = 1.0
        [Space(3)]
        [Header(Shape)]
        _SpecularSize("Highlight Size", Range(0.001, 1)) = 0.5
        _SpecularSmoothness("Smoothness", Range(0, 1)) = 0.8
        _SpecularHardness("Edge Hardness", Range(0, 1)) = 0.0
        [Space(3)]
        [Header(Stylized)]
        [Toggle] _SpecularToon("Toon Mode", Float) = 0
        _SpecularSteps("Toon Steps", Range(1, 10)) = 2
        _SpecularThreshold("Toon Threshold", Range(0, 1)) = 0.5
        [Space(3)]
        [Header(Specular Options)]
        [Toggle] _SpecularFresnel("Fresnel Specular", Float) = 0
        _SpecularAnisotropy("Anisotropy", Range(-1, 1)) = 0.0
        _DiffuseIntensity("Diffuse Intensity", Range(0, 1)) = 0.3
        
        [Header(Translucency)]
        [Space(5)]
        [Toggle(_SB_TRANSLUCENT)] _UseTranslucent("Enable Translucency", Float) = 0
        _TranslucentColor("Translucent Color", Color) = (1, 0.9, 0.8, 1)
        _TranslucentIntensity("Intensity", Range(0, 10)) = 3.0
        _TranslucentPower("Falloff Power", Range(1, 16)) = 2.0
        _TranslucentDistortion("Normal Distortion", Range(-1, 1)) = 0.2
        _TranslucentScale("Scale", Range(0, 5)) = 1.0
        
        [Header(Occlusion)]
        [Space(5)]
        [Toggle(_SB_OCCLUSION_MAP)] _UseOcclusion("Enable Occlusion Map", Float) = 0
        _OcclusionMap("Occlusion Map", 2D) = "white" {}
        _OcclusionStrength("Occlusion Strength", Range(0, 1)) = 1.0
        
        [Header(Emission)]
        [Space(5)]
        [Toggle(_SB_EMISSION)] _UseEmission("Enable Emission", Float) = 0
        [Toggle(_SB_EMISSION_MAP)] _UseEmissionMap("Use Emission Map", Float) = 0
        _EmissionMap("Emission Map", 2D) = "white" {}
        [HDR] _EmissionColor("Emission Color", Color) = (0, 0, 0, 0)
        _EmissionIntensity("Emission Intensity", Range(0, 10)) = 1.0
        
        [Header(Triplanar)]
        [Space(5)]
        [Toggle(_SB_TRIPLANAR)] _UseTriplanar("Enable Triplanar", Float) = 0
        _TriplanarScale("Triplanar Scale", Float) = 1.0
        _TriplanarSharpness("Blend Sharpness", Range(1, 20)) = 8.0
        
        [Header(Advanced)]
        [Space(5)]
        
        [Header(Absorption)]
        [Toggle(_SB_ABSORPTION)] _UseAbsorption("Enable Absorption", Float) = 0
        _AbsorptionColor("Absorption Color", Color) = (0.2, 0.5, 0.3, 0.5)
        _AbsorptionDensity("Density", Range(0, 5)) = 1.0
        _AbsorptionFalloff("Falloff", Range(0.5, 3)) = 1.0
        
        [Header(Caustics)]
        [Toggle(_SB_CAUSTICS)] _UseCaustics("Enable Caustics", Float) = 0
        [Toggle(_SB_CAUSTICS_PROCEDURAL)] _UseCausticsProcedural("Procedural Caustics", Float) = 1
        _CausticsTexture("Caustics Texture", 2D) = "black" {}
        _CausticsColor("Caustics Color", Color) = (1, 1, 1, 1)
        _CausticsIntensity("Intensity", Range(0, 3)) = 1.0
        _CausticsScale("Scale", Range(0.1, 10)) = 2.0
        _CausticsSpeed("Speed", Range(0, 2)) = 0.5
        _CausticsDistortion("Distortion", Range(0, 1)) = 0.3
        
        [Header(Total Internal Reflection)]
        [Toggle(_SB_TIR)] _UseTIR("Enable TIR", Float) = 0
        _TIRIntensity("TIR Intensity", Range(0, 2)) = 1.0
        _TIRCriticalAngle("Critical Angle", Range(0.5, 1)) = 0.7
        _TIRSharpness("Edge Sharpness", Range(1, 20)) = 8.0
        
        [Header(Sparkle)]
        [Toggle(_SB_SPARKLE)] _UseSparkle("Enable Sparkle", Float) = 0
        _SparkleColor("Sparkle Color", Color) = (1, 1, 1, 1)
        _SparkleIntensity("Intensity", Range(0, 10)) = 3.0
        _SparkleScale("Scale", Range(5, 200)) = 50.0
        _SparkleSpeed("Animation Speed", Range(0, 10)) = 2.0
        _SparkleDensity("Density", Range(0, 0.5)) = 0.1
        _SparkleSize("Sparkle Size", Range(0.5, 5)) = 1.5
        
        [Header(Dirt and Moss)]
        [Space(5)]
        [Toggle(_SB_DUST)] _UseDust("Enable Dirt/Moss", Float) = 0
        
        [Space(3)]
        [Header(Direction)]
        [KeywordEnum(Bottom Up, Top Down, Normal Based)] _DirtDirection("Growth Direction", Float) = 0
        _DirtHeight("Height Level", Range(-2, 2)) = 0.0
        _DirtSpread("Spread (Transition)", Range(0.01, 2)) = 0.3
        _DirtSoftness("Edge Softness", Range(0, 1)) = 0.2
        
        [Space(3)]
        [Header(Appearance)]
        _DustTexture("Texture (R=Pattern)", 2D) = "white" {}
        _DustColor("Color", Color) = (0.35, 0.45, 0.25, 1)
        _DirtColorVariation("Color Variation", Color) = (0.25, 0.35, 0.18, 1)
        _DirtVariationScale("Variation Scale", Range(0.1, 10)) = 2.0
        _DustTiling("Texture Tiling", Float) = 3.0
        
        [Space(3)]
        [Header(Coverage)]
        _DustIntensity("Amount", Range(0, 1)) = 0.7
        _DustCoverage("Coverage Threshold", Range(0, 1)) = 0.5
        _DirtFullOpacity("Full Opacity", Range(0, 1)) = 1.0
        
        [Space(3)]
        [Header(Surface)]
        _DustRoughness("Roughness", Range(0, 1)) = 0.8
        _DustNormalBlend("Normal Blend", Range(0, 1)) = 0.5
        
        [Space(3)]
        [Header(Edge Noise)]
        [Toggle] _DirtUseEdgeNoise("Use Edge Noise", Float) = 1
        _DirtEdgeNoiseScale("Noise Scale", Range(0.1, 20)) = 5.0
        _DirtEdgeNoiseStrength("Noise Strength", Range(0, 1)) = 0.4
        
        [Space(3)]
        [Header(Advanced)]
        [Toggle] _DustEdgeFalloff("Fresnel Falloff", Float) = 0
        _DustEdgePower("Fresnel Power", Range(0.1, 5)) = 2.0
        [Toggle(_SB_DUST_TRIPLANAR)] _UseDustTriplanar("Triplanar Mapping", Float) = 0
        _DustTriplanarScale("Triplanar Scale", Float) = 1.0
        _DustTriplanarSharpness("Triplanar Sharpness", Range(1, 10)) = 4.0
        _DustTriplanarRotation("Triplanar Rotation", Range(0, 360)) = 0.0
        
        [Header(Damage)]
        [Space(5)]
        [Toggle(_SB_DAMAGE)] _UseDamage("Enable Damage", Float) = 0
        _DamageProgression("Damage Progression", Range(0, 1)) = 0.0
        _ProceduralCrackDensity("Crack Density", Range(2, 20)) = 8.0
        _ProceduralCrackSeed("Crack Seed", Range(0, 100)) = 0.0
        _CrackWidth("Crack Width", Range(0.01, 2)) = 0.5
        _CrackSharpness("Crack Sharpness", Range(0.1, 5)) = 1.0
        _CrackColor("Crack Color", Color) = (0.1, 0.1, 0.1, 1)
        _CrackDepth("Crack Depth (Normal)", Range(0, 2)) = 0.5
        _CrackEmission("Crack Edge Emission", Range(0, 5)) = 0.5
        _ShatterDistortion("Shatter Distortion", Range(0, 1)) = 0.2
        _DamageMask("Crack Zone Mask (R)", 2D) = "white" {}
        _CrackNormalMap("Crack Normal Map", 2D) = "bump" {}
        
        [Header(Decals)]
        [Space(5)]
        [Toggle(_SB_DECALS)] _UseDecals("Enable Decals", Float) = 0
        
        [Space(5)]
        [Header(Decal 1)]
        _DecalTexture1("Texture (RGBA)", 2D) = "black" {}
        _DecalPosition1("Position XY", Vector) = (0.5, 0.5, 0, 0)
        _DecalSize1("Size", Range(0.05, 2)) = 0.3
        _DecalRotation1("Rotation", Range(0, 360)) = 0
        _DecalIntensity1("Intensity", Range(0, 1)) = 1
        [HDR] _DecalTint1("Tint", Color) = (1, 1, 1, 1)
        
        [Space(5)]
        [Header(Decal 2)]
        [Toggle(_SB_DECAL2)] _UseDecal2("Enable Decal 2", Float) = 0
        _DecalTexture2("Texture (RGBA)", 2D) = "black" {}
        _DecalPosition2("Position XY", Vector) = (0.3, 0.7, 0, 0)
        _DecalSize2("Size", Range(0.05, 2)) = 0.3
        _DecalRotation2("Rotation", Range(0, 360)) = 0
        _DecalIntensity2("Intensity", Range(0, 1)) = 1
        [HDR] _DecalTint2("Tint", Color) = (1, 1, 1, 1)
        
        [Space(5)]
        [Header(Decal 3)]
        [Toggle(_SB_DECAL3)] _UseDecal3("Enable Decal 3", Float) = 0
        _DecalTexture3("Texture (RGBA)", 2D) = "black" {}
        _DecalPosition3("Position XY", Vector) = (0.7, 0.3, 0, 0)
        _DecalSize3("Size", Range(0.05, 2)) = 0.3
        _DecalRotation3("Rotation", Range(0, 360)) = 0
        _DecalIntensity3("Intensity", Range(0, 1)) = 1
        [HDR] _DecalTint3("Tint", Color) = (1, 1, 1, 1)
        
        [Space(5)]
        [Header(Decal 4)]
        [Toggle(_SB_DECAL4)] _UseDecal4("Enable Decal 4", Float) = 0
        _DecalTexture4("Texture (RGBA)", 2D) = "black" {}
        _DecalPosition4("Position XY", Vector) = (0.5, 0.2, 0, 0)
        _DecalSize4("Size", Range(0.05, 2)) = 0.3
        _DecalRotation4("Rotation", Range(0, 360)) = 0
        _DecalIntensity4("Intensity", Range(0, 1)) = 1
        [HDR] _DecalTint4("Tint", Color) = (1, 1, 1, 1)
        
        [Header(Fingerprints)]
        [Space(5)]
        [Toggle(_SB_FINGERPRINTS)] _UseFingerprints("Enable Fingerprints", Float) = 0
        _FingerprintTint("Global Tint", Color) = (0.95, 0.93, 0.88, 0.5)
        
        [Space(5)]
        [Header(Slot 1)]
        _FingerprintTexture1("Texture (R=mask)", 2D) = "white" {}
        [KeywordEnum(UV, World, Triplanar)] _FingerprintMapping1("Mapping Mode", Float) = 0
        _FingerprintPos1("UV Position XY", Vector) = (0.5, 0.5, 0, 0)
        _FingerprintScale1("UV Scale", Vector) = (0.2, 0.3, 1, 1)
        _FingerprintWorldPos1("Local Position XYZ", Vector) = (0, 0, 0, 0)
        _FingerprintWorldRadius1("Radius", Range(0.01, 2)) = 0.15
        _FingerprintTriplanarScale1("Triplanar Scale", Range(0.1, 10)) = 1.0
        _FingerprintRotation1("Rotation", Range(0, 360)) = 0
        _FingerprintIntensity1("Intensity", Range(0, 1)) = 0.7
        _FingerprintRoughness1("Roughness Add", Range(0, 1)) = 0.3
        _FingerprintFalloff1("Edge Falloff", Range(0, 1)) = 0.3
        
        [Space(5)]
        [Header(Slot 2)]
        [Toggle(_SB_FINGERPRINTS_SLOT2)] _UseSlot2("Enable Slot 2", Float) = 0
        _FingerprintTexture2("Texture (R=mask)", 2D) = "white" {}
        [KeywordEnum(UV, World, Triplanar)] _FingerprintMapping2("Mapping Mode", Float) = 0
        _FingerprintPos2("UV Position XY", Vector) = (0.7, 0.3, 0, 0)
        _FingerprintScale2("UV Scale", Vector) = (0.15, 0.2, 1, 1)
        _FingerprintWorldPos2("Local Position XYZ", Vector) = (0.3, 0, 0, 0)
        _FingerprintWorldRadius2("Radius", Range(0.01, 2)) = 0.12
        _FingerprintTriplanarScale2("Triplanar Scale", Range(0.1, 10)) = 1.0
        _FingerprintRotation2("Rotation", Range(0, 360)) = 0
        _FingerprintIntensity2("Intensity", Range(0, 1)) = 0.5
        _FingerprintRoughness2("Roughness Add", Range(0, 1)) = 0.25
        _FingerprintFalloff2("Edge Falloff", Range(0, 1)) = 0.35
        
        [Space(5)]
        [Header(Slot 3)]
        [Toggle(_SB_FINGERPRINTS_SLOT3)] _UseSlot3("Enable Slot 3", Float) = 0
        _FingerprintTexture3("Texture (R=mask)", 2D) = "white" {}
        [KeywordEnum(UV, World, Triplanar)] _FingerprintMapping3("Mapping Mode", Float) = 0
        _FingerprintPos3("UV Position XY", Vector) = (0.3, 0.6, 0, 0)
        _FingerprintScale3("UV Scale", Vector) = (0.25, 0.25, 1, 1)
        _FingerprintWorldPos3("Local Position XYZ", Vector) = (-0.3, 0, 0, 0)
        _FingerprintWorldRadius3("Radius", Range(0.01, 2)) = 0.1
        _FingerprintTriplanarScale3("Triplanar Scale", Range(0.1, 10)) = 1.0
        _FingerprintRotation3("Rotation", Range(0, 360)) = 0
        _FingerprintIntensity3("Intensity", Range(0, 1)) = 0.4
        _FingerprintRoughness3("Roughness Add", Range(0, 1)) = 0.2
        _FingerprintFalloff3("Edge Falloff", Range(0, 1)) = 0.4
        
        [Space(5)]
        [Header(Slot 4)]
        [Toggle(_SB_FINGERPRINTS_SLOT4)] _UseSlot4("Enable Slot 4", Float) = 0
        _FingerprintTexture4("Texture (R=mask)", 2D) = "white" {}
        [KeywordEnum(UV, World, Triplanar)] _FingerprintMapping4("Mapping Mode", Float) = 0
        _FingerprintPos4("UV Position XY", Vector) = (0.5, 0.8, 0, 0)
        _FingerprintScale4("UV Scale", Vector) = (0.3, 0.15, 1, 1)
        _FingerprintWorldPos4("Local Position XYZ", Vector) = (0, 0.5, 0, 0)
        _FingerprintWorldRadius4("Radius", Range(0.01, 2)) = 0.08
        _FingerprintTriplanarScale4("Triplanar Scale", Range(0.1, 10)) = 1.0
        _FingerprintRotation4("Rotation", Range(0, 360)) = 0
        _FingerprintIntensity4("Intensity", Range(0, 1)) = 0.3
        _FingerprintRoughness4("Roughness Add", Range(0, 1)) = 0.15
        _FingerprintFalloff4("Edge Falloff", Range(0, 1)) = 0.5
        
        [Header(Distortion FX)]
        [Space(5)]
        [Toggle(_SB_DISTORTION)] _UseDistortion("Enable Distortion FX", Float) = 0
        
        [Space(5)]
        [Header(Magnify)]
        [Toggle(_SB_MAGNIFY)] _UseMagnify("Enable Magnify", Float) = 0
        _MagnifyStrength("Magnify Strength", Range(-2, 2)) = 0.5
        _MagnifyCenter("Center XY", Vector) = (0.5, 0.5, 0, 0)
        _MagnifyRadius("Radius", Range(0.01, 1)) = 0.3
        _MagnifyFalloff("Falloff", Range(0.01, 1)) = 0.1
        
        [Space(5)]
        [Header(Barrel)]
        [Toggle(_SB_BARREL)] _UseBarrel("Enable Barrel Distortion", Float) = 0
        _BarrelStrength("Barrel Strength", Range(-1, 1)) = 0.2
        
        [Space(5)]
        [Header(Waves)]
        [Toggle(_SB_WAVES)] _UseWaves("Enable Waves", Float) = 0
        _WaveAmplitude("Wave Amplitude", Range(0, 0.1)) = 0.02
        _WaveFrequency("Wave Frequency", Range(1, 50)) = 10
        _WaveSpeed("Wave Speed", Range(0, 10)) = 2
        [Toggle] _WaveRadial("Radial Waves", Float) = 0
        
        [Space(5)]
        [Header(Ripple)]
        [Toggle(_SB_RIPPLE)] _UseRipple("Enable Ripple", Float) = 0
        _RippleCenter("Center XY", Vector) = (0.5, 0.5, 0, 0)
        _RippleAmplitude("Amplitude", Range(0, 0.1)) = 0.03
        _RippleFrequency("Frequency", Range(5, 100)) = 30
        _RippleSpeed("Speed", Range(0, 10)) = 3
        _RippleDecay("Decay", Range(0, 10)) = 2
        
        [Space(5)]
        [Header(Swirl)]
        [Toggle(_SB_SWIRL)] _UseSwirl("Enable Swirl", Float) = 0
        _SwirlCenter("Center XY", Vector) = (0.5, 0.5, 0, 0)
        _SwirlStrength("Strength", Range(-5, 5)) = 1
        _SwirlRadius("Radius", Range(0.1, 2)) = 0.5
        _SwirlSpeed("Animation Speed", Range(0, 5)) = 0
        
        [Space(5)]
        [Header(Heat Haze)]
        [Toggle(_SB_HEAT_HAZE)] _UseHeatHaze("Enable Heat Haze", Float) = 0
        _HeatHazeStrength("Strength", Range(0, 0.1)) = 0.02
        _HeatHazeSpeed("Speed", Range(0, 10)) = 3
        _HeatHazeScale("Scale", Range(1, 50)) = 15
        
        [Space(5)]
        [Header(Pixelate)]
        [Toggle(_SB_PIXELATE)] _UsePixelate("Enable Pixelate", Float) = 0
        _PixelateSize("Pixel Size", Range(1, 100)) = 20
        
        [Header(Shadows)]
        [Space(5)]
        [Toggle(_SB_RECEIVE_SHADOWS)] _ReceiveShadows("Receive Shadows", Float) = 1
        _ShadowIntensity("Shadow Intensity", Range(0, 1)) = 0.3
        
        [Header(Rendering)]
        [Space(5)]
        [KeywordEnum(Standard, Additive, Soft)] _BlendMode("Blend Mode", Float) = 0
        [Enum(UnityEngine.Rendering.CullMode)] _Cull("Cull Mode", Float) = 2
        [Enum(Off, 0, On, 1)] _ZWrite("ZWrite", Float) = 0
        [Enum(UnityEngine.Rendering.CompareFunction)] _ZTest("ZTest", Float) = 4
        
        [HideInInspector] _SrcBlend("Src Blend", Float) = 5
        [HideInInspector] _DstBlend("Dst Blend", Float) = 10
    }
    
    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "Transparent"
            "Queue" = "Transparent"
            "IgnoreProjector" = "True"
            "UniversalMaterialType" = "Lit"
            "DisableBatching" = "False"
        }
        
        LOD 300
        
        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }
            
            Blend [_SrcBlend] [_DstBlend]
            ZWrite [_ZWrite]
            ZTest [_ZTest]
            Cull [_Cull]
            
            HLSLPROGRAM
            #pragma target 3.0
            #pragma exclude_renderers gles
            
            #pragma vertex SB_VertexForward
            #pragma fragment SB_FragmentForward
            
            // ============ UNITY VERSION COMPATIBILITY ============
            // Unity 2021-2022: Standard URP keywords
            // Unity 6+: Updated keywords with _FORWARD_PLUS
            
            // ============ GPU INSTANCING ============
            #pragma multi_compile_instancing
            #pragma instancing_options renderinglayer
            
            // ============ VR SUPPORT ============
            // Single Pass Instanced (Unity 2021+)
            #pragma multi_compile _ STEREO_INSTANCING_ON STEREO_MULTIVIEW_ON
            
            // ============ FOG & VOLUMETRICS ============
            #pragma multi_compile_fog
            #pragma multi_compile _ _LIGHT_COOKIES
            
            // ============ LIGHTING ============
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile _ _SHADOWS_SOFT
            #pragma multi_compile _ _MIXED_LIGHTING_SUBTRACTIVE
            #pragma multi_compile _ _REFLECTION_PROBE_BLENDING
            #pragma multi_compile _ _REFLECTION_PROBE_BOX_PROJECTION
            #pragma multi_compile _ _SCREEN_SPACE_OCCLUSION
            #pragma multi_compile _ _LIGHT_LAYERS
            
            // ============ DECAL SUPPORT ============
            #pragma multi_compile_fragment _ _DBUFFER_MRT1 _DBUFFER_MRT2 _DBUFFER_MRT3
            
            // ============ UNITY 6+ / URP 17+ (Forward+) ============
            // Note: Forward+ keywords are handled automatically by URP when enabled.
            // No explicit pragmas needed - URP injects them at runtime.
            // This ensures compatibility across Unity 2021, 2022, and Unity 6.
            
            #pragma shader_feature_local _SB_QUALITY_LOW _SB_QUALITY_MEDIUM _SB_QUALITY_HIGH
            #pragma shader_feature_local _SB_NORMALMAP
            #pragma shader_feature_local _SB_METALLICGLOSS_MAP
            #pragma shader_feature_local _SB_DETAIL_ALBEDO
            #pragma shader_feature_local _SB_DETAIL_NORMAL
            #pragma shader_feature_local _SB_DETAIL_NORMAL_TRIPLANAR
            #pragma shader_feature_local _SB_REFRACTION
            #pragma shader_feature_local _SB_IOR
            #pragma shader_feature_local _SB_CHROMATIC_ABERRATION
            #pragma shader_feature_local _SB_BLUR
            #pragma shader_feature_local _SB_REFLECTION
            #pragma shader_feature_local _SB_REFLECTION_CUBEMAP
            #pragma shader_feature_local _SB_IRIDESCENCE
            #pragma shader_feature_local _SB_FRESNEL
            #pragma shader_feature_local _SB_SPECULAR
            #pragma shader_feature_local _SB_RIM
            #pragma shader_feature_local _SB_TRANSLUCENT
            #pragma shader_feature_local _SB_OCCLUSION_MAP
            #pragma shader_feature_local _SB_EMISSION
            #pragma shader_feature_local _SB_EMISSION_MAP
            #pragma shader_feature_local _SB_ALPHA_CLIP
            #pragma shader_feature_local _SB_FALLOFF_OPACITY
            #pragma shader_feature_local _SB_SURFACE_NOISE
            #pragma shader_feature_local _SB_TINT_TEXTURE
            #pragma shader_feature_local _SB_EDGE_DARKENING
            #pragma shader_feature_local _SB_INNER_GLOW
            #pragma shader_feature_local _SB_THICKNESS_MAP
            #pragma shader_feature_local _SB_DEPTH_FADE
            #pragma shader_feature_local _SB_TRIPLANAR
            #pragma shader_feature_local _SB_ABSORPTION
            #pragma shader_feature_local _SB_CAUSTICS
            #pragma shader_feature_local _SB_CAUSTICS_PROCEDURAL
            #pragma shader_feature_local _SB_TIR
            #pragma shader_feature_local _SB_SPARKLE
            #pragma shader_feature_local _SB_DUST
            #pragma shader_feature_local _SB_DUST_TRIPLANAR
            #pragma shader_feature_local _SB_DAMAGE
            #pragma shader_feature_local _DIRTDIRECTION_BOTTOM_UP _DIRTDIRECTION_TOP_DOWN _DIRTDIRECTION_NORMAL_BASED
            #pragma shader_feature_local _SB_DECALS
            #pragma shader_feature_local _SB_DECAL2
            #pragma shader_feature_local _SB_DECAL3
            #pragma shader_feature_local _SB_DECAL4
            #pragma shader_feature_local _SB_FINGERPRINTS
            #pragma shader_feature_local _FINGERPRINTMAPPING1_UV _FINGERPRINTMAPPING1_WORLD _FINGERPRINTMAPPING1_TRIPLANAR
            #pragma shader_feature_local _SB_FINGERPRINTS_SLOT2
            #pragma shader_feature_local _FINGERPRINTMAPPING2_UV _FINGERPRINTMAPPING2_WORLD _FINGERPRINTMAPPING2_TRIPLANAR
            #pragma shader_feature_local _SB_FINGERPRINTS_SLOT3
            #pragma shader_feature_local _FINGERPRINTMAPPING3_UV _FINGERPRINTMAPPING3_WORLD _FINGERPRINTMAPPING3_TRIPLANAR
            #pragma shader_feature_local _SB_FINGERPRINTS_SLOT4
            #pragma shader_feature_local _FINGERPRINTMAPPING4_UV _FINGERPRINTMAPPING4_WORLD _FINGERPRINTMAPPING4_TRIPLANAR
            #pragma shader_feature_local _SB_RAIN
            #pragma shader_feature_local _SB_RAIN_TRIPLANAR
            #pragma shader_feature_local _SB_DISTORTION
            #pragma shader_feature_local _SB_MAGNIFY
            #pragma shader_feature_local _SB_BARREL
            #pragma shader_feature_local _SB_WAVES
            #pragma shader_feature_local _SB_RIPPLE
            #pragma shader_feature_local _SB_SWIRL
            #pragma shader_feature_local _SB_HEAT_HAZE
            #pragma shader_feature_local _SB_PIXELATE
            #pragma shader_feature_local _SB_RECEIVE_SHADOWS
            
            #include "Includes/SB_GlassPass.hlsl"
            ENDHLSL
        }
        
        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }
            
            ZWrite On
            ZTest LEqual
            ColorMask 0
            Cull [_Cull]
            
            HLSLPROGRAM
            #pragma target 3.0
            #pragma exclude_renderers gles
            
            #pragma vertex SB_VertexShadow
            #pragma fragment SB_FragmentShadow
            
            // GPU Instancing & VR
            #pragma multi_compile_instancing
            #pragma multi_compile _ STEREO_INSTANCING_ON STEREO_MULTIVIEW_ON
            
            // Shadow variants
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW
            
            #pragma shader_feature_local _SB_ALPHA_CLIP
            
            #define SB_SHADOW_CASTER_PASS
            #include "Includes/SB_GlassPass.hlsl"
            ENDHLSL
        }
        
        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }
            
            ZWrite On
            ColorMask R
            Cull [_Cull]
            
            HLSLPROGRAM
            #pragma target 3.0
            #pragma exclude_renderers gles
            
            #pragma vertex SB_VertexDepth
            #pragma fragment SB_FragmentDepth
            
            // GPU Instancing & VR
            #pragma multi_compile_instancing
            #pragma multi_compile _ STEREO_INSTANCING_ON STEREO_MULTIVIEW_ON
            
            #pragma shader_feature_local _SB_ALPHA_CLIP
            
            #define SB_DEPTH_ONLY_PASS
            #include "Includes/SB_GlassPass.hlsl"
            ENDHLSL
        }
        
        // DepthNormals Pass - Required for Depth Priming and SSAO
        Pass
        {
            Name "DepthNormals"
            Tags { "LightMode" = "DepthNormals" }
            
            ZWrite On
            Cull [_Cull]
            
            HLSLPROGRAM
            #pragma target 3.0
            #pragma exclude_renderers gles
            
            #pragma vertex SB_VertexDepthNormals
            #pragma fragment SB_FragmentDepthNormals
            
            // GPU Instancing & VR
            #pragma multi_compile_instancing
            #pragma multi_compile _ STEREO_INSTANCING_ON STEREO_MULTIVIEW_ON
            
            #pragma shader_feature_local _SB_ALPHA_CLIP
            #pragma shader_feature_local _SB_NORMALMAP
            
            #define SB_DEPTH_NORMALS_PASS
            #include "Includes/SB_GlassPass.hlsl"
            ENDHLSL
        }
        
        Pass
        {
            Name "Meta"
            Tags { "LightMode" = "Meta" }
            
            Cull Off
            
            HLSLPROGRAM
            #pragma target 3.0
            #pragma exclude_renderers gles
            
            #pragma vertex SB_VertexMeta
            #pragma fragment SB_FragmentMeta
            
            // GPU Instancing
            #pragma multi_compile_instancing
            
            #pragma shader_feature_local _SB_EMISSION
            #pragma shader_feature_local _SB_EMISSION_MAP
            
            #define SB_META_PASS
            #include "Includes/SB_GlassPass.hlsl"
            ENDHLSL
        }
    }
    
    CustomEditor "SingularBear.Shaders.SB_GlassShaderEditor"
    FallBack "Universal Render Pipeline/Lit"
}
