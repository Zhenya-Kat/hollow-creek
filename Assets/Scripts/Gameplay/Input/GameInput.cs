using System;
using HollowCreek.Core;
using UnityEngine;
using UnityEngine.InputSystem;

namespace HollowCreek.Gameplay.Input
{
    /// <summary>Что сейчас делает игрок — от этого зависит, какие кнопки работают и виден ли курсор.</summary>
    public enum InputMode
    {
        /// <summary>Ходьба и взаимодействие. Курсор скрыт и захвачен.</summary>
        Gameplay,
        /// <summary>Осмотр предмета крупным планом. Курсор виден.</summary>
        Inspect,
        /// <summary>Открыт экран интерфейса (дневник, сообщение…). Курсор виден.</summary>
        UI,
    }

    /// <summary>
    /// Единая точка доступа к управлению. Включает нужные наборы действий под текущий <see cref="InputMode"/>.
    /// Режим переключает <see cref="Modals.ModalStack"/> — другим системам не нужно трогать курсор самим.
    /// </summary>
    [DefaultExecutionOrder(-900)]
    public sealed class GameInput : MonoBehaviour
    {
        public const string KeyboardMouseScheme = "Keyboard&Mouse";
        public const string GamepadScheme = "Gamepad";

        [SerializeField] InputActionAsset actions;

        InputActionMap player, inspect, global, ui;

        public InputMode Mode { get; private set; } = InputMode.Gameplay;

        public InputAction Move { get; private set; }
        public InputAction Look { get; private set; }
        public InputAction Interact { get; private set; }
        public InputAction Sprint { get; private set; }
        public InputAction Rotate { get; private set; }
        public InputAction Drag { get; private set; }
        public InputAction Zoom { get; private set; }
        public InputAction Back { get; private set; }
        public InputAction Notebook { get; private set; }

        void Awake()
        {
            player = actions.FindActionMap("Player", true);
            inspect = actions.FindActionMap("Inspect", true);
            global = actions.FindActionMap("Global", true);
            ui = actions.FindActionMap("UI", true);

            Move = player.FindAction("Move", true);
            Look = player.FindAction("Look", true);
            Interact = player.FindAction("Interact", true);
            Sprint = player.FindAction("Sprint", true);
            Rotate = inspect.FindAction("Rotate", true);
            Drag = inspect.FindAction("Drag", true);
            Zoom = inspect.FindAction("Zoom", true);
            Back = global.FindAction("Back", true);
            Notebook = global.FindAction("Notebook", true);

            global.Enable();
            ui.Enable();
            SetMode(InputMode.Gameplay);
            Services.Register(this);
        }

        void OnDestroy()
        {
            Services.Unregister(this);
            actions.Disable();
        }

        public void SetMode(InputMode mode)
        {
            Mode = mode;
            SetMapEnabled(player, mode == InputMode.Gameplay);
            SetMapEnabled(inspect, mode == InputMode.Inspect);
            ApplyCursor();
        }

        /// <summary>Играет ли игрок сейчас на геймпаде (по последнему нажатию). От этого зависят подсказки.</summary>
        public bool UsingGamepad { get; private set; }

        /// <summary>Игрок переключился между клавиатурой с мышью и геймпадом.</summary>
        public event Action<bool> UsingGamepadChanged;

        /// <summary>
        /// Название первой клавиши действия для текущего устройства, как его показывает Input System
        /// («LMB», «E», «Button South»…). Перевод в понятный игроку вид — дело интерфейса.
        /// </summary>
        public string BindingName(InputAction action)
        {
            var group = UsingGamepad ? GamepadScheme : KeyboardMouseScheme;
            var bindings = action.bindings;
            for (var i = 0; i < bindings.Count; i++)
            {
                var binding = bindings[i];
                if (binding.isComposite || binding.isPartOfComposite) continue;
                if (!InputBinding.MaskByGroup(group).Matches(binding)) continue;
                return action.GetBindingDisplayString(i, InputBinding.DisplayStringOptions.DontIncludeInteractions);
            }
            return string.Empty;
        }

        /// <summary>Пришло ли последнее значение действия с геймпада (для него нужна другая чувствительность).</summary>
        public static bool IsFromGamepad(InputAction action) => action.activeControl?.device is Gamepad;

        void OnEnable() => InputSystem.onActionChange += OnActionChange;
        void OnDisable() => InputSystem.onActionChange -= OnActionChange;

        void OnActionChange(object target, InputActionChange change)
        {
            if (change != InputActionChange.ActionPerformed || target is not InputAction action) return;
            var device = action.activeControl?.device;
            if (device == null) return;
            var gamepad = device is Gamepad;
            if (gamepad == UsingGamepad) return;
            UsingGamepad = gamepad;
            UsingGamepadChanged?.Invoke(gamepad);
        }

        // После сворачивания окна Windows отпускает курсор — возвращаем его обратно.
        void OnApplicationFocus(bool hasFocus)
        {
            if (hasFocus) ApplyCursor();
        }

        void ApplyCursor()
        {
            var captured = Mode == InputMode.Gameplay;
            Cursor.lockState = captured ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !captured;
        }

        static void SetMapEnabled(InputActionMap map, bool enabled)
        {
            if (enabled) map.Enable();
            else map.Disable();
        }
    }
}
