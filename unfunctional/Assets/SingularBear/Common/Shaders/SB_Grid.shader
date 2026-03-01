Shader "SingularBear/Presentation/Grid"
{
    Properties
    {
        [Header(Background)]
        _BaseColor ("Background Color", Color) = (0.05, 0.05, 0.05, 1)
        
        [Header(Reflections)]
        _ReflectionStrength ("Reflection Strength", Range(0, 2)) = 0.5
        _ReflectionRoughness ("Reflection Roughness", Range(0, 1)) = 0.0
        [Toggle(_ADD_FRESNEL)] _UseFresnel ("Use Fresnel", Float) = 0

        [Header(Main Grid)]
        [HDR] _GridColor ("Grid Color", Color) = (1, 1, 1, 1)
        _GridEmission ("Emission Intensity", Range(0, 20)) = 5.0  
        _GridOpacity ("Grid Opacity", Range(0, 1)) = 1.0
        _GridScale ("Grid Scale", Float) = 1.0
        _GridThickness ("Line Thickness", Range(0.001, 0.1)) = 0.02
        
        [Header(Sub Grid)]
        [Toggle(_SUBGRID)] _UseSubGrid ("Enable Sub Grid", Float) = 0
        [HDR] _SubGridColor ("Sub Grid Color", Color) = (0.2, 0.2, 0.2, 1)
        _SubGridEmission ("Sub Emission Intensity", Range(0, 20)) = 1.0
        _SubGridOpacity ("Sub Grid Opacity", Range(0, 1)) = 0.3
        _SubGridScale ("Sub Grid Scale", Float) = 5.0
        _SubGridThickness ("Sub Grid Thickness", Range(0.001, 0.1)) = 0.005

        [Header(Studio Effects)]
        [Toggle(_STUDIO_EFFECTS)] _UseStudioEffects ("Enable Studio Effects", Float) = 1
        _FadeStart ("Distance Fade Start", Float) = 5.0
        _FadeEnd ("Distance Fade End", Float) = 20.0
        _RadialFocus ("Radial Focus Radius", Float) = 10.0
        _PulseSpeed ("Pulse Speed", Range(0, 5)) = 0.0

        [Header(Triplanar Settings)]
        _TriplanarSharpness ("Triplanar Blend", Range(1, 64)) = 8.0
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" }
        LOD 200

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma shader_feature_local _SUBGRID
            #pragma shader_feature_local _STUDIO_EFFECTS
            #pragma shader_feature_local _ADD_FRESNEL
            
            #pragma multi_compile _ _REFLECTION_PROBE_BOX_PROJECTION
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS   : TEXCOORD1;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                
                float _ReflectionStrength;
                float _ReflectionRoughness;

                float4 _GridColor;
                float _GridEmission;
                float _GridOpacity;
                float _GridScale;
                float _GridThickness;
                
                float4 _SubGridColor;
                float _SubGridEmission; 
                float _SubGridOpacity;
                float _SubGridScale;
                float _SubGridThickness;
                
                float _FadeStart;
                float _FadeEnd;
                float _RadialFocus;
                float _PulseSpeed;
                float _TriplanarSharpness;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output;
                VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs normalInput = GetVertexNormalInputs(input.normalOS);

                output.positionCS = vertexInput.positionCS;
                output.positionWS = vertexInput.positionWS;
                output.normalWS = normalInput.normalWS;
                return output;
            }

            float GridFactor(float2 uv, float scale, float thickness)
            {
                float2 pos = uv * scale;
                float2 d = abs(frac(pos - 0.5) - 0.5);
                float2 fw = fwidth(pos);
                float2 g = smoothstep(thickness - fw, thickness + fw, d);
                return 1.0 - min(g.x, g.y);
            }

            half4 frag(Varyings input) : SV_Target
            {
                float3 pos = input.positionWS;
                float3 normal = normalize(input.normalWS);
                float3 viewDir = normalize(GetWorldSpaceViewDir(pos));

                // Effects
                float studioMask = 1.0; 
                #if defined(_STUDIO_EFFECTS)
                    float distCam = distance(pos, _WorldSpaceCameraPos);
                    float distFade = 1.0 - smoothstep(_FadeStart, _FadeEnd, distCam);
                    float distOrigin = length(pos.xz);
                    float radialFade = 1.0 - smoothstep(_RadialFocus * 0.5, _RadialFocus, distOrigin);
                    float pulse = 1.0;
                    if (_PulseSpeed > 0.0) pulse = 0.8 + 0.2 * sin(_Time.y * _PulseSpeed);
                    studioMask = distFade * radialFade * pulse;
                #endif

                // Grids
                float3 weights = abs(normal);
                weights = pow(weights, _TriplanarSharpness);
                weights /= (weights.x + weights.y + weights.z + 0.001);

                float gridX = GridFactor(pos.zy, _GridScale, _GridThickness);
                float gridY = GridFactor(pos.xz, _GridScale, _GridThickness);
                float gridZ = GridFactor(pos.xy, _GridScale, _GridThickness);
                float gridMix = gridX * weights.x + gridY * weights.y + gridZ * weights.z;
                gridMix *= _GridOpacity * studioMask;

                float subGridMix = 0;
                #if defined(_SUBGRID)
                    float subGridX = GridFactor(pos.zy, _SubGridScale, _SubGridThickness);
                    float subGridY = GridFactor(pos.xz, _SubGridScale, _SubGridThickness);
                    float subGridZ = GridFactor(pos.xy, _SubGridScale, _SubGridThickness);
                    subGridMix = subGridX * weights.x + subGridY * weights.y + subGridZ * weights.z;
                    subGridMix *= _SubGridOpacity * studioMask;
                #endif

                // Reflection
                float3 reflectDir = reflect(-viewDir, normal);

                #if defined(_REFLECTION_PROBE_BOX_PROJECTION)
                    reflectDir = BoxProjectedCubemapDirection(reflectDir, pos, unity_SpecCube0_ProbePosition, unity_SpecCube0_BoxMin, unity_SpecCube0_BoxMax);
                #endif

                float3 reflectionColor = GlossyEnvironmentReflection(reflectDir, _ReflectionRoughness, 1.0);

                // Fresnel
                float fresnel = 1.0;
                #if defined(_ADD_FRESNEL)
                    float NdotV = saturate(dot(normal, viewDir));
                    fresnel = pow(1.0 - NdotV, 4.0);
                #endif
                
                reflectionColor *= _ReflectionStrength * fresnel;

                // Render
                float3 mainGridNeon = _GridColor.rgb * _GridEmission;
                float3 subGridNeon = _SubGridColor.rgb * _SubGridEmission;

                half3 finalColor = _BaseColor.rgb + reflectionColor;

                #if defined(_SUBGRID)
                    finalColor = lerp(finalColor, subGridNeon, subGridMix);
                #endif
                finalColor = lerp(finalColor, mainGridNeon, gridMix);

                // Lighting
                Light mainLight = GetMainLight();
                float NdotL = saturate(dot(normal, mainLight.direction));
                float shadow = mainLight.shadowAttenuation; 
                
                float totalGridMask = saturate(gridMix + subGridMix);

                float3 backgroundLit = finalColor * (NdotL * mainLight.distanceAttenuation * shadow + 0.05);
                
                float3 result = lerp(backgroundLit, finalColor, totalGridMask);
                
                return half4(result, 1.0);
            }
            ENDHLSL
        }
    }
}