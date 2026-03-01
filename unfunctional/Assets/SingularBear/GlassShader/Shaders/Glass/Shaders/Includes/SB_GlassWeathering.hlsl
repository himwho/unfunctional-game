// SB_GlassWeathering.hlsl
#ifndef SB_GLASS_WEATHERING_INCLUDED
#define SB_GLASS_WEATHERING_INCLUDED

struct SB_WeatheringResult
{
    half3 overlay;
    half opacity;
    half smoothnessOffset;
    half3 normalOffset;
    half emission;
    half wetness;
};

struct ProceduralCrackData
{
    half crackMask;
    half crackEdge;
    half2 crackNormal;
};

inline ProceduralCrackData SB_CalculateProceduralCracks(float2 uv, float progression, float seed)
{
    ProceduralCrackData result;
    result.crackMask = 0;
    result.crackEdge = 0;
    result.crackNormal = half2(0.5, 0.5);
    
    float2 cellUV = uv * 8.0 + seed;
    float2 cell = floor(cellUV);
    float2 local = frac(cellUV);
    
    float minDist = 1.0;
    float2 closestOffset = float2(0, 0);
    
    for (int y = -1; y <= 1; y++)
    {
        for (int x = -1; x <= 1; x++)
        {
            float2 neighbor = float2(x, y);
            float2 cellId = cell + neighbor;
            float2 randomOffset = frac(sin(float2(dot(cellId, float2(127.1, 311.7)),
                                                   dot(cellId, float2(269.5, 183.3)))) * 43758.5453);
            float2 point = neighbor + randomOffset - local;
            float dist = length(point);
            
            if (dist < minDist)
            {
                minDist = dist;
                closestOffset = point;
            }
        }
    }
    
    float crackWidth = 0.05 + progression * 0.1;
    float edgeDist = abs(minDist - 0.5);
    result.crackMask = saturate(1.0 - edgeDist / crackWidth) * progression;
    result.crackEdge = saturate(1.0 - edgeDist / (crackWidth * 0.3)) * result.crackMask;
    result.crackNormal = half2(0.5 + closestOffset.x * 0.5, 0.5 + closestOffset.y * 0.5);
    
    return result;
}

half4 SB_SampleWeatheringTriplanar(
    TEXTURE2D_PARAM(tex, samp),
    float3 posWS,
    float3 weights,
    float tiling)
{
    half4 texX = SAMPLE_TEXTURE2D(tex, samp, posWS.zy * tiling);
    half4 texY = SAMPLE_TEXTURE2D(tex, samp, posWS.xz * tiling);
    half4 texZ = SAMPLE_TEXTURE2D(tex, samp, posWS.xy * tiling);
    return texX * weights.x + texY * weights.y + texZ * weights.z;
}

void SB_ApplyWeathering(
    inout SB_WeatheringResult result,
    float3 positionWS,
    float3 normalWS,
    float3 triWeights,
    half NdotV)
{
    float weatherScale = 3.0;
    
    // Triplanar noise sampling
    float noiseX = SB_ValueNoise(positionWS.zy * weatherScale);
    float noiseY = SB_ValueNoise(positionWS.xz * weatherScale);
    float noiseZ = SB_ValueNoise(positionWS.xy * weatherScale);
    float weatherNoise = noiseX * triWeights.x + noiseY * triWeights.y + noiseZ * triWeights.z;
    
    // Vertical gradient (more weathering at bottom/top)
    float verticalFactor = saturate(positionWS.y * _WeatheringGradient + 0.5);
    
    // Edge factor (more weathering at glancing angles)
    float edgeFactor = pow(1.0 - NdotV, 2.0) * _WeatheringEdge;
    
    // Combined weathering amount
    float weatherAmount = _WeatheringAmount * (weatherNoise * 0.5 + 0.5) * (verticalFactor + edgeFactor);
    weatherAmount = saturate(weatherAmount);
    
    // Apply results
    result.overlay += _WeatheringColor.rgb * weatherAmount * 0.5;
    result.opacity += weatherAmount * 0.6;
    result.smoothnessOffset += _WeatheringRoughness * weatherAmount;
}


// DIRT & GRIME


void SB_ApplyDirt(
    inout SB_WeatheringResult result,
    float3 positionWS,
    float3 normalWS,
    float3 triWeights,
    half NdotV)
{
    // Sample dirt texture triplanar
    half4 dirtSample = SB_SampleWeatheringTriplanar(
        TEXTURE2D_ARGS(_DirtMap, sampler_DirtMap),
        positionWS,
        triWeights,
        _DirtTiling
    );
    
    // Edge grime (accumulates in crevices)
    float grimeFactor = pow(1.0 - NdotV, 3.0) * _GrimeEdgeAmount;
    
    // Gravity-based grime (accumulates at bottom)
    float gravityGrime = saturate(1.0 - positionWS.y * 0.5) * _GrimeGradient;
    
    // Combined dirt
    float totalDirt = saturate(dirtSample.r * _DirtAmount + grimeFactor + gravityGrime);
    
    result.overlay += _DirtColor.rgb * totalDirt;
    result.opacity += totalDirt * 0.8;
    result.smoothnessOffset += _DirtRoughness * totalDirt;
    
    // Dust on top surfaces
    float dustFactor = saturate(normalWS.y) * _DustAmount;
    result.overlay += _DustColor.rgb * dustFactor * 0.5;
    result.opacity += dustFactor * 0.4;
}


// SMUDGES & FINGERPRINTS


void SB_ApplySmudges(
    inout SB_WeatheringResult result,
    float2 uv)
{
    if (_SmudgeStrength < 0.001) return;
    
    half smudge = SAMPLE_TEXTURE2D(_SmudgeMap, sampler_SmudgeMap, uv * _SmudgeTiling).r;
    smudge *= _SmudgeStrength;
    
    result.smoothnessOffset += smudge * 0.3;
}


// SCRATCHES


void SB_ApplyScratchesa(
    inout SB_WeatheringResult result,
    float2 uv)
{
    if (_ScratchStrength < 0.001) return;
    
    float2 scratchUV = uv * _ScratchTiling;
    half scratch = SAMPLE_TEXTURE2D(_ScratchMap, sampler_ScratchMap, scratchUV).r;
    scratch *= _ScratchStrength;
    
    // Roughness from scratches
    result.smoothnessOffset += scratch * 0.1;
    
    // Generate normal offset from scratch gradient
    float2 scratchGrad;
    scratchGrad.x = SAMPLE_TEXTURE2D(_ScratchMap, sampler_ScratchMap, scratchUV + float2(0.01, 0)).r - scratch;
    scratchGrad.y = SAMPLE_TEXTURE2D(_ScratchMap, sampler_ScratchMap, scratchUV + float2(0, 0.01)).r - scratch;
    
    result.normalOffset += float3(scratchGrad * _ScratchDepth, 0);
}


// RAIN (Animated triplanar)


half3 SB_CalculateRainNormal(
    float3 positionWS,
    float3 normalWS,
    float3 triWeights,
    float3x3 TBN,
    float time)
{
    float rainTiling = _RainTiling * 0.5;
    
    // UV for each axis with vertical scroll on vertical surfaces
    float2 rainUV_X = positionWS.zy * rainTiling;
    rainUV_X.y -= time * _RainSpeed;
    
    float2 rainUV_Y = positionWS.xz * rainTiling;
    // Horizontal surfaces don't scroll
    
    float2 rainUV_Z = positionWS.xy * rainTiling;
    rainUV_Z.y -= time * _RainSpeed;
    
    // Sample rain normals per axis
    half3 rainNormal_X = UnpackNormalScale(
        SAMPLE_TEXTURE2D(_RainNormalMap, sampler_RainNormalMap, rainUV_X),
        _RainStrength
    );
    half3 rainNormal_Y = UnpackNormalScale(
        SAMPLE_TEXTURE2D(_RainNormalMap, sampler_RainNormalMap, rainUV_Y),
        _RainStrength * 0.2 // Less effect on horizontal
    );
    half3 rainNormal_Z = UnpackNormalScale(
        SAMPLE_TEXTURE2D(_RainNormalMap, sampler_RainNormalMap, rainUV_Z),
        _RainStrength
    );
    
    // RAIN DROPLETS (smaller, faster layer)
    if (_RainDroplets > 0.001)
    {
        float2 dropUV_X = positionWS.zy * rainTiling * 2.0;
        dropUV_X.y -= time * _RainSpeed * 0.6;
        
        float2 dropUV_Z = positionWS.xy * rainTiling * 2.0;
        dropUV_Z.y -= time * _RainSpeed * 0.6;
        
        half3 dropNormal_X = UnpackNormalScale(
            SAMPLE_TEXTURE2D(_RainNormalMap, sampler_RainNormalMap, dropUV_X),
            _RainStrength * _RainDroplets
        );
        half3 dropNormal_Z = UnpackNormalScale(
            SAMPLE_TEXTURE2D(_RainNormalMap, sampler_RainNormalMap, dropUV_Z),
            _RainStrength * _RainDroplets
        );
        
        rainNormal_X.xy += dropNormal_X.xy;
        rainNormal_Z.xy += dropNormal_Z.xy;
    }
    
    // RAIN STREAKS (elongated, fast)
    if (_RainStreaks > 0.001)
    {
        float2 streakUV_X = positionWS.zy * float2(rainTiling, rainTiling * 4.0);
        streakUV_X.y -= time * _RainSpeed * 2.0;
        
        float2 streakUV_Z = positionWS.xy * float2(rainTiling, rainTiling * 4.0);
        streakUV_Z.y -= time * _RainSpeed * 2.0;
        
        half3 streakNormal_X = UnpackNormalScale(
            SAMPLE_TEXTURE2D(_RainNormalMap, sampler_RainNormalMap, streakUV_X),
            _RainStrength * _RainStreaks
        );
        half3 streakNormal_Z = UnpackNormalScale(
            SAMPLE_TEXTURE2D(_RainNormalMap, sampler_RainNormalMap, streakUV_Z),
            _RainStrength * _RainStreaks
        );
        
        rainNormal_X.xy += streakNormal_X.xy;
        rainNormal_Z.xy += streakNormal_Z.xy;
    }
    
    // Blend triplanar
    half3 combinedRain = normalize(half3(
        rainNormal_X.xy * triWeights.x + 
        rainNormal_Y.xy * triWeights.y + 
        rainNormal_Z.xy * triWeights.z,
        1.0
    ));
    
    // Transform to world space
    return normalize(mul(combinedRain, TBN));
}


// DAMAGE & CRACKS


void SB_ApplyDamage(
    inout SB_WeatheringResult result,
    float2 uv,
    float2 screenUV,
    inout float2 refractionOffset)
{
    half crackMask = 0.0;
    half crackEmission = 0.0;
    half3 crackNormalOffset = half3(0, 0, 0);
    
    if (_ProceduralCracks > 0.5)
    {
        // Procedural cracks using Voronoi
        ProceduralCrackData cracks = SB_CalculateProceduralCracks(
            uv, 
            _DamageProgression, 
            _ProceduralCrackSeed
        );
        
        crackMask = cracks.crackMask;
        crackEmission = cracks.crackEdge * _CrackEmission;
        crackNormalOffset = float3((cracks.crackNormal.xy - 0.5) * crackMask * _CrackDepth, 0);
    }
    else
    {
        // Texture-based damage mask
        half4 damageSample = SAMPLE_TEXTURE2D(_DamageMask, sampler_DamageMask, uv);
        crackMask = damageSample.r * _DamageProgression;
        crackEmission = crackMask * _CrackEmission;
        
        // Sample crack normal if available
        half3 crackNormal = UnpackNormalScale(
            SAMPLE_TEXTURE2D(_CrackNormalMap, sampler_CrackNormalMap, uv),
            crackMask * _CrackDepth
        );
        crackNormalOffset = crackNormal - half3(0, 0, 1);
    }
    
    // Apply to results
    result.opacity += crackMask * 0.5;
    result.overlay = lerp(result.overlay, _CrackColor.rgb, crackMask * 0.7);
    result.normalOffset += crackNormalOffset;
    result.emission = crackEmission;
    
    // Shatter distortion (offset refraction UV)
    refractionOffset += float2(crackMask * _ShatterDistortion * 0.1, 0);
}


// WETNESS EFFECT


void SB_ApplyWetness(
    inout SB_WeatheringResult result,
    float3 normalWS,
    half wetness)
{
    if (wetness < 0.001) return;
    
    // Wet surfaces are smoother
    result.smoothnessOffset -= wetness * 0.3;
    
    // Darker when wet
    result.overlay *= 1.0 - wetness * 0.2;
    
    result.wetness = wetness;
}


// MAIN WEATHERING FUNCTION


SB_WeatheringResult SB_CalculateWeathering(
    float3 positionWS,
    float3 normalWS,
    float2 uv,
    float2 screenUV,
    float3x3 TBN,
    half NdotV,
    float time,
    inout float2 refractionOffset,
    inout float3 finalNormalWS)
{
    SB_WeatheringResult result;
    result.overlay = half3(0, 0, 0);
    result.opacity = 0.0;
    result.smoothnessOffset = 0.0;
    result.normalOffset = half3(0, 0, 0);
    result.emission = 0.0;
    result.wetness = 0.0;
    
    // Get triplanar weights
    float3 triWeights = SB_GetTriplanarWeightsFast(normalWS);
    
    // WEATHERING
#if defined(_SB_WEATHERING)
    SB_ApplyWeathering(result, positionWS, normalWS, triWeights, NdotV);
#endif

    // DIRT
#if defined(_SB_DIRT)
    SB_ApplyDirt(result, positionWS, normalWS, triWeights, NdotV);
#endif

    // IMPERFECTIONS
#if defined(_SB_IMPERFECTIONS)
    SB_ApplySmudges(result, uv);
    SB_ApplyScratchesa(result, uv);
#endif

    // DAMAGE
#if defined(_SB_DAMAGE)
    SB_ApplyDamage(result, uv, screenUV, refractionOffset);
#endif

    // RAIN
#if defined(_SB_RAIN)
    finalNormalWS = SB_CalculateRainNormal(positionWS, normalWS, triWeights, TBN, time);
    SB_ApplyWetness(result, normalWS, _RainWetness);
#endif

    // Apply normal offset to final normal
    finalNormalWS = normalize(finalNormalWS + result.normalOffset);
    
    return result;
}

#endif // SB_GLASS_WEATHERING_INCLUDED
