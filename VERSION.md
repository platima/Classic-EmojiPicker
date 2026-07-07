# Version Information

## Current Version: v0.1.1

**Release Date**: 2025-06-21 (unreleased; see CHANGELOG for later updates)
**Build**: Release
**Target Framework**: .NET 8.0-windows
**Architecture**: x64 (self-contained)

### Version Details
- **Assembly Version**: 0.1.1.0
- **File Version**: 0.1.1.0
- **Product Version**: 0.1.1
- **Informational Version**: 0.1.1

### Release Notes
- Resident system-tray app that takes over **Win+.** in place of the built-in Windows emoji panel
- Full Unicode emoji set in the original seven Windows 10 categories
- Direct insertion into the previously focused app (clipboard fallback)
- Dark mode that follows the Windows theme
- Per-user Inno Setup installer with optional start-with-Windows

### Compatibility
- **OS**: Windows 11, Windows 10 (version 1809+)
- **Runtime**: Self-contained - no separate .NET install required
- **Emoji Font**: System Segoe UI Emoji (ships with supported Windows versions)

### Known Issues
- Search matches emoji names only (no keyword/alias search yet)
- No skin-tone modifiers yet
- The Win+. hook requires the app to be running (tray)

### Next Release
- **Target**: v0.2.0
- **Focus**: Keyword/alias search, skin-tone modifiers, configurable hotkey
