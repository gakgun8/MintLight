Shader "DK2_URP_Shader/Effect/Add_OR_Blend"
{
    Properties
    {
        //[Enum(UnityEngine.Rendering.BlendMode)]
        [Enum(Add,1,Blend,10)] _DstBlend ("DestBlend", Float) = 1

      [HDR]_TintColor ("Tint Color", Color) = (0.5,0.5,0.5,0.5)
        _MainTex ("Particle Texture", 2D) = "white" {}
        _InvFade ("Soft Particles Factor", Range(0.01,3.0)) = 1.0
    }

    SubShader
    {
        Tags 
        { 
            "Queue" = "Transparent"
            "IgnoreProjector" = "True"
            "RenderType" = "Transparent" 
            "PreviewType" = "Plane" 
            "RenderPipeline" = "UniversalPipeline" 
        }

        Blend SrcAlpha [_DstBlend]
       // ColorMask RGB
        Cull back Lighting Off ZWrite Off

        Pass
        {
            HLSLPROGRAM
            #pragma prefer_hlslcc gles   
            #pragma exclude_renderers d3d11_9x  
            #pragma target 3.0
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

            struct appdata_t 
            {
                float4 vertex : POSITION;
                half4 color : COLOR;
                float2 texcoord : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f 
            {
                float4 vertex : SV_POSITION;
                half4 color : COLOR;
                float2 texcoord : TEXCOORD0;
                float4 projPos : TEXCOORD2;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            sampler2D _MainTex;
            
            CBUFFER_START(UnityPerMaterial)
                half4 _TintColor, _MainTex_ST;
                half _InvFade;
            CBUFFER_END

            v2f vert (appdata_t v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_TRANSFER_INSTANCE_ID(v, o);

                o.vertex = TransformObjectToHClip(v.vertex.xyz);
                //o.projPos = ComputeScreenPos (o.vertex);

                //COMPUTE_EYEDEPTH(o.projPos.z) to follow
                o.projPos.z = -TransformWorldToView(TransformObjectToWorld(v.vertex.xyz)).z;
                                
                o.color = v.color;
                o.texcoord = TRANSFORM_TEX(v.texcoord,_MainTex);

                return o;
            }

            half4 frag (v2f i) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(i);

                
                // float sceneZ = LinearEyeDepth(SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, sampler_CameraDepthTexture, i.projPos.xy/ i.projPos.w), _ZBufferParams);
                // float partZ = i.projPos.z;
                // float fade = saturate(_InvFade * (sceneZ - partZ));
                // i.color.a *= fade;
                

                half4 col = 2.0f * i.color * _TintColor * tex2D(_MainTex, i.texcoord);
                col.a = saturate(col.a); // alpha should not have double-brightness applied to it, but we can't fix that legacy behavior without breaking everyone's effects, so instead clamp the output to get sensible HDR behavior (case 967476)

                return col;
            }
            ENDHLSL
        }
    }
}
