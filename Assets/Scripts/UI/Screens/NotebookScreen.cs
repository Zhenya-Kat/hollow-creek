using System;
using HollowCreek.Core.Facts;
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
        const string EntryClass = "notebook__entry";
        const string SelectedClass = "notebook__entry--selected";

        readonly GameInput input;
        readonly IGameStateReader state;
        readonly ScrollView list;
        readonly Label detailTitle;
        readonly Label detailSubtitle;
        readonly Label detailBody;
        readonly Button closeButton;
        Button selected;

        public NotebookScreen(VisualElement root, ModalStack modals, GameInput input, IGameStateReader state)
            : base(root, modals)
        {
            this.input = input;
            this.state = state;
            list = root.Q<ScrollView>("list");
            detailTitle = root.Q<Label>("detail-title");
            detailSubtitle = root.Q<Label>("detail-subtitle");
            detailBody = root.Q<Label>("detail-body");
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
            list.Clear();
            selected = null;
            ShowDetail(null, null, null);

            // Сначала улики: самые свежие сверху — их чаще всего хочется перечитать.
            Button newest = null;
            AddSection("notebook.clues", "notebook.empty.clues", fact =>
            {
                if (fact is not ClueDefinition clue) return null;
                return AddEntry(clue.Title, () => ShowDetail(clue.Title, clue.Subtitle, clue.Description));
            }, ref newest);

            Button ignored = null;
            AddSection("notebook.items", "notebook.empty.items", fact =>
            {
                if (fact is not ItemDefinition item) return null;
                return AddEntry(item.Title, () => ShowDetail(item.Title, null, item.Description));
            }, ref ignored);

            if (newest != null)
            {
                Select(newest);
                newest.Focus();
            }
            else
            {
                detailBody.text = UIText.Get("notebook.select");
                closeButton.Focus();
            }
        }

        void AddSection(string headerKey, string emptyKey, Func<FactDefinition, Button> addEntry, ref Button first)
        {
            var header = new Label(UIText.Get(headerKey));
            header.AddToClassList("section-header");
            list.Add(header);

            var count = 0;
            for (var i = state.Facts.Count - 1; i >= 0; i--)
            {
                var entry = addEntry(state.Facts[i]);
                if (entry == null) continue;
                first ??= entry;
                count++;
            }

            if (count > 0) return;
            var empty = new Label(UIText.Get(emptyKey));
            empty.AddToClassList("notebook__empty");
            list.Add(empty);
        }

        Button AddEntry(UnityEngine.Localization.LocalizedString title, Action show)
        {
            var button = new Button { text = UIText.Get(title) };
            button.AddToClassList(EntryClass);
            button.clicked += () =>
            {
                Select(button);
                show();
            };
            button.userData = show;
            list.Add(button);
            return button;
        }

        void Select(Button button)
        {
            selected?.RemoveFromClassList(SelectedClass);
            selected = button;
            selected.AddToClassList(SelectedClass);
            (button.userData as Action)?.Invoke();
        }

        void ShowDetail(UnityEngine.Localization.LocalizedString title,
            UnityEngine.Localization.LocalizedString subtitle, UnityEngine.Localization.LocalizedString body)
        {
            detailTitle.text = UIText.Get(title);
            detailSubtitle.text = UIText.Get(subtitle);
            detailSubtitle.EnableInClassList(HiddenClass, string.IsNullOrEmpty(detailSubtitle.text));
            detailBody.text = UIText.Get(body);
        }
    }
}
