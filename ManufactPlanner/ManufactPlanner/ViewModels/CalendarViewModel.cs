using ManufactPlanner.Models;
using Microsoft.EntityFrameworkCore;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using static ManufactPlanner.ViewModels.CalendarViewModel;
using Task = ManufactPlanner.Models.Task;

namespace ManufactPlanner.ViewModels
{
    public class CalendarViewModel : ViewModelBase
    {
        private readonly MainWindowViewModel _mainWindowViewModel;
        private readonly PostgresContext _dbContext;

        // Базовые свойства для отображения календаря
        private DateTime _currentDate;
        private string _currentPeriod;
        private bool _isToday = true;
        private string[] _weekDays;
        private string[] _monthDays;

        // События календаря по дням недели
        private ObservableCollection<CalendarEventViewModel> _mondayEvents;
        private ObservableCollection<CalendarEventViewModel> _tuesdayEvents;
        private ObservableCollection<CalendarEventViewModel> _wednesdayEvents;
        private ObservableCollection<CalendarEventViewModel> _thursdayEvents;
        private ObservableCollection<CalendarEventViewModel> _fridayEvents;
        private ObservableCollection<CalendarEventViewModel> _saturdayEvents;
        private ObservableCollection<CalendarEventViewModel> _sundayEvents;

        // События календаря по месяцам и годам
        private ObservableCollection<CalendarDateViewModel> _monthCalendar;
        private ObservableCollection<CalendarMonthViewModel> _yearCalendar;

        // Режим отображения
        private CalendarMode _currentMode = CalendarMode.Week;

        // Фильтры
        private ObservableCollection<string> _statuses;
        private ObservableCollection<string> _priorities;
        private ObservableCollection<string> _projects;
        private ObservableCollection<string> _assignees;

        private string _selectedStatus;
        private string _selectedPriority;
        private string _selectedProject;
        private string _selectedAssignee;

        private bool _isLoading = false;

        public enum CalendarMode
        {
            Week,
            Month,
            Year
        }

        // Свойства календаря
        public DateTime CurrentDate
        {
            get => _currentDate;
            set
            {
                if (_currentDate != value)
                {
                    this.RaiseAndSetIfChanged(ref _currentDate, value);
                    UpdateCalendarPeriod();

                    // При смене даты всегда перезагружаем данные
                    _ = LoadEventsAsync();
                }
            }
        }

        // Добавляем метод для обработки изменений в фильтрах
        private void OnFilterChanged()
        {
            // Перезагружаем данные при изменении фильтров
            _ = LoadEventsAsync();
        }

        public string CurrentPeriod
        {
            get => _currentPeriod;
            set => this.RaiseAndSetIfChanged(ref _currentPeriod, value);
        }

        public bool IsToday
        {
            get => _isToday;
            set => this.RaiseAndSetIfChanged(ref _isToday, value);
        }

        public string[] WeekDays
        {
            get => _weekDays;
            set => this.RaiseAndSetIfChanged(ref _weekDays, value);
        }

        public string[] MonthDays
        {
            get => _monthDays;
            set => this.RaiseAndSetIfChanged(ref _monthDays, value);
        }

        // События по дням недели
        public ObservableCollection<CalendarEventViewModel> MondayEvents
        {
            get => _mondayEvents;
            set => this.RaiseAndSetIfChanged(ref _mondayEvents, value);
        }

        public ObservableCollection<CalendarEventViewModel> TuesdayEvents
        {
            get => _tuesdayEvents;
            set => this.RaiseAndSetIfChanged(ref _tuesdayEvents, value);
        }

        public ObservableCollection<CalendarEventViewModel> WednesdayEvents
        {
            get => _wednesdayEvents;
            set => this.RaiseAndSetIfChanged(ref _wednesdayEvents, value);
        }

        public ObservableCollection<CalendarEventViewModel> ThursdayEvents
        {
            get => _thursdayEvents;
            set => this.RaiseAndSetIfChanged(ref _thursdayEvents, value);
        }

        public ObservableCollection<CalendarEventViewModel> FridayEvents
        {
            get => _fridayEvents;
            set => this.RaiseAndSetIfChanged(ref _fridayEvents, value);
        }

        public ObservableCollection<CalendarEventViewModel> SaturdayEvents
        {
            get => _saturdayEvents;
            set => this.RaiseAndSetIfChanged(ref _saturdayEvents, value);
        }

        public ObservableCollection<CalendarEventViewModel> SundayEvents
        {
            get => _sundayEvents;
            set => this.RaiseAndSetIfChanged(ref _sundayEvents, value);
        }

        // Календарь месяца и года
        public ObservableCollection<CalendarDateViewModel> MonthCalendar
        {
            get => _monthCalendar;
            set => this.RaiseAndSetIfChanged(ref _monthCalendar, value);
        }

        public ObservableCollection<CalendarMonthViewModel> YearCalendar
        {
            get => _yearCalendar;
            set => this.RaiseAndSetIfChanged(ref _yearCalendar, value);
        }

        // Режим календаря
        public CalendarMode CurrentMode
        {
            get => _currentMode;
            set
            {
                if (_currentMode != value)
                {
                    this.RaiseAndSetIfChanged(ref _currentMode, value);
                    this.RaisePropertyChanged(nameof(IsWeekMode));
                    this.RaisePropertyChanged(nameof(IsMonthMode));
                    this.RaisePropertyChanged(nameof(IsYearMode));
                    UpdateCalendarPeriod();

                    // При смене режима всегда перезагружаем данные
                    _ = LoadEventsAsync();
                }
            }
        }


        public bool IsWeekMode => CurrentMode == CalendarMode.Week;
        public bool IsMonthMode => CurrentMode == CalendarMode.Month;
        public bool IsYearMode => CurrentMode == CalendarMode.Year;

        // Фильтры и селекторы
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

        public ObservableCollection<string> Projects
        {
            get => _projects;
            set => this.RaiseAndSetIfChanged(ref _projects, value);
        }

        public ObservableCollection<string> Assignees
        {
            get => _assignees;
            set => this.RaiseAndSetIfChanged(ref _assignees, value);
        }

        // Обновляем свойства фильтров, чтобы они вызывали перезагрузку данных
        public string SelectedStatus
        {
            get => _selectedStatus;
            set
            {
                this.RaiseAndSetIfChanged(ref _selectedStatus, value); OnFilterChanged();
            }
        }
        public string SelectedPriority
        {
            get => _selectedPriority;
            set
            {
                this.RaiseAndSetIfChanged(ref _selectedPriority, value); OnFilterChanged();
            }
        }

        public string SelectedProject
        {
            get => _selectedProject;
            set
            {
                this.RaiseAndSetIfChanged(ref _selectedProject, value);  OnFilterChanged();
            }
        }

        public string SelectedAssignee
        {
            get => _selectedAssignee;
            set
            {
                this.RaiseAndSetIfChanged(ref _selectedAssignee, value); OnFilterChanged();
            }
        }

        public bool IsLoading
        {
            get => _isLoading;
            set => this.RaiseAndSetIfChanged(ref _isLoading, value);
        }

        // Свойства для отображения выбранного дня
        private bool _isSelectedDayVisible = false;
        private string _selectedDayTitle = string.Empty;
        private ObservableCollection<CalendarEventViewModel> _selectedDayEvents = new ObservableCollection<CalendarEventViewModel>();
        private CalendarDateViewModel _selectedDay = null;

        public bool IsSelectedDayVisible
        {
            get => _isSelectedDayVisible;
            set => this.RaiseAndSetIfChanged(ref _isSelectedDayVisible, value);
        }

        public string SelectedDayTitle
        {
            get => _selectedDayTitle;
            set => this.RaiseAndSetIfChanged(ref _selectedDayTitle, value);
        }

        public ObservableCollection<CalendarEventViewModel> SelectedDayEvents
        {
            get => _selectedDayEvents;
            set => this.RaiseAndSetIfChanged(ref _selectedDayEvents, value);
        }
        private double _currentTimePositionExact;
        public double CurrentTimePositionExact
        {
            get => _currentTimePositionExact;
            set => this.RaiseAndSetIfChanged(ref _currentTimePositionExact, value);
        }

        // Команды для работы с выбранным днем
        public ICommand SelectDayCommand { get; }
        public ICommand CloseSelectedDayCommand { get; }
        public ICommand CreateEventOnSelectedDayCommand { get; }

        public ICommand SelectMonthCommand { get; }

        // Команды
        public ICommand PreviousPeriodCommand { get; }
        public ICommand NextPeriodCommand { get; }
        public ICommand TodayCommand { get; }
        public ICommand CreateEventCommand { get; }
        public ICommand OpenTaskDetailsCommand { get; }
        public ICommand SwitchToWeekModeCommand { get; }
        public ICommand SwitchToMonthModeCommand { get; }
        public ICommand SwitchToYearModeCommand { get; }
        public ICommand RefreshCommand { get; }


        // Модифицируем конструктор, чтобы гарантировать загрузку данных
        public CalendarViewModel(MainWindowViewModel mainWindowViewModel, PostgresContext dbContext)
        {
            _mainWindowViewModel = mainWindowViewModel;
            _dbContext = dbContext;

            // Инициализация даты
            _currentDate = DateTime.Now;

            // Инициализация фильтров
            InitializeFilterOptions();

            // Инициализация коллекций
            InitializeCollections();

            // Инициализация команд
            PreviousPeriodCommand = ReactiveCommand.Create(PreviousPeriod);
            NextPeriodCommand = ReactiveCommand.Create(NextPeriod);
            TodayCommand = ReactiveCommand.Create(GoToToday);
            CreateEventCommand = ReactiveCommand.Create(CreateEvent);
            OpenTaskDetailsCommand = ReactiveCommand.Create<int>(OpenTaskDetails);
            SwitchToWeekModeCommand = ReactiveCommand.Create(() => { CurrentMode = CalendarMode.Week; });
            SwitchToMonthModeCommand = ReactiveCommand.Create(() => { CurrentMode = CalendarMode.Month; });
            SwitchToYearModeCommand = ReactiveCommand.Create(() => { CurrentMode = CalendarMode.Year; });
            RefreshCommand = ReactiveCommand.CreateFromTask(LoadEventsAsync);

            SelectDayCommand = ReactiveCommand.Create<CalendarDateViewModel>(SelectDay);
            CloseSelectedDayCommand = ReactiveCommand.Create(CloseSelectedDay);
            CreateEventOnSelectedDayCommand = ReactiveCommand.Create(CreateEventOnSelectedDay);

            SelectMonthCommand = ReactiveCommand.Create<int>(SelectMonth);
            // Обновляем данные календаря
            UpdateCalendarPeriod();

            // Добавить инициализацию таймера для обновления времени
            StartTimeUpdater();

            // Загружаем события асинхронно после инициализации всех свойств
            // Используем Task.Run для запуска в отдельном потоке
            _ = LoadEventsAsync();
        }


        public CalendarViewModel()
        {
            // Конструктор для дизайнера
            _currentDate = DateTime.Now;

            InitializeFilterOptions();
            InitializeCollections();

            PreviousPeriodCommand = ReactiveCommand.Create(PreviousPeriod);
            NextPeriodCommand = ReactiveCommand.Create(NextPeriod);
            TodayCommand = ReactiveCommand.Create(GoToToday);
            CreateEventCommand = ReactiveCommand.Create(CreateEvent);
            OpenTaskDetailsCommand = ReactiveCommand.Create<int>(OpenTaskDetails);
            SwitchToWeekModeCommand = ReactiveCommand.Create(() => { CurrentMode = CalendarMode.Week; });
            SwitchToMonthModeCommand = ReactiveCommand.Create(() => { CurrentMode = CalendarMode.Month; });
            SwitchToYearModeCommand = ReactiveCommand.Create(() => { CurrentMode = CalendarMode.Year; });
            RefreshCommand = ReactiveCommand.CreateFromTask(LoadEventsAsync);

            UpdateCalendarPeriod();
            LoadTestData();
        }

        private void InitializeFilterOptions()
        {
            _statuses = new ObservableCollection<string>
            {
                "Все статусы",
                "В очереди",
                "В работе",
                "Ждем производство",
                "Готово"
            };

            _priorities = new ObservableCollection<string>
            {
                "Все приоритеты",
                "Высокий",
                "Средний",
                "Низкий"
            };

            _projects = new ObservableCollection<string>
            {
                "Все проекты",
                "ОП-113/24",
                "ОП-136/24",
                "ОП-141/24",
                "ОП-145/24",
                "ОП-168/24",
                "ОП-169/24"
            };

            _assignees = new ObservableCollection<string>
            {
                "Все исполнители",
                "Вяткин А.И.",
                "Киреев Б.В.",
                "Турушев С.М.",
                "Еретин Д.К.",
                "Шулепов И.Л."
            };

            _selectedStatus = _statuses[0];
            _selectedPriority = _priorities[0];
            _selectedProject = _projects[0];
            _selectedAssignee = _assignees[0];
        }

        private void InitializeCollections()
        {
            // Инициализация коллекций для событий
            _mondayEvents = new ObservableCollection<CalendarEventViewModel>();
            _tuesdayEvents = new ObservableCollection<CalendarEventViewModel>();
            _wednesdayEvents = new ObservableCollection<CalendarEventViewModel>();
            _thursdayEvents = new ObservableCollection<CalendarEventViewModel>();
            _fridayEvents = new ObservableCollection<CalendarEventViewModel>();
            _saturdayEvents = new ObservableCollection<CalendarEventViewModel>();
            _sundayEvents = new ObservableCollection<CalendarEventViewModel>();

            // Инициализация календарей по месяцам и годам
            _monthCalendar = new ObservableCollection<CalendarDateViewModel>();
            _yearCalendar = new ObservableCollection<CalendarMonthViewModel>();
        }

        private void UpdateCalendarPeriod()
        {
            switch (CurrentMode)
            {
                case CalendarMode.Week:
                    UpdateWeekView();
                    break;
                case CalendarMode.Month:
                    UpdateMonthView();
                    break;
                case CalendarMode.Year:
                    UpdateYearView();
                    break;
            }
        }

        private void UpdateWeekView()
        {
            // Определяем начало недели (понедельник)
            DateTime startOfWeek = _currentDate.Date.AddDays(-(int)_currentDate.DayOfWeek + 1);
            if (startOfWeek.DayOfWeek == DayOfWeek.Sunday) startOfWeek = startOfWeek.AddDays(-6);

            // Заполняем дни недели
            string[] days = new string[7];
            for (int i = 0; i < 7; i++)
            {
                days[i] = startOfWeek.AddDays(i).Day.ToString();
            }

            WeekDays = days;

            // Обновляем заголовок периода
            CurrentPeriod = startOfWeek.ToString("dd.MM") + " - " + startOfWeek.AddDays(6).ToString("dd.MM.yyyy");

            // Проверяем, содержит ли неделя сегодняшний день
            IsToday = _currentDate.Date >= startOfWeek && _currentDate.Date < startOfWeek.AddDays(7);
        }

        private void UpdateMonthView()
        {
            // Очищаем календарь месяца
            MonthCalendar?.Clear();

            // Первый день месяца
            DateTime firstDayOfMonth = new DateTime(_currentDate.Year, _currentDate.Month, 1);

            // День недели, с которого начинается месяц (0 - понедельник, 6 - воскресенье)
            int dayOfWeek = ((int)firstDayOfMonth.DayOfWeek + 6) % 7;

            // Количество дней в месяце
            int daysInMonth = DateTime.DaysInMonth(_currentDate.Year, _currentDate.Month);

            // Заголовок периода
            CurrentPeriod = _currentDate.ToString("MMMM yyyy");

            // Заполняем календарь
            MonthCalendar = new ObservableCollection<CalendarDateViewModel>();

            // Добавляем дни предыдущего месяца
            DateTime prevMonth = firstDayOfMonth.AddMonths(-1);
            int daysInPrevMonth = DateTime.DaysInMonth(prevMonth.Year, prevMonth.Month);

            for (int i = 0; i < dayOfWeek; i++)
            {
                MonthCalendar.Add(new CalendarDateViewModel
                {
                    Date = new DateTime(prevMonth.Year, prevMonth.Month, daysInPrevMonth - dayOfWeek + i + 1),
                    Day = (daysInPrevMonth - dayOfWeek + i + 1).ToString(),
                    IsCurrentMonth = false,
                    Events = new ObservableCollection<CalendarEventViewModel>(),
                    EventIndicators = new ObservableCollection<EventIndicator>()
                });
            }

            // Добавляем дни текущего месяца
            for (int i = 1; i <= daysInMonth; i++)
            {
                DateTime currentDate = new DateTime(_currentDate.Year, _currentDate.Month, i);
                bool isToday = currentDate.Date == DateTime.Now.Date;

                MonthCalendar.Add(new CalendarDateViewModel
                {
                    Date = currentDate,
                    Day = i.ToString(),
                    IsCurrentMonth = true,
                    IsToday = isToday,
                    Events = new ObservableCollection<CalendarEventViewModel>(),
                    EventIndicators = new ObservableCollection<EventIndicator>()
                });
            }

            // Добавляем дни следующего месяца до конца сетки
            int remainingDays = 42 - MonthCalendar.Count; // 6 недель по 7 дней
            DateTime nextMonth = firstDayOfMonth.AddMonths(1);

            for (int i = 1; i <= remainingDays; i++)
            {
                MonthCalendar.Add(new CalendarDateViewModel
                {
                    Date = new DateTime(nextMonth.Year, nextMonth.Month, i),
                    Day = i.ToString(),
                    IsCurrentMonth = false,
                    Events = new ObservableCollection<CalendarEventViewModel>(),
                    EventIndicators = new ObservableCollection<EventIndicator>()
                });
            }

            // Проверяем, является ли текущий месяц текущим
            IsToday = _currentDate.Year == DateTime.Now.Year && _currentDate.Month == DateTime.Now.Month;
        }

        private void UpdateYearView()
        {
            // Очищаем календарь года
            YearCalendar?.Clear();

            // Заголовок периода
            CurrentPeriod = _currentDate.Year.ToString();

            // Заполняем календарь по месяцам
            YearCalendar = new ObservableCollection<CalendarMonthViewModel>();

            string[] monthNames = new[]
            {
                "Январь", "Февраль", "Март", "Апрель",
                "Май", "Июнь", "Июль", "Август",
                "Сентябрь", "Октябрь", "Ноябрь", "Декабрь"
            };

            for (int month = 1; month <= 12; month++)
            {
                YearCalendar.Add(new CalendarMonthViewModel
                {
                    Month = month,
                    MonthName = monthNames[month - 1],
                    EventsCount = 1,
                    IsCurrentMonth = DateTime.Now.Year == _currentDate.Year && DateTime.Now.Month == month
                });
            }

            // Проверяем, является ли текущий год текущим
            IsToday = _currentDate.Year == DateTime.Now.Year;
        }

        // Модифицируем метод LoadEventsAsync для более надежной загрузки
        private async System.Threading.Tasks.Task LoadEventsAsync()
        {
            try
            {
                IsLoading = true;

                // Получаем текущего пользователя из MainWindowViewModel
                Guid currentUserId = Guid.Empty;
                if (_mainWindowViewModel != null && _mainWindowViewModel.CurrentUserId != Guid.Empty)
                {
                    currentUserId = _mainWindowViewModel.CurrentUserId;
                }
                else
                {
                    // Используем тестовые данные, если нет пользователя
                    LoadTestData();
                    return;
                }

                // Проверяем доступность базы данных
                if (_dbContext == null)
                {
                    // Если БД недоступна, используем тестовые данные
                    LoadTestData();
                    return;
                }

                // Загружаем пользователя вместе с его ролями
                // Добавляем проверку на null и обработку исключений
                try
                {
                    var user = await _dbContext.Users
                        .Include(u => u.Roles)
                        .FirstOrDefaultAsync(u => u.Id == currentUserId);

                    if (user == null)
                    {
                        // Если пользователь не найден, используем тестовые данные
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
                        tasksQuery = _dbContext.Tasks.AsNoTracking();
                    }
                    else if (isManager)
                    {
                        // Менеджер видит задачи своего подразделения
                        var userDepartments = await _dbContext.UserDepartments
                            .Where(ud => ud.UserId == currentUserId)
                            .Select(ud => ud.DepartmentId)
                            .ToListAsync();

                        tasksQuery = _dbContext.Tasks.AsNoTracking().Where(t =>
                            t.Assignee.UserDepartments.Any(ud =>
                                userDepartments.Contains(ud.DepartmentId)));
                    }
                    else
                    {
                        // Обычный исполнитель видит только свои задачи
                        tasksQuery = _dbContext.Tasks.AsNoTracking().Where(t => t.AssigneeId == currentUserId);
                    }

                    // Применяем фильтры
                    if (_selectedStatus != null && _selectedStatus != "Все статусы")
                    {
                        tasksQuery = tasksQuery.Where(t => t.Status == _selectedStatus);
                    }

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
                            tasksQuery = tasksQuery.Where(t => t.Priority == priorityValue);
                        }
                    }

                    if (_selectedProject != null && _selectedProject != "Все проекты")
                    {
                        tasksQuery = tasksQuery.Where(t => t.OrderPosition.Order.OrderNumber == _selectedProject);
                    }

                    if (_selectedAssignee != null && _selectedAssignee != "Все исполнители")
                    {
                        tasksQuery = tasksQuery.Where(t =>
                            t.Assignee.FirstName + " " + t.Assignee.LastName == _selectedAssignee);
                    }

                    // Дополнительно фильтруем задачи по текущему периоду календаря
                    // для оптимизации производительности
                    if (CurrentMode == CalendarMode.Week)
                    {
                        // Определяем начало недели (понедельник)
                        DateTime startOfWeek = _currentDate.Date.AddDays(-(int)_currentDate.DayOfWeek + 1);
                        if (startOfWeek.DayOfWeek == DayOfWeek.Sunday) startOfWeek = startOfWeek.AddDays(-6);
                        DateTime endOfWeek = startOfWeek.AddDays(6);

                        DateOnly startDate = DateOnly.FromDateTime(startOfWeek);
                        DateOnly endDate = DateOnly.FromDateTime(endOfWeek);

                        tasksQuery = tasksQuery.Where(t =>
                            (t.StartDate.HasValue && t.StartDate <= endDate && (t.EndDate.HasValue ? t.EndDate >= startDate : true)) ||
                            (t.EndDate.HasValue && t.EndDate >= startDate && (t.StartDate.HasValue ? t.StartDate <= endDate : true)) ||
                            (!t.StartDate.HasValue && !t.EndDate.HasValue) // Задачи без дат тоже показываем
                        );
                    }
                    else if (CurrentMode == CalendarMode.Month)
                    {
                        DateTime firstDayOfMonth = new DateTime(_currentDate.Year, _currentDate.Month, 1);
                        DateTime lastDayOfMonth = firstDayOfMonth.AddMonths(1).AddDays(-1);

                        DateOnly startDate = DateOnly.FromDateTime(firstDayOfMonth);
                        DateOnly endDate = DateOnly.FromDateTime(lastDayOfMonth);

                        tasksQuery = tasksQuery.Where(t =>
                            (t.StartDate.HasValue && t.StartDate <= endDate && (t.EndDate.HasValue ? t.EndDate >= startDate : true)) ||
                            (t.EndDate.HasValue && t.EndDate >= startDate && (t.StartDate.HasValue ? t.StartDate <= endDate : true)) ||
                            (!t.StartDate.HasValue && !t.EndDate.HasValue)
                        );
                    }
                    else if (CurrentMode == CalendarMode.Year)
                    {
                        DateTime startOfYear = new DateTime(_currentDate.Year, 1, 1);
                        DateTime endOfYear = new DateTime(_currentDate.Year, 12, 31);

                        DateOnly startDate = DateOnly.FromDateTime(startOfYear);
                        DateOnly endDate = DateOnly.FromDateTime(endOfYear);

                        tasksQuery = tasksQuery.Where(t =>
                            (t.StartDate.HasValue && t.StartDate <= endDate && (t.EndDate.HasValue ? t.EndDate >= startDate : true)) ||
                            (t.EndDate.HasValue && t.EndDate >= startDate && (t.StartDate.HasValue ? t.StartDate <= endDate : true)) ||
                            (!t.StartDate.HasValue && !t.EndDate.HasValue)
                        );
                    }

                    // Включаем необходимые зависимости и загружаем данные
                    var tasks = await tasksQuery
                        .Include(t => t.OrderPosition)
                        .ThenInclude(op => op.Order)
                        .Include(t => t.Assignee)
                        .ToListAsync();

                    // Загружаем события календаря
                    LoadCalendarEvents(tasks);

                    // Обновляем фильтры на основе полученных данных
                    UpdateFiltersFromData(tasks);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Ошибка при загрузке задач из БД: {ex.Message}");
                    LoadTestData();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Общая ошибка при загрузке задач: {ex.Message}");
                LoadTestData();
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void UpdateFiltersFromData(List<Task> tasks)
        {
            // Обновляем статусы
            var statuses = tasks
                .Select(t => t.Status)
                .Where(s => !string.IsNullOrEmpty(s))
                .Distinct()
                .ToList();

            Statuses.Clear();
            Statuses.Add("Все статусы");
            foreach (var status in statuses)
            {
                Statuses.Add(status);
            }

            // Обновляем проекты
            var projects = tasks
                .Where(t => t.OrderPosition != null && t.OrderPosition.Order != null)
                .Select(t => t.OrderPosition.Order.OrderNumber)
                .Distinct()
                .ToList();

            Projects.Clear();
            Projects.Add("Все проекты");
            foreach (var project in projects)
            {
                Projects.Add(project);
            }

            // Обновляем исполнителей
            var assignees = tasks
                .Where(t => t.Assignee != null)
                .Select(t => t.Assignee.FirstName + " " + t.Assignee.LastName)
                .Distinct()
                .ToList();

            Assignees.Clear();
            Assignees.Add("Все исполнители");
            foreach (var assignee in assignees)
            {
                Assignees.Add(assignee);
            }
        }

        private void LoadCalendarEvents(List<Task> tasks = null)
        {
            switch (CurrentMode)
            {
                case CalendarMode.Week:
                    LoadWeekEvents(tasks);
                    break;
                case CalendarMode.Month:
                    LoadMonthEvents(tasks);
                    break;
                case CalendarMode.Year:
                    LoadYearEvents(tasks);
                    break;
            }
        }

        private void LoadWeekEvents(List<Task> tasks = null)
        {
            // Очищаем все дни
            MondayEvents?.Clear();
            TuesdayEvents?.Clear();
            WednesdayEvents?.Clear();
            ThursdayEvents?.Clear();
            FridayEvents?.Clear();
            SaturdayEvents?.Clear();
            SundayEvents?.Clear();

            // Если нет задач, выходим
            if (tasks == null || !tasks.Any())
                return;

            // Определяем начало недели (понедельник)
            DateTime startOfWeek = _currentDate.Date.AddDays(-(int)_currentDate.DayOfWeek + 1);
            if (startOfWeek.DayOfWeek == DayOfWeek.Sunday) startOfWeek = startOfWeek.AddDays(-6);

            // Конец недели
            DateTime endOfWeek = startOfWeek.AddDays(6);

            // Фильтруем задачи по датам
            var filteredTasks = tasks.Where(t =>
                (t.StartDate.HasValue && DateOnly.FromDateTime(startOfWeek) <= t.StartDate && t.StartDate <= DateOnly.FromDateTime(endOfWeek)) ||
                (t.EndDate.HasValue && DateOnly.FromDateTime(startOfWeek) <= t.EndDate && t.EndDate <= DateOnly.FromDateTime(endOfWeek)) ||
                (t.StartDate.HasValue && t.EndDate.HasValue && DateOnly.FromDateTime(startOfWeek) >= t.StartDate && DateOnly.FromDateTime(endOfWeek) <= t.EndDate)
            ).ToList();

            // Распределяем задачи по дням недели
            foreach (var task in filteredTasks)
            {
                // Задаем дату начала и окончания
                DateTime startDate = task.StartDate.HasValue ?
                    task.StartDate.Value.ToDateTime(new TimeOnly(8, 0)) :
                    startOfWeek;

                DateTime endDate = task.EndDate.HasValue ?
                    task.EndDate.Value.ToDateTime(new TimeOnly(17, 0)) :
                    startDate.AddHours(2);

                // Определяем день недели и создаем событие
                for (int i = 0; i < 7; i++)
                {
                    DateTime currentDay = startOfWeek.AddDays(i);
                    if (currentDay.Date >= startDate.Date && currentDay.Date <= endDate.Date)
                    {
                        var eventViewModel = CreateEventFromTask(task, currentDay);
                        AddEventToDay(i, eventViewModel);
                    }
                }
            }
        }

        // Изменения в CalendarViewModel.cs метод LoadMonthEvents:

        private void LoadMonthEvents(List<Task> tasks = null)
        {
            // Если календарь месяца не инициализирован, выходим
            if (MonthCalendar == null)
                return;

            // Очищаем события во всех днях
            foreach (var day in MonthCalendar)
            {
                day.Events.Clear();
                day.EventIndicators.Clear();
            }

            // Если нет задач, выходим
            if (tasks == null || !tasks.Any())
                return;

            // Определяем первый и последний день месяца в календаре
            var firstDay = MonthCalendar.FirstOrDefault()?.Date;
            var lastDay = MonthCalendar.LastOrDefault()?.Date;

            if (firstDay == null || lastDay == null)
                return;

            // Создаем словарь для быстрого поиска дней
            var daysDictionary = MonthCalendar.ToDictionary(d => d.Date.Date);

            // Распределяем задачи по дням календаря
            foreach (var task in tasks)
            {
                // Определяем даты начала и окончания задачи
                DateTime startDate = task.StartDate.HasValue ?
                    task.StartDate.Value.ToDateTime(new TimeOnly(0, 0)) :
                    DateTime.Now.Date;

                DateTime endDate = task.EndDate.HasValue ?
                    task.EndDate.Value.ToDateTime(new TimeOnly(23, 59)) :
                    startDate;

                // Определяем цвет события по статусу
                string color = task.Status switch
                {
                    "В очереди" => "#00ACC1",
                    "В работе" => "#FFB74D",
                    "Ждем производство" => "#9575CD",
                    "Готово" => "#4CAF9D",
                    _ => "#666666"
                };

                // Добавляем событие на каждый день в диапазоне
                for (DateTime day = startDate.Date; day <= endDate.Date; day = day.AddDays(1))
                {
                    if (daysDictionary.TryGetValue(day.Date, out var calendarDay))
                    {
                        // Создаем событие и добавляем его
                        var eventViewModel = CreateEventFromTask(task, day);
                        calendarDay.Events.Add(eventViewModel);

                        // Убедимся, что коллекция индикаторов создана
                        if (calendarDay.EventIndicators == null)
                        {
                            calendarDay.EventIndicators = new ObservableCollection<EventIndicator>();
                        }

                        // Добавляем индикатор цвета, если такого еще нет
                        if (!calendarDay.EventIndicators.Any(i => i.Color == color))
                        {
                            calendarDay.EventIndicators.Add(new EventIndicator { Color = color });
                        }

                        // Обновляем свойства для отображения
                        calendarDay.RaisePropertyChanged(nameof(calendarDay.HasEvents));
                        calendarDay.RaisePropertyChanged(nameof(calendarDay.ShowEventsCount));
                        calendarDay.RaisePropertyChanged(nameof(calendarDay.EventsCountText));
                    }
                }
            }
        }

        // Изменения в CalendarViewModel.cs метод LoadYearEvents:

        private void LoadYearEvents(List<Task> tasks = null)
        {
            // Если календарь года не инициализирован, выходим
            if (YearCalendar == null)
                return;

            // Сбрасываем счетчики событий для всех месяцев
            foreach (var month in YearCalendar)
            {
                month.EventsCount = 0;
            }

            // Если нет задач, выходим
            if (tasks == null || !tasks.Any())
                return;

            // Создаем словарь для подсчета задач по месяцам
            var monthCounts = new Dictionary<int, int>();
            for (int i = 1; i <= 12; i++)
            {
                monthCounts[i] = 0;
            }

            // Год, для которого считаем задачи
            int currentYear = _currentDate.Year;

            // Подсчитываем задачи по месяцам для текущего года
            foreach (var task in tasks)
            {
                // Получаем даты начала и окончания задачи
                DateTime? taskStartDate = task.StartDate.HasValue ?
                    task.StartDate.Value.ToDateTime(new TimeOnly(0, 0)) : null;

                DateTime? taskEndDate = task.EndDate.HasValue ?
                    task.EndDate.Value.ToDateTime(new TimeOnly(23, 59)) : null;

                // Если нет ни даты начала, ни даты окончания, пропускаем задачу
                if (!taskStartDate.HasValue && !taskEndDate.HasValue)
                    continue;

                // Если есть только дата начала, считаем ее также датой окончания
                if (taskStartDate.HasValue && !taskEndDate.HasValue)
                    taskEndDate = taskStartDate;

                // Если есть только дата окончания, считаем ее также датой начала
                if (!taskStartDate.HasValue && taskEndDate.HasValue)
                    taskStartDate = taskEndDate;

                // Проверяем, относится ли задача к текущему году
                if ((taskStartDate.Value.Year == currentYear || taskEndDate.Value.Year == currentYear) ||
                    (taskStartDate.Value.Year < currentYear && taskEndDate.Value.Year > currentYear))
                {
                    // Определяем месяцы, входящие в период задачи
                    DateTime startInYear = new DateTime(
                        Math.Max(taskStartDate.Value.Year, currentYear),
                        taskStartDate.Value.Year < currentYear ? 1 : taskStartDate.Value.Month,
                        1);

                    DateTime endInYear = new DateTime(
                        Math.Min(taskEndDate.Value.Year, currentYear),
                        taskEndDate.Value.Year > currentYear ? 12 : taskEndDate.Value.Month,
                        1);

                    // Увеличиваем счетчики для каждого месяца в периоде задачи
                    for (DateTime month = startInYear; month <= endInYear; month = month.AddMonths(1))
                    {
                        if (month.Year == currentYear)
                        {
                            monthCounts[month.Month]++;
                        }
                    }
                }
            }

            // Обновляем значения счетчиков в модели представления
            foreach (var month in YearCalendar)
            {
                month.EventsCount = monthCounts[month.Month];

                // Принудительно обновляем связанные свойства для правильного отображения
                month.RaisePropertyChanged(nameof(month.HasEvents));
                month.RaisePropertyChanged(nameof(month.EventsCountText));
                month.RaisePropertyChanged(nameof(month.ActivityColor));
            }
        }

        private CalendarEventViewModel CreateEventFromTask(Task task, DateTime day)
        {
            // Определяем цвет события в зависимости от статуса задачи
            string color = task.Status switch
            {
                "В очереди" => "#00ACC1",
                "В работе" => "#FFB74D",
                "Ждем производство" => "#9575CD",
                "Готово" => "#4CAF9D",
                _ => "#666666"
            };

            // Определяем приоритет задачи
            string priority = task.Priority switch
            {
                1 => "Высокий",
                2 => "Средний",
                3 => "Низкий",
                _ => "Средний"
            };

            // Рассчитываем время начала и окончания события
            TimeOnly startTime = new TimeOnly(8, 0);
            TimeOnly endTime = new TimeOnly(12, 0);

            // Если задача имеет заданное время начала
            if (task.StartDate.HasValue && task.StartDate.Value.ToDateTime(TimeOnly.MinValue).Date == day.Date)
            {
                startTime = new TimeOnly(8, 0); // По умолчанию начало в 8:00
            }

            // Если задача имеет заданное время окончания
            if (task.EndDate.HasValue && task.EndDate.Value.ToDateTime(TimeOnly.MinValue).Date == day.Date)
            {
                endTime = new TimeOnly(17, 0); // По умолчанию окончание в 17:00
            }

            // Рассчитываем высоту и положение события на временной шкале
            int duration = 100; // Высота по умолчанию
            int top = 0; // Положение сверху по умолчанию

            // Устанавливаем высоту в зависимости от длительности задачи
            if (task.StartDate.HasValue && task.EndDate.HasValue)
            {
                var totalDays = (task.EndDate.Value.DayNumber - task.StartDate.Value.DayNumber) + 1;
                // Чем дольше задача, тем выше она в списке
                if (totalDays <= 1)
                    top = 30; // Быстрые задачи внизу
                else if (totalDays <= 3)
                    top = 100; // Средние задачи посередине
                else
                    top = 160; // Длинные задачи вверху

                // Рассчитываем высоту события
                duration = Math.Min(100, 60 + totalDays * 10); // Ограничиваем максимальную высоту
            }
            else
            {
                // Если нет дат, размещаем по приоритету
                top = task.Priority switch
                {
                    1 => 30, // Высокий приоритет вверху
                    2 => 100, // Средний приоритет посередине
                    3 => 160, // Низкий приоритет внизу
                    _ => 100
                };
            }

            // Формируем строку времени
            string timeStr = $"{startTime.Hour:D2}:{startTime.Minute:D2}-{endTime.Hour:D2}:{endTime.Minute:D2}";

            // Создаем модель события
            return new CalendarEventViewModel
            {
                Id = task.Id,
                Title = task.Name.Length > 15 ? task.Name.Substring(0, 15) + "..." : task.Name,
                Description = task.Description,
                Time = timeStr,
                Color = color,
                Duration = duration,
                Top = top,
                Priority = priority,
                Status = task.Status ?? "В очереди"
            };
        }

        private void AddEventToDay(int dayIndex, CalendarEventViewModel eventViewModel)
        {
            switch (dayIndex)
            {
                case 0: // Понедельник
                    MondayEvents.Add(eventViewModel);
                    break;
                case 1: // Вторник
                    TuesdayEvents.Add(eventViewModel);
                    break;
                case 2: // Среда
                    WednesdayEvents.Add(eventViewModel);
                    break;
                case 3: // Четверг
                    ThursdayEvents.Add(eventViewModel);
                    break;
                case 4: // Пятница
                    FridayEvents.Add(eventViewModel);
                    break;
                case 5: // Суббота
                    SaturdayEvents.Add(eventViewModel);
                    break;
                case 6: // Воскресенье
                    SundayEvents.Add(eventViewModel);
                    break;
            }
        }

        private void LoadTestData()
        {
            // Тестовые данные для отображения в случае ошибки или в режиме дизайнера
            switch (CurrentMode)
            {
                case CalendarMode.Week:
                    LoadTestWeekData();
                    break;
                case CalendarMode.Month:
                    LoadTestMonthData();
                    break;
                case CalendarMode.Year:
                    LoadTestYearData();
                    break;
            }
        }

        private void LoadTestWeekData()
        {
            // Очищаем все коллекции
            MondayEvents.Clear();
            TuesdayEvents.Clear();
            WednesdayEvents.Clear();
            ThursdayEvents.Clear();
            FridayEvents.Clear();
            SaturdayEvents.Clear();
            SundayEvents.Clear();

            // Добавляем тестовые события на каждый день недели
            MondayEvents.Add(new CalendarEventViewModel
            {
                Id = 1,
                Title = "Совещание",
                Time = "08:30-10:00",
                Color = "#9575CD",
                Duration = 80,
                Top = 30,
                Status = "В работе",
                Priority = "Высокий"
            });

            TuesdayEvents.Add(new CalendarEventViewModel
            {
                Id = 2,
                Title = "Разработка",
                Time = "14:00-16:00",
                Color = "#4CAF9D",
                Duration = 100,
                Top = 260,
                Status = "Готово",
                Priority = "Средний"
            });

            WednesdayEvents.Add(new CalendarEventViewModel
            {
                Id = 3,
                Title = "Тестирование",
                Time = "10:00-12:00",
                Color = "#FFB74D",
                Duration = 120,
                Top = 100,
                Status = "В работе",
                Priority = "Высокий"
            });

            ThursdayEvents.Add(new CalendarEventViewModel
            {
                Id = 4,
                Title = "Проектирование",
                Time = "08:00-09:00",
                Color = "#4CAF9D",
                Duration = 60,
                Top = 0,
                Status = "Готово",
                Priority = "Средний"
            });

            ThursdayEvents.Add(new CalendarEventViewModel
            {
                Id = 5,
                Title = "Разработка",
                Time = "11:00-14:00",
                Color = "#00ACC1",
                Duration = 180,
                Top = 120,
                Status = "В очереди",
                Priority = "Средний"
            });

            FridayEvents.Add(new CalendarEventViewModel
            {
                Id = 6,
                Title = "Совещание",
                Time = "12:00-14:00",
                Color = "#FF7043",
                Duration = 120,
                Top = 160,
                Status = "Ждем производство",
                Priority = "Низкий"
            });
        }

        private void LoadTestMonthData()
        {
            // Если календарь месяца не инициализирован, инициализируем его
            if (MonthCalendar == null || MonthCalendar.Count == 0)
            {
                UpdateMonthView();
            }

            // Очищаем все события
            foreach (var day in MonthCalendar)
            {
                day.Events.Clear();
            }

            // Добавляем тестовые события на случайные дни месяца
            var random = new Random();

            // Получаем только дни текущего месяца
            var currentMonthDays = MonthCalendar.Where(d => d.IsCurrentMonth).ToList();

            if (currentMonthDays.Count > 0)
            {
                // Добавляем событие "Совещание"
                var day1 = currentMonthDays[random.Next(currentMonthDays.Count)];
                day1.Events.Add(new CalendarEventViewModel
                {
                    Id = 1,
                    Title = "Совещание",
                    Time = "08:30-10:00",
                    Color = "#9575CD",
                    Status = "В работе",
                    Priority = "Высокий"
                });

                // Добавляем событие "Разработка"
                var day2 = currentMonthDays[random.Next(currentMonthDays.Count)];
                day2.Events.Add(new CalendarEventViewModel
                {
                    Id = 2,
                    Title = "Разработка",
                    Time = "14:00-16:00",
                    Color = "#4CAF9D",
                    Status = "Готово",
                    Priority = "Средний"
                });

                // Добавляем событие "Тестирование"
                var day3 = currentMonthDays[random.Next(currentMonthDays.Count)];
                day3.Events.Add(new CalendarEventViewModel
                {
                    Id = 3,
                    Title = "Тестирование",
                    Time = "10:00-12:00",
                    Color = "#FFB74D",
                    Status = "В работе",
                    Priority = "Высокий"
                });

                // Добавляем еще несколько событий
                for (int i = 0; i < 5; i++)
                {
                    var day = currentMonthDays[random.Next(currentMonthDays.Count)];
                    string title = i % 2 == 0 ? "Задача #" + (i + 4) : "Встреча #" + (i + 4);
                    string color = i % 2 == 0 ? "#00ACC1" : "#FF7043";

                    day.Events.Add(new CalendarEventViewModel
                    {
                        Id = i + 4,
                        Title = title,
                        Time = $"{8 + random.Next(8)}:00-{14 + random.Next(4)}:00",
                        Color = color,
                        Status = i % 3 == 0 ? "В очереди" : (i % 3 == 1 ? "В работе" : "Готово"),
                        Priority = i % 3 == 0 ? "Высокий" : (i % 3 == 1 ? "Средний" : "Низкий")
                    });
                }
            }
        }

        private void LoadTestYearData()
        {
            // Если календарь года не инициализирован, инициализируем его
            if (YearCalendar == null || YearCalendar.Count == 0)
            {
                UpdateYearView();
            }

            // Заполняем случайными данными
            var random = new Random();

            foreach (var month in YearCalendar)
            {
                month.EventsCount = random.Next(1, 15); // Случайное количество событий для каждого месяца
            }
        }

        // Методы для навигации по календарю
        private void PreviousPeriod()
        {
            switch (CurrentMode)
            {
                case CalendarMode.Week:
                    CurrentDate = _currentDate.AddDays(-7);
                    break;
                case CalendarMode.Month:
                    CurrentDate = _currentDate.AddMonths(-1);
                    break;
                case CalendarMode.Year:
                    CurrentDate = _currentDate.AddYears(-1);
                    break;
            }

            IsToday = false;
        }

        private void NextPeriod()
        {
            switch (CurrentMode)
            {
                case CalendarMode.Week:
                    CurrentDate = _currentDate.AddDays(7);
                    break;
                case CalendarMode.Month:
                    CurrentDate = _currentDate.AddMonths(1);
                    break;
                case CalendarMode.Year:
                    CurrentDate = _currentDate.AddYears(1);
                    break;
            }

            IsToday = false;
        }

        private void GoToToday()
        {
            CurrentDate = DateTime.Now;
            IsToday = true;
        }

        private void CreateEvent()
        {
            // Здесь будет логика создания нового события (задачи) в календаре
            // Например, можно открыть диалоговое окно для создания задачи
        }

        private void OpenTaskDetails(int taskId)
        {
            _mainWindowViewModel.NavigateToTaskDetails(taskId);
        }

        // Добавьте эти свойства в CalendarViewModel
        private int _currentTimePosition;
        private Timer _timeUpdateTimer;

        public int CurrentTimePosition
        {
            get => _currentTimePosition;
            set => this.RaiseAndSetIfChanged(ref _currentTimePosition, value);
        }

        // Добавьте этот метод в конструктор
        private void StartTimeUpdater()
        {
            // Немедленно установить положение
            UpdateCurrentTimePosition();

            // Создаем таймер, который будет обновлять положение каждую минуту
            _timeUpdateTimer = new Timer(_ => UpdateCurrentTimePosition(), null,
                TimeSpan.Zero, TimeSpan.FromMinutes(1));
        }

        private void UpdateCurrentTimePosition()
        {
            var now = DateTime.Now;

            // Начальное время: 8:00, каждый час занимает примерно 60 пикселей
            var startHour = 8;
            var currentHour = now.Hour;
            var currentMinute = now.Minute;

            // Проверяем, находится ли время в рабочих часах
            if (currentHour < startHour)
            {
                CurrentTimePosition = 0;
                CurrentTimePositionExact = 0;
            }
            else if (currentHour >= 18)
            {
                CurrentTimePosition = 600; // 10 часов * 60 пикселей
                CurrentTimePositionExact = 600;
            }
            else
            {
                // Рассчитываем точную позицию:
                // Каждый час = 60 пикселей, плюс отступ 10 для первого часа
                double hoursFromStart = currentHour - startHour;
                double minutesAsHours = currentMinute / 60.0;
                double totalHours = hoursFromStart + minutesAsHours;

                // 10 пикселей отступ сверху + 42 пикселя между часами
                CurrentTimePositionExact = 10 + (totalHours * 42);
                CurrentTimePosition = (int)CurrentTimePositionExact;
            }

            // Убеждаемся, что сегодня действительно сегодня
            var today = DateTime.Now.Date;
            var startOfWeek = _currentDate.Date.AddDays(-(int)_currentDate.DayOfWeek + 1);
            if (startOfWeek.DayOfWeek == DayOfWeek.Sunday) startOfWeek = startOfWeek.AddDays(-6);

            IsToday = today >= startOfWeek && today <= startOfWeek.AddDays(6);
        }

        // Не забудьте освободить ресурсы при закрытии окна
        public void Dispose()
        {
            _timeUpdateTimer?.Dispose();
        }


        // Методы для обработки выбранного дня
        private void SelectDay(CalendarDateViewModel day)
        {
            if (day == null)
                return;

            // Сохраняем выбранный день
            _selectedDay = day;

            // Если день содержит события, показываем панель с событиями
            if (day.HasEvents)
            {
                SelectedDayTitle = day.Date.ToString("d MMMM yyyy");
                SelectedDayEvents = new ObservableCollection<CalendarEventViewModel>(day.Events);
                IsSelectedDayVisible = true;
            }
            else
            {
                // Если событий нет, можно либо скрыть панель, либо показать пустую панель
                SelectedDayTitle = day.Date.ToString("d MMMM yyyy");
                SelectedDayEvents = new ObservableCollection<CalendarEventViewModel>();
                IsSelectedDayVisible = true;
            }

            // Также можно добавить функционал перехода на еженедельный вид для выбранного дня
            // Это дополнительная функция, которая может быть полезна
            if (day.IsCurrentMonth)
            {
                CurrentDate = day.Date;
                // Переключаемся на недельный вид, если это необходимо
                // CurrentMode = CalendarMode.Week;
            }
        }

        private void CloseSelectedDay()
        {
            IsSelectedDayVisible = false;
            _selectedDay = null;
        }

        private void CreateEventOnSelectedDay()
        {
            if (_selectedDay != null)
            {
                // Здесь можно открыть диалог создания события с предварительно выбранной датой
                // Например, вызвать метод, который откроет диалог создания задачи
                CreateEventWithDate(_selectedDay.Date);
            }
        }

        private void CreateEventWithDate(DateTime date)
        {
            // Здесь будет логика открытия диалога создания события с заданной датой
            // Можно передать информацию в главное окно для открытия диалога
            // _mainWindowViewModel.OpenCreateTaskDialog(date);
        }

        // Класс для индикаторов событий
        public class EventIndicator
        {
            public string Color { get; set; }
        }

        private void SelectMonth(int month)
        {
            // Обновляем текущую дату на выбранный месяц
            CurrentDate = new DateTime(_currentDate.Year, month, 1);

            // Переключаемся на месячный режим
            CurrentMode = CalendarMode.Month;
        }
    }

    public class CalendarEventViewModel : ViewModelBase
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string Time { get; set; }
        public string Color { get; set; }
        public int Duration { get; set; }
        public int Top { get; set; }
        public string Status { get; set; }
        public string Priority { get; set; }
    }

    public class CalendarDateViewModel : ViewModelBase
    {
        private ObservableCollection<EventIndicator> _eventIndicators = new ObservableCollection<EventIndicator>();

        public DateTime Date { get; set; }
        public string Day { get; set; }
        public bool IsCurrentMonth { get; set; }
        public bool IsToday { get; set; }
        public ObservableCollection<CalendarEventViewModel> Events { get; set; }

        public ObservableCollection<EventIndicator> EventIndicators
        {
            get => _eventIndicators;
            set => this.RaiseAndSetIfChanged(ref _eventIndicators, value);
        }

        public bool HasEvents => Events != null && Events.Count > 0;

        public bool ShowEventsCount => Events != null && Events.Count > 3;

        public string EventsCountText => Events != null && Events.Count > 0 ? $"+{Events.Count} событий" : string.Empty;
    }

    public class CalendarMonthViewModel : ViewModelBase
    {
        private int _eventsCount;

        public int Month { get; set; }
        public string MonthName { get; set; }

        public int EventsCount
        {
            get => _eventsCount;
            set
            {
                if (_eventsCount != value)
                {
                    this.RaiseAndSetIfChanged(ref _eventsCount, value);
                    this.RaisePropertyChanged(nameof(HasEvents));
                    this.RaisePropertyChanged(nameof(EventsCountText));
                    this.RaisePropertyChanged(nameof(ActivityColor));
                }
            }
        }

        public bool IsCurrentMonth { get; set; }

        // Вычисляемые свойства для улучшения отображения
        public bool HasEvents => EventsCount > 0;

        public string EventsCountText => EventsCount switch
        {
            0 => "событий",
            1 => "событие",
            2 => "события",
            3 => "события",
            4 => "события",
            _ => "событий"
        };

        public string ActivityColor => EventsCount switch
        {
            0 => "#9E9E9E",
            1 or 2 => "#9575CD",     // Мало событий
            3 or 4 => "#FFB74D",     // Средне событий
            _ => "#00ACC1"           // Много событий
        };
    }
}