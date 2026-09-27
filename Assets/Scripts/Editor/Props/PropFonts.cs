using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

namespace HollowCreek.Editor.Props
{
    public enum PropFont
    {
        /// <summary>PT Serif Bold — вывески, таблички.</summary>
        Serif,
        /// <summary>PT Sans — печатный текст: объявления, бланки.</summary>
        Sans,
        /// <summary>PT Sans Bold — заголовки на бланках.</summary>
        SansBold,
        /// <summary>Caveat — рукописный текст.</summary>
        Hand,
    }

    /// <summary>
    /// Шрифты TextMeshPro для надписей на предметах. Атласы статические (все нужные символы запекаются
    /// один раз), чтобы файлы шрифтов не менялись в git во время игры.
    /// </summary>
    public static class PropFonts
    {
        const string Dir = "Assets/Art/Fonts";
        const string Chars =
            "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz" +
            "АБВГДЕЁЖЗИЙКЛМНОПРСТУФХЦЧШЩЪЫЬЭЮЯабвгдеёжзийклмнопрстуфхцчшщъыьэюя" +
            "0123456789 .,:;!?-–—«»„“”·'\"()/$%&№*+=#@";

        public static TMP_FontAsset Get(PropFont font) => font switch
        {
            PropFont.Serif => Load("PTSerif-Bold SDF", "Assets/UI/Fonts/PTSerif-Bold.ttf"),
            PropFont.Sans => Load("PTSans-Regular SDF", "Assets/UI/Fonts/PTSans-Regular.ttf"),
            PropFont.SansBold => Load("PTSans-Bold SDF", "Assets/UI/Fonts/PTSans-Bold.ttf"),
            _ => Load("Caveat SDF", Dir + "/Caveat.ttf"),
        };

        static TMP_FontAsset Load(string name, string source)
        {
            var path = $"{Dir}/{name}.asset";
            var existing = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);
            if (existing != null) return existing;

            var ttf = AssetDatabase.LoadAssetAtPath<Font>(source);
            var asset = TMP_FontAsset.CreateFontAsset(ttf, 72, 8, GlyphRenderMode.SDFAA, 1024, 1024, AtlasPopulationMode.Dynamic, true);
            asset.name = name;
            asset.TryAddCharacters(Chars, out var missing);
            if (!string.IsNullOrEmpty(missing)) Debug.LogWarning($"[Props] В шрифте {name} нет символов: {missing}");
            asset.atlasPopulationMode = AtlasPopulationMode.Static;
            AssetDatabase.CreateAsset(asset, path);
            foreach (var atlas in asset.atlasTextures)
            {
                atlas.name = name + " Atlas";
                AssetDatabase.AddObjectToAsset(atlas, asset);
            }
            asset.material.name = name + " Material";
            AssetDatabase.AddObjectToAsset(asset.material, asset);
            EditorUtility.SetDirty(asset);
            AssetDatabase.SaveAssets();
            return AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);
        }
    }
}
