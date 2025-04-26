using ManufactPlanner.Models;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace ManufactPlanner.ViewModels
{
    public class SettingsViewModel : ViewModelBase
    {
        private readonly MainWindowViewModel _mainWindowViewModel;
        private readonly PostgresContext _dbContext;

        // Профиль пользователя
        private string _username = "admin";
        private string _firstName = "Администратор";
        private string _lastName = "Системы";
        private string _email = "admin@example.com";

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

        // Команды
        public ICommand SaveProfileCommand { get; }
        public ICommand ChangePasswordCommand { get; }
        public ICommand SaveNotificationSettingsCommand { get; }
        public ICommand ApplyInterfaceSettingsCommand { get; }

        public SettingsViewModel(MainWindowViewModel mainWindowViewModel, PostgresContext dbContext)
        {
            _mainWindowViewModel = mainWindowViewModel;
            _dbContext = dbContext;

            SaveProfileCommand = ReactiveCommand.Create(SaveProfile);
            ChangePasswordCommand = ReactiveCommand.Create(ChangePassword);
            SaveNotificationSettingsCommand = ReactiveCommand.Create(SaveNotificationSettings);
            ApplyInterfaceSettingsCommand = ReactiveCommand.Create(ApplyInterfaceSettings);

            LoadUserSettings();
        }

        public SettingsViewModel()
        {
            // Конструктор для дизайнера
            SaveProfileCommand = ReactiveCommand.Create(SaveProfile);
            ChangePasswordCommand = ReactiveCommand.Create(ChangePassword);
            SaveNotificationSettingsCommand = ReactiveCommand.Create(SaveNotificationSettings);
            ApplyInterfaceSettingsCommand = ReactiveCommand.Create(ApplyInterfaceSettings);
        }

        private void LoadUserSettings()
        {
            // В реальном приложении здесь будет загрузка настроек пользователя из БД или файла конфигурации
        }

        private void SaveProfile()
        {
            // Логика сохранения профиля пользователя
        }

        private void ChangePassword()
        {
            // Логика изменения пароля
            // Проверка соответствия нового пароля и подтверждения
            if (NewPassword != ConfirmPassword)
            {
                // Отобразить ошибку
                return;
            }

            // Проверка текущего пароля и сохранение нового
        }

        private void SaveNotificationSettings()
        {
            // Логика сохранения настроек уведомлений
        }

        private void ApplyInterfaceSettings()
        {
            // Логика применения настроек интерфейса
        }
    }
}