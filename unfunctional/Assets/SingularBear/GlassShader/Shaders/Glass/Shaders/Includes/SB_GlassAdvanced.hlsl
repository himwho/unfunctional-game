// SB_GlassAdvanced.hlsl - Absorption, Caustics, TIR, Sparkle, Dust

#ifndef SB_GLASS_ADVANCED_INCLUDED
#define SB_GLASS_ADVANCED_INCLUDED

#if defined(_SB_ABSORPTION)
inline half3 CalculateAbsorption(half thickness, half NdotV)
{
    half pathLength = thickness / max(NdotV, 0.1);
    pathLength = pow(pathLength, _AbsorptionFalloff);
    half3 absorption = exp(-_AbsorptionColor.rgb * _AbsorptionDensity * pathLength);
    return absorption;
}

inline half3 ApplyAbsorption(half3 refractionColor, half thickness, half NdotV)
{
    half3 absorption = CalculateAbsorption(thickness, NdotV);
    half3 tintedColor = refractionColor * absorption;
    half3 deepColor = lerp(tintedColor, _AbsorptionColor.rgb * 0.5, saturate(1.0 - absorption.g));
    return lerp(refractionColor, deepColor, _AbsorptionColor.a);
}
#endif

#if defined(_SB_CAUSTICS)

inline float CausticCell(float2 uv, float time)
{
    float2 p = frac(uv) - 0.5;
    float2 id = floor(uv);
    float minDist = 1.0;
    
    for (int y = -1; y <= 1; y++)
    {
        for (int x = -1; x <= 1; x++)
        {
            float2 offset = float2(x, y);
            float2 cellId = id + offset;
            float2 noise = frac(sin(float2(dot(cellId, float2(127.1, 311.7)),
                                           dot(cellId, float2(269.5, 183.3)))) * 43758.5453);
            float2 cellPos = offset + sin(noise * 6.28 + time) * 0.5 - p;
            float dist = length(cellPos);
            minDist = min(minDist, dist);
        }
    }
    return minDist;
}

inline half3 CalculateCaustics(float3 positionWS, float3 normalWS, float time)
{
    float2 uvBase = positionWS.xz * _CausticsScale;
    float distortion = (normalWS.x + normalWS.z) * _CausticsDistortion;
    uvBase += distortion;
    
    float caustic1 = CausticCell(uvBase, time * _CausticsSpeed);
    float caustic2 = CausticCell(uvBase * 1.3 + 0.5, time * _CausticsSpeed * 0.8);
    float caustics = min(caustic1, caustic2);
    caustics = pow(1.0 - caustics, 3.0);
    
    return _CausticsColor.rgb * caustics * _CausticsIntensity;
}

inline half3 ProceduralCaustics(float3 positionWS, float3 normalWS, float time)
{
    return CalculateCaustics(positionWS, normalWS, time);
}

inline half3 SampleCaustics(float3 positionWS, float3 normalWS, float time)
{
    float2 uv = positionWS.xz * _CausticsScale;
    uv = uv * _CausticsTexture_ST.xy + _CausticsTexture_ST.zw;
    half4 causticSample = SAMPLE_TEXTURE2D(_CausticsTexture, sampler_CausticsTexture, uv + time * _CausticsSpeed * 0.1);
    return causticSample.rgb * _CausticsColor.rgb * _CausticsIntensity;
}
#endif

#if defined(_SB_TIR)
inline half CalculateTIR(half NdotV, half fresnel)
{
    half tirAngle = 1.0 - _TIRCriticalAngle;
    half tirFactor = saturate((tirAngle - NdotV) / tirAngle);
    tirFactor = pow(max(tirFactor, 0.001), 1.0 / max(_TIRSharpness, 0.1));
    return tirFactor * _TIRIntensity;
}

inline half3 ApplyTIR(half3 refraction, half3 reflection, half tirFactor)
{
    // Total Internal Reflection: at critical angles, light is fully reflected
    return lerp(refraction, reflection, saturate(tirFactor));
}
#endif

#if defined(_SB_SPARKLE)
inline half3 CalculateSparkle(float3 positionWS, float3 viewDirWS, float3 normalWS, float time)
{
    // Grid position for sparkle cells
    float3 sparklePos = positionWS * _SparkleScale;
    float3 cellID = floor(sparklePos);
    float3 cellUV = frac(sparklePos) - 0.5;
    
    // Random values per cell
    float3 noise3D = SB_Hash3D(cellID);
    
    // Density check - lower value = more sparkles
    float presence = step(noise3D.x, _SparkleDensity);
    if (presence < 0.5) return half3(0, 0, 0);
    
    // Random sparkle normal for this cell
    float3 sparkleNormal = normalize(noise3D * 2.0 - 1.0);
    
    // View-dependent sparkle (catches light at specific angles)
    float sparkleNdotV = saturate(dot(sparkleNormal, viewDirWS));
    float sparkleIntensity = pow(sparkleNdotV, 16.0 / _SparkleSize);
    
    // Animation - twinkle effect
    float phase = noise3D.y * 6.28318;
    float animation = sin(time * _SparkleSpeed + phase) * 0.5 + 0.5;
    animation = pow(animation, 2.0); // Sharper twinkle
    
    // Combine
    sparkleIntensity *= animation;
    
    // Fade by distance from cell center for softer look
    float distFromCenter = length(cellUV);
    sparkleIntensity *= 1.0 - saturate(distFromCenter * 2.0);
    
    return _SparkleColor.rgb * sparkleIntensity * _SparkleIntensity;
}
#endif

// ============================================================
// DIRT/MOSS SYSTEM
// ============================================================
#if defined(_SB_DUST)

// Simple noise function for edge variation
inline half DirtNoise(float2 p)
{
    return frac(sin(dot(p, float2(12.9898, 78.233))) * 43758.5453);
}

// Value noise for smoother variation
inline half DirtValueNoise(float2 p)
{
    float2 i = floor(p);
    float2 f = frac(p);
    f = f * f * (3.0 - 2.0 * f);
    
    half a = DirtNoise(i);
    half b = DirtNoise(i + float2(1.0, 0.0));
    half c = DirtNoise(i + float2(0.0, 1.0));
    half d = DirtNoise(i + float2(1.0, 1.0));
    
    return lerp(lerp(a, b, f.x), lerp(c, d, f.x), f.y);
}

// Sample dirt texture with optional triplanar - ALL IN LOCAL SPACE
inline half SampleDirtTexture(float2 uv, float3 positionOS, float3 normalOS)
{
    half dirtSample;
    
    #if defined(_SB_DUST_TRIPLANAR)
        // Triplanar sampling in LOCAL/OBJECT space (follows object movement)
        float3 blending = pow(abs(normalOS), _DustTriplanarSharpness);
        blending /= (blending.x + blending.y + blending.z + 0.001);
        
        // Rotation matrix (around Y axis to hide seams)
        float angle = _DustTriplanarRotation * 0.0174533; // Degrees to radians
        float cosA = cos(angle);
        float sinA = sin(angle);
        
        // Rotate LOCAL position for UV generation
        float3 rotatedPos;
        rotatedPos.x = positionOS.x * cosA - positionOS.z * sinA;
        rotatedPos.y = positionOS.y;
        rotatedPos.z = positionOS.x * sinA + positionOS.z * cosA;
        
        float2 uvX = rotatedPos.zy * _DustTriplanarScale;
        float2 uvY = rotatedPos.xz * _DustTriplanarScale;
        float2 uvZ = rotatedPos.xy * _DustTriplanarScale;
        
        half sampleX = SAMPLE_TEXTURE2D(_DustTexture, sampler_DustTexture, uvX * _DustTexture_ST.xy + _DustTexture_ST.zw).r;
        half sampleY = SAMPLE_TEXTURE2D(_DustTexture, sampler_DustTexture, uvY * _DustTexture_ST.xy + _DustTexture_ST.zw).r;
        half sampleZ = SAMPLE_TEXTURE2D(_DustTexture, sampler_DustTexture, uvZ * _DustTexture_ST.xy + _DustTexture_ST.zw).r;
        
        dirtSample = sampleX * blending.x + sampleY * blending.y + sampleZ * blending.z;
    #else
        float2 dirtUV = uv * _DustTiling;
        dirtUV = dirtUV * _DustTexture_ST.xy + _DustTexture_ST.zw;
        dirtSample = SAMPLE_TEXTURE2D(_DustTexture, sampler_DustTexture, dirtUV).r;
    #endif
    
    return dirtSample;
}

// Calculate dirt/moss growth based on direction and local position
inline half CalculateDirtGrowth(float3 positionOS, float3 normalOS, float2 uv)
{
    half growthFactor = 0.0;
    
    // Use local Y position for height-based growth (follows object)
    half localY = positionOS.y;
    
    #if defined(_DIRTDIRECTION_BOTTOM_UP)
        // Moss grows from bottom up
        // _DirtHeight = where moss ENDS (top of moss)
        // _DirtSpread = how gradual the transition is
        // Below (_DirtHeight - _DirtSpread) = full moss
        // Above _DirtHeight = no moss
        half topEdge = _DirtHeight;
        half bottomEdge = _DirtHeight - _DirtSpread;
        growthFactor = 1.0 - saturate((localY - bottomEdge) / max(0.01, _DirtSpread));
        
    #elif defined(_DIRTDIRECTION_TOP_DOWN)
        // Dirt/grime drips from top down
        // _DirtHeight = where dirt ENDS (bottom of dirt)
        // Above (_DirtHeight + _DirtSpread) = full dirt
        // Below _DirtHeight = no dirt
        half bottomEdge = _DirtHeight;
        half topEdge = _DirtHeight + _DirtSpread;
        growthFactor = saturate((localY - bottomEdge) / max(0.01, _DirtSpread));
        
    #else // _DIRTDIRECTION_NORMAL_BASED
        // Settles on horizontal surfaces (like dust)
        half upFacing = saturate(normalOS.y + _DirtHeight);
        growthFactor = pow(upFacing, 2.0 - _DirtSpread);
    #endif
    
    // Add edge noise for organic look (in local space)
    if (_DirtUseEdgeNoise > 0.5)
    {
        half noise = DirtValueNoise(positionOS.xz * _DirtEdgeNoiseScale);
        noise = noise * 2.0 - 1.0; // -1 to 1
        growthFactor += noise * _DirtEdgeNoiseStrength;
        growthFactor = saturate(growthFactor);
    }
    
    return growthFactor;
}

// Result structure for dirt calculation
struct DirtResult
{
    half amount;           // Final dirt amount (0-1)
    half3 color;           // Dirt color with variation
    half roughness;        // Roughness to add
    half normalBlend;      // How much to blend normal
    half effectMask;       // Mask for hiding specular/fresnel (1 = hide effects)
};

// Full dirt/moss calculation - ALL IN LOCAL/OBJECT SPACE
inline DirtResult CalculateDirt(
    float2 uv, 
    float3 positionOS,
    float3 normalOS, 
    half NdotV)
{
    DirtResult result = (DirtResult)0;
    
    // Calculate growth factor based on height/direction (local space)
    half growthFactor = CalculateDirtGrowth(positionOS, normalOS, uv);
    
    // Sample dirt texture pattern (local space triplanar)
    half textureSample = SampleDirtTexture(uv, positionOS, normalOS);
    
    // Coverage Threshold controls how much texture affects the result
    // 0 = texture fully controls pattern
    // 1 = ignore texture, use height gradient only
    half textureInfluence = lerp(textureSample, 1.0, _DustCoverage);
    
    // Combine: growth defines WHERE, texture defines PATTERN
    half rawAmount = growthFactor * textureInfluence;
    
    // Apply edge softness for smooth transition
    half softness = max(0.01, _DirtSoftness);
    half finalAmount = smoothstep(0.0, softness, rawAmount);
    
    // Optional fresnel falloff (less dirt on edges facing camera)
    if (_DustEdgeFalloff > 0.5)
    {
        finalAmount *= pow(NdotV, _DustEdgePower);
    }
    
    // Final amount
    result.amount = finalAmount * _DustIntensity;
    result.amount = saturate(result.amount);
    
    // Color with variation (local space noise)
    half variation = DirtValueNoise(positionOS.xz * _DirtVariationScale);
    result.color = lerp(_DustColor.rgb, _DirtColorVariation.rgb, variation);
    
    // Surface properties - smooth falloff
    result.roughness = result.amount * _DustRoughness;
    result.normalBlend = result.amount * _DustNormalBlend;
    
    // Effect mask - use smooth curve that still reaches full masking
    // smoothstep gives gradual ramp but reaches 1.0 properly
    half maskBase = result.amount * _DirtFullOpacity;
    result.effectMask = smoothstep(0.0, 0.7, maskBase); // 70% dirt = 100% mask
    
    return result;
}

// Legacy compatibility wrapper
inline void CalculateDust(
    float2 uv, 
    float3 positionWS, 
    float3 normalWS, 
    half NdotV,
    out half dustAmount, 
    out half dustRoughness,
    out half dustNormalFlatten)
{
    float3 positionOS = mul(unity_WorldToObject, float4(positionWS, 1.0)).xyz;
    float3 normalOS = mul((float3x3)unity_WorldToObject, normalWS);
    normalOS = normalize(normalOS);
    DirtResult dirt = CalculateDirt(uv, positionOS, normalOS, NdotV);
    dustAmount = dirt.amount;
    dustRoughness = dirt.roughness;
    dustNormalFlatten = dirt.normalBlend;
}

// Apply dirt wrapper - smooth transition
inline half3 ApplyDust(half3 color, half dustAmount)
{
    half3 dirtColor = _DustColor.rgb;
    half opacity = smoothstep(0.0, 0.7, dustAmount * _DirtFullOpacity);
    return lerp(color, dirtColor, opacity);
}

#endif

#endif
