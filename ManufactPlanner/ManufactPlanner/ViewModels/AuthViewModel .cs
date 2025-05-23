﻿using System;
using System.Linq;
using System.Reactive;
using Avalonia.Controls;
using ReactiveUI;
using ManufactPlanner.Models;
using ManufactPlanner.Services;
using System.Threading.Tasks;
using System.IO;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace ManufactPlanner.ViewModels
{
    public class AuthViewModel : ViewModelBase
    {
        private readonly MainWindowViewModel _mainWindowViewModel;
        private readonly PostgresContext _dbContext;
        private readonly UserCredentialService _credentialService;

        private string _username = string.Empty;
        private string _password = string.Empty;
        private bool _rememberMe = false;
        private string _errorMessage = string.Empty;
        private bool _hasError = false;
        private bool _isLoading = false;
        private bool _isAutoLogin = false;
        // Настройки БД
        private bool _isDbSettingsOpen = false;
        private string _databaseHost = "localhost";
        private int _databasePort = 5432;
        private string _databaseName = "postgres";
        private string _databaseUsername = "postgres";
        private string _databasePassword = "";
        private bool _isConnectionValid = false;
        private string _connectionStatus = "Не проверено";

        public bool IsDbSettingsOpen
        {
            get => _isDbSettingsOpen;
            set => this.RaiseAndSetIfChanged(ref _isDbSettingsOpen, value);
        }

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
            set
            {
                this.RaiseAndSetIfChanged(ref _isConnectionValid, value);
                ConnectionStatus = value ? "Подключение успешно" : "Ошибка подключения";
            }
        }

        public string ConnectionStatus
        {
            get => _connectionStatus;
            set => this.RaiseAndSetIfChanged(ref _connectionStatus, value);
        }

        public ReactiveCommand<Unit, Unit> TestConnectionCommand { get; }
        public ReactiveCommand<Unit, Unit> SaveDbSettingsCommand { get; }
        public string Username
        {
            get => _username;
            set
            {
                this.RaiseAndSetIfChanged(ref _username, value);
                this.RaisePropertyChanged(nameof(HasUsername));
            }
        }

        public string Password
        {
            get => _password;
            set
            {
                this.RaiseAndSetIfChanged(ref _password, value);
                this.RaisePropertyChanged(nameof(HasPassword));
            }
        }

        public bool RememberMe
        {
            get => _rememberMe;
            set => this.RaiseAndSetIfChanged(ref _rememberMe, value);
        }

        public string ErrorMessage
        {
            get => _errorMessage;
            set
            {
                this.RaiseAndSetIfChanged(ref _errorMessage, value);
                HasError = !string.IsNullOrEmpty(value);
            }
        }

        public bool HasError
        {
            get => _hasError;
            set => this.RaiseAndSetIfChanged(ref _hasError, value);
        }

        public bool IsLoading
        {
            get => _isLoading;
            set => this.RaiseAndSetIfChanged(ref _isLoading, value);
        }

        public bool HasUsername => !string.IsNullOrEmpty(Username);
        public bool HasPassword => !string.IsNullOrEmpty(Password);

        public ReactiveCommand<Unit, Unit> LoginCommand { get; }

        public AuthViewModel(MainWindowViewModel mainWindowViewModel, PostgresContext dbContext)
        {
            _mainWindowViewModel = mainWindowViewModel;
            _dbContext = dbContext;
            _credentialService = new UserCredentialService();

            LoginCommand = ReactiveCommand.Create(Login);

            // Загружаем сохраненные учетные данные при создании ViewModel
            TestConnectionCommand = ReactiveCommand.CreateFromTask(TestDatabaseConnection);
            SaveDbSettingsCommand = ReactiveCommand.CreateFromTask(SaveDatabaseSettings);

            // Загружаем настройки БД при создании ViewModel
            LoadDatabaseSettings();
            LoadSavedCredentialsAsync();
        }
        private void LoadDatabaseSettings()
        {
            try
            {
                var settingsPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "ManufactPlanner",
                    "database-settings.json"
                );

                if (File.Exists(settingsPath))
                {
                    var settingsText = File.ReadAllText(settingsPath);
                    var settings = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(settingsText);

                    DatabaseHost = settings.ContainsKey("Host") ? settings["Host"].ToString() : "localhost";
                    DatabasePort = settings.ContainsKey("Port") ? int.Parse(settings["Port"].ToString()) : 5432;
                    DatabaseName = settings.ContainsKey("Database") ? settings["Database"].ToString() : "postgres";
                    DatabaseUsername = settings.ContainsKey("Username") ? settings["Username"].ToString() : "postgres";
                    DatabasePassword = settings.ContainsKey("Password") ? settings["Password"].ToString() : "";
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка загрузки настроек БД: {ex.Message}");
            }
        }

        private async System.Threading.Tasks.Task TestDatabaseConnection()
        {
            try
            {
                ConnectionStatus = "Проверка подключения...";
                var connectionString = $"Host={DatabaseHost};Port={DatabasePort};Database={DatabaseName};Username={DatabaseUsername};Password={DatabasePassword}";

                using var connection = new Npgsql.NpgsqlConnection(connectionString);
                await connection.OpenAsync();
                connection.Close();

                IsConnectionValid = true;
            }
            catch (Exception ex)
            {
                IsConnectionValid = false;
                ConnectionStatus = $"Ошибка: {ex.Message}";
            }
        }

        private async System.Threading.Tasks.Task SaveDatabaseSettings()
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

                // Создаем новый контекст с новыми настройками
                var optionsBuilder = new DbContextOptionsBuilder<PostgresContext>();
                var connectionString = $"Host={DatabaseHost};Port={DatabasePort};Database={DatabaseName};Username={DatabaseUsername};Password={DatabasePassword}";
                optionsBuilder.UseNpgsql(connectionString);

                // Обновляем контекст в MainWindowViewModel 
                _mainWindowViewModel.DbContext?.Dispose();
                _mainWindowViewModel.DbContext = new PostgresContext(optionsBuilder.Options);

                ConnectionStatus = "Настройки сохранены";
                IsDbSettingsOpen = false; // Закрываем окно после сохранения
            }
            catch (Exception ex)
            {
                ConnectionStatus = $"Ошибка сохранения: {ex.Message}";
            }
        }
        private async void LoadSavedCredentialsAsync()
        {
            try
            {
                var savedCredentials = await _credentialService.LoadCredentialsAsync();
                if (savedCredentials != null)
                {
                    Username = savedCredentials.Username;
                    Password = savedCredentials.Password;
                    RememberMe = true;
                    _isAutoLogin = true;

                    // Автоматически выполняем вход, если есть сохраненные данные
                    await System.Threading.Tasks.Task.Delay(500); // Небольшая задержка для корректного отображения UI
                    Login();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при загрузке учетных данных: {ex.Message}");
            }
        }

        private async void Login()
        {
            // Проверяем, что поля не пустые
            if (string.IsNullOrEmpty(Username))
            {
                ErrorMessage = "Пожалуйста, введите имя пользователя";
                return;
            }

            if (string.IsNullOrEmpty(Password))
            {
                ErrorMessage = "Пожалуйста, введите пароль";
                return;
            }

            try
            {
                IsLoading = true;
                ErrorMessage = string.Empty;

                // Ищем пользователя в базе данных по логину и паролю
                var user = _dbContext.Users
                    .FirstOrDefault(u => u.Username == Username && u.Password == Password);

                if (user != null)
                {
                    // Проверяем, активен ли пользователь (с учетом nullable типа)
                    if (user.IsActive.HasValue && !user.IsActive.Value)
                    {
                        ErrorMessage = "Учетная запись деактивирована. Обратитесь к администратору.";
                        if (_isAutoLogin)
                        {
                            // Если это автоматический вход, то удаляем сохраненные данные
                            _credentialService.ClearCredentials();
                            _isAutoLogin = false;
                        }
                        return;
                    }

                    // Обновляем дату последнего входа
                    user.LastLogin = DateTime.Now;
                    _dbContext.SaveChanges();

                    // Устанавливаем имя пользователя в главном ViewModel
                    _mainWindowViewModel.CurrentUserName = $"{user.FirstName} {user.LastName}";
                    _mainWindowViewModel.Inicial = $"{user.FirstName?[0]}{user.LastName?[0]}".ToUpper(); // Добавьте эту строку
                    _mainWindowViewModel.DbContext = _dbContext;

                    _mainWindowViewModel.CurrentUserId = user.Id ; // Сохраняем ID текущего пользователя
                    await _mainWindowViewModel.LoadAndApplyUserSettingsAsync(user.Id);

                    var roleService = RoleService.Instance;
                    _mainWindowViewModel.UserRoles = await roleService.GetUserRolesAsync(_dbContext, user.Id);
                    _mainWindowViewModel.RaisePropertyChanged(nameof(_mainWindowViewModel.IsAdministrator));
                    _mainWindowViewModel.RaisePropertyChanged(nameof(_mainWindowViewModel.IsManager));
                    _mainWindowViewModel.RaisePropertyChanged(nameof(_mainWindowViewModel.IsExecutor));
                    _mainWindowViewModel.RaisePropertyChanged(nameof(_mainWindowViewModel.IsAdministratorOrManager));

                    // Загружаем количество непрочитанных уведомлений
                    var unreadNotifications = _dbContext.Notifications
                        .Count(n => n.UserId == user.Id && n.IsRead != true);

                    _mainWindowViewModel.UnreadNotificationsCount = unreadNotifications;

                    // Если пользователь хочет сохранить данные для входа
                    if (RememberMe)
                    {
                        await _credentialService.SaveCredentialsAsync(Username, Password);
                    }
                    else if (!RememberMe && !_isAutoLogin)
                    {
                        // Если пользователь не хочет сохранять данные и это не автоматический вход,
                        // то удаляем сохраненные ранее данные
                        _credentialService.ClearCredentials();
                    }
                    // Запускаем сервис уведомлений после успешной авторизации
                    var notificationService = NotificationService.Instance;
                    notificationService.Initialize(_dbContext, _mainWindowViewModel).ConfigureAwait(false);
                    notificationService.Start();

                    // Переходим на дашборд
                    _mainWindowViewModel.NavigateToDashboard();

                    _mainWindowViewModel.NavigateToDashboard();
                }
                else
                {
                    ErrorMessage = "Неверное имя пользователя или пароль";
                    if (_isAutoLogin)
                    {
                        // Если это автоматический вход и данные не верны, удаляем сохраненные данные
                        _credentialService.ClearCredentials();
                        _isAutoLogin = false;
                    }
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Ошибка при входе: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }
    }
}