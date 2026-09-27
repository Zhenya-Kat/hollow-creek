using System;
using System.Collections.Generic;
using HollowCreek.Core.Data;
using HollowCreek.Core.Facts;
using HollowCreek.Core.Logic;
using UnityEngine;
using UnityEngine.Localization;

namespace HollowCreek.Core.Dialogue
{
    /// <summary>
    /// Персонаж, с которым можно поговорить: вступительная реплика, темы для вопросов
    /// и реакции на предъявленные улики.
    /// </summary>
    [CreateAssetMenu(menuName = "Hollow Creek/Character", fileName = "Character", order = 10)]
    public sealed class CharacterDefinition : GameDefinition
    {
        [SerializeField] LocalizedString displayName;
        [SerializeField, Tooltip("Кто это: «Сестра Элис», «Мэр Холлоу-Крик»…")]
        LocalizedString role;
        [SerializeField, Tooltip("Что персонаж говорит, когда к нему подходят")]
        LocalizedString greeting;
        [SerializeField, Tooltip("Факт «игрок говорил с персонажем». Выдаётся при первом разговоре.")]
        FactDefinition metFact;

        [SerializeField] List<DialogueTopic> topics = new();
        [SerializeField, Tooltip("Как персонаж реагирует на конкретные улики")]
        List<EvidenceReaction> reactions = new();
        [SerializeField, Tooltip("Ответ на улику, для которой нет особой реакции")]
        LocalizedString defaultReaction;

        public LocalizedString DisplayName => displayName;
        public LocalizedString Role => role;
        public LocalizedString Greeting => greeting;
        public FactDefinition MetFact => metFact;
        public IReadOnlyList<DialogueTopic> Topics => topics;
        public IReadOnlyList<EvidenceReaction> Reactions => reactions;
        public LocalizedString DefaultReaction => defaultReaction;

#if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate();
            if (StableId.EnsureUnique(topics) | StableId.EnsureUnique(reactions))
                UnityEditor.EditorUtility.SetDirty(this);
        }
#endif
    }

    /// <summary>Вопрос, который игрок может задать персонажу.</summary>
    [Serializable]
    public sealed class DialogueTopic : IHasStableId
    {
        [SerializeField, HideInInspector] string id;
        [SerializeField] LocalizedString question;
        [SerializeField] LocalizedString answer;
        [SerializeReference, SubclassSelector, Tooltip("Когда вопрос появляется. Пусто — сразу.")]
        Condition availableWhen;
        [SerializeReference, SubclassSelector, Tooltip("Что происходит после ответа (например, выдать улику)")]
        List<GameAction> onAsked = new();

        public string Id { get => id; set => id = value; }
        public LocalizedString Question => question;
        public LocalizedString Answer => answer;
        public Condition AvailableWhen => availableWhen;
        public IReadOnlyList<GameAction> OnAsked => onAsked;
    }

    /// <summary>Реакция персонажа на предъявленную улику.</summary>
    [Serializable]
    public sealed class EvidenceReaction : IHasStableId
    {
        [SerializeField, HideInInspector] string id;
        [SerializeField, Tooltip("Какую улику (или предмет) нужно предъявить")]
        FactDefinition evidence;
        [SerializeField] LocalizedString response;
        [SerializeReference, SubclassSelector, Tooltip("Дополнительное условие. Пусто — достаточно самой улики.")]
        Condition availableWhen;
        [SerializeReference, SubclassSelector]
        List<GameAction> onPresented = new();

        public string Id { get => id; set => id = value; }
        public FactDefinition Evidence => evidence;
        public LocalizedString Response => response;
        public Condition AvailableWhen => availableWhen;
        public IReadOnlyList<GameAction> OnPresented => onPresented;
    }
}
