// Polygon Racing flat-shaded lighting for Quest (URP, single-pass stereo).
// Every face gets one crisp tone: the facet normal comes from screen-space
// derivatives of the camera-relative position, so mesh normals don't matter
// and smooth-normal meshes still read as clean polygons.
Shader "Polygon Racing/Flat Lit"
{
    Properties
    {
        [MainColor] _BaseColor("Color", Color) = (1,1,1,1)
        _EmissionColor("Emission", Color) = (0,0,0,1)
        _SkyAmbient("Sky ambient", Color) = (0.60,0.74,0.86,1)
        _GroundAmbient("Ground ambient", Color) = (0.50,0.42,0.33,1)
        _AmbientStrength("Ambient strength", Range(0,2)) = 0.75
        _SunWrap("Sun wrap", Range(0,1)) = 0.12
        _RimStrength("Rim light", Range(0,1)) = 0.12
        _GroundShade("Ground contact shade", Range(0,1)) = 0.22
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
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 relativeWS : TEXCOORD0;   // world position minus camera position
                float heightWS : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            // Radial fog stays put when the head turns, unlike depth fog.
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
                // Point the facet normal back toward the viewer. This is independent of
                // platform derivative sign conventions and lights two-sided fronds sensibly.
                faceNormal = dot(faceNormal, rel) > 0 ? -faceNormal : faceNormal;

                Light sun = GetMainLight();
                half ndl = dot(faceNormal, sun.direction);
                half diffuse = saturate((ndl + _SunWrap) / (1 + _SunWrap));
                half up = faceNormal.y * 0.5h + 0.5h;
                half3 hemisphere = lerp(_GroundAmbient.rgb, _SkyAmbient.rgb, up) * _AmbientStrength;
                half3 lighting = hemisphere + sun.color * diffuse;

                // Darken the lower metre or so of vertical faces: cheap contact grounding.
                half contact = 1 - _GroundShade * (1 - saturate(faceNormal.y)) * (1 - saturate(input.heightWS / 1.6));
                half3 albedo = _BaseColor.rgb;
                half3 color = albedo * lighting * contact;

                float distance = length(rel);
                half3 viewDir = (half3)(-rel / max(distance, 1e-4));
                half rim = pow(1 - saturate(dot(faceNormal, viewDir)), 3) * _RimStrength * up;
                color += (unity_FogColor.rgb * 0.5h + albedo * 0.5h) * rim;
                color += _EmissionColor.rgb;

                color = lerp(unity_FogColor.rgb, color, FogVisibility(distance));
                return half4(color, 1);
            }
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
            struct Attributes { float4 positionOS : POSITION; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct Varyings { float4 positionCS : SV_POSITION; UNITY_VERTEX_OUTPUT_STEREO };
            Varyings DepthVert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                return output;
            }
            half4 DepthFrag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                return 0;
            }
            ENDHLSL
        }
    }
    FallBack Off
}
