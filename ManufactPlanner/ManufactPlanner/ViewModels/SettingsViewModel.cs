// ViewModels/SettingsViewModel.cs
using ManufactPlanner.Models;
using ManufactPlanner.Services;
using Microsoft.EntityFrameworkCore;
using ReactiveUI;
using System;
using System.Diagnostics;
using System.Reactive;
using System.Threading.Tasks;
using System.Windows.Input;

namespace ManufactPlanner.ViewModels
{
    public class SettingsViewModel : ViewModelBase
    {
        private readonly MainWindowViewModel _mainWindowViewModel;
        private readonly PostgresContext _dbContext;
        private readonly UserSettingsService _userSettingsService;
        private readonly ThemeService _themeService;

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

        // Сообщения
        private string _statusMessage = "";
        private bool _hasStatusMessage = false;
        private bool _isStatusSuccess = true;

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


        private bool _autoStartEnabled;

        public bool AutoStartEnabled
        {
            get => _autoStartEnabled;
            set => this.RaiseAndSetIfChanged(ref _autoStartEnabled, value);
        }
        // В начале класса добавьте новую команду:
        public ReactiveCommand<Unit, Unit> SendTestEmailCommand { get; }
        public ReactiveCommand<Unit, Unit> ApplyAutoStartCommand { get; }

        public SettingsViewModel(MainWindowViewModel mainWindowViewModel, PostgresContext dbContext)
        {
            _mainWindowViewModel = mainWindowViewModel;
            _dbContext = dbContext;
            _userSettingsService = new UserSettingsService(dbContext);
            

            // Инициализируем состояние UI из сервисов
            ApplyAutoStartCommand = ReactiveCommand.Create(ApplyAutoStartSettings);

            SaveProfileCommand = ReactiveCommand.CreateFromTask(SaveProfile);
            ChangePasswordCommand = ReactiveCommand.CreateFromTask(ChangePassword);
            SaveNotificationSettingsCommand = ReactiveCommand.Create(SaveNotificationSettings);
            ApplyInterfaceSettingsCommand = ReactiveCommand.Create(ApplyInterfaceSettings);
            AutoStartEnabled = AutoStartService.IsAutoStartEnabled();
            SendTestEmailCommand = ReactiveCommand.CreateFromTask(SendTestEmail);
            LoadUserSettings();

            SaveNotificationSettingsCommand = ReactiveCommand.Create(SaveNotificationSettings);
            ApplyInterfaceSettingsCommand = ReactiveCommand.Create(ApplyInterfaceSettings);

            // Загрузка настроек пользователя
            _ = LoadUserSettingsAsync();

        }
        // Добавьте метод для тестирования email:
        private async System.Threading.Tasks.Task SendTestEmail()
        {
            if (string.IsNullOrWhiteSpace(Email) || !EmailService.Instance.IsValidEmail(Email))
            {
                StatusMessage = "Пожалуйста, укажите корректный email в профиле";
                IsStatusSuccess = false;
                HasStatusMessage = true;
                return;
            }

            IsStatusSuccess = true;
            StatusMessage = "Отправка тестового письма...";
            HasStatusMessage = true;

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

            HasStatusMessage = true;
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
                HasStatusMessage = true;
            }
            catch (Exception ex)
            {
                StatusMessage = $"Ошибка при настройке автозапуска: {ex.Message}";
                IsStatusSuccess = false;
                HasStatusMessage = true;
            }
        }
        public SettingsViewModel()
        {
            // Конструктор для дизайнера
            

            SaveProfileCommand = ReactiveCommand.CreateFromTask(SaveProfile);
            ChangePasswordCommand = ReactiveCommand.CreateFromTask(ChangePassword);
            SaveNotificationSettingsCommand = ReactiveCommand.Create(SaveNotificationSettings);
            ApplyInterfaceSettingsCommand = ReactiveCommand.Create(ApplyInterfaceSettings);
        }

        private void LoadUserSettings()
        {
            if (_mainWindowViewModel != null && _mainWindowViewModel.CurrentUserId != Guid.Empty)
            {
                System.Threading.Tasks.Task.Run(async () =>
                {
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
                });
            }
        }

        private async Task<bool> SaveProfile()
        {
            if (_mainWindowViewModel == null || _mainWindowViewModel.CurrentUserId == Guid.Empty)
            {
                IsStatusSuccess = false;
               
                return false;
            }

            var user = await _userSettingsService.GetUserProfileAsync(_mainWindowViewModel.CurrentUserId);
            if (user == null)
            {
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

                IsStatusSuccess = true;
                return true;
            }
            else
            {
                IsStatusSuccess = false;
                return false;
            }
        }

        private async Task<bool> ChangePassword()
        {
            // Проверка соответствия нового пароля и подтверждения
            if (NewPassword != ConfirmPassword)
            {
                IsStatusSuccess = false;
                return false;
            }

            if (_mainWindowViewModel == null || _mainWindowViewModel.CurrentUserId == Guid.Empty)
            {
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

                IsStatusSuccess = true;
                return true;
            }
            else
            {
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
                    _mainWindowViewModel.NotifyDesktopEnabled = settings.NotifyDesktop ?? true;
                    // Автозапуск
                    AutoStartEnabled = settings.AutoStartEnabled ?? false;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Ошибка при загрузке настроек: {ex.Message}");
                StatusMessage = "Ошибка при загрузке настроек";
                IsStatusSuccess = false;
                HasStatusMessage = true;
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
                    settings = new UserSetting // Обратите внимание на использование UserSetting, а не UserSettings
                    {
                        UserId = _mainWindowViewModel.CurrentUserId
                    };
                    _dbContext.UserSettings.Add(settings);
                }

                // Обновляем настройки
                settings.NotifyNewTasks = NotifyNewTasks;
                settings.NotifyStatusChanges = NotifyTaskStatusChanges; // Изменено имя поля
                settings.NotifyComments = NotifyComments;
                settings.NotifyDeadlines = NotifyDeadlines;
                settings.NotifyEmail = NotifyEmail;
                settings.NotifyDesktop = NotifyDesktop;
                settings.AutoStartEnabled = AutoStartEnabled;
                settings.UpdatedAt = DateTime.Now;

                await _dbContext.SaveChangesAsync();

                StatusMessage = "Настройки успешно сохранены";
                IsStatusSuccess = true;
                HasStatusMessage = true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Ошибка при сохранении настроек: {ex.Message}");
                StatusMessage = $"Ошибка при сохранении настроек: {ex.Message}";
                IsStatusSuccess = false;
                HasStatusMessage = true;
            }
        }
        // Модифицируйте метод SaveNotificationSettings:
        private async void SaveNotificationSettings()
        {
            // Проверяем валидность настроек email
            if (!ValidateEmailSettings())
                return;

            await SaveUserSettingsAsync();

            // Обновляем настройку в MainViewModel
            if (_mainWindowViewModel != null)
            {
                _mainWindowViewModel.NotifyDesktopEnabled = NotifyDesktop;
                _mainWindowViewModel.NotifyEmailEnabled = NotifyEmail;
            }

            // Показываем сообщение об успехе
            IsStatusSuccess = true;
            StatusMessage = "Настройки уведомлений сохранены";
            HasStatusMessage = true;
        }

        private bool ValidateEmailSettings()
        {
            if (NotifyEmail && string.IsNullOrWhiteSpace(Email))
            {
                IsStatusSuccess = false;
                StatusMessage = "Для включения email-уведомлений необходимо указать адрес электронной почты";
                HasStatusMessage = true;
                NotifyEmail = false; // Отключаем настройку
                return false;
            }

            if (NotifyEmail && !EmailService.Instance.IsValidEmail(Email))
            {
                IsStatusSuccess = false;
                StatusMessage = "Указан некорректный формат email-адреса";
                HasStatusMessage = true;
                NotifyEmail = false; // Отключаем настройку
                return false;
            }

            return true;
        }
        private void ApplyInterfaceSettings()
        {
            // Применяем настройки автозапуска
            AutoStartService.SetAutoStart(AutoStartEnabled);

            // Применяем другие настройки интерфейса
            ThemeService.Instance.IsLightTheme = IsLightTheme;


            // Показываем сообщение об успешном применении
            StatusMessage = "Настройки интерфейса успешно применены";
            IsStatusSuccess = true;
            HasStatusMessage = true;
        }

       
    }
}