// SB_GlassPass.hlsl

#ifndef SB_GLASS_PASS_INCLUDED
#define SB_GLASS_PASS_INCLUDED

#include "SB_GlassSurface.hlsl"
#include "SB_GlassLighting.hlsl"

SB_Varyings SB_VertexForward(SB_Attributes input)
{
    SB_Varyings output = (SB_Varyings)0;
    
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_TRANSFER_INSTANCE_ID(input, output);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
    
    // Transform positions
    VertexPositionInputs positionInputs = GetVertexPositionInputs(input.positionOS.xyz);
    VertexNormalInputs normalInputs = GetVertexNormalInputs(input.normalOS, input.tangentOS);
    
    output.positionCS = positionInputs.positionCS;
    output.positionWS = positionInputs.positionWS;
    
    // UVs
    output.uv.xy = TRANSFORM_TEX(input.uv, _MainTex);
    
#if defined(_SB_DETAIL_ALBEDO) || defined(_SB_DETAIL_NORMAL)
    output.uv.zw = input.uv * _DetailTiling;
#else
    output.uv.zw = float2(0.0, 0.0);
#endif
    
    // Normals and tangents
    half3 viewDirWS = GetWorldSpaceNormalizeViewDir(positionInputs.positionWS);
    
#if defined(_SB_NORMALMAP) || defined(_SB_DETAIL_NORMAL) || defined(_SB_RAIN)
    output.normalWS = half4(normalInputs.normalWS, viewDirWS.x);
    output.tangentWS = half4(normalInputs.tangentWS, viewDirWS.y);
    
    half sign = input.tangentOS.w * GetOddNegativeScale();
    output.bitangentWS = half4(cross(normalInputs.normalWS, normalInputs.tangentWS) * sign, viewDirWS.z);
#else
    output.normalWS = normalInputs.normalWS;
    output.viewDirWS = viewDirWS;
#endif
    
    // Screen position for refraction
    output.screenPos = ComputeScreenPos(positionInputs.positionCS);
    
    // Shadow coords
#if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
    output.shadowCoord = GetShadowCoord(positionInputs);
#endif
    
    // Lightmap / SH
#if defined(LIGHTMAP_ON)
    output.uvLightmap = input.uvLightmap * unity_LightmapST.xy + unity_LightmapST.zw;
#else
    output.vertexSH = SampleSHVertex(normalInputs.normalWS);
#endif
    
    // Fog and vertex lighting
    half fogFactor = ComputeFogFactor(positionInputs.positionCS.z);
    
#if defined(_ADDITIONAL_LIGHTS_VERTEX)
    half3 vertexLight = CalculateVertexLighting(positionInputs.positionWS, normalInputs.normalWS);
    output.fogFactorAndVertexLight = half4(fogFactor, vertexLight);
#else
    output.fogFactorAndVertexLight = half4(fogFactor, 0.0, 0.0, 0.0);
#endif
    
    return output;
}


// FORWARD PASS - FRAGMENT


half4 SB_FragmentForward(SB_Varyings input) : SV_Target
{
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
    
    // Time for animations
    float time = _Time.y;
    
    // Initialize surface
    SB_GlassSurface surface = InitializeSurface(input);
    
    // Calculate lighting
    half4 finalColor = CalculateLighting(surface, input, time);
    
    // Alpha clip
#if defined(_SB_ALPHA_CLIP)
    clip(finalColor.a - _AlphaClip);
#endif
    
    return finalColor;
}


// SHADOW CASTER PASS


#ifdef SB_SHADOW_CASTER_PASS

float3 _LightDirection;
float3 _LightPosition;

struct SB_ShadowVaryings
{
    float4 positionCS : SV_POSITION;
    float2 uv : TEXCOORD0;
    UNITY_VERTEX_INPUT_INSTANCE_ID
    UNITY_VERTEX_OUTPUT_STEREO
};

float4 GetShadowPositionHClip(SB_Attributes input)
{
    float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
    float3 normalWS = TransformObjectToWorldNormal(input.normalOS);
    
#if _CASTING_PUNCTUAL_LIGHT_SHADOW
    float3 lightDirectionWS = normalize(_LightPosition - positionWS);
#else
    float3 lightDirectionWS = _LightDirection;
#endif
    
    float4 positionCS = TransformWorldToHClip(ApplyShadowBias(positionWS, normalWS, lightDirectionWS));
    
#if UNITY_REVERSED_Z
    positionCS.z = min(positionCS.z, UNITY_NEAR_CLIP_VALUE);
#else
    positionCS.z = max(positionCS.z, UNITY_NEAR_CLIP_VALUE);
#endif
    
    return positionCS;
}

SB_ShadowVaryings SB_VertexShadow(SB_Attributes input)
{
    SB_ShadowVaryings output = (SB_ShadowVaryings)0;
    
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_TRANSFER_INSTANCE_ID(input, output);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
    
    output.positionCS = GetShadowPositionHClip(input);
    output.uv = TRANSFORM_TEX(input.uv, _MainTex);
    
    return output;
}

half4 SB_FragmentShadow(SB_ShadowVaryings input) : SV_TARGET
{
    UNITY_SETUP_INSTANCE_ID(input);
    
#if defined(_SB_ALPHA_CLIP)
    half alpha = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv).a * _Color.a * _Opacity;
    clip(alpha - _AlphaClip);
#endif
    
    return 0;
}

#endif // SB_SHADOW_CASTER_PASS


// DEPTH ONLY PASS


#ifdef SB_DEPTH_ONLY_PASS

struct SB_DepthVaryings
{
    float4 positionCS : SV_POSITION;
    float2 uv : TEXCOORD0;
    UNITY_VERTEX_INPUT_INSTANCE_ID
    UNITY_VERTEX_OUTPUT_STEREO
};

SB_DepthVaryings SB_VertexDepth(SB_Attributes input)
{
    SB_DepthVaryings output = (SB_DepthVaryings)0;
    
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_TRANSFER_INSTANCE_ID(input, output);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
    
    output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
    output.uv = TRANSFORM_TEX(input.uv, _MainTex);
    
    return output;
}

half4 SB_FragmentDepth(SB_DepthVaryings input) : SV_TARGET
{
    UNITY_SETUP_INSTANCE_ID(input);
    
#if defined(_SB_ALPHA_CLIP)
    half alpha = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv).a * _Color.a * _Opacity;
    clip(alpha - _AlphaClip);
#endif
    
    return 0;
}

#endif // SB_DEPTH_ONLY_PASS


// DEPTH NORMALS PASS


#ifdef SB_DEPTH_NORMALS_PASS

struct SB_DepthNormalsVaryings
{
    float4 positionCS : SV_POSITION;
    float2 uv : TEXCOORD0;
    float3 normalWS : TEXCOORD1;
    UNITY_VERTEX_INPUT_INSTANCE_ID
    UNITY_VERTEX_OUTPUT_STEREO
};

SB_DepthNormalsVaryings SB_VertexDepthNormals(SB_Attributes input)
{
    SB_DepthNormalsVaryings output = (SB_DepthNormalsVaryings)0;
    
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_TRANSFER_INSTANCE_ID(input, output);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
    
    output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
    output.uv = TRANSFORM_TEX(input.uv, _MainTex);
    output.normalWS = TransformObjectToWorldNormal(input.normalOS);
    
    return output;
}

half4 SB_FragmentDepthNormals(SB_DepthNormalsVaryings input) : SV_TARGET
{
    UNITY_SETUP_INSTANCE_ID(input);
    
#if defined(_SB_ALPHA_CLIP)
    half alpha = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv).a * _Color.a * _Opacity;
    clip(alpha - _AlphaClip);
#endif
    
    return half4(NormalizeNormalPerPixel(input.normalWS), 0.0);
}

#endif // SB_DEPTH_NORMALS_PASS


// META PASS (Lightmapping)


#ifdef SB_META_PASS

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/MetaInput.hlsl"

struct SB_MetaVaryings
{
    float4 positionCS : SV_POSITION;
    float2 uv : TEXCOORD0;
    UNITY_VERTEX_INPUT_INSTANCE_ID
    UNITY_VERTEX_OUTPUT_STEREO
};

SB_MetaVaryings SB_VertexMeta(SB_Attributes input)
{
    SB_MetaVaryings output = (SB_MetaVaryings)0;
    
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_TRANSFER_INSTANCE_ID(input, output);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
    
    output.positionCS = MetaVertexPosition(input.positionOS, input.uvLightmap, input.uvLightmap, unity_LightmapST, unity_DynamicLightmapST);
    output.uv = TRANSFORM_TEX(input.uv, _MainTex);
    
    return output;
}

half4 SB_FragmentMeta(SB_MetaVaryings input) : SV_TARGET
{
    UNITY_SETUP_INSTANCE_ID(input);
    
    half4 albedo = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv) * _Color;
    
    half3 emission = half3(0, 0, 0);
#if defined(_SB_EMISSION)
    #if defined(_SB_EMISSION_MAP)
        emission = SAMPLE_TEXTURE2D(_EmissionMap, sampler_EmissionMap, input.uv).rgb * _EmissionColor.rgb * _EmissionIntensity;
    #else
        emission = _EmissionColor.rgb * _EmissionIntensity;
    #endif
#endif
    
    MetaInput metaInput;
    metaInput.Albedo = albedo.rgb;
    metaInput.Emission = emission;
    
    return MetaFragment(metaInput);
}

#endif // SB_META_PASS

#endif // SB_GLASS_PASS_INCLUDED
