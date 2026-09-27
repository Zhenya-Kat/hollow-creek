using HollowCreek.Core.Dialogue;
using HollowCreek.Core.Facts;
using HollowCreek.Gameplay.Dialogue;
using HollowCreek.Gameplay.Modals;
using HollowCreek.UI.Common;
using UnityEngine.Localization;
using UnityEngine.UIElements;

namespace HollowCreek.UI.Screens
{
    /// <summary>
    /// Экран разговора: реплика персонажа, список вопросов (новые отмечены, заданные приглушены),
    /// кнопки «Предъявить улику» и «Уйти».
    /// </summary>
    public sealed class DialogueScreen : ScreenView
    {
        const string ChoiceClass = "dialogue__choice";
        const string AskedClass = "dialogue__choice--asked";

        readonly DialogueService dialogue;
        readonly EvidencePickerScreen picker;
        readonly Label nameLabel;
        readonly Label roleLabel;
        readonly Typewriter speech;
        readonly VisualElement choices;
        readonly Button presentButton;
        readonly Button leaveButton;
        Conversation conversation;

        public DialogueScreen(VisualElement root, ModalStack modals, DialogueService dialogue, EvidencePickerScreen picker)
            : base(root, modals)
        {
            this.dialogue = dialogue;
            this.picker = picker;
            nameLabel = root.Q<Label>("name");
            roleLabel = root.Q<Label>("role");
            speech = new Typewriter(root.Q<Label>("speech"));
            choices = root.Q("choices");
            presentButton = root.Q<Button>("present");
            leaveButton = root.Q<Button>("leave");
            presentButton.clicked += OnPresentClicked;
            leaveButton.clicked += Close;

            dialogue.Started += OnStarted;
            dialogue.Ended += OnEnded;
        }

        public override void Dispose()
        {
            dialogue.Started -= OnStarted;
            dialogue.Ended -= OnEnded;
            presentButton.clicked -= OnPresentClicked;
            leaveButton.clicked -= Close;
            base.Dispose();
        }

        public override void OnBack()
        {
            // Первое нажатие Esc дописывает реплику, второе — завершает разговор.
            if (speech.IsTyping) speech.Complete();
            else Close();
        }

        void OnStarted(Conversation started)
        {
            conversation = started;
            nameLabel.text = UIText.Get(started.Character.DisplayName);
            roleLabel.text = UIText.Get(started.Character.Role);
            Open();
            Say(started.Greeting);
        }

        void OnEnded(Conversation _)
        {
            conversation = null;
            Close();
        }

        protected override void OnClosed()
        {
            if (conversation != null) dialogue.End();
        }

        void Say(LocalizedString line)
        {
            speech.Show(UIText.Get(line));
            RebuildChoices();
        }

        void RebuildChoices()
        {
            choices.Clear();
            if (conversation == null) return;

            Button firstNew = null;
            foreach (var topic in conversation.AvailableTopics)
            {
                var asked = conversation.WasAsked(topic);
                var button = new Button { text = UIText.Get(topic.Question) };
                button.AddToClassList("button");
                button.AddToClassList(ChoiceClass);
                button.EnableInClassList(AskedClass, asked);
                var chosen = topic;
                button.clicked += () => Say(conversation.Ask(chosen));
                choices.Add(button);
                if (!asked) firstNew ??= button;
            }
            (firstNew ?? presentButton).Focus();
        }

        void OnPresentClicked() =>
            picker.Pick("evidence.title", includeItems: false, OnEvidencePicked);

        void OnEvidencePicked(FactDefinition evidence)
        {
            if (conversation == null) return;
            Say(conversation.Present(evidence));
        }
    }
}
