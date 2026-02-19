Shader "DK2_URP_Shader/Map/Gradient_Sky"
{
    Properties 
    {
        [HDR]_Color1 ("Top Color", Color) = (0.97, 0.67, 0.51, 0)
        [HDR]_Color2 ("Middle Color", Color) = (0, 0.7, 0.74, 0)
        [HDR]_Color3 ("Bottom Color", Color) = (0, 0.7, 0.74, 0)
        [HDR]_CloudColor ("Cloud Color", Color) = (1, 1, 1, 1)
        _CloudTex ("Cloud ",2D) = "white" {}


         [Space]
        _Rotate ("Rotate", Range (0,1)) = 0

        [Space]
        _Intensity ("Intensity", Range (0, 2)) = 1.0
       

        [Space]
        _ColorRange ("Color Grade Range", Range (-1, 1)) = 0
        _ColorSmooth ("Color Grade smooth", Range (0, 1)) = 0.5

        [Space]
        _Direction ("Direction", Vector) = (0, 1, 0, 0)
    }

    SubShader 
    {
        Tags
        { 
            "RenderType" = "Background"
            "Queue" = "Background" 
            "RenderPipeline" = "UniversalPipeline" 
        }

        Pass 
        {
            Tags 
			{ 
				//"LightMode" = "Background"
			}
            ZWrite On
            Cull back
            Fog { Mode Off }

            HLSLPROGRAM
            #pragma prefer_hlslcc gles   
            #pragma exclude_renderers d3d11_9x  
            #pragma target 3.0
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/RealtimeLights.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"

            struct appdata 
            {
                float4 position : POSITION;
                float3 texcoord : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f 
            {
                float4 position : SV_POSITION;
                float3 texcoord : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            CBUFFER_START(UnityPerMaterial)

            sampler2D _CloudTex;
            half4 _Color1,_Color2,_Color3,_CloudColor;
            half4 _CloudTex_ST;
            half3 _Direction;
            half _Intensity,_Rotate;
            half _Exponent,_ColorRange,_ColorSmooth;
            CBUFFER_END

            v2f vert (appdata v) 
            {
                v2f o;
                
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_TRANSFER_INSTANCE_ID(v, o);

                o.position = TransformObjectToHClip(v.position.xyz);
                o.texcoord = v.texcoord;
                return o;
            }

               float2 ToRadialCoords(float3 coords)
            {
                float3 normalizedCoords = normalize(coords);
                float latitude = acos(normalizedCoords.y);
                float longitude = atan2(normalizedCoords.z, normalizedCoords.x);
                const float2 sphereCoords = float2(longitude, latitude) * float2(0.5 / PI,2 / PI);
                return float2(0.5, 1.0) - sphereCoords;
            }


            half4 frag (v2f i) : COLOR 
            {
                //Light mainLight = GetMainLight();
                half2 tc = ToRadialCoords(i.texcoord);
                half4 Clouddiff = tex2D(_CloudTex, TRANSFORM_TEX(half2(tc.x + 0.01 * _Time.x ,tc.y), _CloudTex))  ;
                Clouddiff *= _CloudColor;
                
                half s = dot(normalize(i.texcoord), _Direction) - 0.5;
              //  half s = dot(normalize(i.texcoord), float4(0,1,0,0)) -0.5;
                half d= smoothstep(_ColorRange - _ColorSmooth , _ColorRange + _ColorSmooth,s );

                half4 finalCol = lerp (_Color2, _Color1, saturate(d)) * _Intensity;
                half UVoffset = smoothstep(0.25 + 0.1 , 0.25 - 0.1,s );
                half UVoffset1 = smoothstep(-0.5- 0.1 , -0.5 + 0.1,s );
                finalCol = lerp(_Color3,finalCol,UVoffset1);
                finalCol = lerp ( finalCol ,Clouddiff, Clouddiff.a * UVoffset * UVoffset1);

                return finalCol  ;
            }
            ENDHLSL
        }
    }
}
