using HollowCreek.Core;
using HollowCreek.Gameplay.Locations;
using HollowCreek.Gameplay.Player;
using UnityEngine;

namespace HollowCreek.Gameplay.Audio
{
    /// <summary>Шаги игрока: звук берётся из текущей локации (бетон на улице, дерево в доме).</summary>
    [RequireComponent(typeof(PlayerController))]
    public sealed class FootstepPlayer : MonoBehaviour
    {
        PlayerController player;
        LocationLoader locations;
        AudioService audioService;

        void Awake()
        {
            player = GetComponent<PlayerController>();
            locations = Services.Get<LocationLoader>();
        }

        void Start() => audioService = Services.Get<AudioService>();

        void OnEnable() => player.Stepped += OnStepped;
        void OnDisable() => player.Stepped -= OnStepped;

        void OnStepped()
        {
            var location = locations.Current;
            if (location != null) audioService.Play(location.Location.Footsteps);
        }
    }
}
