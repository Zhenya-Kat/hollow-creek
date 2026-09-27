using System;
using HollowCreek.Core;
using HollowCreek.Core.Data;
using HollowCreek.Core.Logic;
using UnityEngine;

namespace HollowCreek.Gameplay.Locations
{
    /// <summary>Перейти в другую локацию — например, через дверь.</summary>
    [Serializable, SelectorLabel("Перейти в локацию")]
    public sealed class GoToLocationAction : GameAction
    {
        [SerializeField] LocationDefinition location;
        [SerializeField, Tooltip("Id точки появления в новой локации (SpawnPoint). Пусто — «default».")]
        string spawnId;

        public GoToLocationAction() { }

        public GoToLocationAction(LocationDefinition location, string spawnId)
        {
            this.location = location;
            this.spawnId = spawnId;
        }

        public override void Execute(in ActionContext context)
        {
            if (location == null)
            {
                Debug.LogWarning("[Action] Не указана локация.", context.Source);
                return;
            }
            Services.Get<LocationLoader>().Go(location, spawnId);
        }
    }
}
