// SB_GlassFingerprints.hlsl
// Simplified texture-based fingerprint system
#ifndef SB_GLASS_FINGERPRINTS_INCLUDED
#define SB_GLASS_FINGERPRINTS_INCLUDED

// ============================================================
// FINGERPRINT RESULT STRUCTURE
// ============================================================

struct FingerprintResult
{
    half totalAmount;
    half totalRoughness;
    half3 colorTint;
    half3 normalPerturbation;
};

// ============================================================
// UV TRANSFORMS
// ============================================================

// Transform UV for decal-style placement
inline float2 TransformFingerprintUV(float2 uv, float2 position, float rotation, float2 scale)
{
    // Translate to position
    float2 localUV = uv - position;
    
    // Rotate (rotation in radians)
    float s = sin(rotation);
    float c = cos(rotation);
    float2 rotatedUV = float2(
        localUV.x * c - localUV.y * s,
        localUV.x * s + localUV.y * c
    );
    
    // Scale and center
    float2 scaledUV = rotatedUV / max(scale, 0.001) + 0.5;
    
    return scaledUV;
}

// Ellipse mask for natural fingerprint shape
inline half GetEllipseMask(float2 localUV, float falloff)
{
    float2 centered = (localUV - 0.5) * 2.0;
    // Slightly elongated for finger shape
    centered.y *= 0.75;
    float dist = length(centered);
    
    return 1.0 - smoothstep(1.0 - falloff, 1.0 + falloff * 0.2, dist);
}

// ============================================================
// WORLD POSITION MODE
// ============================================================

// Calculate mask based on world position distance
inline half GetWorldPositionMask(float3 pixelWorldPos, float3 fingerprintWorldPos, float radius, float falloff)
{
    float dist = distance(pixelWorldPos, fingerprintWorldPos);
    float normalizedDist = dist / max(radius, 0.001);
    return 1.0 - smoothstep(1.0 - falloff, 1.0 + falloff * 0.3, normalizedDist);
}

// Get local UV for world position mode (projects onto surface)
inline float2 GetWorldPositionLocalUV(float3 pixelWorldPos, float3 fingerprintWorldPos, float3 normalWS, float radius, float rotation)
{
    // Create tangent frame from normal
    float3 up = abs(normalWS.y) < 0.999 ? float3(0, 1, 0) : float3(1, 0, 0);
    float3 tangent = normalize(cross(up, normalWS));
    float3 bitangent = cross(normalWS, tangent);
    
    // Project world offset onto tangent plane
    float3 offset = pixelWorldPos - fingerprintWorldPos;
    float2 planarOffset = float2(dot(offset, tangent), dot(offset, bitangent));
    
    // Normalize by radius and center
    float2 localUV = (planarOffset / max(radius, 0.001)) * 0.5 + 0.5;
    
    // Apply rotation
    float s = sin(rotation);
    float c = cos(rotation);
    float2 centered = localUV - 0.5;
    localUV = float2(
        centered.x * c - centered.y * s,
        centered.x * s + centered.y * c
    ) + 0.5;
    
    return localUV;
}

// ============================================================
// TRIPLANAR SAMPLING
// ============================================================

// Get triplanar blend weights
inline half3 GetTriplanarBlendWeights(float3 normalWS, float sharpness)
{
    half3 blend = abs(normalWS);
    blend = pow(blend, sharpness);
    blend /= (blend.x + blend.y + blend.z + 0.0001);
    return blend;
}

// Sample fingerprint with triplanar projection in a world-space zone
inline half SampleFingerprintTriplanar(
    TEXTURE2D_PARAM(tex, samp),
    float4 texST,
    float3 positionWS,
    float3 normalWS,
    float3 worldCenter,
    float worldRadius,
    float scale,
    float rotation,
    float falloff)
{
    // First check distance from center
    float dist = distance(positionWS, worldCenter);
    float normalizedDist = dist / max(worldRadius, 0.001);
    half mask = 1.0 - smoothstep(1.0 - falloff, 1.0 + falloff * 0.3, normalizedDist);
    
    if (mask < 0.001) return 0.0;
    
    float rotRad = rotation;
    float s = sin(rotRad);
    float c = cos(rotRad);
    
    // Get blend weights
    half3 blend = GetTriplanarBlendWeights(normalWS, 4.0);
    
    // Calculate UVs relative to world center, scaled by triplanar scale
    float3 relPos = (positionWS - worldCenter) * scale;
    
    float2 uvX = relPos.zy;
    float2 uvY = relPos.xz;
    float2 uvZ = relPos.xy;
    
    // Apply rotation to each UV
    float2 uvXrot = float2(uvX.x * c - uvX.y * s, uvX.x * s + uvX.y * c) + 0.5;
    float2 uvYrot = float2(uvY.x * c - uvY.y * s, uvY.x * s + uvY.y * c) + 0.5;
    float2 uvZrot = float2(uvZ.x * c - uvZ.y * s, uvZ.x * s + uvZ.y * c) + 0.5;
    
    // Sample each projection (with bounds check, then apply texture ST)
    half sampleX = (uvXrot.x >= 0 && uvXrot.x <= 1 && uvXrot.y >= 0 && uvXrot.y <= 1) 
                   ? SAMPLE_TEXTURE2D(tex, samp, uvXrot * texST.xy + texST.zw).r : 0.0;
    half sampleY = (uvYrot.x >= 0 && uvYrot.x <= 1 && uvYrot.y >= 0 && uvYrot.y <= 1) 
                   ? SAMPLE_TEXTURE2D(tex, samp, uvYrot * texST.xy + texST.zw).r : 0.0;
    half sampleZ = (uvZrot.x >= 0 && uvZrot.x <= 1 && uvZrot.y >= 0 && uvZrot.y <= 1) 
                   ? SAMPLE_TEXTURE2D(tex, samp, uvZrot * texST.xy + texST.zw).r : 0.0;
    
    // Blend samples
    half triSample = sampleX * blend.x + sampleY * blend.y + sampleZ * blend.z;
    
    return triSample * mask;
}

// ============================================================
// TEXTURE SAMPLING
// ============================================================

// Sample fingerprint texture - R channel = intensity
inline half SampleFingerprintTexture(TEXTURE2D_PARAM(tex, samp), float2 uv)
{
    // Check bounds
    if (uv.x < 0.0 || uv.x > 1.0 || uv.y < 0.0 || uv.y > 1.0)
        return 0.0;
    
    return SAMPLE_TEXTURE2D(tex, samp, uv).r;
}

// ============================================================
// SINGLE SLOT CALCULATION
// ============================================================

// mappingMode: 0 = UV, 1 = World, 2 = Triplanar
inline half CalculateFingerprintSlot(
    TEXTURE2D_PARAM(tex, samp),
    float4 texST,
    float2 uv,
    float3 positionWS,
    float3 normalWS,
    float2 uvPosition,
    float2 uvScale,
    float3 worldPosition,
    float worldRadius,
    float triplanarScale,
    float rotation,
    float intensity,
    float falloff,
    int mappingMode)
{
    half mask = 0.0;
    float2 texUV = float2(0.5, 0.5);
    float rotRad = rotation * 0.0174533; // degrees to radians
    
    // Triplanar Mode - uses world position + radius with triplanar sampling
    if (mappingMode == 2)
    {
        half triSample = SampleFingerprintTriplanar(
            TEXTURE2D_ARGS(tex, samp),
            texST,
            positionWS,
            normalWS,
            worldPosition,
            worldRadius,
            triplanarScale,
            rotRad,
            falloff
        );
        return triSample * intensity;
    }
    // World Position Mode
    else if (mappingMode == 1)
    {
        mask = GetWorldPositionMask(positionWS, worldPosition, worldRadius, falloff);
        if (mask < 0.001) return 0.0;
        
        texUV = GetWorldPositionLocalUV(positionWS, worldPosition, normalWS, worldRadius, rotRad);
    }
    // UV Mode (default)
    else
    {
        float2 localUV = TransformFingerprintUV(uv, uvPosition, rotRad, uvScale);
        mask = GetEllipseMask(localUV, falloff);
        if (mask < 0.001) return 0.0;
        
        texUV = localUV;
    }
    
    // Apply texture tiling/offset within fingerprint space
    texUV = texUV * texST.xy + texST.zw;
    
    // Sample texture
    half texSample = SampleFingerprintTexture(TEXTURE2D_ARGS(tex, samp), texUV);
    
    return texSample * mask * intensity;
}

// ============================================================
// MAIN CALCULATION FUNCTION
// ============================================================

inline FingerprintResult CalculateAdvancedFingerprints(
    float2 uv,
    float3 positionWS,
    float3 normalWS,
    float3 tangentWS,
    float3 bitangentWS)
{
    FingerprintResult result = (FingerprintResult)0;
    result.colorTint = half3(1, 1, 1);
    result.normalPerturbation = half3(0, 0, 0);
    
#if defined(_SB_FINGERPRINTS)
    
    half totalAmount = 0.0;
    half totalRoughness = 0.0;
    
    // ========== SLOT 1 ==========
    {
        // Transform local position to world position (follows object movement)
        float3 fpWorldPos = mul(unity_ObjectToWorld, float4(_FingerprintWorldPos1.xyz, 1.0)).xyz;
        
        half amount = CalculateFingerprintSlot(
            TEXTURE2D_ARGS(_FingerprintTexture1, sampler_FingerprintTexture1),
            _FingerprintTexture1_ST,
            uv, positionWS, normalWS,
            _FingerprintPos1.xy,
            _FingerprintScale1.xy,
            fpWorldPos,
            _FingerprintWorldRadius1,
            _FingerprintTriplanarScale1,
            _FingerprintRotation1,
            _FingerprintIntensity1,
            _FingerprintFalloff1,
            (int)_FingerprintMapping1
        );
        totalAmount += amount;
        totalRoughness += amount * _FingerprintRoughness1;
    }
    
    // ========== SLOT 2 ==========
    #if defined(_SB_FINGERPRINTS_SLOT2)
    {
        float3 fpWorldPos = mul(unity_ObjectToWorld, float4(_FingerprintWorldPos2.xyz, 1.0)).xyz;
        
        half amount = CalculateFingerprintSlot(
            TEXTURE2D_ARGS(_FingerprintTexture2, sampler_FingerprintTexture2),
            _FingerprintTexture2_ST,
            uv, positionWS, normalWS,
            _FingerprintPos2.xy,
            _FingerprintScale2.xy,
            fpWorldPos,
            _FingerprintWorldRadius2,
            _FingerprintTriplanarScale2,
            _FingerprintRotation2,
            _FingerprintIntensity2,
            _FingerprintFalloff2,
            (int)_FingerprintMapping2
        );
        totalAmount += amount;
        totalRoughness += amount * _FingerprintRoughness2;
    }
    #endif
    
    // ========== SLOT 3 ==========
    #if defined(_SB_FINGERPRINTS_SLOT3)
    {
        float3 fpWorldPos = mul(unity_ObjectToWorld, float4(_FingerprintWorldPos3.xyz, 1.0)).xyz;
        
        half amount = CalculateFingerprintSlot(
            TEXTURE2D_ARGS(_FingerprintTexture3, sampler_FingerprintTexture3),
            _FingerprintTexture3_ST,
            uv, positionWS, normalWS,
            _FingerprintPos3.xy,
            _FingerprintScale3.xy,
            fpWorldPos,
            _FingerprintWorldRadius3,
            _FingerprintTriplanarScale3,
            _FingerprintRotation3,
            _FingerprintIntensity3,
            _FingerprintFalloff3,
            (int)_FingerprintMapping3
        );
        totalAmount += amount;
        totalRoughness += amount * _FingerprintRoughness3;
    }
    #endif
    
    // ========== SLOT 4 ==========
    #if defined(_SB_FINGERPRINTS_SLOT4)
    {
        float3 fpWorldPos = mul(unity_ObjectToWorld, float4(_FingerprintWorldPos4.xyz, 1.0)).xyz;
        
        half amount = CalculateFingerprintSlot(
            TEXTURE2D_ARGS(_FingerprintTexture4, sampler_FingerprintTexture4),
            _FingerprintTexture4_ST,
            uv, positionWS, normalWS,
            _FingerprintPos4.xy,
            _FingerprintScale4.xy,
            fpWorldPos,
            _FingerprintWorldRadius4,
            _FingerprintTriplanarScale4,
            _FingerprintRotation4,
            _FingerprintIntensity4,
            _FingerprintFalloff4,
            (int)_FingerprintMapping4
        );
        totalAmount += amount;
        totalRoughness += amount * _FingerprintRoughness4;
    }
    #endif
    
    // Clamp and apply tint
    result.totalAmount = saturate(totalAmount);
    result.totalRoughness = saturate(totalRoughness);
    result.colorTint = lerp(half3(1, 1, 1), _FingerprintTint.rgb, result.totalAmount * _FingerprintTint.a);
    
#endif // _SB_FINGERPRINTS
    
    return result;
}

// ============================================================
// APPLY TO SURFACE
// ============================================================

inline void ApplyFingerprintsToSurface(
    inout half3 albedo,
    inout half roughness,
    inout half3 normalWS,
    FingerprintResult fp)
{
#if defined(_SB_FINGERPRINTS)
    // Tint albedo
    albedo *= fp.colorTint;
    
    // Add roughness (fingerprints make surface slightly rougher)
    roughness = saturate(roughness + fp.totalRoughness);
#endif
}

#endif // SB_GLASS_FINGERPRINTS_INCLUDED
