using System;
using System.Collections.Generic;
using HollowCreek.Core.Facts;
using HollowCreek.Core.State;
using UnityEngine;

namespace HollowCreek.Core.Logic
{
    /// <summary>С чем выполняется действие: объект, который его запустил.</summary>
    public readonly struct ActionContext
    {
        public readonly GameObject Source;

        public ActionContext(GameObject source) => Source = source;
    }

    /// <summary>
    /// Действие, которое происходит в игре: дать улику, показать сообщение, открыть головоломку…
    /// Действия настраиваются в Inspector списком и выполняются по порядку.
    /// Новые виды действий — это новые наследники этого класса.
    /// </summary>
    [Serializable]
    public abstract class GameAction
    {
        public abstract void Execute(in ActionContext context);

        public static void RunAll(IEnumerable<GameAction> actions, in ActionContext context)
        {
            if (actions == null) return;
            foreach (var action in actions)
                action?.Execute(context);
        }
    }

    [Serializable, SelectorLabel("Дать факт / улику / предмет")]
    public sealed class GrantFactAction : GameAction
    {
        [SerializeField] FactDefinition fact;

        public override void Execute(in ActionContext context)
        {
            if (fact == null)
            {
                Debug.LogWarning("[Action] Не указан факт.", context.Source);
                return;
            }
            Services.Get<GameState>().Grant(fact);
        }
    }
}
