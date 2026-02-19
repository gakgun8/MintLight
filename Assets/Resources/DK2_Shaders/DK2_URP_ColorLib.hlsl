#ifndef DK2_URP_COLOR_LIB_INCLUDED
#define DK2_URP_COLOR_LIB_INCLUDED

#ifndef UNIVERSAL_LIGHTING_INCLUDED
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
#endif //UNIVERSAL_LIGHTING_INCLUDED

//----------------------------------------------------------------------------------
//
CBUFFER_START(UnityPerMaterial)
half		_global_UseRamp, _UseEnvRamp;
half		_global_EnvPct_Char;	//캐릭터
half		_global_EnvPct_Map;		//배경
half		_global_EnvPct_Efx;		//이펙트
CBUFFER_END

sampler2D	_global_Ramp_Map;

//----------------------------------------------------------------------------------

float3 AdjustContrast(float3 In, float Contrast)
{
    float midpoint = pow(0.5, 2.2);
    return (In - midpoint) * Contrast + midpoint;
}

//----------------------------------------------------------------------------------

float3 ApplyEnvColorRamp(bool useRamp, float3 col, sampler2D ramp, float pct = 1)
{
    //col = AdjustContrast(col, 1.21);

	if( useRamp == 0 )
		return col;

	float3 envCol = col;

	envCol.r = tex2D(ramp, col.rr).r;
	envCol.g = tex2D(ramp, col.gg).g;
	envCol.b = tex2D(ramp, col.bb).b;

	return lerp(col, envCol, pct);
}

//----------------------------------------------------------------------------------
//채도 (Saturation)
half3 AdjustSaturation(half3 color, half saturation)
{
	half3 intensity = dot(color, half3(0.299, 0.587, 0.114));

	color.rgb = lerp(intensity, color, saturation);

	return color;
}

//----------------------------------------------------------------------------------
//Unity URP Decal
float3 Apply_URP_Decal( half4 f4PosCS )
{
    half3   f3Color = 0;
    half3   f3Normal = 0;
    half3   f3Specular = 0;
    half    fMetalic = 0;
    half    fOcclusion = 0;
    half    fSmoothness = 0;

    ApplyDecal( f4PosCS, f3Color, f3Specular, f3Normal, fMetalic, fOcclusion, fSmoothness );

    return f3Color;
}

//----------------------------------------------------------------------------------
//----------------------------------------------------------------------------------
//Photoshop Color Functions
float pinLight(float s, float d)
{
    return (2.0 * s - 1.0 > d) ? 2.0 * s - 1.0 : (s < 0.5 * d) ? 2.0 * s : d;
}

//--------------------------------------------------------------------------------------

float vividLight(float s, float d)
{
    return (s < 0.5) ? 1.0 - (1.0 - d) / (2.0 * s) : d / (2.0 * (1.0 - s));
}

//--------------------------------------------------------------------------------------

float hardLight(float s, float d)
{
    return (s < 0.5) ? 2.0 * s * d : 1.0 - 2.0 * (1.0 - s) * (1.0 - d);
}

//--------------------------------------------------------------------------------------

float softLight(float s, float d)
{
    return (s < 0.5) ? d - (1.0 - 2.0 * s) * d * (1.0 - d)
        : (d < 0.25) ? d + (2.0 * s - 1.0) * d * ((16.0 * d - 12.0) * d + 3.0)
        : d + (2.0 * s - 1.0) * (sqrt(d) - d);
}

//--------------------------------------------------------------------------------------

float overlay(float s, float d)
{
    return (d < 0.5) ? 2.0 * s * d : 1.0 - 2.0 * (1.0 - s) * (1.0 - d);
}

//--------------------------------------------------------------------------------------

half3 rgb2hsv(half3 c)
{
    half4 K = half4(0.0, -1.0 / 3.0, 2.0 / 3.0, -1.0);
    half4 p = lerp(half4(c.bg, K.wz), half4(c.gb, K.xy), step(c.b, c.g));
    half4 q = lerp(half4(p.xyw, c.r), half4(c.r, p.yzx), step(p.x, c.r));

    float d = q.x - min(q.w, q.y);
    float e = 1.0e-10;
    return half3(abs(q.z + (q.w - q.y) / (6.0 * d + e)), d / (q.x + e), q.x);
}

//--------------------------------------------------------------------------------------

half3 hsv2rgb(half3 c)
{
    half4 K = half4(1.0, 2.0 / 3.0, 1.0 / 3.0, 3.0);
    half3 p = abs(frac(c.xxx + K.xyz) * 6.0 - K.www);
    return c.z * lerp(K.xxx, clamp(p - K.xxx, 0.0, 1.0), c.y);
}

//--------------------------------------------------------------------------------------
// API BLEND MODE
half3 ColorBurn(half3 s, half3 d)
{
    return 1.0 - (1.0 - d) / s;
}

//--------------------------------------------------------------------------------------

half3 LinearBurn(half3 s, half3 d)
{
    return s + d - 1.0;
}

//--------------------------------------------------------------------------------------

half3 DarkerColor(half3 s, half3 d)
{
    return (s.x + s.y + s.z < d.x + d.y + d.z) ? s : d;
}

//--------------------------------------------------------------------------------------

half3 Lighten(half3 s, half3 d)
{
    return max(s, d);
}

//--------------------------------------------------------------------------------------

half3 Screen(half3 s, half3 d)
{
    return s + d - s * d;
}

//--------------------------------------------------------------------------------------

half3 ColorDodge(half3 s, half3 d)
{
    return d / (1.0 - s);
}

//--------------------------------------------------------------------------------------

half3 LinearDodge(half3 s, half3 d)
{
    return s + d;
}

//--------------------------------------------------------------------------------------

half3 LighterColor(half3 s, half3 d)
{
    return (s.x + s.y + s.z > d.x + d.y + d.z) ? s : d;
}

//--------------------------------------------------------------------------------------

half3 Overlay(half3 s, half3 d)
{
    half3 c;
    c.x = overlay(s.x, d.x);
    c.y = overlay(s.y, d.y);
    c.z = overlay(s.z, d.z);
    return c;
}

//--------------------------------------------------------------------------------------

half3 Overlay_Fast(half3 s, half3 d)
{
    half3 r = s > .5 ? 1.0 - 2.0 * (1.0 - s) * (1.0 - d) : 2.0 * s * d;
    return r;
}

//--------------------------------------------------------------------------------------

half3 SoftLight(half3 s, half3 d)
{
    half3 c;
    c.x = softLight(s.x, d.x);
    c.y = softLight(s.y, d.y);
    c.z = softLight(s.z, d.z);
    return c;
}

//--------------------------------------------------------------------------------------

half3 HardLight(half3 s, half3 d)
{
    half3 c;
    c.x = hardLight(s.x, d.x);
    c.y = hardLight(s.y, d.y);
    c.z = hardLight(s.z, d.z);
    return c;
}

//--------------------------------------------------------------------------------------

half3 VividLight(half3 s, half3 d)
{
    half3 c;
    c.x = vividLight(s.x, d.x);
    c.y = vividLight(s.y, d.y);
    c.z = vividLight(s.z, d.z);
    return c;
}

//--------------------------------------------------------------------------------------

half3 LinearLight(half3 s, half3 d)
{
    return 2.0 * s + d - 1.0;
}

//--------------------------------------------------------------------------------------

half3 PinLight(half3 s, half3 d)
{
    half3 c;
    c.x = pinLight(s.x, d.x);
    c.y = pinLight(s.y, d.y);
    c.z = pinLight(s.z, d.z);
    return c;
}

//--------------------------------------------------------------------------------------

half3 HardMix(half3 s, half3 d)
{
    return floor(s + d);
}

//--------------------------------------------------------------------------------------

half3 Difference(half3 s, half3 d)
{
    return abs(d - s);
}

//--------------------------------------------------------------------------------------

half3 Exclusion(half3 s, half3 d)
{
    return s + d - 2.0 * s * d;
}

//--------------------------------------------------------------------------------------

half3 Subtract(half3 s, half3 d)
{
    return s - d;
}

//--------------------------------------------------------------------------------------

half3 Divide(half3 s, half3 d)
{
    return s / d;
}

//--------------------------------------------------------------------------------------

half3 Add(half3 s, half3 d)
{
    return s + d;
}

//--------------------------------------------------------------------------------------

half3 Hue(half3 s, half3 d)
{
    d = rgb2hsv(d);
    d.x = rgb2hsv(s).x;
    return hsv2rgb(d);
}

//--------------------------------------------------------------------------------------

half3 Color(half3 s, half3 d)
{
    s = rgb2hsv(s);
    s.z = rgb2hsv(d).z;
    return hsv2rgb(s);
}

//--------------------------------------------------------------------------------------

half3 Saturation(half3 s, half3 d)
{
    d = rgb2hsv(d);
    d.y = rgb2hsv(s).y;
    return hsv2rgb(d);
}

//--------------------------------------------------------------------------------------

half3 Luminosity(half3 s, half3 d)
{
    half3 l     = half3(0.3, 0.59, 0.11);
    float dLum  = dot(d, l);
    float sLum  = dot(s, l);
    float lum   = sLum - dLum;
    half3 c     = d + lum;
    float minC  = min(min(c.x, c.y), c.z);
    float maxC  = max(max(c.x, c.y), c.z);

    if (minC < 0.0) return sLum + ((c - sLum) * sLum) / (sLum - minC);
    else if (maxC > 1.0) return sLum + ((c - sLum) * (1.0 - sLum)) / (maxC - sLum);
    else return c;
}
//Photoshop Color Functions End
//----------------------------------------------------------------------------------
//----------------------------------------------------------------------------------

#endif // DK2_URP_COLOR_LIB_INCLUDED
