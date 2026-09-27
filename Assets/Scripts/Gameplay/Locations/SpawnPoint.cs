using UnityEngine;

namespace HollowCreek.Gameplay.Locations
{
    /// <summary>Точка, в которой появляется игрок при входе в локацию (например, «от двери дома»).</summary>
    public sealed class SpawnPoint : MonoBehaviour
    {
        public const string DefaultId = "default";

        [SerializeField, Tooltip("Идентификатор точки. Двери ссылаются на него, чтобы игрок появился в нужном месте.")]
        string id = DefaultId;

        public string Id => id;

        void OnDrawGizmos()
        {
            Gizmos.color = new Color(0.3f, 0.9f, 0.4f);
            var t = transform;
            Gizmos.DrawWireSphere(t.position + Vector3.up * 0.9f, 0.3f);
            Gizmos.DrawLine(t.position, t.position + Vector3.up * 1.8f);
            Gizmos.DrawLine(t.position + Vector3.up * 1.6f, t.position + Vector3.up * 1.6f + t.forward * 0.6f);
        }
    }
}
