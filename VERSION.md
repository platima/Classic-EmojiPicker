# Version Information

## Current Version: v0.1.5

**Release Date**: 2026-07-13
**Build**: Release
**Target Framework**: .NET 8.0-windows
**Architecture**: x64 (self-contained; a framework-dependent "lite" build is also published)

### Version Details
- **Assembly Version**: 0.1.5.0
- **File Version**: 0.1.5.0
- **Product Version**: 0.1.5
- **Informational Version**: 0.1.5

### Release Notes
- New MSI installer (per-machine, silent/enterprise: `msiexec /i <file> /qn`) alongside the Setup.exe installers, which now offer install-for-all-users as well as per-user
- Search is popularity-aware (Unicode frequency data) with Windows 10-style keyword associations (emojilib): "laugh" finds 😅, "spl" puts 💦 first
- Win+. toggles the picker closed when it is already open; releasing Win no longer opens the Start menu
- Inserts feel immediate (foreground readiness is polled instead of a fixed 250 ms wait); elevated targets fall back to the clipboard
- Esc clears an active search first, closes on the second press; arrow-key navigation fixed after deep scrolling
- The seven category tabs fill the strip evenly with larger icons
- 19 code-review fixes across robustness, shutdown, logging (5 MB rotation), and cleanup

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
