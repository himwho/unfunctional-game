// SB_GlassLighting.hlsl
#ifndef SB_GLASS_LIGHTING_INCLUDED
#define SB_GLASS_LIGHTING_INCLUDED

#include "SB_GlassCore.hlsl"


// DIFFUSE LIGHTING


// Lambert diffuse
inline half3 LambertDiffuse(half3 normalWS, half3 lightDir, half3 lightColor, half atten)
{
    half NdotL = saturate(dot(normalWS, lightDir));
    NdotL = lerp(1.0, NdotL, _DiffuseIntensity);
    return lightColor * NdotL * atten;
}

// Half-Lambert for softer shadows (good for glass)
inline half3 HalfLambertDiffuse(half3 normalWS, half3 lightDir, half3 lightColor, half atten)
{
    half NdotL = dot(normalWS, lightDir) * 0.5 + 0.5;
    NdotL = lerp(1.0, NdotL, _DiffuseIntensity);
    return lightColor * NdotL * atten;
}


// SHADOW HANDLING


inline half GetShadowAttenuation(half shadowAtten)
{
    // Soften shadow intensity for glass (glass shouldn't have hard shadows)
    return lerp(1.0, shadowAtten, _ShadowIntensity);
}


// MAIN LIGHT


struct SB_LightingData
{
    half3 diffuse;
    half3 specular;
    half3 rim;
    half3 translucency;
    half  shadowAtten;
};

SB_LightingData CalculateMainLight(SB_GlassSurface surface, float3 positionWS, float4 shadowCoord)
{
    SB_LightingData data = (SB_LightingData)0;
    
    // Get main light
    Light mainLight = GetMainLight(shadowCoord);
    
    half3 lightDir = mainLight.direction;
    half3 lightColor = mainLight.color;
    half atten = mainLight.distanceAttenuation;
    half shadowAtten = GetShadowAttenuation(mainLight.shadowAttenuation);
    
    data.shadowAtten = shadowAtten;
    
    // Diffuse
#if defined(_SB_QUALITY_HIGH)
    data.diffuse = LambertDiffuse(surface.normalWS, lightDir, lightColor, atten * shadowAtten);
#else
    data.diffuse = HalfLambertDiffuse(surface.normalWS, lightDir, lightColor, atten * shadowAtten);
#endif
    
    // Specular
    data.specular = CalculateSpecular(surface.normalWS, surface.viewDirWS, lightDir, lightColor, atten * shadowAtten);
    
    // Rim
#if defined(_SB_RIM)
    data.rim = CalculateRim(surface.NdotV, lightColor * atten * shadowAtten);
#endif
    
    // Translucency
#if defined(_SB_TRANSLUCENT)
    data.translucency = CalculateTranslucency(surface.normalWS, surface.viewDirWS, lightDir, lightColor, atten);
#endif
    
    return data;
}


// ADDITIONAL LIGHTS


#if defined(_ADDITIONAL_LIGHTS)
SB_LightingData CalculateAdditionalLights(SB_GlassSurface surface, float3 positionWS, half4 shadowMask)
{
    SB_LightingData data = (SB_LightingData)0;
    
    uint lightCount = GetAdditionalLightsCount();
    
    // Limit iterations on mobile
#if defined(_SB_QUALITY_LOW)
    lightCount = min(lightCount, 2u);
#elif defined(_SB_QUALITY_MEDIUM)
    lightCount = min(lightCount, 4u);
#endif
    
    for (uint lightIndex = 0u; lightIndex < lightCount; ++lightIndex)
    {
        Light light = GetAdditionalLight(lightIndex, positionWS, shadowMask);
        
        half3 lightDir = light.direction;
        half3 lightColor = light.color;
        half atten = light.distanceAttenuation * light.shadowAttenuation;
        
        // Diffuse
        half NdotL = saturate(dot(surface.normalWS, lightDir));
        data.diffuse += lightColor * NdotL * atten;
        
        // Specular (consistent with main light)
    #if defined(_SB_SPECULAR)
        half3 halfDir = SafeNormalize(lightDir + surface.viewDirWS);
        half NdotH = saturate(dot(surface.normalWS, halfDir));
        
        // Base specular
        half sizeExp = lerp(4.0, 512.0, _SpecularSize * _SpecularSize);
        half spec = pow(NdotH, sizeExp * max(0.1, _SpecularSmoothness));
        
        // Toon mode
        if (_SpecularToon > 0.5)
        {
            half steps = max(1.0, floor(_SpecularSteps));
            if (steps <= 1.5)
                spec = step(_SpecularThreshold, spec);
            else
                spec = step(_SpecularThreshold, spec) * (floor(spec * steps + 0.5) / steps);
        }
        
        // Hardness (only if not toon)
        if (_SpecularHardness > 0.01 && _SpecularToon < 0.5)
        {
            half hardnessLow = lerp(0.0, 0.45, _SpecularHardness);
            half hardnessHigh = lerp(1.0, 0.55, _SpecularHardness);
            spec = smoothstep(hardnessLow, hardnessHigh, spec);
        }
        
        data.specular += _SpecularColor.rgb * spec * _SpecularIntensity * 0.5 * lightColor * atten;
    #endif
        
        // Translucency (simplified)
    #if defined(_SB_TRANSLUCENT) && !defined(_SB_QUALITY_LOW)
        half transDot = pow(saturate(dot(surface.viewDirWS, -lightDir)), _TranslucentPower);
        data.translucency += lightColor * transDot * atten * _TranslucentIntensity * 0.5 * _TranslucentColor.rgb;
    #endif
    }
    
    return data;
}
#endif


// VERTEX LIGHTING (Mobile fallback)


#if defined(_ADDITIONAL_LIGHTS_VERTEX)
half3 CalculateVertexLighting(float3 positionWS, half3 normalWS)
{
    half3 vertexLight = half3(0.0, 0.0, 0.0);
    
    uint lightCount = GetAdditionalLightsCount();
    lightCount = min(lightCount, 4u);
    
    for (uint i = 0u; i < lightCount; ++i)
    {
        Light light = GetAdditionalLight(i, positionWS);
        half NdotL = saturate(dot(normalWS, light.direction));
        vertexLight += light.color * light.distanceAttenuation * NdotL;
    }
    
    return vertexLight;
}
#endif


// AMBIENT / GI


half3 CalculateAmbient(half3 normalWS, SB_Varyings input)
{
    half3 ambient = half3(0.0, 0.0, 0.0);
    
#if defined(LIGHTMAP_ON)
    // Baked lightmap
    ambient = SampleLightmap(input.uvLightmap, normalWS);
#else
    // Spherical harmonics (vertex SH + per-pixel adjustment)
    ambient = input.vertexSH;
#endif
    
    return ambient;
}


// FULL LIGHTING CALCULATION


half4 CalculateLighting(SB_GlassSurface surface, SB_Varyings input, float time)
{
    // Get shadow coordinates
    float4 shadowCoord = float4(0.0, 0.0, 0.0, 0.0);
    
#if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
    shadowCoord = input.shadowCoord;
#elif defined(MAIN_LIGHT_CALCULATE_SHADOWS)
    shadowCoord = TransformWorldToShadowCoord(input.positionWS);
#endif
    
    // Main light
    Light mainLight = GetMainLight(shadowCoord);
    half3 mainLightDir = mainLight.direction;
    
    SB_LightingData mainLightData = CalculateMainLight(surface, input.positionWS, shadowCoord);
    
    half3 totalDiffuse = mainLightData.diffuse;
    half3 totalSpecular = mainLightData.specular;
    half3 totalRim = mainLightData.rim;
    half3 totalTranslucency = mainLightData.translucency;
    
    // Additional lights
#if defined(_ADDITIONAL_LIGHTS)
    half4 shadowMask = half4(1.0, 1.0, 1.0, 1.0);
    #if defined(SHADOWS_SHADOWMASK) && defined(LIGHTMAP_ON)
        shadowMask = SAMPLE_SHADOWMASK(input.uvLightmap);
    #elif defined(LIGHTMAP_ON)
        shadowMask = unity_ProbesOcclusion;
    #endif
    
    SB_LightingData addLightData = CalculateAdditionalLights(surface, input.positionWS, shadowMask);
    totalDiffuse += addLightData.diffuse;
    totalSpecular += addLightData.specular;
    totalTranslucency += addLightData.translucency;
#endif
    
    // Vertex lighting
#if defined(_ADDITIONAL_LIGHTS_VERTEX)
    totalDiffuse += input.fogFactorAndVertexLight.yzw;
#endif
    
    // Ambient
    half3 ambient = CalculateAmbient(surface.normalWS, input);
    totalDiffuse += ambient;
    
    // Combine
    half4 finalColor = MixFinalColor(surface, totalDiffuse, totalRim, totalSpecular, totalTranslucency, time, input, mainLightDir);
    
    // Fog
    half fogFactor = input.fogFactorAndVertexLight.x;
    finalColor.rgb = MixFog(finalColor.rgb, fogFactor);
    
    return finalColor;
}

#endif // SB_GLASS_LIGHTING_INCLUDED
