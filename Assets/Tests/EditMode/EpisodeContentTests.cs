using System.Collections.Generic;
using System.Linq;
using HollowCreek.Core.Facts;
using HollowCreek.Core.Story;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.Localization;

namespace HollowCreek.Tests
{
    /// <summary>
    /// Проверка данных всех эпизодов: нет битых ссылок, пустых текстов и фактов, забытых в списке эпизода.
    /// Ловит ошибки при правке содержимого в Inspector.
    /// </summary>
    public class EpisodeContentTests
    {
        static IEnumerable<EpisodeDefinition> Episodes() =>
            AssetDatabase.FindAssets("t:EpisodeDefinition")
                .Select(AssetDatabase.GUIDToAssetPath)
                .Select(AssetDatabase.LoadAssetAtPath<EpisodeDefinition>);

        [Test]
        public void AtLeastOneEpisodeExists() => Assert.IsNotEmpty(Episodes());

        [TestCaseSource(nameof(Episodes))]
        public void FactsHaveUniqueIds(EpisodeDefinition episode)
        {
            Assert.IsFalse(episode.Facts.Any(f => f == null), "пустой элемент в списке фактов");
            Assert.IsFalse(episode.Facts.Any(f => string.IsNullOrEmpty(f.Id)), "факт без Id");
            var duplicates = episode.Facts.GroupBy(f => f.Id).Where(g => g.Count() > 1).Select(g => g.Key).ToList();
            CollectionAssert.IsEmpty(duplicates, "повторяющиеся Id фактов");
        }

        [TestCaseSource(nameof(Episodes))]
        public void EveryReferencedFactIsListedInEpisode(EpisodeDefinition episode)
        {
            var listed = new HashSet<FactDefinition>(episode.Facts);
            var problems = new List<string>();
            foreach (var asset in ContentAssets(episode))
                Walk(asset, (property, path) =>
                {
                    if (property.propertyType == SerializedPropertyType.ObjectReference
                        && property.objectReferenceValue is FactDefinition fact && !listed.Contains(fact))
                        problems.Add($"{asset.name}: {path} → {fact.name} нет в списке фактов эпизода");
                });
            CollectionAssert.IsEmpty(problems);
        }

        [TestCaseSource(nameof(Episodes))]
        public void TextsAreNotEmpty(EpisodeDefinition episode)
        {
            var problems = new List<string>();
            foreach (var character in episode.Characters)
            {
                Require(problems, character.DisplayName, $"{character.name}: имя");
                Require(problems, character.Greeting, $"{character.name}: приветствие");
                Require(problems, character.DefaultReaction, $"{character.name}: реакция по умолчанию");
                foreach (var topic in character.Topics)
                {
                    Require(problems, topic.Question, $"{character.name}: вопрос {topic.Id}");
                    Require(problems, topic.Answer, $"{character.name}: ответ {topic.Id}");
                }
                foreach (var reaction in character.Reactions)
                {
                    Require(problems, reaction.Response, $"{character.name}: реакция {reaction.Id}");
                    if (reaction.Evidence == null) problems.Add($"{character.name}: реакция {reaction.Id} без улики");
                }
            }
            foreach (var objective in episode.Objectives.Objectives)
            {
                Require(problems, objective.Text, $"цель {objective.Id}");
                if (objective.Hints.Count != 3) problems.Add($"цель {objective.Id}: подсказок {objective.Hints.Count}, нужно 3");
            }
            foreach (var puzzle in episode.Puzzles)
            {
                if (puzzle == null) { problems.Add("пустой элемент в списке головоломок"); continue; }
                Require(problems, puzzle.Title, $"{puzzle.name}: название");
                if (puzzle.SolvedFact == null) problems.Add($"{puzzle.name}: нет факта решения");
            }
            foreach (var fact in episode.Facts.OfType<ClueDefinition>())
            {
                Require(problems, fact.Title, $"{fact.name}: название");
                Require(problems, fact.Description, $"{fact.name}: описание");
            }
            CollectionAssert.IsEmpty(problems);
        }

        [TestCaseSource(nameof(Episodes))]
        public void CaseIsConsistent(EpisodeDefinition episode)
        {
            var c = episode.Case;
            Assert.IsNotNull(c, "нет разгадки дела");
            Assert.IsNotNull(c.Culprit, "не указан убийца");
            Assert.IsTrue(c.Suspects.Any(s => s.Character == c.Culprit), "убийцы нет среди подозреваемых");
            Assert.IsNotEmpty(c.AcceptedMotives);
            Assert.IsNotEmpty(c.AcceptedLies);
            Assert.IsNotNull(c.SolvedFact);
            foreach (var suspect in c.Suspects.Where(s => s.Character != c.Culprit))
                Assert.IsFalse(suspect.WrongReason.IsEmpty, $"нет объяснения для {suspect.Character.name}");
        }

        [TestCaseSource(nameof(Episodes))]
        public void ActionListsHaveNoEmptySlots(EpisodeDefinition episode)
        {
            var problems = new List<string>();
            foreach (var asset in ContentAssets(episode))
                Walk(asset, (property, path) =>
                {
                    if (property.propertyType == SerializedPropertyType.ManagedReference
                        // Сам элемент списка (путь кончается на «]»), а не необязательное поле внутри элемента.
                        && property.managedReferenceValue == null && path.EndsWith("]"))
                        problems.Add($"{asset.name}: пустой элемент {path}");
                });
            CollectionAssert.IsEmpty(problems);
        }

        static IEnumerable<Object> ContentAssets(EpisodeDefinition episode)
        {
            yield return episode;
            foreach (var character in episode.Characters) yield return character;
            foreach (var puzzle in episode.Puzzles) yield return puzzle;
            if (episode.Objectives) yield return episode.Objectives;
            if (episode.Rules) yield return episode.Rules;
            if (episode.Case) yield return episode.Case;
        }

        static void Walk(Object asset, System.Action<SerializedProperty, string> visit)
        {
            var property = new SerializedObject(asset).GetIterator();
            while (property.Next(true)) visit(property, property.propertyPath);
        }

        static void Require(List<string> problems, LocalizedString text, string what)
        {
            if (text == null || text.IsEmpty) problems.Add(what + " — пусто");
        }
    }
}
