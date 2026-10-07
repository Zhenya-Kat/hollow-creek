using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using Object=UnityEngine.Object;
namespace HollowCreek.Editor.Props
{
 public static class HouseWindowFinishAuthoring
 {
  const string Dir="Assets/Art/Environment/UnifiedHouse";
  public static void Apply(Transform shell){
   var old=shell.Find("Rain on house windows");if(old)Object.DestroyImmediate(old.gameObject);
   var rainRoot=new GameObject("Rain on house windows").transform;rainRoot.SetParent(shell,false);
   var path=Dir+"/RainOnHouseWindows.mat";var rain=AssetDatabase.LoadAssetAtPath<Material>(path);
   if(!rain){rain=new Material(Shader.Find("HollowCreek/Rain on Window"));AssetDatabase.CreateAsset(rain,path);}
   rain.SetColor("_Tint",new Color(.42f,.52f,.62f));rain.SetFloat("_Amount",.65f);rain.SetFloat("_Speed",.12f);rain.SetFloat("_PreviewTime",11);EditorUtility.SetDirty(rain);
   foreach(var pane in shell.GetComponentsInChildren<MeshRenderer>().Where(r=>r.name.StartsWith("Physical house window")).ToArray()){
    var layer=new GameObject("Raindrops on "+pane.name);layer.transform.SetParent(rainRoot,false);layer.transform.SetPositionAndRotation(pane.transform.position-pane.transform.forward*.003f,pane.transform.rotation);layer.transform.localScale=pane.transform.lossyScale;
    layer.AddComponent<MeshFilter>().sharedMesh=pane.GetComponent<MeshFilter>().sharedMesh;var renderer=layer.AddComponent<MeshRenderer>();renderer.sharedMaterial=rain;renderer.shadowCastingMode=ShadowCastingMode.Off;
   }
   var upper=shell.Find("Original house upper storey and roof");var filter=upper.GetComponent<MeshFilter>();var rendererUpper=upper.GetComponent<MeshRenderer>();var source=filter.sharedMesh;
   var vertices=source.vertices;var tex=source.uv;var glass=new List<int[]>();
   for(int sub=0;sub<source.subMeshCount;sub++){var t=source.GetTriangles(sub);for(int j=0;j<t.Length;j+=3){var ids=new[]{t[j],t[j+1],t[j+2]};var u=(tex[ids[0]]+tex[ids[1]]+tex[ids[2]])/3;if(u.x>.68f&&u.x<.76f&&u.y>.51f&&u.y<.66f)glass.Add(ids);}}
   var windows=new List<List<int[]>>();
   while(glass.Count>0){var group=new List<int[]>{glass[0]};glass.RemoveAt(0);var points=new HashSet<Vector3>(group[0].Select(i=>vertices[i]));bool changed;do{changed=false;for(int i=glass.Count-1;i>=0;i--)if(glass[i].Any(id=>points.Contains(vertices[id]))){foreach(var id in glass[i])points.Add(vertices[id]);group.Add(glass[i]);glass.RemoveAt(i);changed=true;}}while(changed);windows.Add(group);}
   var bounds=windows.Select(w=>{var points=w.SelectMany(t=>t).Select(i=>vertices[i]).ToArray();var b=new Bounds(points[0],Vector3.zero);foreach(var p in points)b.Encapsulate(p);b.Expand(new Vector3(.95f,1.1f,1.6f));return b;}).ToArray();
   var white=new List<int>();var submeshes=new List<int[]>();
   for(int sub=0;sub<source.subMeshCount;sub++){var keep=new List<int>();var t=source.GetTriangles(sub);for(int j=0;j<t.Length;j+=3){var ids=new[]{t[j],t[j+1],t[j+2]};var u=(tex[ids[0]]+tex[ids[1]]+tex[ids[2]])/3;var centre=(vertices[ids[0]]+vertices[ids[1]]+vertices[ids[2]])/3;
    bool frame=sub==0&&u.x<.15f&&u.y>.25f&&u.y<.53f&&bounds.Any(b=>b.Contains(centre));(frame?white:keep).AddRange(ids);}submeshes.Add(keep.ToArray());}
   var finished=new Mesh{name="House upper storey with white window frames",indexFormat=IndexFormat.UInt32};finished.vertices=vertices;finished.normals=source.normals;finished.uv=tex;finished.tangents=source.tangents;finished.subMeshCount=submeshes.Count+1;for(int i=0;i<submeshes.Count;i++)finished.SetTriangles(submeshes[i],i);finished.SetTriangles(white,submeshes.Count);finished.RecalculateBounds();
   filter.sharedMesh=StoreMesh(finished,"WhiteWindowUpperStorey");rendererUpper.sharedMaterials=rendererUpper.sharedMaterials.Concat(new[]{AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/Environment/BuildingFinishes/WeatheredWhiteSiding.mat")}).ToArray();
   for(int w=0;w<windows.Count;w++){
    var points=windows[w].SelectMany(t=>t).Select(i=>vertices[i]).ToArray();var b=new Bounds(points[0],Vector3.zero);foreach(var p in points)b.Encapsulate(p);bool alongX=b.size.x>b.size.z;
    var pList=new List<Vector3>();var uvList=new List<Vector2>();var indices=new List<int>();
    foreach(var tri in windows[w]){var n=Vector3.Cross(vertices[tri[1]]-vertices[tri[0]],vertices[tri[2]]-vertices[tri[0]]).normalized;foreach(var id in tri){var p=vertices[id];indices.Add(pList.Count);pList.Add(p+n*.004f);uvList.Add(new Vector2(alongX?(p.x-b.min.x)/Mathf.Max(.001f,b.size.x):(p.z-b.min.z)/Mathf.Max(.001f,b.size.z),(p.y-b.min.y)/Mathf.Max(.001f,b.size.y)));}}
    var mesh=new Mesh{name="Rain on upper window "+w};mesh.SetVertices(pList);mesh.SetUVs(0,uvList);mesh.SetTriangles(indices,0);mesh.RecalculateNormals();mesh.RecalculateBounds();
    var layer=new GameObject("Raindrops on upper window "+(w+1));layer.transform.SetParent(rainRoot,false);layer.AddComponent<MeshFilter>().sharedMesh=StoreMesh(mesh,"RainUpperWindow"+w);var renderer=layer.AddComponent<MeshRenderer>();renderer.sharedMaterial=rain;renderer.shadowCastingMode=ShadowCastingMode.Off;
   }
   AssetDatabase.SaveAssets();
  }
  static Mesh StoreMesh(Mesh mesh,string name){var path=Dir+"/"+name+".asset";var existing=AssetDatabase.LoadAssetAtPath<Mesh>(path);if(existing){existing.Clear();existing.indexFormat=mesh.indexFormat;existing.vertices=mesh.vertices;existing.normals=mesh.normals;existing.uv=mesh.uv;existing.tangents=mesh.tangents;existing.subMeshCount=mesh.subMeshCount;for(int i=0;i<mesh.subMeshCount;i++)existing.SetTriangles(mesh.GetTriangles(i),i);existing.RecalculateBounds();Object.DestroyImmediate(mesh);EditorUtility.SetDirty(existing);return existing;}AssetDatabase.CreateAsset(mesh,path);return mesh;}
 }
}


