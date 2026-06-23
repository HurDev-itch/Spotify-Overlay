[Setup]
AppName=Spotify Overlay
AppVersion=1.5.0
AppPublisher=SpotifyOverlay Team
DefaultDirName={pf}\Spotify Overlay
DefaultGroupName=Spotify Overlay
OutputBaseFilename=SpotifyOverlay_Installer
Compression=lzma
SolidCompression=yes
ArchitecturesInstallIn64BitMode=x64
PrivilegesRequired=admin

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Files]
; C# App and Service
Source: "src\SpotifyOverlay.App\bin\x64\Release\net8.0-windows10.0.19041.0\win-x64\publish\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs
; C++ Hook DLL
Source: "src\SpotifyOverlay.Hook\build\Release\SpotifyOverlayHook.dll"; DestDir: "{app}\Hook"; Flags: ignoreversion

[Icons]
Name: "{group}\Spotify Overlay"; Filename: "{app}\SpotifyOverlay.App.exe"
Name: "{group}\{cm:UninstallProgram,Spotify Overlay}"; Filename: "{uninstallexe}"
Name: "{commondesktop}\Spotify Overlay"; Filename: "{app}\SpotifyOverlay.App.exe"; Tasks: desktopicon

[Run]
Filename: "{app}\SpotifyOverlay.App.exe"; Description: "{cm:LaunchProgram,Spotify Overlay}"; Flags: nowait postinstall skipifsilent
