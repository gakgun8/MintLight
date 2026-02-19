Shader "DK2_URP_Shader/Map/BG_Mirror"
{
    Properties
    {
        _Tint("Tint Color", Color) = (1, 1, 1, 1)
        _Blur_Intensity("Blur Intensity",Range(0, 5)) = 1
    }

        SubShader
    {
        Name  "DK2_Mirror_Reflect"

        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "Transparent"
            "Queue" = "Transparent"
        }

        Cull Back

        Blend One One

        Pass
        {
           Name "Universal Forward"
           Tags
           {
               "LightMode" = "UniversalForward"
           }

           HLSLPROGRAM

        //
        #pragma prefer_hlslcc gles   
        #pragma exclude_renderers d3d11_9x  
        #pragma target 3.0

        //
        #pragma vertex vert_Main
        #pragma fragment frag_Main

        //For Shadow
        //#pragma multi_compile _ _MAIN_LIGHT_SHADOWS

        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

        CBUFFER_START(UnityPerMaterial)

        half4 _Tint;
        half _Blur_Intensity;
            
        CBUFFER_END
         
        uniform sampler2D _ReflectionTexUp;
        float4	_ReflectionTexUp_TexelSize;
        //sampler2D _NoiseTex;
         
        struct VertexInput
        {
            float4 vertex : POSITION;
            float2 uv     : TEXCOORD0;
                
            UNITY_VERTEX_INPUT_INSTANCE_ID                              
        };

        struct VertexOutput
        {
            float4 vertex       : SV_POSITION;
            //float2 uv           : TEXCOORD0;
            float4 uvScr        : TEXCOORD0;
            float4 shadowCoord  : TEXCOORD1;
             
            UNITY_VERTEX_INPUT_INSTANCE_ID
        };

          VertexOutput vert_Main(VertexInput v)
            {
              VertexOutput o;
              UNITY_SETUP_INSTANCE_ID(v);
              UNITY_TRANSFER_INSTANCE_ID(v, o);

              o.vertex  = TransformObjectToHClip(v.vertex.xyz);
              //o.uv      = v.uv;
              o.uvScr   = ComputeScreenPos(o.vertex);
                 
              VertexPositionInputs vertexInput = GetVertexPositionInputs(v.vertex.xyz);
              o.shadowCoord = GetShadowCoord(vertexInput);
            
              return o;
            }

            half2 ScaleUV(float2 fUV, float fScale)
            {
                float2 fScaleUV = fUV * fScale - ( fScale * 0.5 );

                return fScaleUV;
            }

            // Standard box filtering
            half4 UpsampleBox(sampler2D texName, float2 uv, float2 texelSize, float sampleScale)
            {
                float4	offset = texelSize.xyxy * float4(-1.0, -1.0, 1.0, 1.0) * (sampleScale * 0.5);
                half4	sum = tex2D(texName, uv + offset.xy);

                sum += tex2D(texName, uv + offset.zy);
                sum += tex2D(texName, uv + offset.xw);
                sum += tex2D(texName, uv + offset.zw);

                return sum * 0.25f;
            }

            half4 frag_Main(VertexOutput i) : SV_Target
            {
              UNITY_SETUP_INSTANCE_ID(i);
              UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
              
              //With Noise
              //float2 texNoise   = tex2D(_NoiseTex, i.uv).rg;
              //float2 scrUV      = ( i.uvScr / i.uvScr.w ).xy + ScaleUV( texNoise, _Ref_Intencity );
              //float4 texColor   = tex2D( _ReflectionTexUp, scrUV );
              //
              //Light mainLight = GetMainLight(i.shadowCoord);
              //
              //texColor.rgb *= _MainLightColor.rgb * mainLight.shadowAttenuation * mainLight.distanceAttenuation;
              //
              //return texColor;
              
              //With UpSampling
              float2 scrUV = (i.uvScr / i.uvScr.w).xy;
              
              half4 result = UpsampleBox( _ReflectionTexUp, scrUV, _ReflectionTexUp_TexelSize, _Blur_Intensity);
              //half4 two = UpsampleBox(_ReflectionTexUp, scrUV, _ReflectionTexUp_TexelSize, 1.25);
              //half4 three = UpsampleBox(_ReflectionTexUp, scrUV, _ReflectionTexUp_TexelSize, 1.5);

              //result.a = smoothstep( 0, 1, result.rgb * 5 );

              result.rgb *= 0.5;

              return result * _Tint;
              //return ( two + three ) * 0.5f;
            }

            ENDHLSL  
        }
    }
}
