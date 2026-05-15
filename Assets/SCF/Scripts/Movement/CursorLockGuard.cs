using UnityEngine;

namespace SCF.Gameplay
{
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(10000)]
    public sealed class CursorLockGuard : MonoBehaviour
    {
        [SerializeField] private bool keepCursorVisible = true;
        [SerializeField] private bool keepCursorUnlocked = true;

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

            if (keepCursorVisible && !Cursor.visible)
            {
                Cursor.visible = true;
            }
        }
    }
}
