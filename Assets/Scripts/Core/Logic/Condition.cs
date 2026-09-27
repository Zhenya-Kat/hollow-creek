using System;
using System.Collections.Generic;
using HollowCreek.Core.Facts;
using HollowCreek.Core.State;
using UnityEngine;

namespace HollowCreek.Core.Logic
{
    /// <summary>
    /// Условие над состоянием игры. Из условий собираются правила: когда доступен вопрос в диалоге,
    /// какая цель сейчас показана, можно ли выдвигать обвинение и т.д.
    /// Новые виды условий — это новые наследники этого класса.
    /// </summary>
    [Serializable]
    public abstract class Condition
    {
        public abstract bool Evaluate(IGameStateReader state);

        /// <summary>Пустое условие (null) считается выполненным.</summary>
        public static bool IsMet(Condition condition, IGameStateReader state) =>
            condition == null || condition.Evaluate(state);
    }

    [Serializable, SelectorLabel("Есть факт")]
    public sealed class HasFact : Condition
    {
        [SerializeField] FactDefinition fact;

        public HasFact() { }
        public HasFact(FactDefinition fact) => this.fact = fact;

        public override bool Evaluate(IGameStateReader state) => state.Has(fact);
    }

    [Serializable, SelectorLabel("Все условия")]
    public sealed class AllOf : Condition
    {
        [SerializeReference, SubclassSelector] List<Condition> conditions = new();

        public AllOf() { }
        public AllOf(params Condition[] conditions) => this.conditions = new List<Condition>(conditions);

        public override bool Evaluate(IGameStateReader state)
        {
            foreach (var c in conditions)
                if (!IsMet(c, state)) return false;
            return true;
        }
    }

    [Serializable, SelectorLabel("Хотя бы одно условие")]
    public sealed class AnyOf : Condition
    {
        [SerializeReference, SubclassSelector] List<Condition> conditions = new();

        public AnyOf() { }
        public AnyOf(params Condition[] conditions) => this.conditions = new List<Condition>(conditions);

        public override bool Evaluate(IGameStateReader state)
        {
            foreach (var c in conditions)
                if (c != null && c.Evaluate(state)) return true;
            return false;
        }
    }

    [Serializable, SelectorLabel("Не")]
    public sealed class Not : Condition
    {
        [SerializeReference, SubclassSelector] Condition condition;

        public Not() { }
        public Not(Condition condition) => this.condition = condition;

        public override bool Evaluate(IGameStateReader state) => !IsMet(condition, state);
    }
}
