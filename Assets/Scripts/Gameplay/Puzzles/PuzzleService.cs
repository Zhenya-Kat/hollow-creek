using System;
using HollowCreek.Core;
using HollowCreek.Core.Logic;
using HollowCreek.Core.Puzzles;
using HollowCreek.Core.State;
using HollowCreek.Gameplay.Messages;
using UnityEngine;

namespace HollowCreek.Gameplay.Puzzles
{
    /// <summary>
    /// Открывает головоломки и выдаёт награду за решение. Экраны головоломок (слой интерфейса)
    /// подписываются на <see cref="Requested"/> и сообщают о решении через <see cref="Complete"/>.
    /// </summary>
    [DefaultExecutionOrder(-860)]
    public sealed class PuzzleService : MonoBehaviour
    {
        GameState state;
        GameObject currentSource;

        /// <summary>Нужно показать головоломку.</summary>
        public event Action<PuzzleDefinition> Requested;

        public PuzzleDefinition Current { get; private set; }

        void Awake()
        {
            state = Services.Get<GameState>();
            Services.Register(this);
        }

        void OnDestroy() => Services.Unregister(this);

        public bool IsSolved(PuzzleDefinition puzzle) =>
            puzzle.SolvedFact != null && state.Has(puzzle.SolvedFact);

        /// <summary>Попробовать открыть головоломку: уже решена — напоминание, заперта — пояснение.</summary>
        public void Open(PuzzleDefinition puzzle, GameObject source = null)
        {
            if (puzzle == null) throw new ArgumentNullException(nameof(puzzle));

            if (IsSolved(puzzle))
            {
                ShowMessage(puzzle, puzzle.RevisitMessage);
                return;
            }
            if (!Condition.IsMet(puzzle.Requires, state))
            {
                ShowMessage(puzzle, puzzle.LockedMessage);
                return;
            }

            Current = puzzle;
            currentSource = source;
            Requested?.Invoke(puzzle);
        }

        /// <summary>Экран сообщает: головоломка решена. Выдаём факт, выполняем действия, показываем сообщение.</summary>
        public void Complete(PuzzleDefinition puzzle)
        {
            if (puzzle == null || puzzle != Current) return;
            var source = currentSource;
            Current = null;
            currentSource = null;

            if (puzzle.SolvedFact != null) state.Grant(puzzle.SolvedFact);
            GameAction.RunAll(puzzle.OnSolved, new ActionContext(source));
            ShowMessage(puzzle, puzzle.SolvedMessage);
        }

        /// <summary>Игрок закрыл головоломку, не решив её.</summary>
        public void Cancel(PuzzleDefinition puzzle)
        {
            if (puzzle != Current) return;
            Current = null;
            currentSource = null;
        }

        static void ShowMessage(PuzzleDefinition puzzle, UnityEngine.Localization.LocalizedString text)
        {
            if (text == null || text.IsEmpty) return;
            Services.Get<IMessagePresenter>().Show(puzzle.Title, text);
        }
    }

    [Serializable, SelectorLabel("Открыть головоломку")]
    public sealed class OpenPuzzleAction : GameAction
    {
        [SerializeField] PuzzleDefinition puzzle;

        public OpenPuzzleAction() { }
        public OpenPuzzleAction(PuzzleDefinition puzzle) => this.puzzle = puzzle;

        public override void Execute(in ActionContext context)
        {
            if (puzzle == null)
            {
                Debug.LogWarning("[Action] Не указана головоломка.", context.Source);
                return;
            }
            Services.Get<PuzzleService>().Open(puzzle, context.Source);
        }
    }
}
