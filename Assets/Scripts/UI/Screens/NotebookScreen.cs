using HollowCreek.Core.State;
using HollowCreek.Gameplay.Input;
using HollowCreek.Gameplay.Modals;
using HollowCreek.UI.Common;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

namespace HollowCreek.UI.Screens
{
    /// <summary>
    /// Дневник героини: найденные улики и предметы в кармане. Открывается клавишей Tab (или J).
    /// </summary>
    public sealed class NotebookScreen : ScreenView
    {
        readonly GameInput input;
        readonly IGameStateReader state;
        readonly FactListView facts;
        readonly Button closeButton;

        public NotebookScreen(VisualElement root, ModalStack modals, GameInput input, IGameStateReader state)
            : base(root, modals)
        {
            this.input = input;
            this.state = state;
            facts = new FactListView(root);
            closeButton = root.Q<Button>("close");
            closeButton.clicked += Close;
            input.Notebook.performed += OnNotebookPressed;
        }

        public override void Dispose()
        {
            input.Notebook.performed -= OnNotebookPressed;
            closeButton.clicked -= Close;
            base.Dispose();
        }

        void OnNotebookPressed(InputAction.CallbackContext _)
        {
            if (IsOpen && Modals.Top == this) Close();
            else if (Modals.IsEmpty) Open();
        }

        protected override void OnOpened()
        {
            if (facts.Build(state, includeItems: true)) facts.FocusSelection();
            else closeButton.Focus();
        }
    }
}
