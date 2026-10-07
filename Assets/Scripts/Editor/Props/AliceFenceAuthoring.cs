using System.Collections.Generic;using System.Linq;using UnityEditor;using UnityEngine;using Object=UnityEngine.Object;
namespace HollowCreek.Editor.Props {
 public static class AliceFenceAuthoring {
  const string Dir="Assets/Art/Environment/UnifiedHouse";
  public static void Apply(Transform root){
   var shared=root.Find("Street buildings and trees")?root:root.Find("Shared evening street");if(!shared)return;
   System.IO.Directory.CreateDirectory(Dir);
   var picket=Material("WeatheredFencePickets",false);var rail=Material("WeatheredFenceRails",true);
   foreach(var r in shared.GetComponentsInChildren<MeshRenderer>(true)){
    var filter=r.GetComponent<MeshFilter>();if(!filter||!filter.sharedMesh)continue;
    var centre=r.bounds.center;if(Mathf.Abs(centre.x)>9||centre.z<5||centre.z>13)continue;
    if(filter.sharedMesh.name=="fence"||filter.sharedMesh.name=="WeatheredFence"){
     if(filter.sharedMesh.name=="fence")filter.sharedMesh=SplitBoards(filter.sharedMesh);
     r.sharedMaterials=new[]{picket,rail};r.receiveShadows=true;
     PrefabUtility.RecordPrefabInstancePropertyModifications(filter);PrefabUtility.RecordPrefabInstancePropertyModifications(r);
    }else if(r.name.StartsWith("Fence")&&filter.sharedMesh.name=="Cube"){
     r.enabled=false;PrefabUtility.RecordPrefabInstancePropertyModifications(r);
    }else if(r.name.StartsWith("Gate Post")){
     r.sharedMaterial=picket;PrefabUtility.RecordPrefabInstancePropertyModifications(r);
    }
   }
   AssetDatabase.SaveAssets();
  }
  static Material Material(string name,bool horizontal){
   var path=Dir+"/"+name+".mat";var mat=AssetDatabase.LoadAssetAtPath<Material>(path);if(!mat){mat=new Material(Shader.Find("HollowCreek/Weathered Fence Wood"));AssetDatabase.CreateAsset(mat,path);}
   mat.shader=Shader.Find("HollowCreek/Weathered Fence Wood");mat.SetColor("_BaseColor",new Color(.43f,.29f,.18f));mat.SetFloat("_Horizontal",horizontal?1:0);EditorUtility.SetDirty(mat);return mat;
  }
  static Mesh SplitBoards(Mesh source){
   var path=Dir+"/WeatheredFence.asset";var existing=AssetDatabase.LoadAssetAtPath<Mesh>(path);if(existing)return existing;
   var v=source.vertices;var t=source.triangles;var remaining=Enumerable.Range(0,t.Length/3).ToList();var pickets=new List<int>();var rails=new List<int>();
   while(remaining.Count>0){var component=new List<int>{remaining[0]};remaining.RemoveAt(0);var points=new HashSet<Vector3>(Enumerable.Range(0,3).Select(k=>v[t[component[0]*3+k]]));bool changed;
    do{changed=false;for(int j=remaining.Count-1;j>=0;j--){int tri=remaining[j];if(Enumerable.Range(0,3).Any(k=>points.Contains(v[t[tri*3+k]]))){for(int k=0;k<3;k++)points.Add(v[t[tri*3+k]]);component.Add(tri);remaining.RemoveAt(j);changed=true;}}}while(changed);
    var b=new Bounds(points.First(),Vector3.zero);foreach(var p in points)b.Encapsulate(p);bool horizontal=Mathf.Max(b.size.x,b.size.z)>b.size.y*1.5f;
    foreach(int tri in component)for(int k=0;k<3;k++)(horizontal?rails:pickets).Add(t[tri*3+k]);
   }
   var mesh=new Mesh{name="WeatheredFence",indexFormat=source.indexFormat};mesh.vertices=v;mesh.normals=source.normals;mesh.uv=source.uv;mesh.tangents=source.tangents;mesh.subMeshCount=2;mesh.SetTriangles(pickets,0);mesh.SetTriangles(rails,1);mesh.RecalculateBounds();AssetDatabase.CreateAsset(mesh,path);return mesh;
  }
 }
}


