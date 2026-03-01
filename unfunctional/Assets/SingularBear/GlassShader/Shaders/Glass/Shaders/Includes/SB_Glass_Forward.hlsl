#ifndef SB_GLASS_FORWARD_INCLUDED
#define SB_GLASS_FORWARD_INCLUDED

// Triplanar helpers - only compiled if needed
#if defined(_WEATHERING) || defined(_DIRT) || defined(_RAIN)
float3 SB_GetTriplanarWeights(float3 normalWS)
{
    float3 weights = abs(normalWS);
    weights = pow(max(weights, 0.0001), 4.0);
    weights /= (weights.x + weights.y + weights.z + 0.001);
    return weights;
}
#endif

#if defined(_DIRT)
half4 SB_SampleTriplanar(TEXTURE2D_PARAM(tex, samp), float3 posWS, float3 weights, float tiling)
{
    half4 texX = SAMPLE_TEXTURE2D(tex, samp, posWS.zy * tiling);
    half4 texY = SAMPLE_TEXTURE2D(tex, samp, posWS.xz * tiling);
    half4 texZ = SAMPLE_TEXTURE2D(tex, samp, posWS.xy * tiling);
    return texX * weights.x + texY * weights.y + texZ * weights.z;
}
#endif

SB_Varyings SB_VertexForward(SB_Attributes input)
{
    SB_Varyings output = (SB_Varyings)0;
    
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_TRANSFER_INSTANCE_ID(input, output);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
    
    VertexPositionInputs posInputs = GetVertexPositionInputs(input.positionOS.xyz);
    VertexNormalInputs normalInputs = GetVertexNormalInputs(input.normalOS, input.tangentOS);
    
    output.positionCS = posInputs.positionCS;
    output.positionWS = posInputs.positionWS;
    output.normalWS = normalInputs.normalWS;
    output.tangentWS = float4(normalInputs.tangentWS, input.tangentOS.w);
    output.viewDirWS = GetWorldSpaceViewDir(posInputs.positionWS);
    output.uv = input.uv;
    output.uv2 = input.uv2;
    output.screenPos = ComputeScreenPos(posInputs.positionCS);
    output.fogFactor = ComputeFogFactor(posInputs.positionCS.z);
    
    return output;
}

half4 SB_FragmentForward(SB_Varyings input, half facing : VFACE) : SV_Target
{
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
    
    float time = _Time.y;
    bool isBackface = facing < 0;
    
    // ===========================================
    // HIDE INTERIOR - Only if enabled
    // ===========================================
    #if defined(_HIDE_INTERIOR)
    {
        if (isBackface)
        {
            float3 viewDirNorm = normalize(input.viewDirWS);
            float3 normalNorm = normalize(input.normalWS);
            float backfaceFade = saturate(dot(viewDirNorm, -normalNorm));
            backfaceFade = pow(backfaceFade, 2.0) * _InteriorFade;
            if (backfaceFade < 0.05)
                discard;
        }
    }
    #endif
    
    // ===========================================
    // BASE SETUP - Always needed
    // ===========================================
    float3 normalWS = normalize(input.normalWS);
    if (isBackface) normalWS = -normalWS;
    
    float3 viewDirWS = normalize(input.viewDirWS);
    float2 screenUV = input.screenPos.xy / input.screenPos.w;
    half NdotV = saturate(dot(normalWS, viewDirWS));
    
    // TBN - Only if normal mapping or rain
    #if defined(_NORMALMAP) || defined(_RAIN)
    float3 tangentWS = normalize(input.tangentWS.xyz);
    float3 bitangentWS = cross(normalWS, tangentWS) * input.tangentWS.w;
    float3x3 TBN = float3x3(tangentWS, bitangentWS, normalWS);
    #endif
    
    // Triplanar weights - Only if weathering/dirt/rain
    #if defined(_WEATHERING) || defined(_DIRT) || defined(_RAIN)
    float3 triWeights = SB_GetTriplanarWeights(normalWS);
    #endif
    
    // ===========================================
    // THICKNESS - Only if features need it
    // ===========================================
    #if defined(_REFRACTION) || defined(_TRANSMISSION) || defined(_ABSORPTION) || defined(_INTERIOR_FOG) || defined(_TRANSLUCENCY)
    half thickness = SAMPLE_TEXTURE2D(_ThicknessMap, sampler_ThicknessMap, input.uv).r * _ThicknessScale;
    #else
    half thickness = 0.0;
    #endif
    
    // ===========================================
    // NORMAL MAPPING - Only if enabled
    // ===========================================
    float3 finalNormalWS = normalWS;
    
    #if defined(_NORMALMAP)
    {
        half3 normalTS = UnpackNormalScale(
            SAMPLE_TEXTURE2D(_NormalMap, sampler_NormalMap, input.uv * _NormalMap_ST.xy + _NormalMap_ST.zw),
            _NormalStrength
        );
        
        #if defined(_DETAIL_NORMAL)
        {
            float2 detailUV = input.uv * _DetailNormalTiling;
            half3 detailNormal = UnpackNormalScale(
                SAMPLE_TEXTURE2D(_DetailNormalMap, sampler_DetailNormalMap, detailUV),
                _DetailNormalStrength
            );
            normalTS = normalize(half3(normalTS.xy + detailNormal.xy, normalTS.z));
        }
        #endif
        
        finalNormalWS = normalize(mul(normalTS, TBN));
    }
    #endif
    
    // ===========================================
    // RAIN - Only if enabled (triplanar)
    // ===========================================
    #if defined(_RAIN)
    {
        float rainTiling = _RainTiling * 0.5;
        
        float2 rainUV_X = input.positionWS.zy * rainTiling; rainUV_X.y -= time * _RainSpeed;
        float2 rainUV_Y = input.positionWS.xz * rainTiling;
        float2 rainUV_Z = input.positionWS.xy * rainTiling; rainUV_Z.y -= time * _RainSpeed;
        
        half3 rainNormal_X = UnpackNormalScale(SAMPLE_TEXTURE2D(_RainNormalMap, sampler_RainNormalMap, rainUV_X), _RainStrength);
        half3 rainNormal_Y = UnpackNormalScale(SAMPLE_TEXTURE2D(_RainNormalMap, sampler_RainNormalMap, rainUV_Y), _RainStrength * 0.2);
        half3 rainNormal_Z = UnpackNormalScale(SAMPLE_TEXTURE2D(_RainNormalMap, sampler_RainNormalMap, rainUV_Z), _RainStrength);
        
        // Droplets layer
        float2 dropUV_X = input.positionWS.zy * rainTiling * 2.0; dropUV_X.y -= time * _RainSpeed * 0.6;
        float2 dropUV_Z = input.positionWS.xy * rainTiling * 2.0; dropUV_Z.y -= time * _RainSpeed * 0.6;
        half3 dropNormal_X = UnpackNormalScale(SAMPLE_TEXTURE2D(_RainNormalMap, sampler_RainNormalMap, dropUV_X), _RainStrength * _RainDroplets);
        half3 dropNormal_Z = UnpackNormalScale(SAMPLE_TEXTURE2D(_RainNormalMap, sampler_RainNormalMap, dropUV_Z), _RainStrength * _RainDroplets);
        rainNormal_X.xy += dropNormal_X.xy;
        rainNormal_Z.xy += dropNormal_Z.xy;
        
        // Streaks layer
        float2 streakUV_X = input.positionWS.zy * float2(rainTiling, rainTiling * 4.0); streakUV_X.y -= time * _RainSpeed * 2.0;
        float2 streakUV_Z = input.positionWS.xy * float2(rainTiling, rainTiling * 4.0); streakUV_Z.y -= time * _RainSpeed * 2.0;
        half3 streakNormal_X = UnpackNormalScale(SAMPLE_TEXTURE2D(_RainNormalMap, sampler_RainNormalMap, streakUV_X), _RainStrength * _RainStreaks);
        half3 streakNormal_Z = UnpackNormalScale(SAMPLE_TEXTURE2D(_RainNormalMap, sampler_RainNormalMap, streakUV_Z), _RainStrength * _RainStreaks);
        rainNormal_X.xy += streakNormal_X.xy;
        rainNormal_Z.xy += streakNormal_Z.xy;
        
        half3 combinedRain = normalize(half3(
            rainNormal_X.xy * triWeights.x + rainNormal_Y.xy * triWeights.y + rainNormal_Z.xy * triWeights.z,
            1.0
        ));
        
        finalNormalWS = normalize(mul(combinedRain, TBN));
    }
    #endif
    
    // ===========================================
    // SMOOTHNESS
    // ===========================================
    half smoothness = _Smoothness * _PolishLevel;
    smoothness = saturate(smoothness - _MicroRoughness);
    if (_MatteFinish > 0.5) smoothness *= 0.1;
    
    // ===========================================
    // DEPTH FADE - Only if refraction enabled
    // ===========================================
    #if defined(_REFRACTION)
    float rawDepth = SampleSceneDepth(screenUV);
    float sceneDepth = LinearEyeDepth(rawDepth, _ZBufferParams);
    float surfaceDepth = LinearEyeDepth(input.positionCS.z / input.positionCS.w, _ZBufferParams);
    half depthFade = saturate((sceneDepth - surfaceDepth) / max(_DepthFadeDistance, 0.001));
    #else
    half depthFade = 1.0;
    #endif
    
    // ===========================================
    // WEATHERING - Only if enabled
    // ===========================================
    half3 dirtOverlay = half3(0, 0, 0);
    half dirtOpacity = 0.0;
    
    #if defined(_WEATHERING)
    {
        float weatherScale = 3.0;
        float noiseX = SB_WeatherNoise(input.positionWS.zy * weatherScale);
        float noiseY = SB_WeatherNoise(input.positionWS.xz * weatherScale);
        float noiseZ = SB_WeatherNoise(input.positionWS.xy * weatherScale);
        float weatherNoise = noiseX * triWeights.x + noiseY * triWeights.y + noiseZ * triWeights.z;
        
        float verticalFactor = saturate(input.positionWS.y * _WeatheringGradient + 0.5);
        float edgeFactor = pow(1.0 - NdotV, 2.0) * _WeatheringEdge;
        float weatherAmount = _WeatheringAmount * (weatherNoise * 0.5 + 0.5) * (verticalFactor + edgeFactor);
        weatherAmount = saturate(weatherAmount);
        
        dirtOverlay += _WeatheringColor.rgb * weatherAmount * 0.5;
        dirtOpacity += weatherAmount * 0.6;
        smoothness = saturate(smoothness - _WeatheringRoughness * weatherAmount);
    }
    #endif
    
    // ===========================================
    // DIRT - Only if enabled
    // ===========================================
    #if defined(_DIRT)
    {
        half4 dirtSample = SB_SampleTriplanar(TEXTURE2D_ARGS(_DirtMap, sampler_DirtMap), input.positionWS, triWeights, 0.5);
        float grimeFactor = pow(1.0 - NdotV, 3.0) * _GrimeEdgeAmount;
        float gravityGrime = saturate(1.0 - input.positionWS.y * 0.5) * _GrimeGradient;
        float totalDirt = saturate(dirtSample.r * _DirtAmount + grimeFactor + gravityGrime);
        
        dirtOverlay += _DirtColor.rgb * totalDirt;
        dirtOpacity += totalDirt * 0.8;
        smoothness = saturate(smoothness - _DirtRoughness * totalDirt);
        
        float dustFactor = saturate(normalWS.y) * _DustAmount;
        dirtOverlay += _DustColor.rgb * dustFactor * 0.5;
        dirtOpacity += dustFactor * 0.4;
    }
    #endif
    
    // ===========================================
    // IMPERFECTIONS - Only if enabled
    // ===========================================
    #if defined(_IMPERFECTIONS)
    {
        half smudge = SAMPLE_TEXTURE2D(_SmudgeMap, sampler_SmudgeMap, input.uv * _SmudgeTiling).r * _SmudgeStrength;
        half scratch = SAMPLE_TEXTURE2D(_ScratchMap, sampler_ScratchMap, input.uv * _ScratchTiling).r * _ScratchStrength;
        smoothness = saturate(smoothness - smudge * 0.3 - scratch * 0.1);
        
        float2 scratchGrad;
        scratchGrad.x = SAMPLE_TEXTURE2D(_ScratchMap, sampler_ScratchMap, input.uv * _ScratchTiling + float2(0.01, 0)).r - scratch;
        scratchGrad.y = SAMPLE_TEXTURE2D(_ScratchMap, sampler_ScratchMap, input.uv * _ScratchTiling + float2(0, 0.01)).r - scratch;
        finalNormalWS = normalize(finalNormalWS + float3(scratchGrad * _ScratchStrength * 2.0, 0));
    }
    #endif
    
    // ===========================================
    // DAMAGE - Only if enabled
    // ===========================================
    half crackMask = 0.0;
    half crackEmission = 0.0;
    
    #if defined(_DAMAGE)
    {
        if (_ProceduralCracks > 0.5)
        {
            ProceduralCrackData cracks = SB_CalculateProceduralCracks(input.uv, _DamageProgression, _ProceduralCrackSeed);
            crackMask = cracks.crackMask;
            crackEmission = cracks.crackEdge * _CrackEmission;
            finalNormalWS = normalize(finalNormalWS + float3((cracks.crackNormal.xy - 0.5) * crackMask, 0));
        }
        else
        {
            float2 damageUV = input.uv * _DamageMask_ST.xy + _DamageMask_ST.zw;
            half4 damageSample = SAMPLE_TEXTURE2D(_DamageMask, sampler_DamageMask, damageUV);
            crackMask = damageSample.r * _DamageProgression;
            crackEmission = crackMask * _CrackEmission;
        }
        
        dirtOpacity += crackMask * 0.5;
        dirtOverlay = lerp(dirtOverlay, _CrackColor.rgb, crackMask * 0.7);
    }
    #endif
    
    // Update NdotV with final normal
    NdotV = saturate(dot(finalNormalWS, viewDirWS));
    
    // ===========================================
    // REFRACTION UV
    // ===========================================
    float2 refractionUV = screenUV;
    
    #if defined(_DAMAGE)
    refractionUV += float2(crackMask * _ShatterDistortion * 0.1, 0);
    #endif
    
    // ===========================================
    // SCENE COLOR / REFRACTION / DISTORTION
    // ===========================================
    half3 sceneColor = half3(0, 0, 0);
    float2 sampleUV = screenUV;
    
    // Apply refraction offset if enabled
    #if defined(_REFRACTION)
    {
        float2 refractionOffset = float2(0, 0);
        
        if (_RefractionType < 0.5)
            refractionOffset = finalNormalWS.xy * _RefractionStrength * 0.1;
        else if (_RefractionType < 1.5)
        {
            float3 refracted = refract(-viewDirWS, finalNormalWS, 1.0 / _IOR);
            refractionOffset = (refracted.xy - (-viewDirWS).xy) * _RefractionStrength * 0.1;
        }
        else
        {
            float bend = sqrt(1.0 - (1.0 - NdotV * NdotV) / (_IOR * _IOR));
            refractionOffset = finalNormalWS.xy * (1.0 - bend) * _RefractionStrength * 0.5;
        }
        
        refractionOffset *= depthFade;
        sampleUV = refractionUV + refractionOffset;
    }
    #endif
    
    // Apply distortion effects (works with or without refraction)
    #if defined(_DISTORTION)
    sampleUV = SB_ApplyDistortions(sampleUV, time);
    #endif
    
    sampleUV = clamp(sampleUV, 0.001, 0.999);
    
    // Sample scene color
    #if defined(_CHROMATIC_DISPERSION) && defined(_REFRACTION)
    {
        float2 refractionOffset = sampleUV - screenUV;
        float2 redOffset = refractionOffset * (1.0 + _ChromaticDispersion);
        float2 blueOffset = refractionOffset * (1.0 - _ChromaticDispersion);
        sceneColor.r = SampleSceneColor(screenUV + redOffset).r;
        sceneColor.g = SampleSceneColor(sampleUV).g;
        sceneColor.b = SampleSceneColor(screenUV + blueOffset).b;
    }
    #elif defined(_BLUR_REFRACTION)
    {
        float2 texelSize = float2(1.0 / _ScreenParams.x, 1.0 / _ScreenParams.y);
        sceneColor = SB_FrostedGlassBlur(sampleUV, texelSize, _BlurStrength, _BlurRadius, (int)_BlurQuality);
    }
    #else
    {
        sceneColor = SampleSceneColor(sampleUV).rgb;
    }
    #endif
    
    // ===========================================
    // ABSORPTION - Only if enabled
    // ===========================================
    #if defined(_ABSORPTION)
    {
        half3 absorption = exp(-_AbsorptionColor.rgb * thickness * _AbsorptionDensity);
        sceneColor *= absorption;
    }
    #endif
    
    // ===========================================
    // INTERIOR FOG - Only if enabled
    // ===========================================
    #if defined(_INTERIOR_FOG)
    {
        half fogAmount = 1.0 - exp(-_InteriorFogDensity * thickness * 3.0);
        sceneColor = lerp(sceneColor, _InteriorFogColor.rgb, saturate(fogAmount));
    }
    #endif
    
    // ===========================================
    // TRANSLUCENCY - Only if enabled
    // ===========================================
    #if defined(_TRANSLUCENCY)
    {
        Light mainLight = GetMainLight();
        float3 H = normalize(mainLight.direction + normalWS * _TranslucencyNormalDistortion);
        float VdotH = pow(saturate(dot(viewDirWS, -H)), _TranslucencyScattering);
        half3 translucency = _TranslucencyColor.rgb * VdotH * _TranslucencyStrength;
        translucency *= mainLight.color * _TranslucencyDirect * (1.0 - thickness);
        sceneColor += translucency;
    }
    #endif
    
    // ===========================================
    // FRESNEL - Only if enabled
    // ===========================================
    half fresnelFactor = 0.0;
    half3 fresnelColor = half3(0, 0, 0);
    
    #if defined(_FRESNEL)
    {
        half fresnel = pow(1.0 - NdotV, _FresnelPower);
        fresnel = fresnel * _FresnelScale + _FresnelBias;
        if (_FresnelInvert > 0.5) fresnel = 1.0 - fresnel;
        fresnel = saturate(fresnel) * _FresnelStrength;
        if (_FresnelRimOnly > 0.5) fresnel *= pow(1.0 - NdotV, 2.0);
        fresnelFactor = fresnel;
        fresnelColor = lerp(_FresnelColorInner.rgb, _FresnelColor.rgb, fresnel);
    }
    #endif
    
    // ===========================================
    // SPECULAR - Only if enabled
    // ===========================================
    half3 specularColor = half3(0, 0, 0);
    
    #if defined(_SPECULAR)
    {
        if (_MatteFinish < 0.5)
        {
            Light mainLight = GetMainLight();
            float3 lightDir = normalize(mainLight.direction);
            float3 halfDir = normalize(viewDirWS + lightDir);
            half NdotH = saturate(dot(finalNormalWS, halfDir));
            half NdotL = saturate(dot(finalNormalWS, lightDir));
            
            half spec = 0.0;
            
            // Blinn-Phong
            if (_SpecularMode < 0.5)
            {
                spec = pow(NdotH, _SpecularPower) * NdotL;
            }
            // GGX
            else if (_SpecularMode < 1.5)
            {
                half roughness = 1.0 / (_SpecularPower * 0.01 + 0.01);
                half a2 = roughness * roughness;
                half denom = NdotH * NdotH * (a2 - 1.0) + 1.0;
                spec = (a2 / (SB_PI * denom * denom + 0.0001)) * NdotL;
            }
            // Anisotropic
            else if (_SpecularMode < 2.5)
            {
                float3 tangentWS = normalize(input.tangentWS.xyz);
                float3 bitangentWS = cross(finalNormalWS, tangentWS) * input.tangentWS.w;
                
                half TdotH = dot(tangentWS, halfDir);
                half BdotH = dot(bitangentWS, halfDir);
                
                half roughness = 1.0 - smoothness;
                half at = max(roughness * (1.0 + _Anisotropy), 0.001);
                half ab = max(roughness * (1.0 - _Anisotropy), 0.001);
                
                half denom = (TdotH * TdotH) / (at * at) + (BdotH * BdotH) / (ab * ab) + NdotH * NdotH;
                spec = saturate(1.0 / (SB_PI * at * ab * denom * denom + 0.0001)) * NdotL;
            }
            // Toon
            else
            {
                half rawSpec = pow(NdotH, _SpecularPower);
                spec = smoothstep(_SpecularToonCutoff, _SpecularToonCutoff + max(_SpecularToonSmoothness, 0.001), rawSpec) * NdotL;
            }
            
            specularColor = spec * _SpecularColor.rgb * mainLight.color * _SpecularStrength;
            
            // Clearcoat
            if (_ClearcoatStrength > 0.001)
            {
                half ccRough = 1.0 - _ClearcoatSmoothness;
                half ccA2 = ccRough * ccRough;
                half ccDenom = NdotH * NdotH * (ccA2 - 1.0) + 1.0;
                half ccD = ccA2 / (SB_PI * ccDenom * ccDenom + 0.0001);
                specularColor += ccD * NdotL * _ClearcoatStrength * mainLight.color * 0.25;
            }
            
            // ===========================================
            // ADDITIONAL LIGHTS SPECULAR
            // ===========================================
            #if defined(_ADDITIONAL_LIGHTS) && defined(_ADDITIONAL_LIGHTS_ON)
            {
                uint lightCount = GetAdditionalLightsCount();
                
                for (uint i = 0u; i < lightCount; ++i)
                {
                    Light addLight = GetAdditionalLight(i, input.positionWS);
                    float3 addLightDir = normalize(addLight.direction);
                    half3 addLightColor = addLight.color * addLight.distanceAttenuation * _AdditionalLightsIntensity;
                    
                    #if defined(_ADDITIONAL_LIGHT_SHADOWS)
                        addLightColor *= addLight.shadowAttenuation;
                    #endif
                    
                    float3 addHalfDir = normalize(viewDirWS + addLightDir);
                    half addNdotH = saturate(dot(finalNormalWS, addHalfDir));
                    half addNdotL = saturate(dot(finalNormalWS, addLightDir));
                    
                    half addSpec = 0.0;
                    if (_SpecularMode < 1.5) // Blinn-Phong or GGX
                    {
                        half roughness = 1.0 / (_SpecularPower * 0.01 + 0.01);
                        half a2 = roughness * roughness;
                        half denom = addNdotH * addNdotH * (a2 - 1.0) + 1.0;
                        addSpec = (a2 / (SB_PI * denom * denom + 0.0001)) * addNdotL;
                    }
                    else // Toon
                    {
                        half rawSpec = pow(addNdotH, _SpecularPower);
                        addSpec = smoothstep(_SpecularToonCutoff, _SpecularToonCutoff + 0.1, rawSpec) * addNdotL;
                    }
                    
                    specularColor += addSpec * _SpecularColor.rgb * addLightColor * _AdditionalSpecularStrength;
                }
            }
            #endif
        }
    }
    #endif
    
    // ===========================================
    // EDGE GLOW - Only if enabled
    // ===========================================
    half3 edgeGlowColor = half3(0, 0, 0);
    
    #if defined(_EDGE_GLOW)
    {
        half edgeFactor = pow(1.0 - NdotV, _EdgeGlowPower);
        edgeGlowColor = _EdgeGlowColor.rgb * edgeFactor * _EdgeGlowStrength;
        if (_InnerGlowStrength > 0.001)
            edgeGlowColor += _InnerGlowColor.rgb * NdotV * _InnerGlowStrength;
    }
    #endif
    
    // ===========================================
    // TRANSMISSION - Only if enabled
    // ===========================================
    half3 transmissionColor = half3(0, 0, 0);
    
    #if defined(_TRANSMISSION)
    {
        Light mainLight = GetMainLight();
        float3 transLightDir = mainLight.direction + finalNormalWS * _TransmissionDistortion;
        half transDot = pow(saturate(dot(-transLightDir, viewDirWS)), _TransmissionPower);
        transmissionColor = _TransmissionColor.rgb * transDot * _TransmissionStrength;
        transmissionColor *= mainLight.color * (1.0 - thickness * 0.5);
    }
    #endif
    
    // ===========================================
    // IRIDESCENCE - Only if enabled
    // ===========================================
    half3 iridescenceColor = half3(0, 0, 0);
    half iridescenceMask = 0.0;
    
    #if defined(_IRIDESCENCE)
    {
        half filmThickness = (1.0 - NdotV) * _IridescenceScale * 2.0 + time * _IridescenceSpeed;
        
        iridescenceColor.r = sin(filmThickness * 6.28318 + _IridescenceShift * 6.28318) * 0.5 + 0.5;
        iridescenceColor.g = sin(filmThickness * 6.28318 + _IridescenceShift * 6.28318 + 2.094) * 0.5 + 0.5;
        iridescenceColor.b = sin(filmThickness * 6.28318 + _IridescenceShift * 6.28318 + 4.188) * 0.5 + 0.5;
        iridescenceColor *= _IridescenceTint.rgb;
        iridescenceMask = pow(1.0 - NdotV, 1.5) * _IridescenceStrength;
    }
    #endif
    
    // ===========================================
    // CAUSTICS - Only if enabled
    // ===========================================
    half3 causticsColor = half3(0, 0, 0);
    
    #if defined(_CAUSTICS)
    {
        Light mainLight = GetMainLight();
        float2 causticsUV = input.positionWS.xz * _CausticsScale;
        float2 animOffset = float2(time * _CausticsSpeed, time * _CausticsSpeed * 0.7);
        
        half c1 = SAMPLE_TEXTURE2D(_CausticsMap, sampler_CausticsMap, causticsUV + animOffset).r;
        half c2 = SAMPLE_TEXTURE2D(_CausticsMap, sampler_CausticsMap, causticsUV * 0.8 - animOffset * 0.5).r;
        half caustics = c1 * c2 * 2.0;
        
        half NdotL = saturate(dot(finalNormalWS, mainLight.direction));
        causticsColor = _CausticsColor.rgb * caustics * _CausticsStrength * mainLight.color * NdotL;
    }
    #endif
    
    // ===========================================
    // REFLECTION - Only if enabled
    // ===========================================
    half3 envReflection = half3(0, 0, 0);
    
    #if defined(_REFLECTION)
    {
        if (_MatteFinish < 0.5)
        {
            float3 reflectDir = reflect(-viewDirWS, finalNormalWS);
            half roughness = 1.0 - smoothness;
            half totalBlur = _ReflectionBlur + roughness * 4.0;
            
            envReflection = SAMPLE_TEXTURECUBE_LOD(_CubeMap, sampler_CubeMap, reflectDir, totalBlur).rgb;
            envReflection *= _ReflectionTint.rgb;
            
            #if defined(_FRESNEL)
            envReflection *= lerp(1.0, fresnelFactor, _ReflectionFresnel);
            #endif
            
            envReflection *= _ReflectionStrength;
        }
    }
    #endif
    
    // ===========================================
    // FRESNEL LIGHTING - Only if enabled
    // ===========================================
    half3 fresnelLightingColor = half3(0, 0, 0);
    
    #if defined(_FRESNEL_LIGHTING)
    {
        half fresnelMask = pow(1.0 - NdotV, 3.0);
        Light mainLight = GetMainLight();
        half NdotL = saturate(dot(finalNormalWS, mainLight.direction));
        half3 mainContrib = mainLight.color * NdotL * _FresnelMainLightScale;
        mainContrib = lerp(Luminance(mainContrib).xxx, mainContrib, _FresnelMainLightSaturation);
        
        #if defined(_FRESNEL)
        fresnelLightingColor = mainContrib * fresnelMask * _FresnelColor.rgb;
        #else
        fresnelLightingColor = mainContrib * fresnelMask;
        #endif
    }
    #endif
    
    // ===========================================
    // FINAL COMPOSITION - GLASS LIGHTING
    // ===========================================
    
    // Get main light
    Light mainLight = GetMainLight();
    float3 mainLightDir = normalize(mainLight.direction);
    half3 mainLightColor = mainLight.color;
    
    // Shadow for main light
    half mainShadow = 1.0;
    #if defined(_MAIN_LIGHT_SHADOWS) || defined(_MAIN_LIGHT_SHADOWS_CASCADE)
    {
        float4 shadowCoord = TransformWorldToShadowCoord(input.positionWS);
        mainShadow = MainLightRealtimeShadow(shadowCoord);
    }
    #endif
    
    // ===========================================
    // SURFACE LIGHTING (for opaque glass)
    // Half-Lambert for softer look
    // ===========================================
    half NdotL = dot(finalNormalWS, mainLightDir);
    half halfLambert = saturate(NdotL * 0.5 + 0.5);
    halfLambert = halfLambert * halfLambert; // Square for softer falloff
    
    // Direct surface lighting
    half3 surfaceLighting = mainLightColor * halfLambert * mainShadow;
    
    // Surface receives light based on opacity
    half3 litSurfaceColor = _BaseColor.rgb * surfaceLighting;
    
    // ===========================================
    // LIGHT TRANSMISSION (stained glass effect)
    // Light passing through colored glass
    // ===========================================
    
    // How much light comes from behind
    half transmitDot = saturate(dot(-viewDirWS, mainLightDir));
    transmitDot = pow(transmitDot, 1.5); // Focus transmission
    
    // Also consider light from the lit side passing through
    half throughDot = saturate(-NdotL); // Negative NdotL = light from behind
    throughDot = pow(throughDot * 0.5 + 0.5, 2.0);
    
    // Combined transmission factor
    half transmissionFactor = max(transmitDot, throughDot * 0.7);
    
    // Light color filtered through glass (vitrail effect)
    // Square the color for more saturated result
    half3 filteredLight = mainLightColor * _BaseColor.rgb * _BaseColor.rgb;
    
    // Transmission amount: even opaque glass transmits SOME light
    half transmitAmount = (1.0 - _Opacity * 0.7); // 0.3 to 1.0 (never zero!)
    half3 transmittedLight = filteredLight * transmissionFactor * transmitAmount * 1.5; // Boosted!
    
    // Wrap lighting - soft light around edges
    half wrap = saturate(NdotL * 0.4 + 0.6);
    half3 wrapLight = mainLightColor * _BaseColor.rgb * wrap * 0.4; // Boosted
    
    // Edge glow from transmitted light
    half edgeTransmit = pow(1.0 - NdotV, 2.5) * transmissionFactor;
    half3 edgeGlowTransmit = filteredLight * edgeTransmit * 1.2; // Boosted
    
    // ===========================================
    // ADDITIONAL LIGHTS
    // ===========================================
    half3 additionalSurfaceLight = half3(0, 0, 0);
    half3 additionalTransmitLight = half3(0, 0, 0);
    
    #if defined(_ADDITIONAL_LIGHTS)
    {
        uint lightCount = GetAdditionalLightsCount();
        for (uint i = 0u; i < lightCount; ++i)
        {
            Light addLight = GetAdditionalLight(i, input.positionWS);
            float3 addLightDir = normalize(addLight.direction);
            half3 addLightColor = addLight.color * addLight.distanceAttenuation;
            
            #if defined(_ADDITIONAL_LIGHT_SHADOWS)
            addLightColor *= addLight.shadowAttenuation;
            #endif
            
            // Surface lighting from this light
            half addNdotL = dot(finalNormalWS, addLightDir);
            half addHalfLambert = saturate(addNdotL * 0.5 + 0.5);
            addHalfLambert = addHalfLambert * addHalfLambert;
            additionalSurfaceLight += addLightColor * addHalfLambert;
            
            // Transmission from this light
            half addTransmitDot = saturate(dot(-viewDirWS, addLightDir));
            addTransmitDot = pow(addTransmitDot, 1.5);
            half addThroughDot = saturate(-addNdotL);
            addThroughDot = pow(addThroughDot * 0.5 + 0.5, 2.0);
            half addTransmit = max(addTransmitDot, addThroughDot * 0.7);
            
            half3 addFilteredLight = addLightColor * _BaseColor.rgb * _BaseColor.rgb;
            half addTransmitAmount = (1.0 - _Opacity * 0.7);
            additionalTransmitLight += addFilteredLight * addTransmit * addTransmitAmount * 1.5;
            
            // Edge transmission
            additionalTransmitLight += addFilteredLight * pow(1.0 - NdotV, 2.5) * addTransmit * 0.8;
        }
    }
    #endif
    
    // Total surface lighting
    half3 totalSurfaceLight = surfaceLighting + additionalSurfaceLight;
    half3 totalLitSurface = _BaseColor.rgb * totalSurfaceLight;
    
    // Total transmitted light
    half3 totalTransmitted = transmittedLight + wrapLight + edgeGlowTransmit + additionalTransmitLight;
    
    // ===========================================
    // COMBINE SCENE + LIGHTING
    // ===========================================
    
    // Apply smoothness to scene clarity
    half3 frostedScene = sceneColor;
    if (smoothness < 0.9)
    {
        half frostAmount = 1.0 - smoothness;
        frostedScene = lerp(sceneColor, Luminance(sceneColor).xxx, frostAmount * 0.3);
    }
    
    // ===========================================
    // VITRAIL EFFECT - Glass color is ALWAYS visible
    // ===========================================
    half3 glassTint = _BaseColor.rgb;
    
    // Method 1: Multiply scene by glass color (darkens if scene is dark)
    half3 tintedSceneMult = frostedScene * glassTint;
    
    // Method 2: Add glass color overlay (ensures color is visible even on dark backgrounds)
    // Screen blend: 1 - (1 - a) * (1 - b) - brightens while adding color
    half3 tintedSceneScreen = 1.0 - (1.0 - frostedScene) * (1.0 - glassTint * 0.5);
    
    // Method 3: Lerp toward glass color based on opacity
    half3 tintedSceneLerp = lerp(frostedScene, glassTint, _Opacity * 0.6);
    
    // Combine methods based on scene brightness
    half sceneBrightness = Luminance(frostedScene);
    
    // Dark scene: use screen/lerp blend to show color
    // Bright scene: use multiply to tint
    half3 tintedScene = lerp(
        lerp(tintedSceneLerp, tintedSceneScreen, 0.5),  // Dark scene blend
        tintedSceneMult,                                  // Bright scene multiply
        saturate(sceneBrightness * 2.0)                   // Blend factor
    );
    
    // Always add some glass color (ensures visibility on ANY background)
    tintedScene += glassTint * (0.1 + _Opacity * 0.3);
    
    tintedScene *= _Brightness;
    tintedScene = (tintedScene - 0.5) * _Contrast + 0.5;
    
    // ===========================================
    // FINAL COMPOSITION
    // ===========================================
    
    // Transmitted light (from lights behind/through the glass)
    half3 transparentResult = tintedScene + totalTransmitted;
    
    // Lit surface (opaque glass receiving direct light)
    half3 opaqueResult = totalLitSurface + glassTint * 0.15; // Add base glass color
    
    // Blend: transparent shows tinted scene, opaque shows lit surface
    half3 finalColor = lerp(transparentResult, opaqueResult, _Opacity * 0.6);
    
    // IMPORTANT: Always blend in some glass color so it's never invisible
    finalColor = lerp(finalColor, glassTint, 0.1 + _Opacity * 0.15);
    
    #if defined(_IRIDESCENCE)
    finalColor = lerp(finalColor, finalColor * iridescenceColor * 2.0 + iridescenceColor * 0.3, iridescenceMask);
    #endif
    
    if (isBackface) finalColor *= (1.0 - _BackfaceDarken);
    
    #if defined(_REFLECTION)
    {
        #if defined(_FRESNEL)
        finalColor = lerp(finalColor, envReflection, fresnelFactor * _ReflectionStrength);
        #else
        finalColor = lerp(finalColor, envReflection, _ReflectionStrength * 0.3);
        #endif
    }
    #endif
    
    #if defined(_FRESNEL)
    finalColor += fresnelColor * fresnelFactor * 0.5;
    #endif
    
    #if defined(_FRESNEL_LIGHTING)
    finalColor += fresnelLightingColor;
    #endif
    
    #if defined(_SPECULAR)
    finalColor += specularColor;
    #endif
    
    #if defined(_TRANSMISSION)
    finalColor += transmissionColor;
    #endif
    
    #if defined(_EDGE_GLOW)
    finalColor += edgeGlowColor;
    #endif
    
    #if defined(_CAUSTICS)
    finalColor += causticsColor;
    #endif
    
    #if defined(_DAMAGE)
    finalColor += _CrackColor.rgb * crackEmission;
    #endif
    
    #if defined(_WEATHERING) || defined(_DIRT) || defined(_DAMAGE)
    finalColor = lerp(finalColor, dirtOverlay, saturate(dirtOpacity));
    #endif
    
    finalColor = MixFog(finalColor, input.fogFactor);
    
    // ===========================================
    // ALPHA - Based on Opacity and effects
    // ===========================================
    // Base alpha from Opacity (0 = transparent, 1 = opaque)
    half alpha = _Opacity;
    
    // Smoothness affects alpha - rougher glass is more visible
    alpha += (1.0 - smoothness) * 0.3;
    
    // Fresnel edge effect on alpha
    half edgeOpacity = pow(1.0 - NdotV, 2.0) * 0.4;
    alpha += edgeOpacity;
    
    #if defined(_FRESNEL)
    if (_FresnelAffectsAlpha > 0.001)
        alpha = lerp(alpha, alpha + fresnelFactor * 0.3, _FresnelAffectsAlpha);
    #endif
    
    // Dirt/damage adds opacity
    #if defined(_WEATHERING) || defined(_DIRT) || defined(_DAMAGE)
    alpha += dirtOpacity * 0.4;
    #endif
    
    // Thickness adds slight opacity
    #if defined(_REFRACTION) || defined(_TRANSMISSION) || defined(_ABSORPTION) || defined(_INTERIOR_FOG) || defined(_TRANSLUCENCY)
    alpha += thickness * 0.05;
    #endif
    
    // Backface slightly more opaque
    if (isBackface) alpha += 0.1 * (1.0 - _BackfaceDarken);
    
    alpha = saturate(alpha);
    
    #if defined(_DITHERING)
    {
        if (SB_ApplyDithering(alpha, input.positionCS.xy, _DitherStrength, _DitherScale))
            discard;
    }
    #endif
    
    return half4(finalColor, alpha);
}

#endif
