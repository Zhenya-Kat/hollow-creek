using System.Collections.Generic;
using System.Linq;
using HollowCreek.Core.Facts;
using HollowCreek.Core.Logic;
using HollowCreek.Gameplay.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace HollowCreek.Editor.Props
{
    /// <summary>
    /// Расставляет префабы реквизита в сценах локаций. Повторный запуск безопасен: старые модели заменяются.
    /// <para>
    /// «Attach» — модель становится дочерним объектом «Model» у интерактивного объекта. Сам объект
    /// приводится к масштабу 1 (размер переносится в коллайдер), его серый куб перестаёт рисоваться,
    /// а коллайдер подгоняется под модель — логика (действия, осмотр, состояния) не меняется.
    /// «Place» — модель ставится рядом как отдельный объект (обстановка без логики).
    /// </para>
    /// </summary>
    public static class PropPlacement
    {
        sealed class Spec
        {
            public string Target;          // Attach: имя интерактивного объекта; Place: имя родителя
            public string Prefab;
            public Vector3 Position;       // Attach: локально у цели; Place: мировая позиция
            public float Yaw;
            public bool Place;
            public bool FitCollider = true;
            public Vector3 MinCollider = new(0.06f, 0.03f, 0.06f);
            public Vector3? ColliderCenter;  // без подгонки: задать коллайдер явно
            public Vector3? ColliderSize;
        }

        static Spec Attach(string target, string prefab, Vector3 local, float yaw = 0, bool fit = true) =>
            new() { Target = target, Prefab = prefab, Position = local, Yaw = yaw, FitCollider = fit };

        static Spec With(Spec spec, Vector3 colliderCenter, Vector3 colliderSize)
        {
            spec.ColliderCenter = colliderCenter;
            spec.ColliderSize = colliderSize;
            return spec;
        }

        static Spec Place(string parent, string prefab, Vector3 world, float yaw = 0) =>
            new() { Target = parent, Prefab = prefab, Position = world, Yaw = yaw, Place = true };

        static readonly Dictionary<string, (Spec[] specs, string[] hide)> Scenes = new()
        {
            ["Street"] = (new[]
            {
                Attach("Poster", "Poster", new Vector3(0, 0, -0.015f)),
                Attach("Postcard", "Postcard", new Vector3(0, -0.03f, 0)),
                Attach("Monument", "Monument", new Vector3(0, -2.5f, 0)),
            }, new[] { "Poster Text", "Monument Plaque", "Monument Base" }),

            ["House"] = (new[]
            {
                Attach("Safe", "Safe", new Vector3(0, -0.5f, 0)),
                Attach("Safe Note", "SafeNote", new Vector3(0, 0, -0.005f)),
                Attach("Watch", "Watch", new Vector3(0, 0.005f, 0)),
                Attach("Receipt", "Receipt", new Vector3(0, 0.016f, 0)),
                Attach("Pencil", "Pencil", new Vector3(0, -0.01f, 0)),
                Attach("Diary", "Diary", new Vector3(0, -0.02f, 0)),
                // Бумаги и ящик соседствуют с ежедневником и диктофоном: их зоны не раздуваем, чтобы не перекрыть соседей.
                With(Attach("Papers", "Papers", new Vector3(0, -0.01f, 0), 0, false), Vector3.zero, new Vector3(0.5f, 0.02f, 0.35f)),
                With(Attach("Drawer", "Drawer", new Vector3(0, 0, -0.12f), 180, false), new Vector3(0, 0, -0.12f), new Vector3(0.55f, 0.14f, 0.05f)),
                Attach("Recorder Spot", "Recorder", new Vector3(0, 0.048f, 0), 180),
                Attach("Photo Box", "PhotoBox", new Vector3(0, -0.1f, 0), 270),
                Attach("Portrait", "Portrait", new Vector3(-0.005f, 0, 0), 270),
                Attach("Mirror", "MirrorGlass", new Vector3(0.005f, 0, 0), 270, false),
                Attach("Mirror Writing", "MirrorGlassFogged", new Vector3(0.025f, 0, 0), 270, false),
                Attach("Front Door", "HouseDoor", new Vector3(0, -1.1f, -0.08f), 0, false),
                Place("Props", "MirrorFrame", new Vector3(4.925f, 1.6f, 12f), 270),
                Place("Props", "Candlestick", new Vector3(-4.4f, 1.13f, 7.55f)),
                Place("Props", "ShelfDust", new Vector3(-4.42f, 1.122f, 7.85f), 90),
            }, new[] { "Mirror Frame", "Candlestick", "Dust Circle" }),

            ["Diner"] = (new[]
            {
                Attach("Outage Notice", "OutageNotice", new Vector3(0, 0, -0.01f)),
                Attach("Pastry Showcase", "PastryShowcase", new Vector3(0, -0.25f, 0)),
                Attach("Coffee Cup", "DinerMug", new Vector3(0, -0.06f, 0)),
                Attach("Diner Window", "DinerBlinds", new Vector3(0.025f, 0, 0), 90, false),
                Attach("Front Door", "DinerDoor", new Vector3(0, -1.1f, -0.08f), 0, false),
                Place("Props", "Register", new Vector3(3.5f, 1.05f, 8.5f)),
                Place("Props", "Tongs", new Vector3(0.35f, 1.046f, 8.3f), 30),
                Place("Props", "PieSlice", new Vector3(-4.65f, 0.76f, 5.8f), 200),
            }, new[] { "Register", "cup-coffee", "pie" }),
        };

        /// <summary>Зонды отражений: латунь, хром, стекло и зеркало отражают комнату, а не небо.</summary>
        static readonly (string scene, string name, Vector3 center, Vector3 size)[] Probes =
        {
            ("House", "Study", new Vector3(0, 1.5f, 9.5f), new Vector3(10, 3, 9)),
            ("House", "Hall", new Vector3(0, 1.5f, 2.5f), new Vector3(10, 3, 5)),
            ("Diner", "Hall", new Vector3(0, 1.6f, 5f), new Vector3(12, 3.2f, 10)),
        };

        [MenuItem("Hollow Creek/Реквизит/Расставить в сценах")]
        static void RunMenu() => Debug.Log("[Props] " + Run());

        public static string Run()
        {
            var log = new List<string>();
            foreach (var (scene, (specs, hide)) in Scenes)
            {
                var path = $"Assets/Scenes/Locations/{scene}.unity";
                var s = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
                foreach (var name in hide) HideRenderers(name);
                foreach (var spec in specs)
                {
                    var ok = spec.Place ? DoPlace(spec) : DoAttach(spec);
                    log.Add($"{scene}/{spec.Prefab}{(ok ? "" : " — НЕ НАЙДЕН " + spec.Target)}");
                }
                if (scene == "House")
                {
                    SafeStates();
                    // Надпись проступает прямо на запотевшем стекле.
                    var text = Find("Mirror Text");
                    if (text != null) text.transform.position = new Vector3(4.898f, text.transform.position.y, text.transform.position.z);
                }
                foreach (var probe in Probes.Where(p => p.scene == scene)) BakeProbe(probe.scene, probe.name, probe.center, probe.size);
                EditorSceneManager.MarkSceneDirty(s);
                EditorSceneManager.SaveScene(s);
            }
            return string.Join(", ", log);
        }

        static GameObject Find(string name) =>
            Object.FindObjectsByType<Transform>(FindObjectsInactive.Include).FirstOrDefault(t => t.name == name)?.gameObject;

        static GameObject LoadPrefab(string name) =>
            AssetDatabase.LoadAssetAtPath<GameObject>($"{PropMaterials.Root}/{name}.prefab");

        static void HideRenderers(string name)
        {
            foreach (var t in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include).Where(t => t.name == name))
            {
                // Прячем только собственный рисунок объекта: у реквизита-замены имя другое.
                foreach (var r in t.GetComponents<Renderer>()) r.enabled = false;
                // Kenney-модели — префабы, их рисунок у дочерних объектов.
                if (PrefabUtility.IsAnyPrefabInstanceRoot(t.gameObject))
                    foreach (var r in t.GetComponentsInChildren<Renderer>(true)) r.enabled = false;
            }
        }

        static bool DoAttach(Spec spec)
        {
            var target = Find(spec.Target);
            var prefab = LoadPrefab(spec.Prefab);
            if (target == null || prefab == null) return false;
            var t = target.transform;

            // Масштаб → в коллайдер и дочерние объекты, чтобы модель не растягивалась.
            var scale = t.localScale;
            if (scale != Vector3.one)
            {
                t.localScale = Vector3.one;
                foreach (Transform child in t)
                {
                    child.localPosition = Vector3.Scale(child.localPosition, scale);
                    child.localScale = Vector3.Scale(child.localScale, scale);
                }
                switch (target.GetComponent<Collider>())
                {
                    case BoxCollider box:
                        box.center = Vector3.Scale(box.center, scale);
                        box.size = Vector3.Scale(box.size, scale);
                        break;
                    case CapsuleCollider capsule:
                        capsule.center = Vector3.Scale(capsule.center, scale);
                        capsule.radius *= Mathf.Max(scale.x, scale.z);
                        capsule.height *= scale.y;
                        break;
                }
            }
            // Серый куб больше не рисуем (фильтр меша оставляем — от него ничего не зависит).
            var ownRenderer = target.GetComponent<MeshRenderer>();
            if (ownRenderer != null) ownRenderer.enabled = false;

            var old = t.Find("Model");
            if (old != null) Object.DestroyImmediate(old.gameObject);
            var model = (GameObject)PrefabUtility.InstantiatePrefab(prefab, t);
            model.name = "Model";
            model.transform.localPosition = spec.Position;
            model.transform.localRotation = Quaternion.Euler(0, spec.Yaw, 0);
            model.transform.localScale = Vector3.one;

            if (spec.ColliderCenter is { } center && target.GetComponent<Collider>() is BoxCollider moved)
            {
                moved.center = center;
                if (spec.ColliderSize is { } size) moved.size = size;
            }
            if (spec.FitCollider && target.GetComponent<Collider>() is BoxCollider fit)
            {
                var bounds = LocalBounds(t, model);
                fit.center = bounds.center;
                fit.size = Vector3.Max(bounds.size + Vector3.one * 0.01f, spec.MinCollider);
            }
            return true;
        }

        static bool DoPlace(Spec spec)
        {
            var parent = Find(spec.Target);
            var prefab = LoadPrefab(spec.Prefab);
            if (parent == null || prefab == null) return false;
            var name = spec.Prefab + " (Prop)";
            var old = parent.transform.Find(name);
            if (old != null) Object.DestroyImmediate(old.gameObject);
            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent.transform);
            go.name = name;
            go.transform.position = spec.Position;
            go.transform.rotation = Quaternion.Euler(0, spec.Yaw, 0);
            return true;
        }

        /// <summary>Границы всех мешей модели в локальных координатах цели.</summary>
        static Bounds LocalBounds(Transform space, GameObject model)
        {
            var first = true;
            var b = new Bounds();
            foreach (var mf in model.GetComponentsInChildren<MeshFilter>())
            {
                if (mf.sharedMesh == null) continue;
                var mb = mf.sharedMesh.bounds;
                for (var i = 0; i < 8; i++)
                {
                    var corner = mb.center + Vector3.Scale(mb.extents, new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1));
                    var p = space.InverseTransformPoint(mf.transform.TransformPoint(corner));
                    if (first)
                    {
                        b = new Bounds(p, Vector3.zero);
                        first = false;
                    }
                    else b.Encapsulate(p);
                }
            }
            return b;
        }

        /// <summary>Запекает зонд отражений в кубическую карту и подключает её как готовую (Custom).</summary>
        static void BakeProbe(string scene, string name, Vector3 center, Vector3 size)
        {
            var root = Find($"[Location] {scene}");
            if (root == null) return;
            var goName = $"Reflection Probe · {name}";
            var old = root.transform.Find(goName);
            if (old != null) Object.DestroyImmediate(old.gameObject);
            var go = new GameObject(goName);
            go.transform.SetParent(root.transform, false);
            go.transform.position = center;
            var probe = go.AddComponent<ReflectionProbe>();
            probe.mode = UnityEngine.Rendering.ReflectionProbeMode.Custom;
            probe.size = size;
            probe.boxProjection = true;
            probe.resolution = 256;
            probe.hdr = true;
            // Запекание зонда видит только «статичные» объекты, поэтому снимаем кубическую карту камерой.
            PropMaterials.EnsureFolder(PropMaterials.Root + "/Reflections");
            var path = $"{PropMaterials.Root}/Reflections/{scene}_{name}.cubemap";
            var cubemap = AssetDatabase.LoadAssetAtPath<Cubemap>(path);
            if (cubemap == null)
            {
                cubemap = new Cubemap(256, TextureFormat.RGBAHalf, true);
                AssetDatabase.CreateAsset(cubemap, path);
            }
            var camGo = new GameObject("Probe Camera");
            camGo.transform.position = center;
            var cam = camGo.AddComponent<Camera>();
            cam.nearClipPlane = 0.05f;
            cam.farClipPlane = 60f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Color.black;
            cam.RenderToCubemap(cubemap);
            Object.DestroyImmediate(camGo);
            EditorUtility.SetDirty(cubemap);
            AssetDatabase.SaveAssets();
            probe.customBakedTexture = cubemap;
        }

        /// <summary>Дверца сейфа открыта, когда документы из сейфа уже у игрока.</summary>
        static void SafeStates()
        {
            var model = Find("Safe")?.transform.Find("Model");
            var ledger = AssetDatabase.LoadAssetAtPath<FactDefinition>("Assets/Data/Episode01/Clues/Clue_Ledger.asset");
            if (model == null || ledger == null) return;
            var props = Find("Props").transform;
            SetState(props, "Safe State · Closed", new Not(new HasFact(ledger)), model.Find("Door Closed").gameObject);
            SetState(props, "Safe State · Open", new HasFact(ledger), model.Find("Door Open").gameObject);
        }

        static void SetState(Transform parent, string name, Condition condition, GameObject target)
        {
            var old = parent.Find(name);
            if (old != null) Object.DestroyImmediate(old.gameObject);
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var sv = go.AddComponent<StateVisibility>();
            var so = new SerializedObject(sv);
            so.FindProperty("visibleWhen").managedReferenceValue = condition;
            var targets = so.FindProperty("targets");
            targets.arraySize = 1;
            targets.GetArrayElementAtIndex(0).objectReferenceValue = target;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
