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
        /// Attempts to focus <paramref name="targetWindow"/> and type <paramref name="text"/> into it.
        /// Returns false when there is no usable target (e.g. the window is gone or elevated),
        /// so the caller can fall back to the clipboard.
        /// </summary>
        public static bool TryInsert(IntPtr targetWindow, string text)
        {
            if (targetWindow == IntPtr.Zero || !IsWindow(targetWindow) || !SetForegroundWindow(targetWindow))
            {
                return false;
            }

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
