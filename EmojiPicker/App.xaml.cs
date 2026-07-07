using System;
using System.Drawing;
using System.IO;
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
        private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
        private const string RunValueName = "ClassicEmojiPicker";

        /// <summary>
        /// The window that was focused when the hotkey fired; selected emoji are
        /// inserted into it. Set by the hotkey hook before the picker is shown.
        /// </summary>
        public static IntPtr PreviousForegroundWindow { get; set; }

        private Mutex? instanceMutex;
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
                // Another instance is already running; nothing to do here
                Shutdown();
                return;
            }

            // Stay alive with no visible window until the hotkey shows the picker
            ShutdownMode = ShutdownMode.OnExplicitShutdown;

            global::Emoji.Wpf.EmojiData.Load();
            ThemeManager.Initialize();

            picker = new MainWindow();

            hotkey = new HotkeyListener();
            hotkey.HotkeyPressed += OnHotkeyPressed;
            hotkey.Start();

            CreateTrayIcon();
        }

        private void OnHotkeyPressed(IntPtr targetWindow)
        {
            // The hook captured the focused window at key-press time; the picker
            // inserts the chosen emoji back into it
            PreviousForegroundWindow = targetWindow;
            picker?.ShowPicker();
        }

        private void CreateTrayIcon()
        {
            var menu = new ContextMenuStrip();
            menu.Items.Add("Open emoji picker", null, (_, _) => picker?.ShowPicker());
            menu.Items.Add(new ToolStripSeparator());

            var startupItem = new ToolStripMenuItem("Start with Windows")
            {
                Checked = IsStartupEnabled(),
                CheckOnClick = true,
            };
            startupItem.CheckedChanged += (_, _) => SetStartupEnabled(startupItem.Checked);
            menu.Items.Add(startupItem);

            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add("Exit", null, (_, _) => Shutdown());

            trayIcon = new NotifyIcon
            {
                Icon = LoadTrayIcon(),
                Text = "Classic Emoji Picker",
                Visible = true,
                ContextMenuStrip = menu,
            };
            trayIcon.DoubleClick += (_, _) => picker?.ShowPicker();
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
