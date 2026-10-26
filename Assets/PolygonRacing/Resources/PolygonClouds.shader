// Polygon Racing faceted clouds (URP, single-pass stereo).
// The cloud mesh is authored as offsets from the viewer, and the vertex shader
// re-centres it on each eye's camera every frame, so the clouds sit at infinity
// like the sky itself, without a script or a far clip plane change.
Shader "Polygon Racing/Sky Clouds"
{
    Properties
    {
        [MainColor] _BaseColor("Cloud color", Color) = (1,0.99,0.97,1)
        _ShadowColor("Shade color", Color) = (0.62,0.75,0.88,1)
        _HorizonColor("Horizon haze (match fog)", Color) = (0.66,0.86,0.93,1)
        _HorizonMix("Horizon haze amount", Range(0,1)) = 0.18
        _SunWrap("Sun wrap", Range(0,1)) = 0.3
        _SunStrength("Sun strength", Range(0,2)) = 0.55
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" "Queue"="Geometry+10" }
        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }
            Cull Off
            ZWrite On
            ZTest LEqual
            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_instancing
            #pragma multi_compile _ STEREO_INSTANCING_ON STEREO_MULTIVIEW_ON
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
                half4 _ShadowColor;
                half4 _HorizonColor;
                half _HorizonMix;
                half _SunWrap;
                half _SunStrength;
            CBUFFER_END
            struct Attributes { float4 positionOS : POSITION; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 offset : TEXCOORD0;
                UNITY_VERTEX_OUTPUT_STEREO
            };
            Varyings Vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                output.offset = input.positionOS.xyz;
                output.positionCS = TransformWorldToHClip(_WorldSpaceCameraPos + input.positionOS.xyz);
                return output;
            }
            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                float3 rel = input.offset;
                float3 crossed = cross(ddx(rel), ddy(rel));
                float3 n = crossed * rsqrt(max(dot(crossed, crossed), 1e-20));
                n = dot(n, rel) > 0 ? -n : n;
                Light sun = GetMainLight();
                half diffuse = saturate((dot(n, sun.direction) + _SunWrap) / (1 + _SunWrap));
                half3 color = _BaseColor.rgb * (_ShadowColor.rgb * 0.9h + sun.color * diffuse * _SunStrength);
                color = lerp(color, _HorizonColor.rgb, _HorizonMix);
                return half4(color, 1);
            }
            ENDHLSL
        }
    }
    FallBack Off
}
