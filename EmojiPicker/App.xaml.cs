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

        // Run-again signals arriving this soon after startup are ignored: when
        // both the HKLM (all-users installer) and HKCU (tray toggle) Run values
        // exist, the second logon start would otherwise pop the picker open
        private static readonly TimeSpan StartupShowGrace = TimeSpan.FromSeconds(3);

        private Mutex? instanceMutex;
        private EventWaitHandle? showEvent;
        private MainWindow? picker;
        private HotkeyListener? hotkey;
        private NotifyIcon? trayIcon;
        private Thread? showThread;
        private System.Windows.Threading.DispatcherTimer? hookRearmTimer;
        private volatile bool shuttingDown;
        private DateTime showGraceAnchorUtc;
        private bool graceActive;

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
            Settings.Load();

            // Create the run-again event now, before the heavy warm-up below, so a
            // relaunch during startup can signal it instead of failing to open a
            // not-yet-created event. The auto-reset event latches the signal until
            // the show-event loop starts (after the picker exists).
            showEvent = new EventWaitHandle(false, EventResetMode.AutoReset, ShowEventName);

            // The 3s show-grace only needs to suppress the one duplicate logon start
            // that happens when both the all-users (HKLM) and per-user (HKCU)
            // autostart entries exist. Without that duplicate, a run-again signal is
            // always a genuine user relaunch and should open the picker immediately.
            graceActive = IsStartupEnabled() && IsMachineStartupEnabled();

            // A resident utility should survive a bad frame: log the exception
            // and keep running rather than take Win+. down until relaunch
            DispatcherUnhandledException += (_, args) =>
            {
                Logger.LogAlways($"UNHANDLED (UI, continuing): {args.Exception}");
                args.Handled = true;
                trayIcon?.ShowBalloonTip(4000, "Classic Emoji Picker",
                    $"Something went wrong; details in {Logger.LogPath}", ToolTipIcon.Warning);
            };
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

            // Windows can silently drop a low-level hook (callback timeout, secure
            // desktop, session switch). Re-arm on session change and on a periodic
            // backstop so Win+. recovers without a restart.
            Microsoft.Win32.SystemEvents.SessionSwitch += OnSessionSwitch;
            hookRearmTimer = new System.Windows.Threading.DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(60),
            };
            hookRearmTimer.Tick += (_, _) => hotkey?.Rearm();
            hookRearmTimer.Start();

            // Now that the picker exists, start handling run-again signals (a signal
            // latched during startup is processed here). The event was created above.
            // Anchor the show-grace HERE, not at process start: a duplicate-logon
            // signal latched during a slow warm-up must still fall inside the grace
            // window when the loop finally consumes it, or the picker would pop open.
            showGraceAnchorUtc = DateTime.UtcNow;
            showThread = new Thread(ShowEventLoop) { IsBackground = true };
            showThread.Start();

            CreateTrayIcon();
        }

        private void OnHotkeyPressed(IntPtr targetWindow)
        {
            // Resolve the focused control and caret here (UI thread), off the hook
            // thread. The target app still has focus (our window isn't shown yet),
            // so this is the same state the hook would have captured - but a hung
            // target can no longer stall the low-level hook and get it removed.
            var focusWindow = TextInjector.GetFocusedControl(targetWindow);
            System.Drawing.Rectangle? caretRect =
                TextInjector.TryGetCaretRect(targetWindow, out var rect) ? rect : null;

            Logger.Log($"Hotkey pressed; target={targetWindow} focus={focusWindow} caret={(caretRect.HasValue ? caretRect.Value.ToString() : "none")}");

            // Win+. while the picker is open dismisses it (like the Windows 10
            // panel). Without this, the hook would capture the picker itself as
            // the insertion target and the chosen emoji would go nowhere.
            if (picker != null && targetWindow == new System.Windows.Interop.WindowInteropHelper(picker).Handle)
            {
                if (picker.IsVisible)
                {
                    Logger.Log("Hotkey while open -> toggle dismiss");
                    picker.DismissPicker();
                }

                return; // hidden-but-captured is a stale race; keep the old target
            }

            PreviousForegroundWindow = targetWindow;
            PreviousFocusWindow = focusWindow;
            PreviousCaretRect = caretRect;
            picker?.ShowPicker();
        }

        private void OnSessionSwitch(object sender, Microsoft.Win32.SessionSwitchEventArgs e)
        {
            // SystemEvents raises this on a background thread; re-arm on the UI
            // thread, which owns the hook's message loop
            Dispatcher.BeginInvoke(new Action(() =>
            {
                hotkey?.Rearm();
                Logger.Log($"Session switch ({e.Reason}) -> hook re-armed");
            }));
        }

        private void ShowEventLoop()
        {
            while (showEvent != null && showEvent.WaitOne())
            {
                if (shuttingDown)
                {
                    return;
                }

                // Snapshot the foreground state on this thread, at signal time
                var target = NativeMethods.GetForegroundWindow();
                var focus = TextInjector.GetFocusedControl(target);
                System.Drawing.Rectangle? caret =
                    TextInjector.TryGetCaretRect(target, out var caretRect) ? caretRect : null;

                Dispatcher.BeginInvoke(new Action(() =>
                {
                    // Ignore signals right after startup ONLY when a duplicate logon
                    // start is actually possible (both the HKLM all-users and HKCU
                    // per-user Run values present); that duplicate would otherwise
                    // pop the picker. Otherwise a run-again is a real user relaunch.
                    if (graceActive && DateTime.UtcNow - showGraceAnchorUtc < StartupShowGrace)
                    {
                        Logger.Log("Show requested (run-again) ignored during startup grace");
                        return;
                    }

                    Logger.Log("Show requested (run-again)");

                    // Don't let the picker become its own insertion target when
                    // it is already open; keep whatever target it had
                    if (picker == null || target != new System.Windows.Interop.WindowInteropHelper(picker).Handle)
                    {
                        PreviousForegroundWindow = target;
                        PreviousFocusWindow = focus;
                        PreviousCaretRect = caret;
                    }

                    picker?.ShowPicker();
                }));
            }
        }

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
            if (IsMachineStartupEnabled())
            {
                // An all-users install manages autostart via HKLM, which this
                // per-user toggle can't change - show it as on and read-only
                startupItem.Checked = true;
                startupItem.CheckOnClick = false;
                startupItem.Enabled = false;
                startupItem.ToolTipText = "Enabled for all users by the installer";
            }

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
            // Opened by mouse from the tray: there is no caret to anchor to, and
            // the target/caret left over from an earlier hotkey press would send
            // the emoji to a window the user isn't looking at any more. With no
            // target, a pick falls back to the clipboard - predictable.
            PreviousForegroundWindow = IntPtr.Zero;
            PreviousFocusWindow = IntPtr.Zero;
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

        /// <summary>
        /// True when an all-users install registered autostart under HKLM
        /// (read-only from this per-user process; the tray toggle can't change it).
        /// </summary>
        private static bool IsMachineStartupEnabled()
        {
            try
            {
                using var key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(RunKeyPath);
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
            hookRearmTimer?.Stop();
            try
            {
                Microsoft.Win32.SystemEvents.SessionSwitch -= OnSessionSwitch;
            }
            catch (Exception)
            {
                // SystemEvents teardown is best-effort
            }

            hotkey?.Dispose();
            ThemeManager.Shutdown();

            // Wake the show-event thread so it observes the flag and exits before
            // the handle is disposed (disposing mid-WaitOne throws on that thread)
            shuttingDown = true;
            showEvent?.Set();
            showThread?.Join(1000);
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
