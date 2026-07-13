using System;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Threading.Tasks;

namespace EmojiPicker
{
    /// <summary>
    /// Types text into the window that was focused before the picker opened,
    /// mimicking the Windows 10 emoji panel's insert behaviour.
    /// </summary>
    internal static class TextInjector
    {
        // Whether this process runs elevated; injection into an elevated target
        // from a non-elevated process is silently discarded by UIPI, so we
        // detect that case and fall back to the clipboard instead
        private static readonly bool SelfElevated = IsSelfElevated();

        /// <summary>
        /// Returns the control that currently has keyboard focus within
        /// <paramref name="topLevel"/>'s thread (e.g. Explorer's Search or address
        /// edit), or the top-level window itself when it can't be determined.
        /// Captured before the picker opens so focus can be restored on insert.
        /// </summary>
        public static IntPtr GetFocusedControl(IntPtr topLevel)
        {
            if (topLevel == IntPtr.Zero)
            {
                return topLevel;
            }

            var threadId = NativeMethods.GetWindowThreadProcessId(topLevel, out _);
            var gui = new NativeMethods.GUITHREADINFO { cbSize = Marshal.SizeOf<NativeMethods.GUITHREADINFO>() };
            if (threadId != 0 && NativeMethods.GetGUIThreadInfo(threadId, ref gui) && gui.hwndFocus != IntPtr.Zero)
            {
                return gui.hwndFocus;
            }

            return topLevel;
        }

        /// <summary>
        /// Screen-coordinate rectangle of the text caret in <paramref name="topLevel"/>'s
        /// thread, when the app exposes one (classic edit controls do; Chromium and
        /// Electron apps publish one for accessibility). Returns false when there is
        /// no system caret, so the caller can fall back to the mouse position.
        /// </summary>
        public static bool TryGetCaretRect(IntPtr topLevel, out System.Drawing.Rectangle rect)
        {
            rect = default;
            if (topLevel == IntPtr.Zero)
            {
                return false;
            }

            var threadId = NativeMethods.GetWindowThreadProcessId(topLevel, out _);
            var gui = new NativeMethods.GUITHREADINFO { cbSize = Marshal.SizeOf<NativeMethods.GUITHREADINFO>() };
            if (threadId == 0 || !NativeMethods.GetGUIThreadInfo(threadId, ref gui) || gui.hwndCaret == IntPtr.Zero)
            {
                return false;
            }

            // rcCaret is in hwndCaret's client coordinates; convert both corners
            var topLeft = new System.Drawing.Point(gui.rcCaret.Left, gui.rcCaret.Top);
            var bottomRight = new System.Drawing.Point(gui.rcCaret.Right, gui.rcCaret.Bottom);
            if (!NativeMethods.ClientToScreen(gui.hwndCaret, ref topLeft) || !NativeMethods.ClientToScreen(gui.hwndCaret, ref bottomRight))
            {
                return false;
            }

            rect = System.Drawing.Rectangle.FromLTRB(topLeft.X, topLeft.Y, bottomRight.X, bottomRight.Y);
            return true;
        }

        /// <summary>
        /// Attempts to focus <paramref name="targetWindow"/> and type <paramref name="text"/> into it.
        /// <paramref name="focusWindow"/> is the child control that had keyboard focus before the
        /// picker opened; focus is restored to it so text lands in the right place. Returns false
        /// when there is no usable target (the window is gone, or it is elevated and we are not -
        /// UIPI would silently discard the injected input), so the caller can fall back to the
        /// clipboard. Must be awaited on the UI thread; the focus-settle delay is non-blocking.
        /// </summary>
        public static async Task<bool> TryInsertAsync(IntPtr targetWindow, IntPtr focusWindow, string text)
        {
            if (targetWindow == IntPtr.Zero || !NativeMethods.IsWindow(targetWindow))
            {
                return false;
            }

            if (!SelfElevated && IsWindowElevated(targetWindow))
            {
                Logger.Log("Insert target is elevated; UIPI would drop the input - using clipboard");
                return false;
            }

            if (!NativeMethods.SetForegroundWindow(targetWindow))
            {
                return false;
            }

            // Restore focus to the exact control that had it; activating our picker
            // moves focus off edits like Explorer's Search box or address bar
            RestoreFocus(targetWindow, focusWindow);

            // Wait for the target to actually become foreground before typing -
            // usually one or two ticks - rather than a fixed worst-case delay.
            // Awaited (not slept) so the UI thread keeps pumping.
            var waited = 0;
            while (waited < 250 && NativeMethods.GetForegroundWindow() != targetWindow)
            {
                await Task.Delay(15);
                waited += 15;
            }

            // One extra tick for keyboard focus to settle inside the window
            await Task.Delay(15);
            Logger.Log($"Insert: target foreground after ~{waited}ms");

            if (!NativeMethods.IsWindow(targetWindow))
            {
                return false; // target closed while we waited
            }

            // All key-downs first, then all key-ups: the two halves of a surrogate
            // pair must produce consecutive WM_CHAR messages or the receiving edit
            // control shows two broken characters instead of one emoji
            var inputs = new NativeMethods.INPUT[text.Length * 2];
            for (int i = 0; i < text.Length; i++)
            {
                inputs[i] = UnicodeKeyEvent(text[i], keyUp: false);
                inputs[text.Length + i] = UnicodeKeyEvent(text[i], keyUp: true);
            }

            return NativeMethods.SendInput((uint)inputs.Length, inputs, Marshal.SizeOf<NativeMethods.INPUT>()) == (uint)inputs.Length;
        }

        private static void RestoreFocus(IntPtr targetWindow, IntPtr focusWindow)
        {
            if (focusWindow == IntPtr.Zero || focusWindow == targetWindow || !NativeMethods.IsWindow(focusWindow))
            {
                return;
            }

            var targetThread = NativeMethods.GetWindowThreadProcessId(targetWindow, out _);
            var thisThread = NativeMethods.GetCurrentThreadId();

            // Focus is per input-queue; attach to the target thread so SetFocus takes
            if (targetThread != 0 && targetThread != thisThread && NativeMethods.AttachThreadInput(thisThread, targetThread, true))
            {
                NativeMethods.SetFocus(focusWindow);
                NativeMethods.AttachThreadInput(thisThread, targetThread, false);
            }
            else
            {
                NativeMethods.SetFocus(focusWindow);
            }
        }

        private static bool IsSelfElevated()
        {
            try
            {
                using var identity = WindowsIdentity.GetCurrent();
                return new WindowsPrincipal(identity).IsInRole(WindowsBuiltInRole.Administrator);
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// True when the window's owning process runs elevated (or is so protected
        /// we can't even query it, which implies the same for injection purposes).
        /// </summary>
        private static bool IsWindowElevated(IntPtr window)
        {
            NativeMethods.GetWindowThreadProcessId(window, out var pid);
            if (pid == 0)
            {
                return false;
            }

            var process = NativeMethods.OpenProcess(NativeMethods.ProcessQueryLimitedInformation, false, pid);
            if (process == IntPtr.Zero)
            {
                return true; // can't query at all -> treat as protected
            }

            try
            {
                if (!NativeMethods.OpenProcessToken(process, NativeMethods.TokenQuery, out var token))
                {
                    return true;
                }

                try
                {
                    if (NativeMethods.GetTokenInformation(token, NativeMethods.TokenElevation,
                        out var elevated, sizeof(int), out _))
                    {
                        return elevated != 0;
                    }

                    return false;
                }
                finally
                {
                    NativeMethods.CloseHandle(token);
                }
            }
            finally
            {
                NativeMethods.CloseHandle(process);
            }
        }

        private static NativeMethods.INPUT UnicodeKeyEvent(char codeUnit, bool keyUp)
        {
            return new NativeMethods.INPUT
            {
                type = NativeMethods.InputKeyboard,
                u = new NativeMethods.InputUnion
                {
                    ki = new NativeMethods.KEYBDINPUT
                    {
                        wScan = codeUnit,
                        dwFlags = NativeMethods.KeyEventUnicode | (keyUp ? NativeMethods.KeyEventKeyUp : 0),
                    },
                },
            };
        }
    }
}
