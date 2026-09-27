using System;
using HollowCreek.Core;
using HollowCreek.Core.Dialogue;
using HollowCreek.Core.Logic;
using UnityEngine;

namespace HollowCreek.Gameplay.Dialogue
{
    [Serializable, SelectorLabel("Поговорить")]
    public sealed class TalkAction : GameAction
    {
        [SerializeField, Tooltip("С кем. Пусто — персонаж из компонента Npc на этом объекте.")]
        CharacterDefinition character;

        public TalkAction() { }
        public TalkAction(CharacterDefinition character) => this.character = character;

        public override void Execute(in ActionContext context)
        {
            var npc = context.Source != null ? context.Source.GetComponentInParent<Npc>() : null;
            var who = character != null ? character : npc != null ? npc.Character : null;
            if (who == null)
            {
                Debug.LogWarning("[Action] Не указан персонаж для разговора.", context.Source);
                return;
            }
            Services.Get<DialogueService>().Begin(who, npc);
        }
    }
}
