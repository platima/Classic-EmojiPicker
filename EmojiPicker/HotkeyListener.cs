using System;
using System.Runtime.InteropServices;
using System.Windows;

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

        // Injected between Win-down and Win-up when we swallow the '.', so the
        // shell doesn't treat the sequence as a bare Win press and open the
        // Start menu on release. 0xFF is an unassigned/no-op virtual key.
        private const ushort VkNone = 0xFF;

        // Keep the delegate alive for the lifetime of the hook so the GC
        // does not collect it out from under unmanaged code
        private readonly NativeMethods.LowLevelKeyboardProc hookProc;
        private IntPtr hookHandle;

        /// <summary>
        /// Raised on the UI thread when Win+. is pressed. Arguments are the
        /// foreground window, the focused child control at key-press time
        /// (the insertion target), and the text caret's screen rectangle when
        /// the target app exposes one (the picker anchors to it).
        /// </summary>
        public event Action<IntPtr, IntPtr, System.Drawing.Rectangle?>? HotkeyPressed;

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
            hookHandle = NativeMethods.SetWindowsHookEx(WhKeyboardLl, hookProc, IntPtr.Zero, 0);
            if (hookHandle == IntPtr.Zero)
            {
                // Without the hook Win+. silently does nothing - make sure the
                // failure is diagnosable even when the debug toggle is off
                Logger.LogAlways($"SetWindowsHookEx failed (error {Marshal.GetLastWin32Error()}); Win+. will not work");
            }
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
                        // Capture the target window, its focused control, and the
                        // text caret now, before showing our own window steals focus
                        var target = NativeMethods.GetForegroundWindow();
                        var focus = TextInjector.GetFocusedControl(target);
                        System.Drawing.Rectangle? caret =
                            TextInjector.TryGetCaretRect(target, out var caretRect) ? caretRect : null;

                        // The shell never sees the swallowed '.', so on Win-up it
                        // would open the Start menu; a no-op key press in between
                        // convinces it the Win key was a modifier, not a tap
                        InjectNoOpKey();

                        // Marshal to the UI thread; showing a window from inside
                        // the hook callback would block the input queue
                        Application.Current?.Dispatcher.BeginInvoke(new Action(() => HotkeyPressed?.Invoke(target, focus, caret)));

                        // Return non-zero to swallow the key so neither the '.'
                        // nor the built-in emoji panel reaches the foreground app
                        return new IntPtr(1);
                    }
                }
            }

            return NativeMethods.CallNextHookEx(hookHandle, nCode, wParam, lParam);
        }

        private static void InjectNoOpKey()
        {
            var inputs = new NativeMethods.INPUT[]
            {
                new NativeMethods.INPUT
                {
                    type = NativeMethods.InputKeyboard,
                    u = new NativeMethods.InputUnion { ki = new NativeMethods.KEYBDINPUT { wVk = VkNone } },
                },
                new NativeMethods.INPUT
                {
                    type = NativeMethods.InputKeyboard,
                    u = new NativeMethods.InputUnion
                    {
                        ki = new NativeMethods.KEYBDINPUT { wVk = VkNone, dwFlags = NativeMethods.KeyEventKeyUp },
                    },
                },
            };
            NativeMethods.SendInput((uint)inputs.Length, inputs, Marshal.SizeOf<NativeMethods.INPUT>());
        }

        private static bool IsWinDown()
        {
            // GetAsyncKeyState reflects the live physical key state, unlike
            // GetKeyState which lags behind inside a low-level hook
            const int pressed = 0x8000;
            return (NativeMethods.GetAsyncKeyState(VkLwin) & pressed) != 0
                || (NativeMethods.GetAsyncKeyState(VkRwin) & pressed) != 0;
        }

        public void Dispose()
        {
            if (hookHandle != IntPtr.Zero)
            {
                NativeMethods.UnhookWindowsHookEx(hookHandle);
                hookHandle = IntPtr.Zero;
            }
        }
    }
}
