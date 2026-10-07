using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace HollowCreek.Editor.Props
{
    /// <summary>
    /// Материалы реквизита. Каждый ключ — отдельный файл в <c>Assets/Art/Props/Materials</c>;
    /// при сборке он создаётся или обновляется по описанию ниже. Текстуры рисует <c>Tools/prop_textures.py</c>.
    /// </summary>
    public static class PropMaterials
    {
        public const string Root = "Assets/Art/Props";
        public const string MaterialsDir = Root + "/Materials";
        public const string TexturesDir = Root + "/Textures";

        sealed class Def
        {
            public Color Color = Color.white;
            public float Metallic;
            public float Smoothness = 0.35f;
            public string Texture;
            public Color Emission = Color.black;
            public float Alpha = 1f;
            public bool Transparent;
            public bool DoubleSided;
        }

        static Def D(Color c, float metal = 0f, float smooth = 0.35f, string tex = null) =>
            new() { Color = c, Metallic = metal, Smoothness = smooth, Texture = tex };

        static Def T(string tex, float smooth = 0.3f, float metal = 0f) =>
            new() { Texture = tex, Smoothness = smooth, Metallic = metal };

        static Def Glass(Color c, float alpha, string tex = null, float smooth = 0.95f) =>
            new() { Color = c, Alpha = alpha, Transparent = true, Smoothness = smooth, Texture = tex, DoubleSided = true };

        static Color C(float r, float g, float b) => new(r, g, b);

        static readonly Dictionary<string, Def> Defs = new()
        {
            // Металлы
            ["Brass"] = D(C(0.80f, 0.62f, 0.33f), 1f, 0.62f, "metal_wear"),
            ["BrassDark"] = D(C(0.52f, 0.39f, 0.20f), 1f, 0.45f, "metal_wear"),
            ["Copper"] = D(C(0.80f, 0.47f, 0.30f), 1f, 0.55f, "metal_wear"),
            ["Bronze"] = D(C(0.42f, 0.33f, 0.20f), 1f, 0.40f, "metal_wear"),
            ["Chrome"] = D(C(0.86f, 0.86f, 0.88f), 1f, 0.86f),
            ["Steel"] = D(C(0.55f, 0.56f, 0.58f), 1f, 0.55f, "metal_wear"),
            ["BakeryTongsSteel"] = D(C(0.78f, 0.80f, 0.82f), 0.75f, 0.48f),
            ["SteelDark"] = D(C(0.20f, 0.21f, 0.22f), 0.9f, 0.45f, "metal_wear"),
            ["BlackMetal"] = D(C(0.05f, 0.05f, 0.05f), 0.7f, 0.40f),
            ["GoldPaint"] = D(C(0.86f, 0.68f, 0.32f), 0.8f, 0.55f),
            // Сейф
            ["SafeEnamel"] = T("safe_enamel", 0.55f, 0.25f),
            ["SafeDoor"] = T("safe_door", 0.55f, 0.25f),
            ["SafeWheels"] = T("safe_wheels", 0.5f, 0.3f),
            ["SafeInside"] = D(C(0.35f, 0.08f, 0.07f), 0f, 0.15f),
            ["Felt"] = D(C(0.30f, 0.06f, 0.06f), 0f, 0.05f),
            // Дерево и ткань
            ["WoodWalnut"] = T("wood_walnut", 0.45f),
            ["WoodOak"] = T("wood_oak", 0.40f),
            ["WoodDoor"] = T("wood_door", 0.45f),
            ["WoodPine"] = T("wood_pine", 0.35f),
            ["WoodCedar"] = T("wood_cedar", 0.4f),
            ["Leather"] = T("leather_brown", 0.35f),
            ["LeatherRed"] = T("leather_red", 0.4f),
            // Бумага
            ["Paper"] = T("paper_plain", 0.1f),
            ["PaperAged"] = T("paper_aged", 0.1f),
            ["Cardboard"] = D(C(0.62f, 0.50f, 0.36f), 0f, 0.1f),
            ["Poster"] = T("poster_art", 0.12f),
            ["PostcardBack"] = T("postcard_back", 0.15f),
            ["PostcardFront"] = T("postcard_front", 0.35f),
            ["SafeNote"] = T("safe_note", 0.1f),
            ["Receipt"] = T("receipt", 0.1f),
            ["Outage"] = T("outage_notice", 0.1f),
            ["Typed"] = T("papers_typed", 0.1f),
            ["Letterhead"] = T("papers_letterhead", 0.1f),
            ["DiaryPages"] = T("diary_pages", 0.1f),
            ["PageEdges"] = T("page_edges", 0.1f),
            ["Photo"] = T("photo_tiles", 0.55f),
            ["Portrait"] = T("portrait", 0.3f),
            ["Tape"] = Glass(C(0.95f, 0.93f, 0.80f), 0.45f, null, 0.7f),
            // Часы
            ["WatchDial"] = T("watch_dial", 0.4f),
            ["WatchGlass"] = Glass(C(0.9f, 0.95f, 1f), 0.35f, "watch_crack"),
            ["CopperShavings"] = D(C(0.85f, 0.45f, 0.25f), 1f, 0.7f),
            // Камень и воск
            ["Granite"] = T("granite", 0.35f),
            ["GraniteDark"] = new Def { Texture = "granite", Color = C(0.6f, 0.6f, 0.62f), Smoothness = 0.4f },
            ["Wax"] = D(C(0.93f, 0.88f, 0.76f), 0f, 0.35f),
            ["Wick"] = D(C(0.08f, 0.07f, 0.06f), 0f, 0.1f),
            // Стекло и зеркало
            ["Glass"] = Glass(C(0.85f, 0.92f, 0.95f), 0.18f),
            ["MirrorGlass"] = D(C(0.72f, 0.76f, 0.78f), 1f, 0.97f),
            ["MirrorFog"] = Glass(C(0.9f, 0.92f, 0.94f), 1f, "mirror_fog", 0.6f),
            ["DustRing"] = Glass(C(0.62f, 0.58f, 0.52f), 1f, "dust_ring", 0.05f),
            // Карандаш
            ["PencilPaint"] = D(C(0.10f, 0.27f, 0.18f), 0f, 0.75f),
            ["PencilWood"] = D(C(0.86f, 0.70f, 0.50f), 0f, 0.2f),
            ["Graphite"] = D(C(0.12f, 0.12f, 0.13f), 0.6f, 0.6f),
            ["Eraser"] = D(C(0.86f, 0.45f, 0.47f), 0f, 0.15f),
            // Пластик и электроника
            ["PlasticBlack"] = D(C(0.04f, 0.04f, 0.045f), 0f, 0.55f),
            ["PlasticGrey"] = D(C(0.35f, 0.36f, 0.37f), 0f, 0.5f),
            ["PlasticBeige"] = D(C(0.78f, 0.74f, 0.64f), 0f, 0.45f),
            ["RegisterKeys"] = T("register_keys", 0.5f),
            ["Lcd"] = new Def { Color = C(0.10f, 0.18f, 0.10f), Emission = C(0.25f, 0.55f, 0.2f), Smoothness = 0.9f, Texture = "lcd" },
            ["Rubber"] = D(C(0.06f, 0.06f, 0.06f), 0f, 0.15f),
            ["Mesh"] = D(C(0.15f, 0.15f, 0.16f), 0.8f, 0.3f, "speaker_mesh"),
            // Закусочная
            ["Ceramic"] = D(C(0.93f, 0.92f, 0.88f), 0f, 0.85f),
            ["CeramicRim"] = D(C(0.60f, 0.12f, 0.12f), 0f, 0.85f),
            ["Coffee"] = D(C(0.10f, 0.05f, 0.03f), 0f, 0.92f),
            ["Lipstick"] = D(C(0.62f, 0.07f, 0.10f), 0f, 0.5f),
            ["PriceTag"] = T("price_tags", 0.2f),
            ["PieCrust"] = D(C(0.84f, 0.60f, 0.34f), 0f, 0.3f),
            ["PieFilling"] = D(C(0.42f, 0.05f, 0.08f), 0f, 0.8f),
            // Двери
            ["DoorPaint"] = D(C(0.20f, 0.23f, 0.20f), 0f, 0.5f),
            ["Aluminium"] = D(C(0.70f, 0.71f, 0.72f), 1f, 0.55f, "metal_wear"),
            ["DoorGlass"] = Glass(C(0.30f, 0.40f, 0.48f), 0.35f),
            ["DoorDecal"] = Glass(Color.white, 1f, "door_decal", 0.3f),
            // Окно
            ["Blinds"] = D(C(0.86f, 0.83f, 0.74f), 0f, 0.35f),
            ["Cord"] = D(C(0.85f, 0.82f, 0.75f), 0f, 0.1f),
            // Шкатулка
            ["BoxLining"] = D(C(0.28f, 0.10f, 0.20f), 0f, 0.1f),
        };

        static readonly Dictionary<string, Material> Cache = new();

        public static void BeginBuild() => Cache.Clear();

        public static Material Get(string key)
        {
            if (Cache.TryGetValue(key, out var cached) && cached != null) return cached;
            if (!Defs.TryGetValue(key, out var def))
            {
                Debug.LogError($"[Props] Нет описания материала «{key}»");
                def = D(Color.magenta);
            }
            EnsureFolder(MaterialsDir);
            var path = $"{MaterialsDir}/{key}.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                AssetDatabase.CreateAsset(mat, path);
            }
            Apply(mat, def);
            EditorUtility.SetDirty(mat);
            Cache[key] = mat;
            return mat;
        }

        static void Apply(Material mat, Def def)
        {
            var color = def.Color;
            color.a = def.Alpha;
            mat.SetColor("_BaseColor", color);
            mat.SetFloat("_Metallic", def.Metallic);
            mat.SetFloat("_Smoothness", def.Smoothness);
            mat.SetTexture("_BaseMap", def.Texture != null ? LoadTexture(def.Texture) : null);
            if (def.Emission.maxColorComponent > 0)
            {
                mat.EnableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", def.Emission);
                mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            }
            else
            {
                mat.DisableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", Color.black);
            }
            mat.SetFloat("_Cull", def.DoubleSided ? (float)CullMode.Off : (float)CullMode.Back);

            if (def.Transparent)
            {
                mat.SetFloat("_Surface", 1);
                mat.SetFloat("_Blend", 0);
                mat.SetOverrideTag("RenderType", "Transparent");
                mat.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
                mat.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
                mat.SetFloat("_SrcBlendAlpha", (float)BlendMode.One);
                mat.SetFloat("_DstBlendAlpha", (float)BlendMode.OneMinusSrcAlpha);
                mat.SetFloat("_ZWrite", 0);
                mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                mat.renderQueue = (int)RenderQueue.Transparent;
            }
            else
            {
                mat.SetFloat("_Surface", 0);
                mat.SetOverrideTag("RenderType", "Opaque");
                mat.SetFloat("_SrcBlend", (float)BlendMode.One);
                mat.SetFloat("_DstBlend", (float)BlendMode.Zero);
                mat.SetFloat("_SrcBlendAlpha", (float)BlendMode.One);
                mat.SetFloat("_DstBlendAlpha", (float)BlendMode.Zero);
                mat.SetFloat("_ZWrite", 1);
                mat.DisableKeyword("_SURFACE_TYPE_TRANSPARENT");
                mat.renderQueue = -1;
            }
        }

        static Texture2D LoadTexture(string name)
        {
            var path = $"{TexturesDir}/{name}.png";
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
            {
                Debug.LogError($"[Props] Нет текстуры {path} — запустите Tools/prop_textures.py");
                return null;
            }
            // Плитки (дерево, металл, камень) повторяются, остальное — картинки на одну грань.
            var tiled = name.StartsWith("wood_") || name.StartsWith("metal_") || name.StartsWith("leather_") ||
                        name.StartsWith("paper_") || name == "granite" || name == "speaker_mesh" || name == "safe_enamel";
            var wrap = tiled ? TextureWrapMode.Repeat : TextureWrapMode.Clamp;
            if (importer.wrapMode != wrap || importer.anisoLevel != 4 || !importer.alphaIsTransparency)
            {
                importer.wrapMode = wrap;
                importer.anisoLevel = 4;
                importer.alphaIsTransparency = true;
                importer.maxTextureSize = 2048;
                importer.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        public static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            var slash = path.LastIndexOf('/');
            EnsureFolder(path.Substring(0, slash));
            AssetDatabase.CreateFolder(path.Substring(0, slash), path.Substring(slash + 1));
        }
    }
}
