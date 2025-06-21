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
- **Performance First:** Keep memory usage minimal (~119MB target)
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
- [ ] Application starts without errors
- [ ] All three category tabs work
- [ ] Search functionality works
- [ ] Emoji copying works (clipboard)
- [ ] Window minimises after emoji selection
- [ ] ESC key closes application
- [ ] Memory usage stays under 130MB

#### **Performance Testing**
```powershell
# Monitor memory usage
Get-Process EmojiPicker | Select-Object Name, WorkingSet, PagedMemorySize

# Startup time test (should be under 2 seconds)
Measure-Command { Start-Process EmojiPicker.exe }
```

### **Release Process**

#### **Version Bumping**
1. Update version in `EmojiPicker.csproj`
2. Update `CHANGELOG.md` with new version
3. Update `VERSION.md` with release notes
4. Update window title in `MainWindow.xaml`

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
- **Font Issues:** Ensure `seguiemj.ttf` is in `Fonts/` directory
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
