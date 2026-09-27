using UnityEngine;

namespace HollowCreek.Core.Puzzles
{
    /// <summary>Кодовый замок: нужно набрать правильную комбинацию цифр.</summary>
    [CreateAssetMenu(menuName = "Hollow Creek/Puzzles/Code Lock", fileName = "CodeLock", order = 40)]
    public sealed class CodeLockPuzzle : PuzzleDefinition
    {
        [SerializeField, Tooltip("Правильный код, только цифры")]
        string code = "0000";

        public string Code => code;

#if UNITY_EDITOR
        void OnValidate()
        {
            var digits = System.Text.RegularExpressions.Regex.Replace(code ?? string.Empty, "[^0-9]", string.Empty);
            if (digits.Length == 0) digits = "0";
            code = digits;
        }
#endif
    }

    /// <summary>Состояние набора кода. Только логика — кнопки и экран живут в интерфейсе.</summary>
    public sealed class CodeLockState
    {
        readonly string code;

        public CodeLockState(string code) => this.code = code;

        public string Entered { get; private set; } = string.Empty;
        public int Length => code.Length;
        public bool IsFull => Entered.Length >= code.Length;

        /// <summary>Добавить цифру. Возвращает false, если все позиции заняты.</summary>
        public bool Press(char digit)
        {
            if (IsFull || digit < '0' || digit > '9') return false;
            Entered += digit;
            return true;
        }

        public void Erase()
        {
            if (Entered.Length > 0) Entered = Entered.Substring(0, Entered.Length - 1);
        }

        public void Clear() => Entered = string.Empty;

        /// <summary>Проверить набранный код. Неверный код сбрасывается.</summary>
        public bool Submit()
        {
            if (Entered == code) return true;
            Clear();
            return false;
        }
    }
}
