// Upgrade NOTE: upgraded instancing buffer 'TurishaderUIVHSUI' to new syntax.

// Made with Amplify Shader Editor v1.9.8.1
// Available at the Unity Asset Store - http://u3d.as/y3X 
Shader "Turishader/UI/VHSUI"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)

        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255

        _ColorMask ("Color Mask", Float) = 15

        [Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip ("Use Alpha Clip", Float) = 0

        [KeywordEnum(Screen,UV)] _Coords("Coords", Float) = 0
        [Header(Color)]_ColorMultiplier("ColorMultiplier", Float) = 1
        _Colorbleedingamount("Color bleeding amount", Range( 0 , 0.1)) = 0.01
        [Header(Mip level blur (requires mip level enabled on texture ))][IntRange]_MipLevel("MipLevel", Range( 0 , 10)) = 1
        _BlurAmount("BlurAmount", Range( 0 , 1)) = 0
        [Header(Grid)]_PixelGridAmount("PixelGridAmount", Range( 0 , 1)) = 0
        _PixelGridSize("PixelGridSize", Float) = 100
        [Header(Scanlines)]_ScanLinesAmount("ScanLinesAmount", Range( 0 , 1)) = 0
        _ScanLinesFrenquency("ScanLinesFrenquency", Float) = 100
        _ScanLinesSpeed("ScanLinesSpeed", Float) = 2
        [Header(Noise)]_NoiseAmount("NoiseAmount", Range( 0 , 1)) = 0
        [Header(Glitch)]_GlitchAmount("GlitchAmount", Range( 0 , 1)) = 0
        _GlitchTiling("GlitchTiling", Float) = 1
        _GlitchMinOpacity("GlitchMinOpacity", Range( 0 , 1)) = 0
        _GlitchMinColorMultiply("GlitchMinColorMultiply", Range( 0 , 1)) = 0
        _DistortionAmount("DistortionAmount", Float) = 0

    }

    SubShader
    {
		LOD 0

        Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" "PreviewType"="Plane" "CanUseSpriteAtlas"="True" }

        Stencil
        {
        	Ref [_Stencil]
        	ReadMask [_StencilReadMask]
        	WriteMask [_StencilWriteMask]
        	Comp [_StencilComp]
        	Pass [_StencilOp]
        }


        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend One OneMinusSrcAlpha
        ColorMask [_ColorMask]

        
        Pass
        {
            Name "Default"
        CGPROGRAM
            #define ASE_VERSION 19801

            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.5

            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP

            #include "UnityShaderVariables.cginc"
            #define ASE_NEEDS_FRAG_COLOR
            #pragma shader_feature_local _COORDS_SCREEN _COORDS_UV
            #pragma multi_compile_instancing


            struct appdata_t
            {
                float4 vertex   : POSITION;
                float4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                
            };

            struct v2f
            {
                float4 vertex   : SV_POSITION;
                fixed4 color    : COLOR;
                float2 texcoord  : TEXCOORD0;
                float4 worldPosition : TEXCOORD1;
                float4  mask : TEXCOORD2;
                UNITY_VERTEX_OUTPUT_STEREO
                float4 ase_texcoord3 : TEXCOORD3;
            };

            sampler2D _MainTex;
            fixed4 _Color;
            fixed4 _TextureSampleAdd;
            float4 _ClipRect;
            float4 _MainTex_ST;
            float _UIMaskSoftnessX;
            float _UIMaskSoftnessY;

            uniform float _DistortionAmount;
            uniform float _BlurAmount;
            uniform float _Colorbleedingamount;
            uniform float _NoiseAmount;
            UNITY_INSTANCING_BUFFER_START(TurishaderUIVHSUI)
            	UNITY_DEFINE_INSTANCED_PROP(float, _GlitchTiling)
#define _GlitchTiling_arr TurishaderUIVHSUI
            	UNITY_DEFINE_INSTANCED_PROP(float, _GlitchAmount)
#define _GlitchAmount_arr TurishaderUIVHSUI
            	UNITY_DEFINE_INSTANCED_PROP(float, _MipLevel)
#define _MipLevel_arr TurishaderUIVHSUI
            	UNITY_DEFINE_INSTANCED_PROP(float, _ColorMultiplier)
#define _ColorMultiplier_arr TurishaderUIVHSUI
            	UNITY_DEFINE_INSTANCED_PROP(float, _GlitchMinColorMultiply)
#define _GlitchMinColorMultiply_arr TurishaderUIVHSUI
            	UNITY_DEFINE_INSTANCED_PROP(float, _PixelGridSize)
#define _PixelGridSize_arr TurishaderUIVHSUI
            	UNITY_DEFINE_INSTANCED_PROP(float, _PixelGridAmount)
#define _PixelGridAmount_arr TurishaderUIVHSUI
            	UNITY_DEFINE_INSTANCED_PROP(float, _ScanLinesFrenquency)
#define _ScanLinesFrenquency_arr TurishaderUIVHSUI
            	UNITY_DEFINE_INSTANCED_PROP(float, _ScanLinesSpeed)
#define _ScanLinesSpeed_arr TurishaderUIVHSUI
            	UNITY_DEFINE_INSTANCED_PROP(float, _ScanLinesAmount)
#define _ScanLinesAmount_arr TurishaderUIVHSUI
            	UNITY_DEFINE_INSTANCED_PROP(float, _GlitchMinOpacity)
#define _GlitchMinOpacity_arr TurishaderUIVHSUI
            UNITY_INSTANCING_BUFFER_END(TurishaderUIVHSUI)
            float3 mod2D289( float3 x ) { return x - floor( x * ( 1.0 / 289.0 ) ) * 289.0; }
            float2 mod2D289( float2 x ) { return x - floor( x * ( 1.0 / 289.0 ) ) * 289.0; }
            float3 permute( float3 x ) { return mod2D289( ( ( x * 34.0 ) + 1.0 ) * x ); }
            float snoise( float2 v )
            {
            	const float4 C = float4( 0.211324865405187, 0.366025403784439, -0.577350269189626, 0.024390243902439 );
            	float2 i = floor( v + dot( v, C.yy ) );
            	float2 x0 = v - i + dot( i, C.xx );
            	float2 i1;
            	i1 = ( x0.x > x0.y ) ? float2( 1.0, 0.0 ) : float2( 0.0, 1.0 );
            	float4 x12 = x0.xyxy + C.xxzz;
            	x12.xy -= i1;
            	i = mod2D289( i );
            	float3 p = permute( permute( i.y + float3( 0.0, i1.y, 1.0 ) ) + i.x + float3( 0.0, i1.x, 1.0 ) );
            	float3 m = max( 0.5 - float3( dot( x0, x0 ), dot( x12.xy, x12.xy ), dot( x12.zw, x12.zw ) ), 0.0 );
            	m = m * m;
            	m = m * m;
            	float3 x = 2.0 * frac( p * C.www ) - 1.0;
            	float3 h = abs( x ) - 0.5;
            	float3 ox = floor( x + 0.5 );
            	float3 a0 = x - ox;
            	m *= 1.79284291400159 - 0.85373472095314 * ( a0 * a0 + h * h );
            	float3 g;
            	g.x = a0.x * x0.x + h.x * x0.y;
            	g.yz = a0.yz * x12.xz + h.yz * x12.yw;
            	return 130.0 * dot( m, g );
            }
            


            v2f vert(appdata_t v )
            {
                v2f OUT;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);

                float4 ase_positionCS = UnityObjectToClipPos( v.vertex );
                float4 screenPos = ComputeScreenPos( ase_positionCS );
                OUT.ase_texcoord3 = screenPos;
                

                v.vertex.xyz +=  float3( 0, 0, 0 ) ;

                float4 vPosition = UnityObjectToClipPos(v.vertex);
                OUT.worldPosition = v.vertex;
                OUT.vertex = vPosition;

                float2 pixelSize = vPosition.w;
                pixelSize /= float2(1, 1) * abs(mul((float2x2)UNITY_MATRIX_P, _ScreenParams.xy));

                float4 clampedRect = clamp(_ClipRect, -2e10, 2e10);
                float2 maskUV = (v.vertex.xy - clampedRect.xy) / (clampedRect.zw - clampedRect.xy);
                OUT.texcoord = v.texcoord;
                OUT.mask = float4(v.vertex.xy * 2 - clampedRect.xy - clampedRect.zw, 0.25 / (0.25 * half2(_UIMaskSoftnessX, _UIMaskSoftnessY) + abs(pixelSize.xy)));

                OUT.color = v.color * _Color;
                return OUT;
            }

            fixed4 frag(v2f IN ) : SV_Target
            {
                //Round up the alpha color coming from the interpolator (to 1.0/256.0 steps)
                //The incoming alpha could have numerical instability, which makes it very sensible to
                //HDR color transparency blend, when it blends with the world's texture.
                const half alphaPrecision = half(0xff);
                const half invAlphaPrecision = half(1.0/alphaPrecision);
                IN.color.a = round(IN.color.a * alphaPrecision)*invAlphaPrecision;

                float4 screenPos = IN.ase_texcoord3;
                float4 ase_positionSSNorm = screenPos / screenPos.w;
                ase_positionSSNorm.z = ( UNITY_NEAR_CLIP_VALUE >= 0 ) ? ase_positionSSNorm.z : ase_positionSSNorm.z * 0.5 + 0.5;
                float4 ase_positionSS_Center = float4( ase_positionSSNorm.xy * 2 - 1, 0, 0 );
                float2 appendResult52 = (float2(( ase_positionSS_Center.x * ( _ScreenParams.x / _ScreenParams.y ) ) , ase_positionSS_Center.y));
                float2 ScreenPos333 = appendResult52;
                float2 texCoord461 = IN.texcoord.xy * float2( 1,1 ) + float2( 0,0 );
                #if defined( _COORDS_SCREEN )
                float2 staticSwitch462 = ScreenPos333;
                #elif defined( _COORDS_UV )
                float2 staticSwitch462 = texCoord461;
                #else
                float2 staticSwitch462 = ScreenPos333;
                #endif
                float2 PatternUV463 = staticSwitch462;
                float2 break529 = PatternUV463;
                float _GlitchTiling_Instance = UNITY_ACCESS_INSTANCED_PROP(_GlitchTiling_arr, _GlitchTiling);
                float2 appendResult301 = (float2(( 0.0 * break529.x ) , ( break529.y * _GlitchTiling_Instance )));
                float mulTime306 = _Time.y * 3.0;
                float temp_output_2_0_g40 = 5.0;
                float simplePerlin2D304 = snoise( ( appendResult301 + ( round( ( mulTime306 * temp_output_2_0_g40 ) ) / temp_output_2_0_g40 ) )*100.0 );
                simplePerlin2D304 = simplePerlin2D304*0.5 + 0.5;
                float GlitchPattern460 = simplePerlin2D304;
                float _GlitchAmount_Instance = UNITY_ACCESS_INSTANCED_PROP(_GlitchAmount_arr, _GlitchAmount);
                float GlitchAmount444 = _GlitchAmount_Instance;
                float2 texCoord434 = IN.texcoord.xy * float2( 1,1 ) + float2( 0,0 );
                float2 appendResult437 = (float2(( ( (-1.0 + (GlitchPattern460 - 1.0) * (1.0 - -1.0) / (0.0 - 1.0)) * _DistortionAmount * GlitchAmount444 ) + texCoord434.x ) , texCoord434.y));
                float2 temp_output_8_0_g68 = appendResult437;
                float4 tex2DNode1_g68 = tex2D( _MainTex, temp_output_8_0_g68 );
                float _MipLevel_Instance = UNITY_ACCESS_INSTANCED_PROP(_MipLevel_arr, _MipLevel);
                float4 tex2DNode2_g68 = tex2Dlod( _MainTex, float4( temp_output_8_0_g68, 0, (float)(int)_MipLevel_Instance) );
                float temp_output_6_0_g68 = _BlurAmount;
                float4 lerpResult3_g68 = lerp( tex2DNode1_g68 , tex2DNode2_g68 , temp_output_6_0_g68);
                float4 break10_g68 = lerpResult3_g68;
                float lerpResult5_g68 = lerp( tex2DNode1_g68.a , tex2DNode2_g68.a , temp_output_6_0_g68);
                float temp_output_486_4 = lerpResult5_g68;
                float2 appendResult493 = (float2(_Colorbleedingamount , 0.0));
                float2 temp_output_8_0_g69 = ( appendResult437 + appendResult493 );
                float4 tex2DNode1_g69 = tex2D( _MainTex, temp_output_8_0_g69 );
                float4 tex2DNode2_g69 = tex2Dlod( _MainTex, float4( temp_output_8_0_g69, 0, (float)(int)_MipLevel_Instance) );
                float temp_output_6_0_g69 = _BlurAmount;
                float4 lerpResult3_g69 = lerp( tex2DNode1_g69 , tex2DNode2_g69 , temp_output_6_0_g69);
                float4 break10_g69 = lerpResult3_g69;
                float lerpResult5_g69 = lerp( tex2DNode1_g69.a , tex2DNode2_g69.a , temp_output_6_0_g69);
                float temp_output_487_4 = lerpResult5_g69;
                float2 appendResult494 = (float2(( _Colorbleedingamount * -1.0 ) , 0.0));
                float2 temp_output_8_0_g70 = ( appendResult437 + appendResult494 );
                float4 tex2DNode1_g70 = tex2D( _MainTex, temp_output_8_0_g70 );
                float4 tex2DNode2_g70 = tex2Dlod( _MainTex, float4( temp_output_8_0_g70, 0, (float)(int)_MipLevel_Instance) );
                float temp_output_6_0_g70 = _BlurAmount;
                float4 lerpResult3_g70 = lerp( tex2DNode1_g70 , tex2DNode2_g70 , temp_output_6_0_g70);
                float4 break10_g70 = lerpResult3_g70;
                float lerpResult5_g70 = lerp( tex2DNode1_g70.a , tex2DNode2_g70.a , temp_output_6_0_g70);
                float temp_output_488_4 = lerpResult5_g70;
                float4 appendResult490 = (float4(( IN.color.r * break10_g68.r * temp_output_486_4 ) , ( IN.color.g * break10_g69.g * temp_output_487_4 ) , ( IN.color.b * break10_g70.b * temp_output_488_4 ) , 0.0));
                float _ColorMultiplier_Instance = UNITY_ACCESS_INSTANCED_PROP(_ColorMultiplier_arr, _ColorMultiplier);
                float mulTime470 = _Time.y * 20.0;
                float temp_output_2_0_g64 = 2.0;
                float2 temp_output_469_0 = ( PatternUV463 + ( round( ( mulTime470 * temp_output_2_0_g64 ) ) / temp_output_2_0_g64 ) );
                float simplePerlin2D466 = snoise( temp_output_469_0*300.0 );
                simplePerlin2D466 = simplePerlin2D466*0.5 + 0.5;
                float simplePerlin2D535 = snoise( temp_output_469_0*350.0 );
                simplePerlin2D535 = simplePerlin2D535*0.5 + 0.5;
                float simplePerlin2D536 = snoise( temp_output_469_0*250.0 );
                simplePerlin2D536 = simplePerlin2D536*0.5 + 0.5;
                float4 appendResult537 = (float4(simplePerlin2D466 , simplePerlin2D535 , simplePerlin2D536 , 0.0));
                float4 lerpResult476 = lerp( float4( 1,1,1,0 ) , appendResult537 , _NoiseAmount);
                float4 Noise525 = lerpResult476;
                float lerpResult320 = lerp( 1.0 , pow( simplePerlin2D304 , 3.0 ) , _GlitchAmount_Instance);
                float Glitch324 = lerpResult320;
                float _GlitchMinColorMultiply_Instance = UNITY_ACCESS_INSTANCED_PROP(_GlitchMinColorMultiply_arr, _GlitchMinColorMultiply);
                float _PixelGridSize_Instance = UNITY_ACCESS_INSTANCED_PROP(_PixelGridSize_arr, _PixelGridSize);
                float2 temp_cast_6 = (_PixelGridSize_Instance).xx;
                float temp_output_2_0_g71 = 0.7;
                float2 appendResult10_g72 = (float2(temp_output_2_0_g71 , temp_output_2_0_g71));
                float2 temp_output_11_0_g72 = ( abs( (frac( (PatternUV463*temp_cast_6 + float2( 0,0 )) )*2.0 + -1.0) ) - appendResult10_g72 );
                float2 break16_g72 = ( 1.0 - ( temp_output_11_0_g72 / max( fwidth( temp_output_11_0_g72 ) , float2( 1E-05,1E-05 ) ) ) );
                float _PixelGridAmount_Instance = UNITY_ACCESS_INSTANCED_PROP(_PixelGridAmount_arr, _PixelGridAmount);
                float lerpResult503 = lerp( 1.0 , saturate( min( break16_g72.x , break16_g72.y ) ) , _PixelGridAmount_Instance);
                float Grid505 = lerpResult503;
                float _ScanLinesFrenquency_Instance = UNITY_ACCESS_INSTANCED_PROP(_ScanLinesFrenquency_arr, _ScanLinesFrenquency);
                float _ScanLinesSpeed_Instance = UNITY_ACCESS_INSTANCED_PROP(_ScanLinesSpeed_arr, _ScanLinesSpeed);
                float mulTime520 = _Time.y * _ScanLinesSpeed_Instance;
                float temp_output_519_0 = ( ( PatternUV463.y * _ScanLinesFrenquency_Instance ) + mulTime520 );
                float _ScanLinesAmount_Instance = UNITY_ACCESS_INSTANCED_PROP(_ScanLinesAmount_arr, _ScanLinesAmount);
                float lerpResult511 = lerp( 1.0 , (0.0 + (sin( temp_output_519_0 ) - -1.0) * (1.0 - 0.0) / (1.0 - -1.0)) , _ScanLinesAmount_Instance);
                float ScanLines512 = lerpResult511;
                float temp_output_485_0 = max( max( temp_output_486_4 , temp_output_487_4 ) , temp_output_488_4 );
                float _GlitchMinOpacity_Instance = UNITY_ACCESS_INSTANCED_PROP(_GlitchMinOpacity_arr, _GlitchMinOpacity);
                float Alpha421 = ( IN.color.a * temp_output_485_0 * max( Glitch324 , _GlitchMinOpacity_Instance ) );
                float4 appendResult401 = (float4(( appendResult490 * _ColorMultiplier_Instance * Noise525 * max( Glitch324 , _GlitchMinColorMultiply_Instance ) * Grid505 * ScanLines512 ).xyz , Alpha421));
                

                half4 color = appendResult401;

                #ifdef UNITY_UI_CLIP_RECT
                half2 m = saturate((_ClipRect.zw - _ClipRect.xy - abs(IN.mask.xy)) * IN.mask.zw);
                color.a *= m.x * m.y;
                #endif

                #ifdef UNITY_UI_ALPHACLIP
                clip (color.a - 0.001);
                #endif

                color.rgb *= color.a;

                return color;
            }
        ENDCG
        }
    }
    CustomEditor "AmplifyShaderEditor.MaterialInspector"
	
	Fallback Off
}
/*ASEBEGIN
Version=19801
Node;AmplifyShaderEditor.CommentaryNode;150;-3744,-528;Inherit;False;1460;466.95;;6;24;15;25;51;52;333;SCREEN SPACE PROJECTION;1,1,1,1;0;0
Node;AmplifyShaderEditor.ScreenParams;24;-3696,-304;Inherit;False;0;5;FLOAT4;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.ScreenPosInputsNode;15;-3696,-480;Float;False;2;False;0;5;FLOAT4;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleDivideOpNode;25;-3344,-224;Inherit;False;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;51;-3264,-464;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.DynamicAppendNode;52;-2992,-384;Inherit;False;FLOAT2;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;333;-2704,-384;Inherit;False;ScreenPos;-1;True;1;0;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.TextureCoordinatesNode;461;-2704,160;Inherit;False;0;-1;2;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;5;FLOAT2;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.StaticSwitch;462;-2352,-16;Inherit;False;Property;_Coords;Coords;0;0;Create;True;0;0;0;False;0;False;0;0;0;True;;KeywordEnum;2;Screen;UV;Create;True;True;All;9;1;FLOAT2;0,0;False;0;FLOAT2;0,0;False;2;FLOAT2;0,0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT2;0,0;False;6;FLOAT2;0,0;False;7;FLOAT2;0,0;False;8;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.CommentaryNode;522;-1490,1314.979;Inherit;False;2116;592.4709;;19;327;310;326;306;308;301;307;305;304;460;321;444;309;320;324;303;302;528;529;Glitch;1,1,1,1;0;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;463;-2016,-16;Inherit;False;PatternUV;-1;True;1;0;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.GetLocalVarNode;528;-1472,1504;Inherit;False;463;PatternUV;1;0;OBJECT;;False;1;FLOAT2;0
Node;AmplifyShaderEditor.RangedFloatNode;327;-1376,1664;Inherit;False;InstancedProperty;_GlitchTiling;GlitchTiling;12;0;Create;True;0;0;0;False;0;False;1;2;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.BreakToComponentsNode;529;-1280,1488;Inherit;False;FLOAT2;1;0;FLOAT2;0,0;False;16;FLOAT;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4;FLOAT;5;FLOAT;6;FLOAT;7;FLOAT;8;FLOAT;9;FLOAT;10;FLOAT;11;FLOAT;12;FLOAT;13;FLOAT;14;FLOAT;15
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;310;-1104,1472;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0.5;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;326;-1088,1584;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleTimeNode;306;-880,1696;Inherit;False;1;0;FLOAT;3;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;308;-848,1776;Inherit;False;Constant;_Float4;Float 3;15;0;Create;True;0;0;0;False;0;False;5;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.DynamicAppendNode;301;-848,1520;Inherit;False;FLOAT2;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.FunctionNode;307;-672,1696;Inherit;False;SectionsRemap;-1;;40;e07d5d5a126b6f243ac17cc867746f65;0;2;1;FLOAT;0;False;2;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;305;-384,1616;Inherit;False;2;2;0;FLOAT2;0,0;False;1;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.NoiseGeneratorNode;304;-272,1600;Inherit;False;Simplex2D;True;False;2;0;FLOAT2;0,0;False;1;FLOAT;100;False;1;FLOAT;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;460;169.8065,1364.979;Inherit;False;GlitchPattern;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;321;-176,1744;Inherit;False;InstancedProperty;_GlitchAmount;GlitchAmount;11;1;[Header];Create;True;1;Glitch;0;0;False;0;False;0;0;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;436;-1808,-240;Inherit;False;460;GlitchPattern;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;444;183.8279,1794.5;Inherit;False;GlitchAmount;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.TFHCRemapNode;443;-1568,-256;Inherit;False;5;0;FLOAT;0;False;1;FLOAT;1;False;2;FLOAT;0;False;3;FLOAT;-1;False;4;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;442;-1600,-64;Inherit;False;Property;_DistortionAmount;DistortionAmount;15;0;Create;True;0;0;0;False;0;False;0;0.02;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;445;-1600,16;Inherit;False;444;GlitchAmount;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.CommentaryNode;524;-2912,784;Inherit;False;1652;434.95;;13;508;513;507;521;514;520;519;510;511;512;518;530;531;Scanlines;1,1,1,1;0;0
Node;AmplifyShaderEditor.TextureCoordinatesNode;434;-1600,96;Inherit;False;0;-1;2;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;5;FLOAT2;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;439;-1296,-240;Inherit;False;3;3;0;FLOAT;0;False;1;FLOAT;0.01;False;2;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;508;-2832,912;Inherit;False;463;PatternUV;1;0;OBJECT;;False;1;FLOAT2;0
Node;AmplifyShaderEditor.CommentaryNode;527;-1234,846;Inherit;False;1465.959;370.95;;12;470;472;467;471;469;477;466;476;525;535;536;537;Noise;1,1,1,1;0;0
Node;AmplifyShaderEditor.SimpleAddOpNode;435;-1104,-64;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;491;-1280,192;Inherit;False;Property;_Colorbleedingamount;Color bleeding amount;2;0;Create;True;0;0;0;False;0;False;0.01;0.01;0;0.1;0;1;FLOAT;0
Node;AmplifyShaderEditor.BreakToComponentsNode;513;-2576,832;Inherit;False;FLOAT2;1;0;FLOAT2;0,0;False;16;FLOAT;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4;FLOAT;5;FLOAT;6;FLOAT;7;FLOAT;8;FLOAT;9;FLOAT;10;FLOAT;11;FLOAT;12;FLOAT;13;FLOAT;14;FLOAT;15
Node;AmplifyShaderEditor.RangedFloatNode;507;-2832,992;Inherit;False;InstancedProperty;_ScanLinesFrenquency;ScanLinesFrenquency;8;0;Create;True;1;Glitch;0;0;False;0;False;100;50;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;521;-2864,1104;Inherit;False;InstancedProperty;_ScanLinesSpeed;ScanLinesSpeed;9;0;Create;True;1;Glitch;0;0;False;0;False;2;2;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleTimeNode;470;-1184,1008;Inherit;False;1;0;FLOAT;20;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;472;-1168,1104;Inherit;False;Constant;_Float0;Float 0;10;0;Create;True;0;0;0;False;0;False;2;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.DynamicAppendNode;437;-880,-48;Inherit;False;FLOAT2;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;492;-944,240;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;-1;False;1;FLOAT;0
Node;AmplifyShaderEditor.DynamicAppendNode;493;-944,112;Inherit;False;FLOAT2;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.PowerNode;309;-48,1648;Inherit;False;False;2;0;FLOAT;0;False;1;FLOAT;3;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;514;-2400,832;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleTimeNode;520;-2544,1040;Inherit;False;1;0;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;467;-864,944;Inherit;False;463;PatternUV;1;0;OBJECT;;False;1;FLOAT2;0
Node;AmplifyShaderEditor.FunctionNode;471;-960,1024;Inherit;False;SectionsRemap;-1;;64;e07d5d5a126b6f243ac17cc867746f65;0;2;1;FLOAT;0;False;2;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.CommentaryNode;523;-2930,1406;Inherit;False;1172;338.95;;6;504;500;497;501;503;505;Grid;1,1,1,1;0;0
Node;AmplifyShaderEditor.RangedFloatNode;475;-864,-304;Inherit;False;Property;_BlurAmount;BlurAmount;4;0;Create;True;0;0;0;False;0;False;0;0;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.TemplateShaderPropertyNode;403;-800,-384;Inherit;False;0;0;_MainTex;Shader;False;0;5;SAMPLER2D;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleAddOpNode;480;-656,-48;Inherit;False;2;2;0;FLOAT2;0,0;False;1;FLOAT2;0.2,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.DynamicAppendNode;494;-784,240;Inherit;False;FLOAT2;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.RangedFloatNode;458;-864,-224;Inherit;False;InstancedProperty;_MipLevel;MipLevel;3;2;[Header];[IntRange];Create;True;1;Mip level blur (requires mip level enabled on texture );0;0;False;0;False;1;2;0;10;0;1;FLOAT;0
Node;AmplifyShaderEditor.LerpOp;320;160,1520;Inherit;False;3;0;FLOAT;1;False;1;FLOAT;0;False;2;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;519;-2240,928;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;469;-672,944;Inherit;False;2;2;0;FLOAT2;0,0;False;1;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleAddOpNode;482;-640,80;Inherit;False;2;2;0;FLOAT2;0,0;False;1;FLOAT2;-0.2,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.FunctionNode;487;-464,-112;Inherit;False;MipMapBlur;-1;;69;df61e54dd2caea542bed29d717ba7a77;0;4;6;FLOAT;0;False;7;INT;0;False;8;FLOAT2;0,0;False;9;SAMPLER2D;;False;5;FLOAT;11;FLOAT;12;FLOAT;13;COLOR;0;FLOAT;4
Node;AmplifyShaderEditor.RegisterLocalVarNode;324;384,1520;Inherit;False;Glitch;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;504;-2880,1536;Inherit;False;InstancedProperty;_PixelGridSize;PixelGridSize;6;0;Create;True;1;Glitch;0;0;False;0;False;100;50;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;500;-2880,1456;Inherit;False;463;PatternUV;1;0;OBJECT;;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SinOpNode;530;-2048,1008;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.FunctionNode;486;-480,-288;Inherit;False;MipMapBlur;-1;;68;df61e54dd2caea542bed29d717ba7a77;0;4;6;FLOAT;0;False;7;INT;0;False;8;FLOAT2;0,0;False;9;SAMPLER2D;;False;5;FLOAT;11;FLOAT;12;FLOAT;13;COLOR;0;FLOAT;4
Node;AmplifyShaderEditor.NoiseGeneratorNode;466;-544,912;Inherit;False;Simplex2D;True;False;2;0;FLOAT2;0,0;False;1;FLOAT;300;False;1;FLOAT;0
Node;AmplifyShaderEditor.NoiseGeneratorNode;535;-544,1008;Inherit;False;Simplex2D;True;False;2;0;FLOAT2;0,0;False;1;FLOAT;350;False;1;FLOAT;0
Node;AmplifyShaderEditor.NoiseGeneratorNode;536;-544,1104;Inherit;False;Simplex2D;True;False;2;0;FLOAT2;0,0;False;1;FLOAT;250;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMaxOpNode;483;-208,-128;Inherit;False;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.FunctionNode;488;-464,48;Inherit;False;MipMapBlur;-1;;70;df61e54dd2caea542bed29d717ba7a77;0;4;6;FLOAT;0;False;7;INT;0;False;8;FLOAT2;0,0;False;9;SAMPLER2D;;False;5;FLOAT;11;FLOAT;12;FLOAT;13;COLOR;0;FLOAT;4
Node;AmplifyShaderEditor.GetLocalVarNode;420;-384,272;Inherit;False;324;Glitch;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;441;-512,400;Inherit;False;InstancedProperty;_GlitchMinOpacity;GlitchMinOpacity;13;0;Create;True;1;Glitch;0;0;False;0;False;0;1;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;510;-2336,1072;Inherit;False;InstancedProperty;_ScanLinesAmount;ScanLinesAmount;7;1;[Header];Create;True;1;Scanlines;0;0;False;0;False;0;1;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;501;-2608,1632;Inherit;False;InstancedProperty;_PixelGridAmount;PixelGridAmount;5;1;[Header];Create;True;1;Grid;0;0;False;0;False;0;1;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.FunctionNode;497;-2544,1472;Inherit;False;GridCustomUV;-1;;71;f91aea11d974db3439518532de40981b;0;4;11;FLOAT2;0,0;False;5;FLOAT2;8,8;False;6;FLOAT2;0,0;False;2;FLOAT;0.7;False;1;FLOAT;0
Node;AmplifyShaderEditor.TFHCRemapNode;531;-1904,992;Inherit;False;5;0;FLOAT;0;False;1;FLOAT;-1;False;2;FLOAT;1;False;3;FLOAT;0;False;4;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;477;-336,1136;Inherit;False;Property;_NoiseAmount;NoiseAmount;10;1;[Header];Create;True;1;Noise;0;0;False;0;False;0;0;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.DynamicAppendNode;537;-336,960;Inherit;False;FLOAT4;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT4;0
Node;AmplifyShaderEditor.SimpleMaxOpNode;485;-80,-80;Inherit;False;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMaxOpNode;440;-144,320;Inherit;False;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.LerpOp;503;-2192,1520;Inherit;False;3;0;FLOAT;1;False;1;FLOAT;0;False;2;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.LerpOp;511;-1696,960;Inherit;False;3;0;FLOAT;1;False;1;FLOAT;0;False;2;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.VertexColorNode;411;-448,-528;Inherit;False;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.LerpOp;476;-160,928;Inherit;False;3;0;FLOAT4;1,1,1,0;False;1;FLOAT4;0,0,0,0;False;2;FLOAT;0;False;1;FLOAT4;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;405;64,-112;Inherit;False;3;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;505;-2000,1520;Inherit;False;Grid;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;512;-1504,960;Inherit;False;ScanLines;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;496;0,336;Inherit;False;InstancedProperty;_GlitchMinColorMultiply;GlitchMinColorMultiply;14;0;Create;True;1;Glitch;0;0;False;0;False;0;1;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;533;-160,-352;Inherit;False;3;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;534;-160,-256;Inherit;False;3;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;532;-160,-480;Inherit;False;3;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;525;0,960;Inherit;False;Noise;-1;True;1;0;FLOAT4;0,0,0,0;False;1;FLOAT4;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;421;240,-112;Inherit;False;Alpha;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMaxOpNode;495;384,32;Inherit;False;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;517;656,160;Inherit;False;512;ScanLines;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;506;656,80;Inherit;False;505;Grid;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;526;576,-48;Inherit;False;525;Noise;1;0;OBJECT;;False;1;FLOAT4;0
Node;AmplifyShaderEditor.RangedFloatNode;409;576,-128;Inherit;False;InstancedProperty;_ColorMultiplier;ColorMultiplier;1;1;[Header];Create;True;1;Color;0;0;False;0;False;1;10;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.DynamicAppendNode;490;32,-352;Inherit;False;FLOAT4;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT4;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;408;928,-144;Inherit;False;6;6;0;FLOAT4;0,0,0,0;False;1;FLOAT;0;False;2;FLOAT4;0,0,0,0;False;3;FLOAT;0;False;4;FLOAT;0;False;5;FLOAT;0;False;1;FLOAT4;0
Node;AmplifyShaderEditor.GetLocalVarNode;448;1152,96;Inherit;False;421;Alpha;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.NoiseGeneratorNode;518;-2048,912;Inherit;False;Simplex2D;True;False;2;0;FLOAT2;0,0;False;1;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.DynamicAppendNode;401;1360,32;Inherit;False;FLOAT4;4;0;FLOAT3;0,0,0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;1;False;1;FLOAT4;0
Node;AmplifyShaderEditor.RangedFloatNode;303;-848,1616;Inherit;False;Constant;_Float3;Float 3;15;0;Create;True;0;0;0;False;0;False;50;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.FunctionNode;302;-640,1376;Inherit;False;SectionsRemap;-1;;73;e07d5d5a126b6f243ac17cc867746f65;0;2;1;FLOAT2;0,0;False;2;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;473;304,-384;Inherit;False;2;2;0;FLOAT4;0,0,0,0;False;1;FLOAT;0;False;1;FLOAT4;0
Node;AmplifyShaderEditor.TemplateMultiPassMasterNode;397;1616,112;Float;False;True;-1;3;AmplifyShaderEditor.MaterialInspector;0;3;Turishader/UI/VHSUI;5056123faa0c79b47ab6ad7e8bf059a4;True;Default;0;0;Default;2;False;True;3;1;False;;10;False;;0;1;False;;0;False;;False;False;False;False;False;False;False;False;False;False;False;False;True;2;False;;False;True;True;True;True;True;0;True;_ColorMask;False;False;False;False;False;False;False;True;True;0;True;_Stencil;255;True;_StencilReadMask;255;True;_StencilWriteMask;0;True;_StencilComp;0;True;_StencilOp;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;False;True;2;False;;True;0;True;unity_GUIZTestMode;False;True;5;Queue=Transparent=Queue=0;IgnoreProjector=True;RenderType=Transparent=RenderType;PreviewType=Plane;CanUseSpriteAtlas=True;False;False;0;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;True;3;False;0;;0;0;Standard;0;0;1;True;False;;False;0
WireConnection;25;0;24;1
WireConnection;25;1;24;2
WireConnection;51;0;15;1
WireConnection;51;1;25;0
WireConnection;52;0;51;0
WireConnection;52;1;15;2
WireConnection;333;0;52;0
WireConnection;462;1;333;0
WireConnection;462;0;461;0
WireConnection;463;0;462;0
WireConnection;529;0;528;0
WireConnection;310;1;529;0
WireConnection;326;0;529;1
WireConnection;326;1;327;0
WireConnection;301;0;310;0
WireConnection;301;1;326;0
WireConnection;307;1;306;0
WireConnection;307;2;308;0
WireConnection;305;0;301;0
WireConnection;305;1;307;0
WireConnection;304;0;305;0
WireConnection;460;0;304;0
WireConnection;444;0;321;0
WireConnection;443;0;436;0
WireConnection;439;0;443;0
WireConnection;439;1;442;0
WireConnection;439;2;445;0
WireConnection;435;0;439;0
WireConnection;435;1;434;1
WireConnection;513;0;508;0
WireConnection;437;0;435;0
WireConnection;437;1;434;2
WireConnection;492;0;491;0
WireConnection;493;0;491;0
WireConnection;309;0;304;0
WireConnection;514;0;513;1
WireConnection;514;1;507;0
WireConnection;520;0;521;0
WireConnection;471;1;470;0
WireConnection;471;2;472;0
WireConnection;480;0;437;0
WireConnection;480;1;493;0
WireConnection;494;0;492;0
WireConnection;320;1;309;0
WireConnection;320;2;321;0
WireConnection;519;0;514;0
WireConnection;519;1;520;0
WireConnection;469;0;467;0
WireConnection;469;1;471;0
WireConnection;482;0;437;0
WireConnection;482;1;494;0
WireConnection;487;6;475;0
WireConnection;487;7;458;0
WireConnection;487;8;480;0
WireConnection;487;9;403;0
WireConnection;324;0;320;0
WireConnection;530;0;519;0
WireConnection;486;6;475;0
WireConnection;486;7;458;0
WireConnection;486;8;437;0
WireConnection;486;9;403;0
WireConnection;466;0;469;0
WireConnection;535;0;469;0
WireConnection;536;0;469;0
WireConnection;483;0;486;4
WireConnection;483;1;487;4
WireConnection;488;6;475;0
WireConnection;488;7;458;0
WireConnection;488;8;482;0
WireConnection;488;9;403;0
WireConnection;497;11;500;0
WireConnection;497;5;504;0
WireConnection;531;0;530;0
WireConnection;537;0;466;0
WireConnection;537;1;535;0
WireConnection;537;2;536;0
WireConnection;485;0;483;0
WireConnection;485;1;488;4
WireConnection;440;0;420;0
WireConnection;440;1;441;0
WireConnection;503;1;497;0
WireConnection;503;2;501;0
WireConnection;511;1;531;0
WireConnection;511;2;510;0
WireConnection;476;1;537;0
WireConnection;476;2;477;0
WireConnection;405;0;411;4
WireConnection;405;1;485;0
WireConnection;405;2;440;0
WireConnection;505;0;503;0
WireConnection;512;0;511;0
WireConnection;533;0;411;2
WireConnection;533;1;487;12
WireConnection;533;2;487;4
WireConnection;534;0;411;3
WireConnection;534;1;488;13
WireConnection;534;2;488;4
WireConnection;532;0;411;1
WireConnection;532;1;486;11
WireConnection;532;2;486;4
WireConnection;525;0;476;0
WireConnection;421;0;405;0
WireConnection;495;0;420;0
WireConnection;495;1;496;0
WireConnection;490;0;532;0
WireConnection;490;1;533;0
WireConnection;490;2;534;0
WireConnection;408;0;490;0
WireConnection;408;1;409;0
WireConnection;408;2;526;0
WireConnection;408;3;495;0
WireConnection;408;4;506;0
WireConnection;408;5;517;0
WireConnection;518;0;519;0
WireConnection;401;0;408;0
WireConnection;401;3;448;0
WireConnection;302;1;301;0
WireConnection;302;2;303;0
WireConnection;473;0;490;0
WireConnection;473;1;485;0
WireConnection;397;0;401;0
ASEEND*/
//CHKSM=32BBA094322A9DBC6A107CF4EBFC55B79AEB886E