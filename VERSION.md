# Version Information

## Current Version: v0.1.4

**Release Date**: 2026-07-11
**Build**: Release
**Target Framework**: .NET 8.0-windows
**Architecture**: x64 (self-contained; a framework-dependent "lite" build is also published)

### Version Details
- **Assembly Version**: 0.1.4.0
- **File Version**: 0.1.4.0
- **Product Version**: 0.1.4
- **Informational Version**: 0.1.4

### Release Notes
- Idle-in-tray memory cut from ~150 MB to ~20 MB (GC heap compaction + working-set trim after startup and whenever the picker hides)
- App metadata renamed to "Classic Emoji Picker" (no Microsoft trademark in the title)
- New lite installer/zip: framework-dependent (~2 MB app payload), requires the .NET Desktop Runtime 8 (x64); setup detects a missing runtime and offers the download page
- Opens where you're typing: anchors to the text caret (mouse-pointer fallback), opening above it when there's no room below
- Search by name or keyword (e.g. "splash" finds 💦), with name matches ranked ahead of keyword-only matches
- Debug logging toggled from the tray menu's "Debug logging" item

### Compatibility
- **OS**: Windows 11, Windows 10 (version 1809+)
- **Runtime**: Self-contained builds need nothing extra; lite builds need the .NET Desktop Runtime 8 (x64)
- **Emoji Font**: System Segoe UI Emoji (ships with supported Windows versions)

### Known Issues
- No skin-tone modifiers yet
- The Win+. hook requires the app to be running (tray)
- Caret anchoring relies on the target app exposing a system caret; apps that don't fall back to the mouse pointer
- Elevated (run-as-administrator) apps: Windows blocks non-elevated hooks from elevated input, so Win+. there opens the built-in panel, and picks targeting an elevated window land on the clipboard

### Next Release
- **Target**: v0.1.5
- **Focus**: MSI installer for silent/enterprise deploys, install-for-all-users option, category tab sizing, code-review fixes
