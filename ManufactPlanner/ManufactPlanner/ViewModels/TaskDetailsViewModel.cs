using System;
using System.Windows.Input;
using ManufactPlanner.Models;
using ReactiveUI;
using System.Reactive;

namespace ManufactPlanner.ViewModels
{
    public class TaskDetailsViewModel : ViewModelBase
    {
        private readonly MainWindowViewModel _mainWindowViewModel;
        private readonly PostgresContext _dbContext;
        private readonly int _taskId;

        // Модель представления для задачи
        public class TaskViewModel
        {
            public int Id { get; set; }
            public string TaskNumber { get; set; }
            public string Name { get; set; }
            public string Description { get; set; }
            public DateTime CreatedAt { get; set; }
            public string Status { get; set; }
            public string Priority { get; set; }
            public DateTime Deadline { get; set; }
            public string AssigneeName { get; set; }
            public string OrderPositionInfo { get; set; }
        }

        // Модель представления заказа
        public class OrderViewModel
        {
            public int Id { get; set; }
            public string OrderNumber { get; set; }
            public string CustomerName { get; set; }
            public DateTime DeliveryDeadline { get; set; }
            public string Status { get; set; }
        }

        // Модель представления связанной задачи
        public class RelatedTaskViewModel
        {
            public int Id { get; set; }
            public string TaskNumber { get; set; }
            public string Name { get; set; }
            public string Status { get; set; }
        }

        private TaskViewModel _task;
        private OrderViewModel _order;
        private RelatedTaskViewModel[] _relatedTasks;

        public TaskViewModel Task
        {
            get => _task;
            set => this.RaiseAndSetIfChanged(ref _task, value);
        }

        public OrderViewModel Order
        {
            get => _order;
            set => this.RaiseAndSetIfChanged(ref _order, value);
        }

        public RelatedTaskViewModel[] RelatedTasks
        {
            get => _relatedTasks;
            set => this.RaiseAndSetIfChanged(ref _relatedTasks, value);
        }

        // Команда для редактирования задачи
        public ICommand EditTaskCommand { get; }

        // Команда для изменения статуса задачи
        public ICommand ChangeStatusCommand { get; }

        public TaskDetailsViewModel(MainWindowViewModel mainWindowViewModel, PostgresContext dbContext, int taskId)
        {
            _mainWindowViewModel = mainWindowViewModel;
            _dbContext = dbContext;
            _taskId = taskId;

            EditTaskCommand = ReactiveCommand.Create(() => {
                // Логика для открытия окна редактирования задачи
            });

            ChangeStatusCommand = ReactiveCommand.Create<string>(status => {
                // Логика для изменения статуса задачи
                // Например: Task.Status = status;
                // Сохранение в БД
            });

            // Загрузка данных задачи
            LoadTaskDetails();
        }

        private void LoadTaskDetails()
        {
            // В реальном приложении здесь будет загрузка данных из БД
            // Например:
            // var taskEntity = _dbContext.Tasks
            //     .Include(t => t.OrderPosition)
            //     .Include(t => t.OrderPosition.Order)
            //     .Include(t => t.Assignee)
            //     .FirstOrDefault(t => t.Id == _taskId);

            // if (taskEntity != null)
            // {
            //     Task = new TaskViewModel
            //     {
            //         Id = taskEntity.Id,
            //         TaskNumber = $"T-{taskEntity.Id}",
            //         Name = taskEntity.Name,
            //         Description = taskEntity.Description,
            //         CreatedAt = taskEntity.CreatedAt,
            //         Status = taskEntity.Status,
            //         Priority = taskEntity.Priority == 1 ? "Высокий" : (taskEntity.Priority == 2 ? "Средний" : "Низкий"),
            //         Deadline = taskEntity.EndDate,
            //         AssigneeName = taskEntity.Assignee.FirstName + " " + taskEntity.Assignee.LastName,
            //         OrderPositionInfo = $"{taskEntity.OrderPosition.Order.OrderNumber} поз. {taskEntity.OrderPosition.PositionNumber}"
            //     };
            // }

            // Тестовые данные для примера
            Task = new TaskViewModel
            {
                Id = _taskId,
                TaskNumber = $"T-{_taskId}",
                Name = "Отладка монтажной схемы",
                Description = "Необходимо выполнить отладку и тестирование монтажной схемы для учебного стенда \"Электрические станции и подстанции\" (заказ ОП-168-24 поз. 1.14).",
                CreatedAt = DateTime.Now.AddDays(-5),
                Status = "В работе",
                Priority = "Высокий",
                Deadline = DateTime.Now.AddDays(8),
                AssigneeName = "Еретин Д.К.",
                OrderPositionInfo = "ОП-168-24 поз. 1.14"
            };

            Order = new OrderViewModel
            {
                Id = 168,
                OrderNumber = "ОП-168/24",
                CustomerName = "ГГПК",
                DeliveryDeadline = DateTime.Parse("17.02.2024"),
                Status = "Активен"
            };

            RelatedTasks = new RelatedTaskViewModel[]
            {
                new RelatedTaskViewModel { Id = 115, TaskNumber = "T-115", Name = "Разработка монтажной схемы", Status = "Готово" },
                new RelatedTaskViewModel { Id = 142, TaskNumber = "T-142", Name = "Тестирование компонентов", Status = "В работе" },
                new RelatedTaskViewModel { Id = 175, TaskNumber = "T-175", Name = "Подготовка документации", Status = "В очереди" }
            };
        }
    }
}
