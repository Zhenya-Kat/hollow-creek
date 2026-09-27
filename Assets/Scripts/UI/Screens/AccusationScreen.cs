using System.Collections.Generic;
using HollowCreek.Core.Dialogue;
using HollowCreek.Core.Facts;
using HollowCreek.Core.Story;
using HollowCreek.Gameplay.Messages;
using HollowCreek.Gameplay.Modals;
using HollowCreek.UI.Common;
using UnityEngine.UIElements;

namespace HollowCreek.UI.Screens
{
    /// <summary>
    /// Обвинение: игрок выбирает подозреваемого, улику-мотив и улику, разоблачающую ложь.
    /// Неверный ответ объясняет, что не сходится; верный — закрывает дело и показывает развязку.
    /// </summary>
    public sealed class AccusationScreen : ScreenView
    {
        const string SelectedClass = "accusation__suspect--selected";

        readonly CaseService caseService;
        readonly EvidencePickerScreen picker;
        readonly IMessagePresenter messages;
        readonly VisualElement suspectsRow;
        readonly Button motiveButton;
        readonly Button lieButton;
        readonly Label status;
        readonly Button submitButton;
        readonly Button cancelButton;
        readonly Dictionary<CharacterDefinition, Button> suspectButtons = new();
        CharacterDefinition suspect;
        FactDefinition motive;
        FactDefinition lie;

        public AccusationScreen(VisualElement root, ModalStack modals, CaseService caseService,
            EvidencePickerScreen picker, IMessagePresenter messages) : base(root, modals)
        {
            this.caseService = caseService;
            this.picker = picker;
            this.messages = messages;
            suspectsRow = root.Q("suspects");
            motiveButton = root.Q<Button>("motive");
            lieButton = root.Q<Button>("lie");
            status = root.Q<Label>("accuse-status");
            submitButton = root.Q<Button>("accuse-submit");
            cancelButton = root.Q<Button>("accuse-cancel");

            motiveButton.clicked += () => picker.Pick("accuse.pick.motive", "accuse.pick.confirm", false, fact => { motive = fact; Refresh(); });
            lieButton.clicked += () => picker.Pick("accuse.pick.lie", "accuse.pick.confirm", false, fact => { lie = fact; Refresh(); });
            submitButton.clicked += Submit;
            cancelButton.clicked += Close;
        }

        protected override void OnOpened()
        {
            suspectsRow.Clear();
            suspectButtons.Clear();
            foreach (var s in caseService.Definition.Suspects)
            {
                var character = s.Character;
                var button = new Button { text = UIText.Get(character.DisplayName) };
                button.AddToClassList("button");
                button.AddToClassList("accusation__suspect");
                button.clicked += () => { suspect = character; Refresh(); };
                suspectButtons[character] = button;
                suspectsRow.Add(button);
            }
            status.text = string.Empty;
            Refresh();
            (suspect != null ? suspectButtons[suspect] : suspectsRow.Q<Button>())?.Focus();
        }

        void Refresh()
        {
            foreach (var pair in suspectButtons) pair.Value.EnableInClassList(SelectedClass, pair.Key == suspect);
            motiveButton.text = Title(motive);
            lieButton.text = Title(lie);
            submitButton.SetEnabled(suspect != null && motive != null && lie != null);
        }

        void Submit()
        {
            if (suspect == null || motive == null || lie == null)
            {
                status.text = UIText.Get("accuse.incomplete");
                return;
            }

            var verdict = caseService.Accuse(suspect, motive, lie);
            if (verdict == Verdict.Correct)
            {
                Close();
                var definition = caseService.Definition;
                messages.Show(definition.EndingTitle, definition.EndingText);
                return;
            }
            UiAudio.Play(c => c.wrong);
            status.text = UIText.Get(caseService.Explain(verdict, suspect));
        }

        static string Title(FactDefinition fact) =>
            fact is ClueDefinition clue ? UIText.Get(clue.Title) : UIText.Get("accuse.choose");
    }
}
