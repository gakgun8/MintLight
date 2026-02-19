// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "DK2_URP_Shader/Map/BG_Splatmap"
{
	Properties
	{
		_Intencity("Intencity", float) = 1
		[HDR] _TintColor("Tint Color", Color) = (1, 1, 1, 1)
		_SplatMap01("MaskTex (RGB)", 2D) = "black" {}
		[Enum(UV_1,0,WPos,1,UV_3,2,UV_4,3)] _MASK_UV("Using UV", int) = 1
		
		[Space(30)]
		[HDR] _BaseTexColor("baseTex Color", Color) = (1, 1, 1, 1)
		[HDR] _BasespecColor("baseSpec Color", Color) = (1, 1, 1, 1)
		_Albedo01("BaseTex (RGB)", 2D) = "black" {}
		_BumpMap01("Base Bumpmap (bump)", 2D) = "bump" {}
	
		[Enum(UV_1,0,WPos,1,UV_3,2,UV_4,3)] _Base_UV("Using UV", int) = 1
		
		[Space(30)]
		[HDR] _RedTexColor("RedTex Color", Color) = (1, 1, 1, 1)
		[HDR] _RedspecColor("RedSpec Color", Color) = (1, 1, 1, 1)
		_Albedo02("RedTex (RGB)", 2D) = "black" {}
		_BumpMap02("Red Bumpmap (bump)", 2D) = "bump" {}
	
		[Enum(UV_1,0,WPos,1,UV_3,2,UV_4,3)] _Red_UV("Using UV", int) = 1
		
		[Space(30)]
		[HDR] _GreenTexColor("GreenTex Color", Color) = (1, 1, 1, 1)
		[HDR] _GreenspecColor("GreenSpec Color", Color) = (1, 1, 1, 1)
		_Albedo03("GreenTex (RGB)", 2D) = "black" {}
		_BumpMap03("Green Bumpmap (bump)", 2D) = "bump" {}
	
		[Enum(UV_1,0,WPos,1,UV_3,2,UV_4,3)] _Green_UV("Using UV", int) = 1
		
		[Space(30)]
		[HDR] _BlueTexColor("BlueTex Color", Color) = (1, 1, 1, 1)
		[HDR] _BluespecColor("BlueSpec Color", Color) = (1, 1, 1, 1)
		_Albedo04("BlueTex (RGB)", 2D) = "black" {}
		_BumpMap04("Blue Bumpmap (bump)", 2D) = "bump" {}
	
		[Enum(UV_1,0,WPos,1,UV_3,2,UV_4,3)] _Blue_UV("Using UV", int) = 1
		
		[Space(30)]
		[HDR] _AlphaTexColor("AlphaTex Color", Color) = (1, 1, 1, 1)
		[HDR] _AlphaspecColor("AlphaSpec Color", Color) = (1, 1, 1, 1)
		_Albedo05("AlphaTex (RGB)", 2D) = "black" {}
		_BumpMap5("Blue Bumpmap (bump)", 2D) = "bump" {}
		
		[Enum(UV_1,0,WPos,1,UV_3,2,UV_4,3)] _Alpha_UV("Using UV", int) = 1

		
		[MaterialToggle] _Nosaturate("Nosaturate Ambient", float) = 0
		_SpecPos("Spec Pos", Vector) = (2.6, 5.4, -10, 0)
		_Gloss("Gloss", Range(0, 10)) = 0.5
		_GlossPow("Glosspow", Range(0, 1)) = 0.5
	}

	SubShader
	{
		Tags
		{
			"Queue" = "Geometry"
			"RenderType" = "Opaque"
			"RenderPipeline" = "UniversalPipeline"
		}

		Blend SrcAlpha OneMinusSrcAlpha
		ZWrite on
		Cull back
		Pass
		{
			Tags
			{
				"LightMode" = "UniversalForward"
			}

			HLSLPROGRAM
			#pragma prefer_hlslcc gles
			#pragma exclude_renderers d3d11_9x
			#pragma vertex vert_Main
			#pragma fragment frag_Main
			#pragma multi_compile_instancing
			#pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
			#pragma multi_compile _ _ADDITIONAL_LIGHTS 
			#pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
			#pragma multi_compile _ _SHADOWS_SOFT
			#pragma multi_compile_fragment _ _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
			#pragma multi_compile_fragment _ _LIGHT_COOKIES
			#pragma multi_compile_fog

			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
			#include "../Lighting.hlsl"
			#include "../RealtimeLights.hlsl"
			//#include "../DK2_URP_ColorLib.hlsl"

			CBUFFER_START(UnityPerMaterial)

			sampler2D _SplatMap01,_Albedo01,_Albedo02,_Albedo03,_Albedo04,_Albedo05,_BumpMap01,_BumpMap02,_BumpMap03,_BumpMap04,_BumpMap05;
			
			float4 _SplatMap01_ST,_Albedo01_ST,_Albedo02_ST,_Albedo03_ST,_Albedo04_ST,_Albedo05_ST , _BumpMap01_ST, _BumpMap02_ST , _BumpMap03_ST, _BumpMap04_ST, _BumpMap05_ST;
			

			float		_Intencity;
			float		_LMPos;
			float4		_SpecPos;
			float		_WPOSUseLightMap;
			float		_UseLightMap;

			float		_Base_UV;
			float		_MASK_UV;
			float		_Red_UV;
			float		_Green_UV;
			float		_Blue_UV;
			float		_Alpha_UV;
			float		_Worldpos_UV;
			float		_Gloss;
			float		_GlossPow;
			float4		_TintColor;
			float4		_BaseTexColor;
			float4		_RedTexColor;
			float4		_GreenTexColor;
			float4		_BlueTexColor;
			float4		_AlphaTexColor;
			float4		_BasespecColor , _RedspecColor, _GreenspecColor, _BluespecColor, _AlphaspecColor;
			float		_Nosaturate;

			CBUFFER_END

			struct VertexInput
			{
				float4	vertex		: POSITION;
				float4	v4Color		: COLOR0;
				float3 Normal		: NORMAL;
				float2  v2UV		: TEXCOORD0;	//BaseTex select 0
				float2  v2UV_LM		: TEXCOORD1;	//LightMapTex
				float2  v2UV2		: TEXCOORD2;	//BaseTex select 1
				float2  v2UV3		: TEXCOORD3;	//MaskTex
				float3 worldPos		: TEXCOORD4;
				float4 tangent 		: TANGENT;
				

				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct tOUTPUT
			{
				float4  vertex : SV_POSITION;
				float4	v4Color : COLOR0;
				float3 Normal : NORMAL;
				float2  v2UV	: TEXCOORD0;	//BaseTex select 0
				float2  v2UV_LM : TEXCOORD1;	//LightMapTex
				float2  v2UV2	: TEXCOORD2;	//BaseTex select 1
				float2  v2UV3	: TEXCOORD3;	//MaskTex
				float3 	worldPos : TEXCOORD4;
				float 	fogCoord : TEXCOORD5;
				float4	 shadowCoord : TEXCOORD6;
				float3	tangent		: TEXCOORD7;
				float3  bitangent	: TEXCOORD8;
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			float2 SelectUV(int idx, tOUTPUT tOut)
			{
				if (idx == 0)
					return tOut.v2UV;
				if (idx == 1)
					return tOut.worldPos.xz * 0.15;
				if (idx == 2)
					return tOut.v2UV2;
				if (idx == 3)
					return tOut.v2UV3;

				return 0;
			}

			tOUTPUT vert_Main(VertexInput v)
			{
				tOUTPUT	tOut;
				UNITY_SETUP_INSTANCE_ID(v);
				UNITY_TRANSFER_INSTANCE_ID(v, tOut);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(tOut);
				tOut.vertex = TransformObjectToHClip(v.vertex.xyz);
				tOut.v2UV = v.v2UV;
				tOut.v2UV_LM = v.v2UV_LM;
				tOut.v2UV2 = v.v2UV2;
				tOut.v2UV3 = v.v2UV3;
				tOut.v4Color = v.v4Color;
				tOut.worldPos = TransformObjectToWorld(v.vertex.xyz);
				tOut.Normal = TransformObjectToWorldNormal(v.Normal);
				tOut.tangent = TransformObjectToWorldDir(v.tangent.xyz);
				tOut.bitangent = cross(tOut.Normal, tOut.tangent) * v.tangent.w;
				tOut.fogCoord = ComputeFogFactor(tOut.vertex.z);
				tOut.shadowCoord = float4(0, 0, 0, 0);
				VertexPositionInputs vertexInput = GetVertexPositionInputs(v.vertex.xyz);

				return tOut;
			}

			float4 frag_Main(tOUTPUT tIn) : SV_Target
			{
				UNITY_SETUP_INSTANCE_ID(tIn);
				UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(tIn);

				

		
			//	float2 WorldposUV = tIn.worldPos.xz * 0.2;
				float4 f4MaskTexColor = tex2D(_SplatMap01, TRANSFORM_TEX(SelectUV(_MASK_UV,tIn), _SplatMap01));
				float4 f4BaseTexColor = tex2D(_Albedo01, TRANSFORM_TEX(SelectUV(_Base_UV, tIn), _Albedo01));
				float4 f4RedTexColor = tex2D(_Albedo02, TRANSFORM_TEX(SelectUV(_Red_UV, tIn), _Albedo02));
				float4 f4GreenTexColor = tex2D(_Albedo03,TRANSFORM_TEX(SelectUV(_Green_UV, tIn), _Albedo03));
				float4 f4BlueTexColor = tex2D(_Albedo04,  TRANSFORM_TEX(SelectUV(_Blue_UV, tIn), _Albedo04));
				float4 f4alphaTexColor = tex2D(_Albedo05, TRANSFORM_TEX(SelectUV(_Alpha_UV, tIn), _Albedo05));
				
				
				
				 f4MaskTexColor.a *= _BasespecColor.a;
				 f4BaseTexColor.a *= _BasespecColor.a;
				 f4RedTexColor.a *=_RedspecColor.a ;
				 f4GreenTexColor.a *= _GreenspecColor.a;
				 f4BlueTexColor.a *= _BluespecColor.a;
				 f4alphaTexColor.a *= _AlphaspecColor.a;


				float3  baseNormal = UnpackNormal(tex2D(_BumpMap01,   TRANSFORM_TEX(SelectUV(_Base_UV, tIn), _Albedo01)));
				float3  RedNormal = UnpackNormal(tex2D(_BumpMap02,  TRANSFORM_TEX(SelectUV(_Red_UV, tIn), _Albedo02)));
				float3  GreenNormal = UnpackNormal(tex2D(_BumpMap03,  TRANSFORM_TEX(SelectUV(_Green_UV, tIn), _Albedo03)));
				float3  BlueNormal = UnpackNormal(tex2D(_BumpMap04, TRANSFORM_TEX(SelectUV(_Blue_UV, tIn), _Albedo04)));
				float3  AlphaNormal = UnpackNormal(tex2D(_BumpMap05,  TRANSFORM_TEX(SelectUV(_Alpha_UV, tIn), _Albedo05)));
				
	
				//float mask1 = saturate((f4MaskTexColor.r + f4RedTexColor.a) * f4MaskTexColor.r * 2);
				//float mask2 = saturate((f4MaskTexColor.g + f4GreenTexColor.a) * f4MaskTexColor.g * 2);
				//float mask3 = saturate((f4MaskTexColor.b + f4BlueTexColor.a) * f4MaskTexColor.b * 2);
				//float mask4 = saturate((f4MaskTexColor.a + f4alphaTexColor.a) * f4MaskTexColor.a * 2);

				float4 f4Red = float4(lerp(float4(f4BaseTexColor.rgb,f4BaseTexColor.a * _RedspecColor.r) * _BaseTexColor,float4( f4RedTexColor.rgb,f4RedTexColor.a) * _RedTexColor, f4MaskTexColor.r)) ;
				float4 f4Green = float4(lerp(f4Red, f4GreenTexColor * _GreenTexColor, f4MaskTexColor.g));
				float4 f4Blue = float4(lerp(f4Green, f4BlueTexColor * _BlueTexColor, f4MaskTexColor.b));
				float4 f4alpha = float4(lerp(f4Blue, f4alphaTexColor * _AlphaTexColor, f4MaskTexColor.a));

				
			
				
				
				float3 N4Red = lerp(baseNormal.rgb , RedNormal.rgb , f4MaskTexColor.r);
				float3 N4Green = lerp(N4Red.rgb, GreenNormal.rgb , f4MaskTexColor.g);
				float3 N4Blue = lerp(N4Green.rgb, BlueNormal.rgb , f4MaskTexColor.b);
				float3 N4alpha = lerp(N4Blue.rgb, AlphaNormal.rgb , f4MaskTexColor.a);



				
				//float3 Noraml = 
				float3 camerapos = _WorldSpaceCameraPos.xyz;
				float3 normalDirection = normalize(tIn.Normal);
				float3x3 tangentTransform = float3x3(tIn.tangent, tIn.bitangent, tIn.Normal);
				normalDirection = normalize(mul(N4alpha, tangentTransform));
				float3 ViewDir = normalize(camerapos - tIn.worldPos.xyz);
				float3 lightdir = normalize(_MainLightPosition.xyz);
				
						
					// 쉐도우 	
					tIn.shadowCoord = TransformWorldToShadowCoord(tIn.worldPos);

					// 라이팅 
					Light mainLight = GetMainLight(tIn.shadowCoord);
					float3 ambient = SampleSH(normalDirection);
					ambient = _Nosaturate ? saturate(ambient) : ambient ;
					float NdotL = max(0,dot(lightdir, normalDirection));
					float3 finalDiff =  NdotL;
					//float3 finalDiff = ( NdotL *  _MainLightColor.rgb*mainLight.shadowAttenuation *mainLight.distanceAttenuation+ambient);

					
						
				//float3 customlight =normalize(_CustomLightpos.xyz - tIn.worldPos.xyz);
				
				float3 halfDir = normalize(ViewDir + lightdir * mainLight.distanceAttenuation);

				float3 fianlhalfDir =  halfDir;  
		
			///////// Gloss:
				float gloss = 1 *f4alpha.a ;
				float specPow = exp2(gloss * 10.0 + 1.0);
			//Specular0
				float3 SpeDir = max(0.0, dot(fianlhalfDir, normalDirection));
				

				
				
				
			//float SpeDir1 = saturate(dot(halfDir, Normal));
				float3 SpecCol = pow(abs(SpeDir ), specPow)  * _BasespecColor.rgb *_MainLightColor.rgb;
				finalDiff.rgb +=  SpecCol * f4alpha.a ;
				//finalDiff.rgb +=+ AddlightSpec;

				finalDiff *= _MainLightColor.rgb*mainLight.shadowAttenuation * mainLight.distanceAttenuation;
				finalDiff += ambient;			
							

				#ifdef _ADDITIONAL_LIGHTS
					uint pixelLightCount = GetAdditionalLightsCount();
					for (uint lightIndex = 0u; lightIndex < pixelLightCount; ++lightIndex)
					{
						Light addLight = GetAdditionalLight(lightIndex, tIn.worldPos,1);
						float distance = saturate(addLight.distanceAttenuation);
						float Shadowdist =addLight.shadowAttenuation;
						float3 addLightCol =  max(0, dot(addLight.direction, normalDirection));
						finalDiff +=  addLightCol * addLight.color.rgb * Shadowdist *  distance;
					};



					#endif


				float3 f4final = f4alpha.rgb * finalDiff.rgb  * _Intencity;

				f4final.rgb *= _TintColor.rgb;
				f4final.rgb = MixFog(f4final.rgb, InitializeInputDataFog(float4(tIn.worldPos, 1.0), 1));

				//Apply Color Ramp
				//f4final.rgb = ApplyEnvColorRamp( _global_UseRamp, f4final.rgb, _global_Ramp_Map, _global_EnvPct_Map );

				

				return float4(f4final,  tIn.v4Color.r);
			}

			ENDHLSL
		}

		Pass
		{
			Name "ShadowCaster"

			Tags 
			{
				"LightMode" = "ShadowCaster"
			}

			Cull Back

			HLSLPROGRAM

			#pragma prefer_hlslcc gles
			#pragma exclude_renderers d3d11_9x
			#pragma target 2.0

			#pragma vertex ShadowPassVertex
			#pragma fragment ShadowPassFragment

			// GPU Instancing
			#pragma multi_compile_instancing

			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
			//#include "Packages/com.unity.render-pipelines.universal/Shaders/LitInput.hlsl"
			//#include "Packages/com.unity.render-pipelines.universal/Shaders/ShadowCasterPass.hlsl"

			CBUFFER_START(UnityPerMaterial)
			CBUFFER_END

			struct VertexInput
			{
				float4 vertex : POSITION;
				float4 normal : NORMAL;

				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct VertexOutput
			{
				float4 vertex : SV_POSITION;

				UNITY_VERTEX_INPUT_INSTANCE_ID
				UNITY_VERTEX_OUTPUT_STEREO
			};

			VertexOutput ShadowPassVertex(VertexInput v)
			{
				VertexOutput o;
				UNITY_SETUP_INSTANCE_ID(v);
				UNITY_TRANSFER_INSTANCE_ID(v, o);

				float3 positionWS = TransformObjectToWorld(v.vertex.xyz);
				float3 normalWS = TransformObjectToWorldNormal(v.normal.xyz);

				float4 positionCS = TransformWorldToHClip(ApplyShadowBias(positionWS, normalWS, float3(0,0,0)));

				o.vertex = positionCS;

				return o;
			}

			half4 ShadowPassFragment(VertexOutput i) : SV_TARGET
			{
				UNITY_SETUP_INSTANCE_ID(i);
				return 0;
			}

			ENDHLSL
		}

		Pass
		{
			Name "DepthOnly"

			Tags
			{
				"LightMode" = "DepthOnly"
			}

			ZWrite On
			ColorMask 0

			Cull Back

			HLSLPROGRAM

			#pragma prefer_hlslcc gles
			#pragma exclude_renderers d3d11_9x
			#pragma target 2.0

			// GPU Instancing
			#pragma multi_compile_instancing

			#pragma vertex vert
			#pragma fragment frag

			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

			CBUFFER_START(UnityPerMaterial)
			CBUFFER_END

			struct VertexInput
			{
				float4 vertex : POSITION;
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct VertexOutput
			{
				float4 vertex : SV_POSITION;

				UNITY_VERTEX_INPUT_INSTANCE_ID
				UNITY_VERTEX_OUTPUT_STEREO
			};

			VertexOutput vert(VertexInput v)
			{
				VertexOutput o;
				UNITY_SETUP_INSTANCE_ID(v);
				UNITY_TRANSFER_INSTANCE_ID(v, o);

				o.vertex = TransformWorldToHClip(TransformObjectToWorld(v.vertex.xyz));

				return o;
			}

			half4 frag(VertexOutput IN) : SV_TARGET
			{
				return 0;
			}

			ENDHLSL
		}
	}
}