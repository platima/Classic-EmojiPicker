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
        private const int WmKeyUp = 0x0101;
        private const int WmSysKeyDown = 0x0104;
        private const int WmSysKeyUp = 0x0105;

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

        // Tracks that our Win+. '.' is physically held so auto-repeat key-downs
        // don't re-fire the hotkey (which would thrash the picker open/closed)
        private bool periodHeld;

        /// <summary>
        /// Raised on the UI thread when Win+. is pressed, carrying the foreground
        /// window at key-press time (the insertion target). The focused control and
        /// caret are resolved by the handler, off this hook thread.
        /// </summary>
        public event Action<IntPtr>? HotkeyPressed;

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

        /// <summary>
        /// Re-installs the hook. Windows silently removes a low-level hook whose
        /// callback exceeds the system timeout, and hooks can be dropped across
        /// secure-desktop / session switches; re-arming (periodically and on
        /// session change) keeps Win+. working without a restart. Installs the fresh
        /// hook before removing the old one so there is no gap. Must be called on the
        /// thread that owns the message loop (the UI thread), like Start().
        /// </summary>
        public void Rearm()
        {
            var fresh = NativeMethods.SetWindowsHookEx(WhKeyboardLl, hookProc, IntPtr.Zero, 0);
            if (fresh == IntPtr.Zero)
            {
                Logger.Log($"Hook re-arm failed (error {Marshal.GetLastWin32Error()}); keeping the existing hook");
                return;
            }

            var previous = hookHandle;
            hookHandle = fresh;
            if (previous != IntPtr.Zero)
            {
                NativeMethods.UnhookWindowsHookEx(previous);
            }
        }

        private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0)
            {
                var message = (int)wParam;
                var vkCode = Marshal.ReadInt32(lParam);

                if (message == WmKeyDown || message == WmSysKeyDown)
                {
                    if (vkCode == VkOemPeriod && IsWinDown())
                    {
                        // Ignore auto-repeat: only the first '.' down of a physical
                        // Win+. press fires (holding it must not thrash the picker)
                        if (periodHeld)
                        {
                            return new IntPtr(1);
                        }

                        periodHeld = true;

                        // Only the (instant) foreground capture runs on the hook
                        // thread; the focused control and caret are resolved by the
                        // handler, so a hung target can't stall the hook past its
                        // timeout and get it silently removed by Windows.
                        var target = NativeMethods.GetForegroundWindow();

                        // The shell never sees the swallowed '.', so on Win-up it
                        // would open the Start menu; a no-op key press in between
                        // convinces it the Win key was a modifier, not a tap
                        InjectNoOpKey();

                        // Marshal to the UI thread; showing a window from inside
                        // the hook callback would block the input queue
                        Application.Current?.Dispatcher.BeginInvoke(new Action(() => HotkeyPressed?.Invoke(target)));

                        // Return non-zero to swallow the key so neither the '.'
                        // nor the built-in emoji panel reaches the foreground app
                        return new IntPtr(1);
                    }
                }
                else if (message == WmKeyUp || message == WmSysKeyUp)
                {
                    if (vkCode == VkOemPeriod && periodHeld)
                    {
                        // Swallow the matching key-up of our hotkey and re-arm for
                        // the next press
                        periodHeld = false;
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
