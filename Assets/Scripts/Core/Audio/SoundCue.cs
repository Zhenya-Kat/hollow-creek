using UnityEngine;

namespace HollowCreek.Core.Audio
{
    /// <summary>К какой группе громкости относится звук (настраивается игроком).</summary>
    public enum SoundCategory
    {
        Effects,
        Interface,
        Ambience,
        Music,
    }

    /// <summary>
    /// Звук, который можно проиграть: один или несколько клипов (каждый раз выбирается случайный),
    /// громкость и небольшой разброс высоты тона, чтобы повторы не звучали одинаково.
    /// </summary>
    [CreateAssetMenu(menuName = "Hollow Creek/Audio/Sound Cue", fileName = "Sound", order = 50)]
    public sealed class SoundCue : ScriptableObject
    {
        [SerializeField] AudioClip[] clips = new AudioClip[0];
        [SerializeField, Range(0f, 1f)] float volume = 1f;
        [SerializeField, Tooltip("Случайная высота тона: от и до (1 — без изменений)")]
        Vector2 pitch = new(0.95f, 1.05f);
        [SerializeField] SoundCategory category = SoundCategory.Effects;

        public float Volume => volume;
        public SoundCategory Category => category;

        public AudioClip PickClip() => clips.Length == 0 ? null : clips[Random.Range(0, clips.Length)];

        public float PickPitch() => Random.Range(pitch.x, pitch.y);
    }
}
