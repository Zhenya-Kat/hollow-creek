Shader "HollowCreek/Distant Soft Light"
{
    Properties { _Tint ("Light color", Color) = (1,0.5,0.18,1) }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" "RenderPipeline"="UniversalPipeline" }
        Pass
        {
            Tags { "LightMode"="SRPDefaultUnlit" }
            Blend One One
            ZWrite Off
            Cull Off
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            CBUFFER_START(UnityPerMaterial)
                half4 _Tint;
            CBUFFER_END
            struct Attributes { float4 positionOS : POSITION; float2 uv : TEXCOORD0; };
            struct Varyings { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; };
            Varyings Vert(Attributes i)
            {
                Varyings o; o.positionCS=TransformObjectToHClip(i.positionOS.xyz); o.uv=i.uv; return o;
            }
            half4 Frag(Varyings i) : SV_Target
            {
                float r=length((i.uv-0.5)*2);
                float glow=exp(-r*r*6.5)*(1-smoothstep(0.7,1,r));
                glow+=0.22*(1-smoothstep(0.18,0.48,r));
                return half4(_Tint.rgb*glow*_Tint.a,0);
            }
            ENDHLSL
        }
    }
}
