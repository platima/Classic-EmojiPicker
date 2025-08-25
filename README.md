<img align="right" src="https://visitor-badge.laobi.icu/badge?page_id=platima.cep" height="20" />

# Classic Emoji Picker v0.1.0

[![Build and Test](https://github.com/platima/Classic-EmojiPicker/actions/workflows/build.yml/badge.svg)](https://github.com/platima/Classic-EmojiPicker/actions/workflows/build.yml)
[![Release](https://github.com/platima/Classic-EmojiPicker/actions/workflows/release.yml/badge.svg)](https://github.com/platima/Classic-EmojiPicker/actions/workflows/release.yml)
[![Latest Release](https://img.shields.io/github/v/release/platima/Classic-EmojiPicker)](https://github.com/platima/Classic-EmojiPicker/releases/latest)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)

A lightweight, standalone recreation of the Windows 10 emoji picker for Windows 11 users who prefer the simpler, cleaner interface without GIFs and bloated features.

## ✅ Current Status: v0.1.0 - Working Release

This version includes core functionality and is ready for daily use:

- ✅ **Builds and runs successfully** on Windows 11
- ✅ **Core emoji picking functionality** working
- ✅ **Three categories** with ~70 popular emojis
- ✅ **Search functionality** by name and keywords
- ✅ **Windows 10 styling** and behavior
- ✅ **One-click copy to clipboard** with auto-minimize

## Features

- **Clean Windows 10 Design** - Faithful recreation of the original Windows 10 emoji picker interface
- **Three Categories** - Smileys & Emotion, People & Body, Objects
- **Real-time Search** - Type to filter emojis by name or keywords
- **One-Click Copy** - Click any emoji to copy it to clipboard
- **Lightweight** - No unnecessary features, GIFs, or bloat
- **Custom Font Support** - Uses bundled Windows 10 emoji font (seguiemj.ttf)
- **Keyboard Shortcuts** - ESC to close, search functionality

## Requirements

- Windows 11 (or Windows 10 version 1809+)
- .NET 8 Runtime (download from Microsoft if not installed)
- Visual Studio 2022 or Visual Studio Code with C# extension (for development)

## Installation

### Option 1: Download Release
Download the latest v0.1.0 release from the [Releases](../../releases) page (coming soon).

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
- **Performance**: Keep memory usage minimal (~119MB target)

## Roadmap

### v0.2.0 (Next Release)
- [ ] Add more emoji categories (Animals, Food, Travel, etc.)
- [ ] Expand emoji database with more emojis  
- [ ] Improved error handling and user feedback

### v0.3.0 (Future)
- [ ] Global hotkey support (Win+. replacement)
- [ ] Recently used emojis section
- [ ] Skin tone modifiers for people emojis

### v1.0.0 (Stable Release)
- [ ] Settings/preferences window
- [ ] Auto-start with Windows option
- [ ] Installer/packaging for easy distribution

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## Acknowledgments

- Original Windows 10 emoji picker design by Microsoft
- Windows 10 emoji font (seguiemj.ttf) by Microsoft
- Built with WPF and .NET 8.0

## Why This Project?

Windows 11's emoji picker became bloated with GIFs, reactions, and other features that many users don't need. This project brings back the simple, clean interface of Windows 10's emoji picker for users who just want to quickly find and copy emojis.
