Shader "Custom/VFX/FireParticleAdditive"
{
    Properties
    {
        _MainTex ("Texture (grayscale)", 2D) = "white" {}
        _Intensity ("Intensity", Float) = 2.5
        _Contrast ("Contrast", Range(0.5, 3)) = 1.2
        _CoreBoost ("Hot Core Boost", Range(0, 2)) = 0.8
    }

    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" "PreviewType"="Plane" }

        Pass
        {
            Name "FireAdditive"
            Tags { "LightMode"="UniversalForward" }
            Blend SrcAlpha One
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                half _Intensity;
                half _Contrast;
                half _CoreBoost;
            CBUFFER_END

            struct Attributes { float4 positionOS : POSITION; half4 color : COLOR; float2 uv : TEXCOORD0; };
            struct Varyings { float4 positionCS : SV_POSITION; half4 color : COLOR; float2 uv : TEXCOORD0; };

            Varyings vert(Attributes v)
            {
                Varyings o;
                o.positionCS = TransformObjectToHClip(v.positionOS.xyz);
                o.color = v.color;
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                half mask = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv).r;
                mask = pow(saturate(mask), _Contrast);
                half a = saturate(mask * i.color.a);
                half3 hot = i.color.rgb + (1.0 - i.color.rgb) * mask * mask * 0.5;
                half3 col = hot * _Intensity * (1.0 + _CoreBoost * mask);
                return half4(col, a);
            }
            ENDHLSL
        }
    }
    FallBack Off
}
