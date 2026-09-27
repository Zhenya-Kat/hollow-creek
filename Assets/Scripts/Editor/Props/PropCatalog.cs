using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace HollowCreek.Editor.Props
{
    /// <summary>Помечает метод <c>static void Имя(PropBuilder b)</c>, который строит предмет «Имя».</summary>
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class PropRecipeAttribute : Attribute { }

    /// <summary>
    /// Все рецепты реквизита. Меню «Hollow Creek → Реквизит → Собрать все модели» пересобирает
    /// меши, материалы и префабы в <c>Assets/Art/Props</c>; сцены подхватывают изменения сами.
    /// </summary>
    public static class PropCatalog
    {
        public static IEnumerable<MethodInfo> Recipes() =>
            typeof(PropCatalog).Assembly.GetTypes()
                .SelectMany(t => t.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic))
                .Where(m => m.GetCustomAttribute<PropRecipeAttribute>() != null);

        [MenuItem("Hollow Creek/Реквизит/Собрать все модели")]
        static void BuildAllMenu() => Debug.Log("[Props] " + Build());

        /// <summary>Собрать все рецепты или только перечисленные через запятую.</summary>
        public static string Build(string only = null)
        {
            var names = string.IsNullOrEmpty(only) ? null : new HashSet<string>(only.Split(',').Select(s => s.Trim()));
            PropMaterials.BeginBuild();
            var built = new List<string>();
            try
            {
                foreach (var recipe in Recipes().OrderBy(m => m.Name))
                {
                    if (names != null && !names.Contains(recipe.Name)) continue;
                    var builder = new PropBuilder(recipe.Name);
                    try
                    {
                        recipe.Invoke(null, new object[] { builder });
                        builder.Save();
                        built.Add(recipe.Name);
                    }
                    catch (Exception e)
                    {
                        UnityEngine.Object.DestroyImmediate(builder.Root);
                        Debug.LogException(e.InnerException ?? e);
                        built.Add(recipe.Name + " (ошибка)");
                    }
                }
            }
            finally
            {
                AssetDatabase.SaveAssets();
            }
            return "собрано: " + string.Join(", ", built);
        }
    }
}
