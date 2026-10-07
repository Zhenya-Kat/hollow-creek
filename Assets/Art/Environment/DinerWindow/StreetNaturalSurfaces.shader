Shader "HollowCreek/Street Natural Surfaces"
{
 Properties {
 _BaseColor("Colour",Color)=(0.2,0.25,0.18,1)
 [MainTexture] _BaseMap("Leaf texture",2D)="white"{}
 _EmissionMap("Lantern glass mask",2D)="black"{}
 _Mode("Bark 0, needles 1, metal 2, leaves 3",Float)=0
 _Cull("Cull",Float)=0
 }
 SubShader {
 Tags {"RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" "Queue"="AlphaTest"}
 Cull [_Cull]
 Pass {
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
 TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);
 TEXTURE2D(_EmissionMap); SAMPLER(sampler_EmissionMap);
 CBUFFER_START(UnityPerMaterial)
 half4 _BaseColor; float _Mode,_Cull;
 CBUFFER_END
 struct A {float4 p:POSITION;float3 n:NORMAL;float2 uv:TEXCOORD0;half4 colour:COLOR;};
 struct V {float4 clip:SV_POSITION;float3 p:TEXCOORD0;float3 n:TEXCOORD1;float2 uv:TEXCOORD2;half4 colour:COLOR;float fog:TEXCOORD3;};
 V Vert(A a){V o;o.p=TransformObjectToWorld(a.p.xyz);o.clip=TransformWorldToHClip(o.p);o.n=TransformObjectToWorldNormal(a.n);o.uv=a.uv;o.colour=a.colour;o.fog=ComputeFogFactor(o.clip.z);return o;}
 float Hash(float2 p){return frac(sin(dot(p,float2(127.1,311.7)))*43758.5453);}
 float Noise(float2 p){float2 q=floor(p),f=frac(p);f=f*f*(3-2*f);return lerp(lerp(Hash(q),Hash(q+float2(1,0)),f.x),lerp(Hash(q+float2(0,1)),Hash(q+1),f.x),f.y);}
 half4 Frag(V i, FRONT_FACE_TYPE face:FRONT_FACE_SEMANTIC):SV_Target {
 float3 n=normalize(i.n)*IS_FRONT_VFACE(face,1,-1);
 float2 p=abs(n.x)>abs(n.z)?i.p.zy:i.p.xy;
 float fine=Noise(p*190),coarse=Noise(p*6),height=0;
 half3 albedo=_BaseColor.rgb,emission=0;float metallic=0,smoothness=.16;
 if(_Mode<.5){
 float grain=Noise(float2(p.x*45+Noise(p*3)*2,p.y*3));
 float cracks=smoothstep(.58,.72,grain);
 albedo*=.57+.6*grain+.12*coarse;
 albedo=lerp(albedo,half3(.12,.095,.065),cracks*.25);
 height=.0018*grain+.0003*fine;
 }else if(_Mode<1.5){
 float x=i.uv.x,y=abs(i.uv.y-.5)*2;
 float phase=x*19-y*3.2;
 float needleDistance=abs(frac(phase)-.5);
 float width=max(fwidth(phase)*.55,.18);
 float needles=1-smoothstep(width,width+.035,needleDistance);
 float envelope=pow(saturate(sin(x*3.14159265)),.6);
 clip(min(envelope-y, max(needles-.35,.035-y)));
 albedo*=i.colour.rgb*(.7+.35*(1-y)+.13*coarse);
 }else if(_Mode<2.5){
 float glass=SAMPLE_TEXTURE2D(_EmissionMap,sampler_EmissionMap,i.uv).r*step(.75,i.uv.x)*step(i.uv.x,.85);
 float chips=smoothstep(.77,.89,Noise(p*80))*smoothstep(.6,.8,coarse);
 albedo*=.8+.28*coarse+.08*fine;
 albedo=lerp(albedo,half3(.22,.17,.105),chips*.42);
 albedo=lerp(albedo,half3(.65,.43,.2),glass);
 metallic=lerp(.65,0,glass);smoothness=lerp(.34,.48,glass);
 emission=half3(1,.58,.23)*glass*.85;
 height=.00018*fine;
 }else{
 half4 tex=SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,i.uv);clip(tex.a-.45);
 albedo*=tex.rgb*(.85+.25*coarse);
 height=.0002*fine;
 }
 float3 dx=ddx(i.p),dy=ddy(i.p),r1=cross(dy,n),r2=cross(n,dx);float det=dot(dx,r1);
 n=normalize(n-sign(det)*(ddx(height)*r1+ddy(height)*r2)/max(abs(det),.000001));
 InputData input=(InputData)0;input.positionWS=i.p;input.normalWS=n;input.viewDirectionWS=GetWorldSpaceNormalizeViewDir(i.p);input.shadowCoord=TransformWorldToShadowCoord(i.p);input.bakedGI=SampleSH(n);input.normalizedScreenSpaceUV=GetNormalizedScreenSpaceUV(i.clip);input.shadowMask=half4(1,1,1,1);
 SurfaceData s=(SurfaceData)0;s.albedo=albedo;s.alpha=1;s.normalTS=half3(0,0,1);s.metallic=metallic;s.smoothness=smoothness;s.occlusion=.9;s.emission=emission+albedo*half3(.12,.14,.17);
 half4 c=UniversalFragmentPBR(input,s);c.rgb=MixFog(c.rgb,i.fog);return c;
 }
 ENDHLSL
 }

 }
}


