# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Planned
- Global hotkey support (Win+. replacement)
- Recently used emojis section
- More emoji categories (Animals, Food, Travel, etc.)
- Skin tone modifiers for people emojis
- Settings/preferences window
- Auto-start with Windows option

## [1.0.0] - 2025-06-21

### Added
- Initial release of Windows 10 Emoji Picker recreation
- Clean Windows 10-style user interface
- Three emoji categories: Smileys & Emotion, People & Body, Objects
- Real-time search functionality with keyword filtering
- One-click emoji copying to clipboard
- Custom Windows 10 emoji font support (seguiemj.ttf)
- ESC key to close application
- Auto-minimize after copying emoji
- Responsive emoji grid layout
- Tooltip display showing emoji names on hover
- Search box with placeholder text and focus handling

### Technical
- Built with WPF and .NET Framework 4.8
- Embedded emoji font as application resource
- MVVM-style data binding for emoji display
- Custom button styles matching Windows 10 design
- Proper keyboard navigation support

### Font Support
- Bundled Windows 10 emoji font (seguiemj.ttf)
- Automatic fallback to system Segoe UI Emoji font
- Support for custom emoji font replacement

### Performance
- Lightweight application with minimal memory footprint
- Fast emoji loading and search filtering
- Optimized for quick startup and usage

## Version History

- **1.0.0** - Initial release with core functionality
