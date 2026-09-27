using System;
using System.Collections.Generic;

namespace HollowCreek.Core.Dialogue
{
    /// <summary>Какие вопросы игрок уже задавал. Нужен, чтобы отмечать заданные вопросы и сохранять это.</summary>
    public sealed class DialogueLog
    {
        readonly HashSet<string> asked = new();

        public event Action Changed;

        public IEnumerable<string> Entries => asked;

        public bool WasAsked(CharacterDefinition character, DialogueTopic topic) =>
            asked.Contains(Key(character, topic));

        public void MarkAsked(CharacterDefinition character, DialogueTopic topic)
        {
            if (asked.Add(Key(character, topic))) Changed?.Invoke();
        }

        /// <summary>Восстановление из сохранения.</summary>
        public void Load(IEnumerable<string> entries)
        {
            asked.Clear();
            if (entries != null) asked.UnionWith(entries);
            Changed?.Invoke();
        }

        public void Clear() => asked.Clear();

        static string Key(CharacterDefinition character, DialogueTopic topic) => character.Id + "/" + topic.Id;
    }
}
