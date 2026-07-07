; Inno Setup script for Classic Emoji Picker
; Build with: ISCC.exe /DAppVersion=0.1.1 /DPublishDir=<path-to-publish> installer\EmojiPicker.iss
; Expects a self-contained publish (dotnet publish -r win-x64 --self-contained) so
; end users do not need the .NET runtime installed.

#ifndef AppVersion
  #define AppVersion "0.1.1"
#endif

#ifndef PublishDir
  ; Default to the conventional publish output relative to this script
  #define PublishDir "..\EmojiPicker\bin\Release\net8.0-windows\win-x64\publish"
#endif

#define AppName "Classic Emoji Picker"
#define AppExe "EmojiPicker.exe"
#define AppPublisher "Platima"
#define AppUrl "https://github.com/platima/Classic-EmojiPicker"

[Setup]
AppId={{B6C3E1A2-7F4D-4C9E-9B21-1E2A3C4D5E6F}
AppName={#AppName}
AppVersion={#AppVersion}
AppPublisher={#AppPublisher}
AppSupportURL={#AppUrl}
DefaultDirName={autopf}\Classic Emoji Picker
DefaultGroupName={#AppName}
DisableProgramGroupPage=yes
UninstallDisplayIcon={app}\{#AppExe}
OutputDir=.\output
OutputBaseFilename=EmojiPicker-Setup-{#AppVersion}
Compression=lzma2
SolidCompression=yes
; Per-user install: no UAC prompt, and lets us write the HKCU Run key
PrivilegesRequired=lowest
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
WizardStyle=modern

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "startup"; Description: "Start Classic Emoji Picker automatically when I sign in (recommended, needed for the Win+. shortcut)"; GroupDescription: "Startup:"

[Files]
Source: "{#PublishDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\{#AppName}"; Filename: "{app}\{#AppExe}"
Name: "{group}\Uninstall {#AppName}"; Filename: "{uninstallexe}"

[Registry]
; Start with Windows (per-user). Mirrors the tray "Start with Windows" toggle.
Root: HKCU; Subkey: "Software\Microsoft\Windows\CurrentVersion\Run"; \
    ValueType: string; ValueName: "ClassicEmojiPicker"; ValueData: """{app}\{#AppExe}"""; \
    Tasks: startup; Flags: uninsdeletevalue

[Run]
Filename: "{app}\{#AppExe}"; Description: "Launch Classic Emoji Picker now"; Flags: nowait postinstall skipifsilent

[UninstallRun]
; Stop the resident app before removing files so the exe isn't locked
Filename: "{cmd}"; Parameters: "/C taskkill /IM {#AppExe} /F"; Flags: runhidden; RunOnceId: "StopEmojiPicker"
