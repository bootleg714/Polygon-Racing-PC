// Polygon Racing animated low-poly water (URP, single-pass stereo).
// The surface stays flat geometry; each pixel finds its triangle in a world-space
// lattice, animates that triangle's three corner heights and lights the resulting
// facet. Facets fade to calm water in the distance to avoid shimmer in the headset.
Shader "Polygon Racing/Faceted Water"
{
    Properties
    {
        [MainColor] _BaseColor("Water color", Color) = (0.025,0.43,0.63,1)
        _ReflectionZenith("Reflected sky (overhead)", Color) = (0.20,0.52,0.80,1)
        _SkyAmbient("Sky ambient", Color) = (0.60,0.74,0.86,1)
        _GroundAmbient("Ground ambient", Color) = (0.50,0.42,0.33,1)
        _AmbientStrength("Ambient strength", Range(0,2)) = 0.75
        _CellSize("Facet size (m)", Float) = 8
        _WaveHeight("Wave height (m)", Float) = 1.5
        _WaveSpeed("Wave speed", Float) = 1.2
        _FacetFadeStart("Facet fade start (m)", Float) = 90
        _FacetFadeEnd("Facet fade end (m)", Float) = 420
        _Fresnel("Sky reflection", Range(0,1)) = 0.35
        _Specular("Sun glint", Range(0,4)) = 2.0
        _SpecularPower("Glint sharpness", Range(8,400)) = 60
        _FacetTint("Facet tint variation", Range(0,0.5)) = 0.22
        [Enum(UnityEngine.Rendering.CullMode)] _Cull("Cull", Float) = 2
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" "Queue"="Geometry" }

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        CBUFFER_START(UnityPerMaterial)
            half4 _BaseColor;
            half4 _ReflectionZenith;
            half4 _SkyAmbient;
            half4 _GroundAmbient;
            half _AmbientStrength;
            float _CellSize;
            float _WaveHeight;
            float _WaveSpeed;
            float _FacetFadeStart;
            float _FacetFadeEnd;
            half _Fresnel;
            half _Specular;
            half _SpecularPower;
            half _FacetTint;
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
                float3 relativeWS : TEXCOORD0;
                float3 positionWS : TEXCOORD1;   // passed through: no per-eye camera lookup in the fragment
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

            float Hash(float2 i) { return frac(sin(dot(i, float2(127.1, 311.7))) * 43758.5453); }

            // Height of one lattice corner (in skewed lattice coordinates).
            float3 Corner(float2 lattice, float time)
            {
                const float G2 = 0.21132487;               // (3 - sqrt(3)) / 6
                float2 xz = (lattice - (lattice.x + lattice.y) * G2) * _CellSize;
                float phase = Hash(lattice) * 6.2831853;
                float h = _WaveHeight * (0.6 * sin(xz.x * 0.07 + xz.y * 0.05 + time * 1.3 + phase)
                                       + 0.4 * sin(xz.x * -0.04 + xz.y * 0.11 - time * 1.7 + phase * 1.7));
                return float3(xz.x, h, xz.y);
            }

            Varyings Vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                output.positionCS = TransformWorldToHClip(positionWS);
                output.relativeWS = positionWS - _WorldSpaceCameraPos;
                output.positionWS = positionWS;
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                float3 rel = input.relativeWS;
                float distance = length(rel);
                float3 positionWS = input.positionWS;
                float time = _Time.y * _WaveSpeed;

                // Facets fade to calm water in the distance; past the fade the lattice is skipped.
                float fade = saturate((distance - _FacetFadeStart) / max(_FacetFadeEnd - _FacetFadeStart, 1));
                float3 n = float3(0, 1, 0);
                half tint = 1;
                [branch] if (fade < 1)
                {
                    // Triangular (simplex) lattice lookup.
                    const float F2 = 0.36602540;           // (sqrt(3) - 1) / 2
                    const float G2 = 0.21132487;
                    float2 uv = positionWS.xz / _CellSize;
                    float2 cell = floor(uv + (uv.x + uv.y) * F2);
                    float2 local = uv - (cell - (cell.x + cell.y) * G2);
                    float2 step1 = local.x > local.y ? float2(1, 0) : float2(0, 1);
                    float3 p0 = Corner(cell, time);
                    float3 p1 = Corner(cell + step1, time);
                    float3 p2 = Corner(cell + 1, time);
                    float3 facet = normalize(cross(p2 - p0, p1 - p0));
                    facet = facet.y < 0 ? -facet : facet;
                    n = normalize(lerp(facet, float3(0, 1, 0), fade));
                    tint = 1 + (Hash(cell + step1 * 0.5) - 0.5) * _FacetTint * (1 - fade);
                }

                Light sun = GetMainLight();
                half3 viewDir = (half3)(-rel / max(distance, 1e-4));
                half ndl = saturate(dot(n, sun.direction));
                half3 hemisphere = (_GroundAmbient.rgb * 0.3h + _SkyAmbient.rgb * 0.7h) * _AmbientStrength;
                half3 body = _BaseColor.rgb * tint * (hemisphere + sun.color * (0.35h + 0.65h * ndl));

                half nv = saturate(dot(n, viewDir));
                half fresnel = 0.02h + _Fresnel * pow(1 - nv, 5);
                half3 r = reflect(-viewDir, (half3)n);
                half skyBlend = saturate(r.y * 2.5h);
                half3 reflection = lerp(unity_FogColor.rgb, _ReflectionZenith.rgb, skyBlend);
                half glint = pow(saturate(dot(r, sun.direction)), _SpecularPower) * _Specular * (1 - fade);

                half3 color = lerp(body, reflection, fresnel) + sun.color * glint;
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
