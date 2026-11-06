Shader "Custom/VFX/HeatHazeParticle"
{
    Properties
    {
        _MainTex ("Mask", 2D) = "white" {}
        _Strength ("Distortion Strength", Range(0, 0.1)) = 0.02
        _NoiseScale ("Noise Scale", Float) = 5
        _NoiseSpeed ("Noise Speed", Float) = 2
    }

    SubShader
    {
        // Drawn before the sword and the flames so it only bends the opaque scene behind them.
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent-100" "RenderType"="Transparent" "IgnoreProjector"="True" "PreviewType"="Plane" }

        Pass
        {
            Name "HeatHaze"
            Tags { "LightMode"="UniversalForward" }
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareOpaqueTexture.hlsl"

            TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                half _Strength;
                float _NoiseScale;
                float _NoiseSpeed;
            CBUFFER_END

            float HH_Hash(float2 p)
            {
                p = frac(p * float2(123.34, 456.21));
                p += dot(p, p + 45.32);
                return frac(p.x * p.y);
            }

            float HH_Noise(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                float2 u = f * f * (3.0 - 2.0 * f);
                return lerp(lerp(HH_Hash(i), HH_Hash(i + float2(1, 0)), u.x),
                            lerp(HH_Hash(i + float2(0, 1)), HH_Hash(i + float2(1, 1)), u.x), u.y);
            }

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
                half mask = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv).r * i.color.a;
                float t = _Time.y * _NoiseSpeed;
                float2 n = float2(
                    HH_Noise(i.uv * _NoiseScale + float2(0.0, -t)),
                    HH_Noise(i.uv * _NoiseScale * 1.3 + float2(3.7, -t * 1.3))) - 0.5;
                float2 suv = GetNormalizedScreenSpaceUV(i.positionCS);
                half3 col = SampleSceneColor(suv + n * 2.0 * _Strength * mask);
                return half4(col, saturate(mask * 1.5));
            }
            ENDHLSL
        }
    }
    FallBack Off
}
