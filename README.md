<img align="right" src="https://visitor-badge.laobi.icu/badge?page_id=platima.cep" height="20" />

# Classic Emoji Picker

[![Build](https://github.com/platima/Classic-EmojiPicker/actions/workflows/build.yml/badge.svg)](https://github.com/platima/Classic-EmojiPicker/actions/workflows/build.yml)
[![Release](https://github.com/platima/Classic-EmojiPicker/actions/workflows/release.yml/badge.svg)](https://github.com/platima/Classic-EmojiPicker/actions/workflows/release.yml)
[![Latest Release](https://img.shields.io/github/v/release/platima/Classic-EmojiPicker)](https://github.com/platima/Classic-EmojiPicker/releases/latest)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)

A lightweight, standalone recreation of the Windows 10 emoji picker for Windows 11 users who prefer the simpler, cleaner interface without GIFs and bloated features.

## ✅ Current Status: v0.1.1 - Working Release

This version includes core functionality and is ready for daily use:

- ✅ **Replaces Win+.** - takes over the emoji shortcut so it opens this picker instead of the built-in panel
- ✅ **Full Unicode emoji set** (~1,400 emoji) in the original seven Windows 10 categories
- ✅ **Direct insertion** - the selected emoji is typed into the app you were using
- ✅ **Dark mode** - automatically follows the Windows light/dark setting
- ✅ **Search functionality** by emoji name, focused and ready as soon as the picker opens
- ✅ **Keyboard driven** - arrow keys move the selection, Enter inserts, ESC closes
- ✅ **Installer** with optional start-with-Windows

## Features

- **Clean Windows 10 Design** - Faithful recreation of the original Windows 10 emoji picker interface
- **Win+. Takeover** - Runs quietly in the system tray and opens on Win+. in place of the Windows 11 panel
- **The Original Seven Categories** - Recent, Smiley faces & animals, People, Celebrations & objects, Food & plants, Transportation & places, Symbols
- **Full Emoji Set** - Every renderable Unicode emoji from the database bundled with Emoji.Wpf (flags excluded, as in the Windows 10 picker)
- **Direct Insertion** - Picking an emoji types it into the previously focused window; falls back to copying to the clipboard when no target exists
- **Dark Mode** - Follows the system theme automatically and switches live when you change it
- **Recent Emojis** - Recently used emojis are remembered between sessions
- **Real-time Search** - Just start typing to filter emojis by name or keyword (e.g. "splash" finds 💦)
- **Keyboard Navigation** - Arrow keys move the highlighted emoji, Tab/Shift+Tab switch category, Enter inserts, ESC closes
- **Lightweight** - No unnecessary features, GIFs, or bloat
- **True Colour Emoji** - Rendered with [Emoji.Wpf](https://github.com/samhocevar/emoji.wpf) using the system Segoe UI Emoji font

## Requirements

- Windows 11 (or Windows 10 version 1809+)
- The installer is self-contained - no separate .NET runtime is required
- For building from source: .NET 8 SDK and Visual Studio 2022 or VS Code with the C# extension

## Installation

### Option 1: Installer (recommended)

1. Download `EmojiPicker-Setup-<version>.exe` from the [Releases](../../releases) page.
2. Run it (a per-user install - no administrator prompt).
3. Tick **Start with Windows** when asked so Win+. works after every sign-in.

Classic Emoji Picker then lives in the system tray. Press **Win+.** anywhere to open it. Right-click the tray icon for **Open**, a **Start with Windows** toggle, and **Exit**. **Shift+right-click** the tray icon toggles a diagnostic log at `%APPDATA%\ClassicEmojiPicker\debug.log` if you ever need to troubleshoot.

> **Restoring the built-in panel:** while Classic Emoji Picker is running it intercepts Win+.. Choose **Exit** from the tray icon (and untick Start with Windows) to hand the shortcut back to Windows.

### Option 2: Build from Source

1. **Clone the repository:**
   ```bash
   git clone https://github.com/platima/Classic-EmojiPicker.git
   cd Classic-EmojiPicker
   ```

2. **Open in Visual Studio:**
   - Open `Classic-EmojiPicker.sln` in Visual Studio
   - Build → Build Solution (Ctrl+Shift+B)
   - Run → Start Debugging (F5)

3. **Alternative - Visual Studio Code:**
   - Open the project folder in VS Code
   - Use the .NET build commands or integrated terminal

## Usage

1. In any app, press **Win+.** (or double-click the tray icon) to open the picker where your cursor is
2. **Type** to search - the search box is focused from the start
3. **Browse** categories with **Tab / Shift+Tab** (or click the tabs at the bottom); move the highlight with the arrow keys
4. **Press Enter** (or click an emoji) to insert it into the app you were using
5. If there is no target window, the emoji is copied to the clipboard instead

The picker hides after inserting an emoji (or when it loses focus, or on ESC) and waits in the tray for the next Win+.. Recently used emojis are saved to `%APPDATA%\ClassicEmojiPicker\recent.json` and shown in the Recent tab 🕒.

## How the Win+. takeover works

The app installs a low-level keyboard hook that catches Win+. before the Windows shell does, opens this picker, and swallows the keystroke so the built-in panel doesn't also appear. This means the app must be running (it sits in the tray) for the shortcut to work - the installer's **Start with Windows** option keeps it available after every sign-in.

## Emoji Rendering

Colour emoji rendering is handled by the [Emoji.Wpf](https://github.com/samhocevar/emoji.wpf) library, which uses the system's Segoe UI Emoji font (present on all supported versions of Windows). No bundled fonts are required.

## Development

### Project Structure
```
EmojiPicker/
├── MainWindow.xaml(.cs)     # Picker UI + emoji/keyboard/insertion logic
├── App.xaml(.cs)           # Tray host: single instance, hook, theme, lifecycle
├── HotkeyListener.cs       # Global Win+. keyboard hook (WH_KEYBOARD_LL)
├── TextInjector.cs         # Types the emoji into the previously focused window
├── ThemeManager.cs         # Follows the Windows light/dark setting
├── Theme/                  # LightTheme.xaml / DarkTheme.xaml brush dictionaries
├── Resources/app.ico       # Application + tray icon
└── Properties/AssemblyInfo.cs
installer/
└── EmojiPicker.iss         # Inno Setup script (built in the release workflow)
```

### Code Quality & Standards

The project follows enterprise-grade code quality practices:

#### **Formatting & Style**
- **EditorConfig** (`.editorconfig`) - Enforces consistent code formatting
- **C# Conventions** - PascalCase naming, 4-space indentation
- **Auto-formatting** - `dotnet format` for automatic code cleanup

#### **Code Analysis**
- **Static Analysis** - .NET analyzers enabled with latest rules
- **Build Validation** - Warnings and errors caught during build
- **Performance Checks** - Memory and startup optimisation validation

#### **Quality Assurance Tools**
```powershell
# Local code quality check (equivalent to cppcheck)
.\code-quality-simple.ps1

# Auto-fix formatting issues
dotnet format

# Manual detailed analysis
dotnet build --configuration Release --verbosity normal
```

#### **CI/CD Integration**
- **GitHub Actions** - Automated build and quality checks
- **Release Workflow** - Quality gates before releases
- **Format Verification** - Prevents improperly formatted code

### Emoji Data

The emoji list is not hardcoded: it comes from the Unicode emoji database embedded in Emoji.Wpf (`EmojiData.AllGroups`), filtered to emoji the system font can render.

### Categories

The Unicode groups are mapped onto the original Windows 10 categories in `GroupToCategory` in `MainWindow.xaml.cs`:
- 🕒 **Recent** - Most recently used (persisted)
- 😀 **Smileys** - Smileys & Emotion + Animals & Nature
- 👤 **People** - People & Body
- 🎉 **Celebrations** - Activities + Objects
- 🍕 **Food** - Food & Drink + plants
- 🚗 **Transport** - Travel & Places
- ♥️ **Symbols** - Symbols

Flags are excluded, matching the Windows 10 picker.

## Contributing

We welcome contributions! Please follow these guidelines:

### **Development Setup**
1. Fork the repository
2. Clone your fork: `git clone https://github.com/platima/Classic-EmojiPicker.git`
3. Open in Visual Studio 2022 or VS Code
4. Ensure .NET 8 SDK is installed

### **Code Quality Standards**
Before submitting, ensure your code meets quality standards:

```powershell
# Run full quality check
.\code-quality-simple.ps1

# Fix formatting automatically
dotnet format

# Build and test
dotnet build --configuration Release
```

### **Contribution Workflow**
1. Create a feature branch (`git checkout -b feature/amazing-feature`)
2. Make your changes following the coding standards
3. Run code quality checks locally
4. Commit your changes (`git commit -m 'Add amazing feature'`)
5. Push to the branch (`git push origin feature/amazing-feature`)
6. Open a Pull Request

### **Pull Request Guidelines**
- ✅ Code quality checks must pass
- ✅ Follow existing code style (enforced by EditorConfig)
- ✅ Add appropriate comments and documentation
- ✅ Keep changes focused and atomic
- ✅ Update CHANGELOG.md if needed

### **Code Style**
- **Language**: C# 12 with modern features
- **Formatting**: Enforced by `.editorconfig` and `dotnet format`
- **Naming**: PascalCase for public members, camelCase for private
- **Comments**: Australian English, clear and concise
- **Performance**: Keep the resident (idle-in-tray) footprint modest

## Roadmap

### v0.2.0 (Next Release)
- [x] Add more emoji categories (Animals, Food, Travel, etc.)
- [x] Expand emoji database with more emojis
- [ ] Improved error handling and user feedback

### v0.3.0 (Future)
- [x] Global hotkey support (Win+. replacement)
- [x] Dark mode
- [x] Installer/packaging for easy distribution
- [x] Auto-start with Windows option
- [ ] Skin tone modifiers for people emojis
- [x] Search by keyword/alias (matches emoji names and keyword tags)

### v1.0.0 (Stable Release)
- [ ] Settings/preferences window
- [ ] Configurable hotkey

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## Acknowledgments

- Original Windows 10 emoji picker design by Microsoft
- Colour emoji rendering by [Emoji.Wpf](https://github.com/samhocevar/emoji.wpf) (Sam Hocevar)
- Grid virtualization by [VirtualizingWrapPanel](https://github.com/sbaeumlisberger/VirtualizingWrapPanel) (S. Bäumlisberger)
- Search keyword data from [emojibase](https://github.com/milesj/emojibase) (MIT), derived from Unicode CLDR
- Built with WPF and .NET 8.0

## Why This Project?

Windows 11's emoji picker became bloated with GIFs, reactions, and other features that many users don't need. This project brings back the simple, clean interface of Windows 10's emoji picker for users who just want to quickly find and copy emojis.
