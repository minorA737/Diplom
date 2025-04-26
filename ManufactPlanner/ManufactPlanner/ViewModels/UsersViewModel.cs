using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System;
using System.Reactive.Linq;
using ReactiveUI;
using ManufactPlanner.Models;

namespace ManufactPlanner.ViewModels
{
    public class UsersViewModel : ViewModelBase
    {
        private readonly MainWindowViewModel _mainWindowViewModel;
        private readonly PostgresContext _dbContext;

        // Информация о пользователе
        private string _fullName = "Александр Петров";
        private string _username = "petrov";
        private string _userInitials = "АП";
        private string _department = "Конструкторский отдел";
        private string _role = "Менеджер";
        private string _email = "petrov@example.com";
        private string _lastLogin = "10.05.2025, 09:15";

        // Статистика задач
        private int _totalTasks = 27;
        private int _completedTasks = 19;
        private int _inProgressTasks = 5;
        private int _pendingTasks = 3;
        private int _completionPercentage = 70;

        // Свойства для отображения в интерфейсе
        public string FullName
        {
            get => _fullName;
            set => this.RaiseAndSetIfChanged(ref _fullName, value);
        }

        public string Username
        {
            get => _username;
            set => this.RaiseAndSetIfChanged(ref _username, value);
        }

        public string UserInitials
        {
            get => _userInitials;
            set => this.RaiseAndSetIfChanged(ref _userInitials, value);
        }

        public string Department
        {
            get => _department;
            set => this.RaiseAndSetIfChanged(ref _department, value);
        }

        public string Role
        {
            get => _role;
            set => this.RaiseAndSetIfChanged(ref _role, value);
        }

        public string Email
        {
            get => _email;
            set => this.RaiseAndSetIfChanged(ref _email, value);
        }

        public string LastLogin
        {
            get => _lastLogin;
            set => this.RaiseAndSetIfChanged(ref _lastLogin, value);
        }

        public int TotalTasks
        {
            get => _totalTasks;
            set => this.RaiseAndSetIfChanged(ref _totalTasks, value);
        }

        public int CompletedTasks
        {
            get => _completedTasks;
            set => this.RaiseAndSetIfChanged(ref _completedTasks, value);
        }

        public int InProgressTasks
        {
            get => _inProgressTasks;
            set => this.RaiseAndSetIfChanged(ref _inProgressTasks, value);
        }

        public int PendingTasks
        {
            get => _pendingTasks;
            set => this.RaiseAndSetIfChanged(ref _pendingTasks, value);
        }

        public int CompletionPercentage
        {
            get => _completionPercentage;
            set => this.RaiseAndSetIfChanged(ref _completionPercentage, value);
        }

        public UsersViewModel(MainWindowViewModel mainWindowViewModel, PostgresContext dbContext)
        {
            _mainWindowViewModel = mainWindowViewModel;
            _dbContext = dbContext;

            // Загрузка данных пользователя и статистики
            LoadUserData();
            LoadUserStatistics();
        }

        private void LoadUserData()
        {
            // В реальном приложении здесь будет загрузка данных из базы данных
            // Например:
            // var user = _dbContext.Users.FirstOrDefault(u => u.Id == currentUserId);
            // if (user != null)
            // {
            //     FullName = $"{user.FirstName} {user.LastName}";
            //     Username = user.Username;
            //     UserInitials = GetInitials(user.FirstName, user.LastName);
            //     Email = user.Email;
            //     LastLogin = user.LastLogin.ToString("dd.MM.yyyy, HH:mm");
            //     
            //     // Загрузка отдела и роли
            //     var userDepartment = _dbContext.UserDepartments
            //         .Include(ud => ud.Department)
            //         .FirstOrDefault(ud => ud.UserId == currentUserId);
            //     if (userDepartment != null)
            //     {
            //         Department = userDepartment.Department.Name;
            //     }
            //     
            //     var userRole = _dbContext.UserRoles
            //         .Include(ur => ur.Role)
            //         .FirstOrDefault(ur => ur.UserId == currentUserId);
            //     if (userRole != null)
            //     {
            //         Role = userRole.Role.Name;
            //     }
            // }
        }

        private void LoadUserStatistics()
        {
            // В реальном приложении здесь будет загрузка статистики из базы данных
            // Например:
            // var userId = currentUserId;
            // TotalTasks = _dbContext.Tasks.Count(t => t.AssigneeId == userId);
            // CompletedTasks = _dbContext.Tasks.Count(t => t.AssigneeId == userId && t.Status == "Завершено");
            // InProgressTasks = _dbContext.Tasks.Count(t => t.AssigneeId == userId && t.Status == "В процессе");
            // PendingTasks = _dbContext.Tasks.Count(t => t.AssigneeId == userId && t.Status == "Ожидание");
            // 
            // CompletionPercentage = TotalTasks > 0 
            //     ? (int)Math.Round((double)CompletedTasks / TotalTasks * 100)
            //     : 0;
        }

        private string GetInitials(string firstName, string lastName)
        {
            // Вспомогательный метод для получения инициалов
            if (string.IsNullOrEmpty(firstName) || string.IsNullOrEmpty(lastName))
                return "?";

            return $"{firstName[0]}{lastName[0]}";
        }
    }
}