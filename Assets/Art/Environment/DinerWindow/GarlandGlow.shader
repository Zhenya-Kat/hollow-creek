Shader "HollowCreek/Garland Bulb Glow"
{
    Properties { _Tint ("Light color", Color) = (1,0.67,0.3,0.42) }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" "RenderPipeline"="UniversalPipeline" "DisableBatching"="True" }
        Pass
        {
            Tags { "LightMode"="SRPDefaultUnlit" }
            Blend One One
            ZWrite Off
            Cull Off
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            CBUFFER_START(UnityPerMaterial)
                half4 _Tint;
            CBUFFER_END
            struct Attributes { float4 positionOS : POSITION; float2 uv : TEXCOORD0; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct Varyings { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; };
            Varyings Vert(Attributes i)
            {
                UNITY_SETUP_INSTANCE_ID(i);
                float3 center=TransformObjectToWorld(float3(0,0,0));
                float2 size=float2(length(unity_ObjectToWorld._m00_m10_m20),length(unity_ObjectToWorld._m01_m11_m21));
                float3 right=UNITY_MATRIX_I_V._m00_m10_m20;
                float3 up=UNITY_MATRIX_I_V._m01_m11_m21;
                float3 position=center+right*i.positionOS.x*size.x+up*i.positionOS.y*size.y;
                Varyings o; o.positionCS=TransformWorldToHClip(position); o.uv=i.uv; return o;
            }
            half4 Frag(Varyings i) : SV_Target
            {
                float r=length((i.uv-0.5)*2);
                float glow=exp(-r*r*6.5)*(1-smoothstep(0.7,1,r));
                return half4(_Tint.rgb*glow*_Tint.a,0);
            }
            ENDHLSL
        }
    }
}
