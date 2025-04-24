using Avalonia.Controls;
using ManufactPlanner.Models;
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
        private int _unreadNotificationsCount;

        // Текущее отображаемое представление
        public UserControl CurrentView
        {
            get => _currentView;
            set => this.RaiseAndSetIfChanged(ref _currentView, value);
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

        public MainWindowViewModel()
        {
            // Инициализация базы данных
            DbContext = new PostgresContext();

            // По умолчанию отображаем страницу авторизации
            //CurrentView = new Views.Auth.AuthPage(this, DbContext);

            // Заглушка данных пользователя (в реальном приложении будет загружаться после авторизации)
            CurrentUserName = "Администратор";
            UnreadNotificationsCount = 3;
        }

        // Навигационные методы для различных страниц
        public void NavigateToDashboard()
        {
            CurrentMenuItem = "dashboard";
            //CurrentView = new Views.Dashboard.DashboardPage(this, DbContext);
        }

        public void NavigateToOrders()
        {
            CurrentMenuItem = "orders";
            //CurrentView = new Views.Orders.OrdersPage(this, DbContext);
        }

        public void NavigateToTasks()
        {
            CurrentMenuItem = "tasks";
            //CurrentView = new Views.Tasks.TasksPage(this, DbContext);
        }

        public void NavigateToCalendar()
        {
            CurrentMenuItem = "calendar";
            //CurrentView = new Views.Tasks.CalendarPage(this, DbContext);
        }

        public void NavigateToDocumentation()
        {
            CurrentMenuItem = "documentation";
            //CurrentView = new Views.Documentation.DocumentationPage(this, DbContext);
        }

        public void NavigateToProduction()
        {
            CurrentMenuItem = "production";
            //CurrentView = new Views.Production.ProductionPage(this, DbContext);
        }

        public void NavigateToAnalytics()
        {
            CurrentMenuItem = "analytics";
            //CurrentView = new Views.Analytics.AnalyticsPage(this, DbContext);
        }

        public void NavigateToSettings()
        {
            CurrentMenuItem = "settings";
            //CurrentView = new Views.Settings.SettingsPage(this, DbContext);
        }

        // Метод для отображения подробной информации о задаче
        public void NavigateToTaskDetails(int taskId)
        {
            CurrentMenuItem = "tasks";
            //CurrentView = new Views.Tasks.TaskDetailsPage(this, DbContext, taskId);
        }

        // Метод для отображения подробной информации о заказе
        public void NavigateToOrderDetails(int orderId)
        {
            CurrentMenuItem = "orders";
            //CurrentView = new Views.Orders.OrderDetailsPage(this, DbContext, orderId);
        }

        // Метод для переключения состояния боковой панели
        public void ToggleSidebar()
        {
            IsSidebarCollapsed = !IsSidebarCollapsed;
        }
    }
}
