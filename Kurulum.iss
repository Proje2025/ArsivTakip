; Arşiv Takip Programı Kurulum Scripti
; Inno Setup 6

#define MyAppName "Arşiv Takip Programı"
#define MyAppVersion "1.1.1"
#define MyAppPublisher "Birtana"
#define MyAppURL "https://github.com/Proje2025/ArsivTakip"
#define MyAppExeName "ArsivTakip.exe"

[Setup]
AppId={{8A7B9C3D-4E5F-6A7B-8C9D-0E1F2A3B4C5D}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}
AppUpdatesURL={#MyAppURL}
DefaultDirName={autopf}\{#MyAppName}
DisableProgramGroupPage=yes
LicenseFile=
PrivilegesRequired=lowest
OutputDir=Kurulum
OutputBaseFilename=ArsivTakipKurulum
SetupIconFile=Assets\icon.ico
Compression=lzma
SolidCompression=yes
WizardStyle=modern

[Languages]
Name: "turkish"; MessagesFile: "compiler:Languages\Turkish.isl"
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Files]
Source: "bin\Release\net8.0-windows\publish\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{autoprograms}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram}"; Flags: nowait postinstall skipifsilent

[Code]
procedure CurStepChanged(CurStep: TSetupStep);
var
  ConfigFile: TStringList;
begin
  if CurStep = ssPostInstall then
  begin
    ConfigFile := TStringList.Create;
    try
      ConfigFile.Add('{');
      ConfigFile.Add('  "ConnectionStrings": {');
      ConfigFile.Add('    "DefaultConnection": "Data Source=ArsivDB.db"');
      ConfigFile.Add('  },');
      ConfigFile.Add('  "PdfFolderPath": "ArsivPDF"');
      ConfigFile.Add('}');
      ConfigFile.SaveToFile(ExpandConstant('{app}\appsettings.json'));
    finally
      ConfigFile.Free;
    end;
  end;
end;