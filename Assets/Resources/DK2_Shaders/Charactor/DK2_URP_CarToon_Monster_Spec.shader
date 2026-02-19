Shader "DK2_URP_Shader/Charactor/CarToon_Monster_Spec"
{
    Properties
    {
        [Enum(OFF,0,FRONT,1,BACK,2)] _CullMode("Cull Mode", int) = 2
        [Space(20)]
        [Header(_________________________________________________________________________________________)]
        [Header(Env Color)]
        [Space(20)]

        [Toggle]_UseEnvRamp                     ( "Use Env Ramp", Int )                = 1

		[Header(_________________________________________________________________________________________)]
		[Header(Damage Color)]
		[Space(20)]

        [HDR]_DamageC						( "Damage Color", Color )				= ( 0, 0, 0, 1 )
       	[Header(_________________________________________________________________________________________)]
		[Header(Main Color)]
		[Space(20)]
        [HDR]_MainColor						( "MainColor", Color )				    = ( 1, 1, 1, 1 )
        [MaterialToggle] _AlphaCut          ("Alpha Cut", float)                    = 0
        [MaterialToggle] _AlphaBlend        ("Alpha Blend", float)                  = 0
        _AlphaCutoff                        ("Alpha Cutoff", Range(0, 1))           = 0.5
		_Alpha								( "Alpha", Range( 0, 1 ) )			    = 1.0


         [NoScaleOffset]
		_MainTex							( "Base (RGB)", 2D)						= "white" {}

		[Header(_________________________________________________________________________________________)]
		[Header(Toon Ramp)]
		[Space(20)]

		_RampThreshold						( "Ramp Threshold", Range( 0, 1 ) )		= 0.5
		_RampSmooth							( "Ramp Smoothing", Range( 0.001, 1 ) ) = 0.03
		[HDR]_RampC							( "Ramp Color", Color )					= ( 0.8, 0.8, 0.8, 1 )

        [NoScaleOffset]
		_RampTex							( "Ramp Tex (RGB)", 2D)					= "white" {}

        [Header(_________________________________________________________________________________________)]
        [Header(R_RampRange  G_SpecRange  B_Area  A_Intensity)]
        [Space(20)]
		_MaskTex							( "Mask Tex (RGB) ", 2D)					= "white" {}
		
        [Header(_________________________________________________________________________________________)]
		[Header(Rim Light)]
		[Space(20)]

        [Toggle]_UseRim                     ( "Use Rim Light", Int )                = 0
		[HDR]_RimLC							( "Rim Color", Color )					= ( 1, 1, 1, 1 )
		_RimMin								( "Rim Min", Range( 0, 0.3 ) )			= 0
		_RimMax								( "Rim Max", Range( 0, 0.3 ) )			= 0.3
        _RimTC_ITS                          ( "Rim Tex Color Intensity", Float )    = 1.5

        [Header(_________________________________________________________________________________________)]
		[Header(Specular Color)]
       	[Space(20)]
        //[Toggle(USE_NORMAL_MAP_SHADING)]
        //_UseNormalMap                       ("Use Normal Texture(Toggle)", Int)     = 0

      
        _SpecularSmooth						( "Specular Smoothing", Range( 0.001, 1 ) ) = 0.003
        _SpecularRange                      ( "Specular Range", Range( 0.5, 1 ) )         =  0.75
        _SpecularITS                        ( "Specular Intensity", Range( 0, 5 ) )     = 1
        
		[Header(_________________________________________________________________________________________)]
		[Header(Out Line)]
		[Space(20)]

		_OutlineSize						( "Outline size", range(0.001,0.01)	)			= 0.003
		_OutlinePower						( "Outline power", float )				= 1.0
        _OutLineDepth                       ( "Outline Depth", float )				= 0
		[HDR]_OutlineC						( "Outline Color", Color )				= ( 0.2, 0.2, 0.2, 1 )

        [Header(_________________________________________________________________________________________)]
		[Header(Glow Color)]
		[Space(20)]

		[HDR]_GlowC							( "Glow Color", Color )					= ( 1, 1, 1, 1 )
	
        [NoScaleOffset]
		_GlowMaskTex						( "Glow Mask", 2D)						= "black" {}

        [Header(_________________________________________________________________________________________)]
        [Header(Dissove)]
       
        // _DissolveScale ("Dissolve Progression", Range(0.0, 1.0)) = 0.0
        [HDR]_DssvColor							( "Glow Color1", Color )					= ( 2, 2, 2, 2 )

        [Space(20)]

        [NoScaleOffset]
        _DssvRampTex                        ( "Dissolve Ramp", 2D)						= "white" {}
        _DssvNoiseTex                       ( "Dissolve Noise", 2D)						= "black" {}
        _DssvAmount                         ( "_SliceAmount",range( 0 ,1) )       = 0.0
        _DissolveDir ("Dissolve Direction", vector ) = (1,0,0,0)
        _DissolveRange("Dissolve Range", float) = 3
        _DissolveBand("Dissolve Band Size", Float) = 0.25



       
    }

    SubShader
    {
        Tags 
        {
            "RenderPipeline"="UniversalPipeline"
            "RenderType"="Opaque"
            "Queue"="Geometry"
        }

        Cull [_CullMode]
		
		Blend SrcAlpha OneMinusSrcAlpha

        //MainToon Pass
        Pass
        {
            Name "Universal Forward"
            Tags 
            { 
                "LightMode" = "UniversalForward"
            }



            HLSLPROGRAM

            //
            #include "../DK2_URP_CarToonLib.hlsl"
          //  #include "../DK2_URP_ColorLib.hlsl"

            //
            #pragma prefer_hlslcc gles   
            #pragma exclude_renderers d3d11_9x  
            #pragma target 3.0
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
			#pragma multi_compile _ _ADDITIONAL_LIGHTS 
			#pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
			#pragma multi_compile _ _SHADOWS_SOFT

            //
            #pragma vertex vert_Main
			#pragma fragment frag_Main
            
            //
            tOUTPUT_Base vert_Main( tINPUT_Full tIn )
			{
				tOUTPUT_Base	tOut;

                UNITY_SETUP_INSTANCE_ID( tIn );
                UNITY_TRANSFER_INSTANCE_ID( tIn, tOut );
                
               tOut.worldPos  = TransformObjectToWorld( tIn.vertex.xyz );
                tOut.f4Pos		= TransformObjectToHClip( tIn.vertex.xyz );
                tOut.f2UV		= TRANSFORM_TEX( tIn.texcoord, _MainTex );
				tOut.f3NormalW	= TransformObjectToWorldNormal( tIn.normal );
				tOut.f3EyeW     = normalize( _WorldSpaceCameraPos.xyz - tOut.worldPos );
				
				tOut.shadowCoord     = float4( 0 , 0 , 0 , 0 );


                
               // float3 dDir = normalize(_DissolveRange.xyz - _DissolveStart.xyz);
               // float3 dissolveStartConverted = _DissolveStart.xyz -_DssvAmount* dDir;
               // float dBandFactor = 1.0f / _DissolveBand;
               //float3 dPoint = lerp(dissolveStartConverted, _DissolveRange.xyz, _DssvAmount);
               // tOut.height = dot(tIn.vertex -dPoint, dissolveStartConverted) * dBandFactor;	
                // tOut.height = dot( normalize(_DissolveStart.xyz), normalize(tIn.vertex.xyz - _DissolveEnd.xyz))*_DissolveRange;	
              tOut.height = tIn.vertex.xyz * _DissolveDir.xyz ;
				
				return tOut;
			}

            //RampTex 의 Alpha영역으로 Rim의 밝기 제어 하고
			//RampTex 의 RGB색으로 Ramp색을 제어함.
            float4 frag_Main( tOUTPUT_Base tIn ) : SV_Target
			{
                UNITY_SETUP_INSTANCE_ID( tIn );


                tIn.shadowCoord = TransformWorldToShadowCoord(tIn.worldPos);
              
                Light   lightMain       = GetMainLight(tIn.shadowCoord);
	            float4	f4MainTex	    = tex2D( _MainTex, tIn.f2UV );
                float4	f4RampTex	    = tex2D( _RampTex, tIn.f2UV );
                float4  f4GlowMaskTex   = tex2D( _GlowMaskTex, tIn.f2UV );
                float4  MaskTex         = tex2D( _MaskTex, tIn.f2UV );
              //  float   fSpMaskTex      = tex2D( _SpMaskTex, tIn.f2UV ).r;
                float  Shadow = saturate(lightMain.shadowAttenuation  ) ; 
               

                float3 Addlightcolor = (0,0,0);
                  #ifdef _ADDITIONAL_LIGHTS
				uint pixelLightCount = GetAdditionalLightsCount();
				for (uint lightIndex = 0u; lightIndex < pixelLightCount; ++lightIndex)
				{
					Light addLight = GetAdditionalLight(lightIndex, tIn.worldPos,half4(1,1,1,1));
                    float distance = addLight.distanceAttenuation;
					Shadow = Shadow * addLight.shadowAttenuation;
					float3 addLightCol = max(0, dot(addLight.direction, tIn.f3NormalW));
					Addlightcolor.rgb += addLight.color.rgb  *  distance ;
                    
				};
				#endif
                


                //Ramp
                float	fNDotL		= max( 0, dot( _MainLightPosition.xyz, normalize( tIn.f3NormalW ) ) )* Shadow ; //이렇게 수정함.
            
               float3	f3Ramp		= Calc_RampColor( f4RampTex.rgb *_RampC.rgb  , fNDotL *  MaskTex.r * 2, 0.25f,1) ;
            
                
                
                //float3	f3Ramp1		= lerp( f4RampTex.rgb *_RampC.rgb  , 1  , f3Ramp );
				
				//Rim
                float    fNDotRD     = max( 0, dot( tIn.f3EyeW, tIn.f3NormalW ) );
                float3   f3RimColor  = Calc_RimColor(  1, fNDotRD ) * ( f4MainTex.rgb * _RimTC_ITS );
                float    Rimoutline = smoothstep( 0.2 - 0.01 , 0.2+0.03 , fNDotRD );
				
                //Specular
                float3	f3Reflect	= normalize(tIn.f3EyeW + normalize(_MainLightPosition.xyz) );
                
                float	fRDotV		= max( 0, dot( f3Reflect,normalize(tIn.f3NormalW )) );
                float3   fSpcColor   = Calc_SpecularColor( f4MainTex.rgb , fRDotV, _SpecularITS, _SpecularRange, MaskTex );
                
                //Result
				float3	f3Result	=(( f4MainTex * f3Ramp ).rgb + f3RimColor *f4MainTex.rgb) + fSpcColor;
              
              
				
                //Glow
                f3Result = Calc_GlowColor( f3Result,_GlowC.rgb, f4GlowMaskTex.rgb );
                              
              
                //Apply Color Ramp
               // f3Result = ApplyEnvColorRamp( ( _global_UseRamp && _UseEnvRamp ), f3Result, _global_Ramp_Map, _global_EnvPct_Char );


                float alphacutoff = (_AlphaCut ? f4MainTex.a - _AlphaCutoff : 1);
				clip(alphacutoff);
				float alpha = _AlphaBlend ? _Alpha * f4MainTex.a : _Alpha;

                f3Result *= (saturate(lightMain.color.rgb * lightMain.distanceAttenuation + 0.4 + Addlightcolor) ) *_MainColor.rgb;
                    f3Result = Calc_Dissolve( f3Result, tIn.f2UV, _DssvAmount , _DissolveBand,0.2,tIn.height) ;

				//return( float4( height, _Alpha ) );
				return( float4( f3Result  + _DamageC.rgb , alpha  ) );
			}

            ENDHLSL  
        }

		//OutLine Pass
        Pass
        {
            Name "ShadowCaster"

            Tags{"LightMode" = "ShadowCaster"}

            Cull [_CullMode]

            HLSLPROGRAM

            #include "../DK2_URP_ShadowCastLib.hlsl"

            #pragma prefer_hlslcc gles
            #pragma exclude_renderers d3d11_9x
            #pragma target 3.0

            // GPU Instancing
            #pragma multi_compile_instancing
            
            #pragma vertex ShadowPassVertex
            #pragma fragment ShadowPassFragment
            
            ENDHLSL
        }
    }
}
