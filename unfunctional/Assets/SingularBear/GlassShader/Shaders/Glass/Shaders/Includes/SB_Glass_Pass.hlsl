// ============================================================================
// SingularBear Glass - Main Pass File
// Includes all dependencies and provides auxiliary passes
// ============================================================================

#ifndef SB_GLASS_PASS_INCLUDED
#define SB_GLASS_PASS_INCLUDED

// Core URP includes
#include "SB_Glass_Core.hlsl"

// Shader properties and structures
#include "SB_Glass_Input.hlsl"

// Main forward rendering
#include "SB_Glass_Forward.hlsl"

// ============================================================================
// SHADOW CASTER PASS
// ============================================================================

float3 _LightDirection;
float3 _LightPosition;

SB_VaryingsMinimal SB_VertexShadow(SB_Attributes input)
{
    SB_VaryingsMinimal output = (SB_VaryingsMinimal)0;
    
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_TRANSFER_INSTANCE_ID(input, output);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
    
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
    
    output.positionCS = positionCS;
    return output;
}

half4 SB_FragmentShadow(SB_VaryingsMinimal input) : SV_Target
{
    return 0;
}

// ============================================================================
// DEPTH ONLY PASS
// ============================================================================

SB_VaryingsMinimal SB_VertexDepth(SB_Attributes input)
{
    SB_VaryingsMinimal output = (SB_VaryingsMinimal)0;
    
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_TRANSFER_INSTANCE_ID(input, output);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
    
    output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
    return output;
}

half4 SB_FragmentDepth(SB_VaryingsMinimal input) : SV_Target
{
    return 0;
}

// ============================================================================
// DEPTH NORMALS PASS
// ============================================================================

SB_VaryingsDepthNormals SB_VertexDepthNormals(SB_Attributes input)
{
    SB_VaryingsDepthNormals output = (SB_VaryingsDepthNormals)0;
    
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_TRANSFER_INSTANCE_ID(input, output);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
    
    output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
    output.normalWS = TransformObjectToWorldNormal(input.normalOS);
    return output;
}

half4 SB_FragmentDepthNormals(SB_VaryingsDepthNormals input) : SV_Target
{
    float3 normalWS = normalize(input.normalWS);
    return half4(normalWS * 0.5 + 0.5, 0);
}

#endif // SB_GLASS_PASS_INCLUDED
