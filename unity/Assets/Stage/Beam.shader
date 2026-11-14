// A beam of stage light in the dusty air of the theatre: a cone that adds light, brightest near the lamp and along its axis, soft at the rim,
// with slow dust drifting through it. It writes no depth and casts nothing.
Shader "Stage/Beam"
{
    Properties { _Color("Color", Color) = (1, 0.95, 0.85, 1) _Gain("Gain", Float) = 0.2 _Rim("Rim softness", Float) = 2.2 _Fall("Falloff", Float) = 1.4 _Dust("Dust", Float) = 0.35 _T("Time", Float) = 0 }
    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent+10" "RenderPipeline" = "UniversalPipeline" }
        Pass
        {
            Name "Beam" Tags { "LightMode" = "UniversalForward" } Blend One One ZWrite Off Cull Off
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            CBUFFER_START(UnityPerMaterial)
            float4 _Color; float _Gain, _Rim, _Fall, _Dust, _T;
            CBUFFER_END
            struct A { float4 positionOS : POSITION; float3 normalOS : NORMAL; float2 uv : TEXCOORD0; };
            struct V { float4 positionCS : SV_POSITION; float3 n : TEXCOORD0; float3 v : TEXCOORD1; float2 uv : TEXCOORD2; float3 w : TEXCOORD3; };
            V vert(A i) { V o; float3 w = TransformObjectToWorld(i.positionOS.xyz); o.positionCS = TransformWorldToHClip(w); o.n = TransformObjectToWorldNormal(i.normalOS); o.v = GetWorldSpaceViewDir(w); o.uv = i.uv; o.w = w; return o; }
            float h3(float3 p) { p = frac(p * 0.3183099 + 0.1); p *= 17.0; return frac(p.x * p.y * p.z * (p.x + p.y + p.z)); }
            float noise(float3 x)
            {
                float3 i = floor(x), f = frac(x); f = f * f * (3 - 2 * f);
                return lerp(lerp(lerp(h3(i), h3(i + float3(1, 0, 0)), f.x), lerp(h3(i + float3(0, 1, 0)), h3(i + float3(1, 1, 0)), f.x), f.y), lerp(lerp(h3(i + float3(0, 0, 1)), h3(i + float3(1, 0, 1)), f.x), lerp(h3(i + float3(0, 1, 1)), h3(i + float3(1, 1, 1)), f.x), f.y), f.z);
            }
            half4 frag(V i) : SV_Target
            {
                float rim = pow(saturate(abs(dot(normalize(i.n), normalize(i.v)))), _Rim);
                float along = pow(saturate(1 - i.uv.y), _Fall) * smoothstep(0.0, 0.04, i.uv.y);
                float d = noise(i.w * 3.1 + float3(0.05, -0.11, 0.03) * _T) * 0.6 + noise(i.w * 9.0 + float3(-0.09, -0.2, 0.07) * _T) * 0.4;
                float a = rim * along * (1 - _Dust + _Dust * 2 * d) * _Gain;
                return half4(_Color.rgb * a, 0);
            }
            ENDHLSL
        }
    }
}
