using System;
using System.Collections.Generic;
using HollowCreek.Core.Facts;
using HollowCreek.Core.State;
using UnityEngine.Localization;
using UnityEngine.UIElements;

namespace HollowCreek.UI.Common
{
    /// <summary>
    /// Список улик и предметов с панелью подробностей. Используется в дневнике,
    /// при предъявлении улики в разговоре и при обвинении.
    /// </summary>
    public sealed class FactListView
    {
        const string EntryClass = "notebook__entry";
        const string SelectedClass = "notebook__entry--selected";

        readonly ScrollView list;
        readonly Label detailTitle;
        readonly Label detailSubtitle;
        readonly Label detailBody;
        readonly Dictionary<FactDefinition, Button> entries = new();
        Button selectedButton;

        /// <summary>Игрок выбрал запись в списке.</summary>
        public event Action<FactDefinition> SelectionChanged;

        /// <summary>Игрок подтвердил запись клавишей Enter или кнопкой геймпада.</summary>
        public event Action<FactDefinition> Submitted;

        public FactDefinition Selected { get; private set; }

        public FactListView(VisualElement root)
        {
            list = root.Q<ScrollView>("list");
            detailTitle = root.Q<Label>("detail-title");
            detailSubtitle = root.Q<Label>("detail-subtitle");
            detailBody = root.Q<Label>("detail-body");
        }

        /// <summary>
        /// Заполняет список: улики (свежие сверху), затем, если нужно, предметы.
        /// Возвращает, есть ли хоть одна запись. Выбирается самая свежая улика.
        /// </summary>
        public bool Build(IGameStateReader state, bool includeItems, Predicate<FactDefinition> filter = null)
        {
            list.Clear();
            entries.Clear();
            selectedButton = null;
            Selected = null;
            ShowDetail(null, null, null);

            var first = AddSection(state, "notebook.clues", "notebook.empty.clues",
                f => f is ClueDefinition && (filter == null || filter(f)));
            var firstItem = includeItems
                ? AddSection(state, "notebook.items", "notebook.empty.items",
                    f => f is ItemDefinition && (filter == null || filter(f)))
                : null;

            var toSelect = first ?? firstItem;
            if (toSelect != null) Select(toSelect);
            else detailBody.text = UIText.Get("notebook.select");
            return toSelect != null;
        }

        public void FocusSelection()
        {
            if (Selected != null && entries.TryGetValue(Selected, out var button)) button.Focus();
        }

        FactDefinition AddSection(IGameStateReader state, string headerKey, string emptyKey, Predicate<FactDefinition> accept)
        {
            var header = new Label(UIText.Get(headerKey));
            header.AddToClassList("section-header");
            list.Add(header);

            FactDefinition first = null;
            for (var i = state.Facts.Count - 1; i >= 0; i--)
            {
                var fact = state.Facts[i];
                if (!accept(fact)) continue;
                AddEntry(fact);
                first ??= fact;
            }

            if (first == null)
            {
                var empty = new Label(UIText.Get(emptyKey));
                empty.AddToClassList("notebook__empty");
                list.Add(empty);
            }
            return first;
        }

        void AddEntry(FactDefinition fact)
        {
            var button = new Button { text = UIText.Get(TitleOf(fact)) };
            button.AddToClassList(EntryClass);
            button.clicked += () => Select(fact);
            // Геймпад/клавиатура: выбор следует за фокусом, подтверждение — Enter/«A».
            button.RegisterCallback<FocusInEvent>(_ => Select(fact));
            button.RegisterCallback<NavigationSubmitEvent>(_ => Submitted?.Invoke(fact));
            list.Add(button);
            entries[fact] = button;
        }

        public void Select(FactDefinition fact)
        {
            if (Selected == fact || !entries.TryGetValue(fact, out var button)) return;
            selectedButton?.RemoveFromClassList(SelectedClass);
            selectedButton = button;
            selectedButton.AddToClassList(SelectedClass);
            Selected = fact;
            switch (fact)
            {
                case ClueDefinition clue: ShowDetail(clue.Title, clue.Subtitle, clue.Description); break;
                case ItemDefinition item: ShowDetail(item.Title, null, item.Description); break;
            }
            SelectionChanged?.Invoke(fact);
        }

        static LocalizedString TitleOf(FactDefinition fact) => fact switch
        {
            ClueDefinition clue => clue.Title,
            ItemDefinition item => item.Title,
            _ => null,
        };

        void ShowDetail(LocalizedString title, LocalizedString subtitle, LocalizedString body)
        {
            detailTitle.text = UIText.Get(title);
            detailSubtitle.text = UIText.Get(subtitle);
            detailSubtitle.EnableInClassList(ScreenView.HiddenClass, string.IsNullOrEmpty(detailSubtitle.text));
            detailBody.text = UIText.Get(body);
        }
    }
}
