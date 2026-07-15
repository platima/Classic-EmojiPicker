# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased - v0.1.7]

### Fixed
- **Joined emoji split apart in some apps** - emoji like 🤷‍♂️ (a ZWJ sequence), flags, and skin-tone variants are several code points glued together, and typing them as separate synthetic keystrokes made apps like WhatsApp/Discord/Slack show the pieces (and a stray joiner) separately. These are now **pasted** so the target composes them correctly, while simple emoji still type as before. The transient paste (and the restore of your previous clipboard) is tagged to stay out of Clipboard History (Win+V), Cloud Clipboard, and clipboard managers, so it doesn't pollute the history stack

### Added
- **`settings.json`** (`%APPDATA%\ClassicEmojiPicker\settings.json`, created on first run) with `emojiInsertMode`: `hybrid` (default - type simple, paste joined), `paste` (always clipboard), or `keystroke` (always type, never touch the clipboard). See the README

## [v0.1.6] - 2026-07-13

### Changed
- **Emoji cells match the Windows 10 look** - glyphs are larger (32px in the 40px cell, ~75% fill like the original) and selection is a solid filled highlight square (theme-tinted) instead of a 2px accent border that ate into the cell

## [v0.1.5] - 2026-07-13

### Fixed (post-verification)
- **Search coverage now matches Windows 10's associations** - community keyword data from [emojilib](https://github.com/muan/emojilib) (MIT) is merged into the bundled keywords, so colloquial searches like "laugh" now find 😅 (previously only the emoji whose formal tags mentioned it). Windows' own keyword data is proprietary and can't be redistributed; emojilib provides the equivalent coverage
- **Perceptible pause between Enter and the emoji appearing** - insertion now polls for the target window to actually take foreground (typically ~30-60 ms) instead of always waiting a fixed 250 ms
- **Search ranking is now popularity-aware** - results are ordered by match quality (word-start before mid-word) and then by Unicode's published emoji usage-frequency tiers (bundled as `Resources/popularity.json`, supplemented with popular post-2018 emoji missing from that dataset). Keyword-only matches carry a one-tier handicap so a hidden tag must be genuinely more popular to outrank a visible name match - "spl" now puts 💦 (splash) above 🖐️ ("hand with fingers *spl*ayed"), while "whi" still ranks white-named emoji above heart decoration's "white" tag
- **Down/Up arrow navigation broke after scrolling** (regression from the review fixes) - the grid's column count is now derived from the wrap panel's width instead of realized-container geometry, which recycling invalidates
- **Documented**: Win+. inside elevated (admin) apps opens the built-in Windows panel - Windows does not deliver elevated-bound keystrokes to a non-elevated hook, so this cannot be intercepted without a signed UIAccess binary

### Fixed (full code review)
- **Win+. while the picker was open** captured the picker as its own insertion target; it now toggles the picker closed like the Windows 10 panel
- **Start menu opening when releasing Win after Win+.** - the swallowed `.` made the shell treat Win as a bare tap; a no-op key is now injected in between
- **Tray-menu / double-click opens** targeted whatever window the *previous hotkey* captured; they now target nothing (picks land on the clipboard, predictably)
- **Picker popping open at sign-in** when both the all-users (HKLM) and per-user (HKCU) autostart entries exist - run-again signals are ignored for the first 3 s after startup
- **250 ms UI freeze on every insert** - the focus-settle delay is now awaited instead of blocking the UI thread
- **Inserting into elevated apps** silently did nothing (Windows drops the injected input); it now detects an elevated target and falls back to the clipboard
- **Up/Down arrow jumps** could be off by one column when scrolled deep into a category (the fallback column estimate ignored the scrollbar)
- **Search with a trailing space** (e.g. "splash ") found nothing; input is now trimmed
- **Esc** now clears an active search first and closes on the second press (previously always closed)
- **"Start with Windows" tray toggle** now recognises an all-users install (HKLM) and shows as on/read-only instead of misleadingly off
- **debug.log grew without bound** while logging was enabled; it now rotates at 5 MB (one `.old.log` kept)
- An unhandled UI exception now logs and keeps the app alive (previously it silently killed the resident process, taking Win+. with it)
- Hook-installation failure (Win+. dead) is now always logged; a shutdown race that could log spurious fatals is fixed; the FE0F normalizer no longer relies on an invisible character in source; recents only write to disk when changed; P/Invoke declarations consolidated into `NativeMethods`; crisper 1px borders via layout rounding; removed the redundant `code-quality-check.ps1`

### Added
- **MSI installer** (`ClassicEmojiPicker-<version>-win-x64.msi`) - self-contained, per-machine, built with WiX for silent/enterprise deployment: `msiexec /i <file> /qn` (add `AUTOSTART=0` to skip the start-with-Windows Run key). Upgrades and uninstalls terminate the running tray app automatically
- **Install for all users** - the Setup.exe installers now ask whether to install for all users (elevates, Program Files, HKLM Run key) or just the current user (unchanged default); `/ALLUSERS` and `/CURRENTUSER` select the mode in silent installs. Setup warns when the other mode's copy is already installed, and an all-users uninstall also cleans up the uninstalling user's leftover per-user Run value

### Changed
- **Category tabs fill the strip** - the seven bottom tabs now share the full width evenly (previously packed left with dead space) with ~15% larger icons, so they read as category buttons rather than more emoji

## [v0.1.4] - 2026-07-11

### Fixed
- **~150 MB idle memory** - the resident process now compacts the GC heap and empties its working set after startup and whenever the picker hides, and the runtime is tuned for a small heap (`System.GC.ConserveMemory`, non-concurrent GC). Idle-in-tray footprint dropped from ~150 MB to ~20 MB, with no measurable cost to open speed (~35 ms)
- **App metadata said "Windows 10 Emoji Picker"** - the assembly title/description now use the product's own name, "Classic Emoji Picker" (Windows 10 is a Microsoft trademark)

### Added
- **Lite installer and zip** (`EmojiPicker-Setup-<version>-lite.exe`, `...-lite.zip`) - a framework-dependent build a fraction of the size of the self-contained one. It requires the [.NET Desktop Runtime 8 (x64)](https://dotnet.microsoft.com/download/dotnet/8.0); the lite installer detects a missing runtime and offers to open the download page

## [v0.1.3] - 2026-07-10

### Fixed
- **Picker opened at the mouse pointer, not where you were typing** - the target app's text caret position is now captured at hotkey time (classic edit controls and Chromium/Electron apps expose it) and the picker anchors to it like the Windows 10 panel, falling back to the mouse pointer when no caret is available. When there's no room below the anchor (e.g. typing near the bottom of the screen), the picker opens *above* it instead of clamping to the screen edge
- **Debug logging toggle** - now a normal checkable **Debug logging** item in the tray menu; the old Shift+right-click gesture didn't work reliably on Windows 11
- **Laggy typing and slow opens** - the emoji grid now uses a UI-virtualized `VirtualizingWrapPanel`, so only the visible cells (~65) render instead of the whole category (up to ~310). Steady-state open dropped to ~40 ms; a startup pre-warm makes the first hotkey open fast too
- **Inserting into controls that lose focus** - the focused child control (e.g. File Explorer's Search box or address bar) is now captured at hotkey time and focus is restored to it before typing, so the emoji lands in the right place (best-effort; some shell/UIA surfaces may still resist)
- **Couldn't switch category tabs** - the tabs required a mouse click, which didn't land reliably because the picker holds keyboard focus without always being the active window. **Tab / Shift+Tab** now cycle categories from the keyboard, and tabs also respond on mouse-up (like emoji selection) so clicking is more reliable
- **Search missed many emoji** - search now also matches keyword tags (e.g. "splash" finds 💦, "rofl" finds 🤣), not just the Unicode name. Keyword data is bundled from [emojibase](https://github.com/milesj/emojibase). Name matches are ranked ahead of keyword-only matches, so an incidental tag (e.g. heart decoration's "white" tag for "whi") never outranks the emoji whose name contains the query
- **People tab icon** looked small and black (it was the monochrome 👤 silhouette); it's now a colourful person 🧑

### Changed
- Search is debounced (~120 ms) so filtering runs once typing pauses instead of on every keystroke
- Arrow-key Up/Down now derive the grid's column count at runtime (robust across width/DPI/scrollbar) instead of assuming a fixed 8

## [v0.1.2] - 2026-07-08

### Fixed
- **Picker never appeared on high-DPI / multi-monitor setups** - `PositionNearCursor` fed physical-pixel coordinates into WPF's device-independent `Left`/`Top`, placing the window off-screen (e.g. dual 4K @ 150%). Coordinates are now converted with the window's DPI scale, with an `EnsureOnScreen` fallback that recentres on the primary monitor if the window would still land off every display

### Added
- **Debug logging** toggled via **Shift+right-click** on the tray icon (off by default), written to `%APPDATA%\ClassicEmojiPicker\debug.log`; fatal exceptions are always recorded. A balloon tip reports the on/off state and log path
- **Run again to open** - launching the app (or its shortcut) while it is already running now opens the picker, as a fallback if the Win+. hotkey is unavailable
- Guard against the picker hiding from a transient deactivation while it is being brought to the foreground

### Changed
- The picker now opens on the **Recent** tab when you have history (like the Windows 10 picker), otherwise on Smiley faces & animals
- Tray menu item capitalised to "Open Emoji Picker" (the app name)

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
