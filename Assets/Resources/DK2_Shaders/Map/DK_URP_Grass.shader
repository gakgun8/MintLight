Shader "DK2_URP_Shader/Map/BG_Plant_Grass"
{
	Properties
	{
		
		[Enum(OFF,0,FRONT,1,BACK,2)] _CullMode("Cull Mode", int) = 2
		_Intencity("Intencity", Range(0, 10)) = 1.0
		[HDR]_Color("Main Color", Color) = (1,1,1,1)
		[MaterialToggle] _ReplaceColorOn("ReplaceColor On", float) = 0                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                       
		_ReplaceColor("ReplaceColor", Color) = (1,1,1,1)
		_MainTex("Base (RGB)", 2D) = "white" {}
		[Space(20)]
		[Header(CastShadow________________________________________________________________________________________________)]



		[Space(20)]
		[Header(Ambient Color________________________________________________________________________________________________)]
		[Space(10)]
		[MaterialToggle] _NoAmbient ("No Ambient ", float) = 0
		[MaterialToggle] _ReplaceAmbient("ReplaceAmbient Color", float) = 0
		_AmbentColor("Custom Ambient Color", Color) = (1,1,1,1)
		[MaterialToggle] _NosatAmbient("NoSaturate Ambient", float) = 0

		[Space(10)]
		[Header(VertexColor Color________________________________________________________________________________________________)]
		[Space(10)]
		[MaterialToggle] _Vcolor("Vertex Color on (Red Color):Defaut Color ", float) = 0
		[HDR]_SColor("Vertex Color Shadow", Color) = (1,1,1,1)
		[Toggle(Leaf)]_Leaf("Leaf on",float ) = 0
		_WorldposY("WorldposY ", vector ) =  (0,0,0,0)
		_InnerAOPower("InnerAOPower ", range(0,1) ) = 0
		_InnerAOLength("InnerAOLength ", range(0,1) ) = 1
				
		

		[Space(10)]
		[Header(Alpha Cut________________________________________________________________________________________________)]
		[Space(10)]
		
		[MaterialToggle] _AlphaCut("Alpha Cut", float) = 0
		[MaterialToggle] _AlphaBlend("Alpha Blend", float) = 0
		_Alpha("Alpha", range(0,1)) = 1
		_AlphaCutoff("Alpha Cutoff", Range(0, 1)) = 0.5
		
		

		[Space(10)]
		[Header(Vertex Animation________________________________________________________________________________________________)]
		[MaterialToggle] _VertexAni("Vertex Animation", float) = 0
		_MoveTex("RandTex (RGB)", 2D) = "Gray" {}
		_MoveAmount("Wind Strength",  float) = 0.5
		_MoveSpeed("Wind Speed",  float) = 5
		_WindFrequency("Wind Frequency",  float) = 1
		_WindDirection("Wind Direction", Vector) = (1, 1, 0)


		//   [Header(_________________________________________________________________________________________)]
        // [Header(SSS)]
        // [Space(20)]
        // _SSSColor("SSS Color", Color) = (0, 0, 0, 1)
        // _SSSDistortion("SSS Distortion", Float) = 1
        // _SSSPower("SSS Power", Float) = 1
        // _SSSScale("SSS Scale", Float) = 1
        // _SSSAttenuation("SSS Attenuation", Range(0, 2)) = 1
        // _SSSThickness("SSS Thickness", Float) = 1
        // _SSSFrontSpread( "SSS FrontSpread", Range( 0, 1 ) ) = 0




		 [Space(20)]

		[Header(Fog on off________________________________________________________________________________________________)]
		[Toggle(Fogon)] _Fogon ("Fog on", float) = 1
		[Space(20)]
		[Header(ZWrite________________________________________________________________________________________________)]
		[Enum(Off, 0, On, 1)] _ZWrite("ZWrite", Float) = 1
	}

	SubShader
	{

		LOD 0
		Tags
		{
			"RenderPipeline" = "UniversalPipeline"
			"RenderType" = "Opaque"
			"IgnoreProjector" = "True"
			"Queue" = "Geometry"
		}

		Cull[_CullMode]
 		Name "Universal Forward"
		Pass
		{
			Tags 
			{ 
				"LightMode" = "UniversalForward"
			}
			
			ZWrite[_ZWrite]
		
			//Blend SrcAlpha OneMinusSrcAlpha
			HLSLPROGRAM
			#pragma vertex vert_Main
			#pragma fragment frag_Main 

			#pragma multi_compile_instancing
			#pragma multi_compile _ _MAIN_LIGHT_SHADOWS 
			#pragma multi_compile _ _ADDITIONAL_LIGHTS 
			#pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
			#pragma multi_compile_fragment _ _LIGHT_COOKIES
			#pragma multi_compile_fragment _ _SHADOWS_SOFT
			//#pragma multi_compile_fragment _ LOD_FADE_CROSSFADE
			//#pragma  shader_feature  VertexAnimation
			//#pragma multi_compile_fragment AlphaCutOff
			//#pragma multi_compile_feature AlphaBlend
			//#pragma shader_feature  Leaf
			#pragma multi_compile_fog
			
			
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
			#include "../Lighting.hlsl"
			//#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/LODCrossfade.hlsl"
			//#include "../DK2_URP_ColorLib.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
		
			CBUFFER_START(UnityPerMaterial)

			sampler2D _MainTex, _MoveTex;
			
			
			float4 _MainTex_ST, _MoveTex_ST,
					_SColor, _Color,
					_ReplaceColor, _WorldposY;
		
			float  _VertexAni, _MoveAmount, _MoveSpeed, _WindFrequency ,_AlphaCut,_AlphaBlend,_Alpha,_Intencity,_AlphaCutoff,
			_InnerAOPower,_InnerAOLength,_Leaf,_NosatAmbient,_ReplaceAmbient,_NoAmbient, 
			_Vcolor,_CustomLight,_ReplaceColorOn;
	
			float3 _WindDirection,_CustomLightpos,_AmbentColor;
			
			CBUFFER_END
		
			
			struct Vertexinput
			{
				float4  vertex		: POSITION;
				float2  v2UV		: TEXCOORD0;
				float3  normal		: NORMAL;
				
				float4  vcolor		: COLOR0;
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct tOUTPUT
			{
				float4  vertex		: POSITION;
				float4  vcolor		: COLOR0;
				float3  Normal		: NORMAL;
				float2  v2UV		: TEXCOORD0;
				float3	worldPos	: TEXCOORD1;
				
			
				
				float   fogCoord	: TEXCOORD2;
				float4 shadowCoord	: TEXCOORD3;
				float4 sPos			: TEXCOORD4;
				
		

			

				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			
			float3 VertexPositionBasedRandomMovement(float3 worldPos)
			{
				float timedWindSpeed = _Time.x * _MoveSpeed;
				float randWindPower = tex2Dlod(_MoveTex, float4(worldPos.xz * 0.1f * _WindFrequency + timedWindSpeed, 0, 0)).r * _MoveAmount;
				float3 normalizedWindDir = normalize(_WindDirection);
				return normalizedWindDir * randWindPower;
			}




			
	




			tOUTPUT vert_Main(Vertexinput v)
			{
				tOUTPUT	tOut;
				UNITY_SETUP_INSTANCE_ID(v);
				UNITY_TRANSFER_INSTANCE_ID(v, tOut);

				tOut.worldPos = TransformObjectToWorld(v.vertex.xyz);
				
				tOut.vertex = TransformObjectToHClip(v.vertex.xyz);
				
		


				


				tOut.sPos = ComputeScreenPos(tOut.vertex);
				tOut.v2UV = v.v2UV;
				tOut.Normal = TransformObjectToWorldNormal(v.normal);



				tOut.vertex.xyz += _VertexAni ? float3(VertexPositionBasedRandomMovement(tOut.worldPos).xyz * v.vcolor.g *  tOut.v2UV.y): 0 ;			

	



				tOut.vcolor = v.vcolor;
				
				
				
				float Height= v.vertex.y ;
				tOut.vcolor.b = Height;			
				tOut.fogCoord = ComputeFogFactor(tOut.vertex.z);
				tOut.shadowCoord = float4(0, 0, 0, 0);

				
				return tOut;
			}



			float4 frag_Main(tOUTPUT tIn) : SV_Target
			{
				UNITY_SETUP_INSTANCE_ID(tIn);

				
				float4	f4TexColor = tex2D(_MainTex, TRANSFORM_TEX(tIn.v2UV, _MainTex)) ;
				//f4TexColor *= _Color;
				
				f4TexColor.rgb = _ReplaceColorOn ? _ReplaceColor.rgb : f4TexColor.rgb;
			
			
			


				float3 normalDirection = normalize(tIn.Normal);

				// 그림자
				tIn.shadowCoord = TransformWorldToShadowCoord(tIn.worldPos);

				//cameraview
				float3 camerapos = _WorldSpaceCameraPos.xyz;
				//Normal
			

				//float4 ScreenPos = tIn.sPos;
				//float4 screentposNorm = ScreenPos / ScreenPos.w;
				//float eyeDepth43 = LinearEyeDepth(SampleSceneDepth(screentposNorm.xy), _ZBufferParams);
				//float  depthDiff = eyeDepth43 - LinearEyeDepth(screentposNorm.z, _ZBufferParams);
			
				
				//float BlendTex1 = smoothstep(0,0.5,BlendTex2) ;	

		



		
			
				// 메인라이팅 
				
				Light mainLight = GetMainLight(tIn.shadowCoord);
				




				float3 lightDir = normalize(_MainLightPosition.xyz);
				float3 Mainlightcol = _MainLightColor.rgb;
				float3 ambientColor =lerp(SampleSH(normalDirection) , _AmbentColor ,_ReplaceAmbient);
				
				//#ifdef Leaf

				
				float NdotL = _NoAmbient ? 1 : max(0, dot(lightDir, normalDirection));

				
				//#ifdef Leaf
				//float3 UnmapedNorml = tIn.worldPos - TransformObjectToWorld(float3(0,0,0)) ;
				//float NdotL = (max(0, dot(lightDir, normalize( UnmapedNorml) ) ));
				//NdotL += pow ( NdotL,10)*2;
			//	float occlusion = 1-(cos(tIn.InnerAo.x)) * (cos(tIn.InnerAo.y)) * (cos(tIn.InnerAo.z)) ;
				
			//	occlusion = smoothstep( _InnerAOPower , _InnerAOLength , occlusion) ;

			//	#else
			//	float NdotL = _NoAmbient ? 1 : max(0, dot(lightDir, normalDirection));
			//	#endif
				
				
				
		
				
				
				ambientColor = _NosatAmbient ? saturate(ambientColor) : ambientColor ; 

				
				
				
				float3 finalDiff = NdotL*  Mainlightcol * mainLight.shadowAttenuation * mainLight.distanceAttenuation + ambientColor;
			//	float3 ViewDir = normalize(camerapos - tIn.worldPos.xyz);





			
				#ifdef _ADDITIONAL_LIGHTS
				uint pixelLightCount = GetAdditionalLightsCount();
				for (uint lightIndex = 0u; lightIndex < pixelLightCount; ++lightIndex)
				{
					
					Light addLight = GetAdditionalLight(lightIndex, tIn.worldPos,half4(0,0,0,0));
					float distance = addLight.distanceAttenuation;
					float Shadowdist = addLight.shadowAttenuation;
					float3 addLightCol = _NoAmbient ? 1 : max(0, dot(addLight.direction, normalDirection));
					finalDiff +=  addLightCol * addLight.color.rgb * Shadowdist *  distance*0.2;
					
				};
				#endif
				finalDiff *= f4TexColor.rgb * _Color.rgb;

				//ViewDir
		
				
				
				
		
	

				//Vertex Color
				finalDiff = _Vcolor ? (lerp( finalDiff*_SColor.rgb, finalDiff,  tIn.vcolor.r)) : finalDiff;

				// Glow Texture
			
				


			
				

		

				//Alpha
			
				float alphacutoff =  _AlphaCut? f4TexColor.a  - _AlphaCutoff : 1;
				clip( alphacutoff);
			
		 				
							
			//	float alpha = _AlphaBlend?  _Alpha * f4TexColor.a : 1;
			


			// 	float3  f3Half      = ( _MainLightPosition.xyz + normalDirection * _SSSDistortion );
            //     float   fVoH        = pow( saturate( dot( ViewDir, -f3Half ) ), _SSSPower + 0.001 ) * _SSSScale;
            //     float3  f3SSSColor  = _SSSAttenuation* ( fVoH + _SSSFrontSpread ) * _SSSThickness * _SSSColor.rgb * mainLight.color.rgb* mainLight.shadowAttenuation;

            //   finalDiff.rgb    = float3( finalDiff.xyz + f3SSSColor );





	

			

				finalDiff.rgb *=  _Intencity ;

				//Fog On off
			
				finalDiff.rgb =  MixFog(finalDiff.rgb, InitializeInputDataFog(float4(tIn.worldPos, 1.0), 1));
				//Apply Color Ramp
			//	finalDiff = ApplyEnvColorRamp( _global_UseRamp, finalDiff, _global_Ramp_Map, _global_EnvPct_Map );
			
				
				
				
				#ifdef LOD_FADE_CROSSFADE
    				 LODDitheringTransition( tIn.vertex.xyz,  unity_LODFade.x);

				#endif



				return float4(finalDiff.rgb , 1);
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
			ZWrite[_ZWrite]
            ZTest LEqual
            ColorMask 0
			Cull[_CullMode]
			
			HLSLPROGRAM
			//#pragma prefer_hlslcc gles
			//#pragma exclude_renderers d3d11_9x
			#pragma vertex vert_Main
			#pragma fragment frag_Main
			#pragma multi_compile_instancing
			#pragma shader_feature VertexAnimation
			//#pragma shader_feature AlphaCutOff
			
			//#pragma multi_compile _ _LIGHT_LAYERS
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
			#include "../Lighting.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"
			
			
					CBUFFER_START(UnityPerMaterial)

			sampler2D _MainTex, _MoveTex;
			
			
			float4 _MainTex_ST, _MoveTex_ST,
					_SColor, _Color,
					_ReplaceColor, _WorldposY;
		
			float  _VertexAni, _MoveAmount, _MoveSpeed, _WindFrequency ,_AlphaCut,_AlphaBlend,_Alpha,_Intencity,_AlphaCutoff,
			_InnerAOPower,_InnerAOLength,_Leaf,_NosatAmbient,_ReplaceAmbient,_NoAmbient, 
			_Vcolor,_CustomLight,_ReplaceColorOn;
	
			float3 _WindDirection,_CustomLightpos,_AmbentColor;
			
			CBUFFER_END

			//float3 _LightDirection;
			//float3 _LightPosition;




			struct VertexInput
			{
				float4 vertex	: POSITION;
				float4 normal	: NORMAL;
				float2  v2UV	: TEXCOORD0;
				float4  vcolor		:COLOR0;
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct tOUTPUT
			{
				float4 vertex		: SV_POSITION;
				float2  v2UV		: TEXCOORD0;
				float4  vcolor		:COLOR0;
				UNITY_VERTEX_INPUT_INSTANCE_ID
				//UNITY_VERTEX_OUTPUT_STEREO
			};
			
						float3 VertexPositionBasedRandomMovement(float3 worldPos)
						{
							float timedWindSpeed = _Time.x * _MoveSpeed;
							float randWindPower = tex2Dlod(_MoveTex, float4(worldPos.xz * 0.1f * _WindFrequency + timedWindSpeed, 0, 0)).r * _MoveAmount ;
							float3 normalizedWindDir = normalize(_WindDirection);
							return normalizedWindDir * randWindPower;
						}
		




			tOUTPUT vert_Main(VertexInput v)
			{
				tOUTPUT tOut;
				UNITY_SETUP_INSTANCE_ID(v);
				UNITY_TRANSFER_INSTANCE_ID(v, tOut);
				tOut.v2UV = v.v2UV;
				tOut.vcolor = v.vcolor;
				float3 positionWS = TransformObjectToWorld(v.vertex.xyz);

				float3 normalWS = TransformObjectToWorldNormal(v.normal.xyz);
			
				

				positionWS += _VertexAni ? float3(VertexPositionBasedRandomMovement(positionWS).xyz * v.vcolor.g) : 0;
		
				
				float4 positionCS = TransformWorldToHClip(ApplyShadowBias(positionWS, normalWS, float3(0,0,0)));
				tOut.vertex = positionCS;
				return tOut;
			}

			half4 frag_Main(tOUTPUT tIn) : SV_TARGET
			{
				UNITY_SETUP_INSTANCE_ID(tIn);
				float4	f4TexColor = tex2D(_MainTex, tIn.v2UV);
				
				float alphacutoff =  f4TexColor.a - _AlphaCutoff ;
				clip(alphacutoff);
				
				return 0;
			}

			ENDHLSL
		}


		 
	}
}