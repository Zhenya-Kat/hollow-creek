using System;
using System.Collections.Generic;
using System.Linq;
using HollowCreek.Editor.Localization;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.TextCore;
using UnityEngine.Localization.Components;
using Object = UnityEngine.Object;

namespace HollowCreek.Editor.Props
{
    /// <summary>
    /// Собирает один предмет: части-меши (каждая сохраняется в <c>Assets/Art/Props/Meshes</c>),
    /// надписи и вложенные объекты, затем сохраняет всё как префаб <c>Assets/Art/Props/&lt;Имя&gt;.prefab</c>.
    /// </summary>
    public sealed class PropBuilder
    {
        public const string MeshesDir = PropMaterials.Root + "/Meshes";

        public readonly string Name;
        public readonly GameObject Root;

        public PropBuilder(string name)
        {
            Name = name;
            Root = new GameObject(name);
        }

        public Transform Group(string name, Transform parent = null, Vector3 position = default, Vector3 euler = default)
        {
            var go = new GameObject(name).transform;
            go.SetParent(parent ? parent : Root.transform, false);
            go.localPosition = position;
            go.localEulerAngles = euler;
            return go;
        }

        /// <summary>Часть предмета из меша. Меш сохраняется отдельным ассетом (его GUID не меняется при пересборке).</summary>
        public GameObject Part(string name, MeshKit kit, Transform parent = null, Vector3 position = default, Vector3 euler = default)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent ? parent : Root.transform, false);
            go.transform.localPosition = position;
            go.transform.localEulerAngles = euler;
            go.AddComponent<MeshFilter>().sharedMesh = SaveMesh(kit.Build($"{Name}_{name}"), $"{Name}_{name}");
            var renderer = go.AddComponent<MeshRenderer>();
            renderer.sharedMaterials = kit.Materials.Select(PropMaterials.Get).ToArray();
            return go;
        }

        static Mesh SaveMesh(Mesh mesh, string file)
        {
            PropMaterials.EnsureFolder(MeshesDir);
            var path = $"{MeshesDir}/{file}.asset";
            var existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (existing == null)
            {
                AssetDatabase.CreateAsset(mesh, path);
                return mesh;
            }
            existing.Clear();
            EditorUtility.CopySerialized(mesh, existing);
            Object.DestroyImmediate(mesh);
            EditorUtility.SetDirty(existing);
            return existing;
        }

        /// <summary>
        /// Надпись TextMeshPro. Поворот задаётся относительно лицевой стороны предмета (+Z):
        /// (0,0,0) — на передней грани, <see cref="OnTop"/> — лёжа на верхней грани.
        /// Если задан <paramref name="key"/>, текст берётся из таблицы локализации «World».
        /// </summary>
        public TextMeshPro Text(string name, Transform parent, Vector3 position, Vector3 euler, Vector2 size,
            PropFont font, Color color, string key = null, string russian = null, string literal = null,
            float fontSize = 0f, TextAlignmentOptions align = TextAlignmentOptions.Center, float lineSpacing = 0f)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent ? parent : Root.transform, false);
            go.transform.localPosition = position;
            // Читаемая сторона TextMeshPro смотрит в −Z, поэтому разворачиваем на 180° к лицу предмета.
            go.transform.localRotation = Quaternion.Euler(euler) * Quaternion.Euler(0, 180, 0);
            var tmp = go.AddComponent<TextMeshPro>();
            tmp.font = PropFonts.Get(font);
            // Без лигатур: их глифов нет в статическом атласе, и буквы пропадали бы.
            tmp.fontFeatures = new List<OTL_FeatureTag> { OTL_FeatureTag.kern, OTL_FeatureTag.mark, OTL_FeatureTag.mkmk };
            tmp.color = color;
            tmp.alignment = align;
            tmp.lineSpacing = lineSpacing;
            tmp.rectTransform.sizeDelta = size;
            tmp.textWrappingMode = TextWrappingModes.Normal;
            if (fontSize > 0)
            {
                tmp.enableAutoSizing = false;
                tmp.fontSize = fontSize;
            }
            else
            {
                tmp.enableAutoSizing = true;
                tmp.fontSizeMin = 0.01f;
                tmp.fontSizeMax = 10f;
            }
            tmp.text = literal ?? russian ?? string.Empty;
            if (key != null)
            {
                var localized = LocalizationAuthoring.Set("World", key, russian ?? string.Empty);
                var localize = go.AddComponent<LocalizeStringEvent>();
                localize.StringReference = localized;
                var setter = (UnityAction<string>)Delegate.CreateDelegate(typeof(UnityAction<string>), tmp, "set_text");
                UnityEventTools.AddPersistentListener(localize.OnUpdateString, setter);
            }
            return tmp;
        }

        /// <summary>Поворот для надписи, лежащей на горизонтальной поверхности (читается со стороны +Z).</summary>
        public static readonly Vector3 OnTop = new(-90, 0, 0);

        public GameObject Save()
        {
            PropMaterials.EnsureFolder(PropMaterials.Root);
            var path = $"{PropMaterials.Root}/{Name}.prefab";
            var prefab = PrefabUtility.SaveAsPrefabAsset(Root, path);
            Object.DestroyImmediate(Root);
            return prefab;
        }
    }
}
