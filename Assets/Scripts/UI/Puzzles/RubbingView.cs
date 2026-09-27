using System.Collections.Generic;
using System.Text;
using HollowCreek.Core.Puzzles;
using HollowCreek.Gameplay.Input;
using HollowCreek.UI.Common;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

namespace HollowCreek.UI.Puzzles
{
    /// <summary>
    /// Лист ежедневника: игрок штрихует карандашом, и вдавленные строки проступают.
    /// Мышь — зажать ЛКМ и водить. Геймпад — левый стик ведёт карандаш, «A» штрихует.
    /// Клавиатура — стрелки ведут карандаш, пробел штрихует.
    /// </summary>
    public sealed class RubbingView : IPuzzleView
    {
        const float PaperWidth = 980f;
        const float PaperHeight = 400f;
        const float TextLeft = 60f;
        const float TextRight = 60f;
        const float FirstLineY = 110f;
        const float LineGap = 95f;
        const float LineBand = 36f;
        const float PencilRadius = 16f;
        const float VirtualPencilSpeed = 520f;
        const int MaxPoints = 4000;

        readonly GameInput input;
        RubbingPuzzle puzzle;
        RubbingState state;
        PuzzleViewContext context;
        readonly List<string> texts = new();
        readonly List<Label> lineLabels = new();
        // Штрихи карандаша: каждый — непрерывная линия от нажатия до отпускания.
        readonly List<List<Vector2>> strokes = new();
        int pointCount;
        bool virtualPressing;
        VisualElement paper;
        VisualElement cursor;
        IVisualElementScheduledItem ticker;
        Vector2 virtualPencil = new(TextLeft, FirstLineY);
        bool pointerDown;
        bool done;

        public RubbingView(GameInput input) => this.input = input;

        public VisualElement Build(PuzzleDefinition definition, PuzzleViewContext viewContext)
        {
            puzzle = (RubbingPuzzle)definition;
            context = viewContext;
            state = new RubbingState(puzzle.Lines.Count, puzzle.SegmentsPerLine, puzzle.RequiredCoverage);

            paper = new VisualElement { focusable = true };
            paper.AddToClassList("rubbing__paper");
            paper.style.width = PaperWidth;
            paper.style.height = PaperHeight;
            paper.generateVisualContent += DrawMarks;

            for (var i = 0; i < puzzle.Lines.Count; i++)
            {
                texts.Add(UIText.Get(puzzle.Lines[i]));
                var label = new Label { enableRichText = true, pickingMode = PickingMode.Ignore };
                label.AddToClassList("rubbing__line");
                label.style.left = TextLeft;
                label.style.right = TextRight;
                label.style.top = FirstLineY + i * LineGap - LineBand * 0.5f;
                lineLabels.Add(label);
                paper.Add(label);
                UpdateLine(i);
            }

            cursor = new VisualElement { pickingMode = PickingMode.Ignore };
            cursor.AddToClassList("rubbing__cursor");
            cursor.AddToClassList(ScreenView.HiddenClass);
            paper.Add(cursor);

            paper.RegisterCallback<PointerDownEvent>(e =>
            {
                pointerDown = true;
                paper.CapturePointer(e.pointerId);
                BeginStroke();
                RubAt(e.localPosition);
            });
            paper.RegisterCallback<PointerMoveEvent>(e =>
            {
                if (pointerDown) RubAt(e.localPosition);
            });
            paper.RegisterCallback<PointerUpEvent>(e =>
            {
                pointerDown = false;
                paper.ReleasePointer(e.pointerId);
            });

            // Стрелки и стик ведут карандаш, а не переводят фокус на кнопки.
            paper.RegisterCallback<NavigationMoveEvent>(e =>
            {
                e.StopPropagation();
                paper.focusController?.IgnoreEvent(e);
            });

            ticker = paper.schedule.Execute(Tick).Every(16);
            UpdateHint();
            input.UsingGamepadChanged += OnDeviceChanged;
            return paper;
        }

        public void Focus() => paper.Focus();

        public void Dispose()
        {
            ticker?.Pause();
            input.UsingGamepadChanged -= OnDeviceChanged;
        }

        void OnDeviceChanged(bool _) => UpdateHint();

        void UpdateHint() =>
            context.SetStatus(UIText.Get(input.UsingGamepad ? "rubbing.hint.gamepad" : "rubbing.hint.km"));

        // Карандаш с геймпада или клавиатуры.
        void Tick(TimerState timer)
        {
            if (done) return;
            var dt = timer.deltaTime / 1000f;
            var move = Vector2.zero;
            var pressing = false;

            var pad = Gamepad.current;
            if (pad != null)
            {
                move += pad.leftStick.ReadValue();
                pressing |= pad.buttonSouth.isPressed;
            }
            var keyboard = Keyboard.current;
            if (keyboard != null)
            {
                if (keyboard.leftArrowKey.isPressed) move.x -= 1f;
                if (keyboard.rightArrowKey.isPressed) move.x += 1f;
                if (keyboard.upArrowKey.isPressed) move.y += 1f;
                if (keyboard.downArrowKey.isPressed) move.y -= 1f;
                pressing |= keyboard.spaceKey.isPressed;
            }

            if (pressing && !virtualPressing) BeginStroke();
            virtualPressing = pressing;
            if (move == Vector2.zero && !pressing) return;
            cursor.RemoveFromClassList(ScreenView.HiddenClass);
            // У интерфейса ось Y направлена вниз, у стика — вверх.
            virtualPencil += new Vector2(move.x, -move.y) * (VirtualPencilSpeed * dt);
            virtualPencil.x = Mathf.Clamp(virtualPencil.x, 0f, PaperWidth);
            virtualPencil.y = Mathf.Clamp(virtualPencil.y, 0f, PaperHeight);
            cursor.style.left = virtualPencil.x - PencilRadius;
            cursor.style.top = virtualPencil.y - PencilRadius;
            if (pressing) RubAt(virtualPencil);
        }

        void RubAt(Vector2 point)
        {
            if (done) return;
            if (strokes.Count == 0) BeginStroke();
            var stroke = strokes[^1];
            if (pointCount < MaxPoints && (stroke.Count == 0 || (stroke[^1] - point).sqrMagnitude > 16f))
            {
                stroke.Add(point);
                pointCount++;
                paper.MarkDirtyRepaint();
            }

            var textWidth = PaperWidth - TextLeft - TextRight;
            var x = (point.x - TextLeft) / textWidth;
            var radius = PencilRadius / textWidth;
            for (var i = 0; i < state.LineCount; i++)
            {
                var lineY = FirstLineY + i * LineGap;
                if (Mathf.Abs(point.y - lineY) > LineBand) continue;
                if (state.Rub(i, x, radius)) UpdateLine(i);
            }

            if (state.IsComplete)
            {
                done = true;
                for (var i = 0; i < state.LineCount; i++) lineLabels[i].text = texts[i];
                context.Solved();
            }
        }

        /// <summary>Проявляет символы строки там, где участок заштрихован.</summary>
        void UpdateLine(int line)
        {
            var text = texts[line];
            var builder = new StringBuilder(text.Length * 2);
            bool? lastVisible = null;
            for (var j = 0; j < text.Length; j++)
            {
                var segment = Mathf.Min(state.Segments - 1, j * state.Segments / Mathf.Max(1, text.Length));
                var visible = state.IsCovered(line, segment);
                if (visible != lastVisible)
                {
                    builder.Append(visible ? "<alpha=#FF>" : "<alpha=#00>");
                    lastVisible = visible;
                }
                builder.Append(text[j]);
            }
            lineLabels[line].text = builder.ToString();
        }

        void BeginStroke() => strokes.Add(new List<Vector2>());

        void DrawMarks(MeshGenerationContext mgc)
        {
            var painter = mgc.painter2D;
            painter.strokeColor = new Color(0.30f, 0.29f, 0.30f, 0.22f);
            painter.lineWidth = PencilRadius * 2f;
            painter.lineCap = LineCap.Round;
            painter.lineJoin = LineJoin.Round;
            foreach (var stroke in strokes)
            {
                if (stroke.Count == 0) continue;
                painter.BeginPath();
                painter.MoveTo(stroke[0]);
                // Одна точка — короткий штрих, чтобы след был виден и от одиночного щелчка.
                if (stroke.Count == 1) painter.LineTo(stroke[0] + new Vector2(0.5f, 0f));
                for (var i = 1; i < stroke.Count; i++) painter.LineTo(stroke[i]);
                painter.Stroke();
            }
        }
    }
}
