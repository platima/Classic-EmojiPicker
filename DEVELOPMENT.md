# Development Guide

## Code Quality & Standards

### **Local Development Setup**

1. **Install Prerequisites:**
   - Visual Studio 2022 or VS Code with C# extension
   - .NET 8 SDK
   - Git for Windows

2. **Clone and Setup:**
   ```bash
   git clone https://github.com/platima/Classic-EmojiPicker.git
   cd Classic-EmojiPicker
   dotnet restore
   ```

3. **Build and Run:**
   ```bash
   dotnet build --configuration Release
   dotnet run --project EmojiPicker
   ```
   The app launches into the system tray with no visible window - press **Win+.** or double-click the tray icon to open the picker. Exit via the tray menu.

4. **Build the installer** (requires [Inno Setup 6](https://jrsoftware.org/isdl.php)):
   ```powershell
   dotnet publish EmojiPicker/EmojiPicker.csproj -c Release -r win-x64 --self-contained true -o ./publish
   ISCC.exe /DAppVersion=0.1.1 "/DPublishDir=$(Resolve-Path ./publish)" installer/EmojiPicker.iss
   ```
   The release GitHub Actions workflow does this automatically for tagged builds.

### **Code Quality Workflow**

#### **Before Committing**
Always run the quality check script:
```powershell
.\code-quality-simple.ps1
```

This script checks:
- ✅ Project builds successfully
- ✅ No code analysis warnings/errors
- ✅ Code formatting is consistent
- ✅ Summary of any issues found

#### **Fix Common Issues**
```powershell
# Auto-fix formatting
dotnet format

# Detailed build analysis
dotnet build --configuration Release --verbosity normal

# Manual format verification
dotnet format --verify-no-changes
```

### **Code Style Guidelines**

#### **C# Conventions (Enforced by EditorConfig)**
- **Indentation:** 4 spaces (no tabs)
- **Line Endings:** CRLF (Windows standard)
- **Braces:** New line for all braces (Allman style)
- **Naming:**
  - Classes, Methods, Properties: `PascalCase`
  - Private fields: `camelCase`
  - Constants: `PascalCase`
  - Interfaces: `IPascalCase`

#### **Project-Specific Guidelines**
- **Performance First:** Keep the resident (idle-in-tray) footprint modest; the picker window is reused, not recreated per open
- **No Bloat:** Resist feature creep, maintain Windows 10 simplicity
- **Australian English:** Comments and documentation
- **Null Safety:** Use null checks, especially for UI elements
- **LINQ Usage:** Prefer LINQ for collections, but watch performance

### **Architecture Guidelines**

#### **MainWindow.xaml.cs Structure**
- Keep `Emoji` class simple (data structure only)
- All business logic in MainWindow class
- Event handlers organised by functionality
- Null checks for UI elements during initialization

#### **Performance Considerations**
- Minimize memory allocations in hot paths
- Use `List<T>` over `IEnumerable<T>` when materialisation needed
- Cache expensive operations (font loading, style resources)
- Avoid string concatenation in loops

#### **UI Guidelines**
- Match Windows 10 design exactly
- Use embedded font resources
- Custom styles defined in XAML resources
- Responsive layout with proper wrapping

### **Testing Strategy**

#### **Manual Testing Checklist**
- [ ] App starts to the tray with no visible window
- [ ] **Win+.** opens the picker near the cursor and the built-in Windows panel does NOT appear
- [ ] Search box is focused on open; typing filters immediately
- [ ] All seven category tabs work and show populated grids
- [ ] Arrow keys move the selection; Enter inserts the highlighted emoji into the app you came from
- [ ] Clipboard fallback works when there is no target window
- [ ] Picker hides after selection, on focus loss, and on ESC (process stays in the tray)
- [ ] Recent emojis persist across restarts
- [ ] Dark/light: switch the Windows theme and confirm the picker recolours (live)
- [ ] Tray menu: Open, Start with Windows (toggles the HKCU Run key), Exit
- [ ] After Exit, Win+. reopens the built-in Windows panel

> Note: the Win+. hook is global while the app runs. When testing with scripted input, always terminate the process afterward so the hook is removed.
- [ ] Idle-in-tray memory stays reasonable

#### **Performance Testing**
```powershell
# Monitor the resident process's memory while idle in the tray
Get-Process EmojiPicker | Select-Object Name, WorkingSet, PagedMemorySize
```
The app starts into the tray; measure how quickly the picker appears on Win+., not process start time.

### **Release Process**

#### **Version Bumping**
1. Update version in `EmojiPicker.csproj`
2. Update `CHANGELOG.md` with new version
3. Update `VERSION.md` with release notes
4. Tag `vX.Y.Z` and push - the release workflow publishes and builds the installer

#### **Quality Gates**
- [ ] All code quality checks pass
- [ ] Manual testing completed
- [ ] Documentation updated
- [ ] GitHub Actions build passes

#### **Tagging and Release**
```bash
git tag v0.x.x
git push origin v0.x.x
# GitHub Actions automatically creates release
```

### **Troubleshooting**

#### **Common Issues**
- **Build Fails:** Check .NET 8 SDK installation
- **Emoji Render as Monochrome/Boxes:** Ensure the system Segoe UI Emoji font is present (bundled with Windows 10 1809+)
- **Formatting Errors:** Run `dotnet format` to auto-fix
- **Memory Leaks:** Check event handler disposal

#### **Debug Configuration**
```xml
<!-- Add to EmojiPicker.csproj for debugging -->
<PropertyGroup Condition="'$(Configuration)'=='Debug'">
  <DefineConstants>DEBUG;TRACE</DefineConstants>
  <DebugType>portable</DebugType>
  <DebugSymbols>true</DebugSymbols>
</PropertyGroup>
```

### **Resources**

- **C# Coding Conventions:** [Microsoft Docs](https://docs.microsoft.com/en-us/dotnet/csharp/programming-guide/inside-a-program/coding-conventions)
- **EditorConfig:** [Official Documentation](https://editorconfig.org/)
- **WPF Best Practices:** [Microsoft Docs](https://docs.microsoft.com/en-us/dotnet/desktop/wpf/)
- **.NET Performance:** [Performance Guidelines](https://docs.microsoft.com/en-us/dotnet/framework/performance/)
