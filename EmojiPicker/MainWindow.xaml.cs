using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;
using WpfEmojiData = Emoji.Wpf.EmojiData;

namespace EmojiPicker
{
    public partial class MainWindow : Window
    {
        private const int MaxRecentEmojis = 24;
        private const string RecentCategoryKey = "Recent";
        private const string SearchHeader = "Search results";

        // Must match the UniformGrid Columns value in MainWindow.xaml
        private const int GridColumns = 8;

        private static readonly string RecentEmojisFile = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "ClassicEmojiPicker",
            "recent.json");

        // Windows 10's seven picker categories, keyed by the Unicode group names
        // that Emoji.Wpf's EmojiData exposes ("Component" and "Flags" are excluded,
        // as in the original picker)
        private static readonly Dictionary<string, string> GroupToCategory = new Dictionary<string, string>
        {
            ["Smileys & Emotion"] = "Smileys",
            ["Animals & Nature"] = "Smileys",
            ["People & Body"] = "People",
            ["Activities"] = "Celebrations",
            ["Objects"] = "Celebrations",
            ["Food & Drink"] = "Food",
            ["Travel & Places"] = "Transport",
            ["Symbols"] = "Symbols",
        };

        private static readonly List<EmojiCategory> Categories = new List<EmojiCategory>
        {
            new EmojiCategory(RecentCategoryKey, "🕒", "Most recently used"),
            new EmojiCategory("Smileys", "😀", "Smiley faces and animals"),
            new EmojiCategory("People", "👤", "People"),
            new EmojiCategory("Celebrations", "🎉", "Celebrations and objects"),
            new EmojiCategory("Food", "🍕", "Food and plants"),
            new EmojiCategory("Transport", "🚗", "Transportation and places"),
            new EmojiCategory("Symbols", "♥️", "Symbols"),
        };

        private const string DefaultCategoryKey = "Smileys";

        private List<Emoji> allEmojis = new List<Emoji>();
        private List<Emoji> recentEmojis = new List<Emoji>();
        private string currentCategory = DefaultCategoryKey;

        public MainWindow()
        {
            InitializeComponent();
            InitializeEmojis();
            LoadRecentEmojis();

            CategoryTabs.ItemsSource = Categories;
            CategoryTabs.SelectedIndex = Categories.FindIndex(category => category.Key == currentCategory);
        }

        /// <summary>
        /// Brings the picker up ready to search, positioned near the cursor, and
        /// takes foreground. The window is reused across hotkey presses rather
        /// than recreated, so this resets it to a clean state each time.
        /// </summary>
        public void ShowPicker()
        {
            SearchBox.Clear();
            var defaultIndex = Categories.FindIndex(category => category.Key == DefaultCategoryKey);
            if (CategoryTabs.SelectedIndex == defaultIndex)
            {
                LoadCategory(DefaultCategoryKey); // no SelectionChanged will fire; refresh directly
            }
            else
            {
                CategoryTabs.SelectedIndex = defaultIndex;
            }

            PositionNearCursor();

            Show();
            Activate();
            var handle = new WindowInteropHelper(this).Handle;
            if (handle != IntPtr.Zero)
            {
                ForceForeground(handle);
            }

            SearchBox.Focus();
            Keyboard.Focus(SearchBox);
        }

        /// <summary>
        /// Brings our window to the foreground even though the hotkey fired from
        /// another app. A background process can't normally steal focus, so we
        /// briefly attach to the current foreground thread's input queue.
        /// </summary>
        private static void ForceForeground(IntPtr hwnd)
        {
            var foregroundThread = GetWindowThreadProcessId(GetForegroundWindow(), out _);
            var thisThread = GetCurrentThreadId();

            if (foregroundThread != thisThread && foregroundThread != 0)
            {
                AttachThreadInput(foregroundThread, thisThread, true);
                SetForegroundWindow(hwnd);
                AttachThreadInput(foregroundThread, thisThread, false);
            }
            else
            {
                SetForegroundWindow(hwnd);
            }
        }

        private void PositionNearCursor()
        {
            if (!GetCursorPos(out var cursor))
            {
                WindowStartupLocation = WindowStartupLocation.CenterScreen;
                return;
            }

            // Offset so the panel sits below-right of the caret/cursor, and keep
            // it fully on the screen that contains the cursor
            var screen = System.Windows.Forms.Screen.FromPoint(new System.Drawing.Point(cursor.X, cursor.Y));
            var area = screen.WorkingArea;

            var left = cursor.X + 8;
            var top = cursor.Y + 8;
            left = Math.Max(area.Left, Math.Min(left, area.Right - (int)Width));
            top = Math.Max(area.Top, Math.Min(top, area.Bottom - (int)Height));

            WindowStartupLocation = WindowStartupLocation.Manual;
            Left = left;
            Top = top;
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // The Windows 10 picker is ready to search the moment it opens
            SearchBox.Focus();
            Keyboard.Focus(SearchBox);
        }

        private void MainWindow_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // Allow dragging the window when clicking anywhere on it
            if (e.ButtonState == MouseButtonState.Pressed)
            {
                DragMove();
            }
        }

        private void InitializeEmojis()
        {
            // Build the full emoji set from the Unicode database that ships
            // inside Emoji.Wpf, mapped onto the Windows 10 categories
            allEmojis = new List<Emoji>();
            foreach (var group in WpfEmojiData.AllGroups)
            {
                if (!GroupToCategory.TryGetValue(group.Name, out var categoryKey))
                {
                    continue;
                }

                foreach (var subGroup in group.SubGroups)
                {
                    // Windows 10 filed plants under "Food and plants", not with animals
                    var key = subGroup.Name.StartsWith("plant-", StringComparison.Ordinal) ? "Food" : categoryKey;

                    foreach (var emoji in subGroup.EmojiList)
                    {
                        if (emoji.Renderable)
                        {
                            allEmojis.Add(new Emoji(emoji.Text, emoji.Name, key));
                        }
                    }
                }
            }
        }

        private void LoadCategory(string categoryKey)
        {
            if (EmojiGrid == null)
            {
                return; // UI not ready yet
            }

            List<Emoji> categoryEmojis = categoryKey == RecentCategoryKey
                ? recentEmojis.ToList()
                : allEmojis.Where(emoji => emoji.Category == categoryKey).ToList();

            CategoryHeader.Text = Categories.FirstOrDefault(category => category.Key == categoryKey)?.DisplayName ?? categoryKey;
            ShowEmojis(categoryEmojis);
        }

        private void ShowEmojis(List<Emoji> emojis)
        {
            EmojiGrid.ItemsSource = emojis;
            EmojiGrid.SelectedIndex = emojis.Count > 0 ? 0 : -1;
            if (EmojiGrid.SelectedItem != null)
            {
                EmojiGrid.ScrollIntoView(EmojiGrid.SelectedItem);
            }
        }

        private void CategoryTabs_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (CategoryTabs.SelectedItem is EmojiCategory category)
            {
                currentCategory = category.Key;
                if (string.IsNullOrEmpty(SearchBox.Text))
                {
                    LoadCategory(category.Key);
                }
                else
                {
                    SearchBox.Clear(); // triggers TextChanged, which loads the category
                }

                // Typing must keep working after a tab is clicked
                SearchBox.Focus();
            }
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (EmojiGrid == null)
            {
                return; // UI not ready yet
            }

            if (string.IsNullOrWhiteSpace(SearchBox.Text))
            {
                LoadCategory(currentCategory);
            }
            else
            {
                SearchEmojis(SearchBox.Text);
            }
        }

        private void SearchEmojis(string searchText)
        {
            var filteredEmojis = allEmojis
                .Where(emoji => emoji.Name.Contains(searchText, StringComparison.OrdinalIgnoreCase))
                .ToList();

            CategoryHeader.Text = SearchHeader;
            ShowEmojis(filteredEmojis);
        }

        private void MainWindow_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            // Win10 picker behaviour: typing searches while the arrow keys move
            // the grid selection and Enter commits it, wherever focus happens to be
            switch (e.Key)
            {
                case Key.Enter:
                    CommitSelectedEmoji();
                    e.Handled = true;
                    break;
                case Key.Left:
                    MoveSelection(-1);
                    e.Handled = true;
                    break;
                case Key.Right:
                    MoveSelection(1);
                    e.Handled = true;
                    break;
                case Key.Up:
                    MoveSelection(-GridColumns);
                    e.Handled = true;
                    break;
                case Key.Down:
                    MoveSelection(GridColumns);
                    e.Handled = true;
                    break;
            }
        }

        private void MoveSelection(int delta)
        {
            if (EmojiGrid.Items.Count == 0)
            {
                return;
            }

            var index = EmojiGrid.SelectedIndex < 0 ? 0 : EmojiGrid.SelectedIndex + delta;
            EmojiGrid.SelectedIndex = Math.Clamp(index, 0, EmojiGrid.Items.Count - 1);
            EmojiGrid.ScrollIntoView(EmojiGrid.SelectedItem);
        }

        private void CommitSelectedEmoji()
        {
            if (EmojiGrid.SelectedItem is Emoji emoji)
            {
                CommitEmoji(emoji);
            }
        }

        private void EmojiItem_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (sender is ListBoxItem { DataContext: Emoji emoji })
            {
                e.Handled = true;
                CommitEmoji(emoji);
            }
        }

        private void CommitEmoji(Emoji emoji)
        {
            AddToRecentEmojis(emoji);
            DismissPicker();

            // Defer the insertion until the current input event has finished
            // processing: injecting keystrokes from inside a key/mouse handler
            // corrupts the injected Unicode sequence
            Dispatcher.BeginInvoke(new Action(() =>
            {
                // Insert into the app that was focused before the picker opened,
                // like the Windows 10 panel; fall back to the clipboard otherwise
                if (!TextInjector.TryInsert(App.PreviousForegroundWindow, emoji.Character))
                {
                    try
                    {
                        Clipboard.SetText(emoji.Character);
                    }
                    catch (Exception)
                    {
                        // Clipboard can be locked by another process; nothing sensible to do here
                    }
                }
            }), System.Windows.Threading.DispatcherPriority.Background);
        }

        /// <summary>
        /// Hides the resident picker (it is reused on the next hotkey press)
        /// and persists the recents list.
        /// </summary>
        private void DismissPicker()
        {
            SaveRecentEmojis();
            Hide();
        }

        private void MainWindow_Deactivated(object sender, EventArgs e)
        {
            // Dismiss when focus leaves the panel, like the Windows 10 picker
            DismissPicker();
        }

        private void AddToRecentEmojis(Emoji emoji)
        {
            // Remove if already exists
            recentEmojis.RemoveAll(item => item.Character == emoji.Character);

            // Add to beginning
            recentEmojis.Insert(0, emoji);

            // Keep only the most recent MaxRecentEmojis
            if (recentEmojis.Count > MaxRecentEmojis)
            {
                recentEmojis.RemoveAt(recentEmojis.Count - 1);
            }
        }

        private void LoadRecentEmojis()
        {
            try
            {
                if (!File.Exists(RecentEmojisFile))
                {
                    return;
                }

                var characters = JsonSerializer.Deserialize<List<string>>(File.ReadAllText(RecentEmojisFile));
                if (characters == null)
                {
                    return;
                }

                recentEmojis = characters
                    .Select(character => allEmojis.FirstOrDefault(item => item.Character == character))
                    .OfType<Emoji>()
                    .Take(MaxRecentEmojis)
                    .ToList();
            }
            catch (Exception)
            {
                // A corrupt or unreadable recents file should never stop the app from starting
                recentEmojis = new List<Emoji>();
            }
        }

        private void SaveRecentEmojis()
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(RecentEmojisFile)!);
                File.WriteAllText(RecentEmojisFile, JsonSerializer.Serialize(recentEmojis.Select(item => item.Character)));
            }
            catch (Exception)
            {
                // Losing the recents list is not worth interrupting the user for
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            DismissPicker();
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                DismissPicker();
            }
            base.OnKeyDown(e);
        }

        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint processId);

        [DllImport("kernel32.dll")]
        private static extern uint GetCurrentThreadId();

        [DllImport("user32.dll")]
        private static extern bool AttachThreadInput(uint idAttach, uint idAttachTo, bool fAttach);

        [DllImport("user32.dll")]
        private static extern bool GetCursorPos(out POINT lpPoint);

        [StructLayout(LayoutKind.Sequential)]
        private struct POINT
        {
            public int X;
            public int Y;
        }
    }

    public class Emoji
    {
        public string Character { get; }
        public string Name { get; }
        public string Category { get; }

        public Emoji(string character, string name, string category)
        {
            Character = character;
            Name = name;
            Category = category;
        }
    }

    public class EmojiCategory
    {
        public string Key { get; }
        public string Icon { get; }
        public string DisplayName { get; }

        public EmojiCategory(string key, string icon, string displayName)
        {
            Key = key;
            Icon = icon;
            DisplayName = displayName;
        }
    }
}
