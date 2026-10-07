Shader "HollowCreek/Occupied House Window"
{
    Properties { _BaseColor("Room light", Color) = (1,.75,.4,1) }
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" }
        Cull Off
        Pass
        {
            Tags { "LightMode"="UniversalForward" }
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_fog
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            CBUFFER_START(UnityPerMaterial)
            half4 _BaseColor;
            CBUFFER_END
            struct Attributes { float4 positionOS:POSITION; float2 uv:TEXCOORD0; };
            struct Varyings { float4 positionCS:SV_POSITION; float2 uv:TEXCOORD0; float fog:TEXCOORD1; };
            Varyings Vert(Attributes v)
            {
                Varyings o; o.positionCS=TransformObjectToHClip(v.positionOS.xyz); o.uv=v.uv;
                o.fog=ComputeFogFactor(o.positionCS.z); return o;
            }
            half4 Frag(Varyings i):SV_Target
            {
                // Soft curtains and a brighter interior centre, behind the existing frames.
                float folds=.84+.12*sin(i.uv.x*39)+.04*sin(i.uv.x*73);
                float edges=smoothstep(0,.14,i.uv.x)*smoothstep(0,.14,1-i.uv.x);
                float room=.60+.26*edges+.14*(1-i.uv.y);
                return half4(MixFog(_BaseColor.rgb*folds*room*1.45,i.fog),1);
            }
            ENDHLSL
        }
    }
}
