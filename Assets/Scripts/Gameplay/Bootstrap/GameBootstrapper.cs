using System;
using HollowCreek.Core;
using HollowCreek.Core.Data;
using HollowCreek.Gameplay.Locations;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace HollowCreek.Gameplay.Bootstrap
{
    /// <summary>
    /// Точка входа в игру. Живёт в постоянной сцене Bootstrap: регистрирует сервисы и загружает первую локацию.
    /// </summary>
    [DefaultExecutionOrder(-1000)]
    public sealed class GameBootstrapper : MonoBehaviour
    {
        /// <summary>
        /// Ключ, через который редактор передаёт имя сцены, открытой перед нажатием Play.
        /// Так можно запускать игру прямо из сцены локации.
        /// </summary>
        public const string EditorStartSceneKey = "HollowCreek.EditorStartScene";

        [SerializeField] LocationLoader locationLoader;
        [SerializeField] LocationCatalog locationCatalog;
        [SerializeField, Tooltip("С какой локации начинается новая игра")]
        LocationDefinition startLocation;

        void Awake()
        {
            Services.Register(locationLoader);
            Services.Register(locationCatalog);
        }

        void OnDestroy()
        {
            Services.Unregister(locationLoader);
            Services.Unregister(locationCatalog);
        }

        async void Start()
        {
            try
            {
                var alreadyLoaded = FindLoadedLocation();
                if (alreadyLoaded != null)
                {
                    locationLoader.Adopt(alreadyLoaded);
                    return;
                }
                await locationLoader.LoadAsync(ResolveStartLocation());
            }
            catch (Exception e)
            {
                Debug.LogException(e, this);
            }
        }

        LocationDefinition ResolveStartLocation()
        {
#if UNITY_EDITOR
            var editorScene = UnityEditor.SessionState.GetString(EditorStartSceneKey, string.Empty);
            UnityEditor.SessionState.EraseString(EditorStartSceneKey);
            var fromEditor = locationCatalog.FindBySceneName(editorScene);
            if (fromEditor != null) return fromEditor;
#endif
            return startLocation;
        }

        static LocationRoot FindLoadedLocation()
        {
            for (var i = 0; i < SceneManager.sceneCount; i++)
            {
                var root = LocationRoot.FindIn(SceneManager.GetSceneAt(i));
                if (root != null) return root;
            }
            return null;
        }
    }
}
