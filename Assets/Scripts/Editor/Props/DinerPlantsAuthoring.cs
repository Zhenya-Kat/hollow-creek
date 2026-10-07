using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace HollowCreek.Editor.Props
{
    public static class DinerPlantsAuthoring
    {
        const string Directory = "Assets/Art/Environment/DinerInterior/Plants";
        const string GroupName = "Diner plants and candle pumpkin";
        public const string PrefabPath = Directory + "/DinerPlants.prefab";

        [MenuItem("Hollow Creek/Реквизит/Растения и тыква в закусочной")]
        public static void Build()
        {
            var scene = SceneManager.GetActiveScene();
            if (EditorApplication.isPlaying || !scene.path.EndsWith("/Diner.unity"))
                throw new InvalidOperationException("Open Diner in Edit mode before placing plants.");
            var diner = GameObject.Find("[Location] Diner");
            // Old authoring runs saved their temporary root before destroying it.
            // Remove only that owned, unpacked staging group at the scene root.
            foreach (var orphan in scene.GetRootGameObjects().Where(g => g.name == GroupName
                && !PrefabUtility.IsAnyPrefabInstanceRoot(g)).ToArray())
                Object.DestroyImmediate(orphan);
            var floor = diner.transform.Find("Geometry/Floor").GetComponent<Renderer>().bounds.max.y;
            System.IO.Directory.CreateDirectory(Directory);
            AssetDatabase.Refresh();
            var stage = new GameObject(GroupName);
            try
            {
                AddFern(stage.transform, new Vector3(-5.15f, floor, 9.23f));
                AddCypress(stage.transform, "Tall potted cypress", new Vector3(-4.72f, floor, 9.63f), 1.65f, 28);
                AddCypress(stage.transform, "Small potted cypress", new Vector3(-5.57f, floor, 8.99f), 1.12f, -17);
                AddPumpkin(stage.transform, "Carved pumpkin with candle", new Vector3(-4.87f, floor, 8.66f), 0.39f, 160);
                var hay = GardenModel(stage.transform, "Bundled hay bale", "hay-bale-bundled", new Vector3(-4.05f, floor, 9.31f), 0.45f, 8);
                var hayTop = hay.GetComponent<Renderer>().bounds.max.y;
                GardenModel(stage.transform, "Small bundled hay bale", "hay-bale-bundled", new Vector3(-3.33f, floor, 9.40f), 0.29f, -12);
                AddPumpkin(stage.transform, "Large carved pumpkin on hay", new Vector3(-4.05f, hayTop, 9.31f), 0.50f, 160);
                AddPumpkin(stage.transform, "Small carved pumpkin with candle", new Vector3(-3.44f, floor, 8.86f), 0.24f, 145);
                var vaseMesh = MakeVase();
                var flowerMesh = MakeGerbera();
                var tables = diner.GetComponentsInChildren<Transform>().Where(t => t.name.StartsWith("Two-seat table ")).OrderBy(t => t.name).ToArray();
                for (var i = 0; i < tables.Length; i++)
                {
                    var bounds = tables[i].Find("Round table").GetComponent<Renderer>().bounds;
                    var clothLift = AssetDatabase.LoadAssetAtPath<GameObject>(DinerTableclothAuthoring.PrefabPath)
                        ? DinerTableclothAuthoring.SurfaceLift : 0;
                    AddVase(stage.transform, "Gerbera vase " + (i + 1), new Vector3(bounds.center.x, bounds.max.y + clothLift, bounds.center.z), i * 47, vaseMesh, flowerMesh);
                }
                var maraTable = diner.transform.Find("Art/tableCross").GetComponent<Renderer>().bounds;
                AddVase(stage.transform, "Mara gerbera vase", new Vector3(-4.42f, maraTable.max.y, 6.59f), 24, vaseMesh, flowerMesh);
                var owenTable = diner.GetComponentsInChildren<Transform>().FirstOrDefault(t => t.name == "Large booth table");
                if (owenTable)
                {
                    var bounds = owenTable.GetComponent<Renderer>().bounds;
                    AddVase(stage.transform, "Owen booth gerbera vase", new Vector3(-4.42f, bounds.max.y, bounds.center.z + 0.59f), 61, vaseMesh, flowerMesh);
                }
                AssetDatabase.SaveAssets();
                var prefab = PrefabUtility.SaveAsPrefabAsset(stage, PrefabPath);
                Object.DestroyImmediate(stage);
                foreach (var old in diner.GetComponentsInChildren<Transform>(true).Where(t => PrefabUtility.IsAnyPrefabInstanceRoot(t.gameObject)
                    && PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(t.gameObject) == PrefabPath).ToArray())
                    Object.DestroyImmediate(old.gameObject);
                var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                instance.name = GroupName;
                instance.transform.SetParent(diner.transform.Find("Art"), false);
                PrefabUtility.RecordPrefabInstancePropertyModifications(instance);
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
                Selection.activeGameObject = null;
                SceneView.RepaintAll();
            }
            finally { if (stage) Object.DestroyImmediate(stage); }
        }

        static void AddFern(Transform parent, Vector3 position)
        {
            var fern = new GameObject("Floor fern by Mara").transform;
            fern.SetParent(parent, false);
            fern.position = position;
            var source = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/Environment/Kenney/Furniture/pottedPlant.fbx");
            var pot = Object.Instantiate(source, fern);
            pot.name = "Fern clay pot";
            foreach (Transform child in pot.transform.Cast<Transform>().ToArray()) Object.DestroyImmediate(child.gameObject);
            pot.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
            pot.transform.localScale = Vector3.one;
            var raw = pot.GetComponent<Renderer>().bounds;
            pot.transform.localScale = new Vector3(0.42f / Mathf.Max(raw.size.x, raw.size.z), 0.40f / raw.size.y, 0.42f / Mathf.Max(raw.size.x, raw.size.z));
            var bounds = pot.GetComponent<Renderer>().bounds;
            pot.transform.position += position - new Vector3(bounds.center.x, bounds.min.y, bounds.center.z);
            pot.GetComponent<Renderer>().sharedMaterials = new[] {
                Material("FernTerracotta", new Color(0.43f, 0.20f, 0.105f), 0.12f),
                Material("PottingSoil", new Color(0.055f, 0.032f, 0.02f), 0.02f)
            };
            var collider = pot.AddComponent<BoxCollider>();
            collider.center = pot.GetComponent<MeshFilter>().sharedMesh.bounds.center;
            collider.size = pot.GetComponent<MeshFilter>().sharedMesh.bounds.size;
            var kit = new MeshKit();
            kit.Mat("leaf"); kit.Mat("shade"); kit.Mat("new growth");
            var stem = kit.Mat("stem");
            var random = new System.Random(357);
            for (var i = 0; i < 56; i++)
            {
                var low = i < 24;
                var young = i >= 44;
                var ringIndex = low ? i : young ? i - 44 : i - 24;
                var ringCount = low ? 24 : young ? 12 : 20;
                var angle = ringIndex * 2 * Mathf.PI / ringCount + (young ? 0.38f : low ? 0 : 0.17f)
                    + (float)random.NextDouble() * 0.18f;
                var radial = new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle));
                var side = Vector3.Cross(Vector3.up, radial);
                var baseRadius = young ? 0.015f + (float)random.NextDouble() * 0.040f
                    : low ? 0.105f + (float)random.NextDouble() * 0.035f : 0.065f + (float)random.NextDouble() * 0.040f;
                var reach = young ? 0.16f + (float)random.NextDouble() * 0.08f
                    : low ? 0.37f + (float)random.NextDouble() * 0.07f : 0.30f + (float)random.NextDouble() * 0.09f;
                var rise = young ? 0.58f + (float)random.NextDouble() * 0.12f
                    : low ? 0.25f + (float)random.NextDouble() * 0.09f : 0.42f + (float)random.NextDouble() * 0.09f;
                // Roots spread across the soil, instead of all fronds narrowing into one tall petiole.
                Vector3 Spine(float t) => radial * (baseRadius + reach * Mathf.Sin(t * Mathf.PI * 0.5f))
                    + Vector3.up * (0.388f + rise * Mathf.Sin(t * Mathf.PI * (young ? 0.53f : low ? 0.88f : 0.74f))
                        - (young ? 0 : low ? 0.10f : 0.035f) * t * t);
                var spine = Enumerable.Range(0, 25).Select(j => Spine(j / 24f)).ToArray();
                kit.Sweep(spine, new Vector2(0.003f, 0.002f), stem);
                for (var j = 0; j < 23; j++)
                {
                    var t = 0.025f + j / 23f * 0.955f;
                    var tangent = (Spine(t + 0.01f) - Spine(t - 0.01f)).normalized;
                    var length = (young ? 0.044f : 0.073f) * (0.48f + 0.52f * Mathf.Sin(t * Mathf.PI)) + 0.008f;
                    for (var sign = -1; sign <= 1; sign += 2)
                    {
                        var root = Spine(t) + tangent * (sign == 1 ? 0.004f : 0);
                        var tip = root + side * (sign * length) + tangent * (length * 0.27f);
                        Blade(kit, root, tip, tangent, 0.014f * (0.4f + 0.6f * Mathf.Sin(t * Mathf.PI)) + 0.003f, random.Next(3), -0.005f);
                    }
                }
            }
            MeshObject(fern, "Arching fern fronds", SaveMesh(kit.Build("Fern fronds"), "FernFronds"), new[] {
                Material("FernGreen", new Color(0.14f, 0.39f, 0.16f), 0.11f),
                Material("FernDeepGreen", new Color(0.075f, 0.25f, 0.10f), 0.10f),
                Material("FernFreshGreen", new Color(0.23f, 0.45f, 0.17f), 0.10f),
                Material("GreenStems", new Color(0.12f, 0.25f, 0.055f), 0.10f)
            });
        }

        static void AddCypress(Transform parent, string name, Vector3 position, float height, float yaw)
        {
            var tree = new GameObject(name).transform;
            tree.SetParent(parent, false);
            tree.SetPositionAndRotation(position, Quaternion.Euler(0, yaw, 0));
            tree.localScale = Vector3.one * (height / 1.65f);
            var source = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/Environment/Kenney/Furniture/pottedPlant.fbx");
            var pot = Object.Instantiate(source, tree);
            pot.name = "Cypress terracotta pot";
            foreach (Transform child in pot.transform.Cast<Transform>().ToArray()) Object.DestroyImmediate(child.gameObject);
            pot.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
            pot.transform.localScale = Vector3.one;
            var filter = pot.GetComponent<MeshFilter>();
            var raw = filter.sharedMesh.bounds;
            var horizontal = 0.34f / Mathf.Max(raw.size.x, raw.size.z);
            pot.transform.localScale = new Vector3(horizontal, 0.34f / raw.size.y, horizontal);
            pot.transform.localPosition = -Vector3.Scale(new Vector3(raw.center.x, raw.min.y, raw.center.z), pot.transform.localScale);
            pot.GetComponent<Renderer>().sharedMaterials = new[] {
                Material("FernTerracotta", new Color(0.43f, 0.20f, 0.105f), 0.12f),
                Material("PottingSoil", new Color(0.055f, 0.032f, 0.02f), 0.02f)
            };
            var collider = pot.AddComponent<BoxCollider>(); collider.center = raw.center; collider.size = raw.size;
            MeshObject(tree, "Dense upright cypress foliage", MakeCypress(), new[] {
                Material("CypressGreen", new Color(0.23f, 0.43f, 0.18f), 0.08f),
                Material("CypressShade", new Color(0.12f, 0.30f, 0.12f), 0.06f),
                Material("CypressNewGrowth", new Color(0.36f, 0.55f, 0.25f), 0.08f),
                Material("CypressBark", new Color(0.29f, 0.19f, 0.09f), 0.04f)
            });
        }

        static Mesh MakeCypress()
        {
            const string meshName = "ColumnarCypress";
            const float bottom = 0.42f, crownHeight = 1.20f;
            var kit = new MeshKit();
            var green = kit.Mat("green"); var shade = kit.Mat("shade");
            var young = kit.Mat("young growth"); var bark = kit.Mat("bark");
            var random = new System.Random(916);
            float Radius(float t) => 0.20f * Mathf.Pow(Mathf.Max(0, 1 - t), 0.48f)
                * Mathf.Lerp(0.76f, 1.12f, Mathf.Sin(t * Mathf.PI)) + 0.008f;
            Vector3 Axis(float t) => new Vector3(0.009f * Mathf.Sin(t * 5), bottom + t * crownHeight, 0.006f * t);
            kit.Sweep(new[] { new Vector3(0, 0.28f, 0), Axis(0.30f), Axis(0.68f), Axis(1) },
                new Vector2(0.013f, 0.012f), bark);
            // An irregular closed core gives the plant density, while separate shoots break its outline.
            const int rings = 24, around = 28;
            var vertices = new int[rings + 1, around];
            for (var ring = 0; ring <= rings; ring++)
            for (var a = 0; a < around; a++)
            {
                var t = ring / (float)rings;
                var angle = a * Mathf.PI * 2 / around;
                var radius = Radius(t) * (0.78f + 0.05f * Mathf.Sin(angle * 7 + t * 9)
                    + 0.07f * Mathf.Sin(angle * 11 - t * 15));
                var direction = new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle));
                vertices[ring, a] = kit.Vertex(Axis(t) + direction * radius,
                    (direction + Vector3.up * (0.1f + t * 0.25f)).normalized, new Vector2(a / (float)around, t));
            }
            for (var ring = 0; ring < rings; ring++)
            for (var a = 0; a < around; a++)
            {
                var next = (a + 1) % around;
                kit.Quad((a + ring * 3) % 7 == 0 ? shade : green,
                    vertices[ring, a], vertices[ring, next], vertices[ring + 1, next], vertices[ring + 1, a]);
            }
            var top = kit.Vertex(Axis(1) + Vector3.up * 0.025f, Vector3.up, Vector2.one);
            var foot = kit.Vertex(Axis(0), Vector3.down, Vector2.zero);
            for (var a = 0; a < around; a++)
            {
                kit.Tri(young, vertices[rings, a], vertices[rings, (a + 1) % around], top);
                kit.Tri(shade, vertices[0, (a + 1) % around], vertices[0, a], foot);
            }
            // Overlapping leafy tufts cover the core with a soft, irregular surface.
            for (var row = 0; row < 18; row++)
            for (var tuft = 0; tuft < 14; tuft++)
            {
                var t = 0.008f + row / 17f * 0.92f;
                var angle = tuft * 2 * Mathf.PI / 14 + row * 0.41f + (float)random.NextDouble() * 0.14f;
                var radial = new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle));
                var width = (0.027f + (float)random.NextDouble() * 0.010f) * Mathf.Lerp(1, 0.45f, t);
                var length = (0.13f + (float)random.NextDouble() * 0.020f) * Mathf.Lerp(1, 0.55f, t);
                var basePosition = Axis(t) + radial * Radius(t) * (0.70f + (float)random.NextDouble() * 0.10f);
                var orientation = Quaternion.FromToRotation(Vector3.up, Vector3.up + radial * 0.35f);
                using (kit.At(basePosition, orientation.eulerAngles))
                    kit.Lathe(new List<Vector2> { new(0,0), new(width * 0.76f, length * 0.14f),
                        new(width,length * 0.38f), new(width * 0.76f,length * 0.66f),
                        new(width * 0.31f,length * 0.86f), new(0,length) },
                        random.Next(5) == 0 ? young : green, 7, 55);
            }
            for (var row = 0; row < 15; row++)
            for (var shoot = 0; shoot < 9; shoot++)
            {
                var t = 0.025f + row / 15f * 0.94f + (float)random.NextDouble() * 0.018f;
                var angle = shoot * 2 * Mathf.PI / 9 + row * 0.73f + (float)random.NextDouble() * 0.32f;
                var radial = new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle));
                var side = Vector3.Cross(Vector3.up, radial);
                var radius = Radius(t);
                var start = Axis(t) + radial * radius * 0.50f;
                var tip = Axis(Mathf.Min(1.015f, t + 0.075f)) + radial * radius
                    * (0.94f + (float)random.NextDouble() * 0.17f);
                var tangent = (tip - start).normalized;
                kit.Sweep(new[] { start, Vector3.Lerp(start, tip, 0.6f), tip }, new Vector2(0.0018f, 0.0015f), green);
                for (var pair = 0; pair < 5; pair++)
                {
                    var root = Vector3.Lerp(start, tip, 0.18f + pair * 0.15f);
                    var length = (0.024f + (float)random.NextDouble() * 0.020f) * Mathf.Lerp(1, 0.50f, t);
                    foreach (var sign in new[] { -1, 1 })
                    {
                        var end = root + side * (sign * length * 0.61f) + tangent * length;
                        CypressScale(kit, root, end, radial, length * 0.16f,
                            random.Next(5) == 0 ? young : random.Next(4) == 0 ? shade : green);
                    }
                }
                CypressScale(kit, tip - tangent * 0.02f, tip + tangent * 0.014f, radial, 0.004f, young);
            }
            return SaveMesh(kit.Build("Columnar cypress with scale sprays"), meshName);
        }

        static void CypressScale(MeshKit kit, Vector3 start, Vector3 tip, Vector3 outward, float width, int material)
        {
            var axis = (tip - start).normalized;
            var side = Vector3.Cross(axis, outward).normalized;
            var normal = Vector3.Cross(side, axis).normalized;
            var mid = Vector3.Lerp(start, tip, 0.48f);
            var ridge = mid + normal * width * 0.5f;
            foreach (var sign in new[] { 1f, -1f })
            {
                var n = normal * sign;
                var a = kit.Vertex(start, n, Vector2.zero);
                var b = kit.Vertex(mid - side * width, n, new Vector2(0, 0.48f));
                var c = kit.Vertex(ridge, n, new Vector2(0.5f, 0.48f));
                var d = kit.Vertex(mid + side * width, n, new Vector2(1, 0.48f));
                var e = kit.Vertex(tip, n, Vector2.one);
                kit.Tri(material, a, b, c); kit.Tri(material, a, c, d);
                kit.Tri(material, b, e, c); kit.Tri(material, c, e, d);
            }
        }

        static GameObject GardenModel(Transform parent, string name, string asset, Vector3 position, float height, float yaw)
        {
            var source = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/Environment/Kenney/Graveyard/" + asset + ".fbx");
            var model = Object.Instantiate(source, parent);
            model.name = name;
            model.transform.SetPositionAndRotation(Vector3.zero, Quaternion.Euler(0, yaw, 0));
            model.transform.localScale = Vector3.one;
            var renderer = model.GetComponent<Renderer>();
            model.transform.localScale = Vector3.one * (height / renderer.bounds.size.y);
            var bounds = renderer.bounds;
            model.transform.position += position - new Vector3(bounds.center.x, bounds.min.y, bounds.center.z);
            if (asset.StartsWith("hay"))
            {
                // The original atlas makes the bale and its bindings look reddish indoors.
                // Keep the Kenney geometry but give the straw and rope separate materials.
                var mesh = Object.Instantiate(model.GetComponent<MeshFilter>().sharedMesh);
                var uv = mesh.uv;
                var triangles = mesh.triangles;
                var strawTriangles = new List<int>();
                var bindingTriangles = new List<int>();
                for (var i = 0; i < triangles.Length; i += 3)
                {
                    var u = (uv[triangles[i]].x + uv[triangles[i + 1]].x + uv[triangles[i + 2]].x) / 3;
                    var target = u > 0.4f ? strawTriangles : bindingTriangles;
                    target.Add(triangles[i]); target.Add(triangles[i + 1]); target.Add(triangles[i + 2]);
                }
                if (strawTriangles.Count == 0 || bindingTriangles.Count == 0)
                    throw new InvalidOperationException("Hay bale atlas no longer separates straw from bindings.");
                mesh.subMeshCount = 2;
                mesh.SetTriangles(strawTriangles, 0);
                mesh.SetTriangles(bindingTriangles, 1);
                var vertices = mesh.vertices;
                var normals = mesh.normals;
                var raw = mesh.bounds;
                for (var i = 0; i < uv.Length; i++)
                {
                    var p = vertices[i] - raw.center;
                    uv[i] = Mathf.Abs(normals[i].x) > 0.8f
                        ? new Vector2(p.z / raw.size.z + 0.5f, p.y / raw.size.y + 0.5f)
                        : new Vector2(Mathf.Atan2(p.z, p.y) / (2 * Mathf.PI) + 0.5f, p.x / raw.size.x + 0.5f);
                }
                mesh.uv = uv;
                model.GetComponent<MeshFilter>().sharedMesh = SaveMesh(mesh, "BundledHayBale");
                var straw = Material("HayBale", new Color(0.98f, 0.90f, 0.66f), 0.03f);
                straw.SetTexture("_BaseMap", MakeHayTexture());
                var binding = Material("HayBinding", new Color(0.38f, 0.25f, 0.12f), 0.04f);
                renderer.sharedMaterials = new[] { straw, binding };
            }
            else
            {
                var material = Material("HarvestPumpkin", Color.white, 0.12f);
                material.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Art/Environment/Kenney/Graveyard/Textures/colormap.png"));
                renderer.sharedMaterial = material;
            }
            var local = model.GetComponent<MeshFilter>().sharedMesh.bounds;
            var collider = model.AddComponent<BoxCollider>(); collider.center = local.center; collider.size = local.size;
            return model;
        }

        static Texture2D MakeHayTexture()
        {
            const int size = 256;
            var texture = new Texture2D(size, size, TextureFormat.RGB24, false);
            var pixels = new Color[size * size];
            for (var y = 0; y < size; y++)
            for (var x = 0; x < size; x++)
            {
                var u = x / (float)size; var v = y / (float)size;
                var wave = Mathf.Sin((u * 64 + 0.17f * Mathf.Sin(v * 2 * Mathf.PI + u * 9)) * 2 * Mathf.PI);
                var variation = Mathf.PerlinNoise(u * 37, v * 19);
                pixels[y * size + x] = Color.Lerp(new Color(0.66f, 0.57f, 0.36f), new Color(1, 0.97f, 0.82f),
                    Mathf.Clamp01(0.61f + wave * 0.23f + (variation - 0.5f) * 0.24f));
            }
            texture.SetPixels(pixels); texture.Apply();
            var path = Directory + "/HayStraw.png";
            var bytes = texture.EncodeToPNG();
            Object.DestroyImmediate(texture);
            if (!System.IO.File.Exists(path) || !System.IO.File.ReadAllBytes(path).SequenceEqual(bytes))
            {
                System.IO.File.WriteAllBytes(path, bytes);
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            }
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        static void AddPumpkin(Transform parent, string name, Vector3 position, float height, float yaw)
        {
            var pumpkin = new GameObject(name).transform;
            pumpkin.SetParent(parent, false);
            pumpkin.SetPositionAndRotation(position, Quaternion.Euler(0, yaw, 0));
            var sizeScale = height / 0.39f;
            pumpkin.localScale = Vector3.one * sizeScale;
            var source = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/Environment/Kenney/Graveyard/pumpkin-carved.fbx");
            var original = source.GetComponent<MeshFilter>().sharedMesh;
            // The imported pumpkin contains solid overlapping lobes. Give this candle prop a true hollow shell.
            var mesh = MakeHollowPumpkin(original);
            var shell = Material("CandlePumpkinSkin", Color.white, 0.19f);
            shell.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Art/Environment/Kenney/Graveyard/Textures/colormap.png"));
            shell.SetFloat("_Cull", 0);
            var edges = Material("PumpkinWarmCutEdges", new Color(0.95f, 0.47f, 0.075f), 0.12f);
            edges.EnableKeyword("_EMISSION"); edges.SetColor("_EmissionColor", new Color(1, 0.34f, 0.025f) * 1.1f);
            var warmInterior = Material("PumpkinCandlelitInterior", new Color(0.54f, 0.21f, 0.045f), 0.12f);
            warmInterior.EnableKeyword("_EMISSION");
            warmInterior.SetColor("_EmissionColor", new Color(1, 0.38f, 0.055f) * 1.35f);
            var body = MeshObject(pumpkin, "Hollow pumpkin shell", SaveMesh(mesh, "HollowCarvedPumpkin"), new[] { shell, edges, warmInterior });
            var scale = 0.39f / original.bounds.size.y;
            body.transform.localScale = Vector3.one * scale;
            body.transform.localPosition = Vector3.up * (-original.bounds.min.y * scale);
            var box = body.AddComponent<BoxCollider>(); box.center = original.bounds.center; box.size = original.bounds.size;
            var candle = new MeshKit();
            candle.Lathe(new List<Vector2> {new(0, 0.025f),new(0.027f,0.025f),new(0.028f,0.111f),new(0.024f,0.118f),new(0.007f,0.113f),new(0,0.113f)}, candle.Mat("wax"), 20, 55);
            candle.Sweep(new[] {new Vector3(0,0.112f,0),new Vector3(0.002f,0.127f,0)}, new Vector2(0.002f,0.002f), candle.Mat("wick"));
            MeshObject(pumpkin, "Candle inside pumpkin", SaveMesh(candle.Build("Pumpkin candle"), "PumpkinCandle"), new[] {
                Material("IvoryCandleWax", new Color(0.87f,0.78f,0.57f), 0.13f), Material("CandleWick", new Color(0.035f,0.022f,0.012f), 0)
            });
            var flame = new MeshKit();
            flame.Lathe(new List<Vector2> {new(0,0),new(0.009f,0.008f),new(0.008f,0.020f),new(0.003f,0.036f),new(0,0.045f)}, flame.Mat("fire"), 12, 75);
            var fire = Material("CandleFlame", new Color(1,0.58f,0.13f), 0);
            fire.EnableKeyword("_EMISSION"); fire.SetColor("_EmissionColor", new Color(1,0.49f,0.10f) * 6);
            var flameObject = MeshObject(pumpkin, "Candle flame", SaveMesh(flame.Build("Candle flame"), "CandleFlame"), new[] { fire });
            flameObject.transform.localPosition = new Vector3(0,0.124f,0);
            var light = new GameObject("Warm candle light").AddComponent<Light>();
            light.transform.SetParent(pumpkin, false);
            light.transform.localPosition = new Vector3(0,0.15f,0.025f);
            light.type = LightType.Point; light.color = new Color(1,0.59f,0.22f);
            light.intensity = 0.35f * sizeScale * sizeScale; light.range = 1.05f * sizeScale; light.shadows = LightShadows.Soft;
            light.shadowNearPlane = 0.01f;
            light.shadowBias = 0.003f; light.shadowNormalBias = 0.005f;
            var flicker = light.gameObject.AddComponent<HollowCreek.World.CandleFlicker>();
            flicker.Flame = flameObject.transform;
        }

        static Mesh MakeHollowPumpkin(Mesh source)
        {
            const int around=96, rows=48;
            var outer=new Vector3[around,rows+1];var inner=new Vector3[around,rows+1];var holes=new bool[around,rows];
            var kit=new MeshKit();var skin=kit.Mat("skin");var cut=kit.Mat("cut edge");var warm=kit.Mat("warm inner wall");
            for(var a=0;a<around;a++)for(var r=0;r<=rows;r++)
            {
                var angle=a*2*Mathf.PI/around;var latitude=-Mathf.PI/2+r*Mathf.PI/rows;
                var rib=1+0.035f*Mathf.Cos(angle*8);
                var radius=0.181f*Mathf.Cos(latitude)*rib;
                var insideRadius=0.167f*Mathf.Cos(latitude)*rib;
                outer[a,r]=new Vector3(Mathf.Cos(angle)*radius,0.1135f+0.1135f*Mathf.Sin(latitude),Mathf.Sin(angle)*radius);
                inner[a,r]=new Vector3(Mathf.Cos(angle)*insideRadius,0.1135f+0.102f*Mathf.Sin(latitude),Mathf.Sin(angle)*insideRadius);
            }
            bool InTriangle(Vector2 p,Vector2 a,Vector2 b,Vector2 c)
            {
                float Cross(Vector2 u,Vector2 v)=>u.x*v.y-u.y*v.x;
                var s1=Cross(b-a,p-a);var s2=Cross(c-b,p-b);var s3=Cross(a-c,p-c);
                return (s1>=0 && s2>=0 && s3>=0)||(s1<=0 && s2<=0 && s3<=0);
            }
            bool Aperture(Vector3 p)
            {
                if(p.z<0.07f)return false;
                var point=new Vector2(p.x,p.y);
                if(InTriangle(point,new Vector2(-0.095f,0.143f),new Vector2(-0.048f,0.194f),new Vector2(-0.018f,0.143f))
                    ||InTriangle(point,new Vector2(0.018f,0.143f),new Vector2(0.048f,0.194f),new Vector2(0.095f,0.143f)))return true;
                var half=0.085f;var x=Mathf.Abs(p.x);
                var top=0.078f+0.18f*x;
                // A few teeth remain in the otherwise open smile.
                if((x<0.012f || (x>0.040f && x<0.053f)))top-=0.012f;
                var bottom=0.038f+0.38f*x;
                return x<half && p.y>bottom && p.y<top;
            }
            for(var a=0;a<around;a++)for(var r=0;r<rows;r++)
            {
                var next=(a+1)%around;
                holes[a,r]=Aperture((outer[a,r]+outer[next,r]+outer[next,r+1]+outer[a,r+1])/4);
            }
            Vector3 Normal(Vector3 p)=>new Vector3(p.x, (p.y-0.1135f)*2.5f, p.z).normalized;
            void Surface(Vector3 a,Vector3 b,Vector3 c,Vector3 d,int material,bool reverse=false)
            {
                var sign=reverse?-1f:1f;
                int V(Vector3 p)=>kit.Vertex(p,Normal(p)*sign,new Vector2(0.46875f,0.025f+p.y*0.89f));
                kit.Quad(material,V(a),V(b),V(c),V(d));
            }
            void Rim(Vector3 a,Vector3 b,Vector3 c,Vector3 d)
            {
                var n=Vector3.Cross(b-a,c-a).normalized;
                kit.Quad(cut,kit.Vertex(a,n,Vector2.zero),kit.Vertex(b,n,Vector2.zero),kit.Vertex(c,n,Vector2.zero),kit.Vertex(d,n,Vector2.zero));
            }
            for(var a=0;a<around;a++)for(var r=0;r<rows;r++)
            {
                if(holes[a,r])continue;var next=(a+1)%around;var prev=(a+around-1)%around;
                Surface(outer[a,r],outer[next,r],outer[next,r+1],outer[a,r+1],skin);
                Surface(inner[a,r],inner[next,r],inner[next,r+1],inner[a,r+1],warm,true);
                if(holes[prev,r])Rim(outer[a,r+1],outer[a,r],inner[a,r],inner[a,r+1]);
                if(holes[next,r])Rim(outer[next,r],outer[next,r+1],inner[next,r+1],inner[next,r]);
                if(r>0 && holes[a,r-1])Rim(outer[a,r],outer[next,r],inner[next,r],inner[a,r]);
                if(r<rows-1 && holes[a,r+1])Rim(outer[next,r+1],outer[a,r+1],inner[a,r+1],inner[next,r+1]);
            }
            // Keep the Kenney stem and its original atlas colours.
            var atlas=new Texture2D(2,2);atlas.LoadImage(System.IO.File.ReadAllBytes("Assets/Art/Environment/Kenney/Graveyard/Textures/colormap.png"));
            var tr=source.triangles;var uv=source.uv;var vertices=source.vertices;var normals=source.normals;
            for(var i=0;i<tr.Length;i+=3)
            {
                var colour=atlas.GetPixelBilinear((uv[tr[i]].x+uv[tr[i+1]].x+uv[tr[i+2]].x)/3,(uv[tr[i]].y+uv[tr[i+1]].y+uv[tr[i+2]].y)/3);
                if(colour.g<colour.r*1.3f)continue;
                kit.Tri(skin,kit.Vertex(vertices[tr[i]],normals[tr[i]],uv[tr[i]]),kit.Vertex(vertices[tr[i+1]],normals[tr[i+1]],uv[tr[i+1]]),kit.Vertex(vertices[tr[i+2]],normals[tr[i+2]],uv[tr[i+2]]));
            }
            Object.DestroyImmediate(atlas);
            return kit.Build("Hollow ribbed candle pumpkin");
        }

        static Mesh MakeVase()
        {
            var kit = new MeshKit();
            kit.Lathe(new List<Vector2> {new(0,0),new(0.024f,0),new(0.030f,0.005f),new(0.038f,0.046f),
                new(0.036f,0.084f),new(0.025f,0.126f),new(0.021f,0.146f),new(0.021f,0.153f),
                new(0.016f,0.153f),new(0.016f,0.144f),new(0.021f,0.126f),new(0.030f,0.080f),new(0.020f,0.018f),new(0,0.018f)}, kit.Mat("ceramic"), 28, 65);
            return SaveMesh(kit.Build("Small flower vase"), "SmallVase");
        }

        static Mesh MakeGerbera()
        {
            var kit = new MeshKit();
            var red = kit.Mat("red"); var burgundy = kit.Mat("burgundy"); var middle = kit.Mat("centre"); var pollen = kit.Mat("pollen");
            for (var layer = 0; layer < 2; layer++)
            for (var p = 0; p < 28; p++)
            {
                var angle = (p + layer * 0.5f) * 2 * Mathf.PI / 28;
                var direction = new Vector3(Mathf.Cos(angle),0,Mathf.Sin(angle));
                var width = Vector3.Cross(Vector3.up, direction);
                var length = layer == 0 ? 0.053f + 0.002f * Mathf.Sin(p * 7.1f) : 0.037f;
                var start = direction * 0.010f + Vector3.up * (layer * 0.003f);
                Blade(kit, start, direction * length + Vector3.up * (layer == 0 ? -0.006f : 0.006f), width,
                    layer == 0 ? 0.0055f : 0.0038f, p % 4 == 0 ? burgundy : red, 0.004f);
            }
            kit.Sphere(new Vector3(0,0.005f,0),0.015f,middle,20,0.33f);
            for (var p=0;p<22;p++)
            {
                var angle=p*2*Mathf.PI/22;
                kit.Sphere(new Vector3(Mathf.Cos(angle)*0.012f,0.010f,Mathf.Sin(angle)*0.012f),0.0012f,pollen,6);
            }
            return SaveMesh(kit.Build("Dark red gerbera"), "DarkRedGerbera");
        }

        static void AddVase(Transform parent, string name, Vector3 position, float yaw, Mesh vase, Mesh flower)
        {
            var group = new GameObject(name).transform;
            group.SetParent(parent, false); group.SetPositionAndRotation(position, Quaternion.Euler(0,yaw,0));
            MeshObject(group, "Small ceramic vase", vase, new[] {Material("VaseCreamCeramic", new Color(0.79f,0.71f,0.59f),0.30f)});
            var flowerMaterials=new[] {Material("GerberaWineRed",new Color(0.53f,0.043f,0.065f),0.15f),
                Material("GerberaBurgundy",new Color(0.35f,0.018f,0.04f),0.12f),
                Material("GerberaCentre",new Color(0.08f,0.024f,0.016f),0.09f),
                Material("GerberaPollen",new Color(0.32f,0.14f,0.045f),0.08f)};
            var heads=new[]{new Vector3(-0.049f,0.307f,0.008f),new Vector3(0.049f,0.349f,0.020f),new Vector3(0.003f,0.278f,-0.055f)};
            var stems = new MeshKit();var stem=stems.Mat("stem");var leaf=stems.Mat("leaf");
            for(var i=0;i<3;i++)
            {
                var head=MeshObject(group,"Gerbera "+(i+1),flower,flowerMaterials);
                head.transform.localPosition=heads[i];
                head.transform.localRotation=Quaternion.FromToRotation(Vector3.up,new Vector3(heads[i].x*3,1,-0.20f-i*0.08f));
                head.transform.localScale=Vector3.one*(i==1?1.0f:0.91f);
                var foot=new Vector3((i-1)*0.007f,0.065f,0);
                stems.Sweep(new[]{foot,Vector3.Lerp(foot,heads[i],0.48f)+new Vector3(0.005f,0,0.006f),heads[i]},new Vector2(0.003f,0.003f),stem);
                var start=Vector3.Lerp(foot,heads[i],0.54f);
                Blade(stems,start,start+new Vector3(i==0?-0.033f:0.029f,0.017f,-0.025f),Vector3.up,0.007f,leaf,0.002f);
            }
            MeshObject(group,"Three flower stems",SaveMesh(stems.Build("Gerbera stems"),"GerberaStems"),new[]{
                Material("FlowerStems",new Color(0.12f,0.27f,0.065f),0.10f),Material("FlowerLeaves",new Color(0.075f,0.22f,0.075f),0.12f)});
        }

        static void Blade(MeshKit kit, Vector3 root, Vector3 tip, Vector3 widthAxis, float width, int material, float bow)
        {
            var length=(tip-root).normalized;
            var normal=Vector3.Cross(length,widthAxis).normalized;
            for(var segment=0;segment<5;segment++)
            {
                var t0=segment/5f;var t1=(segment+1)/5f;
                Vector3 Centre(float t)=>Vector3.Lerp(root,tip,t)+normal*(bow*Mathf.Sin(t*Mathf.PI));
                var a=Centre(t0);var b=Centre(t1);
                var w0=width*Mathf.Sin(t0*Mathf.PI);var w1=width*Mathf.Sin(t1*Mathf.PI);
                foreach(var sign in new[]{1f,-1f})
                {
                    var v0=kit.Vertex(a-widthAxis*w0,normal*sign,new Vector2(0,t0));
                    var v1=kit.Vertex(a+widthAxis*w0,normal*sign,new Vector2(1,t0));
                    var v2=kit.Vertex(b+widthAxis*w1,normal*sign,new Vector2(1,t1));
                    var v3=kit.Vertex(b-widthAxis*w1,normal*sign,new Vector2(0,t1));
                    kit.Quad(material,v0,v1,v2,v3);
                }
            }
        }

        static Material Material(string name, Color colour, float smoothness)
        {
            var path=Directory+"/"+name+".mat";
            var mat=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(!mat){mat=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(mat,path);}
            mat.SetColor("_BaseColor",colour);mat.SetFloat("_Smoothness",smoothness);EditorUtility.SetDirty(mat);return mat;
        }

        static GameObject MeshObject(Transform parent,string name,Mesh mesh,Material[] materials)
        {
            var go=new GameObject(name);go.transform.SetParent(parent,false);
            go.AddComponent<MeshFilter>().sharedMesh=mesh;go.AddComponent<MeshRenderer>().sharedMaterials=materials;return go;
        }

        static Mesh SaveMesh(Mesh built,string name)
        {
            var path=Directory+"/"+name+".asset";var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if(!mesh){mesh=built;mesh.name=name;AssetDatabase.CreateAsset(mesh,path);}
            else
            {
                mesh.Clear();mesh.indexFormat=built.indexFormat;mesh.vertices=built.vertices;mesh.normals=built.normals;mesh.uv=built.uv;mesh.tangents=built.tangents;
                mesh.subMeshCount=built.subMeshCount;for(var i=0;i<built.subMeshCount;i++)mesh.SetTriangles(built.GetTriangles(i),i);
                mesh.bounds=built.bounds;EditorUtility.SetDirty(mesh);Object.DestroyImmediate(built);
            }
            return mesh;
        }
    }
}
