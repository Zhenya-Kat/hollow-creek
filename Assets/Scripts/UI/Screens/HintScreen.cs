using HollowCreek.Core.Story;
using HollowCreek.Gameplay.Modals;
using HollowCreek.UI.Common;
using UnityEngine.UIElements;

namespace HollowCreek.UI.Screens
{
    /// <summary>
    /// Подсказки к текущей цели: открываются по одной. Последняя — прямой ответ,
    /// её нужно подтвердить вторым нажатием, чтобы не увидеть решение случайно.
    /// </summary>
    public sealed class HintScreen : ScreenView
    {
        readonly ObjectiveTracker objectives;
        readonly Label objectiveLabel;
        readonly VisualElement list;
        readonly Button moreButton;
        readonly Button closeButton;
        Objective objective;
        bool confirmingAnswer;

        public HintScreen(VisualElement root, ModalStack modals, ObjectiveTracker objectives) : base(root, modals)
        {
            this.objectives = objectives;
            objectiveLabel = root.Q<Label>("hint-objective");
            list = root.Q("hint-list");
            moreButton = root.Q<Button>("hint-more");
            closeButton = root.Q<Button>("hint-close");
            moreButton.clicked += OnMore;
            closeButton.clicked += Close;
        }

        public override void Dispose()
        {
            moreButton.clicked -= OnMore;
            closeButton.clicked -= Close;
            base.Dispose();
        }

        protected override void OnOpened()
        {
            objective = objectives.Current;
            confirmingAnswer = false;
            // Первая подсказка открывается сразу: игрок за ней и пришёл.
            if (objective != null && objectives.RevealedHints(objective) == 0) objectives.RevealNextHint(objective);
            Refresh();
        }

        void OnMore()
        {
            if (objective == null) return;
            if (objectives.IsNextHintTheAnswer(objective) && !confirmingAnswer)
            {
                confirmingAnswer = true;
                Refresh();
                return;
            }
            confirmingAnswer = false;
            objectives.RevealNextHint(objective);
            Refresh();
        }

        void Refresh()
        {
            list.Clear();
            if (objective == null)
            {
                objectiveLabel.text = string.Empty;
                list.Add(Line(UIText.Get("hint.none")));
                moreButton.AddToClassList(HiddenClass);
                closeButton.Focus();
                return;
            }

            objectiveLabel.text = UIText.Get(objective.Text);
            var shown = objectives.RevealedHints(objective);
            for (var i = 0; i < shown && i < objective.Hints.Count; i++)
                list.Add(Line(UIText.Get(objective.Hints[i])));

            var more = objectives.HasMoreHints(objective);
            moreButton.EnableInClassList(HiddenClass, !more);
            if (!more)
            {
                closeButton.Focus();
                return;
            }
            moreButton.text = UIText.Get(!objectives.IsNextHintTheAnswer(objective) ? "hint.more"
                : confirmingAnswer ? "hint.answer.confirm" : "hint.answer");
            moreButton.Focus();
        }

        static Label Line(string text)
        {
            var label = new Label(text);
            label.AddToClassList("body-text");
            label.AddToClassList("hint__line");
            return label;
        }
    }
}
