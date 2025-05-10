using System;
using System.Linq;
using System.Reactive;
using Avalonia.Controls;
using ReactiveUI;
using ManufactPlanner.Models;
using ManufactPlanner.Services;
using System.Threading.Tasks;

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
            LoadSavedCredentialsAsync();
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