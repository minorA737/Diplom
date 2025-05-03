using ManufactPlanner.Models;
using Microsoft.EntityFrameworkCore;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Notification = ManufactPlanner.Models.Notification;
using Task = ManufactPlanner.Models.Task;

namespace ManufactPlanner.ViewModels
{
    public class TasksViewModel : ViewModelBase
    {
        private readonly MainWindowViewModel _mainWindowViewModel;
        private readonly PostgresContext _dbContext;
        private ObservableCollection<TaskItemViewModel> _tasks;
        private ObservableCollection<TaskItemViewModel> _filteredTasks;
        private ObservableCollection<string> _statuses;
        private ObservableCollection<string> _priorities;
        private ObservableCollection<string> _assignees;
        private ObservableCollection<string> _deadlinePeriods;
        private string _selectedStatus;
        private string _selectedPriority;
        private string _selectedAssignee;
        private string _selectedDeadlinePeriod;
        private int _currentPage = 1;
        private int _totalPages = 1;
        private const int PageSize = 10;
        private string _searchText = "";
        private bool _isLoading = false;
        private ViewMode _currentViewMode = ViewMode.Table;
        private ObservableCollection<TaskColumnViewModel> _kanbanColumns;

        public enum ViewMode
        {
            Table,
            Kanban,
            Calendar
        }

        public ObservableCollection<TaskItemViewModel> Tasks
        {
            get => _filteredTasks;
            set => this.RaiseAndSetIfChanged(ref _filteredTasks, value);
        }

        public ObservableCollection<string> Statuses
        {
            get => _statuses;
            set => this.RaiseAndSetIfChanged(ref _statuses, value);
        }

        public ObservableCollection<string> Priorities
        {
            get => _priorities;
            set => this.RaiseAndSetIfChanged(ref _priorities, value);
        }

        public ObservableCollection<string> Assignees
        {
            get => _assignees;
            set => this.RaiseAndSetIfChanged(ref _assignees, value);
        }

        public ObservableCollection<string> DeadlinePeriods
        {
            get => _deadlinePeriods;
            set => this.RaiseAndSetIfChanged(ref _deadlinePeriods, value);
        }

        public string SelectedStatus
        {
            get => _selectedStatus;
            set
            {
                this.RaiseAndSetIfChanged(ref _selectedStatus, value);
                ApplyFilters();
            }
        }

        public string SelectedPriority
        {
            get => _selectedPriority;
            set
            {
                this.RaiseAndSetIfChanged(ref _selectedPriority, value);
                ApplyFilters();
            }
        }

        public string SelectedAssignee
        {
            get => _selectedAssignee;
            set
            {
                this.RaiseAndSetIfChanged(ref _selectedAssignee, value);
                ApplyFilters();
            }
        }

        public string SelectedDeadlinePeriod
        {
            get => _selectedDeadlinePeriod;
            set
            {
                this.RaiseAndSetIfChanged(ref _selectedDeadlinePeriod, value);
                ApplyFilters();
            }
        }

        public int CurrentPage
        {
            get => _currentPage;
            set
            {
                this.RaiseAndSetIfChanged(ref _currentPage, value);
                UpdatePaginatedTasks();
                this.RaisePropertyChanged(nameof(CanGoToPreviousPage));
                this.RaisePropertyChanged(nameof(CanGoToNextPage));
            }
        }

        public int TotalPages
        {
            get => _totalPages;
            set => this.RaiseAndSetIfChanged(ref _totalPages, value);
        }

        public bool CanGoToPreviousPage => CurrentPage > 1;
        public bool CanGoToNextPage => CurrentPage < TotalPages;

        public string SearchText
        {
            get => _searchText;
            set
            {
                this.RaiseAndSetIfChanged(ref _searchText, value);
                ApplyFilters();
            }
        }

        public bool IsLoading
        {
            get => _isLoading;
            set => this.RaiseAndSetIfChanged(ref _isLoading, value);
        }

        public ViewMode CurrentViewMode
        {
            get => _currentViewMode;
            set
            {
                this.RaiseAndSetIfChanged(ref _currentViewMode, value);
                this.RaisePropertyChanged(nameof(IsTableViewActive));
                this.RaisePropertyChanged(nameof(IsKanbanViewActive));
                this.RaisePropertyChanged(nameof(IsCalendarViewActive));

                // Если переключились на канбан, обновляем представление канбана
                if (value == ViewMode.Kanban)
                {
                    UpdateKanbanView();
                }
            }
        }

        public bool IsTableViewActive => CurrentViewMode == ViewMode.Table;
        public bool IsKanbanViewActive => CurrentViewMode == ViewMode.Kanban;
        public bool IsCalendarViewActive => CurrentViewMode == ViewMode.Calendar;

        public ObservableCollection<TaskColumnViewModel> KanbanColumns
        {
            get => _kanbanColumns;
            set => this.RaiseAndSetIfChanged(ref _kanbanColumns, value);
        }

        public ICommand CreateTaskCommand { get; }
        public ICommand OpenTaskDetailsCommand { get; }
        public ICommand NextPageCommand { get; }
        public ICommand PreviousPageCommand { get; }
        public ICommand GoToPageCommand { get; }
        public ICommand RefreshCommand { get; }
        public ICommand SwitchToTableViewCommand { get; }
        public ICommand SwitchToKanbanViewCommand { get; }
        public ICommand SwitchToCalendarViewCommand { get; }
        public ICommand MoveTaskCommand { get; }
        public ICommand ChangeTaskStatusCommand { get; }

        public TasksViewModel(MainWindowViewModel mainWindowViewModel, PostgresContext dbContext)
        {
            _mainWindowViewModel = mainWindowViewModel;
            _dbContext = dbContext;

            // Инициализация команд
            CreateTaskCommand = ReactiveCommand.Create(CreateTask);
            OpenTaskDetailsCommand = ReactiveCommand.Create<int>(OpenTaskDetails);

            NextPageCommand = ReactiveCommand.Create(
                NextPage,
                this.WhenAnyValue(x => x.CanGoToNextPage));

            PreviousPageCommand = ReactiveCommand.Create(
                PreviousPage,
                this.WhenAnyValue(x => x.CanGoToPreviousPage));

            GoToPageCommand = ReactiveCommand.Create<int>(GoToPage);
            RefreshCommand = ReactiveCommand.CreateFromTask(LoadTasksAsync);

            SwitchToTableViewCommand = ReactiveCommand.Create(() => { CurrentViewMode = ViewMode.Table; });
            SwitchToKanbanViewCommand = ReactiveCommand.Create(() => { CurrentViewMode = ViewMode.Kanban; });
            SwitchToCalendarViewCommand = ReactiveCommand.Create(SwitchToCalendarView);

            MoveTaskCommand = ReactiveCommand.Create<(TaskItemViewModel task, string newStatus)>(tuple => MoveTask(tuple.task, tuple.newStatus));
            ChangeTaskStatusCommand = ReactiveCommand.Create<(int taskId, string newStatus)>(tuple => ChangeTaskStatus(tuple.taskId, tuple.newStatus));

            InitializeFilterOptions();
            InitializeKanbanColumns();

            // Асинхронная загрузка задач при создании ViewModel
            System.Threading.Tasks.Task.Run(() => LoadTasksAsync().ConfigureAwait(false));
        }

        private void InitializeFilterOptions()
        {
            _statuses = new ObservableCollection<string>
            {
                "Все статусы"
            };

            _priorities = new ObservableCollection<string>
            {
                "Все приоритеты",
                "Высокий",
                "Средний",
                "Низкий"
            };

            _assignees = new ObservableCollection<string>
            {
                "Все исполнители"
            };

            _deadlinePeriods = new ObservableCollection<string>
            {
                "Любой период",
                "Сегодня",
                "Завтра",
                "Эта неделя",
                "Следующая неделя",
                "Этот месяц",
                "Просроченные"
            };

            _selectedStatus = _statuses[0];
            _selectedPriority = _priorities[0];
            _selectedAssignee = _assignees[0];
            _selectedDeadlinePeriod = _deadlinePeriods[0];

            // Инициализация списков для предотвращения NullReferenceException
            _tasks = new ObservableCollection<TaskItemViewModel>();
            _filteredTasks = new ObservableCollection<TaskItemViewModel>();
        }

        private void InitializeKanbanColumns()
        {
            _kanbanColumns = new ObservableCollection<TaskColumnViewModel>
            {
                new TaskColumnViewModel
                {
                    Title = "В очереди",
                    Status = "В очереди",
                    StatusColor = "#00ACC1",
                    Tasks = new ObservableCollection<TaskItemViewModel>()
                },
                new TaskColumnViewModel
                {
                    Title = "В работе",
                    Status = "В работе",
                    StatusColor = "#FFB74D",
                    Tasks = new ObservableCollection<TaskItemViewModel>()
                },
                new TaskColumnViewModel
                {
                    Title = "Ждем производство",
                    Status = "Ждем производство",
                    StatusColor = "#9575CD",
                    Tasks = new ObservableCollection<TaskItemViewModel>()
                },
                new TaskColumnViewModel
                {
                    Title = "Готово",
                    Status = "Готово",
                    StatusColor = "#4CAF9D",
                    Tasks = new ObservableCollection<TaskItemViewModel>()
                }
            };
        }

        private async System.Threading.Tasks.Task LoadTasksAsync()
        {
            try
            {
                IsLoading = true;

                // Получаем текущего пользователя из MainWindowViewModel
                Guid currentUserId = Guid.Empty;
                if (_mainWindowViewModel != null)
                {
                    currentUserId = _mainWindowViewModel.CurrentUserId;
                }

                // Проверяем доступность базы данных
                if (_dbContext == null)
                {
                    // Если БД недоступна, используем тестовые данные
                    LoadTestData();
                    return;
                }

                // Загружаем пользователя вместе с его ролями
                var user = await _dbContext.Users
                    .Include(u => u.Roles)
                    .FirstOrDefaultAsync(u => u.Id == currentUserId);

                if (user == null)
                {
                    // Если пользователь не найден, вернуть тестовые данные
                    LoadTestData();
                    return;
                }

                // Получаем список ролей пользователя
                var userRoles = user.Roles.Select(r => r.Name).ToList();

                bool isAdmin = userRoles.Contains("Администратор");
                bool isManager = userRoles.Contains("Менеджер");

                // Загружаем задачи в зависимости от роли пользователя
                IQueryable<Task> tasksQuery;

                if (isAdmin)
                {
                    // Администратор видит все задачи
                    tasksQuery = _dbContext.Tasks;
                }
                else if (isManager)
                {
                    // Менеджер видит задачи своего подразделения
                    var userDepartments = await _dbContext.UserDepartments
                        .Where(ud => ud.UserId == currentUserId)
                        .Select(ud => ud.DepartmentId)
                        .ToListAsync();

                    tasksQuery = _dbContext.Tasks.Where(t =>
                        t.Assignee.UserDepartments.Any(ud =>
                            userDepartments.Contains(ud.DepartmentId)));
                }
                else
                {
                    // Обычный исполнитель видит только свои задачи
                    tasksQuery = _dbContext.Tasks.Where(t => t.AssigneeId == currentUserId);
                }

                // Получаем все уникальные статусы задач из БД
                var statusList = await tasksQuery
                    .Select(t => t.Status)
                    .Where(s => s != null)
                    .Distinct()
                    .ToListAsync();

                Statuses.Clear();
                Statuses.Add("Все статусы");

                foreach (var status in statusList)
                {
                    Statuses.Add(status);
                }

                // Получаем всех уникальных исполнителей из БД для задач, к которым пользователь имеет доступ
                var assigneeList = await tasksQuery
                    .Where(t => t.Assignee != null)
                    .Select(t => t.Assignee.FirstName + " " + t.Assignee.LastName)
                    .Distinct()
                    .ToListAsync();

                Assignees.Clear();
                Assignees.Add("Все исполнители");

                foreach (var assignee in assigneeList)
                {
                    Assignees.Add(assignee);
                }

                // Загружаем задачи с необходимыми связанными данными
                var tasks = await tasksQuery
                    .Include(t => t.OrderPosition)
                    .ThenInclude(op => op.Order)
                    .Include(t => t.Assignee)
                    .OrderByDescending(t => t.CreatedAt)
                    .Select(t => new TaskItemViewModel
                    {
                        Id = t.Id,
                        TaskId = "T-" + t.Id,
                        Name = t.Name,
                        Description = t.Description,
                        OrderPosition = t.OrderPosition != null ? t.OrderPosition.Order.OrderNumber + " поз. " + t.OrderPosition.PositionNumber : "-",
                        OrderPositionId = t.OrderPositionId,
                        Assignee = t.Assignee != null ? t.Assignee.FirstName + " " + t.Assignee.LastName : "-",
                        AssigneeId = t.AssigneeId,
                        Priority = t.Priority,
                        PriorityText = GetPriorityText(t.Priority),
                        Status = t.Status ?? "В очереди",
                        StatusColor = GetStatusColor(t.Status),
                        Stage = t.Stage,
                        StartDate = t.StartDate,
                        EndDate = t.EndDate,
                        // Преобразуем DateOnly в строку для отображения
                        Deadline = t.EndDate.HasValue ?
                            t.EndDate.Value.ToString("dd.MM.yyyy") :
                            "-",
                        // Считаем срок критическим, если до него осталось меньше 3 дней
                        IsDateCritical = t.EndDate.HasValue &&
                            t.EndDate.Value <= DateOnly.FromDateTime(DateTime.Now.AddDays(3)),
                        CreatedAt = t.CreatedAt,
                        UpdatedAt = t.UpdatedAt
                    })
                    .ToListAsync();

                _tasks = new ObservableCollection<TaskItemViewModel>(tasks);
                ApplyFilters();

                // Обновляем канбан представление если оно активно
                if (CurrentViewMode == ViewMode.Kanban)
                {
                    UpdateKanbanView();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при загрузке задач: {ex.Message}");

                // В случае ошибки загружаем тестовые данные
                LoadTestData();
            }
            finally
            {
                IsLoading = false;
            }
        }

        private string GetPriorityText(int priority)
        {
            return priority switch
            {
                1 => "Высокий",
                2 => "Средний",
                3 => "Низкий",
                _ => "Средний",
            };
        }

        private string GetStatusColor(string status)
        {
            return status switch
            {
                "В очереди" => "#00ACC1",
                "В работе" => "#FFB74D",
                "Ждем производство" => "#9575CD",
                "Готово" => "#4CAF9D",
                _ => "#666666",
            };
        }

        private void LoadTestData()
        {
            // Тестовые данные для отображения в случае ошибки или в режиме дизайнера
            var testTasks = new List<TaskItemViewModel>
            {
                new TaskItemViewModel { Id = 123, TaskId = "T-123", Name = "Разработка схемы электрической", OrderPosition = "ОП-136-24 поз. 1", Assignee = "Вяткин А.И.", Deadline = "07.04.2025", Status = "Готово", StatusColor = "#4CAF9D", Priority = 1, PriorityText = "Высокий", CreatedAt = DateTime.Now.AddDays(-10) },
                new TaskItemViewModel { Id = 128, TaskId = "T-128", Name = "Отладка монтажной схемы", OrderPosition = "ОП-168-24 поз. 1.14", Assignee = "Еретин Д.К.", Deadline = "18.04.2025", Status = "В работе", StatusColor = "#FFB74D", Priority = 1, PriorityText = "Высокий", IsDateCritical = true, CreatedAt = DateTime.Now.AddDays(-5) },
                new TaskItemViewModel { Id = 145, TaskId = "T-145", Name = "Проектирование корпуса", OrderPosition = "ОП-113-24 поз. 18", Assignee = "Турушев С.М.", Deadline = "24.04.2025", Status = "Ждем производство", StatusColor = "#9575CD", Priority = 2, PriorityText = "Средний", CreatedAt = DateTime.Now.AddDays(-3) },
                new TaskItemViewModel { Id = 152, TaskId = "T-152", Name = "Подготовка технического задания", OrderPosition = "ОП-169-24 поз. 1", Assignee = "Киреев Б.В.", Deadline = "30.04.2025", Status = "В очереди", StatusColor = "#00ACC1", Priority = 3, PriorityText = "Низкий", CreatedAt = DateTime.Now.AddDays(-1) },
                new TaskItemViewModel { Id = 161, TaskId = "T-161", Name = "Разработка программного обеспечения", OrderPosition = "ОП-145-24 поз. 1.10", Assignee = "Шулепов И.Л.", Deadline = "12.05.2025", Status = "В работе", StatusColor = "#FFB74D", Priority = 2, PriorityText = "Средний", CreatedAt = DateTime.Now.AddDays(-7) },
                new TaskItemViewModel { Id = 168, TaskId = "T-168", Name = "Тестирование прототипа", OrderPosition = "ОП-141-24 поз. 1", Assignee = "Нестеров Р.С.", Deadline = "20.05.2025", Status = "В очереди", StatusColor = "#00ACC1", Priority = 2, PriorityText = "Средний", CreatedAt = DateTime.Now.AddDays(-15) }
            };

            _tasks = new ObservableCollection<TaskItemViewModel>(testTasks);

            // Заполняем фильтры на основе тестовых данных
            UpdateFilterOptionsFromTestData();

            ApplyFilters();

            // Обновляем канбан представление если оно активно
            if (CurrentViewMode == ViewMode.Kanban)
            {
                UpdateKanbanView();
            }
        }

        private void UpdateFilterOptionsFromTestData()
        {
            // Обновляем статусы
            var statuses = _tasks.Select(t => t.Status).Distinct().ToList();
            Statuses.Clear();
            Statuses.Add("Все статусы");
            foreach (var status in statuses)
            {
                Statuses.Add(status);
            }

            // Обновляем исполнителей
            var assignees = _tasks.Select(t => t.Assignee).Distinct().ToList();
            Assignees.Clear();
            Assignees.Add("Все исполнители");
            foreach (var assignee in assignees)
            {
                Assignees.Add(assignee);
            }
        }

        private void ApplyFilters()
        {
            if (_tasks == null)
                return;

            IEnumerable<TaskItemViewModel> filteredTasks = _tasks;

            // Фильтр по статусу
            if (_selectedStatus != null && _selectedStatus != "Все статусы")
            {
                filteredTasks = filteredTasks.Where(t => t.Status == _selectedStatus);
            }

            // Фильтр по приоритету
            if (_selectedPriority != null && _selectedPriority != "Все приоритеты")
            {
                int priorityValue = _selectedPriority switch
                {
                    "Высокий" => 1,
                    "Средний" => 2,
                    "Низкий" => 3,
                    _ => 0
                };

                if (priorityValue > 0)
                {
                    filteredTasks = filteredTasks.Where(t => t.Priority == priorityValue);
                }
            }

            // Фильтр по исполнителю
            if (_selectedAssignee != null && _selectedAssignee != "Все исполнители")
            {
                filteredTasks = filteredTasks.Where(t => t.Assignee == _selectedAssignee);
            }

            // Фильтр по сроку выполнения
            if (_selectedDeadlinePeriod != null && _selectedDeadlinePeriod != "Любой период")
            {
                DateTime now = DateTime.Now;
                DateTime today = now.Date;
                DateTime tomorrow = today.AddDays(1);
                DateTime weekEnd = today.AddDays(7 - (int)now.DayOfWeek);
                DateTime nextWeekEnd = weekEnd.AddDays(7);
                DateTime monthEnd = new DateTime(now.Year, now.Month, DateTime.DaysInMonth(now.Year, now.Month));

                switch (_selectedDeadlinePeriod)
                {
                    case "Сегодня":
                        filteredTasks = filteredTasks.Where(t =>
                        {
                            if (t.Deadline != "-" && DateTime.TryParse(t.Deadline, out DateTime deadline))
                            {
                                return deadline.Date == today;
                            }
                            return false;
                        });
                        break;
                    case "Завтра":
                        filteredTasks = filteredTasks.Where(t =>
                        {
                            if (t.Deadline != "-" && DateTime.TryParse(t.Deadline, out DateTime deadline))
                            {
                                return deadline.Date == tomorrow;
                            }
                            return false;
                        });
                        break;
                    case "Эта неделя":
                        filteredTasks = filteredTasks.Where(t =>
                        {
                            if (t.Deadline != "-" && DateTime.TryParse(t.Deadline, out DateTime deadline))
                            {
                                return deadline.Date >= today && deadline.Date <= weekEnd;
                            }
                            return false;
                        });
                        break;
                    case "Следующая неделя":
                        filteredTasks = filteredTasks.Where(t =>
                        {
                            if (t.Deadline != "-" && DateTime.TryParse(t.Deadline, out DateTime deadline))
                            {
                                return deadline.Date > weekEnd && deadline.Date <= nextWeekEnd;
                            }
                            return false;
                        });
                        break;
                    case "Этот месяц":
                        filteredTasks = filteredTasks.Where(t =>
                        {
                            if (t.Deadline != "-" && DateTime.TryParse(t.Deadline, out DateTime deadline))
                            {
                                return deadline.Date >= today && deadline.Date <= monthEnd;
                            }
                            return false;
                        });
                        break;
                    case "Просроченные":
                        filteredTasks = filteredTasks.Where(t => t.IsDateCritical);
                        break;
                }
            }

            // Фильтр по поисковому запросу
            if (!string.IsNullOrWhiteSpace(_searchText))
            {
                string search = _searchText.ToLower();
                filteredTasks = filteredTasks.Where(t =>
                    t.TaskId.ToLower().Contains(search) ||
                    t.Name.ToLower().Contains(search) ||
                    t.OrderPosition.ToLower().Contains(search) ||
                    t.Assignee.ToLower().Contains(search));
            }

            // Обновляем список и пагинацию
            _filteredTasks = new ObservableCollection<TaskItemViewModel>(filteredTasks);
            UpdatePagination();

            // Обновляем канбан представление если оно активно
            if (CurrentViewMode == ViewMode.Kanban)
            {
                UpdateKanbanView();
            }
        }

        private void UpdatePagination()
        {
            // Вычисляем общее количество страниц
            TotalPages = Math.Max(1, (_filteredTasks.Count + PageSize - 1) / PageSize);

            // Корректируем текущую страницу, если она выходит за пределы
            if (CurrentPage > TotalPages)
                CurrentPage = TotalPages;
            else if (CurrentPage < 1)
                CurrentPage = 1;

            // Обновляем состояние кнопок пагинации
            this.RaisePropertyChanged(nameof(CanGoToPreviousPage));
            this.RaisePropertyChanged(nameof(CanGoToNextPage));

            // Применяем пагинацию
            UpdatePaginatedTasks();
        }

        private void UpdatePaginatedTasks()
        {
            if (CurrentViewMode == ViewMode.Table)
            {
                // Получаем только элементы для текущей страницы
                var paginatedTasks = _filteredTasks
                    .Skip((CurrentPage - 1) * PageSize)
                    .Take(PageSize)
                    .ToList();

                // Создаем новую коллекцию для отображения
                var displayTasks = new ObservableCollection<TaskItemViewModel>(paginatedTasks);

                // Применение чередования цветов строк
                for (int i = 0; i < displayTasks.Count; i++)
                {
                    displayTasks[i].IsAlternate = i % 2 == 1;
                }

                // Обновляем отображаемый список
                Tasks = displayTasks;
            }
            else
            {
                // Для Канбан представления не используем пагинацию
                Tasks = _filteredTasks;
            }
        }

        private void UpdateKanbanView()
        {
            // Очищаем все колонки
            foreach (var column in KanbanColumns)
            {
                column.Tasks.Clear();
            }

            // Распределяем задачи по колонкам в соответствии со статусом
            foreach (var task in _filteredTasks)
            {
                var column = KanbanColumns.FirstOrDefault(c => c.Status == task.Status);
                if (column != null)
                {
                    column.Tasks.Add(task);
                }
                else
                {
                    // Если не нашли подходящую колонку, добавляем в "В очереди" по умолчанию
                    KanbanColumns.First(c => c.Status == "В очереди").Tasks.Add(task);
                }
            }

            // Уведомляем об изменении
            this.RaisePropertyChanged(nameof(KanbanColumns));
        }

        private void NextPage()
        {
            if (CurrentPage < TotalPages)
            {
                CurrentPage++;
            }
        }

        private void PreviousPage()
        {
            if (CurrentPage > 1)
            {
                CurrentPage--;
            }
        }

        private void GoToPage(int page)
        {
            if (page >= 1 && page <= TotalPages)
            {
                CurrentPage = page;
            }
        }

        private void CreateTask()
        {
            // Здесь будет логика создания новой задачи
            // Например, открытие диалогового окна для создания задачи
        }

        private void OpenTaskDetails(int taskId)
        {
            // Переход на страницу с детальной информацией о задаче
            _mainWindowViewModel.NavigateToTaskDetails(taskId);
        }

        private void SwitchToCalendarView()
        {
            // Переход на страницу календаря
            _mainWindowViewModel.NavigateToCalendar();
        }

        private void MoveTask(TaskItemViewModel task, string newStatus)
        {
            // Изменяем статус задачи
            task.Status = newStatus;
            task.StatusColor = GetStatusColor(newStatus);

            // Обновляем задачу в базе данных
            ChangeTaskStatus(task.Id, newStatus);

            // Перестраиваем представление Канбан
            UpdateKanbanView();
        }

        private async void ChangeTaskStatus(int taskId, string newStatus)
        {
            try
            {
                if (_dbContext != null)
                {
                    var task = await _dbContext.Tasks.FindAsync(taskId);
                    if (task != null)
                    {
                        task.Status = newStatus;
                        task.UpdatedAt = DateTime.Now;

                        await _dbContext.SaveChangesAsync();

                        // Записываем историю изменений
                        var history = new TaskHistory
                        {
                            TaskId = taskId,
                            ChangedBy = _mainWindowViewModel.CurrentUserId,
                            ChangedAt = DateTime.Now,
                            FieldName = "status",
                            OldValue = task.Status,
                            NewValue = newStatus
                        };
                        
                        _dbContext.TaskHistories.Add(history);
                        await _dbContext.SaveChangesAsync();
                        
                        // Создаем уведомление для автора задачи
                        if (task.CreatedBy.HasValue && task.CreatedBy.Value != _mainWindowViewModel.CurrentUserId)
                        {
                            var notification = new Notification
                            {
                                UserId = task.CreatedBy.Value,
                                Title = "Изменен статус задачи",
                                Message = $"Задача \"{task.Name}\" изменила статус на \"{newStatus}\"",
                                IsRead = false,
                                CreatedAt = DateTime.Now,
                                LinkTo = $"/tasks/{taskId}",
                                NotificationType = "status_changed"
                            };
                            
                            _dbContext.Notifications.Add(notification);
                            await _dbContext.SaveChangesAsync();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при изменении статуса задачи: {ex.Message}");
            }
        }
    }

    public class TaskItemViewModel : ViewModelBase
    {
        public int Id { get; set; }
        public string TaskId { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string OrderPosition { get; set; }
        public int? OrderPositionId { get; set; }
        public string Assignee { get; set; }
        public Guid? AssigneeId { get; set; }
        public int Priority { get; set; }
        public string PriorityText { get; set; }
        private string _status;
        public string Status
        {
            get => _status;
            set => this.RaiseAndSetIfChanged(ref _status, value);
        }
        private string _statusColor;
        public string StatusColor
        {
            get => _statusColor;
            set => this.RaiseAndSetIfChanged(ref _statusColor, value);
        }
        public string Stage { get; set; }
        public DateOnly? StartDate { get; set; }
        public DateOnly? EndDate { get; set; }
        public string Deadline { get; set; }
        public bool IsDateCritical { get; set; }
        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        private bool _isAlternate;
        public bool IsAlternate
        {
            get => _isAlternate;
            set => this.RaiseAndSetIfChanged(ref _isAlternate, value);
        }
    }
    
    public class TaskColumnViewModel : ViewModelBase
    {
        public string Title { get; set; }
        public string Status { get; set; }
        public string StatusColor { get; set; }
        private ObservableCollection<TaskItemViewModel> _tasks;
        public ObservableCollection<TaskItemViewModel> Tasks
        {
            get => _tasks;
            set => this.RaiseAndSetIfChanged(ref _tasks, value);
        }
    }
}