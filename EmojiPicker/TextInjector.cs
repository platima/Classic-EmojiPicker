using System;
using System.Runtime.InteropServices;
using System.Threading;

namespace EmojiPicker
{
    /// <summary>
    /// Types text into the window that was focused before the picker opened,
    /// mimicking the Windows 10 emoji panel's insert behaviour.
    /// </summary>
    internal static class TextInjector
    {
        private const uint InputKeyboard = 1;
        private const uint KeyEventKeyUp = 0x0002;
        private const uint KeyEventUnicode = 0x0004;

        [DllImport("user32.dll")]
        internal static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool IsWindow(IntPtr hWnd);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);

        [DllImport("user32.dll")]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint processId);

        [DllImport("kernel32.dll")]
        private static extern uint GetCurrentThreadId();

        [DllImport("user32.dll")]
        private static extern bool AttachThreadInput(uint idAttach, uint idAttachTo, bool fAttach);

        [DllImport("user32.dll")]
        private static extern IntPtr SetFocus(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool GetGUIThreadInfo(uint idThread, ref GUITHREADINFO lpgui);

        [DllImport("user32.dll")]
        private static extern bool ClientToScreen(IntPtr hWnd, ref System.Drawing.Point lpPoint);

        [StructLayout(LayoutKind.Sequential)]
        private struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct GUITHREADINFO
        {
            public int cbSize;
            public uint flags;
            public IntPtr hwndActive;
            public IntPtr hwndFocus;
            public IntPtr hwndCapture;
            public IntPtr hwndMenuOwner;
            public IntPtr hwndMoveSize;
            public IntPtr hwndCaret;
            public RECT rcCaret;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct INPUT
        {
            public uint type;
            public InputUnion u;
        }

        [StructLayout(LayoutKind.Explicit)]
        private struct InputUnion
        {
            [FieldOffset(0)] public MOUSEINPUT mi;
            [FieldOffset(0)] public KEYBDINPUT ki;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct MOUSEINPUT
        {
            public int dx;
            public int dy;
            public uint mouseData;
            public uint dwFlags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct KEYBDINPUT
        {
            public ushort wVk;
            public ushort wScan;
            public uint dwFlags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

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

            var threadId = GetWindowThreadProcessId(topLevel, out _);
            var gui = new GUITHREADINFO { cbSize = Marshal.SizeOf<GUITHREADINFO>() };
            if (threadId != 0 && GetGUIThreadInfo(threadId, ref gui) && gui.hwndFocus != IntPtr.Zero)
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

            var threadId = GetWindowThreadProcessId(topLevel, out _);
            var gui = new GUITHREADINFO { cbSize = Marshal.SizeOf<GUITHREADINFO>() };
            if (threadId == 0 || !GetGUIThreadInfo(threadId, ref gui) || gui.hwndCaret == IntPtr.Zero)
            {
                return false;
            }

            // rcCaret is in hwndCaret's client coordinates; convert both corners
            var topLeft = new System.Drawing.Point(gui.rcCaret.Left, gui.rcCaret.Top);
            var bottomRight = new System.Drawing.Point(gui.rcCaret.Right, gui.rcCaret.Bottom);
            if (!ClientToScreen(gui.hwndCaret, ref topLeft) || !ClientToScreen(gui.hwndCaret, ref bottomRight))
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
        /// when there is no usable target (e.g. the window is gone or elevated), so the caller can
        /// fall back to the clipboard.
        /// </summary>
        public static bool TryInsert(IntPtr targetWindow, IntPtr focusWindow, string text)
        {
            if (targetWindow == IntPtr.Zero || !IsWindow(targetWindow) || !SetForegroundWindow(targetWindow))
            {
                return false;
            }

            // Restore focus to the exact control that had it; activating our picker
            // moves focus off edits like Explorer's Search box or address bar
            RestoreFocus(targetWindow, focusWindow);

            // Give the target window a moment to take keyboard focus before typing
            Thread.Sleep(250);

            // All key-downs first, then all key-ups: the two halves of a surrogate
            // pair must produce consecutive WM_CHAR messages or the receiving edit
            // control shows two broken characters instead of one emoji
            var inputs = new INPUT[text.Length * 2];
            for (int i = 0; i < text.Length; i++)
            {
                inputs[i] = UnicodeKeyEvent(text[i], keyUp: false);
                inputs[text.Length + i] = UnicodeKeyEvent(text[i], keyUp: true);
            }

            return SendInput((uint)inputs.Length, inputs, Marshal.SizeOf<INPUT>()) == (uint)inputs.Length;
        }

        private static void RestoreFocus(IntPtr targetWindow, IntPtr focusWindow)
        {
            if (focusWindow == IntPtr.Zero || focusWindow == targetWindow || !IsWindow(focusWindow))
            {
                return;
            }

            var targetThread = GetWindowThreadProcessId(targetWindow, out _);
            var thisThread = GetCurrentThreadId();

            // Focus is per input-queue; attach to the target thread so SetFocus takes
            if (targetThread != 0 && targetThread != thisThread && AttachThreadInput(thisThread, targetThread, true))
            {
                SetFocus(focusWindow);
                AttachThreadInput(thisThread, targetThread, false);
            }
            else
            {
                SetFocus(focusWindow);
            }
        }

        private static INPUT UnicodeKeyEvent(char codeUnit, bool keyUp)
        {
            return new INPUT
            {
                type = InputKeyboard,
                u = new InputUnion
                {
                    ki = new KEYBDINPUT
                    {
                        wScan = codeUnit,
                        dwFlags = KeyEventUnicode | (keyUp ? KeyEventKeyUp : 0),
                    },
                },
            };
        }
    }
}
