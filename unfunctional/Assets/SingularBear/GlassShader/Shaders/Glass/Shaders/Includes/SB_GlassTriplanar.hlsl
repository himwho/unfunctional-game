// SB_GlassTriplanar.hlsl
#ifndef SB_GLASS_TRIPLANAR_INCLUDED
#define SB_GLASS_TRIPLANAR_INCLUDED


// TRIPLANAR DATA STRUCTURE


struct SB_TriplanarData
{
    float2 uvFront;     // XY plane (Z-facing)
    float2 uvSide;      // ZY plane (X-facing)
    float2 uvTop;       // XZ plane (Y-facing)
    float3 weights;     // Blend weights per axis
    float3 signs;       // Sign of each axis for normal correction
};


// TRIPLANAR WEIGHTS CALCULATION


// Standard triplanar weights with sharpness control
float3 SB_GetTriplanarWeights(float3 normalWS, float sharpness)
{
    float3 weights = abs(normalWS);
    weights = pow(weights, sharpness);
    weights /= (weights.x + weights.y + weights.z + 0.0001);
    return weights;
}

// Fast triplanar weights (sharpness 4.0 baked in for mobile)
float3 SB_GetTriplanarWeightsFast(float3 normalWS)
{
    float3 weights = abs(normalWS);
    weights *= weights; // pow 2
    weights *= weights; // pow 4
    weights /= (weights.x + weights.y + weights.z + 0.0001);
    return weights;
}


// TRIPLANAR DATA CALCULATION


// Standard triplanar setup
SB_TriplanarData SB_CalculateTriplanar(
    float3 positionWS,
    float3 normalWS,
    float tiling,
    float sharpness)
{
    SB_TriplanarData data;
    
    // Calculate blend weights
    data.weights = SB_GetTriplanarWeights(normalWS, sharpness);
    
    // Store signs for normal mapping
    data.signs = sign(normalWS);
    
    // Calculate UVs for each plane
    // Front (Z-facing): uses XY
    data.uvFront = positionWS.xy * tiling;
    // Side (X-facing): uses ZY
    data.uvSide = positionWS.zy * tiling;
    // Top (Y-facing): uses XZ
    data.uvTop = positionWS.xz * tiling;
    
    return data;
}

// Fast triplanar setup (mobile optimized)
SB_TriplanarData SB_CalculateTriplanarFast(
    float3 positionWS,
    float3 normalWS,
    float tiling)
{
    SB_TriplanarData data;
    
    data.weights = SB_GetTriplanarWeightsFast(normalWS);
    data.signs = sign(normalWS);
    
    data.uvFront = positionWS.xy * tiling;
    data.uvSide = positionWS.zy * tiling;
    data.uvTop = positionWS.xz * tiling;
    
    return data;
}

// Triplanar with offset (for animation)
SB_TriplanarData SB_CalculateTriplanarOffset(
    float3 positionWS,
    float3 normalWS,
    float tiling,
    float2 offset,
    float sharpness)
{
    SB_TriplanarData data;
    
    data.weights = SB_GetTriplanarWeights(normalWS, sharpness);
    data.signs = sign(normalWS);
    
    data.uvFront = positionWS.xy * tiling + offset;
    data.uvSide = positionWS.zy * tiling + offset;
    data.uvTop = positionWS.xz * tiling + offset;
    
    return data;
}


// TRIPLANAR TEXTURE SAMPLING


// Sample color texture with triplanar
half4 SB_SampleTriplanar(
    TEXTURE2D_PARAM(tex, samp),
    SB_TriplanarData data)
{
    half4 texFront = SAMPLE_TEXTURE2D(tex, samp, data.uvFront);
    half4 texSide = SAMPLE_TEXTURE2D(tex, samp, data.uvSide);
    half4 texTop = SAMPLE_TEXTURE2D(tex, samp, data.uvTop);
    
    return texFront * data.weights.z +
           texSide * data.weights.x +
           texTop * data.weights.y;
}

// Sample with LOD control
half4 SB_SampleTriplanarLOD(
    TEXTURE2D_PARAM(tex, samp),
    SB_TriplanarData data,
    float lod)
{
    half4 texFront = SAMPLE_TEXTURE2D_LOD(tex, samp, data.uvFront, lod);
    half4 texSide = SAMPLE_TEXTURE2D_LOD(tex, samp, data.uvSide, lod);
    half4 texTop = SAMPLE_TEXTURE2D_LOD(tex, samp, data.uvTop, lod);
    
    return texFront * data.weights.z +
           texSide * data.weights.x +
           texTop * data.weights.y;
}


// TRIPLANAR NORMAL MAPPING


// Triplanar normal with Whiteout blending
half3 SB_SampleTriplanarNormal(
    TEXTURE2D_PARAM(normalTex, samp),
    SB_TriplanarData data,
    float3 normalWS,
    half strength)
{
    // Sample normals for each plane
    half4 nFront = SAMPLE_TEXTURE2D(normalTex, samp, data.uvFront);
    half4 nSide = SAMPLE_TEXTURE2D(normalTex, samp, data.uvSide);
    half4 nTop = SAMPLE_TEXTURE2D(normalTex, samp, data.uvTop);
    
    // Unpack with strength
    half3 tnormalZ = UnpackNormalScale(nFront, strength);
    half3 tnormalX = UnpackNormalScale(nSide, strength);
    half3 tnormalY = UnpackNormalScale(nTop, strength);
    
    // Flip normals based on surface direction
    tnormalZ.xy *= data.signs.z;
    tnormalX.xy *= data.signs.x;
    tnormalY.xy *= data.signs.y;
    
    // UDN (Unreal Derivative Normal) blending - better than whiteout for triplanar
    // Swizzle tangent normals to align with world axes and blend
    half3 absNormal = abs(normalWS);
    
    // X-axis (side)
    half3 axisX = half3(tnormalX.zy + half2(normalWS.z, normalWS.y), absNormal.x);
    // Y-axis (top)
    half3 axisY = half3(tnormalY.xz + half2(normalWS.x, normalWS.z), absNormal.y);
    // Z-axis (front)
    half3 axisZ = half3(tnormalZ.xy + half2(normalWS.x, normalWS.y), absNormal.z);
    
    // Blend based on weights
    half3 result = normalize(
        axisX.xzy * data.weights.x +
        axisY.xzy * data.weights.y +
        axisZ.xyz * data.weights.z
    );
    
    return result;
}

// Simplified triplanar normal (faster, less accurate)
half3 SB_SampleTriplanarNormalFast(
    TEXTURE2D_PARAM(normalTex, samp),
    SB_TriplanarData data,
    half strength)
{
    half4 nFront = SAMPLE_TEXTURE2D(normalTex, samp, data.uvFront);
    half4 nSide = SAMPLE_TEXTURE2D(normalTex, samp, data.uvSide);
    half4 nTop = SAMPLE_TEXTURE2D(normalTex, samp, data.uvTop);
    
    half3 tnormalZ = UnpackNormalScale(nFront, strength);
    half3 tnormalX = UnpackNormalScale(nSide, strength);
    half3 tnormalY = UnpackNormalScale(nTop, strength);
    
    // Simple weighted average
    return normalize(
        tnormalZ * data.weights.z +
        half3(tnormalX.z, tnormalX.y, tnormalX.x) * data.weights.x +
        half3(tnormalY.x, tnormalY.z, tnormalY.y) * data.weights.y
    );
}


// HELPER: Sample texture with optional triplanar


// Generic sampler that switches between UV and triplanar
half4 SB_SampleTextureAuto(
    TEXTURE2D_PARAM(tex, samp),
    float2 uv,
    SB_TriplanarData triData,
    bool useTriplanar)
{
    if (useTriplanar)
    {
        return SB_SampleTriplanar(TEXTURE2D_ARGS(tex, samp), triData);
    }
    else
    {
        return SAMPLE_TEXTURE2D(tex, samp, uv);
    }
}


// STOCHASTIC SAMPLING (Anti-tiling)


// Hash function for stochastic
float2 SB_StochasticHash(float2 p)
{
    return frac(sin(float2(
        dot(p, float2(127.1, 311.7)),
        dot(p, float2(269.5, 183.3))
    )) * 43758.5453);
}

// Stochastic sampling (reduces texture tiling artifacts)
half4 SB_SampleStochastic(
    TEXTURE2D_PARAM(tex, samp),
    float2 uv,
    float variation)
{
    // Simple stochastic: add random offset based on tile
    float2 tile = floor(uv);
    float2 offset = SB_StochasticHash(tile) * variation;
    
    // Sample with rotated/offset UVs
    float2 newUV = frac(uv) + offset;
    
    return SAMPLE_TEXTURE2D(tex, samp, newUV);
}

// Triplanar with stochastic anti-tiling
half4 SB_SampleTriplanarStochastic(
    TEXTURE2D_PARAM(tex, samp),
    SB_TriplanarData data,
    float variation)
{
    half4 texFront = SB_SampleStochastic(TEXTURE2D_ARGS(tex, samp), data.uvFront, variation);
    half4 texSide = SB_SampleStochastic(TEXTURE2D_ARGS(tex, samp), data.uvSide, variation);
    half4 texTop = SB_SampleStochastic(TEXTURE2D_ARGS(tex, samp), data.uvTop, variation);
    
    return texFront * data.weights.z +
           texSide * data.weights.x +
           texTop * data.weights.y;
}

#endif // SB_GLASS_TRIPLANAR_INCLUDED
