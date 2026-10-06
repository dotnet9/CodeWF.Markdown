; CodeWF.Markdown Demo Windows installer.
; Build from the repository root with Inno Setup 6 and pass /DAppVersion=x.y.z.

#ifndef AppVersion
#define AppVersion "0.0.0"
#endif

#ifndef SourceDir
#define SourceDir "..rtifacts\publish\win-x64\CodeWF.Markdown.Sample"
#endif

#ifndef OutputDir
#define OutputDir "..rtifactselease"
#endif

[Setup]
AppId={{7C31A2D4-5F0E-4B8A-9C6D-1E2A3B4C5D6E}
AppName=CodeWF Markdown Demo
AppVersion={#AppVersion}
AppPublisher=Dotnet9
AppPublisherURL=https://github.com/dotnet9/CodeWF.Markdown
AppSupportURL=https://github.com/dotnet9/CodeWF.Markdown/issues
DefaultDirName={autopf}\CodeWFMarkdownDemo
DefaultGroupName=CodeWF Markdown Demo
DisableProgramGroupPage=yes
OutputDir={#OutputDir}
OutputBaseFilename=CodeWFMarkdownDemo-v{#AppVersion}-win-x64-setup
Compression=lzma2/ultra64
SolidCompression=yes
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
; per-user 安装：应用以普通权限运行，可持久化自身设置（配置文件在安装目录内）。
PrivilegesRequired=lowest
CloseApplications=yes
RestartApplications=yes
CloseApplicationsFilter=CodeWF.Markdown.Sample.exe
UninstallDisplayIcon={app}\CodeWF.Markdown.Sample.exe
WizardStyle=modern

[Languages]
Name: "chinesesimplified"; MessagesFile: "Languages\ChineseSimplified.isl"

[Tasks]
Name: "desktopicon"; Description: "Create a desktop shortcut"; GroupDescription: "Additional shortcuts:"; Flags: unchecked

[Files]
Source: "{#SourceDir}\*"; DestDir: "{app}"; Excludes: "*.pdb"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{autoprograms}\CodeWF Markdown Demo"; Filename: "{app}\CodeWF.Markdown.Sample.exe"; WorkingDir: "{app}"
Name: "{autodesktop}\CodeWF Markdown Demo"; Filename: "{app}\CodeWF.Markdown.Sample.exe"; WorkingDir: "{app}"; Tasks: desktopicon

[Run]
Filename: "{app}\CodeWF.Markdown.Sample.exe"; Description: "Launch CodeWF Markdown Demo"; Flags: nowait postinstall skipifsilent
