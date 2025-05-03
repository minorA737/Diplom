using System;
using System.Windows.Input;
using ManufactPlanner.Models;
using ReactiveUI;
using System.Reactive;
using System.Collections.ObjectModel;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

namespace ManufactPlanner.ViewModels
{
    public class TaskDetailsViewModel : ViewModelBase
    {
        private readonly MainWindowViewModel _mainWindowViewModel;
        private readonly PostgresContext _dbContext;

        // Свойства для отображения данных
        private string _taskId = string.Empty;
        private string _taskName = string.Empty;
        private string _createdDate = string.Empty;
        private string _status = string.Empty;
        private string _statusColor = "#FFB74D";
        private string _priority = string.Empty;
        private string _priorityColor = "#FF7043";
        private string _deadline = string.Empty;
        private string _description = string.Empty;
        private string _assignee = string.Empty;
        private string _orderPosition = string.Empty;
        private string _stageStatus = string.Empty;
        private string _notes = string.Empty;
        private string _customer = string.Empty;
        private string _orderDeadline = string.Empty;
        private string _orderStatus = string.Empty;
        private bool _isLoading = true;

        private ObservableCollection<RelatedTaskViewModel> _relatedTasks = new ObservableCollection<RelatedTaskViewModel>();

        #region Свойства
        public string TaskId
        {
            get => _taskId;
            set => this.RaiseAndSetIfChanged(ref _taskId, value);
        }

        public string TaskName
        {
            get => _taskName;
            set => this.RaiseAndSetIfChanged(ref _taskName, value);
        }

        public string CreatedDate
        {
            get => _createdDate;
            set => this.RaiseAndSetIfChanged(ref _createdDate, value);
        }

        public string Status
        {
            get => _status;
            set => this.RaiseAndSetIfChanged(ref _status, value);
        }

        public string StatusColor
        {
            get => _statusColor;
            set => this.RaiseAndSetIfChanged(ref _statusColor, value);
        }

        public string Priority
        {
            get => _priority;
            set => this.RaiseAndSetIfChanged(ref _priority, value);
        }

        public string PriorityColor
        {
            get => _priorityColor;
            set => this.RaiseAndSetIfChanged(ref _priorityColor, value);
        }

        public string Deadline
        {
            get => _deadline;
            set => this.RaiseAndSetIfChanged(ref _deadline, value);
        }

        public string Description
        {
            get => _description;
            set => this.RaiseAndSetIfChanged(ref _description, value);
        }

        public string Assignee
        {
            get => _assignee;
            set => this.RaiseAndSetIfChanged(ref _assignee, value);
        }

        public string OrderPosition
        {
            get => _orderPosition;
            set => this.RaiseAndSetIfChanged(ref _orderPosition, value);
        }

        public string StageStatus
        {
            get => _stageStatus;
            set => this.RaiseAndSetIfChanged(ref _stageStatus, value);
        }

        public string Notes
        {
            get => _notes;
            set => this.RaiseAndSetIfChanged(ref _notes, value);
        }

        public string Customer
        {
            get => _customer;
            set => this.RaiseAndSetIfChanged(ref _customer, value);
        }

        public string OrderDeadline
        {
            get => _orderDeadline;
            set => this.RaiseAndSetIfChanged(ref _orderDeadline, value);
        }

        public string OrderStatus
        {
            get => _orderStatus;
            set => this.RaiseAndSetIfChanged(ref _orderStatus, value);
        }

        public ObservableCollection<RelatedTaskViewModel> RelatedTasks
        {
            get => _relatedTasks;
            set => this.RaiseAndSetIfChanged(ref _relatedTasks, value);
        }

        public bool IsLoading
        {
            get => _isLoading;
            set => this.RaiseAndSetIfChanged(ref _isLoading, value);
        }
        #endregion

        public ICommand NavigateToTasksCommand { get; }
        public ICommand EditTaskCommand { get; }

        public TaskDetailsViewModel(MainWindowViewModel mainWindowViewModel, PostgresContext dbContext, int taskId)
        {
            _mainWindowViewModel = mainWindowViewModel;
            _dbContext = dbContext;

            NavigateToTasksCommand = ReactiveCommand.Create(() => _mainWindowViewModel.NavigateToTasks());
            EditTaskCommand = ReactiveCommand.Create(EditTask);

            // Загружаем данные асинхронно
            LoadTaskDetailsAsync(taskId);
        }

        private async void LoadTaskDetailsAsync(int taskId)
        {
            IsLoading = true;

            try
            {
                // Пытаемся найти задачу в представлении TaskDetailsView
                var taskView = await _dbContext.TaskDetailsViews
                    .FirstOrDefaultAsync(t => t.Id == taskId);

                if (taskView != null)
                {
                    // Заполняем данные из представления
                    TaskId = $"T-{taskId}";
                    TaskName = taskView.Name ?? "Без названия";
                    Status = taskView.Status ?? "Без статуса";
                    StatusColor = GetStatusColor(taskView.Status);
                    Description = taskView.Description ?? "Нет описания";

                    // Работа с датами
                    if (taskView.EndDate.HasValue)
                    {
                        Deadline = $"Срок: {taskView.EndDate.Value:dd.MM.yyyy}";
                    }
                    else
                    {
                        Deadline = "Срок не указан";
                    }

                    // Приоритет
                    var priorityValue = taskView.Priority ?? 3;
                    Priority = $"Приоритет: {GetPriorityText(priorityValue)}";
                    PriorityColor = GetPriorityColor(priorityValue);

                    // Поля
                    Assignee = taskView.AssigneeName ?? "Не назначен";
                    OrderPosition = $"{taskView.OrderNumber} поз. {taskView.PositionNumber}";
                    StageStatus = taskView.Stage ?? "Не определен";
                    Notes = taskView.Notes ?? "Нет примечаний";
                    Customer = taskView.Customer ?? "Не указан";

                    // Дополнительные настройки
                    CreatedDate = "Создана " + DateTime.Now.AddDays(-5).ToString("dd.MM.yyyy");
                }
                else
                {
                    // Поиск в основной таблице задач с соединениями
                    var task = await _dbContext.Tasks
                        .Include(t => t.Assignee)
                        .Include(t => t.OrderPosition)
                            .ThenInclude(op => op.Order)
                        .FirstOrDefaultAsync(t => t.Id == taskId);

                    if (task != null)
                    {
                        // Заполнение из основной таблицы
                        TaskId = $"T-{task.Id}";
                        TaskName = task.Name;
                        Status = task.Status ?? "Без статуса";
                        StatusColor = GetStatusColor(task.Status);
                        Description = task.Description ?? "Нет описания";

                        // Работа с датами
                        if (task.EndDate.HasValue)
                        {
                            Deadline = $"Срок: {task.EndDate.Value:dd.MM.yyyy}";
                        }
                        else
                        {
                            Deadline = "Срок не указан";
                        }

                        if (task.CreatedAt.HasValue)
                        {
                            CreatedDate = $"Создана {task.CreatedAt.Value:dd.MM.yyyy}";
                        }
                        else
                        {
                            CreatedDate = "Дата создания не указана";
                        }

                        // Приоритет
                        Priority = $"Приоритет: {GetPriorityText(task.Priority)}";
                        PriorityColor = GetPriorityColor(task.Priority);

                        // Поля с проверками на null
                        Assignee = task.Assignee?.FirstName + " " + task.Assignee?.LastName ?? "Не назначен";

                        if (task.OrderPosition != null)
                        {
                            OrderPosition = $"{task.OrderPosition.Order?.OrderNumber} поз. {task.OrderPosition.PositionNumber}";
                            Customer = task.OrderPosition.Order?.Customer ?? "Не указан";

                            if (task.OrderPosition.Order?.ContractDeadline != null)
                            {
                                OrderDeadline = task.OrderPosition.Order.ContractDeadline.Value.ToString("dd.MM.yyyy");
                            }
                            else
                            {
                                OrderDeadline = "Не указан";
                            }

                            OrderStatus = task.OrderPosition.Order?.Status ?? "Неизвестно";
                        }
                        else
                        {
                            OrderPosition = "Не указана";
                            Customer = "Не указан";
                            OrderDeadline = "Не указан";
                            OrderStatus = "Неизвестно";
                        }

                        StageStatus = task.Stage ?? "Не определен";
                        Notes = task.Notes ?? "Нет примечаний";
                    }
                    else
                    {
                        // Если задача не найдена, заполняем заглушками
                        TaskId = $"T-{taskId}";
                        TaskName = "Задача не найдена";
                        Status = "Ошибка";
                        StatusColor = "#FF7043";
                        Description = "Не удалось найти данные задачи с указанным ID.";
                    }
                }

                // Загрузим связанные задачи
                await LoadRelatedTasksAsync(taskId);
            }
            catch (Exception ex)
            {
                // Обработка ошибок
                TaskName = "Ошибка загрузки";
                Description = $"Произошла ошибка при загрузке данных: {ex.Message}";
                Status = "Ошибка";
                StatusColor = "#FF7043";
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async System.Threading.Tasks.Task LoadRelatedTasksAsync(int taskId)
        {
            try
            {
                // Находим задачу, чтобы получить OrderPositionId
                var task = await _dbContext.Tasks
                    .FirstOrDefaultAsync(t => t.Id == taskId);

                if (task?.OrderPositionId != null)
                {
                    // Находим все задачи с тем же OrderPositionId, кроме текущей
                    var relatedTasks = await _dbContext.Tasks
                        .Where(t => t.OrderPositionId == task.OrderPositionId && t.Id != taskId)
                        .OrderByDescending(t => t.CreatedAt)
                        .Take(5)
                        .ToListAsync();

                    // Преобразуем в модель представления
                    var relatedTasksVM = relatedTasks.Select(t => new RelatedTaskViewModel
                    {
                        Id = t.Id,
                        Name = $"T-{t.Id}: {t.Name}",
                        Status = t.Status ?? "Нет статуса",
                        StatusColor = GetStatusColor(t.Status)
                    }).ToList();

                    // Обновляем коллекцию
                    RelatedTasks = new ObservableCollection<RelatedTaskViewModel>(relatedTasksVM);
                }
            }
            catch (Exception)
            {
                // В случае ошибки просто оставляем пустую коллекцию
            }
        }

        private string GetStatusColor(string? status)
        {
            if (string.IsNullOrEmpty(status))
                return "#666666";

            return status switch
            {
                "Готово" or "Завершено" => "#4CAF9D",
                "В процессе" or "В работе" => "#00ACC1",
                "В очереди" => "#9575CD",
                "Ожидание" => "#FFB74D",
                "Ждем производство" => "#FF9800",
                "Отменено" => "#F44336",
                _ => "#666666"
            };
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

        private string GetPriorityColor(int priority)
        {
            return priority switch
            {
                1 => "#FF7043", // Высокий - красный
                2 => "#FFB74D", // Средний - оранжевый
                3 => "#9575CD", // Низкий - синий 
                _ => "#666666"  // По умолчанию - серый
            };
        }

        private void EditTask()
        {
            // Логика редактирования задачи
            // TODO: Реализовать в будущем
        }
    }

    public class RelatedTaskViewModel : ViewModelBase
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string StatusColor { get; set; } = "#666666";
    }
}