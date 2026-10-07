Shader "HollowCreek/Evening Street Surround"
{
    Properties
    {
        _MainTex ("Panorama", 2D) = "black" {}
        _Tint ("Evening tint", Color) = (0.16,0.22,0.34,1)
        _Rotation ("Panorama rotation", Float) = 0.15
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Background" "RenderPipeline"="UniversalPipeline" "DisableBatching"="True" }
        Pass
        {
            Tags { "LightMode"="SRPDefaultUnlit" }
            Cull Front
            ZWrite Off
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);
            CBUFFER_START(UnityPerMaterial)
                half4 _Tint;
                float _Rotation;
            CBUFFER_END
            struct Attributes { float4 positionOS : POSITION; };
            struct Varyings { float4 positionCS : SV_POSITION; float3 direction : TEXCOORD0; };
            Varyings Vert(Attributes i)
            {
                Varyings o;
                o.positionCS = TransformObjectToHClip(i.positionOS.xyz);
                o.direction = i.positionOS.xyz;
                return o;
            }
            half4 Frag(Varyings i) : SV_Target
            {
                float3 d = normalize(i.direction);
                float2 uv = float2(frac(atan2(d.z,d.x)/6.2831853 + 0.5 + _Rotation), asin(clamp(d.y,-1,1))/3.14159265 + 0.5);
                half3 color = SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,uv).rgb;
                color = lerp(color,half3(0.18,0.25,0.35),0.14);
                return half4(color*_Tint.rgb,1);
            }
            ENDHLSL
        }
    }
}
