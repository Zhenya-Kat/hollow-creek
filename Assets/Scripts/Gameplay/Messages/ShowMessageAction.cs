using System;
using HollowCreek.Core;
using HollowCreek.Core.Logic;
using UnityEngine;
using UnityEngine.Localization;

namespace HollowCreek.Gameplay.Messages
{
    /// <summary>
    /// Показывает окно с текстом. Сам экран живёт в слое интерфейса — игровой код знает только этот интерфейс.
    /// </summary>
    public interface IMessagePresenter
    {
        void Show(LocalizedString title, LocalizedString body);
    }

    [Serializable, SelectorLabel("Показать сообщение")]
    public sealed class ShowMessageAction : GameAction
    {
        [SerializeField] LocalizedString title;
        [SerializeField] LocalizedString body;

        public ShowMessageAction() { }

        public ShowMessageAction(LocalizedString title, LocalizedString body)
        {
            this.title = title;
            this.body = body;
        }

        public override void Execute(in ActionContext context) =>
            Services.Get<IMessagePresenter>().Show(title, body);
    }
}
