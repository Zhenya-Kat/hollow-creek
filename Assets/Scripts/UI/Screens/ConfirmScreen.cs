using System;
using HollowCreek.Gameplay.Modals;
using HollowCreek.UI.Common;
using UnityEngine.UIElements;

namespace HollowCreek.UI.Screens
{
    /// <summary>Окно «Вы уверены?» с двумя кнопками. Esc — отказ.</summary>
    public sealed class ConfirmScreen : ScreenView
    {
        readonly Label title;
        readonly Label text;
        readonly Button yesButton;
        readonly Button noButton;
        Action<bool> callback;

        public ConfirmScreen(VisualElement root, ModalStack modals) : base(root, modals)
        {
            title = root.Q<Label>("confirm-title");
            text = root.Q<Label>("confirm-text");
            yesButton = root.Q<Button>("confirm-yes");
            noButton = root.Q<Button>("confirm-no");
            yesButton.clicked += () => Answer(true);
            noButton.clicked += () => Answer(false);
        }

        public void Ask(string titleKey, string textKey, string yesKey, Action<bool> result)
        {
            callback = result;
            title.text = UIText.Get(titleKey);
            text.text = UIText.Get(textKey);
            yesButton.text = UIText.Get(yesKey);
            noButton.text = UIText.Get("common.cancel");
            Open();
            noButton.Focus();
        }

        public override void OnBack() => Answer(false);

        void Answer(bool yes)
        {
            var result = callback;
            callback = null;
            Close();
            result?.Invoke(yes);
        }
    }
}
