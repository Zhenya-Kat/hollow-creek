using System;
using HollowCreek.Core;
using HollowCreek.Core.Logic;
using UnityEngine;

namespace HollowCreek.Gameplay.Inspection
{
    [Serializable, SelectorLabel("Осмотреть крупным планом")]
    public sealed class InspectAction : GameAction
    {
        [SerializeField, Tooltip("Что осматривать. Пусто — Inspectable на этом же объекте.")]
        Inspectable target;

        public override void Execute(in ActionContext context)
        {
            var inspectable = target != null ? target : context.Source.GetComponentInParent<Inspectable>();
            if (inspectable == null)
            {
                Debug.LogWarning("[Action] Нет компонента Inspectable для осмотра.", context.Source);
                return;
            }
            Services.Get<InspectionController>().Begin(inspectable);
        }
    }
}
