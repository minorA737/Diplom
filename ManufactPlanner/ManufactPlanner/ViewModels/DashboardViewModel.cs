using Avalonia.Media;
using ManufactPlanner.Models;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using System.Windows.Input;

namespace ManufactPlanner.ViewModels
{
    public class DashboardViewModel : ViewModelBase
    {
        private readonly MainWindowViewModel _mainWindowViewModel;
        private readonly PostgresContext _dbContext;
        private int _activeTasksCount;
        private int _activeOrdersCount;
        private int _deadlinesTodayCount;
        private ObservableCollection<TaskItemViewModelDashbord> _recentTasks;
        private ObservableCollection<CalendarItemViewModel> _calendarItems;
        private ObservableCollection<TaskStatisticItem> _tasksByStatus;
        private string _userName;
        private string _userRole;
        private List<GraphPoint> _taskCompletionGraphPoints;

        public int ActiveTasksCount
        {
            get => _activeTasksCount;
            set => this.RaiseAndSetIfChanged(ref _activeTasksCount, value);
        }

        public int ActiveOrdersCount
        {
            get => _activeOrdersCount;
            set => this.RaiseAndSetIfChanged(ref _activeOrdersCount, value);
        }

        public int DeadlinesTodayCount
        {
            get => _deadlinesTodayCount;
            set => this.RaiseAndSetIfChanged(ref _deadlinesTodayCount, value);
        }

        public ObservableCollection<TaskItemViewModelDashbord> RecentTasks
        {
            get => _recentTasks;
            set => this.RaiseAndSetIfChanged(ref _recentTasks, value);
        }

        public ObservableCollection<CalendarItemViewModel> CalendarItems
        {
            get => _calendarItems;
            set => this.RaiseAndSetIfChanged(ref _calendarItems, value);
        }

        public ObservableCollection<TaskStatisticItem> TasksByStatus
        {
            get => _tasksByStatus;
            set => this.RaiseAndSetIfChanged(ref _tasksByStatus, value);
        }

        public string UserName
        {
            get => _userName;
            set => this.RaiseAndSetIfChanged(ref _userName, value);
        }

        public string UserRole
        {
            get => _userRole;
            set => this.RaiseAndSetIfChanged(ref _userRole, value);
        }

        public List<GraphPoint> TaskCompletionGraphPoints
        {
            get => _taskCompletionGraphPoints;
            set => this.RaiseAndSetIfChanged(ref _taskCompletionGraphPoints, value);
        }

        public string TaskCompletionGraphPointsString =>
            string.Join(" ", TaskCompletionGraphPoints.Select(p => $"{p.X},{p.Y}"));

        // Календарь
        public string CurrentMonthYear => DateTime.Now.ToString("MMMM yyyy");
        public int CurrentDay => DateTime.Now.Day;
        public List<int> DaysInMonth { get; private set; }
        public int FirstDayOfWeek { get; private set; }

        // Команды
        public ICommand OpenTaskDetailsCommand { get; }
        public ICommand ViewCalendarCommand { get; }
        public ICommand RefreshDataCommand { get; }

        public DashboardViewModel(MainWindowViewModel mainWindowViewModel, PostgresContext dbContext)
        {
            _mainWindowViewModel = mainWindowViewModel;
            _dbContext = dbContext;

            // Инициализация коллекций
            RecentTasks = new ObservableCollection<TaskItemViewModelDashbord>();
            CalendarItems = new ObservableCollection<CalendarItemViewModel>();
            TasksByStatus = new ObservableCollection<TaskStatisticItem>();
            TaskCompletionGraphPoints = new List<GraphPoint>();

            // Команды
            OpenTaskDetailsCommand = ReactiveCommand.Create<int>(OpenTaskDetails);
            ViewCalendarCommand = ReactiveCommand.Create(NavigateToCalendar);
            RefreshDataCommand = ReactiveCommand.Create(LoadDashboardData);

            // Получение информации о текущем пользователе
            LoadUserInfo();

            // Загрузка данных дашборда
            LoadDashboardData();

            // Инициализация календаря
            InitializeCalendar();
        }

        private void LoadUserInfo()
        {
            if (_mainWindowViewModel.CurrentUserId != Guid.Empty)
            {
                var user = _dbContext.Users.FirstOrDefault(u => u.Id == _mainWindowViewModel.CurrentUserId);
                if (user != null)
                {
                    UserName = $"{user.FirstName} {user.LastName}";

                    // Получение роли пользователя
                    var userRoles = _dbContext.Users
                    .Where(u => u.Id == user.Id)
                    .SelectMany(u => u.Roles)
                    .Select(r => r.Name)
                    .FirstOrDefault();

                    UserRole = userRoles ?? "Пользователь";
                }
            }
        }

        private void LoadDashboardData()
        {
            var currentUserId = _mainWindowViewModel.CurrentUserId;

            // Загрузка задач с учетом роли пользователя
            // Администратор видит все, Менеджер - свои и своих подчиненных, Исполнитель - только свои
            if (UserRole == "Администратор")
            {
                // Администратор видит все задачи
                //ActiveTasksCount = _dbContext.Tasks.Count(t => t.Status == "В процессе" || t.Status == "В очереди");
                ActiveTasksCount = _dbContext.Tasks.Count(t => t.Status == "В процессе" || t.Status == "В очереди" || t.Status == "Просрочено" || t.Status == "Ждем производство" || t.Status == "В процессе" || t.Status == "Выполнено");
                LoadRecentTasksForAdmin();
            }
            else if (UserRole == "Менеджер")
            {
                // Менеджер видит задачи, созданные им и назначенные его подчиненным
                var departmentIds = _dbContext.UserDepartments
                    .Where(ud => ud.UserId == currentUserId && ud.IsHead == true)
                    .Select(ud => ud.DepartmentId)
                    .ToList();

                var subordinateUserIds = _dbContext.UserDepartments
                    .Where(ud => departmentIds.Contains(ud.DepartmentId) && ud.UserId != currentUserId)
                    .Select(ud => ud.UserId)
                    .ToList();

                ActiveTasksCount = _dbContext.Tasks.Count(t =>
                    (t.Status == "В процессе" || t.Status == "В очереди") &&
                    (t.CreatedBy == currentUserId || subordinateUserIds.Contains(t.AssigneeId ?? Guid.Empty)));

                LoadRecentTasksForManager(subordinateUserIds);
            }
            else
            {
                // Исполнитель видит только свои задачи
                ActiveTasksCount = _dbContext.Tasks.Count(t =>
                    (t.Status == "В процессе" || t.Status == "В очереди") &&
                    t.AssigneeId == currentUserId);

                LoadRecentTasksForAssignee();
            }

            // Загрузка заказов с учетом роли
            if (UserRole == "Администратор" || UserRole == "Менеджер")
            {
                ActiveOrdersCount = _dbContext.Orders.Count(o => o.Status == "Активен");
            }
            else
            {
                // Исполнитель видит количество заказов, к которым принадлежат его задачи
                var orderPositionIds = _dbContext.Tasks
                    .Where(t => t.AssigneeId == currentUserId)
                    .Select(t => t.OrderPositionId)
                    .ToList();

                var orderIds = _dbContext.OrderPositions
                    .Where(op => orderPositionIds.Contains(op.Id))
                    .Select(op => op.OrderId)
                    .Distinct()
                    .ToList();

                ActiveOrdersCount = _dbContext.Orders.Count(o => o.Status == "Активен" && orderIds.Contains(o.Id));
            }

            // Дедлайны на сегодня для текущего пользователя
            var today = DateOnly.FromDateTime(DateTime.Today);
            if (UserRole == "Администратор")
            {
                DeadlinesTodayCount = _dbContext.Tasks.Count(t => t.EndDate == today);
            }
            else if (UserRole == "Менеджер")
            {
                var departmentIds = _dbContext.UserDepartments
                    .Where(ud => ud.UserId == currentUserId && ud.IsHead == true)
                    .Select(ud => ud.DepartmentId)
                    .ToList();

                var subordinateUserIds = _dbContext.UserDepartments
                    .Where(ud => departmentIds.Contains(ud.DepartmentId))
                    .Select(ud => ud.UserId)
                    .ToList();

                DeadlinesTodayCount = _dbContext.Tasks.Count(t =>
                    t.EndDate == today &&
                    (t.CreatedBy == currentUserId || subordinateUserIds.Contains(t.AssigneeId ?? Guid.Empty)));
            }
            else
            {
                DeadlinesTodayCount = _dbContext.Tasks.Count(t =>
                    t.EndDate == today &&
                    t.AssigneeId == currentUserId);
            }

            // Статистика по статусам задач
            LoadTaskStatusStatistics();

            // Данные для графика выполнения
            LoadTaskCompletionGraph();
        }

        private void LoadRecentTasksForAdmin()
        {
            var recentTasks = _dbContext.TaskDetailsViews
                .OrderByDescending(t => t.EndDate)
                .Take(5)
                .ToList();

            RecentTasks.Clear();
            foreach (var task in recentTasks)
            {
                RecentTasks.Add(new TaskItemViewModelDashbord
                {
                    Id = task.Id ?? 0,
                    Name = task.Name ?? string.Empty,
                    Description = $"{task.OrderNumber} поз. {task.PositionNumber}",
                    Status = task.Status ?? "Не определен",
                    Priority = task.Priority ?? 3,
                    EndDate = task.EndDate
                });
            }
        }

        private void LoadRecentTasksForManager(List<Guid> subordinateUserIds)
        {
            var currentUserId = _mainWindowViewModel.CurrentUserId;

            var recentTasks = _dbContext.Tasks
                .Join(_dbContext.TaskDetailsViews,
                    t => t.Id,
                    tdv => tdv.Id,
                    (t, tdv) => new { Task = t, Details = tdv })
                .Where(t => t.Task.CreatedBy == currentUserId || subordinateUserIds.Contains(t.Task.AssigneeId ?? Guid.Empty))
                .OrderByDescending(t => t.Task.EndDate)
                .Take(5)
                .Select(t => t.Details)
                .ToList();

            RecentTasks.Clear();
            foreach (var task in recentTasks)
            {
                RecentTasks.Add(new TaskItemViewModelDashbord
                {
                    Id = task.Id ?? 0,
                    Name = task.Name ?? string.Empty,
                    Description = $"{task.OrderNumber} поз. {task.PositionNumber}",
                    Status = task.Status ?? "Не определен",
                    Priority = task.Priority ?? 3,
                    EndDate = task.EndDate
                });
            }
        }

        private void LoadRecentTasksForAssignee()
        {
            var currentUserId = _mainWindowViewModel.CurrentUserId;

            var recentTasks = _dbContext.Tasks
                .Join(_dbContext.TaskDetailsViews,
                    t => t.Id,
                    tdv => tdv.Id,
                    (t, tdv) => new { Task = t, Details = tdv })
                .Where(t => t.Task.AssigneeId == currentUserId)
                .OrderByDescending(t => t.Task.EndDate)
                .Take(5)
                .Select(t => t.Details)
                .ToList();

            RecentTasks.Clear();
            foreach (var task in recentTasks)
            {
                RecentTasks.Add(new TaskItemViewModelDashbord
                {
                    Id = task.Id ?? 0,
                    Name = task.Name ?? string.Empty,
                    Description = $"{task.OrderNumber} поз. {task.PositionNumber}",
                    Status = task.Status ?? "Не определен",
                    Priority = task.Priority ?? 3,
                    EndDate = task.EndDate
                });
            }
        }

        private void LoadTaskStatusStatistics()
        {
            var currentUserId = _mainWindowViewModel.CurrentUserId;
            var tasks = Enumerable.Empty<Task>();

            // Выборка задач в зависимости от роли
            if (UserRole == "Администратор")
            {
                tasks = _dbContext.Tasks;
            }
            else if (UserRole == "Менеджер")
            {
                var departmentIds = _dbContext.UserDepartments
                    .Where(ud => ud.UserId == currentUserId && ud.IsHead == true)
                    .Select(ud => ud.DepartmentId)
                    .ToList();

                var subordinateUserIds = _dbContext.UserDepartments
                    .Where(ud => departmentIds.Contains(ud.DepartmentId))
                    .Select(ud => ud.UserId)
                    .ToList();

                tasks = _dbContext.Tasks
                    .Where(t => t.CreatedBy == currentUserId || subordinateUserIds.Contains(t.AssigneeId ?? Guid.Empty));
            }
            else
            {
                tasks = _dbContext.Tasks
                    .Where(t => t.AssigneeId == currentUserId);
            }

            // Группировка по статусам
            var taskStats = tasks
                .GroupBy(t => t.Status ?? "Не определен")
                .Select(g => new TaskStatisticItem
                {
                    Status = g.Key,
                    Count = g.Count(),
                    ColorHex = GetColorForStatus(g.Key)
                })
                .OrderByDescending(s => s.Count)
                .ToList();

            // Вычисление процентов
            int totalTasks = taskStats.Sum(s => s.Count);
            if (totalTasks > 0)
            {
                foreach (var stat in taskStats)
                {
                    stat.Percentage = (int)Math.Round((double)stat.Count / totalTasks * 100);
                }
            }

            TasksByStatus = new ObservableCollection<TaskStatisticItem>(taskStats);
        }

        private string GetColorForStatus(string status)
        {
            return status switch
            {
                "Выполнено" => "#4CAF9D",
                "В процессе" => "#00ACC1",
                "В очереди" => "#9575CD",
                "Ждем производство" => "#FFB74D",
                "Просрочено" => "#FF7043",
                _ => "#9E9E9E"
            };
        }

        private void LoadTaskCompletionGraph()
        {
            var currentUserId = _mainWindowViewModel.CurrentUserId;
            var today = DateTime.Today;
            var startDate = today.AddMonths(-6);

            // Запрос задач по периодам в зависимости от роли
            var tasksByMonth = new Dictionary<DateTime, int>();
            var plansByMonth = new Dictionary<DateTime, int>();

            for (var date = startDate; date <= today; date = date.AddMonths(1))
            {
                var monthStart = new DateOnly(date.Year, date.Month, 1);
                var monthEnd = new DateOnly(
                    date.Month == 12 ? date.Year + 1 : date.Year,
                    date.Month == 12 ? 1 : date.Month + 1,
                    1).AddDays(-1);

                int completedTasks = 0;
                int plannedTasks = 0;

                if (UserRole == "Администратор")
                {
                    completedTasks = _dbContext.Tasks.Count(t =>
                        t.Status == "Выполнено" &&
                        t.EndDate >= monthStart && t.EndDate <= monthEnd);

                    plannedTasks = _dbContext.Tasks.Count(t =>
                        t.EndDate >= monthStart && t.EndDate <= monthEnd);
                }
                else if (UserRole == "Менеджер")
                {
                    var departmentIds = _dbContext.UserDepartments
                        .Where(ud => ud.UserId == currentUserId && ud.IsHead == true)
                        .Select(ud => ud.DepartmentId)
                        .ToList();

                    var subordinateUserIds = _dbContext.UserDepartments
                        .Where(ud => departmentIds.Contains(ud.DepartmentId))
                        .Select(ud => ud.UserId)
                        .ToList();

                    completedTasks = _dbContext.Tasks.Count(t =>
                        t.Status == "Выполнено" &&
                        t.EndDate >= monthStart && t.EndDate <= monthEnd &&
                        (t.CreatedBy == currentUserId || subordinateUserIds.Contains(t.AssigneeId ?? Guid.Empty)));

                    plannedTasks = _dbContext.Tasks.Count(t =>
                        t.EndDate >= monthStart && t.EndDate <= monthEnd &&
                        (t.CreatedBy == currentUserId || subordinateUserIds.Contains(t.AssigneeId ?? Guid.Empty)));
                }
                else
                {
                    completedTasks = _dbContext.Tasks.Count(t =>
                        t.Status == "Выполнено" &&
                        t.EndDate >= monthStart && t.EndDate <= monthEnd &&
                        t.AssigneeId == currentUserId);

                    plannedTasks = _dbContext.Tasks.Count(t =>
                        t.EndDate >= monthStart && t.EndDate <= monthEnd &&
                        t.AssigneeId == currentUserId);
                }

                tasksByMonth[date] = completedTasks;
                plansByMonth[date] = plannedTasks;
            }

            // Преобразование данных для графика
            var graphPoints = new List<GraphPoint>();
            var planPoints = new List<GraphPoint>();

            double xStep = 180.0 / (tasksByMonth.Count - 1);
            double maxTasks = Math.Max(tasksByMonth.Values.DefaultIfEmpty(0).Max(), plansByMonth.Values.DefaultIfEmpty(0).Max());
            maxTasks = Math.Max(maxTasks, 1); // Избегаем деления на ноль

            int i = 0;
            foreach (var month in tasksByMonth.Keys.OrderBy(d => d))
            {
                double x = i * xStep;

                // Инвертируем Y, так как в графике (0,0) - левый верхний угол
                double yCompleted = 130 - (tasksByMonth[month] / maxTasks * 130);
                double yPlanned = 130 - (plansByMonth[month] / maxTasks * 130);

                graphPoints.Add(new GraphPoint { X = x, Y = yCompleted });
                planPoints.Add(new GraphPoint { X = x, Y = yPlanned });

                i++;
            }

            TaskCompletionGraphPoints = graphPoints;
        }

        private void InitializeCalendar()
        {
            var today = DateTime.Today;
            var currentMonth = new DateTime(today.Year, today.Month, 1);

            // Определяем первый день месяца в неделе (0 - воскресенье, 1 - понедельник и т.д.)
            FirstDayOfWeek = ((int)currentMonth.DayOfWeek + 6) % 7; // Корректировка для недели, начинающейся с понедельника

            // Получаем количество дней в текущем месяце
            int daysInMonth = DateTime.DaysInMonth(today.Year, today.Month);

            // Создаем список дней
            DaysInMonth = Enumerable.Range(1, daysInMonth).ToList();

            // Загружаем данные о событиях в календаре (задачах)
            LoadCalendarEvents();
        }

        private void LoadCalendarEvents()
        {
            if (_dbContext == null || _mainWindowViewModel == null)
                return;

            try
            {
                var currentUserId = _mainWindowViewModel.CurrentUserId;
                var today = DateTime.Today;
                var monthStart = new DateOnly(today.Year, today.Month, 1);
                var monthEnd = new DateOnly(
                    today.Month == 12 ? today.Year + 1 : today.Year,
                    today.Month == 12 ? 1 : today.Month + 1,
                    1).AddDays(-1);

                // Запрос задач в зависимости от роли
                var tasksByDay = new Dictionary<int, int>();

                if (UserRole == "Администратор")
                {
                    // Преобразуем запрос, чтобы избежать оператора ?? в лямбда-выражении
                    var tasks = _dbContext.Tasks
                        .Where(t => t.EndDate >= monthStart && t.EndDate <= monthEnd)
                        .ToList();

                    foreach (var task in tasks.Where(t => t.EndDate.HasValue))
                    {
                        int day = task.EndDate.Value.Day;
                        if (tasksByDay.ContainsKey(day))
                            tasksByDay[day]++;
                        else
                            tasksByDay[day] = 1;
                    }
                }
                else if (UserRole == "Менеджер")
                {
                    var departmentIds = _dbContext.UserDepartments
                        .Where(ud => ud.UserId == currentUserId && ud.IsHead == true)
                        .Select(ud => ud.DepartmentId)
                        .ToList();

                    var subordinateUserIds = _dbContext.UserDepartments
                        .Where(ud => departmentIds.Contains(ud.DepartmentId))
                        .Select(ud => ud.UserId)
                        .ToList();

                    // Преобразуем запрос, чтобы избежать оператора ?? в лямбда-выражении
                    var tasks = _dbContext.Tasks
                        .Where(t => t.EndDate >= monthStart && t.EndDate <= monthEnd)
                        .Where(t => t.CreatedBy == currentUserId ||
                               (t.AssigneeId != null && subordinateUserIds.Contains(t.AssigneeId.Value)))
                        .ToList();

                    foreach (var task in tasks.Where(t => t.EndDate.HasValue))
                    {
                        int day = task.EndDate.Value.Day;
                        if (tasksByDay.ContainsKey(day))
                            tasksByDay[day]++;
                        else
                            tasksByDay[day] = 1;
                    }
                }
                else
                {
                    // Преобразуем запрос, чтобы избежать оператора ?? в лямбда-выражении
                    var tasks = _dbContext.Tasks
                        .Where(t => t.EndDate >= monthStart && t.EndDate <= monthEnd && t.AssigneeId == currentUserId)
                        .ToList();

                    foreach (var task in tasks.Where(t => t.EndDate.HasValue))
                    {
                        int day = task.EndDate.Value.Day;
                        if (tasksByDay.ContainsKey(day))
                            tasksByDay[day]++;
                        else
                            tasksByDay[day] = 1;
                    }
                }

                // Заполнение календаря
                CalendarItems.Clear(); CalendarItems.Clear();
                for (int day = 1; day <= DaysInMonth.Count; day++)
                {
                    bool hasEvents = tasksByDay.ContainsKey(day) && tasksByDay[day] > 0;
                    bool isToday = day == today.Day;

                    CalendarItems.Add(new CalendarItemViewModel
                    {
                        Day = day,
                        IsToday = isToday,
                        HasEvents = hasEvents,
                        EventsCount = tasksByDay.ContainsKey(day) ? tasksByDay[day] : 0
                    });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при загрузке событий календаря: {ex.Message}");
                CalendarItems.Clear();
            }
        }

        private void OpenTaskDetails(int taskId)
        {
            _mainWindowViewModel.NavigateToTaskDetails(taskId);
        }

        private void NavigateToCalendar()
        {
            _mainWindowViewModel.NavigateToCalendar();
        }

        public IBrush GetPriorityColor(int priority)
        {
            return priority switch
            {
                1 => new SolidColorBrush(Color.Parse("#FF7043")),
                2 => new SolidColorBrush(Color.Parse("#FFB74D")),
                3 => new SolidColorBrush(Color.Parse("#4CAF9D")),
                _ => new SolidColorBrush(Color.Parse("#9575CD"))
            };
        }
    }

    public class TaskItemViewModelDashbord : ViewModelBase
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string Status { get; set; }
        public int Priority { get; set; }
        public DateOnly? EndDate { get; set; }

        public string GetDeadlineText()
        {
            if (EndDate == null) return "Без срока";

            var today = DateOnly.FromDateTime(DateTime.Today);

            if (EndDate == today) return "Сегодня";
            if (EndDate == today.AddDays(1)) return "Завтра";

            return EndDate.Value.ToString("dd.MM.yyyy");
        }
    }

    public class CalendarItemViewModel : ViewModelBase
    {
        public int Day { get; set; }
        public bool IsToday { get; set; }
        public bool HasEvents { get; set; }
        public int EventsCount { get; set; }
    }

    public class TaskStatisticItem : ViewModelBase
    {
        public string Status { get; set; }
        public int Count { get; set; }
        public int Percentage { get; set; }
        public string ColorHex { get; set; }
    }

    public class GraphPoint
    {
        public double X { get; set; }
        public double Y { get; set; }
    }
}