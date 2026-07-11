; Inno Setup script for Classic Emoji Picker
;
; Two variants share this script:
;   Full (default) - expects a self-contained publish (dotnet publish -r win-x64
;     --self-contained true); end users need nothing extra installed.
;     ISCC.exe /DAppVersion=0.1.4 /DPublishDir=<publish> installer\EmojiPicker.iss
;   Lite - expects a framework-dependent publish (--self-contained false), a much
;     smaller download that requires the .NET Desktop Runtime 8 (x64). Setup checks
;     for the runtime and points at the download page when it is missing.
;     ISCC.exe /DAppVersion=0.1.4 /DPublishDir=<publish-fd> /DFrameworkDependent=1 installer\EmojiPicker.iss

#ifndef AppVersion
  #define AppVersion "0.1.4"
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
#ifdef FrameworkDependent
OutputBaseFilename=EmojiPicker-Setup-{#AppVersion}-lite
#else
OutputBaseFilename=EmojiPicker-Setup-{#AppVersion}
#endif
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

#ifdef FrameworkDependent
[Code]
const
  RuntimeDownloadUrl = 'https://dotnet.microsoft.com/download/dotnet/8.0';

// The lite build carries no runtime of its own: it needs the .NET Desktop
// Runtime 8 (x64). The runtime installer registers each version as a value
// (e.g. "8.0.27") under this key in the 32-bit registry view; fall back to
// probing the shared-framework directory on disk.
function IsDesktopRuntime8Installed(): Boolean;
var
  Names: TArrayOfString;
  I: Integer;
  FindRec: TFindRec;
begin
  Result := False;

  if RegGetValueNames(HKLM32,
    'SOFTWARE\dotnet\Setup\InstalledVersions\x64\sharedfx\Microsoft.WindowsDesktop.App',
    Names) then
  begin
    for I := 0 to GetArrayLength(Names) - 1 do
      if Copy(Names[I], 1, 2) = '8.' then
      begin
        Result := True;
        exit;
      end;
  end;

  // Registry missing/incomplete: look for an 8.x folder on disk instead
  if FindFirst(ExpandConstant('{commonpf64}\dotnet\shared\Microsoft.WindowsDesktop.App\8.*'), FindRec) then
  begin
    try
      Result := True;
    finally
      FindClose(FindRec);
    end;
  end;
end;

function InitializeSetup(): Boolean;
var
  ErrorCode: Integer;
begin
  Result := True;
  if not IsDesktopRuntime8Installed() then
  begin
    if MsgBox('This lite installer requires the .NET Desktop Runtime 8 (x64), '
      + 'which was not found on this computer.' #13#10 #13#10
      + 'Open the Microsoft download page now? Install ".NET Desktop Runtime 8" '
      + 'from there, then run this setup again.' #13#10 #13#10
      + '(Alternatively, download the full Classic Emoji Picker installer '
      + 'instead - it includes everything and needs no separate runtime.)',
      mbConfirmation, MB_YESNO) = IDYES then
    begin
      ShellExec('open', RuntimeDownloadUrl, '', '', SW_SHOWNORMAL, ewNoWait, ErrorCode);
    end;
    Result := False;
  end;
end;
#endif
