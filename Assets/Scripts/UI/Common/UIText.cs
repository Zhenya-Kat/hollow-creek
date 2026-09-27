using System;
using System.Collections.Generic;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.UIElements;

namespace HollowCreek.UI.Common
{
    /// <summary>Тексты интерфейса из таблицы локализации «UI».</summary>
    public static class UIText
    {
        public const string Table = "UI";

        public static string Get(string key) =>
            LocalizationSettings.StringDatabase.GetLocalizedString(Table, key);

        public static string Format(string key, params object[] args) => string.Format(Get(key), args);

        public static string Get(LocalizedString text) =>
            text == null || text.IsEmpty ? string.Empty : text.GetLocalizedString();

        /// <summary>
        /// Понятное игроку название клавиши: «LMB» → «ЛКМ». Перевод берётся из записи «key.&lt;имя&gt;»,
        /// если её нет — показывается как есть.
        /// </summary>
        public static string KeyName(string inputSystemName)
        {
            if (string.IsNullOrEmpty(inputSystemName)) return string.Empty;
            var entry = LocalizationSettings.StringDatabase.GetTable(Table)?.GetEntry("key." + inputSystemName);
            return entry != null ? entry.GetLocalizedString() : inputSystemName;
        }
    }

    /// <summary>
    /// Переводит статичные тексты в разметке: у элемента пишется text="@ключ",
    /// и он заменяется значением из таблицы «UI». При смене языка тексты обновляются.
    /// </summary>
    public sealed class StaticTextLocalizer : IDisposable
    {
        readonly List<(TextElement element, string key)> entries = new();

        public StaticTextLocalizer(VisualElement root)
        {
            root.Query<TextElement>().ForEach(element =>
            {
                if (element.text is { Length: > 1 } text && text[0] == '@')
                    entries.Add((element, text.Substring(1)));
            });
            Apply();
            LocalizationSettings.SelectedLocaleChanged += OnLocaleChanged;
        }

        public void Dispose() => LocalizationSettings.SelectedLocaleChanged -= OnLocaleChanged;

        void OnLocaleChanged(Locale _) => Apply();

        void Apply()
        {
            foreach (var (element, key) in entries)
                element.text = UIText.Get(key);
        }
    }
}
