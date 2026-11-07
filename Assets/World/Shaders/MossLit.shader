Shader "World/MossLit"
{
    Properties
    {
        _BaseMap ("Albedo", 2D) = "white" {}
        _BaseColor ("Tint", Color) = (1,1,1,1)
        [Normal] _BumpMap ("Normal", 2D) = "bump" {}
        _BumpScale ("Normal Strength", Float) = 1
        _OcclusionMap ("Occlusion", 2D) = "white" {}
        _OcclusionStrength ("Occlusion Strength", Range(0,1)) = 1
        _Smoothness ("Smoothness", Range(0,1)) = 0.12
        _Metallic ("Metallic", Range(0,1)) = 0
        _MossMap ("Moss Albedo", 2D) = "white" {}
        [Normal] _MossNormal ("Moss Normal", 2D) = "bump" {}
        _MossColor ("Moss Tint", Color) = (0.55,0.75,0.42,1)
        _MossAmount ("Moss Amount", Range(0,1)) = 0.45
        _MossSharpness ("Moss Edge Sharpness", Range(1,16)) = 6
        _MossScale ("Moss Tiling (m)", Float) = 1.6
        _MossNoiseScale ("Moss Breakup (m)", Float) = 3.5
        _MossCavity ("Moss In Cavities", Range(0,1)) = 0.5
        _MossHeightFade ("Moss Height Fade (m, 0=off)", Float) = 0
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

            TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);
            TEXTURE2D(_BumpMap); SAMPLER(sampler_BumpMap);
            TEXTURE2D(_OcclusionMap); SAMPLER(sampler_OcclusionMap);
            TEXTURE2D(_MossMap); SAMPLER(sampler_MossMap);
            TEXTURE2D(_MossNormal); SAMPLER(sampler_MossNormal);

            CBUFFER_START(UnityPerMaterial)
            float4 _BaseMap_ST; half4 _BaseColor; half _BumpScale; half _OcclusionStrength; half _Smoothness; half _Metallic;
            half4 _MossColor; half _MossAmount; half _MossSharpness; float _MossScale; float _MossNoiseScale; half _MossCavity; float _MossHeightFade;
            CBUFFER_END

            struct Attributes { float4 positionOS : POSITION; float3 normalOS : NORMAL; float4 tangentOS : TANGENT; float2 uv : TEXCOORD0; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct Varyings
            {
                float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; float3 positionWS : TEXCOORD1;
                float3 normalWS : TEXCOORD2; float4 tangentWS : TEXCOORD3; float fogCoord : TEXCOORD4;
                UNITY_VERTEX_INPUT_INSTANCE_ID UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings vert(Attributes v)
            {
                Varyings o = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(v); UNITY_TRANSFER_INSTANCE_ID(v, o); UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                VertexPositionInputs p = GetVertexPositionInputs(v.positionOS.xyz);
                VertexNormalInputs n = GetVertexNormalInputs(v.normalOS, v.tangentOS);
                o.positionCS = p.positionCS; o.positionWS = p.positionWS; o.uv = TRANSFORM_TEX(v.uv, _BaseMap);
                o.normalWS = n.normalWS; o.tangentWS = float4(n.tangentWS, v.tangentOS.w * GetOddNegativeScale());
                o.fogCoord = ComputeFogFactor(p.positionCS.z);
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(i);
                float3 nGeo = normalize(i.normalWS);
                float3 bit = i.tangentWS.w * cross(nGeo, i.tangentWS.xyz);
                half3x3 tbn = half3x3(i.tangentWS.xyz, bit, nGeo);
                half4 albedo = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, i.uv) * _BaseColor;
                half3 nTS = UnpackNormalScale(SAMPLE_TEXTURE2D(_BumpMap, sampler_BumpMap, i.uv), _BumpScale);
                half occ = lerp(1, SAMPLE_TEXTURE2D(_OcclusionMap, sampler_OcclusionMap, i.uv).g, _OcclusionStrength);
                float3 nBase = normalize(mul(nTS, tbn));

                float2 mUV = i.positionWS.xz / _MossScale;
                half4 moss = SAMPLE_TEXTURE2D(_MossMap, sampler_MossMap, mUV) * _MossColor;
                half3 mN = UnpackNormalScale(SAMPLE_TEXTURE2D(_MossNormal, sampler_MossNormal, mUV), 1);
                float3 nMoss = normalize(float3(mN.x, mN.z, mN.y));

                float n1 = SAMPLE_TEXTURE2D(_MossMap, sampler_MossMap, i.positionWS.xz / _MossNoiseScale + 0.37).g;
                float n2 = SAMPLE_TEXTURE2D(_MossMap, sampler_MossMap, i.positionWS.zx / (_MossNoiseScale * 2.7) + 0.71).r;
                float breakup = (n1 * 0.6 + n2 * 0.4) - 0.35;
                float up = saturate(nBase.y * 0.65 + nGeo.y * 0.35);
                float cav = (1 - occ) * _MossCavity;
                float m = (up - (1 - _MossAmount)) * _MossSharpness + breakup * 2.2 + cav * 2;
                if (_MossHeightFade > 0) m -= saturate((i.positionWS.y - _MossHeightFade) * 0.15) * 2;
                m = saturate(m) * step(0.05, _MossAmount);
                m *= saturate(nGeo.y * 4 + 0.6);

                SurfaceData s = (SurfaceData)0;
                s.albedo = lerp(albedo.rgb, moss.rgb, m);
                s.metallic = lerp(_Metallic, 0, m);
                s.smoothness = lerp(_Smoothness, 0.05, m);
                s.occlusion = lerp(occ, 1, m * 0.5);
                s.alpha = 1;
                s.normalTS = half3(0, 0, 1);

                InputData d = (InputData)0;
                d.positionWS = i.positionWS;
                d.normalWS = normalize(lerp(nBase, normalize(nMoss * 0.5 + nGeo), m));
                d.viewDirectionWS = GetWorldSpaceNormalizeViewDir(i.positionWS);
                d.shadowCoord = TransformWorldToShadowCoord(i.positionWS);
                d.fogCoord = i.fogCoord;
                d.bakedGI = SampleSH(d.normalWS);
                d.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(i.positionCS);
                d.shadowMask = half4(1, 1, 1, 1);
                half4 c = UniversalFragmentPBR(d, s);
                c.rgb = MixFog(c.rgb, d.fogCoord);
                return c;
            }
            ENDHLSL
        }
        UsePass "Universal Render Pipeline/Lit/ShadowCaster"
        UsePass "Universal Render Pipeline/Lit/DepthOnly"
        UsePass "Universal Render Pipeline/Lit/DepthNormals"
    }
    Fallback "Universal Render Pipeline/Lit"
}
