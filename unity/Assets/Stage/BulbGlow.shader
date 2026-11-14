// The glass of a burning bulb: white-hot where you look straight through it at the filament, orange at the rim. Opaque and in the depth
// texture, so that the depth of field treats it as the near object it is.
Shader "Stage/BulbGlow"
{
    Properties { _Core("Core", Color) = (1, 0.93, 0.78, 1) _Rim("Rim", Color) = (1, 0.42, 0.10, 1) _Power("Power", Float) = 2.2 _Gain("Gain", Float) = 1 _CoreGain("Core gain", Float) = 3 _RimGain("Rim gain", Float) = 0.8 }
    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" "Queue" = "Geometry" }
        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        CBUFFER_START(UnityPerMaterial)
        float4 _Core, _Rim; float _Power, _Gain, _CoreGain, _RimGain;
        CBUFFER_END
        struct A { float4 positionOS : POSITION; float3 normalOS : NORMAL; };
        struct V { float4 positionCS : SV_POSITION; float3 n : TEXCOORD0; float3 v : TEXCOORD1; };
        V vert(A i) { V o; float3 w = TransformObjectToWorld(i.positionOS.xyz); o.positionCS = TransformWorldToHClip(w); o.n = TransformObjectToWorldNormal(i.normalOS); o.v = GetWorldSpaceViewDir(w); return o; }
        ENDHLSL
        Pass
        {
            Name "ForwardLit" Tags { "LightMode" = "UniversalForward" }
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            half4 frag(V i) : SV_Target { float k = pow(saturate(abs(dot(normalize(i.n), normalize(i.v)))), _Power); return half4((_Rim.rgb * _RimGain * (1 - k) + _Core.rgb * _CoreGain * k) * _Gain, 1); }
            ENDHLSL
        }
        Pass
        {
            Name "DepthOnly" Tags { "LightMode" = "DepthOnly" } ZWrite On ColorMask R
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            half4 frag(V i) : SV_Target { return i.positionCS.z; }
            ENDHLSL
        }
        Pass
        {
            Name "DepthNormals" Tags { "LightMode" = "DepthNormals" } ZWrite On
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            half4 frag(V i) : SV_Target { return half4(normalize(i.n), 0); }
            ENDHLSL
        }
    }
}
