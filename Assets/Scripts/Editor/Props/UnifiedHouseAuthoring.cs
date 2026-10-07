using System;
using System.Collections.Generic;
using System.Linq;
using HollowCreek.Core.Data;
using HollowCreek.Gameplay.Locations;
using HollowCreek.Gameplay.Interaction;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using Object=UnityEngine.Object;
namespace HollowCreek.Editor.Props
{
 public static class UnifiedHouseAuthoring
 {
  const string Dir="Assets/Art/Environment/UnifiedHouse";
  const string StreetPath="Assets/Scenes/Locations/Street.unity";
  const string HousePath="Assets/Scenes/Locations/House.unity";
  const string InteriorPath=Dir+"/AliceHouseInterior.prefab";
  static readonly Vector3 Offset=new(0,0,12);
  // Front hall windows and study windows, clear of the portrait and mirror.
  static readonly (Vector3 centre,Vector3 outward)[] Windows={
   (new(-3.1f,1.65f,0),Vector3.back),(new(3.1f,1.65f,0),Vector3.back),
   (new(-5,1.65f,8.5f),Vector3.left),(new(-5,1.65f,11.5f),Vector3.left),
   (new(5,1.65f,7.5f),Vector3.right)
  };
  [MenuItem("Hollow Creek/Реквизит/Объединить дом Элис с улицей")]
  public static void Build(){
   if(EditorApplication.isPlaying)throw new InvalidOperationException("Exit Play mode first.");
   var active=SceneManager.GetActiveScene();if(active.isDirty)EditorSceneManager.SaveScene(active);
   System.IO.Directory.CreateDirectory(Dir);AssetDatabase.Refresh();
   var street=EditorSceneManager.OpenScene(StreetPath,OpenSceneMode.Single);
   var root=street.GetRootGameObjects().Single(g=>g.name=="[Location] Street").transform;
   var existing=root.Find("Alice house interior");GameObject stage;
   if(existing)stage=Object.Instantiate(existing.gameObject);
   else {
    var sourceScene=EditorSceneManager.OpenScene(HousePath,OpenSceneMode.Additive);
    var source=sourceScene.GetRootGameObjects().Single(g=>g.GetComponent<LocationRoot>());
    stage=Object.Instantiate(source);SceneManager.MoveGameObjectToScene(stage,street);EditorSceneManager.CloseScene(sourceScene,true);
   }
   stage.name="Alice house interior";
   if(PrefabUtility.IsAnyPrefabInstanceRoot(stage))PrefabUtility.UnpackPrefabInstance(stage,PrefabUnpackMode.OutermostRoot,InteractionMode.AutomatedAction);
   stage.transform.SetParent(null);stage.transform.SetPositionAndRotation(Vector3.zero,Quaternion.identity);stage.transform.localScale=Vector3.one;
   try{
    var moon=stage.transform.Find("Moon");if(moon)Object.DestroyImmediate(moon.gameObject);
    var settings=new SerializedObject(stage.GetComponent<LocationRoot>());
    settings.FindProperty("convertLegacySavedCoordinates").boolValue=true;
    settings.FindProperty("legacySavedBounds").boundsValue=new Bounds(new Vector3(0,1.5f,7),new Vector3(10.3f,7,14.3f));settings.ApplyModifiedPropertiesWithoutUndo();
    foreach(var volume in stage.GetComponentsInChildren<Volume>(true)){
     volume.isGlobal=false;volume.priority=10;volume.blendDistance=.3f;
     var box=volume.GetComponent<BoxCollider>();if(!box)box=volume.gameObject.AddComponent<BoxCollider>();box.isTrigger=true;box.center=new Vector3(0,1.5f,7);box.size=new Vector3(10,3,14);
    }
    CutInteriorWindows(stage.transform);
    var door=stage.transform.Find("Props/Front Door");ConfigureDoor(door.GetComponent<Interactable>());
    var collider=door.GetComponent<BoxCollider>();collider.center=new Vector3(0,.1f,-.08f);collider.size=new Vector3(1.34f,2.4f,.06f);
    var entrance=stage.GetComponent<LocationRoot>().FindSpawnPoint("from_street");entrance.transform.SetPositionAndRotation(new Vector3(0,0,1.25f),Quaternion.identity);
    PrefabUtility.SaveAsPrefabAsset(stage,InteriorPath);
   }finally{Object.DestroyImmediate(stage);}
   if(existing)Object.DestroyImmediate(existing.gameObject);
   var interior=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(InteriorPath),street);
   interior.name="Alice house interior";interior.transform.SetParent(root,false);interior.transform.position=Offset;PrefabUtility.RecordPrefabInstancePropertyModifications(interior.transform);
   var definition=AssetDatabase.LoadAssetAtPath<LocationDefinition>("Assets/Data/Locations/House.asset");var serialized=new SerializedObject(definition);
   serialized.FindProperty("scene").objectReferenceValue=AssetDatabase.LoadAssetAtPath<SceneAsset>(StreetPath);serialized.FindProperty("sceneName").stringValue="Street";serialized.ApplyModifiedPropertiesWithoutUndo();
   ApplyExisting(root);
   EditorBuildSettings.scenes=EditorBuildSettings.scenes.Select(s=>s.path==HousePath?new EditorBuildSettingsScene(s.path,false):s).ToArray();
   AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(street);EditorSceneManager.SaveScene(street);
   // Refresh the diner captures too: they now see the physical house.
   UnifiedDinerAuthoring.Build();
   var view=SceneView.lastActiveSceneView;if(view){view.LookAt(new Vector3(0,1.7f,12.2f),Quaternion.Euler(5,20,0),7,false,true);view.Focus();view.Repaint();}
  }
  static void CutInteriorWindows(Transform stage){
   var geometry=stage.Find("Geometry");
   var left=geometry.Find("Wall West");if(!left)return;var wall=left.GetComponent<Renderer>().sharedMaterial;
   foreach(var name in new[]{"Wall West","Wall East","Wall South L","Wall South R","Wall South Top"}){var old=geometry.Find(name);if(old)Object.DestroyImmediate(old.gameObject);}
   Wall(geometry,"Wall West",0,14,-5,true,new[]{8.5f,11.5f},false,wall,.15f);
   Wall(geometry,"Wall East",0,14,5,true,new[]{7.5f},false,wall,.15f);
   Wall(geometry,"Wall South",-5,5,0,false,new[]{-3.1f,3.1f},true,wall,.15f);
  }
  public static void ApplyExisting(Transform root){
   var interior=root.Find("Alice house interior");if(!interior)return;
   AliceFenceAuthoring.Apply(root);
   if(!interior.Find("Inherited home furnishings"))AliceHomeInteriorAuthoring.Apply(interior);
   System.IO.Directory.CreateDirectory(Dir);
   // Retired facade overlays are siblings of the old model; hide them in the shared environment.
   foreach(var overlay in root.Find("Shared evening street").GetComponentsInChildren<Renderer>(true)
    .Where(r=>r.name.StartsWith("building-type-d lit rooms"))){overlay.enabled=false;PrefabUtility.RecordPrefabInstancePropertyModifications(overlay);}
   // Keep the house floor clear of the street ground at y=0.
   var floor=interior.Find("Geometry/Floor");floor.localPosition=new Vector3(0,-.04f,7);PrefabUtility.RecordPrefabInstancePropertyModifications(floor);
   var facade=root.Find("Shared evening street/Street buildings and trees").Cast<Transform>().Single(t=>t.name=="building-type-d"&&t.position.z>0);
   var originalRenderer=facade.GetComponent<MeshRenderer>();originalRenderer.enabled=false;PrefabUtility.RecordPrefabInstancePropertyModifications(originalRenderer);
   foreach(var child in facade.GetComponentsInChildren<Renderer>(true))if(child!=originalRenderer){child.enabled=false;PrefabUtility.RecordPrefabInstancePropertyModifications(child);}
   foreach(var name in new[]{"Alice House","Alice House Roof"}){
    var greybox=root.Find("Geometry/"+name);if(!greybox)continue;
    foreach(var r in greybox.GetComponentsInChildren<Renderer>(true))r.enabled=false;
    foreach(var c in greybox.GetComponentsInChildren<Collider>(true))c.enabled=false;
   }
   var oldPorch=root.Find("Geometry/Porch");if(oldPorch){foreach(var collider in oldPorch.GetComponents<Collider>())collider.enabled=false;}
   var porch=root.Find("Shared evening street/Street terrain/Porch");if(porch){porch.position=new Vector3(0,-.015f,11.4f);porch.localScale=new Vector3(3,.04f,1.2f);PrefabUtility.RecordPrefabInstancePropertyModifications(porch);}
   var oldShell=root.Find("Physical Alice house exterior");if(oldShell)Object.DestroyImmediate(oldShell.gameObject);
   var shell=new GameObject("Physical Alice house exterior").transform;shell.SetParent(root,false);
   UpperStorey(shell,facade.GetComponent<MeshFilter>(),originalRenderer);
   Box(shell,"Level entrance landing",new Vector3(0,-.015f,11.4f),new Vector3(3,.04f,1.2f),AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/Environment/DinerWindow/StreetNightPavement.mat"));
   var brick=AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/Environment/BuildingFinishes/OldRedBrick.mat");
   Wall(shell,"Front brick",-5.15f,5.15f,11.90f,false,new[]{-3.1f,3.1f},true,brick,.12f);
   Wall(shell,"West brick",11.9f,26.15f,-5.15f,true,new[]{20.5f,23.5f},false,brick,.12f);
   Wall(shell,"East brick",11.9f,26.15f,5.15f,true,new[]{19.5f},false,brick,.12f);
   Wall(shell,"Rear brick",-5.15f,5.15f,26.15f,false,Array.Empty<float>(),false,brick,.12f);
   var streetDoor=root.Find("Props/Alice House Door");ConfigureDoor(streetDoor.GetComponent<Interactable>());
   foreach(var r in streetDoor.GetComponentsInChildren<Renderer>(true)){r.enabled=false;PrefabUtility.RecordPrefabInstancePropertyModifications(r);}
   streetDoor.SetPositionAndRotation(new Vector3(0,1.2f,11.83f),Quaternion.identity);streetDoor.localScale=Vector3.one;
   var box=streetDoor.GetComponent<BoxCollider>();box.center=Vector3.zero;box.size=new Vector3(1.34f,2.4f,.06f);
   root.Find("Spawn (from_house)").SetPositionAndRotation(new Vector3(0,0,10.7f),Quaternion.Euler(0,180,0));
   var panes=new List<Renderer>();
   for(int i=0;i<Windows.Length;i++){
    var (local,outward)=Windows[i];var centre=local+Offset;
    var glass=Glass("Window"+i);var pane=GameObject.CreatePrimitive(PrimitiveType.Quad);pane.name="Physical house window "+(i+1);pane.transform.SetParent(shell,false);
    pane.transform.SetPositionAndRotation(centre+outward*.18f,Quaternion.LookRotation(-outward));pane.transform.localScale=new Vector3(1.55f,1.35f,1);Object.DestroyImmediate(pane.GetComponent<Collider>());
    var renderer=pane.GetComponent<Renderer>();renderer.sharedMaterial=glass;renderer.shadowCastingMode=ShadowCastingMode.Off;panes.Add(renderer);
    var side=Vector3.Cross(outward,Vector3.up);var trim=AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/Environment/BuildingFinishes/WeatheredWhiteSiding.mat");
    for(int sign=-1;sign<=1;sign+=2){
     var post=Box(shell,"Window vertical trim",centre+outward*.20f+side*(sign*.815f),new Vector3(.07f,1.49f,.07f),trim,false);
     Box(shell,"Window horizontal trim",centre+outward*.20f+Vector3.up*(sign*.71f),Mathf.Abs(outward.x)>.5f?new Vector3(.07f,.07f,1.70f):new Vector3(1.70f,.07f,.07f),trim,false);
    }
   }
   foreach(var pane in panes)pane.enabled=false;
   try{for(int i=0;i<panes.Count;i++){
    var cube=DinerExteriorAuthoring.CaptureCube(Dir+"/WindowReflection"+i+".cubemap",panes[i].transform.position+Windows[i].outward*.2f,128,true);
    panes[i].sharedMaterial.SetTexture("_StreetCube",cube);EditorUtility.SetDirty(panes[i].sharedMaterial);
   }}finally{foreach(var pane in panes)pane.enabled=true;}
   foreach(var probe in interior.GetComponentsInChildren<ReflectionProbe>(true)){
    var cube=DinerExteriorAuthoring.CaptureCube(Dir+"/"+(probe.name.Contains("Study")?"Study":"Hall")+".cubemap",probe.transform.position,128,true);
    probe.mode=ReflectionProbeMode.Custom;probe.customBakedTexture=cube;probe.boxProjection=true;PrefabUtility.RecordPrefabInstancePropertyModifications(probe);
   }
   HouseWindowFinishAuthoring.Apply(shell);

   foreach(var c in new Component[]{streetDoor,box,streetDoor.GetComponent<Interactable>(),root.Find("Spawn (from_house)")})PrefabUtility.RecordPrefabInstancePropertyModifications(c);
  }
  static void ConfigureDoor(Interactable door){
   var serialized=new SerializedObject(door);var actions=serialized.FindProperty("actions");
   for(int i=0;i<actions.arraySize;i++){var action=actions.GetArrayElementAtIndex(i);if(action.managedReferenceValue is GoToLocationAction||action.managedReferenceValue is SharedLocationDoorAction)
    action.managedReferenceValue=new SharedLocationDoorAction(AssetDatabase.LoadAssetAtPath<LocationDefinition>("Assets/Data/Locations/House.asset"),AssetDatabase.LoadAssetAtPath<LocationDefinition>("Assets/Data/Locations/Street.asset"),"from_street","from_house");}
   serialized.ApplyModifiedPropertiesWithoutUndo();
  }
  static Material Glass(string name){var path=Dir+"/"+name+".mat";var mat=AssetDatabase.LoadAssetAtPath<Material>(path);if(!mat){mat=new Material(Shader.Find("HollowCreek/Physical Diner Glass"));AssetDatabase.CreateAsset(mat,path);}mat.SetFloat("_Reflection",.025f);return mat;}
  static GameObject Box(Transform parent,string name,Vector3 centre,Vector3 size,Material material,bool collider=true){
   var go=GameObject.CreatePrimitive(PrimitiveType.Cube);go.name=name;go.transform.SetParent(parent,false);go.transform.localPosition=centre;go.transform.localScale=size;go.GetComponent<Renderer>().sharedMaterial=material;if(!collider)Object.DestroyImmediate(go.GetComponent<Collider>());return go;
  }
  static void Wall(Transform parent,string name,float start,float end,float constant,bool alongZ,float[] windows,bool door,Material material,float thickness){
   var holes=windows.Select(c=>(centre:c,width:1.55f,bottom:.975f,top:2.325f)).ToList();if(door)holes.Add((0,1.4f,0,2.4f));holes.Sort((a,b)=>a.centre.CompareTo(b.centre));
   void Segment(float a,float b,float bottom,float top){if(b-a<.001f||top-bottom<.001f)return;Box(parent,name,alongZ?new Vector3(constant,(bottom+top)/2,(a+b)/2):new Vector3((a+b)/2,(bottom+top)/2,constant),alongZ?new Vector3(thickness,top-bottom,b-a):new Vector3(b-a,top-bottom,thickness),material);}
   float previous=start;foreach(var hole in holes){float a=hole.centre-hole.width/2,b=hole.centre+hole.width/2;Segment(previous,a,0,3.1f);Segment(a,b,0,hole.bottom);Segment(a,b,hole.top,3.1f);previous=b;}Segment(previous,end,0,3.1f);
  }
  struct Vertex{public Vector3 p,n;public Vector2 uv;public static Vertex Lerp(Vertex a,Vertex b,float t)=>new(){p=Vector3.Lerp(a.p,b.p,t),n=Vector3.Lerp(a.n,b.n,t).normalized,uv=Vector2.Lerp(a.uv,b.uv,t)};}
  static void UpperStorey(Transform parent,MeshFilter source,Renderer renderer){
   var mesh=source.sharedMesh;var bounds=renderer.bounds;var v=mesh.vertices;var n=mesh.normals;var uv=mesh.uv;var positions=new List<Vector3>();var normals=new List<Vector3>();var tex=new List<Vector2>();var triangles=new List<int[]>();
   Vertex Read(int i){var p=source.transform.TransformPoint(v[i]);p.x=Mathf.Lerp(-5.15f,5.15f,Mathf.InverseLerp(bounds.min.x,bounds.max.x,p.x));p.z=Mathf.Lerp(11.9f,26.15f,Mathf.InverseLerp(bounds.min.z,bounds.max.z,p.z));return new Vertex{p=p,n=source.transform.TransformDirection(n[i]),uv=uv[i]};}
   for(int sub=0;sub<mesh.subMeshCount;sub++){var indices=new List<int>();var original=mesh.GetTriangles(sub);
    for(int t=0;t<original.Length;t+=3){var input=new[]{Read(original[t]),Read(original[t+1]),Read(original[t+2])};var polygon=new List<Vertex>();
     for(int e=0;e<3;e++){var a=input[e];var b=input[(e+1)%3];bool ia=a.p.y>=3.08f,ib=b.p.y>=3.08f;if(ia)polygon.Add(a);if(ia!=ib)polygon.Add(Vertex.Lerp(a,b,(3.08f-a.p.y)/(b.p.y-a.p.y)));}
     for(int j=1;j+1<polygon.Count;j++)foreach(var point in new[]{polygon[0],polygon[j],polygon[j+1]}){indices.Add(positions.Count);positions.Add(point.p);normals.Add(point.n);tex.Add(point.uv);}}
    triangles.Add(indices.ToArray());}
   var built=new Mesh{name="Alice house upper storey",indexFormat=IndexFormat.UInt32};built.SetVertices(positions);built.SetNormals(normals);built.SetUVs(0,tex);built.subMeshCount=triangles.Count;for(int i=0;i<triangles.Count;i++)built.SetTriangles(triangles[i],i);built.RecalculateBounds();built.RecalculateTangents();
   var path=Dir+"/UpperStorey.asset";var stored=AssetDatabase.LoadAssetAtPath<Mesh>(path);if(stored){EditorUtility.CopySerialized(built,stored);Object.DestroyImmediate(built);EditorUtility.SetDirty(stored);}else{stored=built;AssetDatabase.CreateAsset(stored,path);}
   var go=new GameObject("Original house upper storey and roof");go.transform.SetParent(parent,false);go.AddComponent<MeshFilter>().sharedMesh=stored;go.AddComponent<MeshRenderer>().sharedMaterials=renderer.sharedMaterials;
  }
 }
}







