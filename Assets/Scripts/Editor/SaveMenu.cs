using HollowCreek.Core.Save;
using UnityEditor;
using UnityEngine;

namespace HollowCreek.Editor
{
    /// <summary>Меню «Hollow Creek → Сохранение»: удалить или показать файл сохранения.</summary>
    static class SaveMenu
    {
        [MenuItem("Hollow Creek/Сохранение/Удалить сохранение")]
        static void DeleteSave()
        {
            var store = SaveStore.Default();
            if (!store.Exists)
            {
                Debug.Log("[Save] Сохранения нет.");
                return;
            }
            store.Delete();
            Debug.Log("[Save] Сохранение удалено: " + store.Path);
        }

        [MenuItem("Hollow Creek/Сохранение/Показать файл")]
        static void Reveal()
        {
            var store = SaveStore.Default();
            EditorUtility.RevealInFinder(store.Exists ? store.Path : Application.persistentDataPath);
        }
    }
}
