Shader "DK2_URP_Shader/Map/BG_Water"
{
	Properties
	{
		[HDR] _BaseColor("Base Color", Color) = (0, 0.3, 0.3, 1)
		[HDR] _ShadowColor("Light Color", Color) = (0, 0.3, 0.3, 1)
		[HDR] _OutsideColor("OutSide Color", Color) = (0, 0.3, 0.3, 1)
	//	[HDR] _ShadowColor2("Sea Color2", Color) = (0, 1, 0.8, 1)
		_Lightpos("light pos", Vector) = (0,0,0,0)
	
		
		[Space(30)]
		_RampThr("Ramp Thr", Range(-1,1)) = 1.0
		_RampSmooth("Ramp Smooth", Range(0,1)) = 1.0
		
		_ColorRangeThr("Color Range Thr", Range(0,5)) = 1.0
		_ColorRangeSmooth("Color Range Smoonth", Range(-5,5)) = 1.0
	
			
		/*[Space(30)]
		[HDR] _RimColor("Rim Color", Color) = (1, 1, 1, 1)
		_RimThr("Rim Thr", Range(0,0.3)) = 1.0
		_RimSmooth("Rim Smooth", Range(0,1)) = 1.0*/

			
		[Space(30)]
		[HDR] _SColor("Spec Color", Color) = (1, 1, 1, 1)
		_specular("specular Pow", Range(0,1)) =0.5
		_SpecMin("Spec Min", Range(0,1)) = 0.5
		_SpecMax("Spec Max", Range(0.5,1)) = 1.0

		_MainTex("Bumpmap (bump)", 2D) = "bump" {}
		_BumpSpeed("Bump Speed", Vector) = (2,2,2,2)
		[Space(30)]
		[HDR]_FoamColor("Foam Color 1", Color) = (1, 1, 1, 1)
		
		[HDR]_FoamLineColor("Foam Line Color ", Color) = (1, 1, 1, 1)
		_FoamTex("Foam tex (bump)", 2D) = "gray" {}
		_FoamSpeed("Foam Speed", Vector) = (2,2,2,2)
		_FoamDst("Foam Dst",Range(0,10)) = 1.0
		_ChromaticRange("Chromatic range", Range(0,1)) = 1.0
		
		_FoamStrength("foam smooth", Range(0,2)) = 1.0
		_FoamSpread("foam Ragne", Range(1,-1)) = 1.0
		_FoamInLinethr("foamInLine thr ", range(0.001,0.1)) = 0.01
		_FoamInLinesmth("foamInLine smth", range(0.001,0.1)) = 0.01
		_FoamOutLinethr("foamOutLine thr ", range(0.001,0.1)) = 0.01
		_FoamOutLinesmth("foamOutLine smth", range(0.001,0.1)) = 0.01
	
		[Space(30)]
		_DepthMinAlpha("Depth Alpha", Range(0,1)) = 1
		_DepthAlphamin("DepthAlpha Smooth", Range(-5,5)) = 0.5
		_DepthAlphamax("DepthAlnpa Threshold", Range(0.0,10)) = 0.7
		[Space(30)]
		_Cube("Cube", Cube) = ""{}
	//	[HDR]_cubeColor("Cube Color", Color) = (0, 0.3, 0.3, 1)
		_Cubelerp("Cubelerp", Range(0,1)) = 1.0
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
		//	#include "../DK2_URP_ColorLib.hlsl"

			CBUFFER_START(UnityPerMaterial)
			sampler2D _MainTex ,_FoamTex;
			
			float4 _MainTex_ST,  _FoamTex_ST;

		
			
		
			sampler2D _CameraOpaqueTexture;
			
			samplerCUBE		_Cube;
			float4			_BumpSpeed;
			float4			_FoamSpeed;
			float4			_Lightpos;
			float			_DepthMinAlpha;
			float			_DepthAlphamin;
			float			_DepthAlphamax;
			float			_RampThr;
			float			_RampSmooth;
			float			_ColorRangeThr;
			float			_ColorRangeSmooth;

			float			_FoamStrength;
			float			_FoamSpread;
			float			_FoamDst;
			float			_FoamLine1;
			float			_FoamLine2;
			float			_FoamInLinethr , _FoamInLinesmth ,_FoamOutLinethr ,_FoamOutLinesmth;
		
			float			_Cubelerp;

		/*	float			_RimSmooth;
			float			_RimThr;*/
			/*float			_SpecMin;
			float			_SpecMax;*/

			float3			_ShadowColor;
			float3			_OutsideColor;
			float3			_BaseColor;
		//	float3			_ShadowColor2;
			float3			_SColor;
			/*float3			_RimColor;*/
			float4			_FoamColor;
			float4			_FoamLineColor;
			float			_ChromaticRange;

			float			_specular;
		//	float3			_cubeColor;
			CBUFFER_END

			struct VertexInput
			{
				float4  vertex		: POSITION;
				float3	normal		: NORMAL;
				float4	tangent		: TANGENT;
			};

			struct tOUTPUT
			{
				float4  vertex		: SV_POSITION;
				float3  posWorld	: TEXCOORD0;
				float4	sPos		: TEXCOORD1;
				float3	normal		: NORMAL;
				float3	tangent		: TEXCOORD2;
				float3  bitangent	: TEXCOORD3;
				float  fogCoord		: TEXCOORD4;
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
				return tOut;
			}

			float4 frag_Main(tOUTPUT tIn) : SV_Target
			{
				float4 Lightpos = _Lightpos * 1000;
				float3 viewDirection = normalize(_WorldSpaceCameraPos.xyz - tIn.posWorld.xyz);
				float3 lightDirection = normalize(Lightpos.xyz - tIn.posWorld.xyz);
			//	float3 halfDirection = normalize(viewDirection.xyz + lightDirection.xyz);
			//	float specPow = _specular;

				//------------ Depth Alpha---------------
				float4 ScreenPos = tIn.sPos;
				float4 screentposNorm = ScreenPos / ScreenPos.w;
				float eyeDepth43 = LinearEyeDepth(SampleSceneDepth(screentposNorm.xy), _ZBufferParams);
				float linedepth = LinearEyeDepth(screentposNorm.z, _ZBufferParams);

					#if SHADER_API_D3D11 || SHADER_API_VULKAN || SHADER_API_METAL || SHADER_API_GLCORE
					linedepth = linedepth;
					#else 
					linedepth = linedepth *2;
					#endif

				
				
				float  depthDiff = eyeDepth43 - linedepth;




				tIn.normal = normalize(tIn.normal);
				float3 Normal1 = UnpackNormal(tex2D(_MainTex,tIn.posWorld.xz * _MainTex_ST.xy * 0.1 + _Time.xx * _BumpSpeed.xy * 0.1));
				float3 Normal2 = UnpackNormal(tex2D(_MainTex,  (1 - tIn.posWorld.xz)*0.7 * _MainTex_ST.xy * 0.1 + _Time.xx * _BumpSpeed.zw * 0.1));
				float3 Normal3 = normalize(Normal1 + Normal2);
				float Dist = normalize(Normal3).r;
				float3x3 tangentTransform = float3x3(tIn.tangent, tIn.bitangent, tIn.normal);

				float3 normalDirection = normalize(mul(Normal3, tangentTransform));



				float3 viewReflectDirection = reflect(-viewDirection.xyz,  tIn.normal  +  Dist *_FoamDst * 0.1);
				float3 refcolor = texCUBE(_Cube, viewReflectDirection.xyz).rgb * _ShadowColor.rgb;

				
				///////// Gloss:
				float gloss = _specular;
				float specPow = exp2(gloss * 10.0 + 1.0);





				float3 directSpecular = pow(max(0, dot(reflect(-lightDirection, normalDirection), viewDirection)), specPow);
				

				//float foamtex = tex2D( _FoamTex,  (tIn.posWorld.xz-0.5 + Dist * _FoamDst) * _FoamTex_ST.xy*0.1 +_Time.xx * _FoamSpeed.xy *0.1).r;
				//float foamtex1 = tex2D(_FoamTex,  (1 - tIn.posWorld.xz-0.5 + Dist * _FoamDst) * _FoamTex_ST.xy *0.1 + _Time.xx * _FoamSpeed.zw *0.1).r;
				//float foamtex3 = tex2D(_FoamTex,  (1 - tIn.posWorld.xz-0.5 +Dist * _FoamDst *0.2) * _FoamTex_ST.xy *0.1 + _Time.xx * _FoamSpeed.xy *0.05).g;
				//float foamtex6 = tex2D(_FoamTex,  ( tIn.posWorld.xz +Dist * _FoamDst*0.2) * _FoamTex_ST.xy *0.1 + _Time.xx * _FoamSpeed.zw *0.05).g;
				//float foamtex4 = tex2D(_FoamTex,  (1 - tIn.posWorld.xz + Dist * _FoamDst) * _FoamTex_ST.xy *0.1 + _Time.xx * _FoamSpeed.zw *0.1).b;
				
				//float foamtex2 = (foamtex + foamtex1);
				//float foamtex5 = (foamtex3 + foamtex6) * foamtex4 ;
				

				float ChomaticUV = _ChromaticRange*0.01;
				float foamtexRed = tex2D( _FoamTex, ((tIn.posWorld.xz-0.5 + Dist * _FoamDst) * _FoamTex_ST.xy*0.1 +_Time.xx * _FoamSpeed.xy *0.1)+ float2(ChomaticUV,0)).r;
				float foamtexGreen= tex2D( _FoamTex, ((tIn.posWorld.xz-0.5 + Dist * _FoamDst) * _FoamTex_ST.xy*0.1 +_Time.xx * _FoamSpeed.xy *0.1)+ float2(ChomaticUV,ChomaticUV)).r;
				float foamtexBlue = tex2D( _FoamTex, ((tIn.posWorld.xz-0.5 + Dist * _FoamDst) * _FoamTex_ST.xy*0.1 +_Time.xx * _FoamSpeed.xy *0.1)+ float2(0,ChomaticUV)).r;
				float foamtexRed1 = tex2D( _FoamTex,  ((1-tIn.posWorld.xz-0.5 + Dist * _FoamDst) * _FoamTex_ST.xy*0.1 +_Time.xx * _FoamSpeed.zw *0.1)+ float2(ChomaticUV,0)).r;
				float foamtexGreen1= tex2D( _FoamTex,  ((1-tIn.posWorld.xz-0.5 + Dist * _FoamDst) * _FoamTex_ST.xy*0.1 +_Time.xx * _FoamSpeed.zw *0.1)+ float2(ChomaticUV,ChomaticUV)).r;
				float foamtexBlue1 = tex2D( _FoamTex,  ((1-tIn.posWorld.xz-0.5 + Dist * _FoamDst) * _FoamTex_ST.xy*0.1 +_Time.xx * _FoamSpeed.zw *0.1)+ float2(0,ChomaticUV)).r;
				
				
				
				float3 foamtex1 = float3(foamtexRed,foamtexGreen,foamtexBlue);
				float3 foamtex2 = float3(foamtexRed1,foamtexGreen1,foamtexBlue1);
				
				float3 foamtex5 = (foamtex1 +  foamtex2);
				float ramp = dot(viewDirection, normalDirection);
				
				float ramprange = smoothstep(_RampThr - _RampSmooth , _RampSmooth+_RampThr, ramp) ;



				float colorRange = smoothstep(_ColorRangeThr-_ColorRangeSmooth ,_ColorRangeSmooth +_ColorRangeThr, depthDiff) ;
				float Depthalpha = smoothstep(_DepthAlphamin ,_DepthAlphamax, depthDiff) ;
		
				//------------ Depth Alpha---------------

				float depthdiff1 = 1 - depthDiff;
				float foamDepth = smoothstep(_FoamSpread -_FoamStrength,_FoamSpread+_FoamStrength , depthdiff1);
				float foamlineOut = smoothstep(_FoamInLinethr - _FoamInLinesmth , _FoamInLinethr +  _FoamInLinesmth , depthDiff);
				float foamlineIn = (1 -smoothstep(_FoamOutLinethr - _FoamOutLinesmth, _FoamOutLinethr + _FoamOutLinesmth * Dist *10, depthDiff));
				float foamline =  saturate( foamlineOut *  foamlineIn  * Dist) * 50  ;


			

				//Ambient
				float3 ambient = SampleSH(normalDirection);

			
				




		
				float3 specular = directSpecular * _SColor;


			
				
				
				
				//float foam = step(foamDiff - (saturate(sin((foamDiff - _Time.y * _FoamLinesSpeed) * 8 * UNITY_PI)) * (1.0 - foamDiff)), foamTex);
			//	float finalfoam =  step(0.1, foamline + foamtex2) *foamDepth;
				//float finalfoam =  step(foamDepth - (saturate(sin((foamDepth - _Time.y *0.05) * 10 * 3.14)) * foamtex2  *1), depthdiff1)* foamDepth;
				float foamtex6 = (foamtex5.r+foamtex5.g+foamtex5.b)*0.333;
				
				
				float3 maincolor = lerp(_ShadowColor,_BaseColor,ramprange);
				maincolor = lerp(maincolor , _OutsideColor, colorRange);
				maincolor = lerp( _FoamColor.rgb,maincolor,  saturate(Depthalpha +0.5));
				//maincolor = lerp(maincolor, _FoamColor3.rgb,  foamline * _FoamColor3.a);
				
				
				float3 diffuse = lerp( maincolor , refcolor.rgb, _Cubelerp ) + specular;
				diffuse = lerp(diffuse ,   foamtex5.rgb * _FoamColor.rgb ,  saturate(depthdiff1) ) + foamline *_FoamLineColor;
				//foamDepth * foamlineOut * _FoamColor2.rgb;
				

				float alpha = saturate( Depthalpha   * _DepthMinAlpha  )* foamlineOut  ;
			//	float4 final = lerp(float4(diffuse,alpha), float4(_FoamColor.rgb , 1), finalfoam);
				float4 final = float4(diffuse,alpha);
			

				//-----Grabpass
			//	float4 DistUV = ScreenPos + Dist * depthdiff1 ;
			//	float4 grabPass = tex2Dproj(_CameraOpaqueTexture, DistUV);
				


			//	final = lerp(grabPass , final , ( alpha  + finalfoam)  );







					//Apply Color Ramp
				//final.rgb = ApplyEnvColorRamp( _global_UseRamp, final.rgb, _global_Ramp_Map, _global_EnvPct_Map );


			//	float4 finalcolor = float4(foamtex1,foamtex1,foamtex1,1);
				final.rgb = MixFog(final.rgb, InitializeInputDataFog(float4(tIn.posWorld, 1.0), 1));
			

				return final;
			}

			ENDHLSL
		}
	}
}