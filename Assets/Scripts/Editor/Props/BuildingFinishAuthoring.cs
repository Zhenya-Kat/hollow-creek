using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object=UnityEngine.Object;

namespace HollowCreek.Editor.Props
{
 public static class BuildingFinishAuthoring
 {
  const string Directory="Assets/Art/Environment/BuildingFinishes";
  [MenuItem("Hollow Creek/Реквизит/Сайдинг и кирпич зданий")]
  public static void Build()
  {
   if(EditorApplication.isPlaying)throw new InvalidOperationException("Exit Play mode first.");
   var scene=EditorSceneManager.GetActiveScene();if(scene.isDirty)EditorSceneManager.SaveScene(scene);
   scene=EditorSceneManager.OpenScene("Assets/Scenes/Locations/Street.unity",OpenSceneMode.Single);
   System.IO.Directory.CreateDirectory(Directory);AssetDatabase.Refresh();
   var white=Material("WeatheredWhiteSiding",new Color(0.8f,0.79f,0.75f),new Color(0.36f,0.32f,0.26f),true);
   var red=Material("OldRedBrick",new Color(0.49f,0.205f,0.14f),new Color(0.32f,0.30f,0.27f),false);
   var grey=Material("OldGreyBrick",new Color(0.39f,0.405f,0.40f),new Color(0.29f,0.285f,0.26f),false);
   var root=scene.GetRootGameObjects().Single(g=>g.name=="[Location] Street").transform;
   Apply(root,white,red,grey);
   AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
   var view=SceneView.lastActiveSceneView;if(view){view.LookAt(new Vector3(19,3.2f,11),Quaternion.Euler(4,20,0),7.0f,false,true);view.Focus();view.Repaint();}
  }
  internal static void ApplyExisting(Transform root)
  {
   var white=AssetDatabase.LoadAssetAtPath<Material>(Directory+"/WeatheredWhiteSiding.mat");
   if(!white)return;
   Apply(root,white,AssetDatabase.LoadAssetAtPath<Material>(Directory+"/OldRedBrick.mat"),AssetDatabase.LoadAssetAtPath<Material>(Directory+"/OldGreyBrick.mat"));
  }
  static void Apply(Transform root,Material white,Material red,Material grey)
  {
   var shared=root.Find("Shared evening street");
   var houses=shared.GetComponentsInChildren<MeshRenderer>()
    .Where(r=>r.enabled&&((r.name.StartsWith("building-type-")&&r.transform.parent.name=="Street buildings and trees")||r.name=="House silhouette opposite diner"))
    .OrderBy(r=>r.bounds.center.x).ThenBy(r=>r.bounds.center.z).ToArray();
   foreach(var house in houses)Finish(house,Mathf.FloorToInt(house.bounds.center.x/10)%2==0?grey:red,false);
   foreach(var crypt in shared.GetComponentsInChildren<MeshRenderer>().Where(r=>r.enabled&&r.name=="crypt"))Finish(crypt,grey,false);
   var shell=root.Find("Physical diner exterior");
   foreach(var renderer in shell.GetComponentsInChildren<MeshRenderer>())
   {
    if(renderer.name=="Original upper storey and roof")Finish(renderer,white,true);
    else if(new[]{"Front ","Rear ","Left ","Right "}.Any(prefix=>renderer.name.StartsWith(prefix)))
     renderer.sharedMaterial=white;
   }
   foreach(var renderer in root.GetComponentsInChildren<MeshRenderer>(true))
   {
    PrefabUtility.RecordPrefabInstancePropertyModifications(renderer);
    var filter=renderer.GetComponent<MeshFilter>();if(filter)PrefabUtility.RecordPrefabInstancePropertyModifications(filter);
   }
  }
  static void Finish(MeshRenderer renderer,Material finish,bool diner)
  {
   var filter=renderer.GetComponent<MeshFilter>();var mesh=filter.sharedMesh;
   if(mesh.name.EndsWith(" wall finish")){var mats=renderer.sharedMaterials;mats[mats.Length-1]=finish;renderer.sharedMaterials=mats;return;}
   var texture=renderer.sharedMaterial.mainTexture as Texture2D;
   if(!texture)throw new InvalidOperationException("Missing atlas on "+renderer.name);
   var vertices=mesh.vertices;var uv=mesh.uv;var indices=new List<int[]>();var wall=new List<int>();
   float cryptColumn=-1,largest=0;
   if(renderer.name=="crypt")
   {
    var tris=mesh.triangles;
    for(var i=0;i<tris.Length;i+=3)
    {
     var cross=Vector3.Cross(vertices[tris[i+1]]-vertices[tris[i]],vertices[tris[i+2]]-vertices[tris[i]]);
     if(Mathf.Abs(cross.normalized.y)<0.08f&&cross.magnitude>largest)
     {largest=cross.magnitude;cryptColumn=(uv[tris[i]].x+uv[tris[i+1]].x+uv[tris[i+2]].x)/3;}
    }
   }
   for(var sub=0;sub<mesh.subMeshCount;sub++)
   {
    var keep=new List<int>();var triangles=mesh.GetTriangles(sub);
    for(var j=0;j<triangles.Length;j+=3)
    {
     int a=triangles[j],b=triangles[j+1],c=triangles[j+2];
     var n=Vector3.Cross(vertices[b]-vertices[a],vertices[c]-vertices[a]).normalized;
     var centre=(uv[a]+uv[b]+uv[c])/3;
     // Kenney's atlas uses a pale column for plaster walls and a dark column
     // for this diner's upper walls. Windows, roof and trim have other swatches.
     bool column=centre.x>0.43f&&centre.x<0.51f;
     if(diner)column|=centre.x>0.18f&&centre.x<0.26f;
     bool isWall=Mathf.Abs(n.y)<0.08f&&column&&centre.y>0.29f&&centre.y<0.46f;
     if(cryptColumn>=0)isWall=Mathf.Abs(n.y)<0.08f&&Mathf.Abs(centre.x-cryptColumn)<0.025f;
     (isWall?wall:keep).AddRange(new[]{a,b,c});
    }
    indices.Add(keep.ToArray());
   }
   if(wall.Count==0)throw new InvalidOperationException("No walls selected: "+renderer.name);
   var built=Object.Instantiate(mesh);built.name=mesh.name+" wall finish";built.subMeshCount=indices.Count+1;
   for(var i=0;i<indices.Count;i++)built.SetTriangles(indices[i],i);built.SetTriangles(wall,indices.Count);
   var path=Directory+"/"+(diner?"DinerUpper":mesh.name)+".asset";var stored=AssetDatabase.LoadAssetAtPath<Mesh>(path);
   if(stored){EditorUtility.CopySerialized(built,stored);Object.DestroyImmediate(built);EditorUtility.SetDirty(stored);}
   else{stored=built;AssetDatabase.CreateAsset(stored,path);}
   filter.sharedMesh=stored;renderer.sharedMaterials=renderer.sharedMaterials.Concat(new[]{finish}).ToArray();
  }
  static Material Material(string name,Color colour,Color mortar,bool siding)
  {
   var shader=Shader.Find("HollowCreek/Weathered Building Finish");
   if(!shader||ShaderUtil.ShaderHasError(shader))throw new InvalidOperationException("Building finish shader failed.");
   var path=Directory+"/"+name+".mat";var material=AssetDatabase.LoadAssetAtPath<Material>(path);
   if(!material){material=new Material(shader);AssetDatabase.CreateAsset(material,path);}
   material.shader=shader;material.SetColor("_BaseColor",colour);material.SetColor("_MortarColor",mortar);
   material.SetFloat("_Siding",siding?1:0);material.SetFloat("_Weathering",siding?0.32f:0.28f);
   EditorUtility.SetDirty(material);return material;
  }
 }
}

