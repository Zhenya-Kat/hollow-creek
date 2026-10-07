using System;
using HollowCreek.Core;
using HollowCreek.Core.Data;
using HollowCreek.Core.State;
using HollowCreek.Core.Story;
using HollowCreek.Gameplay.Locations;
using HollowCreek.Gameplay.Menus;
using HollowCreek.Gameplay.Messages;
using HollowCreek.Gameplay.Player;
using HollowCreek.Gameplay.Save;
using HollowCreek.Gameplay.Story;
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
                var pause = Services.Get<PauseService>();
                var testLocation = EditorTestLocation();
                if (testLocation != null)
                {
                    // Проверка локации из редактора: без меню, чистое состояние, настоящее сохранение не трогаем.
                    await locationLoader.LoadAsync(testLocation);
                    saves.Begin(episode, enableAutosave: false);
                    Services.Get<StoryService>().Begin();
                    pause.Enabled = true;
                    return;
                }

                // Стартовая локация — фон для главного меню.
                await locationLoader.LoadAsync(episode.StartLocation);
                var save = saves.Store.Read();
                var canContinue = save != null && save.episode == episode.Id;
                var choice = await (await WaitForService<IMainMenu>()).ShowAsync(canContinue);

                if (choice == MainMenuChoice.Continue && canContinue)
                {
                    var location = saves.Apply(save, episode, locationCatalog) ?? episode.StartLocation;
                    if (locationLoader.Current == null || locationLoader.Current.Location != location)
                        await locationLoader.LoadAsync(location);
                    if (save.location == location.Id && Services.TryGet<PlayerController>(out var player))
                    {
                        var position = save.position;
                        var yaw = save.yaw;
                        if (!save.worldSpacePose) locationLoader.Current.ResolveSavedPose(ref position, ref yaw);
                        player.Teleport(position, yaw, save.pitch);
                    }
                }
                else
                {
                    saves.Store.Delete();
                    Services.Get<IMessagePresenter>().Show(episode.Title, episode.IntroText);
                }
                saves.Begin(episode, enableAutosave: true);
                Services.Get<StoryService>().Begin();
                pause.Enabled = true;
            }
            catch (Exception e)
            {
                Debug.LogException(e, this);
            }
        }

        /// <summary>Интерфейс регистрирует свои сервисы в Start — ждём, пока он будет готов.</summary>
        static async Awaitable<T> WaitForService<T>() where T : class
        {
            T service;
            while (!Services.TryGet(out service)) await Awaitable.NextFrameAsync();
            return service;
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

