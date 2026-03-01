// ============================================================================
// SB_GlassInput.hlsl
// Material properties and CBUFFER for SRP Batcher compatibility
// ============================================================================
// IMPORTANT: All properties must be inside CBUFFER_START/CBUFFER_END
// for SRP Batcher to work correctly. Textures are declared outside.
// ============================================================================

#ifndef SB_GLASS_INPUT_INCLUDED
#define SB_GLASS_INPUT_INCLUDED

// ============================================================================
// TEXTURE DECLARATIONS (Outside CBUFFER for SRP Batcher)
// ============================================================================

TEXTURE2D(_MainTex);            SAMPLER(sampler_MainTex);
TEXTURE2D(_BumpMap);            SAMPLER(sampler_BumpMap);

#if defined(_SB_DETAIL_ALBEDO) || defined(_SB_DETAIL_NORMAL)
    TEXTURE2D(_DetailAlbedoMap);    SAMPLER(sampler_DetailAlbedoMap);
    TEXTURE2D(_DetailNormalMap);    SAMPLER(sampler_DetailNormalMap);
#endif

#if defined(_SB_METALLICGLOSS_MAP)
    TEXTURE2D(_MetallicGlossMap);   SAMPLER(sampler_MetallicGlossMap);
#endif

#if defined(_SB_OCCLUSION_MAP)
    TEXTURE2D(_OcclusionMap);       SAMPLER(sampler_OcclusionMap);
#endif

#if defined(_SB_EMISSION_MAP)
    TEXTURE2D(_EmissionMap);        SAMPLER(sampler_EmissionMap);
#endif

#if defined(_SB_REFLECTION_CUBEMAP)
    TEXTURECUBE(_ReflectionCube);   SAMPLER(sampler_ReflectionCube);
#endif

#if defined(_SB_TINT_TEXTURE)
    TEXTURE2D(_TintTexture);        SAMPLER(sampler_TintTexture);
#endif

#if defined(_SB_THICKNESS_MAP)
    TEXTURE2D(_ThicknessMap);       SAMPLER(sampler_ThicknessMap);
#endif
#if defined(_SB_CAUSTICS)
    TEXTURE2D(_CausticsTexture);    SAMPLER(sampler_CausticsTexture);
#endif

#if defined(_SB_DUST)
    TEXTURE2D(_DustTexture);        SAMPLER(sampler_DustTexture);
#endif

#if defined(_SB_DAMAGE)
    TEXTURE2D(_DamageMask);         SAMPLER(sampler_DamageMask);
    TEXTURE2D(_CrackNormalMap);     SAMPLER(sampler_CrackNormalMap);
#endif

#if defined(_SB_FINGERPRINTS)
    TEXTURE2D(_FingerprintTexture); SAMPLER(sampler_FingerprintTexture);
#endif
#if defined(_SB_FINGERPRINTS)
    TEXTURE2D(_FingerprintTexture1); SAMPLER(sampler_FingerprintTexture1);
    TEXTURE2D(_FingerprintTexture2); SAMPLER(sampler_FingerprintTexture2);
    TEXTURE2D(_FingerprintTexture3); SAMPLER(sampler_FingerprintTexture3);
    TEXTURE2D(_FingerprintTexture4); SAMPLER(sampler_FingerprintTexture4);
#endif

// Decal textures
#if defined(_SB_DECALS)
    TEXTURE2D(_DecalTexture1); SAMPLER(sampler_DecalTexture1);
    TEXTURE2D(_DecalTexture2); SAMPLER(sampler_DecalTexture2);
    TEXTURE2D(_DecalTexture3); SAMPLER(sampler_DecalTexture3);
    TEXTURE2D(_DecalTexture4); SAMPLER(sampler_DecalTexture4);
#endif

// Rain texture
#if defined(_SB_RAIN)
    TEXTURE2D(_RainTexture); SAMPLER(sampler_RainTexture);
#endif

CBUFFER_START(UnityPerMaterial)
    
    float4 _MainTex_ST;
    float4 _BumpMap_ST;
    float4 _MetallicGlossMap_ST;
    float4 _OcclusionMap_ST;
    float4 _EmissionMap_ST;
    float4 _ThicknessMap_ST;
    float4 _DetailAlbedoMap_ST;
    float4 _DetailNormalMap_ST;
    
    
    half4 _Color;
    half4 _DetailColor;
    half4 _RimColor;
    half4 _ReflectionColor;
    half4 _TranslucentColor;
    half4 _EmissionColor;
    half4 _SpecularColor;
    
    
    // Main surface
    half _Metallic;
    half _Smoothness;
    half _BumpScale;
    half _OcclusionStrength;
    
    half _Saturation;
    half _Brightness;
    half _Pad_Main1;
    half _Pad_Main2;
    
    // Refraction
    half _Distortion;
    half _IndexOfRefraction;
    half _ChromaticAberration;
    half _Pad_Refr1;
    
    // Group 2b: Physical IOR
    half _IOROrigin;        // IOR of source medium (1.0=air, 1.33=water)
    half _Pad_IOR1;
    half _Pad_IOR2;
    half _Pad_IOR3;
    
    // Reflection
    half _ReflectionIntensity;
    half _ReflectionBlur;
    half _Pad_Refl1;
    half _Pad_Refl2;
    
    // Fresnel
    half4 _FresnelColor;
    half _FresnelPower;
    half _FresnelIntensity;
    half _FresnelMin;
    half _FresnelMax;
    half _FresnelInvert;
    half _FresnelAffectAlpha;
    half _FresnelAffectReflection;
    half _FresnelPad1;
    
    // Rim
    half _RimPower;
    half _RimIntensity;
    half _RimMin;
    half _RimMax;
    
    // Detail
    half _DetailAlbedoIntensity;
    half _DetailNormalScale;
    half _DetailTiling;
    half _DetailNormalTiling;
    
    half _DetailNormalTriplanarScale;
    half _DetailNormalTriplanarSharpness;
    half _MainTint;
    half _Pad_Detail1;
    
    // Specular
    half _SpecularIntensity;
    half _SpecularSmoothness;
    half _SpecularSize;
    half _SpecularHardness;
    
    half _SpecularToon;
    half _SpecularSteps;
    half _SpecularThreshold;
    half _SpecularFresnel;
    
    half _SpecularAnisotropy;
    half _DiffuseIntensity;
    half _ShadowIntensity;
    half _SpecularPad1;
    
    // Translucent
    half _TranslucentIntensity;
    half _TranslucentPower;
    half _TranslucentDistortion;
    half _TranslucentScale;
    
    // Alpha/misc
    half _Opacity;
    half _AlphaClip;
    half _EmissionIntensity;
    half _Pad_Alpha1;
    
    // Falloff Opacity
    half _FalloffOpacityIntensity;
    half _FalloffOpacityPower;
    half _FalloffOpacityInvert;
    half _Pad_Falloff1;
    
    // Distortion FX
    half _MagnifyStrength;
    half _MagnifyRadius;
    half _MagnifyFalloff;
    half _BarrelStrength;
    
    // Distortion FX continued
    float4 _MagnifyCenter;
    
    // Wave distortion
    half _WaveAmplitude;
    half _WaveFrequency;
    half _WaveSpeed;
    half _SwirlStrength;
    
    // Swirl/Pixelate
    half _SwirlRadius;
    half _PixelateSize;
    half _HeatHazeStrength;
    half _HeatHazeScale;
    
    // Heat haze / Ripple
    half _HeatHazeSpeed;
    half _RippleAmplitude;
    half _RippleFrequency;
    half _RippleSpeed;
    
    // Ripple continued
    float4 _RippleCenter;
    half _RippleDecay;
    
    // Blur
    half _BlurStrength;
    half _BlurRadius;
    half _BlurQuality;
    
    // Triplanar
    half _TriplanarScale;
    half _TriplanarSharpness;
    half _Pad_Tri1;
    half _Pad_Tri2;
    
    // Shadow options
    half _ReceiveShadows;
    half _FlipReflection;
    half _FlipRefraction;
    half _Pad_Shadow1;
    
    // Depth Fade
    half _DepthFadeDistance;
    half _Pad_Depth1;
    half _Pad_Depth2;
    half _Pad_Depth3;
    
    // Depth Fade Color
    half4 _DepthFadeColor;
    
    // Iridescence
    half _IridescenceStrength;
    half _IridescenceScale;
    half _IridescenceShift;
    half _IridescenceSpeed;
    
    // Iridescence color
    half4 _IridescenceColor;
    
    // Surface Noise
    half _SurfaceNoiseScale;
    half _SurfaceNoiseStrength;
    half _SurfaceNoiseSpeed;
    half _SurfaceNoiseDistortion;
    
    // Tint Texture
    float4 _TintTexture_ST;
    half4 _TintTextureColor;
    half _TintTextureStrength;
    half _TintTextureBlend;
    half _TintDistortionAmount;
    half _Pad_Tint1;
    
    // Edge Effects
    half _EdgeDarkeningStrength;
    half _EdgeDarkeningPower;
    half _EdgeDarkeningDistance;
    half _Pad_Edge1;
    
    // Inner Glow
    half4 _InnerGlowColor;
    half _InnerGlowStrength;
    half _InnerGlowPower;
    half _InnerGlowFalloff;
    half _Pad_Glow1;
    
    // Thickness
    half _ThicknessMin;
    half _ThicknessMax;
    half _ThicknessAffectsColor;
    half _ThicknessAffectsDistortion;
    
    // FEATURES
    
    // Beer-Lambert Absorption
    half4 _AbsorptionColor;
    half _AbsorptionDensity;
    half _AbsorptionFalloff;
    half _AbsorptionPadding1;
    half _AbsorptionPadding2;
    
    // Caustics
    float4 _CausticsTexture_ST;
    half4 _CausticsColor;
    half _CausticsIntensity;
    half _CausticsScale;
    half _CausticsSpeed;
    half _CausticsDistortion;
    
    // Total Internal Reflection
    half _TIRIntensity;
    half _TIRCriticalAngle;
    half _TIRSharpness;
    half _TIRPadding1;
    
    // Sparkle/Glitter
    half4 _SparkleColor;
    half _SparkleIntensity;
    half _SparkleDensity;
    half _SparkleSpeed;
    half _SparkleScale;
    
    // Sparkle variation
    half _SparkleSize;
    half _SparkleThreshold;
    half _SparkleContrast;
    half _SparklePadding2;
    
    // Dirt/Moss System
    float4 _DustTexture_ST;
    half4 _DustColor;                     // Main dirt/moss color
    half4 _DirtColorVariation;            // Secondary color for variation
    
    half _DustIntensity;                  // Amount (0-1)
    half _DustCoverage;                   // Coverage threshold
    half _DustTiling;                     // Texture tiling
    half _DirtFullOpacity;                // Full opacity on covered areas
    
    half _DirtHeight;                     // Height level for growth
    half _DirtSpread;                     // Spread distance
    half _DirtSoftness;                   // Edge softness
    half _DirtVariationScale;             // Color variation scale
    
    half _DustRoughness;                  // Roughness on dirty areas
    half _DustNormalBlend;                // Normal blend strength
    half _DirtUseEdgeNoise;               // Use edge noise (0/1)
    half _DirtEdgeNoiseScale;             // Edge noise scale
    
    half _DirtEdgeNoiseStrength;          // Edge noise strength
    half _DustEdgeFalloff;                // Fresnel falloff toggle
    half _DustEdgePower;                  // Fresnel power
    half _DustTriplanarScale;             // Triplanar texture scale
    
    half _DustTriplanarSharpness;         // Triplanar blend sharpness
    half _DustTriplanarRotation;          // Triplanar rotation angle
    half _DirtPadding2;
    half _DirtPadding3;
    
    // Fingerprints System (Texture-based)
    half4 _FingerprintTint;           // Global tint color
    
    // Slot 1
    float4 _FingerprintPos1;          // xy=UV position
    float4 _FingerprintWorldPos1;     // xyz=world position
    float4 _FingerprintScale1;        // xy=UV scale
    float4 _FingerprintTexture1_ST;
    half _FingerprintMapping1;        // 0=UV, 1=World, 2=Triplanar
    half _FingerprintIntensity1;
    half _FingerprintRoughness1;
    half _FingerprintFalloff1;
    half _FingerprintRotation1;
    half _FingerprintWorldRadius1;
    half _FingerprintTriplanarScale1;
    half _FPPad1;
    
    // Slot 2
    float4 _FingerprintPos2;
    float4 _FingerprintWorldPos2;
    float4 _FingerprintScale2;
    float4 _FingerprintTexture2_ST;
    half _FingerprintMapping2;
    half _FingerprintIntensity2;
    half _FingerprintRoughness2;
    half _FingerprintFalloff2;
    half _FingerprintRotation2;
    half _FingerprintWorldRadius2;
    half _FingerprintTriplanarScale2;
    half _FPPad2;
    
    // Slot 3
    float4 _FingerprintPos3;
    float4 _FingerprintWorldPos3;
    float4 _FingerprintScale3;
    float4 _FingerprintTexture3_ST;
    half _FingerprintMapping3;
    half _FingerprintIntensity3;
    half _FingerprintRoughness3;
    half _FingerprintFalloff3;
    half _FingerprintRotation3;
    half _FingerprintWorldRadius3;
    half _FingerprintTriplanarScale3;
    half _FPPad3;
    
    // Slot 4
    float4 _FingerprintPos4;
    float4 _FingerprintWorldPos4;
    float4 _FingerprintScale4;
    float4 _FingerprintTexture4_ST;
    half _FingerprintMapping4;
    half _FingerprintIntensity4;
    half _FingerprintRoughness4;
    half _FingerprintFalloff4;
    half _FingerprintRotation4;
    half _FingerprintWorldRadius4;
    half _FingerprintTriplanarScale4;
    half _FPPad4;
    
    // Decals System
    // Decal 1
    float4 _DecalTexture1_ST;
    float4 _DecalPosition1;
    half4 _DecalTint1;
    half _DecalSize1;
    half _DecalRotation1;
    half _DecalIntensity1;
    half _DecalPad1;
    
    // Decal 2
    float4 _DecalTexture2_ST;
    float4 _DecalPosition2;
    half4 _DecalTint2;
    half _DecalSize2;
    half _DecalRotation2;
    half _DecalIntensity2;
    half _DecalPad2;
    
    // Decal 3
    float4 _DecalTexture3_ST;
    float4 _DecalPosition3;
    half4 _DecalTint3;
    half _DecalSize3;
    half _DecalRotation3;
    half _DecalIntensity3;
    half _DecalPad3;
    
    // Decal 4
    float4 _DecalTexture4_ST;
    float4 _DecalPosition4;
    half4 _DecalTint4;
    half _DecalSize4;
    half _DecalRotation4;
    half _DecalIntensity4;
    half _DecalPad4;
    
    // Rain Effect
    float4 _RainTexture_ST;
    float4 _RainTiling;           // xy = tiling
    float4 _RainOffset;           // xy = offset
    float4 _RainSpeed;            // xy = speed
    half _RainIntensity;
    half _RainRotation;
    half _RainNormalStrength;
    half _RainDistortion;
    half _RainWetness;
    half _RainTriplanarScale;
    half _RainTriplanarSharpness;
    half _RainPad1;
    
    // Damage / Procedural Cracks
    half _DamageProgression;
    half _ProceduralCrackDensity;
    half _ProceduralCrackSeed;
    half _CrackDepth;
    half _CrackEmission;
    half _ShatterDistortion;
    half _CrackWidth;
    half _CrackSharpness;
    half _DamagePad1;
    half4 _CrackColor;
    float4 _DamageMask_ST;
    
CBUFFER_END

// ============================================================================
// TEXTURE SAMPLING HELPERS
// ============================================================================

// Sample main albedo with tint
inline half4 SampleAlbedo(float2 uv)
{
    half4 albedo = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv);
    return albedo * _Color;
}

// Sample normal map with scale
inline half3 SampleNormal(float2 uv, half scale)
{
    half4 n = SAMPLE_TEXTURE2D(_BumpMap, sampler_BumpMap, uv);
    
#if defined(UNITY_NO_DXT5nm)
    half3 normal = n.xyz * 2.0 - 1.0;
#else
    // DXT5nm: normal stored in AG channels
    half3 normal;
    normal.xy = n.ag * 2.0 - 1.0;
    normal.z = sqrt(1.0 - saturate(dot(normal.xy, normal.xy)));
#endif
    
    normal.xy *= scale;
    return normalize(normal);
}

// Sample metallic/smoothness
inline half2 SampleMetallicSmoothness(float2 uv)
{
#if defined(_SB_METALLICGLOSS_MAP)
    half4 mg = SAMPLE_TEXTURE2D(_MetallicGlossMap, sampler_MetallicGlossMap, uv);
    return half2(mg.r * _Metallic, mg.a * _Smoothness);
#else
    return half2(_Metallic, _Smoothness);
#endif
}

// Sample occlusion
inline half SampleOcclusion(float2 uv)
{
#if defined(_SB_OCCLUSION_MAP)
    half occ = SAMPLE_TEXTURE2D(_OcclusionMap, sampler_OcclusionMap, uv).g;
    return lerp(1.0, occ, _OcclusionStrength);
#else
    return 1.0;
#endif
}

// Sample emission
inline half3 SampleEmission(float2 uv)
{
#if defined(_SB_EMISSION)
    #if defined(_SB_EMISSION_MAP)
        return SAMPLE_TEXTURE2D(_EmissionMap, sampler_EmissionMap, uv).rgb * _EmissionColor.rgb * _EmissionIntensity;
    #else
        return _EmissionColor.rgb * _EmissionIntensity;
    #endif
#else
    return half3(0.0, 0.0, 0.0);
#endif
}

// Sample detail maps
#if defined(_SB_DETAIL_ALBEDO) || defined(_SB_DETAIL_NORMAL)
inline half4 SampleDetailAlbedo(float2 uv)
{
    return SAMPLE_TEXTURE2D(_DetailAlbedoMap, sampler_DetailAlbedoMap, uv) * _DetailColor;
}

inline half3 SampleDetailNormal(float2 uv, half scale)
{
    half4 n = SAMPLE_TEXTURE2D(_DetailNormalMap, sampler_DetailNormalMap, uv);
    
#if defined(UNITY_NO_DXT5nm)
    half3 normal = n.xyz * 2.0 - 1.0;
#else
    half3 normal;
    normal.xy = n.ag * 2.0 - 1.0;
    normal.z = sqrt(1.0 - saturate(dot(normal.xy, normal.xy)));
#endif
    
    normal.xy *= scale;
    return normalize(normal);
}
#endif

// Sample reflection cubemap
#if defined(_SB_REFLECTION_CUBEMAP)
inline half3 SampleReflectionCube(half3 reflectDir, half blur)
{
    half mip = blur * 6.0; // Cubemap has ~6 mip levels
    return SAMPLE_TEXTURECUBE_LOD(_ReflectionCube, sampler_ReflectionCube, reflectDir, mip).rgb;
}
#endif

#endif // SB_GLASS_INPUT_INCLUDED
