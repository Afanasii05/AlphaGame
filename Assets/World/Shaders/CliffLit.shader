Shader "World/CliffLit"
{
    Properties
    {
        _RockA ("Rock A (main)", 2D) = "white" {}
        [Normal] _RockANormal ("Rock A Normal", 2D) = "bump" {}
        _RockARough ("Rock A Roughness", 2D) = "white" {}
        _RockB ("Rock B (secondary)", 2D) = "white" {}
        _RockC ("Damp Base Rock", 2D) = "white" {}
        _Moss ("Moss", 2D) = "white" {}
        _TintA ("Tint A", Color) = (1.2,1.15,1.07,1)
        _TintB ("Tint B", Color) = (0.98,0.96,0.94,1)
        _TintC ("Tint Damp", Color) = (0.62,0.64,0.6,1)
        _MossTint ("Moss Tint", Color) = (0.6,0.62,0.5,1)
        _TileA ("Tile A (1/m)", Float) = 0.22
        _TileB ("Tile B (1/m)", Float) = 0.26
        _TileC ("Tile Damp (1/m)", Float) = 0.3
        _TileM ("Tile Moss (1/m)", Float) = 0.5
        _MacroTile ("Macro Tile (1/m)", Float) = 0.05
        _BlendTile ("A/B Blend Tile (1/m)", Float) = 0.025
        _DesatB ("Desaturate B", Range(0,1)) = 0.55
        _BaseDamp ("Base Damp Height (0-1)", Range(0,1)) = 0.22
        _TopBleach ("Top Bleach", Range(0,1)) = 0.14
        _MossAmount ("Moss Amount", Range(0,1)) = 0.5
        _AOStrength ("Baked AO Strength", Range(0,1)) = 0.5
        _NormalStrength ("Normal Strength", Float) = 0.8
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" "Queue"="Geometry" }
        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            #pragma multi_compile _ _FORWARD_PLUS _CLUSTER_LIGHT_LOOP
            #pragma multi_compile_fragment _ _SCREEN_SPACE_OCCLUSION
            #pragma multi_compile_fog
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            TEXTURE2D(_RockA); SAMPLER(sampler_RockA);
            TEXTURE2D(_RockANormal); SAMPLER(sampler_RockANormal);
            TEXTURE2D(_RockARough); SAMPLER(sampler_RockARough);
            TEXTURE2D(_RockB); SAMPLER(sampler_RockB);
            TEXTURE2D(_RockC); SAMPLER(sampler_RockC);
            TEXTURE2D(_Moss); SAMPLER(sampler_Moss);

            CBUFFER_START(UnityPerMaterial)
            half4 _TintA, _TintB, _TintC, _MossTint;
            float _TileA, _TileB, _TileC, _TileM, _MacroTile, _BlendTile;
            half _DesatB, _BaseDamp, _TopBleach, _MossAmount, _AOStrength, _NormalStrength;
            CBUFFER_END

            struct Attributes { float4 positionOS : POSITION; float3 normalOS : NORMAL; half4 color : COLOR; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct Varyings { float4 positionCS : SV_POSITION; float3 positionWS : TEXCOORD0; float3 normalWS : TEXCOORD1; half4 color : TEXCOORD2; float fogCoord : TEXCOORD3; UNITY_VERTEX_INPUT_INSTANCE_ID };

            Varyings vert(Attributes v)
            {
                Varyings o = (Varyings)0; UNITY_SETUP_INSTANCE_ID(v); UNITY_TRANSFER_INSTANCE_ID(v, o);
                VertexPositionInputs p = GetVertexPositionInputs(v.positionOS.xyz);
                o.positionCS = p.positionCS; o.positionWS = p.positionWS; o.normalWS = TransformObjectToWorldNormal(v.normalOS);
                o.color = v.color; o.fogCoord = ComputeFogFactor(p.positionCS.z); return o;
            }

            float3 TriW(float3 n) { float3 w = pow(abs(n), 4); return w / (w.x + w.y + w.z); }
            #define TRI(tex, smp, pos, s, w) (SAMPLE_TEXTURE2D(tex, smp, (pos).zy * (s)) * (w).x + SAMPLE_TEXTURE2D(tex, smp, (pos).xz * (s)) * (w).y + SAMPLE_TEXTURE2D(tex, smp, (pos).xy * (s)) * (w).z)
            half Luma(half3 c) { return dot(c, half3(0.299, 0.587, 0.114)); }
            half G(half x) { return pow(saturate(x), 0.4545); }

            float3 TriNormal(float3 pos, float3 n, float3 w, float s)
            {
                half3 tx = UnpackNormalScale(SAMPLE_TEXTURE2D(_RockANormal, sampler_RockANormal, pos.zy * s), _NormalStrength);
                half3 ty = UnpackNormalScale(SAMPLE_TEXTURE2D(_RockANormal, sampler_RockANormal, pos.xz * s), _NormalStrength);
                half3 tz = UnpackNormalScale(SAMPLE_TEXTURE2D(_RockANormal, sampler_RockANormal, pos.xy * s), _NormalStrength);
                tx = half3(tx.xy + n.zy, abs(tx.z) * n.x);
                ty = half3(ty.xy + n.xz, abs(ty.z) * n.y);
                tz = half3(tz.xy + n.xy, abs(tz.z) * n.z);
                return normalize(tx.zyx * w.x + ty.xzy * w.y + tz.xyz * w.z);
            }

            half4 frag(Varyings i) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(i);
                float3 n = normalize(i.normalWS); float3 w = TriW(n); float3 P = i.positionWS;
                half H = i.color.r; half AO = i.color.g;
                half n1 = G(Luma(TRI(_RockC, sampler_RockC, P + 17.0, 0.035, w).rgb));
                half n2 = G(Luma(TRI(_RockB, sampler_RockB, P + 5.0, 0.071, w).rgb));
                half n3 = G(Luma(TRI(_Moss, sampler_Moss, P + 1.3, 0.13, w).rgb));
                half noiseL = saturate((n1 * 0.5 + n2 * 0.35 + n3 * 0.15 - 0.3) * 2.6);
                half3 a1 = TRI(_RockA, sampler_RockA, P, _TileA, w).rgb;
                half3 a2 = TRI(_RockA, sampler_RockA, P.zxy * 1.0 + 7.3, _TileA * 0.43, w).rgb;
                half3 a = lerp(a1, a2, smoothstep(0.35, 0.65, noiseL)) * _TintA.rgb;
                half3 b = TRI(_RockB, sampler_RockB, P + 4.1, _TileB, w).rgb; b = lerp(b, Luma(b).xxx, _DesatB) * _TintB.rgb;
                half blendN = n1;
                half3 rock = lerp(a, b, smoothstep(0.42, 0.62, blendN) * 0.75);
                rock *= lerp(0.84, 1.08, noiseL);
                half3 c = TRI(_RockC, sampler_RockC, P + 2.0, _TileC, w).rgb; c = lerp(c, Luma(c).xxx, 0.4) * _TintC.rgb;
                rock = lerp(rock, c, smoothstep(_BaseDamp, 0.0, H) * lerp(0.6, 1, n3));
                rock = lerp(rock, half3(0.84, 0.82, 0.78), smoothstep(0.55, 1.0, H) * _TopBleach);
                float3 nB = TriNormal(P, n, w, _TileA);
                half mossN = G(TRI(_Moss, sampler_Moss, P + 3.0, 0.12, w).g);
                half m = smoothstep(0.45, 0.8, nB.y * 0.5 + n.y * 0.5) * smoothstep(0.3, 0.5, mossN * 0.6 + noiseL * 0.4 + (_MossAmount - 0.5));
                half3 moss = TRI(_Moss, sampler_Moss, P, _TileM, w).rgb * _MossTint.rgb;
                half3 alb = lerp(rock, moss, m);
                half occ = lerp(1, lerp(0.5, 1, AO), _AOStrength);
                half rough = lerp(0.68, 0.95, TRI(_RockARough, sampler_RockARough, P, _TileA, w).r);
                rough = max(rough, m * 0.95);

                SurfaceData s = (SurfaceData)0;
                s.albedo = alb * lerp(1, occ, 0.6); s.metallic = 0; s.smoothness = 1 - rough; s.occlusion = occ; s.alpha = 1; s.normalTS = half3(0, 0, 1);
                InputData d = (InputData)0;
                d.positionWS = P; d.normalWS = normalize(lerp(nB, n, m * 0.5));
                d.viewDirectionWS = GetWorldSpaceNormalizeViewDir(P); d.shadowCoord = TransformWorldToShadowCoord(P);
                d.fogCoord = i.fogCoord; d.bakedGI = SampleSH(d.normalWS); d.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(i.positionCS); d.shadowMask = half4(1, 1, 1, 1);
                half4 col = UniversalFragmentPBR(d, s); col.rgb = MixFog(col.rgb, d.fogCoord); return col;
            }
            ENDHLSL
        }
        UsePass "Universal Render Pipeline/Lit/ShadowCaster"
        UsePass "Universal Render Pipeline/Lit/DepthOnly"
        UsePass "Universal Render Pipeline/Lit/DepthNormals"
    }
    Fallback "Universal Render Pipeline/Lit"
}
