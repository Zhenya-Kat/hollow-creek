using HollowCreek.Core;
using HollowCreek.Core.Facts;
using HollowCreek.Core.State;
using HollowCreek.Core.Story;
using UnityEngine;

namespace HollowCreek.Gameplay.Story
{
    /// <summary>
    /// Сюжет эпизода в игре: сюжетные правила, цели с подсказками и обвинение.
    /// Создаёт логические объекты из данных эпизода и регистрирует их как сервисы.
    /// </summary>
    [DefaultExecutionOrder(-840)]
    public sealed class StoryService : MonoBehaviour
    {
        GameState state;
        StoryRuleRunner rules;
        bool rulesActive;

        public ObjectiveTracker Objectives { get; private set; }
        public CaseService Case { get; private set; }

        void Awake()
        {
            var episode = Services.Get<EpisodeDefinition>();
            state = Services.Get<GameState>();
            rules = new StoryRuleRunner(episode.Rules, state);
            Objectives = new ObjectiveTracker(episode.Objectives, state);
            Case = new CaseService(episode.Case, state);
            Services.Register(this);
            Services.Register(Objectives);
            Services.Register(Case);
            state.FactGranted += OnFactGranted;
        }

        void OnDestroy()
        {
            state.FactGranted -= OnFactGranted;
            Services.Unregister(this);
            Services.Unregister(Objectives);
            Services.Unregister(Case);
        }

        /// <summary>
        /// Включить сюжетные правила. Вызывается после загрузки первой локации, когда интерфейс готов
        /// показывать сообщения правил.
        /// </summary>
        public void Begin()
        {
            rulesActive = true;
            rules.Evaluate();
        }

        void OnFactGranted(FactDefinition _)
        {
            if (rulesActive) rules.Evaluate();
        }
    }
}
