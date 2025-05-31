[Setup]
AppName=GM4Manager - Group Manager for Manager
AppVersion=1.0.0
DefaultDirName={localappdata}\GM4Manager
DefaultGroupName=GM4Manager
OutputBaseFilename=GM4ManagerSetup
OutputDir=Output
DisableProgramGroupPage=yes
Compression=lzma
SolidCompression=yes
PrivilegesRequired=lowest
UninstallDisplayIcon={app}\GM4Manager.exe
AppPublisher=MaikL
AppPublisherURL=https://github.com/MaikL/GM4Manager

[Files]
Source: "bin\Release\net8.0-windows\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs;

[Icons]
Name: "{group}\GM4Manager"; Filename: "{app}\GM4Manager.exe"
Name: "{userdesktop}\GM4Manager"; Filename: "{app}\GM4Manager.exe"; Tasks: desktopicon

[Tasks]
Name: "desktopicon"; Description: "Create a &desktop icon"; GroupDescription: "Additional icons:"

[Run]
Filename: "{app}\GM4Manager.exe"; Description: "Launch GM4Manager"; Flags: nowait postinstall skipifsilent
