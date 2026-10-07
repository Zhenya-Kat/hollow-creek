using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace HollowCreek.Editor.Props
{
    public static class DinerWindowAuthoring
    {
        const string Directory = "Assets/Art/Environment/DinerWindow";
        static readonly float[] WindowCentres = { 2.5f, 5.25f, 8.0f };
        static readonly float[] EastWindowCentres = { 2.5f, 5.5f };
        static readonly float[] NorthWindowCentres = { -3.25f };

        [MenuItem("Hollow Creek/Реквизит/Вечерний вид из окна закусочной")]
        public static void Build()
        {
            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if (EditorApplication.isPlaying || !scene.path.EndsWith("/Diner.unity"))
                throw new InvalidOperationException("Open Diner in Edit mode before rebuilding its window view.");
            var window = GameObject.Find("Diner Window");
            var wall = GameObject.Find("Wall West");
            var eastWall = GameObject.Find("Wall East");
            var northWall = GameObject.Find("Wall North");
            if (!window || !wall || !eastWall || !northWall) throw new InvalidOperationException("Diner window or walls are missing.");
            var frame = window.transform.parent.Find("Diner Window Frame");
            var blinds = window.transform.Find("Model");
            if (!frame || !blinds) throw new InvalidOperationException("The original window frame or blinds are missing.");

            var panorama = AssetDatabase.LoadAssetAtPath<Texture2D>(Directory + "/Kloppenheim07.jpg");
            if (!panorama) throw new InvalidOperationException("The Poly Haven panorama is missing.");
            var importer = (TextureImporter)AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(panorama));
            if (importer.maxTextureSize != 4096 || importer.wrapMode != TextureWrapMode.Clamp)
            {
                importer.maxTextureSize = 4096;
                importer.wrapMode = TextureWrapMode.Clamp;
                importer.mipmapEnabled = true;
                importer.SaveAndReimport();
            }

            CreateWallOpenings(wall, "West Window Opening", WindowCentres, 0, 10, false);
            CreateWallOpenings(eastWall, "East Window Opening", EastWindowCentres, 0, 10, false);
            CreateWallOpenings(northWall, "North Window Opening", NorthWindowCentres, -6, 6, true);

            var stage = new GameObject("Diner Evening Window");
            EveningStreetAuthoring.InstantiateForDiner(stage.transform);
            var glass = Material("RainGlass", "HollowCreek/Rain on Window", new Color(0.38f, 0.52f, 0.66f));
            glass.SetFloat("_Amount", 0.25f);
            glass.SetFloat("_Speed", 0.45f);
            glass.SetFloat("_PreviewTime", 0);
            Quad(stage.transform, "Rain on the window glass", new Vector3(-5.917f, 1.6f, 2.5f), new Vector2(2.2f, 1.4f), glass);
            // Only the frame, blinds and glass repeat. All apertures share one physical street.
            // The original window retains its inspection clue; these copies are visual props.
            for (var i = 1; i < WindowCentres.Length; i++)
            {
                AddWindow(stage.transform, i + 1, new Vector3(-5.95f, 1.6f, WindowCentres[i]), 0, window.transform.position, frame, blinds, glass);
            }
            AddWindow(stage.transform, 4, new Vector3(NorthWindowCentres[0], 1.6f, 9.95f), 90, window.transform.position, frame, blinds, glass);
            for (var i = 0; i < EastWindowCentres.Length; i++)
                AddWindow(stage.transform, i + 5, new Vector3(5.95f, 1.6f, EastWindowCentres[i]), 180, window.transform.position, frame, blinds, glass);

            AssetDatabase.SaveAssets();
            var prefab = PrefabUtility.SaveAsPrefabAsset(stage, Directory + "/DinerEveningWindow.prefab");
            Object.DestroyImmediate(stage);
            // Unity may rename a prefab root to its filename during reimport. Match its asset
            // rather than its display name so repeated builds never stack multiple backdrops.
            var oldInstances = scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<Transform>(true))
                .Select(t => t.gameObject)
                .Where(go => PrefabUtility.IsAnyPrefabInstanceRoot(go)
                    && PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(go) == Directory + "/DinerEveningWindow.prefab")
                .ToArray();
            foreach (var old in oldInstances) Object.DestroyImmediate(old);
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            instance.name = "Diner Evening Window";
            PrefabUtility.RecordPrefabInstancePropertyModifications(instance);
            instance.transform.SetParent(window.transform.parent, true);
            DinerInteriorAuthoring.ApplyExistingMaterials();

            // Imported character poses are sampled again after editor domain reloads.
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
                view.sceneViewState.alwaysRefresh = true;
                view.cameraSettings.fieldOfView = 65;
                view.LookAt(new Vector3(0, 1.6f, 5.8f), Quaternion.Euler(4, 46, 0), 4.0f, false, true);
                view.Focus();
                view.Repaint();
            }
        }

        internal static GameObject CreateWindowDecorations(Vector3 streetWindow)
        {
            var group = new GameObject("Opposite-side decorations");
            var leftTree = Tree(group.transform, "Tree by the left pane", new Vector3(streetWindow.x + 3.05f, 0, -9.5f), 4.9f, -66);
            var rightTree = Tree(group.transform, "Tree by the right pane", new Vector3(streetWindow.x - 2.85f, 0, -9.5f), 4.6f, -127);
            Garlands(leftTree, "Left tree garlands", "LeftTree", 0.0f);
            Garlands(rightTree, "Right tree garlands", "RightTree", 0.8f);
            // Continue the decorated planting along the street with staggered spacing.
            // Reuse the fitted garland meshes together with their tree so the cables
            // stay on the crown and the extra trees do not need unique mesh assets.
            ContinueDecoratedTree(rightTree, "West garden garland tree", new Vector3(streetWindow.x - 10.1f, 0, -9.9f), 4.3f, -101);
            ContinueDecoratedTree(leftTree, "West end garland tree", new Vector3(streetWindow.x - 18.4f, 0, -10.8f), 4.7f, -42);
            ContinueDecoratedTree(rightTree, "East garden garland tree", new Vector3(streetWindow.x + 10.7f, 0, -10.2f), 4.8f, -148);
            ContinueDecoratedTree(leftTree, "East end garland tree", new Vector3(streetWindow.x + 19.0f, 0, -9.7f), 4.2f, -79);
            return group;
        }

        internal static void AddNearSideDecoratedTrees(Transform street)
        {
            var decorations = street.Find("Opposite-side decorations");
            var left = decorations.Find("Tree by the left pane").gameObject;
            var right = decorations.Find("Tree by the right pane").gameObject;
            var group = ReplaceGroup("Diner-side garland trees", street);
            // Uneven clusters and setbacks leave the pavement and diner entrance clear.
            ContinueDecoratedTree(left, "Near west outer garland tree", new Vector3(-22.7f, 0, 5.2f), 4.4f, 23, group);
            ContinueDecoratedTree(right, "Near west cluster garland tree", new Vector3(-17.8f, 0, 3.7f), 3.9f, -154, group);
            ContinueDecoratedTree(right, "Near house garden garland tree", new Vector3(-3.4f, 0, 4.8f), 4.8f, -83, group);
            ContinueDecoratedTree(left, "Near house edge garland tree", new Vector3(3.1f, 0, 4.1f), 4.1f, 51, group);
            ContinueDecoratedTree(left, "Near diner east garland tree", new Vector3(33.8f, 0, 4.6f), 4.6f, -19, group);
            ContinueDecoratedTree(right, "Near east rear garland tree", new Vector3(42.3f, 0, 7.0f), 4.2f, 137, group);
        }

        static void ContinueDecoratedTree(GameObject source, string name, Vector3 position, float height, float yaw, Transform parent = null)
        {
            var tree = Object.Instantiate(source, parent ? parent : source.transform.parent);
            tree.name = name;
            tree.transform.position = Vector3.zero;
            tree.transform.rotation = Quaternion.Euler(0, yaw, 0);
            // The first child contains the model; the second contains the garland.
            var renderers = tree.transform.GetChild(0).GetComponentsInChildren<Renderer>();
            var bounds = renderers[0].bounds;
            foreach (var renderer in renderers) bounds.Encapsulate(renderer.bounds);
            tree.transform.localScale *= height / bounds.size.y;
            bounds = renderers[0].bounds;
            foreach (var renderer in renderers) bounds.Encapsulate(renderer.bounds);
            tree.transform.position = position - new Vector3(bounds.center.x, bounds.min.y, bounds.center.z);
        }

        static Transform ReplaceGroup(string name, Transform parent)
        {
            var old = parent.Find(name);
            if (old) Object.DestroyImmediate(old.gameObject);
            var group = new GameObject(name).transform;
            group.SetParent(parent, false);
            return group;
        }

        static void CreateWallOpenings(GameObject wall, string groupName, float[] centres, float start, float end, bool alongX)
        {
            // Keep the original wall collider: a decorative window is not a passage.
            wall.GetComponent<Renderer>().enabled = false;
            var opening = ReplaceGroup(groupName, wall.transform.parent);
            var material = wall.GetComponent<Renderer>().sharedMaterial;
            void Section(string name, float centre, float y, float width, float height)
            {
                var position = alongX ? new Vector3(centre, y, wall.transform.position.z) : new Vector3(wall.transform.position.x, y, centre);
                var size = alongX ? new Vector3(width, height, 0.15f) : new Vector3(0.15f, height, width);
                Box(opening, name, position, size, material);
            }
            var previous = start;
            for (var i = 0; i < centres.Length; i++)
            {
                var centre = centres[i];
                var edge = centre - 1.1f;
                Section("Wall pier " + (i + 1), (previous + edge) * 0.5f, 1.6f, edge - previous, 3.2f);
                Section("Wall below window " + (i + 1), centre, 0.45f, 2.2f, 0.9f);
                Section("Wall above window " + (i + 1), centre, 2.75f, 2.2f, 0.9f);
                previous = centre + 1.1f;
            }
            Section("Wall after windows", (previous + end) * 0.5f, 1.6f, end - previous, 3.2f);
            foreach (var renderer in opening.GetComponentsInChildren<Renderer>())
            {
                renderer.shadowCastingMode = ShadowCastingMode.On;
                renderer.receiveShadows = true;
            }
        }

        static void AddWindow(Transform stage, int number, Vector3 position, float yaw, Vector3 originalPosition, Transform frame, Transform blinds, Material glass)
        {
            var rotation = Quaternion.Euler(0, yaw, 0);
            var extra = new GameObject("Additional diner window " + number).transform;
            extra.SetParent(stage, false);
            extra.position = position;
            CloneWindowPart(frame, extra, "Window frame", originalPosition, position, rotation);
            CloneWindowPart(blinds, extra, "Raised blinds", originalPosition, position, rotation);
            var pane = Primitive(extra, "Rain on the window glass", PrimitiveType.Quad,
                position + rotation * new Vector3(0.033f, 0, 0), new Vector3(2.2f, 1.4f, 1), glass);
            pane.transform.rotation = rotation * Quaternion.Euler(0, -90, 0);
        }

        static void CloneWindowPart(Transform source, Transform parent, string name, Vector3 originalPosition, Vector3 position, Quaternion rotation)
        {
            var copy = Object.Instantiate(source.gameObject, parent, true);
            copy.name = name;
            copy.transform.SetPositionAndRotation(position + rotation * (source.position - originalPosition), rotation * source.rotation);
            foreach (var collider in copy.GetComponentsInChildren<Collider>(true)) Object.DestroyImmediate(collider);
        }

        static Material Material(string name, string shaderName, Color color)
        {
            var shader = Shader.Find(shaderName);
            if (!shader) throw new InvalidOperationException("Missing shader: " + shaderName);
            var path = Directory + "/" + name + ".mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (!material) { material = new Material(shader); AssetDatabase.CreateAsset(material, path); }
            material.shader = shader;
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
            else material.SetColor("_Tint", color);
            EditorUtility.SetDirty(material);
            return material;
        }

        static GameObject Primitive(Transform parent, string name, PrimitiveType type, Vector3 position, Vector3 scale, Material material)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.position = position;
            go.transform.localScale = scale;
            Object.DestroyImmediate(go.GetComponent<Collider>());
            var renderer = go.GetComponent<Renderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            return go;
        }

        static void Box(Transform parent, string name, Vector3 position, Vector3 size, Material material) =>
            Primitive(parent, name, PrimitiveType.Cube, position, size, material);

        static void Quad(Transform parent, string name, Vector3 position, Vector2 size, Material material)
        {
            var go = Primitive(parent, name, PrimitiveType.Quad, position, new Vector3(size.x, size.y, 1), material);
            go.transform.rotation = Quaternion.Euler(0, -90, 0);
        }

        static GameObject Tree(Transform parent, string name, Vector3 position, float height, float yaw)
        {
            var source = AssetDatabase.LoadAssetAtPath<GameObject>(Directory + "/QuaterniusTree.glb");
            if (!source) throw new InvalidOperationException("Missing Quaternius tree.");
            var tree = Object.Instantiate(source, parent);
            tree.name = name;
            tree.transform.position = Vector3.zero;
            tree.transform.rotation = Quaternion.Euler(0, yaw, 0);
            tree.transform.localScale = Vector3.one;
            var renderers = tree.GetComponentsInChildren<Renderer>();
            var bounds = renderers[0].bounds;
            foreach (var renderer in renderers) bounds.Encapsulate(renderer.bounds);
            tree.transform.localScale = Vector3.one * (height / bounds.size.y);
            bounds = renderers[0].bounds;
            foreach (var renderer in renderers) bounds.Encapsulate(renderer.bounds);
            tree.transform.position = position - new Vector3(bounds.center.x, bounds.min.y, bounds.center.z);
            foreach (var renderer in renderers)
            {
                renderer.sharedMaterials = renderer.sharedMaterials.Select(TreeMaterial).ToArray();
                renderer.shadowCastingMode = ShadowCastingMode.Off;
                renderer.receiveShadows = false;
            }
            return tree;
        }

        static void Garlands(GameObject tree, string name, string assetPrefix, float variation)
        {
            var group = new GameObject(name).transform;
            group.SetParent(tree.transform, false);
            var gold = Material("GarlandGoldBokeh", "HollowCreek/Garland Bulb Glow", new Color(1.0f, 0.67f, 0.30f, 0.42f));
            var warmWhite = Material("GarlandWarmBokeh", "HollowCreek/Garland Bulb Glow", new Color(1.0f, 0.82f, 0.53f, 0.32f));
            gold.enableInstancing = warmWhite.enableInstancing = true;
            var goldCore = Material("GarlandGoldBulbs", "Universal Render Pipeline/Unlit", new Color(1.0f, 0.71f, 0.30f));
            var whiteCore = Material("GarlandCreamBulbs", "Universal Render Pipeline/Unlit", new Color(1.0f, 0.84f, 0.60f));
            var cable = Material("GarlandCable", "Universal Render Pipeline/Unlit", new Color(0.03f, 0.035f, 0.032f));

            // Trace horizontal sections of the leaf mesh, rather than its rectangular bounds.
            // The continuous spiral follows those sections all the way around the crown.
            var triangles = new List<Vector3>();
            foreach (var filter in tree.GetComponentsInChildren<MeshFilter>())
            {
                var mesh = filter.sharedMesh;
                var vertices = mesh.vertices;
                var materials = filter.GetComponent<Renderer>().sharedMaterials;
                for (var sub = 0; sub < mesh.subMeshCount; sub++)
                {
                    if (!materials[sub].name.Contains("Leaves")) continue;
                    foreach (var index in mesh.GetTriangles(sub))
                        triangles.Add(filter.transform.TransformPoint(vertices[index]));
                }
            }
            if (triangles.Count == 0) throw new InvalidOperationException("The tree has no leaf geometry for its garland.");
            var bottom = triangles.Min(v => v.y);
            var top = triangles.Max(v => v.y);
            var profiles = new List<CrownSection>();
            const int sectionCount = 18;
            for (var section = 0; section < sectionCount; section++)
            {
                var y = Mathf.Lerp(bottom, top, Mathf.Lerp(0.17f, 0.89f, section / (sectionCount - 1f)));
                var points = new List<Vector2>();
                for (var i = 0; i < triangles.Count; i += 3)
                {
                    AddSectionPoint(triangles[i], triangles[i + 1], y, points);
                    AddSectionPoint(triangles[i + 1], triangles[i + 2], y, points);
                    AddSectionPoint(triangles[i + 2], triangles[i], y, points);
                }
                var hull = ConvexHull(points);
                if (hull.Count < 3) throw new InvalidOperationException("Cannot trace the tree crown at height " + y);
                profiles.Add(new CrownSection { Height = y, Hull = hull, Center = hull.Aggregate(Vector2.zero, (a, b) => a + b) / hull.Count });
            }

            Vector3 Point(float t)
            {
                var y = Mathf.Lerp(profiles[sectionCount - 1].Height, profiles[0].Height, t);
                var profilePosition = (1 - t) * (sectionCount - 1);
                var section = Mathf.Min(Mathf.FloorToInt(profilePosition), sectionCount - 2);
                var angle = variation + t * Mathf.PI * 6;
                var direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                var a = profiles[section].Outline(direction);
                var b = profiles[section + 1].Outline(direction);
                var point = Vector2.Lerp(a, b, profilePosition - section) + direction * 0.009f;
                return new Vector3(point.x, y, point.y);
            }

            var scale = tree.transform.lossyScale.x;
            var path = Enumerable.Range(0, 241).Select(i => group.InverseTransformPoint(Point(i / 240f))).ToArray();
            var wireMesh = new MeshKit();
            wireMesh.Sweep(path, Vector2.one * (0.003f / scale), wireMesh.Mat("cable"));
            MeshPart(group, "Cable wrapped around the crown", assetPrefix + "GarlandCable", wireMesh, cable);
            var goldMesh = new MeshKit();
            var whiteMesh = new MeshKit();
            for (var bulb = 0; bulb < 42; bulb++)
            {
                var position = Point((bulb + 0.5f) / 42f) - Vector3.up * 0.008f;
                var localPosition = group.InverseTransformPoint(position);
                var creamy = bulb % 3 == 0;
                var mesh = creamy ? whiteMesh : goldMesh;
                mesh.Sphere(localPosition, 0.007f / scale, mesh.Mat("bulb"), 8);
                var diameter = 0.14f + 0.012f * Mathf.Sin(bulb * 2.2f + variation);
                // Only the halo faces the camera; its center remains on the physical 3D bulb.
                Primitive(group, $"Bulb {bulb + 1} soft glow", PrimitiveType.Quad, position,
                    Vector3.one * (diameter / scale), creamy ? warmWhite : gold);
            }
            MeshPart(group, "Gold bulbs", assetPrefix + "GarlandGoldBulbs", goldMesh, goldCore);
            MeshPart(group, "Cream bulbs", assetPrefix + "GarlandCreamBulbs", whiteMesh, whiteCore);
        }

        sealed class CrownSection
        {
            public float Height;
            public Vector2 Center;
            public List<Vector2> Hull;

            public Vector2 Outline(Vector2 direction)
            {
                var radius = float.PositiveInfinity;
                for (var i = 0; i < Hull.Count; i++)
                {
                    var start = Hull[i] - Center;
                    var edge = Hull[(i + 1) % Hull.Count] - Hull[i];
                    var denominator = Cross(direction, edge);
                    if (Mathf.Abs(denominator) < 0.000001f) continue;
                    var distance = Cross(start, edge) / denominator;
                    var along = Cross(start, direction) / denominator;
                    if (distance >= 0 && along >= -0.00001f && along <= 1.00001f)
                        radius = Mathf.Min(radius, distance);
                }
                if (float.IsInfinity(radius)) throw new InvalidOperationException("Cannot trace a garland around the crown section.");
                // Leaf cards extend beyond the visible alpha-cutout leaves. Tuck the
                // strand slightly into that envelope so it rests among the branches.
                return Center + direction * (radius * 0.92f);
            }
        }

        static float Cross(Vector2 a, Vector2 b) => a.x * b.y - a.y * b.x;

        static void AddSectionPoint(Vector3 a, Vector3 b, float y, List<Vector2> points)
        {
            if ((a.y > y) == (b.y > y) || Mathf.Abs(b.y - a.y) < 0.000001f) return;
            var p = Vector3.Lerp(a, b, (y - a.y) / (b.y - a.y));
            points.Add(new Vector2(p.x, p.z));
        }

        static List<Vector2> ConvexHull(List<Vector2> points)
        {
            var sorted = points.Distinct().OrderBy(p => p.x).ThenBy(p => p.y).ToArray();
            var hull = new List<Vector2>();
            foreach (var p in sorted)
            {
                while (hull.Count >= 2 && Cross(hull[hull.Count - 1] - hull[hull.Count - 2], p - hull[hull.Count - 1]) <= 0)
                    hull.RemoveAt(hull.Count - 1);
                hull.Add(p);
            }
            var lowerCount = hull.Count;
            for (var i = sorted.Length - 2; i >= 0; i--)
            {
                var p = sorted[i];
                while (hull.Count > lowerCount && Cross(hull[hull.Count - 1] - hull[hull.Count - 2], p - hull[hull.Count - 1]) <= 0)
                    hull.RemoveAt(hull.Count - 1);
                hull.Add(p);
            }
            if (hull.Count > 1) hull.RemoveAt(hull.Count - 1);
            return hull;
        }

        static void MeshPart(Transform parent, string name, string assetName, MeshKit kit, Material material)
        {
            var generated = kit.Build(assetName);
            var path = Directory + "/" + assetName + ".asset";
            var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (mesh)
            {
                // Update the live mesh buffers as well as its serialized asset. Reusing
                // the asset preserves its GUID when a garland is fitted again.
                mesh.Clear();
                mesh.indexFormat = generated.indexFormat;
                mesh.vertices = generated.vertices;
                mesh.normals = generated.normals;
                mesh.uv = generated.uv;
                mesh.tangents = generated.tangents;
                mesh.subMeshCount = generated.subMeshCount;
                for (var sub = 0; sub < generated.subMeshCount; sub++)
                    mesh.SetTriangles(generated.GetTriangles(sub), sub, false);
                mesh.bounds = generated.bounds;
                Object.DestroyImmediate(generated);
                EditorUtility.SetDirty(mesh);
            }
            else
            {
                mesh = generated;
                AssetDatabase.CreateAsset(mesh, path);
            }
            var part = new GameObject(name);
            part.transform.SetParent(parent, false);
            part.AddComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = part.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
        }

        static void LampAndPumpkins(Transform parent, string name, Vector3 position, float height, float yaw)
        {
            var placement = position;
            position = Vector3.zero;
            var group = new GameObject(name).transform;
            group.SetParent(parent, false);
            var lanternScale = height / 2.05f;
            var lampMaterial = Material("WindowLampPost", "Universal Render Pipeline/Lit", new Color(0.12f, 0.13f, 0.14f));
            lampMaterial.SetTexture("_BaseMap", null);
            lampMaterial.SetTexture("_EmissionMap", null);
            lampMaterial.DisableKeyword("_EMISSION");
            lampMaterial.SetColor("_EmissionColor", Color.black);
            lampMaterial.SetFloat("_Metallic", 0.35f);
            lampMaterial.SetFloat("_Smoothness", 0.32f);
            var lamp = GraveyardModel(group, "Lamp post", "lightpost-single", position, height, 90, lampMaterial);
            var bounds = lamp.GetComponentInChildren<Renderer>().bounds;
            var bulbPosition = new Vector3(bounds.max.x - 0.16f * lanternScale, bounds.max.y - 0.28f * lanternScale, position.z);
            var glass = Material("StreetLampGlass", "Universal Render Pipeline/Lit", new Color(1.0f, 0.71f, 0.30f));
            glass.EnableKeyword("_EMISSION");
            glass.SetColor("_EmissionColor", new Color(1.0f, 0.65f, 0.25f) * 1.2f);
            glass.SetFloat("_Smoothness", 0.2f);
            var glassPosition = new Vector3(bounds.max.x + 0.006f, bulbPosition.y, position.z);
            Box(group, "Lit front glass of the lantern", glassPosition, new Vector3(0.008f, 0.23f, 0.14f) * lanternScale, glass);
            var light = new GameObject("Warm lamp light").AddComponent<Light>();
            light.transform.SetParent(group, false);
            light.transform.position = bulbPosition;
            light.type = LightType.Point;
            light.color = new Color(1.0f, 0.70f, 0.39f);
            light.intensity = 1.8f * lanternScale;
            light.range = 3.1f * lanternScale;
            light.shadows = LightShadows.None;
            var glow = Material("StreetLampGlow", "HollowCreek/Distant Soft Light", new Color(1.0f, 0.70f, 0.35f, 0.60f));
            Quad(group, "Soft halo around the lit lantern", glassPosition + new Vector3(0.02f, 0, 0), Vector2.one * (0.32f * lanternScale), glow);

            var pumpkinMaterial = Material("WindowPumpkin", "Universal Render Pipeline/Lit", new Color(0.88f, 0.75f, 0.60f));
            pumpkinMaterial.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Art/Environment/Kenney/Graveyard/Textures/colormap.png"));
            pumpkinMaterial.SetFloat("_Metallic", 0);
            pumpkinMaterial.SetFloat("_Smoothness", 0.22f);
            var pumpkinSpread = Mathf.Sqrt(lanternScale);
            GraveyardModel(group, "Round carved pumpkin", "pumpkin-carved", position + new Vector3(0.22f * pumpkinSpread, 0.001f, -0.28f * pumpkinSpread), 0.34f, 90, pumpkinMaterial);
            GraveyardModel(group, "Tall carved pumpkin", "pumpkin-tall-carved", position + new Vector3(-0.18f * pumpkinSpread, 0.001f, 0.30f * pumpkinSpread), 0.40f, 75, pumpkinMaterial);
            group.SetPositionAndRotation(placement, Quaternion.Euler(0, yaw, 0));
        }

        static GameObject GraveyardModel(Transform parent, string name, string asset, Vector3 position, float height, float yaw, Material material)
        {
            var source = AssetDatabase.LoadAssetAtPath<GameObject>($"Assets/Art/Environment/Kenney/Graveyard/{asset}.fbx");
            if (!source) throw new InvalidOperationException("Missing Kenney prop: " + asset);
            var model = Object.Instantiate(source, parent);
            model.name = name;
            model.transform.position = Vector3.zero;
            model.transform.rotation = Quaternion.Euler(0, yaw, 0);
            model.transform.localScale = Vector3.one;
            var renderers = model.GetComponentsInChildren<Renderer>();
            var bounds = renderers[0].bounds;
            foreach (var renderer in renderers) bounds.Encapsulate(renderer.bounds);
            model.transform.localScale = Vector3.one * (height / bounds.size.y);
            bounds = renderers[0].bounds;
            foreach (var renderer in renderers) bounds.Encapsulate(renderer.bounds);
            model.transform.position = position - new Vector3(0, bounds.min.y, 0);
            foreach (var renderer in renderers)
            {
                renderer.sharedMaterials = renderer.sharedMaterials.Select(_ => material).ToArray();
                renderer.shadowCastingMode = ShadowCastingMode.Off;
                renderer.receiveShadows = false;
            }
            return model;
        }

        static Material TreeMaterial(Material source)
        {
            var leaves = source.name.Contains("Leaves");
            var mat = Material(leaves ? "EveningLeaves" : "EveningBark", "Universal Render Pipeline/Unlit",
                leaves ? new Color(0.24f, 0.32f, 0.36f) : new Color(0.19f, 0.23f, 0.27f));
            var texture = source.HasProperty("baseColorTexture") ? source.GetTexture("baseColorTexture") : source.mainTexture;
            mat.SetTexture("_BaseMap", texture);
            mat.SetFloat("_Cull", leaves ? 0 : 2);
            mat.SetFloat("_AlphaClip", leaves ? 1 : 0);
            mat.SetFloat("_Cutoff", 0.45f);
            if (leaves)
            {
                mat.EnableKeyword("_ALPHATEST_ON");
                mat.SetOverrideTag("RenderType", "TransparentCutout");
                mat.renderQueue = 2450;
            }
            EditorUtility.SetDirty(mat);
            return mat;
        }
    }
}
