using Avalonia.Controls;
using Avalonia.Threading;
using ManufactPlanner.Models;
using ManufactPlanner.Services;
using ManufactPlanner.Views;
using Microsoft.EntityFrameworkCore;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows.Input;

namespace ManufactPlanner.ViewModels
{
    public class MainWindowViewModel : ViewModelBase
    {
        private UserControl _currentView;
        private PostgresContext _dbContext;
        private bool _isSidebarCollapsed;
        private string _currentUserName;
        private Guid _currentUserId;
        
        private int _unreadNotificationsCount;

        private bool _isAuthenticated = false;


        // Текущее отображаемое представление
        public UserControl CurrentView
        {
            get => _currentView;
            set
            {
                Console.WriteLine($"CurrentView изменено на: {value?.GetType().Name}");
                this.RaiseAndSetIfChanged(ref _currentView, value);
            }
        }

        // Контекст базы данных PostgreSQL
        public PostgresContext DbContext
        {
            get => _dbContext;
            set => this.RaiseAndSetIfChanged(ref _dbContext, value);
        }


        private readonly RoleService _roleService = RoleService.Instance;
        private List<string> _userRoles = new List<string>();
        public List<string> UserRoles
        {
            get => _userRoles;
            set
            {
                this.RaiseAndSetIfChanged(ref _userRoles, value);
                this.RaisePropertyChanged(nameof(IsAdministrator));
                this.RaisePropertyChanged(nameof(IsManager));
                this.RaisePropertyChanged(nameof(IsExecutor));
                this.RaisePropertyChanged(nameof(IsAdministratorOrManager));
            }
        }

        // Свойства для быстрой проверки роли
        public bool IsAdministrator => _userRoles.Contains(RoleService.ROLE_ADMINISTRATOR);
        public bool IsManager => _userRoles.Contains(RoleService.ROLE_MANAGER);
        public bool IsExecutor => _userRoles.Contains(RoleService.ROLE_EXECUTOR);

        

        // Возможность свернуть боковую панель
        public bool IsSidebarCollapsed
        {
            get => _isSidebarCollapsed;
            set => this.RaiseAndSetIfChanged(ref _isSidebarCollapsed, value);
        }

        // Имя текущего пользователя для отображения в шапке
        public string CurrentUserName
        {
            get => _currentUserName;
            set => this.RaiseAndSetIfChanged(ref _currentUserName, value);
        }
        // Id текущего пользователя для отображения в шапке
        public Guid CurrentUserId
        {
            get => _currentUserId;
            set => this.RaiseAndSetIfChanged(ref _currentUserId, value);
        }

        // Количество непрочитанных уведомлений
        public int UnreadNotificationsCount
        {
            get => _unreadNotificationsCount;
            set => this.RaiseAndSetIfChanged(ref _unreadNotificationsCount, value);
        }

        // Текущий выбранный пункт меню (для подсветки в навигации)
        private string _currentMenuItem = "dashboard";
        public string CurrentMenuItem
        {
            get => _currentMenuItem;
            set => this.RaiseAndSetIfChanged(ref _currentMenuItem, value);
        }
        private string _Inicial;
        public string Inicial
        {
            get => _Inicial;
            set => this.RaiseAndSetIfChanged(ref _Inicial, value);
        }
        
        public bool IsAuthenticated
        {
            get => _isAuthenticated;
            set 
            { 
                Console.WriteLine($"IsAuthenticated изменено на: {value}");
                this.RaiseAndSetIfChanged(ref _isAuthenticated, value);
            }
        }

        private readonly ThemeService _themeService;

        private readonly NotificationService _notificationService;

        public ICommand ToggleNotificationsPanel { get; }

        public Window MainWindow { get; set; }


        
        private bool _notifyDesktopEnabled = true;

        public bool NotifyDesktopEnabled
        {
            get => _notifyDesktopEnabled;
            set => this.RaiseAndSetIfChanged(ref _notifyDesktopEnabled, value);
        }
        private bool _notifyEmailEnabled;
        public bool NotifyEmailEnabled
        {
            get => _notifyEmailEnabled;
            set => this.RaiseAndSetIfChanged(ref _notifyEmailEnabled, value);
        }
        public bool IsAdministratorOrManager => IsAdministrator || IsManager;
        private void NotifyRolePropertiesChanged()
        {
            this.RaisePropertyChanged(nameof(IsAdministrator));
            this.RaisePropertyChanged(nameof(IsManager));
            this.RaisePropertyChanged(nameof(IsExecutor));
            this.RaisePropertyChanged(nameof(IsAdministratorOrManager));
        }

        public MainWindowViewModel _mainViewModel;
        public MainWindowViewModel()
        {
            _themeService = ThemeService.Instance;
            _notificationService = NotificationService.Instance;
            // Подписка на изменение темы
            _themeService.ThemeChanged.Subscribe(isLight =>
            {
                // При необходимости можно обновить UI при изменении темы
            });
            _mainViewModel = this;
            // Инициализация базы данных
            DbContext = new PostgresContext();

            // Начинаем с неаутентифицированного состояния
            IsAuthenticated = false;

            // Заглушка данных пользователя (будет видна только после авторизации)
            CurrentUserName = string.Empty;
            UnreadNotificationsCount = 0;


            // Команда для переключения видимости панели уведомлений
            ToggleNotificationsPanel = ReactiveCommand.Create(() =>
            {
                // Вместо переключения панели переходим на страницу уведомлений
                NavigateToNotifications();
            });
        }
        public async System.Threading.Tasks.Task LoadAndApplyUserSettingsAsync(Guid userId)
        {
            if (userId == Guid.Empty || DbContext == null)
                return;

            try
            {
                // Загружаем настройки пользователя из базы данных
                var settings = await DbContext.UserSettings
                    .FirstOrDefaultAsync(s => s.UserId == userId);

                if (settings != null)
                {
                    // Применяем настройки темы
                    _themeService.IsLightTheme = settings.Islighttheme ?? true;

                    // Кэшируем настройки уведомлений
                    NotifyDesktopEnabled = settings.NotifyDesktop ?? true;
                    NotifyEmailEnabled = settings.NotifyEmail ?? false;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Ошибка при загрузке настроек пользователя: {ex.Message}");
            }
        }
        public async System.Threading.Tasks.Task CacheUserNotificationSettingsAsync(Guid userId)
        {
            try
            {
                if (_dbContext == null || userId == Guid.Empty || _mainViewModel == null)
                    return;

                var settings = await _dbContext.UserSettings
                    .FirstOrDefaultAsync(s => s.UserId == userId);

                // Кэшируем настройки в MainViewModel
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    _mainViewModel.NotifyDesktopEnabled = settings?.NotifyDesktop ?? true;
                    _mainViewModel.NotifyEmailEnabled = settings?.NotifyEmail ?? false;
                });
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Ошибка при кэшировании настроек уведомлений: {ex.Message}");
            }
        }
        public void NavigateToNotifications()
        {
            CurrentMenuItem = "notifications"; // Можно использовать существующий пункт меню или создать новый
            CurrentView = new Views.NotificationsPage(this, DbContext);
        }
        // Метод для доступа к сервису тем из ViewModel
        public void ToggleTheme()
        {
            _themeService.IsLightTheme = !_themeService.IsLightTheme;
        }
        // Модифицируем метод NavigateToDashboard для инициализации NotificationService
        public void NavigateToDashboard()
        {
            CurrentMenuItem = "dashboard";
            var dashboardPage = new Views.DashboardPage(this, DbContext);
            Console.WriteLine($"Создана страница дашборда: {dashboardPage != null}");
            CurrentView = dashboardPage;
            IsAuthenticated = true;

            // Инициализируем и запускаем сервис уведомлений после успешной авторизации
            if (CurrentUserId != Guid.Empty)
            {
                _notificationService.Initialize(DbContext, this).ConfigureAwait(false);
                _notificationService.Start();

                // Загрузка и применение настроек пользователя уже произошли в AuthViewModel
            }
        }

        public void NavigateToOrders()
        {
            CurrentMenuItem = "orders";
            CurrentView = new Views.OrdersPage(this, DbContext);
        }

        public void NavigateToTasks()
        {
            CurrentMenuItem = "tasks";
            CurrentView = new Views.TasksPage(this, DbContext);
        }

        public void NavigateToCalendar()
        {
            CurrentMenuItem = "calendar";
            //CurrentView = new Views.Tasks.CalendarPage(this, DbContext);
            CurrentView = new Views.CalendarPage(this, DbContext);
        }

        public void NavigateToDocumentation()
        {
            CurrentMenuItem = "documentation";
            //CurrentView = new Views.Documentation.DocumentationPage(this, DbContext);
            CurrentView = new Views.DocumentationPage(this, DbContext);
        }

        public void NavigateToAnalytics()
        {
            if (!IsAdministratorOrManager)
            {
                // Можно добавить уведомление о недостаточных правах
                return;
            }

            CurrentMenuItem = "analytics";
            CurrentView = new Views.AnalyticsPage(this, DbContext);
        }

        public void NavigateToProduction()
        {
            if (!IsAdministratorOrManager)
            {
                // Можно добавить уведомление о недостаточных правах
                return;
            }

            CurrentMenuItem = "production";
            CurrentView = new Views.ProductionPage(this, DbContext);
        }

        public void NavigateToSettings()
        {
            CurrentMenuItem = "settings";
            //CurrentView = new Views.Settings.SettingsPage(this, DbContext);
            CurrentView = new Views.SettingsPage(this, DbContext);
        }

        // Метод для отображения подробной информации о задаче
        public void NavigateToTaskDetails(int taskId)
        {
            CurrentMenuItem = "tasks"; // Устанавливаем текущий пункт меню как "tasks"
            CurrentView = new TaskDetailsPage(this, DbContext, taskId);
        }

        // Метод для отображения подробной информации о заказе
        public void NavigateToOrderDetails(int orderId)
        {
            CurrentMenuItem = "orders";
            //CurrentView = new Views.Orders.OrderDetailsPage(this, DbContext, orderId);
            CurrentView = new Views.OrderDetailsPage(this, DbContext, orderId);
        }

        // Метод для переключения состояния боковой панели
        public void ToggleSidebar()
        {
            IsSidebarCollapsed = !IsSidebarCollapsed;
        }

        public void NavigateToUserProfile()
        {
            CurrentMenuItem = "profile";  // Можно добавить новый пункт меню или использовать существующий
            CurrentView = new Views.UsersPage(this, DbContext);
        }

        // Модифицируем метод Logout для очистки сохраненных данных
        public void Logout()
        {
            // Останавливаем сервис уведомлений
            _notificationService.Stop();

            // Очищаем данные пользователя
            CurrentUserName = string.Empty;
            UnreadNotificationsCount = 0;
            CurrentUserId = Guid.Empty;
            IsAuthenticated = false; // Сбрасываем флаг авторизации
            Inicial = string.Empty; // Добавьте эту строку
            // Удаляем сохраненные учетные данные
            var credentialService = new UserCredentialService();
            credentialService.ClearCredentials();
            UserRoles.Clear();
            _roleService.ClearCache(CurrentUserId);

            // Переходим обратно на страницу авторизации
            CurrentView = new AuthPage(this, DbContext);
        }
        public void NextProfil()
        {
            
            // Переходим на страницу профиля
            CurrentView = new UsersPage(this, DbContext);
        }
    }
}
