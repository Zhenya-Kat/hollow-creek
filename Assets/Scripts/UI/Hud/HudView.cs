using System;
using HollowCreek.Core.Facts;
using HollowCreek.Core.State;
using HollowCreek.Gameplay.Input;
using HollowCreek.Gameplay.Interaction;
using HollowCreek.Gameplay.Modals;
using HollowCreek.UI.Common;
using UnityEngine.UIElements;

namespace HollowCreek.UI.Hud
{
    /// <summary>Прицел, подсказка «ЛКМ — Осмотреть» и всплывающие уведомления о новых уликах.</summary>
    public sealed class HudView : IDisposable
    {
        const string CrosshairActiveClass = "crosshair--active";
        const string ToastClass = "toast";
        const string ToastHiddenClass = "toast--hidden";
        const long ToastLifetimeMs = 3500;
        const long ToastFadeMs = 400;

        readonly VisualElement crosshair;
        readonly Label prompt;
        readonly VisualElement toasts;
        readonly Interactor interactor;
        readonly ModalStack modals;
        readonly GameInput input;
        readonly IGameStateReader state;

        public HudView(VisualElement root, Interactor interactor, ModalStack modals, GameInput input,
            IGameStateReader state)
        {
            crosshair = root.Q("crosshair");
            prompt = root.Q<Label>("prompt");
            toasts = root.Q("toasts");
            this.interactor = interactor;
            this.modals = modals;
            this.input = input;
            this.state = state;

            interactor.FocusChanged += OnFocusChanged;
            modals.Changed += Refresh;
            input.UsingGamepadChanged += OnDeviceChanged;
            state.FactGranted += OnFactGranted;
            Refresh();
        }

        public void Dispose()
        {
            interactor.FocusChanged -= OnFocusChanged;
            modals.Changed -= Refresh;
            input.UsingGamepadChanged -= OnDeviceChanged;
            state.FactGranted -= OnFactGranted;
        }

        void OnFocusChanged(Interactable _) => Refresh();
        void OnDeviceChanged(bool _) => Refresh();

        void Refresh()
        {
            var inGameplay = modals.IsEmpty;
            crosshair.EnableInClassList(ScreenView.HiddenClass, !inGameplay);

            var target = inGameplay ? interactor.Current : null;
            crosshair.EnableInClassList(CrosshairActiveClass, target != null);
            prompt.text = target == null
                ? string.Empty
                : UIText.Format("prompt.format", UIText.KeyName(input.BindingName(input.Interact)), UIText.Get(target.Prompt));
        }

        void OnFactGranted(FactDefinition fact)
        {
            var text = fact switch
            {
                ClueDefinition clue => UIText.Format("toast.clue", UIText.Get(clue.Title)),
                ItemDefinition item => UIText.Format("toast.item", UIText.Get(item.Title)),
                _ => null,
            };
            if (text != null) ShowToast(text);
        }

        void ShowToast(string text)
        {
            var toast = new Label(text);
            toast.AddToClassList(ToastClass);
            toast.AddToClassList(ToastHiddenClass);
            toasts.Add(toast);
            // Класс снимается в следующем кадре, чтобы сработала анимация появления.
            toast.schedule.Execute(() => toast.RemoveFromClassList(ToastHiddenClass));
            toast.schedule.Execute(() => toast.AddToClassList(ToastHiddenClass)).StartingIn(ToastLifetimeMs);
            toast.schedule.Execute(toast.RemoveFromHierarchy).StartingIn(ToastLifetimeMs + ToastFadeMs);
        }
    }
}
