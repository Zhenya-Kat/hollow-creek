using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace HollowCreek.Editor.Props
{
    public static class DinerExteriorAuthoring
    {
        const string Directory = "Assets/Art/Environment/DinerWindow/Facade";
        const string DinerPath = "Assets/Scenes/Locations/Diner.unity";
        const string StreetPath = "Assets/Scenes/Locations/Street.unity";
        const string WindowsPrefab = Directory + "/MatchedExteriorWindows.prefab";
        const string FramePrefab = Directory + "/MatchingWindowFrame.prefab";
        const string CubePath = Directory + "/StreetAtDinerEntrance.cubemap";
        const string GroupName = "Matched downstairs diner windows";
        static readonly Vector3[] InteriorCentres = {
            new(-5.95f,1.6f,2.5f), new(-5.95f,1.6f,5.25f), new(-5.95f,1.6f,8),
            new(5.95f,1.6f,2.5f), new(5.95f,1.6f,5.5f), new(-3.25f,1.6f,9.95f)
        };

        [MenuItem("Hollow Creek/Реквизит/Наружные окна и стекло двери закусочной")]
        public static void Build()
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(UnifiedDinerAuthoring.InteriorPrefabPath))
            {
                UnifiedDinerAuthoring.Build();
                return;
            }
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Exit Play mode before updating the diner exterior.");
            var current = SceneManager.GetActiveScene();
            if (current.isDirty) EditorSceneManager.SaveScene(current);
            var diner = current.path == DinerPath ? current : EditorSceneManager.OpenScene(DinerPath, OpenSceneMode.Single);
            System.IO.Directory.CreateDirectory(Directory); AssetDatabase.Refresh();
            var window = GameObject.Find("Diner Window").transform;
            var frame = window.parent.Find("Diner Window Frame");
            var frameOffset = frame.position - window.position;
            var frameRotation = frame.rotation;
            var frameScale = frame.lossyScale;
            var template = Object.Instantiate(frame.gameObject);
            try
            {
                template.name = "Matching wooden window frame";
                foreach (var collider in template.GetComponentsInChildren<Collider>(true)) Object.DestroyImmediate(collider);
                template.transform.SetPositionAndRotation(Vector3.zero, frameRotation);
                template.transform.localScale = frameScale;
                PrefabUtility.SaveAsPrefabAsset(template, FramePrefab);
            }
            finally { Object.DestroyImmediate(template); }
            foreach (var name in new[] { "NPC Mara", "NPC Owen" })
            {
                var animator = GameObject.Find(name).GetComponentInChildren<Animator>();
                if (animator && animator.runtimeAnimatorController)
                    animator.runtimeAnimatorController.animationClips.First(c => c.name.Contains("Idle")).SampleAnimation(animator.gameObject, 0);
            }
            var exteriorInstances = diner.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Transform>(true))
                .Where(t => t.gameObject.activeSelf && PrefabUtility.IsAnyPrefabInstanceRoot(t.gameObject)
                    && PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(t.gameObject) == EveningStreetAuthoring.PrefabPath)
                .Select(t=>t.gameObject).ToArray();
            foreach (var exterior in exteriorInstances) exterior.SetActive(false);
            try
            {
                for (var i = 0; i < InteriorCentres.Length; i++)
                {
                    CaptureInterior(i, InteriorCentres[i], InteriorDirection(i));
                    CaptureCube(Directory + "/InteriorWindow" + (i + 1) + ".cubemap",
                        InteriorCentres[i] + InteriorDirection(i) * 0.12f, 512, true);
                    CaptureInteriorDepth(diner, i, InteriorCentres[i] + InteriorDirection(i) * 0.12f);
                }
            }
            finally { foreach (var exterior in exteriorInstances) if (exterior) exterior.SetActive(true); }
            if (diner.isDirty) EditorSceneManager.SaveScene(diner);

            var street = EditorSceneManager.OpenScene(StreetPath, OpenSceneMode.Single);
            var streetRoot = street.GetRootGameObjects().Single(g => g.name == "[Location] Street").transform;
            var obsoleteWindow = streetRoot.Find("Geometry/Diner Window").GetComponent<Renderer>();
            obsoleteWindow.enabled = false;
            var door = streetRoot.Find("Props/Diner Door");
            var cube = CaptureStreet(door.position + new Vector3(0, 0.25f, -0.42f));
            var contents = PrefabUtility.LoadPrefabContents(EveningStreetAuthoring.PrefabPath);
            try
            {
                var facade = contents.transform.Find("Street buildings and trees/Diner facade");
                var bounds = facade.GetComponent<Renderer>().bounds;
                var anchor = contents.transform.Find("Diner exterior window anchor").position;
                var old = facade.Find(GroupName);
                if (old) Object.DestroyImmediate(old.gameObject);
                var stage = new GameObject(GroupName);
                try
                {
                    // Keep the original upper storey and entrance. The downstairs cladding
                    // covers the imported window pattern and receives the six matching frames.
                    var paint = PaintMaterial();
                    Cladding(stage.transform, bounds, door.position.x, paint);
                    var centres = new[] {
                        new Vector3(anchor.x,1.6f,bounds.min.z-0.055f),
                        new Vector3(anchor.x-2.75f,1.6f,bounds.min.z-0.055f),
                        new Vector3(anchor.x-5.5f,1.6f,bounds.min.z-0.055f),
                        new Vector3(anchor.x,1.6f,bounds.max.z+0.055f),
                        new Vector3(anchor.x-3,1.6f,bounds.max.z+0.055f),
                        new Vector3(bounds.min.x-0.055f,1.6f,anchor.z+2.7f)
                    };
                    // Each pane sees the street from its own location, including rear and side views.
                    // Disable the old panes while capturing to avoid recursively reflecting stale glass.
                    var streetGlass = streetRoot.GetComponentsInChildren<Renderer>(true)
                        .Where(r => r.enabled && r.sharedMaterials.Any(m => m && m.shader.name == "HollowCreek/Diner Street Glass"))
                        .ToArray();
                    foreach (var renderer in streetGlass) renderer.enabled = false;
                    var reflections = new Cubemap[centres.Length];
                    try
                    {
                        for (var i = 0; i < centres.Length; i++)
                        {
                            var yaw = i < 3 ? 0 : i < 5 ? 180 : 90;
                            var outward = Quaternion.Euler(0,yaw,0) * Vector3.back;
                            reflections[i] = CaptureCube(Directory + "/StreetAtWindow" + (i + 1) + ".cubemap",
                                centres[i] + outward * 0.15f, 128, true);
                        }
                    }
                    finally { foreach (var renderer in streetGlass) if (renderer) renderer.enabled = true; }
                    for (var i = 0; i < centres.Length; i++)
                        Window(stage.transform, i, centres[i], i < 3 ? 0 : i < 5 ? 180 : 90, frameOffset, frameRotation, frameScale, reflections[i]);
                    var prefab = PrefabUtility.SaveAsPrefabAsset(stage, WindowsPrefab);
                    var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, contents.scene);
                    instance.name = GroupName;
                    instance.transform.SetParent(facade, true);
                    PrefabUtility.RecordPrefabInstancePropertyModifications(instance);
                    PrefabUtility.SaveAsPrefabAsset(contents, EveningStreetAuthoring.PrefabPath);
                }
                finally { Object.DestroyImmediate(stage); }
            }
            finally { PrefabUtility.UnloadPrefabContents(contents); }
            AssetDatabase.SaveAssets();
            EditorSceneManager.MarkSceneDirty(street); EditorSceneManager.SaveScene(street);
            EditorSceneManager.OpenScene(DinerPath, OpenSceneMode.Single);
            ApplyDoorGlass();
        }

        public static void ApplyDoorGlass()
        {
            var scene = SceneManager.GetActiveScene();
            if (EditorApplication.isPlaying || scene.path != DinerPath) throw new InvalidOperationException("Open Diner in Edit mode.");
            var material = GlassMaterial("ExitDoorStreetGlass", AssetDatabase.LoadAssetAtPath<Cubemap>(CubePath), null);
            material.SetFloat("_ViewMode", 1); material.SetFloat("_Reflection", 0.035f);
            material.SetFloat("_Opacity", 0.94f); material.SetColor("_Tint", new Color(0.94f, 0.97f, 1));
            material.SetFloat("_UseInteriorCube", 0); material.SetFloat("_UseReflectionBasis", 0);
            var door = GameObject.Find("Front Door").transform.Find("Model/Door").GetComponent<Renderer>();
            var materials = door.sharedMaterials;
            var index = Array.FindIndex(materials, m => m.name == "DoorGlass" || m.name == "ExitDoorStreetGlass");
            if (index < 0) throw new InvalidOperationException("Cannot find the exit door glass material slot.");
            materials[index] = material; door.sharedMaterials = materials;
            EditorUtility.SetDirty(material); EditorUtility.SetDirty(door);
            PrefabUtility.RecordPrefabInstancePropertyModifications(door);
            AssetDatabase.SaveAssets(); EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
            SceneView.RepaintAll();
        }

        internal static void AttachExistingWindows(Transform facade)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(WindowsPrefab);
            if (!prefab) return;
            var old = facade.Find(GroupName);
            if (old) Object.DestroyImmediate(old.gameObject);
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, facade.gameObject.scene);
            instance.name = GroupName;
            instance.transform.SetParent(facade, true);
            PrefabUtility.RecordPrefabInstancePropertyModifications(instance);
        }

        static void CaptureInterior(int number, Vector3 centre, Vector3 direction)
        {
            var go = new GameObject("Temporary interior window capture") { hideFlags = HideFlags.HideAndDontSave };
            var target = new RenderTexture(384, 244, 24, RenderTextureFormat.ARGB32);
            var previous = RenderTexture.active;
            Texture2D pixels = null;
            try
            {
                var camera = go.AddComponent<Camera>(); camera.enabled = false;
                camera.transform.SetPositionAndRotation(centre - direction * 0.23f, Quaternion.LookRotation(direction));
                camera.orthographic = true; camera.orthographicSize = 0.7f; camera.aspect = 2.2f / 1.4f;
                camera.nearClipPlane = 0.04f; camera.farClipPlane = 18;
                camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color(0.04f, 0.028f, 0.018f);
                camera.targetTexture = target; camera.Render(); RenderTexture.active = target;
                pixels = new Texture2D(384, 244, TextureFormat.RGB24, false);
                pixels.ReadPixels(new Rect(0, 0, 384, 244), 0, 0); pixels.Apply();
                var path = Directory + "/InteriorWindow" + (number + 1) + ".png";
                System.IO.File.WriteAllBytes(path, pixels.EncodeToPNG()); AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
                var importer = (TextureImporter)AssetImporter.GetAtPath(path);
                importer.wrapMode = TextureWrapMode.Clamp; importer.mipmapEnabled = true; importer.SaveAndReimport();
            }
            finally
            {
                RenderTexture.active = previous;
                if (pixels) Object.DestroyImmediate(pixels);
                target.Release(); Object.DestroyImmediate(target); Object.DestroyImmediate(go);
            }
        }

        static Cubemap CaptureStreet(Vector3 position)
            => CaptureCube(CubePath, position, 256);

        static Vector3 InteriorDirection(int number) => number < 3 ? Vector3.right : number < 5 ? Vector3.left : Vector3.back;

        static void CaptureInteriorDepth(Scene scene, int number, Vector3 position)
        {
            var shader = Shader.Find("Hidden/HollowCreek/Window Interior Depth");
            if (!shader || ShaderUtil.ShaderHasError(shader)) throw new InvalidOperationException("Interior depth shader is unavailable.");
            var depth = new Material(shader);
            depth.SetVector("_CapturePosition",position);
            var renderers = scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Renderer>())
                .Where(r=>r.enabled).Select(r=>new { renderer=r, materials=r.sharedMaterials }).ToArray();
            try
            {
                foreach (var item in renderers)
                {
                    // Window rain and transparent panes must not occlude the room in the distance map.
                    if(item.materials.All(m=>m && m.renderQueue>=2500))item.renderer.enabled=false;
                    else item.renderer.sharedMaterials=item.materials.Select(m=>depth).ToArray();
                }
                CaptureCube(Directory + "/InteriorDepth" + (number+1) + ".cubemap",position,512,true,true);
            }
            finally
            {
                foreach(var item in renderers)if(item.renderer)
                {
                    item.renderer.sharedMaterials=item.materials; item.renderer.enabled=true;
                }
                Object.DestroyImmediate(depth);
            }
        }

        internal static Cubemap CaptureCube(string path, Vector3 position, int resolution, bool compress = false, bool depth = false)
        {
            var go = new GameObject("Temporary street capture") { hideFlags = HideFlags.HideAndDontSave };
            var target = new RenderTexture(resolution, resolution, 24, RenderTextureFormat.ARGBHalf, RenderTextureReadWrite.Linear) { dimension = TextureDimension.Cube };
            var previous = RenderTexture.active;
            var pixels = new Texture2D(resolution, resolution, TextureFormat.RGBAHalf, false, true);
            var stored = AssetDatabase.LoadAssetAtPath<Cubemap>(path);
            var cube = new Cubemap(resolution, TextureFormat.RGBAHalf, true);
            try
            {
                var camera = go.AddComponent<Camera>(); camera.enabled = false; camera.transform.position = position;
                camera.nearClipPlane = 0.025f; camera.farClipPlane = 250; camera.allowHDR = true;
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = depth ? new Color(32,32,32,1) : new Color(0.025f, 0.035f, 0.07f);
                target.Create();
                if (!camera.RenderToCubemap(target)) throw new InvalidOperationException("Failed to capture the street cubemap.");
                for (var face = 0; face < 6; face++)
                {
                    Graphics.SetRenderTarget(target, 0, (CubemapFace)face);
                    pixels.ReadPixels(new Rect(0, 0, resolution, resolution), 0, 0, false); pixels.Apply(false);
                    cube.SetPixels(pixels.GetPixels(), (CubemapFace)face);
                }
                cube.Apply(true, false);
                if (compress) EditorUtility.CompressCubemapTexture(cube, TextureFormat.BC6H, TextureCompressionQuality.Normal);
                cube.filterMode=depth?FilterMode.Point:FilterMode.Trilinear;
                if (stored)
                {
                    EditorUtility.CopySerialized(cube, stored);
                    EditorUtility.SetDirty(stored); Object.DestroyImmediate(cube);
                }
                else AssetDatabase.CreateAsset(cube, path);
                AssetDatabase.SaveAssets();
                // Reimport recreates the GPU resource when an existing capture changes size or format.
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);
                return AssetDatabase.LoadAssetAtPath<Cubemap>(path);
            }
            finally
            {
                RenderTexture.active = previous; target.Release(); Object.DestroyImmediate(target);
                Object.DestroyImmediate(pixels); Object.DestroyImmediate(go);
                if(cube && !EditorUtility.IsPersistent(cube))Object.DestroyImmediate(cube);
            }
        }

        static void Window(Transform parent, int number, Vector3 centre, float yaw, Vector3 frameOffset, Quaternion frameRotation, Vector3 frameScale, Cubemap cube)
        {
            var group = new GameObject("Matching exterior window " + (number + 1)).transform;
            group.SetParent(parent, false); group.position = centre;
            var turn = Quaternion.Euler(0, yaw + 90, 0);
            var frame = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(FramePrefab));
            frame.name = "Same wooden frame as inside"; frame.transform.SetParent(group, true);
            frame.transform.SetPositionAndRotation(centre + turn * frameOffset, turn * frameRotation); frame.transform.localScale = frameScale;
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(Directory + "/InteriorWindow" + (number + 1) + ".png");
            var material = GlassMaterial("ExteriorWindowGlass" + (number + 1), cube, texture);
            material.SetFloat("_ViewMode", 0); material.SetFloat("_Reflection", 0.055f); material.SetFloat("_InteriorBrightness", 0.72f);
            material.SetFloat("_Opacity", 1); material.SetColor("_Tint", new Color(0.98f, 0.99f, 1));
            var inward = InteriorDirection(number);
            material.SetFloat("_UseInteriorCube", 1);
            material.SetFloat("_UseInteriorDepth", 1);
            material.SetTexture("_InteriorDepth", AssetDatabase.LoadAssetAtPath<Cubemap>(Directory + "/InteriorDepth" + (number+1) + ".cubemap"));
            material.SetTexture("_InteriorCube", AssetDatabase.LoadAssetAtPath<Cubemap>(Directory + "/InteriorWindow" + (number + 1) + ".cubemap"));
            material.SetVector("_InteriorCapture", InteriorCentres[number] + inward * 0.12f);
            material.SetVector("_InteriorPaneCentre", InteriorCentres[number]);
            material.SetVector("_InteriorRight", Vector3.Cross(Vector3.up,inward));
            material.SetVector("_InteriorForward", inward);
            material.SetVector("_RoomMin", new Vector3(-6,0.008f,0));
            material.SetVector("_RoomMax", new Vector3(6,3.2f,10));
            var exteriorRotation = Quaternion.Euler(0,yaw,0);
            material.SetFloat("_UseReflectionBasis", 1);
            material.SetVector("_ReflectionRight", exteriorRotation * Vector3.right);
            material.SetVector("_ReflectionForward", exteriorRotation * Vector3.forward);
            var mesh = new MeshKit();
            var m = mesh.Mat("glass"); var n = Vector3.back;
            mesh.Quad(m, mesh.Vertex(new Vector3(-1.1f,-0.7f,0),n,new Vector2(0,0)),mesh.Vertex(new Vector3(1.1f,-0.7f,0),n,new Vector2(1,0)),
                mesh.Vertex(new Vector3(1.1f,0.7f,0),n,new Vector2(1,1)),mesh.Vertex(new Vector3(-1.1f,0.7f,0),n,new Vector2(0,1)));
            var pane = new GameObject("Glass showing matching diner interior"); pane.transform.SetParent(group, false);
            pane.transform.localRotation = Quaternion.Euler(0, yaw, 0);
            pane.AddComponent<MeshFilter>().sharedMesh = SaveMesh(mesh.Build("Exterior diner glass"), "ExteriorGlassPane");
            var renderer = pane.AddComponent<MeshRenderer>(); renderer.sharedMaterial = material; renderer.shadowCastingMode = ShadowCastingMode.Off;
        }

        static void Cladding(Transform parent, Bounds b, float doorX, Material material)
        {
            const float height = 4.40f;
            var left = doorX - 0.74f; var right = doorX + 0.74f;
            var minX=b.min.x-0.031f; var maxX=b.max.x+0.031f;
            Panel(parent, "Front facade beside windows", new Vector3((minX+left)/2,height/2,b.min.z-0.018f),new Vector3(left-minX,height,0.026f),material);
            Panel(parent, "Front facade beside entrance",new Vector3((right+maxX)/2,height/2,b.min.z-0.018f),new Vector3(maxX-right,height,0.026f),material);
            Panel(parent, "Facade above entrance",new Vector3(doorX,(2.65f+height)/2,b.min.z-0.018f),new Vector3(1.48f,height-2.65f,0.026f),material);
            Panel(parent, "Back facade",new Vector3(b.center.x,height/2,b.max.z+0.018f),new Vector3(b.size.x+0.062f,height,0.026f),material);
            foreach (var x in new[] { b.min.x-0.018f, b.max.x+0.018f })
                Panel(parent, "Side facade "+x,new Vector3(x,height/2,b.center.z),new Vector3(0.026f,height,b.size.z+0.062f),material);
        }

        static void Panel(Transform parent, string name, Vector3 position, Vector3 size, Material material)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube); go.name = name; go.transform.SetParent(parent, false);
            go.transform.position = position; go.transform.localScale = size; Object.DestroyImmediate(go.GetComponent<Collider>());
            go.GetComponent<Renderer>().sharedMaterial = material;
        }

        static Material PaintMaterial()
        {
            // The imported lower storey uses the pale part of the Suburban atlas.
            var path = Directory + "/DinerExteriorPaint.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (!material) { material = new Material(Shader.Find("Universal Render Pipeline/Unlit")); AssetDatabase.CreateAsset(material,path); }
            material.SetColor("_BaseColor",new Color(0.34f,0.37f,0.445f)); EditorUtility.SetDirty(material); return material;
        }

        static Material GlassMaterial(string name, Cubemap cube, Texture2D interior)
        {
            var path = Directory + "/" + name + ".mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            var shader = Shader.Find("HollowCreek/Diner Street Glass");
            if (!shader) throw new InvalidOperationException("Diner street glass shader is missing.");
            if (!material) { material = new Material(shader); AssetDatabase.CreateAsset(material,path); }
            material.shader = shader; material.SetTexture("_StreetCube",cube); material.SetTexture("_InteriorTex",interior);
            EditorUtility.SetDirty(material); return material;
        }

        static Mesh SaveMesh(Mesh built, string name)
        {
            var path = Directory + "/" + name + ".asset"; var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (!mesh) { mesh = built; mesh.name = name; AssetDatabase.CreateAsset(mesh,path); }
            else
            {
                mesh.Clear(); mesh.vertices=built.vertices; mesh.normals=built.normals; mesh.uv=built.uv; mesh.tangents=built.tangents;
                mesh.subMeshCount=built.subMeshCount; for(var i=0;i<built.subMeshCount;i++)mesh.SetTriangles(built.GetTriangles(i),i);
                mesh.bounds=built.bounds; EditorUtility.SetDirty(mesh); Object.DestroyImmediate(built);
            }
            return mesh;
        }
    }
}
