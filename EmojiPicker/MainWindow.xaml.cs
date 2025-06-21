using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace EmojiPicker
{
    public partial class MainWindow : Window
    {
        private List<Emoji> allEmojis = new List<Emoji>();
        private string currentCategory = "Smileys";
        private bool isSearchBoxFocused = false;

        public MainWindow()
        {
            InitializeComponent();
            InitializeEmojis();
            LoadCategory(currentCategory);
        }

        private void InitializeEmojis()
        {
            allEmojis = new List<Emoji>
            {
                // Smileys & Emotion
                new Emoji("😊", "Smiling face with smiling eyes", "Smileys", new[] { "smile", "happy", "joy" }),
                new Emoji("😂", "Face with tears of joy", "Smileys", new[] { "laugh", "funny", "tears" }),
                new Emoji("🤣", "Rolling on the floor laughing", "Smileys", new[] { "rofl", "laugh", "funny" }),
                new Emoji("❤️", "Red heart", "Smileys", new[] { "love", "heart", "red" }),
                new Emoji("😍", "Smiling face with heart-eyes", "Smileys", new[] { "love", "heart", "eyes" }),
                new Emoji("😭", "Loudly crying face", "Smileys", new[] { "cry", "sad", "tears" }),
                new Emoji("👌", "OK hand", "Smileys", new[] { "ok", "hand", "good" }),
                new Emoji("😘", "Face blowing a kiss", "Smileys", new[] { "kiss", "love", "heart" }),
                new Emoji("💔", "Broken heart", "Smileys", new[] { "broken", "heart", "sad" }),
                new Emoji("🤔", "Thinking face", "Smileys", new[] { "think", "wondering", "hmm" }),
                new Emoji("👍", "Thumbs up", "Smileys", new[] { "good", "approve", "like" }),
                new Emoji("👎", "Thumbs down", "Smileys", new[] { "bad", "disapprove", "dislike" }),
                new Emoji("😌", "Relieved face", "Smileys", new[] { "relieved", "peaceful", "calm" }),
                new Emoji("😎", "Smiling face with sunglasses", "Smileys", new[] { "cool", "sunglasses", "awesome" }),
                new Emoji("🙌", "Raising hands", "Smileys", new[] { "celebration", "hands", "praise" }),
                new Emoji("🙏", "Folded hands", "Smileys", new[] { "pray", "thanks", "please" }),
                new Emoji("✌️", "Victory hand", "Smileys", new[] { "peace", "victory", "two" }),
                new Emoji("🤞", "Crossed fingers", "Smileys", new[] { "luck", "hope", "wish" }),
                new Emoji("🤟", "Love-you gesture", "Smileys", new[] { "love", "you", "hand" }),
                new Emoji("💗", "Growing heart", "Smileys", new[] { "heart", "growing", "love" }),
                new Emoji("💝", "Heart with ribbon", "Smileys", new[] { "gift", "heart", "love" }),
                new Emoji("🎉", "Party popper", "Smileys", new[] { "party", "celebration", "tada" }),

                // People & Body
                new Emoji("👨", "Man", "People", new[] { "man", "male", "person" }),
                new Emoji("👩", "Woman", "People", new[] { "woman", "female", "person" }),
                new Emoji("👶", "Baby", "People", new[] { "baby", "child", "infant" }),
                new Emoji("👴", "Old man", "People", new[] { "old", "man", "elder" }),
                new Emoji("👵", "Old woman", "People", new[] { "old", "woman", "elder" }),
                new Emoji("🙋", "Person raising hand", "People", new[] { "hand", "question", "raise" }),
                new Emoji("🤷", "Person shrugging", "People", new[] { "shrug", "dunno", "whatever" }),
                new Emoji("🤦", "Person facepalming", "People", new[] { "facepalm", "frustration", "doh" }),

                // Objects
                new Emoji("🎧", "Headphone", "Objects", new[] { "headphone", "music", "audio" }),
                new Emoji("🔥", "Fire", "Objects", new[] { "fire", "flame", "hot" }),
                new Emoji("💎", "Gem stone", "Objects", new[] { "gem", "diamond", "jewel" }),
                new Emoji("🚀", "Rocket", "Objects", new[] { "rocket", "space", "launch" }),
                new Emoji("🎵", "Musical note", "Objects", new[] { "music", "note", "sound" }),
                new Emoji("⚡", "High voltage", "Objects", new[] { "lightning", "electric", "power" }),
                new Emoji("🌟", "Glowing star", "Objects", new[] { "star", "glowing", "sparkle" }),
                new Emoji("🔍", "Magnifying glass tilted left", "Objects", new[] { "search", "magnify", "find" }),
                new Emoji("💻", "Laptop computer", "Objects", new[] { "laptop", "computer", "pc" }),
                new Emoji("📱", "Mobile phone", "Objects", new[] { "phone", "mobile", "cell" }),
                new Emoji("⌚", "Watch", "Objects", new[] { "watch", "time", "clock" }),
                new Emoji("📷", "Camera", "Objects", new[] { "camera", "photo", "picture" }),
                new Emoji("🎮", "Video game", "Objects", new[] { "game", "controller", "gaming" }),
                new Emoji("🎯", "Direct hit", "Objects", new[] { "target", "bullseye", "aim" }),
                new Emoji("🏆", "Trophy", "Objects", new[] { "trophy", "winner", "award" }),
                new Emoji("🎊", "Confetti ball", "Objects", new[] { "confetti", "party", "celebration" })
            };
        }
        private void LoadCategory(string category)
        {
            if (EmojiGrid == null) return; // UI not ready yet

            var categoryEmojis = allEmojis.Where(e => e.Category == category).ToList();
            EmojiGrid.ItemsSource = categoryEmojis;
            UpdateTabSelection(category);
        }
        private void UpdateTabSelection(string category)
        {
            // UI not ready yet
            if (SmileysTab == null || PeopleTab == null || ObjectsTab == null) return;

            // Reset all tabs
            SmileysTab.Style = (Style)FindResource("TabButtonStyle");
            PeopleTab.Style = (Style)FindResource("TabButtonStyle");
            ObjectsTab.Style = (Style)FindResource("TabButtonStyle");

            // Set selected tab
            switch (category)
            {
                case "Smileys":
                    SmileysTab.Style = (Style)FindResource("SelectedTabStyle");
                    break;
                case "People":
                    PeopleTab.Style = (Style)FindResource("SelectedTabStyle");
                    break;
                case "Objects":
                    ObjectsTab.Style = (Style)FindResource("SelectedTabStyle");
                    break;
            }
        }

        private void CategoryTab_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is string category)
            {
                currentCategory = category;
                LoadCategory(category);

                // Clear search if active
                if (isSearchBoxFocused)
                {
                    SearchBox.Text = "Keep typing to find an emoji";
                    SearchBox.Foreground = new SolidColorBrush(Colors.Gray);
                    isSearchBoxFocused = false;
                }
            }
        }

        private void SearchBox_GotFocus(object sender, RoutedEventArgs e)
        {
            if (!isSearchBoxFocused)
            {
                SearchBox.Text = "";
                SearchBox.Foreground = new SolidColorBrush(Colors.Black);
                isSearchBoxFocused = true;
            }
        }

        private void SearchBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(SearchBox.Text))
            {
                SearchBox.Text = "Keep typing to find an emoji";
                SearchBox.Foreground = new SolidColorBrush(Colors.Gray);
                isSearchBoxFocused = false;
                LoadCategory(currentCategory);
            }
        }
        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (EmojiGrid == null) return; // UI not ready yet

            if (isSearchBoxFocused && !string.IsNullOrWhiteSpace(SearchBox.Text))
            {
                SearchEmojis(SearchBox.Text);
            }
            else if (!isSearchBoxFocused)
            {
                LoadCategory(currentCategory);
            }
        }

        private void SearchEmojis(string searchText)
        {
            var filteredEmojis = allEmojis.Where(emoji =>
                emoji.Name.ToLower().Contains(searchText.ToLower()) ||
                emoji.Keywords.Any(keyword => keyword.ToLower().Contains(searchText.ToLower()))
            ).ToList();

            EmojiGrid.ItemsSource = filteredEmojis;
        }

        private void EmojiButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Content is string emoji)
            {
                try
                {
                    System.Windows.Clipboard.SetText(emoji);

                    // Optional: Show brief feedback
                    button.Background = new SolidColorBrush(Color.FromRgb(0, 120, 212));

                    // Reset background after brief moment
                    var timer = new System.Windows.Threading.DispatcherTimer();
                    timer.Interval = TimeSpan.FromMilliseconds(150);
                    timer.Tick += (s, args) =>
                    {
                        button.Background = new SolidColorBrush(Colors.Transparent);
                        timer.Stop();
                    };
                    timer.Start();

                    // Close window after copying (like Windows 10 behavior)
                    this.WindowState = WindowState.Minimized;
                    this.Hide();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error copying emoji: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        protected override void OnKeyDown(System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Escape)
            {
                this.Close();
            }
            base.OnKeyDown(e);
        }
    }

    public class Emoji
    {
        public string Character { get; set; }
        public string Name { get; set; }
        public string Category { get; set; }
        public string[] Keywords { get; set; }

        public Emoji(string character, string name, string category, string[] keywords)
        {
            Character = character;
            Name = name;
            Category = category;
            Keywords = keywords;
        }
    }
}
