using System.Collections.Generic;
using HollowCreek.Core.Facts;
using HollowCreek.Core.Logic;
using UnityEngine;
using UnityEngine.Localization;

namespace HollowCreek.Core.Puzzles
{
    /// <summary>
    /// Общие настройки головоломки: название, пояснение, что нужно, чтобы начать,
    /// и что происходит после решения. Конкретные виды головоломок — наследники этого класса.
    /// </summary>
    public abstract class PuzzleDefinition : ScriptableObject
    {
        [Header("Текст")]
        [SerializeField] LocalizedString title;
        [SerializeField, Tooltip("Короткое пояснение, что делать")]
        LocalizedString instructions;

        [Header("Доступ")]
        [SerializeReference, SubclassSelector, Tooltip("Что нужно, чтобы начать (например, ключ). Пусто — ничего.")]
        Condition requires;
        [SerializeField, Tooltip("Что сказать, если начать пока нельзя")]
        LocalizedString lockedMessage;

        [Header("Решение")]
        [SerializeField, Tooltip("Факт «головоломка решена»: после него она не открывается повторно")]
        FactDefinition solvedFact;
        [SerializeReference, SubclassSelector, Tooltip("Что происходит после решения")]
        List<GameAction> onSolved = new();
        [SerializeField, Tooltip("Сообщение сразу после решения. Необязательно.")]
        LocalizedString solvedMessage;
        [SerializeField, Tooltip("Что сказать, если подойти к уже решённой головоломке")]
        LocalizedString revisitMessage;

        public LocalizedString Title => title;
        public LocalizedString Instructions => instructions;
        public Condition Requires => requires;
        public LocalizedString LockedMessage => lockedMessage;
        public FactDefinition SolvedFact => solvedFact;
        public IReadOnlyList<GameAction> OnSolved => onSolved;
        public LocalizedString SolvedMessage => solvedMessage;
        public LocalizedString RevisitMessage => revisitMessage;
    }
}
