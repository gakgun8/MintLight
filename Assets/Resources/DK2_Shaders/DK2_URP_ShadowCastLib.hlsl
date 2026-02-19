#ifndef DK2_URP_SHADOWCAST_LIB_INCLUDED
#define DK2_URP_SHADOWCAST_LIB_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
#include "Packages/com.unity.render-pipelines.universal/Shaders/LitInput.hlsl"
#include "../DK2_URP_CarToonLib.hlsl"
//----------------------------------------------------------------------------------
//
CBUFFER_START(UnityPerMaterial)

CBUFFER_END

//----------------------------------------------------------------------------------
//
struct tINPUT_Shadow
{          
    float4 vertex : POSITION;
    float2 texcoord : TEXCOORD0;
    float4 normal : NORMAL;

    UNITY_VERTEX_INPUT_INSTANCE_ID  
};
          
struct tOUTPUT_Shadow
{          
    float4 vertex : SV_POSITION;
    float2 texcoord : TEXCOORD0;

    UNITY_VERTEX_INPUT_INSTANCE_ID          
    UNITY_VERTEX_OUTPUT_STEREO
};

//----------------------------------------------------------------------------------
//
tOUTPUT_Shadow ShadowPassVertex(tINPUT_Shadow v)
{
    tOUTPUT_Shadow o;
    UNITY_SETUP_INSTANCE_ID(v);
    UNITY_TRANSFER_INSTANCE_ID(v, o);
            
    float3 positionWS = TransformObjectToWorld(v.vertex.xyz);
    float3 normalWS   = TransformObjectToWorldNormal(v.normal.xyz);
         
    float4 positionCS = TransformWorldToHClip(ApplyShadowBias(positionWS, normalWS, _MainLightPosition.xyz));
              
    o.vertex = positionCS;
    o.texcoord = v.texcoord;
    return o;
}

//----------------------------------------------------------------------------------
//
half4 ShadowPassFragment(tOUTPUT_Shadow i) : SV_TARGET
{  
    UNITY_SETUP_INSTANCE_ID(i);

	half3   f3DssvNoise = tex2D(_DssvNoiseTex, TRANSFORM_TEX(i.texcoord, _DssvNoiseTex)).rgb;
	half    fSample = f3DssvNoise.r - _DssvAmount;

	clip(fSample);

    return 0;
}

#endif // DK2_URP_SHADOWCAST_LIB_INCLUDED
