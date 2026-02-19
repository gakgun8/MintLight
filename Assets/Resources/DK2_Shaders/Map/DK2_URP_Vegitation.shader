Shader "DK2_URP_Shader/Map/VegitationShader_Modify"
{
	Properties
	{
			_LeafTex("LeafTex", 2D) = "white" {}

			_LeafBaseColour("LeafBaseColour", Color) = (0.07843138,0.02015968,0,0)
			_LeafNoiseColour("LeafNoiseColour", Color) = (0.07843138,0.02015968,0,0)
			_LeafNoiseLargeColour("LeafNoiseLargeColour", Color) = (0.07843138,0.02015968,0,0)

			_ColourNoiseSmallScale("ColourNoiseSmallScale", Range( 0 , 1)) = 0
				
			_NoiseTex("Noise", 2D) = "white" {}
		
			_FrostingColour("FrostingColour", Color) = (1,1,1,0)
			
			_FrostingHeight("FrostingHeight", Float) = 1
			_FrostingFalloff("FrostingFalloff", Float) = 1
	}

	SubShader
	{
		LOD 0

		Tags { "RenderPipeline" = "UniversalPipeline" "RenderType" = "Opaque" "Queue" = "Geometry" }
		Cull Off
		AlphaToMask Off

		Pass
		{

			Name "Forward"
			Tags { "LightMode" = "UniversalForward" }

			Blend One Zero, One Zero
			ZWrite On
			ZTest LEqual
			
			HLSLPROGRAM

			#pragma multi_compile _ DIRLIGHTMAP_COMBINED
			#pragma target 3.0
			#pragma prefer_hlslcc gles
			#pragma exclude_renderers d3d11_9x 

			#pragma vertex vert
			#pragma fragment frag

			#define SHADERPASS_FORWARD

			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
			
			struct VertexInput
			{
				float4 vertex : POSITION;
				float3 ase_normal : NORMAL;
				float4 ase_tangent : TANGENT;
				float4 texcoord1 : TEXCOORD1;
				float4 texcoord : TEXCOORD0;
				float4 ase_color : COLOR;
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct VertexOutput
			{
				float4 clipPos : SV_POSITION;
				
				float4 tSpace0 : TEXCOORD3;
				float4 tSpace1 : TEXCOORD4;
				float4 tSpace2 : TEXCOORD5;
				
				float4 ase_color : COLOR;
				float4 ase_texcoord7 : TEXCOORD7;
				UNITY_VERTEX_INPUT_INSTANCE_ID
				UNITY_VERTEX_OUTPUT_STEREO
			};

			CBUFFER_START(UnityPerMaterial)
				float4 _FrostingColour;
				float4 _LeafNoiseLargeColour;
				float4 _LeafBaseColour;
				float4 _LeafTex_ST;
				float4 _LeafNoiseColour;
				float _FrostingFalloff;
				float _FrostingHeight;
				float _ColourNoiseSmallScale;
			CBUFFER_END

				sampler2D _LeafTex;
				sampler2D _NoiseTex;
			
			VertexOutput vert(VertexInput v)
			{
				VertexOutput o = (VertexOutput)0;
				UNITY_SETUP_INSTANCE_ID(v);
				UNITY_TRANSFER_INSTANCE_ID(v, o);
				
				o.ase_color = v.ase_color;
				o.ase_texcoord7.xy = v.texcoord.xy;
				o.ase_texcoord7.zw = 0;

				

				//여기가 중요합니다.
				float3 positionWS = TransformObjectToWorld(v.vertex.xyz);
				float4 positionCS = TransformWorldToHClip(positionWS);

				VertexNormalInputs normalInput = GetVertexNormalInputs(v.ase_normal);

				o.tSpace0 = float4(normalInput.normalWS, positionWS.x);
				o.tSpace1 = float4(normalInput.tangentWS, positionWS.y);
				o.tSpace2 = float4(normalInput.bitangentWS, positionWS.z);
				//여기 까지.

				o.clipPos = positionCS;

				return o;
			}
			

			half4 frag(VertexOutput IN
						#ifdef ASE_DEPTH_WRITE_ON
						,out float outputDepth : ASE_SV_DEPTH
						#endif
						 ) : SV_Target
			{
				UNITY_SETUP_INSTANCE_ID(IN);
				
			//여기는 모두 중요.
				half3	WorldNormal		= normalize(IN.tSpace0.xyz);
				half3	WorldPosition	= half3(IN.tSpace0.w,IN.tSpace1.w,IN.tSpace2.w);
				half2	uv_LeafTex		= IN.ase_texcoord7.xy * _LeafTex_ST.xy + _LeafTex_ST.zw;
				half	NoiseValue		= tex2D(_NoiseTex, IN.ase_texcoord7.xy * _ColourNoiseSmallScale).r * 0.5 + 0.5;
				
				half4	lerpNoiseToBaseColor		= lerp(_LeafNoiseColour , _LeafBaseColour , NoiseValue);
				half3	sinFromWPos					= sin(WorldPosition * 0.5);
				half	lmnSin						= dot( sinFromWPos, half3( 0.2126729f, 0.7151522f, 0.0721750f ) ); //Luminence
				half4	lerpNoiseBaseToLargeColor	= lerp( lerpNoiseToBaseColor, _LeafNoiseLargeColour, lmnSin);
				
				half	resultPercent				= saturate( pow(WorldNormal.y, _FrostingHeight) * _FrostingFalloff );
				half3	resultColor					= lerp(lerpNoiseBaseToLargeColor, _FrostingColour, resultPercent ).rgb;
				half	alpha						= tex2D(_LeafTex, uv_LeafTex).a;

				clip( alpha - 0.5 );
				
				return half4( resultColor.rgb, alpha );
			}

			ENDHLSL
		}
	}

	CustomEditor "UnityEditor.ShaderGraph.PBRMasterGUI"
	Fallback "Hidden/InternalErrorShader"
}
