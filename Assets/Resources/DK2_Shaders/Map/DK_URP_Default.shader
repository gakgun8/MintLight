Shader "DK2_URP_Shader/Map/BG_Default"
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
				
		[Header(Specular________________________________________________________________________________________________)]
		[Space(10)]
		[Toggle(Specular)]_Specular("Specular On", float) = 1
		[MaterialToggle]_CustomLight("Custom Light", float) =0 
		[HDR]_SpecColor("Specular Color", Color) = (0.5,0.5,0.5,1)
		_Smoothness("Specular Smoothness", Range(0,1)) = 0.5
		_CustomLightpos ("CustomLight pos", Vector) =(0,0,0,0)

			
		[Space(10)]
		[Header(Alpha Cut________________________________________________________________________________________________)]
		[Space(10)]
		
		[MaterialToggle] _AlphaCut("Alpha Cut", float) = 0
		[MaterialToggle] _AlphaBlend("Alpha Blend", float) = 0
		_Alpha("Alpha", range(0,1)) = 1
		_AlphaCutoff("Alpha Cutoff", Range(0, 1)) = 0.5
		
		
		
		[Space(10)]
		[Header(Blend Texture________________________________________________________________________________________________)]
		[Toggle(BlendTexture)] _BlendTexture ("Blend Textur on", float) = 0
		_BlendTexpos ("Blend Textur pos", float) = 0
		_BlendTexpos1 ("Blend Textur range", float) = 0
		[HDR]_BlendTexColor("BlendTex Color", Color) = (1,1,1,1)
		_BlendTex("BlendTex (RGB)", 2D) = "Gray" {}

	[Space(10)]
		[Header(NormalMap________________________________________________________________________________________________)]
		[Space(10)]
		[Toggle(Normalmap)] _Normalmap("Normalmap On", float) = 0
		_BumpMap("Bumpmap (bump)", 2D) = "bump" {}

	


		[Space(10)]
		[Header(Glow________________________________________________________________________________________________)]
		[MaterialToggle] _Glow("Glow on", float) = 0
		[HDR]_GlowColor("Glow Color", Color) = (0.5,0.5,0.5,0.5)
		[MaterialToggle] _GlowTexture("Glow Texture on", float) = 0
		[HDR]_GlowColorAni("Glow Ani Color", Color) = (0.5,0.5,0.5,0.5)
		
		_GlowTex("Glow Tex (RGB)", 2D) = "Black" {}
		_GlowPow("Glow Pow", float) = 1
		_Speed("Speed", Vector) = (1,1,1,1)
		
			
		[Space(10)]
		[Header(Vertex Animation________________________________________________________________________________________________)]
		[Toggle(VertexAnimation)] _VertexAni("Vertex Animation", float) = 0
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



		[Space(10)]
		[Header(Reflection________________________________________________________________________________________________)]
		[Space(10)]
		[Toggle(Reflection)] _Reflection("Reflection", float) = 0
		[Toggle(RealTimeReflection)] _RealTimeReflection("RealTimeReflection", float) = 0
		
		[HDR]_CubeColor("Cube Color", Color) = (1,1,1,1)
		_Cube("Cube", Cube) = ""{}
		_CubeIntencity("CubeIntencity", range(0,10)) = 1
		_Cubelerp("Cubelerp", Range(0,1)) = 1.0
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
			#pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
			#pragma multi_compile _ _ADDITIONAL_LIGHTS 
			#pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
			#pragma multi_compile_fragment _ _LIGHT_COOKIES
			#pragma multi_compile_fragment _ _SHADOWS_SOFT
			#pragma multi_compile_fragment _ _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
			#pragma multi_compile_fragment _ LOD_FADE_CROSSFADE
			#pragma multi_compile_fog
			
			#pragma shader_feature Normalmap
			//#pragma shader_feature Reflection
			//#pragma shader_feature Specular
			//#pragma shader_feature BlendTexture
			//#pragma shader_feature Glow
		

			//#pragma shader_feature RealTimeReflection
	
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
			#include "../Lighting.hlsl"
			//#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/LODCrossfade.hlsl"
			
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
		
			CBUFFER_START(UnityPerMaterial)

			sampler2D _MainTex, _GlowTex, _BumpMap, _BlendTex,_MoveTex,_ReflectionTexUp;
			samplerCUBE		_Cube;
			
			float4 _MainTex_ST, _MoveTex_ST, _BumpMap_ST, _GlowTex_ST, _BlendTex_ST,
					_SColor, _BlendTexColor,_GlowColor, _GlowColorAni,_Speed , _Color,
					_ReplaceColor, _SpecColor, _CubeColor,_WorldposY;
		
			float  _VertexAni, _MoveAmount, _MoveSpeed, _WindFrequency ,_AlphaCut,_AlphaBlend,_Alpha,_Intencity,_AlphaCutoff,
			_InnerAOPower,_InnerAOLength,_Leaf,_NosatAmbient,_ReplaceAmbient,_NoAmbient,_BlendTexture, 
			_Vcolor, _Glow, _GlowTexture,_GlowPow,_BlendTexpos,_BlendTexpos1,_Smoothness,_Normalmap, _Specular,_CustomLight,
			_ReplaceColorOn, _Reflection,_CubeIntencity, _Cubelerp, _Fogon;
	
			float3 _WindDirection,SpecCol,_CustomLightpos,_AmbentColor;
			
			CBUFFER_END
		
			
			struct Vertexinput
			{
				float4  vertex		: POSITION;
				float2  v2UV		: TEXCOORD0;
				float3  normal		: NORMAL;
				#ifdef Normalmap
				float4  tangent		: TANGENT;
				#endif
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
				
			
				#ifdef Normalmap
				float3	tangent		: TEXCOORD2;
				float3  bitangent	: TEXCOORD3;
				#endif
				float   fogCoord	: TEXCOORD4;
				float4 shadowCoord	: TEXCOORD5;
				float4 sPos			: TEXCOORD6;
				
			

				UNITY_VERTEX_INPUT_INSTANCE_ID
			};



			
	




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


	
				#ifdef Normalmap
								tOut.tangent = TransformObjectToWorldDir(v.tangent.xyz);
								tOut.bitangent = cross(tOut.Normal, tOut.tangent) * v.tangent.w;
				#endif


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
				
	
			

				f4TexColor.rgb = _Vcolor ? f4TexColor.rgb * tIn.vcolor.r * _SColor.rgb: f4TexColor;

				float3 normalDirection = normalize(tIn.Normal);



				#ifdef Normalmap 

				float3  Normal = UnpackNormal(tex2D(_BumpMap,  tIn.v2UV));

				float3x3 tangentTransform = float3x3(tIn.tangent, tIn.bitangent, tIn.Normal);
				normalDirection = normalize(mul(Normal, tangentTransform));
				
				#endif


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


				float4	BlendTex = tex2D(_BlendTex,  TRANSFORM_TEX(float2(tIn.worldPos.xz *0.15), _BlendTex)) *_BlendTexColor ;
				f4TexColor.rgb = _BlendTexture ? lerp(f4TexColor.rgb,BlendTex.rgb,saturate( (tIn.vcolor.b + _BlendTexpos)* _BlendTexpos1)*f4TexColor.a ) : f4TexColor.rgb;


			
		



		
			
				// 메인라이팅 
				
				Light mainLight = GetMainLight(tIn.shadowCoord);
				




				float3 lightDir = normalize(_MainLightPosition.xyz);
				float3 Mainlightcol = _MainLightColor.rgb;
				float3 ambientColor =lerp(SampleSH(normalDirection) , _AmbentColor ,_ReplaceAmbient);
				
			
				
				float NdotL = _NoAmbient ? 1 : max(0, dot(lightDir, normalDirection));
				
	
				
				
			

				
				
				
				float3 finalDiff =  NdotL* Mainlightcol * mainLight.shadowAttenuation * mainLight.distanceAttenuation + ambientColor;
				float3 ViewDir = normalize(camerapos - tIn.worldPos.xyz);





			
				#ifdef _ADDITIONAL_LIGHTS
				uint pixelLightCount = GetAdditionalLightsCount();
				for (uint lightIndex = 0u; lightIndex < pixelLightCount; ++lightIndex)
				{
					
					Light addLight = GetAdditionalLight(lightIndex, tIn.worldPos,half4(0,0,0,0));
					float distance = addLight.distanceAttenuation;
					float Shadowdist = addLight.shadowAttenuation;
					float3 addLightCol = _NoAmbient ? 1 : max(0, dot(addLight.direction, normalDirection));
					finalDiff +=  addLightCol * addLight.color.rgb * Shadowdist *  distance;
					
				};
				#endif
				finalDiff *= f4TexColor.rgb * _Color.rgb;
				//finalDiff = _Vcolor ? finalDiff * tIn.vcolor.rgb :finalDiff;

				//ViewDir
		
				
				
		

				//Vertex Color
			

				// Glow Texture
		
					float4	shiningcolor1 = _GlowTexture ? (pow(abs(tex2D(_GlowTex, TRANSFORM_TEX(tIn.v2UV.xy * 0.15 + _Speed.xy * _Time.xx, _GlowTex)).r +tex2D(_GlowTex, TRANSFORM_TEX(1 - tIn.v2UV.xy * 0.15 * 0.7 + _Speed.zw * _Time.xx, _GlowTex)).r * 0.5),_GlowPow)) :0;
					float3 GlowColor = 	_GlowTexture ? lerp( finalDiff.rgb ,_GlowColorAni.rgb,f4TexColor.a * max(0,shiningcolor1.rgb) ) : _GlowColor.rgb;
					finalDiff = (_Glow ? lerp(finalDiff , GlowColor , f4TexColor.a) : finalDiff);
				
				

		

				//Alpha
				
				float alphacutoff =  _AlphaCut ? f4TexColor.a  - _AlphaCutoff : 1 ;
				clip( alphacutoff);
				
		 				
				
				
				//float alpha = _AlphaBlend?  _Alpha * f4TexColor.a:1 ;
				


			// 	float3  f3Half      = ( _MainLightPosition.xyz + normalDirection * _SSSDistortion );
            //     float   fVoH        = pow( saturate( dot( ViewDir, -f3Half ) ), _SSSPower + 0.001 ) * _SSSScale;
            //     float3  f3SSSColor  = _SSSAttenuation* ( fVoH + _SSSFrontSpread ) * _SSSThickness * _SSSColor.rgb * mainLight.color.rgb* mainLight.shadowAttenuation;

            //   finalDiff.rgb    = float3( finalDiff.xyz + f3SSSColor );





			//   #ifdef Reflection
			//   {


			// 		#ifdef RealTimeReflection
			// 	{
			// 		float2 scrUV = (tIn.sPos / tIn.sPos .w).xy;
			// 		float3 Realtimeref = tex2D(_ReflectionTexUp,scrUV);
			// 		finalDiff.rgb = lerp(finalDiff.rgb,Realtimeref,_Cubelerp);
			// 	}
			// 	#else
			// 	{
			// 		float3 viewReflectDirection = reflect(-ViewDir.xyz, normalDirection );
			// 		float3 refcolor = texCUBE(_Cube, viewReflectDirection.xyz).rgb ;
			// 		finalDiff.rgb = lerp( finalDiff.rgb,refcolor*_CubeIntencity*_CubeColor.rgb,_Cubelerp);
			// 	}
			// 	#endif


			//   }
			//   #endif

			

				finalDiff.rgb *=  _Intencity ;

				//Fog On off
				
				finalDiff.rgb = _Fogon ?  MixFog(finalDiff.rgb, InitializeInputDataFog(float4(tIn.worldPos, 1.0), 1)): finalDiff.rgb;
			
			

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
			#pragma shader_feature AlphaCutOff
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
			#include "../Lighting.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"
			
			
					CBUFFER_START(UnityPerMaterial)

			sampler2D _MainTex, _GlowTex, _BumpMap, _BlendTex,_MoveTex,_ReflectionTexUp;
			samplerCUBE		_Cube;
			
			float4 _MainTex_ST, _MoveTex_ST, _BumpMap_ST, _GlowTex_ST, _BlendTex_ST,
					_SColor, _BlendTexColor,_GlowColor, _GlowColorAni,_Speed , _Color,_WorldposY,
					_ReplaceColor, _SpecColor, _CubeColor;
		
			float  _VertexAni, _MoveAmount, _MoveSpeed, _WindFrequency ,_AlphaCut,_AlphaBlend,_Alpha,_Intencity,_AlphaCutoff,
			_InnerAOPower,_InnerAOLength,_Leaf,_NosatAmbient,_ReplaceAmbient,_NoAmbient,_BlendTexture, 
			_Vcolor, _Glow, _GlowTexture,_GlowPow,_BlendTexpos,_BlendTexpos1,_Smoothness,_Normalmap, _Specular,_CustomLight,
			_ReplaceColorOn, _Reflection,_CubeIntencity, _Cubelerp, _Fogon;
	
			float3 _WindDirection,SpecCol,_CustomLightpos,_AmbentColor;
			
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
							float randWindPower = tex2Dlod(_MoveTex, float4(worldPos.xz * 0.1f * _WindFrequency + timedWindSpeed, 0, 0)).r * _MoveAmount;
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
				float4 positionCS = TransformWorldToHClip(ApplyShadowBias(positionWS, normalWS, float3(0,0,0)));
				tOut.vertex = positionCS;
				return tOut;
			}

			half4 frag_Main(tOUTPUT tIn) : SV_TARGET
			{
				UNITY_SETUP_INSTANCE_ID(tIn);
				float4	f4TexColor = tex2D(_MainTex, tIn.v2UV);
				
				float alphacutoff =  _AlphaCut ? f4TexColor.a - _AlphaCutoff : 1;
				clip(alphacutoff);
				
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

			#pragma vertex vert_Main
			#pragma fragment frag_Main
			#pragma multi_compile_instancing


				#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
					CBUFFER_START(UnityPerMaterial)

			sampler2D _MainTex, _GlowTex, _BumpMap, _BlendTex,_MoveTex,_ReflectionTexUp;
			samplerCUBE		_Cube;
			
			float4 _MainTex_ST, _MoveTex_ST, _BumpMap_ST, _GlowTex_ST, _BlendTex_ST,
					_SColor, _BlendTexColor,_GlowColor, _GlowColorAni,_Speed , _Color,
					_ReplaceColor, _SpecColor, _CubeColor,_WorldposY;
		
			float  _VertexAni, _MoveAmount, _MoveSpeed, _WindFrequency ,_AlphaCut,_AlphaBlend,_Alpha,_Intencity,_AlphaCutoff,
			_InnerAOPower,_InnerAOLength,_Leaf,_NosatAmbient,_ReplaceAmbient,_NoAmbient,_BlendTexture, 
			_Vcolor, _Glow, _GlowTexture,_GlowPow,_BlendTexpos,_BlendTexpos1,_Smoothness,_Normalmap, _Specular,_CustomLight,
			_ReplaceColorOn, _Reflection,_CubeIntencity, _Cubelerp, _Fogon;
	
			float3 _WindDirection,SpecCol,_CustomLightpos,_AmbentColor;
			
			CBUFFER_END
			
			struct VertexInput
			{
				float4 vertex : POSITION;
				float2  v2UV	: TEXCOORD0;
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct VertexOutput
			{
				float4 vertex : SV_POSITION;
				float2  v2UV	: TEXCOORD0;
				UNITY_VERTEX_INPUT_INSTANCE_ID
				UNITY_VERTEX_OUTPUT_STEREO
			};

			VertexOutput vert_Main(VertexInput v)
			{
				VertexOutput tOut;
				UNITY_SETUP_INSTANCE_ID(v);
				UNITY_TRANSFER_INSTANCE_ID(v, tOut);
				tOut.v2UV = v.v2UV;
				tOut.vertex = TransformWorldToHClip(TransformObjectToWorld(v.vertex.xyz));
				return tOut;
			}

			half4 frag_Main(VertexOutput tIn) : SV_TARGET
			{
				float4	f4TexColor = tex2D(_MainTex,  tIn.v2UV);
				float alphacutoff = (_AlphaCut ? f4TexColor.a - _AlphaCutoff : 1);
				clip(alphacutoff);
				return 0;
			}
	//		#include "Packages/com.unity.render-pipelines.universal/Shaders/SimpleLitInput.hlsl"
           
			ENDHLSL
		}
		  
	}
}