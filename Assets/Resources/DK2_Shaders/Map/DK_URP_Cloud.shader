Shader "DK2_URP_Shader/Map/BG_Cloud"
{
	Properties
	{
		[MaterialToggle] _UseLightMap("UseLightMap", float)		= 1
		
		[HDR]_Color("Main Color", Color) = (1,1,1,1)
		//_SColor("Water SColor", Color) = (1,1,1,1)
		_MainTex( "Base (RGB)", 2D )							= "white" {}
		[Toggle(RampTex)] _RampTexOn ("RampTex on", float) = 0
		_RampTex( "Ramp (RGB)", 2D )							= "white" {}
		//_BaseTexSpeed("Base Speed", Vector) = (2,2,2,2)

		//_MaskTex( "Mask Tex (RGB)", 2D )							= "white" {}
		//_MaskSpeed("Mask Speed", Vector) = (2,2,2,2)
	
		//_CloudDst("Cloud Dst", Range(0,2)) = 0.5
		//_CloudAlphaThr("Cloud Alpha Thr", Range(0,1)) = 0.5
		//_CloudAlphaSmooth("Cloud Alpha Smooth", Range(0,1)) = 0.5
		//_CloudThr("Cloud Thr", Range(0,1)) = 0.5
		//_CloudSmooth("Cloud Smooth", Range(0,1)) = 0.5
		//_Fogthr("Fogthr", Range(0,1)) = 0.5
		//_ShadowAlphaThr("Shadow Alpha Thr", Range(0,1)) = 0.5
		
	}

	SubShader
	{
		Tags { "Queue" = "Transparent" "RenderType" = "Transparent"   "RenderPipeline" = "UniversalPipeline" }
		
		Blend SrcAlpha OneMinusSrcAlpha
		Cull Front

	
		Pass
		{
			//Tags{ "LightMode" = "ForwardBase" }
			//LOD 100

			
			HLSLPROGRAM
			//#pragma target 3.0
			#pragma vertex vert_Main
			#pragma fragment frag_Main
			#pragma shader_feature RampTex
			
		//	#pragma multi_compile_fog

			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
		;

			sampler2D		_MainTex,_RampTex;
			float4			_MainTex_ST,_RampTex_ST;
			float4			_Color;

			struct Vertexinput
			{
				float4  vertex		: POSITION;
				float4	vcolor		: COLOR0;
				float2	texcoord	: TEXCOORD0;
			};

			
			struct tOUTPUT
			{
				float4  pos			: POSITION;
				float4	vcolor		: COLOR0;
				float2  UV			: TEXCOORD0;
		
			
			};

			tOUTPUT vert_Main(Vertexinput v )// 주의!!! 매크로 함수를 사용하므로, 파라메터 이름을 v로 지정해야함.
			{
				tOUTPUT	tOut;
			
				float4 pos		= TransformObjectToHClip(v.vertex.xyz);
				tOut.pos = pos;
				tOut.UV  = v.texcoord;
				tOut.vcolor = v.vcolor;

			
				return tOut;
			}

			float4 frag_Main( tOUTPUT tIn ) : COLOR
			{
				//UNITY_SETUP_INSTANCE_ID(tIn);
				

				float4 f4TexColor = tex2D(_MainTex, TRANSFORM_TEX(tIn.UV, _MainTex));
				
				#ifdef RampTex
				f4TexColor.rgb = tex2D(_RampTex, TRANSFORM_TEX(float2(f4TexColor.r,1), _RampTex));
				#endif


				f4TexColor.rgb *= _Color;
				

				float Alpha = f4TexColor.a * tIn.vcolor.a *_Color.a;
				
				return float4(f4TexColor.rgb,Alpha);
			}

			ENDHLSL
		}
		

	}
	 
	
}