using UnityEngine;
using UnityEngine.Localization;

namespace HollowCreek.Core.Facts
{
    /// <summary>Предмет в кармане героини (ключ, карандаш…): факт, который показывается в дневнике.</summary>
    [CreateAssetMenu(menuName = "Hollow Creek/Item", fileName = "Item", order = 2)]
    public sealed class ItemDefinition : FactDefinition
    {
        [SerializeField] LocalizedString title;
        [SerializeField] LocalizedString description;

        public LocalizedString Title => title;
        public LocalizedString Description => description;
    }
}
