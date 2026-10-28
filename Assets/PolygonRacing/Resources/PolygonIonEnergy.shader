Shader "Polygon Racing/Ion Energy Channel"
{
    Properties
    {
        [MainColor] _BaseColor("Channel",Color)=(.008,.055,.085,1)
        [HDR] _EmissionColor("Energy",Color)=(.07,1.4,1.9,1)
    }
    SubShader
    {
        Tags {"RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" "Queue"="Geometry"}
        Pass
        {
            Tags {"LightMode"="SRPDefaultUnlit"}
            Cull Back ZWrite On ZTest LEqual
            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_instancing
            #pragma multi_compile _ STEREO_INSTANCING_ON STEREO_MULTIVIEW_ON
            #pragma multi_compile_fog
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
                half4 _EmissionColor;
            CBUFFER_END
            struct Attributes {float4 positionOS:POSITION; UNITY_VERTEX_INPUT_INSTANCE_ID};
            struct Varyings {float4 positionCS:SV_POSITION; float3 positionWS:TEXCOORD0; UNITY_VERTEX_OUTPUT_STEREO};
            Varyings Vert(Attributes i)
            {
                Varyings o;UNITY_SETUP_INSTANCE_ID(i);UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.positionWS=TransformObjectToWorld(i.positionOS.xyz);o.positionCS=TransformWorldToHClip(o.positionWS);return o;
            }
            half4 Frag(Varyings i):SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
                float2 p=i.positionWS.xz;
                float stream=sin(p.x*.18+sin(p.y*.045-_Time.y*.7)*2.5);
                float band=frac(p.y*.032-_Time.y*.24+sin(p.x*.07)*.4);
                float pulse=smoothstep(.58,.67,band)*(1-smoothstep(.81,.95,band));
                half3 color=_BaseColor.rgb+_EmissionColor.rgb*(.20+.32*pow(abs(stream),8)+pulse*.55);
                float distance=length(i.positionWS-_WorldSpaceCameraPos);half fog=1;
                #if defined(FOG_LINEAR)
                    fog=saturate(distance*unity_FogParams.z+unity_FogParams.w);
                #elif defined(FOG_EXP)
                    fog=saturate(exp2(-unity_FogParams.y*distance));
                #elif defined(FOG_EXP2)
                    float f=unity_FogParams.x*distance;fog=saturate(exp2(-f*f));
                #endif
                return half4(lerp(unity_FogColor.rgb,color,fog),1);
            }
            ENDHLSL
        }
    }
    FallBack Off
}
