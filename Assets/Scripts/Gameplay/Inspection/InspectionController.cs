using System;
using HollowCreek.Core;
using HollowCreek.Core.State;
using HollowCreek.Gameplay.Input;
using HollowCreek.Gameplay.Modals;
using Unity.Cinemachine;
using UnityEngine;

namespace HollowCreek.Gameplay.Inspection
{
    /// <summary>
    /// Осмотр предмета: отдельная камера плавно подлетает к предмету, игрок вращает её вокруг и приближает.
    /// </summary>
    [DefaultExecutionOrder(-880)]
    public sealed class InspectionController : MonoBehaviour, IModal
    {
        [SerializeField, Tooltip("Камера осмотра. Приоритет выше, чем у камеры игрока; по умолчанию выключена.")]
        CinemachineCamera inspectCamera;
        [SerializeField, Tooltip("Градусов на пиксель мыши при вращении")]
        float mouseSensitivity = 0.25f;
        [SerializeField, Tooltip("Градусов в секунду при полном отклонении стика")]
        float gamepadSpeed = 120f;
        [SerializeField, Tooltip("Метров за один шаг колеса мыши")]
        float zoomStep = 0.15f;

        GameInput input;
        ModalStack modals;
        GameState state;
        float yaw, pitch, distance;

        public Inspectable Current { get; private set; }

        public event Action<Inspectable> Started;
        public event Action<Inspectable> Ended;

        InputMode IModal.InputMode => InputMode.Inspect;
        bool IModal.DefersMessages => true;

        void Awake()
        {
            input = Services.Get<GameInput>();
            modals = Services.Get<ModalStack>();
            state = Services.Get<GameState>();
            inspectCamera.gameObject.SetActive(false);
            Services.Register(this);
        }

        void OnDestroy() => Services.Unregister(this);

        public void Begin(Inspectable target)
        {
            if (target == null) throw new ArgumentNullException(nameof(target));
            if (Current != null) End();

            Current = target;
            yaw = 0f;
            pitch = 0f;
            distance = target.Distance;
            UpdateCamera();
            inspectCamera.gameObject.SetActive(true);
            modals.Push(this);

            if (Services.TryGet<Audio.AudioService>(out var audio)) audio.Play(audio.Cues != null ? audio.Cues.inspectStart : null);
            if (target.Grants != null) state.Grant(target.Grants);
            Started?.Invoke(target);
        }

        public void End()
        {
            if (Current == null) return;
            var finished = Current;
            Current = null;
            inspectCamera.gameObject.SetActive(false);
            modals.Remove(this);
            Ended?.Invoke(finished);
        }

        void IModal.OnBack() => End();

        void LateUpdate()
        {
            if (Current == null) return;
            // Предмет могли уничтожить или выключить во время осмотра.
            if (!Current || !Current.isActiveAndEnabled)
            {
                End();
                return;
            }

            if (modals.Top == (IModal)this) ReadInput();
            UpdateCamera();
        }

        void ReadInput()
        {
            var rotate = input.Rotate.ReadValue<Vector2>();
            Vector2 delta;
            if (GameInput.IsFromGamepad(input.Rotate)) delta = rotate * (gamepadSpeed * Time.deltaTime);
            else delta = input.Drag.IsPressed() ? rotate * mouseSensitivity : Vector2.zero;

            var range = Current.YawRange;
            yaw = Mathf.Clamp(yaw + delta.x, range.x, range.y);
            range = Current.PitchRange;
            pitch = Mathf.Clamp(pitch - delta.y, range.x, range.y);

            var zoom = input.Zoom.ReadValue<float>();
            if (GameInput.IsFromGamepad(input.Zoom)) zoom *= Time.deltaTime * 4f;
            range = Current.DistanceRange;
            distance = Mathf.Clamp(distance - zoom * zoomStep * 10f, range.x, range.y);
        }

        void UpdateCamera()
        {
            var focus = Current.Focus;
            // Камера стоит перед предметом (по его оси forward) и поворачивается вокруг него.
            var orbit = focus.rotation * Quaternion.Euler(pitch, 180f + yaw, 0f);
            var position = focus.position - orbit * Vector3.forward * distance;
            inspectCamera.transform.SetPositionAndRotation(position, Quaternion.LookRotation(focus.position - position));
        }
    }
}
