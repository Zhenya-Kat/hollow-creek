using System;
using HollowCreek.Gameplay.Input;
using HollowCreek.Gameplay.Modals;
using UnityEngine.UIElements;

namespace HollowCreek.UI.Common
{
    /// <summary>
    /// Основа экрана интерфейса, который открывается поверх игры (дневник, сообщение…).
    /// Открытый экран кладётся в <see cref="ModalStack"/>: курсор освобождается, ходьба блокируется,
    /// а Esc закрывает верхний экран.
    /// </summary>
    public abstract class ScreenView : IModal, IDisposable
    {
        public const string HiddenClass = "hidden";

        protected readonly VisualElement Root;
        protected readonly ModalStack Modals;

        protected ScreenView(VisualElement root, ModalStack modals)
        {
            Root = root ?? throw new ArgumentNullException(nameof(root));
            Modals = modals;
            Root.AddToClassList(HiddenClass);
        }

        public bool IsOpen { get; private set; }

        public virtual InputMode InputMode => InputMode.UI;

        /// <summary>См. <see cref="IModal.DefersMessages"/>.</summary>
        public virtual bool DefersMessages => false;

        bool IModal.DefersMessages => DefersMessages;

        public void Open()
        {
            if (!IsOpen)
            {
                IsOpen = true;
                Root.RemoveFromClassList(HiddenClass);
                OnOpened();
            }
            Modals.Push(this);
        }

        public void Close()
        {
            if (!IsOpen) return;
            IsOpen = false;
            Root.AddToClassList(HiddenClass);
            Modals.Remove(this);
            OnClosed();
        }

        public virtual void OnBack() => Close();

        public virtual void Dispose()
        {
            if (IsOpen) Close();
        }

        protected virtual void OnOpened() { }
        protected virtual void OnClosed() { }
    }
}
