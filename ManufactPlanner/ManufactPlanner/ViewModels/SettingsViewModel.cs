// ViewModels/SettingsViewModel.cs
using ManufactPlanner.Models;
using ManufactPlanner.Services;
using Microsoft.EntityFrameworkCore;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reactive;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Input;

namespace ManufactPlanner.ViewModels
{
    public class SettingsViewModel : ViewModelBase
    {
        private readonly MainWindowViewModel _mainWindowViewModel;
        private readonly PostgresContext _dbContext;
        private readonly UserSettingsService _userSettingsService;

        // Профиль пользователя
        private string _username;
        private string _firstName;
        private string _lastName;
        private string _email;

        // Пароль
        private string _currentPassword = "";
        private string _newPassword = "";
        private string _confirmPassword = "";

        // Настройки уведомлений
        private bool _notifyNewTasks = true;
        private bool _notifyTaskStatusChanges = true;
        private bool _notifyComments = true;
        private bool _notifyDeadlines = true;
        private bool _notifyEmail = true;
        private bool _notifyDesktop = true;

        // Настройки интерфейса
        private int _selectedLanguage = 0;
        private bool _isLightTheme = true;
        private bool _autoStartEnabled;

        // Сообщения статуса
        private string _statusMessage = "";
        private bool _hasStatusMessage = false; // Изначально false, показываем сообщение только после действий
        private bool _isStatusSuccess = true;

        // Настройки подключения к БД
        private string _databaseHost = "localhost";
        private int _databasePort = 5432;
        private string _databaseName = "postgres";
        private string _databaseUsername = "postgres";
        private string _databasePassword = "";
        private bool _isConnectionValid = false;

        // Свойства подключения к БД
        public string DatabaseHost
        {
            get => _databaseHost;
            set => this.RaiseAndSetIfChanged(ref _databaseHost, value);
        }

        public int DatabasePort
        {
            get => _databasePort;
            set => this.RaiseAndSetIfChanged(ref _databasePort, value);
        }

        public string DatabaseName
        {
            get => _databaseName;
            set => this.RaiseAndSetIfChanged(ref _databaseName, value);
        }

        public string DatabaseUsername
        {
            get => _databaseUsername;
            set => this.RaiseAndSetIfChanged(ref _databaseUsername, value);
        }

        public string DatabasePassword
        {
            get => _databasePassword;
            set => this.RaiseAndSetIfChanged(ref _databasePassword, value);
        }

        public bool IsConnectionValid
        {
            get => _isConnectionValid;
            set => this.RaiseAndSetIfChanged(ref _isConnectionValid, value);
        }

        // Добавить команды
        public ReactiveCommand<Unit, Unit> TestConnectionCommand { get; }
        public ReactiveCommand<Unit, Unit> SaveConnectionSettingsCommand { get; }

        // Свойства профиля
        public string Username
        {
            get => _username;
            set => this.RaiseAndSetIfChanged(ref _username, value);
        }

        public string FirstName
        {
            get => _firstName;
            set => this.RaiseAndSetIfChanged(ref _firstName, value);
        }

        public string LastName
        {
            get => _lastName;
            set => this.RaiseAndSetIfChanged(ref _lastName, value);
        }

        public string Email
        {
            get => _email;
            set => this.RaiseAndSetIfChanged(ref _email, value);
        }

        // Свойства пароля
        public string CurrentPassword
        {
            get => _currentPassword;
            set => this.RaiseAndSetIfChanged(ref _currentPassword, value);
        }

        public string NewPassword
        {
            get => _newPassword;
            set => this.RaiseAndSetIfChanged(ref _newPassword, value);
        }

        public string ConfirmPassword
        {
            get => _confirmPassword;
            set => this.RaiseAndSetIfChanged(ref _confirmPassword, value);
        }

        // Свойства уведомлений
        public bool NotifyNewTasks
        {
            get => _notifyNewTasks;
            set => this.RaiseAndSetIfChanged(ref _notifyNewTasks, value);
        }

        public bool NotifyTaskStatusChanges
        {
            get => _notifyTaskStatusChanges;
            set => this.RaiseAndSetIfChanged(ref _notifyTaskStatusChanges, value);
        }

        public bool NotifyComments
        {
            get => _notifyComments;
            set => this.RaiseAndSetIfChanged(ref _notifyComments, value);
        }

        public bool NotifyDeadlines
        {
            get => _notifyDeadlines;
            set => this.RaiseAndSetIfChanged(ref _notifyDeadlines, value);
        }

        public bool NotifyEmail
        {
            get => _notifyEmail;
            set => this.RaiseAndSetIfChanged(ref _notifyEmail, value);
        }

        public bool NotifyDesktop
        {
            get => _notifyDesktop;
            set => this.RaiseAndSetIfChanged(ref _notifyDesktop, value);
        }

        // Свойства интерфейса
        public int SelectedLanguage
        {
            get => _selectedLanguage;
            set => this.RaiseAndSetIfChanged(ref _selectedLanguage, value);
        }

        public bool IsLightTheme
        {
            get => _isLightTheme;
            set => this.RaiseAndSetIfChanged(ref _isLightTheme, value);
        }

        public bool AutoStartEnabled
        {
            get => _autoStartEnabled;
            set => this.RaiseAndSetIfChanged(ref _autoStartEnabled, value);
        }

        // Свойства сообщений
        public string StatusMessage
        {
            get => _statusMessage;
            set
            {
                this.RaiseAndSetIfChanged(ref _statusMessage, value);
                HasStatusMessage = !string.IsNullOrEmpty(value);
            }
        }

        public bool HasStatusMessage
        {
            get => _hasStatusMessage;
            set => this.RaiseAndSetIfChanged(ref _hasStatusMessage, value);
        }

        public bool IsStatusSuccess
        {
            get => _isStatusSuccess;
            set => this.RaiseAndSetIfChanged(ref _isStatusSuccess, value);
        }

        // Команды
        public ReactiveCommand<Unit, bool> SaveProfileCommand { get; }
        public ReactiveCommand<Unit, bool> ChangePasswordCommand { get; }
        public ReactiveCommand<Unit, Unit> SaveNotificationSettingsCommand { get; }
        public ReactiveCommand<Unit, Unit> ApplyInterfaceSettingsCommand { get; }
        public ReactiveCommand<Unit, Unit> SendTestEmailCommand { get; }
        public ReactiveCommand<Unit, Unit> ApplyAutoStartCommand { get; }

        public ReactiveCommand<Unit, Unit> ResetToDefaultCommand { get; }

        public SettingsViewModel(MainWindowViewModel mainWindowViewModel, PostgresContext dbContext)
        {
            _mainWindowViewModel = mainWindowViewModel;
            _dbContext = dbContext;
            _userSettingsService = new UserSettingsService(dbContext);

            // Инициализируем команды
            SaveProfileCommand = ReactiveCommand.CreateFromTask(SaveProfile);
            ChangePasswordCommand = ReactiveCommand.CreateFromTask(ChangePassword);
            SaveNotificationSettingsCommand = ReactiveCommand.Create(SaveNotificationSettings);
            ApplyInterfaceSettingsCommand = ReactiveCommand.Create(ApplyInterfaceSettings);
            SendTestEmailCommand = ReactiveCommand.CreateFromTask(SendTestEmail);
            ApplyAutoStartCommand = ReactiveCommand.Create(ApplyAutoStartSettings);
            ResetToDefaultCommand = ReactiveCommand.Create(ResetToDefault);
            // Загрузка настроек пользователя
            _ = LoadUserSettingsAsync();

            // Проверка автозапуска
            try
            {
                AutoStartEnabled = AutoStartService.IsAutoStartEnabled();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Ошибка при проверке автозапуска: {ex.Message}");
                AutoStartEnabled = false;
            }
            TestConnectionCommand = ReactiveCommand.CreateFromTask(TestConnection);
            SaveConnectionSettingsCommand = ReactiveCommand.CreateFromTask(SaveConnectionSettings);

            // Загружаем настройки подключения
            LoadConnectionSettings();
            // Очищаем сообщение о статусе при инициализации
            HasStatusMessage = false;
        }

        private void ResetToDefault()
        {
            SetDefaultConnectionSettings();
            StatusMessage = "Настройки сброшены к значениям по умолчанию";
            IsStatusSuccess = true;
        }
        // Модификация метода LoadConnectionSettings
        private void LoadConnectionSettings()
        {
            try
            {
                var appSettings = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(
                    File.ReadAllText(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                        "ManufactPlanner", "database-settings.json"))
                );

                if (appSettings.TryGetValue("Host", out var host)) DatabaseHost = host.ToString();
                if (appSettings.TryGetValue("Port", out var port)) DatabasePort = int.Parse(port.ToString());
                if (appSettings.TryGetValue("Database", out var database)) DatabaseName = database.ToString();
                if (appSettings.TryGetValue("Username", out var username)) DatabaseUsername = username.ToString();
                if (appSettings.TryGetValue("Password", out var password)) DatabasePassword = password.ToString();

                // Проверяем, действительны ли загруженные настройки
                var connectionString = $"Host={DatabaseHost};Port={DatabasePort};Database={DatabaseName};Username={DatabaseUsername};Password={DatabasePassword}";
                IsConnectionValid = IsConnectionStringValid(connectionString);

                if (!IsConnectionValid)
                {
                    StatusMessage = "Сохраненные настройки подключения недействительны. Проверьте настройки.";
                    IsStatusSuccess = false;
                }
            }
            catch
            {
                // Если файл не существует или не читается, используем значения по умолчанию
                SetDefaultConnectionSettings();
            }
        }

        // Новый метод для установки значений по умолчанию
        private void SetDefaultConnectionSettings()
        {
            DatabaseHost = "localhost";
            DatabasePort = 5432;
            DatabaseName = "postgres";
            DatabaseUsername = "postgres";
            DatabasePassword = "";
            IsConnectionValid = false;
        }
        private async System.Threading.Tasks.Task SaveConnectionSettings()
        {
            try
            {
                var settingsPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "ManufactPlanner"
                );

                if (!Directory.Exists(settingsPath))
                    Directory.CreateDirectory(settingsPath);

                var settings = new Dictionary<string, object>
        {
            {"Host", DatabaseHost},
            {"Port", DatabasePort},
            {"Database", DatabaseName},
            {"Username", DatabaseUsername},
            {"Password", DatabasePassword}
        };

                await File.WriteAllTextAsync(
                    Path.Combine(settingsPath, "database-settings.json"),
                    System.Text.Json.JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true })
                );

                StatusMessage = "Настройки подключения к БД сохранены. Перезапустите приложение для применения изменений.";
                IsStatusSuccess = true;
            }
            catch (Exception ex)
            {
                StatusMessage = $"Ошибка при сохранении настроек: {ex.Message}";
                IsStatusSuccess = false;
            }
        }
        private bool IsConnectionStringValid(string connectionString)
        {
            try
            {
                using var connection = new Npgsql.NpgsqlConnection(connectionString);
                connection.Open();
                connection.Close();
                return true;
            }
            catch
            {
                return false;
            }
        }
        private async System.Threading.Tasks.Task TestConnection()
        {
            try
            {
                var connectionString = $"Host={DatabaseHost};Port={DatabasePort};Database={DatabaseName};Username={DatabaseUsername};Password={DatabasePassword}";

                using var context = new PostgresContext(
                    new DbContextOptionsBuilder<PostgresContext>()
                        .UseNpgsql(connectionString)
                        .Options
                );

                // Простой тест подключения
                await context.Database.CanConnectAsync();

                IsConnectionValid = true;
                StatusMessage = "Подключение к базе данных успешно установлено";
                IsStatusSuccess = true;
            }
            catch (Exception ex)
            {
                IsConnectionValid = false;
                StatusMessage = $"Ошибка подключения к БД: {ex.Message}";
                IsStatusSuccess = false;
            }
        }
        public SettingsViewModel()
        {
            // Конструктор для дизайнера
            HasStatusMessage = false;

            SaveProfileCommand = ReactiveCommand.CreateFromTask(SaveProfile);
            ChangePasswordCommand = ReactiveCommand.CreateFromTask(ChangePassword);
            SaveNotificationSettingsCommand = ReactiveCommand.Create(SaveNotificationSettings);
            ApplyInterfaceSettingsCommand = ReactiveCommand.Create(ApplyInterfaceSettings);
            SendTestEmailCommand = ReactiveCommand.CreateFromTask(SendTestEmail);
            ApplyAutoStartCommand = ReactiveCommand.Create(ApplyAutoStartSettings);
        }

        private async Task<bool> SaveProfile()
        {
            if (_mainWindowViewModel == null || _mainWindowViewModel.CurrentUserId == Guid.Empty)
            {
                StatusMessage = "Не удалось найти информацию о пользователе";
                IsStatusSuccess = false;
                return false;
            }

            var user = await _userSettingsService.GetUserProfileAsync(_mainWindowViewModel.CurrentUserId);
            if (user == null)
            {
                StatusMessage = "Не удалось найти профиль пользователя";
                IsStatusSuccess = false;
                return false;
            }

            user.FirstName = FirstName;
            user.LastName = LastName;
            user.Email = Email;

            var result = await _userSettingsService.UpdateUserProfileAsync(user);
            if (result)
            {
                // Обновляем имя пользователя в главном ViewModel
                _mainWindowViewModel.CurrentUserName = $"{FirstName} {LastName}";

                StatusMessage = "Профиль успешно обновлен";
                IsStatusSuccess = true;
                return true;
            }
            else
            {
                StatusMessage = "Не удалось обновить профиль";
                IsStatusSuccess = false;
                return false;
            }
        }

        private async Task<bool> ChangePassword()
        {
            // Проверка соответствия нового пароля и подтверждения
            if (string.IsNullOrEmpty(CurrentPassword))
            {
                StatusMessage = "Введите текущий пароль";
                IsStatusSuccess = false;
                return false;
            }

            if (string.IsNullOrEmpty(NewPassword))
            {
                StatusMessage = "Введите новый пароль";
                IsStatusSuccess = false;
                return false;
            }

            if (NewPassword != ConfirmPassword)
            {
                StatusMessage = "Новый пароль и подтверждение не совпадают";
                IsStatusSuccess = false;
                return false;
            }

            if (_mainWindowViewModel == null || _mainWindowViewModel.CurrentUserId == Guid.Empty)
            {
                StatusMessage = "Не удалось найти информацию о пользователе";
                IsStatusSuccess = false;
                return false;
            }

            var result = await _userSettingsService.ChangePasswordAsync(
                _mainWindowViewModel.CurrentUserId,
                CurrentPassword,
                NewPassword);

            if (result)
            {
                // Очищаем поля ввода пароля
                CurrentPassword = string.Empty;
                NewPassword = string.Empty;
                ConfirmPassword = string.Empty;

                StatusMessage = "Пароль успешно изменен";
                IsStatusSuccess = true;
                return true;
            }
            else
            {
                StatusMessage = "Не удалось изменить пароль. Проверьте правильность текущего пароля";
                IsStatusSuccess = false;
                return false;
            }
        }

        public async System.Threading.Tasks.Task LoadUserSettingsAsync()
        {
            if (_mainWindowViewModel?.CurrentUserId == null || _mainWindowViewModel.CurrentUserId == Guid.Empty)
                return;

            try
            {
                // Загружаем информацию о пользователе
                var user = await _userSettingsService.GetUserProfileAsync(_mainWindowViewModel.CurrentUserId);
                if (user != null)
                {
                    await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        Username = user.Username;
                        FirstName = user.FirstName;
                        LastName = user.LastName;
                        Email = user.Email ?? string.Empty;
                    });
                }

                // Загружаем настройки пользователя
                var settings = await _dbContext.UserSettings
                    .FirstOrDefaultAsync(s => s.UserId == _mainWindowViewModel.CurrentUserId);

                if (settings != null)
                {
                    // Настройки уведомлений - преобразуем nullable bool в bool
                    NotifyNewTasks = settings.NotifyNewTasks ?? true;
                    NotifyTaskStatusChanges = settings.NotifyStatusChanges ?? true;
                    NotifyComments = settings.NotifyComments ?? true;
                    NotifyDeadlines = settings.NotifyDeadlines ?? true;
                    NotifyEmail = settings.NotifyEmail ?? true;
                    NotifyDesktop = settings.NotifyDesktop ?? true;

                    // Обновляем настройки в главном ViewModel
                    _mainWindowViewModel.NotifyDesktopEnabled = settings.NotifyDesktop ?? true;
                    _mainWindowViewModel.NotifyEmailEnabled = settings.NotifyEmail ?? true;

                    // Настройки интерфейса
                    IsLightTheme = settings.Islighttheme ?? true;
                    AutoStartEnabled = settings.AutoStartEnabled ?? false;

                    // Применяем тему, если нужно
                    ThemeService.Instance.IsLightTheme = IsLightTheme;
                }

                // Очищаем сообщение о статусе после загрузки
                HasStatusMessage = false;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Ошибка при загрузке настроек: {ex.Message}");
                // Не показываем ошибку пользователю при загрузке, только записываем в лог
            }
        }

        private async System.Threading.Tasks.Task SaveUserSettingsAsync()
        {
            if (_mainWindowViewModel?.CurrentUserId == null || _mainWindowViewModel.CurrentUserId == Guid.Empty)
                return;

            try
            {
                // Получаем или создаем настройки пользователя
                var settings = await _dbContext.UserSettings
                    .FirstOrDefaultAsync(s => s.UserId == _mainWindowViewModel.CurrentUserId);

                if (settings == null)
                {
                    settings = new UserSetting
                    {
                        UserId = _mainWindowViewModel.CurrentUserId
                    };
                    _dbContext.UserSettings.Add(settings);
                }

                // Обновляем настройки
                settings.NotifyNewTasks = NotifyNewTasks;
                settings.NotifyStatusChanges = NotifyTaskStatusChanges;
                settings.NotifyComments = NotifyComments;
                settings.NotifyDeadlines = NotifyDeadlines;
                settings.NotifyEmail = NotifyEmail;
                settings.NotifyDesktop = NotifyDesktop;
                settings.AutoStartEnabled = AutoStartEnabled;
                settings.Islighttheme = IsLightTheme;
                settings.UpdatedAt = DateTime.Now;

                await _dbContext.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Ошибка при сохранении настроек: {ex.Message}");
                throw; // Пробрасываем исключение для обработки в вызывающем методе
            }
        }

        private async void SaveNotificationSettings()
        {
            try
            {
                // Проверяем валидность настроек email
                if (!ValidateEmailSettings())
                    return;

                await SaveUserSettingsAsync();

                // Обновляем настройки в MainViewModel
                if (_mainWindowViewModel != null)
                {
                    _mainWindowViewModel.NotifyDesktopEnabled = NotifyDesktop;
                    _mainWindowViewModel.NotifyEmailEnabled = NotifyEmail;
                }

                // Показываем сообщение об успехе
                StatusMessage = "Настройки уведомлений сохранены";
                IsStatusSuccess = true;
            }
            catch (Exception ex)
            {
                StatusMessage = $"Ошибка при сохранении настроек: {ex.Message}";
                IsStatusSuccess = false;
            }
        }

        private bool ValidateEmailSettings()
        {
            if (NotifyEmail && string.IsNullOrWhiteSpace(Email))
            {
                StatusMessage = "Для включения email-уведомлений необходимо указать адрес электронной почты";
                IsStatusSuccess = false;
                NotifyEmail = false; // Отключаем настройку
                return false;
            }

            if (NotifyEmail && !EmailService.Instance.IsValidEmail(Email))
            {
                StatusMessage = "Указан некорректный формат email-адреса";
                IsStatusSuccess = false;
                NotifyEmail = false; // Отключаем настройку
                return false;
            }

            return true;
        }

        private async void ApplyInterfaceSettings()
        {
            try
            {
                // Применяем настройки темы
                ThemeService.Instance.IsLightTheme = IsLightTheme;

                // Сохраняем настройки в БД
                await SaveUserSettingsAsync();

                // Показываем сообщение об успешном применении
                StatusMessage = "Настройки интерфейса успешно применены";
                IsStatusSuccess = true;
            }
            catch (Exception ex)
            {
                StatusMessage = $"Ошибка при применении настроек: {ex.Message}";
                IsStatusSuccess = false;
            }
        }

        private void ApplyAutoStartSettings()
        {
            try
            {
                // Применяем настройки автозапуска
                AutoStartService.SetAutoStart(AutoStartEnabled);

                // Показываем сообщение об успешном применении
                StatusMessage = "Настройки автозапуска успешно применены";
                IsStatusSuccess = true;
            }
            catch (Exception ex)
            {
                StatusMessage = $"Ошибка при настройке автозапуска: {ex.Message}";
                IsStatusSuccess = false;
            }
        }

        private async System.Threading.Tasks.Task SendTestEmail()
        {
            if (string.IsNullOrWhiteSpace(Email) || !EmailService.Instance.IsValidEmail(Email))
            {
                StatusMessage = "Пожалуйста, укажите корректный email в профиле";
                IsStatusSuccess = false;
                return;
            }

            StatusMessage = "Отправка тестового письма...";
            IsStatusSuccess = true;

            try
            {
                var result = await EmailService.Instance.SendTestEmailAsync(Email);

                if (result.Success)
                {
                    StatusMessage = "Тестовое письмо успешно отправлено";
                    IsStatusSuccess = true;
                }
                else
                {
                    StatusMessage = result.Message;
                    IsStatusSuccess = false;
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Ошибка при отправке письма: {ex.Message}";
                IsStatusSuccess = false;
            }
        }
    }
}