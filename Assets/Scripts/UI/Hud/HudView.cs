using System;
using HollowCreek.Core.Facts;
using HollowCreek.Core.State;
using HollowCreek.Core.Story;
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
        const string ObjectiveNewClass = "objective--new";

        readonly VisualElement crosshair;
        readonly Label prompt;
        readonly VisualElement toasts;
        readonly Interactor interactor;
        readonly ModalStack modals;
        readonly GameInput input;
        readonly IGameStateReader state;

        readonly VisualElement objectivePanel;
        readonly Label objectiveText;
        readonly ObjectiveTracker objectives;
        Objective shownObjective;

        public HudView(VisualElement root, Interactor interactor, ModalStack modals, GameInput input,
            IGameStateReader state, ObjectiveTracker objectives)
        {
            crosshair = root.Q("crosshair");
            prompt = root.Q<Label>("prompt");
            toasts = root.Q("toasts");
            objectivePanel = root.Q("objective");
            objectiveText = root.Q<Label>("objective-text");
            this.interactor = interactor;
            this.modals = modals;
            this.input = input;
            this.state = state;
            this.objectives = objectives;

            interactor.FocusChanged += OnFocusChanged;
            modals.Changed += Refresh;
            input.UsingGamepadChanged += OnDeviceChanged;
            state.FactGranted += OnFactGranted;
            objectives.Changed += RefreshObjective;
            Refresh();
            RefreshObjective();
        }

        public void Dispose()
        {
            interactor.FocusChanged -= OnFocusChanged;
            modals.Changed -= Refresh;
            input.UsingGamepadChanged -= OnDeviceChanged;
            state.FactGranted -= OnFactGranted;
            objectives.Changed -= RefreshObjective;
        }

        void RefreshObjective()
        {
            var current = objectives.Current;
            objectivePanel.EnableInClassList(ScreenView.HiddenClass, current == null);
            if (current == null || current == shownObjective) return;
            shownObjective = current;
            objectiveText.text = UIText.Get(current.Text);
            // Новая цель коротко подсвечивается.
            objectivePanel.AddToClassList(ObjectiveNewClass);
            objectivePanel.schedule.Execute(() => objectivePanel.RemoveFromClassList(ObjectiveNewClass)).StartingIn(1500);
        }

        void OnFocusChanged(Interactable _) => Refresh();
        void OnDeviceChanged(bool _) => Refresh();

        void Refresh()
        {
            var inGameplay = modals.IsEmpty;
            objectivePanel.EnableInClassList("objective--dimmed", !inGameplay);
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
            if (text == null) return;
            ShowToast(text);
            UiAudio.Play(c => fact is ClueDefinition ? c.clueFound : c.itemFound);
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
