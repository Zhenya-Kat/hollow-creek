using System;
using HollowCreek.Core.Data;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace HollowCreek.Gameplay.Locations
{
    /// <summary>
    /// Загружает и выгружает сцены-локации поверх постоянной сцены Bootstrap.
    /// Одновременно загружена только одна локация.
    /// </summary>
    public sealed class LocationLoader : MonoBehaviour
    {
        /// <summary>Локация загружена; передаётся точка, в которой должен появиться игрок (может быть null).</summary>
        public event Action<LocationRoot, SpawnPoint> LocationLoaded;

        public LocationRoot Current { get; private set; }
        public bool IsLoading { get; private set; }

        public async Awaitable LoadAsync(LocationDefinition location, string spawnId = null)
        {
            if (location == null) throw new ArgumentNullException(nameof(location));
            if (IsLoading)
            {
                Debug.LogWarning($"[Location] Загрузка уже идёт, переход в «{location.name}» пропущен.");
                return;
            }

            IsLoading = true;
            try
            {
                if (Current != null)
                {
                    var previous = Current.gameObject.scene;
                    Current = null;
                    await SceneManager.UnloadSceneAsync(previous);
                }

                await SceneManager.LoadSceneAsync(location.SceneName, LoadSceneMode.Additive);
                var scene = SceneManager.GetSceneByName(location.SceneName);
                var root = LocationRoot.FindIn(scene);
                if (root == null)
                    throw new InvalidOperationException($"В сцене «{location.SceneName}» нет компонента LocationRoot.");
                Enter(root, spawnId);
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>Принять уже загруженную сцену как текущую локацию (запуск игры из сцены локации в редакторе).</summary>
        public void Adopt(LocationRoot root, string spawnId = null)
        {
            if (root == null) throw new ArgumentNullException(nameof(root));
            Enter(root, spawnId);
        }

        void Enter(LocationRoot root, string spawnId)
        {
            // Активная сцена определяет освещение, туман и небо, а также куда попадают новые объекты.
            SceneManager.SetActiveScene(root.gameObject.scene);
            Current = root;
            LocationLoaded?.Invoke(root, root.FindSpawnPoint(spawnId));
        }
    }
}
