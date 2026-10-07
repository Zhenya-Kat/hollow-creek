using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace HollowCreek.Editor.Props
{
    /// <summary>Tile and painted-wall surfaces with physical texture scale across window openings.</summary>
    public static class DinerInteriorAuthoring
    {
        const string Directory = "Assets/Art/Environment/DinerInterior";
        const int Resolution = 512;

        [MenuItem("Hollow Creek/Реквизит/Плитка и краска закусочной")]
        public static void Build()
        {
            var scene = SceneManager.GetActiveScene();
            if (EditorApplication.isPlaying || !scene.path.EndsWith("/Diner.unity"))
                throw new InvalidOperationException("Open Diner in Edit mode before rebuilding its surfaces.");
            System.IO.Directory.CreateDirectory(Directory + "/Meshes");
            AssetDatabase.Refresh();
            GenerateTextures("CheckerCeramic", true);
            GenerateTextures("BeigePaint", false);
            CreateMaterial("CheckerCeramic", 1f);
            CreateMaterial("BeigePaint", 0.85f);
            ApplyExistingMaterials();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        // Window authoring calls this after rebuilding the wall pieces, so textures keep
        // their metre scale and continuity when the number or size of openings changes.
        internal static void ApplyExistingMaterials()
        {
            var tiles = AssetDatabase.LoadAssetAtPath<Material>(Directory + "/CheckerCeramic.mat");
            var paint = AssetDatabase.LoadAssetAtPath<Material>(Directory + "/BeigePaint.mat");
            if (!tiles || !paint) return;
            var root = GameObject.Find("[Location] Diner");
            if (!root) return;
            var geometry = root.transform.Find("Geometry");
            if (!geometry) throw new InvalidOperationException("Diner geometry is missing.");
            foreach (var renderer in geometry.GetComponentsInChildren<MeshRenderer>(true))
            {
                var floor = renderer.name == "Floor";
                if (!floor && !renderer.name.StartsWith("Wall ")) continue;
                // The shared street ground reaches y=0 underneath the entire room.
                // Give the ceramic finish its own thickness so coplanar surfaces never flicker.
                if (floor) renderer.transform.position += Vector3.up * (0.008f - renderer.bounds.max.y);
                renderer.sharedMaterial = floor ? tiles : paint;
                ApplyWorldUVs(renderer.GetComponent<MeshFilter>(), floor ? 1f : 0.5f, geometry);
            }
            AssetDatabase.SaveAssets();
        }

        static void GenerateTextures(string prefix, bool tiles)
        {
            var count = Resolution * Resolution;
            var color = new Color[count];
            var surface = new Color[count];
            var heights = new float[count];
            for (var y = 0; y < Resolution; y++)
            for (var x = 0; x < Resolution; x++)
            {
                var u = (x + 0.5f) / Resolution;
                var v = (y + 0.5f) / Resolution;
                var i = y * Resolution + x;
                var grain = Noise(u, v, 128, 128, 37) - 0.5f;
                if (tiles)
                {
                    var a = Mathf.Repeat(u * 2, 1);
                    var b = Mathf.Repeat(v * 2, 1);
                    var edge = Mathf.Min(a, 1 - a, b, 1 - b) * 0.5f;
                    var black = ((Mathf.FloorToInt(u * 2) + Mathf.FloorToInt(v * 2)) & 1) != 0;
                    var face = Mathf.SmoothStep(0, 1, (edge - 0.002f) / 0.002f);
                    var bevel = Mathf.SmoothStep(0, 1, (edge - 0.003f) / 0.005f);
                    var ceramic = black ? new Color(0.105f, 0.11f, 0.115f) : new Color(0.91f, 0.905f, 0.88f);
                    ceramic *= 1 + grain * 0.018f;
                    color[i] = Color.Lerp(new Color(0.46f, 0.445f, 0.415f) * (1 + grain * 0.10f), ceramic, face);
                    heights[i] = -0.0015f + 0.0015f * face + 0.0005f * bevel + grain * 0.000016f;
                    var smoothness = Mathf.Lerp(0.055f, (black ? 0.27f : 0.34f) + grain * 0.028f, face);
                    surface[i] = new Color(0, 0, 0, smoothness);
                }
                else
                {
                    var roller = Noise(u, v, 24, 64, 81) - 0.5f;
                    var fine = Noise(u, v, 256, 256, 29) - 0.5f;
                    color[i] = new Color(0.80f, 0.76f, 0.685f) * (1 + grain * 0.028f + roller * 0.008f);
                    heights[i] = roller * 0.00035f + grain * 0.00022f + fine * 0.000055f;
                    surface[i] = new Color(0, 0, 0, 0.12f + grain * 0.045f);
                }
            }
            var normals = new Color[count];
            var metres = tiles ? 1f : 0.5f;
            for (var y = 0; y < Resolution; y++)
            for (var x = 0; x < Resolution; x++)
            {
                var left = heights[y * Resolution + (x + Resolution - 1) % Resolution];
                var right = heights[y * Resolution + (x + 1) % Resolution];
                var below = heights[((y + Resolution - 1) % Resolution) * Resolution + x];
                var above = heights[((y + 1) % Resolution) * Resolution + x];
                var normal = new Vector3(-(right - left) * Resolution / (2 * metres), -(above - below) * Resolution / (2 * metres), 1).normalized;
                normals[y * Resolution + x] = new Color(normal.x * 0.5f + 0.5f, normal.y * 0.5f + 0.5f, normal.z * 0.5f + 0.5f, 1);
            }
            SaveTexture(prefix + "_BaseColor", color, false, true);
            SaveTexture(prefix + "_Normal", normals, true, false);
            SaveTexture(prefix + "_MetallicSmoothness", surface, false, false);
        }

        static float Noise(float u, float v, int cellsX, int cellsY, uint seed)
        {
            var px = u * cellsX;
            var py = v * cellsY;
            var ix = Mathf.FloorToInt(px);
            var iy = Mathf.FloorToInt(py);
            var tx = Mathf.SmoothStep(0, 1, px - ix);
            var ty = Mathf.SmoothStep(0, 1, py - iy);
            float Hash(int x, int y)
            {
                unchecked
                {
                    var h = (uint)(x % cellsX) * 374761393u + (uint)(y % cellsY) * 668265263u + seed * 2246822519u;
                    h = (h ^ (h >> 13)) * 1274126177u;
                    return (h ^ (h >> 16)) / (float)uint.MaxValue;
                }
            }
            return Mathf.Lerp(Mathf.Lerp(Hash(ix, iy), Hash(ix + 1, iy), tx), Mathf.Lerp(Hash(ix, iy + 1), Hash(ix + 1, iy + 1), tx), ty);
        }

        static void SaveTexture(string name, Color[] pixels, bool normal, bool srgb)
        {
            var texture = new Texture2D(Resolution, Resolution, TextureFormat.RGBA32, false, !srgb);
            texture.SetPixels(pixels);
            texture.Apply();
            var path = Directory + "/" + name + ".png";
            File.WriteAllBytes(path, texture.EncodeToPNG());
            Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = normal ? TextureImporterType.NormalMap : TextureImporterType.Default;
            importer.sRGBTexture = srgb;
            importer.wrapMode = TextureWrapMode.Repeat;
            importer.filterMode = FilterMode.Trilinear;
            importer.anisoLevel = 8;
            importer.mipmapEnabled = true;
            importer.maxTextureSize = Resolution;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.alphaSource = normal ? TextureImporterAlphaSource.None : TextureImporterAlphaSource.FromInput;
            importer.alphaIsTransparency = false;
            importer.SaveAndReimport();
        }

        static void CreateMaterial(string name, float normalStrength)
        {
            var path = Directory + "/" + name + ".mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (!material) { material = new Material(shader); AssetDatabase.CreateAsset(material, path); }
            material.shader = shader;
            material.SetColor("_BaseColor", Color.white);
            material.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(Directory + "/" + name + "_BaseColor.png"));
            material.SetTexture("_BumpMap", AssetDatabase.LoadAssetAtPath<Texture2D>(Directory + "/" + name + "_Normal.png"));
            material.SetTexture("_MetallicGlossMap", AssetDatabase.LoadAssetAtPath<Texture2D>(Directory + "/" + name + "_MetallicSmoothness.png"));
            material.SetTextureScale("_BaseMap", Vector2.one);
            material.SetTextureOffset("_BaseMap", Vector2.zero);
            material.SetFloat("_BumpScale", normalStrength);
            material.SetFloat("_Smoothness", 1);
            material.SetFloat("_SmoothnessTextureChannel", 0);
            material.SetFloat("_Metallic", 0);
            material.EnableKeyword("_NORMALMAP");
            material.EnableKeyword("_METALLICSPECGLOSSMAP");
            EditorUtility.SetDirty(material);
        }

        static void ApplyWorldUVs(MeshFilter filter, float metres, Transform geometry)
        {
            if (!filter || !filter.sharedMesh) return;
            var source = filter.sharedMesh;
            var vertices = source.vertices;
            var normals = source.normals;
            var uv = new Vector2[vertices.Length];
            for (var i = 0; i < vertices.Length; i++)
            {
                var position = filter.transform.TransformPoint(vertices[i]);
                var n = filter.transform.TransformDirection(normals[i]);
                var axes = new Vector3(Mathf.Abs(n.x), Mathf.Abs(n.y), Mathf.Abs(n.z));
                uv[i] = (axes.y > axes.x && axes.y > axes.z ? new Vector2(position.x, position.z)
                    : axes.x > axes.z ? new Vector2(position.z, position.y) : new Vector2(position.x, position.y)) / metres;
            }
            var parent = filter.transform.parent == geometry ? "" : filter.transform.parent.name + "_";
            var path = Directory + "/Meshes/" + (parent + filter.name).Replace(' ', '_') + ".asset";
            var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (!mesh)
            {
                mesh = Object.Instantiate(source);
                mesh.name = filter.name + " metre UVs";
                AssetDatabase.CreateAsset(mesh, path);
            }
            mesh.uv = uv;
            mesh.RecalculateTangents();
            EditorUtility.SetDirty(mesh);
            filter.sharedMesh = mesh;
        }
    }
}
