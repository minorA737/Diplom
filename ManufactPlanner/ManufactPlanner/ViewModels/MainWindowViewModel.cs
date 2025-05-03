using Avalonia.Controls;
using ManufactPlanner.Models;
using ManufactPlanner.Services;
using ManufactPlanner.Views;
using ReactiveUI;
using System;

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

        public bool IsAuthenticated
        {
            get => _isAuthenticated;
            set 
            { 
                Console.WriteLine($"IsAuthenticated изменено на: {value}");
                this.RaiseAndSetIfChanged(ref _isAuthenticated, value);
            }
        }
        public MainWindowViewModel()
        {
            // Инициализация базы данных
            DbContext = new PostgresContext();

            // Начинаем с неаутентифицированного состояния
            IsAuthenticated = false;

            // Заглушка данных пользователя (будет видна только после авторизации)
            CurrentUserName = string.Empty;
            UnreadNotificationsCount = 0;
        }

        // Навигационные методы для различных страниц
        public void NavigateToDashboard()
        {
            CurrentMenuItem = "dashboard";
            var dashboardPage = new Views.DashboardPage(this, DbContext);
            Console.WriteLine($"Создана страница дашборда: {dashboardPage != null}");
            CurrentView = dashboardPage;
            IsAuthenticated = true;
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

        public void NavigateToProduction()
        {
            CurrentMenuItem = "production";
            //CurrentView = new Views.Production.ProductionPage(this, DbContext);
            CurrentView = new Views.ProductionPage(this, DbContext);
        }

        public void NavigateToAnalytics()
        {
            CurrentMenuItem = "analytics";
            //CurrentView = new Views.Analytics.AnalyticsPage(this, DbContext);
            CurrentView = new Views.AnalyticsPage(this, DbContext);
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
            // Очищаем данные пользователя
            CurrentUserName = string.Empty;
            UnreadNotificationsCount = 0;
            CurrentUserId = Guid.Empty;
            IsAuthenticated = false; // Сбрасываем флаг авторизации

            // Удаляем сохраненные учетные данные
            var credentialService = new UserCredentialService();
            credentialService.ClearCredentials();

            // Переходим обратно на страницу авторизации
            CurrentView = new AuthPage(this, DbContext);
        }
    }
}
