using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using Object=UnityEngine.Object;
namespace HollowCreek.Editor.Props
{
 public static class StreetNaturalAuthoring
 {
  const string Dir="Assets/Art/Environment/StreetNatural";
  static Material Surface(string name,Color colour,float mode){
   var path=Dir+"/"+name+".mat";var mat=AssetDatabase.LoadAssetAtPath<Material>(path);
   if(!mat){mat=new Material(Shader.Find("HollowCreek/Street Natural Surfaces"));AssetDatabase.CreateAsset(mat,path);}
   mat.shader=Shader.Find("HollowCreek/Street Natural Surfaces");mat.SetColor("_BaseColor",colour);mat.SetFloat("_Mode",mode);mat.SetFloat("_Cull",0);EditorUtility.SetDirty(mat);return mat;
  }
  public static void ApplyExisting(Transform root){
   System.IO.Directory.CreateDirectory(Dir);AssetDatabase.Refresh();
   var street=root.name=="Shared evening street"?root:root.Find("Shared evening street");if(!street)return;
   var bark=Surface("Weathered bark",new Color(.31f,.25f,.18f),0);
   var needles=Surface("Evergreen needles",new Color(.30f,.38f,.25f),1);
   var metal=Surface("Aged graphite lantern metal",new Color(.16f,.175f,.18f),2);
   metal.SetTexture("_EmissionMap",AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/Environment/Kenney/Graveyard/Lightpost.mat").GetTexture("_EmissionMap"));
   foreach(var renderer in street.GetComponentsInChildren<MeshRenderer>(true)){
    var filter=renderer.GetComponent<MeshFilter>();if(!filter||!filter.sharedMesh)continue;
    var mesh=filter.sharedMesh;
    if(mesh.name.StartsWith("pine")||mesh.name.StartsWith("Conifer_")){
     if(!mesh.name.StartsWith("Conifer_"))filter.sharedMesh=Conifer(renderer);
     renderer.sharedMaterials=new[]{bark,needles};renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=true;
    }else if(mesh.name.StartsWith("lightpost")){
     renderer.sharedMaterials=Enumerable.Repeat(metal,renderer.sharedMaterials.Length).ToArray();renderer.receiveShadows=true;
    }else if(mesh.name=="CommonTree_1"){
     var leaf=Surface("Textured autumn leaves",new Color(.42f,.45f,.32f),3);
     leaf.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/Environment/DinerWindow/EveningLeaves.mat").GetTexture("_BaseMap"));
     renderer.sharedMaterials=new[]{bark,leaf};renderer.receiveShadows=true;
    }
    PrefabUtility.RecordPrefabInstancePropertyModifications(renderer);PrefabUtility.RecordPrefabInstancePropertyModifications(filter);
   }
   // Match the two decorated posts to the 4.6 m street lamps; leave pumpkins at ground level.
   foreach(var name in new[]{"Left street lamp","Right street lamp"}){
    var group=street.Find("Opposite-side decorations/"+name);if(!group)continue;
    var post=group.GetComponentsInChildren<MeshRenderer>().FirstOrDefault(r=>r.GetComponent<MeshFilter>()?.sharedMesh.name=="lightpost-single");if(!post)continue;
    float factor=4.6f/post.bounds.size.y;
    if(Mathf.Abs(factor-1)>.005f){
     
     post.transform.localPosition=new Vector3(post.transform.localPosition.x,post.transform.localPosition.y*factor,post.transform.localPosition.z);
     post.transform.localScale*=factor;
     foreach(Transform part in group){if(part==post.transform||part.name.Contains("pumpkin"))continue;
      var local=part.localPosition;local.y*=factor;part.localPosition=local;
      if(part.GetComponent<Renderer>())part.localScale*=factor;
      PrefabUtility.RecordPrefabInstancePropertyModifications(part);
     }
     PrefabUtility.RecordPrefabInstancePropertyModifications(post.transform);
    }
    var light=group.GetComponentInChildren<Light>();if(light){light.color=new Color(1,.68f,.36f);light.intensity=3;light.range=7;PrefabUtility.RecordPrefabInstancePropertyModifications(light);}
   }
   AssetDatabase.SaveAssets();
  }
  static Mesh Conifer(MeshRenderer renderer){
   var b=renderer.bounds;var origin=new Vector3(b.center.x,b.min.y,b.center.z);float h=b.size.y,radius=Mathf.Min(b.extents.x,b.extents.z)*.91f;
   int seed=Mathf.RoundToInt(origin.x*137+origin.z*59+h*1000);var random=new System.Random(seed);
   var vertices=new List<Vector3>();var uv=new List<Vector2>();var colors=new List<Color>();var wood=new List<int>();var leaves=new List<int>();
   int Add(Vector3 p,Vector2 u,Color c){vertices.Add(renderer.transform.InverseTransformPoint(p));uv.Add(u);colors.Add(c);return vertices.Count-1;}
   void Tube(Vector3 a,Vector3 z,float ra,float rz){
    var dir=(z-a).normalized;var cross=Vector3.Cross(dir,Vector3.forward);if(cross.sqrMagnitude<.01f)cross=Vector3.Cross(dir,Vector3.right);cross.Normalize();var other=Vector3.Cross(dir,cross);
    int start=vertices.Count;const int sides=7;
    for(int j=0;j<2;j++)for(int k=0;k<sides;k++){float angle=k*Mathf.PI*2/sides;Add((j==0?a:z)+(cross*Mathf.Cos(angle)+other*Mathf.Sin(angle))*(j==0?ra:rz),new Vector2(k/(float)sides,j),Color.white);}
    for(int k=0;k<sides;k++){int next=(k+1)%sides;wood.AddRange(new[]{start+k,start+next,start+sides+k,start+next,start+sides+next,start+sides+k});}
   }
   void Spray(Vector3 a,Vector3 direction,float length,float width,Color colour){
    direction.Normalize();var side=Vector3.Cross(direction,Vector3.up).normalized;if(side.sqrMagnitude<.01f)side=Vector3.right;
    for(int plane=0;plane<2;plane++){
     var across=Quaternion.AngleAxis(plane*70,direction)*side;int start=vertices.Count;
     Add(a-across*width*.5f,new Vector2(0,0),colour);Add(a+across*width*.5f,new Vector2(0,1),colour);
     Add(a+direction*length-across*width*.5f,new Vector2(1,0),colour);Add(a+direction*length+across*width*.5f,new Vector2(1,1),colour);
     leaves.AddRange(new[]{start,start+1,start+2,start+1,start+3,start+2});
    }
   }
   Tube(origin,origin+Vector3.up*h,.065f*h,.008f*h);
   for(int tier=0;tier<15;tier++){
    float t=tier/15f,y=h*(.17f+.80f*t),length=radius*Mathf.Pow(1-t,.73f);
    int count=9;float offset=(float)random.NextDouble()*6.28f;
    for(int branch=0;branch<count;branch++){
     float angle=offset+branch*6.28f/count;var radial=new Vector3(Mathf.Cos(angle),0,Mathf.Sin(angle));var lateral=Vector3.Cross(radial,Vector3.up);
     var a=origin+Vector3.up*(y+((float)random.NextDouble()-.5f)*h*.045f);var end=a+radial*length*(.82f+(float)random.NextDouble()*.25f)+Vector3.down*length*.13f;
     Tube(a,end,.024f*h*(1-t),.006f);
     for(int k=0;k<8;k++){
      float along=.12f+k*.115f;var point=Vector3.Lerp(a,end,along);float sprayLength=Mathf.Lerp(.65f,.24f,along)*(1-t*.65f)*(h/5.5f);
      for(int sign=-1;sign<=1;sign+=2){
       var direction=(radial*.6f+lateral*sign*.78f+Vector3.up*.12f).normalized;
       float tint=.72f+(float)random.NextDouble()*.48f;var colour=new Color(tint,tint*(.97f+(float)random.NextDouble()*.09f),tint*.9f);
       Spray(point,direction,sprayLength,sprayLength*.40f,colour);
      }
     }
     Spray(end,radial+Vector3.up*.2f,.35f*(1-t*.6f),.16f*(1-t*.6f),Color.white);
    }
   }
   Spray(origin+Vector3.up*h*.91f,Vector3.up,h*.09f,h*.028f,Color.white);
   var mesh=new Mesh{name="Natural conifer "+seed,indexFormat=IndexFormat.UInt32};mesh.SetVertices(vertices);mesh.SetUVs(0,uv);mesh.SetColors(colors);mesh.subMeshCount=2;mesh.SetTriangles(wood,0);mesh.SetTriangles(leaves,1);mesh.RecalculateNormals();mesh.RecalculateBounds();
   var path=Dir+"/Conifer_"+seed+".asset";var existing=AssetDatabase.LoadAssetAtPath<Mesh>(path);
   if(existing){EditorUtility.CopySerialized(mesh,existing);Object.DestroyImmediate(mesh);mesh=existing;}else AssetDatabase.CreateAsset(mesh,path);
   return mesh;
  }
 }
}


