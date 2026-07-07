---
applyTo: '**'
---
# Windows 10 Emoji Picker - Project Context

## Project Overview
This is a standalone WPF application that recreates the Windows 10 emoji picker interface for Windows 11 users. The goal is to provide a clean, lightweight alternative to Windows 11's bloated emoji picker that includes unwanted features like GIFs and reactions.

## Current Development Status
See Project README.md

## Deisgn Goals
1. Must match Windows 10 emoji picker functionality as 1:1 as possible
2. No external dependencies that do not ship with Windows 11 in final executable

## Technical Stack & Decisions
- **Framework**: WPF with .NET 8 (modern, performant, included with Windows 11)
- **Language**: C# 12 with modern language features
- **UI**: XAML with custom styling to match Windows 10 design
- **Emoji Rendering**: Emoji.Wpf NuGet package (colour emoji via the system Segoe UI Emoji font)
- **Target Platform**: Windows 11 (resident system-tray app that takes over Win+.)
- **Packaging**: Self-contained publish, packaged by Inno Setup (`installer/EmojiPicker.iss`)

## Code Architecture
- **App.xaml.cs**: Tray host - single-instance mutex, `NotifyIcon` menu, installs the hook, applies the theme, owns the resident lifecycle (`ShutdownMode.OnExplicitShutdown`)
- **HotkeyListener.cs**: `WH_KEYBOARD_LL` hook; on Win+. it captures the foreground window, raises an event, and swallows the key so the built-in panel is suppressed
- **MainWindow.xaml.cs**: `Emoji`/`EmojiCategory` classes and picker logic; `ShowPicker()` positions near the cursor and force-foregrounds (AttachThreadInput); selection hides (not closes) the reused window
- **Emoji Data**: Full Unicode set from Emoji.Wpf's `EmojiData.AllGroups`, mapped to the seven Win10 categories in `GroupToCategory`
- **Insertion**: `TextInjector.cs` types the emoji into the previously focused window (SendInput/KEYEVENTF_UNICODE), clipboard fallback
- **Theming**: `ThemeManager.cs` merges `Theme/Light|DarkTheme.xaml` per the Windows setting and swaps live on `SystemEvents.UserPreferenceChanged`; XAML uses `DynamicResource` brushes
- **Recent Emojis**: Persisted to `%APPDATA%\ClassicEmojiPicker\recent.json`
- **Keyboard**: Window-level PreviewKeyDown - arrows move selection, Enter commits, ESC dismisses; focus lives in the search box
- **WinForms interop note**: `UseWindowsForms` is on for the tray icon, but the WinForms implicit global using is removed in the csproj to avoid clashing with WPF types; import `System.Windows.Forms` explicitly where needed

## Development Environment
- User has: Visual Studio Enterprise, VS Code, Windows 11, GitHub account
- Project targets: .NET 8 (modern SDK-style project)
- Building with: Standard Visual Studio 2022 WPF workflow

## Key Design Principles
- **Simplicity**: Zero bloat, just emoji picking functionality
- **Windows 10 Fidelity**: Match original design exactly
- **Performance**: Lightweight, fast startup and search
- **Standalone**: No system modifications, easy to install/uninstall

## Development Context
- This project was created to replace Windows 11's emoji picker which became bloated
- User specifically wanted Windows 10 design with no additional features
- Font file is user-provided and confirmed compatible
- Focus is on clean, functional implementation over feature richness

## When Providing Assistance
- Use Australian English in all output
- Prioritise performance and simplicity over features
- Maintain focus on core emoji picking functionality
- Respect the "no bloat" philosophy
- Think about memory usage and startup time impact
- Code format must abide by the rules in .clang-format if it exists
- Do not add "// FIXED" comments to code
- Do not stray from the users request or make changes that were not asked for
- The GitHub repository URL is https://github.com/platima/Classic-EmojiPicker and currently private

## File Dependencies
- **Segoe UI Emoji**: System font used for rendering (ships with Windows 10 1809+ / Windows 11)
- **Visual Studio 2022**: Primary development environment (VS Code and VS 2019 may work)
- **.NET 8 (and SDK)**: Must be installed (may need separate download)
