using System;
using HollowCreek.Gameplay.Input;
using HollowCreek.Gameplay.Inspection;
using HollowCreek.UI.Common;
using UnityEngine.UIElements;

namespace HollowCreek.UI.Hud
{
    /// <summary>Панель с названием и описанием предмета во время осмотра крупным планом.</summary>
    public sealed class InspectionOverlay : IDisposable
    {
        readonly VisualElement root;
        readonly Label title;
        readonly Label description;
        readonly Label hint;
        readonly InspectionController inspection;
        readonly GameInput input;

        public InspectionOverlay(VisualElement root, InspectionController inspection, GameInput input)
        {
            this.root = root;
            this.inspection = inspection;
            this.input = input;
            title = root.Q<Label>("title");
            description = root.Q<Label>("description");
            hint = root.Q<Label>("hint");
            root.AddToClassList(ScreenView.HiddenClass);

            inspection.Started += OnStarted;
            inspection.Ended += OnEnded;
            input.UsingGamepadChanged += OnDeviceChanged;
        }

        public void Dispose()
        {
            inspection.Started -= OnStarted;
            inspection.Ended -= OnEnded;
            input.UsingGamepadChanged -= OnDeviceChanged;
        }

        void OnStarted(Inspectable target)
        {
            title.text = UIText.Get(target.Title);
            description.text = UIText.Get(target.Description);
            description.EnableInClassList(ScreenView.HiddenClass, string.IsNullOrEmpty(description.text));
            UpdateHint();
            root.RemoveFromClassList(ScreenView.HiddenClass);
        }

        void OnEnded(Inspectable _) => root.AddToClassList(ScreenView.HiddenClass);

        void OnDeviceChanged(bool _) => UpdateHint();

        void UpdateHint() =>
            hint.text = UIText.Get(input.UsingGamepad ? "inspect.hint.gamepad" : "inspect.hint.km");
    }
}
