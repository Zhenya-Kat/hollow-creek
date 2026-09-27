using System;
using HollowCreek.Core.Dialogue;
using HollowCreek.Core.Facts;
using HollowCreek.Core.Logic;
using HollowCreek.Core.State;
using UnityEngine.Localization;

namespace HollowCreek.Core.Story
{
    /// <summary>Обвинение: можно ли его выдвинуть, проверка ответа и отметка «дело раскрыто».</summary>
    public sealed class CaseService
    {
        readonly GameState state;

        public CaseService(CaseDefinition definition, GameState state)
        {
            Definition = definition;
            this.state = state;
        }

        public CaseDefinition Definition { get; }

        public bool IsSolved => Definition?.SolvedFact != null && state.Has(Definition.SolvedFact);

        public bool IsAvailable => Definition != null && Condition.IsMet(Definition.AvailableWhen, state);

        /// <summary>Дело раскрыто (после этого показывается развязка).</summary>
        public event Action Solved;

        /// <summary>Проверить ответ. Верный — отмечает дело раскрытым.</summary>
        public Verdict Accuse(CharacterDefinition suspect, FactDefinition motive, FactDefinition lie)
        {
            var verdict = Definition.Judge(suspect, motive, lie);
            if (verdict != Verdict.Correct) return verdict;
            if (Definition.SolvedFact != null) state.Grant(Definition.SolvedFact);
            Solved?.Invoke();
            return verdict;
        }

        /// <summary>Пояснение, что не сходится в неверном ответе.</summary>
        public LocalizedString Explain(Verdict verdict, CharacterDefinition suspect) => verdict switch
        {
            Verdict.WrongSuspect => Definition.FindSuspect(suspect)?.WrongReason,
            Verdict.WrongMotive => Definition.WrongMotiveText,
            Verdict.WrongLie => Definition.WrongLieText,
            _ => null,
        };
    }
}
