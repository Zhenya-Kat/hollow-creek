Shader "HollowCreek/Rain on Window"
{
    Properties
    {
        _Tint ("Rain highlights", Color) = (0.38, 0.52, 0.66, 1)
        _Amount ("Rain visibility", Range(0, 1)) = 0.25
        _Speed ("Rain speed", Range(0, 2)) = 0.45
        _PreviewTime ("Preview time offset", Float) = 0
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent+20" "RenderPipeline"="UniversalPipeline" }
        Pass
        {
            Tags { "LightMode"="SRPDefaultUnlit" }
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            CBUFFER_START(UnityPerMaterial)
                half4 _Tint;
                float _Amount, _Speed, _PreviewTime;
            CBUFFER_END
            struct Attributes { float4 positionOS : POSITION; float2 uv : TEXCOORD0; };
            struct Varyings { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; };
            Varyings Vert(Attributes i)
            {
                Varyings o; o.positionCS=TransformObjectToHClip(i.positionOS.xyz); o.uv=i.uv; return o;
            }
            float Hash(float2 p) { return frac(sin(dot(p,float2(127.1,311.7)))*43758.5453); }
            float Drops(float2 uv, float2 grid, float time, float seed)
            {
                float2 p=uv*grid+float2(seed,time);
                float2 cell=floor(p);
                float n=Hash(cell+seed);
                float2 q=frac(p)-float2(0.2+0.6*n,0.25+0.5*Hash(cell+5.1));
                float radius=length(q*float2(1.0,0.58));
                float drop=(1-smoothstep(0.06,0.11,radius))*step(0.75,n);
                float rim=smoothstep(0.035,0.065,radius)*(1-smoothstep(0.07,0.105,radius));
                float tail=(1-smoothstep(0.011,0.032,abs(q.x)))
                    *smoothstep(-0.30,-0.08,q.y)*(1-smoothstep(-0.08,0.03,q.y))*step(0.78,n);
                return drop*(0.22+rim*0.6)+tail*0.18;
            }
            half4 Frag(Varyings i) : SV_Target
            {
                float time=(_Time.y+_PreviewTime)*_Speed;
                float wet=Drops(i.uv,float2(30,16),0,1.7);
                wet+=Drops(i.uv,float2(17,8),time,8.3);
                float2 p=i.uv*float2(36,7)+float2(i.uv.y*1.4,time*2.2);
                float n=Hash(floor(p));
                float2 q=frac(p);
                float streak=(1-smoothstep(0.012,0.05,abs(q.x-(0.15+0.7*n))))
                    *smoothstep(0.1,0.23,q.y)*(1-smoothstep(0.45,0.78,q.y))*step(0.9,n);
                return half4(_Tint.rgb,saturate((wet+streak*0.42)*_Amount));
            }
            ENDHLSL
        }
    }
}
