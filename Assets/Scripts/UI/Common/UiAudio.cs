using System;
using HollowCreek.Core;
using HollowCreek.Core.Audio;
using HollowCreek.Gameplay.Audio;

namespace HollowCreek.UI.Common
{
    /// <summary>Короткий доступ к общим звукам из интерфейса: <c>UiAudio.Play(c => c.buttonClick)</c>.</summary>
    public static class UiAudio
    {
        public static void Play(Func<AudioCues, SoundCue> pick)
        {
            if (!Services.TryGet<AudioService>(out var audio) || audio.Cues == null) return;
            audio.Play(pick(audio.Cues));
        }
    }
}
