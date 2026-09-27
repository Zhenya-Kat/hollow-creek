using UnityEngine;
using UnityEngine.Localization;

namespace HollowCreek.Core.Data
{
    /// <summary>Локация игры: отдельная сцена, которая подгружается к постоянной сцене Bootstrap.</summary>
    [CreateAssetMenu(menuName = "Hollow Creek/Location", fileName = "Location")]
    public sealed class LocationDefinition : GameDefinition
    {
        [SerializeField, Tooltip("Название локации для интерфейса")]
        LocalizedString displayName;

        [SerializeField, HideInInspector] string sceneName;

        [Header("Звук")]
        [SerializeField, Tooltip("Фоновый звук локации (ветер, гул помещения)")]
        Audio.SoundCue ambience;
        [SerializeField, Tooltip("Звук шагов в этой локации")]
        Audio.SoundCue footsteps;

#if UNITY_EDITOR
        [SerializeField, Tooltip("Сцена локации. Должна быть добавлена в список сцен сборки.")]
        UnityEditor.SceneAsset scene;

        protected override void OnValidate()
        {
            base.OnValidate();
            sceneName = scene != null ? scene.name : string.Empty;
        }
#endif

        public LocalizedString DisplayName => displayName;
        public string SceneName => sceneName;
        public Audio.SoundCue Ambience => ambience;
        public Audio.SoundCue Footsteps => footsteps;
    }
}

