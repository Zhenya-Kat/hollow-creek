Shader "HollowCreek/Evening Window Backdrop"
{
    Properties
    {
        _MainTex ("Panorama", 2D) = "black" {}
        _PanoramaRect ("Panorama crop (offset XY, size ZW)", Vector) = (0.01, 0.44, 0.34, 0.30)
        _Tint ("Evening tint", Color) = (0.22, 0.31, 0.46, 1)
        _Blur ("Distant softness", Range(0, 0.01)) = 0.0012
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" "RenderPipeline"="UniversalPipeline" }
        Pass
        {
            Tags { "LightMode"="SRPDefaultUnlit" }
            Cull Off
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);
            CBUFFER_START(UnityPerMaterial)
                float4 _PanoramaRect;
                half4 _Tint;
                float _Blur;
            CBUFFER_END
            struct Attributes { float4 positionOS : POSITION; float2 uv : TEXCOORD0; };
            struct Varyings { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; };
            Varyings Vert(Attributes i)
            {
                Varyings o; o.positionCS=TransformObjectToHClip(i.positionOS.xyz); o.uv=i.uv; return o;
            }
            half4 Frag(Varyings i) : SV_Target
            {
                float2 uv=_PanoramaRect.xy+i.uv*_PanoramaRect.zw;
                half3 color=SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,uv).rgb*0.28;
                color+=SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,uv+float2(_Blur,0)).rgb*0.18;
                color+=SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,uv-float2(_Blur,0)).rgb*0.18;
                color+=SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,uv+float2(0,_Blur)).rgb*0.18;
                color+=SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,uv-float2(0,_Blur)).rgb*0.18;
                return half4(color*_Tint.rgb,1);
            }
            ENDHLSL
        }
    }
}
