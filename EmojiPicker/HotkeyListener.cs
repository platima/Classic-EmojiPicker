using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;

namespace EmojiPicker
{
    /// <summary>
    /// Global low-level keyboard hook that intercepts Win+. (the shortcut the
    /// Windows shell uses for its own emoji panel) and raises <see cref="HotkeyPressed"/>.
    /// The keystroke is swallowed so the built-in panel does not also open.
    /// </summary>
    internal sealed class HotkeyListener : IDisposable
    {
        private const int WhKeyboardLl = 13;
        private const int WmKeyDown = 0x0100;
        private const int WmSysKeyDown = 0x0104;

        private const int VkLwin = 0x5B;
        private const int VkRwin = 0x5C;
        private const int VkOemPeriod = 0xBE; // '.' on US layouts

        // Keep the delegate alive for the lifetime of the hook so the GC
        // does not collect it out from under unmanaged code
        private readonly LowLevelKeyboardProc hookProc;
        private IntPtr hookHandle;

        /// <summary>
        /// Raised on the UI thread when Win+. is pressed. Arguments are the
        /// foreground window and the focused child control at key-press time
        /// (the insertion target).
        /// </summary>
        public event Action<IntPtr, IntPtr>? HotkeyPressed;

        public HotkeyListener()
        {
            hookProc = HookCallback;
        }

        public void Start()
        {
            if (hookHandle != IntPtr.Zero)
            {
                return;
            }

            // WH_KEYBOARD_LL is a global hook; passing a null module handle is
            // fine for a low-level hook running on the installing thread
            hookHandle = SetWindowsHookEx(WhKeyboardLl, hookProc, IntPtr.Zero, 0);
        }

        private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0)
            {
                var message = (int)wParam;
                if (message == WmKeyDown || message == WmSysKeyDown)
                {
                    var vkCode = Marshal.ReadInt32(lParam);
                    if (vkCode == VkOemPeriod && IsWinDown())
                    {
                        // Capture the target window and its focused control now,
                        // before showing our own window steals focus
                        var target = GetForegroundWindow();
                        var focus = TextInjector.GetFocusedControl(target);

                        // Marshal to the UI thread; showing a window from inside
                        // the hook callback would block the input queue
                        Application.Current?.Dispatcher.BeginInvoke(new Action(() => HotkeyPressed?.Invoke(target, focus)));

                        // Return non-zero to swallow the key so neither the '.'
                        // nor the built-in emoji panel reaches the foreground app
                        return new IntPtr(1);
                    }
                }
            }

            return CallNextHookEx(hookHandle, nCode, wParam, lParam);
        }

        private static bool IsWinDown()
        {
            // GetAsyncKeyState reflects the live physical key state, unlike
            // GetKeyState which lags behind inside a low-level hook
            const int pressed = 0x8000;
            return (GetAsyncKeyState(VkLwin) & pressed) != 0 || (GetAsyncKeyState(VkRwin) & pressed) != 0;
        }

        public void Dispose()
        {
            if (hookHandle != IntPtr.Zero)
            {
                UnhookWindowsHookEx(hookHandle);
                hookHandle = IntPtr.Zero;
            }
        }

        private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll")]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll")]
        private static extern short GetAsyncKeyState(int nVirtKey);

        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();
    }
}
