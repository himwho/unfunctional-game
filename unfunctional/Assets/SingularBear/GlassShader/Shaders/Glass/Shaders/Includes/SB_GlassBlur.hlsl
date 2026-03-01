// SB_GlassBlur.hlsl
#ifndef SB_GLASS_BLUR_INCLUDED
#define SB_GLASS_BLUR_INCLUDED


// GAUSSIAN KERNEL DATA


// 8-tap optimized pattern (diagonal + cross)
static const float2 BlurOffsets8[8] = {
    float2(-1.0, -1.0), float2(1.0, -1.0),
    float2(-1.0,  1.0), float2(1.0,  1.0),
    float2(-1.414, 0.0), float2(1.414, 0.0),
    float2(0.0, -1.414), float2(0.0, 1.414)
};

// 13-tap Gaussian kernel (optimized)
static const float2 BlurOffsets13[13] = {
    float2(0.0, 0.0),
    float2(-1.0, 0.0), float2(1.0, 0.0),
    float2(0.0, -1.0), float2(0.0, 1.0),
    float2(-0.707, -0.707), float2(0.707, -0.707),
    float2(-0.707, 0.707), float2(0.707, 0.707),
    float2(-1.414, -1.414), float2(1.414, -1.414),
    float2(-1.414, 1.414), float2(1.414, 1.414)
};

static const float BlurWeights13[13] = {
    0.1964825501511,
    0.1178097248394, 0.1178097248394,
    0.1178097248394, 0.1178097248394,
    0.0559016994375, 0.0559016994375,
    0.0559016994375, 0.0559016994375,
    0.0220472440945, 0.0220472440945,
    0.0220472440945, 0.0220472440945
};


// BLUR FUNCTIONS (Always compiled - used by CalculateRefraction)


// 4-tap box blur - Ultra fast for mobile
half3 SB_Blur4(float2 uv, float blurSize)
{
    half3 color = half3(0, 0, 0);
    
    color += SampleSceneColor(uv + float2(-blurSize, -blurSize));
    color += SampleSceneColor(uv + float2( blurSize, -blurSize));
    color += SampleSceneColor(uv + float2(-blurSize,  blurSize));
    color += SampleSceneColor(uv + float2( blurSize,  blurSize));
    
    return color * 0.25;
}

// 8-tap Gaussian blur - Good mobile quality
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

// 13-tap weighted Gaussian blur - High quality
half3 SB_Blur13(float2 uv, float blurSize)
{
    half3 color = half3(0, 0, 0);
    
    [unroll]
    for (int i = 0; i < 13; i++)
    {
        color += SampleSceneColor(uv + BlurOffsets13[i] * blurSize) * BlurWeights13[i];
    }
    
    return color;
}

// 16-tap grid blur - Very high quality
half3 SB_Blur16(float2 uv, float blurSize)
{
    half3 color = half3(0, 0, 0);
    
    [unroll]
    for (int x = -2; x <= 1; x++)
    {
        [unroll]
        for (int y = -2; y <= 1; y++)
        {
            float2 offset = float2(x + 0.5, y + 0.5) * blurSize;
            color += SampleSceneColor(uv + offset);
        }
    }
    
    return color * 0.0625; // 1/16
}

// Variable sample count blur - Ultra quality (expensive!)
half3 SB_BlurVariable(float2 uv, float blurSize, int samples)
{
    half3 color = half3(0, 0, 0);
    float totalWeight = 0.0;
    int halfSamples = samples / 2;
    
    for (int x = -halfSamples; x <= halfSamples; x++)
    {
        for (int y = -halfSamples; y <= halfSamples; y++)
        {
            float2 offset = float2(x, y) * blurSize / float(halfSamples);
            color += SampleSceneColor(uv + offset);
            totalWeight += 1.0;
        }
    }
    
    return color / totalWeight;
}


// MAIN BLUR FUNCTION - Quality based selection


// Quality levels:
// 4  = Mobile Low (4 samples)
// 8  = Mobile High (8 samples)
// 13 = PC Medium (13 samples weighted)
// 16 = PC High (16 samples)
// 32 = Ultra (variable samples)

half3 SB_SampleSceneBlurred(float2 uv, half strength, half radius, int quality)
{
    // Early out if no blur
    if (strength < 0.001)
        return SampleSceneColor(uv).rgb;
    
    // Calculate blur size in UV space
    // radius is in pixels, convert to UV using screen params
    float blurSize = (radius * strength) / _ScreenParams.y;
    
    // Clamp blur size to reasonable range
    blurSize = clamp(blurSize, 0.0, 0.1);
    
    // Select blur quality
    if (quality <= 4)
    {
        return SB_Blur4(uv, blurSize);
    }
    else if (quality <= 8)
    {
        return SB_Blur8(uv, blurSize);
    }
    else if (quality <= 13)
    {
        return SB_Blur13(uv, blurSize);
    }
    else if (quality <= 16)
    {
        return SB_Blur16(uv, blurSize);
    }
    else
    {
        // Ultra quality - use variable samples (capped at 6x6=36)
        int samples = min(quality / 4, 6);
        return SB_BlurVariable(uv, blurSize, samples);
    }
}


// DIRECTIONAL BLUR (Motion blur style)


half3 SB_BlurDirectional(float2 uv, float2 direction, half strength, int samples)
{
    if (strength < 0.001) return SampleSceneColor(uv).rgb;
    
    half3 color = half3(0, 0, 0);
    float2 step = direction * strength / _ScreenParams.y;
    
    samples = max(samples, 2);
    
    [unroll(16)]
    for (int i = 0; i < samples; i++)
    {
        float t = (float(i) / float(samples - 1)) - 0.5;
        color += SampleSceneColor(uv + step * t * float(samples));
    }
    
    return color / float(samples);
}


// RADIAL BLUR (Zoom blur)


half3 SB_BlurRadial(float2 uv, float2 center, half strength, int samples)
{
    if (strength < 0.001) return SampleSceneColor(uv).rgb;
    
    half3 color = half3(0, 0, 0);
    float2 direction = uv - center;
    
    samples = max(samples, 2);
    
    [unroll(16)]
    for (int i = 0; i < samples; i++)
    {
        float scale = 1.0 - strength * 0.1 * (float(i) / float(samples));
        float2 sampleUV = center + direction * scale;
        color += SampleSceneColor(sampleUV);
    }
    
    return color / float(samples);
}

#endif // SB_GLASS_BLUR_INCLUDED
