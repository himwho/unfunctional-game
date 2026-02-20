#include_with_pragmas "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRenderingKeywords.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRendering.hlsl"

bool IsStereoEyeLeft(float3 WorldCamPos, float3 WorldCamRight, float force)
{
	if(force >= 0){
		return force == 0;
	}
#if defined(USING_STEREO_MATRICES)
	// Unity 5.4 has this new variable
	return (unity_StereoEyeIndex == 0);
#elif defined (UNITY_DECLARE_MULTIVIEW)
	// OVR_multiview extension
	return (UNITY_VIEWID == 0);
#else
	// NOTE: Bug #1165: _WorldSpaceCameraPos is not correct in multipass VR (when skybox is used) but UNITY_MATRIX_I_V seems to be
	#if defined(UNITY_MATRIX_I_V)
		float3 renderCameraPos = UNITY_MATRIX_I_V._m03_m13_m23;
	#else
		float3 renderCameraPos = _WorldSpaceCameraPos.xyz;
	#endif

	float fL = distance(WorldCamPos - WorldCamRight, renderCameraPos);
	float fR = distance(WorldCamPos + WorldCamRight, renderCameraPos);
	return (fL < fR);
#endif
}

void StereoSwitch_half(float3 WorldCameraPosition, float3 WorldCameraRight, float Force, float4 Left, float4 Right, out float4 Out) {
	Out = Left;
	if (IsStereoEyeLeft(WorldCameraPosition, WorldCameraRight, Force)){
	  Out = Left;
	}
	else {
	  Out = Right;
	}
}
void StereoSwitch_float(float3 WorldCameraPosition, float3 WorldCameraRight, float Force, float4 Left, float4 Right, out float4 Out) {
	Out = Left;
	if (IsStereoEyeLeft(WorldCameraPosition, WorldCameraRight, Force)){
	  Out = Left;
	}
	else {
	  Out = Right;
	}
}


void SampleReflectionProbe_float(float3 PositionWS, float3 NormalWS, float3 ViewDirWS, float Roughness, out float3 hdrColor)
{
#ifdef SHADERGRAPH_PREVIEW
    hdrColor = float3(0, 0, 0);
#else
    float3 viewDirWS = normalize(ViewDirWS);
    float3 normalWS = normalize(NormalWS);
    float3 reflDir = normalize(reflect(-viewDirWS, normalWS));

#ifdef _REFLECTION_PROBE_BOX_PROJECTION
#endif

    reflDir = BoxProjectedCubemapDirection(reflDir, PositionWS, unity_SpecCube0_ProbePosition, unity_SpecCube0_BoxMin, unity_SpecCube0_BoxMax);
    reflDir = normalize(reflDir);

    // Clamp roughness
    //Roughness = saturate(Roughness);

    // Sample the reflection probe
    float4 encodedIrradiance = SAMPLE_TEXTURECUBE_LOD(unity_SpecCube0, samplerunity_SpecCube0, reflDir, Roughness);

    // Decode HDR data
    hdrColor = DecodeHDREnvironment(encodedIrradiance, unity_SpecCube0_HDR);
#endif

}


// The Raw mode screenPosition must first be normalized: position.xy/position.ww
void CorrectedScreenPosition_float(float4 screenPosition, out float2 correctedScreenPosition)
{
	screenPosition = screenPosition.xyzw/screenPosition.wwww;
        
#if UNITY_UV_STARTS_AT_TOP
        screenPosition.y = 1.0 - screenPosition.y;
#endif
    
    if (_ProjectionParams.x < 0.0)
        screenPosition.y = 1.0 - screenPosition.y;
    
    //correctedScreenPosition = FoveatedRemapLinearToNonUniform(screenPosition);
    correctedScreenPosition = screenPosition;
}
// The Raw mode screenPosition must first be normalized: position.xy/position.ww
void CorrectedScreenPosition_half(float4 screenPosition, out float2 correctedScreenPosition)
{
    screenPosition = screenPosition.xyzw / screenPosition.wwww;
        
#if UNITY_UV_STARTS_AT_TOP
        screenPosition.y = 1.0 - screenPosition.y;
#endif
    
    if (_ProjectionParams.x < 0.0)
        screenPosition.y = 1.0 - screenPosition.y;
    
    //correctedScreenPosition = FoveatedRemapLinearToNonUniform(screenPosition);
    correctedScreenPosition = screenPosition;
}

void DivideByW_float(float4 input, out float2 output)
{
    // Check if w is non-zero to avoid division by zero
    if (input.w != 0.0)
    {
        output = input.xy / input.w;
    }
    else
    {
        // Return (0, 0) if w is zero to handle edge cases
        output = float2(0.0, 0.0);
    }
}
void DivideByW_half(float4 input, out float2 output)
{
    // Check if w is non-zero to avoid division by zero
    if (input.w != 0.0)
    {
        output = input.xy / input.w;
    }
    else
    {
        // Return (0, 0) if w is zero to handle edge cases
        output = float2(0.0, 0.0);
    }
}