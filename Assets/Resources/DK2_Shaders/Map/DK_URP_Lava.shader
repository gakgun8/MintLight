Shader "DK2_URP_Shader/Map/BG_Lava"
{
	Properties
	{
		[HDR] _BaseColor("Base Color", Color) = (0, 0.3, 0.3, 1)

		[Space(30)]
		
		_SplatMap01("Mask tex", 2D) = "gray" {}
		_DistotionTex("Distotion tex", 2D) = "gray" {}
		_DistotionTexSpeed("Distotion Speed : Alpha", Vector) = (1,1,1,1)
		
	
		
		[Space(30)]
		[HDR] _GlowColorBase("Glow Color Base: BaseTex Alpha", Color) = (1, 1, 1, 1)
		_Albedo01("BaseTex ", 2D) = "Color" {}
		_BaseTexSpeed("BaseTex Speed", Vector) = (1,1,1,1)
		_DistotionBase("DistotionBase", Range(0,1)) = 1.0

		[Space(30)]
		[HDR] _GlowColorRed("Glow Color Red : RedTex Alpha", Color) = (1, 1, 1, 1)
		_Albedo02("RedTex ", 2D) = "Color" {}
		_RedTexSpeed("RedTex Speed", Vector) = (1,1,1,1)
		_DistotionRed("DistotionRed", Range(0,1)) = 1.0

		[Space(30)]
		[HDR] _GlowColorGreen("Glow Color Green : GreenTex Alpha", Color) = (1, 1, 1, 1)
		_Albedo03("GreenTex ", 2D) = "Color" {}
		_GreenTexSpeed("GreenTex Speed", Vector) = (1,1,1,1)
		_DistotionGreen("DistotionGreen", Range(0,1)) = 1.0




		
		

		
	
	
	}

	SubShader
	{
		Tags
		{
			"RenderPipeline" = "UniversalPipeline"
			"Queue" = "Transparent"
			"RenderType" = "Transparent"
			//"UniversalMaterialType" = "Lit"
		}

		Blend SrcAlpha OneMinusSrcAlpha
		Cull Back

		Pass
		{
			HLSLPROGRAM
			#pragma vertex vert_Main
			#pragma fragment frag_Main
//			#pragma multi_compile _ _MAIN_LIGHT_SHADOWS
//			#pragma multi_compile _ _SHADOWS_SOFT
			#pragma multi_compile_fog

			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
			//#include "../DK2_URP_ColorLib.hlsl"

			CBUFFER_START(UnityPerMaterial)
			sampler2D _SplatMap01 ,_Albedo01 , _Albedo02, _Albedo03 , _DistotionTex ;
			
			float4  _SplatMap01_ST, _Albedo01_ST,  _Albedo02_ST,_Albedo03_ST, _DistotionTex_ST;

		
			
		
			sampler2D _CameraOpaqueTexture;
			
			samplerCUBE		_Cube;
			float4			_BaseTexSpeed , _RedTexSpeed ,_GreenTexSpeed ,_DistotionTexSpeed ;
		

			float3			_GlowColorBase,_GlowColorRed ,_GlowColorGreen;
			float3			_BaseColor;
			
			float			_DistotionBase,_DistotionRed,_DistotionGreen,_DistotionBlue;

			float			_specular;
		//	float3			_cubeColor;
			CBUFFER_END

			struct VertexInput
			{
				float4  vertex		: POSITION;
				float3	normal		: NORMAL;
				float4	tangent		: TANGENT;
				float2  UV			: TEXCOORD0;
			};

			struct tOUTPUT
			{
				float4  vertex		: SV_POSITION;
				float3  posWorld	: TEXCOORD0;
				float2  UV			: TEXCOORD1;
				float4	sPos		: TEXCOORD2;
				float3	normal		: NORMAL;
				float3	tangent		: TEXCOORD3;
				float3  bitangent	: TEXCOORD4;
				float  fogCoord		: TEXCOORD5;
			};

			tOUTPUT vert_Main(VertexInput v)// 주의!!! 매크로 함수를 사용하므로, 파라메터 이름을 v로 지정해야함.
			{
				tOUTPUT	tOut;
				UNITY_TRANSFER_INSTANCE_ID(v, tsOut);
				tOut.vertex = TransformObjectToHClip(v.vertex.xyz);
				tOut.posWorld = TransformObjectToWorld(v.vertex.xyz);
				tOut.sPos = ComputeScreenPos(tOut.vertex);

				//-----------NormalTexture
				tOut.normal = TransformObjectToWorldNormal(v.normal);
				tOut.tangent = TransformObjectToWorldDir(v.tangent.xyz);
				tOut.bitangent = cross(tOut.normal, tOut.tangent) * v.tangent.w;
				tOut.fogCoord = ComputeFogFactor(tOut.vertex.z);
				tOut.UV = v.UV;
				return tOut;
			}

			float4 frag_Main(tOUTPUT tIn) : SV_Target
			{ 
			


				tIn.normal = normalize(tIn.normal);
				float posworldtiling =0.15;
				float4 masktex = tex2D(_SplatMap01,tIn.UV );
				float maskAlpha = tex2D(_DistotionTex,TRANSFORM_TEX(float2(tIn.posWorld.xz *posworldtiling + _DistotionTexSpeed.xy*_Time.xx),_DistotionTex)).r;
				float maskAlpha1 = tex2D(_DistotionTex,TRANSFORM_TEX(float2((1-tIn.posWorld.xz*posworldtiling) + _DistotionTexSpeed.zw*_Time.xx),_DistotionTex)).r;
				float maskalpha2 = (maskAlpha + maskAlpha1 )*0.5;
				
				
				float4 BaseTex = tex2D(_Albedo01,TRANSFORM_TEX(float2(tIn.posWorld.xz *posworldtiling + maskalpha2*_DistotionBase +_Time.xx * _BaseTexSpeed.xy), _Albedo01 ));
				float4 RedTex = tex2D(_Albedo02,TRANSFORM_TEX(float2(tIn.posWorld.xz *posworldtiling + (masktex.r * maskalpha2)*_DistotionRed + _Time.xx *_RedTexSpeed.xy),_Albedo02));
				float4 GreenTex = tex2D(_Albedo03,TRANSFORM_TEX(float2(tIn.posWorld.xz *posworldtiling + (maskalpha2)*_DistotionGreen + _Time.xx *_GreenTexSpeed.xy),_Albedo03));
				
				
				BaseTex.rgb = lerp(BaseTex,BaseTex*_GlowColorBase.rgb , BaseTex.a * maskalpha2);
				RedTex.rgb = lerp(RedTex*_GlowColorRed.rgb,RedTex,masktex.r);
				GreenTex.rgb = lerp(GreenTex,GreenTex*_GlowColorGreen.rgb,GreenTex.a*maskalpha2);
				
				float3 finalDiff = lerp(BaseTex.rgb,RedTex.rgb,masktex.r) ;
				finalDiff = lerp(finalDiff,GreenTex.rgb,masktex.g );
				//finalDiff = lerp


				finalDiff *= _BaseColor;
				//finalDiff = lerp( finalDiff,finalDiff *_GlowColorBase.rgb , BaseTex.a);
					//Apply Color Ramp
			//	finalDiff.rgb = ApplyEnvColorRamp( _global_UseRamp, finalDiff.rgb, _global_Ramp_Map, _global_EnvPct_Map );


			//	float4 finalcolor = float4(foamtex1,foamtex1,foamtex1,1);
				finalDiff.rgb = MixFog(finalDiff.rgb, InitializeInputDataFog(float4(tIn.posWorld, 1.0), 1));
			

				return float4(finalDiff, 1);
			}

			ENDHLSL
		}
	}
}