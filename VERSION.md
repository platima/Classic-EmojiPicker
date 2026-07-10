# Version Information

## Current Version: v0.1.3

**Release Date**: 2026-07-10
**Build**: Release
**Target Framework**: .NET 8.0-windows
**Architecture**: x64 (self-contained)

### Version Details
- **Assembly Version**: 0.1.3.0
- **File Version**: 0.1.3.0
- **Product Version**: 0.1.3
- **Informational Version**: 0.1.3

### Release Notes
- Opens where you're typing: anchors to the text caret (mouse-pointer fallback), opening above it when there's no room below
- Search by name or keyword (e.g. "splash" finds 💦), with name matches ranked ahead of keyword-only matches
- Tab/Shift+Tab switch category; more reliable tab clicks
- Faster typing and opening via a UI-virtualized emoji grid (+ startup pre-warm)
- Inserts into the previously focused control (e.g. Explorer Search/address bar) by restoring focus
- Fixed the picker not appearing on high-DPI / multi-monitor setups (DPI-correct positioning + on-screen fallback)
- Debug logging toggled from the tray menu's "Debug logging" item
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
- No skin-tone modifiers yet
- The Win+. hook requires the app to be running (tray)
- Caret anchoring relies on the target app exposing a system caret; apps that don't fall back to the mouse pointer

### Next Release
- **Target**: v0.2.0
- **Focus**: Skin-tone modifiers, configurable hotkey, settings window
