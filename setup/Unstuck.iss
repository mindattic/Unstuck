; Unstuck installer (Inno Setup 6). Built by setup\build.ps1, which passes
;   /DMajor=<whole-number version>  /DStage=<folder with the files to install>
; Per-user install, no admin rights and no .NET needed (UnstuckTaskbar.exe is self-contained).

#ifndef Major
  #define Major "0"
#endif
#ifndef Stage
  #define Stage "..\artifacts\installer\stage"
#endif

[Setup]
AppId={{3F6A2D1E-7C4B-4E0A-9B52-6E8D1C0A55B7}
AppName=Unstuck
AppVersion={#Major}
AppVerName=Unstuck v{#Major}
AppPublisher=MindAttic
AppPublisherURL=https://mindattic.github.io/Unstuck/
AppSupportURL=https://github.com/mindattic/Unstuck/issues
AppUpdatesURL=https://github.com/mindattic/Unstuck/releases
VersionInfoVersion={#Major}.0.0.0
DefaultDirName={localappdata}\Programs\Unstuck
DisableProgramGroupPage=yes
DisableDirPage=auto
PrivilegesRequired=lowest
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
OutputBaseFilename=Unstuck-Setup-v{#Major}
SetupIconFile=..\assets\logo.ico
UninstallDisplayIcon={app}\UnstuckTaskbar.exe
UninstallDisplayName=Unstuck
WizardStyle=modern
Compression=lzma2/ultra64
SolidCompression=yes
CloseApplications=no

[Tasks]
Name: startup; Description: "Start the Unstuck tray app when Windows starts (Ctrl+Alt+Break runs the recovery)"; Check: not SchedulerStartsTray
Name: desktopicon; Description: "Create an Unstuck desktop shortcut (runs the recovery once)"

[Files]
Source: "{#Stage}\*"; DestDir: "{app}"; Flags: recursesubdirs createallsubdirs ignoreversion

[InstallDelete]
; shortcuts the old script installer (Install-Unstuck.ps1, v1) made; this installer replaces them
Type: filesandordirs; Name: "{userprograms}\Unstuck"
Type: files; Name: "{userstartup}\Unstuck Tray.lnk"
Type: files; Name: "{userdesktop}\Unstuck.lnk"

[Icons]
Name: "{autoprograms}\Unstuck"; Filename: "{app}\unstuck.bat"; WorkingDir: "{app}"; IconFilename: "{app}\logo.ico"; Comment: "Recover a desktop that stopped responding to clicks"
Name: "{autoprograms}\Unstuck (check only)"; Filename: "{app}\unstuck.bat"; Parameters: "check"; WorkingDir: "{app}"; IconFilename: "{app}\logo.ico"; Comment: "Report what Unstuck would do, change nothing"
Name: "{autoprograms}\Unstuck Tray"; Filename: "{app}\UnstuckTaskbar.exe"; WorkingDir: "{app}"; Comment: "Tray icon and the Ctrl+Alt+Break hotkey"
Name: "{autodesktop}\Unstuck"; Filename: "{app}\unstuck.bat"; WorkingDir: "{app}"; IconFilename: "{app}\logo.ico"; Comment: "Recover a desktop that stopped responding to clicks"; Tasks: desktopicon

[Registry]
Root: HKCU; Subkey: "Software\Microsoft\Windows\CurrentVersion\Run"; ValueType: string; ValueName: "Unstuck"; ValueData: """{app}\UnstuckTaskbar.exe"""; Flags: uninsdeletevalue; Tasks: startup

[Run]
Filename: "{app}\UnstuckTaskbar.exe"; Description: "Start the Unstuck tray app now"; Flags: nowait postinstall

[UninstallDelete]
; the Start menu shortcut UnstuckTaskbar.exe keeps for itself (StartupRegistrar)
Type: files; Name: "{userprograms}\MindAttic\UnstuckTaskbar.lnk"
Type: dirifempty; Name: "{userprograms}\MindAttic"

[Messages]
FinishedLabel=Unstuck is in your tray.%n%nWhenever clicks stop working, press Ctrl+Alt+Break (or Ctrl+Alt+U). It works from any app, even when the mouse is dead.

[Code]
function SchedulerStartsTray: Boolean;
var
  Code: Integer;
begin
  { MindAttic.Scheduler's UnstuckTaskbar job already starts the tray app at sign-in; a Run entry would only race it }
  Result := Exec(ExpandConstant('{sys}\schtasks.exe'), '/Query /TN "\MindAttic\Scheduler\UnstuckTaskbar"', '',
    SW_HIDE, ewWaitUntilTerminated, Code) and (Code = 0);
end;

procedure StopTray;
var
  Code: Integer;
begin
  { this session's tray app only: it locks its exe. Another signed-in user's copy is theirs. }
  Exec(ExpandConstant('{sys}\WindowsPowerShell\v1.0\powershell.exe'),
    '-NoProfile -NonInteractive -Command "$s = (Get-Process -Id $PID).SessionId; Get-Process UnstuckTaskbar -ErrorAction SilentlyContinue | Where-Object SessionId -eq $s | Stop-Process -Force"',
    '', SW_HIDE, ewWaitUntilTerminated, Code);
  Sleep(500);
end;

function PrepareToInstall(var NeedsRestart: Boolean): String;
var
  Old: String;
begin
  StopTray;
  { the old script installer's folder: remove it unless it's a build-from-source install (run.ps1 writes run.bat) }
  Old := 'C:\Apps\Unstuck';
  if FileExists(Old + '\Install-Unstuck.ps1') and not FileExists(Old + '\run.bat') then
    DelTree(Old, True, True, True);
  Result := '';
end;

procedure CurUninstallStepChanged(CurUninstallStep: TUninstallStep);
begin
  if CurUninstallStep = usUninstall then
    StopTray;
end;
