using HollowCreek.Core.Dialogue;
using UnityEngine;

namespace HollowCreek.Gameplay.Dialogue
{
    /// <summary>
    /// Персонаж в сцене. Связывает объект с его <see cref="CharacterDefinition"/>
    /// и умеет поворачиваться к собеседнику.
    /// </summary>
    public sealed class Npc : MonoBehaviour
    {
        [SerializeField] CharacterDefinition character;
        [SerializeField, Tooltip("Голова — на неё смотрит игрок во время разговора")]
        Transform head;
        [SerializeField, Tooltip("Поворачиваться к игроку во время разговора (сидящим за столом лучше выключить)")]
        bool turnToSpeaker = true;
        [SerializeField, Tooltip("Скорость поворота (градусы в секунду)")]
        float turnSpeed = 240f;
        [SerializeField, Tooltip("Аниматор модели. Необязательно: без него персонаж просто стоит.")]
        Animator animator;

        static readonly int TalkTrigger = Animator.StringToHash("Talk");

        float restYaw;
        float targetYaw;

        public CharacterDefinition Character => character;
        public Vector3 LookPoint => head != null ? head.position : transform.position + Vector3.up * 1.6f;

        void Awake()
        {
            restYaw = transform.eulerAngles.y;
            targetYaw = restYaw;
        }

        void Update()
        {
            var yaw = transform.eulerAngles.y;
            if (Mathf.Approximately(Mathf.DeltaAngle(yaw, targetYaw), 0f)) return;
            transform.rotation = Quaternion.Euler(0f, Mathf.MoveTowardsAngle(yaw, targetYaw, turnSpeed * Time.deltaTime), 0f);
        }

        public void FaceTowards(Vector3 worldPoint)
        {
            if (!turnToSpeaker) return;
            var flat = worldPoint - transform.position;
            flat.y = 0f;
            if (flat.sqrMagnitude > 0.0001f) targetYaw = Quaternion.LookRotation(flat).eulerAngles.y;
        }

        public void ReturnToRest() => targetYaw = restYaw;

        /// <summary>Начался разговор: повернуться к собеседнику и сделать жест.</summary>
        public void BeginConversation(Vector3 speakerPosition)
        {
            FaceTowards(speakerPosition);
            if (animator != null) animator.SetTrigger(TalkTrigger);
        }
    }
}
