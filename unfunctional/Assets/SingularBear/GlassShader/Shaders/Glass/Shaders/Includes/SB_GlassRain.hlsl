#ifndef SB_GLASS_RAIN_INCLUDED
#define SB_GLASS_RAIN_INCLUDED

// ============================================
// SINGULARBEAR GLASS - RAIN EFFECT
// ============================================
// Procedural rain droplets
// - Animated droplets running down
// - Static droplets
// - Wet surface effect
// - UV or Triplanar projection mode
// ============================================

// ============================================
// NOISE FUNCTIONS
// ============================================

float2 SB_Rain_Hash22(float2 p)
{
    float3 p3 = frac(float3(p.xyx) * float3(0.1031, 0.1030, 0.0973));
    p3 += dot(p3, p3.yzx + 33.33);
    return frac((p3.xx + p3.yz) * p3.zy);
}

float SB_Rain_Hash12(float2 p)
{
    float3 p3 = frac(float3(p.xyx) * 0.1031);
    p3 += dot(p3, p3.yzx + 33.33);
    return frac((p3.x + p3.y) * p3.z);
}

// ============================================
// TRIPLANAR UTILITIES
// ============================================

half3 SB_Rain_TriplanarWeights(half3 worldNormal, half sharpness)
{
    half3 weights = abs(worldNormal);
    weights = pow(weights, sharpness);
    weights /= (weights.x + weights.y + weights.z + 0.0001);
    return weights;
}

// ============================================
// LAYER 1: STATIC DROPLETS
// ============================================

float4 SB_StaticRainDrops(float2 uv, float scale, float size)
{
    float2 scaledUV = uv * scale;
    float2 cellID = floor(scaledUV);
    float2 cellUV = frac(scaledUV) - 0.5;
    
    float2 totalPerturbation = float2(0, 0);
    float mask = 0;
    
    for (int y = -1; y <= 1; y++)
    {
        for (int x = -1; x <= 1; x++)
        {
            float2 neighborID = cellID + float2(x, y);
            float2 neighborUV = cellUV - float2(x, y);
            
            float2 randOffset = SB_Rain_Hash22(neighborID) - 0.5;
            randOffset *= 0.6;
            
            float2 dropUV = neighborUV - randOffset;
            
            float2 stretchedUV = dropUV;
            stretchedUV.y *= 0.7;
            float dist = length(stretchedUV);
            
            float dropSize = size * (0.5 + SB_Rain_Hash12(neighborID * 7.3) * 0.5);
            
            float drop = 1.0 - smoothstep(0.0, dropSize, dist);
            drop = drop * drop;
            
            float presence = step(0.4, SB_Rain_Hash12(neighborID * 3.7));
            drop *= presence;
            
            if (drop > 0.01)
            {
                float2 gradient = stretchedUV / max(dist, 0.001);
                totalPerturbation += gradient * drop;
                mask = max(mask, drop);
            }
        }
    }
    
    return float4(totalPerturbation, 0, mask);
}

// ============================================
// LAYER 2: DRIPPING DROPS
// ============================================

float4 SB_DrippingRain(float2 uv, float time, float columns, float speed, float trailLength)
{
    float2 totalPerturbation = float2(0, 0);
    float mask = 0;
    
    float columnID = floor(uv.x * columns);
    float columnUV = frac(uv.x * columns);
    
    for (int c = -1; c <= 1; c++)
    {
        float currentColumn = columnID + c;
        float localX = columnUV - c - 0.5;
        
        float xOffset = (SB_Rain_Hash12(float2(currentColumn, 0)) - 0.5) * 0.6;
        localX -= xOffset;
        
        for (int i = 0; i < 3; i++)
        {
            float dropSeed = currentColumn * 13.7 + i * 41.3;
            
            float dropSpeed = speed * (0.5 + SB_Rain_Hash12(float2(dropSeed, 1)) * 1.0);
            float dropPhase = SB_Rain_Hash12(float2(dropSeed, 2));
            float dropPresence = step(0.3, SB_Rain_Hash12(float2(dropSeed, 3)));
            
            float t = frac(time * dropSpeed * 0.15 + dropPhase);
            float dropY = 1.0 - t;
            
            float wobble = sin(t * 15.0 + dropSeed) * 0.02 * (1.0 - t * 0.5);
            
            float2 dropUV = float2(localX - wobble, uv.y - dropY);
            
            float headDist = length(dropUV * float2(1.0, 1.5)) * 15.0;
            float head = 1.0 - saturate(headDist);
            head = head * head;
            
            float trail = 0;
            if (dropUV.y > 0 && dropUV.y < trailLength * 0.3)
            {
                float trailWidth = 0.02 * (1.0 - dropUV.y / (trailLength * 0.3));
                trail = 1.0 - saturate(abs(localX - wobble) / trailWidth);
                trail *= 1.0 - dropUV.y / (trailLength * 0.3);
                trail = trail * trail * 0.5;
            }
            
            float drop = max(head, trail) * dropPresence;
            
            drop *= smoothstep(0.95, 0.8, dropY);
            drop *= smoothstep(0.0, 0.15, dropY);
            
            if (drop > 0.01)
            {
                float2 gradient = dropUV * drop;
                totalPerturbation += gradient;
                mask = max(mask, drop);
            }
        }
    }
    
    return float4(totalPerturbation, 0, mask);
}

// ============================================
// RAIN LAYER (Internal - Single Plane)
// ============================================

void SB_CalculateRainLayer_Internal(
    float2 uv,
    float time,
    half normalStrength,
    half speed,
    out half2 outPerturbation,
    out half outMask
)
{
    outPerturbation = half2(0, 0);
    outMask = 0;
    
    half2 totalPerturbation = half2(0, 0);
    half combinedMask = 0;
    
    // Static droplets - density ~8, size ~0.08
    float4 staticDrops = SB_StaticRainDrops(uv, 8.0, 0.08);
    totalPerturbation += staticDrops.xy * normalStrength;
    combinedMask = max(combinedMask, staticDrops.w);
    
    // Second static layer (smaller, denser)
    float4 staticDrops2 = SB_StaticRainDrops(uv + 0.37, 14.0, 0.05);
    totalPerturbation += staticDrops2.xy * normalStrength * 0.7;
    combinedMask = max(combinedMask, staticDrops2.w * 0.7);
    
    // Dripping drops - columns ~8, trail ~0.5
    float4 drips = SB_DrippingRain(uv, time, 8.0, speed, 0.5);
    totalPerturbation += drips.xy * normalStrength * 1.5;
    combinedMask = max(combinedMask, drips.w);
    
    outPerturbation = totalPerturbation;
    outMask = combinedMask;
}

// ============================================
// MAIN RAIN FUNCTION (UV MODE)
// Uses existing shader properties:
// - intensity, tiling, offset, speed, normalStrength, wetness
// ============================================

void SB_CalculateRainUV(
    float2 uv,
    float time,
    half intensity,
    half2 tiling,
    half2 offset,
    half2 speed,
    half normalStrength,
    half wetness,
    out half3 outNormal,
    out half outMask,
    out half outWetness
)
{
    outNormal = half3(0, 0, 1);
    outMask = 0;
    outWetness = 0;
    
    if (intensity < 0.001) return;
    
    // Apply tiling, offset and animation
    float2 rainUV = uv * tiling + offset;
    rainUV += time * speed;
    
    half2 perturbation;
    half mask;
    SB_CalculateRainLayer_Internal(rainUV, time, normalStrength, speed.y * 10.0, perturbation, mask);
    
    outNormal = half3(perturbation * intensity, 1.0);
    outMask = mask * intensity;
    outWetness = saturate(wetness + outMask * 0.5) * intensity;
}

// ============================================
// MAIN RAIN FUNCTION (TRIPLANAR MODE)
// Uses existing shader properties + triplanar scale/sharpness
// ============================================

void SB_CalculateRainTriplanar(
    float3 worldPos,
    half3 worldNormal,
    float time,
    half intensity,
    half2 speed,
    half normalStrength,
    half wetness,
    half triplanarScale,
    half triplanarSharpness,
    out half3 outNormal,
    out half outMask,
    out half outWetness
)
{
    outNormal = half3(0, 0, 1);
    outMask = 0;
    outWetness = 0;
    
    if (intensity < 0.001) return;
    
    // Triplanar blend weights
    half3 blendWeights = SB_Rain_TriplanarWeights(worldNormal, triplanarSharpness);
    
    // Scale world position
    float3 scaledPos = worldPos * triplanarScale;
    
    // UVs for each projection plane
    float2 uvX = scaledPos.zy; // X-axis: YZ plane
    float2 uvY = scaledPos.xz; // Y-axis: XZ plane (horizontal)
    float2 uvZ = scaledPos.xy; // Z-axis: XY plane
    
    half2 perturbX, perturbY, perturbZ;
    half maskX, maskY, maskZ;
    
    // X-axis projection (side walls facing X) - full rain with drips
    SB_CalculateRainLayer_Internal(uvX + float2(0, time * speed.y), time, normalStrength, speed.y * 10.0, perturbX, maskX);
    
    // Y-axis projection (horizontal surfaces) - static drops only, no drips
    float4 staticY = SB_StaticRainDrops(uvY, 8.0, 0.08);
    float4 staticY2 = SB_StaticRainDrops(uvY + 0.37, 14.0, 0.05);
    perturbY = (staticY.xy + staticY2.xy * 0.7) * normalStrength;
    maskY = max(staticY.w, staticY2.w * 0.7);
    
    // Z-axis projection (side walls facing Z) - full rain with drips
    SB_CalculateRainLayer_Internal(uvZ + float2(0, time * speed.y), time, normalStrength, speed.y * 10.0, perturbZ, maskZ);
    
    // Blend perturbations to world-space offsets
    half3 normalOffset = half3(0, 0, 0);
    
    // X projection: perturbation is in ZY space
    normalOffset += half3(0, perturbX.y, perturbX.x) * blendWeights.x;
    
    // Y projection: perturbation is in XZ space
    normalOffset += half3(perturbY.x, 0, perturbY.y) * blendWeights.y;
    
    // Z projection: perturbation is in XY space
    normalOffset += half3(perturbZ.x, perturbZ.y, 0) * blendWeights.z;
    
    // Blend masks
    half combinedMask = maskX * blendWeights.x + maskY * blendWeights.y + maskZ * blendWeights.z;
    
    // Output tangent-space style normal
    outNormal.xy = normalOffset.xz * intensity;
    outNormal.z = 1.0;
    
    outMask = combinedMask * intensity;
    outWetness = saturate(wetness + outMask * 0.5) * intensity;
}

// ============================================
// SIMPLE VERSION (UV MODE) - Backward compatible
// ============================================

half3 SB_RainNormalSimple(float2 uv, float time, half intensity, half scale, half speed)
{
    if (intensity < 0.001) return half3(0, 0, 1);
    
    float2 animUV = uv * scale + float2(0, time * speed);
    
    float4 drops = SB_StaticRainDrops(animUV, 8.0, 0.08);
    
    float2 animUV2 = uv * scale * 0.7 + float2(0, time * speed * 0.6);
    float4 drops2 = SB_StaticRainDrops(animUV2 + 0.5, 12.0, 0.05);
    
    half3 normal = half3(0, 0, 1);
    normal.xy = (drops.xy * drops.w + drops2.xy * drops2.w * 0.5) * intensity * 2.0;
    normal.z = 1.0;
    
    return normalize(normal);
}

// ============================================
// SIMPLE VERSION (TRIPLANAR MODE)
// ============================================

half3 SB_RainNormalSimpleTriplanar(
    float3 worldPos,
    half3 worldNormal,
    float time,
    half intensity,
    half scale,
    half speed,
    half sharpness
)
{
    if (intensity < 0.001) return half3(0, 0, 1);
    
    half3 blendWeights = SB_Rain_TriplanarWeights(worldNormal, sharpness);
    
    float3 scaledPos = worldPos * scale;
    
    float2 uvX = scaledPos.zy + float2(0, time * speed);
    float2 uvY = scaledPos.xz;
    float2 uvZ = scaledPos.xy + float2(0, time * speed);
    
    float4 dropsX = SB_StaticRainDrops(uvX, 8.0, 0.08);
    float4 dropsX2 = SB_StaticRainDrops(uvX * 0.7 + 0.5, 12.0, 0.05);
    
    float4 dropsY = SB_StaticRainDrops(uvY, 8.0, 0.08);
    float4 dropsY2 = SB_StaticRainDrops(uvY * 0.7 + 0.5, 12.0, 0.05);
    
    float4 dropsZ = SB_StaticRainDrops(uvZ, 8.0, 0.08);
    float4 dropsZ2 = SB_StaticRainDrops(uvZ * 0.7 + 0.5, 12.0, 0.05);
    
    half2 perturbX = (dropsX.xy * dropsX.w + dropsX2.xy * dropsX2.w * 0.5) * 2.0;
    half2 perturbY = (dropsY.xy * dropsY.w + dropsY2.xy * dropsY2.w * 0.5) * 2.0;
    half2 perturbZ = (dropsZ.xy * dropsZ.w + dropsZ2.xy * dropsZ2.w * 0.5) * 2.0;
    
    half3 normalOffset = half3(0, 0, 0);
    normalOffset += half3(0, perturbX.y, perturbX.x) * blendWeights.x;
    normalOffset += half3(perturbY.x, 0, perturbY.y) * blendWeights.y;
    normalOffset += half3(perturbZ.x, perturbZ.y, 0) * blendWeights.z;
    
    half3 normal = half3(normalOffset.xz * intensity, 1.0);
    return normalize(normal);
}

#endif // SB_GLASS_RAIN_INCLUDED
