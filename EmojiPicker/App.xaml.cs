using System;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;
using System.Windows.Forms;
using Application = System.Windows.Application;
using MessageBox = System.Windows.MessageBox;

namespace EmojiPicker
{
    public partial class App : Application
    {
        private const string MutexName = "ClassicEmojiPicker.SingleInstance";
        private const string ShowEventName = "ClassicEmojiPicker.Show";
        private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
        private const string RunValueName = "ClassicEmojiPicker";

        /// <summary>
        /// The foreground window when the hotkey fired; selected emoji are inserted
        /// into it. Set by the hotkey hook before the picker is shown.
        /// </summary>
        public static IntPtr PreviousForegroundWindow { get; set; }

        /// <summary>
        /// The child control that had keyboard focus when the hotkey fired (e.g.
        /// Explorer's Search or address edit); focus is restored to it on insert.
        /// </summary>
        public static IntPtr PreviousFocusWindow { get; set; }

        /// <summary>
        /// Screen rectangle of the target app's text caret at hotkey time, when it
        /// exposed one; the picker anchors to it (like the Windows 10 panel) instead
        /// of the mouse pointer. Null when unknown - the picker falls back to the mouse.
        /// </summary>
        public static System.Drawing.Rectangle? PreviousCaretRect { get; set; }

        private Mutex? instanceMutex;
        private EventWaitHandle? showEvent;
        private MainWindow? picker;
        private HotkeyListener? hotkey;
        private NotifyIcon? trayIcon;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // Only one resident instance may own the global hook and tray icon
            instanceMutex = new Mutex(true, MutexName, out var isNew);
            if (!isNew)
            {
                // Already running: ask that instance to open the picker (so running
                // the shortcut again behaves like a launcher), then exit
                try
                {
                    EventWaitHandle.OpenExisting(ShowEventName).Set();
                }
                catch (Exception)
                {
                    // The primary may be mid-startup or shutting down; nothing to do
                }

                Shutdown();
                return;
            }

            Logger.Initialize();
            var version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
            Logger.Log($"=== Startup v{version} ===");

            // Record otherwise-silent crashes even when logging is toggled off
            DispatcherUnhandledException += (_, args) =>
                Logger.LogAlways($"FATAL (UI): {args.Exception}");
            AppDomain.CurrentDomain.UnhandledException += (_, args) =>
                Logger.LogAlways($"FATAL: {args.ExceptionObject}");

            // Stay alive with no visible window until the hotkey shows the picker
            ShutdownMode = ShutdownMode.OnExplicitShutdown;

            global::Emoji.Wpf.EmojiData.Load();
            ThemeManager.Initialize();

            picker = new MainWindow();
            picker.PreWarm(); // warm the render path so the first hotkey open is fast

            hotkey = new HotkeyListener();
            hotkey.HotkeyPressed += OnHotkeyPressed;
            hotkey.Start();
            Logger.Log("Keyboard hook installed");

            // Let a second launch (e.g. running the shortcut again) open the picker
            showEvent = new EventWaitHandle(false, EventResetMode.AutoReset, ShowEventName);
            var showThread = new Thread(ShowEventLoop) { IsBackground = true };
            showThread.Start();

            CreateTrayIcon();
        }

        private void OnHotkeyPressed(IntPtr targetWindow, IntPtr focusWindow, System.Drawing.Rectangle? caretRect)
        {
            // The hook captured the foreground window, focused control, and caret at
            // key-press time; the picker inserts the chosen emoji back into it
            Logger.Log($"Hotkey pressed; target={targetWindow} focus={focusWindow} caret={(caretRect.HasValue ? caretRect.Value.ToString() : "none")}");
            PreviousForegroundWindow = targetWindow;
            PreviousFocusWindow = focusWindow;
            PreviousCaretRect = caretRect;
            picker?.ShowPicker();
        }

        private void ShowEventLoop()
        {
            while (showEvent != null && showEvent.WaitOne())
            {
                var target = GetForegroundWindow();
                var focus = TextInjector.GetFocusedControl(target);
                System.Drawing.Rectangle? caret =
                    TextInjector.TryGetCaretRect(target, out var caretRect) ? caretRect : null;
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    Logger.Log("Show requested (run-again)");
                    PreviousForegroundWindow = target;
                    PreviousFocusWindow = focus;
                    PreviousCaretRect = caret;
                    picker?.ShowPicker();
                }));
            }
        }

        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        private void CreateTrayIcon()
        {
            var menu = new ContextMenuStrip();
            menu.Items.Add("Open Emoji Picker", null, (_, _) => ShowPickerFromTray());
            menu.Items.Add(new ToolStripSeparator());

            var startupItem = new ToolStripMenuItem("Start with Windows")
            {
                Checked = IsStartupEnabled(),
                CheckOnClick = true,
            };
            startupItem.CheckedChanged += (_, _) => SetStartupEnabled(startupItem.Checked);
            menu.Items.Add(startupItem);

            var loggingItem = new ToolStripMenuItem("Debug logging")
            {
                Checked = Logger.Enabled,
                ToolTipText = Logger.LogPath,
            };
            loggingItem.Click += (_, _) =>
            {
                var on = Logger.Toggle();
                loggingItem.Checked = on;
                trayIcon?.ShowBalloonTip(
                    4000,
                    "Classic Emoji Picker",
                    on ? $"Debug logging ON\n{Logger.LogPath}" : "Debug logging OFF",
                    ToolTipIcon.Info);
            };
            menu.Items.Add(loggingItem);

            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add("Exit", null, (_, _) => Shutdown());

            trayIcon = new NotifyIcon
            {
                Icon = LoadTrayIcon(),
                Text = "Classic Emoji Picker",
                Visible = true,
                ContextMenuStrip = menu,
            };
            trayIcon.DoubleClick += (_, _) => ShowPickerFromTray();
        }

        private void ShowPickerFromTray()
        {
            // Opened by mouse from the tray: there is no caret to anchor to, and a
            // rect left over from an earlier hotkey press would be misleading
            PreviousCaretRect = null;
            picker?.ShowPicker();
        }

        private static Icon LoadTrayIcon()
        {
            try
            {
                var uri = new Uri("pack://application:,,,/Resources/app.ico");
                var stream = GetResourceStream(uri)?.Stream;
                if (stream != null)
                {
                    return new Icon(stream);
                }
            }
            catch (Exception)
            {
                // Fall through to the shipped application icon
            }

            var exePath = Environment.ProcessPath;
            return (exePath != null ? Icon.ExtractAssociatedIcon(exePath) : null) ?? SystemIcons.Application;
        }

        private static bool IsStartupEnabled()
        {
            try
            {
                using var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(RunKeyPath);
                return key?.GetValue(RunValueName) != null;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private static void SetStartupEnabled(bool enabled)
        {
            try
            {
                using var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(RunKeyPath, writable: true);
                if (key == null)
                {
                    return;
                }

                if (enabled)
                {
                    var exePath = Environment.ProcessPath;
                    if (exePath != null)
                    {
                        key.SetValue(RunValueName, $"\"{exePath}\"");
                    }
                }
                else
                {
                    key.DeleteValue(RunValueName, throwOnMissingValue: false);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Could not update the startup setting: {ex.Message}",
                    "Classic Emoji Picker", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        protected override void OnExit(ExitEventArgs e)
        {
            hotkey?.Dispose();
            ThemeManager.Shutdown();
            showEvent?.Dispose();

            if (trayIcon != null)
            {
                trayIcon.Visible = false;
                trayIcon.Dispose();
            }

            instanceMutex?.Dispose();
            base.OnExit(e);
        }
    }
}
