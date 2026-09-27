using System;
using System.Collections.Generic;
using HollowCreek.Core.Data;
using HollowCreek.Core.Logic;
using HollowCreek.Core.State;
using UnityEngine;
using UnityEngine.Localization;

namespace HollowCreek.Core.Story
{
    /// <summary>
    /// Цели расследования. Порядок в списке — приоритет: в HUD показывается первая активная
    /// и ещё не выполненная цель, в дневнике — все активные.
    /// У каждой цели есть подсказки: от лёгкого намёка до прямого ответа.
    /// </summary>
    [CreateAssetMenu(menuName = "Hollow Creek/Objectives", fileName = "Objectives", order = 20)]
    public sealed class ObjectiveSet : ScriptableObject
    {
        [SerializeField] List<Objective> objectives = new();

        public IReadOnlyList<Objective> Objectives => objectives;

        /// <summary>Главная текущая цель (null — целей нет).</summary>
        public Objective Current(IGameStateReader state)
        {
            foreach (var objective in objectives)
                if (objective.IsOpen(state)) return objective;
            return null;
        }

#if UNITY_EDITOR
        void OnValidate()
        {
            if (StableId.EnsureUnique(objectives)) UnityEditor.EditorUtility.SetDirty(this);
        }
#endif
    }

    [Serializable]
    public sealed class Objective : IHasStableId
    {
        [SerializeField, HideInInspector] string id;
        [SerializeField] LocalizedString text;
        [SerializeReference, SubclassSelector, Tooltip("Когда цель появляется. Пусто — с самого начала.")]
        Condition activeWhen;
        [SerializeReference, SubclassSelector, Tooltip("Когда цель выполнена. Пусто — никогда (финальная цель).")]
        Condition completeWhen;
        [SerializeField, Tooltip("Подсказки по возрастанию: намёк → сильнее → прямой ответ")]
        List<LocalizedString> hints = new();

        public string Id { get => id; set => id = value; }
        public LocalizedString Text => text;
        public IReadOnlyList<LocalizedString> Hints => hints;

        public bool IsActive(IGameStateReader state) => Condition.IsMet(activeWhen, state);
        public bool IsComplete(IGameStateReader state) => completeWhen != null && completeWhen.Evaluate(state);
        public bool IsOpen(IGameStateReader state) => IsActive(state) && !IsComplete(state);
    }
}
