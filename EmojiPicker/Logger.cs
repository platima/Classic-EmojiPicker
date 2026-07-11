using System;
using System.IO;

namespace EmojiPicker
{
    /// <summary>
    /// Lightweight opt-in file logger for diagnosing runtime issues (the picker
    /// not appearing, hotkey/foreground problems, etc.). Logging is off by default
    /// and toggled via the tray icon's "Debug logging" menu item; the state
    /// persists between runs through a marker file. Fatal exceptions are always recorded.
    /// </summary>
    internal static class Logger
    {
        // Rotate before the log grows unbounded (it's opt-in, but users forget
        // to turn it off); the previous log is kept once as debug.old.log
        private const long MaxLogBytes = 5 * 1024 * 1024;

        private static readonly object Gate = new object();

        private static readonly string Dir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "ClassicEmojiPicker");

        private static readonly string EnabledMarker = Path.Combine(Dir, "debug.enabled");

        public static string LogPath { get; } = Path.Combine(Dir, "debug.log");

        public static bool Enabled { get; private set; }

        /// <summary>Reads the persisted on/off state. Call once at startup.</summary>
        public static void Initialize()
        {
            try
            {
                Enabled = File.Exists(EnabledMarker);
            }
            catch (Exception)
            {
                Enabled = false;
            }

            if (Enabled)
            {
                Log("--- logging resumed (enabled) ---");
            }
        }

        /// <summary>Flips logging on/off, persists it, and returns the new state.</summary>
        public static bool Toggle()
        {
            try
            {
                Directory.CreateDirectory(Dir);
                if (Enabled)
                {
                    File.Delete(EnabledMarker);
                    Log("--- logging disabled ---"); // recorded before we stop
                    Enabled = false;
                }
                else
                {
                    File.WriteAllText(EnabledMarker, DateTime.Now.ToString("o"));
                    Enabled = true;
                    Log("--- logging enabled ---");
                }
            }
            catch (Exception)
            {
                // Toggling the marker is best-effort; never throw from the tray handler
            }

            return Enabled;
        }

        public static void Log(string message)
        {
            if (Enabled)
            {
                Write(message);
            }
        }

        /// <summary>Writes regardless of the toggle - used for fatal errors.</summary>
        public static void LogAlways(string message)
        {
            Write(message);
        }

        private static void Write(string message)
        {
            try
            {
                lock (Gate)
                {
                    Directory.CreateDirectory(Dir);

                    var info = new FileInfo(LogPath);
                    if (info.Exists && info.Length > MaxLogBytes)
                    {
                        File.Move(LogPath, Path.ChangeExtension(LogPath, ".old.log"), overwrite: true);
                    }

                    File.AppendAllText(LogPath, $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}  {message}{Environment.NewLine}");
                }
            }
            catch (Exception)
            {
                // Logging must never crash the app or interrupt the user
            }
        }
    }
}
