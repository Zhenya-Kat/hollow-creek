using System.Collections.Generic;
using HollowCreek.Core.Puzzles;
using HollowCreek.UI.Common;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

namespace HollowCreek.UI.Puzzles
{
    /// <summary>Кодовый замок: табло, лампа и цифровая клавиатура. Цифры можно набирать и с клавиатуры.</summary>
    public sealed class CodeLockView : IPuzzleView
    {
        const string LampWrong = "codelock__lamp--wrong";
        const string LampRight = "codelock__lamp--right";

        CodeLockState state;
        PuzzleViewContext context;
        Label display;
        VisualElement lamp;
        Button openButton;
        Button firstKey;
        readonly List<Button> keys = new();
        bool locked;

        public VisualElement Build(PuzzleDefinition puzzle, PuzzleViewContext viewContext)
        {
            context = viewContext;
            state = new CodeLockState(((CodeLockPuzzle)puzzle).Code);

            var root = new VisualElement();
            root.AddToClassList("codelock");

            var face = new VisualElement();
            face.AddToClassList("codelock__face");
            lamp = new VisualElement();
            lamp.AddToClassList("codelock__lamp");
            display = new Label();
            display.AddToClassList("codelock__display");
            face.Add(lamp);
            face.Add(display);
            root.Add(face);

            var pad = new VisualElement();
            pad.AddToClassList("codelock__pad");
            foreach (var key in new[] { "1", "2", "3", "4", "5", "6", "7", "8", "9", "erase", "0", "open" })
            {
                var button = new Button();
                button.AddToClassList("button");
                button.AddToClassList("codelock__key");
                switch (key)
                {
                    case "erase":
                        button.text = UIText.Get("codelock.erase");
                        button.AddToClassList("codelock__key--word");
                        button.clicked += Erase;
                        break;
                    case "open":
                        button.text = UIText.Get("codelock.open");
                        button.AddToClassList("codelock__key--word");
                        button.AddToClassList("button--primary");
                        button.clicked += Submit;
                        openButton = button;
                        break;
                    default:
                        var digit = key[0];
                        button.text = key;
                        button.clicked += () => Press(digit);
                        break;
                }
                firstKey ??= button;
                keys.Add(button);
                pad.Add(button);
            }
            root.Add(pad);

            if (Keyboard.current != null) Keyboard.current.onTextInput += OnTextInput;
            Refresh();
            context.SetStatus(UIText.Get("codelock.prompt"));
            return root;
        }

        public void Focus() => firstKey?.Focus();

        public void Dispose()
        {
            if (Keyboard.current != null) Keyboard.current.onTextInput -= OnTextInput;
        }

        // Цифры и Backspace с клавиатуры. Enter не обрабатываем: он нажимает кнопку «Открыть» в фокусе.
        void OnTextInput(char c)
        {
            if (c >= '0' && c <= '9') Press(c);
            else if (c == '\b') Erase();
        }

        void Press(char digit)
        {
            if (locked || !state.Press(digit)) return;
            UiAudio.Play(c => c.keypadPress);
            if (lamp.ClassListContains(LampWrong))
            {
                lamp.RemoveFromClassList(LampWrong);
                context.SetStatus(UIText.Get("codelock.prompt"));
            }
            Refresh();
            if (state.IsFull) openButton.Focus();
        }

        void Erase()
        {
            if (locked) return;
            state.Erase();
            Refresh();
        }

        void Submit()
        {
            if (locked || state.Entered.Length == 0) return;
            if (state.Submit())
            {
                locked = true;
                UiAudio.Play(c => c.unlock);
                lamp.AddToClassList(LampRight);
                context.SetStatus(UIText.Get("codelock.right"));
                Refresh();
                context.Solved();
                return;
            }
            lamp.AddToClassList(LampWrong);
            UiAudio.Play(c => c.wrong);
            context.SetStatus(UIText.Get("codelock.wrong"));
            Refresh();
            firstKey.Focus();
        }

        void Refresh()
        {
            var slots = new string[state.Length];
            for (var i = 0; i < slots.Length; i++)
                slots[i] = i < state.Entered.Length ? state.Entered[i].ToString() : "_";
            display.text = string.Join(" ", slots);
        }
    }
}
