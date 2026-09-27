using System;
using HollowCreek.Core;
using HollowCreek.Core.State;
using HollowCreek.Gameplay.Input;
using HollowCreek.Gameplay.Locations;
using UnityEngine;

namespace HollowCreek.Gameplay.Interaction
{
    /// <summary>Находит объект под прицелом и запускает взаимодействие по нажатию кнопки.</summary>
    [DefaultExecutionOrder(-100)]
    public sealed class Interactor : MonoBehaviour
    {
        [SerializeField, Tooltip("Откуда смотрим — обычно голова игрока")]
        Transform viewOrigin;
        [SerializeField, Tooltip("Дотянуться можно до объектов не дальше этого расстояния (м)")]
        float reach = 2.6f;
        [SerializeField, Tooltip("Слои, которые перекрывают и содержат интерактивные объекты. Слой игрока исключить.")]
        LayerMask layers = ~0;

        GameInput input;
        GameState state;
        LocationLoader locations;

        /// <summary>Объект под прицелом (null — ничего).</summary>
        public Interactable Current { get; private set; }

        public event Action<Interactable> FocusChanged;

        void Awake()
        {
            input = Services.Get<GameInput>();
            state = Services.Get<GameState>();
            locations = Services.Get<LocationLoader>();
            Services.Register(this);
        }

        void OnDestroy() => Services.Unregister(this);

        void Update()
        {
            if (input.Mode != InputMode.Gameplay || locations.IsLoading)
            {
                SetCurrent(null);
                return;
            }

            SetCurrent(FindTarget());
            if (Current != null && input.Interact.WasPressedThisFrame())
                Current.Interact();
        }

        Interactable FindTarget()
        {
            var ray = new Ray(viewOrigin.position, viewOrigin.forward);
            if (!Physics.Raycast(ray, out var hit, reach, layers, QueryTriggerInteraction.Collide)) return null;
            var target = hit.collider.GetComponentInParent<Interactable>();
            return target != null && target.isActiveAndEnabled && target.IsAvailable(state) ? target : null;
        }

        void SetCurrent(Interactable target)
        {
            if (Current == target) return;
            Current = target;
            FocusChanged?.Invoke(target);
        }
    }
}
