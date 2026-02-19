Shader "DK2_URP_Shader/Charactor/Ghost"
{
	Properties 
	{
		[Toggle]_UseEnvRamp ( "Use Env Ramp", Int ) = 1

		[Enum(UnityEngine.Rendering.ColorWriteMask)] _ColorWriteMask("ColorWriteMask", Float) = 15

		_Alpha		( "Alpha", Range(0, 1) )	= 1.0
		_MainTex	( "Base (RGB)", 2D )		= "black" {}
		_TintColor	("Tint Color", Color)		= (1.0, 1.0, 1.0, 1.0)
		_Intensity	("Intensity", Range(0, 5))	= 1
		_Min		( "Min", Range( 0, 0.5 ) )	= 0.15
		_Max		( "Max", Range( 0.6, 1 ) )	= 1
	}
	
	SubShader 
	{
		Tags { "Queue"="Transparent" "RenderType"="Transparent" "RenderPipeline" = "UniversalPipeline" }
	
//		ZTest Always
		Cull Back
		Lighting Off
		ZWrite Off

		Blend SrcAlpha OneMinusSrcAlpha

		ColorMask [_ColorWriteMask]

		Pass
        {
            Name  "FrontPass"
            Tags {"LightMode" = "SRPDefaultUnlit"}
            ZWrite On
            ColorMask 0
        }

		Pass
		{
            Name "TransparentPass"
            Tags {"LightMode" = "UniversalForward"}

			HLSLPROGRAM

			#pragma prefer_hlslcc gles   
            #pragma exclude_renderers d3d11_9x  
            #pragma target 3.0

			#pragma vertex vert_Main
			#pragma fragment frag_Main

			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
			//#include "../DK2_URP_ColorLib.hlsl"

			struct tINPUT
             {
                float4 vertex   : POSITION;
                float3 normal   : NORMAL;
                float2 texcoord : TEXCOORD0;

                UNITY_VERTEX_INPUT_INSTANCE_ID                              
              };

			struct v2f_OUTPUT 
			{
				float4 Pos		: SV_POSITION;
				float2 TexCoord : TEXCOORD0;
				float  Diff		: TEXCOORD1;

				UNITY_VERTEX_INPUT_INSTANCE_ID
			};
			
			CBUFFER_START(UnityPerMaterial)
			
			sampler2D	_MainTex;
			float4		_MainTex_ST;
			half		_Alpha, _Intensity, _Min, _Max;
			half4		_TintColor;

			CBUFFER_END
			
			v2f_OUTPUT vert_Main( tINPUT tIn )
			{
				v2f_OUTPUT Out;

				UNITY_SETUP_INSTANCE_ID( tIn );
                UNITY_TRANSFER_INSTANCE_ID( tIn, tOut );
				
				half3 f3PosW = TransformObjectToWorld( tIn.vertex.xyz );
				half3 f3NorW = TransformObjectToWorldNormal( tIn.normal );
				half3 f3EyeW = _WorldSpaceCameraPos.xyz - f3PosW;

				Out.Pos			= TransformObjectToHClip( tIn.vertex.xyz );
				Out.TexCoord	= TRANSFORM_TEX( tIn.texcoord, _MainTex );
				Out.Diff		= 1 - saturate( dot( normalize( f3NorW ), normalize( f3EyeW ) ) );

				return Out;  
			}
			
			float4 frag_Main( v2f_OUTPUT In ) : COLOR
			{
				UNITY_SETUP_INSTANCE_ID( tIn );

				float4	texColor	= tex2D( _MainTex, In.TexCoord );
				float	fDiff		= smoothstep( _Min, _Max, In.Diff );

				texColor.rgb	*= _TintColor.rgb * _Intensity;
				texColor.a		= fDiff * _Alpha;

				//Apply Color Ramp
				//texColor.rgb = ApplyEnvColorRamp( ( _global_UseRamp && _UseEnvRamp ), texColor.rgb, _global_Ramp_Map, _global_EnvPct_Efx );

				return( texColor ); 
			}
			
			ENDHLSL
		}
	}
}
