using System;
using System.Collections.Generic;
using HollowCreek.Core.Logic;
using HollowCreek.Core.State;
using UnityEngine;
using UnityEngine.Localization;

namespace HollowCreek.Gameplay.Interaction
{
    /// <summary>
    /// Объект, с которым можно взаимодействовать: навести прицел и нажать ЛКМ.
    /// Что произойдёт — задаётся списком действий в Inspector. Нужен коллайдер на объекте или его детях.
    /// </summary>
    public sealed class Interactable : MonoBehaviour
    {
        [SerializeField, Tooltip("Подсказка у прицела: «Осмотреть», «Поговорить»…")]
        LocalizedString prompt;

        [SerializeReference, SubclassSelector, Tooltip("Когда объект доступен. Пусто — всегда.")]
        Condition availableWhen;

        [SerializeReference, SubclassSelector, Tooltip("Что происходит при взаимодействии, по порядку")]
        List<GameAction> actions = new();

        public LocalizedString Prompt => prompt;

        /// <summary>С объектом только что взаимодействовали.</summary>
        public event Action Interacted;

        public bool IsAvailable(IGameStateReader state) => Condition.IsMet(availableWhen, state);

        public void Interact()
        {
            GameAction.RunAll(actions, new ActionContext(gameObject));
            Interacted?.Invoke();
        }
    }
}
