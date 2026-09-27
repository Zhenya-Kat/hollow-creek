using System;
using HollowCreek.Core;
using HollowCreek.Core.Audio;
using HollowCreek.Core.Logic;
using UnityEngine;

namespace HollowCreek.Gameplay.Audio
{
    [Serializable, SelectorLabel("Проиграть звук")]
    public sealed class PlaySoundAction : GameAction
    {
        [SerializeField] SoundCue sound;

        public PlaySoundAction() { }
        public PlaySoundAction(SoundCue sound) => this.sound = sound;

        public override void Execute(in ActionContext context)
        {
            if (Services.TryGet<AudioService>(out var audio)) audio.Play(sound);
        }
    }
}
