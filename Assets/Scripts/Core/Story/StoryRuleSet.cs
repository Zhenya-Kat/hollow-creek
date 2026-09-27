using System;
using System.Collections.Generic;
using HollowCreek.Core.Data;
using HollowCreek.Core.Logic;
using UnityEngine;

namespace HollowCreek.Core.Story
{
    /// <summary>
    /// Сюжетные правила «когда — тогда»: как только условие выполнено, срабатывают действия.
    /// Например: есть алиби мэра и объявление об аварии → Нора звонит шерифу и получает улику.
    /// Действия правила должны делать его условие ложным (обычно условие содержит «Не → Есть факт X»,
    /// а действие выдаёт факт X) — так правило не сработает повторно, в том числе после загрузки.
    /// </summary>
    [CreateAssetMenu(menuName = "Hollow Creek/Story Rules", fileName = "StoryRules", order = 21)]
    public sealed class StoryRuleSet : ScriptableObject
    {
        [SerializeField] List<StoryRule> rules = new();

        public IReadOnlyList<StoryRule> Rules => rules;

#if UNITY_EDITOR
        void OnValidate()
        {
            if (StableId.EnsureUnique(rules)) UnityEditor.EditorUtility.SetDirty(this);
        }
#endif
    }

    [Serializable]
    public sealed class StoryRule : IHasStableId
    {
        [SerializeField, HideInInspector] string id;
        [SerializeField, Tooltip("Заметка для разработчика: что делает правило")]
        string note;
        [SerializeReference, SubclassSelector] Condition when;
        [SerializeReference, SubclassSelector] List<GameAction> actions = new();

        public string Id { get => id; set => id = value; }
        public string Note => note;
        public Condition When => when;
        public IReadOnlyList<GameAction> Actions => actions;
    }
}
