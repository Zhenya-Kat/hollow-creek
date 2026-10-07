Shader "HollowCreek/Weathered Building Finish"
{
 Properties
 {
  _BaseColor("Finish colour",Color)=(0.75,0.74,0.7,1)
  _MortarColor("Mortar / exposed wood",Color)=(0.4,0.38,0.34,1)
  _Siding("Horizontal painted siding",Float)=0
  _Weathering("Weathering",Range(0,1))=0.3
 }
 SubShader
 {
  Tags {"RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" "Queue"="Geometry"}
  Pass
  {
   Name "ForwardLit"
   Tags {"LightMode"="UniversalForward"}
   HLSLPROGRAM
   #pragma target 3.5
   #pragma vertex Vert
   #pragma fragment Frag
   #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
   #pragma multi_compile _ _ADDITIONAL_LIGHTS
   #pragma multi_compile _ _CLUSTER_LIGHT_LOOP
   #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
   #pragma multi_compile_fragment _ _SHADOWS_SOFT
   #pragma multi_compile_fog
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
   CBUFFER_START(UnityPerMaterial)
    half4 _BaseColor,_MortarColor;
    float _Siding,_Weathering;
   CBUFFER_END
   struct Attributes {float4 positionOS:POSITION;float3 normalOS:NORMAL;};
   struct Varyings {float4 positionCS:SV_POSITION;float3 p:TEXCOORD0;float3 n:TEXCOORD1;float fog:TEXCOORD2;};
   Varyings Vert(Attributes i){Varyings o;o.p=TransformObjectToWorld(i.positionOS.xyz);o.positionCS=TransformWorldToHClip(o.p);o.n=TransformObjectToWorldNormal(i.normalOS);o.fog=ComputeFogFactor(o.positionCS.z);return o;}
   float Hash(float2 p){return frac(sin(dot(p,float2(127.1,311.7)))*43758.5453);}
   float Noise(float2 p){float2 q=floor(p),f=frac(p);f=f*f*(3-2*f);return lerp(lerp(Hash(q),Hash(q+float2(1,0)),f.x),lerp(Hash(q+float2(0,1)),Hash(q+1),f.x),f.y);}
   half4 Frag(Varyings i):SV_Target
   {
    float3 normal=normalize(i.n);
    float2 p=abs(normal.x)>abs(normal.z)?float2(i.p.z,i.p.y):i.p.xy;
    float coarse=Noise(p*1.7),grain=Noise(p*140),height;
    half3 albedo;
    if(_Siding>0.5)
    {
     float boardCoordinate=p.y/0.18;
     float row=floor(boardCoordinate),v=frac(boardCoordinate);
     float aa=max(fwidth(boardCoordinate),0.008);
     float groove=1-smoothstep(0.015,0.015+aa,min(v,1-v));
     float wood=Noise(float2(p.x*1.9,p.y*95));
     float wear=smoothstep(0.75,0.92,Noise(float2(p.x*7,row*13)))*
         (1-smoothstep(0.08,0.3,v))*_Weathering;
     float scuff=smoothstep(0.75,0.9,Noise(p*float2(25,110)))*_Weathering;
     albedo=_BaseColor.rgb*(0.93+0.07*Hash(float2(row,2))+0.035*(wood-0.5)+0.025*(grain-0.5));
     albedo=lerp(albedo,_MortarColor.rgb,saturate(wear*0.35+scuff*0.08));
     albedo*= (1-groove*0.23)*(0.94+0.06*smoothstep(0,0.25,v));
     albedo*=1-_Weathering*0.16*(1-coarse);
     height=0.00015*wood;
    }
    else
    {
     float row=floor(p.y/0.095);
     float2 grid=float2(p.x/0.28+fmod(abs(row),2)*0.5,p.y/0.095);
     float2 cell=floor(grid),f=frac(grid);
     float2 edge=min(f,1-f)*float2(0.28,0.095);
     float d=min(edge.x,edge.y),aa=max(fwidth(d),0.0005);
     float brick=smoothstep(0.003-aa,0.006+aa,d);
     float variation=Hash(cell);
     half3 clay=_BaseColor.rgb*(0.82+0.28*variation+0.07*(coarse-0.5)+0.06*(grain-0.5));
     float erosion=Noise(p*28);
     clay=lerp(clay,clay*0.76,_Weathering*(1-erosion));
     float stain=smoothstep(0.58,0.88,Noise(p*float2(1.2,3.5)))*_Weathering;
     clay=lerp(clay,half3(0.48,0.46,0.4),stain*0.13);
     albedo=lerp(_MortarColor.rgb*(0.93+0.08*grain),clay,brick);
     height=0.0025*brick+0.00035*erosion*brick;
    }
    float3 dx=ddx(i.p),dy=ddy(i.p),r1=cross(dy,normal),r2=cross(normal,dx);
    float determinant=dot(dx,r1);
    normal=normalize(normal-sign(determinant)*(ddx(height)*r1+ddy(height)*r2)/max(abs(determinant),0.000001));
    InputData input=(InputData)0;
    input.positionWS=i.p;input.normalWS=normal;input.viewDirectionWS=GetWorldSpaceNormalizeViewDir(i.p);
    input.shadowCoord=TransformWorldToShadowCoord(i.p);
    input.bakedGI=SampleSH(normal);input.normalizedScreenSpaceUV=GetNormalizedScreenSpaceUV(i.positionCS);
    input.shadowMask=half4(1,1,1,1);
    SurfaceData surface=(SurfaceData)0;
    surface.albedo=albedo;surface.alpha=1;surface.normalTS=half3(0,0,1);
    surface.smoothness=0.12;surface.occlusion=1;
    surface.emission=albedo*half3(0.045,0.052,0.068);
    half4 colour=UniversalFragmentPBR(input,surface);colour.rgb=MixFog(colour.rgb,i.fog);return colour;
   }
   ENDHLSL
  }
  UsePass "Universal Render Pipeline/Lit/ShadowCaster"
  UsePass "Universal Render Pipeline/Lit/DepthOnly"
  UsePass "Universal Render Pipeline/Lit/DepthNormals"
 }
}
