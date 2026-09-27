using System;
using HollowCreek.Core.Logic;
using HollowCreek.Core.State;

namespace HollowCreek.Core.Story
{
    /// <summary>
    /// Проверяет сюжетные правила и запускает те, чьё условие выполнено.
    /// Действия правила могут выдать новые факты — тогда правила проверяются снова,
    /// пока ничего не изменится (с защитой от бесконечного цикла).
    /// </summary>
    public sealed class StoryRuleRunner
    {
        const int MaxPasses = 32;

        readonly StoryRuleSet rules;
        readonly IGameStateReader state;
        bool running;
        bool again;

        public StoryRuleRunner(StoryRuleSet rules, IGameStateReader state)
        {
            this.rules = rules;
            this.state = state;
        }

        /// <summary>Правило сработало (для отладки и тестов).</summary>
        public event Action<StoryRule> Fired;

        public void Evaluate()
        {
            if (rules == null) return;
            // Правило выдало факт → FactGranted → Evaluate: не заходим повторно, а делаем ещё один проход.
            if (running)
            {
                again = true;
                return;
            }

            running = true;
            try
            {
                var passes = 0;
                do
                {
                    again = false;
                    foreach (var rule in rules.Rules)
                    {
                        if (rule.When == null || !rule.When.Evaluate(state)) continue;
                        GameAction.RunAll(rule.Actions, default);
                        Fired?.Invoke(rule);
                    }
                    if (++passes >= MaxPasses)
                    {
                        UnityEngine.Debug.LogError("[Story] Правила срабатывают по кругу. У правила должно быть условие " +
                                                   "«Не → Есть факт X», а действие должно выдавать факт X.");
                        break;
                    }
                } while (again);
            }
            finally
            {
                running = false;
            }
        }
    }
}
