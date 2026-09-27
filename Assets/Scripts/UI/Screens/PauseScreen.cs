using HollowCreek.Gameplay.Menus;
using HollowCreek.Gameplay.Modals;
using HollowCreek.UI.Common;
using UnityEngine.UIElements;

namespace HollowCreek.UI.Screens
{
    /// <summary>Меню паузы. Открывается по Esc во время игры (и при потере фокуса окном игры).</summary>
    public sealed class PauseScreen : ScreenView
    {
        readonly PauseService pause;
        readonly Button resumeButton;

        public PauseScreen(VisualElement root, ModalStack modals, PauseService pause, SettingsScreen settings,
            ConfirmScreen confirm) : base(root, modals)
        {
            this.pause = pause;
            resumeButton = root.Q<Button>("pause-resume");
            resumeButton.clicked += Close;
            root.Q<Button>("pause-settings").clicked += settings.Open;
            root.Q<Button>("pause-menu").clicked += () =>
                confirm.Ask("pause.menu.confirm.title", "pause.saved.text", "confirm.quit", yes => { if (yes) pause.QuitToMainMenu(); });
            root.Q<Button>("pause-quit").clicked += () =>
                confirm.Ask("pause.quit.confirm.title", "pause.saved.text", "confirm.quit", yes => { if (yes) pause.QuitGame(); });
            pause.Paused += OnPaused;
        }

        public override void Dispose()
        {
            pause.Paused -= OnPaused;
            base.Dispose();
        }

        void OnPaused()
        {
            Open();
            resumeButton.Focus();
        }

        protected override void OnClosed() => pause.Resume();
    }
}
