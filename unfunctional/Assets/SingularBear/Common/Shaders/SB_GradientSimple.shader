Shader "SingularBear/URP/GradientSimple"
{
    Properties
    {
        [Header(Lighting)]
        _Color("Main Tint", Color) = (1,1,1,1)
        _ShadowColor("Shadow Tint", Color) = (0.4, 0.4, 0.7, 1) 
        _ShadowStep("Shadow Hardness", Range(0, 1)) = 0.5
        _ShadowFeather("Shadow Softness", Range(0.01, 0.5)) = 0.2 

        [Header(Gradient Settings)]
        _GradientBottom("Gradient Bottom Color", Color) = (0.6, 0.6, 0.65, 1) 
        _GradientTop("Gradient Top Color", Color) = (1, 1, 1, 1)
        _GradientScale("Gradient Scale (Height)", Float) = 10.0
        _GradientOffset("Gradient Offset (Vertical)", Float) = 0.0
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" "Queue"="Geometry" }
        
        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
        
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _SHADOWS_SOFT
            #pragma multi_compile _ _ADDITIONAL_LIGHTS

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
                float3 positionOS : TEXCOORD2; 
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _Color;
                float4 _ShadowColor;
                float _ShadowStep;
                float _ShadowFeather;
                
                float4 _GradientBottom;
                float4 _GradientTop;
                float _GradientScale;
                float _GradientOffset;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output;
                VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs normalInput = GetVertexNormalInputs(input.normalOS);

                output.positionCS = vertexInput.positionCS;
                output.positionWS = vertexInput.positionWS;
                output.normalWS   = normalInput.normalWS;
                output.positionOS = input.positionOS.xyz; 
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float3 normalWS = normalize(input.normalWS);
                float gradientFactor = saturate((input.positionOS.y + _GradientOffset) / _GradientScale);
                half3 gradientColor = lerp(_GradientBottom.rgb, _GradientTop.rgb, gradientFactor);

                half3 albedo = _Color.rgb * gradientColor;

                Light mainLight = GetMainLight(TransformWorldToShadowCoord(input.positionWS));
                float NdotL = dot(normalWS, mainLight.direction);

                float shadowTerm = smoothstep(_ShadowStep - _ShadowFeather, _ShadowStep + _ShadowFeather, NdotL * mainLight.shadowAttenuation);
                float3 lighting = lerp(_ShadowColor.rgb, mainLight.color, shadowTerm);

                return half4(albedo * lighting, 1.0);
            }
            ENDHLSL
        }
    }
    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}