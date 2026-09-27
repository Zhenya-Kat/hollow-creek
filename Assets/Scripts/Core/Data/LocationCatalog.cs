using System.Collections.Generic;
using UnityEngine;

namespace HollowCreek.Core.Data
{
    /// <summary>Список всех локаций игры. Нужен, чтобы найти локацию по Id (при загрузке сохранения) или по сцене.</summary>
    [CreateAssetMenu(menuName = "Hollow Creek/Location Catalog", fileName = "LocationCatalog")]
    public sealed class LocationCatalog : ScriptableObject
    {
        [SerializeField] List<LocationDefinition> locations = new();

        public IReadOnlyList<LocationDefinition> Locations => locations;

        public LocationDefinition FindById(string id) =>
            string.IsNullOrEmpty(id) ? null : locations.Find(l => l != null && l.Id == id);

        public LocationDefinition FindBySceneName(string sceneName) =>
            string.IsNullOrEmpty(sceneName) ? null : locations.Find(l => l != null && l.SceneName == sceneName);
    }
}
