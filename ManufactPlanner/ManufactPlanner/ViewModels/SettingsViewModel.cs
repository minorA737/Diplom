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

            // Очищаем сообщение о статусе при инициализации
            HasStatusMessage = false;
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