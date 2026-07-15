using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Text;
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

            // Simple emoji type reliably everywhere; joined graphemes (ZWJ
            // sequences, flags, skin-tone variants) split into pieces when
            // injected as separate synthetic keystrokes in some apps, so those
            // are pasted instead. The mode is user-configurable.
            var mode = Settings.Current.InsertMode;
            var usePaste = mode == EmojiInsertMode.Paste
                || (mode == EmojiInsertMode.Hybrid && IsMultiScalarGrapheme(text));

            if (usePaste)
            {
                return await PasteViaClipboardAsync(text);
            }

            return SendUnicodeKeystrokes(text);
        }

        private static bool SendUnicodeKeystrokes(string text)
        {
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

        /// <summary>
        /// Places <paramref name="text"/> on the clipboard, sends Ctrl+V so the
        /// target composes it as one string (which synthetic keystrokes fail to do
        /// for joined emoji in some apps), then restores the previous clipboard
        /// text. Both writes are tagged to stay out of Clipboard History (Win+V),
        /// Cloud Clipboard and third-party monitors, so this transient paste does
        /// not pollute the history stack. Non-text clipboard content can't be
        /// preserved across a paste.
        /// </summary>
        private static async Task<bool> PasteViaClipboardAsync(string text)
        {
            string? previousText = null;
            var hadText = false;
            try
            {
                hadText = System.Windows.Clipboard.ContainsText();
                if (hadText)
                {
                    previousText = System.Windows.Clipboard.GetText();
                }
            }
            catch (Exception)
            {
                // Couldn't read the clipboard; carry on but we won't restore it
            }

            if (!SetClipboardTransient(text))
            {
                return false;
            }

            SendCtrlV();

            // Let the target read the clipboard before we put the old text back
            await Task.Delay(150);

            // Restore the previous text (also history-excluded, so the restore
            // doesn't add a duplicate entry). We leave non-text content alone.
            if (hadText && previousText != null)
            {
                SetClipboardTransient(previousText);
            }

            Logger.Log("Paste: inserted via clipboard (Ctrl+V)");
            return true;
        }

        /// <summary>
        /// Puts text on the clipboard tagged so Clipboard History (Win+V), Cloud
        /// Clipboard, and clipboard monitors ignore it - the paste is transient and
        /// must not clobber the user's history stack. Returns false on failure.
        /// </summary>
        private static bool SetClipboardTransient(string text)
        {
            try
            {
                var data = new System.Windows.DataObject();
                data.SetText(text);

                // These well-known formats opt the clipboard entry out of history,
                // cloud sync, and monitor processing. Value 0 (DWORD) = exclude.
                var excludeDword = new byte[] { 0, 0, 0, 0 };
                data.SetData("CanIncludeInClipboardHistory", new MemoryStream(excludeDword));
                data.SetData("CanUploadToCloudClipboard", new MemoryStream(excludeDword));
                data.SetData("ExcludeClipboardContentFromMonitorProcessing", new MemoryStream(new byte[] { 0 }));

                // copy=true renders the formats now so they persist after this call
                System.Windows.Clipboard.SetDataObject(data, copy: true);
                return true;
            }
            catch (Exception ex)
            {
                Logger.Log($"Paste: could not set clipboard ({ex.Message})");
                return false;
            }
        }

        /// <summary>
        /// True when the emoji is a joined grapheme cluster - more than one Unicode
        /// scalar once a trailing emoji-presentation selector (VS16) is discounted.
        /// Covers ZWJ sequences, flags, keycaps and skin-tone variants; false for a
        /// plain single emoji (with or without VS16).
        /// </summary>
        private static bool IsMultiScalarGrapheme(string text)
        {
            var count = 0;
            var lastValue = 0;
            foreach (var rune in text.EnumerateRunes())
            {
                count++;
                lastValue = rune.Value;
            }

            if (lastValue == 0xFE0F)
            {
                count--; // a lone trailing VS16 just requests emoji presentation
            }

            return count > 1;
        }

        private static void SendCtrlV()
        {
            var inputs = new[]
            {
                VirtualKeyEvent(VkControl, keyUp: false),
                VirtualKeyEvent(VkV, keyUp: false),
                VirtualKeyEvent(VkV, keyUp: true),
                VirtualKeyEvent(VkControl, keyUp: true),
            };
            NativeMethods.SendInput((uint)inputs.Length, inputs, Marshal.SizeOf<NativeMethods.INPUT>());
        }

        private const ushort VkControl = 0x11;
        private const ushort VkV = 0x56;

        private static NativeMethods.INPUT VirtualKeyEvent(ushort virtualKey, bool keyUp)
        {
            return new NativeMethods.INPUT
            {
                type = NativeMethods.InputKeyboard,
                u = new NativeMethods.InputUnion
                {
                    ki = new NativeMethods.KEYBDINPUT
                    {
                        wVk = virtualKey,
                        dwFlags = keyUp ? NativeMethods.KeyEventKeyUp : 0,
                    },
                },
            };
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
