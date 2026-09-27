using System;
using HollowCreek.Core.Facts;
using HollowCreek.Core.State;
using HollowCreek.Gameplay.Modals;
using HollowCreek.UI.Common;
using UnityEngine.UIElements;

namespace HollowCreek.UI.Screens
{
    /// <summary>
    /// Выбор улики из дневника: чтобы предъявить её собеседнику, а позже — для обвинения.
    /// Открывается поверх другого экрана и возвращает выбор через колбэк.
    /// </summary>
    public sealed class EvidencePickerScreen : ScreenView
    {
        readonly IGameStateReader state;
        readonly FactListView facts;
        readonly Label title;
        readonly Button confirmButton;
        readonly Button cancelButton;
        Action<FactDefinition> onPicked;

        public EvidencePickerScreen(VisualElement root, ModalStack modals, IGameStateReader state) : base(root, modals)
        {
            this.state = state;
            facts = new FactListView(root);
            title = root.Q<Label>("picker-title");
            confirmButton = root.Q<Button>("confirm");
            cancelButton = root.Q<Button>("cancel");
            confirmButton.clicked += Confirm;
            cancelButton.clicked += Close;
            // Enter/«A» на записи подтверждает именно её, а не ранее выделенную.
            facts.Submitted += fact =>
            {
                facts.Select(fact);
                Confirm();
            };
        }

        /// <summary>
        /// Открыть выбор. <paramref name="picked"/> вызывается только если игрок что-то выбрал.
        /// <paramref name="confirmKey"/> — подпись кнопки подтверждения («Предъявить», «Выбрать»).
        /// </summary>
        public void Pick(string titleKey, string confirmKey, bool includeItems, Action<FactDefinition> picked)
        {
            onPicked = picked;
            title.text = UIText.Get(titleKey);
            confirmButton.text = UIText.Get(confirmKey);
            Open();
            var any = facts.Build(state, includeItems);
            confirmButton.SetEnabled(any);
            if (any) facts.FocusSelection();
            else cancelButton.Focus();
        }

        void Confirm()
        {
            var fact = facts.Selected;
            if (fact == null) return;
            var callback = onPicked;
            onPicked = null;
            Close();
            callback?.Invoke(fact);
        }

        protected override void OnClosed() => onPicked = null;

        public override void Dispose()
        {
            confirmButton.clicked -= Confirm;
            cancelButton.clicked -= Close;
            base.Dispose();
        }
    }
}
