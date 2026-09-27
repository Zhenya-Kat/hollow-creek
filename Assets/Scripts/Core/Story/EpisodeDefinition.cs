using System.Collections.Generic;
using HollowCreek.Core.Data;
using HollowCreek.Core.Dialogue;
using HollowCreek.Core.Facts;
using HollowCreek.Core.Puzzles;
using UnityEngine;
using UnityEngine.Localization;

namespace HollowCreek.Core.Story
{
    /// <summary>
    /// Эпизод целиком: вступление, все факты (по ним загружаются сохранения), персонажи, цели,
    /// сюжетные правила и разгадка. Чтобы сделать новый эпизод, достаточно нового такого ассета.
    /// </summary>
    [CreateAssetMenu(menuName = "Hollow Creek/Episode", fileName = "Episode", order = 30)]
    public sealed class EpisodeDefinition : GameDefinition
    {
        [Header("Вступление")]
        [SerializeField] LocalizedString title;
        [SerializeField] LocalizedString introText;
        [SerializeField] LocationDefinition startLocation;

        [Header("Содержимое")]
        [SerializeField, Tooltip("Все факты эпизода: улики, предметы, отметки прогресса")]
        List<FactDefinition> facts = new();
        [SerializeField] List<CharacterDefinition> characters = new();
        [SerializeField, Tooltip("Головоломки эпизода (для проверки данных и обзора)")]
        List<PuzzleDefinition> puzzles = new();
        [SerializeField] ObjectiveSet objectives;
        [SerializeField] StoryRuleSet rules;
        [SerializeField] CaseDefinition caseDefinition;

        public LocalizedString Title => title;
        public LocalizedString IntroText => introText;
        public LocationDefinition StartLocation => startLocation;
        public IReadOnlyList<FactDefinition> Facts => facts;
        public IReadOnlyList<CharacterDefinition> Characters => characters;
        public IReadOnlyList<PuzzleDefinition> Puzzles => puzzles;
        public ObjectiveSet Objectives => objectives;
        public StoryRuleSet Rules => rules;
        public CaseDefinition Case => caseDefinition;

        public FactDefinition FindFact(string id) =>
            string.IsNullOrEmpty(id) ? null : facts.Find(f => f != null && f.Id == id);
    }
}
