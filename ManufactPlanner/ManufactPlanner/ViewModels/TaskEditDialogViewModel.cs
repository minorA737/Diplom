using ManufactPlanner.Models;
using Microsoft.EntityFrameworkCore;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using System.Threading.Tasks;
using Task = ManufactPlanner.Models.Task;

namespace ManufactPlanner.ViewModels
{
    public class TaskEditDialogViewModel : ViewModelBase
    {
        private readonly PostgresContext _dbContext;
        private readonly Task _originalTask;

        private int _taskId;
        private string _name;
        private string _description;
        private int _priority = 3; // По умолчанию низкий приоритет
        private string _status = "В очереди"; // По умолчанию

        private DateTime? _startDate;
        private DateTime? _endDate;

        private Guid? _assigneeId;
        private string _coAssignees;
        private int? _orderPositionId;
        private string _stage;
        private string _debuggingStatus;
        private string _notes;
        private bool _isProcessing;
        private string _errorMessage;
        private bool _hasError;

        // Выпадающие списки для селекторов
        private ObservableCollection<UserViewModel> _users;
        private UserViewModel _selectedAssignee;

        public int TaskId
        {
            get => _taskId;
            set => this.RaiseAndSetIfChanged(ref _taskId, value);
        }

        public DateTime? StartDate
        {
            get => _startDate;
            set => this.RaiseAndSetIfChanged(ref _startDate, value);
        }

        public DateTime? EndDate
        {
            get => _endDate;
            set => this.RaiseAndSetIfChanged(ref _endDate, value);
        }

        public string Name
        {
            get => _name;
            set => this.RaiseAndSetIfChanged(ref _name, value);
        }

        public string Description
        {
            get => _description;
            set => this.RaiseAndSetIfChanged(ref _description, value);
        }

        public int Priority
        {
            get => _priority;
            set => this.RaiseAndSetIfChanged(ref _priority, value);
        }

        public string Status
        {
            get => _status;
            set => this.RaiseAndSetIfChanged(ref _status, value);
        }

        public string CoAssignees
        {
            get => _coAssignees;
            set => this.RaiseAndSetIfChanged(ref _coAssignees, value);
        }

        public string Stage
        {
            get => _stage;
            set => this.RaiseAndSetIfChanged(ref _stage, value);
        }

        public string DebuggingStatus
        {
            get => _debuggingStatus;
            set => this.RaiseAndSetIfChanged(ref _debuggingStatus, value);
        }

        public string Notes
        {
            get => _notes;
            set => this.RaiseAndSetIfChanged(ref _notes, value);
        }

        public bool IsProcessing
        {
            get => _isProcessing;
            set => this.RaiseAndSetIfChanged(ref _isProcessing, value);
        }

        public string ErrorMessage
        {
            get => _errorMessage;
            set
            {
                this.RaiseAndSetIfChanged(ref _errorMessage, value);
                HasError = !string.IsNullOrEmpty(value);
            }
        }

        public bool HasError
        {
            get => _hasError;
            set => this.RaiseAndSetIfChanged(ref _hasError, value);
        }

        public ObservableCollection<UserViewModel> Users
        {
            get => _users;
            set => this.RaiseAndSetIfChanged(ref _users, value);
        }

        public UserViewModel SelectedAssignee
        {
            get => _selectedAssignee;
            set
            {
                this.RaiseAndSetIfChanged(ref _selectedAssignee, value);
                if (value != null)
                {
                    _assigneeId = value.Id;
                }
            }
        }

        // Опции для выпадающих списков
        public ObservableCollection<string> StatusOptions { get; } = new ObservableCollection<string>
        {
            "В очереди",
            "В процессе",
            "Ждем производство",
            "Готово",
            "Отменено"
        };

        public ObservableCollection<int> PriorityOptions { get; } = new ObservableCollection<int> { 1, 2, 3 };

        // Команды
        public ReactiveCommand<Unit, (bool Success, Task Task)> SaveCommand { get; }
        public ReactiveCommand<Unit, (bool Success, Task Task)> CancelCommand { get; }

        public TaskEditDialogViewModel(PostgresContext dbContext, Guid currentUserId, Task taskToEdit)
        {
            _dbContext = dbContext;
            _originalTask = taskToEdit;

            // Инициализация свойств из задачи
            TaskId = taskToEdit.Id;
            Name = taskToEdit.Name;
            Description = taskToEdit.Description ?? string.Empty;
            Priority = taskToEdit.Priority;
            Status = taskToEdit.Status ?? "В очереди";
            CoAssignees = taskToEdit.CoAssignees ?? string.Empty;
            Stage = taskToEdit.Stage ?? string.Empty;
            DebuggingStatus = taskToEdit.DebuggingStatus ?? string.Empty;
            Notes = taskToEdit.Notes ?? string.Empty;
            _orderPositionId = taskToEdit.OrderPositionId;
            _assigneeId = taskToEdit.AssigneeId;

            // Инициализируем даты
            if (taskToEdit.StartDate.HasValue)
                _startDate = taskToEdit.StartDate.Value.ToDateTime(new TimeOnly(0, 0));
            else
                _startDate = null;

            if (taskToEdit.EndDate.HasValue)
                _endDate = taskToEdit.EndDate.Value.ToDateTime(new TimeOnly(0, 0));
            else
                _endDate = null;

            // Загружаем данные для выпадающих списков
            LoadUsers();

            // Инициализация команд
            SaveCommand = ReactiveCommand.CreateFromTask(SaveTaskAsync);
            CancelCommand = ReactiveCommand.Create(() => (false, (Task)null));
        }

        private async System.Threading.Tasks.Task LoadUsers()
        {
            try
            {
                IsProcessing = true;

                var users = await _dbContext.Users
                    .Where(u => u.IsActive == true)
                    .OrderBy(u => u.LastName)
                    .Select(u => new UserViewModel
                    {
                        Id = u.Id,
                        Name = $"{u.LastName} {u.FirstName}"
                    })
                    .ToListAsync();

                Users = new ObservableCollection<UserViewModel>(users);

                // Устанавливаем выбранного исполнителя, если он назначен
                if (_assigneeId.HasValue)
                {
                    var selectedUser = Users.FirstOrDefault(u => u.Id == _assigneeId.Value);
                    if (selectedUser != null)
                    {
                        SelectedAssignee = selectedUser;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при загрузке пользователей: {ex.Message}");
                ErrorMessage = $"Ошибка при загрузке пользователей: {ex.Message}";
                Users = new ObservableCollection<UserViewModel>();
            }
            finally
            {
                IsProcessing = false;
            }
        }

        private bool ValidateInput()
        {
            if (string.IsNullOrWhiteSpace(Name))
            {
                ErrorMessage = "Название задачи не может быть пустым";
                return false;
            }

            if (_endDate.HasValue && _startDate.HasValue && _endDate < _startDate)
            {
                ErrorMessage = "Дата окончания не может быть раньше даты начала";
                return false;
            }

            return true;
        }

        private async System.Threading.Tasks.Task<(bool Success, Task Task)> SaveTaskAsync()
        {
            if (!ValidateInput())
            {
                return (false, null);
            }

            try
            {
                IsProcessing = true;
                ErrorMessage = string.Empty;

                // Получаем задачу из базы данных
                var task = await _dbContext.Tasks.FindAsync(_taskId);

                if (task == null)
                {
                    ErrorMessage = "Задача не найдена в базе данных";
                    return (false, null);
                }

                // Сохраняем предыдущее состояние для истории изменений
                string oldStatus = task.Status;
                int oldPriority = task.Priority;
                Guid? oldAssigneeId = task.AssigneeId;
                DateOnly? oldEndDate = task.EndDate;

                // Обновляем задачу
                task.Name = Name;
                task.Description = Description;
                task.Priority = Priority;
                task.Status = Status;
                task.StartDate = StartDate.HasValue ? DateOnly.FromDateTime(StartDate.Value) : null;
                task.EndDate = EndDate.HasValue ? DateOnly.FromDateTime(EndDate.Value) : null;
                task.AssigneeId = _assigneeId;
                task.CoAssignees = CoAssignees;
                task.Stage = Stage;
                task.DebuggingStatus = DebuggingStatus;
                task.Notes = Notes;
                task.UpdatedAt = DateTime.Now;

                // Получаем текущего пользователя для записи в историю
                Guid changedByUserId = Guid.Empty; // По умолчанию
                if (_dbContext.Entry(task).Property("CreatedBy").CurrentValue != null)
                {
                    changedByUserId = (Guid)_dbContext.Entry(task).Property("CreatedBy").CurrentValue;
                }

                // Сохраняем в базу данных
                await System.Threading.Tasks.Task.Run(() =>
                {
                    _dbContext.Tasks.Update(task);
                    _dbContext.SaveChanges();
                });

                // Добавляем записи в историю изменений
                await AddTaskHistoryEntries(task, oldStatus, oldPriority, oldAssigneeId, oldEndDate, changedByUserId);

                // Создаем уведомление для нового исполнителя, если он изменился
                if (_assigneeId.HasValue && oldAssigneeId != _assigneeId)
                {
                    var notification = new Models.Notification
                    {
                        UserId = _assigneeId.Value,
                        Title = "Изменение задачи",
                        Message = $"Вам назначена задача: {Name}",
                        IsRead = false,
                        CreatedAt = DateTime.Now,
                        LinkTo = $"/tasks/{task.Id}",
                        NotificationType = "task_assigned"
                    };

                    await System.Threading.Tasks.Task.Run(() =>
                    {
                        _dbContext.Notifications.Add(notification);
                        _dbContext.SaveChanges();
                    });
                }

                return (true, task);
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Ошибка при сохранении задачи: {ex.Message}";
                return (false, null);
            }
            finally
            {
                IsProcessing = false;
            }
        }

        private async System.Threading.Tasks.Task AddTaskHistoryEntries(Task task, string oldStatus, int oldPriority, Guid? oldAssigneeId, DateOnly? oldEndDate, Guid changedByUserId)
        {
            try
            {
                // Добавляем записи в историю только если значения изменились
                var historyEntries = new List<TaskHistory>();

                // Проверяем статус
                if (oldStatus != task.Status)
                {
                    historyEntries.Add(new TaskHistory
                    {
                        TaskId = task.Id,
                        ChangedBy = changedByUserId,
                        ChangedAt = DateTime.Now,
                        FieldName = "status",
                        OldValue = oldStatus,
                        NewValue = task.Status
                    });
                }

                // Проверяем приоритет
                if (oldPriority != task.Priority)
                {
                    historyEntries.Add(new TaskHistory
                    {
                        TaskId = task.Id,
                        ChangedBy = changedByUserId,
                        ChangedAt = DateTime.Now,
                        FieldName = "priority",
                        OldValue = oldPriority.ToString(),
                        NewValue = task.Priority.ToString()
                    });
                }

                // Проверяем исполнителя
                if (oldAssigneeId != task.AssigneeId)
                {
                    historyEntries.Add(new TaskHistory
                    {
                        TaskId = task.Id,
                        ChangedBy = changedByUserId,
                        ChangedAt = DateTime.Now,
                        FieldName = "assignee",
                        OldValue = oldAssigneeId?.ToString() ?? "не назначен",
                        NewValue = task.AssigneeId?.ToString() ?? "не назначен"
                    });
                }

                // Проверяем срок
                if (oldEndDate != task.EndDate)
                {
                    historyEntries.Add(new TaskHistory
                    {
                        TaskId = task.Id,
                        ChangedBy = changedByUserId,
                        ChangedAt = DateTime.Now,
                        FieldName = "end_date",
                        OldValue = oldEndDate?.ToString() ?? "не указан",
                        NewValue = task.EndDate?.ToString() ?? "не указан"
                    });
                }

                // Добавляем записи в базу, если есть что добавлять
                if (historyEntries.Any())
                {
                    await System.Threading.Tasks.Task.Run(() =>
                    {
                        _dbContext.TaskHistories.AddRange(historyEntries);
                        _dbContext.SaveChanges();
                    });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при добавлении истории изменений: {ex.Message}");
                // Не выбрасываем исключение, чтобы не прерывать сохранение задачи
            }
        }
    }
}