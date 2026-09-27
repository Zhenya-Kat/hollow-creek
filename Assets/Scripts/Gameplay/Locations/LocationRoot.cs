using HollowCreek.Core.Data;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace HollowCreek.Gameplay.Locations
{
    /// <summary>
    /// Корневой объект сцены-локации. Связывает сцену с её <see cref="LocationDefinition"/>
    /// и знает точки появления игрока. В каждой сцене-локации должен быть ровно один.
    /// </summary>
    public sealed class LocationRoot : MonoBehaviour
    {
        [SerializeField] LocationDefinition location;

        public LocationDefinition Location => location;

        public SpawnPoint FindSpawnPoint(string spawnId)
        {
            var points = GetComponentsInChildren<SpawnPoint>(true);
            if (points.Length == 0) return null;
            var wanted = string.IsNullOrEmpty(spawnId) ? SpawnPoint.DefaultId : spawnId;
            foreach (var point in points)
                if (point.Id == wanted) return point;
            Debug.LogWarning($"[Location] В «{name}» нет точки появления «{wanted}», используется первая.", this);
            return points[0];
        }

        public static LocationRoot FindIn(Scene scene)
        {
            if (!scene.IsValid() || !scene.isLoaded) return null;
            foreach (var go in scene.GetRootGameObjects())
                if (go.TryGetComponent(out LocationRoot root)) return root;
            return null;
        }
    }
}
