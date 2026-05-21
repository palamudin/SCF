using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.Serialization;

namespace SCF.Gameplay
{
    public enum SCFInputBinding
    {
        None,
        Jump,
        WalkToggle,
        Slide
    }

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
        [SerializeField] private Key jumpFallbackKey = Key.Space;
        [SerializeField] private Key walkToggleFallbackKey = Key.LeftCtrl;
        [SerializeField] private Key slideFallbackKey = Key.LeftAlt;

        [Header("Cursor")]
        [SerializeField] private bool managePlayModeCursor = true;
        [SerializeField] private bool confineCursorToGameWindow = true;
        [FormerlySerializedAs("hideCursorUnlessControlHeld")]
        [SerializeField] private bool hideCursorUnlessControlAltHeld = true;

        [Header("Debug Time Freeze")]
        [SerializeField] private bool enableTimeFreezeHotkey = true;
        [SerializeField] private bool logTimeFreezeToggle = true;

        public Vector2 Move { get; private set; }
        public Vector2 AimValue { get; private set; }
        public AimInputMode AimMode { get; private set; }
        public bool SprintHeld { get; private set; }
        public bool AttackHeld { get; private set; }
        public bool AttackPressedThisFrame { get; private set; }
        public bool AimHeld { get; private set; }
        public bool WalkToggled { get; private set; }
        public bool SkillPressedThisFrame { get; private set; }
        public bool MobilityHeld { get; private set; }
        public bool MobilityPressedThisFrame { get; private set; }
        public bool MobilityReleasedThisFrame { get; private set; }
        public float MobilityHeldDuration { get; private set; }
        public bool SlideHeld { get; private set; }
        public bool SlidePressedThisFrame { get; private set; }
        public bool SlideReleasedThisFrame { get; private set; }
        public Key JumpFallbackKey => jumpFallbackKey;
        public Key WalkToggleFallbackKey => walkToggleFallbackKey;
        public Key SlideFallbackKey => slideFallbackKey;

        private const string JumpFallbackKeyPref = "SCF.Input.Jump";
        private const string WalkToggleFallbackKeyPref = "SCF.Input.WalkToggle";
        private const string SlideFallbackKeyPref = "SCF.Input.Slide";
        private static bool timeFrozenByHotkey;
        private static float preFreezeTimeScale = 1f;
        private bool capturedCursorState;
        private CursorLockMode previousCursorLockState;
        private bool previousCursorVisible;

        private void Awake()
        {
            LoadFallbackBindings();
        }

        private void OnEnable()
        {
            LoadFallbackBindings();
            CaptureCursorState();
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
            WalkToggled = false;
            SkillPressedThisFrame = false;
            MobilityHeld = false;
            MobilityPressedThisFrame = false;
            MobilityReleasedThisFrame = false;
            MobilityHeldDuration = 0f;
            SlideHeld = false;
            SlidePressedThisFrame = false;
            SlideReleasedThisFrame = false;

            if (timeFrozenByHotkey)
            {
                RestoreTimeScale(false);
            }

            RestoreCursorState();
        }

        public Key GetFallbackBinding(SCFInputBinding binding)
        {
            switch (binding)
            {
                case SCFInputBinding.Jump:
                    return jumpFallbackKey;
                case SCFInputBinding.WalkToggle:
                    return walkToggleFallbackKey;
                case SCFInputBinding.Slide:
                    return slideFallbackKey;
                default:
                    return Key.None;
            }
        }

        public void SetFallbackBinding(SCFInputBinding binding, Key key, bool save = true)
        {
            if (key == Key.None)
            {
                return;
            }

            switch (binding)
            {
                case SCFInputBinding.Jump:
                    jumpFallbackKey = key;
                    break;
                case SCFInputBinding.WalkToggle:
                    walkToggleFallbackKey = key;
                    break;
                case SCFInputBinding.Slide:
                    slideFallbackKey = key;
                    break;
            }

            if (save)
            {
                SaveFallbackBindings();
            }
        }

        public void ResetFallbackBindings()
        {
            jumpFallbackKey = Key.Space;
            walkToggleFallbackKey = Key.LeftCtrl;
            slideFallbackKey = Key.LeftAlt;
            SaveFallbackBindings();
        }

        public static string FormatBindingKey(Key key)
        {
            switch (key)
            {
                case Key.LeftCtrl:
                case Key.RightCtrl:
                    return "Ctrl";
                case Key.LeftAlt:
                case Key.RightAlt:
                    return "Alt";
                case Key.LeftShift:
                case Key.RightShift:
                    return "Shift";
                case Key.Space:
                    return "Space";
                case Key.None:
                    return "-";
                default:
                    return key.ToString();
            }
        }

        public static string FormatBindingName(SCFInputBinding binding)
        {
            switch (binding)
            {
                case SCFInputBinding.Jump:
                    return "Jump";
                case SCFInputBinding.WalkToggle:
                    return "Walk";
                case SCFInputBinding.Slide:
                    return "Slide";
                default:
                    return "None";
            }
        }

        public static bool TryConvertKeyCode(KeyCode keyCode, out Key key)
        {
            key = Key.None;
            if (keyCode == KeyCode.None)
            {
                return false;
            }

            switch (keyCode)
            {
                case KeyCode.Space:
                    key = Key.Space;
                    return true;
                case KeyCode.LeftControl:
                    key = Key.LeftCtrl;
                    return true;
                case KeyCode.RightControl:
                    key = Key.RightCtrl;
                    return true;
                case KeyCode.LeftAlt:
                    key = Key.LeftAlt;
                    return true;
                case KeyCode.RightAlt:
                    key = Key.RightAlt;
                    return true;
                case KeyCode.LeftShift:
                    key = Key.LeftShift;
                    return true;
                case KeyCode.RightShift:
                    key = Key.RightShift;
                    return true;
                case KeyCode.Tab:
                    key = Key.Tab;
                    return true;
                case KeyCode.Return:
                    key = Key.Enter;
                    return true;
                case KeyCode.Backspace:
                    key = Key.Backspace;
                    return true;
                case KeyCode.Delete:
                    key = Key.Delete;
                    return true;
                case KeyCode.UpArrow:
                    key = Key.UpArrow;
                    return true;
                case KeyCode.DownArrow:
                    key = Key.DownArrow;
                    return true;
                case KeyCode.LeftArrow:
                    key = Key.LeftArrow;
                    return true;
                case KeyCode.RightArrow:
                    key = Key.RightArrow;
                    return true;
            }

            if (keyCode >= KeyCode.A && keyCode <= KeyCode.Z)
            {
                return Enum.TryParse(keyCode.ToString(), out key);
            }

            if (keyCode >= KeyCode.Alpha0 && keyCode <= KeyCode.Alpha9)
            {
                int digit = (int)keyCode - (int)KeyCode.Alpha0;
                return Enum.TryParse("Digit" + digit, out key);
            }

            if (keyCode >= KeyCode.F1 && keyCode <= KeyCode.F12)
            {
                return Enum.TryParse(keyCode.ToString(), out key);
            }

            return false;
        }

        private void LoadFallbackBindings()
        {
            jumpFallbackKey = LoadBinding(JumpFallbackKeyPref, jumpFallbackKey);
            walkToggleFallbackKey = LoadBinding(WalkToggleFallbackKeyPref, walkToggleFallbackKey);
            slideFallbackKey = LoadBinding(SlideFallbackKeyPref, slideFallbackKey);
        }

        private void SaveFallbackBindings()
        {
            PlayerPrefs.SetString(JumpFallbackKeyPref, jumpFallbackKey.ToString());
            PlayerPrefs.SetString(WalkToggleFallbackKeyPref, walkToggleFallbackKey.ToString());
            PlayerPrefs.SetString(SlideFallbackKeyPref, slideFallbackKey.ToString());
            PlayerPrefs.Save();
        }

        private static Key LoadBinding(string keyName, Key fallback)
        {
            string stored = PlayerPrefs.GetString(keyName, string.Empty);
            return Enum.TryParse(stored, out Key key) && key != Key.None ? key : fallback;
        }

        private void Update()
        {
            ApplyCursorPolicy();
            TickTimeFreezeHotkey();
            if (timeFrozenByHotkey)
            {
                ClearPressedThisFrameFlags();
                return;
            }

            Move = Vector2.ClampMagnitude(ReadMove(), 1f);
            SprintHeld = ReadButton(sprintAction) || ReadFallbackSprint();
            AttackHeld = ReadButton(attackAction) || ReadFallbackAttack();
            AttackPressedThisFrame = ReadPressedThisFrame(attackAction) || ReadFallbackAttackPressed();
            AimHeld = ReadFallbackAimHeld();
            if (ReadFallbackWalkTogglePressed())
            {
                WalkToggled = !WalkToggled;
            }

            MobilityHeld = ReadButton(skillAction) || ReadFallbackMobilityHeld();
            MobilityPressedThisFrame = ReadPressedThisFrame(skillAction) || ReadFallbackMobilityPressed();
            MobilityReleasedThisFrame = ReadReleasedThisFrame(skillAction) || ReadFallbackMobilityReleased();
            MobilityHeldDuration = MobilityHeld ? MobilityHeldDuration + Time.deltaTime : 0f;
            SlideHeld = ReadFallbackSlideHeld();
            SlidePressedThisFrame = ReadFallbackSlidePressed();
            SlideReleasedThisFrame = ReadFallbackSlideReleased();
            SkillPressedThisFrame = MobilityPressedThisFrame;
            ReadAim();
        }

        private void TickTimeFreezeHotkey()
        {
            if (!enableTimeFreezeHotkey)
            {
                return;
            }

            Keyboard keyboard = Keyboard.current;
            if (keyboard == null || !keyboard.fKey.wasPressedThisFrame)
            {
                return;
            }

            if (timeFrozenByHotkey)
            {
                RestoreTimeScale(logTimeFreezeToggle);
            }
            else
            {
                preFreezeTimeScale = Time.timeScale > 0.0001f ? Time.timeScale : 1f;
                Time.timeScale = 0f;
                timeFrozenByHotkey = true;
                if (logTimeFreezeToggle)
                {
                    Debug.Log("SCF time frozen. Press F to resume.");
                }
            }
        }

        private void ClearPressedThisFrameFlags()
        {
            AttackPressedThisFrame = false;
            SkillPressedThisFrame = false;
            MobilityPressedThisFrame = false;
            MobilityReleasedThisFrame = false;
            SlidePressedThisFrame = false;
            SlideReleasedThisFrame = false;
        }

        private static void RestoreTimeScale(bool announce)
        {
            Time.timeScale = preFreezeTimeScale > 0.0001f ? preFreezeTimeScale : 1f;
            timeFrozenByHotkey = false;
            if (announce)
            {
                Debug.Log("SCF time resumed.");
            }
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
                AimMode = AimInputMode.ScreenPoint;
                AimValue = mouse.position.ReadValue();
            }
        }

        private void CaptureCursorState()
        {
            if (capturedCursorState)
            {
                return;
            }

            previousCursorLockState = Cursor.lockState;
            previousCursorVisible = Cursor.visible;
            capturedCursorState = true;
        }

        private void ApplyCursorPolicy()
        {
            if (!managePlayModeCursor || !Application.isPlaying)
            {
                return;
            }

            Cursor.lockState = confineCursorToGameWindow ? CursorLockMode.Confined : CursorLockMode.None;
            Cursor.visible = !hideCursorUnlessControlAltHeld || IsCursorOverrideHeld();
        }

        private void RestoreCursorState()
        {
            if (!capturedCursorState)
            {
                return;
            }

            Cursor.lockState = previousCursorLockState;
            Cursor.visible = previousCursorVisible;
            capturedCursorState = false;
        }

        private static bool IsCursorOverrideHeld()
        {
            Keyboard keyboard = Keyboard.current;
            return keyboard != null
                   && (keyboard.leftCtrlKey.isPressed || keyboard.rightCtrlKey.isPressed)
                   && (keyboard.leftAltKey.isPressed || keyboard.rightAltKey.isPressed);
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

        private bool ReadFallbackWalkTogglePressed()
        {
            if (!enableFallbackBindings)
            {
                return false;
            }

            Keyboard keyboard = Keyboard.current;
            return keyboard != null
                   && !IsCursorOverrideHeld()
                   && WasKeyPressedThisFrame(keyboard, walkToggleFallbackKey);
        }

        private bool ReadFallbackSlideHeld()
        {
            if (!enableFallbackBindings)
            {
                return false;
            }

            Keyboard keyboard = Keyboard.current;
            if (keyboard != null && IsKeyPressed(keyboard, slideFallbackKey))
            {
                return true;
            }

            Gamepad gamepad = Gamepad.current;
            return gamepad != null && gamepad.buttonEast.isPressed;
        }

        private bool ReadFallbackSlidePressed()
        {
            if (!enableFallbackBindings)
            {
                return false;
            }

            Keyboard keyboard = Keyboard.current;
            if (keyboard != null && WasKeyPressedThisFrame(keyboard, slideFallbackKey))
            {
                return true;
            }

            Gamepad gamepad = Gamepad.current;
            return gamepad != null && gamepad.buttonEast.wasPressedThisFrame;
        }

        private bool ReadFallbackSlideReleased()
        {
            if (!enableFallbackBindings)
            {
                return false;
            }

            Keyboard keyboard = Keyboard.current;
            if (keyboard != null && WasKeyReleasedThisFrame(keyboard, slideFallbackKey))
            {
                return true;
            }

            Gamepad gamepad = Gamepad.current;
            return gamepad != null && gamepad.buttonEast.wasReleasedThisFrame;
        }

        private bool ReadFallbackMobilityHeld()
        {
            if (!enableFallbackBindings)
            {
                return false;
            }

            Keyboard keyboard = Keyboard.current;
            if (keyboard != null && IsKeyPressed(keyboard, jumpFallbackKey))
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
            if (keyboard != null && WasKeyPressedThisFrame(keyboard, jumpFallbackKey))
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
            if (keyboard != null && WasKeyReleasedThisFrame(keyboard, jumpFallbackKey))
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

        private static bool IsKeyPressed(Keyboard keyboard, Key key)
        {
            KeyControl control = ResolveKeyControl(keyboard, key);
            return control != null && control.isPressed;
        }

        private static bool WasKeyPressedThisFrame(Keyboard keyboard, Key key)
        {
            KeyControl control = ResolveKeyControl(keyboard, key);
            return control != null && control.wasPressedThisFrame;
        }

        private static bool WasKeyReleasedThisFrame(Keyboard keyboard, Key key)
        {
            KeyControl control = ResolveKeyControl(keyboard, key);
            return control != null && control.wasReleasedThisFrame;
        }

        private static KeyControl ResolveKeyControl(Keyboard keyboard, Key key)
        {
            if (keyboard == null || key == Key.None)
            {
                return null;
            }

            switch (key)
            {
                case Key.LeftCtrl:
                case Key.RightCtrl:
                    return keyboard.leftCtrlKey.isPressed || keyboard.leftCtrlKey.wasPressedThisFrame || keyboard.leftCtrlKey.wasReleasedThisFrame
                        ? keyboard.leftCtrlKey
                        : keyboard.rightCtrlKey;
                case Key.LeftAlt:
                case Key.RightAlt:
                    return keyboard.leftAltKey.isPressed || keyboard.leftAltKey.wasPressedThisFrame || keyboard.leftAltKey.wasReleasedThisFrame
                        ? keyboard.leftAltKey
                        : keyboard.rightAltKey;
                case Key.LeftShift:
                case Key.RightShift:
                    return keyboard.leftShiftKey.isPressed || keyboard.leftShiftKey.wasPressedThisFrame || keyboard.leftShiftKey.wasReleasedThisFrame
                        ? keyboard.leftShiftKey
                        : keyboard.rightShiftKey;
                default:
                    return keyboard[key];
            }
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
