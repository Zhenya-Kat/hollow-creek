Shader "HollowCreek/Weathered Fence Wood"
{
 Properties { _BaseColor("Timber colour",Color)=(.37,.24,.14,1) _Horizontal("Rail grain",Float)=0 }
 SubShader {
 Tags {"RenderType"="Opaque" "RenderPipeline"="UniversalPipeline"}
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
 CBUFFER_START(UnityPerMaterial)
 half4 _BaseColor;float _Horizontal;
 CBUFFER_END
 struct A{float4 p:POSITION;float3 n:NORMAL;};
 struct V{float4 clip:SV_POSITION;float3 p:TEXCOORD0;float3 n:TEXCOORD1;float fog:TEXCOORD2;};
 V Vert(A a){V o;o.p=TransformObjectToWorld(a.p.xyz);o.n=TransformObjectToWorldNormal(a.n);o.clip=TransformWorldToHClip(o.p);o.fog=ComputeFogFactor(o.clip.z);return o;}
 float Hash(float2 p){return frac(sin(dot(p,float2(127.1,311.7)))*43758.5453);}
 float Noise(float2 p){float2 q=floor(p),f=frac(p);f=f*f*(3-2*f);return lerp(lerp(Hash(q),Hash(q+float2(1,0)),f.x),lerp(Hash(q+float2(0,1)),Hash(q+1),f.x),f.y);}
 half4 Frag(V i):SV_Target{
 float3 n=normalize(i.n);float across=abs(n.x)>abs(n.z)?i.p.z:i.p.x;
 float2 p=_Horizontal>.5?float2(i.p.y,across):float2(across,i.p.y);
 float coarse=Noise(p*float2(7,1.5));float warp=Noise(p*float2(5,1.4))*.9;
 float grain=Noise(float2(p.x*95+warp*7,p.y*2.8));float fine=Noise(p*float2(310,12));
 float2 cell=floor(p/float2(.22,.8)),f=frac(p/float2(.22,.8));
 float2 centre=float2(.23+.54*Hash(cell),.2+.6*Hash(cell+5));
 float2 q=(f-centre)*float2(1,2.8);float radius=length(q);float knot=(1-smoothstep(.06,.15,radius))*step(.79,Hash(cell+13));
 float rings=sin(radius*90)*exp(-radius*12)*step(.79,Hash(cell+13));
 float groove=smoothstep(.63,.83,grain);float splits=smoothstep(.78,.91,Noise(p*float2(65,2)))*smoothstep(.55,.78,coarse);
 float weather=Noise(p*float2(12,3));float low=1-smoothstep(.05,.65,i.p.y);
 half3 albedo=_BaseColor.rgb*(.83+.24*coarse+.16*(grain-.5)+.06*(fine-.5));
 albedo=lerp(albedo,half3(.31,.29,.24),.27*weather);albedo*=1-groove*.19-splits*.27-knot*.33+ rings*.12;
 albedo*=1-low*.25;
 float height=.00065*grain+.00013*fine-.0006*splits;
 float3 dx=ddx(i.p),dy=ddy(i.p),r1=cross(dy,n),r2=cross(n,dx);float det=dot(dx,r1);
 n=normalize(n-sign(det)*(ddx(height)*r1+ddy(height)*r2)/max(abs(det),.000001));
 InputData input=(InputData)0;input.positionWS=i.p;input.normalWS=n;input.viewDirectionWS=GetWorldSpaceNormalizeViewDir(i.p);input.shadowCoord=TransformWorldToShadowCoord(i.p);input.bakedGI=SampleSH(n);input.normalizedScreenSpaceUV=GetNormalizedScreenSpaceUV(i.clip);input.shadowMask=half4(1,1,1,1);
 SurfaceData s=(SurfaceData)0;s.albedo=albedo;s.alpha=1;s.normalTS=half3(0,0,1);s.smoothness=.16+.12*(1-weather);s.occlusion=.92;s.emission=albedo*half3(.08,.10,.12);
 half4 c=UniversalFragmentPBR(input,s);c.rgb=MixFog(c.rgb,i.fog);return c;
 }
 ENDHLSL
 }
 UsePass "Universal Render Pipeline/Lit/ShadowCaster"
 UsePass "Universal Render Pipeline/Lit/DepthOnly"
 }
}
