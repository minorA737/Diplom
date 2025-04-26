using Avalonia.Controls;
using ManufactPlanner.Models;
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

            // Во время разработки начнем с дашборда для удобства
            NavigateToDashboard();

            // Заглушка данных пользователя
            CurrentUserName = "Администратор";
            UnreadNotificationsCount = 3;
        }

        // Навигационные методы для различных страниц
        public void NavigateToDashboard()
        {
            CurrentMenuItem = "dashboard";
            CurrentView = new Views.DashboardPage(this, DbContext);
        }

        public void NavigateToOrders()
        {
            CurrentMenuItem = "orders";
            //CurrentView = new Views.Orders.OrdersPage(this, DbContext);
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
            CurrentView = new Views.CalendarPage();
        }

        public void NavigateToDocumentation()
        {
            CurrentMenuItem = "documentation";
            //CurrentView = new Views.Documentation.DocumentationPage(this, DbContext);
            CurrentView = new Views.DocumentationPage();
        }

        public void NavigateToProduction()
        {
            CurrentMenuItem = "production";
            //CurrentView = new Views.Production.ProductionPage(this, DbContext);
            CurrentView = new Views.ProductionPage();
        }

        public void NavigateToAnalytics()
        {
            CurrentMenuItem = "analytics";
            //CurrentView = new Views.Analytics.AnalyticsPage(this, DbContext);
            CurrentView = new Views.AnalyticsPage();
        }

        public void NavigateToSettings()
        {
            CurrentMenuItem = "settings";
            //CurrentView = new Views.Settings.SettingsPage(this, DbContext);
            CurrentView = new Views.SettingsPage();
        }

        // Метод для отображения подробной информации о задаче
        public void NavigateToTaskDetails(int taskId)
        {
            CurrentMenuItem = "tasks";
            CurrentView = new Views.TaskDetailsPage(this, DbContext, taskId);
        }

        // Метод для отображения подробной информации о заказе
        public void NavigateToOrderDetails(int orderId)
        {
            CurrentMenuItem = "orders";
            //CurrentView = new Views.Orders.OrderDetailsPage(this, DbContext, orderId);
            CurrentView = new Views.OrderDetailsPage();
        }

        // Метод для переключения состояния боковой панели
        public void ToggleSidebar()
        {
            IsSidebarCollapsed = !IsSidebarCollapsed;
        }

        public void Logout()
        {
            //// Очищаем данные пользователя
            //CurrentUserName = string.Empty;
            //UnreadNotificationsCount = 0;

            //// Создаем новую страницу авторизации
            //var authViewModel = new AuthViewModel(this, DbContext);
            //CurrentView = new AuthPage(authViewModel);
        }
    }
}
