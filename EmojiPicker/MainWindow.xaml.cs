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
using System.Windows.Threading;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;
using WpfEmojiData = Emoji.Wpf.EmojiData;

namespace EmojiPicker
{
    public partial class MainWindow : Window
    {
        private const int MaxRecentEmojis = 24;
        private const string RecentCategoryKey = "Recent";
        private const string SearchHeader = "Search results";

        // Emoji cell footprint in DIPs (40x40 border + 1px margin each side);
        // used to derive the grid's current column count for keyboard nav
        private const double ItemCellWidth = 42.0;

        // How long to wait after the last keystroke before filtering, so typing
        // stays smooth instead of re-rendering the grid on every character
        private static readonly TimeSpan SearchDebounce = TimeSpan.FromMilliseconds(120);

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
            new EmojiCategory("People", "🧑", "People"),
            new EmojiCategory("Celebrations", "🎉", "Celebrations and objects"),
            new EmojiCategory("Food", "🍕", "Food and plants"),
            new EmojiCategory("Transport", "🚗", "Transportation and places"),
            new EmojiCategory("Symbols", "♥️", "Symbols"),
        };

        private const string DefaultCategoryKey = "Smileys";

        private readonly DispatcherTimer searchTimer;
        private List<Emoji> allEmojis = new List<Emoji>();
        private List<Emoji> recentEmojis = new List<Emoji>();
        private string currentCategory = DefaultCategoryKey;
        private bool isShowing;
        private bool recentsDirty;

        public MainWindow()
        {
            InitializeComponent();
            InitializeEmojis();
            LoadRecentEmojis();

            searchTimer = new DispatcherTimer { Interval = SearchDebounce };
            searchTimer.Tick += (_, _) => RunSearch();

            CategoryTabs.ItemsSource = Categories;
            CategoryTabs.SelectedIndex = Categories.FindIndex(category => category.Key == currentCategory);
        }

        // The items panel hosting the emoji cells; cached after the first lookup.
        // Its ActualWidth is the viewport content width (scrollbar excluded),
        // which is stable regardless of scroll position or container recycling.
        private WpfToolkit.Controls.VirtualizingWrapPanel? emojiPanel;

        // Columns currently shown by the virtualizing wrap panel, derived from
        // the panel's width so Up/Down move exactly one visual row. Geometry of
        // realized containers is NOT used: recycled containers report garbage
        // positions once the grid has scrolled, which broke row navigation.
        private int ColumnsPerRow
        {
            get
            {
                if (EmojiGrid.Items.Count == 0)
                {
                    return 1;
                }

                emojiPanel ??= FindVisualChild<WpfToolkit.Controls.VirtualizingWrapPanel>(EmojiGrid);
                if (emojiPanel != null && emojiPanel.ActualWidth > 0)
                {
                    return Math.Max(1, (int)(emojiPanel.ActualWidth / ItemCellWidth));
                }

                // Before first layout: estimate from the ListBox width, allowing
                // for the 10px themed scrollbar (or the estimate lands one high)
                return Math.Max(1, (int)((EmojiGrid.ActualWidth - 12) / ItemCellWidth));
            }
        }

        private static T? FindVisualChild<T>(DependencyObject parent) where T : DependencyObject
        {
            for (var i = 0; i < System.Windows.Media.VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = System.Windows.Media.VisualTreeHelper.GetChild(parent, i);
                if (child is T match)
                {
                    return match;
                }

                if (FindVisualChild<T>(child) is T nested)
                {
                    return nested;
                }
            }

            return null;
        }

        /// <summary>
        /// Renders the window once off-screen at startup so the WPF visual tree and
        /// Emoji.Wpf glyph path are JIT-warmed; the first real hotkey open is then
        /// as fast as subsequent ones instead of paying a cold-start cost.
        /// </summary>
        public void PreWarm()
        {
            WindowStartupLocation = WindowStartupLocation.Manual;
            Left = -32000;
            Top = -32000;
            ShowActivated = false;
            Show();

            Dispatcher.BeginInvoke(
                new Action(() =>
                {
                    Hide();
                    ShowActivated = true;

                    // Startup allocates heavily (emoji database, glyph warm-up);
                    // return it before settling into the tray
                    Dispatcher.BeginInvoke(new Action(MemoryTrimmer.Trim), DispatcherPriority.ContextIdle);
                }),
                DispatcherPriority.Loaded);
        }

        /// <summary>
        /// Brings the picker up ready to search, positioned near the cursor, and
        /// takes foreground. The window is reused across hotkey presses rather
        /// than recreated, so this resets it to a clean state each time.
        /// </summary>
        public void ShowPicker()
        {
            // Ignore focus-loss triggered while we are bringing the window up
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            isShowing = true;

            SearchBox.Clear();

            // Open on Recent when there is history (like the Windows 10 picker),
            // otherwise the first content tab
            var openKey = recentEmojis.Count > 0 ? RecentCategoryKey : DefaultCategoryKey;
            var openIndex = Categories.FindIndex(category => category.Key == openKey);
            if (CategoryTabs.SelectedIndex == openIndex)
            {
                LoadCategory(openKey); // no SelectionChanged will fire; refresh directly
            }
            else
            {
                CategoryTabs.SelectedIndex = openIndex; // fires SelectionChanged -> LoadCategory
            }

            PositionNearCursor();

            Show();
            EnsureOnScreen();
            Activate();
            var handle = new WindowInteropHelper(this).Handle;
            if (handle != IntPtr.Zero)
            {
                ForceForeground(handle);
            }

            SearchBox.Focus();
            Keyboard.Focus(SearchBox);

            Logger.Log($"ShowPicker done in {stopwatch.ElapsedMilliseconds}ms: Left={Left:F0} Top={Top:F0} " +
                $"W={Width} H={Height} foreground={NativeMethods.GetForegroundWindow()} thisHwnd={handle}");

            // Clear the guard once the show/activation storm has settled
            Dispatcher.BeginInvoke(new Action(() => isShowing = false), System.Windows.Threading.DispatcherPriority.Background);
        }

        /// <summary>
        /// Brings our window to the foreground even though the hotkey fired from
        /// another app. A background process can't normally steal focus, so we
        /// briefly attach to the current foreground thread's input queue.
        /// </summary>
        private static void ForceForeground(IntPtr hwnd)
        {
            var foregroundThread = NativeMethods.GetWindowThreadProcessId(NativeMethods.GetForegroundWindow(), out _);
            var thisThread = NativeMethods.GetCurrentThreadId();

            if (foregroundThread != thisThread && foregroundThread != 0)
            {
                NativeMethods.AttachThreadInput(foregroundThread, thisThread, true);
                NativeMethods.SetForegroundWindow(hwnd);
                NativeMethods.AttachThreadInput(foregroundThread, thisThread, false);
            }
            else
            {
                NativeMethods.SetForegroundWindow(hwnd);
            }
        }

        private void PositionNearCursor()
        {
            // Anchor at the target app's text caret when it exposed one at hotkey
            // time (like the Windows 10 panel); otherwise at the mouse pointer.
            // The anchor is a small rectangle: the picker opens below its bottom
            // edge, or above its top edge when there's no room below.
            int anchorX, anchorTop, anchorBottom;
            string anchor;
            if (App.PreviousCaretRect is System.Drawing.Rectangle caret)
            {
                anchorX = caret.Left;
                anchorTop = caret.Top;
                anchorBottom = caret.Bottom;
                anchor = "caret";
            }
            else if (NativeMethods.GetCursorPos(out var cursor))
            {
                anchorX = cursor.X;
                anchorTop = cursor.Y;
                anchorBottom = cursor.Y;
                anchor = "mouse";
            }
            else
            {
                WindowStartupLocation = WindowStartupLocation.CenterScreen;
                return;
            }

            // The anchor and Screen.WorkingArea are in physical pixels, but WPF's
            // Left/Top are in device-independent units. Convert with the window's DPI
            // scale, or the panel lands off-screen on scaled/high-DPI displays.
            var hwnd = new WindowInteropHelper(this).EnsureHandle();
            double scale = NativeMethods.GetDpiForWindow(hwnd) / 96.0;
            if (scale <= 0)
            {
                scale = 1.0;
            }

            var screen = System.Windows.Forms.Screen.FromPoint(new System.Drawing.Point(anchorX, anchorBottom));
            var area = screen.WorkingArea;

            // Work in physical pixels, then convert the top-left to DIPs for WPF.
            var physicalWidth = Width * scale;
            var physicalHeight = Height * scale;
            const int gap = 8;

            // Horizontal: align the picker's left edge with the anchor, clamped
            // within the anchor's monitor.
            double left = Math.Max(area.Left, Math.Min(anchorX + gap, area.Right - physicalWidth));

            // Vertical: prefer below the anchor; if it won't fit (e.g. the caret is
            // near the bottom of the screen), open *above* it like the Windows 10
            // picker; otherwise clamp within the monitor.
            double top;
            string vplace;
            if (anchorBottom + gap + physicalHeight <= area.Bottom)
            {
                top = anchorBottom + gap;
                vplace = "below";
            }
            else if (anchorTop - gap - physicalHeight >= area.Top)
            {
                top = anchorTop - gap - physicalHeight;
                vplace = "above";
            }
            else
            {
                top = Math.Max(area.Top, Math.Min(anchorBottom + gap, area.Bottom - physicalHeight));
                vplace = "clamped";
            }

            WindowStartupLocation = WindowStartupLocation.Manual;
            Left = left / scale;
            Top = top / scale;

            Logger.Log($"PositionNearCursor: anchor={anchor}({anchorX},{anchorTop}-{anchorBottom}) scale={scale} vplace={vplace} " +
                $"area=[{area.Left},{area.Top},{area.Right},{area.Bottom}] => DIP Left={Left:F0} Top={Top:F0}");
        }

        /// <summary>
        /// Last line of defence against the window opening off-screen (e.g. mixed-DPI
        /// monitors): if its bounds fall outside every display, recentre on the primary.
        /// </summary>
        private void EnsureOnScreen()
        {
            double vsLeft = SystemParameters.VirtualScreenLeft;
            double vsTop = SystemParameters.VirtualScreenTop;
            double vsRight = vsLeft + SystemParameters.VirtualScreenWidth;
            double vsBottom = vsTop + SystemParameters.VirtualScreenHeight;

            bool onScreen = Left < vsRight && Left + Width > vsLeft && Top < vsBottom && Top + Height > vsTop;
            if (!onScreen)
            {
                var work = SystemParameters.WorkArea; // primary monitor, in DIPs
                Left = work.Left + ((work.Width - Width) / 2);
                Top = work.Top + ((work.Height - Height) / 2);
                Logger.Log($"EnsureOnScreen: off-screen, recentred to Left={Left:F0} Top={Top:F0}");
            }
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

        // Popularity tier assigned to emoji absent from the frequency data
        // (rarely used, or newer than the dataset and not in its supplement)
        private const int UnrankedPopularity = 99;

        private void InitializeEmojis()
        {
            // Build the full emoji set from the Unicode database that ships
            // inside Emoji.Wpf, mapped onto the Windows 10 categories
            var keywords = LoadResourceMap<string>("keywords.json");
            var popularity = LoadResourceMap<int>("popularity.json");
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
                            var normalized = NormalizeEmoji(emoji.Text);
                            keywords.TryGetValue(normalized, out var tags);
                            var rank = popularity.TryGetValue(normalized, out var tier) ? tier : UnrankedPopularity;
                            allEmojis.Add(new Emoji(emoji.Text, emoji.Name, key, tags ?? string.Empty, rank));
                        }
                    }
                }
            }
        }

        // Emoji.Wpf and the keyword data can differ on the FE0F variation selector;
        // strip it so lookups line up
        private static string NormalizeEmoji(string text) => text.Replace("\uFE0F", string.Empty);

        private static Dictionary<string, T> LoadResourceMap<T>(string fileName)
        {
            try
            {
                var uri = new Uri($"pack://application:,,,/Resources/{fileName}");
                using var stream = Application.GetResourceStream(uri)?.Stream;
                if (stream != null)
                {
                    var map = JsonSerializer.Deserialize<Dictionary<string, T>>(stream);
                    if (map != null)
                    {
                        return map;
                    }
                }
            }
            catch (Exception)
            {
                // Search still works (on names, in category order) without the data
            }

            return new Dictionary<string, T>();
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

            Logger.Log($"LoadCategory '{categoryKey}' -> {categoryEmojis.Count} items");
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

            if (Logger.Enabled)
            {
                // Confirm virtualization: realized containers should stay ~visible-only
                Dispatcher.BeginInvoke(
                    new Action(() =>
                    {
                        var realized = 0;
                        for (var i = 0; i < EmojiGrid.Items.Count; i++)
                        {
                            if (EmojiGrid.ItemContainerGenerator.ContainerFromIndex(i) != null)
                            {
                                realized++;
                            }
                        }

                        Logger.Log($"Grid realized {realized}/{EmojiGrid.Items.Count} containers");
                    }),
                    DispatcherPriority.Loaded);
            }
        }

        private void CategoryTabs_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (CategoryTabs.SelectedItem is EmojiCategory category)
            {
                Logger.Log($"TabSelectionChanged -> {category.Key}");
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

            searchTimer.Stop();
            if (string.IsNullOrWhiteSpace(SearchBox.Text))
            {
                // Clearing the box should restore the category instantly
                LoadCategory(currentCategory);
            }
            else
            {
                // Debounce: filter once typing pauses, not on every keystroke
                searchTimer.Start();
            }
        }

        private void RunSearch()
        {
            searchTimer.Stop();
            if (EmojiGrid == null || string.IsNullOrWhiteSpace(SearchBox.Text))
            {
                return;
            }

            // Trim so an accidental trailing space doesn't zero out the results
            var searchText = SearchBox.Text.Trim();
            if (searchText.Length == 0)
            {
                return;
            }

            // Rank matches by quality, then by real-world usage:
            //  - word-start matches (name or keyword) come before mid-word ones
            //  - within that, order by Unicode usage-frequency tier, with
            //    keyword-only matches handicapped one tier so a hidden tag needs
            //    to be genuinely more popular to outrank a visible name match
            //    (e.g. "spl": 💦's "splash" keyword beats 🖐️ "…fingers splayed",
            //    but "whi": ⚪ "white circle" still beats 💟's "white" tag)
            var scored = new List<(Emoji Emoji, int Tier, int Score, bool IsName, int Index)>();
            for (var i = 0; i < allEmojis.Count; i++)
            {
                var emoji = allEmojis[i];
                var nameWordStart = HasWordStartMatch(emoji.Name, searchText);
                var keywordWordStart = HasWordStartMatch(emoji.Keywords, searchText);
                var nameContains = nameWordStart || emoji.Name.Contains(searchText, StringComparison.OrdinalIgnoreCase);
                var keywordContains = keywordWordStart || emoji.Keywords.Contains(searchText, StringComparison.OrdinalIgnoreCase);

                if (!nameContains && !keywordContains)
                {
                    continue;
                }

                var tier = nameWordStart || keywordWordStart ? 0 : 1;
                var isName = tier == 0 ? nameWordStart : nameContains;
                var score = emoji.Popularity + (isName ? 0 : 1);
                scored.Add((emoji, tier, score, isName, i));
            }

            var filteredEmojis = scored
                .OrderBy(match => match.Tier)
                .ThenBy(match => match.Score)
                .ThenByDescending(match => match.IsName)
                .ThenBy(match => match.Index)
                .Select(match => match.Emoji)
                .ToList();

            Logger.Log($"Search '{searchText}' -> {scored.Count(match => match.Tier == 0)} word-start + " +
                $"{scored.Count(match => match.Tier == 1)} substring; top: " +
                string.Join(", ", filteredEmojis.Take(3).Select(emoji => emoji.Name)));
            CategoryHeader.Text = SearchHeader;
            ShowEmojis(filteredEmojis);
        }

        // True when the query appears at the start of any word (words separated
        // by spaces or hyphens, as in Unicode names and emojibase tags)
        private static bool HasWordStartMatch(string text, string query)
        {
            var index = text.IndexOf(query, StringComparison.OrdinalIgnoreCase);
            while (index >= 0)
            {
                if (index == 0 || text[index - 1] == ' ' || text[index - 1] == '-')
                {
                    return true;
                }

                index = text.IndexOf(query, index + 1, StringComparison.OrdinalIgnoreCase);
            }

            return false;
        }

        private void MainWindow_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            // Win10 picker behaviour: typing searches while the arrow keys move
            // the grid selection and Enter commits it, wherever focus happens to be
            switch (e.Key)
            {
                case Key.Enter:
                    // Apply any pending debounced search so the selection is current
                    if (searchTimer.IsEnabled)
                    {
                        RunSearch();
                    }

                    CommitSelectedEmoji();
                    e.Handled = true;
                    break;
                case Key.Tab:
                    // Cycle categories from the keyboard (the tab strip isn't
                    // reliably clickable when the picker isn't the active window)
                    SwitchCategory((Keyboard.Modifiers & ModifierKeys.Shift) != 0 ? -1 : 1);
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
                    MoveSelection(-ColumnsPerRow);
                    e.Handled = true;
                    break;
                case Key.Down:
                    MoveSelection(ColumnsPerRow);
                    e.Handled = true;
                    break;
            }
        }

        private void SwitchCategory(int direction)
        {
            var count = Categories.Count;
            if (count == 0)
            {
                return;
            }

            // Wrap around; SelectionChanged loads the category and refocuses search
            var next = (((CategoryTabs.SelectedIndex + direction) % count) + count) % count;
            CategoryTabs.SelectedIndex = next;
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
            Logger.Log($"MoveSelection delta={delta} (columns={ColumnsPerRow}) -> index {EmojiGrid.SelectedIndex}");
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

        private void TabItem_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            // Select the category on mouse-up (like emoji commit), which lands even
            // when the picker only has attached-input focus rather than full activation
            if (sender is ListBoxItem { DataContext: EmojiCategory category })
            {
                e.Handled = true;
                var index = Categories.FindIndex(item => item.Key == category.Key);
                if (index >= 0)
                {
                    CategoryTabs.SelectedIndex = index;
                }
            }
        }

        private void CommitEmoji(Emoji emoji)
        {
            Logger.Log($"CommitEmoji: '{emoji.Character}' -> target={App.PreviousForegroundWindow}");
            AddToRecentEmojis(emoji);
            DismissPicker();

            // Defer the insertion until the current input event has finished
            // processing: injecting keystrokes from inside a key/mouse handler
            // corrupts the injected Unicode sequence
            Dispatcher.BeginInvoke(new Action(async () =>
            {
                // Insert into the app that was focused before the picker opened,
                // like the Windows 10 panel; fall back to the clipboard otherwise.
                // Awaited so the focus-settle delay doesn't block the UI thread.
                bool inserted;
                try
                {
                    inserted = await TextInjector.TryInsertAsync(
                        App.PreviousForegroundWindow, App.PreviousFocusWindow, emoji.Character);
                }
                catch (Exception ex)
                {
                    Logger.LogAlways($"Insert threw: {ex}");
                    inserted = false;
                }

                if (!inserted)
                {
                    Logger.Log("Insert failed; falling back to clipboard");
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
        /// and persists the recents list. Public so the hotkey can toggle the
        /// picker closed when pressed while it is already open.
        /// </summary>
        public void DismissPicker()
        {
            searchTimer.Stop(); // no point filtering a hidden grid
            if (recentsDirty)
            {
                SaveRecentEmojis();
                recentsDirty = false;
            }

            Hide();

            // Give the memory back while we idle in the tray; ContextIdle runs
            // after the hide (and any pending insertion) has fully settled
            Dispatcher.BeginInvoke(new Action(MemoryTrimmer.Trim), DispatcherPriority.ContextIdle);
        }

        private void MainWindow_Deactivated(object sender, EventArgs e)
        {
            // Ignore the transient deactivation that can occur while we are still
            // bringing the window to the foreground, or it would hide immediately
            if (isShowing)
            {
                Logger.Log("Deactivated ignored (still showing)");
                return;
            }

            // Dismiss when focus leaves the panel, like the Windows 10 picker
            Logger.Log("Deactivated -> dismiss");
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

            recentsDirty = true;
            Logger.Log($"AddToRecent '{emoji.Character}' -> recents now {recentEmojis.Count}");
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
                // First Esc clears an active search (back to the category);
                // Esc with nothing to clear closes the picker
                if (SearchBox.Text.Trim().Length > 0)
                {
                    SearchBox.Clear(); // TextChanged restores the category
                }
                else
                {
                    DismissPicker();
                }

                e.Handled = true;
            }
            base.OnKeyDown(e);
        }

    }

    public class Emoji
    {
        public string Character { get; }
        public string Name { get; }
        public string Category { get; }

        /// <summary>Extra search terms (emojibase tags), e.g. "splash" for 💦.</summary>
        public string Keywords { get; }

        /// <summary>Usage-popularity tier from Unicode's frequency data
        /// (0 = most used); unranked emoji get a large sentinel value.</summary>
        public int Popularity { get; }

        public Emoji(string character, string name, string category, string keywords, int popularity)
        {
            Character = character;
            Name = name;
            Category = category;
            Keywords = keywords;
            Popularity = popularity;
        }

        // Shown by UI Automation / screen readers for the grid items
        public override string ToString() => Name;
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
