using System;
using System.Collections.Generic;
using HollowCreek.Core.Dialogue;
using HollowCreek.Core.Facts;
using HollowCreek.Core.Logic;
using UnityEngine;
using UnityEngine.Localization;

namespace HollowCreek.Core.Story
{
    /// <summary>
    /// Разгадка дела: игрок называет шерифу подозреваемого, его мотив и его ложь (улики из дневника).
    /// Неверный ответ объясняет, что не сходится, — проиграть нельзя.
    /// </summary>
    [CreateAssetMenu(menuName = "Hollow Creek/Case", fileName = "Case", order = 22)]
    public sealed class CaseDefinition : ScriptableObject
    {
        [SerializeReference, SubclassSelector, Tooltip("Когда можно выдвинуть обвинение")]
        Condition availableWhen;
        [SerializeField, Tooltip("Что показать, если обвинять ещё рано")]
        LocalizedString notReadyText;

        [Header("Подозреваемые")]
        [SerializeField] List<Suspect> suspects = new();
        [SerializeField] CharacterDefinition culprit;

        [Header("Улики, которые принимаются как ответ")]
        [SerializeField] List<FactDefinition> acceptedMotives = new();
        [SerializeField] List<FactDefinition> acceptedLies = new();
        [SerializeField] LocalizedString wrongMotiveText;
        [SerializeField] LocalizedString wrongLieText;

        [Header("Развязка")]
        [SerializeField] LocalizedString endingTitle;
        [SerializeField] LocalizedString endingText;
        [SerializeField, Tooltip("Факт «дело раскрыто»")]
        FactDefinition solvedFact;

        public Condition AvailableWhen => availableWhen;
        public LocalizedString NotReadyText => notReadyText;
        public IReadOnlyList<Suspect> Suspects => suspects;
        public CharacterDefinition Culprit => culprit;
        public IReadOnlyList<FactDefinition> AcceptedMotives => acceptedMotives;
        public IReadOnlyList<FactDefinition> AcceptedLies => acceptedLies;
        public LocalizedString WrongMotiveText => wrongMotiveText;
        public LocalizedString WrongLieText => wrongLieText;
        public LocalizedString EndingTitle => endingTitle;
        public LocalizedString EndingText => endingText;
        public FactDefinition SolvedFact => solvedFact;

        /// <summary>Проверка ответа. Возвращает, какая часть неверна (первая по порядку).</summary>
        public Verdict Judge(CharacterDefinition suspect, FactDefinition motive, FactDefinition lie)
        {
            if (suspect != culprit) return Verdict.WrongSuspect;
            if (!acceptedMotives.Contains(motive)) return Verdict.WrongMotive;
            if (!acceptedLies.Contains(lie)) return Verdict.WrongLie;
            return Verdict.Correct;
        }

        public Suspect FindSuspect(CharacterDefinition character) =>
            suspects.Find(s => s.Character == character);
    }

    public enum Verdict
    {
        Correct,
        WrongSuspect,
        WrongMotive,
        WrongLie,
    }

    [Serializable]
    public sealed class Suspect
    {
        [SerializeField] CharacterDefinition character;
        [SerializeField, Tooltip("Почему это не он — объяснение для неверного обвинения")]
        LocalizedString wrongReason;

        public CharacterDefinition Character => character;
        public LocalizedString WrongReason => wrongReason;
    }
}
