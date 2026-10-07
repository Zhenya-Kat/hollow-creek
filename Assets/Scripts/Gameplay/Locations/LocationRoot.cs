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
        [SerializeField] bool convertLegacySavedCoordinates;
        [SerializeField] Bounds legacySavedBounds;

        public LocationDefinition Location => location;

        public void ResolveSavedPose(ref Vector3 position, ref float yaw)
        {
            if (!convertLegacySavedCoordinates || !legacySavedBounds.Contains(position)) return;
            position = transform.TransformPoint(position);
            yaw += transform.eulerAngles.y;
        }

        public SpawnPoint FindSpawnPoint(string spawnId)
        {
            var points = GetComponentsInChildren<SpawnPoint>(true);
            if (points.Length == 0) return null;
            var wanted = string.IsNullOrEmpty(spawnId) ? SpawnPoint.DefaultId : spawnId;
            foreach (var point in points)
                if (point.GetComponentInParent<LocationRoot>() == this && point.Id == wanted) return point;
            Debug.LogWarning($"[Location] В «{name}» нет точки появления «{wanted}», используется первая.", this);
            foreach (var point in points)
                if (point.GetComponentInParent<LocationRoot>() == this) return point;
            return null;
        }

        public static LocationRoot FindIn(Scene scene, LocationDefinition location = null)
        {
            if (!scene.IsValid() || !scene.isLoaded) return null;
            foreach (var go in scene.GetRootGameObjects())
                foreach (var root in go.GetComponentsInChildren<LocationRoot>(true))
                    if (location == null || root.Location == location) return root;
            return null;
        }
    }
}
