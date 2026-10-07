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
    /// <summary>The same exterior geometry is used by Street and by the diner window.</summary>
    public static class EveningStreetAuthoring
    {
        const string Directory = "Assets/Art/Environment/DinerWindow";
        public const string PrefabPath = Directory + "/SharedEveningStreet.prefab";
        static readonly Vector3 InteriorWindow = new(-5.95f, 0, 2.5f);
        static readonly string[] TerrainNames = {
            "Ground", "Road", "Sidewalk", "Square", "Porch", "Fence L", "Fence R",
            "Fence Side L", "Fence Side R", "Gate Post L", "Gate Post R"
        };

        [MenuItem("Hollow Creek/Реквизит/Согласовать вечернюю улицу и окно закусочной")]
        public static void Build()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Exit Play mode before authoring the street.");
            var current = SceneManager.GetActiveScene();
            if (current.isDirty) EditorSceneManager.SaveScene(current);
            var street = current.path.EndsWith("/Street.unity") ? current
                : EditorSceneManager.OpenScene("Assets/Scenes/Locations/Street.unity", OpenSceneMode.Single);
            var root = street.GetRootGameObjects().Single(g => g.name == "[Location] Street").transform;
            var geometry = root.Find("Geometry");
            var art = root.Find("Art");
            var props = root.Find("Props");
            var streetWindow = geometry.Find("Diner Window").position;
            var anchor = new Vector3(streetWindow.x, 0, streetWindow.z);

            var shared = new GameObject("Shared evening street");
            var anchorObject = new GameObject("Diner exterior window anchor").transform;
            anchorObject.SetParent(shared.transform, false);
            anchorObject.position = anchor;

            var terrain = new GameObject("Street terrain").transform;
            terrain.SetParent(shared.transform, false);
            foreach (var name in TerrainNames)
            {
                var original = geometry.Find(name);
                if (!original) continue;
                var copy = CopyVisual(original.gameObject, terrain);
                var renderer = copy.GetComponent<Renderer>();
                renderer.enabled = true;
                if (name == "Road")
                {
                    copy.transform.localScale = new Vector3(220, 0.01f, 6);
                    renderer.sharedMaterial = Material("StreetWetAsphalt", "Universal Render Pipeline/Unlit", new Color(0.20f, 0.225f, 0.26f));
                }
                else if (name == "Ground")
                {
                    copy.transform.localScale = new Vector3(240, 0.2f, 160);
                    renderer.sharedMaterial = Material("StreetNightGround", "Universal Render Pipeline/Unlit", new Color(0.085f, 0.11f, 0.13f));
                }
                else if (name == "Sidewalk")
                {
                    copy.transform.localScale = new Vector3(220, 0.12f, 2.5f);
                    renderer.sharedMaterial = Material("StreetNightPavement", "Universal Render Pipeline/Unlit", new Color(0.25f, 0.275f, 0.29f));
                }
                else renderer.sharedMaterial = NightMaterial(renderer.sharedMaterial);
                original.GetComponent<Renderer>().enabled = false;
            }
            var pavement = Material("StreetNightPavement", "Universal Render Pipeline/Unlit", new Color(0.25f, 0.275f, 0.29f));
            Box(terrain, "Opposite sidewalk", new Vector3(0, 0.04f, -7.15f), new Vector3(220, 0.08f, 2.3f), pavement);
            Box(terrain, "Path from diner door", new Vector3(23.86f, 0.025f, 6.2f), new Vector3(2.4f, 0.05f, 7.4f), pavement);
            var curb = Material("StreetCurb", "Universal Render Pipeline/Unlit", new Color(0.29f, 0.30f, 0.31f));
            Box(terrain, "Near curb", new Vector3(0, 0.075f, 0.045f), new Vector3(220, 0.15f, 0.09f), curb);
            Box(terrain, "Far curb", new Vector3(0, 0.055f, -6.045f), new Vector3(220, 0.11f, 0.09f), curb);
            var stripe = Material("StreetFadedRoadMarking", "Universal Render Pipeline/Unlit", new Color(0.42f, 0.39f, 0.29f));
            for (var x = -105; x <= 105; x += 6)
                Box(terrain, "Road center dash " + x, new Vector3(x, 0.012f, -3), new Vector3(2.4f, 0.004f, 0.085f), stripe);

            var buildings = CopyVisual(art.gameObject, shared.transform);
            buildings.name = "Street buildings and trees";
            foreach (var renderer in buildings.GetComponentsInChildren<Renderer>(true))
            {
                renderer.enabled = true;
                renderer.sharedMaterials = renderer.sharedMaterials.Select(m => m.name.Contains("Lightpost") ? m : NightMaterial(m)).ToArray();
            }
            var diner = buildings.transform.Cast<Transform>().Single(t => t.name == "building-type-q" && t.position.z > 0);
            diner.name = "Diner facade";
            DinerExteriorAuthoring.AttachExistingWindows(diner);
            foreach (var renderer in art.GetComponentsInChildren<Renderer>(true)) renderer.enabled = false;
            // Remove obsolete glowing greybox window panels; the buildings have their own windows.
            foreach (var name in new[] { "Window L", "Window R", "Diner Window" })
            {
                var window = geometry.Find(name);
                if (window) window.GetComponent<Renderer>().enabled = false;
            }

            var details = new GameObject("Street details").transform;
            details.SetParent(shared.transform, false);
            foreach (Transform prop in props)
            {
                var model = prop.Find("Model");
                if (!model) continue;
                var copy = CopyVisual(model.gameObject, details);
                copy.name = prop.name;
                // Street keeps the original model under its interaction/state components.
                // The independent copy is only used as distant scenery through the window.
                model.gameObject.SetActive(true);
            }
            var lighting = CopyVisual(root.Find("Lights").gameObject, shared.transform);
            lighting.name = "Street lighting";
            foreach (var light in lighting.GetComponentsInChildren<Light>(true)) light.enabled = true;
            foreach (var light in root.Find("Lights").GetComponentsInChildren<Light>(true)) light.enabled = true;

            // Clear the central tree in the opposite garden so the two decorated trees
            // have room and the house remains visible between them.
            var crowdedTree = buildings.transform.Cast<Transform>().FirstOrDefault(t => t.name.StartsWith("pine")
                && Mathf.Abs(t.position.x - anchor.x) < 2 && t.position.z < -8 && t.position.z > -12);
            if (crowdedTree) crowdedTree.gameObject.SetActive(false);
            var decorations = DinerWindowAuthoring.CreateWindowDecorations(anchor);
            decorations.transform.SetParent(shared.transform, false);
            DinerWindowAuthoring.AddNearSideDecoratedTrees(shared.transform);
            AddOppositeHouse(shared.transform);
            AddWetReflections(shared.transform);
            AddDinerGarden(shared.transform, art);
            AliceFenceAuthoring.Apply(shared.transform);
            StreetLifeAuthoring.Apply(shared.transform);
            var surround = Material("StreetEveningSurround", "HollowCreek/Evening Street Surround", new Color(0.16f, 0.22f, 0.34f));
            surround.SetTexture("_MainTex", AssetDatabase.LoadAssetAtPath<Texture2D>(Directory + "/Kloppenheim07.jpg"));
            var sky = Primitive(shared.transform, "Continuous evening horizon", PrimitiveType.Sphere, anchor + Vector3.up * 1.5f, Vector3.one * 320, surround);
            sky.GetComponent<Renderer>().allowOcclusionWhenDynamic = false;

            AssetDatabase.SaveAssets();
            var prefab = PrefabUtility.SaveAsPrefabAsset(shared, PrefabPath);
            Object.DestroyImmediate(shared);
            foreach (var old in root.Cast<Transform>().Where(t => PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(t.gameObject) == PrefabPath).ToArray())
                Object.DestroyImmediate(old.gameObject);
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            instance.transform.SetParent(root, false);
            instance.name = "Shared evening street";
            PrefabUtility.RecordPrefabInstancePropertyModifications(instance);
            foreach (var name in new[] { "Street details", "Street lighting" })
            {
                var copy = instance.transform.Find(name).gameObject;
                copy.SetActive(false);
                PrefabUtility.RecordPrefabInstancePropertyModifications(copy);
            }
            EditorSceneManager.MarkSceneDirty(street);
            EditorSceneManager.SaveScene(street);

            EditorSceneManager.OpenScene("Assets/Scenes/Locations/Diner.unity", OpenSceneMode.Single);
            DinerWindowAuthoring.Build();
            if(AssetDatabase.LoadAssetAtPath<GameObject>(UnifiedDinerAuthoring.InteriorPrefabPath))UnifiedDinerAuthoring.Build();
        }

        public static GameObject InstantiateForDiner(Transform parent)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            if (!prefab) throw new InvalidOperationException("Build the shared evening street before rebuilding the diner window.");
            var street = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            street.transform.SetParent(parent, false);
            var anchor = prefab.transform.Find("Diner exterior window anchor").localPosition;
            var rotation = Quaternion.Euler(0, 90, 0);
            street.transform.SetPositionAndRotation(InteriorWindow - rotation * anchor, rotation);
            // The interior already contains the diner walls. An exterior facade would seal its window.
            street.transform.Find("Street buildings and trees/Diner facade").gameObject.SetActive(false);
            street.transform.Find("Street lighting/Diner Sign Light").gameObject.SetActive(false);
            return street;
        }

        static GameObject CopyVisual(GameObject source, Transform parent)
        {
            var copy = Object.Instantiate(source, parent, true);
            copy.name = source.name;
            copy.SetActive(true);
            foreach (var collider in copy.GetComponentsInChildren<Collider>(true)) Object.DestroyImmediate(collider);
            foreach (var behaviour in copy.GetComponentsInChildren<MonoBehaviour>(true))
                if (behaviour && behaviour.GetType().Namespace != null && behaviour.GetType().Namespace.StartsWith("HollowCreek"))
                    Object.DestroyImmediate(behaviour);
            return copy;
        }

        static Material NightMaterial(Material source)
        {
            var texture = source.mainTexture;
            var family = texture ? AssetDatabase.GetAssetPath(texture).Contains("Suburban") ? "Suburban" : "Graveyard" : source.name;
            var material = Material("StreetEvening" + family, "Universal Render Pipeline/Unlit", new Color(0.34f, 0.39f, 0.48f));
            material.SetTexture("_BaseMap", texture);
            if (!texture && source.HasProperty("_BaseColor"))
                material.SetColor("_BaseColor", source.GetColor("_BaseColor") * new Color(0.34f, 0.39f, 0.48f, 1));
            return material;
        }

        static void AddOppositeHouse(Transform parent)
        {
            var source = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/Environment/Kenney/Suburban/building-type-c.fbx");
            var house = Object.Instantiate(source, parent);
            house.name = "House silhouette opposite diner";
            house.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            house.transform.localScale = Vector3.one;
            var renderers = house.GetComponentsInChildren<Renderer>();
            var bounds = renderers[0].bounds;
            foreach (var r in renderers) bounds.Encapsulate(r.bounds);
            house.transform.localScale = Vector3.one * (4.2f / bounds.size.x);
            bounds = renderers[0].bounds;
            foreach (var r in renderers) bounds.Encapsulate(r.bounds);
            house.transform.position = new Vector3(20.35f - bounds.center.x, -bounds.min.y, -16.2f - bounds.center.z);
            var material = Material("StreetOppositeHouse", "Universal Render Pipeline/Unlit", new Color(0.19f, 0.24f, 0.33f));
            material.SetTexture("_BaseMap", renderers[0].sharedMaterial.mainTexture);
            foreach (var r in renderers) r.sharedMaterials = r.sharedMaterials.Select(_ => material).ToArray();
        }

        static void AddWetReflections(Transform parent)
        {
            var reflection = Material("StreetSoftWetReflection", "HollowCreek/Distant Soft Light", new Color(1, 0.68f, 0.36f, 0.075f));
            var glow = Material("StreetDistantLanternGlow", "HollowCreek/Garland Bulb Glow", new Color(1, 0.68f, 0.36f, 0.45f));
            for (var x = -30; x <= 30; x += 12)
            {
                var pool = Primitive(parent, "Lantern reflection on wet road " + x, PrimitiveType.Quad,
                    new Vector3(x, 0.017f, -1.7f), new Vector3(1.1f, 3.4f, 1), reflection);
                pool.transform.rotation = Quaternion.Euler(90, 0, 0);
                for (var side = -1; side <= 1; side += 2)
                    Primitive(parent, "Soft street lantern " + x + " " + side, PrimitiveType.Quad,
                        new Vector3(x + side * 0.75f, 4.1f, 0.4f), Vector3.one * 0.32f, glow);
            }
        }

        static void AddDinerGarden(Transform parent, Transform streetArt)
        {
            var garden = new GameObject("Diner side and rear garden").transform;
            garden.SetParent(parent, false);
            var pavement = Material("StreetNightPavement", "Universal Render Pipeline/Unlit", new Color(0.25f, 0.275f, 0.29f));
            Box(garden, "Side walkway", new Vector3(12.1f, 0.04f, 17.5f), new Vector3(2, 0.08f, 16), pavement);
            Box(garden, "Rear walkway", new Vector3(20, 0.04f, 25.2f), new Vector3(26, 0.08f, 2.4f), pavement);
            GardenModel(garden, streetArt.Cast<Transform>().First(t => t.name == "pine"), "Rear pine", new Vector3(14.8f, 0, 29.3f), 4.8f, 8);
            GardenModel(garden, streetArt.Cast<Transform>().First(t => t.name == "pine-fall"), "Rear autumn tree", new Vector3(24.8f, 0, 30.2f), 5.3f, 25);
            GardenModel(garden, streetArt.Cast<Transform>().First(t => t.name == "pine-fall-crooked"), "Side autumn tree", new Vector3(8.5f, 0, 11.2f), 4.3f, 24);

            var lamp = new GameObject("Rear garden lamp").transform;
            lamp.SetParent(garden, false);
            var post = GardenModel(lamp, streetArt.Cast<Transform>().First(t => t.name == "lightpost-single"), "Lamp post", Vector3.zero, 3.8f, 0);
            var bounds = post.GetComponentInChildren<Renderer>().bounds;
            var centre = new Vector3(bounds.max.x - 0.20f, bounds.max.y - 0.48f, 0);
            var glow = Material("StreetDistantLanternGlow", "HollowCreek/Garland Bulb Glow", new Color(1, 0.68f, 0.36f, 0.45f));
            Primitive(lamp, "Soft garden lantern glow", PrimitiveType.Quad, centre, Vector3.one * 0.40f, glow);
            var lightObject = new GameObject("Garden lantern light");
            lightObject.transform.SetParent(lamp, false);
            lightObject.transform.position = centre;
            var light = lightObject.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = new Color(1, 0.68f, 0.36f);
            light.intensity = 3.5f;
            light.range = 7.5f;
            lamp.SetPositionAndRotation(new Vector3(21.8f, 0.081f, 25.8f), Quaternion.Euler(0, 90, 0));
            var reflection = Material("StreetSoftWetReflection", "HollowCreek/Distant Soft Light", new Color(1, 0.68f, 0.36f, 0.075f));
            var pool = Primitive(garden, "Garden light on wet walkway", PrimitiveType.Quad, new Vector3(21.8f, 0.083f, 24.9f), new Vector3(1.5f, 2.4f, 1), reflection);
            pool.transform.rotation = Quaternion.Euler(90, 0, 0);
        }

        static GameObject GardenModel(Transform parent, Transform source, string name, Vector3 position, float height, float yaw)
        {
            var model = CopyVisual(source.gameObject, parent);
            model.name = name;
            model.transform.SetPositionAndRotation(Vector3.zero, Quaternion.Euler(0, yaw, 0));
            var renderers = model.GetComponentsInChildren<Renderer>();
            foreach (var renderer in renderers)
            {
                renderer.enabled = true;
                renderer.sharedMaterials = renderer.sharedMaterials.Select(m => m.name.Contains("Lightpost") ? m : NightMaterial(m)).ToArray();
            }
            var bounds = renderers[0].bounds;
            foreach (var renderer in renderers) bounds.Encapsulate(renderer.bounds);
            model.transform.localScale *= height / bounds.size.y;
            bounds = renderers[0].bounds;
            foreach (var renderer in renderers) bounds.Encapsulate(renderer.bounds);
            model.transform.position = position - new Vector3(bounds.center.x, bounds.min.y, bounds.center.z);
            return model;
        }

        static Material Material(string name, string shaderName, Color color)
        {
            var shader = Shader.Find(shaderName);
            if (!shader) throw new InvalidOperationException("Missing shader: " + shaderName);
            var path = Directory + "/" + name + ".mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (!material) { material = new Material(shader); AssetDatabase.CreateAsset(material, path); }
            material.shader = shader;
            material.SetColor(material.HasProperty("_BaseColor") ? "_BaseColor" : "_Tint", color);
            EditorUtility.SetDirty(material);
            return material;
        }

        static GameObject Primitive(Transform parent, string name, PrimitiveType type, Vector3 position, Vector3 size, Material material)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.position = position;
            go.transform.localScale = size;
            Object.DestroyImmediate(go.GetComponent<Collider>());
            var renderer = go.GetComponent<Renderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            return go;
        }

        static void Box(Transform parent, string name, Vector3 position, Vector3 size, Material material) =>
            Primitive(parent, name, PrimitiveType.Cube, position, size, material);
    }
}

