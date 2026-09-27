using UnityEngine;
using UnityEngine.Localization;

namespace HollowCreek.Core.Facts
{
    /// <summary>Улика: факт, который попадает в дневник с названием и описанием.</summary>
    [CreateAssetMenu(menuName = "Hollow Creek/Clue", fileName = "Clue", order = 1)]
    public sealed class ClueDefinition : FactDefinition
    {
        [SerializeField] LocalizedString title;
        [SerializeField, Tooltip("Короткая подпись: где найдена или к кому относится")]
        LocalizedString subtitle;
        [SerializeField] LocalizedString description;

        public LocalizedString Title => title;
        public LocalizedString Subtitle => subtitle;
        public LocalizedString Description => description;
    }
}
