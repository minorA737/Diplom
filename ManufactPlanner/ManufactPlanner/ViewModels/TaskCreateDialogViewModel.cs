using ManufactPlanner.Models;
using Microsoft.EntityFrameworkCore;
using ReactiveUI;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using System.Threading.Tasks;
using Task = ManufactPlanner.Models.Task;

namespace ManufactPlanner.ViewModels
{
    public class TaskCreateDialogViewModel : ViewModelBase
    {
        private readonly PostgresContext _dbContext;
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
        private ObservableCollection<OrderPositionViewModel2> _orderPositions;
        private ObservableCollection<UserViewModel> _users;
        private OrderPositionViewModel2 _selectedOrderPosition;
        private UserViewModel _selectedAssignee;


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

        // В конструкторе замените
        
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

        public ObservableCollection<OrderPositionViewModel2> OrderPositions
        {
            get => _orderPositions;
            set => this.RaiseAndSetIfChanged(ref _orderPositions, value);
        }

        public ObservableCollection<UserViewModel> Users
        {
            get => _users;
            set => this.RaiseAndSetIfChanged(ref _users, value);
        }

        public OrderPositionViewModel2 SelectedOrderPosition
        {
            get => _selectedOrderPosition;
            set
            {
                this.RaiseAndSetIfChanged(ref _selectedOrderPosition, value);
                if (value != null)
                {
                    _orderPositionId = value.Id;
                }
            }
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

        public TaskCreateDialogViewModel(PostgresContext dbContext, Guid currentUserId)
        {
            _dbContext = dbContext;

            // Инициализируем текущую дату как начальную
            _startDate = DateTime.Today;
            _endDate = DateTime.Today.AddDays(7); // По умолчанию неделя на выполнение

            // Загружаем данные для выпадающих списков
            LoadOrderPositions();
            LoadUsers();

            // Инициализация команд
            SaveCommand = ReactiveCommand.CreateFromTask(SaveTaskAsync);
            CancelCommand = ReactiveCommand.Create(() => (false, (Task)null));
        }

        private async System.Threading.Tasks.Task LoadOrderPositions()
        {
            try
            {
                IsProcessing = true;

                var orderPositions = await _dbContext.OrderPositions
                    .Include(op => op.Order)
                    .OrderByDescending(op => op.Order.CreatedAt)
                    .Select(op => new OrderPositionViewModel2
                    {
                        Id = op.Id,
                        Name = $"{op.Order.OrderNumber} поз. {op.PositionNumber} - {op.ProductName}"
                    })
                    .ToListAsync();

                OrderPositions = new ObservableCollection<OrderPositionViewModel2>(orderPositions);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при загрузке позиций заказов: {ex.Message}");
                ErrorMessage = $"Ошибка при загрузке позиций заказов: {ex.Message}";
                OrderPositions = new ObservableCollection<OrderPositionViewModel2>();
            }
            finally
            {
                IsProcessing = false;
            }
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

            if (_orderPositionId == null)
            {
                ErrorMessage = "Необходимо выбрать позицию заказа";
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

                // Создаем новую задачу
                var task = new Task
                {
                    OrderPositionId = _orderPositionId,
                    Name = Name,
                    Description = Description,
                    Priority = Priority,
                    Status = Status,
                    StartDate = StartDate.HasValue ? DateOnly.FromDateTime(StartDate.Value) : null,
                    EndDate = EndDate.HasValue ? DateOnly.FromDateTime(EndDate.Value) : null,
                    AssigneeId = _assigneeId,
                    CoAssignees = CoAssignees,
                    Stage = Stage,
                    DebuggingStatus = DebuggingStatus,
                    Notes = Notes,
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now
                };

                // Сохраняем в базу данных
                await System.Threading.Tasks.Task.Run(() =>
                {
                    _dbContext.Tasks.Add(task);
                    _dbContext.SaveChanges();
                });

                // Удаляем создание уведомления здесь, так как это уже делает триггер в БД

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
    }

    public class OrderPositionViewModel2
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }

    public class UserViewModel
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
    }
}