using System;
using HollowCreek.Core;
using HollowCreek.Core.Data;
using HollowCreek.Core.State;
using HollowCreek.Core.Story;
using HollowCreek.Gameplay.Locations;
using HollowCreek.Gameplay.Player;
using HollowCreek.Gameplay.Save;
using UnityEngine;

namespace HollowCreek.Gameplay.Bootstrap
{
    /// <summary>
    /// Точка входа в игру. Живёт в постоянной сцене Bootstrap: создаёт состояние игры,
    /// загружает сохранение (если есть) и первую локацию.
    /// Сервисы-компоненты (управление, загрузчик локаций…) регистрируются сами в своём Awake.
    /// </summary>
    [DefaultExecutionOrder(-1000)]
    public sealed class GameBootstrapper : MonoBehaviour
    {
        /// <summary>
        /// Ключ, через который редактор передаёт имя сцены, открытой перед нажатием Play.
        /// Так можно запускать игру прямо из сцены локации.
        /// </summary>
        public const string EditorStartSceneKey = "HollowCreek.EditorStartScene";

        [SerializeField] EpisodeDefinition episode;
        [SerializeField] LocationLoader locationLoader;
        [SerializeField] LocationCatalog locationCatalog;

        readonly GameState state = new();

        void Awake()
        {
            Services.Register(state);
            Services.Register(locationCatalog);
            Services.Register(episode);
        }

        void OnDestroy()
        {
            Services.Unregister(state);
            Services.Unregister(locationCatalog);
            Services.Unregister(episode);
        }

        async void Start()
        {
            try
            {
                var saves = Services.Get<SaveService>();
                var testLocation = EditorTestLocation();
                if (testLocation != null)
                {
                    // Проверка локации из редактора: чистое состояние, настоящее сохранение не трогаем.
                    await locationLoader.LoadAsync(testLocation);
                    saves.Begin(episode, enableAutosave: false);
                    return;
                }

                var save = saves.Store.Read();
                if (save != null && save.episode == episode.Id)
                {
                    var location = saves.Apply(save, episode, locationCatalog) ?? episode.StartLocation;
                    await locationLoader.LoadAsync(location);
                    if (save.location == location.Id && Services.TryGet<PlayerController>(out var player))
                        player.Teleport(save.position, save.yaw, save.pitch);
                }
                else
                {
                    await locationLoader.LoadAsync(episode.StartLocation);
                }
                saves.Begin(episode, enableAutosave: true);
            }
            catch (Exception e)
            {
                Debug.LogException(e, this);
            }
        }

        /// <summary>Локация, открытая в редакторе перед нажатием Play (null — обычный запуск).</summary>
        LocationDefinition EditorTestLocation()
        {
#if UNITY_EDITOR
            var editorScene = UnityEditor.SessionState.GetString(EditorStartSceneKey, string.Empty);
            UnityEditor.SessionState.EraseString(EditorStartSceneKey);
            return locationCatalog.FindBySceneName(editorScene);
#else
            return null;
#endif
        }
    }
}
