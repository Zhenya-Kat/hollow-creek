using System;
using System.IO;
using UnityEngine;

namespace HollowCreek.Core.Save
{
    /// <summary>
    /// Чтение и запись сохранения в JSON-файл. Запись атомарная: сначала во временный файл, потом замена,
    /// чтобы сбой посреди записи не испортил сохранение.
    /// </summary>
    public sealed class SaveStore
    {
        public const string DefaultFileName = "save.json";

        public SaveStore(string path) => Path = path;

        /// <summary>Сохранение по умолчанию: в папке данных игры пользователя.</summary>
        public static SaveStore Default() =>
            new(System.IO.Path.Combine(Application.persistentDataPath, DefaultFileName));

        public string Path { get; }

        public bool Exists => File.Exists(Path);

        public void Write(SaveData data)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));
            data.version = SaveData.CurrentVersion;
            data.savedAt = DateTime.UtcNow.ToString("o");
            var directory = System.IO.Path.GetDirectoryName(Path);
            if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);

            var temp = Path + ".tmp";
            File.WriteAllText(temp, JsonUtility.ToJson(data, prettyPrint: true));
            if (File.Exists(Path)) File.Replace(temp, Path, null);
            else File.Move(temp, Path);
        }

        /// <summary>Прочитать сохранение. null — нет файла, он повреждён или от более новой версии игры.</summary>
        public SaveData Read()
        {
            if (!Exists) return null;
            try
            {
                var data = JsonUtility.FromJson<SaveData>(File.ReadAllText(Path));
                if (data == null) return null;
                if (data.version > SaveData.CurrentVersion)
                {
                    Debug.LogWarning($"[Save] Сохранение от более новой версии игры ({data.version}), пропускаем.");
                    return null;
                }
                Migrate(data);
                return data;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[Save] Не удалось прочитать сохранение: {e.Message}");
                return null;
            }
        }

        public void Delete()
        {
            if (Exists) File.Delete(Path);
        }

        /// <summary>
        /// Обновление старых сохранений до текущей версии. Каждый шаг поднимает версию на единицу:
        /// <c>if (data.version == 1) { …; data.version = 2; }</c>
        /// </summary>
        static void Migrate(SaveData data)
        {
            data.facts ??= new();
            data.askedTopics ??= new();
            data.hints ??= new();
            data.version = SaveData.CurrentVersion;
        }
    }
}
