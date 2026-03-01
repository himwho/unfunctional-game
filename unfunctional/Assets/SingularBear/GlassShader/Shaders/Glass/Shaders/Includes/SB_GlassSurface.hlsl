// SB_GlassSurface.hlsl
#ifndef SB_GLASS_SURFACE_INCLUDED
#define SB_GLASS_SURFACE_INCLUDED

#include "SB_GlassCore.hlsl"
#include "SB_GlassInput.hlsl"
#include "SB_GlassTriplanar.hlsl"
#include "SB_GlassDistortion.hlsl"
#include "SB_GlassBlur.hlsl"
#include "SB_GlassFingerprints.hlsl"
#include "SB_GlassDecals.hlsl"
#include "SB_GlassAdvanced.hlsl"


// REFRACTION - PHYSICAL MODEL (MirzaBeig style)


// Physical refraction using HLSL refract() function
// Based on Snell's law: n1 * sin(theta1) = n2 * sin(theta2)
inline float2 CalculatePhysicalRefraction(float3 viewDirWS, float3 normalWS, float ior)
{
    // refract() expects incident direction (from camera to surface)
    float3 incidentDir = -viewDirWS;
    
    // Calculate refracted direction using Snell's law
    // eta = n1/n2 where n1=1.0 (air) and n2=IOR (glass)
    float eta = 1.0 / ior;
    float3 refractedDir = refract(incidentDir, normalWS, eta);
    
    // Transform to view space for screen-space offset
    float3 refractedViewSpace = TransformWorldToViewDir(refractedDir, true);
    
    // Return XY offset (screen space)
    return refractedViewSpace.xy;
}

// Per-channel IOR for realistic chromatic dispersion
// Red refracts less, Blue refracts more (like a real prism)
inline float3 GetPerChannelIOR(float baseIOR, float dispersion)
{
    // Cauchy's equation approximation
    // Red (longer wavelength) = lower IOR
    // Blue (shorter wavelength) = higher IOR
    return float3(
        baseIOR - dispersion * 0.02,  // Red
        baseIOR,                       // Green
        baseIOR + dispersion * 0.02   // Blue
    );
}

// Sample with per-channel refraction (chromatic dispersion)
inline half3 SampleWithDispersion(float2 baseUV, float3 viewDirWS, float3 normalWS, float3 iorRGB, float strength)
{
    half3 color = half3(0, 0, 0);
    
    // Calculate refraction offset for each channel
    float2 offsetR = CalculatePhysicalRefraction(viewDirWS, normalWS, iorRGB.r) * strength;
    float2 offsetG = CalculatePhysicalRefraction(viewDirWS, normalWS, iorRGB.g) * strength;
    float2 offsetB = CalculatePhysicalRefraction(viewDirWS, normalWS, iorRGB.b) * strength;
    
    // Sample each channel at different UV
    color.r = SampleSceneColor(clamp(baseUV + offsetR, 0.001, 0.999)).r;
    color.g = SampleSceneColor(clamp(baseUV + offsetG, 0.001, 0.999)).g;
    color.b = SampleSceneColor(clamp(baseUV + offsetB, 0.001, 0.999)).b;
    
    return color;
}


// BLUR - FIXED (Actually visible!)


// Blur with proper scaling that actually shows
inline half3 SampleWithBlur(float2 uv, half strength, half radius, int quality)
{
    // Early out - no blur needed
    if (strength < 0.001)
        return SampleSceneColor(uv).rgb;
    
    // Blur size in screen UV space
    // strength 0-1, radius 0-0.05 -> visible blur
    float blurSize = radius * strength * 2.0;
    
    // Clamp UV to avoid sampling outside screen
    half3 color = half3(0, 0, 0);
    
    if (quality <= 4)
    {
        // 4-tap box
        color = SampleSceneColor(clamp(uv + float2(-blurSize, -blurSize), 0.001, 0.999)).rgb;
        color += SampleSceneColor(clamp(uv + float2( blurSize, -blurSize), 0.001, 0.999)).rgb;
        color += SampleSceneColor(clamp(uv + float2(-blurSize,  blurSize), 0.001, 0.999)).rgb;
        color += SampleSceneColor(clamp(uv + float2( blurSize,  blurSize), 0.001, 0.999)).rgb;
        color *= 0.25;
    }
    else if (quality <= 8)
    {
        // 8-tap cross + diagonal
        color = SampleSceneColor(clamp(uv + float2(-blurSize, 0), 0.001, 0.999)).rgb;
        color += SampleSceneColor(clamp(uv + float2( blurSize, 0), 0.001, 0.999)).rgb;
        color += SampleSceneColor(clamp(uv + float2(0, -blurSize), 0.001, 0.999)).rgb;
        color += SampleSceneColor(clamp(uv + float2(0,  blurSize), 0.001, 0.999)).rgb;
        color += SampleSceneColor(clamp(uv + float2(-blurSize * 0.707, -blurSize * 0.707), 0.001, 0.999)).rgb;
        color += SampleSceneColor(clamp(uv + float2( blurSize * 0.707, -blurSize * 0.707), 0.001, 0.999)).rgb;
        color += SampleSceneColor(clamp(uv + float2(-blurSize * 0.707,  blurSize * 0.707), 0.001, 0.999)).rgb;
        color += SampleSceneColor(clamp(uv + float2( blurSize * 0.707,  blurSize * 0.707), 0.001, 0.999)).rgb;
        color *= 0.125;
    }
    else if (quality <= 16)
    {
        // 16-tap grid
        float weightSum = 0.0;
        [unroll]
        for (int x = -2; x <= 1; x++)
        {
            [unroll]
            for (int y = -2; y <= 1; y++)
            {
                float2 offset = float2(x + 0.5, y + 0.5) * blurSize * 0.5;
                float weight = 1.0 - length(offset) / (blurSize * 1.5);
                weight = max(weight, 0.1);
                color += SampleSceneColor(clamp(uv + offset, 0.001, 0.999)).rgb * weight;
                weightSum += weight;
            }
        }
        color /= weightSum;
    }
    else
    {
        // 25-tap high quality gaussian-ish
        float weightSum = 0.0;
        [unroll]
        for (int x = -2; x <= 2; x++)
        {
            [unroll]
            for (int y = -2; y <= 2; y++)
            {
                float2 offset = float2(x, y) * blurSize * 0.4;
                float dist = length(float2(x, y)) / 2.83; // normalize to 0-1
                float weight = exp(-dist * dist * 2.0); // gaussian falloff
                color += SampleSceneColor(clamp(uv + offset, 0.001, 0.999)).rgb * weight;
                weightSum += weight;
            }
        }
        color /= weightSum;
    }
    
    return color;
}

// Blur with chromatic dispersion combined
inline half3 SampleWithBlurAndDispersion(float2 baseUV, float3 viewDirWS, float3 normalWS, 
    float3 iorRGB, float refractionStrength, half blurStrength, half blurRadius, int blurQuality)
{
    half3 color = half3(0, 0, 0);
    
    // Calculate per-channel UV offsets
    float2 offsetR = CalculatePhysicalRefraction(viewDirWS, normalWS, iorRGB.r) * refractionStrength;
    float2 offsetG = CalculatePhysicalRefraction(viewDirWS, normalWS, iorRGB.g) * refractionStrength;
    float2 offsetB = CalculatePhysicalRefraction(viewDirWS, normalWS, iorRGB.b) * refractionStrength;
    
    float2 uvR = clamp(baseUV + offsetR, 0.001, 0.999);
    float2 uvG = clamp(baseUV + offsetG, 0.001, 0.999);
    float2 uvB = clamp(baseUV + offsetB, 0.001, 0.999);
    
    // Sample with blur per channel
    color.r = SampleWithBlur(uvR, blurStrength, blurRadius, blurQuality).r;
    color.g = SampleWithBlur(uvG, blurStrength, blurRadius, blurQuality).g;
    color.b = SampleWithBlur(uvB, blurStrength, blurRadius, blurQuality).b;
    
    return color;
}


// MAIN REFRACTION FUNCTION


// Full refraction calculation with all effects
inline half3 CalculateRefraction(half3 normalTS, half3 normalWS, half3 viewDirWS, float4 screenPos, float time)
{
    float2 screenUV = ComputeNDC(screenPos);
    
    // Flip Y if needed (for platform compatibility)
    if (_FlipRefraction > 0.5)
    {
        screenUV.y = 1.0 - screenUV.y;
    }
    
    // Base distortion from normal map
    float2 normalOffset = normalTS.xy * _Distortion;
    float2 baseUV = screenUV + normalOffset;
    
    // DISTORTION FX (applied to UV before refraction)
#if defined(_SB_DISTORTION)
    
    #if defined(_SB_MAGNIFY)
    if (abs(_MagnifyStrength) > 0.001)
        baseUV = SB_Magnify(baseUV, _MagnifyStrength, _MagnifyRadius, _MagnifyCenter.xy, _MagnifyFalloff);
    #endif
    
    #if defined(_SB_BARREL)
    if (abs(_BarrelStrength) > 0.001)
        baseUV = SB_Barrel(baseUV, _BarrelStrength);
    #endif
    
    #if defined(_SB_WAVES)
    if (_WaveAmplitude > 0.001)
        baseUV = SB_Waves(baseUV, _WaveAmplitude, _WaveFrequency, _WaveSpeed, time);
    #endif
    
    #if defined(_SB_RIPPLE)
    if (_RippleAmplitude > 0.001)
        baseUV = SB_Ripple(baseUV, _RippleCenter.xy, _RippleAmplitude, _RippleFrequency, _RippleSpeed, time, _RippleDecay);
    #endif
    
    #if defined(_SB_SWIRL)
    if (abs(_SwirlStrength) > 0.001)
        baseUV = SB_Swirl(baseUV, _SwirlStrength, _SwirlRadius, float2(0.5, 0.5));
    #endif
    
    #if defined(_SB_HEAT_HAZE)
    if (_HeatHazeStrength > 0.001)
        baseUV = SB_HeatHaze(baseUV, _HeatHazeStrength, _HeatHazeScale, _HeatHazeSpeed, time);
    #endif
    
    #if defined(_SB_PIXELATE)
    if (_PixelateSize > 1.0)
        baseUV = SB_Pixelate(baseUV, _PixelateSize);
    #endif
    
#endif // _SB_DISTORTION

    baseUV = clamp(baseUV, 0.001, 0.999);
    
    // PHYSICAL REFRACTION + CHROMATIC + BLUR
    
    half3 color;
    
#if defined(_SB_CHROMATIC_ABERRATION)
    // Per-channel IOR for chromatic dispersion
    // IOR = 1.0 + _IndexOfRefraction (maps 0-1 slider to 1.0-2.0 IOR)
    float targetIOR = _IndexOfRefraction + 1.0;
    float3 iorRGB = GetPerChannelIOR(targetIOR, _ChromaticAberration);
    float refractionStrength = _IndexOfRefraction;
    
    // Calculate eta (n1/n2) for each channel using origin IOR
    float3 etaRGB = _IOROrigin / iorRGB;
    
    #if defined(_SB_BLUR)
        // Blur + Chromatic dispersion
        color = SampleWithBlurAndDispersion(baseUV, viewDirWS, normalWS, 
            iorRGB, refractionStrength, _BlurStrength, _BlurRadius, (int)_BlurQuality);
    #else
        // Chromatic dispersion only
        color = SampleWithDispersion(baseUV, viewDirWS, normalWS, iorRGB, refractionStrength);
    #endif
    
#elif defined(_SB_IOR)
    // Physical IOR without chromatic
    // Use origin IOR for correct eta calculation (n1/n2)
    float targetIOR = _IndexOfRefraction + 1.0;
    float eta = _IOROrigin / targetIOR;
    float3 refractDir = refract(-viewDirWS, normalWS, eta);
    float2 physicalOffset = (refractDir.xy - (-viewDirWS).xy) * _IndexOfRefraction;
    float2 finalUV = clamp(baseUV + physicalOffset, 0.001, 0.999);
    
    #if defined(_SB_BLUR)
        color = SampleWithBlur(finalUV, _BlurStrength, _BlurRadius, (int)_BlurQuality);
    #else
        color = SampleSceneColor(finalUV).rgb;
    #endif
    
#else
    // Simple distortion only
    #if defined(_SB_BLUR)
        color = SampleWithBlur(baseUV, _BlurStrength, _BlurRadius, (int)_BlurQuality);
    #else
        color = SampleSceneColor(baseUV).rgb;
    #endif
    
#endif

    return color;
}

// Overload without time parameter
inline half3 CalculateRefraction(half3 normalTS, half3 normalWS, half3 viewDirWS, float4 screenPos)
{
    return CalculateRefraction(normalTS, normalWS, viewDirWS, screenPos, _Time.y);
}


// REFLECTION


// Calculate reflection using environment probes or custom cubemap
inline half3 CalculateReflection(half3 normalWS, half3 viewDirWS, half smoothness, half3 positionWS)
{
    half3 reflectDir = reflect(-viewDirWS, normalWS);
    
    // Fix inverted reflection (flip Y if needed)
    if (_FlipReflection > 0.5)
    {
        reflectDir.y = -reflectDir.y;
    }
    
    half3 reflection = half3(0.0, 0.0, 0.0);
    
    // Calculate mip level from smoothness and blur
    half perceptualRoughness = (1.0 - smoothness) + _ReflectionBlur;
    perceptualRoughness = saturate(perceptualRoughness);
    half mip = perceptualRoughness * 6.0;
    
#if defined(_SB_REFLECTION_CUBEMAP)
    // Custom cubemap reflection
    reflection = SampleReflectionCube(reflectDir, perceptualRoughness);
    reflection *= _ReflectionColor.rgb;
#else
    // Sample reflection probe directly
    // unity_SpecCube0 is the active reflection probe or skybox fallback
    half4 encodedIrradiance = SAMPLE_TEXTURECUBE_LOD(unity_SpecCube0, samplerunity_SpecCube0, reflectDir, mip);
    
    // Decode HDR
    reflection = DecodeHDREnvironment(encodedIrradiance, unity_SpecCube0_HDR);
    
    // Apply reflection color tint
    reflection *= _ReflectionColor.rgb;
#endif
    
    return reflection * _ReflectionIntensity;
}


// FRESNEL


#if defined(_SB_FRESNEL)
// Improved Fresnel calculation with more controls
inline half CalculateFresnel(half NdotV)
{
    half fresnel = pow(max(1.0 - NdotV, 0.0), _FresnelPower);
    
    // Clamp to min/max range
    fresnel = smoothstep(_FresnelMin, _FresnelMax, fresnel);
    
    // Invert if needed
    if (_FresnelInvert > 0.5)
        fresnel = 1.0 - fresnel;
    
    return fresnel * _FresnelIntensity;
}

// Get Fresnel color contribution
inline half3 GetFresnelColor(half fresnel)
{
    return _FresnelColor.rgb * fresnel;
}

// Fresnel with custom F0 (for glass typically 0.04)
inline half CalculateFresnelF0(half NdotV, half F0)
{
    half fresnel = F0 + (1.0 - F0) * pow(max(1.0 - NdotV, 0.0), 5.0);
    
    // Clamp to min/max range
    fresnel = smoothstep(_FresnelMin, _FresnelMax, fresnel);
    
    // Invert if needed
    if (_FresnelInvert > 0.5)
        fresnel = 1.0 - fresnel;
    
    return fresnel * _FresnelIntensity;
}
#else
// Fresnel disabled - return neutral values
inline half CalculateFresnel(half NdotV)
{
    return 0.0;
}

inline half3 GetFresnelColor(half fresnel)
{
    return half3(0, 0, 0);
}

inline half CalculateFresnelF0(half NdotV, half F0)
{
    return 0.0;
}
#endif


// RIM LIGHTING


#if defined(_SB_RIM)
inline half3 CalculateRim(half NdotV, half3 lightColor)
{
    half rim = 1.0 - NdotV;
    rim = smoothstep(_RimMin, _RimMax, rim);
    rim = pow(max(rim, 0.0), _RimPower);
    
    return _RimColor.rgb * rim * _RimIntensity * lightColor;
}
#endif


// TRANSLUCENCY


#if defined(_SB_TRANSLUCENT)
inline half3 CalculateTranslucency(half3 normalWS, half3 viewDirWS, half3 lightDir, half3 lightColor, half atten)
{
    // Subsurface scattering approximation
    // Light passes through the object and scatters
    half3 transLightDir = lightDir + normalWS * _TranslucentDistortion;
    transLightDir = normalize(transLightDir);
    
    // View-dependent term (light coming through toward viewer)
    half VdotL = saturate(dot(viewDirWS, -transLightDir));
    half transDot = pow(VdotL, _TranslucentPower) * _TranslucentScale;
    
    // Also add a direct transmission term for more visibility
    half directTrans = pow(saturate(dot(-normalWS, lightDir)), _TranslucentPower * 0.5);
    transDot = max(transDot, directTrans * 0.5);
    
    half3 translucency = lightColor * transDot * atten * _TranslucentIntensity;
    return translucency * _TranslucentColor.rgb;
}
#endif


// IRIDESCENCE (Thin-film interference)


#if defined(_SB_IRIDESCENCE)
// Convert hue to RGB (for rainbow effect)
inline half3 HueToRGB(half hue)
{
    half r = abs(hue * 6.0 - 3.0) - 1.0;
    half g = 2.0 - abs(hue * 6.0 - 2.0);
    half b = 2.0 - abs(hue * 6.0 - 4.0);
    return saturate(half3(r, g, b));
}

// Thin-film iridescence based on view angle
inline half3 CalculateIridescence(half NdotV, half3 normalWS, half time)
{
    // Base hue from view angle (thin-film interference)
    half viewAngle = saturate(1.0 - NdotV);
    
    // Multi-layer interference pattern
    half phase = viewAngle * _IridescenceScale + _IridescenceShift;
    
    // Add time animation if speed > 0
    phase += time * _IridescenceSpeed;
    
    // Create rainbow color from phase
    half hue = frac(phase);
    half3 iriColor = HueToRGB(hue);
    
    // Add secondary interference layer for more complexity
    half hue2 = frac(phase * 1.5 + 0.33);
    half3 iriColor2 = HueToRGB(hue2);
    
    // Blend layers
    half3 finalIri = lerp(iriColor, iriColor2, viewAngle * 0.5);
    
    // Apply tint
    finalIri *= _IridescenceColor.rgb;
    
    // Intensity based on view angle (more visible at grazing angles)
    half intensity = pow(viewAngle, 1.5) * _IridescenceStrength;
    
    return finalIri * intensity;
}
#endif


// SURFACE NOISE (Procedural micro imperfections)


#if defined(_SB_SURFACE_NOISE)
inline half3 CalculateSurfaceNoise(float2 uv, float3 positionWS, half time)
{
    // Use world position for seamless tiling
    float2 noiseUV = positionWS.xz * 0.1 + uv;
    
    // Get procedural normal from noise
    half3 noiseNormal = SB_NoiseNormal(noiseUV, _SurfaceNoiseScale, _SurfaceNoiseStrength, time * _SurfaceNoiseSpeed);
    
    return noiseNormal;
}
#endif


// TINT TEXTURE (Vitrail / Stained Glass)


#if defined(_SB_TINT_TEXTURE)
inline half4 SampleTintTexture(float2 uv, float2 distortion)
{
    // Apply distortion to tint texture UV if enabled
    float2 tintUV = uv * _TintTexture_ST.xy + _TintTexture_ST.zw;
    tintUV += distortion * _TintDistortionAmount;
    
    half4 tintSample = SAMPLE_TEXTURE2D(_TintTexture, sampler_TintTexture, tintUV);
    tintSample.rgb *= _TintTextureColor.rgb;
    
    return tintSample;
}
#endif


// EDGE DARKENING (Thick glass edges)


#if defined(_SB_EDGE_DARKENING)
inline half CalculateEdgeDarkening(half NdotV)
{
    // More darkening at grazing angles (edges)
    half edge = saturate(1.0 - NdotV);
    edge = pow(edge, _EdgeDarkeningPower);
    return 1.0 - (edge * _EdgeDarkeningStrength);
}
#endif


// INNER GLOW


#if defined(_SB_INNER_GLOW)
inline half3 CalculateInnerGlow(half NdotV, half thickness)
{
    // Glow stronger at center, fades at edges
    half glow = pow(max(NdotV, 0.0), _InnerGlowPower);
    glow *= thickness; // Thicker = more glow
    glow = saturate(glow * _InnerGlowStrength);
    
    // Falloff
    glow *= (1.0 - pow(saturate(1.0 - NdotV), _InnerGlowFalloff));
    
    return _InnerGlowColor.rgb * glow;
}
#endif


// THICKNESS EFFECTS


#if defined(_SB_THICKNESS_MAP)
inline half SampleThickness(float2 uv)
{
    half thickness = SAMPLE_TEXTURE2D(_ThicknessMap, sampler_ThicknessMap, uv).r;
    // Remap from texture value to min/max range
    return lerp(_ThicknessMin, _ThicknessMax, thickness);
}

inline half3 ThicknessToColor(half thickness)
{
    // Darken color based on thickness (thin = light, thick = dark)
    half3 baseColor = _Color.rgb;
    return lerp(baseColor * 1.2, baseColor * 0.5, thickness);
}
#endif


// DEPTH FADE (Soft intersection)


#if defined(_SB_DEPTH_FADE)
inline half CalculateDepthFadeAlpha(float4 screenPos, float3 positionWS)
{
    return SB_DepthFade(screenPos, positionWS, _DepthFadeDistance);
}
#endif



// SPECULAR


#if defined(_SB_SPECULAR)
// Advanced Specular with realistic and stylized modes
inline half3 CalculateSpecular(half3 normalWS, half3 viewDirWS, half3 lightDir, half3 lightColor, half atten)
{
    half3 halfDir = SafeNormalize(lightDir + viewDirWS);
    half NdotH = saturate(dot(normalWS, halfDir));
    half NdotV = saturate(dot(normalWS, viewDirWS));
    
    // === ANISOTROPY (Stretched highlights) ===
    half anisotropicNdotH = NdotH;
    if (abs(_SpecularAnisotropy) > 0.01)
    {
        half3 tangent = normalize(cross(normalWS, half3(0, 1, 0)));
        half3 bitangent = cross(normalWS, tangent);
        
        half TdotH = dot(tangent, halfDir);
        half BdotH = dot(bitangent, halfDir);
        
        half anisoStretch = 1.0 + abs(_SpecularAnisotropy) * 4.0;
        if (_SpecularAnisotropy > 0)
            anisotropicNdotH = saturate(sqrt(1.0 - TdotH * TdotH / anisoStretch));
        else
            anisotropicNdotH = saturate(sqrt(1.0 - BdotH * BdotH / anisoStretch));
        
        anisotropicNdotH = lerp(NdotH, anisotropicNdotH, abs(_SpecularAnisotropy));
    }
    
    // === BASE SPECULAR (always smooth gradient first) ===
    half sizeExponent = lerp(4.0, 512.0, _SpecularSize * _SpecularSize);
    half spec = pow(anisotropicNdotH, sizeExponent * max(0.1, _SpecularSmoothness));
    
    // === TOON MODE (Stepped bands) - Apply BEFORE hardness ===
    if (_SpecularToon > 0.5)
    {
        half steps = max(1.0, floor(_SpecularSteps));
        
        if (steps <= 1.5)
        {
            // Single band - threshold only
            spec = step(_SpecularThreshold, spec);
        }
        else
        {
            // Multiple bands with threshold
            half bandedSpec = floor(spec * steps + 0.5) / steps;
            spec = step(_SpecularThreshold, spec) * bandedSpec;
        }
    }
    
    // === EDGE HARDNESS (Apply AFTER toon for clean band edges) ===
    if (_SpecularHardness > 0.01 && _SpecularToon < 0.5)
    {
        // Only apply hardness if NOT in toon mode (toon already has hard edges)
        half hardnessLow = lerp(0.0, 0.45, _SpecularHardness);
        half hardnessHigh = lerp(1.0, 0.55, _SpecularHardness);
        spec = smoothstep(hardnessLow, hardnessHigh, spec);
    }
    else if (_SpecularHardness > 0.01 && _SpecularToon > 0.5)
    {
        // In toon mode with hardness: sharpen the band edges
        spec = smoothstep(0.01, 0.02, spec) * spec;
    }
    
    // === FRESNEL SPECULAR (Edge boost) ===
    half fresnelSpec = 1.0;
    if (_SpecularFresnel > 0.5)
    {
        fresnelSpec = lerp(0.04, 1.0, pow(1.0 - NdotV, 5.0));
    }
    
    // === FINAL OUTPUT ===
    half3 specColor = _SpecularColor.rgb * spec * fresnelSpec * _SpecularIntensity;
    return specColor * lightColor * atten;
}

// Overload with tangent for better anisotropy
inline half3 CalculateSpecularAniso(half3 normalWS, half3 viewDirWS, half3 lightDir, half3 lightColor, half atten, half3 tangentWS)
{
    half3 halfDir = SafeNormalize(lightDir + viewDirWS);
    half NdotH = saturate(dot(normalWS, halfDir));
    half NdotV = saturate(dot(normalWS, viewDirWS));
    
    // Anisotropy with actual tangent
    half anisotropicNdotH = NdotH;
    if (abs(_SpecularAnisotropy) > 0.01)
    {
        half3 bitangent = cross(normalWS, tangentWS);
        half TdotH = dot(tangentWS, halfDir);
        half BdotH = dot(bitangent, halfDir);
        
        half anisoStretch = 1.0 + abs(_SpecularAnisotropy) * 4.0;
        if (_SpecularAnisotropy > 0)
            anisotropicNdotH = saturate(sqrt(1.0 - TdotH * TdotH / anisoStretch));
        else
            anisotropicNdotH = saturate(sqrt(1.0 - BdotH * BdotH / anisoStretch));
        
        anisotropicNdotH = lerp(NdotH, anisotropicNdotH, abs(_SpecularAnisotropy));
    }
    
    // Base specular
    half sizeExponent = lerp(4.0, 512.0, _SpecularSize * _SpecularSize);
    half spec = pow(anisotropicNdotH, sizeExponent * max(0.1, _SpecularSmoothness));
    
    // Toon mode
    if (_SpecularToon > 0.5)
    {
        half steps = max(1.0, floor(_SpecularSteps));
        if (steps <= 1.5)
            spec = step(_SpecularThreshold, spec);
        else
            spec = step(_SpecularThreshold, spec) * (floor(spec * steps + 0.5) / steps);
    }
    
    // Hardness
    if (_SpecularHardness > 0.01 && _SpecularToon < 0.5)
    {
        half hardnessLow = lerp(0.0, 0.45, _SpecularHardness);
        half hardnessHigh = lerp(1.0, 0.55, _SpecularHardness);
        spec = smoothstep(hardnessLow, hardnessHigh, spec);
    }
    
    // Fresnel
    half fresnelSpec = 1.0;
    if (_SpecularFresnel > 0.5)
        fresnelSpec = lerp(0.04, 1.0, pow(1.0 - NdotV, 5.0));
    
    return _SpecularColor.rgb * spec * fresnelSpec * _SpecularIntensity * lightColor * atten;
}
#else
// Specular disabled - return black
inline half3 CalculateSpecular(half3 normalWS, half3 viewDirWS, half3 lightDir, half3 lightColor, half atten)
{
    return half3(0, 0, 0);
}

inline half3 CalculateSpecularAniso(half3 normalWS, half3 viewDirWS, half3 lightDir, half3 lightColor, half atten, half3 tangentWS)
{
    return half3(0, 0, 0);
}
#endif


// INITIALIZE SURFACE


SB_GlassSurface InitializeSurface(SB_Varyings input)
{
    SB_GlassSurface surface = (SB_GlassSurface)0;
    
    float2 mainUV = input.uv.xy;
    
    // TRIPLANAR SETUP
#if defined(_SB_TRIPLANAR)
    // Calculate triplanar data
    float3 posWS = input.positionWS;
    float3 normWS = normalize(input.normalWS.xyz);
    float2 triOffset = float2(0, 0); // No offset property in shader
    
    SB_TriplanarData triData = SB_CalculateTriplanarOffset(
        posWS, 
        normWS, 
        _TriplanarScale, 
        triOffset,
        _TriplanarSharpness
    );
#endif

    // SAMPLE ALBEDO
#if defined(_SB_TRIPLANAR)
    half4 albedo = SB_SampleTriplanar(TEXTURE2D_ARGS(_MainTex, sampler_MainTex), triData) * _Color;
#else
    half4 albedo = SampleAlbedo(mainUV);
#endif
    
    // Apply Saturation
    half luminance = dot(albedo.rgb, half3(0.2126, 0.7152, 0.0722));
    albedo.rgb = lerp(half3(luminance, luminance, luminance), albedo.rgb, _Saturation);
    
    // Apply Brightness
    albedo.rgb *= _Brightness;
    
    surface.albedo = albedo.rgb;
    surface.alpha = albedo.a * _Opacity;
    
    // Get view direction and tangent
#if defined(_SB_NORMALMAP) || defined(_SB_DETAIL_NORMAL) || defined(_SB_RAIN)
    surface.viewDirWS = half3(input.normalWS.w, input.tangentWS.w, input.bitangentWS.w);
    surface.tangentWS = SafeNormalize(input.tangentWS.xyz);
#else
    surface.viewDirWS = input.viewDirWS;
    surface.tangentWS = half3(1, 0, 0); // Default tangent
#endif
    surface.viewDirWS = SafeNormalize(surface.viewDirWS);
    
    // CALCULATE NORMAL
    half3 normalTS = half3(0.0, 0.0, 1.0);
    
#if defined(_SB_NORMALMAP)
    #if defined(_SB_TRIPLANAR)
        // Triplanar normal sampling
        float3 baseNormalWS = normalize(input.normalWS.xyz);
        surface.normalWS = SB_SampleTriplanarNormal(
            TEXTURE2D_ARGS(_BumpMap, sampler_BumpMap),
            triData,
            baseNormalWS,
            _BumpScale
        );
        // Skip TBN transform since triplanar already outputs world-space normals
        #define _SB_SKIP_TBN
    #else
        float2 bumpUV = mainUV * _BumpMap_ST.xy + _BumpMap_ST.zw;
        normalTS = SampleNormal(bumpUV, _BumpScale);
    #endif
#endif

#if defined(_SB_DETAIL_NORMAL)
    // Detail Normal UV with separate tiling + texture ST
    float2 detailNormalUV = input.uv.xy * _DetailNormalTiling;
    detailNormalUV = detailNormalUV * _DetailNormalMap_ST.xy + _DetailNormalMap_ST.zw;
    
    #if defined(_SB_DETAIL_NORMAL_TRIPLANAR)
        // Independent triplanar for Detail Normal only
        SB_TriplanarData detailNormalTriData = SB_CalculateTriplanar(
            input.positionWS, 
            normalize(input.normalWS.xyz), 
            _DetailNormalTriplanarScale,
            _DetailNormalTriplanarSharpness
        );
        half3 detailNormalWS = SB_SampleTriplanarNormal(
            TEXTURE2D_ARGS(_DetailNormalMap, sampler_DetailNormalMap),
            detailNormalTriData,
            surface.normalWS,
            _DetailNormalScale
        );
        // Blend detail with main normal in world space
        surface.normalWS = normalize(surface.normalWS + detailNormalWS * 0.5);
        #define _SB_DETAIL_NORMAL_APPLIED_WS
    #else
        half3 detailNormal = SampleDetailNormal(detailNormalUV, _DetailNormalScale);
        
        #if defined(_SB_QUALITY_HIGH)
            normalTS = BlendNormalsRNM(normalTS, detailNormal);
        #else
            normalTS = BlendNormalsSimple(normalTS, detailNormal);
        #endif
    #endif
#endif

    // RAIN EFFECT - Apply in tangent space BEFORE TBN transform
#if defined(_SB_RAIN)
    if (_RainIntensity > 0.001)
    {
        half3 rainNormalTS = half3(0, 0, 1);
        half normalScale = _RainIntensity * _RainNormalStrength * 2.0;
        
    #if defined(_SB_RAIN_TRIPLANAR)
        // =====================================================
        // TRIPLANAR RAIN (Object Space) - Rain flows top to bottom
        // =====================================================
        
        // Get position in Object Space (fixed to object, moves with it)
        float3 positionOS = mul(unity_WorldToObject, float4(input.positionWS, 1.0)).xyz;
        
        // Get normal in Object Space for triplanar weights
        float3 normalOS = mul((float3x3)unity_WorldToObject, input.normalWS.xyz);
        normalOS = normalize(normalOS);
        
        // Calculate triplanar blend weights based on Object Space normal
        // Use squared weights for smoother blending and less artifacts
        float3 blend = normalOS * normalOS; // Squared for smoother falloff
        
        // Apply sharpness
        blend = pow(blend, _RainTriplanarSharpness * 0.5);
        
        // Normalize weights
        float blendSum = blend.x + blend.y + blend.z;
        blend /= max(blendSum, 0.0001);
        
        // Scale position for tiling
        float3 scaledPos = positionOS * _RainTriplanarScale;
        
        // Animation - rain flows DOWN (negative Y direction in object space)
        float rainAnim = _Time.y * _RainSpeed.y * 2.0;
        
        // === X-axis projection (YZ plane - left/right faces) ===
        // Rain flows down Y axis
        float2 uvX = float2(scaledPos.z, scaledPos.y) * _RainTiling.xy;
        uvX.y -= rainAnim;
        uvX += _RainOffset.xy;
        
        // === Y-axis projection (XZ plane - top/bottom faces) ===
        // Top face: minimal rain streaks, just wetness ripples
        float2 uvY = float2(scaledPos.x, scaledPos.z) * _RainTiling.xy;
        uvY += _RainOffset.xy;
        
        // === Z-axis projection (XY plane - front/back faces) ===
        // Rain flows down Y axis
        float2 uvZ = float2(scaledPos.x, scaledPos.y) * _RainTiling.xy;
        uvZ.y -= rainAnim;
        uvZ += _RainOffset.xy;
        
        // Sample rain normal map on each plane (apply texture ST)
        half4 rainTexX = SAMPLE_TEXTURE2D(_RainTexture, sampler_RainTexture, uvX * _RainTexture_ST.xy + _RainTexture_ST.zw);
        half4 rainTexY = SAMPLE_TEXTURE2D(_RainTexture, sampler_RainTexture, uvY * _RainTexture_ST.xy + _RainTexture_ST.zw);
        half4 rainTexZ = SAMPLE_TEXTURE2D(_RainTexture, sampler_RainTexture, uvZ * _RainTexture_ST.xy + _RainTexture_ST.zw);
        
        // Unpack normals - Y face (horizontal) gets much less intensity
        half3 rainNormalX = UnpackNormalScale(rainTexX, normalScale);
        half3 rainNormalY = UnpackNormalScale(rainTexY, normalScale * 0.1); // Very subtle on top
        half3 rainNormalZ = UnpackNormalScale(rainTexZ, normalScale);
        
        // Simple blend in tangent space - avoid complex swizzling that causes artifacts
        // Just blend the XY perturbations directly
        half2 perturbX = rainNormalX.xy * blend.x;
        half2 perturbY = rainNormalY.xy * blend.y;
        half2 perturbZ = rainNormalZ.xy * blend.z;
        
        // Combine perturbations
        half2 finalPerturb = perturbX + perturbY + perturbZ;
        
        // Reconstruct normal in tangent space
        rainNormalTS = half3(finalPerturb.x, finalPerturb.y, 1.0);
        rainNormalTS = normalize(rainNormalTS);
        
    #else
        // =====================================================
        // UV-BASED RAIN (original implementation)
        // =====================================================
        
        // Calculate rain UV with tiling, offset, rotation, and animation
        float2 rainUV = input.uv.xy * _RainTexture_ST.xy + _RainTexture_ST.zw;
        
        // Apply tiling and offset
        rainUV = rainUV * _RainTiling.xy + _RainOffset.xy;
        
        // Apply rotation around UV center (0.5, 0.5)
        if (abs(_RainRotation) > 0.01)
        {
            float angle = _RainRotation * 0.0174533; // degrees to radians
            float cosA = cos(angle);
            float sinA = sin(angle);
            float2 centered = rainUV - 0.5;
            rainUV.x = centered.x * cosA - centered.y * sinA + 0.5;
            rainUV.y = centered.x * sinA + centered.y * cosA + 0.5;
        }
        
        // Apply animated speed
        rainUV += _Time.y * _RainSpeed.xy;
        
        // Sample rain normal map
        half4 rainTex = SAMPLE_TEXTURE2D(_RainTexture, sampler_RainTexture, rainUV);
        
        // Unpack normal with intensity and strength
        rainNormalTS = UnpackNormalScale(rainTex, normalScale);
    #endif
        
        // Apply distortion boost to XY (affects refraction)
        rainNormalTS.xy *= (1.0 + _RainDistortion * 2.0);
        
        // Blend normals in tangent space (RNM blend)
        half3 t = normalTS + half3(0, 0, 1);
        half3 u = rainNormalTS * half3(-1, -1, 1);
        normalTS = normalize(t * dot(t, u) - u * t.z);
    }
#endif

    // Transform normal to world space (if not using triplanar)
#if !defined(_SB_SKIP_TBN)
    #if defined(_SB_NORMALMAP) || defined(_SB_DETAIL_NORMAL) || defined(_SB_RAIN)
        half3x3 TBN = half3x3(
            input.tangentWS.xyz,
            input.bitangentWS.xyz,
            input.normalWS.xyz
        );
        surface.normalWS = SafeNormalize(mul(normalTS, TBN));
    #else
        surface.normalWS = SafeNormalize(input.normalWS.xyz);
    #endif
#endif
#undef _SB_SKIP_TBN
    
    // SURFACE NOISE (Procedural micro imperfections)
#if defined(_SB_SURFACE_NOISE)
    {
        half3 noiseNormal = CalculateSurfaceNoise(mainUV, input.positionWS, _Time.y);
        // Blend noise normal with surface normal
        surface.normalWS = SafeNormalize(surface.normalWS + (noiseNormal - half3(0, 0, 1)) * _SurfaceNoiseDistortion);
    }
#endif
    
    // NdotV
    surface.NdotV = saturate(dot(surface.normalWS, surface.viewDirWS));
    
    // Reflect direction
    surface.reflectDir = reflect(-surface.viewDirWS, surface.normalWS);
    
    // Fresnel
    surface.fresnel = CalculateFresnel(surface.NdotV);
    
    // METALLIC / SMOOTHNESS
#if defined(_SB_TRIPLANAR) && defined(_SB_METALLICGLOSS_MAP)
    half4 mgSample = SB_SampleTriplanar(TEXTURE2D_ARGS(_MetallicGlossMap, sampler_MetallicGlossMap), triData);
    surface.metallic = mgSample.r * _Metallic;
    surface.smoothness = mgSample.a * _Smoothness;
#else
    half2 ms = SampleMetallicSmoothness(mainUV * _MetallicGlossMap_ST.xy + _MetallicGlossMap_ST.zw);
    surface.metallic = ms.x;
    surface.smoothness = ms.y;
#endif
    
    // OCCLUSION
#if defined(_SB_TRIPLANAR) && defined(_SB_OCCLUSION_MAP)
    half occSample = SB_SampleTriplanar(TEXTURE2D_ARGS(_OcclusionMap, sampler_OcclusionMap), triData).g;
    surface.occlusion = lerp(1.0, occSample, _OcclusionStrength);
#else
    surface.occlusion = SampleOcclusion(mainUV * _OcclusionMap_ST.xy + _OcclusionMap_ST.zw);
#endif
    
    // EMISSION
#if defined(_SB_EMISSION)
    #if defined(_SB_TRIPLANAR) && defined(_SB_EMISSION_MAP)
        half3 emissionSample = SB_SampleTriplanar(TEXTURE2D_ARGS(_EmissionMap, sampler_EmissionMap), triData).rgb;
        surface.emission = emissionSample * _EmissionColor.rgb * _EmissionIntensity;
    #else
        surface.emission = SampleEmission(mainUV * _EmissionMap_ST.xy + _EmissionMap_ST.zw);
    #endif
#else
    surface.emission = half3(0.0, 0.0, 0.0);
#endif
    
    // DETAIL ALBEDO
#if defined(_SB_DETAIL_ALBEDO)
    #if defined(_SB_TRIPLANAR)
        SB_TriplanarData detailAlbedoTriData = SB_CalculateTriplanar(
            input.positionWS, 
            normalize(input.normalWS.xyz), 
            _DetailTiling,
            _TriplanarSharpness
        );
        half4 detailAlbedo = SB_SampleTriplanar(TEXTURE2D_ARGS(_DetailAlbedoMap, sampler_DetailAlbedoMap), detailAlbedoTriData) * _DetailColor;
    #else
        half4 detailAlbedo = SampleDetailAlbedo(input.uv.zw * _DetailAlbedoMap_ST.xy + _DetailAlbedoMap_ST.zw);
    #endif
    surface.albedo = lerp(surface.albedo, surface.albedo * detailAlbedo.rgb, _DetailAlbedoIntensity * detailAlbedo.a);
#endif
    
    // Refraction / Blur
    // Shatter distortion offset (applied to screen UV before sampling)
    float2 shatterOffset = float2(0, 0);
#if defined(_SB_DAMAGE)
    if (_DamageProgression > 0.001)
    {
        // Use crack data computed later? No - compute a lightweight early offset here
        float2 shatterUV = input.uv.xy * _ProceduralCrackDensity + _ProceduralCrackSeed;
        float2 shatterCell = floor(shatterUV);
        float shatterHash = frac(sin(dot(shatterCell, float2(127.1, 311.7))) * 43758.5453);
        shatterOffset = (shatterHash - 0.5) * _ShatterDistortion * _DamageProgression * 0.05;
    }
#endif
    
#if defined(_SB_REFRACTION)
    // Full refraction with distortion, IOR, chromatic, blur
    surface.refraction = CalculateRefraction(normalTS, surface.normalWS, surface.viewDirWS, input.screenPos);
#elif defined(_SB_BLUR)
    // No refraction but blur enabled - blur the scene without distortion
    float2 screenUV = ComputeNDC(input.screenPos);
    float blurSize = _BlurRadius * _BlurStrength * 2.0;
    
    if (blurSize > 0.001)
    {
        int quality = (int)_BlurQuality;
        if (quality <= 8)
            surface.refraction = SB_Blur8(screenUV, blurSize);
        else if (quality <= 16)
            surface.refraction = SB_Blur13(screenUV, blurSize);
        else
            surface.refraction = SB_BlurVariable(screenUV, blurSize, quality);
    }
    else
    {
        surface.refraction = SampleSceneColor(screenUV);
    }
#else
    // No refraction, no blur - use solid color based on albedo
    surface.refraction = surface.albedo;
#endif
    
    // Reflection
#if defined(_SB_REFLECTION)
    surface.reflection = CalculateReflection(surface.normalWS, surface.viewDirWS, surface.smoothness, input.positionWS);
#endif
    
    return surface;
}


// MIX FINAL COLOR (with Features)


half4 MixFinalColor(SB_GlassSurface surface, half3 lighting, half3 rim, half3 specular, half3 translucency, float time, SB_Varyings input, half3 lightDir)
{
    half3 color = half3(0.0, 0.0, 0.0);
    
    // DIRT/MOSS & FINGERPRINTS (Early - affects roughness and refraction)
    half surfaceRoughnessAdd = 0.0;
    half surfaceDirtAmount = 0.0;
    half3 fingerprintColorMod = half3(1, 1, 1);
    half dustNormalFlatten = 0.0;
    half3 dirtColor = half3(0.5, 0.5, 0.5);
    half dirtEffectMask = 0.0; // Masks specular, fresnel, reflections
    
#if defined(_SB_DUST)
    // Get object/local space position and normal (dirt follows the object)
    float3 positionOS = mul(unity_WorldToObject, float4(input.positionWS, 1.0)).xyz;
    float3 normalOS = mul((float3x3)unity_WorldToObject, surface.normalWS);
    normalOS = normalize(normalOS);
    
    DirtResult dirtResult = CalculateDirt(
        input.uv.xy,
        positionOS,
        normalOS,
        surface.NdotV
    );
    
    surfaceRoughnessAdd += dirtResult.roughness;
    surfaceDirtAmount += dirtResult.amount;
    dustNormalFlatten = dirtResult.normalBlend;
    dirtColor = dirtResult.color;
    dirtEffectMask = dirtResult.effectMask; // Store for masking light effects
    
    // Dirt affects normal (makes surface more matte/flat) - smooth blend
    half3 flatNormal = half3(0, 1, 0);
    surface.normalWS = normalize(lerp(surface.normalWS, flatNormal, dustNormalFlatten * 0.5));
#endif

#if defined(_SB_FINGERPRINTS)
    // Advanced multi-slot fingerprint system
    FingerprintResult fpResult = CalculateAdvancedFingerprints(
        input.uv.xy,
        input.positionWS,
        surface.normalWS,
        surface.tangentWS,
        cross(surface.normalWS, surface.tangentWS)
    );
    surfaceRoughnessAdd += fpResult.totalRoughness;
    surfaceDirtAmount += fpResult.totalAmount * 0.5;
    fingerprintColorMod = fpResult.colorTint;
    
    // Apply normal perturbation
    surface.normalWS = normalize(surface.normalWS + fpResult.normalPerturbation);
#endif
    
    // DAMAGE / PROCEDURAL CRACKS
    half crackMask = 0.0;
    half crackEmission = 0.0;
    
#if defined(_SB_DAMAGE)
    {
        // Always compute Voronoi cracks
        ProceduralCrackData cracks = SB_CalculateProceduralCracks(
            input.uv.xy,
            _DamageProgression,
            _ProceduralCrackSeed,
            _ProceduralCrackDensity,
            _CrackWidth,
            _CrackSharpness
        );
        crackMask = cracks.crackMask;
        crackEmission = cracks.crackEdge * _CrackEmission;
        
        // DamageMask = optional spatial mask (R channel: white = cracked zone, black = intact)
        // Multiplies the Voronoi result to control WHERE cracks are visible
        float2 damageUV = input.uv.xy * _DamageMask_ST.xy + _DamageMask_ST.zw;
        half maskSample = SAMPLE_TEXTURE2D(_DamageMask, sampler_DamageMask, damageUV).r;
        crackMask *= maskSample;
        crackEmission *= maskSample;
        
        // Normal perturbation from cracks (analytical tangent-space normal)
        // cracks.crackNormal is a tangent-space normal (0,0,1 = flat, XY = perturbation)
        half3 crackN = cracks.crackNormal;
        half3 perturbation = half3(crackN.xy * crackMask * _CrackDepth, 0);
        
        // Apply using TBN if available, otherwise direct world-space offset
    #if defined(_SB_NORMALMAP) || defined(_SB_DETAIL_NORMAL) || defined(_SB_RAIN)
        surface.normalWS = normalize(surface.normalWS
            + input.tangentWS.xyz * perturbation.x
            + input.bitangentWS.xyz * perturbation.y);
    #else
        // Fallback: construct approximate tangent from world normal
        half3 approxT = abs(surface.normalWS.y) > 0.99
            ? half3(1, 0, 0) : normalize(cross(surface.normalWS, half3(0, 1, 0)));
        half3 approxB = cross(surface.normalWS, approxT);
        surface.normalWS = normalize(surface.normalWS
            + approxT * perturbation.x
            + approxB * perturbation.y);
    #endif
        
        // Optional: blend CrackNormalMap for extra micro-detail on top
        half4 crackNormalSample = SAMPLE_TEXTURE2D(_CrackNormalMap, sampler_CrackNormalMap, damageUV);
        // Only apply if the texture is not the default bump (check if it has actual data)
        half3 extraNormal = UnpackNormalScale(crackNormalSample, crackMask * _CrackDepth * 0.5);
    #if defined(_SB_NORMALMAP) || defined(_SB_DETAIL_NORMAL) || defined(_SB_RAIN)
        surface.normalWS = normalize(surface.normalWS
            + input.tangentWS.xyz * extraNormal.x
            + input.bitangentWS.xyz * extraNormal.y);
    #else
        surface.normalWS = normalize(surface.normalWS
            + approxT * extraNormal.x
            + approxB * extraNormal.y);
    #endif
    }
    
    // Cracks add dirt-like opacity and color overlay
    surfaceDirtAmount += crackMask * 0.5;
    dirtColor = lerp(dirtColor, _CrackColor.rgb, crackMask * 0.7);
    dirtEffectMask = max(dirtEffectMask, crackMask * 0.3);
#endif
    
    // RAIN WET SURFACE (smoothness increase)
#if defined(_SB_RAIN)
    if (_RainIntensity > 0.001)
    {
        // Wet surface effect - more glossy based on wetness
        surface.smoothness = lerp(surface.smoothness, 0.98, _RainIntensity * _RainWetness);
    }
#endif
    
    // TINT TEXTURE (Vitrail effect)
#if defined(_SB_TINT_TEXTURE)
    float2 tintDistortion = surface.normalWS.xy * _Distortion;
    half4 tintTex = SampleTintTexture(input.uv.xy, tintDistortion);
    half3 tintColor = lerp(half3(1,1,1), tintTex.rgb, _TintTextureStrength);
#else
    half3 tintColor = half3(1,1,1);
#endif
    
    // THICKNESS EFFECTS
#if defined(_SB_THICKNESS_MAP)
    half thickness = SampleThickness(input.uv.xy * _ThicknessMap_ST.xy + _ThicknessMap_ST.zw);
    half3 thicknessColor = ThicknessToColor(thickness);
    tintColor *= lerp(half3(1,1,1), thicknessColor, _ThicknessAffectsColor);
#else
    half thickness = 1.0;
#endif
    
    // BASE REFRACTION
    half3 refractionTinted = lerp(surface.refraction, surface.refraction * surface.albedo, _MainTint);
    refractionTinted *= tintColor;
    
    // BEER-LAMBERT ABSORPTION
#if defined(_SB_ABSORPTION)
    refractionTinted = ApplyAbsorption(refractionTinted, thickness, surface.NdotV);
#endif
    
    // EDGE DARKENING
#if defined(_SB_EDGE_DARKENING)
    half edgeDark = CalculateEdgeDarkening(surface.NdotV);
    refractionTinted *= edgeDark;
#endif
    
    // DIRT/MOSS APPLICATION - Blocks refraction where dirt covers
#if defined(_SB_DUST)
    // Where dirt is present, replace refraction with solid dirt color
    // Use smoothstep for gradual transition (no harsh line)
    half dirtOpacity = smoothstep(0.0, 0.7, surfaceDirtAmount * _DirtFullOpacity);
    refractionTinted = lerp(refractionTinted, dirtColor, dirtOpacity);
#endif

    // DAMAGE - Crack color overlay + shatter distortion on refraction
#if defined(_SB_DAMAGE)
    {
        half crackOverlay = smoothstep(0.0, 0.7, crackMask);
        refractionTinted = lerp(refractionTinted, _CrackColor.rgb, crackOverlay * 0.7);
    }
#endif

#if defined(_SB_FINGERPRINTS)
    // Apply fingerprint color modification
    refractionTinted *= fingerprintColorMod;
#endif

    // RAIN WET GLASS EFFECT
#if defined(_SB_RAIN)
    // Wet surface slightly darkens refraction
    refractionTinted *= lerp(1.0, 0.95, _RainIntensity * 0.3);
#endif
    
    // DECALS
#if defined(_SB_DECALS)
    DecalResult decals = CalculateDecals(input.uv.xy);
    // Blend decals over refraction (they appear on the glass surface)
    refractionTinted = lerp(refractionTinted, decals.color, decals.alpha);
    // Also blend into albedo for reflections
    surface.albedo = lerp(surface.albedo, decals.color, decals.alpha * 0.5);
#endif
    
    // REFLECTION MIXING WITH TIR
#if defined(_SB_REFLECTION)
    // Reflection factor - use intensity directly for more control
    half reflectionFactor = _ReflectionIntensity;
    
    // Dirt completely blocks reflections
    reflectionFactor *= (1.0 - dirtEffectMask);
    
    // Also dim the reflection itself where dirt exists
    half3 maskedReflection = surface.reflection * (1.0 - dirtEffectMask);
    
    #if defined(_SB_FRESNEL)
        // Optionally modulate by fresnel (stronger at grazing angles)
        if (_FresnelAffectReflection > 0.5)
        {
            reflectionFactor *= lerp(0.3, 1.0, surface.fresnel);
        }
    #endif
    
    #if defined(_SB_TIR)
        half tirFactor = CalculateTIR(surface.NdotV, _IndexOfRefraction + 1.0);
        tirFactor *= (1.0 - dirtEffectMask); // Dirt also masks TIR
        reflectionFactor = saturate(reflectionFactor + tirFactor);
        color = ApplyTIR(refractionTinted, maskedReflection, tirFactor);
        color = lerp(color, maskedReflection, reflectionFactor * (1.0 - tirFactor));
    #else
        color = lerp(refractionTinted, maskedReflection, saturate(reflectionFactor));
    #endif
#else
    // No reflection - just use refraction
    color = refractionTinted;
#endif
    
    // FRESNEL COLOR CONTRIBUTION (additive edge glow) - masked by dirt
#if defined(_SB_FRESNEL)
    half3 fresnelGlow = _FresnelColor.rgb * surface.fresnel * _FresnelIntensity;
    fresnelGlow *= (1.0 - dirtEffectMask); // Dirt hides fresnel glow
    color += fresnelGlow;
#endif
    
    // LIGHTING
    color *= lerp(half3(1,1,1), lighting, _DiffuseIntensity);
    
    // Dirt completely masks specular, rim, and translucency effects
    half effectReduction = 1.0 - dirtEffectMask;
    color += specular * effectReduction;
    color += rim * effectReduction;
    color += translucency * surface.albedo * effectReduction;
    
    // CAUSTICS - masked by dirt
#if defined(_SB_CAUSTICS)
    #if defined(_SB_CAUSTICS_PROCEDURAL)
        half3 caustics = ProceduralCaustics(input.positionWS, surface.normalWS, time);
    #else
        half3 caustics = SampleCaustics(input.positionWS, surface.normalWS, time);
    #endif
    color += caustics * effectReduction;
#endif
    
    // SPARKLE / GLITTER - masked by dirt
#if defined(_SB_SPARKLE)
    half3 sparkle = CalculateSparkle(input.positionWS, surface.viewDirWS, surface.normalWS, time);
    color += sparkle * effectReduction;
#endif
    
    // IRIDESCENCE - masked by dirt
#if defined(_SB_IRIDESCENCE)
    half3 iridescence = CalculateIridescence(surface.NdotV, surface.normalWS, time);
    color += iridescence * effectReduction;
#endif
    
    // INNER GLOW - masked by dirt
#if defined(_SB_INNER_GLOW)
    half3 innerGlow = CalculateInnerGlow(surface.NdotV, thickness);
    color += innerGlow * effectReduction;
#endif
    
    color += surface.emission;
    
    // DAMAGE - Crack edge emission (glowing fracture lines)
#if defined(_SB_DAMAGE)
    color += _CrackColor.rgb * crackEmission;
#endif
    
    color *= surface.occlusion;
    
    // FINAL ALPHA
#if defined(_SB_FRESNEL)
    half fresnelAlphaContrib = _FresnelAffectAlpha > 0.5 ? surface.fresnel * _FresnelIntensity * 0.5 : 0.0;
    half alpha = surface.alpha + fresnelAlphaContrib;
#else
    half alpha = surface.alpha;
#endif

#if defined(_SB_FALLOFF_OPACITY)
    // Falloff Opacity based on view angle (similar to Fresnel but for opacity only)
    half falloffNdotV = saturate(surface.NdotV);
    half falloffOpacity = pow(1.0 - falloffNdotV, _FalloffOpacityPower);
    
    // Invert if needed (edge vs center opacity)
    falloffOpacity = _FalloffOpacityInvert > 0.5 ? 1.0 - falloffOpacity : falloffOpacity;
    
    // Apply with intensity control
    alpha = lerp(alpha, alpha * falloffOpacity, _FalloffOpacityIntensity);
#endif
    
#if defined(_SB_DEPTH_FADE)
    half depthFade = CalculateDepthFadeAlpha(input.screenPos, input.positionWS);
    alpha *= depthFade;
#endif
    
#if defined(_SB_TINT_TEXTURE)
    alpha = lerp(alpha, alpha * tintTex.a, _TintTextureBlend);
#endif
    
    alpha = saturate(alpha);
    
    return half4(color, alpha);
}

#endif // SB_GLASS_SURFACE_INCLUDED
