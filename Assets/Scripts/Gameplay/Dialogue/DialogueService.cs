using System;
using HollowCreek.Core;
using HollowCreek.Core.Dialogue;
using HollowCreek.Core.State;
using HollowCreek.Gameplay.Player;
using UnityEngine;

namespace HollowCreek.Gameplay.Dialogue
{
    /// <summary>
    /// Начинает и заканчивает разговоры. Экран разговора (слой интерфейса) подписывается на события
    /// <see cref="Started"/> и <see cref="Ended"/>.
    /// </summary>
    [DefaultExecutionOrder(-870)]
    public sealed class DialogueService : MonoBehaviour
    {
        [SerializeField, Tooltip("На сколько метров ниже лица собеседника смотрит камера в разговоре")]
        float FramingDrop = 0.35f;

        GameState state;
        readonly DialogueLog log = new();
        Npc currentNpc;

        public Conversation Current { get; private set; }
        public DialogueLog Log => log;

        public event Action<Conversation> Started;
        public event Action<Conversation> Ended;

        void Awake()
        {
            state = Services.Get<GameState>();
            Services.Register(this);
            Services.Register(log);
        }

        void OnDestroy()
        {
            Services.Unregister(this);
            Services.Unregister(log);
        }

        public void Begin(CharacterDefinition character, Npc npc = null)
        {
            if (character == null) throw new ArgumentNullException(nameof(character));
            if (Current != null) End();

            var firstMeeting = character.MetFact != null && !state.Has(character.MetFact);
            Current = new Conversation(character, state, log, npc != null ? npc.gameObject : null, firstMeeting);
            currentNpc = npc;

            if (npc != null && Services.TryGet<PlayerController>(out var player))
            {
                // Смотрим чуть ниже лица: так голова собеседника оказывается над панелью разговора.
                player.FaceTowards(npc.LookPoint + Vector3.down * FramingDrop);
                npc.FaceTowards(player.EyePosition);
            }

            Started?.Invoke(Current);
            // Факт знакомства выдаём после начала разговора, чтобы уведомления шли в правильном порядке.
            if (character.MetFact != null) state.Grant(character.MetFact);
        }

        public void End()
        {
            if (Current == null) return;
            var finished = Current;
            Current = null;
            if (currentNpc != null) currentNpc.ReturnToRest();
            currentNpc = null;
            Ended?.Invoke(finished);
        }
    }
}
