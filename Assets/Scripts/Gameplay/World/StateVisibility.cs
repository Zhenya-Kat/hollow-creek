using System.Collections.Generic;
using HollowCreek.Core;
using HollowCreek.Core.Facts;
using HollowCreek.Core.Logic;
using HollowCreek.Core.State;
using UnityEngine;

namespace HollowCreek.Gameplay.World
{
    /// <summary>
    /// Показывает объекты, только пока выполнено условие. Например, карандаш на столе исчезает,
    /// когда игрок его взял, а открытая дверца сейфа появляется после решения головоломки.
    /// </summary>
    public sealed class StateVisibility : MonoBehaviour
    {
        [SerializeReference, SubclassSelector, Tooltip("Когда объекты видны. Пусто — всегда.")]
        Condition visibleWhen;

        [SerializeField, Tooltip("Какие объекты показывать и скрывать. Не добавляйте сюда сам этот объект.")]
        List<GameObject> targets = new();

        GameState state;

        void Start()
        {
            state = Services.Get<GameState>();
            state.FactGranted += OnFactGranted;
            state.Restored += Refresh;
            Refresh();
        }

        void OnDestroy()
        {
            if (state == null) return;
            state.FactGranted -= OnFactGranted;
            state.Restored -= Refresh;
        }

        void OnFactGranted(FactDefinition _) => Refresh();

        void Refresh()
        {
            var visible = Condition.IsMet(visibleWhen, state);
            foreach (var target in targets)
                if (target != null && target != gameObject) target.SetActive(visible);
        }
    }
}
