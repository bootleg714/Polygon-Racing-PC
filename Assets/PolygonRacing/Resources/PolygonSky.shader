// Polygon Racing stylised gradient sky (skybox material, URP, single-pass stereo).
// The horizon colour is set to the scene fog colour so fogged scenery melts into
// the sky. The sun disc follows the scene's main directional light.
Shader "Polygon Racing/Gradient Sky"
{
    Properties
    {
        _ZenithColor("Zenith", Color) = (0.16,0.46,0.82,1)
        _HorizonColor("Horizon (match fog)", Color) = (0.66,0.86,0.93,1)
        _BelowHorizonColor("Below horizon", Color) = (0.40,0.70,0.80,1)
        _GradientPower("Gradient curve", Range(0.1,2)) = 0.55
        _SunColor("Sun", Color) = (1,0.94,0.82,1)
        _SunGlow("Sun glow", Range(0,1)) = 0.18
        _SunHalo("Sun halo", Range(0,1)) = 0.35
        _SunDisc("Sun disc", Range(0,3)) = 1.2
        _SunDiscSize("Sun disc size", Range(0.99,0.99999)) = 0.9994
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Background" "Queue"="Background" "PreviewType"="Skybox" }
        Pass
        {
            Cull Off
            ZWrite Off
            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_instancing
            #pragma multi_compile _ STEREO_INSTANCING_ON STEREO_MULTIVIEW_ON
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            CBUFFER_START(UnityPerMaterial)
                half4 _ZenithColor;
                half4 _HorizonColor;
                half4 _BelowHorizonColor;
                half _GradientPower;
                half4 _SunColor;
                half _SunGlow;
                half _SunHalo;
                half _SunDisc;
                float _SunDiscSize;
            CBUFFER_END
            struct Attributes { float4 positionOS : POSITION; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 direction : TEXCOORD0;
                UNITY_VERTEX_OUTPUT_STEREO
            };
            Varyings Vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.direction = input.positionOS.xyz;
                return output;
            }
            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                float3 d = normalize(input.direction);
                half up = pow(saturate(d.y), _GradientPower);
                half3 color = lerp(_HorizonColor.rgb, _ZenithColor.rgb, up);
                color = lerp(color, _BelowHorizonColor.rgb, saturate(-d.y * 6));

                // _MainLightPosition.xyz is the direction toward the sun (zero if none).
                float3 sunDir = _MainLightPosition.xyz;
                float mu = saturate(dot(d, sunDir));
                half m8 = mu * mu; m8 *= m8; m8 *= m8;
                half m64 = m8 * m8; m64 *= m64; m64 *= m64;
                color += _SunColor.rgb * (m8 * _SunGlow + m64 * _SunHalo);
                color += _SunColor.rgb * _SunDisc * step(_SunDiscSize, mu);
                return half4(color, 1);
            }
            ENDHLSL
        }
    }
    FallBack Off
}
