Shader "RogueKit/Sky Gradient Dusk"
{
    // Hand-tuned dusk sky: warm horizon around the sun, violet band, deep indigo zenith.
    Properties
    {
        _TopColor ("Zenith", Color) = (0.06, 0.07, 0.17, 1)
        _MidColor ("Upper Horizon (away from sun)", Color) = (0.30, 0.20, 0.34, 1)
        _HorizonColor ("Horizon (sun side)", Color) = (1.0, 0.52, 0.26, 1)
        _HorizonAway ("Horizon (opposite side)", Color) = (0.40, 0.28, 0.40, 1)
        _GroundColor ("Below Horizon", Color) = (0.10, 0.085, 0.095, 1)
        _SunColor ("Sun", Color) = (1.0, 0.62, 0.32, 1)
        _SunDir ("Direction to Sun (world)", Vector) = (-0.794, 0.122, -0.596, 0)
        _SunSize ("Sun Disc Size", Range(0.0005, 0.05)) = 0.006
        _SunGlow ("Sun Glow Tightness", Range(1, 64)) = 7
        _SunGlowStrength ("Sun Glow Strength", Range(0, 4)) = 1.2
        _HorizonHeight ("Horizon Band Height", Range(0.02, 1)) = 0.22
        _Exposure ("Exposure", Range(0, 4)) = 1
    }
    SubShader
    {
        Tags { "Queue"="Background" "RenderType"="Background" "PreviewType"="Skybox" }
        Cull Off ZWrite Off
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            float4 _TopColor, _MidColor, _HorizonColor, _HorizonAway, _GroundColor, _SunColor, _SunDir;
            float _SunSize, _SunGlow, _SunGlowStrength, _HorizonHeight, _Exposure;
            struct v2f { float4 pos : SV_POSITION; float3 dir : TEXCOORD0; };
            v2f vert (appdata_base v) { v2f o; o.pos = UnityObjectToClipPos(v.vertex); o.dir = v.vertex.xyz; return o; }
            float4 frag (v2f i) : SV_Target
            {
                float3 v = normalize(i.dir);
                float3 s = normalize(_SunDir.xyz);
                float h = v.y;
                float sd = dot(v, s);
                float sunSide = pow(saturate(dot(normalize(float3(v.x, 0, v.z)), normalize(float3(s.x, 0, s.z))) * 0.5 + 0.5), 2.2);
                float3 horizon = lerp(_HorizonAway.rgb, _HorizonColor.rgb, sunSide);
                float3 c = lerp(horizon, _MidColor.rgb, saturate(h / _HorizonHeight));
                c = lerp(c, _TopColor.rgb, saturate((h - _HorizonHeight * 0.6) * 1.8));
                c = lerp(c, _GroundColor.rgb, saturate(-h * 8.0));
                float glow = pow(saturate(sd), _SunGlow) * _SunGlowStrength + pow(saturate(sd), 400.0) * 1.5;
                float disc = smoothstep(1.0 - _SunSize, 1.0 - _SunSize * 0.5, sd) * step(-0.02, h);
                c += _SunColor.rgb * (glow * saturate(h * 6 + 0.6) + disc * 6.0);
                return float4(c * _Exposure, 1);
            }
            ENDCG
        }
    }
    Fallback Off
}
