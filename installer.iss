[Setup]
AppName=CodeZipTool
AppVersion=1.0.0
; インストール先のデフォルト設定（{autopf} はインストールモードに応じて自動でパスを切り替えます）
DefaultDirName={autopf}\CodeZipTool
; デフォルトの権限を lowest（ユーザー権限）に設定します
PrivilegesRequired=lowest
; インストール開始時に「すべてのユーザー」か「自分のみ」かを選択するダイアログを表示します
PrivilegesRequiredOverridesAllowed=dialogue
DefaultGroupName=CodeZipTool
UninstallDisplayIcon={app}\CodeZipTool.exe
Compression=lzma
SolidCompression=yes
OutputDir=./

[Files]
; publish フォルダの中身をすべてインストール先にコピーします
Source: "publish\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs

[Icons]
; スタートメニューとデスクトップにショートカットを作成します
; ★修正箇所：設定を1行にまとめました
Name: "{group}\CodeZipTool"; Filename: "{app}\CodeZipTool.exe"
Name: "{commondesktop}\CodeZipTool"; Filename: "{app}\CodeZipTool.exe"