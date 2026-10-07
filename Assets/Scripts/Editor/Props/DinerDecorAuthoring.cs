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
    /// <summary>Visual diner dressing, separate from the interactive narrative props.</summary>
    public static class DinerDecorAuthoring
    {
        const string Directory = "Assets/Art/Environment/DinerInterior/Decor";
        public const string PrefabPath = Directory + "/DinerDecor.prefab";

        [MenuItem("Hollow Creek/Реквизит/Обновить осеннюю гирлянду")]
        public static void RefreshGarland()
        {
            var scene = SceneManager.GetActiveScene();
            if (EditorApplication.isPlaying || !scene.path.EndsWith("/Diner.unity"))
                throw new InvalidOperationException("Open Diner in Edit mode before updating its garland.");
            var contents = PrefabUtility.LoadPrefabContents(PrefabPath);
            try
            {
                var old = contents.transform.Find("Autumn leaf garland");
                if (old) Object.DestroyImmediate(old.gameObject);
                AddGarland(contents.transform);
                PrefabUtility.SaveAsPrefabAsset(contents, PrefabPath);
            }
            finally { PrefabUtility.UnloadPrefabContents(contents); }
            AssetDatabase.SaveAssets();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            SceneView.RepaintAll();
        }

        [MenuItem("Hollow Creek/Реквизит/Украшения и сервировка закусочной")]
        public static void Build()
        {
            var scene = SceneManager.GetActiveScene();
            if (EditorApplication.isPlaying || !scene.path.EndsWith("/Diner.unity"))
                throw new InvalidOperationException("Open Diner in Edit mode before placing decorations.");
            var diner = GameObject.Find("[Location] Diner");
            if (!diner) throw new InvalidOperationException("Diner root is missing.");
            var bar = diner.GetComponentsInChildren<Transform>().First(t => t.name == "Window bar table");
            var barBounds = bar.GetComponent<Renderer>().bounds;
            System.IO.Directory.CreateDirectory(Directory);
            AssetDatabase.Refresh();
            var stage = new GameObject("Diner decorations");
            try
            {
                AddBench(stage.transform, diner);
                SetTheBar(stage.transform, barBounds.max.y);
                AddPoster(stage.transform, "Coffee poster", "CoffeePoster", new Vector3(-3.3f, 1.95f, 0.09f));
                AddPoster(stage.transform, "Pie poster", "PiePoster", new Vector3(3.3f, 1.95f, 0.09f));
                AddGarland(stage.transform);
                AssetDatabase.SaveAssets();
                var prefab = PrefabUtility.SaveAsPrefabAsset(stage, PrefabPath);
                foreach (var old in diner.GetComponentsInChildren<Transform>(true)
                    .Where(t => PrefabUtility.IsAnyPrefabInstanceRoot(t.gameObject)
                        && PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(t.gameObject) == PrefabPath).ToArray())
                    Object.DestroyImmediate(old.gameObject);
                var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                instance.name = "Diner decorations";
                instance.transform.SetParent(diner.transform.Find("Art"), false);
                PrefabUtility.RecordPrefabInstancePropertyModifications(instance);
                foreach (var name in new[] { "NPC Mara", "NPC Owen" })
                {
                    var animator = GameObject.Find(name)?.GetComponentInChildren<Animator>();
                    if (animator && animator.runtimeAnimatorController)
                        animator.runtimeAnimatorController.animationClips.First(c => c.name.Contains("Idle")).SampleAnimation(animator.gameObject, 0);
                }
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
                Selection.activeGameObject = null;
            }
            finally { Object.DestroyImmediate(stage); }
        }

        static void AddBench(Transform stage, GameObject diner)
        {
            var source = diner.transform.Find("Art/benchCushion");
            var table = diner.transform.Find("Art/tableCross");
            if (!source || !table) throw new InvalidOperationException("Mara's existing bench or table is missing.");
            var original = Bounds(source.gameObject);
            var tableBounds = Bounds(table.gameObject);
            var bench = Object.Instantiate(source.gameObject, stage);
            bench.name = "Mara opposite bench";
            bench.transform.rotation = Quaternion.Euler(0, source.eulerAngles.y + 180, 0);
            bench.transform.localScale = source.lossyScale;
            var bounds = Bounds(bench);
            // Mirror the existing seating arrangement around the centre of Mara's table.
            var target = new Vector3(2 * tableBounds.center.x - original.center.x, original.center.y, original.center.z);
            bench.transform.position += target - bounds.center;
            var collider = bench.AddComponent<BoxCollider>();
            var local = bench.GetComponent<MeshFilter>().sharedMesh.bounds;
            collider.center = local.center;
            collider.size = local.size;
        }

        static void SetTheBar(Transform stage, float top)
        {
            var settings = new GameObject("Window bar coffee and pie").transform;
            settings.SetParent(stage, false);
            var mugPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/Props/DinerMug.prefab");
            var mugMesh = Object.Instantiate(mugPrefab.GetComponentInChildren<MeshFilter>().sharedMesh);
            // The lipstick mark belongs only to Mara's clue mug.
            mugMesh.SetTriangles(Array.Empty<int>(), 3);
            var cleanMesh = SaveMesh(mugMesh, "CleanCoffeeMug");
            var placements = new[] {
                new Vector3(5.39f, 25, 1.66f), new Vector3(5.63f, 210, 2.85f),
                new Vector3(5.37f, 130, 3.89f), new Vector3(5.60f, 325, 5.08f),
                new Vector3(5.39f, 75, 6.19f)
            };
            for (var i = 0; i < placements.Length; i++)
            {
                var p = placements[i];
                var mug = PlaceProp("DinerMug", "Coffee mug " + (i + 1), settings, new Vector3(p.x, top, p.z), p.y, 1.15f);
                mug.GetComponentInChildren<MeshFilter>().sharedMesh = cleanMesh;
            }
            var pies = new[] {
                new Vector3(5.62f, -15, 1.97f), new Vector3(5.39f, 78, 3.16f),
                new Vector3(5.60f, 180, 4.27f), new Vector3(5.62f, 240, 6.49f)
            };
            for (var i = 0; i < pies.Length; i++)
            {
                var p = pies[i];
                PlaceProp("PieSlice", "Pie on plate " + (i + 1), settings, new Vector3(p.x, top, p.z), p.y, 1.15f);
            }
        }

        static GameObject PlaceProp(string asset, string name, Transform parent, Vector3 position, float yaw, float scale)
        {
            var source = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/Props/" + asset + ".prefab");
            var instance = Object.Instantiate(source, parent);
            instance.name = name;
            instance.transform.SetPositionAndRotation(position, Quaternion.Euler(0, yaw, 0));
            instance.transform.localScale = Vector3.one * scale;
            return instance;
        }

        static void AddPoster(Transform stage, string name, string textureName, Vector3 position)
        {
            var texturePath = Directory + "/" + textureName + ".png";
            var importer = AssetImporter.GetAtPath(texturePath) as TextureImporter;
            if (!importer) throw new InvalidOperationException("Missing poster artwork: " + texturePath);
            importer.textureType = TextureImporterType.Default;
            importer.sRGBTexture = true;
            importer.mipmapEnabled = true;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.maxTextureSize = 2048;
            importer.SaveAndReimport();
            var paper = Material(textureName, new Color(0.94f, 0.91f, 0.84f), 0.05f);
            paper.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath));
            var frame = Material("PosterWalnut", new Color(0.18f, 0.085f, 0.038f), 0.20f);
            var kit = new MeshKit();
            var wood = kit.Mat("frame");
            var print = kit.Mat("paper");
            const float w = 0.68f, h = 1.02f, rim = 0.022f;
            kit.Box(Vector3.zero, new Vector3(w + rim * 2, h + rim * 2, 0.018f), wood, 0.002f);
            kit.Box(new Vector3(0, 0, 0.0095f), new Vector3(w, h, 0.001f), print, 0, Uv.Fit());
            var poster = MeshObject(stage, name, SaveMesh(kit.Build(name), textureName + "Frame"), new[] { frame, paper });
            poster.transform.position = position;
        }

        static void AddGarland(Transform stage)
        {
            var garland = new GameObject("Autumn leaf garland").transform;
            garland.SetParent(stage, false);
            var golden = Material("GoldenLeaf", new Color(1.0f, 0.80f, 0.15f), 0.08f);
            var red = Material("RedLeaf", new Color(0.68f, 0.12f, 0.055f), 0.08f);
            var vein = Material("LeafVein", new Color(0.35f, 0.15f, 0.035f), 0.05f);
            var cord = Material("GarlandTwine", new Color(0.33f, 0.24f, 0.13f), 0.02f);
            var leaves = Enumerable.Range(0, 4).Select(MakeLeaf).ToArray();
            var wall = GameObject.Find("[Location] Diner").transform.Find("Geometry/Wall North").GetComponent<Renderer>();
            var wallZ = wall.bounds.min.z;
            var cordZ = wallZ - 0.006f;
            var path = new List<Vector3>();
            for (var i = 0; i <= 80; i++)
            {
                var t = i / 80f;
                path.Add(GarlandPoint(t, cordZ));
            }
            var rope = new MeshKit();
            rope.Sweep(path, new Vector2(0.006f, 0.006f), rope.Mat("twine"));
            var ends = new List<Vector3[]>();
            for (var side = 0; side < 2; side++)
            {
                var end = new List<Vector3>();
                for (var j = 0; j <= 20; j++)
                {
                    var t = j / 20f;
                    end.Add(new Vector3((side == 0 ? -0.53f : 3.85f) + 0.019f * Mathf.Sin(t * 6 + side) * t,
                        3.08f - t * 0.79f, cordZ));
                }
                ends.Add(end.ToArray());
                rope.Sweep(end, new Vector2(0.006f, 0.006f), 0);
            }
            // Small wall pins support the uneven, hand-tied swags.
            foreach (var t in new[] { 0f, 0.52f, 1f })
                rope.Sphere(GarlandPoint(t, wallZ - 0.0038f), 0.0035f, 0, 8);
            var random = new System.Random(627);
            for (var i = 0; i < 57; i++)
            {
                var t = (i + 0.08f + (float)random.NextDouble() * 0.84f) / 57f;
                AddLeaf(garland, leaves, golden, red, vein, random, i, GarlandPoint(t, cordZ), wallZ, rope);
            }
            for (var side = 0; side < 2; side++)
            for (var j = 0; j < 7; j++)
            {
                var t = 0.10f + (j + (float)random.NextDouble() * 0.6f) / 7f * 0.81f;
                var index = Mathf.Min(19, Mathf.FloorToInt(t * 20));
                var knot = Vector3.Lerp(ends[side][index], ends[side][index + 1], t * 20 - index);
                AddLeaf(garland, leaves, golden, red, vein, random, 57 + side * 7 + j, knot, wallZ, rope);
            }
            MeshObject(garland, "Garland cord", SaveMesh(rope.Build("Garland cord"), "GarlandCord"), new[] { cord });
        }

        static Vector3 GarlandPoint(float t, float z)
        {
            var u = t < 0.52f ? t / 0.52f : (t - 0.52f) / 0.48f;
            var sag = t < 0.52f ? 0.145f : 0.112f;
            return new Vector3(Mathf.Lerp(-0.53f, 3.85f, t),
                3.08f - sag * Mathf.Sin(Mathf.PI * u) + 0.009f * Mathf.Sin(3 * Mathf.PI * u), z);
        }

        static void AddLeaf(Transform garland, Mesh[] meshes, Material golden, Material red, Material vein,
            System.Random random, int i, Vector3 cordPoint, float wallZ, MeshKit rope)
        {
            var isRed = random.NextDouble() < 0.40;
            var mesh = meshes[random.Next(meshes.Length)];
            var leaf = MeshObject(garland, (isRed ? "Red" : "Yellow") + " maple leaf " + (i + 1), mesh,
                new[] { isRed ? red : golden, vein });
            var scale = 0.082f + (float)random.NextDouble() * 0.103f;
            leaf.transform.localScale = new Vector3(scale * (0.66f + (float)random.NextDouble() * 0.28f), scale, scale);
            var rotation = Quaternion.Euler(-8 + (float)random.NextDouble() * 16,
                -10 + (float)random.NextDouble() * 20, 105 + (float)random.NextDouble() * 150);
            var stemTip = new Vector3(0, -0.085f, 0);
            var knot = cordPoint + new Vector3(-0.009f + (float)random.NextDouble() * 0.018f,
                -0.003f - (float)random.NextDouble() * 0.018f, 0);
            leaf.transform.SetPositionAndRotation(knot - rotation * Vector3.Scale(stemTip, leaf.transform.localScale), rotation);
            // Rest the folded blade against the wall without burying any vertex in the paint.
            var nearest = mesh.vertices.Max(v => leaf.transform.TransformPoint(v).z);
            leaf.transform.position += Vector3.forward * (wallZ - 0.0015f - nearest);
            var attachedStem = leaf.transform.TransformPoint(stemTip);
            var bend = Vector3.Lerp(cordPoint, attachedStem, 0.5f) + Vector3.down * 0.002f;
            rope.Sweep(new[] { cordPoint, bend, attachedStem }, new Vector2(0.0015f, 0.0015f), 0);
            rope.Torus(attachedStem, 0.0026f, 0.0007f, 0, 10, 4);
        }

        static Mesh MakeLeaf(int variant)
        {
            // Folded, two-sided maple blade with actual depth and raised veins.
            var outline = new Vector2[] { new(0, 0), new(-0.18f, 0.15f), new(-0.55f, 0.12f), new(-0.42f, 0.32f),
                new(-0.64f, 0.48f), new(-0.30f, 0.50f), new(-0.39f, 0.80f), new(-0.13f, 0.70f), new(0, 1),
                new(0.13f, 0.70f), new(0.39f, 0.80f), new(0.30f, 0.50f), new(0.64f, 0.48f),
                new(0.42f, 0.32f), new(0.55f, 0.12f), new(0.18f, 0.15f) };
            var variation = new System.Random(715 + variant * 117);
            for (var i = 1; i < outline.Length; i++)
                outline[i] = new Vector2(outline[i].x * (0.90f + (float)variation.NextDouble() * 0.20f),
                    outline[i].y * (0.94f + (float)variation.NextDouble() * 0.12f));
            var fold = 0.070f + variant * 0.017f;
            var kit = new MeshKit();
            var blade = kit.Mat("leaf");
            var vein = kit.Mat("vein");
            var centre = new Vector3(0, 0.45f, fold);
            for (var i = 0; i < outline.Length; i++)
            {
                var a = new Vector3(outline[i].x, outline[i].y, 0.015f * Mathf.Sin(outline[i].y * Mathf.PI));
                var b = new Vector3(outline[(i + 1) % outline.Length].x, outline[(i + 1) % outline.Length].y, 0);
                var normal = Vector3.Cross(a - centre, b - centre).normalized;
                foreach (var sign in new[] { 1f, -1f })
                    kit.Tri(blade, kit.Vertex(centre, normal * sign, Vector2.zero),
                        kit.Vertex(a, normal * sign, Vector2.zero), kit.Vertex(b, normal * sign, Vector2.zero));
            }
            kit.Sweep(new[] {new Vector3(0, -0.09f, 0), new Vector3(0, 0.45f, fold + 0.005f), new Vector3(0, 0.87f, 0.028f)},
                new Vector2(0.018f, 0.012f), vein);
            foreach (var side in new[] { -1f, 1f })
            {
                kit.Sweep(new[] {new Vector3(0, 0.40f, fold + 0.002f), new Vector3(side * 0.49f, 0.44f, 0.022f)}, new Vector2(0.010f, 0.006f), vein);
                kit.Sweep(new[] {new Vector3(0, 0.46f, fold + 0.005f), new Vector3(side * 0.28f, 0.71f, 0.030f)}, new Vector2(0.010f, 0.006f), vein);
            }
            return SaveMesh(kit.Build("Folded maple leaf"), variant == 0 ? "MapleLeaf" : "MapleLeaf" + variant);
        }

        static GameObject MeshObject(Transform parent, string name, Mesh mesh, Material[] materials)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            go.AddComponent<MeshRenderer>().sharedMaterials = materials;
            return go;
        }

        static Material Material(string name, Color colour, float smoothness)
        {
            var path = Directory + "/" + name + ".mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (!material) { material = new Material(Shader.Find("Universal Render Pipeline/Lit")); AssetDatabase.CreateAsset(material, path); }
            material.SetColor("_BaseColor", colour);
            material.SetFloat("_Smoothness", smoothness);
            EditorUtility.SetDirty(material);
            return material;
        }

        static Mesh SaveMesh(Mesh built, string name)
        {
            var path = Directory + "/" + name + ".asset";
            var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (!mesh) { mesh = built; mesh.name = name; AssetDatabase.CreateAsset(mesh, path); }
            else
            {
                mesh.Clear(); mesh.vertices = built.vertices; mesh.normals = built.normals;
                mesh.uv = built.uv; mesh.tangents = built.tangents; mesh.subMeshCount = built.subMeshCount;
                for (var i = 0; i < built.subMeshCount; i++) mesh.SetTriangles(built.GetTriangles(i), i);
                mesh.bounds = built.bounds; EditorUtility.SetDirty(mesh); Object.DestroyImmediate(built);
            }
            return mesh;
        }

        static Bounds Bounds(GameObject model)
        {
            var renderers = model.GetComponentsInChildren<Renderer>();
            var bounds = renderers[0].bounds;
            foreach (var renderer in renderers) bounds.Encapsulate(renderer.bounds);
            return bounds;
        }
    }
}
