; Arşiv Takip Programı Kurulum Scripti
; Inno Setup 6

#define MyAppName "Arşiv Takip Programı"
#define MyAppVersion "1.0.1"
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
SetupIconFile=
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
var
  DatabaseNamePage: TInputQueryWizardPage;
  DatabaseName: string;

procedure InitializeWizard;
begin
  DatabaseNamePage := CreateInputQueryPage(
    wpSelectTasks,
    'Veritabanı Ayarları',
    'SQL Server Veritabanı Adı',
    'Lütfen kullanmak istediğiniz veritabanı adını giriniz:'
  );

  DatabaseNamePage.Add('Veritabanı Adı:', False);
  DatabaseNamePage.Values[0] := 'ArsivDB';
end;

function NextButtonClick(CurPageID: Integer): Boolean;
begin
  if CurPageID = DatabaseNamePage.ID then
  begin
    DatabaseName := DatabaseNamePage.Values[0];
    if Trim(DatabaseName) = '' then
    begin
      MsgBox('Lütfen bir veritabanı adı giriniz!', mbError, MB_OK);
      Result := False;
      Exit;
    end;
  end;
  Result := True;
end;

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
      ConfigFile.Add('    "DefaultConnection": "Server=SUNUCU\\SQLEXPRESS;Database=' + DatabaseName + ';Trusted_Connection=True;TrustServerCertificate=True;"');
      ConfigFile.Add('  },');
      ConfigFile.Add('  "PdfFolderPath": "\\\\SUNUCU\\Arsiv"');
      ConfigFile.Add('}');
      ConfigFile.SaveToFile(ExpandConstant('{app}\appsettings.json'));
    finally
      ConfigFile.Free;
    end;
  end;
end;