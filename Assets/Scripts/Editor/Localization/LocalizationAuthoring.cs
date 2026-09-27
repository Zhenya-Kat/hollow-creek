using UnityEditor;
using UnityEditor.Localization;
using UnityEngine.Localization;
using UnityEngine.Localization.Tables;

namespace HollowCreek.Editor.Localization
{
    /// <summary>
    /// Помощник для инструментов редактора: создаёт таблицы строк и записи в них
    /// и возвращает <see cref="LocalizedString"/>, который можно положить в поле ассета.
    /// </summary>
    public static class LocalizationAuthoring
    {
        public const string TablesFolder = "Assets/Localization/Tables";
        public const string DefaultLocale = "ru";

        public static StringTableCollection GetOrCreateTable(string name)
        {
            var collection = LocalizationEditorSettings.GetStringTableCollection(name);
            if (collection != null) return collection;

            EnsureFolder(TablesFolder);
            collection = LocalizationEditorSettings.CreateStringTableCollection(name, TablesFolder);
            // Предзагрузка: строки доступны сразу, без ожидания асинхронной загрузки.
            foreach (var table in collection.StringTables)
                LocalizationEditorSettings.SetPreloadTableFlag(table, true);
            return collection;
        }

        /// <summary>Создаёт или обновляет запись и возвращает ссылку на неё.</summary>
        public static LocalizedString Set(string tableName, string key, string text, string locale = DefaultLocale)
        {
            var collection = GetOrCreateTable(tableName);
            var table = (StringTable)collection.GetTable(locale);
            var entry = table.AddEntry(key, text);
            EditorUtility.SetDirty(table);
            EditorUtility.SetDirty(collection.SharedData);
            return new LocalizedString(collection.SharedData.TableCollectionNameGuid, entry.KeyId);
        }

        static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            var slash = path.LastIndexOf('/');
            EnsureFolder(path.Substring(0, slash));
            AssetDatabase.CreateFolder(path.Substring(0, slash), path.Substring(slash + 1));
        }
    }
}
