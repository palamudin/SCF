using UnityEngine;
using UnityEngine.InputSystem;

namespace SCF.Gameplay
{
    public enum AimInputMode
    {
        None,
        ScreenPoint,
        WorldDirection
    }

    [DisallowMultipleComponent]
    [DefaultExecutionOrder(-100)]
    public sealed class IsometricPlayerInput : MonoBehaviour
    {
        [Header("Input Actions")]
        [SerializeField] private InputActionReference moveAction = null;
        [SerializeField] private InputActionReference lookAction = null;
        [SerializeField] private InputActionReference sprintAction = null;
        [SerializeField] private InputActionReference attackAction = null;
        [SerializeField] private InputActionReference skillAction = null;

        [Header("Fallback Bindings")]
        [SerializeField] private bool enableFallbackBindings = true;
        [SerializeField] private bool enableMouseAim = true;
        [SerializeField, Range(0.05f, 0.5f)] private float stickLookDeadZone = 0.18f;

        public Vector2 Move { get; private set; }
        public Vector2 AimValue { get; private set; }
        public AimInputMode AimMode { get; private set; }
        public bool SprintHeld { get; private set; }
        public bool AttackHeld { get; private set; }
        public bool AttackPressedThisFrame { get; private set; }
        public bool AimHeld { get; private set; }
        public bool SkillPressedThisFrame { get; private set; }
        public bool MobilityHeld { get; private set; }
        public bool MobilityPressedThisFrame { get; private set; }
        public bool MobilityReleasedThisFrame { get; private set; }
        public float MobilityHeldDuration { get; private set; }

        private void OnEnable()
        {
            SetActionEnabled(moveAction, true);
            SetActionEnabled(lookAction, true);
            SetActionEnabled(sprintAction, true);
            SetActionEnabled(attackAction, true);
            SetActionEnabled(skillAction, true);
        }

        private void OnDisable()
        {
            SetActionEnabled(moveAction, false);
            SetActionEnabled(lookAction, false);
            SetActionEnabled(sprintAction, false);
            SetActionEnabled(attackAction, false);
            SetActionEnabled(skillAction, false);

            Move = Vector2.zero;
            AimValue = Vector2.zero;
            AimMode = AimInputMode.None;
            SprintHeld = false;
            AttackHeld = false;
            AttackPressedThisFrame = false;
            AimHeld = false;
            SkillPressedThisFrame = false;
            MobilityHeld = false;
            MobilityPressedThisFrame = false;
            MobilityReleasedThisFrame = false;
            MobilityHeldDuration = 0f;
        }

        private void Update()
        {
            Move = Vector2.ClampMagnitude(ReadMove(), 1f);
            SprintHeld = ReadButton(sprintAction) || ReadFallbackSprint();
            AttackHeld = ReadButton(attackAction) || ReadFallbackAttack();
            AttackPressedThisFrame = ReadPressedThisFrame(attackAction) || ReadFallbackAttackPressed();
            AimHeld = ReadFallbackAimHeld();
            MobilityHeld = ReadButton(skillAction) || ReadFallbackMobilityHeld();
            MobilityPressedThisFrame = ReadPressedThisFrame(skillAction) || ReadFallbackMobilityPressed();
            MobilityReleasedThisFrame = ReadReleasedThisFrame(skillAction) || ReadFallbackMobilityReleased();
            MobilityHeldDuration = MobilityHeld ? MobilityHeldDuration + Time.deltaTime : 0f;
            SkillPressedThisFrame = MobilityPressedThisFrame;
            ReadAim();
        }

        private Vector2 ReadMove()
        {
            Vector2 value = ReadVector2(moveAction);
            if (value.sqrMagnitude > 0.0001f || !enableFallbackBindings)
            {
                return value;
            }

            Vector2 fallback = Vector2.zero;

            Keyboard keyboard = Keyboard.current;
            if (keyboard != null)
            {
                if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed)
                {
                    fallback.x -= 1f;
                }

                if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed)
                {
                    fallback.x += 1f;
                }

                if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed)
                {
                    fallback.y -= 1f;
                }

                if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed)
                {
                    fallback.y += 1f;
                }
            }

            Gamepad gamepad = Gamepad.current;
            if (gamepad != null)
            {
                Vector2 stick = gamepad.leftStick.ReadValue();
                if (stick.sqrMagnitude > fallback.sqrMagnitude)
                {
                    fallback = stick;
                }
            }

            return fallback;
        }

        private void ReadAim()
        {
            AimMode = AimInputMode.None;
            AimValue = Vector2.zero;

            Vector2 lookValue = ReadVector2(lookAction);
            InputAction look = lookAction != null ? lookAction.action : null;
            if (look != null && lookValue.sqrMagnitude >= stickLookDeadZone * stickLookDeadZone)
            {
                string deviceName = look.activeControl != null ? look.activeControl.device.name : string.Empty;
                if (!deviceName.ToLowerInvariant().Contains("mouse"))
                {
                    AimMode = AimInputMode.WorldDirection;
                    AimValue = Vector2.ClampMagnitude(lookValue, 1f);
                    return;
                }
            }

            Gamepad gamepad = Gamepad.current;
            if (enableFallbackBindings && gamepad != null)
            {
                Vector2 stick = gamepad.rightStick.ReadValue();
                if (stick.sqrMagnitude >= stickLookDeadZone * stickLookDeadZone)
                {
                    AimMode = AimInputMode.WorldDirection;
                    AimValue = Vector2.ClampMagnitude(stick, 1f);
                    return;
                }
            }

            Mouse mouse = Mouse.current;
            if (enableMouseAim && mouse != null)
            {
                if (Cursor.lockState != CursorLockMode.None || !Cursor.visible)
                {
                    Cursor.lockState = CursorLockMode.None;
                    Cursor.visible = true;
                }

                AimMode = AimInputMode.ScreenPoint;
                AimValue = mouse.position.ReadValue();
            }
        }

        private bool ReadFallbackSprint()
        {
            if (!enableFallbackBindings)
            {
                return false;
            }

            Keyboard keyboard = Keyboard.current;
            if (keyboard != null && (keyboard.leftShiftKey.isPressed || keyboard.rightShiftKey.isPressed))
            {
                return true;
            }

            Gamepad gamepad = Gamepad.current;
            return gamepad != null && gamepad.leftStickButton.isPressed;
        }

        private bool ReadFallbackAttack()
        {
            if (!enableFallbackBindings)
            {
                return false;
            }

            Mouse mouse = Mouse.current;
            if (mouse != null && mouse.leftButton.isPressed)
            {
                return true;
            }

            Gamepad gamepad = Gamepad.current;
            return gamepad != null && gamepad.rightTrigger.ReadValue() > 0.5f;
        }

        private bool ReadFallbackAttackPressed()
        {
            if (!enableFallbackBindings)
            {
                return false;
            }

            Mouse mouse = Mouse.current;
            if (mouse != null && mouse.leftButton.wasPressedThisFrame)
            {
                return true;
            }

            Gamepad gamepad = Gamepad.current;
            return gamepad != null && gamepad.rightTrigger.wasPressedThisFrame;
        }

        private bool ReadFallbackAimHeld()
        {
            if (!enableFallbackBindings)
            {
                return false;
            }

            Mouse mouse = Mouse.current;
            if (mouse != null && mouse.rightButton.isPressed)
            {
                return true;
            }

            Gamepad gamepad = Gamepad.current;
            return gamepad != null && gamepad.leftTrigger.ReadValue() > 0.5f;
        }

        private bool ReadFallbackMobilityHeld()
        {
            if (!enableFallbackBindings)
            {
                return false;
            }

            Keyboard keyboard = Keyboard.current;
            if (keyboard != null && keyboard.spaceKey.isPressed)
            {
                return true;
            }

            Gamepad gamepad = Gamepad.current;
            return gamepad != null && gamepad.buttonSouth.isPressed;
        }

        private bool ReadFallbackMobilityPressed()
        {
            if (!enableFallbackBindings)
            {
                return false;
            }

            Keyboard keyboard = Keyboard.current;
            if (keyboard != null && keyboard.spaceKey.wasPressedThisFrame)
            {
                return true;
            }

            Gamepad gamepad = Gamepad.current;
            return gamepad != null && gamepad.buttonSouth.wasPressedThisFrame;
        }

        private bool ReadFallbackMobilityReleased()
        {
            if (!enableFallbackBindings)
            {
                return false;
            }

            Keyboard keyboard = Keyboard.current;
            if (keyboard != null && keyboard.spaceKey.wasReleasedThisFrame)
            {
                return true;
            }

            Gamepad gamepad = Gamepad.current;
            return gamepad != null && gamepad.buttonSouth.wasReleasedThisFrame;
        }

        private static Vector2 ReadVector2(InputActionReference reference)
        {
            InputAction action = reference != null ? reference.action : null;
            return action != null ? action.ReadValue<Vector2>() : Vector2.zero;
        }

        private static bool ReadButton(InputActionReference reference)
        {
            InputAction action = reference != null ? reference.action : null;
            return action != null && action.IsPressed();
        }

        private static bool ReadPressedThisFrame(InputActionReference reference)
        {
            InputAction action = reference != null ? reference.action : null;
            return action != null && action.WasPressedThisFrame();
        }

        private static bool ReadReleasedThisFrame(InputActionReference reference)
        {
            InputAction action = reference != null ? reference.action : null;
            return action != null && action.WasReleasedThisFrame();
        }

        private static void SetActionEnabled(InputActionReference reference, bool enabled)
        {
            InputAction action = reference != null ? reference.action : null;
            if (action == null)
            {
                return;
            }

            if (enabled)
            {
                action.Enable();
            }
            else
            {
                action.Disable();
            }
        }
    }
}
