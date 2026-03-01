// ============================================================================
// SingularBear Glass - Core URP Includes
// ============================================================================

#ifndef SB_GLASS_CORE_INCLUDED
#define SB_GLASS_CORE_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareOpaqueTexture.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

// ============================================================================
// UTILITY FUNCTIONS
// ============================================================================

inline half Luminance(half3 color)
{
    return dot(color, half3(0.2126, 0.7152, 0.0722));
}

// ============================================================================
// DITHERING
// ============================================================================

inline half SB_Dither4x4(float2 screenPos, half alpha)
{
    const half dither[16] = {
        0.0/16.0,  8.0/16.0,  2.0/16.0, 10.0/16.0,
        12.0/16.0, 4.0/16.0, 14.0/16.0,  6.0/16.0,
        3.0/16.0, 11.0/16.0,  1.0/16.0,  9.0/16.0,
        15.0/16.0, 7.0/16.0, 13.0/16.0,  5.0/16.0
    };
    
    int2 pos = int2(fmod(screenPos, 4.0));
    int index = pos.y * 4 + pos.x;
    return alpha - dither[index];
}

inline bool SB_ApplyDithering(half alpha, float2 screenPos, half strength, half scale)
{
    float2 ditherPos = screenPos * scale;
    half dithered = SB_Dither4x4(ditherPos, alpha);
    return dithered < strength * (1.0 - alpha);
}

// ============================================================================
// PROCEDURAL CRACKS (Voronoi-based)
// ============================================================================

float SB_CrackHash(float2 p)
{
    return frac(sin(dot(p, float2(127.1, 311.7))) * 43758.5453);
}

float2 SB_CrackVoronoi(float2 p)
{
    float2 n = floor(p);
    float2 f = frac(p);
    
    float minDist = 8.0;
    float2 minCellPoint = float2(0, 0);
    
    for (int y = -1; y <= 1; y++)
    {
        for (int x = -1; x <= 1; x++)
        {
            float2 neighbor = float2(x, y);
            float2 cellPoint = SB_CrackHash(n + neighbor).xx;
            float2 diff = neighbor + cellPoint - f;
            float dist = dot(diff, diff);
            
            if (dist < minDist)
            {
                minDist = dist;
                minCellPoint = cellPoint;
            }
        }
    }
    
    return float2(sqrt(minDist), SB_CrackHash(minCellPoint));
}

struct ProceduralCrackData
{
    half crackMask;
    half crackEdge;
    half2 crackNormal;
};

ProceduralCrackData SB_CalculateProceduralCracks(float2 uv, half progression, half seed)
{
    ProceduralCrackData data;
    
    float2 voronoi = SB_CrackVoronoi(uv * 5.0 + seed);
    half edge = 1.0 - smoothstep(0.0, 0.1, voronoi.x);
    
    half threshold = 1.0 - progression;
    data.crackMask = smoothstep(threshold, threshold + 0.1, edge);
    data.crackEdge = smoothstep(threshold - 0.05, threshold, edge) * 
                     (1.0 - smoothstep(threshold, threshold + 0.05, edge));
    
    float2 grad;
    grad.x = SB_CrackVoronoi(uv * 5.0 + seed + float2(0.01, 0)).x - voronoi.x;
    grad.y = SB_CrackVoronoi(uv * 5.0 + seed + float2(0, 0.01)).x - voronoi.x;
    data.crackNormal = normalize(grad) * 0.5 + 0.5;
    
    return data;
}

// ============================================================================
// WEATHERING NOISE
// ============================================================================

float SB_WeatherNoise(float2 p)
{
    float2 i = floor(p);
    float2 f = frac(p);
    float a = SB_CrackHash(i);
    float b = SB_CrackHash(i + float2(1.0, 0.0));
    float c = SB_CrackHash(i + float2(0.0, 1.0));
    float d = SB_CrackHash(i + float2(1.0, 1.0));
    float2 u = f * f * (3.0 - 2.0 * f);
    return lerp(lerp(a, b, u.x), lerp(c, d, u.x), u.y);
}

// ============================================================================
// BLUR FUNCTIONS
// ============================================================================

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

half3 SB_Blur4(float2 uv, float blurSize)
{
    half3 color = half3(0, 0, 0);
    color += SampleSceneColor(uv + float2(-blurSize, -blurSize));
    color += SampleSceneColor(uv + float2( blurSize, -blurSize));
    color += SampleSceneColor(uv + float2(-blurSize,  blurSize));
    color += SampleSceneColor(uv + float2( blurSize,  blurSize));
    return color * 0.25;
}

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

half3 SB_FrostedGlassBlur(float2 uv, float2 texelSize, half strength, half radius, int quality)
{
    if (strength < 0.001)
        return SampleSceneColor(uv).rgb;
    
    float blurSize = (radius * 0.015) * strength;
    blurSize = clamp(blurSize, 0.0, 0.15);
    
    if (quality <= 4)
        return SB_Blur4(uv, blurSize);
    else if (quality <= 8)
        return SB_Blur8(uv, blurSize);
    else
        return SB_Blur16(uv, blurSize);
}

// ============================================================================
// DISTORTION FX
// ============================================================================

float2 SB_ApplyDistortions(float2 uv, float time)
{
    // Placeholder - distortion functions integrated in Forward
    return uv;
}

#endif // SB_GLASS_CORE_INCLUDED
