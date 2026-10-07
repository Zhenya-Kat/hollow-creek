Shader "HollowCreek/Diner Street Glass"
{
    Properties
    {
        _InteriorTex ("Interior seen through exterior window", 2D) = "black" {}
        _StreetCube ("Actual street at the entrance", Cube) = "" {}
        _InteriorCube ("Interior captured at this window", Cube) = "" {}
        _InteriorDepth ("Interior surface distance", Cube) = "" {}
        _UseInteriorDepth ("Reproject actual interior surfaces", Float) = 0
        _UseInteriorCube ("Use interior parallax", Float) = 0
        _InteriorCapture ("Interior capture position", Vector) = (0,1.6,0,0)
        _InteriorPaneCentre ("Interior window centre", Vector) = (0,1.6,0,0)
        _InteriorRight ("Interior window right", Vector) = (1,0,0,0)
        _InteriorForward ("Interior window inward", Vector) = (0,0,1,0)
        _RoomMin ("Interior bounds minimum", Vector) = (-6,0.008,0,0)
        _RoomMax ("Interior bounds maximum", Vector) = (6,3.2,10,0)
        _UseReflectionBasis ("Reflection uses captured street orientation", Float) = 0
        _ReflectionRight ("Captured street window right", Vector) = (1,0,0,0)
        _ReflectionForward ("Captured street window forward", Vector) = (0,0,1,0)
        _Tint ("Glass tint", Color) = (0.9, 0.94, 1, 1)
        _ViewMode ("Street transmission instead of interior snapshot", Float) = 0
        _Reflection ("Street reflection", Range(0, 1)) = 0.15
        _Opacity ("Opacity", Range(0, 1)) = 1
        _InteriorBrightness ("Interior brightness", Float) = 0.6
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
            #pragma target 3.5
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            TEXTURE2D(_InteriorTex); SAMPLER(sampler_InteriorTex);
            TEXTURECUBE(_StreetCube); SAMPLER(sampler_StreetCube);
            TEXTURECUBE(_InteriorCube); SAMPLER(sampler_InteriorCube);
            TEXTURECUBE(_InteriorDepth); SAMPLER(sampler_InteriorDepth);
            CBUFFER_START(UnityPerMaterial)
                half4 _Tint;
                float _ViewMode, _Reflection, _Opacity, _InteriorBrightness;
                float _UseInteriorCube, _UseReflectionBasis, _UseInteriorDepth;
                float4 _InteriorCapture, _InteriorPaneCentre, _InteriorRight, _InteriorForward;
                float4 _RoomMin, _RoomMax, _ReflectionRight, _ReflectionForward;
            CBUFFER_END
            struct Attributes { float4 positionOS:POSITION; float3 normalOS:NORMAL; float2 uv:TEXCOORD0; };
            struct Varyings { float4 positionCS:SV_POSITION; float3 positionWS:TEXCOORD0; float3 normalWS:TEXCOORD1; float2 uv:TEXCOORD2; float3 positionOS:TEXCOORD3; };
            Varyings Vert(Attributes i)
            {
                Varyings o;
                o.positionWS=TransformObjectToWorld(i.positionOS.xyz);
                o.positionCS=TransformWorldToHClip(o.positionWS);
                o.normalWS=TransformObjectToWorldNormal(i.normalOS);
                o.uv=i.uv; o.positionOS=i.positionOS.xyz; return o;
            }
            half4 Frag(Varyings i):SV_Target
            {
                float3 view=GetWorldSpaceNormalizeViewDir(i.positionWS);
                float3 n=normalize(i.normalWS);
                if(dot(n,view)<0)n=-n;
                float fresnel=pow(1-saturate(dot(n,view)),5);
                float3 reflectedDirection=reflect(-view,n);
                if(_UseReflectionBasis>0.5)
                {
                    float3 localReflection=normalize(mul((float3x3)GetWorldToObjectMatrix(),reflectedDirection));
                    reflectedDirection=_ReflectionRight.xyz*localReflection.x
                        +float3(0,1,0)*localReflection.y+_ReflectionForward.xyz*localReflection.z;
                }
                half3 reflection=SAMPLE_TEXTURECUBE_LOD(_StreetCube,sampler_StreetCube,reflectedDirection,1.5).rgb;
                half3 transmission=SAMPLE_TEXTURECUBE_LOD(_StreetCube,sampler_StreetCube,-view,0.5).rgb;
                half3 interior=SAMPLE_TEXTURE2D(_InteriorTex,sampler_InteriorTex,i.uv).rgb*_InteriorBrightness;
                if(_UseInteriorCube>0.5)
                {
                    float3 localRay=normalize(mul((float3x3)GetWorldToObjectMatrix(),-view));
                    float3 ray=normalize(_InteriorRight.xyz*localRay.x+float3(0,1,0)*localRay.y
                        +_InteriorForward.xyz*localRay.z);
                    float3 origin=_InteriorPaneCentre.xyz+_InteriorRight.xyz*i.positionOS.x
                        +float3(0,i.positionOS.y,0)+_InteriorForward.xyz*0.005;
                    float3 safeRay=(step(0,ray)*2-1)*max(abs(ray),0.0001);
                    float3 farWall=(lerp(_RoomMin.xyz,_RoomMax.xyz,step(0,ray))-origin)/safeRay;
                    float distanceToWall=max(0,min(farWall.x,min(farWall.y,farWall.z)));
                    float3 hit=origin+ray*distanceToWall;
                    if(_UseInteriorDepth>0.5)
                    {
                        // Conservative single-sample reprojection avoids disocclusion streaks.
                        float surfaceDistance=max(0.3,SAMPLE_TEXTURECUBE_LOD(
                            _InteriorDepth,sampler_InteriorDepth,ray,1).r);
                        float3 offset=(origin-_InteriorCapture.xyz)*0.08;
                        hit=_InteriorCapture.xyz+ray*surfaceDistance+offset;
                    }                    interior=SAMPLE_TEXTURECUBE_LOD(_InteriorCube,sampler_InteriorCube,
                        hit-_InteriorCapture.xyz,0).rgb*_InteriorBrightness;
                }
                half3 behind=lerp(interior,transmission,saturate(_ViewMode));
                half3 colour=lerp(behind,reflection,saturate(_Reflection+(1-_Reflection)*fresnel))*_Tint.rgb;
                return half4(colour,_Opacity);
            }
            ENDHLSL
        }
    }
}
