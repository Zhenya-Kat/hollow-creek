using System.Collections.Generic;
using HollowCreek.Core.Facts;
using HollowCreek.Core.Logic;
using HollowCreek.Core.State;
using UnityEngine;
using UnityEngine.Localization;

namespace HollowCreek.Core.Dialogue
{
    /// <summary>
    /// Один разговор с персонажем: какие вопросы доступны, что он отвечает и как реагирует на улики.
    /// Только логика — показывает разговор слой интерфейса.
    /// </summary>
    public sealed class Conversation
    {
        readonly IGameStateReader state;
        readonly DialogueLog log;

        public Conversation(CharacterDefinition character, IGameStateReader state, DialogueLog log,
            GameObject source, bool firstMeeting)
        {
            Character = character;
            this.state = state;
            this.log = log;
            Source = source;
            FirstMeeting = firstMeeting;
        }

        public CharacterDefinition Character { get; }

        /// <summary>Объект персонажа в сцене (может быть null).</summary>
        public GameObject Source { get; }

        public bool FirstMeeting { get; }

        public LocalizedString Greeting => FirstMeeting ? Character.Greeting : Character.ReturnGreeting;

        public IEnumerable<DialogueTopic> AvailableTopics
        {
            get
            {
                foreach (var topic in Character.Topics)
                    if (Condition.IsMet(topic.AvailableWhen, state)) yield return topic;
            }
        }

        public bool WasAsked(DialogueTopic topic) => log.WasAsked(Character, topic);

        /// <summary>Задать вопрос: отмечает его заданным, выполняет действия и возвращает ответ.</summary>
        public LocalizedString Ask(DialogueTopic topic)
        {
            log.MarkAsked(Character, topic);
            GameAction.RunAll(topic.OnAsked, new ActionContext(Source));
            return topic.Answer;
        }

        /// <summary>Реакция на улику, если у персонажа есть особая (с учётом условия), иначе null.</summary>
        public EvidenceReaction FindReaction(FactDefinition evidence)
        {
            foreach (var reaction in Character.Reactions)
                if (reaction.Evidence == evidence && Condition.IsMet(reaction.AvailableWhen, state))
                    return reaction;
            return null;
        }

        /// <summary>Предъявить улику: особая реакция с её действиями или ответ по умолчанию.</summary>
        public LocalizedString Present(FactDefinition evidence)
        {
            var reaction = FindReaction(evidence);
            if (reaction == null) return Character.DefaultReaction;
            GameAction.RunAll(reaction.OnPresented, new ActionContext(Source));
            return reaction.Response;
        }
    }
}
