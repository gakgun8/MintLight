Shader "DK2_URP_Shader/Charactor/CarToon_Monster"
{
    Properties
    {
        [Enum(OFF,0,FRONT,1,BACK,2)] _CullMode("Cull Mode", int) = 2
        [Space(20)]
        [Header(_________________________________________________________________________________________)]
        [Header(Env Color)]
        [Space(20)]

        [Toggle]_UseEnvRamp                     ( "Use Env Ramp", int )                = 1

		[Header(_________________________________________________________________________________________)]
		[Header(Damage Color)]
		[Space(20)]

        [HDR]_DamageC						( "Damage Color", Color )				= ( 0, 0, 0, 1 )
       	[Header(_________________________________________________________________________________________)]
		[Header(Main Color)]
		[Space(20)]
        [HDR]_MainColor						( "MainColor", Color )				= ( 1, 1, 1, 1 )
		_Alpha								( "Alpha", Range( 0, 1 ) )				= 1.0
         [NoScaleOffset]
		_MainTex							( "Base (RGB)", 2D)						= "white" {}

		[Header(_________________________________________________________________________________________)]
		[Header(Toon Ramp)]
		[Space(20)]

		_RampThreshold						( "Ramp Threshold", Range( 0, 1 ) )		= 0.5
		_RampSmooth							( "Ramp Smoothing", Range( 0.001, 1 ) ) = 0.03
		[HDR]_RampC							( "Ramp Color", Color )					= ( 0.8, 0.8, 0.8, 1 )

      

		[Header(_________________________________________________________________________________________)]
		[Header(Out Line)]
		[Space(20)]
       	[HDR]_OutlineC						( "Outline Color", Color )				= ( 0.2, 0.2, 0.2, 1 )
        _RimMin                             ( "RimLight Threshold", Range( 0, 1 ) )		= 0.1
        _RimMax                             ( "RimLight Smothing", Range( 0, 1 ) )		= 0.3

        [Header(_________________________________________________________________________________________)]
		[Header(Glow Color)]
		[Space(20)]

		[HDR]_GlowC							( "Glow Color", Color )					= ( 0.5, 0.5, 0.5, 0.5 )
	
        [NoScaleOffset]
		_GlowMaskTex						( "Glow Mask", 2D)						= "black" {}
        _GlowAni                            ("GlowAni", Vector) = (1,1,1,1)
		

        [Header(_________________________________________________________________________________________)]
        [Header(Dissove)]
       
        // _DissolveScale ("Dissolve Progression", Range(0.0, 1.0)) = 0.0
        [HDR]_DssvColor							( "Glow Color1", Color )					= ( 1, 1, 1, 1 )

        [Space(20)]

        [NoScaleOffset]
        _DssvRampTex                        ( "Dissolve Ramp", 2D)						= "white" {}
        _DssvNoiseTex                       ( "Dissolve Noise", 2D)						= "black" {}
        _Dssvoffset                         ( "Offset   ",range( 0 ,1) )       = 0.5
        _DssvAmount                         ( "SliceAmount",range( 0 ,1) )       = 0.0
       
        _DissolveDir ("Dissolve Direction", vector ) = (1,0,0,0)
        _DissolveRange("Dissolve Range", range(0,10)) = 1
        _DissolveBand("Dissolve Band Size", Float) = 0.15



       
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
           // #include "../DK2_URP_ColorLib.hlsl"
     
            //
            #pragma prefer_hlslcc gles   
            #pragma exclude_renderers d3d11_9x  
            #pragma target 3.0
            #pragma multi_compile_instancing
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
			#pragma multi_compile _ _ADDITIONAL_LIGHTS 
			#pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
			#pragma multi_compile _ _SHADOWS_SOFT
		

            //
            #pragma vertex vert_Main
			#pragma fragment frag_Main
            
            //
            tOUTPUT_Base_Monster vert_Main( tINPUT_Base tIn )
			{
				tOUTPUT_Base_Monster	tOut;

                UNITY_SETUP_INSTANCE_ID( tIn );
                UNITY_TRANSFER_INSTANCE_ID( tIn, tOut );
                
                tOut.worldPos  = TransformObjectToWorld( tIn.vertex.xyz );
                tOut.f4Pos		= TransformObjectToHClip( tIn.vertex.xyz );
                tOut.f2UV		= TRANSFORM_TEX( tIn.texcoord, _MainTex );
                tOut.f2UV2		= TRANSFORM_TEX(tIn.texcoord2.xy + _GlowAni.xy*_Time.xx, _MainTex );
				tOut.f3NormalW	= TransformObjectToWorldNormal( tIn.normal );
                tOut.f3EyeW     = normalize( _WorldSpaceCameraPos.xyz - tOut.worldPos );
				
			
				
				tOut.shadowCoord     = float4( 0 , 0 , 0 , 0 );

                //tOut.height = dot( normalize(_DissolveStart.xyz), normalize(tIn.vertex.xyz - _DissolveEnd.xyz))*_DissolveRange;	
              tOut.height = tIn.vertex.xyz * _DissolveDir.xyz ;
          
				
				return tOut;
			}

            //RampTex 의 Alpha영역으로 Rim의 밝기 제어 하고
			//RampTex 의 RGB색으로 Ramp색을 제어함.
            float4 frag_Main( tOUTPUT_Base_Monster tIn ) : SV_Target
			{
                UNITY_SETUP_INSTANCE_ID( tIn );
                
                
                tIn.shadowCoord = TransformWorldToShadowCoord(tIn.worldPos);
                

                Light   lightMain       = GetMainLight(tIn.shadowCoord);
	            float4	f4MainTex	    = tex2D( _MainTex, tIn.f2UV );
                float4   f4GlowMaskTex   = tex2D( _GlowMaskTex, tIn.f2UV );
                float4   f4GlowMaskTex2   = tex2D( _GlowMaskTex, tIn.f2UV2 + _GlowAni.zw*_Time);
                float4   f4GlowMaskTex1   = tex2D( _GlowMaskTex, tIn.f2UV2 + f4GlowMaskTex2.a );
                float  Shadow = saturate(lightMain.shadowAttenuation + 0.5) ; 
              
                
                //Rim
                  float    fNDotRD     = max( 0, dot( tIn.f3EyeW, tIn.f3NormalW ) );
                  float    Rimoutline = smoothstep( _RimMin - _RimMax , _RimMin +_RimMax , fNDotRD );
                
                //Ramp
                float	fNDotL		= max( 0, dot( tIn.f3EyeW.xyz, normalize( tIn.f3NormalW ) ) ); //이렇게 수정함.
              	float3	f3Ramp		= Calc_RampColor( _RampC.rgb , fNDotL  ,  lightMain.distanceAttenuation, 1);
                
              

               
                  #ifdef _ADDITIONAL_LIGHTS
				uint pixelLightCount = GetAdditionalLightsCount();
				for (uint lightIndex = 0u; lightIndex < pixelLightCount; ++lightIndex)
				{
					Light addLight = GetAdditionalLight(lightIndex, tIn.worldPos,half4(1,1,1,1));
                    float distance = addLight.distanceAttenuation;
					float AddLightShadow =  addLight.shadowAttenuation;
					float3 addLightCol = max(1, dot(addLight.direction, tIn.f3NormalW));
                    float3 lightStep     = smoothstep(_RampThreshold - _RampSmooth , _RampThreshold + _RampSmooth, addLightCol) *0.4;
					f3Ramp.rgb +=  lightStep * addLight.color.rgb  *  distance *  AddLightShadow;
                    
				};
				#endif
                
                
               
				
				  //Result
				float3	f3Result	= lerp (_OutlineC.rgb, (  f4MainTex.rgb * f3Ramp   ).rgb , Rimoutline)  * Shadow  * (lightMain.color + 0.6);
               // f3Result *= 

              
       
				
                //Glow
                f3Result = Calc_GlowColor( f3Result, _GlowC.rgb, f4GlowMaskTex.r * saturate(f4GlowMaskTex1.g + f4GlowMaskTex.b) );
                              
                
                f3Result *= _MainColor.rgb ;
              
                //Apply Color Ramp
              //  f3Result = ApplyEnvColorRamp( ( _global_UseRamp && _UseEnvRamp ), f3Result, _global_Ramp_Map, _global_EnvPct_Char );
                f3Result = Calc_Dissolve( f3Result, tIn.f2UV, _DssvAmount , _DissolveBand,_Dssvoffset,tIn.height)  ;

				//return( float4( height, _Alpha ) );
				return( float4( f3Result  + _DamageC.rgb , _Alpha ) );
			}

            ENDHLSL  
        }

		//OutLine Pass
   //     Pass
   //     {
   //         Name "OutLine"
   //         Tags 
   //         { 
   //             "LightMode" = "SRPDefaultUnlit"
   //         }

   //         Cull Front
   //         ZWrite On

   //         HLSLPROGRAM

   //         //
   //         #include "../DK2_URP_OutLineLib.hlsl"
			
   //         //
   //         #pragma prefer_hlslcc gles
   //         #pragma exclude_renderers d3d11_9x
   //         #pragma target 3.0

   //         //
   //         #pragma vertex vert_OutLine
			//#pragma fragment frag_OutLine

   //         ENDHLSL  
   //     }

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
