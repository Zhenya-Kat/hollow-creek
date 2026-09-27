using System;
using System.Collections.Generic;
using HollowCreek.Core;
using HollowCreek.Gameplay.Input;
using UnityEngine;
using UnityEngine.InputSystem;

namespace HollowCreek.Gameplay.Modals
{
    /// <summary>Что-то, что открывается поверх игры: экран интерфейса, осмотр предмета, головоломка.</summary>
    public interface IModal
    {
        /// <summary>Какой режим управления нужен, пока этот модал сверху.</summary>
        InputMode InputMode { get; }

        /// <summary>Игрок нажал «Назад» (Esc), и этот модал сверху. Обычно — закрыться.</summary>
        void OnBack();
    }

    /// <summary>
    /// Стек открытых модалов. Верхний определяет режим управления и получает нажатие «Назад».
    /// Когда стек пуст — обычная игра.
    /// </summary>
    [DefaultExecutionOrder(-890)]
    public sealed class ModalStack : MonoBehaviour
    {
        [SerializeField] GameInput input;

        readonly List<IModal> stack = new();

        /// <summary>Состав стека изменился.</summary>
        public event Action Changed;

        /// <summary>«Назад» нажато, когда ничего не открыто (сюда подключится меню паузы).</summary>
        public event Action BackPressedInGameplay;

        public IModal Top => stack.Count > 0 ? stack[^1] : null;
        public bool IsEmpty => stack.Count == 0;
        public bool Contains(IModal modal) => stack.Contains(modal);

        void Awake() => Services.Register(this);
        void OnDestroy() => Services.Unregister(this);
        void OnEnable() => input.Back.performed += OnBackPerformed;
        void OnDisable() => input.Back.performed -= OnBackPerformed;

        /// <summary>Открыть модал поверх остальных (если уже открыт — поднять наверх).</summary>
        public void Push(IModal modal)
        {
            if (modal == null) throw new ArgumentNullException(nameof(modal));
            stack.Remove(modal);
            stack.Add(modal);
            Apply();
        }

        public void Remove(IModal modal)
        {
            if (stack.Remove(modal)) Apply();
        }

        void Apply()
        {
            input.SetMode(Top?.InputMode ?? InputMode.Gameplay);
            Changed?.Invoke();
        }

        void OnBackPerformed(InputAction.CallbackContext _)
        {
            if (Top != null) Top.OnBack();
            else BackPressedInGameplay?.Invoke();
        }
    }
}
