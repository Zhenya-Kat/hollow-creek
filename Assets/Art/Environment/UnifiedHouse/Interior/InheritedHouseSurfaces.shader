Shader "HollowCreek/Inherited House Surfaces" {
 Properties { _Cull("Culling",Float)=2 _BaseColor("Colour",Color)=(.3,.2,.12,1) _Kind("0 wood 1 floor 2 plaster 3 textile 4 rug 5 leather 6 metal 7 damask 8 tiles",Float)=0 _Metallic("Metal reflectivity",Range(0,1))=.65 _Smoothness("Finish",Range(0,1))=.2 }
 SubShader {Tags {"RenderType"="Opaque" "RenderPipeline"="UniversalPipeline"}
 Pass {Tags {"LightMode"="UniversalForward"} Cull [_Cull]
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
 half4 _BaseColor;float _Kind;float _Smoothness;float _Metallic;
 CBUFFER_END
 struct A {float4 p:POSITION;float3 n:NORMAL;};
 struct V {float4 clip:SV_POSITION;float3 p:TEXCOORD0;float3 n:TEXCOORD1;float fog:TEXCOORD2;};
 V Vert(A a){V o;o.p=TransformObjectToWorld(a.p.xyz);o.n=TransformObjectToWorldNormal(a.n);o.clip=TransformWorldToHClip(o.p);o.fog=ComputeFogFactor(o.clip.z);return o;}
 float Hash(float2 p){return frac(sin(dot(p,float2(127.1,311.7)))*43758.5453);}
 float Noise(float2 p){float2 q=floor(p),f=frac(p);f=f*f*(3-2*f);return lerp(lerp(Hash(q),Hash(q+float2(1,0)),f.x),lerp(Hash(q+float2(0,1)),Hash(q+1),f.x),f.y);}
 float Leaf(float2 p,float2 centre,float2 size,float angle){p-=centre;float c=cos(angle),s=sin(angle);p=float2(c*p.x-s*p.y,s*p.x+c*p.y)/size;float d=abs(p.x)+p.y*p.y;return 1-smoothstep(.91,1.03,d);}
 float Damask(float2 world){float row=floor(world.y/.72);float2 q=frac(float2(world.x/.52+fmod(row,2)*.5,world.y/.72))-.5;float2 p=float2(abs(q.x),q.y);float r=length(q-float2(0,.045));float ang=atan2(q.y-.045,q.x);float flower=1-smoothstep(.094+.022*cos(8*ang),.102+.022*cos(8*ang),r);float leaf=Leaf(p,float2(.155,-.19),float2(.065,.13),-.7);leaf=max(leaf,Leaf(p,float2(.177,-.025),float2(.062,.13),.4));leaf=max(leaf,Leaf(p,float2(.116,.18),float2(.055,.105),.72));leaf=max(leaf,Leaf(p,float2(.026,.285),float2(.043,.117),.12));float stem=(1-smoothstep(.008,.016,abs(p.x-.16*sin((p.y+.33)*6.1))))*smoothstep(-.37,-.32,p.y)*(1-smoothstep(.21,.26,p.y));float bud=1-smoothstep(.025,.035,length(q-float2(0,-.37)));return saturate(max(max(leaf,flower),max(stem,bud)));}
 half4 Frag(V i):SV_Target {
 float3 n=normalize(i.n);float2 p=abs(n.y)>.65?i.p.xz:(abs(n.x)>abs(n.z)?i.p.zy:i.p.xy);
 float coarse=Noise(p*3),fine=Noise(p*72);float height=0;half3 a=_BaseColor.rgb;float smoothness=_Smoothness;
 if(_Kind<1.5){
 float grain=Noise(float2(p.x*115+Noise(p*float2(5,1.2))*12,p.y*2));
 float knotR=length((frac(p*float2(2.3,.9))-float2(.35,.6))*float2(1,3));float knots=(1-smoothstep(.07,.24,knotR))*step(.62,Hash(floor(p*float2(2.3,.9))));
 a*=.86+.24*coarse+.19*(grain-.5)-.25*knots;height=grain*.0009;
 if(_Kind>.5){float row=floor(p.x/.19);float end=frac((p.y+Hash(float2(row,4))*2.4)/1.65);float edge=min(frac(p.x/.19),1-frac(p.x/.19));float seam=max(1-smoothstep(.003,.014,edge),1-smoothstep(.001,.004,min(end,1-end)));a*=1-.47*seam;a*=.86+.24*Hash(float2(row,floor((p.y+Hash(float2(row,4))*2.4)/1.65)));height-=seam*.002;}
 }else if(_Kind<2.5){
 float stain=Noise(p*.65)*Noise(p*1.6);a*=.96+.07*coarse-.04*stain;height=(fine-.5)*.00016;
 }else if(_Kind<4.5){
 float weave=(Noise(p*170)-.5)*saturate(1-length(fwidth(p))*170);a*=.95+.07*coarse+.04*weave;height=.00012*weave;
 if(_Kind>3.5){float2 q=frac(p*1.9)-.5;float diamond=abs(q.x)+abs(q.y);float motif=1-smoothstep(.24,.28,diamond);float ring=(1-smoothstep(.38,.40,diamond))*smoothstep(.31,.33,diamond);a=lerp(a,half3(.36,.26,.18),.36*motif+.28*ring);a*=.80+.23*Noise(p*8);}
 }else if(_Kind<5.5){a*=.89+.15*coarse+.08*(fine-.5);height=fine*.0007;}else if(_Kind<6.5){a*=.88+.16*coarse;}else if(_Kind<7.5){float motif=Damask(p);a=lerp(a,half3(.235,.285,.205),motif*.80);a*=.97+.06*coarse;float fibre=(fine-.5)*saturate(1-length(fwidth(p))*72);height=fibre*.00023+motif*.00012;}else if(_Kind>8.5){float pile=(Noise(p*130)-.5)*saturate(1-length(fwidth(p))*130);a*=.95+.10*coarse+.11*pile;float facing=pow(1-saturate(abs(dot(n,GetWorldSpaceNormalizeViewDir(i.p)))),3);a+=_BaseColor.rgb*facing*.20;height=pile*.0006;smoothness=.06;}else{float2 cell=p/.115;float2 q=frac(cell);float edge=min(min(q.x,1-q.x),min(q.y,1-q.y));float grout=1-smoothstep(.018,.045,edge);float checker=fmod(floor(cell.x)+floor(cell.y),2);a=lerp(a,a*half3(.69,.78,.71),checker*.22);a=lerp(a,half3(.38,.36,.29),grout);height=-grout*.001;smoothness=.45;}
 float3 dx=ddx(i.p),dy=ddy(i.p),r1=cross(dy,n),r2=cross(n,dx);float det=dot(dx,r1);n=normalize(n-sign(det)*(ddx(height)*r1+ddy(height)*r2)/max(abs(det),.000001));
 InputData input=(InputData)0;input.positionWS=i.p;input.normalWS=n;input.viewDirectionWS=GetWorldSpaceNormalizeViewDir(i.p);input.shadowCoord=TransformWorldToShadowCoord(i.p);input.bakedGI=SampleSH(n);input.normalizedScreenSpaceUV=GetNormalizedScreenSpaceUV(i.clip);input.shadowMask=half4(1,1,1,1);
 SurfaceData s=(SurfaceData)0;s.albedo=a;s.alpha=1;s.normalTS=half3(0,0,1);s.smoothness=smoothness;s.metallic=(_Kind>5.5&&_Kind<6.5)?_Metallic:0;s.occlusion=.9;s.emission=a*.015;
 half4 c=UniversalFragmentPBR(input,s);c.rgb=MixFog(c.rgb,i.fog);return c;
 }
 ENDHLSL }
 UsePass "Universal Render Pipeline/Lit/ShadowCaster"
 UsePass "Universal Render Pipeline/Lit/DepthOnly"
 }
}





