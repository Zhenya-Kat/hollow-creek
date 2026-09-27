using HollowCreek.Core.Facts;
using UnityEngine;
using UnityEngine.Localization;

namespace HollowCreek.Gameplay.Inspection
{
    /// <summary>
    /// Настройки осмотра предмета крупным планом: что показать, что игрок узнаёт и как можно вращать камеру.
    /// Камера смотрит на точку <see cref="Focus"/> со стороны её синей оси (forward).
    /// </summary>
    public sealed class Inspectable : MonoBehaviour
    {
        [Header("Текст")]
        [SerializeField] LocalizedString title;
        [SerializeField] LocalizedString description;
        [SerializeField, Tooltip("Улика, предмет или факт, который игрок получает, осмотрев объект. Необязательно.")]
        FactDefinition grants;

        [Header("Камера")]
        [SerializeField, Tooltip("Точка, на которую смотрит камера. Пусто — этот объект.")]
        Transform focus;
        [SerializeField] float distance = 1.2f;
        [SerializeField] Vector2 distanceRange = new(0.5f, 2.5f);
        [SerializeField, Tooltip("Насколько можно повернуть камеру влево/вправо (градусы)")]
        Vector2 yawRange = new(-60f, 60f);
        [SerializeField, Tooltip("Насколько можно повернуть камеру вниз/вверх (градусы)")]
        Vector2 pitchRange = new(-20f, 50f);

        public LocalizedString Title => title;
        public LocalizedString Description => description;
        public FactDefinition Grants => grants;
        public Transform Focus => focus != null ? focus : transform;
        public float Distance => distance;
        public Vector2 DistanceRange => distanceRange;
        public Vector2 YawRange => yawRange;
        public Vector2 PitchRange => pitchRange;

        void OnDrawGizmosSelected()
        {
            var f = Focus;
            Gizmos.color = Color.cyan;
            var cameraPosition = f.position + f.forward * distance;
            Gizmos.DrawWireSphere(cameraPosition, 0.05f);
            Gizmos.DrawLine(cameraPosition, f.position);
        }
    }
}
