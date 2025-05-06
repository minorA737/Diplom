using System;
using System.Collections.ObjectModel;
using System.Windows.Input;
using ManufactPlanner.Models;
using ReactiveUI;
using System.Reactive;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using System.Collections.Generic;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Controls;
using ManufactPlanner.Views.Dialogs;
using Avalonia;

namespace ManufactPlanner.ViewModels
{
    public class TasksViewModel : ViewModelBase
    {
        private readonly MainWindowViewModel _mainWindowViewModel;
        private readonly PostgresContext _dbContext;

        private ViewMode _currentViewMode = ViewMode.Table;
        private bool _isLoading = true;
        private string _searchText = string.Empty;
        private ObservableCollection<TaskViewModel> _tasks = new ObservableCollection<TaskViewModel>();
        private ObservableCollection<KanbanColumnViewModel> _kanbanColumns = new ObservableCollection<KanbanColumnViewModel>();

        // Пагинация
        private int _currentPage = 1;
        private int _totalPages = 1;
        private int _pageSize = 10;

        // Фильтры
        private ObservableCollection<string> _statuses = new ObservableCollection<string>();
        private ObservableCollection<string> _priorities = new ObservableCollection<string>();
        private ObservableCollection<string> _assignees = new ObservableCollection<string>();
        private ObservableCollection<string> _deadlinePeriods = new ObservableCollection<string>();

        private string _selectedStatus;
        private string _selectedPriority;
        private string _selectedAssignee;
        private string _selectedDeadlinePeriod;

        public enum ViewMode
        {
            Table,
            Kanban,
            Calendar
        }

        #region Свойства
        public ViewMode CurrentViewMode
        {
            get => _currentViewMode;
            set => this.RaiseAndSetIfChanged(ref _currentViewMode, value);
        }

        public bool IsTableViewActive => CurrentViewMode == ViewMode.Table;
        public bool IsKanbanViewActive => CurrentViewMode == ViewMode.Kanban;
        public bool IsCalendarViewActive => CurrentViewMode == ViewMode.Calendar;

        public bool IsLoading
        {
            get => _isLoading;
            set => this.RaiseAndSetIfChanged(ref _isLoading, value);
        }

        public string SearchText
        {
            get => _searchText;
            set
            {
                this.RaiseAndSetIfChanged(ref _searchText, value);
                // При изменении текста поиска - обновляем список задач
                RefreshTasksList();
            }
        }

        public ObservableCollection<TaskViewModel> Tasks
        {
            get => _tasks;
            set => this.RaiseAndSetIfChanged(ref _tasks, value);
        }

        public ObservableCollection<KanbanColumnViewModel> KanbanColumns
        {
            get => _kanbanColumns;
            set => this.RaiseAndSetIfChanged(ref _kanbanColumns, value);
        }

        // Пагинация
        public int CurrentPage
        {
            get => _currentPage;
            set => this.RaiseAndSetIfChanged(ref _currentPage, value);
        }

        public int TotalPages
        {
            get => _totalPages;
            set => this.RaiseAndSetIfChanged(ref _totalPages, value);
        }

        public bool CanGoToPreviousPage => CurrentPage > 1;
        public bool CanGoToNextPage => CurrentPage < TotalPages;

        // Фильтры
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
                RefreshTasksList();
            }
        }

        public string SelectedPriority
        {
            get => _selectedPriority;
            set
            {
                this.RaiseAndSetIfChanged(ref _selectedPriority, value);
                RefreshTasksList();
            }
        }

        public string SelectedAssignee
        {
            get => _selectedAssignee;
            set
            {
                this.RaiseAndSetIfChanged(ref _selectedAssignee, value);
                RefreshTasksList();
            }
        }

        public string SelectedDeadlinePeriod
        {
            get => _selectedDeadlinePeriod;
            set
            {
                this.RaiseAndSetIfChanged(ref _selectedDeadlinePeriod, value);
                RefreshTasksList();
            }
        }
        #endregion

        // Команды для переключения между представлениями
        public ICommand SwitchToTableViewCommand { get; }
        public ICommand SwitchToKanbanViewCommand { get; }
        public ICommand SwitchToCalendarViewCommand { get; }

        // Команда для создания новой задачи
        public ICommand CreateTaskCommand { get; }

        // Команда для открытия детальной информации о задаче
        public ICommand OpenTaskDetailsCommand { get; }

        // Команды пагинации
        public ICommand NextPageCommand { get; }
        public ICommand PreviousPageCommand { get; }

        // Команды обновления
        public ICommand RefreshCommand { get; }

        public TasksViewModel(MainWindowViewModel mainWindowViewModel, PostgresContext dbContext)
        {
            _mainWindowViewModel = mainWindowViewModel;
            _dbContext = dbContext;

            // Инициализация команд
            SwitchToTableViewCommand = ReactiveCommand.Create(() =>
            {
                CurrentViewMode = ViewMode.Table;
                this.RaisePropertyChanged(nameof(IsTableViewActive));
                this.RaisePropertyChanged(nameof(IsKanbanViewActive));
                this.RaisePropertyChanged(nameof(IsCalendarViewActive));
            });

            SwitchToKanbanViewCommand = ReactiveCommand.Create(() =>
            {
                CurrentViewMode = ViewMode.Kanban;
                this.RaisePropertyChanged(nameof(IsTableViewActive));
                this.RaisePropertyChanged(nameof(IsKanbanViewActive));
                this.RaisePropertyChanged(nameof(IsCalendarViewActive));
                // При переключении на Kanban - обновляем колонки
                UpdateKanbanColumns();
            });

            SwitchToCalendarViewCommand = ReactiveCommand.Create(() =>
            {
                CurrentViewMode = ViewMode.Calendar;
                this.RaisePropertyChanged(nameof(IsTableViewActive));
                this.RaisePropertyChanged(nameof(IsKanbanViewActive));
                this.RaisePropertyChanged(nameof(IsCalendarViewActive));
            });

            CreateTaskCommand = ReactiveCommand.Create(CreateTask);

            OpenTaskDetailsCommand = ReactiveCommand.Create<int>((taskId) =>
            {
                _mainWindowViewModel.NavigateToTaskDetails(taskId);
            });

            // Инициализация команд пагинации
            NextPageCommand = ReactiveCommand.Create(() =>
            {
                if (CanGoToNextPage)
                {
                    CurrentPage++;
                    this.RaisePropertyChanged(nameof(CanGoToNextPage));
                    this.RaisePropertyChanged(nameof(CanGoToPreviousPage));
                    LoadTasks();
                }
            });

            PreviousPageCommand = ReactiveCommand.Create(() =>
            {
                if (CanGoToPreviousPage)
                {
                    CurrentPage--;
                    this.RaisePropertyChanged(nameof(CanGoToNextPage));
                    this.RaisePropertyChanged(nameof(CanGoToPreviousPage));
                    LoadTasks();
                }
            });

            RefreshCommand = ReactiveCommand.Create(() =>
            {
                RefreshTasksList();
            });

            // Инициализация фильтров
            InitializeFiltersAsync();

            // Загрузка данных из БД
            LoadTasks();
        }
        // Обновленная реализация метода CreateTask в классе TasksViewModel

        private async void CreateTask()
        {
            try
            {
                // Получаем главное окно приложения
                var mainWindow = GetMainWindow();
                if (mainWindow == null)
                {
                    Console.WriteLine("Не удалось получить главное окно приложения");
                    return;
                }

                // Получаем текущего пользователя из MainWindowViewModel
                Guid currentUserId = _mainWindowViewModel.CurrentUserId;

                // Показываем диалог создания задачи
                var task = await TaskCreateDialog.ShowDialog(mainWindow, _dbContext, currentUserId);

                // Если задача была создана, обновляем список задач
                if (task != null)
                {
                    // Обновляем список задач
                    RefreshTasksList();

                    // Выводим уведомление об успешном создании задачи
                    Console.WriteLine($"Задача {task.Name} успешно создана");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при создании задачи: {ex.Message}");
                // Обработка ошибок
            }
        }

        private Window GetMainWindow()
        {
            if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                return desktop.MainWindow;
            }
            return null;
        }
        private async void InitializeFiltersAsync()
        {
            try
            {
                // Статусы задач
                Statuses = new ObservableCollection<string>
                {
                    "Все статусы",
                    "В очереди",
                    "В процессе",
                    "Ждем производство",
                    "Готово",
                    "Отменено"
                };

                // Приоритеты
                Priorities = new ObservableCollection<string>
                {
                    "Все приоритеты",
                    "Высокий",
                    "Средний",
                    "Низкий"
                };

                // Периоды
                DeadlinePeriods = new ObservableCollection<string>
                {
                    "Все сроки",
                    "Сегодня",
                    "На этой неделе",
                    "На следующей неделе",
                    "Просроченные"
                };

                // Асинхронная загрузка списка исполнителей из базы
                var assigneesFromDb = await _dbContext.Users
                    .Where(u => u.IsActive == true)
                    .OrderBy(u => u.LastName)
                    .Select(u => u.FirstName + " " + u.LastName)
                    .ToListAsync();

                // Добавляем пункт "Все исполнители" в начало списка
                Assignees = new ObservableCollection<string>(new[] { "Все исполнители" }.Concat(assigneesFromDb));

                // Устанавливаем значения по умолчанию
                SelectedStatus = "Все статусы";
                SelectedPriority = "Все приоритеты";
                SelectedAssignee = "Все исполнители";
                SelectedDeadlinePeriod = "Все сроки";
            }
            catch (Exception)
            {
                // В случае ошибки инициализируем базовые значения
                Statuses = new ObservableCollection<string> { "Все статусы" };
                Priorities = new ObservableCollection<string> { "Все приоритеты" };
                DeadlinePeriods = new ObservableCollection<string> { "Все сроки" };
                Assignees = new ObservableCollection<string> { "Все исполнители" };
            }
        }

        private async void LoadTasks()
        {
            IsLoading = true;

            try
            {
                // Создаем базовый запрос
                var query = _dbContext.TaskDetailsViews.AsQueryable();

                // Применяем фильтры, если они выбраны
                if (!string.IsNullOrEmpty(SearchText))
                {
                    query = query.Where(t =>
                        t.Name.Contains(SearchText) ||
                        t.OrderNumber.Contains(SearchText) ||
                        t.PositionNumber.Contains(SearchText) ||
                        t.AssigneeName.Contains(SearchText));
                }

                if (!string.IsNullOrEmpty(SelectedStatus) && SelectedStatus != "Все статусы")
                {
                    query = query.Where(t => t.Status == SelectedStatus);
                }

                if (!string.IsNullOrEmpty(SelectedPriority) && SelectedPriority != "Все приоритеты")
                {
                    int priorityValue = SelectedPriority switch
                    {
                        "Высокий" => 1,
                        "Средний" => 2,
                        "Низкий" => 3,
                        _ => 0
                    };

                    if (priorityValue > 0)
                    {
                        query = query.Where(t => t.Priority == priorityValue);
                    }
                }

                if (!string.IsNullOrEmpty(SelectedAssignee) && SelectedAssignee != "Все исполнители")
                {
                    query = query.Where(t => t.AssigneeName == SelectedAssignee);
                }

                if (!string.IsNullOrEmpty(SelectedDeadlinePeriod) && SelectedDeadlinePeriod != "Все сроки")
                {
                    var today = DateOnly.FromDateTime(DateTime.Today);
                    var endOfWeek = DateOnly.FromDateTime(DateTime.Today.AddDays(7 - (int)DateTime.Today.DayOfWeek));
                    var startOfNextWeek = endOfWeek.AddDays(1);
                    var endOfNextWeek = startOfNextWeek.AddDays(6);

                    query = SelectedDeadlinePeriod switch
                    {
                        "Сегодня" => query.Where(t => t.EndDate == today),
                        "На этой неделе" => query.Where(t => t.EndDate >= today && t.EndDate <= endOfWeek),
                        "На следующей неделе" => query.Where(t => t.EndDate >= startOfNextWeek && t.EndDate <= endOfNextWeek),
                        "Просроченные" => query.Where(t => t.EndDate < today && t.Status != "Готово" && t.Status != "Завершено"),
                        _ => query
                    };
                }

                // Подсчитываем общее количество задач для пагинации
                var totalCount = await query.CountAsync();
                TotalPages = (int)Math.Ceiling(totalCount / (double)_pageSize);

                // Проверяем текущую страницу
                if (CurrentPage > TotalPages && TotalPages > 0)
                {
                    CurrentPage = TotalPages;
                }
                else if (CurrentPage < 1)
                {
                    CurrentPage = 1;
                }

                // Применяем пагинацию
                var pagedTasks = await query
                    .OrderByDescending(t => t.Id)
                    .Skip((CurrentPage - 1) * _pageSize)
                    .Take(_pageSize)
                    .ToListAsync();

                // Преобразуем в ViewModel
                var taskViewModels = pagedTasks.Select((t, index) => new TaskViewModel
                {
                    Id = t.Id ?? 0,
                    TaskId = $"T-{t.Id}",
                    Name = t.Name ?? "Без названия",
                    OrderPosition = $"{t.OrderNumber} поз. {t.PositionNumber}",
                    Assignee = t.AssigneeName ?? "Не назначен",
                    Deadline = t.EndDate?.ToString("dd.MM.yyyy") ?? "Не указан",
                    Status = t.Status ?? "Не указан",
                    Priority = t.Priority ?? 3,
                    PriorityText = GetPriorityText(t.Priority ?? 3),
                    IsAlternate = index % 2 == 1,
                    IsDateCritical = t.EndDate.HasValue && t.EndDate.Value < DateOnly.FromDateTime(DateTime.Today) &&
                                    t.Status != "Готово" && t.Status != "Завершено"
                }).ToList();

                // Обновляем коллекцию
                Tasks = new ObservableCollection<TaskViewModel>(taskViewModels);

                // Обновляем свойства пагинации
                this.RaisePropertyChanged(nameof(CanGoToNextPage));
                this.RaisePropertyChanged(nameof(CanGoToPreviousPage));

                // Если включен режим Канбан, обновляем его колонки
                if (IsKanbanViewActive)
                {
                    UpdateKanbanColumns();
                }
            }
            catch (Exception ex)
            {
                // В случае ошибки показываем пустой список
                Tasks = new ObservableCollection<TaskViewModel>();
                // Здесь можно добавить логирование ошибки
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void RefreshTasksList()
        {
            // Сбрасываем на первую страницу при обновлении списка
            CurrentPage = 1;
            LoadTasks();
        }

        private void UpdateKanbanColumns()
        {
            // Создаем колонки для канбана
            var columns = new List<KanbanColumnViewModel>
    {
        new KanbanColumnViewModel
        {
            Title = "В очереди",
            StatusColor = "#9575CD",
            Tasks = new ObservableCollection<TaskViewModel>(
                Tasks.Where(t => t.Status == "В очереди").ToList())
        },
        new KanbanColumnViewModel
        {
            Title = "В процессе",
            StatusColor = "#00ACC1",
            Tasks = new ObservableCollection<TaskViewModel>(
                Tasks.Where(t => t.Status == "В процессе" || t.Status == "В работе").ToList())
        },
        new KanbanColumnViewModel
        {
            Title = "Ждет",
            StatusColor = "#FFB74D",
            Tasks = new ObservableCollection<TaskViewModel>(
                Tasks.Where(t => t.Status == "Ждем производство" || t.Status == "Ожидание").ToList())
        },
        new KanbanColumnViewModel
        {
            Title = "Завершено",
            StatusColor = "#4CAF9D",
            Tasks = new ObservableCollection<TaskViewModel>(
                Tasks.Where(t => t.Status == "Готово" || t.Status == "Завершено").ToList())
        }
    };

            KanbanColumns = new ObservableCollection<KanbanColumnViewModel>(columns);
        }

        private string GetPriorityText(int priority)
        {
            return priority switch
            {
                1 => "Высокий",
                2 => "Средний",
                3 => "Низкий",
                _ => "Стандартный"
            };
        }
    }

    public class TaskViewModel : ViewModelBase
    {
        public int Id { get; set; }
        public string TaskId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string OrderPosition { get; set; } = string.Empty;
        public string Assignee { get; set; } = string.Empty;
        public string Deadline { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public int Priority { get; set; }
        public string PriorityText { get; set; } = string.Empty;
        public bool IsAlternate { get; set; }
        public bool IsDateCritical { get; set; }
    }

    public class KanbanColumnViewModel : ViewModelBase
    {
        public string Title { get; set; } = string.Empty;
        public string StatusColor { get; set; } = "#666666";
        public ObservableCollection<TaskViewModel> Tasks { get; set; } = new ObservableCollection<TaskViewModel>();
    }
}