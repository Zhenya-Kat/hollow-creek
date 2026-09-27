using System.Collections.Generic;
using HollowCreek.Gameplay.Messages;
using HollowCreek.Gameplay.Modals;
using HollowCreek.UI.Common;
using UnityEngine.Localization;
using UnityEngine.UIElements;

namespace HollowCreek.UI.Screens
{
    /// <summary>Окно с заголовком и текстом. Если сообщений несколько — показываются по очереди.</summary>
    public sealed class MessageScreen : ScreenView, IMessagePresenter
    {
        readonly Queue<(LocalizedString title, LocalizedString body)> queue = new();
        readonly Label title;
        readonly Label body;
        readonly ScrollView scroll;
        readonly Button continueButton;

        public MessageScreen(VisualElement root, ModalStack modals) : base(root, modals)
        {
            title = root.Q<Label>("title");
            body = root.Q<Label>("body");
            scroll = root.Q<ScrollView>("body-scroll");
            continueButton = root.Q<Button>("continue");
            continueButton.clicked += ShowNextOrClose;
            modals.Changed += TryShowNext;
        }

        public void Show(LocalizedString messageTitle, LocalizedString messageBody)
        {
            queue.Enqueue((messageTitle, messageBody));
            TryShowNext();
        }

        /// <summary>Показать следующее сообщение, если сейчас не открыт экран, который нельзя прерывать.</summary>
        void TryShowNext()
        {
            if (IsOpen || queue.Count == 0) return;
            if (Modals.Top != null && Modals.Top.DefersMessages) return;
            ShowNextOrClose();
        }

        public override void OnBack() => ShowNextOrClose();

        void ShowNextOrClose()
        {
            if (queue.Count == 0)
            {
                Close();
                return;
            }

            var (nextTitle, nextBody) = queue.Dequeue();
            title.text = UIText.Get(nextTitle);
            title.EnableInClassList(HiddenClass, string.IsNullOrEmpty(title.text));
            body.text = UIText.Get(nextBody);
            scroll.scrollOffset = UnityEngine.Vector2.zero;
            Open();
            continueButton.Focus();
        }

        public override void Dispose()
        {
            continueButton.clicked -= ShowNextOrClose;
            Modals.Changed -= TryShowNext;
            queue.Clear();
            base.Dispose();
        }
    }
}
