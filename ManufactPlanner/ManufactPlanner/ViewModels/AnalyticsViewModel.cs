using ManufactPlanner.Models;
using ManufactPlanner.Services;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;
using System.Windows.Input;

namespace ManufactPlanner.ViewModels
{
    public class AnalyticsViewModel : ViewModelBase
    {
        private readonly MainWindowViewModel _mainWindowViewModel;
        private readonly PostgresContext _dbContext;
        private readonly DataLensService _dataLensService;

        #region Свойства для фильтрации данных
        private DateTime _startDate = DateTime.Now.AddMonths(-6);
        private DateTime _endDate = DateTime.Now;
        private int _selectedReportType = 0;
        private bool _isRefreshing = false;
        private bool _isEmbeddedDashboardVisible = false;
        private string _currentDashboardUrl;
        #endregion

        #region Свойства для отображения данных
        private Dictionary<string, object> _analyticsData;
        //private ObservableCollection<EmployeeLoadViewModel> _topEmployeesList;
        private int _completedTasksPercent = 42;
        private int _inProgressTasksPercent = 28;
        private int _pendingTasksPercent = 15;
        private int _overdueTasksPercent = 8;
        private int _otherTasksPercent = 7;
        private double _avgTaskDuration = 3.2;
        private int _onTimeCompletionRate = 78;
        private int _employeeEfficiencyRate = 85;
        #endregion

        #region Публичные свойства для привязки данных
        public DateTime StartDate
        {
            get => _startDate;
            set => this.RaiseAndSetIfChanged(ref _startDate, value);
        }

        public DateTime EndDate
        {
            get => _endDate;
            set => this.RaiseAndSetIfChanged(ref _endDate, value);
        }

        public int SelectedReportType
        {
            get => _selectedReportType;
            set => this.RaiseAndSetIfChanged(ref _selectedReportType, value);
        }

        public bool IsRefreshing
        {
            get => _isRefreshing;
            set => this.RaiseAndSetIfChanged(ref _isRefreshing, value);
        }

        public bool IsEmbeddedDashboardVisible
        {
            get => _isEmbeddedDashboardVisible;
            set => this.RaiseAndSetIfChanged(ref _isEmbeddedDashboardVisible, value);
        }

        public string CurrentDashboardUrl
        {
            get => _currentDashboardUrl;
            set => this.RaiseAndSetIfChanged(ref _currentDashboardUrl, value);
        }

        //public ObservableCollection<EmployeeLoadViewModel> TopEmployeesList
        //{
        //    get => _topEmployeesList;
        //    set => this.RaiseAndSetIfChanged(ref _topEmployeesList, value);
        //}

        public int CompletedTasksPercent
        {
            get => _completedTasksPercent;
            set => this.RaiseAndSetIfChanged(ref _completedTasksPercent, value);
        }

        public int InProgressTasksPercent
        {
            get => _inProgressTasksPercent;
            set => this.RaiseAndSetIfChanged(ref _inProgressTasksPercent, value);
        }

        public int PendingTasksPercent
        {
            get => _pendingTasksPercent;
            set => this.RaiseAndSetIfChanged(ref _pendingTasksPercent, value);
        }

        public int OverdueTasksPercent
        {
            get => _overdueTasksPercent;
            set => this.RaiseAndSetIfChanged(ref _overdueTasksPercent, value);
        }

        public int OtherTasksPercent
        {
            get => _otherTasksPercent;
            set => this.RaiseAndSetIfChanged(ref _otherTasksPercent, value);
        }

        public double AvgTaskDuration
        {
            get => _avgTaskDuration;
            set => this.RaiseAndSetIfChanged(ref _avgTaskDuration, value);
        }

        public int OnTimeCompletionRate
        {
            get => _onTimeCompletionRate;
            set => this.RaiseAndSetIfChanged(ref _onTimeCompletionRate, value);
        }

        public int EmployeeEfficiencyRate
        {
            get => _employeeEfficiencyRate;
            set => this.RaiseAndSetIfChanged(ref _employeeEfficiencyRate, value);
        }
        #endregion

        #region Команды
        public ICommand RefreshDataCommand { get; }
        public ICommand ExportToDataLensCommand { get; }
        public ICommand ToggleDashboardCommand { get; }
        #endregion

        public AnalyticsViewModel(MainWindowViewModel mainWindowViewModel, PostgresContext dbContext)
        {
            _mainWindowViewModel = mainWindowViewModel;
            _dbContext = dbContext;
            _dataLensService = DataLensService.Instance;

            // Инициализация коллекции для сотрудников
            //_topEmployeesList = new ObservableCollection<EmployeeLoadViewModel>();

            // Инициализация команд
            RefreshDataCommand = ReactiveCommand.CreateFromTask(RefreshAnalyticsDataAsync);
            ExportToDataLensCommand = ReactiveCommand.CreateFromTask(ExportToDataLensAsync);
            ToggleDashboardCommand = ReactiveCommand.Create(() =>
            {
                IsEmbeddedDashboardVisible = !IsEmbeddedDashboardVisible;
            });

            // Получаем URL дашборда из сервиса
            CurrentDashboardUrl = _dataLensService.GetDashboardUrl();

            // Загружаем начальные данные
            _ = RefreshAnalyticsDataAsync();
        }

        public AnalyticsViewModel()
        {
            //// Конструктор для дизайнера
            //_topEmployeesList = new ObservableCollection<EmployeeLoadViewModel>
            //{
            //    new EmployeeLoadViewModel { EmployeeName = "Вяткин А.И.", LoadPercent = 90 },
            //    new EmployeeLoadViewModel { EmployeeName = "Киреев Б.В.", LoadPercent = 70 },
            //    new EmployeeLoadViewModel { EmployeeName = "Турушев С.М.", LoadPercent = 80 },
            //    new EmployeeLoadViewModel { EmployeeName = "Еретин Д.К.", LoadPercent = 60 },
            //    new EmployeeLoadViewModel { EmployeeName = "Шулепов И.Л.", LoadPercent = 95 }
            //};
        }

        private async System.Threading.Tasks.Task RefreshAnalyticsDataAsync()
        {
            try
            {
                IsRefreshing = true;

                // Получаем данные от сервиса
                _analyticsData = _dataLensService.GetAnalyticsData(StartDate, EndDate);

                // Обновляем свойства для графиков
                UpdatePropertiesFromAnalyticsData();

                // Загружаем список сотрудников
                UpdateEmployeesList();

                // Если нет ошибок, установим успешное завершение
                IsRefreshing = false;
            }
            catch (Exception ex)
            {
                // Обработка ошибок
                Console.WriteLine($"Error refreshing analytics data: {ex.Message}");
                IsRefreshing = false;
            }
        }

        private async System.Threading.Tasks.Task ExportToDataLensAsync()
        {
            try
            {
                IsRefreshing = true;

                string reportType = GetReportTypeString();

                // Вызываем сервис для экспорта данных
                await _dataLensService.ExportDataToCsv(StartDate, EndDate, reportType);

                // Обновляем URL дашборда (т.к. он мог измениться после обновления данных)
                CurrentDashboardUrl = _dataLensService.GetDashboardUrl();

                IsRefreshing = false;

                // После экспорта автоматически показываем дашборд
                IsEmbeddedDashboardVisible = true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error exporting to DataLens: {ex.Message}");
                IsRefreshing = false;
            }
        }

        private string GetReportTypeString()
        {
            switch (SelectedReportType)
            {
                case 0: return "tasks";
                case 1: return "employees";
                case 2: return "production";
                default: return "tasks";
            }
        }

        private void UpdatePropertiesFromAnalyticsData()
        {
            if (_analyticsData == null) return;

            // Обновляем проценты для круговой диаграммы
            if (_analyticsData.ContainsKey("completedPercent"))
                CompletedTasksPercent = Convert.ToInt32(_analyticsData["completedPercent"]);

            if (_analyticsData.ContainsKey("inProgressPercent"))
                InProgressTasksPercent = Convert.ToInt32(_analyticsData["inProgressPercent"]);

            if (_analyticsData.ContainsKey("pendingPercent"))
                PendingTasksPercent = Convert.ToInt32(_analyticsData["pendingPercent"]);

            if (_analyticsData.ContainsKey("overduePercent"))
                OverdueTasksPercent = Convert.ToInt32(_analyticsData["overduePercent"]);

            if (_analyticsData.ContainsKey("otherPercent"))
                OtherTasksPercent = Convert.ToInt32(_analyticsData["otherPercent"]);

            // Обновляем метрики
            if (_analyticsData.ContainsKey("avgTaskDuration"))
                AvgTaskDuration = Convert.ToDouble(_analyticsData["avgTaskDuration"]);

            if (_analyticsData.ContainsKey("onTimeCompletionRate"))
                OnTimeCompletionRate = Convert.ToInt32(_analyticsData["onTimeCompletionRate"]);

            // Дополнительные данные можно добавить по мере необходимости
        }

        private void UpdateEmployeesList()
        {
            if (_analyticsData == null || !_analyticsData.ContainsKey("employeeData")) return;

            var employeeData = _analyticsData["employeeData"] as List<Dictionary<string, object>>;
            if (employeeData == null) return;

            //TopEmployeesList.Clear();

            //foreach (var emp in employeeData)
            //{
            //    TopEmployeesList.Add(new EmployeeLoadViewModel
            //    {
            //        EmployeeName = emp["name"].ToString(),
            //        LoadPercent = Convert.ToInt32(emp["loadPercent"])
            //    });
            //}
        }
    }
}