using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace SCF.Gameplay
{
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(10000)]
    public sealed class CursorLockGuard : MonoBehaviour
    {
        [SerializeField] private bool keepCursorVisible = true;
        [SerializeField] private bool keepCursorUnlocked = true;
        [SerializeField] private bool hideCursorInPlayUnlessControlHeld = true;

        private void OnEnable()
        {
            ReleaseCursor();
        }

        private void Update()
        {
            ReleaseCursor();
        }

        private void LateUpdate()
        {
            ReleaseCursor();
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            if (hasFocus)
            {
                ReleaseCursor();
            }
        }

        private void ReleaseCursor()
        {
            if (keepCursorUnlocked && Cursor.lockState != CursorLockMode.None)
            {
                Cursor.lockState = CursorLockMode.None;
            }

            bool shouldBeVisible = keepCursorVisible;
            if (Application.isPlaying && hideCursorInPlayUnlessControlHeld)
            {
                shouldBeVisible = IsControlHeld();
            }

            if (Cursor.visible != shouldBeVisible)
            {
                Cursor.visible = shouldBeVisible;
            }
        }

        private static bool IsControlHeld()
        {
#if ENABLE_INPUT_SYSTEM
            Keyboard keyboard = Keyboard.current;
            if (keyboard != null && (keyboard.leftCtrlKey.isPressed || keyboard.rightCtrlKey.isPressed))
            {
                return true;
            }
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
            return Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);
#else
            return false;
#endif
        }
    }
}
