# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased - v0.1.1] - 2025-06-21

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
