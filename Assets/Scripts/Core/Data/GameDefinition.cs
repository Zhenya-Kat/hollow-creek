using UnityEngine;

namespace HollowCreek.Core.Data
{
    /// <summary>
    /// Базовый класс для всех ассетов с игровыми данными (локации, улики, персонажи…).
    /// У каждого есть постоянный <see cref="Id"/> — по нему данные сохраняются и загружаются.
    /// Id берётся из GUID ассета: он не меняется при переименовании и перемещении файла.
    /// </summary>
    public abstract class GameDefinition : ScriptableObject
    {
        [SerializeField, HideInInspector] string id;

        public string Id => id;

#if UNITY_EDITOR
        protected virtual void OnValidate() => SyncId();

        void SyncId()
        {
            var path = UnityEditor.AssetDatabase.GetAssetPath(this);
            if (string.IsNullOrEmpty(path)) return;
            var guid = UnityEditor.AssetDatabase.AssetPathToGUID(path);
            if (id == guid) return;
            id = guid;
            UnityEditor.EditorUtility.SetDirty(this);
        }
#endif
    }
}

