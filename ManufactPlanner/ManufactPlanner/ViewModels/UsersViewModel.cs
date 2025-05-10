using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using ReactiveUI;
using ManufactPlanner.Models;
using Microsoft.EntityFrameworkCore;

namespace ManufactPlanner.ViewModels
{
    public class UsersViewModel : ViewModelBase
    {
        private readonly MainWindowViewModel _mainWindowViewModel;
        private readonly PostgresContext _dbContext;

        // Информация о пользователе
        private string _fullName = string.Empty;
        private string _username = string.Empty;
        private string _userInitials = string.Empty;
        private string _department = string.Empty;
        private string _role = string.Empty;
        private string _email = string.Empty;
        private string _lastLogin = string.Empty;

        // Статистика задач
        private int _totalTasks = 0;
        private int _completedTasks = 0;
        private int _inProgressTasks = 0;
        private int _pendingTasks = 0;
        private int _completionPercentage = 0;

        // Коллекция задач пользователя
        private ObservableCollection<UserTaskViewModel> _userTasks;

        // Коллекция последних событий
        private ObservableCollection<UserEventViewModel> _recentEvents;

        public ObservableCollection<UserTaskViewModel> UserTasks
        {
            get => _userTasks;
            set => this.RaiseAndSetIfChanged(ref _userTasks, value);
        }

        public ObservableCollection<UserEventViewModel> RecentEvents
        {
            get => _recentEvents;
            set => this.RaiseAndSetIfChanged(ref _recentEvents, value);
        }

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

            // Инициализация коллекций
            UserTasks = new ObservableCollection<UserTaskViewModel>();
            RecentEvents = new ObservableCollection<UserEventViewModel>();

            // Загрузка данных пользователя и статистики
            LoadUserData();
            LoadUserStatistics();
            LoadUserTasks();
            LoadUserEvents();
        }

        private void LoadUserData()
        {
            try
            {
                var currentUserId = _mainWindowViewModel.CurrentUserId;

                if (currentUserId != Guid.Empty)
                {
                    var user = _dbContext.Users.FirstOrDefault(u => u.Id == currentUserId);

                    if (user != null)
                    {
                        FullName = $"{user.FirstName} {user.LastName}";
                        Username = user.Username;
                        UserInitials = GetInitials(user.FirstName, user.LastName);
                        Email = user.Email ?? "Не указан";
                        LastLogin = user.LastLogin?.ToString("dd.MM.yyyy, HH:mm") ?? "Никогда";

                        // Загрузка отдела пользователя
                        var userDepartment = _dbContext.UserDepartments
                            .Include(ud => ud.Department)
                            .FirstOrDefault(ud => ud.UserId == currentUserId);

                        if (userDepartment != null && userDepartment.Department != null)
                        {
                            Department = userDepartment.Department.Name;
                        }
                        else
                        {
                            Department = "Не назначен";
                        }

                        // Загрузка роли пользователя
                        var userRole = _dbContext.Users
                            .Where(u => u.Id == currentUserId)
                            .SelectMany(u => u.Roles)
                            .Select(r => r.Name)
                            .FirstOrDefault();

                        Role = userRole ?? "Пользователь";
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при загрузке данных пользователя: {ex.Message}");

                // Установка значений по умолчанию
                FullName = "Неизвестный пользователь";
                Username = "unknown";
                UserInitials = "?";
                Department = "Не определено";
                Role = "Не определено";
                Email = "Не указан";
                LastLogin = "Неизвестно";
            }
        }

        private void LoadUserStatistics()
        {
            try
            {
                var currentUserId = _mainWindowViewModel.CurrentUserId;

                if (currentUserId != Guid.Empty)
                {
                    // Общее количество задач пользователя
                    TotalTasks = _dbContext.Tasks.Count(t => t.AssigneeId == currentUserId);

                    // Задачи со статусом "Выполнено"
                    CompletedTasks = _dbContext.Tasks.Count(t => t.AssigneeId == currentUserId && t.Status == "Выполнено");

                    // Задачи со статусом "В процессе"
                    InProgressTasks = _dbContext.Tasks.Count(t => t.AssigneeId == currentUserId && t.Status == "В процессе");

                    // Задачи со статусом "В очереди" или "Ждем производство"
                    PendingTasks = _dbContext.Tasks.Count(t => t.AssigneeId == currentUserId &&
                        (t.Status == "В очереди" || t.Status == "Ждем производство"));

                    // Вычисление процента выполнения
                    CompletionPercentage = TotalTasks > 0
                        ? (int)Math.Round((double)CompletedTasks / TotalTasks * 100)
                        : 0;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при загрузке статистики пользователя: {ex.Message}");

                // Установка значений по умолчанию
                TotalTasks = 0;
                CompletedTasks = 0;
                InProgressTasks = 0;
                PendingTasks = 0;
                CompletionPercentage = 0;
            }
        }

        private void LoadUserTasks()
        {
            try
            {
                var currentUserId = _mainWindowViewModel.CurrentUserId;

                if (currentUserId != Guid.Empty)
                {
                    // Получение задач пользователя с деталями
                    var tasks = _dbContext.Tasks
                        .Join(_dbContext.TaskDetailsViews,
                            t => t.Id,
                            tdv => tdv.Id,
                            (t, tdv) => new { Task = t, Details = tdv })
                        .Where(t => t.Task.AssigneeId == currentUserId)
                        .OrderByDescending(t => t.Task.EndDate)
                        .Take(5)
                        .ToList();

                    UserTasks.Clear();
                    foreach (var task in tasks)
                    {
                        // Форматирование дедлайна
                        string deadlineText = "Без срока";

                        if (task.Task.EndDate.HasValue)
                        {
                            var today = DateOnly.FromDateTime(DateTime.Today);

                            if (task.Task.EndDate.Value == today)
                                deadlineText = "Сегодня";
                            else if (task.Task.EndDate.Value == today.AddDays(1))
                                deadlineText = "Завтра";
                            else
                                deadlineText = task.Task.EndDate.Value.ToString("dd.MM.yyyy");
                        }

                        UserTasks.Add(new UserTaskViewModel
                        {
                            Id = task.Task.Id,
                            Name = task.Task.Name,
                            Project = $"{task.Details.OrderNumber} поз. {task.Details.PositionNumber}",
                            Status = task.Task.Status ?? "Не определен",
                            Deadline = deadlineText,
                            IsDeadlineToday = task.Task.EndDate == DateOnly.FromDateTime(DateTime.Today)
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при загрузке задач пользователя: {ex.Message}");
                UserTasks.Clear();
            }
        }

        private void LoadUserEvents()
        {
            try
            {
                var currentUserId = _mainWindowViewModel.CurrentUserId;

                if (currentUserId != Guid.Empty)
                {
                    // Получение последних уведомлений пользователя
                    var notifications = _dbContext.Notifications
                        .Where(n => n.UserId == currentUserId)
                        .OrderByDescending(n => n.CreatedAt)
                        .Take(5)
                        .ToList();

                    // Получение последних изменений задач пользователя
                    var taskHistories = _dbContext.TaskHistories
                        .Include(th => th.Task)
                        .Where(th => th.Task.AssigneeId == currentUserId)
                        .OrderByDescending(th => th.ChangedAt)
                        .Take(5)
                        .ToList();

                    RecentEvents.Clear();

                    // Добавление уведомлений
                    foreach (var notification in notifications)
                    {
                        RecentEvents.Add(new UserEventViewModel
                        {
                            Title = notification.Title,
                            Timestamp = notification.CreatedAt?.ToString("dd.MM.yyyy, HH:mm") ?? "Неизвестно"
                        });
                    }

                    // Добавление изменений задач
                    foreach (var history in taskHistories)
                    {
                        string eventTitle = "Изменение задачи";

                        if (history.FieldName == "status")
                            eventTitle = $"Изменен статус задачи на '{history.NewValue}'";
                        else if (history.FieldName == "priority")
                            eventTitle = "Изменен приоритет задачи";

                        RecentEvents.Add(new UserEventViewModel
                        {
                            Title = eventTitle,
                            Timestamp = history.ChangedAt?.ToString("dd.MM.yyyy, HH:mm") ?? "Неизвестно"
                        });
                    }

                    // Сортировка событий по времени
                    RecentEvents = new ObservableCollection<UserEventViewModel>(
                        RecentEvents.OrderByDescending(e => DateTime.TryParse(e.Timestamp, out var date) ? date : DateTime.MinValue)
                              .Take(3)
                    );
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при загрузке событий пользователя: {ex.Message}");
                RecentEvents.Clear();
            }
        }

        private string GetInitials(string firstName, string lastName)
        {
            // Вспомогательный метод для получения инициалов
            if (string.IsNullOrEmpty(firstName) || string.IsNullOrEmpty(lastName))
                return "?";

            return $"{firstName[0]}{lastName[0]}";
        }
    }

    // Класс для представления задач пользователя
    public class UserTaskViewModel : ViewModelBase
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Project { get; set; }
        public string Status { get; set; }
        public string Deadline { get; set; }
        public bool IsDeadlineToday { get; set; }
    }

    // Класс для представления событий пользователя
    public class UserEventViewModel : ViewModelBase
    {
        public string Title { get; set; }
        public string Timestamp { get; set; }
    }
}