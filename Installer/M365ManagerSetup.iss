; M365 Manager Installer Script for Inno Setup
; Download Inno Setup from: https://jrsoftware.org/isdl.php

#define MyAppName "M365 Manager"
#define MyAppVersion "1.0.0"
#define MyAppPublisher "Gene Louw Primary School"
#define MyAppExeName "M365Manager.exe"
#define SourcePath "..\bin\x64\Release\net8.0-windows10.0.19041.0\win-x64"

[Setup]
; Unique identifier for this application (generate new GUID for your app)
AppId={{A1B2C3D4-E5F6-7890-ABCD-EF1234567890}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppVerName={#MyAppName} {#MyAppVersion}
AppPublisher={#MyAppPublisher}
DefaultDirName={autopf}\{#MyAppName}
DefaultGroupName={#MyAppName}
AllowNoIcons=yes
; Output settings
OutputDir=..\publish\installer
OutputBaseFilename=M365Manager_Setup_{#MyAppVersion}
; Compression
Compression=lzma2/ultra64
SolidCompression=yes
; Installer appearance
WizardStyle=modern
; SetupIconFile=..\Assets\app-icon.ico  ; Uncomment when you have an icon
UninstallDisplayIcon={app}\{#MyAppExeName}
; Require admin for Program Files installation
PrivilegesRequired=admin
PrivilegesRequiredOverridesAllowed=dialog
; Minimum Windows version (Windows 10 1809+)
MinVersion=10.0.17763
; Architecture
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Files]
; Copy all files from the publish folder - use explicit source path
Source: "{#SourcePath}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{group}\{cm:UninstallProgram,{#MyAppName}}"; Filename: "{uninstallexe}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#StringChange(MyAppName, '&', '&&')}}"; Flags: nowait postinstall skipifsilent

[UninstallDelete]
Type: filesandordirs; Name: "{app}"

[Code]
// Optional: Check for Windows version
function InitializeSetup(): Boolean;
begin
  Result := True;
  // Add any pre-installation checks here
end;
