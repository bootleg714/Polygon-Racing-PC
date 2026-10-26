Shader "Polygon Racing/Gantry Lettering"
{
    Properties { _MainTex("Font Atlas",2D)="white" {} }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent" "RenderType"="Transparent" }
        Pass
        {
            Tags { "LightMode"="SRPDefaultUnlit" }
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            ZTest LEqual
            Cull Off
            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_instancing
            #pragma multi_compile _ STEREO_INSTANCING_ON STEREO_MULTIVIEW_ON
            #pragma multi_compile_fog
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);
            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
            CBUFFER_END
            struct Attributes
            {
                float4 positionOS:POSITION;
                float2 uv:TEXCOORD0;
                half4 color:COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };
            struct Varyings
            {
                float4 positionCS:SV_POSITION;
                float2 uv:TEXCOORD0;
                half4 color:COLOR;
                float fogFactor:TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };
            Varyings Vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                output.positionCS=TransformObjectToHClip(input.positionOS.xyz);
                output.uv=input.uv*_MainTex_ST.xy+_MainTex_ST.zw;
                output.color=input.color;
                output.fogFactor=ComputeFogFactor(output.positionCS.z);
                return output;
            }
            half4 Frag(Varyings input):SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                half alpha=SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,input.uv).a*input.color.a;
                clip(alpha-.01h);
                return half4(MixFog(input.color.rgb,input.fogFactor),alpha);
            }
            ENDHLSL
        }
    }
}
