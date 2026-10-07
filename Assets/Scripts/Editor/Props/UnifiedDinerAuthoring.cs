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
using Object = UnityEngine.Object;

namespace HollowCreek.Editor.Props
{
    public static class UnifiedDinerAuthoring
    {
        const string Directory = "Assets/Art/Environment/DinerWindow/Unified";
        public const string InteriorPrefabPath = Directory + "/DinerInterior.prefab";
        const string StreetPath = "Assets/Scenes/Locations/Street.unity";
        const string DinerPath = "Assets/Scenes/Locations/Diner.unity";
        const string ShellName = "Physical diner exterior";
        static readonly Quaternion Rotation = Quaternion.Euler(0,-90,0);
        static readonly Vector3 Offset = new(23.15f,0,15.92f);
        static readonly Vector3[] Centres = {
            new(-5.95f,1.6f,2.5f),new(-5.95f,1.6f,5.25f),new(-5.95f,1.6f,8),
            new(5.95f,1.6f,2.5f),new(5.95f,1.6f,5.5f),new(-3.25f,1.6f,9.95f)
        };

        [MenuItem("Hollow Creek/Реквизит/Общая сцена улицы и закусочной")]
        public static void Build()
        {
            if(EditorApplication.isPlaying)throw new InvalidOperationException("Exit Play mode first.");
            var active=SceneManager.GetActiveScene();
            if(active.isDirty)EditorSceneManager.SaveScene(active);
            System.IO.Directory.CreateDirectory(Directory); AssetDatabase.Refresh();
            var existing=AssetDatabase.LoadAssetAtPath<GameObject>(InteriorPrefabPath);
            var diner=EditorSceneManager.OpenScene(existing?StreetPath:DinerPath,OpenSceneMode.Single);
            var source=existing?diner.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<LocationRoot>(true))
                .Select(r=>r.gameObject).SingleOrDefault(g=>g.name=="Diner interior")
                :diner.GetRootGameObjects().Single(g=>g.GetComponent<LocationRoot>());
            if(!source)source=existing;
            var stage=Object.Instantiate(source); stage.name="Diner interior";
            if(PrefabUtility.IsAnyPrefabInstanceRoot(stage))
                PrefabUtility.UnpackPrefabInstance(stage,PrefabUnpackMode.OutermostRoot,InteractionMode.AutomatedAction);
            stage.transform.SetParent(null);stage.transform.SetPositionAndRotation(Vector3.zero,Quaternion.identity);
            stage.transform.localScale=Vector3.one;
            try
            {
                var rootSettings=new SerializedObject(stage.GetComponent<LocationRoot>());
                rootSettings.FindProperty("convertLegacySavedCoordinates").boolValue=true;
                rootSettings.FindProperty("legacySavedBounds").boundsValue=new Bounds(new Vector3(0,1.5f,5),new Vector3(13,7,11));
                rootSettings.ApplyModifiedPropertiesWithoutUndo();
                ConfigureDoor(stage.transform.Find("Props/Front Door").GetComponent<Interactable>());
                var insideCollider=stage.transform.Find("Props/Front Door").GetComponent<BoxCollider>();
                insideCollider.center=new Vector3(0,0.1f,-0.08f);
                insideCollider.size=new Vector3(1.34f,2.4f,0.06f);
                foreach(var name in new[]{"Moon","Post Processing"})
                {
                    var child=stage.transform.Find(name);
                    if(name=="Moon" && child)Object.DestroyImmediate(child.gameObject);
                }
                foreach(var t in stage.GetComponentsInChildren<Transform>(true)
                    .Where(t=>t.name=="SharedEveningStreet"||t.name=="Shared evening street"
                        ||(PrefabUtility.IsAnyPrefabInstanceRoot(t.gameObject)
                        && PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(t.gameObject)==EveningStreetAuthoring.PrefabPath)).ToArray())
                    Object.DestroyImmediate(t.gameObject);
                foreach(var probe in stage.GetComponentsInChildren<ReflectionProbe>(true))Object.DestroyImmediate(probe.gameObject);
                foreach(var renderer in stage.GetComponentsInChildren<SkinnedMeshRenderer>(true))renderer.updateWhenOffscreen=true;
                foreach(var volume in stage.GetComponentsInChildren<Volume>(true))
                {
                    volume.isGlobal=false; volume.priority=10; volume.blendDistance=0.3f;
                    var bounds=volume.GetComponent<BoxCollider>();if(!bounds)bounds=volume.gameObject.AddComponent<BoxCollider>(); bounds.isTrigger=true;
                    bounds.center=new Vector3(0,1.6f,5); bounds.size=new Vector3(12,3.2f,10);
                }
                PrefabUtility.SaveAsPrefabAsset(stage,InteriorPrefabPath);
            }
            finally { Object.DestroyImmediate(stage); }

            var street=EditorSceneManager.OpenScene(StreetPath,OpenSceneMode.Single);
            var root=street.GetRootGameObjects().Single(g=>g.GetComponent<LocationRoot>()).transform;
            foreach(var name in new[]{"Diner interior",ShellName})
            {
                var old=root.Find(name); if(old)Object.DestroyImmediate(old.gameObject);
            }
            var interior=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(InteriorPrefabPath),street);
            interior.name="Diner interior"; interior.transform.SetParent(root,false);
            interior.transform.SetPositionAndRotation(Offset,Rotation);
            PrefabUtility.RecordPrefabInstancePropertyModifications(interior.transform);
            var greybox=root.Find("Geometry/Diner");
            foreach(var collider in greybox.GetComponents<Collider>())collider.enabled=false;
            foreach(var renderer in greybox.GetComponents<Renderer>())renderer.enabled=false;
            var facade=root.Find("Shared evening street/Street buildings and trees/Diner facade");
            if(!facade)throw new InvalidOperationException("The shared street facade is missing.");
            facade.GetComponent<Renderer>().enabled=false;
            var oldWindows=facade.Find("Matched downstairs diner windows");
            if(oldWindows)oldWindows.gameObject.SetActive(false);

            var shell=new GameObject(ShellName).transform; shell.SetParent(root,false);
            UpperStorey(shell,facade.GetComponent<MeshFilter>(),facade.GetComponent<Renderer>());
            var paint=AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/Environment/DinerWindow/Facade/DinerExteriorPaint.mat");
            // One physical door on the room's east-facing wall opens straight outside.
            Wall(shell,"Front",13.05f,23.25f,9.80f,false,new[]{15.15f,17.9f,20.65f},null,paint);
            Wall(shell,"Rear",13.05f,23.25f,22.04f,false,new[]{17.65f,20.65f},null,paint);
            Wall(shell,"Left",9.8f,22.04f,13.05f,true,new[]{12.67f},null,paint);
            Wall(shell,"Right",9.8f,22.04f,23.25f,true,Array.Empty<float>(),15.92f,paint);
            var pavement=AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/Environment/DinerWindow/StreetNightPavement.mat");
            Box(shell,"Path to direct diner entrance",new Vector3(24.05f,-0.017f,12.92f),new Vector3(1.65f,0.05f,7),pavement,true);
            // The interior model is visible from both sides; no second door or vestibule.
            var streetDoor=root.Find("Props/Diner Door");
            var previousDoor=streetDoor.Find("Physical entrance door");
            if(previousDoor)Object.DestroyImmediate(previousDoor.gameObject);
            foreach(var renderer in streetDoor.GetComponentsInChildren<Renderer>(true))renderer.enabled=false;
            streetDoor.SetPositionAndRotation(new Vector3(23.32f,1.2f,15.92f),Rotation);
            ConfigureDoor(streetDoor.GetComponent<Interactable>());
            var outsideCollider=streetDoor.GetComponent<BoxCollider>();
            outsideCollider.center=Vector3.zero;outsideCollider.size=new Vector3(1,0.923077f,0.3f);
            var outsideSpawn=root.Find("Spawn (from_diner)");
            outsideSpawn.SetPositionAndRotation(new Vector3(24.5f,0,15.92f),Quaternion.Euler(0,90,0));
            // The diner door's existing model is the definitive matching template.
            var template=interior.transform.Find("Props/Front Door/Model");
            if(!template)throw new InvalidOperationException("Interior door model is missing.");
            BuildingFinishAuthoring.ApplyExisting(root);
            StreetNaturalAuthoring.ApplyExisting(root);
            UnifiedHouseAuthoring.ApplyExisting(root);
            var panes=new List<Renderer>();
            for(var i=0;i<Centres.Length;i++)
            {
                var inward=i<3?Vector3.right:i<5?Vector3.left:Vector3.back;
                var centre=Offset+Rotation*Centres[i];var outward=Rotation*-inward;
                var material=Glass("PhysicalWindow"+(i+1));
                var pane=GameObject.CreatePrimitive(PrimitiveType.Quad);pane.name="Transparent window "+(i+1);
                pane.transform.SetParent(shell,false);pane.transform.SetPositionAndRotation(centre+outward*0.04f,Quaternion.LookRotation(-outward));
                pane.transform.localScale=new Vector3(2.2f,1.4f,1);Object.DestroyImmediate(pane.GetComponent<Collider>());
                var renderer=pane.GetComponent<Renderer>();renderer.sharedMaterial=material;renderer.shadowCastingMode=ShadowCastingMode.Off;
                panes.Add(renderer);
            }
            var doorMaterial=Glass("PhysicalDoor");
            foreach(var door in new[]{template})
                foreach(var renderer in door.GetComponentsInChildren<Renderer>(true))
                {
                    var mats=renderer.sharedMaterials;
                    for(var i=0;i<mats.Length;i++)
                        if(mats[i]&&(mats[i].name.Contains("Glass")||mats[i].shader.name=="HollowCreek/Physical Diner Glass"))
                            mats[i]=doorMaterial;
                    renderer.sharedMaterials=mats;
                }
            foreach(var pane in panes)pane.enabled=false;
            try
            {
                for(var i=0;i<panes.Count;i++)
                {
                    var centre=panes[i].transform.position;
                    var outward=-panes[i].transform.forward;
                    var cube=DinerExteriorAuthoring.CaptureCube(Directory+"/WindowReflection"+(i+1)+".cubemap",centre+outward*0.18f,128,true);
                    panes[i].sharedMaterial.SetTexture("_StreetCube",cube);EditorUtility.SetDirty(panes[i].sharedMaterial);
                }
                doorMaterial.SetTexture("_StreetCube",DinerExteriorAuthoring.CaptureCube(Directory+"/DoorReflection.cubemap",new Vector3(23.55f,1.6f,15.92f),128,true));
                EditorUtility.SetDirty(doorMaterial);
            }
            finally { foreach(var pane in panes)if(pane)pane.enabled=true; }
            var definition=AssetDatabase.LoadAssetAtPath<LocationDefinition>("Assets/Data/Locations/Diner.asset");
            var serialized=new SerializedObject(definition);
            serialized.FindProperty("scene").objectReferenceValue=AssetDatabase.LoadAssetAtPath<SceneAsset>(StreetPath);
            serialized.FindProperty("sceneName").stringValue="Street";serialized.ApplyModifiedPropertiesWithoutUndo();
            // Local location roots retain dialogue, ambience and the original door actions.
            foreach(var renderer in interior.GetComponentsInChildren<Renderer>(true))PrefabUtility.RecordPrefabInstancePropertyModifications(renderer);
            foreach(var transform in new[]{facade,oldWindows,greybox,streetDoor})
                if(transform)foreach(var component in transform.GetComponents<Component>())PrefabUtility.RecordPrefabInstancePropertyModifications(component);
            AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(street);EditorSceneManager.SaveScene(street);
            EditorBuildSettings.scenes=EditorBuildSettings.scenes.Select(s=>s.path==DinerPath?new EditorBuildSettingsScene(s.path,false):s).ToArray();
            var view=SceneView.lastActiveSceneView;
            if(view){view.LookAt(new Vector3(19.4f,1.6f,10),Quaternion.Euler(0,20,0),4.4f,false,true);view.Focus();view.Repaint();}
        }

        static void ConfigureDoor(Interactable door)
        {
            var serialized=new SerializedObject(door);
            var actions=serialized.FindProperty("actions");
            for(var i=0;i<actions.arraySize;i++)
            {
                var action=actions.GetArrayElementAtIndex(i);
                if(action.managedReferenceValue is GoToLocationAction || action.managedReferenceValue is SharedLocationDoorAction)
                    action.managedReferenceValue=new SharedLocationDoorAction(
                        AssetDatabase.LoadAssetAtPath<LocationDefinition>("Assets/Data/Locations/Diner.asset"),
                        AssetDatabase.LoadAssetAtPath<LocationDefinition>("Assets/Data/Locations/Street.asset"),"from_street","from_diner");
            }
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        static Material Glass(string name)
        {
            var path=Directory+"/"+name+".mat";var material=AssetDatabase.LoadAssetAtPath<Material>(path);
            var shader=Shader.Find("HollowCreek/Physical Diner Glass");
            if(!shader||ShaderUtil.ShaderHasError(shader))throw new InvalidOperationException("Physical glass shader failed.");
            if(!material){material=new Material(shader);AssetDatabase.CreateAsset(material,path);}
            material.shader=shader;material.SetFloat("_Reflection",0.025f);EditorUtility.SetDirty(material);return material;
        }

        static void Wall(Transform parent,string name,float start,float end,float constant,bool alongZ,float[] windows,float? door,Material material)
        {
            var holes=windows.Select(c=>(centre:c,width:2.2f,bottom:0.9f,top:2.3f)).ToList();
            if(door.HasValue)holes.Add((door.Value,1.4f,0,2.42f));
            holes.Sort((a,b)=>a.centre.CompareTo(b.centre));
            void Section(string suffix,float a,float b,float y0,float y1)
            {
                if(b-a<0.001f||y1-y0<0.001f)return;
                var p=alongZ?new Vector3(constant,(y0+y1)/2,(a+b)/2):new Vector3((a+b)/2,(y0+y1)/2,constant);
                var size=alongZ?new Vector3(0.06f,y1-y0,b-a):new Vector3(b-a,y1-y0,0.06f);
                Box(parent,name+" "+suffix,p,size,material,true);
            }
            var previous=start;
            foreach(var hole in holes)
            {
                var a=hole.centre-hole.width/2;var b=hole.centre+hole.width/2;
                Section("pier",previous,a,0,3.35f);Section("sill",a,b,0,hole.bottom);Section("lintel",a,b,hole.top,3.35f);previous=b;
            }
            Section("end",previous,end,0,3.35f);
        }

        static void Box(Transform parent,string name,Vector3 position,Vector3 size,Material material,bool collider)
        {
            var go=GameObject.CreatePrimitive(PrimitiveType.Cube);go.name=name;go.transform.SetParent(parent,false);
            go.transform.position=position;go.transform.localScale=size;go.GetComponent<Renderer>().sharedMaterial=material;
            if(!collider)Object.DestroyImmediate(go.GetComponent<Collider>());
        }

        struct Vertex
        {
            public Vector3 p,n;public Vector2 uv;
            public static Vertex Lerp(Vertex a,Vertex b,float t)=>new(){p=Vector3.Lerp(a.p,b.p,t),n=Vector3.Lerp(a.n,b.n,t).normalized,uv=Vector2.Lerp(a.uv,b.uv,t)};
        }

        static void UpperStorey(Transform parent,MeshFilter source,Renderer renderer)
        {
            var mesh=source.sharedMesh;var bounds=renderer.bounds;
            var vertices=mesh.vertices;var normals=mesh.normals;var uv=mesh.uv;
            var positions=new List<Vector3>();var newNormals=new List<Vector3>();var newUv=new List<Vector2>();var triangles=new List<int[]>();
            Vertex Read(int i)
            {
                var p=source.transform.TransformPoint(vertices[i]);
                p.x=Mathf.Lerp(13.05f,23.25f,Mathf.InverseLerp(bounds.min.x,bounds.max.x,p.x));
                p.z=Mathf.Lerp(9.80f,22.04f,Mathf.InverseLerp(bounds.min.z,bounds.max.z,p.z));
                return new Vertex{p=p,n=source.transform.TransformDirection(normals[i]),uv=uv[i]};
            }
            for(var sub=0;sub<mesh.subMeshCount;sub++)
            {
                var indices=new List<int>();var original=mesh.GetTriangles(sub);
                for(var t=0;t<original.Length;t+=3)
                {
                    var input=new[]{Read(original[t]),Read(original[t+1]),Read(original[t+2])};var polygon=new List<Vertex>();
                    for(var edge=0;edge<3;edge++)
                    {
                        var a=input[edge];var b=input[(edge+1)%3];bool insideA=a.p.y>=3.32f,insideB=b.p.y>=3.32f;
                        if(insideA)polygon.Add(a);
                        if(insideA!=insideB)polygon.Add(Vertex.Lerp(a,b,(3.32f-a.p.y)/(b.p.y-a.p.y)));
                    }
                    for(var j=1;j+1<polygon.Count;j++)foreach(var v in new[]{polygon[0],polygon[j],polygon[j+1]})
                    {indices.Add(positions.Count);positions.Add(v.p);newNormals.Add(v.n);newUv.Add(v.uv);}
                }
                triangles.Add(indices.ToArray());
            }
            var built=new Mesh{name="Diner upper storey with open ground floor",indexFormat=IndexFormat.UInt32};
            built.SetVertices(positions);built.SetNormals(newNormals);built.SetUVs(0,newUv);built.subMeshCount=triangles.Count;
            for(var i=0;i<triangles.Count;i++)built.SetTriangles(triangles[i],i);built.RecalculateBounds();built.RecalculateTangents();
            var path=Directory+"/UpperStorey.asset";var stored=AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if(stored){EditorUtility.CopySerialized(built,stored);Object.DestroyImmediate(built);EditorUtility.SetDirty(stored);}else{stored=built;AssetDatabase.CreateAsset(stored,path);}
            var go=new GameObject("Original upper storey and roof");go.transform.SetParent(parent,false);
            go.AddComponent<MeshFilter>().sharedMesh=stored;go.AddComponent<MeshRenderer>().sharedMaterials=renderer.sharedMaterials;
        }
    }
}


