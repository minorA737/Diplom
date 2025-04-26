using System;
using System.Linq;
using System.Reactive;
using Avalonia.Controls;
using ReactiveUI;
using ManufactPlanner.Models;

namespace ManufactPlanner.ViewModels
{
    public class AuthViewModel : ViewModelBase
    {
        private readonly MainWindowViewModel _mainWindowViewModel;
        private readonly PostgresContext _dbContext;
        private string _username = string.Empty;
        private string _password = string.Empty;
        private bool _rememberMe = false;
        private string _errorMessage = string.Empty;
        private bool _hasError = false;
        private bool _isLoading = false;

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
            LoginCommand = ReactiveCommand.Create(Login);
        }

        private void Login()
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
                        return;
                    }

                    // Обновляем дату последнего входа
                    user.LastLogin = DateTime.Now;
                    _dbContext.SaveChanges();

                    // Устанавливаем имя пользователя в главном ViewModel
                    _mainWindowViewModel.CurrentUserName = $"{user.FirstName} {user.LastName}";

                    // Загружаем количество непрочитанных уведомлений
                    var unreadNotifications = _dbContext.Notifications
                        .Count(n => n.UserId == user.Id && n.IsRead != true); // изменено тут

                    _mainWindowViewModel.UnreadNotificationsCount = unreadNotifications;

                    // Переходим на дашборд
                    _mainWindowViewModel.NavigateToDashboard();
                }
                else
                {
                    ErrorMessage = "Неверное имя пользователя или пароль";
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