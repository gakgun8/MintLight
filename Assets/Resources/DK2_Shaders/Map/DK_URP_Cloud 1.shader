Shader "Geosang_M/BG/BG_Cloud_1"
{
	Properties
	{
		[MaterialToggle] _UseLightMap("UseLightMap", float)		= 1
		
		[HDR]_Tint("Tint Color", Color) = (1,1,1,1)
		_Color("Light Color", Color) = (1,1,1,1)
		_SColor("Shadow SColor", Color) = (1,1,1,1)
		_BaseTex( "Base (RGB)", 2D )							= "white" {}
		_BaseTexSpeed("Base Speed", Vector) = (2,2,2,2)

		_MaskTex( "Mask Tex (RGB)", 2D )							= "white" {}
		_MaskSpeed("Mask Speed", Vector) = (2,2,2,2)
	
		_CloudDst("Cloud Dst", Range(0,2)) = 0.5
		_CloudAlphaThr("Cloud Alpha Thr", Range(0,1)) = 0.5
		_CloudAlphaSmooth("Cloud Alpha Smooth", Range(0,1)) = 0.5
		_CloudThr("Cloud Thr", Range(0,1)) = 0.5
		_CloudSmooth("Cloud Smooth", Range(0,1)) = 0.5
		_Fogthr("Fogthr", Range(0,1)) = 0.5
		_ShadowAlphaThr("Shadow Alpha Thr", Range(0,1)) = 0.5
		
	}

	SubShader
	{
		Tags { "RenderType" = "Opaque"   "RenderPipeline" = "UniversalPipeline" }
		
		Blend SrcAlpha OneMinusSrcAlpha
		Cull Back
		ZWrite Off

	
		Pass
		{
				Tags 
			{ 
				"LightMode" = "UniversalForward"
			}
			
			HLSLPROGRAM
			//#pragma target 3.0
			#pragma vertex vert_Main
			#pragma fragment frag_Main
			//#pragma multi_compile_instancing
			//#pragma multi_compile_fwdbase nolightmap nodirlightmap nodynlightmap novertexlight interpolateview noforwardadd
			#pragma multi_compile_fog

			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
			//float			_UseLightMap;
			//float			_VertexColor;

			sampler2D		_BaseTex;
			float2			_BaseTex_ST;
			sampler2D		_MaskTex;
			float2			_MaskTex_ST;
			//sampler2D		_FoamTex;
			//float2			_FoamTex_ST;
			float4			_Color;
			float4			_SColor;
			float			_Fogthr;
			float3			_Tint;
			//float4			_FoamColor;
			float4			_BaseTexSpeed;
			float4			_MaskSpeed;


			float			_CloudDst;
			float			_CloudAlphaThr;
			//float			_ShadowAlphaThr;
			float			_CloudAlphaSmooth;
			float			_CloudThr;
			float			_CloudSmooth;
		
			//float			_Intencity;
			//float			_ShadowIntencity;
			//float4			_ShadowColor;
		
			struct Vertexinput
			{
				float4  vertex		: POSITION;
				float4	vcolor		: COLOR0;
			};

			
			struct tOUTPUT
			{
				float4  pos			: POSITION;
				float4	vcolor		: COLOR0;
				//float2  v2UV		: TEXCOORD0;
				float3  posWorld	: TEXCOORD0;
				float4  sPos	: TEXCOORD1;
				//float2  v2UV_LM		: TEXCOORD1;
				//UNITY_VERTEX_INPUT_INSTANCE_ID
				//SHADOW_COORDS( 2 )
				float   fogCoord : TEXCOORD2;
				//SHADOW_COORDS(3)
			};

			tOUTPUT vert_Main(Vertexinput v )// 주의!!! 매크로 함수를 사용하므로, 파라메터 이름을 v로 지정해야함.
			{
				tOUTPUT	tOut;
			
				float4 pos		= TransformObjectToHClip(v.vertex.xyz);
				float3 posworld	= TransformObjectToWorld(v.vertex.xyz);
				tOut.sPos = ComputeScreenPos(pos);
				tOut.pos = pos;
				tOut.posWorld = posworld;
				tOut.vcolor = v.vcolor;

				//COMPUTE_EYEDEPTH(tOut.sPos.z);
				//tOut.v2UV		= TRANSFORM_TEX(v.texcoord, _BaseTex);
				//tOut.posWorld = posworld;
				//COMPUTE_EYEDEPTH(tOut.sPos.z);
				//TRANSFER_SHADOW(tOut)
				tOut.fogCoord = ComputeFogFactor(tOut.pos.z);
				return tOut;
			}

			float4 frag_Main( tOUTPUT tIn ) : COLOR
			{
				//UNITY_SETUP_INSTANCE_ID(tIn);
				float4 f4TexColor = _Color;
				
				float4 CloudTex = tex2D(_MaskTex, tIn.posWorld.xz *_MaskTex_ST.xy * 0.1 + _Time.xx * _MaskSpeed.xy * 0.1);
				//float3 foamtex4 = tex2D(_FoamTex, (tIn.posWorld.xz +CloudTex*_CloudDst) *_FoamTex_ST.xy * 0.1 + _Time.xx * _FoamSpeed.xy * 0.1);
				//float3 CloudTex = tex2D(_MaskTex, 1-tIn.posWorld.xz *_MaskTex_ST.xy * 0.1 + _Time.xx * _BaseTexSpeed.zw * 0.1);
				float3 CloudTex1 = tex2D(_BaseTex, (tIn.posWorld.xz *0.5+ CloudTex.g *_CloudDst) * _BaseTex_ST.xy * 0.1 + _Time.xx * _BaseTexSpeed.xy * 0.1);
				float3 CloudTex2 = tex2D(_BaseTex, 1-(tIn.posWorld.xz + CloudTex.g *_CloudDst) * _BaseTex_ST.xy * 0.1 + _Time.xx * _BaseTexSpeed.zw * 0.1);
				float CloudTex3 = saturate(CloudTex1.g * CloudTex2.g);
				float CloudTex4 = smoothstep(_CloudAlphaThr - _CloudAlphaSmooth,_CloudAlphaThr +  _CloudAlphaSmooth, CloudTex3 ) ;
				float3 CloudTex5 = lerp(_SColor,_Color, smoothstep(_CloudThr - _CloudSmooth ,_CloudThr + _CloudSmooth, CloudTex3 ))*_Tint.rgb;
				//CloudTex5.rgb = max(0,dot (_MainLightPosition.xyz,CloudTex5));
				
				
				
				//Depth Alpha
				float4 ScreenPos = tIn.sPos;
				float4 screentposNorm = ScreenPos / ScreenPos.w;
				float eyeDepth43 = LinearEyeDepth(SampleSceneDepth(screentposNorm.xy), _ZBufferParams);
				float linedepth = LinearEyeDepth(screentposNorm.z, _ZBufferParams);

					#if SHADER_API_D3D11 || SHADER_API_VULKAN
					linedepth = linedepth;
					#else
					linedepth = linedepth * 2;
					#endif

				
				
				float  depthDiff = eyeDepth43 - linedepth;



				//float sceneZ = SAMPLE_DEPTH_TEXTURE_PROJ(_CameraDepthTexture, UNITY_PROJ_COORD(tIn.sPos));
				//sceneZ = LinearEyeDepth(sceneZ);
			//	float partZ = tIn.sPos.z ;
				//float partZ1 = tIn.sPos.z +  _FoamSpread * foamtex4;
			//	float depthDiff = abs(sceneZ - partZ);
			//	float foamDiff = sceneZ - partZ1;
				//float finalDepth = saturate((_DepthAlpha * depthDiff) + _DepthThrAlpha);
				//float foamDepth = saturate ( foamDiff);
				//float foamAlpha = saturate(_FoamStrength - foamDiff) * saturate (1- foamDepth)*foamtex4 ;
				//f4TexColor.rgb	=lerp(  _Color.rgb + foamtex3* foamtex3*_SColor.rgb *_SColor.a , _FoamColor.rgb,foamAlpha); 
				//f4TexColor.rgb	= foamtex5 ; 
				//f4TexColor.rgb	=foamAlpha; 
				//float alpha = finalDepth + _FoamColor.a * foamAlpha ;
				//UNITY_APPLY_FOG( tIn.fogCoord, f4TexColor.rgb);
				//float4 finalcolor = (_Color.rgb,_Color.a);
			//	UNITY_APPLY_FOG(tIn.fogCoord +_Fogthr, CloudTex5);
				//tIn.fogCoord = ComputeFogFactor(tIn.pos.z);
				tIn.fogCoord =  InitializeInputDataFog(float4(tIn.posWorld, 1.0), 1);
				CloudTex5.rgb = MixFog(CloudTex5.rgb, InitializeInputDataFog(float4(tIn.posWorld, 1.0), 1));

				return float4(CloudTex5.rgb ,CloudTex4*_Color.a * saturate( depthDiff*0.1));
				//return float4(CloudTex5.rgb ,CloudTex.a);
			}

			ENDHLSL
		}
		//					 Pass
  //      {
  //          Tags { "LightMode"="ShadowCaster"}
		////	Blend SrcAlpha OneThrusSrcAlpha
		//
		//		//ColorMask 0
		//		Cull back
		//		HLSLPROGRAM
		//		#pragma vertex vert_Main
		//	#pragma fragment frag_Main
		//		#pragma multi_compile_instancing
		//						 #define SHADOWS_SEMITRANSPARENT 
		//						 //#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
		//		 #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
		//	
		//	sampler2D		_BaseTex;
		//	float4			_BaseTex_ST;
		//	sampler2D		_MaskTex;
		//	float4			_MaskTex_ST;
		//	float			_CloudDst;
		//	float4			_BaseTexSpeed;
		//	float4			_MaskSpeed;
		//	float			_CloudSmooth;
		//	float3 _LightDirection;
		//	
		//	float			_ShadowAlphaThr;
		//	float			_CloudAlphaThr;
		//	float			_CloudThr;
		//	float			_CloudAlphaSmooth;
		//		
		//	struct VertexInput
		//	{ 
		//		float4  vertex	: POSITION;
		//		float3  posWorld	: TEXCOORD0;
		//		float3  normal	: NORMAL;
		//		UNITY_VERTEX_INPUT_INSTANCE_ID
  //          };	
		//	
		//	
		//	struct v2f { 
		//		float4  vertex	: SV_POSITION;
		//		float3  posWorld	: TEXCOORD0;
		//	//	float3  normal	: NORMAL;
		//		UNITY_VERTEX_INPUT_INSTANCE_ID
  //          };

  //          v2f vert_Main(VertexInput v)
  //          {
		//		v2f o;
		//		UNITY_SETUP_INSTANCE_ID(v);
		//		UNITY_TRANSFER_INSTANCE_ID(v, o);
		//		float3 posWorld = TransformObjectToWorld(v.vertex.xyz);
		//		float3 world_nml = TransformObjectToWorldNormal(v.normal);
		//		o.posWorld = posWorld;
		//		//o.normal = world_nml;
		//		o.vertex = TransformWorldToHClip(ApplyShadowBias(posWorld, world_nml, _LightDirection.xyz));
  //             // TRANSFER_SHADOW_CASTER_NORMALOFFSET(o)
  //              return o;
  //          }

  //          float4 frag_Main(v2f i) : SV_Target
  //          {
		//		UNITY_SETUP_INSTANCE_ID(i);
		//		float3 CloudTex = tex2D(_MaskTex, i.posWorld.xz *_MaskTex_ST.xy * 0.1 + _Time.xx * _MaskSpeed.xy * 0.1);
		//		float3 foamtex = tex2D(_BaseTex, (i.posWorld.xz *0.5+CloudTex*_CloudDst) * _BaseTex_ST.xy * 0.1 + _Time.xx * _BaseTexSpeed.xy * 0.1);
		//		float3 foamtex2 = tex2D(_BaseTex, 1-(i.posWorld.xz +CloudTex*_CloudDst) * _BaseTex_ST.xy * 0.1 + _Time.xx * _BaseTexSpeed.zw * 0.1);
		//		float3 foamtex3 = saturate((foamtex.g+foamtex2.g) - _CloudSmooth);
		//		float foamtex5 = saturate(smoothstep(_CloudAlphaThr - _CloudAlphaSmooth, _CloudAlphaThr + _CloudAlphaSmooth, foamtex3));
		//		clip( foamtex5 - _ShadowAlphaThr);
  //              
		//		return float4(0, 0, 0,0);
		//		// SHADOW_CASTER_FRAGMENT(i )
		//	}

  //          ENDHLSL
  //      }
	}
	 
	//FallBack "Diffuse"
}