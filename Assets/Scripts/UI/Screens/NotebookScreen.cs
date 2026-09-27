using HollowCreek.Core.State;
using HollowCreek.Core.Story;
using HollowCreek.Gameplay.Input;
using HollowCreek.Gameplay.Messages;
using HollowCreek.Gameplay.Modals;
using HollowCreek.UI.Common;
using UnityEngine.InputSystem;
using UnityEngine.Localization;
using UnityEngine.UIElements;

namespace HollowCreek.UI.Screens
{
    /// <summary>
    /// Дневник героини: текущие цели, найденные улики и предметы в кармане.
    /// Отсюда же — подсказки и обвинение. Открывается клавишей Tab (или J).
    /// </summary>
    public sealed class NotebookScreen : ScreenView
    {
        readonly GameInput input;
        readonly IGameStateReader state;
        readonly ObjectiveTracker objectives;
        readonly CaseService caseService;
        readonly HintScreen hints;
        readonly AccusationScreen accusation;
        readonly IMessagePresenter messages;
        readonly FactListView facts;
        readonly VisualElement objectivesBlock;
        readonly Button hintButton;
        readonly Button accuseButton;
        readonly Button closeButton;

        public NotebookScreen(VisualElement root, ModalStack modals, GameInput input, IGameStateReader state,
            ObjectiveTracker objectives, CaseService caseService, HintScreen hints, AccusationScreen accusation,
            IMessagePresenter messages) : base(root, modals)
        {
            this.input = input;
            this.state = state;
            this.objectives = objectives;
            this.caseService = caseService;
            this.hints = hints;
            this.accusation = accusation;
            this.messages = messages;
            facts = new FactListView(root);
            objectivesBlock = root.Q("objectives");
            hintButton = root.Q<Button>("hint");
            accuseButton = root.Q<Button>("accuse");
            closeButton = root.Q<Button>("close");
            hintButton.clicked += hints.Open;
            accuseButton.clicked += OnAccuse;
            closeButton.clicked += Close;
            input.Notebook.performed += OnNotebookPressed;
            objectives.Changed += OnObjectivesChanged;
        }

        public override void Dispose()
        {
            input.Notebook.performed -= OnNotebookPressed;
            objectives.Changed -= OnObjectivesChanged;
            hintButton.clicked -= hints.Open;
            accuseButton.clicked -= OnAccuse;
            closeButton.clicked -= Close;
            base.Dispose();
        }

        void OnNotebookPressed(InputAction.CallbackContext _)
        {
            if (IsOpen && Modals.Top == this) Close();
            else if (Modals.IsEmpty) Open();
        }

        protected override Core.Audio.SoundCue OpenSound(Core.Audio.AudioCues cues) => cues.notebookOpen;
        protected override Core.Audio.SoundCue CloseSound(Core.Audio.AudioCues cues) => cues.notebookClose;

        protected override void OnOpened()
        {
            RefreshObjectives();
            if (facts.Build(state, includeItems: true)) facts.FocusSelection();
            else closeButton.Focus();
        }

        void OnObjectivesChanged()
        {
            if (IsOpen) RefreshObjectives();
        }

        void RefreshObjectives()
        {
            objectivesBlock.Clear();
            var header = new Label(UIText.Get("notebook.objectives"));
            header.AddToClassList("section-header");
            objectivesBlock.Add(header);
            var first = true;
            foreach (var objective in objectives.Open)
            {
                var line = new Label("•  " + UIText.Get(objective.Text));
                line.AddToClassList("notebook__objective");
                line.EnableInClassList("notebook__objective--current", first);
                objectivesBlock.Add(line);
                first = false;
            }

            accuseButton.text = UIText.Get(caseService.IsSolved ? "notebook.ending" : "notebook.accuse");
        }

        void OnAccuse()
        {
            var definition = caseService.Definition;
            if (caseService.IsSolved) messages.Show(definition.EndingTitle, definition.EndingText);
            else if (!caseService.IsAvailable) messages.Show(new LocalizedString(UIText.Table, "accuse.title"), definition.NotReadyText);
            else accusation.Open();
        }
    }
}
