using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace HollowCreek.Editor.Props
{
    public static class StreetLifeAuthoring
    {
        const string Dir = "Assets/Art/Environment/DinerWindow/StreetLife";
        const int Seed = 31062026;

        public static void Apply(Transform street)
        {
            System.IO.Directory.CreateDirectory(Dir);
            foreach (var name in new[] { "Left street lamp", "Right street lamp" })
            {
                var old = street.Find("Opposite-side decorations/" + name);
                if (old) Object.DestroyImmediate(old.gameObject);
            }
            var previous = street.Find("Street life");
            if (previous) Object.DestroyImmediate(previous.gameObject);
            var group = new GameObject("Street life").transform;
            group.SetParent(street, false);
            AddLamps(street, group);
            LightLanternGlass(street);
            AddPumpkins(group);
            AddWindows(street, group);
            AssetDatabase.SaveAssets();
        }

        static void AddLamps(Transform street, Transform parent)
        {
            var group = Child(parent, "Opposite pavement lanterns");
            var source = street.Find("Street buildings and trees").GetComponentsInChildren<MeshRenderer>()
                .First(r => r.GetComponent<MeshFilter>().sharedMesh.name == "lightpost-double");
            var metal = AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/Environment/StreetNatural/Aged graphite lantern metal.mat");
            // Modest variation in setbacks and intervals, with coverage opposite the diner.
            var positions = new[] { new Vector3(-34.4f, .081f, -7.3f), new Vector3(-23.1f, .081f, -7.1f),
                new Vector3(-10.9f, .081f, -7.35f), new Vector3(2.4f, .081f, -7.15f),
                new Vector3(14.6f, .081f, -7.25f), new Vector3(27.9f, .081f, -7.05f), new Vector3(40.8f, .081f, -7.3f) };
            var random = new System.Random(Seed + 1);
            for (var i = 0; i < positions.Length; i++)
            {
                var lamp = Object.Instantiate(source.gameObject, group);
                lamp.name = "Road lantern " + (i + 1);
                lamp.transform.position = Vector3.zero;
                var renderer = lamp.GetComponent<MeshRenderer>();
                renderer.enabled = true;
                if (metal) renderer.sharedMaterial = metal;
                var bounds = renderer.bounds;
                lamp.transform.position = positions[i] - new Vector3(bounds.center.x, bounds.min.y, bounds.center.z);
                foreach (var side in new[] { -1, 1 })
                    WarmLight(lamp.transform, "Lantern pool " + side, positions[i] + new Vector3(side * .94f, 4.05f, 0),
                        2.6f + (float)random.NextDouble() * .8f, 7.8f);
            }
        }

        static void AddPumpkins(Transform parent)
        {
            var group = Child(parent, "Scattered pumpkin clusters");
            var random = new System.Random(Seed);
            var material = AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/Environment/DinerWindow/WindowPumpkin.mat");
            var models = new[] { "pumpkin", "pumpkin-carved", "pumpkin-tall-carved" }
                .Select(n => AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/Environment/Kenney/Graveyard/" + n + ".fbx")).ToArray();
            var anchors = new[] { new Vector2(-31,8), new Vector2(-23.7f,9.9f), new Vector2(-18.6f,5.7f),
                new Vector2(-13.5f,4.6f), new Vector2(-7,6.7f), new Vector2(-3.8f,10.9f), new Vector2(3.8f,10.8f),
                new Vector2(9.4f,5), new Vector2(14.5f,8.2f), new Vector2(25.7f,8.1f), new Vector2(32.5f,5.2f),
                new Vector2(41.5f,6), new Vector2(-34,-8.6f), new Vector2(-23.5f,-9), new Vector2(-17.6f,-11.6f),
                new Vector2(-10.3f,-9), new Vector2(-3.1f,-11.9f), new Vector2(4.5f,-9.4f), new Vector2(12.1f,-10.9f),
                new Vector2(18.9f,-10.8f), new Vector2(25.5f,-9.9f), new Vector2(32.7f,-9), new Vector2(40.5f,-10.7f),
                new Vector2(11,27.3f), new Vector2(24,27.8f), new Vector2(28.8f,21), new Vector2(8.2f,17) };
            for (var i = 0; i < anchors.Length; i++)
            {
                var cluster = Child(group, "Pumpkin cluster " + (i + 1));
                var centre = anchors[i] + new Vector2(Range(random, -.55f, .55f), Range(random, -.4f, .4f));
                var count = random.Next(1, 4);
                var phase = Range(random, 0, 360);
                for (var j = 0; j < count; j++)
                {
                    var model = Object.Instantiate(models[random.Next(models.Length)], cluster);
                    model.name = "Pumpkin " + (j + 1);
                    model.transform.SetPositionAndRotation(Vector3.zero, Quaternion.Euler(0, Range(random, 0, 360), 0));
                    var height = Range(random, .24f, .48f);
                    var bounds = Bounds(model);
                    model.transform.localScale *= height / bounds.size.y;
                    // Wide angular spacing avoids intersections, with different sizes and poses.
                    var angle = (phase + j * 125 + Range(random, -13, 13)) * Mathf.Deg2Rad;
                    var radius = count == 1 ? 0 : Range(random, .32f, .41f);
                    var pos = new Vector3(centre.x + Mathf.Cos(angle) * radius, .012f, centre.y + Mathf.Sin(angle) * radius);
                    bounds = Bounds(model);
                    model.transform.position = pos - new Vector3(bounds.center.x, bounds.min.y, bounds.center.z);
                    foreach (var collider in model.GetComponentsInChildren<Collider>()) Object.DestroyImmediate(collider);
                    foreach (var renderer in model.GetComponentsInChildren<MeshRenderer>())
                    {
                        renderer.sharedMaterials = renderer.sharedMaterials.Select(_ => material).ToArray();
                        renderer.receiveShadows = true;
                    }
                }
            }
        }

        static void LightLanternGlass(Transform street)
        {
            var lanterns = street.GetComponentsInChildren<MeshFilter>()
                .Where(f => f.sharedMesh && f.sharedMesh.name == "lightpost-double").ToArray();
            var source = lanterns[0].sharedMesh;
            var v = source.vertices; var uv = source.uv; var tris = source.triangles;
            var vertices = new List<Vector3>(); var indices = new List<int>();
            for (var i = 0; i < tris.Length; i += 3)
            {
                var centre = (uv[tris[i]] + uv[tris[i + 1]] + uv[tris[i + 2]]) / 3;
                if (centre.x < .75f || centre.x > .85f) continue;
                var normal = Vector3.Cross(v[tris[i + 1]] - v[tris[i]], v[tris[i + 2]] - v[tris[i]]).normalized;
                for (var j = 0; j < 3; j++) { indices.Add(vertices.Count); vertices.Add(v[tris[i + j]] + normal * .0015f); }
            }
            var mesh = new Mesh { name = "Road lantern glass" }; mesh.SetVertices(vertices); mesh.SetTriangles(indices, 0); mesh.RecalculateNormals();
            var path = Dir + "/RoadLanternGlass.asset";
            var stored = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (stored) { EditorUtility.CopySerialized(mesh, stored); Object.DestroyImmediate(mesh); EditorUtility.SetDirty(stored); }
            else { stored = mesh; AssetDatabase.CreateAsset(stored, path); }
            var materialPath = Dir + "/RoadLanternGlass.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if (!material) { material = new Material(Shader.Find("Universal Render Pipeline/Unlit")); AssetDatabase.CreateAsset(material, materialPath); }
            material.SetColor("_BaseColor", new Color(1.6f, 1.05f, .48f));
            material.SetFloat("_Cull", 0); EditorUtility.SetDirty(material);
            foreach (var lantern in lanterns)
            {
                var old = lantern.transform.Find("Warm lantern glass"); if (old) Object.DestroyImmediate(old.gameObject);
                var glass = Child(lantern.transform, "Warm lantern glass");
                glass.gameObject.AddComponent<MeshFilter>().sharedMesh = stored;
                var renderer = glass.gameObject.AddComponent<MeshRenderer>(); renderer.sharedMaterial = material;
                renderer.shadowCastingMode = ShadowCastingMode.Off;
            }
        }

        static void AddWindows(Transform street, Transform parent)
        {
            var group = Child(parent, "Occupied house windows");
            var palettes = new[] { new Color(1, .66f, .31f), new Color(1, .82f, .52f), new Color(.95f, .72f, .41f) };
            var materials = palettes.Select((c, i) => WindowMaterial(i, c)).ToArray();
            var houses = street.GetComponentsInChildren<MeshRenderer>(true)
                .Where(r => r.enabled && ((r.name.StartsWith("building-type-") && r.transform.parent.name=="Street buildings and trees") || r.name == "House silhouette opposite diner"))
                .OrderBy(r => r.bounds.center.x).ThenBy(r => r.bounds.center.z).ToArray();
            var random = new System.Random(Seed + 2);
            for (var houseIndex = 0; houseIndex < houses.Length; houseIndex++)
            {
                var house = houses[houseIndex];
                var source = house.GetComponent<MeshFilter>().sharedMesh;
                var v = source.vertices; var uv = source.uv;
                var candidates = new List<int[]>();
                var triangles = source.triangles;
                for (var t = 0; t < triangles.Length; t += 3)
                {
                    var ids = new[] { triangles[t], triangles[t + 1], triangles[t + 2] };
                    var centre = (uv[ids[0]] + uv[ids[1]] + uv[ids[2]]) / 3;
                    var normal = Vector3.Cross(v[ids[1]] - v[ids[0]], v[ids[2]] - v[ids[0]]).normalized;
                    // Kenney Suburban glass column; exclude roof, frame and plaster swatches.
                    if (centre.x > .68f && centre.x < .76f && centre.y > .51f && centre.y < .66f && Mathf.Abs(normal.y) < .25f)
                        candidates.Add(ids);
                }
                // Join triangles by geometric vertices (FBX may split UV/normal vertices).
                var components = new List<List<int[]>>();
                while (candidates.Count > 0)
                {
                    var component = new List<int[]> { candidates[0] }; candidates.RemoveAt(0);
                    var points = new HashSet<Vector3>(component[0].Select(i => v[i]));
                    bool changed;
                    do
                    {
                        changed = false;
                        for (var t = candidates.Count - 1; t >= 0; t--)
                            if (candidates[t].Any(i => points.Contains(v[i])))
                            {
                                foreach (var i in candidates[t]) points.Add(v[i]);
                                component.Add(candidates[t]); candidates.RemoveAt(t); changed = true;
                            }
                    } while (changed);
                    components.Add(component);
                }
                if (components.Count == 0) continue;
                var vertices = new List<Vector3>(); var texcoords = new List<Vector2>();
                var indices = Enumerable.Range(0, 3).Select(_ => new List<int>()).ToArray();
                int lit = 0;
                foreach (var component in components)
                {
                    if (random.NextDouble() > .52 && lit > 0) continue;
                    var palette = random.Next(3);
                    var points = component.SelectMany(t => t).Select(i => house.transform.TransformPoint(v[i])).ToArray();
                    var bounds = new Bounds(points[0], Vector3.zero); foreach (var p in points) bounds.Encapsulate(p);
                    bool alongX = bounds.size.x > bounds.size.z;
                    foreach (var tri in component)
                    {
                        var normal = Vector3.Cross(v[tri[1]] - v[tri[0]], v[tri[2]] - v[tri[0]]).normalized;
                        foreach (var index in tri)
                        {
                            var world = house.transform.TransformPoint(v[index]);
                            indices[palette].Add(vertices.Count);
                            vertices.Add(v[index] + normal * (.007f / house.transform.lossyScale.x));
                            texcoords.Add(new Vector2(alongX ? (world.x - bounds.min.x) / Mathf.Max(.001f, bounds.size.x)
                                : (world.z - bounds.min.z) / Mathf.Max(.001f, bounds.size.z),
                                (world.y - bounds.min.y) / Mathf.Max(.001f, bounds.size.y)));
                        }
                    }
                    lit++;
                }
                var mesh = new Mesh { name = "Occupied windows " + houseIndex };
                mesh.SetVertices(vertices); mesh.SetUVs(0, texcoords); mesh.subMeshCount = 3;
                for (var i = 0; i < 3; i++) mesh.SetTriangles(indices[i], i);
                mesh.RecalculateNormals(); mesh.RecalculateBounds();
                var path = Dir + "/HouseWindows" + houseIndex + ".asset";
                var stored = AssetDatabase.LoadAssetAtPath<Mesh>(path);
                if (stored) { EditorUtility.CopySerialized(mesh, stored); Object.DestroyImmediate(mesh); EditorUtility.SetDirty(stored); }
                else { stored = mesh; AssetDatabase.CreateAsset(stored, path); }
                var windows = Child(group, house.name + " lit rooms " + houseIndex);
                windows.SetPositionAndRotation(house.transform.position, house.transform.rotation);
                windows.localScale = house.transform.lossyScale;
                windows.gameObject.AddComponent<MeshFilter>().sharedMesh = stored;
                var renderer = windows.gameObject.AddComponent<MeshRenderer>(); renderer.sharedMaterials = materials;
                renderer.shadowCastingMode = ShadowCastingMode.Off;
            }
        }

        static Material WindowMaterial(int index, Color colour)
        {
            var path = Dir + "/WarmRoom" + index + ".mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (!material) { material = new Material(Shader.Find("HollowCreek/Occupied House Window")); AssetDatabase.CreateAsset(material, path); }
            material.SetColor("_BaseColor", colour); EditorUtility.SetDirty(material); return material;
        }
        static float Range(System.Random random, float min, float max) => Mathf.Lerp(min, max, (float)random.NextDouble());
        static Transform Child(Transform parent, string name) { var child = new GameObject(name).transform; child.SetParent(parent, false); return child; }
        static Bounds Bounds(GameObject obj) { var renderers = obj.GetComponentsInChildren<Renderer>(); var bounds = renderers[0].bounds; foreach (var r in renderers) bounds.Encapsulate(r.bounds); return bounds; }
        static void WarmLight(Transform parent, string name, Vector3 position, float intensity, float range)
        {
            var light = Child(parent, name).gameObject.AddComponent<Light>(); light.transform.position = position;
            light.type = LightType.Point; light.color = new Color(1, .73f, .43f); light.intensity = intensity; light.range = range;
            light.shadows = LightShadows.None;
        }
    }
}

