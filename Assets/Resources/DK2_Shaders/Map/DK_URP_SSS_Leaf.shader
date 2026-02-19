Shader "DK2_URP_Shader/Map/SSS_Leaf"
{
    Properties
    {
        [Header(_________________________________________________________________________________________)]
        [Header(Env Color)]
        [Space(20)]

        [Toggle]_UseEnvRamp                     ( "Use Env Ramp", Int )                = 1

		[Header(_________________________________________________________________________________________)]
		[Header(Main Color)]
		[Space(20)]

		[NoScaleOffset]
		_MainTex							( "Base (RGB)", 2D)						= "white" {}

        [Header(_________________________________________________________________________________________)]
        [Header(SSS)]
        [Space(20)]
        _SSSColor("SSS Color", Color) = (0, 0, 0, 1)
        _SSSDistortion("SSS Distortion", Float) = 1
        _SSSPower("SSS Power", Float) = 1
        _SSSScale("SSS Scale", Float) = 1
        _SSSAttenuation("SSS Attenuation", Range(0, 2)) = 1
        _SSSThickness("SSS Thickness", Float) = 1
        _SSSFrontSpread( "SSS FrontSpread", Range( 0, 1 ) ) = 0
    }

        SubShader
        {
            Tags
            {
                "RenderPipeline" = "UniversalPipeline"
                "RenderType" = "Opaque"
                "Queue" = "Geometry"
            }

            Cull Back

            //Blend SrcAlpha OneMinusSrcAlpha

            //MainToon Pass
            Pass
            {
                Name "Universal Forward"
                Tags
                {
                    "LightMode" = "UniversalForward"
                }

                HLSLPROGRAM

            //
            #include "../DK2_URP_CarToonLib.hlsl"
            #include "../DK2_URP_ColorLib.hlsl"

            //
            #pragma prefer_hlslcc gles   
            #pragma exclude_renderers d3d11_9x  
            #pragma target 3.0

            //
            #pragma vertex vert_Main
            #pragma fragment frag_Main

            //
            half4 _SSSColor;
            half _SSSDistortion;
            half _SSSPower;
            half _SSSScale;
            half _SSSAttenuation;
            half _SSSThickness;
            half _SSSFrontSpread;

            //
            tOUTPUT_Base vert_Main( tINPUT_Base tIn )
			{
				tOUTPUT_Base	tOut;

                UNITY_SETUP_INSTANCE_ID( tIn );
                UNITY_TRANSFER_INSTANCE_ID( tIn, tOut );

                half3 f3PosW = TransformObjectToWorld( tIn.vertex.xyz );
                
				tOut.f4Pos		= TransformObjectToHClip( tIn.vertex.xyz );
                tOut.f2UV		= TRANSFORM_TEX( tIn.texcoord, _MainTex );
				
                //Type 1 ( Have Normal )
                tOut.f3NormalW	= TransformObjectToWorldNormal( tIn.normal );

                //Type 2 ( Calc Normal )
                //tOut.f3NormalW = normalize( f3PosW - TransformObjectToWorld( float3( 0, 0, 0 ) ) );
				
                tOut.f3EyeW     = normalize( _WorldSpaceCameraPos.xyz - f3PosW );
				
				return tOut;
			}

            half4 frag_Main( tOUTPUT_Base tIn ) : SV_Target
			{
                UNITY_SETUP_INSTANCE_ID( tIn );

                Light   lightMain       = GetMainLight();
	            half4	f4MainTex	    = tex2D( _MainTex, tIn.f2UV );
                
                clip( f4MainTex.a - 0.5 );

                //SSS
                float3  f3Half      = ( _MainLightPosition.xyz + tIn.f3NormalW * _SSSDistortion );
                float   fVoH        = pow( saturate( dot( tIn.f3EyeW, -f3Half ) ), _SSSPower + 0.001 ) * _SSSScale;
                float3  f3SSSColor  = _SSSAttenuation* ( fVoH + _SSSFrontSpread ) * _SSSThickness * _SSSColor.rgb * lightMain.color.rgb;

                float4  f4Result    = float4( f4MainTex.xyz + f3SSSColor, 1 );

                //Apply Color Ramp
                //f3Result = ApplyEnvColorRamp( ( _global_UseRamp && _UseEnvRamp ), f3Result, _global_Ramp_Map, _global_EnvPct_Char );

				return f4Result;
			}

            ENDHLSL  
        }

        Pass
        {
            Name "ShadowCaster"

            Tags{"LightMode" = "ShadowCaster"}

            Cull Back

            HLSLPROGRAM

            #include "../DK2_URP_ShadowCastLib.hlsl"

            #pragma prefer_hlslcc gles
            #pragma exclude_renderers d3d11_9x
            #pragma target 3.0

            // GPU Instancing
            #pragma multi_compile_instancing
            
            #pragma vertex ShadowPassVertex
            #pragma fragment ShadowPassFragment
            
            ENDHLSL
        }
    }
}

