using System;
using System.Collections.Generic;
using HollowCreek.Core.Puzzles;
using HollowCreek.Gameplay.Modals;
using HollowCreek.Gameplay.Puzzles;
using HollowCreek.UI.Common;
using UnityEngine.UIElements;

namespace HollowCreek.UI.Puzzles
{
    /// <summary>Что вид головоломки может сообщить рамке.</summary>
    public sealed class PuzzleViewContext
    {
        public PuzzleViewContext(Action solved, Action<string> setStatus)
        {
            Solved = solved;
            SetStatus = setStatus;
        }

        /// <summary>Головоломка решена.</summary>
        public Action Solved { get; }

        /// <summary>Показать строку состояния («Неверная комбинация»…).</summary>
        public Action<string> SetStatus { get; }
    }

    /// <summary>Содержимое конкретного вида головоломки внутри общей рамки.</summary>
    public interface IPuzzleView : IDisposable
    {
        VisualElement Build(PuzzleDefinition puzzle, PuzzleViewContext context);

        /// <summary>Поставить фокус на первый элемент управления (для клавиатуры и геймпада).</summary>
        void Focus();
    }

    /// <summary>
    /// Рамка головоломки: заголовок, пояснение, строка состояния, кнопка «Закрыть».
    /// Содержимое создаёт вид, подобранный по типу головоломки.
    /// </summary>
    public sealed class PuzzleScreen : ScreenView
    {
        const long SolvedDelayMs = 900;

        readonly PuzzleService service;
        readonly IReadOnlyDictionary<Type, Func<IPuzzleView>> views;
        readonly Label title;
        readonly Label instructions;
        readonly Label status;
        readonly VisualElement body;
        readonly Button closeButton;
        PuzzleDefinition puzzle;
        IPuzzleView view;
        bool solved;

        public PuzzleScreen(VisualElement root, ModalStack modals, PuzzleService service,
            IReadOnlyDictionary<Type, Func<IPuzzleView>> views) : base(root, modals)
        {
            this.service = service;
            this.views = views;
            title = root.Q<Label>("puzzle-title");
            instructions = root.Q<Label>("puzzle-instructions");
            status = root.Q<Label>("puzzle-status");
            body = root.Q("puzzle-body");
            closeButton = root.Q<Button>("puzzle-close");
            closeButton.clicked += Close;
            service.Requested += OnRequested;
        }

        public override void Dispose()
        {
            service.Requested -= OnRequested;
            closeButton.clicked -= Close;
            base.Dispose();
        }

        void OnRequested(PuzzleDefinition requested)
        {
            if (!views.TryGetValue(requested.GetType(), out var create))
            {
                UnityEngine.Debug.LogError($"[Puzzle] Нет экрана для головоломки типа {requested.GetType().Name}.");
                service.Cancel(requested);
                return;
            }

            puzzle = requested;
            solved = false;
            title.text = UIText.Get(requested.Title);
            instructions.text = UIText.Get(requested.Instructions);
            instructions.EnableInClassList(HiddenClass, string.IsNullOrEmpty(instructions.text));
            status.text = string.Empty;

            view = create();
            body.Clear();
            body.Add(view.Build(requested, new PuzzleViewContext(OnSolved, text => status.text = text)));
            Open();
            view.Focus();
        }

        void OnSolved()
        {
            if (solved) return;
            solved = true;
            UiAudio.Play(c => c.puzzleSolved);
            body.SetEnabled(false);
            // Небольшая пауза, чтобы игрок увидел результат (зелёная лампа, проступивший текст).
            Root.schedule.Execute(() =>
            {
                var finished = puzzle;
                Close();
                service.Complete(finished);
            }).StartingIn(SolvedDelayMs);
        }

        public override bool DefersMessages => true;

        public override void OnBack()
        {
            if (!solved) Close();
        }

        protected override void OnClosed()
        {
            if (!solved && puzzle != null) service.Cancel(puzzle);
            view?.Dispose();
            view = null;
            puzzle = null;
            body.Clear();
            body.SetEnabled(true);
        }
    }
}
