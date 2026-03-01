// ============================================================================
// SB_GlassCore.hlsl
// Core structures and functions for SingularBear Glass Shader
// ============================================================================
// Compatibility:
// - Unity 2021.3+ / Unity 2022 LTS / Unity 6
// - URP 12+ (Universal Render Pipeline)
// - VR: Single Pass Instanced, Multi-View
// - Mobile: GLES 3.0+, Metal, Vulkan
// ============================================================================

#ifndef SB_GLASS_CORE_INCLUDED
#define SB_GLASS_CORE_INCLUDED

// Core URP includes
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareOpaqueTexture.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

// ============================================================================
// VERTEX INPUT STRUCTURE
// ============================================================================
struct SB_Attributes
{
    float4 positionOS   : POSITION;
    float3 normalOS     : NORMAL;
    float4 tangentOS    : TANGENT;
    float2 uv           : TEXCOORD0;
    float2 uvLightmap   : TEXCOORD1;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct SB_Varyings
{
    float4 positionCS       : SV_POSITION;
    float4 uv               : TEXCOORD0;        // xy = main UV, zw = detail UV
    float3 positionWS       : TEXCOORD1;
    
#if defined(_SB_NORMALMAP) || defined(_SB_DETAIL_NORMAL) || defined(_SB_RAIN)
    float4 normalWS         : TEXCOORD2;        // xyz = normal, w = viewDir.x
    float4 tangentWS        : TEXCOORD3;        // xyz = tangent, w = viewDir.y
    float4 bitangentWS      : TEXCOORD4;        // xyz = bitangent, w = viewDir.z
    float4 screenPos        : TEXCOORD5;
    float4 fogFactorAndVertexLight : TEXCOORD6; // x = fog, yzw = vertex light
    #if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
        float4 shadowCoord  : TEXCOORD7;
        #if defined(LIGHTMAP_ON)
            float2 uvLightmap : TEXCOORD8;
        #else
            half3 vertexSH  : TEXCOORD8;
        #endif
    #else
        #if defined(LIGHTMAP_ON)
            float2 uvLightmap : TEXCOORD7;
        #else
            half3 vertexSH  : TEXCOORD7;
        #endif
    #endif
#else
    // Without normal map: fewer interpolators
    float3 normalWS         : TEXCOORD2;
    float3 viewDirWS        : TEXCOORD3;
    float4 screenPos        : TEXCOORD4;
    float4 fogFactorAndVertexLight : TEXCOORD5;
    #if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
        float4 shadowCoord  : TEXCOORD6;
        #if defined(LIGHTMAP_ON)
            float2 uvLightmap : TEXCOORD7;
        #else
            half3 vertexSH  : TEXCOORD7;
        #endif
    #else
        #if defined(LIGHTMAP_ON)
            float2 uvLightmap : TEXCOORD6;
        #else
            half3 vertexSH  : TEXCOORD6;
        #endif
    #endif
#endif
    
    UNITY_VERTEX_INPUT_INSTANCE_ID
    UNITY_VERTEX_OUTPUT_STEREO
};

struct SB_GlassSurface
{
    half3 albedo;
    half3 normalWS;
    half3 viewDirWS;
    half3 tangentWS;
    half3 reflectDir;
    half3 emission;
    half metallic;
    half smoothness;
    half occlusion;
    half alpha;
    half fresnel;
    half NdotV;
    half3 refraction;
    half3 reflection;
};

inline float SB_Hash(float2 p)
{
    float3 p3 = frac(float3(p.xyx) * 0.1031);
    p3 += dot(p3, p3.yzx + 33.33);
    return frac((p3.x + p3.y) * p3.z);
}

inline float SB_Noise2D(float2 p)
{
    float2 i = floor(p);
    float2 f = frac(p);
    f = f * f * (3.0 - 2.0 * f);
    
    float a = SB_Hash(i);
    float b = SB_Hash(i + float2(1.0, 0.0));
    float c = SB_Hash(i + float2(0.0, 1.0));
    float d = SB_Hash(i + float2(1.0, 1.0));
    
    return lerp(lerp(a, b, f.x), lerp(c, d, f.x), f.y);
}

inline float SB_FBM(float2 p, int octaves)
{
    float value = 0.0;
    float amplitude = 0.5;
    float frequency = 1.0;
    
    for (int i = 0; i < octaves; i++)
    {
        value += amplitude * SB_Noise2D(p * frequency);
        amplitude *= 0.5;
        frequency *= 2.0;
    }
    
    return value;
}

inline float3 SB_Hash3D(float3 p)
{
    p = float3(dot(p, float3(127.1, 311.7, 74.7)),
               dot(p, float3(269.5, 183.3, 246.1)),
               dot(p, float3(113.5, 271.9, 124.6)));
    return frac(sin(p) * 43758.5453123);
}

inline float SB_Noise3D(float3 p)
{
    float3 i = floor(p);
    float3 f = frac(p);
    f = f * f * (3.0 - 2.0 * f);
    
    float n = lerp(
        lerp(lerp(dot(SB_Hash3D(i), f),
                  dot(SB_Hash3D(i + float3(1,0,0)), f - float3(1,0,0)), f.x),
             lerp(dot(SB_Hash3D(i + float3(0,1,0)), f - float3(0,1,0)),
                  dot(SB_Hash3D(i + float3(1,1,0)), f - float3(1,1,0)), f.x), f.y),
        lerp(lerp(dot(SB_Hash3D(i + float3(0,0,1)), f - float3(0,0,1)),
                  dot(SB_Hash3D(i + float3(1,0,1)), f - float3(1,0,1)), f.x),
             lerp(dot(SB_Hash3D(i + float3(0,1,1)), f - float3(0,1,1)),
                  dot(SB_Hash3D(i + float3(1,1,1)), f - float3(1,1,1)), f.x), f.y), f.z);
    
    return n * 0.5 + 0.5;
}

inline half3 SB_RGBtoHSV(half3 c)
{
    half4 K = half4(0.0, -1.0/3.0, 2.0/3.0, -1.0);
    half4 p = lerp(half4(c.bg, K.wz), half4(c.gb, K.xy), step(c.b, c.g));
    half4 q = lerp(half4(p.xyw, c.r), half4(c.r, p.yzx), step(p.x, c.r));
    half d = q.x - min(q.w, q.y);
    half e = 1.0e-10;
    return half3(abs(q.z + (q.w - q.y) / (6.0 * d + e)), d / (q.x + e), q.x);
}

inline half3 SB_HSVtoRGB(half3 c)
{
    half4 K = half4(1.0, 2.0/3.0, 1.0/3.0, 3.0);
    half3 p = abs(frac(c.xxx + K.xyz) * 6.0 - K.www);
    return c.z * lerp(K.xxx, saturate(p - K.xxx), c.y);
}

inline half3 SB_HueShift(half3 color, half shift)
{
    half3 hsv = SB_RGBtoHSV(color);
    hsv.x = frac(hsv.x + shift);
    return SB_HSVtoRGB(hsv);
}

inline half SB_FresnelEffect(half3 normal, half3 viewDir, half power)
{
    return pow(1.0 - saturate(dot(normalize(normal), normalize(viewDir))), power);
}

inline half3 SB_FresnelSchlick(half NdotV, half3 F0)
{
    return F0 + (1.0 - F0) * pow(1.0 - NdotV, 5.0);
}

inline float2 SB_GetScreenUV(float4 screenPos)
{
    return screenPos.xy / screenPos.w;
}

inline float2 ComputeNDC(float4 screenPos)
{
    return screenPos.xy / screenPos.w;
}

inline float SB_LinearDepthFromRaw(float rawDepth)
{
    return LinearEyeDepth(rawDepth, _ZBufferParams);
}

inline float SB_ValueNoise(float2 p)
{
    float2 i = floor(p);
    float2 f = frac(p);
    f = f * f * (3.0 - 2.0 * f);
    
    float a = SB_Hash(i);
    float b = SB_Hash(i + float2(1.0, 0.0));
    float c = SB_Hash(i + float2(0.0, 1.0));
    float d = SB_Hash(i + float2(1.0, 1.0));
    
    return lerp(lerp(a, b, f.x), lerp(c, d, f.x), f.y);
}

inline half3 SB_NoiseNormal(float2 uv, float scale, float strength, float time)
{
    float2 noiseUV = uv * scale + time * 0.1;
    
    float h = SB_Noise2D(noiseUV);
    float hx = SB_Noise2D(noiseUV + float2(0.01, 0.0));
    float hy = SB_Noise2D(noiseUV + float2(0.0, 0.01));
    
    float dx = (hx - h) * strength;
    float dy = (hy - h) * strength;
    
    return normalize(half3(-dx, -dy, 1.0));
}

inline half SB_DepthFade(float4 screenPos, float3 positionWS, float fadeDistance)
{
    float2 screenUV = screenPos.xy / screenPos.w;
    float rawDepth = SampleSceneDepth(screenUV);
    float sceneDepth = LinearEyeDepth(rawDepth, _ZBufferParams);
    float surfaceDepth = screenPos.w;
    float depthDiff = sceneDepth - surfaceDepth;
    return saturate(depthDiff / max(fadeDistance, 0.001));
}

inline half3 BlendNormalsRNM(half3 n1, half3 n2)
{
    n1.z += 1.0;
    n2.xy = -n2.xy;
    return normalize(n1 * dot(n1, n2) - n2 * n1.z);
}

// ============================================================================
// PROCEDURAL CRACKS (Voronoi-based, F2-F1 edge detection)
// ============================================================================

struct ProceduralCrackData
{
    half crackMask;     // Overall crack intensity (0 = intact, 1 = cracked)
    half crackEdge;     // Thin bright edge (for emission glow)
    half3 crackNormal;  // Tangent-space normal perturbation
    half cellShade;     // Per-cell random value (for shattered look)
};

float2 SB_CrackHash2(float2 p)
{
    return frac(sin(float2(
        dot(p, float2(127.1, 311.7)),
        dot(p, float2(269.5, 183.3))
    )) * 43758.5453);
}

// Voronoi F1/F2 with analytical edge normal
// Returns: x = F1, y = F2, z = F2-F1 (edge distance)
// Out: cellID = per-cell hash, edgeDir = direction perpendicular to edge in UV space
float3 SB_CrackVoronoi(float2 p, out float cellID, out float2 edgeDir)
{
    float2 n = floor(p);
    float2 f = frac(p);
    
    float f1 = 8.0;
    float f2 = 8.0;
    float2 closestCell = float2(0, 0);
    float2 closestPt = float2(0, 0);
    float2 secondPt = float2(0, 0);
    
    for (int iy = -1; iy <= 1; iy++)
    {
        for (int ix = -1; ix <= 1; ix++)
        {
            float2 neighbor = float2(ix, iy);
            float2 cell = n + neighbor;
            float2 randomPt = SB_CrackHash2(cell);
            float2 delta = neighbor + randomPt - f;
            float dist = dot(delta, delta); // Squared (faster sort)
            
            if (dist < f1)
            {
                f2 = f1;
                secondPt = closestPt;
                f1 = dist;
                closestCell = cell;
                closestPt = delta;
            }
            else if (dist < f2)
            {
                f2 = dist;
                secondPt = delta;
            }
        }
    }
    
    f1 = sqrt(f1);
    f2 = sqrt(f2);
    cellID = frac(sin(dot(closestCell, float2(127.1, 311.7))) * 43758.5453);
    
    // Analytical edge direction: vector from closest to second-closest cell center
    // This is perpendicular to the Voronoi edge, pointing inward (toward cell center)
    float2 toSecond = secondPt - closestPt;
    float toSecondLen = length(toSecond);
    edgeDir = toSecondLen > 0.001 ? toSecond / toSecondLen : float2(0, 1);
    
    return float3(f1, f2, f2 - f1);
}

ProceduralCrackData SB_CalculateProceduralCracks(
    float2 uv,
    half progression,
    half seed,
    half density,
    half crackWidth,
    half sharpness)
{
    ProceduralCrackData data;
    data.crackMask = 0;
    data.crackEdge = 0;
    data.crackNormal = half3(0, 0, 1);
    data.cellShade = 0;
    
    // Scale UV by density + seed offset
    float2 cellUV = uv * density + seed;
    
    // Compute Voronoi F1/F2 with analytical edge direction
    float cellID;
    float2 edgeDir;
    float3 voronoi = SB_CrackVoronoi(cellUV, cellID, edgeDir);
    float edgeDist = voronoi.z; // F2 - F1: small = near edge, large = cell center
    
    data.cellShade = cellID;
    
    // Edge detection: width controls crack thickness, sharpness controls falloff
    float widthScaled = max(crackWidth * 0.15, 0.001);
    float edgeMask = 1.0 - smoothstep(0.0, widthScaled, edgeDist);
    edgeMask = pow(edgeMask, max(sharpness, 0.1));
    
    // Scale by progression (0 = no cracks, 1 = full)
    data.crackMask = edgeMask * progression;
    
    // Thin bright edge for emission
    float thinEdge = 1.0 - smoothstep(0.0, widthScaled * 0.25, edgeDist);
    data.crackEdge = thinEdge * progression;
    
    // Analytical tangent-space normal from edge direction
    // edgeDir is perpendicular to the crack edge in UV space
    // Near a crack: the surface "dips" into the crack = normal points away from edge
    // This creates a groove/bevel effect along the crack lines
    half2 normalXY = edgeDir * data.crackMask;
    data.crackNormal = normalize(half3(normalXY, 1.0 - data.crackMask * 0.8));
    
    return data;
}

#endif
