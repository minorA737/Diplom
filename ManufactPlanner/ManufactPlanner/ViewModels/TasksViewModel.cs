using System;
using System.Collections.ObjectModel;
using System.Windows.Input;
using ManufactPlanner.Models;
using ReactiveUI;
using System.Reactive;

namespace ManufactPlanner.ViewModels
{
    public class TasksViewModel : ViewModelBase
    {
        private readonly MainWindowViewModel _mainWindowViewModel;
        private readonly PostgresContext _dbContext;

        private ViewMode _currentViewMode = ViewMode.Table;

        public enum ViewMode
        {
            Table,
            Kanban,
            Calendar
        }

        public ViewMode CurrentViewMode
        {
            get => _currentViewMode;
            set => this.RaiseAndSetIfChanged(ref _currentViewMode, value);
        }

        public bool IsTableViewActive => CurrentViewMode == ViewMode.Table;
        public bool IsKanbanViewActive => CurrentViewMode == ViewMode.Kanban;
        public bool IsCalendarViewActive => CurrentViewMode == ViewMode.Calendar;

        // Команды для переключения между представлениями
        public ICommand SwitchToTableViewCommand { get; }
        public ICommand SwitchToKanbanViewCommand { get; }
        public ICommand SwitchToCalendarViewCommand { get; }

        // Команда для создания новой задачи
        public ICommand CreateTaskCommand { get; }

        // Команда для открытия детальной информации о задаче
        public ICommand OpenTaskDetailsCommand { get; }

        public TasksViewModel(MainWindowViewModel mainWindowViewModel, PostgresContext dbContext)
        {
            _mainWindowViewModel = mainWindowViewModel;
            _dbContext = dbContext;

            // Инициализация команд
            SwitchToTableViewCommand = ReactiveCommand.Create(() => {
                CurrentViewMode = ViewMode.Table;
                this.RaisePropertyChanged(nameof(IsTableViewActive));
                this.RaisePropertyChanged(nameof(IsKanbanViewActive));
                this.RaisePropertyChanged(nameof(IsCalendarViewActive));
            });

            SwitchToKanbanViewCommand = ReactiveCommand.Create(() => {
                CurrentViewMode = ViewMode.Kanban;
                this.RaisePropertyChanged(nameof(IsTableViewActive));
                this.RaisePropertyChanged(nameof(IsKanbanViewActive));
                this.RaisePropertyChanged(nameof(IsCalendarViewActive));
            });

            SwitchToCalendarViewCommand = ReactiveCommand.Create(() => {
                CurrentViewMode = ViewMode.Calendar;
                this.RaisePropertyChanged(nameof(IsTableViewActive));
                this.RaisePropertyChanged(nameof(IsKanbanViewActive));
                this.RaisePropertyChanged(nameof(IsCalendarViewActive));
            });

            CreateTaskCommand = ReactiveCommand.Create(() => {
                // Логика создания новой задачи
                // Можно открыть диалоговое окно или перейти на страницу создания задачи
            });

            OpenTaskDetailsCommand = ReactiveCommand.Create<int>(taskId => {
                _mainWindowViewModel.NavigateToTaskDetails(taskId);
            });

            // Загрузка данных из БД
            LoadTasks();
        }

        private void LoadTasks()
        {
            // В реальном приложении здесь будет загрузка задач из базы данных
            // Например:
            // var tasks = _dbContext.Tasks.Include(t => t.OrderPosition).Include(t => t.Assignee).ToList();
            // Преобразование данных в модель представления
        }
    }
}
