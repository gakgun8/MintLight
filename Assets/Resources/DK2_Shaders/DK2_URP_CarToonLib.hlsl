#ifndef DK2_URP_CARTOON_LIB_INCLUDED
#define DK2_URP_CARTOON_LIB_INCLUDED

#include "../Lighting.hlsl"
#include "../DK2_URP_ColorLib.hlsl"


//----------------------------------------------------------------------------------
//
CBUFFER_START(UnityPerMaterial)
sampler2D _CameraOpaqueTexture;
float4		_DamageC, _LightDirection;
float4		_MainColor;
float       _CustomeLight;
             
float4		_RimLC,_RimSC;
float		_RimMin, _RimMax, _UseRim, _RimTC_ITS;

float		_RampSmooth, _RampThreshold;
float4		_RampC;
float4		_RampTex_ST,_MaskTex_ST;
sampler2D	_RampTex,_MaskTex;

sampler2D   _SpMaskTex;
float        _SpecularSmooth, _SpecularRange, _SpecularITS;

float4      _GlowC;
float4      _OutlineC;
float4      _GlowAni;
sampler2D   _GlowMaskTex;

sampler2D   _DssvNoiseTex, _DssvRampTex;
float4       _DssvNoiseTex_ST;
float        _DssvAmount;
float4      _DssvColor,_DissolveDir;
float      _DissolveRange, _Dssvoffset;
float      _DissolveBand;
//float      _DissolveScale;




float3 _AnisoDir,_Normal;
float _Gloss,_att,_AnisoOffset,_AnisoMask;

float		_Alpha,_AlphaCut,_AlphaBlend,_AlphaCutoff,_CubeIntencity,_Cubelerp,_CubemapOn;
float4		_MainTex_ST;
sampler2D	_MainTex, _NormalTex;
samplerCUBE		_Cube;

half		_OutlineSize, _OutLineDepth, _OutlinePower;



CBUFFER_END

//----------------------------------------------------------------------------------
//
struct tINPUT_Base
{
    float4 vertex   : POSITION;
    float3 normal   : NORMAL;
    float2 texcoord : TEXCOORD0;
    float2 texcoord2 : TEXCOORD1;

    UNITY_VERTEX_INPUT_INSTANCE_ID                              
};

struct tINPUT_Full
{
    float4 vertex   : POSITION;
    float4 vcolor   : COLOR0;
    float4 tangent  : TANGENT;
    float3 normal   : NORMAL;
    float2 texcoord : TEXCOORD0;
    float2 texcoord2 : TEXCOORD1;
    float3 height	: TEXCOORD2;         
        
    UNITY_VERTEX_INPUT_INSTANCE_ID                              
};

struct tOUTPUT_Base
{
    float4 f4Pos		    : POSITION;
    float4 vcolor           : COLOR0;
    float2 f2UV		        : TEXCOORD0;
    float2 f2UV2		    : TEXCOORD1;
    float3 f3NormalW	    : TEXCOORD2;
    float3 f3EyeW           : TEXCOORD3;
    float3 worldPos	        : TEXCOORD4;
    float4 shadowCoord	    : TEXCOORD5;         
    float3  height	        : TEXCOORD6;         
    float4  sPos            : TEXCOORD7;	
    float3  f3tangentW      : TEXCOORD8;	
    float3  f3bittangentW   : TEXCOORD9;	
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct tOUTPUT_Base_Monster
{
    float4 f4Pos		: POSITION;
    float2 f2UV		    : TEXCOORD0;
    float2 f2UV2		: TEXCOORD1;
    float3 f3NormalW	: TEXCOORD2;
    float3 worldPos	    : TEXCOORD3;
    float4 shadowCoord	: TEXCOORD4;         
    float3 height	    : TEXCOORD5;
    float3 f3EyeW       : TEXCOORD6;
     
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct tOUTPUT_Full
{
    float4 f4Pos		: POSITION;
    float2 f2UV		    : TEXCOORD0;
    float3 f3Light	    : TEXCOORD1;
    float3 f3Eye	    : TEXCOORD2;
    float3  height      : TEXCOORD3;
    float4  sPos        : TEXCOORD4;			
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

//----------------------------------------------------------------------------------
//버텍스 셰이더에 필요한 함수들
float3 Calc_ObjectToUVSpace( float3 f3Pos, float3 f3Tangent, float3 f3BiNormal, float3 f3Normal )
{
    float3   f3UVSpace   = float3( dot( f3Pos, f3Tangent ),
                                 dot( f3Pos, f3BiNormal ),
                                 dot( f3Pos, f3Normal ) );

    return f3UVSpace;
}

//----------------------------------------------------------------------------------
//픽셀 셰이더에 필요한 함수들
float3 Calc_RampColor( float3 f3RampTexColor, float fNDotL, float LightDisAtt, float3 f3LightColor)
{
    //Range 값은 0~1
   // float    fMaskValue  = lerp( 1, fNDotL, fMask );
    float3	f3Ramp		= smoothstep( _RampThreshold - _RampSmooth , _RampThreshold + _RampSmooth , fNDotL*LightDisAtt );
    float3  f3Result    = saturate(f3Ramp + f3RampTexColor );

    return f3Result;
}


float3 Calc_RimColor(  float fMask, float fNDotRL )
{
    float	fRim		= smoothstep(_RimMin -  _RimMax,_RimMin + _RimMax, fNDotRL );
	float3	fRimColor	=  1 - fRim ;

    return fRimColor ;
}


float3 anistoSpecColor (float3 lightDir, float3 viewdir,float3 Normal,float3 tangent, float3 bittangent, float AnisoOffset, float Gloss ,float atten ,float3 MainTex, float4 fMask,float fRange , float3 fNDotL)
{
    
    lightDir = -lightDir;
    float NdotL = saturate(dot(Normal, lightDir));
        

    // ===== Anisotropic Specular =====
    float3 H = normalize(lightDir + viewdir);

    // 핵심 수식
    float TdotH = dot(tangent, H);

    float aniso = pow(saturate(1.0 - abs(TdotH)), Gloss);

    aniso = smoothstep(fRange - _SpecularSmooth, fRange + _SpecularSmooth, aniso );
     
  // float ramp = smoothstep( fRange - _SpecularSmooth, fRange + _SpecularSmooth ,spec * fMask.b );

    float3 RGB = MainTex * aniso * atten ;
    return RGB;



    }

float3 Calc_SpecularColor( float3 f3MainColor,   float fRDotV, float fItensity, float fRange, float4 fMask )
{
    //float3    specPow = exp2(fMask.g *8.0 + 1.0);
   // float3    fRDotV1 = pow(abs(fRDotV), specPow);
    float SpecRange = fRange * fMask.g * 2 ;
    float3   f3Ramp      = smoothstep( SpecRange- _SpecularSmooth , SpecRange  + _SpecularSmooth ,  fRDotV  *  fMask.b ) ;
  
   // float3   f3Specular	=  f3MixColor  * f3Ramp * fItensity * fMask.a ;

    return  f3Ramp * f3MainColor * fItensity * fMask.a   ;
}



float3 Calc_GlowColor( float3 f3MainColor, float3 Glowcolor, float3 f3MaskRGB )
{
    float3   f3GlowColor     = lerp( f3MainColor, f3MainColor * Glowcolor, f3MaskRGB );

    return f3GlowColor;
}

float Unity_Remap_float(float In, float2 InMinMax, float2 OutMinMax)
{
    float Out = OutMinMax.x + (In - InMinMax.x) * (OutMinMax.y - OutMinMax.x) / (InMinMax.y - InMinMax.x);

    return Out;
}

float3 Calc_Dissolve( float3 f3MainColor, float2 f2UV, float fDssvProgress, float fSampleOffset, float fRampUVOffset,float3 DissolvDirection )
{

   /// float Wheight = dot(e(height),float3(0,1,0));

    float height = DissolvDirection.x + DissolvDirection.y + DissolvDirection.z;
    height =   height  -fRampUVOffset + _DssvAmount *_DissolveRange ;
    float3   f3DssvNoise = tex2D( _DssvNoiseTex, TRANSFORM_TEX( f2UV, _DssvNoiseTex ) ).rgb - height;
  //  float    fSample     = f3DssvNoise.r - fDssvProgress; 
    
    float3   f3Result    = f3MainColor;

    if( f3DssvNoise.r < fSampleOffset && fDssvProgress > 0 && fDssvProgress <= 1 )
	{
      

        float3 f3DssvRamp = tex2D( _DssvRampTex, float2( (f3DssvNoise.r) * (1/fSampleOffset),0)).rgb;
	
		clip( f3DssvNoise );
	
      //  f3Result = f3DssvRamp;
        f3Result = lerp(f3DssvRamp *_DssvColor.rgb,f3MainColor,f3DssvNoise);
       // f3Result = f3DssvRamp;
	}

    return f3Result;
}

float3 Cubemap_On ( float3 ViewDir,float3 Normal,  float3 f3MainColor ,float CubeIntencity, float4 RampTex  )
{
        float3 viewReflectDirection = reflect(-ViewDir,  Normal );
        float3 refcolor =  texCUBE(_Cube, viewReflectDirection.xyz ).rgb ;
        f3MainColor.rgb = lerp(f3MainColor.rgb, f3MainColor.rgb * refcolor.b * CubeIntencity, _Cubelerp * RampTex.a);

        return f3MainColor;



}
#endif // DK2_URP_CARTOON_LIB_INCLUDED
