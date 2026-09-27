using System.Threading.Tasks;
using HollowCreek.Gameplay.Menus;
using HollowCreek.Gameplay.Messages;
using HollowCreek.Gameplay.Modals;
using HollowCreek.UI.Common;
using UnityEngine.Localization;
using UnityEngine.UIElements;

namespace HollowCreek.UI.Screens
{
    /// <summary>
    /// Главное меню поверх ночной улицы: продолжить, новая игра (с подтверждением, если есть сохранение),
    /// настройки, об игре, выход.
    /// </summary>
    public sealed class MainMenuScreen : ScreenView, IMainMenu
    {
        readonly ConfirmScreen confirm;
        readonly SettingsScreen settings;
        readonly IMessagePresenter messages;
        readonly PauseService pause;
        readonly Button continueButton;
        readonly Button newButton;
        TaskCompletionSource<MainMenuChoice> choice;
        bool canContinue;

        public MainMenuScreen(VisualElement root, ModalStack modals, ConfirmScreen confirm, SettingsScreen settings,
            IMessagePresenter messages, PauseService pause) : base(root, modals)
        {
            this.confirm = confirm;
            this.settings = settings;
            this.messages = messages;
            this.pause = pause;
            continueButton = root.Q<Button>("menu-continue");
            newButton = root.Q<Button>("menu-new");
            continueButton.clicked += () => Finish(MainMenuChoice.Continue);
            newButton.clicked += OnNewGame;
            root.Q<Button>("menu-settings").clicked += settings.Open;
            root.Q<Button>("menu-credits").clicked += () =>
                messages.Show(new LocalizedString(UIText.Table, "credits.title"), new LocalizedString(UIText.Table, "credits.text"));
            root.Q<Button>("menu-quit").clicked += () =>
                confirm.Ask("pause.quit.confirm.title", "menu.quit.confirm.text", "confirm.quit", yes => { if (yes) pause.QuitGame(); });
        }

        public Task<MainMenuChoice> ShowAsync(bool canContinueGame)
        {
            canContinue = canContinueGame;
            choice = new TaskCompletionSource<MainMenuChoice>();
            continueButton.EnableInClassList(HiddenClass, !canContinue);
            Open();
            (canContinue ? continueButton : newButton).Focus();
            return choice.Task;
        }

        protected override Core.Audio.SoundCue OpenSound(Core.Audio.AudioCues cues) => null;

        // В главном меню Esc ничего не делает.
        public override void OnBack() { }

        void OnNewGame()
        {
            if (!canContinue)
            {
                Finish(MainMenuChoice.NewGame);
                return;
            }
            confirm.Ask("menu.new.confirm.title", "menu.new.confirm.text", "menu.new.confirm.yes",
                yes => { if (yes) Finish(MainMenuChoice.NewGame); });
        }

        void Finish(MainMenuChoice result)
        {
            if (choice == null) return;
            var pending = choice;
            choice = null;
            Close();
            pending.TrySetResult(result);
        }
    }
}
