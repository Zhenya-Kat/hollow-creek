using System.Collections.Generic;
using HollowCreek.Core.Puzzles;
using HollowCreek.UI.Common;
using UnityEngine.UIElements;

namespace HollowCreek.UI.Puzzles
{
    /// <summary>
    /// Раскладка карточек: щелчок по одной, затем по другой — они меняются местами.
    /// С клавиатуры и геймпада — стрелки/стик и Enter/«A».
    /// </summary>
    public sealed class SequenceView : IPuzzleView
    {
        const string SelectedClass = "photo--selected";

        SequencePuzzle puzzle;
        SequenceState state;
        PuzzleViewContext context;
        readonly List<Button> slots = new();
        int selected = -1;

        public VisualElement Build(PuzzleDefinition definition, PuzzleViewContext viewContext)
        {
            puzzle = (SequencePuzzle)definition;
            context = viewContext;
            state = new SequenceState(puzzle.Cards.Count, puzzle.StartOrder);

            var root = new VisualElement();
            root.AddToClassList("sequence");
            var row = new VisualElement();
            row.AddToClassList("sequence__row");
            for (var i = 0; i < state.Count; i++)
            {
                var position = i;
                var slot = new Button();
                slot.AddToClassList("photo");
                var image = new VisualElement { name = "image" };
                image.AddToClassList("photo__image");
                slot.Add(image);
                slot.clicked += () => OnSlotClicked(position);
                slots.Add(slot);
                row.Add(slot);
            }
            root.Add(row);

            var check = new Button { text = UIText.Get("sequence.check") };
            check.AddToClassList("button");
            check.AddToClassList("button--primary");
            check.AddToClassList("sequence__check");
            check.clicked += Check;
            root.Add(check);

            Refresh();
            context.SetStatus(UIText.Get("sequence.hint"));
            return root;
        }

        public void Focus() => slots[0].Focus();

        public void Dispose() { }

        void OnSlotClicked(int position)
        {
            if (selected < 0) selected = position;
            else
            {
                if (selected != position) state.Swap(selected, position);
                selected = -1;
            }
            Refresh();
        }

        void Check()
        {
            if (state.IsSolved) context.Solved();
            else context.SetStatus(UIText.Get(puzzle.WrongOrderMessage));
        }

        void Refresh()
        {
            for (var i = 0; i < slots.Count; i++)
            {
                var card = puzzle.Cards[state.CardAt(i)];
                var image = slots[i].Q("image");
                image.style.backgroundImage = card.Image != null ? new StyleBackground(card.Image) : StyleKeyword.None;
                slots[i].EnableInClassList(SelectedClass, i == selected);
            }
        }
    }
}
