using System;
using HollowCreek.Core;
using HollowCreek.Core.Audio;
using HollowCreek.Gameplay.Audio;
using HollowCreek.Gameplay.Player;
using UnityEngine;

namespace HollowCreek.Gameplay.Settings
{
    /// <summary>Настройки игрока. Хранятся отдельно от сохранения игры и общие для всех прохождений.</summary>
    [Serializable]
    public sealed class GameSettings
    {
        public float masterVolume = 0.9f;
        public float effectsVolume = 1f;
        public float interfaceVolume = 0.8f;
        public float ambienceVolume = 0.8f;
        public float mouseSensitivity = 0.1f;
        public bool invertY;
        public float fieldOfView = 65f;
        public bool fullscreen = true;
        /// <summary>Индекс уровня качества в Project Settings → Quality.</summary>
        public int quality = -1;
    }

    /// <summary>Загружает, применяет и сохраняет <see cref="GameSettings"/>.</summary>
    [DefaultExecutionOrder(-820)]
    public sealed class SettingsService : MonoBehaviour
    {
        const string Key = "HollowCreek.Settings";

        public GameSettings Current { get; private set; }

        public event Action Changed;

        void Awake()
        {
            Current = Load();
            if (Current.quality < 0) Current.quality = QualitySettings.GetQualityLevel();
            Services.Register(this);
        }

        // Звук и игрок регистрируются в своих Awake — применяем настройки, когда все на месте.
        void Start() => Apply();

        void OnDestroy() => Services.Unregister(this);

        public void Apply()
        {
            var s = Current;
            if (Services.TryGet<AudioService>(out var audio))
            {
                audio.MasterVolume = s.masterVolume;
                audio.SetCategoryVolume(SoundCategory.Effects, s.effectsVolume);
                audio.SetCategoryVolume(SoundCategory.Interface, s.interfaceVolume);
                audio.SetCategoryVolume(SoundCategory.Ambience, s.ambienceVolume);
            }
            if (Services.TryGet<PlayerController>(out var player))
            {
                player.MouseSensitivity = s.mouseSensitivity;
                player.InvertY = s.invertY;
                player.FieldOfView = s.fieldOfView;
            }
            if (!Application.isEditor)
            {
                Screen.fullScreenMode = s.fullscreen ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed;
                if (s.quality >= 0 && s.quality < QualitySettings.names.Length && QualitySettings.GetQualityLevel() != s.quality)
                    QualitySettings.SetQualityLevel(s.quality, true);
            }
            Changed?.Invoke();
        }

        public void Save()
        {
            PlayerPrefs.SetString(Key, JsonUtility.ToJson(Current));
            PlayerPrefs.Save();
        }

        public void ResetToDefaults()
        {
            Current = new GameSettings { quality = QualitySettings.GetQualityLevel() };
            Apply();
        }

        static GameSettings Load()
        {
            var json = PlayerPrefs.GetString(Key, string.Empty);
            if (string.IsNullOrEmpty(json)) return new GameSettings();
            try
            {
                return JsonUtility.FromJson<GameSettings>(json) ?? new GameSettings();
            }
            catch (ArgumentException)
            {
                return new GameSettings();
            }
        }
    }
}
