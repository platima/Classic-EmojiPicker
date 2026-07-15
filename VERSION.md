# Version Information

## Current Version: v0.1.7

**Release Date**: 2026-07-13
**Build**: Release
**Target Framework**: .NET 8.0-windows
**Architecture**: x64 (self-contained; a framework-dependent "lite" build is also published)

### Version Details
- **Assembly Version**: 0.1.7.0
- **File Version**: 0.1.7.0
- **Product Version**: 0.1.7
- **Informational Version**: 0.1.7

### Release Notes
- Joined emoji (🤷‍♂️ and other ZWJ sequences, flags, skin-tone variants) now insert correctly in apps like WhatsApp/Discord/Slack: they are pasted so the app composes them, while simple emoji still type
- New `settings.json` with `emojiInsertMode` (hybrid / paste / keystroke) to control how emoji are inserted
- The transient paste stays out of Clipboard History (Win+V), Cloud Clipboard, and clipboard managers

### Previous Release (v0.1.6)
- Emoji cells match the Windows 10 look: larger glyphs (~75% of the cell) and a solid filled selection highlight instead of a border stroke

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
