Shader "Custom/VFX/MirageSword"
{
    Properties
    {
        [MainTexture] _BaseMap ("Base Map", 2D) = "white" {}
        [MainColor] _BaseColor ("Base Color", Color) = (1,1,1,1)
        [Normal][NoScaleOffset] _BumpMap ("Normal Map", 2D) = "bump" {}
        _BumpScale ("Normal Scale", Float) = 1
        _Metallic ("Metallic", Range(0,1)) = 0.9
        _Smoothness ("Smoothness", Range(0,1)) = 0.75

        [Header(Mirage)]
        _Opacity ("Opacity", Range(0,1)) = 0.7
        _RefractionMix ("Refraction Mix", Range(0,1)) = 0.6
        _Distortion ("Refraction Distortion", Range(0,0.1)) = 0.02
        _ShimmerScale ("Shimmer Scale", Float) = 8
        _ShimmerSpeed ("Shimmer Speed", Float) = 1.5
        _Wobble ("Vertex Wobble", Range(0,0.02)) = 0.002
        _Flicker ("Opacity Flicker", Range(0,1)) = 0.15

        [Header(Fire)]
        [HDR] _RimColor ("Rim Color", Color) = (4,1.2,0.2,1)
        _RimPower ("Rim Power", Range(0.5,8)) = 2.5
        [HDR] _HeatColor ("Heat Veins Color", Color) = (3,0.8,0.1,1)
        _HeatScale ("Heat Veins Scale", Float) = 6
        _HeatSpeed ("Heat Veins Speed", Float) = 0.8
        _HeatAmount ("Heat Veins Amount", Range(0,1)) = 0.5
        _HeatStart ("Heat Start Height (object space)", Float) = 0.5
        [NoScaleOffset] _EmissionMap ("Emission Map", 2D) = "white" {}
        [HDR] _EmissionColor ("Emission Color", Color) = (0,0,0,1)

        [Header(Spawn Dissolve)]
        _Dissolve ("Dissolve (0 visible, 1 hidden)", Range(0,1)) = 0
        _DissolveHeight ("Dissolve Height (object space)", Float) = 1.2
        _DissolveEdge ("Dissolve Edge Width", Range(0.005,0.3)) = 0.08
        [HDR] _DissolveEdgeColor ("Dissolve Edge Color", Color) = (8,2.5,0.4,1)

        [Enum(UnityEngine.Rendering.CullMode)] _Cull ("Cull", Float) = 2
    }

    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Transparent" "Queue"="Transparent-1" "IgnoreProjector"="True" }

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

        CBUFFER_START(UnityPerMaterial)
            float4 _BaseMap_ST;
            half4 _BaseColor;
            half _BumpScale;
            half _Metallic;
            half _Smoothness;
            half _Opacity;
            half _RefractionMix;
            half _Distortion;
            float _ShimmerScale;
            float _ShimmerSpeed;
            float _Wobble;
            half _Flicker;
            half4 _RimColor;
            half _RimPower;
            half4 _HeatColor;
            float _HeatScale;
            float _HeatSpeed;
            half _HeatAmount;
            float _HeatStart;
            half4 _EmissionColor;
            half _Dissolve;
            float _DissolveHeight;
            half _DissolveEdge;
            half4 _DissolveEdgeColor;
            half _Cull;
        CBUFFER_END

        float MS_Hash21(float2 p)
        {
            p = frac(p * float2(123.34, 456.21));
            p += dot(p, p + 45.32);
            return frac(p.x * p.y);
        }

        float MS_Noise(float2 p)
        {
            float2 i = floor(p);
            float2 f = frac(p);
            float a = MS_Hash21(i);
            float b = MS_Hash21(i + float2(1, 0));
            float c = MS_Hash21(i + float2(0, 1));
            float d = MS_Hash21(i + float2(1, 1));
            float2 u = f * f * (3.0 - 2.0 * f);
            return lerp(lerp(a, b, u.x), lerp(c, d, u.x), u.y);
        }

        float MS_Fbm(float2 p)
        {
            float v = 0.0;
            float a = 0.5;
            [unroll] for (int k = 0; k < 3; k++)
            {
                v += a * MS_Noise(p);
                p = p * 2.03 + 17.1;
                a *= 0.5;
            }
            return v / 0.875;
        }

        float3 MS_Wobble(float3 posOS, float3 nOS)
        {
            float t = _Time.y * _ShimmerSpeed;
            float w = sin(posOS.y * 25.0 + t * 6.0) * 0.5 + sin(posOS.y * 11.0 - t * 4.3 + posOS.x * 20.0) * 0.5;
            float mask = saturate(posOS.y / max(_DissolveHeight, 0.001));
            return posOS + nOS * (w * _Wobble * mask);
        }

        // > 0 visible, < 0 dissolved. With _Dissolve = 1 everything is hidden, tip appears first when going back to 0.
        float MS_DissolveDiff(float3 posOS)
        {
            float h = saturate(posOS.y / max(_DissolveHeight, 0.001));
            float n = MS_Fbm(posOS.xy * 14.0 + posOS.z * 9.0);
            float d = h * 0.75 + n * 0.25;
            float t = lerp(-_DissolveEdge - 0.01, 1.01, _Dissolve);
            return d - t;
        }
        ENDHLSL

        Pass
        {
            Name "MirageDepthPrepass"
            Tags { "LightMode"="SRPDefaultUnlit" }
            ZWrite On
            ColorMask 0
            Cull [_Cull]

            HLSLPROGRAM
            #pragma vertex DepthVert
            #pragma fragment DepthFrag

            struct DAttributes { float4 positionOS : POSITION; float3 normalOS : NORMAL; };
            struct DVaryings { float4 positionCS : SV_POSITION; float3 positionOS : TEXCOORD0; };

            DVaryings DepthVert(DAttributes v)
            {
                DVaryings o;
                o.positionCS = TransformObjectToHClip(MS_Wobble(v.positionOS.xyz, v.normalOS));
                o.positionOS = v.positionOS.xyz;
                return o;
            }

            half4 DepthFrag(DVaryings i) : SV_Target
            {
                clip(MS_DissolveDiff(i.positionOS));
                return 0;
            }
            ENDHLSL
        }

        Pass
        {
            Name "MirageForward"
            Tags { "LightMode"="UniversalForward" }
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            ZTest LEqual
            Cull [_Cull]

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareOpaqueTexture.hlsl"

            TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);
            TEXTURE2D(_BumpMap); SAMPLER(sampler_BumpMap);
            TEXTURE2D(_EmissionMap); SAMPLER(sampler_EmissionMap);

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float4 tangentOS : TANGENT;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
                float3 normalWS : TEXCOORD2;
                float4 tangentWS : TEXCOORD3;
                float3 positionOS : TEXCOORD4;
                float fogCoord : TEXCOORD5;
            };

            Varyings vert(Attributes v)
            {
                Varyings o;
                float3 pos = MS_Wobble(v.positionOS.xyz, v.normalOS);
                VertexPositionInputs vpi = GetVertexPositionInputs(pos);
                VertexNormalInputs vni = GetVertexNormalInputs(v.normalOS, v.tangentOS);
                o.positionCS = vpi.positionCS;
                o.positionWS = vpi.positionWS;
                o.normalWS = vni.normalWS;
                o.tangentWS = float4(vni.tangentWS, v.tangentOS.w * GetOddNegativeScale());
                o.uv = TRANSFORM_TEX(v.uv, _BaseMap);
                o.positionOS = v.positionOS.xyz;
                o.fogCoord = ComputeFogFactor(vpi.positionCS.z);
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                float diff = MS_DissolveDiff(i.positionOS);
                clip(diff);
                half edge = 1.0 - saturate(diff / max(_DissolveEdge, 0.001));

                half4 albedo = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, i.uv) * _BaseColor;
                half3 nTS = UnpackNormalScale(SAMPLE_TEXTURE2D(_BumpMap, sampler_BumpMap, i.uv), _BumpScale);
                float3 nWS = normalize(i.normalWS);
                float3 tWS = normalize(i.tangentWS.xyz);
                float3 bWS = i.tangentWS.w * cross(nWS, tWS);
                half3 N = normalize(TransformTangentToWorld(nTS, half3x3(tWS, bWS, nWS)));
                half3 V = GetWorldSpaceNormalizeViewDir(i.positionWS);

                // Simple metal lighting: main light + ambient + reflection probe
                Light mainLight = GetMainLight();
                half NdotL = saturate(dot(N, mainLight.direction));
                half3 H = normalize(mainLight.direction + V);
                half specPow = exp2(10.0 * _Smoothness + 1.0);
                half spec = pow(saturate(dot(N, H)), specPow) * _Smoothness;
                half3 diffuseCol = albedo.rgb * (1.0 - _Metallic * 0.7);
                half3 specCol = lerp(half3(0.04, 0.04, 0.04), albedo.rgb, _Metallic);
                half3 R = reflect(-V, N);
                half3 env = GlossyEnvironmentReflection(R, 1.0 - _Smoothness, 1.0);
                half3 ambient = SampleSH(N);
                half3 lit = diffuseCol * (mainLight.color * NdotL + ambient) + specCol * (mainLight.color * spec + env);

                // Fire: rim + flowing heat veins + emission map + dissolve edge
                half NdotV = saturate(dot(N, V));
                half fres = pow(1.0 - NdotV, _RimPower);
                float t = _Time.y;
                float heatN = MS_Fbm(float2((i.positionOS.x + i.positionOS.z) * _HeatScale * 3.0, i.positionOS.y * _HeatScale - t * _HeatSpeed));
                half heatMask = saturate((i.positionOS.y - _HeatStart) * 8.0);
                half veins = pow(saturate(1.0 - abs(heatN - 0.5) * 6.0), 3.0) * _HeatAmount * heatMask; // thin flowing heat cracks
                half3 emissionTex = SAMPLE_TEXTURE2D(_EmissionMap, sampler_EmissionMap, i.uv).rgb * _EmissionColor.rgb;
                half3 emission = _RimColor.rgb * fres + _HeatColor.rgb * veins + emissionTex + _DissolveEdgeColor.rgb * edge;
                half3 col = lit + emission;

                // Mirage: shimmering refraction of what is behind the blade
                float2 suv = GetNormalizedScreenSpaceUV(i.positionCS);
                float st = t * _ShimmerSpeed;
                float2 shimmer = float2(
                    MS_Noise(i.positionOS.xy * _ShimmerScale * float2(1.0, 3.0) + float2(0.0, -st * 2.0)),
                    MS_Noise(i.positionOS.yx * _ShimmerScale * float2(3.0, 1.0) + float2(st * 1.7, 5.3))) - 0.5;
                half3 nVS = TransformWorldToViewDir(N);
                float2 offs = (shimmer * 2.0 + nVS.xy * 0.25) * _Distortion;
                half3 refr = SampleSceneColor(suv + offs);

                half flick = 1.0 - _Flicker * MS_Noise(float2(t * 7.0, i.positionOS.y * 3.0));
                half op = saturate(_Opacity * flick + fres * 0.4 + veins * 0.5);
                op = max(op, edge);
                half m = _RefractionMix;
                half a = op + (1.0 - op) * m;
                half3 outCol = (col * op + refr * (1.0 - op) * m) / max(a, 0.0001);
                outCol = MixFog(outCol, i.fogCoord);
                return half4(outCol, a);
            }
            ENDHLSL
        }
    }
    FallBack Off
}
