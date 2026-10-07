using System.Linq;
using HollowCreek.Core;
using HollowCreek.Core.Data;
using HollowCreek.Core.Dialogue;
using HollowCreek.Core.Facts;
using HollowCreek.Core.Save;
using HollowCreek.Core.State;
using HollowCreek.Core.Story;
using HollowCreek.Gameplay.Locations;
using HollowCreek.Gameplay.Player;
using UnityEngine;

namespace HollowCreek.Gameplay.Save
{
    /// <summary>
    /// Автосохранение: после каждого изменения (новый факт, заданный вопрос, смена локации),
    /// при сворачивании и при выходе из игры. Восстановление — при запуске (см. GameBootstrapper).
    /// </summary>
    [DefaultExecutionOrder(-850)]
    public sealed class SaveService : MonoBehaviour
    {
        [SerializeField, Tooltip("Через сколько секунд после изменения сохранять (чтобы не писать файл на каждое событие)")]
        float autosaveDelay = 0.5f;

        GameState state;
        DialogueLog log;
        LocationLoader locations;
        EpisodeDefinition episode;
        bool autosave;
        float saveAt = -1f;

        public SaveStore Store { get; private set; }

        void Awake()
        {
            Store = SaveStore.Default();
            state = Services.Get<GameState>();
            log = Services.Get<DialogueLog>();
            locations = Services.Get<LocationLoader>();
            Services.Register(this);
        }

        void OnDestroy()
        {
            Services.Unregister(this);
            state.FactGranted -= OnFactGranted;
            log.Changed -= MarkDirty;
            locations.LocationLoaded -= OnLocationLoaded;
        }

        /// <summary>Включить автосохранение для эпизода (после загрузки первой локации).</summary>
        public void Begin(EpisodeDefinition current, bool enableAutosave)
        {
            episode = current;
            autosave = enableAutosave;
            if (!autosave) return;
            state.FactGranted += OnFactGranted;
            log.Changed += MarkDirty;
            locations.LocationLoaded += OnLocationLoaded;
            MarkDirty();
        }

        void OnFactGranted(FactDefinition _) => MarkDirty();
        void OnLocationLoaded(LocationRoot _, SpawnPoint __) => MarkDirty();

        void MarkDirty()
        {
            if (autosave && saveAt < 0f) saveAt = Time.unscaledTime + autosaveDelay;
        }

        void Update()
        {
            if (saveAt < 0f || Time.unscaledTime < saveAt || locations.IsLoading) return;
            SaveNow();
        }

        void OnApplicationPause(bool paused)
        {
            if (paused) SaveNow();
        }

        void OnApplicationQuit() => SaveNow();

        public void SaveNow()
        {
            saveAt = -1f;
            if (!autosave || episode == null || locations.Current == null) return;
            Store.Write(Capture());
        }

        public SaveData Capture()
        {
            var data = new SaveData
            {
                episode = episode.Id,
                worldSpacePose = true,
                location = locations.Current != null ? locations.Current.Location.Id : null,
                facts = state.Facts.Select(f => f.Id).ToList(),
                askedTopics = log.Entries.ToList(),
            };
            if (Services.TryGet<ObjectiveTracker>(out var objectives)) data.hints = objectives.SaveHints().ToList();
            if (Services.TryGet<PlayerController>(out var player))
            {
                data.position = player.transform.position;
                data.yaw = player.Yaw;
                data.pitch = player.Pitch;
            }
            return data;
        }

        /// <summary>
        /// Восстановить факты и журнал разговоров из сохранения. Неизвестные Id (удалённые из игры факты)
        /// пропускаются с предупреждением. Возвращает локацию из сохранения (или null).
        /// </summary>
        public LocationDefinition Apply(SaveData data, EpisodeDefinition current, LocationCatalog catalog)
        {
            var restored = data.facts.Select(id =>
            {
                var fact = current.FindFact(id);
                if (fact == null) Debug.LogWarning($"[Save] Неизвестный факт «{id}» пропущен.");
                return fact;
            });
            state.Restore(restored.Where(f => f != null).ToList());
            log.Load(data.askedTopics);
            if (Services.TryGet<ObjectiveTracker>(out var objectives)) objectives.LoadHints(data.hints);
            return catalog.FindById(data.location);
        }
    }
}

