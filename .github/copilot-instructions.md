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
- **Framework**: WPF with .NET Framework 4.8 (native to Windows 11)
- **Language**: C# 
- **UI**: XAML with custom styling to match Windows 10 design
- **Font**: Custom bundled Windows 10 emoji font (seguiemj.ttf)
- **Target Platform**: Windows 11 (standalone application, no system integration initially)

## Code Architecture
- **MainWindow.xaml.cs**: Contains `Emoji` class and all business logic
- **Emoji Class**: Simple data structure (Character, Name, Category, Keywords[])
- **Font Loading**: Uses pack://application URI to load embedded font
- **Styling**: Custom WPF styles for tabs and emoji buttons
- **Event Handling**: Click handlers for tabs, emoji selection, search functionality

## Development Environment
- User has: Visual Studio Enterprise, VS Code, Windows 11, GitHub account
- Project is version controlled in Git/GitHub
- Building with standard Visual Studio WPF project workflow

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

## File Dependencies
- **seguiemj.ttf**: Critical font file that must be placed in Fonts/ directory
- **Visual Studio**: Primary development environment
- **.NET Framework 4.8**: Must be available (comes with Windows 11)
