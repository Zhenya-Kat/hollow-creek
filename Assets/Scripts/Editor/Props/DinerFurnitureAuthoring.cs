using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace HollowCreek.Editor.Props
{
    public static class DinerFurnitureAuthoring
    {
        const string Directory = "Assets/Art/Environment/DinerInterior/Furniture";
        const string Models = "Assets/Art/Environment/Kenney/Furniture/";
        public const string PrefabPath = Directory + "/DinerSeating.prefab";
        static readonly Vector2[] TableCentres = {
            new(-2.7f, 2.3f), new(1.9f, 2.3f), new(-2.05f, 5.3f), new(1.9f, 5.3f)
        };

        [MenuItem("Hollow Creek/Реквизит/Мебель зала закусочной")]
        public static void Build()
        {
            var scene = SceneManager.GetActiveScene();
            if (EditorApplication.isPlaying || !scene.path.EndsWith("/Diner.unity"))
                throw new InvalidOperationException("Open Diner in Edit mode before placing its furniture.");
            var root = GameObject.Find("[Location] Diner");
            if (!root) throw new InvalidOperationException("Diner root is missing.");
            var floor = root.transform.Find("Geometry/Floor").GetComponent<Renderer>().bounds.max.y;
            System.IO.Directory.CreateDirectory(Directory);
            AssetDatabase.Refresh();
            var stage = new GameObject("Diner seating");
            var bar = new GameObject("Window bar table").transform;
            bar.SetParent(stage.transform, false);
            bar.position = new Vector3(5.52f, floor, 4);
            BuildWindowTable(bar);
            for (var i = 0; i < 5; i++)
            {
                var stool = Model("stoolBar", "Window bar stool " + (i + 1), stage.transform,
                    new Vector3(4.65f, floor, 1.7f + i * 1.15f), 0.78f, 90);
                AddBoundsCollider(stool);
            }
            for (var i = 0; i < TableCentres.Length; i++)
            {
                var centre = TableCentres[i];
                var table = new GameObject("Two-seat table " + (i + 1)).transform;
                table.SetParent(stage.transform, false);
                table.position = new Vector3(centre.x, floor, centre.y);
                var model = Model("tableRound", "Round table", table, table.position, 0.75f, 0, 0.9f);
                AddBoundsCollider(model);
                // Kenney's chair back is at +Z, so its sitting direction is -Z.
                var south = Model("chair", "Chair facing north", table, table.position + Vector3.back * 0.76f, 0.97f, 180);
                var north = Model("chair", "Chair facing south", table, table.position + Vector3.forward * 0.76f, 0.97f, 0);
                AddBoundsCollider(south);
                AddBoundsCollider(north);
            }
            AssetDatabase.SaveAssets();
            var prefab = PrefabUtility.SaveAsPrefabAsset(stage, PrefabPath);
            Object.DestroyImmediate(stage);
            foreach (var old in root.GetComponentsInChildren<Transform>(true)
                .Where(t => PrefabUtility.IsAnyPrefabInstanceRoot(t.gameObject)
                    && PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(t.gameObject) == PrefabPath).ToArray())
                Object.DestroyImmediate(old.gameObject);
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            instance.name = "Diner seating";
            instance.transform.SetParent(root.transform.Find("Art"), false);
            PrefabUtility.RecordPrefabInstancePropertyModifications(instance);
            foreach (var name in new[] { "NPC Mara", "NPC Owen" })
            {
                var npc = GameObject.Find(name);
                var animator = npc ? npc.GetComponentInChildren<Animator>() : null;
                if (animator && animator.runtimeAnimatorController)
                    animator.runtimeAnimatorController.animationClips.First(c => c.name.Contains("Idle")).SampleAnimation(animator.gameObject, 0);
            }
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Selection.activeGameObject = null;
            var view = SceneView.lastActiveSceneView;
            if (view)
            {
                view.cameraSettings.fieldOfView = 65;
                view.sceneViewState.alwaysRefresh = true;
                view.LookAt(new Vector3(0, 1.6f, 5.8f), Quaternion.Euler(4, 46, 0), 4f, false, true);
                view.Focus(); view.Repaint();
            }
        }

        static GameObject Model(string asset, string name, Transform parent, Vector3 position, float height, float yaw, float width = 0)
        {
            var source = AssetDatabase.LoadAssetAtPath<GameObject>(Models + asset + ".fbx");
            if (!source) throw new InvalidOperationException("Missing furniture: " + asset);
            var model = (GameObject)PrefabUtility.InstantiatePrefab(source);
            model.name = name;
            model.transform.SetParent(parent, true);
            model.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            model.transform.localScale = Vector3.one;
            var bounds = Bounds(model);
            var vertical = height / bounds.size.y;
            var horizontal = width > 0 ? width / Mathf.Max(bounds.size.x, bounds.size.z) : vertical;
            model.transform.localScale = new Vector3(horizontal, vertical, horizontal);
            model.transform.rotation = Quaternion.Euler(0, yaw, 0);
            bounds = Bounds(model);
            model.transform.position = position - new Vector3(bounds.center.x, bounds.min.y, bounds.center.z);
            foreach (var renderer in model.GetComponentsInChildren<Renderer>())
            {
                renderer.enabled = true;
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
                renderer.receiveShadows = true;
                PrefabUtility.RecordPrefabInstancePropertyModifications(renderer);
            }
            PrefabUtility.RecordPrefabInstancePropertyModifications(model.transform);
            PrefabUtility.RecordPrefabInstancePropertyModifications(model);
            return model;
        }

        static Bounds Bounds(GameObject model)
        {
            var renderers = model.GetComponentsInChildren<Renderer>();
            var bounds = renderers[0].bounds;
            foreach (var renderer in renderers) bounds.Encapsulate(renderer.bounds);
            return bounds;
        }

        static void AddBoundsCollider(GameObject model)
        {
            // Store bounds in model coordinates, including the imported offset origin.
            var filters = model.GetComponentsInChildren<MeshFilter>();
            var bounds = new Bounds();
            var first = true;
            foreach (var filter in filters)
            foreach (var vertex in filter.sharedMesh.vertices)
            {
                var p = model.transform.InverseTransformPoint(filter.transform.TransformPoint(vertex));
                if (first) { bounds = new Bounds(p, Vector3.zero); first = false; }
                else bounds.Encapsulate(p);
            }
            var collider = model.AddComponent<BoxCollider>();
            collider.center = bounds.center;
            collider.size = bounds.size;
        }

        static void BuildWindowTable(Transform bar)
        {
            var source = AssetDatabase.LoadAssetAtPath<GameObject>(Models + "tableCross.fbx");
            var original = source.GetComponentInChildren<Renderer>().sharedMaterial;
            var wood = Material("WindowTableWood", original, 1, 0.25f);
            var frame = Material("WindowTableFrame", original, 0.55f, 0.16f);
            var kit = new MeshKit();
            var top = kit.Mat("wood");
            var legs = kit.Mat("frame");
            kit.Box(new Vector3(0, 1.025f, 0), new Vector3(0.64f, 0.055f, 5.8f), top, 0.012f);
            kit.Box(new Vector3(0.07f, 0.93f, 0), new Vector3(0.14f, 0.09f, 5.62f), legs, 0.006f);
            foreach (var z in new[] { -2.65f, 2.65f })
            foreach (var x in new[] { -0.22f, 0.22f })
            {
                kit.Box(new Vector3(x, 0.496f, z), new Vector3(0.075f, 0.992f, 0.075f), legs, 0.004f);
                Collider(bar, "Table leg", new Vector3(x, 0.496f, z), new Vector3(0.075f, 0.992f, 0.075f));
            }
            kit.Box(new Vector3(0.22f, 0.496f, 0), new Vector3(0.075f, 0.992f, 0.075f), legs, 0.004f);
            Collider(bar, "Back support", new Vector3(0.22f, 0.496f, 0), new Vector3(0.075f, 0.992f, 0.075f));
            kit.Box(new Vector3(-0.32f, 0.26f, 0), new Vector3(0.035f, 0.035f, 5.25f), legs, 0.01f);
            foreach (var z in new[] { -2.55f, 2.55f })
                kit.Box(new Vector3(-0.27f, 0.26f, z), new Vector3(0.11f, 0.035f, 0.035f), legs, 0.006f);
            var built = kit.Build("Window bar table");
            var path = Directory + "/WindowBarTable.asset";
            var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (!mesh) { mesh = built; AssetDatabase.CreateAsset(mesh, path); }
            else
            {
                mesh.Clear(); mesh.vertices = built.vertices; mesh.normals = built.normals;
                mesh.uv = built.uv; mesh.tangents = built.tangents; mesh.subMeshCount = built.subMeshCount;
                for (var i = 0; i < built.subMeshCount; i++) mesh.SetTriangles(built.GetTriangles(i), i);
                mesh.bounds = built.bounds; EditorUtility.SetDirty(mesh); Object.DestroyImmediate(built);
            }
            bar.gameObject.AddComponent<MeshFilter>().sharedMesh = mesh;
            bar.gameObject.AddComponent<MeshRenderer>().sharedMaterials = new[] { wood, frame };
            Collider(bar, "Tabletop", new Vector3(0, 1.025f, 0), new Vector3(0.64f, 0.055f, 5.8f));
        }

        static Material Material(string name, Material source, float tint, float smoothness)
        {
            var path = Directory + "/" + name + ".mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (!material) { material = new Material(source); AssetDatabase.CreateAsset(material, path); }
            material.SetColor("_BaseColor", source.GetColor("_BaseColor") * new Color(tint, tint, tint, 1));
            material.SetFloat("_Smoothness", smoothness);
            EditorUtility.SetDirty(material);
            return material;
        }

        static void Collider(Transform parent, string name, Vector3 centre, Vector3 size)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var collider = go.AddComponent<BoxCollider>();
            collider.center = centre;
            collider.size = size;
        }
    }
}
