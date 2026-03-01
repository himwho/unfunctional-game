// ============================================================================
// SingularBear Glass Shader - AAA Features
// Beer-Lambert, Caustics, TIR, Sparkle, Dust/Fingerprints
// ============================================================================

#ifndef SB_GLASS_AAA_INCLUDED
#define SB_GLASS_AAA_INCLUDED

// ============================================================================
// BEER-LAMBERT ABSORPTION
// ============================================================================
// Physically accurate light absorption through colored glass
// Thicker glass = more light absorbed = darker/more saturated color

#if defined(_SB_ABSORPTION)
inline half3 CalculateAbsorption(half thickness, half NdotV)
{
    // Beer-Lambert law: I = I0 * e^(-density * distance)
    // thickness approximated by 1/NdotV (thicker at grazing angles)
    half pathLength = thickness / max(NdotV, 0.1);
    pathLength = pow(pathLength, _AbsorptionFalloff);
    
    // Per-channel absorption (colored glass absorbs different wavelengths)
    half3 absorption = exp(-_AbsorptionColor.rgb * _AbsorptionDensity * pathLength);
    
    return absorption;
}

// Apply absorption to refracted color
inline half3 ApplyAbsorption(half3 refractionColor, half thickness, half NdotV)
{
    half3 absorption = CalculateAbsorption(thickness, NdotV);
    
    // Lerp between original and absorbed color
    // Also tint towards absorption color for thick areas
    half3 tintedColor = refractionColor * absorption;
    half3 deepColor = lerp(tintedColor, _AbsorptionColor.rgb * 0.5, saturate(1.0 - absorption.g));
    
    return lerp(refractionColor, deepColor, _AbsorptionColor.a);
}
#endif

// ============================================================================
// CAUSTICS (Fake Projected Light Patterns)
// ============================================================================
// Simulates light focusing through curved glass surfaces

#if defined(_SB_CAUSTICS)
// Animated dual-layer caustics
inline half3 SampleCaustics(float3 positionWS, half3 normalWS, float time)
{
    // Project caustics from above (Y-axis)
    float2 causticsUV = positionWS.xz * _CausticsScale;
    
    // Add normal-based distortion for realism
    causticsUV += normalWS.xz * _CausticsDistortion;
    
    // Dual layer animation (different speeds/directions)
    float2 uv1 = causticsUV + float2(time * _CausticsSpeed, time * _CausticsSpeed * 0.7);
    float2 uv2 = causticsUV * 1.3 - float2(time * _CausticsSpeed * 0.8, time * _CausticsSpeed * 0.5);
    
    // Apply texture tiling
    uv1 = uv1 * _CausticsTexture_ST.xy + _CausticsTexture_ST.zw;
    uv2 = uv2 * _CausticsTexture_ST.xy + _CausticsTexture_ST.zw;
    
    // Sample and blend layers
    half caustics1 = SAMPLE_TEXTURE2D(_CausticsTexture, sampler_CausticsTexture, uv1).r;
    half caustics2 = SAMPLE_TEXTURE2D(_CausticsTexture, sampler_CausticsTexture, uv2).r;
    
    // Min blend creates interesting intersection patterns
    half caustics = min(caustics1, caustics2);
    caustics = pow(caustics, 2.0) * 4.0; // Increase contrast
    
    return _CausticsColor.rgb * caustics * _CausticsIntensity;
}

// Procedural caustics (no texture needed)
inline half3 ProceduralCaustics(float3 positionWS, half3 normalWS, float time)
{
    float2 p = positionWS.xz * _CausticsScale + normalWS.xz * _CausticsDistortion;
    
    // Animated Voronoi-like pattern
    float t = time * _CausticsSpeed;
    
    // Layer 1
    float2 p1 = p + float2(t, t * 0.7);
    float2 i1 = floor(p1);
    float2 f1 = frac(p1);
    
    float minDist1 = 1.0;
    for (int y = -1; y <= 1; y++)
    {
        for (int x = -1; x <= 1; x++)
        {
            float2 neighbor = float2(x, y);
            float2 cellPt = SB_Hash2(i1 + neighbor);
            cellPt = 0.5 + 0.5 * sin(t * 2.0 + 6.2831 * cellPt);
            float dist = length(neighbor + cellPt - f1);
            minDist1 = min(minDist1, dist);
        }
    }
    
    // Layer 2 (different scale/speed)
    float2 p2 = p * 1.5 - float2(t * 0.8, t * 0.5);
    float2 i2 = floor(p2);
    float2 f2 = frac(p2);
    
    float minDist2 = 1.0;
    for (int y2 = -1; y2 <= 1; y2++)
    {
        for (int x2 = -1; x2 <= 1; x2++)
        {
            float2 neighbor2 = float2(x2, y2);
            float2 cellPt2 = SB_Hash2(i2 + neighbor2);
            cellPt2 = 0.5 + 0.5 * sin(t * 1.5 + 6.2831 * cellPt2);
            float dist2 = length(neighbor2 + cellPt2 - f2);
            minDist2 = min(minDist2, dist2);
        }
    }
    
    // Combine with min for intersection effect
    half caustics = min(minDist1, minDist2);
    caustics = 1.0 - caustics;
    caustics = pow(saturate(caustics), 3.0) * 2.0;
    
    return _CausticsColor.rgb * caustics * _CausticsIntensity;
}
#endif

// ============================================================================
// TOTAL INTERNAL REFLECTION (TIR)
// ============================================================================
// When light hits glass at a shallow angle, it reflects totally (diamond effect)

#if defined(_SB_TIR)
inline half CalculateTIR(half NdotV, half ior)
{
    // Critical angle = arcsin(1/n) where n is IOR
    // For glass (1.5): critical angle ≈ 41.8 degrees
    // At angles below critical, we get total reflection
    
    half criticalCos = sqrt(1.0 - (1.0 / (ior * ior)));
    criticalCos = lerp(criticalCos, 0.9, _TIRCriticalAngle); // Adjustable
    
    // Sharp transition at critical angle
    half tirFactor = 1.0 - smoothstep(criticalCos - _TIRSharpness, criticalCos, NdotV);
    
    return tirFactor * _TIRIntensity;
}

// Enhanced reflection for TIR areas
inline half3 ApplyTIR(half3 baseColor, half3 reflectionColor, half tirFactor)
{
    // In TIR zones, reflection dominates completely
    return lerp(baseColor, reflectionColor, tirFactor);
}
#endif

// ============================================================================
// SPARKLE / GLITTER
// ============================================================================
// Sparkling points that respond to view angle (crystal, champagne, frost)

#if defined(_SB_SPARKLE)
// High-frequency noise for sparkle positions
inline half SparkleNoise(float3 p)
{
    // 3D hash for sparkle distribution
    float3 i = floor(p);
    float3 f = frac(p);
    
    // Random value per cell
    float n = i.x + i.y * 157.0 + i.z * 113.0;
    return frac(sin(n) * 43758.5453);
}

inline half3 CalculateSparkle(float3 positionWS, half3 normalWS, half3 viewDirWS, 
                               half3 lightDir, float time)
{
    // High-frequency position for many small sparkles
    float3 sparklePos = positionWS * _SparkleDensity;
    
    // Add subtle animation
    sparklePos += time * _SparkleSpeed * 0.1;
    
    // Get sparkle cell
    float3 cellPos = floor(sparklePos);
    float3 cellFrac = frac(sparklePos);
    
    half sparkle = 0.0;
    
    // Check neighboring cells for sparkles
    for (int z = 0; z <= 1; z++)
    {
        for (int y = 0; y <= 1; y++)
        {
            for (int x = 0; x <= 1; x++)
            {
                float3 offset = float3(x, y, z);
                float3 cell = cellPos + offset;
                
                // Random sparkle position within cell
                float3 randOffset = float3(
                    SB_Hash(cell.xy + cell.z),
                    SB_Hash(cell.yz + cell.x * 2.0),
                    SB_Hash(cell.xz + cell.y * 3.0)
                );
                
                float3 sparklePoint = offset + randOffset * 0.8 + 0.1;
                float dist = length(cellFrac - sparklePoint);
                
                // Random normal for this sparkle facet
                float3 sparkleNormal = normalize(randOffset * 2.0 - 1.0 + normalWS);
                
                // Sparkle only visible at correct angle (like tiny mirrors)
                half3 halfVec = normalize(lightDir + viewDirWS);
                half NdotH = saturate(dot(sparkleNormal, halfVec));
                
                // Sharp falloff for point-like sparkles
                half pointSparkle = pow(NdotH, 256.0 / _SparkleSize);
                
                // Only if within sparkle radius
                half inRadius = 1.0 - smoothstep(0.0, _SparkleSize * 0.1, dist);
                
                // Random intensity per sparkle
                half intensity = step(_SparkleThreshold, SparkleNoise(cell * 7.31));
                
                sparkle += pointSparkle * inRadius * intensity;
            }
        }
    }
    
    // Apply contrast and intensity
    sparkle = pow(saturate(sparkle), _SparkleContrast);
    
    return _SparkleColor.rgb * sparkle * _SparkleIntensity;
}
#endif

// ============================================================================
// DUST & FINGERPRINTS
// ============================================================================
// Surface imperfections that add realism

#if defined(_SB_DUST)
inline void SampleDust(float2 uv, out half dustAmount, out half dustRoughness)
{
    float2 dustUV = uv * _DustTexture_ST.xy + _DustTexture_ST.zw;
    half4 dustSample = SAMPLE_TEXTURE2D(_DustTexture, sampler_DustTexture, dustUV);
    
    dustAmount = dustSample.r * _DustIntensity;
    dustRoughness = dustSample.r * _DustRoughness;
}

inline half3 ApplyDust(half3 baseColor, half dustAmount)
{
    // Dust adds a matte layer on top
    return lerp(baseColor, baseColor * _DustColor.rgb, dustAmount);
}

inline half ApplyDustToRoughness(half baseRoughness, half dustRoughness)
{
    // Dust increases roughness (less shiny)
    return saturate(baseRoughness + dustRoughness);
}
#endif

// ============================================================================
// PROCEDURAL DUST (No texture needed)
// ============================================================================

#if defined(_SB_DUST)
inline half ProceduralDust(float2 uv, float scale)
{
    // Multi-octave noise for natural dust distribution
    float2 p = uv * scale;
    
    half dust = 0.0;
    half amplitude = 0.5;
    
    for (int i = 0; i < 3; i++)
    {
        dust += SB_GradientNoise(p) * amplitude;
        p *= 2.17;
        amplitude *= 0.5;
    }
    
    // Threshold for dust particles
    dust = smoothstep(0.4, 0.6, dust);
    
    return dust;
}
#endif

// ============================================================================
// COMBINED AAA SURFACE FUNCTION
// ============================================================================

struct SB_AAASurface
{
    half3 absorption;
    half3 caustics;
    half tirFactor;
    half3 sparkle;
    half dustAmount;
    half dustRoughness;
};

inline SB_AAASurface InitializeAAASurface()
{
    SB_AAASurface aaa = (SB_AAASurface)0;
    aaa.absorption = half3(1, 1, 1);
    aaa.tirFactor = 0.0;
    return aaa;
}

inline SB_AAASurface CalculateAAASurface(
    float3 positionWS,
    half3 normalWS,
    half3 viewDirWS,
    half3 lightDir,
    half NdotV,
    half thickness,
    float2 uv,
    float time)
{
    SB_AAASurface aaa = InitializeAAASurface();
    
    // Beer-Lambert Absorption
#if defined(_SB_ABSORPTION)
    aaa.absorption = CalculateAbsorption(thickness, NdotV);
#endif
    
    // Caustics
#if defined(_SB_CAUSTICS)
    #if defined(_SB_CAUSTICS_PROCEDURAL)
        aaa.caustics = ProceduralCaustics(positionWS, normalWS, time);
    #else
        aaa.caustics = SampleCaustics(positionWS, normalWS, time);
    #endif
#endif
    
    // Total Internal Reflection
#if defined(_SB_TIR)
    half ior = _IndexOfRefraction + 1.0;
    aaa.tirFactor = CalculateTIR(NdotV, ior);
#endif
    
    // Sparkle
#if defined(_SB_SPARKLE)
    aaa.sparkle = CalculateSparkle(positionWS, normalWS, viewDirWS, lightDir, time);
#endif
    
    // Dust
#if defined(_SB_DUST)
    SampleDust(uv, aaa.dustAmount, aaa.dustRoughness);
#endif
    
    // Note: Fingerprints are now handled by SB_GlassFingerprints.hlsl
    // and processed directly in MixFinalColor for advanced multi-slot support
    
    return aaa;
}

#endif // SB_GLASS_AAA_INCLUDED
