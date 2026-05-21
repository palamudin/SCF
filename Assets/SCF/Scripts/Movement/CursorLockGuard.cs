using System;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Serialization;
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
        [SerializeField] private bool keepCursorUnlocked;
        [SerializeField] private bool confineCursorInPlay = true;
        [FormerlySerializedAs("hideCursorInPlayUnlessControlHeld")]
        [SerializeField] private bool hideCursorInPlayUnlessControlAltHeld = true;

        [Header("Windows Shortcut Guard")]
        [SerializeField] private bool blockWindowsSystemShortcutsInPlay = true;
        [SerializeField] private bool blockWindowsSystemShortcutsInEditor = true;
        [SerializeField] private bool blockWindowsKeyInPlay = true;
        [SerializeField] private bool allowAltEscape = true;

        private void OnEnable()
        {
            ReleaseCursor();
            TickWindowsShortcutGuard();
        }

        private void Update()
        {
            ReleaseCursor();
            TickWindowsShortcutGuard();
        }

        private void LateUpdate()
        {
            ReleaseCursor();
            TickWindowsShortcutGuard();
        }

        private void OnGUI()
        {
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
            Event current = Event.current;
            if (current == null || !ShouldUseWindowsShortcutGuard() || !IsForegroundWindowOwnedByThisProcess())
            {
                return;
            }

            if (current.alt
                && current.type == EventType.KeyDown
                && current.keyCode != KeyCode.Escape)
            {
                current.Use();
            }
#endif
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            if (hasFocus)
            {
                ReleaseCursor();
            }

            TickWindowsShortcutGuard();
        }

        private void OnDisable()
        {
            ReleaseWindowsShortcutGuard(this);
        }

        private void OnApplicationQuit()
        {
            ReleaseWindowsShortcutGuard(this);
        }

        private void ReleaseCursor()
        {
            CursorLockMode desiredLockState = Cursor.lockState;
            if (Application.isPlaying && confineCursorInPlay)
            {
                desiredLockState = CursorLockMode.Confined;
            }
            else if (keepCursorUnlocked)
            {
                desiredLockState = CursorLockMode.None;
            }

            if (Cursor.lockState != desiredLockState)
            {
                Cursor.lockState = desiredLockState;
            }

            bool shouldBeVisible = keepCursorVisible;
            if (Application.isPlaying && hideCursorInPlayUnlessControlAltHeld)
            {
                shouldBeVisible = IsCursorOverrideHeld();
            }

            if (Cursor.visible != shouldBeVisible)
            {
                Cursor.visible = shouldBeVisible;
            }
        }

        private static bool IsCursorOverrideHeld()
        {
#if ENABLE_INPUT_SYSTEM
            Keyboard keyboard = Keyboard.current;
            if (keyboard != null
                && (keyboard.leftCtrlKey.isPressed || keyboard.rightCtrlKey.isPressed)
                && (keyboard.leftAltKey.isPressed || keyboard.rightAltKey.isPressed))
            {
                return true;
            }
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
            bool control = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);
            bool alt = Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt);
            return control && alt;
#else
            return false;
#endif
        }

        private void TickWindowsShortcutGuard()
        {
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
            if (ShouldUseWindowsShortcutGuard())
            {
                AcquireWindowsShortcutGuard(this);
            }
            else
            {
                ReleaseWindowsShortcutGuard(this);
            }
#endif
        }

        private bool ShouldUseWindowsShortcutGuard()
        {
            if (!Application.isPlaying || !blockWindowsSystemShortcutsInPlay)
            {
                return false;
            }

#if UNITY_EDITOR
            return blockWindowsSystemShortcutsInEditor;
#else
            return true;
#endif
        }

#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
        private const int WH_KEYBOARD_LL = 13;
        private const int WM_KEYDOWN = 0x0100;
        private const int WM_KEYUP = 0x0101;
        private const int WM_SYSKEYDOWN = 0x0104;
        private const int WM_SYSKEYUP = 0x0105;
        private const int WM_SYSCOMMAND = 0x0112;
        private const int SC_KEYMENU = 0xF100;
        private const int GWLP_WNDPROC = -4;
        private const int VK_TAB = 0x09;
        private const int VK_RETURN = 0x0D;
        private const int VK_ESCAPE = 0x1B;
        private const int VK_SPACE = 0x20;
        private const int VK_MENU = 0x12;
        private const int VK_LWIN = 0x5B;
        private const int VK_RWIN = 0x5C;
        private const int VK_F4 = 0x73;
        private const int VK_LMENU = 0xA4;
        private const int VK_RMENU = 0xA5;
        private const int LLKHF_ALTDOWN = 0x20;

        private static CursorLockGuard activeShortcutGuard;
        private static bool shortcutGuardCallbackActive;
        private static bool shortcutGuardAltDown;
        private static bool shortcutGuardBlockedAltCombo;
        private static uint currentProcessId;
        private static IntPtr keyboardHook;
        private static LowLevelKeyboardProc keyboardHookProc = WindowsKeyboardHookCallback;
        private static IntPtr guardedWindowHandle;
        private static IntPtr originalWindowProc;
        private static WindowProc guardedWindowProc = WindowsMessageGuardCallback;
        private static bool shortcutGuardHookWarningLogged;
        private static bool windowGuardWarningLogged;

        private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);
        private delegate IntPtr WindowProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

        [StructLayout(LayoutKind.Sequential)]
        private struct Kbdllhookstruct
        {
            public uint vkCode;
            public uint scanCode;
            public uint flags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll")]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);

        [DllImport("kernel32.dll")]
        private static extern uint GetCurrentProcessId();

        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        [DllImport("user32.dll", EntryPoint = "SetWindowLongPtr", SetLastError = true)]
        private static extern IntPtr SetWindowLongPtr64(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

        [DllImport("user32.dll", EntryPoint = "SetWindowLong", SetLastError = true)]
        private static extern IntPtr SetWindowLongPtr32(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

        [DllImport("user32.dll")]
        private static extern IntPtr CallWindowProc(IntPtr lpPrevWndFunc, IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

        private static void AcquireWindowsShortcutGuard(CursorLockGuard guard)
        {
            if (guard == null)
            {
                return;
            }

            activeShortcutGuard = guard;
            shortcutGuardCallbackActive = true;
            if (currentProcessId == 0)
            {
                currentProcessId = GetCurrentProcessId();
            }

            if (keyboardHook == IntPtr.Zero)
            {
                IntPtr moduleHandle = GetModuleHandle(null);
                keyboardHook = SetWindowsHookEx(WH_KEYBOARD_LL, keyboardHookProc, moduleHandle, 0);
                if (keyboardHook == IntPtr.Zero)
                {
                    keyboardHook = SetWindowsHookEx(WH_KEYBOARD_LL, keyboardHookProc, IntPtr.Zero, 0);
                }

                if (keyboardHook == IntPtr.Zero && !shortcutGuardHookWarningLogged)
                {
                    shortcutGuardHookWarningLogged = true;
                    Debug.LogWarning("SCF shortcut guard could not install the Windows keyboard hook. Alt+system shortcuts may still reach the window.");
                }
            }

            RefreshWindowMessageGuard();
        }

        private static void ReleaseWindowsShortcutGuard(CursorLockGuard guard)
        {
            if (activeShortcutGuard != guard)
            {
                return;
            }

            activeShortcutGuard = null;
            shortcutGuardCallbackActive = false;
            shortcutGuardAltDown = false;
            shortcutGuardBlockedAltCombo = false;
            ReleaseWindowMessageGuard();
            if (keyboardHook == IntPtr.Zero)
            {
                return;
            }

            UnhookWindowsHookEx(keyboardHook);
            keyboardHook = IntPtr.Zero;
        }

        private static IntPtr WindowsKeyboardHookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0
                && shortcutGuardCallbackActive
                && activeShortcutGuard != null
                && activeShortcutGuard.ShouldUseWindowsShortcutGuard()
                && IsForegroundWindowOwnedByThisProcess())
            {
                int message = wParam.ToInt32();
                if (message == WM_KEYDOWN || message == WM_SYSKEYDOWN || message == WM_KEYUP || message == WM_SYSKEYUP)
                {
                    Kbdllhookstruct key = Marshal.PtrToStructure<Kbdllhookstruct>(lParam);
                    int vkCode = (int)key.vkCode;
                    bool keyDown = message == WM_KEYDOWN || message == WM_SYSKEYDOWN;
                    bool keyUp = message == WM_KEYUP || message == WM_SYSKEYUP;
                    bool altKey = IsAltKey(vkCode);

                    if (altKey)
                    {
                        shortcutGuardAltDown = keyDown || (shortcutGuardAltDown && !keyUp);
                    }

                    bool altHeld = shortcutGuardAltDown || (key.flags & LLKHF_ALTDOWN) != 0;
                    if (activeShortcutGuard.ShouldBlockWindowsShortcutKey(vkCode, altHeld, keyDown, keyUp))
                    {
                        return (IntPtr)1;
                    }
                }
            }

            return CallNextHookEx(keyboardHook, nCode, wParam, lParam);
        }

        private static bool IsForegroundWindowOwnedByThisProcess()
        {
            return TryGetForegroundWindowOwnedByThisProcess(out _);
        }

        private static bool TryGetForegroundWindowOwnedByThisProcess(out IntPtr foreground)
        {
            foreground = GetForegroundWindow();
            if (foreground == IntPtr.Zero)
            {
                return false;
            }

            GetWindowThreadProcessId(foreground, out uint foregroundProcessId);
            if (currentProcessId == 0)
            {
                currentProcessId = GetCurrentProcessId();
            }

            return foregroundProcessId == currentProcessId;
        }

        private static void RefreshWindowMessageGuard()
        {
            if (!TryGetForegroundWindowOwnedByThisProcess(out IntPtr foreground))
            {
                return;
            }

            if (guardedWindowHandle == foreground && originalWindowProc != IntPtr.Zero)
            {
                return;
            }

            ReleaseWindowMessageGuard();
            IntPtr procPointer = Marshal.GetFunctionPointerForDelegate(guardedWindowProc);
            originalWindowProc = SetWindowLongPtr(guardedWindowHandle = foreground, GWLP_WNDPROC, procPointer);
            if (originalWindowProc == IntPtr.Zero)
            {
                guardedWindowHandle = IntPtr.Zero;
                if (!windowGuardWarningLogged)
                {
                    windowGuardWarningLogged = true;
                    Debug.LogWarning("SCF shortcut guard could not attach to the Unity window message pump. Alt+Space may still open the system menu in editor.");
                }
            }
        }

        private static void ReleaseWindowMessageGuard()
        {
            if (guardedWindowHandle == IntPtr.Zero || originalWindowProc == IntPtr.Zero)
            {
                guardedWindowHandle = IntPtr.Zero;
                originalWindowProc = IntPtr.Zero;
                return;
            }

            SetWindowLongPtr(guardedWindowHandle, GWLP_WNDPROC, originalWindowProc);
            guardedWindowHandle = IntPtr.Zero;
            originalWindowProc = IntPtr.Zero;
        }

        private static IntPtr SetWindowLongPtr(IntPtr hWnd, int nIndex, IntPtr dwNewLong)
        {
            return IntPtr.Size == 8
                ? SetWindowLongPtr64(hWnd, nIndex, dwNewLong)
                : SetWindowLongPtr32(hWnd, nIndex, dwNewLong);
        }

        private static IntPtr WindowsMessageGuardCallback(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam)
        {
            if (shortcutGuardCallbackActive
                && activeShortcutGuard != null
                && activeShortcutGuard.ShouldUseWindowsShortcutGuard())
            {
                if (msg == WM_SYSCOMMAND && ((wParam.ToInt64() & 0xFFF0) == SC_KEYMENU))
                {
                    return IntPtr.Zero;
                }

                if (msg == WM_SYSKEYDOWN || msg == WM_SYSKEYUP)
                {
                    int vkCode = wParam.ToInt32();
                    bool keyDown = msg == WM_SYSKEYDOWN;
                    bool keyUp = msg == WM_SYSKEYUP;
                    bool altHeld = true;
                    if (activeShortcutGuard.ShouldBlockWindowsShortcutKey(vkCode, altHeld, keyDown, keyUp))
                    {
                        return IntPtr.Zero;
                    }
                }
            }

            return CallWindowProc(originalWindowProc, hWnd, msg, wParam, lParam);
        }

        private static bool IsAltKey(int vkCode)
        {
            return vkCode == VK_MENU || vkCode == VK_LMENU || vkCode == VK_RMENU;
        }

        private bool ShouldBlockWindowsShortcutKey(int vkCode, bool altHeld, bool keyDown, bool keyUp)
        {
            if (blockWindowsKeyInPlay && (vkCode == VK_LWIN || vkCode == VK_RWIN))
            {
                return true;
            }

            if (IsAltKey(vkCode))
            {
                if (keyUp && shortcutGuardBlockedAltCombo)
                {
                    shortcutGuardBlockedAltCombo = false;
                    return true;
                }

                return false;
            }

            if (!altHeld)
            {
                return false;
            }

            if (allowAltEscape && vkCode == VK_ESCAPE)
            {
                return false;
            }

            bool block = keyDown
                         && (vkCode == VK_TAB
                             || vkCode == VK_SPACE
                             || vkCode == VK_RETURN
                             || vkCode == VK_F4
                             || vkCode != VK_ESCAPE);
            shortcutGuardBlockedAltCombo |= block;
            return block;
        }
#else
        private static void ReleaseWindowsShortcutGuard(CursorLockGuard guard)
        {
        }
#endif
    }
}
