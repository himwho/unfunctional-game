// SB_GlassDecals.hlsl

#ifndef SB_GLASS_DECALS_INCLUDED
#define SB_GLASS_DECALS_INCLUDED

struct DecalResult
{
    half3 color;
    half alpha;
};

inline float2 TransformDecalUV(float2 uv, float2 position, float rotationDeg, float size)
{
    float2 centered = uv - position;
    float rad = rotationDeg * 0.01745329;
    float c = cos(rad);
    float s = sin(rad);
    float2 rotated = float2(centered.x * c - centered.y * s, centered.x * s + centered.y * c);
    float2 scaled = rotated / max(size, 0.001);
    return scaled + 0.5;
}

inline void SampleDecal(
    TEXTURE2D_PARAM(tex, samp),
    float4 texST,
    float2 uv,
    float2 position,
    float size,
    float rotationDeg,
    float intensity,
    half4 tint,
    inout DecalResult result)
{
    float2 decalUV = TransformDecalUV(uv, position, rotationDeg, size);
    
    if (decalUV.x < 0.0 || decalUV.x > 1.0 || decalUV.y < 0.0 || decalUV.y > 1.0)
        return;
    
    // Apply texture tiling/offset within decal space
    decalUV = decalUV * texST.xy + texST.zw;
    
    half4 decalSample = SAMPLE_TEXTURE2D(tex, samp, decalUV);
    half3 decalColor = decalSample.rgb * tint.rgb;
    half decalAlpha = decalSample.a * tint.a * intensity;
    
    result.color = lerp(result.color, decalColor, decalAlpha);
    result.alpha = saturate(result.alpha + decalAlpha * (1.0 - result.alpha));
}

inline DecalResult CalculateDecals(float2 uv)
{
    DecalResult result = (DecalResult)0;
    result.color = half3(0, 0, 0);
    result.alpha = 0;
    
    #if defined(_SB_DECALS)
    
    SampleDecal(
        TEXTURE2D_ARGS(_DecalTexture1, sampler_DecalTexture1),
        _DecalTexture1_ST,
        uv, _DecalPosition1.xy, _DecalSize1, _DecalRotation1, _DecalIntensity1, _DecalTint1, result);
    
    #if defined(_SB_DECAL2)
    SampleDecal(
        TEXTURE2D_ARGS(_DecalTexture2, sampler_DecalTexture2),
        _DecalTexture2_ST,
        uv, _DecalPosition2.xy, _DecalSize2, _DecalRotation2, _DecalIntensity2, _DecalTint2, result);
    #endif
    
    #if defined(_SB_DECAL3)
    SampleDecal(
        TEXTURE2D_ARGS(_DecalTexture3, sampler_DecalTexture3),
        _DecalTexture3_ST,
        uv, _DecalPosition3.xy, _DecalSize3, _DecalRotation3, _DecalIntensity3, _DecalTint3, result);
    #endif
    
    #if defined(_SB_DECAL4)
    SampleDecal(
        TEXTURE2D_ARGS(_DecalTexture4, sampler_DecalTexture4),
        _DecalTexture4_ST,
        uv, _DecalPosition4.xy, _DecalSize4, _DecalRotation4, _DecalIntensity4, _DecalTint4, result);
    #endif
    
    #endif
    
    return result;
}

#endif
