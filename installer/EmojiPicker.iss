; Inno Setup script for Classic Emoji Picker
;
; Two variants share this script:
;   Full (default) - expects a self-contained publish (dotnet publish -r win-x64
;     --self-contained true); end users need nothing extra installed.
;     ISCC.exe /DAppVersion=0.1.5 /DPublishDir=<publish> installer\EmojiPicker.iss
;   Lite - expects a framework-dependent publish (--self-contained false), a much
;     smaller download that requires the .NET Desktop Runtime 8 (x64). Setup checks
;     for the runtime and points at the download page when it is missing.
;     ISCC.exe /DAppVersion=0.1.5 /DPublishDir=<publish-fd> /DFrameworkDependent=1 installer\EmojiPicker.iss
;
; Install modes: per-user is the default (no UAC). A startup dialog offers
; "install for all users" (elevates, installs to Program Files, HKLM Run key).
; Command line for silent installs:
;   per-user:  EmojiPicker-Setup-x.y.z.exe /VERYSILENT /SUPPRESSMSGBOXES /CURRENTUSER /TASKS=startup
;   all-users: EmojiPicker-Setup-x.y.z.exe /VERYSILENT /SUPPRESSMSGBOXES /ALLUSERS /TASKS=startup
;     (run from an elevated shell for unattended use; otherwise UAC prompts)

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
; Just the name in Apps & Features; the version has its own column (otherwise
; Inno's default shows "Classic Emoji Picker version x.y.z", duplicating it)
UninstallDisplayName={#AppName}
OutputDir=.\output
#ifdef FrameworkDependent
OutputBaseFilename=EmojiPicker-Setup-{#AppVersion}-lite
#else
OutputBaseFilename=EmojiPicker-Setup-{#AppVersion}
#endif
Compression=lzma2
SolidCompression=yes
; Per-user by default (no UAC); the dialog/command line can elevate to an
; all-users install (Program Files + HKLM Run key)
PrivilegesRequired=lowest
PrivilegesRequiredOverridesAllowed=dialog commandline
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
; Start with Windows. HKA resolves to HKLM in an all-users install (starts for
; every user) and HKCU per-user (same value the tray "Start with Windows"
; toggle manages). Note: in an all-users install the tray toggle still writes
; HKCU, so both values can exist; the single-instance mutex makes the second
; logon start a no-op.
Root: HKA; Subkey: "Software\Microsoft\Windows\CurrentVersion\Run"; \
    ValueType: string; ValueName: "ClassicEmojiPicker"; ValueData: """{app}\{#AppExe}"""; \
    Tasks: startup; Flags: uninsdeletevalue

[Run]
Filename: "{app}\{#AppExe}"; Description: "Launch Classic Emoji Picker now"; Flags: nowait postinstall skipifsilent

[UninstallRun]
; Stop the resident app before removing files so the exe isn't locked
Filename: "{cmd}"; Parameters: "/C taskkill /IM {#AppExe} /F"; Flags: runhidden; RunOnceId: "StopEmojiPicker"

[Code]
const
  // Inno's uninstall registration for this AppId (per mode: HKLM for
  // all-users, HKCU for per-user)
  UninstallRegKey = 'Software\Microsoft\Windows\CurrentVersion\Uninstall\{B6C3E1A2-7F4D-4C9E-9B21-1E2A3C4D5E6F}_is1';
  RunRegKey = 'Software\Microsoft\Windows\CurrentVersion\Run';

#ifdef FrameworkDependent
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
#endif

function InitializeSetup(): Boolean;
#ifdef FrameworkDependent
var
  ErrorCode: Integer;
#endif
begin
  Result := True;

#ifdef FrameworkDependent
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
    exit;
  end;
#endif

  // The two install modes register independently (HKLM vs HKCU), so both can
  // end up installed at once. Warn interactively; silent installs proceed.
  if not WizardSilent then
  begin
    if IsAdminInstallMode and RegKeyExists(HKCU, UninstallRegKey) then
    begin
      if MsgBox('Classic Emoji Picker is already installed for the current user. '
        + 'Installing for all users as well would leave two copies.' #13#10 #13#10
        + 'Continue anyway? (Consider uninstalling the per-user copy first.)',
        mbConfirmation, MB_YESNO) = IDNO then
      begin
        Result := False;
      end;
    end
    else if (not IsAdminInstallMode) and RegKeyExists(HKLM, UninstallRegKey) then
    begin
      if MsgBox('Classic Emoji Picker is already installed for all users. '
        + 'Installing for the current user as well would leave two copies.' #13#10 #13#10
        + 'Continue anyway? (Consider uninstalling the all-users copy first.)',
        mbConfirmation, MB_YESNO) = IDNO then
      begin
        Result := False;
      end;
    end;
  end;
end;

procedure CurUninstallStepChanged(CurUninstallStep: TUninstallStep);
begin
  // An all-users uninstall removes the HKLM Run value via the uninstall log,
  // but any HKCU value the tray "Start with Windows" toggle created would be
  // left stale (pointing at a deleted exe) - clean up this user's copy too
  if (CurUninstallStep = usPostUninstall) and IsAdminInstallMode then
  begin
    RegDeleteValue(HKCU, RunRegKey, 'ClassicEmojiPicker');
  end;
end;
