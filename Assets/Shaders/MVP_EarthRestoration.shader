Shader "Earth Reshaping/MVP Earth Restoration"
{
    Properties
    {
        [MainTexture] _BaseMap("Earth Texture", 2D) = "white" {}
        [MainColor] _Tint("Tint", Color) = (1, 1, 1, 1)
        _Saturation("Saturation", Range(0, 1.25)) = 1
        _Brightness("Brightness", Range(0, 2)) = 1
        _ContaminationColor("Contamination Color", Color) = (0.5, 0.35, 0.2, 1)
        _Contamination("Contamination", Range(0, 1)) = 0
        [HDR] _AtmosphereColor("Atmosphere Color", Color) = (0.08, 0.5, 1.4, 1)
        _AtmosphereStrength("Atmosphere Strength", Range(0, 2)) = 0.5
        _FresnelPower("Atmosphere Falloff", Range(1, 8)) = 3.5
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
            "Queue" = "Geometry"
            "RenderPipeline" = "UniversalPipeline"
        }

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile_fog

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                half4 _Tint;
                half _Saturation;
                half _Brightness;
                half4 _ContaminationColor;
                half _Contamination;
                half4 _AtmosphereColor;
                half _AtmosphereStrength;
                half _FresnelPower;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
                half3 normalWS : TEXCOORD2;
                half fogFactor : TEXCOORD3;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                VertexPositionInputs positionInputs =
                    GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs normalInputs =
                    GetVertexNormalInputs(input.normalOS);

                output.positionCS = positionInputs.positionCS;
                output.positionWS = positionInputs.positionWS;
                output.normalWS = normalInputs.normalWS;
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                output.fogFactor = ComputeFogFactor(positionInputs.positionCS.z);
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                half3 normalWS = normalize(input.normalWS);
                half3 viewDirectionWS = SafeNormalize(
                    GetWorldSpaceViewDir(input.positionWS)
                );

                half3 textureColor =
                    SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv).rgb *
                    _Tint.rgb;
                half luminance = dot(
                    textureColor,
                    half3(0.2126h, 0.7152h, 0.0722h)
                );
                half3 restoredColor = lerp(
                    luminance.xxx,
                    textureColor,
                    _Saturation
                );
                half3 contaminatedColor =
                    restoredColor * _ContaminationColor.rgb;
                half3 surfaceColor = lerp(
                    restoredColor,
                    contaminatedColor,
                    _Contamination
                );

                float4 shadowCoord =
                    TransformWorldToShadowCoord(input.positionWS);
                Light mainLight = GetMainLight(shadowCoord);
                half normalToLight = saturate(
                    dot(normalWS, mainLight.direction)
                );

                half3 ambientLight = max(
                    SampleSH(normalWS),
                    half3(0.13h, 0.15h, 0.18h)
                );
                half shadow = lerp(
                    0.72h,
                    mainLight.shadowAttenuation,
                    0.55h
                );
                half3 directLight =
                    mainLight.color *
                    (0.22h + normalToLight * 0.78h) *
                    mainLight.distanceAttenuation *
                    shadow;

                half3 color = surfaceColor *
                    (ambientLight + directLight) *
                    _Brightness;

                half fresnel = pow(
                    1.0h - saturate(dot(normalWS, viewDirectionWS)),
                    _FresnelPower
                );
                color +=
                    _AtmosphereColor.rgb *
                    fresnel *
                    _AtmosphereStrength;

                color = MixFog(color, input.fogFactor);
                return half4(color, 1.0h);
            }
            ENDHLSL
        }

        UsePass "Universal Render Pipeline/Lit/ShadowCaster"
        UsePass "Universal Render Pipeline/Lit/DepthOnly"
    }

    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}
