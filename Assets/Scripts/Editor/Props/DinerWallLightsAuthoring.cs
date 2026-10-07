using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace HollowCreek.Editor.Props
{
    public static class DinerWallLightsAuthoring
    {
        const string Directory = "Assets/Art/Environment/DinerInterior/Lighting";
        public const string PrefabPath = Directory + "/DinerWallLights.prefab";

        [MenuItem("Hollow Creek/Реквизит/Тёплые настенные лампы закусочной")]
        public static void Build()
        {
            var scene = SceneManager.GetActiveScene();
            if (EditorApplication.isPlaying || !scene.path.EndsWith("/Diner.unity"))
                throw new InvalidOperationException("Open Diner in Edit mode before placing wall lights.");
            var diner = GameObject.Find("[Location] Diner");
            System.IO.Directory.CreateDirectory(Directory);
            AssetDatabase.Refresh();
            var black = Material("BlackLampHousing", new Color(0.018f, 0.022f, 0.026f), 0.25f);
            black.SetFloat("_Metallic", 0.35f);
            var diffuser = Material("WarmOpalDiffuser", new Color(1, 0.84f, 0.66f) * 1.8f, 0.18f, "Universal Render Pipeline/Unlit");
            diffuser.DisableKeyword("_EMISSION");
            var kit = new MeshKit();
            var housing = kit.Mat("black housing"); var glow = kit.Mat("opal diffuser");
            // A slim horizontal fixture: wall mounts, black border, front and lower diffusers.
            kit.Box(new Vector3(0, 0, 0.075f), new Vector3(2.1f, 0.15f, 0.14f), housing, 0.012f);
            kit.Box(new Vector3(0, -0.005f, 0.148f), new Vector3(1.96f, 0.068f, 0.012f), glow, 0.004f);
            kit.Box(new Vector3(0, -0.077f, 0.079f), new Vector3(1.96f, 0.010f, 0.080f), glow, 0.003f);
            foreach (var x in new[] { -0.76f, 0.76f })
                kit.Box(new Vector3(x, 0, 0.010f), new Vector3(0.10f, 0.10f, 0.020f), housing, 0.005f);
            var mesh = SaveMesh(kit.Build("Long black wall lamp"), "LongBlackWallLamp");
            var stage = new GameObject("Warm black wall lamps");
            try
            {
                foreach (var z in new[] { 2.5f, 5.25f, 8.0f })
                    Fixture(stage.transform, "West wall lamp " + z, new Vector3(-5.925f, 2.97f, z), 90, mesh, black, diffuser);
                foreach (var z in new[] { 2.5f, 5.5f })
                    Fixture(stage.transform, "East wall lamp " + z, new Vector3(5.925f, 2.97f, z), -90, mesh, black, diffuser);
                Fixture(stage.transform, "Entrance wall lamp", new Vector3(0, 2.97f, 0.075f), 0, mesh, black, diffuser);
                AssetDatabase.SaveAssets();
                var prefab = PrefabUtility.SaveAsPrefabAsset(stage, PrefabPath);
                foreach (var old in diner.GetComponentsInChildren<Transform>(true).Where(t => PrefabUtility.IsAnyPrefabInstanceRoot(t.gameObject)
                    && PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(t.gameObject) == PrefabPath).ToArray())
                    Object.DestroyImmediate(old.gameObject);
                var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                instance.name = stage.name;
                instance.transform.SetParent(diner.transform.Find("Art"), false);
                PrefabUtility.RecordPrefabInstancePropertyModifications(instance);
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
                Selection.activeGameObject = null;
                SceneView.RepaintAll();
            }
            finally { Object.DestroyImmediate(stage); }
        }

        static void Fixture(Transform parent, string name, Vector3 position, float yaw, Mesh mesh, Material black, Material diffuser)
        {
            var fixture = new GameObject(name).transform;
            fixture.SetParent(parent, false);
            fixture.SetPositionAndRotation(position, Quaternion.Euler(0, yaw, 0));
            fixture.gameObject.AddComponent<MeshFilter>().sharedMesh = mesh;
            fixture.gameObject.AddComponent<MeshRenderer>().sharedMaterials = new[] { black, diffuser };
            // Two broad, overlapping sources distribute the light along the diffuser.
            foreach (var x in new[] { -0.58f, 0.58f })
            {
                var light = new GameObject("Soft warm wall light").AddComponent<Light>();
                light.transform.SetParent(fixture, false);
                light.transform.localPosition = new Vector3(x, -0.09f, 0.20f);
                light.transform.localRotation = Quaternion.LookRotation(new Vector3(0, -0.65f, 0.76f));
                light.type = LightType.Spot;
                light.color = new Color(1, 0.84f, 0.66f);
                light.intensity = 2.0f;
                light.range = 5.6f;
                light.spotAngle = 120;
                light.innerSpotAngle = 75;
                light.shadows = LightShadows.Soft;
                light.shadowStrength = 0.55f;
                light.shadowBias = 0.03f;
                light.shadowNormalBias = 0.06f;
                light.shadowNearPlane = 0.05f;
            }
        }

        static Material Material(string name, Color colour, float smoothness, string shaderName = "Universal Render Pipeline/Lit")
        {
            var path = Directory + "/" + name + ".mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            var shader = Shader.Find(shaderName);
            if (!shader) throw new InvalidOperationException("Missing lamp shader: " + shaderName);
            if (!material) { material = new Material(shader); AssetDatabase.CreateAsset(material, path); }
            else if (material.shader != shader) material.shader = shader;
            material.SetColor("_BaseColor", colour); material.SetFloat("_Smoothness", smoothness);
            EditorUtility.SetDirty(material); return material;
        }

        static Mesh SaveMesh(Mesh built, string name)
        {
            var path = Directory + "/" + name + ".asset";
            var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (!mesh) { mesh = built; mesh.name = name; AssetDatabase.CreateAsset(mesh, path); }
            else
            {
                mesh.Clear(); mesh.indexFormat = built.indexFormat;
                mesh.vertices = built.vertices; mesh.normals = built.normals; mesh.uv = built.uv; mesh.tangents = built.tangents;
                mesh.subMeshCount = built.subMeshCount;
                for (var i = 0; i < built.subMeshCount; i++) mesh.SetTriangles(built.GetTriangles(i), i);
                mesh.bounds = built.bounds; EditorUtility.SetDirty(mesh); Object.DestroyImmediate(built);
            }
            return mesh;
        }
    }
}
