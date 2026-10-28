// Animated opaque data facades: one shared material, no texture or per-object update.
Shader "Polygon Racing/Neon Data Panel"
{
    Properties
    {
        [MainColor] _BaseColor("Panel",Color)=(.01,.04,.08,1)
        [HDR] _EmissionColor("Signal",Color)=(.05,1.5,2.5,1)
        _ScrollSpeed("Data speed",Float)=.22
        _PatternScale("Data scale",Float)=.42
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
                float _ScrollSpeed;
                float _PatternScale;
            CBUFFER_END
            struct Attributes {float4 positionOS:POSITION; UNITY_VERTEX_INPUT_INSTANCE_ID};
            struct Varyings {float4 positionCS:SV_POSITION; float3 positionWS:TEXCOORD0; UNITY_VERTEX_OUTPUT_STEREO};
            Varyings Vert(Attributes input)
            {
                Varyings o;UNITY_SETUP_INSTANCE_ID(input);UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.positionWS=TransformObjectToWorld(input.positionOS.xyz);o.positionCS=TransformWorldToHClip(o.positionWS);return o;
            }
            half4 Frag(Varyings input):SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                float column=floor((input.positionWS.x+input.positionWS.z)*_PatternScale);
                float seed=frac(sin(column*17.37)*43758.5453);
                float row=frac(input.positionWS.y*_PatternScale*.24+_Time.y*_ScrollSpeed+seed);
                float pulse=smoothstep(.46,.52,row)*(1-smoothstep(.78,.90,row));
                float glyph=step(.25,frac(input.positionWS.y*_PatternScale*1.8+seed*7));
                half3 color=_BaseColor.rgb+_EmissionColor.rgb*(.12+pulse*glyph*.88);
                float distance=length(input.positionWS-_WorldSpaceCameraPos);half fog=1;
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
