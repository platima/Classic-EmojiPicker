# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased - v0.1.1] - 2025-06-21

### Added (2026-07-07, session 3)
- **Win+. takeover** - a low-level keyboard hook intercepts Win+., opens this picker, and suppresses the built-in Windows emoji panel
- **System-tray resident app** - runs quietly in the tray (Open / Start with Windows / Exit); the picker window is now shown and hidden per hotkey instead of relaunched
- **Dark mode** - follows the Windows light/dark setting and switches live via `ThemeManager`, with `Theme/LightTheme.xaml` and `Theme/DarkTheme.xaml`
- **Cursor-anchored positioning** - the picker opens near the caret/cursor like the original panel
- **Inno Setup installer** (`installer/EmojiPicker.iss`) with an optional "Start with Windows" task; built and attached to releases by the release workflow
- **Application/tray icon** (`Resources/app.ico`)
- **Single-instance guard** so only one resident process owns the hook and tray icon

### Changed (2026-07-07, session 3)
- Selecting an emoji now hides the picker (resident) rather than closing the process; recents are saved on hide
- The picker dismisses when it loses focus, matching the Windows 10 panel

### Added (2026-07-07, session 2)
- **Full Unicode emoji set** (~1,400 renderable emoji) from the database bundled with Emoji.Wpf, replacing the ~50 hardcoded emoji
- **The original seven Windows 10 categories** - Recent, Smiley faces & animals, People, Celebrations & objects, Food & plants, Transportation & places, Symbols (flags excluded, as in Windows 10)
- **Direct insertion** - picking an emoji types it into the previously focused window via SendInput, with clipboard fallback
- **Keyboard navigation** - arrow keys move the grid selection, Enter inserts the highlighted emoji, focus stays in the search box
- **Search box focused on launch** - start typing immediately, like the original panel
- **Category header** above the grid, matching the Windows 10 layout

### Changed (2026-07-07)
- Clicking an emoji now inserts it instead of copying to the clipboard (clipboard is the fallback)
- Window resized to a compact 400x440 with a fixed 8-column grid, closer to the original panel
- Search matches emoji names from the Unicode database (keyword search planned)

### Fixed (2026-07-07)
- Emoji inserted as surrogate pairs no longer arrive corrupted (key events are injected after the triggering input event completes)
- **Build failure** - Removed the unfinished custom COLR/CPAL renderer (`EmojiImage.cs`) that referenced APIs missing from its font library and only ever drew placeholder rectangles
- **Colour emoji rendering** - Now handled by the [Emoji.Wpf](https://github.com/samhocevar/emoji.wpf) library using the system Segoe UI Emoji font
- **Window dragging** - The drag surface was hidden behind the main panel, so the window could not be moved; dragging is now handled by the window itself
- **Recent tab** - Recent emojis are now saved to `%APPDATA%\ClassicEmojiPicker\recent.json` and survive restarts (previously they were lost when the app closed after each pick)
- **Search box** - Replaced the fragile placeholder-text/focus juggling with a proper watermark overlay
- **CI workflow** - Runs on `windows-latest`, and the redundant duplicate-build and no-op test steps were removed
- Removed the bundled `seguiemj.ttf` (2 MB, unused and not redistributable); the system font is used instead

### Added
- ✅ **Borderless window design** - Removed title bar for authentic Windows 10 appearance
- ✅ **Bottom category tabs** - Moved emoji categories to bottom with improved layout
- ✅ **Recent emojis tab** 🕒 - Track and display recently used emojis (up to 24)
- ✅ **Enhanced visual design** - Rounded corners, drop shadow, and modern styling
- ✅ **Improved emoji rendering** - Better font rendering settings for colour emoji display
- ✅ **Instant close behavior** - Application closes immediately after emoji selection

### Changed
- Moved category tabs from top to bottom for Windows 10 authenticity
- Application now closes immediately instead of minimizing after emoji copy
- Enhanced visual appearance with modern rounded design
- Recent emojis are now tracked and displayed in dedicated tab

### Technical
- Added Recent emoji tracking with configurable maximum (24 emojis)
- Improved emoji button styling for better colour rendering
- Added drop shadow effects and rounded corners
- Enhanced window transparency and borderless design

### v0.2.0 (Future)
- Global hotkey support (Win+. replacement)
- More emoji categories (Animals, Food, Travel, etc.)
- Skin tone modifiers for people emojis
- Settings/preferences window
- Auto-start with Windows option

### Added (Development)
- Comprehensive code quality and formatting standards
- EditorConfig for consistent code formatting across editors
- PowerShell script for local code quality checking (`code-quality-simple.ps1`)
- Enhanced GitHub Actions with code analysis and format verification
- .NET static code analysis with latest rules
- Automated code formatting with `dotnet format`
- Enterprise-grade development workflow documentation

## [0.1.0] - 2025-06-21

### Added
- Initial working release of Windows 10 Emoji Picker recreation
- Clean Windows 10-style user interface with custom styling
- Three emoji categories: Smileys & Emotion, People & Body, Objects
- Real-time search functionality with keyword filtering
- One-click emoji copying to clipboard
- Custom Windows 10 emoji font support (seguiemj.ttf)
- ESC key to close application
- Auto-minimize after copying emoji (Windows 10 behavior)
- Responsive emoji grid layout with proper wrapping
- Tooltip display showing emoji names on hover
- Search box with placeholder text and focus handling
- Tab-based category navigation with visual selection

### Technical
- Built with WPF and .NET 8.0 targeting Windows
- Embedded emoji font as application resource
- Data binding for emoji display with ItemsControl
- Custom button styles matching Windows 10 design
- Proper null safety and event handling
- Lightweight memory footprint (~119MB)

### Fixed
- Null reference exceptions during UI initialization
- Event timing issues with SearchBox TextChanged events
- Missing using statements for System.Linq and Collections
- XAML Unicode character corruption in People tab

### Performance
- Fast application startup and emoji loading
- Efficient search filtering with LINQ
- Optimized for quick daily usage

## Version History
- **v0.1.0** - First working release, basic functionality complete

- **1.0.0** - Initial release with core functionality
