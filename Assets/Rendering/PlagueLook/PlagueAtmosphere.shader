Shader "Hidden/PlagueAtmosphere"
{
    // Screen-space atmosphere: sun in-scattering glow + warm/lavender horizon haze.
    // Distance fog is handled by RenderSettings fog (lavender, exp2), so this pass never needs scene depth.
    Properties
    {
        _HazeColor("Haze Color", Color) = (0.70,0.60,0.70,1)
        _SunColor("Sun Scatter Color", Color) = (1.0,0.72,0.45,1)
        _SunPower("Sun Glow Tightness", Float) = 6
        _SunStrength("Sun Glow Strength", Float) = 0.55
        _SunCorePower("Sun Core Tightness", Float) = 60
        _SunCoreStrength("Sun Core Strength", Float) = 0.9
        _HorizonHaze("Horizon Haze", Range(0,1)) = 0.22
        _HorizonWidth("Horizon Width", Float) = 3.5
        _Lift("Shadow Lift (aerial)", Range(0,0.3)) = 0.06
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" }
        ZWrite Off ZTest Always Cull Off Blend Off
        Pass
        {
            Name "PlagueAtmosphere"
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            float4 _HazeColor, _SunColor;
            float _SunPower, _SunStrength, _SunCorePower, _SunCoreStrength, _HorizonHaze, _HorizonWidth, _Lift;
            float4x4 _PA_InvVP; float4 _PA_CamPos;

            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                float2 uv = input.texcoord;
                half4 col = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv);

                float3 farPos = ComputeWorldSpacePosition(uv, UNITY_RAW_FAR_CLIP_VALUE, _PA_InvVP);
                float3 dir = normalize(farPos - _PA_CamPos.xyz);
                float3 sunDir = normalize(_MainLightPosition.xyz);
                float mu = saturate(dot(dir, sunDir));
                float3 sunCol = _SunColor.rgb * _MainLightColor.rgb;

                // aerial lift: crushed blacks get a bit of lavender air
                col.rgb = col.rgb + _HazeColor.rgb * _Lift * (1 - saturate(dot(col.rgb, 0.333)));
                // horizon haze band
                float horizon = exp(-abs(dir.y) * _HorizonWidth);
                float3 hazeCol = lerp(_HazeColor.rgb, sunCol, pow(mu, 3));
                col.rgb = lerp(col.rgb, hazeCol, horizon * _HorizonHaze);
                // sun in-scattering (wide glow + tight core), additive like light through dusty air
                col.rgb += sunCol * (pow(mu, _SunPower) * _SunStrength + pow(mu, _SunCorePower) * _SunCoreStrength);
                return col;
            }
            ENDHLSL
        }
    }
}