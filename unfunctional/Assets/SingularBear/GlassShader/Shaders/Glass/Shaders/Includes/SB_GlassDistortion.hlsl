// SB_GlassDistortion.hlsl
#ifndef SB_GLASS_DISTORTION_INCLUDED
#define SB_GLASS_DISTORTION_INCLUDED

// Constants
#define SB_PI 3.14159265359
#define SB_TWO_PI 6.28318530718


// MAGNIFY / LENS EFFECT


// Magnifying glass / lens effect
// strength > 0 = magnify, strength < 0 = minify
float2 SB_Magnify(float2 uv, float strength, float radius, float2 center, float falloff)
{
    float2 dir = uv - center;
    float dist = length(dir);
    float factor = 1.0 - smoothstep(0.0, radius, dist);
    factor = pow(factor, falloff);
    return uv - dir * strength * factor;
}


// BARREL / PINCUSHION DISTORTION


// Barrel distortion (positive) or pincushion (negative)
// Simulates lens distortion
float2 SB_Barrel(float2 uv, float strength)
{
    float2 centered = uv - 0.5;
    float r2 = dot(centered, centered);
    float factor = 1.0 + strength * r2;
    return centered * factor + 0.5;
}

// Advanced barrel with k1, k2 coefficients
float2 SB_BarrelAdvanced(float2 uv, float k1, float k2)
{
    float2 centered = uv - 0.5;
    float r2 = dot(centered, centered);
    float r4 = r2 * r2;
    float factor = 1.0 + k1 * r2 + k2 * r4;
    return centered * factor + 0.5;
}


// WAVE DISTORTION


// Animated wave distortion
float2 SB_Waves(float2 uv, float amplitude, float frequency, float speed, float time)
{
    float2 result = uv;
    result.x += sin(uv.y * frequency + time * speed) * amplitude;
    result.y += cos(uv.x * frequency + time * speed) * amplitude;
    return result;
}

// Directional waves (horizontal or vertical)
float2 SB_WavesDirectional(float2 uv, float amplitude, float frequency, float speed, float time, float2 direction)
{
    float wave = sin(dot(uv, direction) * frequency + time * speed) * amplitude;
    return uv + direction * wave;
}

// Ripple effect from center point
float2 SB_Ripple(float2 uv, float2 center, float amplitude, float frequency, float speed, float time, float decay)
{
    float2 dir = uv - center;
    float dist = length(dir);
    float wave = sin(dist * frequency - time * speed) * amplitude;
    wave *= exp(-dist * decay); // Decay with distance
    return uv + normalize(dir + 0.0001) * wave;
}


// SWIRL DISTORTION


// Swirl/vortex effect
float2 SB_Swirl(float2 uv, float strength, float radius, float2 center)
{
    float2 dir = uv - center;
    float dist = length(dir);
    float factor = max(0.0, 1.0 - dist / radius);
    float angle = strength * factor * factor * SB_TWO_PI;
    float c = cos(angle);
    float s = sin(angle);
    float2 rotated = float2(
        dir.x * c - dir.y * s,
        dir.x * s + dir.y * c
    );
    return rotated + center;
}

// Animated swirl
float2 SB_SwirlAnimated(float2 uv, float strength, float radius, float2 center, float speed, float time)
{
    float animatedStrength = strength * sin(time * speed);
    return SB_Swirl(uv, animatedStrength, radius, center);
}


// PIXELATE


// Pixelation effect
float2 SB_Pixelate(float2 uv, float amount)
{
    if (amount < 1.0) return uv;
    float2 pixels = floor(uv * amount) / amount;
    return pixels + 0.5 / amount;
}

// Hexagonal pixelation
float2 SB_HexPixelate(float2 uv, float size)
{
    if (size < 1.0) return uv;
    
    float2 hex = uv * size;
    float2 r = float2(1.0, 1.73205); // 1, sqrt(3)
    float2 h = r * 0.5;
    
    float2 a = fmod(hex, r) - h;
    float2 b = fmod(hex - h, r) - h;
    
    float2 gv = dot(a, a) < dot(b, b) ? a : b;
    
    return (hex - gv) / size;
}


// SPHERICAL DISTORTION


// Spherical/fisheye effect
float2 SB_Spherical(float2 uv, float strength, float2 center)
{
    float2 centered = uv - center;
    float dist = length(centered);
    
    if (dist < 0.0001) return uv;
    
    float theta = atan2(centered.y, centered.x);
    float radius = pow(dist, strength);
    
    return center + float2(cos(theta), sin(theta)) * radius;
}


// HEAT HAZE / TURBULENCE


// Simple noise-based turbulence
float SB_SimpleNoise(float2 uv)
{
    return frac(sin(dot(uv, float2(12.9898, 78.233))) * 43758.5453);
}

float2 SB_HeatHaze(float2 uv, float strength, float scale, float speed, float time)
{
    float2 noiseUV = uv * scale + float2(0, time * speed);
    float noise1 = SB_SimpleNoise(noiseUV);
    float noise2 = SB_SimpleNoise(noiseUV + 0.5);
    
    float2 offset = float2(noise1 - 0.5, noise2 - 0.5) * strength;
    return uv + offset;
}


// GLASS-SPECIFIC DISTORTIONS


// Thick glass edge distortion
float2 SB_ThickGlassEdge(float2 uv, float thickness, float NdotV)
{
    float edgeFactor = 1.0 - NdotV;
    edgeFactor = pow(edgeFactor, 2.0);
    
    float2 centered = uv - 0.5;
    return uv - centered * edgeFactor * thickness;
}

// Beveled edge distortion
float2 SB_BevelDistortion(float2 uv, float2 normalXY, float strength)
{
    return uv + normalXY * strength;
}


// COMBINED DISTORTION APPLICATION


#if defined(_SB_DISTORTION)

float2 SB_ApplyAllDistortions(float2 uv, float time)
{
    float2 result = uv;
    
    // Magnify
    #if defined(_SB_MAGNIFY)
    if (abs(_MagnifyStrength) > 0.001)
    {
        result = SB_Magnify(result, _MagnifyStrength, _MagnifyRadius, _MagnifyCenter.xy, _MagnifyFalloff);
    }
    #endif
    
    // Barrel distortion
    #if defined(_SB_BARREL)
    if (abs(_BarrelStrength) > 0.001)
    {
        result = SB_Barrel(result, _BarrelStrength);
    }
    #endif
    
    // Wave distortion
    #if defined(_SB_WAVES)
    if (_WaveAmplitude > 0.001)
    {
        result = SB_Waves(result, _WaveAmplitude, _WaveFrequency, _WaveSpeed, time);
    }
    #endif
    
    // Ripple effect
    #if defined(_SB_RIPPLE)
    if (_RippleAmplitude > 0.001)
    {
        result = SB_Ripple(result, _RippleCenter.xy, _RippleAmplitude, _RippleFrequency, _RippleSpeed, time, _RippleDecay);
    }
    #endif
    
    // Swirl
    #if defined(_SB_SWIRL)
    if (abs(_SwirlStrength) > 0.001)
    {
        result = SB_Swirl(result, _SwirlStrength, _SwirlRadius, float2(0.5, 0.5));
    }
    #endif
    
    // Heat haze
    #if defined(_SB_HEAT_HAZE)
    if (_HeatHazeStrength > 0.001)
    {
        result = SB_HeatHaze(result, _HeatHazeStrength, _HeatHazeScale, _HeatHazeSpeed, time);
    }
    #endif
    
    // Pixelate (apply last)
    #if defined(_SB_PIXELATE)
    if (_PixelateSize > 1.0)
    {
        result = SB_Pixelate(result, _PixelateSize);
    }
    #endif
    
    return result;
}

#endif // _SB_DISTORTION

#endif // SB_GLASS_DISTORTION_INCLUDED
