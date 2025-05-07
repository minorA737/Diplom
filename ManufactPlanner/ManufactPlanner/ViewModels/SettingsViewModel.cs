// ViewModels/SettingsViewModel.cs
using ManufactPlanner.Models;
using ManufactPlanner.Services;
using ReactiveUI;
using System;
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
        private readonly LocalizationService _localizationService;

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

        // Локализированные строки
        public string ProfileSettingsText => _localizationService.GetString("ProfileSettings");
        public string UsernameText => _localizationService.GetString("Username");
        public string FirstNameText => _localizationService.GetString("FirstName");
        public string LastNameText => _localizationService.GetString("LastName");
        public string EmailText => _localizationService.GetString("Email");
        public string SaveChangesText => _localizationService.GetString("SaveChanges");

        public string PasswordSettingsText => _localizationService.GetString("PasswordSettings");
        public string CurrentPasswordText => _localizationService.GetString("CurrentPassword");
        public string NewPasswordText => _localizationService.GetString("NewPassword");
        public string ConfirmPasswordText => _localizationService.GetString("ConfirmPassword");
        public string ChangePasswordText => _localizationService.GetString("ChangePassword");

        public string NotificationSettingsText => _localizationService.GetString("NotificationSettings");
        public string NotifyNewTasksText => _localizationService.GetString("NotifyNewTasks");
        public string NotifyStatusChangesText => _localizationService.GetString("NotifyStatusChanges");
        public string NotifyCommentsText => _localizationService.GetString("NotifyComments");
        public string NotifyDeadlinesText => _localizationService.GetString("NotifyDeadlines");
        public string NotifyEmailText => _localizationService.GetString("NotifyEmail");
        public string NotifyDesktopText => _localizationService.GetString("NotifyDesktop");
        public string SaveNotificationSettingsText => _localizationService.GetString("SaveNotificationSettings");

        public string InterfaceSettingsText => _localizationService.GetString("InterfaceSettings");
        public string LanguageText => _localizationService.GetString("Language");
        public string ThemeText => _localizationService.GetString("Theme");
        public string LightThemeText => _localizationService.GetString("LightTheme");
        public string DarkThemeText => _localizationService.GetString("DarkTheme");
        public string ApplyText => _localizationService.GetString("Apply");

        public string AboutAppText => _localizationService.GetString("AboutApp");
        public string AppNameText => _localizationService.GetString("AppName");
        public string VersionText => _localizationService.GetString("Version");
        public string CopyrightText => _localizationService.GetString("Copyright");
        public string DevelopedAsText => _localizationService.GetString("DevelopedAs");
        public string TechnologiesText => _localizationService.GetString("Technologies");

        // Команды
        public ReactiveCommand<Unit, bool> SaveProfileCommand { get; }
        public ReactiveCommand<Unit, bool> ChangePasswordCommand { get; }
        public ReactiveCommand<Unit, Unit> SaveNotificationSettingsCommand { get; }
        public ReactiveCommand<Unit, Unit> ApplyInterfaceSettingsCommand { get; }

        public SettingsViewModel(MainWindowViewModel mainWindowViewModel, PostgresContext dbContext)
        {
            _mainWindowViewModel = mainWindowViewModel;
            _dbContext = dbContext;
            _userSettingsService = new UserSettingsService(dbContext);
            _localizationService = LocalizationService.Instance;

            // Подписываемся на изменение языка
            _localizationService.TranslationsChanged += (sender, args) => UpdateLocalizedProperties();

            // Инициализируем состояние UI из сервисов
            //IsLightTheme = _themeService.IsLightTheme;
            SelectedLanguage = _localizationService.CurrentLanguage == "ru" ? 0 : 1;

            SaveProfileCommand = ReactiveCommand.CreateFromTask(SaveProfile);
            ChangePasswordCommand = ReactiveCommand.CreateFromTask(ChangePassword);
            SaveNotificationSettingsCommand = ReactiveCommand.Create(SaveNotificationSettings);
            ApplyInterfaceSettingsCommand = ReactiveCommand.Create(ApplyInterfaceSettings);

            LoadUserSettings();
        }

        public SettingsViewModel()
        {
            // Конструктор для дизайнера
            _localizationService = LocalizationService.Instance;

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
                StatusMessage = _localizationService.GetString("ProfileSavedError");
                return false;
            }

            var user = await _userSettingsService.GetUserProfileAsync(_mainWindowViewModel.CurrentUserId);
            if (user == null)
            {
                IsStatusSuccess = false;
                StatusMessage = _localizationService.GetString("ProfileSavedError");
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
                StatusMessage = _localizationService.GetString("ProfileSavedSuccess");
                return true;
            }
            else
            {
                IsStatusSuccess = false;
                StatusMessage = _localizationService.GetString("ProfileSavedError");
                return false;
            }
        }

        private async Task<bool> ChangePassword()
        {
            // Проверка соответствия нового пароля и подтверждения
            if (NewPassword != ConfirmPassword)
            {
                IsStatusSuccess = false;
                StatusMessage = _localizationService.GetString("PasswordMismatch");
                return false;
            }

            if (_mainWindowViewModel == null || _mainWindowViewModel.CurrentUserId == Guid.Empty)
            {
                IsStatusSuccess = false;
                StatusMessage = _localizationService.GetString("PasswordChangedError");
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
                StatusMessage = _localizationService.GetString("PasswordChangedSuccess");
                return true;
            }
            else
            {
                IsStatusSuccess = false;
                StatusMessage = _localizationService.GetString("PasswordChangedError");
                return false;
            }
        }

        private void SaveNotificationSettings()
        {
            // Логика сохранения настроек уведомлений
            // Будет реализована в следующем этапе
        }
        

        private void ApplyInterfaceSettings()
        {
            // Применяем настройки темы
            ThemeService.Instance.IsLightTheme = IsLightTheme;

            // Применяем настройки языка
            var language = SelectedLanguage == 0 ? "ru" : "en";
            _localizationService.SetLanguage(language);
        }

        private void UpdateLocalizedProperties()
        {
            // Вызываем RaisePropertyChanged для всех локализованных свойств
            this.RaisePropertyChanged(nameof(ProfileSettingsText));
            this.RaisePropertyChanged(nameof(UsernameText));
            this.RaisePropertyChanged(nameof(FirstNameText));
            this.RaisePropertyChanged(nameof(LastNameText));
            this.RaisePropertyChanged(nameof(EmailText));
            this.RaisePropertyChanged(nameof(SaveChangesText));

            this.RaisePropertyChanged(nameof(PasswordSettingsText));
            this.RaisePropertyChanged(nameof(CurrentPasswordText));
            this.RaisePropertyChanged(nameof(NewPasswordText));
            this.RaisePropertyChanged(nameof(ConfirmPasswordText));
            this.RaisePropertyChanged(nameof(ChangePasswordText));

            this.RaisePropertyChanged(nameof(NotificationSettingsText));
            this.RaisePropertyChanged(nameof(NotifyNewTasksText));
            this.RaisePropertyChanged(nameof(NotifyStatusChangesText));
            this.RaisePropertyChanged(nameof(NotifyCommentsText));
            this.RaisePropertyChanged(nameof(NotifyDeadlinesText));
            this.RaisePropertyChanged(nameof(NotifyEmailText));
            this.RaisePropertyChanged(nameof(NotifyDesktopText));
            this.RaisePropertyChanged(nameof(SaveNotificationSettingsText));

            this.RaisePropertyChanged(nameof(InterfaceSettingsText));
            this.RaisePropertyChanged(nameof(LanguageText));
            this.RaisePropertyChanged(nameof(ThemeText));
            this.RaisePropertyChanged(nameof(LightThemeText));
            this.RaisePropertyChanged(nameof(DarkThemeText));
            this.RaisePropertyChanged(nameof(ApplyText));

            this.RaisePropertyChanged(nameof(AboutAppText));
            this.RaisePropertyChanged(nameof(AppNameText));
            this.RaisePropertyChanged(nameof(VersionText));
            this.RaisePropertyChanged(nameof(CopyrightText));
            this.RaisePropertyChanged(nameof(DevelopedAsText));
            this.RaisePropertyChanged(nameof(TechnologiesText));
        }
    }
}