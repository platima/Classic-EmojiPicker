<img align="right" src="https://visitor-badge.laobi.icu/badge?page_id=platima.cep" height="20" />

# Classic Emoji Picker

A lightweight, standalone recreation of the Windows 10 emoji picker for Windows 11 users who prefer the simpler, cleaner interface without GIFs and bloated features.

## Features

- **Clean Windows 10 Design** - Faithful recreation of the original Windows 10 emoji picker interface
- **Three Categories** - Smileys & Emotion, People & Body, Objects
- **Real-time Search** - Type to filter emojis by name or keywords
- **One-Click Copy** - Click any emoji to copy it to clipboard
- **Lightweight** - No unnecessary features, GIFs, or bloat
- **Custom Font Support** - Uses bundled Windows 10 emoji font (seguiemj.ttf)
- **Keyboard Shortcuts** - ESC to close, search functionality

## Requirements

- Windows 11 (or Windows 10)
- .NET Framework 4.8 (included with Windows 11)
- Visual Studio 2019/2022 or Visual Studio Code (for development)

## Installation

### Option 1: Download Release (Coming Soon)
Download the latest release from the [Releases](../../releases) page.

### Option 2: Build from Source

1. **Clone the repository:**
   ```bash
   git clone https://github.com/platima/Classic-EmojiPicker.git
   cd windows10-emoji-picker
   ```

2. **Open in Visual Studio:**
   - Open `EmojiPicker.sln` in Visual Studio
   - Ensure you have the `seguiemj.ttf` font file in the `Fonts/` directory
   - Build → Build Solution (Ctrl+Shift+B)
   - Run → Start Debugging (F5)

3. **Alternative - Visual Studio Code:**
   - Open the project folder in VS Code
   - Use the .NET build commands or integrated terminal

## Usage

1. **Launch** the EmojiPicker.exe
2. **Browse** categories using the tabs at the top
3. **Search** by typing in the search box
4. **Click** any emoji to copy it to your clipboard
5. **Paste** the emoji anywhere (Ctrl+V)

The window will automatically minimize after copying an emoji, just like the original Windows 10 behavior.

## Font Configuration

The application is designed to use the Windows 10 emoji font (`seguiemj.ttf`) by default. If you don't have this font:

1. **Bundled Font**: The project includes the font file in `Fonts/seguiemj.ttf`
2. **System Font**: The app will fallback to the system's Segoe UI Emoji font
3. **Custom Font**: Replace the font file in the Fonts directory with your preferred emoji font

## Development

### Project Structure
```
EmojiPicker/
├── MainWindow.xaml          # Main UI layout
├── MainWindow.xaml.cs       # UI logic and emoji handling
├── App.xaml                 # Application definition
├── App.xaml.cs             # Application startup
├── Fonts/
│   └── seguiemj.ttf        # Windows 10 emoji font
└── Properties/
    └── AssemblyInfo.cs     # Assembly metadata
```

### Adding Emojis

To add more emojis, edit the `InitializeEmojis()` method in `MainWindow.xaml.cs`:

```csharp
new Emoji("🎉", "Party popper", "Smileys", new[] { "party", "celebration", "tada" }),
```

### Categories

Current categories:
- **Smileys** - Smileys & Emotion
- **People** - People & Body  
- **Objects** - Objects & Symbols

## Contributing

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/amazing-feature`)
3. Commit your changes (`git commit -m 'Add amazing feature'`)
4. Push to the branch (`git push origin feature/amazing-feature`)
5. Open a Pull Request

## Roadmap

- [ ] Add more emoji categories (Animals, Food, Travel, etc.)
- [ ] Expand emoji database with more emojis
- [ ] Global hotkey support (Win+. replacement)
- [ ] Recently used emojis
- [ ] Skin tone modifiers
- [ ] Settings/preferences window
- [ ] Auto-start with Windows option

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## Acknowledgments

- Original Windows 10 emoji picker design by Microsoft
- Windows 10 emoji font (seguiemj.ttf) by Microsoft
- Built with WPF and .NET Framework 4.8

## Why This Project?

Windows 11's emoji picker became bloated with GIFs, reactions, and other features that many users don't need. This project brings back the simple, clean interface of Windows 10's emoji picker for users who just want to quickly find and copy emojis.
