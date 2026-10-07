Shader "HollowCreek/Physical Diner Glass"
{
    Properties
    {
        _StreetCube ("Environment reflection", Cube) = "" {}
        _Tint ("Reflection tint", Color) = (0.94,0.97,1,1)
        _Reflection ("Normal incidence reflection", Range(0,1)) = 0.035
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" "RenderPipeline"="UniversalPipeline" }
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
            TEXTURECUBE(_StreetCube); SAMPLER(sampler_StreetCube);
            CBUFFER_START(UnityPerMaterial)
            half4 _Tint;
            float _Reflection;
            CBUFFER_END
            struct Attributes { float4 positionOS:POSITION; float3 normalOS:NORMAL; };
            struct Varyings { float4 positionCS:SV_POSITION; float3 positionWS:TEXCOORD0; float3 normalWS:TEXCOORD1; };
            Varyings Vert(Attributes i)
            {
                Varyings o;
                o.positionWS=TransformObjectToWorld(i.positionOS.xyz);
                o.positionCS=TransformWorldToHClip(o.positionWS);
                o.normalWS=TransformObjectToWorldNormal(i.normalOS); return o;
            }
            half4 Frag(Varyings i):SV_Target
            {
                float3 view=GetWorldSpaceNormalizeViewDir(i.positionWS);
                float3 normal=normalize(i.normalWS);
                if(dot(normal,view)<0)normal=-normal;
                float fresnel=pow(1-saturate(dot(normal,view)),5);
                half3 reflection=SAMPLE_TEXTURECUBE_LOD(_StreetCube,sampler_StreetCube,reflect(-view,normal),1).rgb;
                return half4(reflection*_Tint.rgb,min(0.7,_Reflection+(1-_Reflection)*fresnel));
            }
            ENDHLSL
        }
    }
}
