Shader "DK2_URP_Shader/Charactor/CarToon"
{
    Properties
    {

        [Enum(OFF,0,FRONT,1,BACK,2)] _CullMode("Cull Mode", int) = 2
        [Space(20)]
        [Header(_________________________________________________________________________________________)]
        [Header(Env Color)]
        [Space(20)]

        [Toggle]_CustomeLight                     ( "Custome Light", Int )                = 0
        _LightDirection                           ("LightDirection", Vector) = (-1.25,0,0,0)

		[Header(_________________________________________________________________________________________)]
		[Header(Damage Color)]
		[Space(20)]

        [HDR]_DamageC						( "Damage Color", Color )				= ( 0, 0, 0, 1 )
       	[Header(_________________________________________________________________________________________)]
		[Header(Main Color)]
		[Space(20)]
        [HDR]_MainColor						( "MainColor", Color )				    = ( 1, 1, 1, 1 )
        [Toggle(AlphaCutOff)] _AlphaCut          ("Alpha Cut", float)                    = 0
        [Toggle(AlphaBlend)] _AlphaBlend        ("Alpha Blend", float)                  = 0
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
		[HDR]_RimLC							( "Rim Light Color", Color )					= ( 1, 1, 1, 1 )
		[HDR]_RimSC							( "Rim Shadow Color", Color )					= ( 0.5 ,0.5 ,0.5 ,0.5)
		_RimMin								( "Rim Range", Range(0.1,3 ) )			= 0
		_RimMax								( "Rim Smoothing", Range(0,3 ) )			= 0.3
        _RimTC_ITS                          ( "Rim Tex Color Intensity", Float )    = 1.5

        [Header(_________________________________________________________________________________________)]
		[Header(Specular Color)]
       	[Space(20)]
        //[Toggle(USE_NORMAL_MAP_SHADING)]
        //_UseNormalMap                       ("Use Normal Texture(Toggle)", Int)     = 0

      
        _SpecularSmooth						( "Specular Smoothing", Range( 0.001, 1 ) ) = 0.003
        _SpecularRange                      ( "Specular Range", Range( 0., 1 ) )         =  0.75
        _SpecularITS                        ( "Specular Intensity", Range( 0, 5 ) )     = 1
        
        _AnisoDir                           ("AnisoDir", Vector) = (0.5,0.5,1,0)
        _Normal                             ("Normal", Vector) = (0,-100,0,0)
        _AnisoOffset                        ("AnisoOffset", range(0,2)) =  1
        _Gloss                              ("Gloss", range(0.1,10)) =  1
        _AnisoMask                          ("AnisoMask", range(0,1)) = 1
       // _att                                ("Att", float ) = 1
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
        [HDR]_DssvColor							( "Glow Color1", Color )					= ( 1, 1, 1, 1 )

        [Space(20)]

        [NoScaleOffset]
        _DssvRampTex                        ( "Dissolve Ramp", 2D)						= "white" {}
        _DssvNoiseTex                       ( "Dissolve Noise", 2D)						= "black" {}
        _DssvAmount                         ( "_SliceAmount",range( 0 ,1) )       = 0.0
        _DissolveDir ("Dissolve Direction", vector ) = (1,0,0,0)
        _DissolveRange("Dissolve Range", float) = 1
        _DissolveBand("Dissolve Band Size", Float) = 0.25

[Header(_________________________________________________________________________________________)]
        [Header(Cubemap)]

   [Space(20)]
        [Toggle(Cubemap)]_CubemapOn("CubemapOn",float ) = 0
        _Cube("Cube", Cube) = ""{}
        _CubeIntencity("CubeIntencity", range(0,10)) = 1
		_Cubelerp("Cubelerp", Range(0,1)) = 0.5 
       // [Enum(x,0,y,1,z,2)] _DissoveDir ("Dissove Direction", int) = 0
        


       
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
            #include "../DK2_URP_ColorLib.hlsl"

            //
            #pragma prefer_hlslcc gles   
            #pragma exclude_renderers d3d11_9x  
            #pragma target 3.0
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
			#pragma multi_compile _ _ADDITIONAL_LIGHTS 
			#pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
			#pragma multi_compile _ _SHADOWS_SOFT
            #pragma shader_feature AlphaCutOff
            #pragma shader_feature AlphaBlend
            #pragma shader_feature Cubemap
          
            //
            #pragma vertex vert_Main
			#pragma fragment frag_Main

            tOUTPUT_Base vert_Main( tINPUT_Full tIn )
			{
				tOUTPUT_Base	tOut;

                UNITY_SETUP_INSTANCE_ID( tIn );
                UNITY_TRANSFER_INSTANCE_ID( tIn, tOut );
                
                tOut.worldPos  = TransformObjectToWorld( tIn.vertex.xyz );
                tOut.f4Pos		= TransformObjectToHClip( tIn.vertex.xyz );
                tOut.f2UV		= TRANSFORM_TEX( tIn.texcoord, _MainTex );
                tOut.f2UV2		= TRANSFORM_TEX( tIn.texcoord2, _MaskTex );
                tOut.f3NormalW	= TransformObjectToWorldNormal( tIn.normal );
                tOut.f3tangentW = TransformObjectToWorldDir( tIn.tangent.xyz );
                tOut.f3bittangentW = cross( tOut.f3NormalW, tOut.f3tangentW ) * tIn.tangent.w;
               // tOut.f3EyeW     =_WorldSpaceCameraPos.xyz;
                tOut.sPos = ComputeScreenPos(tOut.f4Pos);
                tOut.shadowCoord     = float4( 0 , 0 , 0 , 0 );


             
                tOut.height = tIn.vertex.xyz * _DissolveDir ;
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
                float4  f4GlowMaskTex   = tex2D( _GlowMaskTex, tIn.f2UV2 );
                float4  MaskTex         = tex2D( _MaskTex, tIn.f2UV2 );
                float3  Shadow = saturate(lightMain.shadowAttenuation  ) ; 
                float3  viewDir = normalize(_WorldSpaceCameraPos.xyz-tIn.worldPos);
                float3  lightDir = normalize(_MainLightPosition.xyz);
                float3  f3Normal = normalize(tIn.f3NormalW.xyz);
                float3  f3tangentW = normalize(tIn.f3tangentW.xyz);
                float3  f3BitangentW = normalize(tIn.f3bittangentW.xyz);
          
                float3  CustomeLight = _CustomeLight ?  normalize(viewDir + _LightDirection.xyz) : lightDir ;
                float3	fNDotL		=  max(0,dot( lightDir,  f3Normal  )) ; //이렇게 수정함.
                fNDotL = fNDotL*  MaskTex.r * 2;

            
          
                //RampColor
                float3	f3Ramp		= Calc_RampColor( f4RampTex.rgb *_RampC.rgb  , fNDotL  , lightMain.distanceAttenuation, 1) ;

                //Specular
                float3	f3Reflect	= SafeNormalize( lightDir + viewDir  );
                float	fRDotV		= (max( 0.25, dot( f3Reflect, f3Normal )));
                float3  fSpcColor   = Calc_SpecularColor( f4MainTex.rgb ,  fRDotV * fNDotL , _SpecularITS, _SpecularRange, MaskTex );
                
                //AnisoSpecular
                float3  aniSpcColor = anistoSpecColor( lightDir ,  viewDir, f3Normal , f3tangentW,f3BitangentW, _AnisoOffset, _Gloss, _SpecularITS, f4MainTex.rgb , MaskTex,_SpecularRange, f3Ramp.rgb);
                float3  finalspec   = lerp(fSpcColor , aniSpcColor * fNDotL , f4RampTex.a *_AnisoMask);
                
               
                //AddLight
                #ifdef _ADDITIONAL_LIGHTS
                uint pixelLightCount = GetAdditionalLightsCount();
                for (uint lightIndex = 0u; lightIndex < pixelLightCount; ++lightIndex)
                {
                Light addLight       = GetAdditionalLight(lightIndex, tIn.worldPos,half4(0,0,0,0));
                float distance       = addLight.distanceAttenuation;
                float AddLightShadow =  addLight.shadowAttenuation;
                float3 addLightCol   = max(1,dot(normalize(addLight.direction),f3Normal));
                float3 lightStep     = smoothstep(_RampThreshold - _RampSmooth , _RampThreshold + _RampSmooth, addLightCol*  MaskTex.r * 2  );
                f3Ramp              += lightStep * addLight.color.rgb  *  distance * AddLightShadow  * 0.4;
                };
                #endif
                
                //Result
                float3	f3Result	= (f4MainTex.rgb  * f3Ramp .rgb + finalspec) * saturate(lightMain.color + 0.6)  ;
                f3Result   *= _MainColor.rgb  *  saturate(Shadow + 0.8);
             
                //Rim
                float    fNDotRD     = max( 0, dot( viewDir,f3Normal ) );
                float3   f3RimColor  = Calc_RimColor(  1, fNDotRD * MaskTex.r );
                float    Rimoutline = smoothstep(_RimMin - _RimMax	 , _RimMax	+ _RimMax , fNDotRD );
				
            
        


               
               
                
              
                //Rimlight
                f3Result	= _UseRim ? lerp( lerp(f3Result ,f3Result * _RimLC,  f4RampTex.a) , _RimSC ,   f3RimColor * f4RampTex.a ) : f3Result ;

                //Glow
                f3Result = Calc_GlowColor( f3Result, _GlowC.rgb, f4GlowMaskTex.r );

                //Apply Color Ramp
                //  f3Result = ApplyEnvColorRamp( ( _global_UseRamp && _UseEnvRamp ), f3Result, _global_Ramp_Map, _global_EnvPct_Char );

                #ifdef AlphaCutOff
                clip(f4MainTex.a - _AlphaCutoff);
                #endif

                float alpha = 1;
                #ifdef AlphaBlend
                alpha =  _Alpha  * f4MainTex.a ;
                #endif

               
            
                //Cubemap
                #ifdef Cubemap
                f3Result = Cubemap_On(viewDir , f3Normal, f3Result.rgb, _CubeIntencity, f4RampTex);
                #endif


              
                
                //Grabpass, Distoration
                // float4 ScreenPos = tIn.sPos;
                // float4 screentposNorm = ScreenPos / ScreenPos.w;
                // float4 DistUV = ScreenPos + float4(normalize(tIn.f3NormalW.xyz),1) ;
                // float4 grabPass = tex2Dproj(_CameraOpaqueTexture, DistUV);
                // f3Result = grabPass;
                // f3Result = grabPass;
               
                //Dissolve
                f3Result = Calc_Dissolve( f3Result, tIn.f2UV, _DssvAmount , _DissolveBand,0.2,tIn.height) ;
               
                
                
                return( float4( f3Result,alpha  ) );
			}

            ENDHLSL  
        }

		//OutLine Pass
        Pass
        {
            Name "OutLine"
            Tags 
            { 
                "LightMode" = "SRPDefaultUnlit"
            }

            Cull Front
            ZWrite On
            Blend SrcAlpha OneMinusSrcAlpha
            HLSLPROGRAM

            //
            #include "../DK2_URP_OutLineLib.hlsl"
			
            //
            #pragma prefer_hlslcc gles
            #pragma exclude_renderers d3d11_9x
            #pragma target 3.0

            //
            #pragma vertex vert_OutLine
			#pragma fragment frag_OutLine
            #pragma shader_feature AlphaCutOff
            #pragma shader_feature AlphaBlend

            ENDHLSL  
        }

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
	
				return 0;
			}
	//		#include "Packages/com.unity.render-pipelines.universal/Shaders/SimpleLitInput.hlsl"
           
			ENDHLSL
		}
    }
}
