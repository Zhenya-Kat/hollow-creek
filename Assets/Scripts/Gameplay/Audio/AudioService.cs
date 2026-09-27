using System;
using HollowCreek.Core;
using HollowCreek.Core.Audio;
using HollowCreek.Gameplay.Locations;
using UnityEngine;

namespace HollowCreek.Gameplay.Audio
{
    /// <summary>
    /// Весь звук игры: короткие звуки из пула источников, фоновый звук локации с плавной сменой,
    /// громкость по категориям (для настроек).
    /// </summary>
    [DefaultExecutionOrder(-880)]
    public sealed class AudioService : MonoBehaviour
    {
        [SerializeField] AudioCues cues;
        [SerializeField, Tooltip("Сколько коротких звуков может звучать одновременно")]
        int voices = 12;
        [SerializeField, Tooltip("За сколько секунд сменяется фон при переходе в другую локацию")]
        float ambienceFade = 1.5f;

        readonly float[] categoryVolume = { 1f, 1f, 1f, 1f };
        AudioSource[] pool;
        int nextVoice;
        AudioSource ambienceCurrent, ambienceNext;
        SoundCue ambienceCue;
        float fadeProgress = 1f;
        float fadingOutFrom;
        LocationLoader locations;

        public AudioCues Cues => cues;

        /// <summary>Громкость всей игры (0…1).</summary>
        public float MasterVolume
        {
            get => AudioListener.volume;
            set => AudioListener.volume = Mathf.Clamp01(value);
        }

        void Awake()
        {
            pool = new AudioSource[voices];
            for (var i = 0; i < voices; i++) pool[i] = CreateSource("Voice " + i, loop: false);
            ambienceCurrent = CreateSource("Ambience A", loop: true);
            ambienceNext = CreateSource("Ambience B", loop: true);
            locations = Services.Get<LocationLoader>();
            locations.LocationLoaded += OnLocationLoaded;
            Services.Register(this);
        }

        void OnDestroy()
        {
            locations.LocationLoaded -= OnLocationLoaded;
            Services.Unregister(this);
        }

        public float GetCategoryVolume(SoundCategory category) => categoryVolume[(int)category];

        public void SetCategoryVolume(SoundCategory category, float volume)
        {
            categoryVolume[(int)category] = Mathf.Clamp01(volume);
            if (category == SoundCategory.Ambience) ApplyAmbienceVolume();
        }

        /// <summary>Проиграть короткий звук (без позиции в мире).</summary>
        public void Play(SoundCue cue, float volumeScale = 1f)
        {
            if (cue == null) return;
            var clip = cue.PickClip();
            if (clip == null) return;
            var source = pool[nextVoice];
            nextVoice = (nextVoice + 1) % pool.Length;
            source.clip = clip;
            source.pitch = cue.PickPitch();
            source.volume = cue.Volume * volumeScale * categoryVolume[(int)cue.Category];
            source.Play();
        }

        /// <summary>Сменить фоновый звук (плавно). null — тишина.</summary>
        public void SetAmbience(SoundCue cue)
        {
            if (cue == ambienceCue) return;
            ambienceCue = cue;
            (ambienceCurrent, ambienceNext) = (ambienceNext, ambienceCurrent);
            fadingOutFrom = ambienceNext.volume;
            var clip = cue != null ? cue.PickClip() : null;
            ambienceCurrent.clip = clip;
            ambienceCurrent.volume = 0f;
            if (clip != null)
            {
                // Случайная точка старта — чтобы фон не начинался каждый раз одинаково.
                ambienceCurrent.time = UnityEngine.Random.Range(0f, clip.length * 0.9f);
                ambienceCurrent.Play();
            }
            fadeProgress = 0f;
        }

        void Update()
        {
            if (fadeProgress >= 1f) return;
            fadeProgress = Mathf.MoveTowards(fadeProgress, 1f, Time.unscaledDeltaTime / Mathf.Max(0.01f, ambienceFade));
            ApplyAmbienceVolume();
            if (fadeProgress >= 1f) ambienceNext.Stop();
        }

        void ApplyAmbienceVolume()
        {
            var target = ambienceCue != null ? ambienceCue.Volume * categoryVolume[(int)SoundCategory.Ambience] : 0f;
            ambienceCurrent.volume = target * fadeProgress;
            ambienceNext.volume = fadingOutFrom * (1f - fadeProgress);
        }

        void OnLocationLoaded(LocationRoot root, SpawnPoint _) => SetAmbience(root.Location.Ambience);

        AudioSource CreateSource(string sourceName, bool loop)
        {
            var go = new GameObject(sourceName);
            go.transform.SetParent(transform, false);
            var source = go.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.loop = loop;
            source.spatialBlend = 0f;
            return source;
        }
    }
}
