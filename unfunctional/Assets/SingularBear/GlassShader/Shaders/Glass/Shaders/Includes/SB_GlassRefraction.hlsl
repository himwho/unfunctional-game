// SB_GlassRefraction.hlsl
#ifndef SB_GLASS_REFRACTION_INCLUDED
#define SB_GLASS_REFRACTION_INCLUDED


// REFRACTION TYPES

// 0 = Simple (normal-based offset)
// 1 = Physical (Snell's law with refract())
// 2 = Approximate (faster physical approximation)


// PHYSICAL REFRACTION (Snell's Law)


// Calculate refracted direction using HLSL refract()
// iorOrigin = IOR of the medium we're coming FROM (1.0 for air, 1.33 for water)
// iorTarget = IOR of the medium we're entering (1.5 for glass)
inline float3 SB_PhysicalRefract(float3 viewDirWS, float3 normalWS, float iorOrigin, float iorTarget)
{
    float3 incidentDir = -viewDirWS;
    float eta = iorOrigin / max(iorTarget, 1.001); // n1/n2 ratio
    return refract(incidentDir, normalWS, eta);
}

// Backward compatible version (assumes air origin)
inline float3 SB_PhysicalRefract(float3 viewDirWS, float3 normalWS, float ior)
{
    return SB_PhysicalRefract(viewDirWS, normalWS, 1.0, ior);
}

// Convert refracted direction to screen UV offset
inline float2 SB_RefractToScreenOffset(float3 refractedDir, float3 viewDirWS, float strength)
{
    // Transform to view space
    float3 refractedViewSpace = TransformWorldToViewDir(refractedDir, true);
    float3 viewViewSpace = TransformWorldToViewDir(-viewDirWS, true);
    
    // Return XY difference as screen offset
    return (refractedViewSpace.xy - viewViewSpace.xy) * strength;
}


// PER-CHANNEL IOR (Chromatic Dispersion)


// Cauchy's equation approximation for dispersion
// Different wavelengths have different IOR
inline float3 SB_GetDispersionIOR(float baseIOR, float dispersion)
{
    // Red (longer wavelength) = lower IOR
    // Blue (shorter wavelength) = higher IOR
    return float3(
        baseIOR - dispersion * 0.015,  // Red
        baseIOR,                        // Green  
        baseIOR + dispersion * 0.015   // Blue
    );
}


// BLUR SAMPLING


// Optimized blur patterns
static const float2 BlurOffsets8[8] = {
    float2(-1.0, -1.0), float2(1.0, -1.0),
    float2(-1.0,  1.0), float2(1.0,  1.0),
    float2(-1.414, 0.0), float2(1.414, 0.0),
    float2(0.0, -1.414), float2(0.0, 1.414)
};

static const float2 BlurOffsets16[16] = {
    float2(-1.5, -1.5), float2(-0.5, -1.5), float2(0.5, -1.5), float2(1.5, -1.5),
    float2(-1.5, -0.5), float2(-0.5, -0.5), float2(0.5, -0.5), float2(1.5, -0.5),
    float2(-1.5,  0.5), float2(-0.5,  0.5), float2(0.5,  0.5), float2(1.5,  0.5),
    float2(-1.5,  1.5), float2(-0.5,  1.5), float2(0.5,  1.5), float2(1.5,  1.5)
};

// 4-tap blur (mobile)
half3 SB_Blur4(float2 uv, float blurSize)
{
    half3 color = half3(0, 0, 0);
    color += SampleSceneColor(uv + float2(-blurSize, -blurSize));
    color += SampleSceneColor(uv + float2( blurSize, -blurSize));
    color += SampleSceneColor(uv + float2(-blurSize,  blurSize));
    color += SampleSceneColor(uv + float2( blurSize,  blurSize));
    return color * 0.25;
}

// 8-tap blur (mobile high)
half3 SB_Blur8(float2 uv, float blurSize)
{
    half3 color = half3(0, 0, 0);
    [unroll]
    for (int i = 0; i < 8; i++)
    {
        color += SampleSceneColor(uv + BlurOffsets8[i] * blurSize);
    }
    return color * 0.125;
}

// 16-tap blur (PC)
half3 SB_Blur16(float2 uv, float blurSize)
{
    half3 color = half3(0, 0, 0);
    [unroll]
    for (int i = 0; i < 16; i++)
    {
        color += SampleSceneColor(uv + BlurOffsets16[i] * blurSize * 0.5);
    }
    return color * 0.0625;
}

// Variable quality blur
half3 SB_SampleBlurred(float2 uv, half strength, half radius, int quality)
{
    if (strength < 0.001)
        return SampleSceneColor(uv).rgb;
    
    // Calculate blur size in UV space (percentage of screen)
    float blurSize = (radius * 0.015) * strength;
    blurSize = clamp(blurSize, 0.0, 0.15);
    
    if (quality <= 4)
        return SB_Blur4(uv, blurSize);
    else if (quality <= 8)
        return SB_Blur8(uv, blurSize);
    else
        return SB_Blur16(uv, blurSize);
}


// ABSORPTION (Beer-Lambert Law)


// Physically-based light absorption through colored glass
inline half3 SB_CalculateAbsorption(half3 absorptionColor, half thickness, half density)
{
    // Beer-Lambert: I = I0 * exp(-absorption * distance)
    return exp(-absorptionColor * thickness * density);
}


// INTERIOR FOG


inline half3 SB_ApplyInteriorFog(half3 sceneColor, half3 fogColor, half thickness, half density)
{
    half fogAmount = 1.0 - exp(-density * thickness * 3.0);
    return lerp(sceneColor, fogColor, saturate(fogAmount));
}


// MAIN REFRACTION FUNCTION


half3 SB_CalculateRefraction(
    float2 screenUV,
    float3 normalWS,
    float3 normalTS,
    float3 viewDirWS,
    half NdotV,
    half thickness,
    half depthFade,
    float time)
{
    float2 baseUV = screenUV;
    float2 refractionOffset = float2(0, 0);
    
    // BASE REFRACTION
    
#if defined(_SB_IOR)
    // Physical refraction using Snell's law
    // _IOROrigin = medium we're looking FROM (1.0=air, 1.33=water)
    // _IndexOfRefraction + 1.0 = IOR of target medium (glass)
    float targetIOR = _IndexOfRefraction + 1.0;
    float3 refractedDir = SB_PhysicalRefract(viewDirWS, normalWS, _IOROrigin, targetIOR);
    refractionOffset = SB_RefractToScreenOffset(refractedDir, viewDirWS, _IndexOfRefraction);
#else
    // Simple normal-based distortion
    refractionOffset = normalTS.xy * _Distortion;
#endif

    // Apply depth fade to reduce artifacts at intersections
    refractionOffset *= depthFade;
    
    // DISTORTION FX (if enabled)
#if defined(_SB_DISTORTION)
    float2 distortedUV = screenUV + refractionOffset;
    distortedUV = SB_ApplyAllDistortions(distortedUV, time);
    baseUV = distortedUV;
#else
    baseUV = screenUV + refractionOffset;
#endif

    baseUV = clamp(baseUV, 0.001, 0.999);
    
    // SAMPLE SCENE COLOR
    
    half3 sceneColor;
    
#if defined(_SB_CHROMATIC_ABERRATION)
    // Per-channel chromatic dispersion
    float3 iorRGB = SB_GetDispersionIOR(_IOR, _ChromaticDispersion);
    
    // Calculate offset for each channel
    float3 refractR = SB_PhysicalRefract(viewDirWS, normalWS, iorRGB.r);
    float3 refractG = SB_PhysicalRefract(viewDirWS, normalWS, iorRGB.g);
    float3 refractB = SB_PhysicalRefract(viewDirWS, normalWS, iorRGB.b);
    
    float2 offsetR = SB_RefractToScreenOffset(refractR, viewDirWS, _RefractionStrength) * depthFade;
    float2 offsetG = SB_RefractToScreenOffset(refractG, viewDirWS, _RefractionStrength) * depthFade;
    float2 offsetB = SB_RefractToScreenOffset(refractB, viewDirWS, _RefractionStrength) * depthFade;
    
    float2 uvR = clamp(screenUV + offsetR, 0.001, 0.999);
    float2 uvG = clamp(screenUV + offsetG, 0.001, 0.999);
    float2 uvB = clamp(screenUV + offsetB, 0.001, 0.999);
    
    #if defined(_SB_BLUR)
        int quality = (int)_BlurQuality;
        sceneColor.r = SB_SampleBlurred(uvR, _BlurStrength, _BlurRadius, quality).r;
        sceneColor.g = SB_SampleBlurred(uvG, _BlurStrength, _BlurRadius, quality).g;
        sceneColor.b = SB_SampleBlurred(uvB, _BlurStrength, _BlurRadius, quality).b;
    #else
        sceneColor.r = SampleSceneColor(uvR).r;
        sceneColor.g = SampleSceneColor(uvG).g;
        sceneColor.b = SampleSceneColor(uvB).b;
    #endif
    
#elif defined(_SB_BLUR)
    // Blur only
    int quality = (int)_BlurQuality;
    sceneColor = SB_SampleBlurred(baseUV, _BlurStrength, _BlurRadius, quality);
#else
    // Standard sampling
    sceneColor = SampleSceneColor(baseUV).rgb;
#endif

    // ABSORPTION
#if defined(_SB_ABSORPTION)
    half3 absorption = SB_CalculateAbsorption(_AbsorptionColor.rgb, thickness, _AbsorptionDensity);
    sceneColor *= absorption;
#endif

    // INTERIOR FOG
#if defined(_SB_INTERIOR_FOG)
    sceneColor = SB_ApplyInteriorFog(sceneColor, _InteriorFogColor.rgb, thickness, _InteriorFogDensity);
#endif

    return sceneColor;
}

#endif // SB_GLASS_REFRACTION_INCLUDED
