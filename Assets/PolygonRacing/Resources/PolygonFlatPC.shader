Shader "Polygon Racing/PC Flat Lit"
{
    Properties
    {
        [MainColor] _BaseColor("Color", Color) = (1,1,1,1)
        _EmissionColor("Emission", Color) = (0,0,0,1)
        _SkyAmbient("Sky ambient", Color) = (0.68,0.78,0.90,1)
        _GroundAmbient("Ground ambient", Color) = (0.54,0.48,0.40,1)
        _AmbientStrength("Ambient strength", Range(0,2)) = 0.95
        _SunWrap("Sun wrap", Range(0,1)) = 0.12
        _RimStrength("Rim light", Range(0,1)) = 0.08
        _GroundShade("Ground contact shade", Range(0,1)) = 0.2
        _Metallic("Metallic", Range(0,1)) = 0
        _Smoothness("Smoothness", Range(0,1)) = 0.18
        _ReflectionStrength("Environment reflection", Range(0,1)) = 0.35
        _DirectSpecular("Sun/moon highlight", Range(0,1)) = 1
        [Enum(UnityEngine.Rendering.CullMode)] _Cull("Cull", Float) = 2
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" "Queue"="Geometry" }

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        CBUFFER_START(UnityPerMaterial)
            half4 _BaseColor;
            half4 _EmissionColor;
            half4 _SkyAmbient;
            half4 _GroundAmbient;
            half _AmbientStrength;
            half _SunWrap;
            half _RimStrength;
            half _GroundShade;
            half _Metallic;
            half _Smoothness;
            half _ReflectionStrength;
            half _DirectSpecular;
            half _Cull;
        CBUFFER_END
        ENDHLSL

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }
            Cull [_Cull]
            ZWrite On
            ZTest LEqual
            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_instancing
            #pragma multi_compile _ STEREO_INSTANCING_ON STEREO_MULTIVIEW_ON
            #pragma multi_compile_fog
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            // Forward+ (the PC renderer) only feeds reflection probes to shaders compiled with the
            // cluster light loop and the probe blending/atlas keywords; without it every surface reflected the skybox only. Circuits with
            // no probes still fall back to the skybox, so their look is unchanged.
            #pragma multi_compile _ _CLUSTER_LIGHT_LOOP
            #pragma multi_compile_fragment _ _REFLECTION_PROBE_BLENDING
            #pragma multi_compile_fragment _ _REFLECTION_PROBE_BOX_PROJECTION
            #pragma multi_compile_fragment _ _REFLECTION_PROBE_ATLAS
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 relativeWS : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
                float heightWS : TEXCOORD2;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            half FogVisibility(float distance)
            {
                #if defined(FOG_LINEAR)
                    return saturate(distance * unity_FogParams.z + unity_FogParams.w);
                #elif defined(FOG_EXP)
                    return saturate(exp2(-unity_FogParams.y * distance));
                #elif defined(FOG_EXP2)
                    float d = unity_FogParams.x * distance;
                    return saturate(exp2(-d * d));
                #else
                    return 1;
                #endif
            }

            Varyings Vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                output.positionCS = TransformWorldToHClip(positionWS);
                output.positionWS = positionWS;
                output.relativeWS = positionWS - _WorldSpaceCameraPos;
                output.heightWS = positionWS.y;
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                float3 rel = input.relativeWS;
                float3 crossed = cross(ddx(rel), ddy(rel));
                float3 faceNormal = crossed * rsqrt(max(dot(crossed, crossed), 1e-20));
                faceNormal = dot(faceNormal, rel) > 0 ? -faceNormal : faceNormal;

                float4 shadowCoord = TransformWorldToShadowCoord(input.positionWS);
                Light sun = GetMainLight(shadowCoord);
                half ndl = dot(faceNormal, sun.direction);
                half diffuse = saturate((ndl + _SunWrap) / (1 + _SunWrap));
                half up = faceNormal.y * 0.5h + 0.5h;
                half3 hemisphere = lerp(_GroundAmbient.rgb, _SkyAmbient.rgb, up) * _AmbientStrength;

                half metallic = saturate(_Metallic);
                half smoothness = saturate(_Smoothness);
                half3 albedo = _BaseColor.rgb;
                half contact = 1 - _GroundShade * (1 - saturate(faceNormal.y))
                    * (1 - saturate(input.heightWS / 1.6));
                half3 color = albedo * (hemisphere + sun.color * diffuse * sun.shadowAttenuation)
                    * lerp(1, 0.55h, metallic) * contact;

                float distance = length(rel);
                half3 viewDir = (half3)(-rel / max(distance, 1e-4));
                half3 halfDir = normalize(viewDir + (half3)sun.direction);
                half ndv = saturate(dot(faceNormal, viewDir));
                half3 f0 = lerp(0.04h.xxx, albedo, metallic);
                half fresnel = pow(1 - ndv, 5);
                half specPower = lerp(18, 160, smoothness);
                half directSpecular = pow(saturate(dot(faceNormal, halfDir)), specPower)
                    * sun.shadowAttenuation * smoothness * _DirectSpecular;
                color += sun.color * directSpecular * (f0 + (1 - f0) * fresnel);

                half3 reflectionDir = reflect(-viewDir, faceNormal);
                half3 environment = GlossyEnvironmentReflection(reflectionDir, input.positionWS,
                    1 - smoothness, 1, GetNormalizedScreenSpaceUV(input.positionCS));
                color += environment * (f0 + (1 - f0) * fresnel) * _ReflectionStrength;

                half rim = pow(1 - ndv, 3) * _RimStrength * up;
                color += (unity_FogColor.rgb * 0.5h + albedo * 0.5h) * rim;
                color += _EmissionColor.rgb;
                color = lerp(unity_FogColor.rgb, color, FogVisibility(distance));
                return half4(color, 1);
            }
            ENDHLSL
        }

        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode"="ShadowCaster" }
            Cull [_Cull]
            ZWrite On
            ZTest LEqual
            ColorMask 0
            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex ShadowVert
            #pragma fragment ShadowFrag
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            float3 _LightDirection;
            float3 _LightPosition;
            struct ShadowAttributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };
            struct ShadowVaryings { float4 positionCS : SV_POSITION; };

            ShadowVaryings ShadowVert(ShadowAttributes input)
            {
                ShadowVaryings output;
                UNITY_SETUP_INSTANCE_ID(input);
                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                float3 normalWS = TransformObjectToWorldNormal(input.normalOS);
                #if defined(_CASTING_PUNCTUAL_LIGHT_SHADOW)
                    float3 lightDirectionWS = normalize(_LightPosition - positionWS);
                #else
                    float3 lightDirectionWS = _LightDirection;
                #endif
                output.positionCS = TransformWorldToHClip(
                    ApplyShadowBias(positionWS, normalWS, lightDirectionWS));
                #if UNITY_REVERSED_Z
                    output.positionCS.z = min(output.positionCS.z, UNITY_NEAR_CLIP_VALUE);
                #else
                    output.positionCS.z = max(output.positionCS.z, UNITY_NEAR_CLIP_VALUE);
                #endif
                return output;
            }

            half4 ShadowFrag(ShadowVaryings input) : SV_Target { return 0; }
            ENDHLSL
        }

        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode"="DepthOnly" }
            Cull [_Cull]
            ZWrite On
            ColorMask R
            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex DepthVert
            #pragma fragment DepthFrag
            #pragma multi_compile_instancing
            #pragma multi_compile _ STEREO_INSTANCING_ON STEREO_MULTIVIEW_ON
            struct DepthAttributes { float4 positionOS : POSITION; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct DepthVaryings { float4 positionCS : SV_POSITION; UNITY_VERTEX_OUTPUT_STEREO };
            DepthVaryings DepthVert(DepthAttributes input)
            {
                DepthVaryings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                return output;
            }
            half4 DepthFrag(DepthVaryings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                return 0;
            }
            ENDHLSL
        }
    }
    FallBack Off
}
