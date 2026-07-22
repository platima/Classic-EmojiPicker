# Version Information

## Current Version: v0.1.9

**Release Date**: 2026-07-22
**Build**: Release
**Target Framework**: .NET 8.0-windows
**Architecture**: x64 (self-contained; a framework-dependent "lite" build is also published)

### Version Details
- **Assembly Version**: 0.1.9.0
- **File Version**: 0.1.9.0
- **Product Version**: 0.1.9
- **Informational Version**: 0.1.9

### Release Notes (hardening pass from a multi-agent adversarial review)
- Paste-insert now preserves the whole clipboard (images/files too), skips the restore if you copied during the wait, and no longer risks losing content on slow/remote targets (`pasteRestoreDelayMs`)
- Win+. recovers if Windows drops the keyboard hook (session change / timeout), and holding it no longer thrashes the picker
- MSI remembers `AUTOSTART=0` across upgrade/repair (secure property)
- Search-box IME composition, atomic recent.json writes, degenerate-caret and shutdown-race guards

### Previous Release (v0.1.8)
- Text editing shortcuts work in the search box: Ctrl+Shift+Left/Right, Ctrl+Left/Right, Shift+Left/Right; plain arrows still browse the grid
- Add/Remove Programs no longer repeats the version in the entry name

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
