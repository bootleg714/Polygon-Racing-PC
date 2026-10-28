Shader "Polygon Racing/Aurora Ribbon"
{
    Properties { _Intensity("Intensity",Range(0,2))=1 }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent" "RenderType"="Transparent" }
        Pass
        {
            Tags { "LightMode"="SRPDefaultUnlit" }
            Blend SrcAlpha OneMinusSrcAlpha
            Cull Off ZWrite Off ZTest LEqual
            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_instancing
            #pragma multi_compile _ STEREO_INSTANCING_ON STEREO_MULTIVIEW_ON
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            CBUFFER_START(UnityPerMaterial)
                float _Intensity;
            CBUFFER_END
            struct Attributes {float4 positionOS:POSITION;UNITY_VERTEX_INPUT_INSTANCE_ID};
            struct Varyings {float4 positionCS:SV_POSITION;float3 world:TEXCOORD0;UNITY_VERTEX_OUTPUT_STEREO};
            Varyings Vert(Attributes i)
            {
                Varyings o;UNITY_SETUP_INSTANCE_ID(i);UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.world=TransformObjectToWorld(i.positionOS.xyz);o.positionCS=TransformWorldToHClip(o.world);return o;
            }
            half4 Frag(Varyings i):SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
                float x=(i.world.x+i.world.z*.77)*.004;
                float bands=.5+.5*sin(x*4+sin(x*1.3+_Time.y*.12)*2);
                float glow=.5+.5*sin(x*1.4-_Time.y*.08);
                float fade=smoothstep(280,390,i.world.y)*(1-smoothstep(700,850,i.world.y));
                half3 color=lerp(half3(.04,1.0,.55),half3(.30,.25,1.0),glow)*_Intensity;
                return half4(color,fade*(.12+.32*bands));
            }
            ENDHLSL
        }
    }
    FallBack Off
}
