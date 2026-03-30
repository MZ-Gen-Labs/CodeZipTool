[Setup]
AppName=CodeZipTool
AppVersion=1.0.0
DefaultDirName={autopf}\CodeZipTool
DefaultGroupName=CodeZipTool
UninstallDisplayIcon={app}\CodeZipTool.exe
Compression=lzma
SolidCompression=yes
OutputDir=./

[Files]
Source: "publish\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs

[Icons]
Name: "{group}\CodeZipTool"; Filename: "{app}\CodeZipTool.exe"
Name: "{commondesktop}\CodeZipTool"; Filename: "{app}\CodeZipTool.exe"