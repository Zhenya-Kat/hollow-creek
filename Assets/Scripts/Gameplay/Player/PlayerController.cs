using HollowCreek.Core;
using HollowCreek.Gameplay.Input;
using HollowCreek.Gameplay.Locations;
using Unity.Cinemachine;
using UnityEngine;

namespace HollowCreek.Gameplay.Player
{
    /// <summary>Ходьба от первого лица: WASD/стик — движение, мышь/правый стик — обзор.</summary>
    [RequireComponent(typeof(CharacterController))]
    public sealed class PlayerController : MonoBehaviour
    {
        [Header("Ссылки")]
        [SerializeField, Tooltip("Голова: вращается вверх-вниз, к ней прикреплена камера")]
        Transform head;
        [SerializeField, Tooltip("Камера от первого лица — для настройки поля зрения")]
        CinemachineCamera firstPersonCamera;

        [Header("Движение")]
        [SerializeField] float walkSpeed = 3.0f;
        [SerializeField] float sprintSpeed = 5.0f;
        [SerializeField, Tooltip("Как быстро набирается и сбрасывается скорость")]
        float acceleration = 14f;
        [SerializeField] float gravity = -20f;
        [SerializeField, Tooltip("Длина шага (м) — через столько метров звучит шаг")]
        float stepLength = 0.8f;

        [Header("Обзор")]
        [SerializeField, Tooltip("Градусов на пиксель движения мыши")]
        float mouseSensitivity = 0.1f;
        [SerializeField, Tooltip("Градусов в секунду при полном отклонении стика")]
        float gamepadLookSpeed = 160f;
        [SerializeField] float minPitch = -80f;
        [SerializeField] float maxPitch = 80f;

        CharacterController body;
        GameInput input;
        LocationLoader locations;
        Vector3 horizontalVelocity;
        float verticalVelocity;
        float pitch;
        float distanceSinceStep;

        /// <summary>Игрок сделал шаг (для звука шагов).</summary>
        public event System.Action Stepped;

        /// <summary>Градусов поворота на пиксель движения мыши (настройка игрока).</summary>
        public float MouseSensitivity
        {
            get => mouseSensitivity;
            set => mouseSensitivity = Mathf.Clamp(value, 0.02f, 0.5f);
        }

        /// <summary>Инвертировать вертикальный обзор.</summary>
        public bool InvertY { get; set; }

        /// <summary>Поле зрения камеры от первого лица (градусы по вертикали).</summary>
        public float FieldOfView
        {
            get => firstPersonCamera != null ? firstPersonCamera.Lens.FieldOfView : 0f;
            set
            {
                if (firstPersonCamera == null) return;
                var lens = firstPersonCamera.Lens;
                lens.FieldOfView = Mathf.Clamp(value, 50f, 90f);
                firstPersonCamera.Lens = lens;
            }
        }

        /// <summary>Точка глаз игрока — на неё смотрят собеседники.</summary>
        public Vector3 EyePosition => head.position;

        void Awake()
        {
            Services.Register(this);
            body = GetComponent<CharacterController>();
            input = Services.Get<GameInput>();
            locations = Services.Get<LocationLoader>();
        }

        void OnDestroy() => Services.Unregister(this);

        void OnEnable() => locations.LocationLoaded += OnLocationLoaded;
        void OnDisable() => locations.LocationLoaded -= OnLocationLoaded;

        // Плавный поворот к точке (например, к собеседнику), пока игрок не управляет обзором.
        float turnFromYaw, turnToYaw, turnFromPitch, turnToPitch, turnTime, turnDuration;

        void Update()
        {
            var controllable = input.Mode == InputMode.Gameplay && !locations.IsLoading;
            if (turnTime < turnDuration) UpdateTurn();
            else if (controllable) UpdateLook();
            UpdateMovement(controllable);
        }

        /// <summary>Плавно повернуть голову и тело так, чтобы смотреть на точку.</summary>
        public void FaceTowards(Vector3 worldPoint, float duration = 0.4f)
        {
            var toTarget = worldPoint - head.position;
            var flat = new Vector3(toTarget.x, 0f, toTarget.z);
            if (flat.sqrMagnitude < 0.0001f) return;

            turnFromYaw = transform.eulerAngles.y;
            turnToYaw = turnFromYaw + Mathf.DeltaAngle(turnFromYaw, Quaternion.LookRotation(flat).eulerAngles.y);
            turnFromPitch = pitch;
            turnToPitch = Mathf.Clamp(-Mathf.Atan2(toTarget.y, flat.magnitude) * Mathf.Rad2Deg, minPitch, maxPitch);
            turnTime = 0f;
            turnDuration = Mathf.Max(0.01f, duration);
        }

        void UpdateTurn()
        {
            turnTime += Time.deltaTime;
            var t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(turnTime / turnDuration));
            transform.rotation = Quaternion.Euler(0f, Mathf.Lerp(turnFromYaw, turnToYaw, t), 0f);
            pitch = Mathf.Lerp(turnFromPitch, turnToPitch, t);
            head.localRotation = Quaternion.Euler(pitch, 0f, 0f);
        }

        /// <summary>Поворот тела (градусы вокруг вертикали).</summary>
        public float Yaw => transform.eulerAngles.y;

        /// <summary>Наклон головы: отрицательный — вверх, положительный — вниз.</summary>
        public float Pitch => pitch;

        /// <summary>Мгновенно переместить игрока (вход в локацию, загрузка сохранения).</summary>
        public void Teleport(Vector3 position, float yaw, float headPitch = 0f)
        {
            // CharacterController перезаписывает позицию, пока включён.
            body.enabled = false;
            transform.SetPositionAndRotation(position, Quaternion.Euler(0f, yaw, 0f));
            body.enabled = true;
            pitch = Mathf.Clamp(headPitch, minPitch, maxPitch);
            head.localRotation = Quaternion.Euler(pitch, 0f, 0f);
            turnDuration = 0f;
            horizontalVelocity = Vector3.zero;
            verticalVelocity = 0f;
        }

        void UpdateLook()
        {
            var look = input.Look.ReadValue<Vector2>();
            var degrees = GameInput.IsFromGamepad(input.Look)
                ? look * (gamepadLookSpeed * Time.deltaTime)
                : look * mouseSensitivity;

            transform.Rotate(0f, degrees.x, 0f);
            pitch = Mathf.Clamp(pitch + (InvertY ? degrees.y : -degrees.y), minPitch, maxPitch);
            head.localRotation = Quaternion.Euler(pitch, 0f, 0f);
        }

        void UpdateMovement(bool controllable)
        {
            var move = controllable ? input.Move.ReadValue<Vector2>() : Vector2.zero;
            var speed = controllable && input.Sprint.IsPressed() ? sprintSpeed : walkSpeed;
            var target = (transform.right * move.x + transform.forward * move.y) * speed;
            horizontalVelocity = Vector3.MoveTowards(horizontalVelocity, target, acceleration * Time.deltaTime);

            verticalVelocity = body.isGrounded ? -1f : verticalVelocity + gravity * Time.deltaTime;
            var before = transform.position;
            body.Move((horizontalVelocity + Vector3.up * verticalVelocity) * Time.deltaTime);

            // Шаги считаем по реально пройденному расстоянию: упёрся в стену — шагов нет.
            var moved = transform.position - before;
            moved.y = 0f;
            if (!body.isGrounded) return;
            distanceSinceStep += moved.magnitude;
            var stride = stepLength * (horizontalVelocity.magnitude > walkSpeed + 0.1f ? 1.35f : 1f);
            if (distanceSinceStep < stride) return;
            distanceSinceStep = 0f;
            Stepped?.Invoke();
        }

        void OnLocationLoaded(LocationRoot location, SpawnPoint spawn)
        {
            if (spawn != null) Teleport(spawn.transform.position, spawn.transform.eulerAngles.y);
        }
    }
}
