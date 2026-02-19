#ifndef DK2_URP_OUTLINE_LIB_INCLUDED
#define DK2_URP_OUTLINE_LIB_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/Shaders/UnlitInput.hlsl"
#include "DK2_URP_ColorLib.hlsl"
#include "../DK2_URP_CarToonLib.hlsl"

//----------------------------------------------------------------------------------
//
CBUFFER_START(UnityPerMaterial)


//half4		_OutlineC;
			 
//half		_Alpha ;
//half4		_MainTex_ST;
//sampler2D	_MainTex;

//sampler2D   _DssvNoiseTex;
//half4       _DssvNoiseTex_ST;
//half        _DssvAmount;

CBUFFER_END

//----------------------------------------------------------------------------------
//
struct tINPUT_OutLine
{
    float4 vertex   : POSITION;
    float3 normal   : NORMAL;
    float4 tangent  : TANGENT;
    float4 vcolor   : COLOR0;
    float2 texcoord : TEXCOORD0;
    float2 bakedNormal:TEXCOORD7;
    UNITY_VERTEX_INPUT_INSTANCE_ID                              
};

struct tOUTPUT_OutLine
{
    half4 v4Pos		: POSITION;
    half2 v2UV		: TEXCOORD0;
    float4 bitangent	: TEXCOORD1;
    float4 vcolor   : COLOR0;
    
  // half  vDiff    : TEXCOORD1;

    UNITY_VERTEX_INPUT_INSTANCE_ID
};
struct tOUTPUT_OutLine1
{
    half4 v4Pos		: POSITION;
    half2 v2UV		: TEXCOORD0;
    half  vDiff    : TEXCOORD1;

    UNITY_VERTEX_INPUT_INSTANCE_ID
};

//----------------------------------------------------------------------------------
//
void Calc_Dissolve_OutLine( half2 f2UV, half fDssvProgress )
{

    //half3   f3DssvNoise = tex2D(_DssvNoiseTex, TRANSFORM_TEX(f2UV, _DssvNoiseTex)).rgb;
    half4   Outlineclip = tex2D(_MainTex, TRANSFORM_TEX(f2UV, _MainTex));
   // half    fSample = f3DssvNoise.r  - fDssvProgress;
   // Outlineclip.a = -fDssvProgress;
    //clip(  fSample  - (1 -Outlineclip.a));
}


float3 OctahedronToUnitVector(float2 Oct)
{
    float3 N = float3(Oct, 1 - dot(1, abs(Oct)));
    if (N.z < 0)
    {
        N.xy = (1 - abs(N.yx)) * (N.xy >= 0 ? float2(1, 1) : float2(-1, -1));
    }
    return normalize(N);
}

float3 TransformTBN(float2 bakedNormal, float3x3 tbn)
{
    float3 normal = //float3(bakedNormal, 0);
        OctahedronToUnitVector(bakedNormal);
    //normal.z = sqrt(1.0 - saturate(dot(normal.xy, normal.xy)));
    return  (mul(normal, tbn));
}




//----------------------------------------------------------------------------------
//
tOUTPUT_OutLine vert_OutLine( tINPUT_OutLine tIn )
{
	tOUTPUT_OutLine	tOut;

    UNITY_SETUP_INSTANCE_ID( tIn );
    UNITY_TRANSFER_INSTANCE_ID( tIn, tOut );

 //   half fFOV		= atan( 1.0f / unity_CameraProjection._m11 ) * 114.6f;// * 2.0 * ( 180 / UNITY_PI );
	//half fDist		= distance( _WorldSpaceCameraPos.xyz,  mul(unity_ObjectToWorld, tIn.vertex).xyz );
	//half fOffset	= min( 0.012, _OutlineSize * fDist * fFOV * 0.01f );

    float3 normalOS = normalize(tIn.normal);
	float3 tangentOS = tIn.tangent;
    tangentOS = normalize(tangentOS);
	float3 bitangentOS = normalize(cross(normalOS, tangentOS) * tIn.tangent.w);
	float3x3 tbn = float3x3(tangentOS, bitangentOS, normalOS);

    float3 BakedNormalDir = (TransformTBN(tIn.bakedNormal, tbn));
    float4 pos = TransformObjectToHClip(tIn.vertex);
	float Set_OutlineWidth = pos.w * _OutlineSize;
	Set_OutlineWidth = min(Set_OutlineWidth, _OutlineSize);
	//Set_OutlineWidth = min(Set_OutlineWidth, _OutlineSize);
	
	//Use original normals or smoothed normals
	//float3 Set_NormalDir = lerp(tin.normal, _BakedNormalDir, _Is_BakedNormal);
    tOut.v4Pos		= TransformObjectToHClip(tIn.vertex + BakedNormalDir * Set_OutlineWidth* tIn.vcolor.r);		
	//tIn.vertex.xyz	+= _OutlineSize* tIn.normal.xyz * tIn.vcolor.r ;
	//tOut.v4Pos		= TransformObjectToHClip(tIn.vertex.xyz);
                

                
	tOut.v2UV		= TRANSFORM_TEX( tIn.texcoord, _MainTex ); 
				
	return tOut;
}

//----------------------------------------------------------------------------------
//
half4 frag_OutLine( tOUTPUT_OutLine tIn ) : SV_Target
{
    UNITY_SETUP_INSTANCE_ID( tIn );

    //Shading
    Light   lightMain       = GetMainLight();
	half4 f4TexColor = tex2D( _MainTex, tIn.v2UV );
    float alpha = 1;
    
    #ifdef AlphaBlend
    alpha = f4TexColor.a *_Alpha  ;
    #endif


	half4 f4Result = half4( pow( abs( f4TexColor.rgb * _OutlineC.rgb ), _OutlinePower ), alpha -_DssvAmount );
    f4Result.rgb *= lightMain.color.rgb;

  
  #ifdef AlphaCutOff
    clip(f4TexColor.a -  _AlphaCutoff  );
  #endif
    //Apply Color Ramp
 //   f4Result.rgb = ApplyEnvColorRamp( ( _global_UseRamp && _UseEnvRamp ), f4Result.rgb, _global_Ramp_Map, _global_EnvPct_Char );

	return half4(f4Result);
}

//----------------------------------------------------------------------------------
//
tOUTPUT_OutLine1 vert_OutLine_SeeThrough( tINPUT_OutLine tIn )
{
	tOUTPUT_OutLine1	tOut;

    UNITY_SETUP_INSTANCE_ID( tIn );
    UNITY_TRANSFER_INSTANCE_ID( tIn, tOut );

    half3 f3PosW = TransformObjectToWorld( tIn.vertex.xyz );
	half3 f3NorW = TransformObjectToWorldNormal( tIn.normal );
	half3 f3EyeW = _WorldSpaceCameraPos.xyz - f3PosW;
    half  fAlpha = 1 - saturate(dot(normalize(f3NorW), normalize(f3EyeW)));

    half fFOV		= atan( 1.0f / unity_CameraProjection._m11 ) * 114.6f;// * 2.0 * ( 180 / UNITY_PI );
	half fDist		= distance( _WorldSpaceCameraPos.xyz,  mul(unity_ObjectToWorld, tIn.vertex).xyz );
	half fOffset	= min( 0.012, _OutlineSize * fDist * fFOV * 0.01f );
				
	tIn.vertex.xyz	+= fOffset * tIn.normal.xyz;
	tOut.v4Pos		= TransformObjectToHClip(tIn.vertex.xyz);
                
    #if UNITY_REVERSED_Z
    tOut.v4Pos.z -= _OutLineDepth * 0.1;
    #else
    tOut.v4Pos.z += _OutLineDepth * 0.1 * (1.0 - UNITY_NEAR_CLIP_VALUE);
    #endif
                
	tOut.v2UV	= TRANSFORM_TEX( tIn.texcoord, _MainTex ); 
    tOut.vDiff  = fAlpha;

	return tOut;
}

//----------------------------------------------------------------------------------
//
half4 frag_OutLine_SeeThrough( tOUTPUT_OutLine1 tIn ) : SV_Target
{
    UNITY_SETUP_INSTANCE_ID( tIn );

    //Shading
    Light   lightMain       = GetMainLight();
    half  fRimAlpha = smoothstep( -1, 1, tIn.vDiff );
	half4 f4TexColor = tex2D( _MainTex, tIn.v2UV ) ;
	
	half4 f4Result = half4(  f4TexColor.rgb * _OutlineC.rgb , 0);
    f4Result.rgb *= lightMain.color.rgb;
     
    //Dissove
    Calc_Dissolve_OutLine( tIn.v2UV, _DssvAmount );

    //Apply Color Ramp
    f4Result.rgb = ApplyEnvColorRamp( ( _global_UseRamp && _UseEnvRamp ), f4Result.rgb, _global_Ramp_Map, _global_EnvPct_Char ) ;

	return f4Result;
}

#endif // DK2_URP_OUTLINE_LIB_INCLUDED
