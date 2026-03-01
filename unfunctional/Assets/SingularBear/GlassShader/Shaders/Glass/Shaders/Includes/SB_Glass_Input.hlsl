#ifndef SB_GLASS_INPUT_INCLUDED
#define SB_GLASS_INPUT_INCLUDED

#define SB_PI 3.14159265359
#define SB_TWO_PI 6.28318530718
#define SB_INV_PI 0.31830988618

CBUFFER_START(UnityPerMaterial)
    half4 _BaseColor;
    half _Opacity;
    half _Brightness;
    half _Contrast;
    half _Smoothness;
    half _IOR;
    half _Metallic;
    half _MatteFinish;
    half _PolishLevel;
    half _MicroRoughness;
    half _InteriorFade;
    half _BackfaceDarken;
    half _DepthFadeDistance;
    
    half _RefractionStrength;
    half _RefractionType;
    half _ChromaticDispersion;
    float4 _ThicknessMap_ST;
    half _ThicknessScale;
    
    half _ReflectionStrength;
    half _ReflectionBlur;
    half4 _ReflectionTint;
    half _ReflectionFresnel;
    
    half _FresnelPower;
    half _FresnelStrength;
    half _FresnelBias;
    half _FresnelScale;
    half4 _FresnelColor;
    half4 _FresnelColorInner;
    half _FresnelInvert;
    half _FresnelRimOnly;
    half _FresnelAffectsAlpha;
    
    half4 _SpecularColor;
    half _SpecularPower;
    half _SpecularStrength;
    half _SpecularMode;
    half _Anisotropy;
    half _SpecularToonCutoff;
    half _SpecularToonSmoothness;
    half _ClearcoatStrength;
    half _ClearcoatSmoothness;
    half _AdditionalLightsIntensity;
    half _AdditionalSpecularStrength;
    
    half4 _EdgeGlowColor;
    half _EdgeGlowPower;
    half _EdgeGlowStrength;
    half _InnerGlowStrength;
    half4 _InnerGlowColor;
    
    half4 _TranslucencyColor;
    half _TranslucencyStrength;
    half _TranslucencyNormalDistortion;
    half _TranslucencyScattering;
    half _TranslucencyDirect;
    half _TranslucencyAmbient;
    half _TranslucencyShadow;
    
    half _FresnelMainLightScale;
    half _FresnelMainLightSaturation;
    half _FresnelAdditionalLightScale;
    half _FresnelAdditionalLightSaturation;
    
    half _BlurStrength;
    half _BlurQuality;
    half _BlurSamples;
    half _BlurRadius;
    
    half4 _AbsorptionColor;
    half _AbsorptionDensity;
    
    half4 _InteriorFogColor;
    half _InteriorFogDensity;
    
    half _IridescenceStrength;
    half _IridescenceScale;
    half _IridescenceShift;
    half _IridescenceSpeed;
    half4 _IridescenceTint;
    
    half4 _TransmissionColor;
    half _TransmissionStrength;
    half _TransmissionDistortion;
    half _TransmissionPower;
    
    half4 _CausticsColor;
    half _CausticsStrength;
    half _CausticsScale;
    half _CausticsSpeed;
    float4 _CausticsMap_ST;
    
    float4 _NormalMap_ST;
    half _NormalStrength;
    half _DetailNormalStrength;
    half _DetailNormalTiling;
    
    half _WeatheringAmount;
    half _WeatheringGradient;
    half _WeatheringEdge;
    half4 _WeatheringColor;
    half _WeatheringRoughness;
    
    half4 _DirtColor;
    half _DirtAmount;
    half _DirtRoughness;
    half _GrimeEdgeAmount;
    half _GrimeGradient;
    half _DustAmount;
    half4 _DustColor;
    
    half _SmudgeStrength;
    half _SmudgeTiling;
    half _ScratchStrength;
    half _ScratchTiling;
    half _WaterStainStrength;
    
    float4 _DamageMask_ST;
    half _DamageProgression;
    half _CrackWidth;
    half _CrackDepth;
    half4 _CrackColor;
    half _CrackEmission;
    half _ShatterDistortion;
    half _ProceduralCracks;
    half _ProceduralCrackDensity;
    half _ProceduralCrackSeed;
    
    half _RainStrength;
    half _RainSpeed;
    half _RainTiling;
    half _RainDroplets;
    half _RainStreaks;
    float4 _RainNormalMap_ST;
    
    half _MagnifyStrength;
    half _MagnifyRadius;
    float4 _MagnifyCenter;
    half _MagnifyFalloff;
    half _BarrelDistortion;
    half _WaveAmplitude;
    half _WaveFrequency;
    half _WaveSpeed;
    half _SwirlStrength;
    half _SwirlRadius;
    half _PixelateAmount;
    
    half _DitherStrength;
    half _DitherScale;
CBUFFER_END

TEXTURE2D(_ThicknessMap);       SAMPLER(sampler_ThicknessMap);
TEXTURECUBE(_CubeMap);          SAMPLER(sampler_CubeMap);
TEXTURE2D(_NormalMap);          SAMPLER(sampler_NormalMap);
TEXTURE2D(_DetailNormalMap);    SAMPLER(sampler_DetailNormalMap);
TEXTURE2D(_CausticsMap);        SAMPLER(sampler_CausticsMap);
TEXTURE2D(_DirtMap);            SAMPLER(sampler_DirtMap);
TEXTURE2D(_SmudgeMap);          SAMPLER(sampler_SmudgeMap);
TEXTURE2D(_ScratchMap);         SAMPLER(sampler_ScratchMap);
TEXTURE2D(_WaterStainMap);      SAMPLER(sampler_WaterStainMap);
TEXTURE2D(_DamageMask);         SAMPLER(sampler_DamageMask);
TEXTURE2D(_CrackNormalMap);     SAMPLER(sampler_CrackNormalMap);
TEXTURE2D(_RainNormalMap);      SAMPLER(sampler_RainNormalMap);

struct SB_Attributes
{
    float4 positionOS   : POSITION;
    float3 normalOS     : NORMAL;
    float4 tangentOS    : TANGENT;
    float2 uv           : TEXCOORD0;
    float2 uv2          : TEXCOORD1;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct SB_Varyings
{
    float4 positionCS   : SV_POSITION;
    float3 positionWS   : TEXCOORD0;
    float3 normalWS     : TEXCOORD1;
    float4 tangentWS    : TEXCOORD2;
    float3 viewDirWS    : TEXCOORD3;
    float2 uv           : TEXCOORD4;
    float2 uv2          : TEXCOORD5;
    float4 screenPos    : TEXCOORD6;
    half fogFactor      : TEXCOORD7;
    UNITY_VERTEX_INPUT_INSTANCE_ID
    UNITY_VERTEX_OUTPUT_STEREO
};

struct SB_VaryingsMinimal
{
    float4 positionCS   : SV_POSITION;
    UNITY_VERTEX_INPUT_INSTANCE_ID
    UNITY_VERTEX_OUTPUT_STEREO
};

struct SB_VaryingsDepthNormals
{
    float4 positionCS   : SV_POSITION;
    float3 normalWS     : TEXCOORD0;
    UNITY_VERTEX_INPUT_INSTANCE_ID
    UNITY_VERTEX_OUTPUT_STEREO
};

#endif
