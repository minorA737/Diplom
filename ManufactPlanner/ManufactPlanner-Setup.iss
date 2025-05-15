; Скрипт установщика для ManufactPlanner (исправленная версия)
; Только Release версия
; Версия: 1.0.0

[Setup]
AppId={{B1F8E3D4-9A5C-4E2F-A7B9-6D1C8F2E5A3B}}
AppName=ManufactPlanner
AppVersion=1.0.0
AppVerName=ManufactPlanner 1.0.0
AppPublisher=Дипломный проект
AppPublisherURL=https://github.com/yourusername/manufactplanner
AppSupportURL=https://github.com/yourusername/manufactplanner/issues
AppUpdatesURL=https://github.com/yourusername/manufactplanner/releases
DefaultDirName={autopf}\ManufactPlanner
DisableProgramGroupPage=yes
OutputDir=D:\aa_Диплом\ManufactPlanner\Setup
OutputBaseFilename=ManufactPlanner-1.0.0-Setup
Compression=lzma
SolidCompression=yes
WizardStyle=modern
ArchitecturesInstallIn64BitMode=x64
PrivilegesRequired=lowest
MinVersion=10.0.17763

; Иконки
SetupIconFile=D:\aa_Диплом\ManufactPlanner\ManufactPlanner\Assets\logo.ico
UninstallDisplayIcon={app}\ManufactPlanner.exe
UninstallDisplayName=ManufactPlanner

[Languages]
Name: "russian"; MessagesFile: "compiler:Languages\Russian.isl"
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked
Name: "quicklaunchicon"; Description: "{cm:CreateQuickLaunchIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked; OnlyBelowVersion: 6.1
Name: "associate"; Description: "Ассоциировать .mpp файлы с ManufactPlanner"; GroupDescription: "Ассоциации файлов"; Flags: unchecked

[Files]
; Основной исполняемый файл
Source: "D:\aa_Диплом\ManufactPlanner\ManufactPlanner\bin\Release\net8.0\ManufactPlanner.exe"; DestDir: "{app}"; Flags: ignoreversion

; Все файлы из Release папки (без отладочных файлов)
Source: "D:\aa_Диплом\ManufactPlanner\ManufactPlanner\bin\Release\net8.0\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs; Excludes: "*.pdb,*.xml,*.dev.json"

; Файлы ресурсов (иконки, изображения)
Source: "D:\aa_Диплом\ManufactPlanner\ManufactPlanner\Assets\*"; DestDir: "{app}\Assets"; Flags: ignoreversion recursesubdirs createallsubdirs

; Файлы документации (опционально)
; Source: "D:\aa_Диплом\ManufactPlanner\README.md"; DestDir: "{app}"; DestName: "README.txt"; Flags: ignoreversion
; Source: "D:\aa_Диплом\ManufactPlanner\LICENSE.txt"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{autoprograms}\ManufactPlanner"; Filename: "{app}\ManufactPlanner.exe"; Comment: "Система планирования разработки изделий"
Name: "{autodesktop}\ManufactPlanner"; Filename: "{app}\ManufactPlanner.exe"; Comment: "Система планирования разработки изделий"; Tasks: desktopicon
Name: "{userappdata}\Microsoft\Internet Explorer\Quick Launch\ManufactPlanner"; Filename: "{app}\ManufactPlanner.exe"; Tasks: quicklaunchicon

[Registry]
Root: HKA; Subkey: "Software\Classes\.mpp"; ValueType: string; ValueName: ""; ValueData: "ManufactPlanner.Project"; Flags: uninsdeletevalue; Tasks: associate
Root: HKA; Subkey: "Software\Classes\ManufactPlanner.Project"; ValueType: string; ValueName: ""; ValueData: "ManufactPlanner Project"; Flags: uninsdeletekey; Tasks: associate
Root: HKA; Subkey: "Software\Classes\ManufactPlanner.Project\DefaultIcon"; ValueType: string; ValueName: ""; ValueData: "{app}\ManufactPlanner.exe,0"; Tasks: associate
Root: HKA; Subkey: "Software\Classes\ManufactPlanner.Project\shell\open\command"; ValueType: string; ValueName: ""; ValueData: """{app}\ManufactPlanner.exe"" ""%1"""; Tasks: associate

[Run]
Filename: "{app}\ManufactPlanner.exe"; Description: "{cm:LaunchProgram,ManufactPlanner}"; Flags: nowait postinstall skipifsilent

[Code]
// Проверка наличия .NET 8.0 Runtime
function IsDotNet8RuntimeInstalled: Boolean;
var
  Version: String;
begin
  Result := False;
  
  // Проверяем 64-битную версию
  if RegQueryStringValue(HKEY_LOCAL_MACHINE, 'SOFTWARE\dotnet\Setup\InstalledVersions\x64\sharedhost', 'Version', Version) then
  begin
    if Pos('8.', Version) = 1 then
      Result := True;
  end
  else
  // Проверяем 32-битную версию в реестре WOW64
  if RegQueryStringValue(HKEY_LOCAL_MACHINE, 'SOFTWARE\WOW6432Node\dotnet\Setup\InstalledVersions\x64\sharedhost', 'Version', Version) then
  begin
    if Pos('8.', Version) = 1 then
      Result := True;
  end;
end;

// Проверка перед началом установки
function InitializeSetup: Boolean;
var
  ResultCode: Integer;
begin
  Result := True;
  
  // Проверяем наличие .NET 8.0 Runtime
  if not IsDotNet8RuntimeInstalled then
  begin
    if MsgBox('Для работы ManufactPlanner необходим Microsoft .NET 8.0 Runtime.' + #13#10 + 
              'Хотите скачать его сейчас?' + #13#10 + #13#10 + 
              'Нажмите "Да" для перехода на страницу загрузки или "Нет" для продолжения.',
              mbConfirmation, MB_YESNO) = IDYES then
    begin
      ShellExec('open', 'https://dotnet.microsoft.com/download/dotnet/8.0', '', '', SW_SHOWNORMAL, ewNoWait, ResultCode);
    end;
  end;
end;

// Создание начальных файлов конфигурации
procedure CurStepChanged(CurStep: TSetupStep);
var
  ConfigFile: String;
  ConfigContent: TStringList;
  UserConfigDir: String;
  BuildInfoFile: String;
begin
  if CurStep = ssPostInstall then
  begin
    // Создание папки для пользовательских данных
    UserConfigDir := ExpandConstant('{userappdata}\ManufactPlanner');
    ForceDirectories(UserConfigDir);
    
    // Создание файла конфигурации базы данных
    ConfigFile := ExpandConstant('{app}\appsettings.json');
    if not FileExists(ConfigFile) then
    begin
      ConfigContent := TStringList.Create;
      try
        ConfigContent.Add('{');
        ConfigContent.Add('  "ConnectionStrings": {');
        ConfigContent.Add('    "DefaultConnection": "Host=localhost;Port=5432;Database=postgres;Username=postgres;Password=123"');
        ConfigContent.Add('  },');
        ConfigContent.Add('  "Logging": {');
        ConfigContent.Add('    "LogLevel": {');
        ConfigContent.Add('      "Default": "Information",');
        ConfigContent.Add('      "Microsoft": "Warning",');
        ConfigContent.Add('      "Microsoft.Hosting.Lifetime": "Information"');
        ConfigContent.Add('    }');
        ConfigContent.Add('  },');
        ConfigContent.Add('  "AllowedHosts": "*",');
        ConfigContent.Add('  "Application": {');
        ConfigContent.Add('    "Name": "ManufactPlanner",');
        ConfigContent.Add('    "Version": "1.0.0",');
        ConfigContent.Add('    "Configuration": "Release",');
        ConfigContent.Add('  }');
        ConfigContent.Add('}');
        ConfigContent.SaveToFile(ConfigFile);
      finally
        ConfigContent.Free;
      end;
    end;
    
    // Создание файла с информацией о сборке
    BuildInfoFile := ExpandConstant('{app}\build-info.txt');
    ConfigContent := TStringList.Create;
    try
      ConfigContent.Add('ManufactPlanner Build Information');
      ConfigContent.Add('==========================================');
      ConfigContent.Add('Version: 1.0.0');
      ConfigContent.Add('Configuration: Release');
      ConfigContent.Add('');
      ConfigContent.Add('Release сборка');
      ConfigContent.Add('- Оптимизированная версия для пользователей');
      ConfigContent.Add('- Лучшая производительность');
      ConfigContent.Add('- Официальная версия для использования');
      ConfigContent.Add('');
      ConfigContent.SaveToFile(BuildInfoFile);
    finally
      ConfigContent.Free;
    end;
  end;
end;

// Настройка интерфейса мастера
procedure InitializeWizard;
begin
  WizardForm.WelcomeLabel1.Caption := 'Добро пожаловать в мастер установки ManufactPlanner';
  WizardForm.WelcomeLabel2.Caption := 
    'Эта программа поможет вам установить систему автоматизации планирования ' +
    'разработки изделий на ваш компьютер.' + #13#10 + #13#10 +
    'Рекомендуется закрыть все запущенные приложения перед началом установки.' + #13#10 + #13#10 +
    'Нажмите "Далее" для продолжения или "Отмена" для выхода из мастера установки.';
    
  WizardForm.FinishedLabel.Caption :=
    'Установка ManufactPlanner завершена успешно!' + #13#10 + #13#10 +
    'При первом запуске приложения вам потребуется настроить подключение к базе данных PostgreSQL.' + #13#10 + #13#10 +
    'Нажмите "Готово" для завершения мастера установки.';
end;