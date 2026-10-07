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
    public static class DinerTableclothAuthoring
    {
        const string Directory = "Assets/Art/Environment/DinerInterior/Furniture/Tablecloths";
        public const string PrefabPath = Directory + "/DinerTablecloths.prefab";
        public const float SurfaceLift = 0.004f;
        const float HalfWidth = 0.56f;
        const float PatternRepeat = 0.112f;

        [MenuItem("Hollow Creek/Реквизит/Клетчатые скатерти закусочной")]
        public static void Build()
        {
            var scene = SceneManager.GetActiveScene();
            if (EditorApplication.isPlaying || !scene.path.EndsWith("/Diner.unity"))
                throw new InvalidOperationException("Open Diner in Edit mode before placing tablecloths.");
            var diner = GameObject.Find("[Location] Diner");
            var tables = diner.GetComponentsInChildren<Transform>()
                .Where(t => t.name.StartsWith("Two-seat table ")).OrderBy(t => t.name).ToArray();
            if (tables.Length != 4) throw new InvalidOperationException("Expected four small diner tables.");
            System.IO.Directory.CreateDirectory(Directory);
            AssetDatabase.Refresh();
            var material = FabricMaterial();
            var stage = new GameObject("Red and white tablecloths");
            try
            {
                for (var i = 0; i < tables.Length; i++)
                {
                    var table = tables[i].Find("Round table");
                    var bounds = table.GetComponent<Renderer>().bounds;
                    var centre = new Vector3(bounds.center.x, bounds.max.y, bounds.center.z);
                    var outline = table.GetComponent<MeshFilter>().sharedMesh.vertices
                        .Select(v => table.TransformPoint(v))
                        .Where(p => p.y > bounds.max.y - 0.002f)
                        .Select(p => new Vector2(p.x - centre.x, p.z - centre.z))
                        .Distinct().OrderBy(p => Mathf.Atan2(p.y, p.x)).ToArray();
                    var cloth = new GameObject("Checked tablecloth " + (i + 1));
                    cloth.transform.SetParent(stage.transform, false);
                    cloth.transform.position = centre;
                    cloth.AddComponent<MeshFilter>().sharedMesh = SaveMesh(
                        Drape(outline, i), Directory + "/DrapedTablecloth" + (i + 1) + ".asset");
                    var renderer = cloth.AddComponent<MeshRenderer>();
                    renderer.sharedMaterial = material;
                    renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.TwoSided;
                    renderer.receiveShadows = true;
                }
                AssetDatabase.SaveAssets();
                var prefab = PrefabUtility.SaveAsPrefabAsset(stage, PrefabPath);
                foreach (var old in diner.GetComponentsInChildren<Transform>(true)
                    .Where(t => PrefabUtility.IsAnyPrefabInstanceRoot(t.gameObject)
                        && PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(t.gameObject) == PrefabPath).ToArray())
                    Object.DestroyImmediate(old.gameObject);
                var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                instance.name = stage.name;
                instance.transform.SetParent(diner.transform.Find("Art"), false);
                PrefabUtility.RecordPrefabInstancePropertyModifications(instance);
                SeatVases(diner, tables);
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
                Selection.activeGameObject = null;
                SceneView.RepaintAll();
            }
            finally { Object.DestroyImmediate(stage); }
        }

        static float SupportRadius(Vector2 direction, Vector2[] polygon)
        {
            var radius = float.PositiveInfinity;
            for (var i = 0; i < polygon.Length; i++)
            {
                var a = polygon[i];
                var b = polygon[(i + 1) % polygon.Length];
                var edge = b - a;
                var outward = new Vector2(edge.y, -edge.x).normalized;
                var denominator = Vector2.Dot(outward, direction);
                if (denominator > 0.00001f)
                    radius = Mathf.Min(radius, Vector2.Dot(outward, a) / denominator);
            }
            return radius;
        }

        static Mesh Drape(Vector2[] outline, int variation)
        {
            const int divisions = 80;
            var positions = new List<Vector3>();
            var uvs = new List<Vector2>();
            var triangles = new List<int>();
            var rotation = Quaternion.Euler(0, new[] { 7f, -11f, 15f, -4f }[variation], 0);
            var phase = variation * 1.73f;
            for (var z = 0; z <= divisions; z++)
            for (var x = 0; x <= divisions; x++)
            {
                var sheet = new Vector2(Mathf.Lerp(-HalfWidth, HalfWidth, x / (float)divisions),
                    Mathf.Lerp(-HalfWidth, HalfWidth, z / (float)divisions));
                var p = rotation * new Vector3(sheet.x, 0, sheet.y);
                var direction = new Vector2(p.x, p.z).normalized;
                var radius = sheet.magnitude;
                var support = radius < 0.0001f ? 1 : SupportRadius(direction, outline);
                var excess = radius - (support - 0.006f);
                var height = SurfaceLift;
                if (excess > 0)
                {
                    const float bendRadius = 0.010f;
                    var bendLength = bendRadius * Mathf.PI / 2;
                    var bend = Mathf.Min(excess / bendRadius, Mathf.PI / 2);
                    var fall = Mathf.Max(0, excess - bendLength);
                    var angle = Mathf.Atan2(direction.y, direction.x);
                    var fold = Mathf.Sin(angle * 12 + phase) * 0.011f
                        + Mathf.Sin(angle * 19 - phase) * 0.004f;
                    var foldedRadius = support - 0.006f + bendRadius * Mathf.Sin(bend)
                        + fall * 0.12f + fold * Mathf.SmoothStep(0, 1, fall / 0.15f);
                    p = new Vector3(direction.x * foldedRadius, 0, direction.y * foldedRadius);
                    height -= bendRadius * (1 - Mathf.Cos(bend)) + fall * 0.98f;
                    height += 0.003f * Mathf.Sin(angle * 8 + phase) * Mathf.Clamp01(fall / 0.08f);
                }
                else
                    height += 0.00035f * Mathf.Sin(sheet.x * 27) * Mathf.Sin(sheet.y * 22)
                        * Mathf.Clamp01(radius / 0.16f);
                // A narrow sewn hem makes the border slightly fuller than the fabric.
                var border = HalfWidth - Mathf.Max(Mathf.Abs(sheet.x), Mathf.Abs(sheet.y));
                height += 0.001f * Mathf.Exp(-border * border / 0.000016f);
                positions.Add(new Vector3(p.x, height, p.z));
                uvs.Add(sheet / PatternRepeat + Vector2.one * 0.25f);
            }
            var stride = divisions + 1;
            void Tri(int a, int b, int c) { triangles.Add(a); triangles.Add(b); triangles.Add(c); }
            for (var z = 0; z < divisions; z++)
            for (var x = 0; x < divisions; x++)
            {
                var a = z * stride + x;
                Tri(a, a + stride, a + stride + 1);
                Tri(a, a + stride + 1, a + 1);
            }
            var mesh = new Mesh { name = "Draped checked tablecloth " + (variation + 1) };
            mesh.SetVertices(positions); mesh.SetUVs(0, uvs); mesh.SetTriangles(triangles, 0);
            mesh.RecalculateNormals();
            var normals = mesh.normals;
            var count = positions.Count;
            for (var i = 0; i < count; i++)
            {
                positions.Add(positions[i] - normals[i] * 0.0012f);
                uvs.Add(uvs[i]);
            }
            var frontTriangles = triangles.ToArray();
            for (var i = 0; i < frontTriangles.Length; i += 3)
                Tri(frontTriangles[i] + count, frontTriangles[i + 2] + count, frontTriangles[i + 1] + count);
            void Hem(int a, int b) { Tri(a, b, b + count); Tri(a, b + count, a + count); }
            for (var i = 0; i < divisions; i++)
            {
                Hem(i + 1, i);
                Hem(divisions * stride + i, divisions * stride + i + 1);
                Hem(i * stride, (i + 1) * stride);
                Hem((i + 1) * stride + divisions, i * stride + divisions);
            }
            mesh.SetVertices(positions); mesh.SetUVs(0, uvs); mesh.SetTriangles(triangles, 0);
            mesh.RecalculateNormals(); mesh.RecalculateTangents(); mesh.RecalculateBounds();
            return mesh;
        }

        static Material FabricMaterial()
        {
            const int size = 512;
            var texture = new Texture2D(size, size, TextureFormat.RGB24, false);
            var pixels = new Color[size * size];
            var red = new Color(0.66f, 0.075f, 0.065f);
            var white = new Color(0.95f, 0.94f, 0.91f);
            for (var y = 0; y < size; y++)
            for (var x = 0; x < size; x++)
            {
                var colour = ((x / (size / 2) + y / (size / 2)) & 1) == 0 ? white : red;
                var weave = Mathf.Sin(x * Mathf.PI / 2) * Mathf.Sin(y * Mathf.PI / 2) * 0.022f;
                colour *= 1 + weave + (Mathf.PerlinNoise(x * 0.29f, y * 0.29f) - 0.5f) * 0.025f;
                pixels[y * size + x] = colour;
            }
            texture.SetPixels(pixels); texture.Apply();
            var texturePath = Directory + "/RedWhiteCotton.png";
            System.IO.File.WriteAllBytes(texturePath, texture.EncodeToPNG());
            Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(texturePath, ImportAssetOptions.ForceSynchronousImport);
            var importer = (TextureImporter)AssetImporter.GetAtPath(texturePath);
            importer.wrapMode = TextureWrapMode.Repeat;
            importer.filterMode = FilterMode.Trilinear;
            importer.anisoLevel = 8;
            importer.mipmapEnabled = true;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
            var materialPath = Directory + "/RedWhiteCotton.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if (!material)
            {
                material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                AssetDatabase.CreateAsset(material, materialPath);
            }
            material.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath));
            material.SetColor("_BaseColor", Color.white);
            material.SetFloat("_Metallic", 0);
            material.SetFloat("_Smoothness", 0.06f);
            EditorUtility.SetDirty(material);
            return material;
        }

        static Mesh SaveMesh(Mesh built, string path)
        {
            var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (!mesh) { AssetDatabase.CreateAsset(built, path); return built; }
            mesh.Clear(); mesh.vertices = built.vertices; mesh.normals = built.normals;
            mesh.uv = built.uv; mesh.tangents = built.tangents;
            mesh.triangles = built.triangles; mesh.bounds = built.bounds;
            EditorUtility.SetDirty(mesh); Object.DestroyImmediate(built);
            return mesh;
        }

        static void SeatVases(GameObject diner, Transform[] tables)
        {
            var plants = AssetDatabase.LoadAssetAtPath<GameObject>(DinerPlantsAuthoring.PrefabPath);
            if (!plants) return;
            var contents = PrefabUtility.LoadPrefabContents(DinerPlantsAuthoring.PrefabPath);
            try
            {
                for (var i = 0; i < tables.Length; i++)
                {
                    var top = tables[i].Find("Round table").GetComponent<Renderer>().bounds.max.y + SurfaceLift;
                    var name = "Gerbera vase " + (i + 1);
                    var assetVase = contents.transform.Find(name);
                    if (assetVase) assetVase.position = new Vector3(assetVase.position.x, top, assetVase.position.z);
                    foreach (var vase in diner.GetComponentsInChildren<Transform>().Where(t => t.name == name))
                    {
                        vase.position = new Vector3(vase.position.x, top, vase.position.z);
                        PrefabUtility.RecordPrefabInstancePropertyModifications(vase);
                    }
                }
                PrefabUtility.SaveAsPrefabAsset(contents, DinerPlantsAuthoring.PrefabPath);
            }
            finally { PrefabUtility.UnloadPrefabContents(contents); }
        }
    }
}
