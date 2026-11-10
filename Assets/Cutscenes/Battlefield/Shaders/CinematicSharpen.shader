Shader "Hidden/Cinematic/Sharpen"
{
    Properties
    {
        _Sharpness("Sharpness", Range(0, 1)) = 0.35
    }
    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" }
        ZWrite Off Cull Off ZTest Always

        Pass
        {
            Name "CinematicSharpen"
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            float _Sharpness;

            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                float2 uv = input.texcoord;
                float2 t = _BlitTexture_TexelSize.xy;

                half3 c = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv).rgb;
                half3 n = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv + float2(0,  t.y)).rgb;
                half3 s = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv + float2(0, -t.y)).rgb;
                half3 e = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv + float2( t.x, 0)).rgb;
                half3 w = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv + float2(-t.x, 0)).rgb;

                // Unsharp mask adaptiv: întărește detaliile fără halo (rezultatul e limitat la vecini)
                half3 blur = (n + s + e + w) * 0.25h;
                half3 mn = min(c, min(min(n, s), min(e, w)));
                half3 mx = max(c, max(max(n, s), max(e, w)));
                half3 sharp = c + (c - blur) * (_Sharpness * 2.0h);
                sharp = clamp(sharp, mn, mx);
                return half4(sharp, 1.0h);
            }
            ENDHLSL
        }
    }
}
