using UnityEngine;

namespace HollowCreek.Core.Audio
{
    /// <summary>
    /// Общие звуки игры в одном месте: интерфейс, находки, головоломки.
    /// Звуки конкретных объектов (двери, ящики) задаются действием «Проиграть звук» на самих объектах.
    /// </summary>
    [CreateAssetMenu(menuName = "Hollow Creek/Audio/Audio Cues", fileName = "AudioCues", order = 51)]
    public sealed class AudioCues : ScriptableObject
    {
        [Header("Интерфейс")]
        public SoundCue buttonClick;
        public SoundCue screenOpen;
        public SoundCue screenClose;
        public SoundCue notebookOpen;
        public SoundCue notebookClose;

        [Header("Находки")]
        public SoundCue clueFound;
        public SoundCue itemFound;
        public SoundCue inspectStart;

        [Header("Головоломки и обвинение")]
        public SoundCue keypadPress;
        public SoundCue wrong;
        public SoundCue unlock;
        public SoundCue puzzleSolved;
        public SoundCue photoSwap;
        public SoundCue pencilScratch;
    }
}
