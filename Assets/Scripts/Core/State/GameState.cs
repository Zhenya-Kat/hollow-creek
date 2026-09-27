using System;
using System.Collections.Generic;
using HollowCreek.Core.Facts;

namespace HollowCreek.Core.State
{
    /// <summary>Только чтение состояния — этого достаточно условиям и интерфейсу.</summary>
    public interface IGameStateReader
    {
        bool Has(FactDefinition fact);
        /// <summary>Полученные факты в порядке получения.</summary>
        IReadOnlyList<FactDefinition> Facts { get; }
        event Action<FactDefinition> FactGranted;
        event Action Restored;
    }

    /// <summary>Состояние прохождения: набор полученных фактов.</summary>
    public sealed class GameState : IGameStateReader
    {
        readonly List<FactDefinition> facts = new();
        readonly HashSet<string> ids = new();

        public event Action<FactDefinition> FactGranted;

        public IReadOnlyList<FactDefinition> Facts => facts;

        public bool Has(FactDefinition fact) => fact != null && ids.Contains(fact.Id);

        /// <summary>Добавляет факт. Возвращает false, если он уже был.</summary>
        public bool Grant(FactDefinition fact)
        {
            if (fact == null) throw new ArgumentNullException(nameof(fact));
            if (string.IsNullOrEmpty(fact.Id))
                throw new InvalidOperationException($"У факта «{fact.name}» нет Id — он должен быть сохранён как ассет.");
            if (!ids.Add(fact.Id)) return false;
            facts.Add(fact);
            FactGranted?.Invoke(fact);
            return true;
        }

        /// <summary>Полный сброс (новая игра). События не вызываются.</summary>
        public void Clear()
        {
            facts.Clear();
            ids.Clear();
        }

        /// <summary>
        /// Состояние целиком заменено (загрузка сохранения). В отличие от <see cref="FactGranted"/>,
        /// уведомления о новых уликах при этом не нужны — только обновить отображение.
        /// </summary>
        public event Action Restored;

        /// <summary>Заменить набор фактов (загрузка сохранения). Порядок сохраняется.</summary>
        public void Restore(IEnumerable<FactDefinition> restored)
        {
            Clear();
            foreach (var fact in restored)
                if (fact != null && !string.IsNullOrEmpty(fact.Id) && ids.Add(fact.Id))
                    facts.Add(fact);
            Restored?.Invoke();
        }
    }
}
