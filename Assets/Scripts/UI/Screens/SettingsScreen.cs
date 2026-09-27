using System.Linq;
using HollowCreek.Gameplay.Modals;
using HollowCreek.Gameplay.Settings;
using HollowCreek.UI.Common;
using UnityEngine;
using UnityEngine.UIElements;

namespace HollowCreek.UI.Screens
{
    /// <summary>Настройки: громкость, управление, изображение. Изменения применяются сразу, сохраняются при закрытии.</summary>
    public sealed class SettingsScreen : ScreenView
    {
        readonly SettingsService service;
        readonly Slider master, effects, ui, ambience, sensitivity;
        readonly SliderInt fov;
        readonly Toggle invert, fullscreen;
        readonly DropdownField quality;
        readonly Button closeButton;
        bool refreshing;

        public SettingsScreen(VisualElement root, ModalStack modals, SettingsService service) : base(root, modals)
        {
            this.service = service;
            master = Bind<Slider>(root, "set-master", v => service.Current.masterVolume = v);
            effects = Bind<Slider>(root, "set-effects", v => service.Current.effectsVolume = v);
            ui = Bind<Slider>(root, "set-interface", v => service.Current.interfaceVolume = v);
            ambience = Bind<Slider>(root, "set-ambience", v => service.Current.ambienceVolume = v);
            sensitivity = Bind<Slider>(root, "set-sensitivity", v => service.Current.mouseSensitivity = v);
            fov = root.Q<SliderInt>("set-fov");
            fov.RegisterValueChangedCallback(e => Change(() => service.Current.fieldOfView = e.newValue));
            invert = root.Q<Toggle>("set-invert");
            invert.RegisterValueChangedCallback(e => Change(() => service.Current.invertY = e.newValue));
            fullscreen = root.Q<Toggle>("set-fullscreen");
            fullscreen.RegisterValueChangedCallback(e => Change(() => service.Current.fullscreen = e.newValue));
            quality = root.Q<DropdownField>("set-quality");
            quality.choices = QualitySettings.names.Select(QualityLabel).ToList();
            quality.RegisterValueChangedCallback(_ => Change(() => service.Current.quality = quality.index));
            root.Q<Button>("set-reset").clicked += () =>
            {
                service.ResetToDefaults();
                Refresh();
            };
            closeButton = root.Q<Button>("set-close");
            closeButton.clicked += Close;
        }

        protected override void OnOpened()
        {
            Refresh();
            master.Focus();
        }

        protected override void OnClosed() => service.Save();

        void Refresh()
        {
            refreshing = true;
            var s = service.Current;
            master.value = s.masterVolume;
            effects.value = s.effectsVolume;
            ui.value = s.interfaceVolume;
            ambience.value = s.ambienceVolume;
            sensitivity.value = s.mouseSensitivity;
            fov.value = Mathf.RoundToInt(s.fieldOfView);
            invert.value = s.invertY;
            fullscreen.value = s.fullscreen;
            quality.index = Mathf.Clamp(s.quality, 0, QualitySettings.names.Length - 1);
            refreshing = false;
        }

        T Bind<T>(VisualElement root, string name, System.Action<float> set) where T : BaseField<float>
        {
            var field = root.Q<T>(name);
            field.RegisterValueChangedCallback(e => Change(() => set(e.newValue)));
            return field;
        }

        void Change(System.Action apply)
        {
            if (refreshing) return;
            apply();
            service.Apply();
        }

        static string QualityLabel(string qualityName)
        {
            return UIText.Find("quality." + qualityName) ?? qualityName;
        }
    }
}
