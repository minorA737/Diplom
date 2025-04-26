using System;
using System.Windows.Input;
using ManufactPlanner.Models;
using ReactiveUI;
using System.Reactive;
using System.Collections.ObjectModel;

namespace ManufactPlanner.ViewModels
{
    public class TaskDetailsViewModel : ViewModelBase
    {
        private readonly MainWindowViewModel _mainWindowViewModel;
        private readonly PostgresContext _dbContext;

        private string _taskId = "T-128";
        private string _taskName = "Отладка монтажной схемы";
        private string _createdDate = "Создана 05.04.2025";
        private string _status = "В работе";
        private string _statusColor = "#FFB74D";
        private string _priority = "Приоритет: Высокий";
        private string _priorityColor = "#FF7043";
        private string _deadline = "Срок: 18.04.2025";
        private string _description = "Необходимо выполнить отладку и тестирование монтажной схемы для учебного стенда \"Электрические станции и подстанции\" (заказ ОП-168-24 поз. 1.14).\n\nПроверить работоспособность всех компонентов и соответствие монтажной схемы принципиальной электрической схеме. Выявить и устранить возможные несоответствия и проблемы.\n\nПосле завершения отладки подготовить отчет о проведенных работах и внесенных изменениях.";
        private string _assignee = "Еретин Д.К.";
        private string _orderPosition = "ОП-168-24 поз. 1.14";
        private string _stageStatus = "Тестирование";
        private string _notes = "Необходимо согласовать изменения с конструкторским отделом.";
        private string _customer = "ГГПК";
        private string _orderDeadline = "17.02.2024";
        private string _orderStatus = "Активен";

        private ObservableCollection<RelatedTaskViewModel> _relatedTasks;

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

        public ICommand NavigateToTasksCommand { get; }
        public ICommand EditTaskCommand { get; }

        public TaskDetailsViewModel(MainWindowViewModel mainWindowViewModel, PostgresContext dbContext, int taskId)
        {
            _mainWindowViewModel = mainWindowViewModel;
            _dbContext = dbContext;

            NavigateToTasksCommand = ReactiveCommand.Create(NavigateToTasks);
            EditTaskCommand = ReactiveCommand.Create(EditTask);

            LoadTaskDetails(taskId);
        }

        public TaskDetailsViewModel()
        {
            // Конструктор для дизайнера
            NavigateToTasksCommand = ReactiveCommand.Create(NavigateToTasks);
            EditTaskCommand = ReactiveCommand.Create(EditTask);

            LoadTestData();
        }

        private void LoadTaskDetails(int taskId)
        {
            // В реальном приложении здесь будет загрузка данных о задаче из БД
            // Для примера используем тестовые данные
            LoadTestData();
        }

        private void LoadTestData()
        {
            // Пример связанных задач
            RelatedTasks = new ObservableCollection<RelatedTaskViewModel>
            {
                new RelatedTaskViewModel { Id = 1, Name = "T-115: Разработка монтажной схемы", Status = "Готово", StatusColor = "#4CAF9D" },
                new RelatedTaskViewModel { Id = 2, Name = "T-142: Тестирование компонентов", Status = "В работе", StatusColor = "#FFB74D" },
                new RelatedTaskViewModel { Id = 3, Name = "T-175: Подготовка документации", Status = "В очереди", StatusColor = "#00ACC1" }
            };
        }

        private void NavigateToTasks()
        {
            _mainWindowViewModel.NavigateToTasks();
        }

        private void EditTask()
        {
            // Логика редактирования задачи
        }
    }

    public class RelatedTaskViewModel : ViewModelBase
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Status { get; set; }
        public string StatusColor { get; set; }
    }
}