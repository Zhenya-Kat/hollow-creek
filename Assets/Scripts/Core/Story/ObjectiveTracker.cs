using System;
using System.Collections.Generic;
using HollowCreek.Core.Facts;
using HollowCreek.Core.State;

namespace HollowCreek.Core.Story
{
    /// <summary>
    /// Текущие цели расследования и открытые подсказки. Подсказки открываются по одной:
    /// сначала намёк, потом сильнее, в конце — прямой ответ.
    /// </summary>
    public sealed class ObjectiveTracker
    {
        readonly ObjectiveSet set;
        readonly IGameStateReader state;
        readonly Dictionary<string, int> revealed = new();

        public ObjectiveTracker(ObjectiveSet set, IGameStateReader state)
        {
            this.set = set;
            this.state = state;
            state.FactGranted += OnFactGranted;
            state.Restored += RaiseChanged;
        }

        /// <summary>Цели или подсказки изменились.</summary>
        public event Action Changed;

        /// <summary>Главная цель — её показывает HUD.</summary>
        public Objective Current => set != null ? set.Current(state) : null;

        /// <summary>Все активные невыполненные цели в порядке приоритета.</summary>
        public IEnumerable<Objective> Open
        {
            get
            {
                if (set == null) yield break;
                foreach (var objective in set.Objectives)
                    if (objective.IsOpen(state)) yield return objective;
            }
        }

        public int RevealedHints(Objective objective) =>
            objective != null && revealed.TryGetValue(objective.Id, out var count) ? count : 0;

        public bool HasMoreHints(Objective objective) =>
            objective != null && RevealedHints(objective) < objective.Hints.Count;

        /// <summary>Следующая подсказка — последняя обычно прямой ответ.</summary>
        public bool IsNextHintTheAnswer(Objective objective) =>
            objective != null && RevealedHints(objective) == objective.Hints.Count - 1;

        public bool RevealNextHint(Objective objective)
        {
            if (!HasMoreHints(objective)) return false;
            revealed[objective.Id] = RevealedHints(objective) + 1;
            RaiseChanged();
            return true;
        }

        /// <summary>Для сохранения: «id:сколько открыто».</summary>
        public IEnumerable<string> SaveHints()
        {
            foreach (var pair in revealed) yield return pair.Key + ":" + pair.Value;
        }

        public void LoadHints(IEnumerable<string> entries)
        {
            revealed.Clear();
            if (entries != null)
                foreach (var entry in entries)
                {
                    var colon = entry.LastIndexOf(':');
                    if (colon > 0 && int.TryParse(entry.Substring(colon + 1), out var count))
                        revealed[entry.Substring(0, colon)] = count;
                }
            RaiseChanged();
        }

        void OnFactGranted(FactDefinition _) => RaiseChanged();

        void RaiseChanged() => Changed?.Invoke();
    }
}
