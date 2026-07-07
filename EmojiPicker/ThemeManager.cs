using System;
using System.Windows;
using Microsoft.Win32;

namespace EmojiPicker
{
    /// <summary>
    /// Keeps the application's brushes in step with the Windows light/dark setting,
    /// swapping the merged theme dictionary live when the user changes it.
    /// </summary>
    internal static class ThemeManager
    {
        private const string PersonalizeKey = @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize";
        private const string AppsUseLightThemeValue = "AppsUseLightTheme";

        private static readonly Uri LightThemeUri = new Uri("Theme/LightTheme.xaml", UriKind.Relative);
        private static readonly Uri DarkThemeUri = new Uri("Theme/DarkTheme.xaml", UriKind.Relative);

        private static ResourceDictionary? currentTheme;
        private static bool isDark;

        /// <summary>
        /// Merges the theme matching the current Windows setting and starts
        /// listening for changes. Call once at startup.
        /// </summary>
        public static void Initialize()
        {
            Apply(IsSystemDark());
            SystemEvents.UserPreferenceChanged += OnUserPreferenceChanged;
        }

        public static void Shutdown()
        {
            SystemEvents.UserPreferenceChanged -= OnUserPreferenceChanged;
        }

        private static void OnUserPreferenceChanged(object sender, UserPreferenceChangedEventArgs e)
        {
            if (e.Category != UserPreferenceCategory.General)
            {
                return;
            }

            var wantDark = IsSystemDark();
            if (wantDark == isDark)
            {
                return;
            }

            // SystemEvents fires on a background thread; touch resources on the UI thread
            Application.Current?.Dispatcher.Invoke(() => Apply(wantDark));
        }

        private static void Apply(bool dark)
        {
            var app = Application.Current;
            if (app == null)
            {
                return;
            }

            var theme = new ResourceDictionary { Source = dark ? DarkThemeUri : LightThemeUri };

            if (currentTheme != null)
            {
                app.Resources.MergedDictionaries.Remove(currentTheme);
            }

            app.Resources.MergedDictionaries.Add(theme);
            currentTheme = theme;
            isDark = dark;
        }

        private static bool IsSystemDark()
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(PersonalizeKey);
                // The value is 1 for light apps, 0 for dark; absent means light
                return key?.GetValue(AppsUseLightThemeValue) is int light && light == 0;
            }
            catch (Exception)
            {
                return false; // default to light if the setting can't be read
            }
        }
    }
}
