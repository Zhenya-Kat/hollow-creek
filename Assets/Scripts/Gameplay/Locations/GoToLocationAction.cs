using System;
using HollowCreek.Core;
using HollowCreek.Core.Data;
using HollowCreek.Core.Logic;
using UnityEngine;

namespace HollowCreek.Gameplay.Locations
{
    [Serializable, SelectorLabel("Дверь между локациями общей сцены")]
    public sealed class SharedLocationDoorAction : GameAction
    {
        [SerializeField] LocationDefinition inside;
        [SerializeField] LocationDefinition outside;
        [SerializeField] string insideSpawn;
        [SerializeField] string outsideSpawn;

        public SharedLocationDoorAction() { }
        public SharedLocationDoorAction(LocationDefinition inside, LocationDefinition outside, string insideSpawn, string outsideSpawn)
        {
            this.inside = inside; this.outside = outside;
            this.insideSpawn = insideSpawn; this.outsideSpawn = outsideSpawn;
        }
        public override void Execute(in ActionContext context)
        {
            var loader = Services.Get<LocationLoader>();
            var leaving = loader.Current != null && loader.Current.Location == inside;
            loader.Go(leaving ? outside : inside, leaving ? outsideSpawn : insideSpawn);
        }
    }

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
