Shader "DK2_URP_Shader/Map/Seafoam"
{
	Properties
	{
		[MaterialToggle] _UseLightMap("UseLightMap", float)		= 1
		
		[HDR]_Color("Main Color", Color) = (1,1,1,1)
		//_SColor("Water SColor", Color) = (1,1,1,1)
		_MainTex( "Base (RGB)", 2D )							= "Black" {}
		_TexMoveRange("Tex Move Range", range(0,1))				= 1
		_FoamSpeed("Dist Speed " , Vector) = (0,0,0,0)
		
		_Distoration ( "Distoration", range (0,10)) 			= 1
		_DistScale( "Dist Scale ", range (0,10) )  				= 1 
		_DistSpeed("Dist Speed " , Vector) 						= (0,0,0,0)

		
	}

	SubShader
	{
		Tags { "Queue" = "Transparent" "RenderType" = "Transparent"   "RenderPipeline" = "UniversalPipeline" }
		
		Blend SrcAlpha OneMinusSrcAlpha
		Cull back

	
		Pass
		{
			Tags{
			"LightMode" = "UniversalForward"
			}
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
			float4			_Color,_DistSpeed , _FoamSpeed;
			float _DistScale , _Distoration , _TexMoveRange;

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
				float3  posWorld	: TEXCOORD1;
		
			
			};

			tOUTPUT vert_Main(Vertexinput v )// 주의!!! 매크로 함수를 사용하므로, 파라메터 이름을 v로 지정해야함.
			{
				tOUTPUT	tOut;
			
				float4 pos		= TransformObjectToHClip(v.vertex.xyz);
				tOut.pos = pos;
				tOut.UV  = v.texcoord;
				tOut.vcolor = v.vcolor;
				tOut.posWorld = TransformObjectToWorld(v.vertex.xyz);

			
				return tOut;
			}

			float4 frag_Main( tOUTPUT tIn ) : COLOR
			{
				//UNITY_SETUP_INSTANCE_ID(tIn);


				tIn.posWorld *= 0.01; 
				float4 f4TexColor = tex2D(_MainTex, tIn.UV);
				float Dist1 = tex2D(_MainTex, tIn.posWorld.xz * _DistScale +_DistSpeed.xy *_Time.x  ).b ;
				float Dist2 = tex2D(_MainTex,  (1- tIn.posWorld.xz) * _DistScale +_DistSpeed.zw *_Time.x ).b;
				float Dist = saturate (Dist1 + Dist2) *0.01 * _Distoration;



				float Seafoam1 = tex2D(_MainTex, TRANSFORM_TEX (tIn.UV  * _MainTex_ST + Dist +  (float2(_FoamSpeed.x *_Time.x,cos(_FoamSpeed.y* _Time.x)+1)*_TexMoveRange),_MainTex)  ).g;
				float Seafoam2 = tex2D(_MainTex,  TRANSFORM_TEX((	float2(1-tIn.UV.x,tIn.UV.y)-0.5) * _MainTex_ST +  Dist +(float2(_FoamSpeed.z*_Time.x,sin(_FoamSpeed.w* _Time.x)+1)*_TexMoveRange),_MainTex) ).g;
				//float Seafoam2 = tex2D(_MainTex,  (	float2(1-tIn.UV.x,tIn.UV.y)-0.5) * _MainTex_ST +_FoamSpeed.zw*_Time.x ).g;
				Seafoam1 = saturate(Seafoam1 + Seafoam2);
				
				float3 final = saturate(_Color.rgb * _MainLightColor.rgb);
				

				float Alpha = f4TexColor.r * Seafoam1 *_Color.a *tIn.vcolor.a;
				
				return float4(final,Alpha);
			}

			ENDHLSL
		}
		

	}
	 
	
}