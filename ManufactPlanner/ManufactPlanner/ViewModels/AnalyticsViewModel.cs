using ManufactPlanner.Models;
using ManufactPlanner.Services;
using ReactiveUI;
using ScottPlot;
using ScottPlot.Avalonia;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using System.Threading.Tasks;
using System.Windows.Input;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace ManufactPlanner.ViewModels
{
    public class AnalyticsViewModel : ViewModelBase
    {
        private readonly MainWindowViewModel _mainWindowViewModel;
        private readonly PostgresContext _dbContext;
        private readonly AnalyticsService _analyticsService;

        #region Свойства для фильтрации данных
        private int _selectedReportType = 0;
        private bool _isRefreshing = false;
        #endregion

        #region Свойства для отображения данных
        private ObservableCollection<EmployeeLoadViewModelAnalytic> _topEmployeesList;
        private AvaPlot _tasksProgressPlot;
        private AvaPlot _tasksStatusPiePlot;
        private AvaPlot _employeeLoadPlot;
        private double _avgTaskDuration = 0;
        private int _onTimeCompletionRate = 0;
        private int _employeeEfficiencyRate = 0;
        #endregion

        #region Публичные свойства для привязки данных
        private DateTimeOffset _startDate = DateTimeOffset.Now.AddMonths(-6);
        private DateTimeOffset _endDate = DateTimeOffset.Now;

        // И соответствующие публичные свойства:
        public DateTimeOffset StartDate
        {
            get => _startDate;
            set
            {
                this.RaiseAndSetIfChanged(ref _startDate, value);
                _ = RefreshAnalyticsDataAsync();
            }
        }

        public DateTimeOffset EndDate
        {
            get => _endDate;
            set
            {
                this.RaiseAndSetIfChanged(ref _endDate, value);
                _ = RefreshAnalyticsDataAsync();
            }
        }

        public int SelectedReportType
        {
            get => _selectedReportType;
            set
            {
                this.RaiseAndSetIfChanged(ref _selectedReportType, value);
                _ = RefreshAnalyticsDataAsync();
            }
        }

        public bool IsRefreshing
        {
            get => _isRefreshing;
            set => this.RaiseAndSetIfChanged(ref _isRefreshing, value);
        }

        public ObservableCollection<EmployeeLoadViewModelAnalytic> TopEmployeesList
        {
            get => _topEmployeesList;
            set => this.RaiseAndSetIfChanged(ref _topEmployeesList, value);
        }

        public AvaPlot TasksProgressPlot
        {
            get => _tasksProgressPlot;
            set => this.RaiseAndSetIfChanged(ref _tasksProgressPlot, value);
        }

        public AvaPlot TasksStatusPiePlot
        {
            get => _tasksStatusPiePlot;
            set => this.RaiseAndSetIfChanged(ref _tasksStatusPiePlot, value);
        }

        public AvaPlot EmployeeLoadPlot
        {
            get => _employeeLoadPlot;
            set => this.RaiseAndSetIfChanged(ref _employeeLoadPlot, value);
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
        #endregion
        
        private bool _showTaskChart = true;
        private bool _showEmployeeChart = false;
        private bool _showProductionChart = false;

        public bool ShowTaskChart
        {
            get => _showTaskChart;
            set => this.RaiseAndSetIfChanged(ref _showTaskChart, value);
        }

        public bool ShowEmployeeChart
        {
            get => _showEmployeeChart;
            set => this.RaiseAndSetIfChanged(ref _showEmployeeChart, value);
        }

        public bool ShowProductionChart
        {
            get => _showProductionChart;
            set => this.RaiseAndSetIfChanged(ref _showProductionChart, value);
        }
        // Добавить поля для диаграмм производства
        private AvaPlot _productionStagesPlot;
        private AvaPlot _productionTimeline;

        public AvaPlot ProductionStagesPlot
        {
            get => _productionStagesPlot;
            set => this.RaiseAndSetIfChanged(ref _productionStagesPlot, value);
        }

        public AvaPlot ProductionTimeline
        {
            get => _productionTimeline;
            set => this.RaiseAndSetIfChanged(ref _productionTimeline, value);
        }
        // Добавить после существующих свойств
        private int _totalTasksCount = 0;
        private int _overdueTasksCount = 0;
        private int _averageEmployeeLoad = 0;
        private int _activeEmployeesCount = 0;
        private int _totalInProduction = 0;
        private int _totalDebugging = 0;
        private int _totalReadyForPackaging = 0;

        public int TotalTasksCount
        {
            get => _totalTasksCount;
            set => this.RaiseAndSetIfChanged(ref _totalTasksCount, value);
        }

        public int OverdueTasksCount
        {
            get => _overdueTasksCount;
            set => this.RaiseAndSetIfChanged(ref _overdueTasksCount, value);
        }

        public int AverageEmployeeLoad
        {
            get => _averageEmployeeLoad;
            set => this.RaiseAndSetIfChanged(ref _averageEmployeeLoad, value);
        }

        public int ActiveEmployeesCount
        {
            get => _activeEmployeesCount;
            set => this.RaiseAndSetIfChanged(ref _activeEmployeesCount, value);
        }

        public int TotalInProduction
        {
            get => _totalInProduction;
            set => this.RaiseAndSetIfChanged(ref _totalInProduction, value);
        }

        public int TotalDebugging
        {
            get => _totalDebugging;
            set => this.RaiseAndSetIfChanged(ref _totalDebugging, value);
        }

        public int TotalReadyForPackaging
        {
            get => _totalReadyForPackaging;
            set => this.RaiseAndSetIfChanged(ref _totalReadyForPackaging, value);
        }


        public AnalyticsViewModel(MainWindowViewModel mainWindowViewModel, PostgresContext dbContext)
        {
            _mainWindowViewModel = mainWindowViewModel;
            _dbContext = dbContext;
            _analyticsService = new AnalyticsService(dbContext);

            _startDate = DateTimeOffset.Now.AddMonths(-6);
            _endDate = DateTimeOffset.Now;

            // Инициализация коллекций
            _topEmployeesList = new ObservableCollection<EmployeeLoadViewModelAnalytic>();

            // НЕ инициализируем графики здесь - они будут переданы из View

            // Инициализация команд
            RefreshDataCommand = ReactiveCommand.CreateFromTask(RefreshAnalyticsDataAsync);
        }
        public void InitializeAfterViewsReady()
        {
            Console.WriteLine("InitializeAfterViewsReady called");
            // Загружаем начальные данные после того, как графики готовы
            _ = RefreshAnalyticsDataAsync();
        }
        public AnalyticsViewModel()
        {
            // Конструктор для дизайнера
            _startDate = DateTimeOffset.Now.AddMonths(-6);
            _endDate = DateTimeOffset.Now;

            _topEmployeesList = new ObservableCollection<EmployeeLoadViewModelAnalytic>
            {
                new EmployeeLoadViewModelAnalytic { EmployeeName = "Вяткин А.И.", LoadPercent = 90, TotalTasks = 10, CompletedTasks = 7, InProgressTasks = 2, LoadBarWidth = 288 },
                new EmployeeLoadViewModelAnalytic { EmployeeName = "Киреев Б.В.", LoadPercent = 70, TotalTasks = 8, CompletedTasks = 5, InProgressTasks = 1, LoadBarWidth = 224 },
                new EmployeeLoadViewModelAnalytic { EmployeeName = "Турушев С.М.", LoadPercent = 80, TotalTasks = 12, CompletedTasks = 8, InProgressTasks = 2, LoadBarWidth = 256 },
                new EmployeeLoadViewModelAnalytic { EmployeeName = "Еретин Д.К.", LoadPercent = 60, TotalTasks = 5, CompletedTasks = 3, InProgressTasks = 0, LoadBarWidth = 192 },
                new EmployeeLoadViewModelAnalytic { EmployeeName = "Шулепов И.Л.", LoadPercent = 95, TotalTasks = 15, CompletedTasks = 12, InProgressTasks = 2, LoadBarWidth = 304 }
            };

            InitializePlots();
        }

        private void InitializePlots()
        {
            // Инициализация графика прогресса задач
            _tasksProgressPlot = new AvaPlot();
            var plot1 = _tasksProgressPlot.Plot;

            // Инициализация круговой диаграммы статусов
            _tasksStatusPiePlot = new AvaPlot();
            var plot2 = _tasksStatusPiePlot.Plot;

            // Инициализация графика загрузки сотрудников
            _employeeLoadPlot = new AvaPlot();
            var plot3 = _employeeLoadPlot.Plot;

            _productionStagesPlot = new AvaPlot();
            _productionTimeline = new AvaPlot();

            Console.WriteLine($"Initializing plots - TasksProgressPlot: {_tasksProgressPlot != null}");
            Console.WriteLine($"TasksStatusPiePlot: {_tasksStatusPiePlot != null}");
            Console.WriteLine($"EmployeeLoadPlot: {_employeeLoadPlot != null}");
        }
        private void UpdateProductionCharts(Dictionary<string, object> data)
        {
            if (!data.ContainsKey("productionData")) return;

            var productionData = data["productionData"] as Dictionary<string, object>;
            if (!productionData.ContainsKey("productionStats")) return;

            var stats = productionData["productionStats"] as List<Dictionary<string, object>>;
            if (stats == null) return;

            // Обновляем диаграмму этапов производства
            UpdateProductionStagesChart(stats);

            // Обновляем временную диаграмму
            UpdateProductionTimelineChart(productionData);
        }
        private void UpdateProductionTimelineChart(Dictionary<string, object> productionData)
        {
            // Временно оставляем пустым, можно реализовать позже
            _productionTimeline.Plot.Clear();
            _productionTimeline.Plot.Title("Временная линия производства");
            _productionTimeline.Refresh();
        }
        private void UpdateProductionStagesChart(List<Dictionary<string, object>> stats)
        {
            _productionStagesPlot.Plot.Clear();

            if (!stats.Any()) return;

            var months = stats.Select(s => s["month"].ToString()).ToArray();
            var inProduction = stats.Select(s => Convert.ToDouble(s["inProduction"])).ToArray();
            var debugging = stats.Select(s => Convert.ToDouble(s["debugging"])).ToArray();
            var readyForPackaging = stats.Select(s => Convert.ToDouble(s["readyForPackaging"])).ToArray();
            var completed = stats.Select(s => Convert.ToDouble(s["completed"])).ToArray();

            var positions = Enumerable.Range(0, months.Length).Select(i => (double)i).ToArray();

            // Создаем отдельные группы баров
            var barInProduction = _productionStagesPlot.Plot.Add.Bars(positions, inProduction);
            var barDebugging = _productionStagesPlot.Plot.Add.Bars(positions, debugging);
            var barReady = _productionStagesPlot.Plot.Add.Bars(positions, readyForPackaging);
            var barCompleted = _productionStagesPlot.Plot.Add.Bars(positions, completed);

            // Настраиваем цвета для каждой группы
            foreach (var bar in barInProduction.Bars)
                bar.FillColor = ScottPlot.Color.FromHex("#9575CD");

            foreach (var bar in barDebugging.Bars)
                bar.FillColor = ScottPlot.Color.FromHex("#4CAF9D");

            foreach (var bar in barReady.Bars)
                bar.FillColor = ScottPlot.Color.FromHex("#FFB74D");

            foreach (var bar in barCompleted.Bars)
                bar.FillColor = ScottPlot.Color.FromHex("#00ACC1");

            // Настраиваем легенду
            barInProduction.LegendText = "В производстве";
            barDebugging.LegendText = "Отладка";
            barReady.LegendText = "Готово к упаковке";
            barCompleted.LegendText = "Завершено";

            // Настройка осей
            _productionStagesPlot.Plot.Axes.Bottom.SetTicks(positions, months);
            _productionStagesPlot.Plot.Axes.Bottom.TickLabelStyle.Rotation = 45;
            _productionStagesPlot.Plot.ShowLegend();
            _productionStagesPlot.Plot.Title("Этапы производства по месяцам");
            _productionStagesPlot.Plot.XLabel("Месяц");
            _productionStagesPlot.Plot.YLabel("Количество изделий");

            _productionStagesPlot.Refresh();
        }
        private async System.Threading.Tasks.Task RefreshAnalyticsDataAsync()
        {
            try
            {
                Console.WriteLine($"RefreshAnalyticsDataAsync started. SelectedReportType: {SelectedReportType}");
                Console.WriteLine($"Graphs availability - TasksProgressPlot: {_tasksProgressPlot != null}");

                IsRefreshing = true;

                var reportType = GetReportTypeString();
                Console.WriteLine($"Report type: {reportType}");

                IsRefreshing = true;

                var startDateTime = StartDate.DateTime;
                var endDateTime = EndDate.DateTime;

                var analyticsData = await _analyticsService.GetComprehensiveAnalyticsAsync(startDateTime, endDateTime, reportType);

                // Обновляем данные в зависимости от типа отчета
                switch (reportType)
                {
                    case "tasks":
                        await UpdateTasksProgressChart(analyticsData);
                        UpdateTasksStatusPieChart(analyticsData);
                        break;
                    case "employees":
                        UpdateEmployeesList(analyticsData);
                        UpdateEmployeeLoadChart();
                        // Обновляем метрики для сотрудников
                        if (analyticsData.ContainsKey("employeeData"))
                        {
                            var employeeData = analyticsData["employeeData"] as List<Dictionary<string, object>>;
                            if (employeeData != null)
                            {
                                ActiveEmployeesCount = employeeData.Count;
                                if (employeeData.Any())
                                {
                                    AverageEmployeeLoad = (int)employeeData.Average(e => Convert.ToInt32(e["loadPercent"]));
                                }
                            }
                        }
                        break;
                    case "production":
                        UpdateProductionCharts(analyticsData);
                        // Обновляем метрики производства
                        if (analyticsData.ContainsKey("productionData"))
                        {
                            var productionData = analyticsData["productionData"] as Dictionary<string, object>;
                            if (productionData != null)
                            {
                                if (productionData.ContainsKey("totalInProduction"))
                                    TotalInProduction = Convert.ToInt32(productionData["totalInProduction"]);
                                if (productionData.ContainsKey("totalDebugging"))
                                    TotalDebugging = Convert.ToInt32(productionData["totalDebugging"]);
                                if (productionData.ContainsKey("totalReadyForPackaging"))
                                    TotalReadyForPackaging = Convert.ToInt32(productionData["totalReadyForPackaging"]);
                            }
                        }
                        break;
                }

                // Обновляем общие ключевые метрики для всех типов отчетов
                UpdateKeyMetrics(analyticsData);

                IsRefreshing = false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error refreshing analytics data: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                IsRefreshing = false;
            }
        }

        private void UpdateKeyMetrics(Dictionary<string, object> data)
        {
            if (data.ContainsKey("avgTaskDuration"))
                AvgTaskDuration = Convert.ToDouble(data["avgTaskDuration"]);

            if (data.ContainsKey("onTimeCompletionRate"))
                OnTimeCompletionRate = Convert.ToInt32(data["onTimeCompletionRate"]);

            if (data.ContainsKey("employeeEfficiencyRate"))
                EmployeeEfficiencyRate = Convert.ToInt32(data["employeeEfficiencyRate"]);

            // Добавить новые метрики
            if (data.ContainsKey("totalTasks"))
                TotalTasksCount = Convert.ToInt32(data["totalTasks"]);

            if (data.ContainsKey("overduePercent") && data.ContainsKey("totalTasks"))
            {
                var overduePercent = Convert.ToDouble(data["overduePercent"]);
                var totalTasks = Convert.ToInt32(data["totalTasks"]);
                OverdueTasksCount = (int)(totalTasks * overduePercent / 100);
            }
        }

        private void UpdateEmployeesList(Dictionary<string, object> data)
        {
            if (!data.ContainsKey("employeeData")) return;

            var employeeData = data["employeeData"] as List<Dictionary<string, object>>;
            if (employeeData == null) return;

            TopEmployeesList.Clear();

            foreach (var emp in employeeData)
            {
                var loadPercent = Convert.ToInt32(emp["loadPercent"]);
                var loadBarWidth = Math.Max(0, Math.Min(320, loadPercent * 320 / 100));

                TopEmployeesList.Add(new EmployeeLoadViewModelAnalytic
                {
                    EmployeeName = emp["name"].ToString(),
                    LoadPercent = loadPercent,
                    LoadBarWidth = loadBarWidth,
                    TotalTasks = Convert.ToInt32(emp["totalTasks"]),
                    CompletedTasks = Convert.ToInt32(emp["completedTasks"]),
                    InProgressTasks = Convert.ToInt32(emp["inProgressTasks"])
                });
            }
        }

        private async System.Threading.Tasks.Task UpdateTasksProgressChart(Dictionary<string, object> data)
        {
            if (!data.ContainsKey("progressData")) return;

            var progressData = data["progressData"] as Dictionary<string, object>;
            if (!progressData.ContainsKey("monthlyData")) return;

            var monthlyData = progressData["monthlyData"] as Dictionary<string, Dictionary<string, int>>;
            if (monthlyData == null) return;

            // Очищаем график
            _tasksProgressPlot.Plot.Clear();

            // Подготавливаем данные для графика
            var months = monthlyData.Keys.ToArray();
            var completedValues = monthlyData.Values.Select(v => (double)v["completed"]).ToArray();
            var plannedValues = monthlyData.Values.Select(v => (double)v["planned"]).ToArray();
            var positions = Enumerable.Range(0, months.Length).Select(i => (double)i).ToArray();

            // Создаем линии на графике
            if (completedValues.Length > 0)
            {
                var completedPlot = _tasksProgressPlot.Plot.Add.Scatter(positions, completedValues);
                completedPlot.Color = ScottPlot.Color.FromHex("#00ACC1");
                completedPlot.LineWidth = 3;
                completedPlot.LegendText = "Выполнено";
                completedPlot.MarkerSize = 8;
            }

            if (plannedValues.Length > 0)
            {
                var plannedPlot = _tasksProgressPlot.Plot.Add.Scatter(positions, plannedValues);
                plannedPlot.Color = ScottPlot.Color.FromHex("#9575CD");
                plannedPlot.LineWidth = 3;
                plannedPlot.LineStyle.Pattern = LinePattern.DenselyDashed;
                plannedPlot.LegendText = "План";
                plannedPlot.MarkerSize = 8;
            }

            // Настройка осей
            _tasksProgressPlot.Plot.Axes.Bottom.SetTicks(positions, months);
            _tasksProgressPlot.Plot.Axes.Bottom.MajorTickStyle.Color = ScottPlot.Color.FromHex("#808080");
            _tasksProgressPlot.Plot.Axes.Left.MajorTickStyle.Color = ScottPlot.Color.FromHex("#808080");

            // Добавляем заголовки и легенду
            _tasksProgressPlot.Plot.Title("Выполнение задач по времени");
            _tasksProgressPlot.Plot.XLabel("Месяц");
            _tasksProgressPlot.Plot.YLabel("Количество задач");
            _tasksProgressPlot.Plot.ShowLegend();

            _tasksProgressPlot.Refresh();
        }

        private void UpdateTasksStatusPieChart(Dictionary<string, object> data)
        {

            Console.WriteLine($"UpdateTasksStatusPieChart called. _tasksStatusPiePlot is null: {_tasksStatusPiePlot == null}");

            if (_tasksStatusPiePlot == null)
            {
                Console.WriteLine("TasksStatusPieChart is null, returning");
                return;
            }

            _tasksStatusPiePlot.Plot.Clear();

            // Получаем данные о статусах
            var values = new List<double>();
            var labels = new List<string>();
            var colors = new List<ScottPlot.Color>();

            if (data == null || !data.Any())
            {
                // Показать пустую диаграмму с заглушкой
                values.Add(100);
                labels.Add("Нет данных");
                colors.Add(ScottPlot.Color.FromHex("#E0E0E0"));
            }
            else
            {
                var completedPercent = Convert.ToDouble(data.GetValueOrDefault("completedPercent", 0));
                var inProgressPercent = Convert.ToDouble(data.GetValueOrDefault("inProgressPercent", 0));
                var pendingPercent = Convert.ToDouble(data.GetValueOrDefault("pendingPercent", 0));
                var overduePercent = Convert.ToDouble(data.GetValueOrDefault("overduePercent", 0));
                var otherPercent = Convert.ToDouble(data.GetValueOrDefault("otherPercent", 0));

                if (completedPercent > 0)
                {
                    values.Add(completedPercent);
                    labels.Add($"Выполнено {completedPercent:F1}%");
                    colors.Add(ScottPlot.Color.FromHex("#00ACC1"));
                }
                if (inProgressPercent > 0)
                {
                    values.Add(inProgressPercent);
                    labels.Add($"В процессе {inProgressPercent:F1}%");
                    colors.Add(ScottPlot.Color.FromHex("#4CAF9D"));
                }
                if (pendingPercent > 0)
                {
                    values.Add(pendingPercent);
                    labels.Add($"В очереди {pendingPercent:F1}%");
                    colors.Add(ScottPlot.Color.FromHex("#FFB74D"));
                }
                if (overduePercent > 0)
                {
                    values.Add(overduePercent);
                    labels.Add($"Просрочено {overduePercent:F1}%");
                    colors.Add(ScottPlot.Color.FromHex("#FF7043"));
                }
                if (otherPercent > 0)
                {
                    values.Add(otherPercent);
                    labels.Add($"Отменено {otherPercent:F1}%");
                    colors.Add(ScottPlot.Color.FromHex("#9575CD"));
                }

                if (values.Count > 0)
                {
                    // Создаем pie chart
                    var pie = _tasksStatusPiePlot.Plot.Add.Pie(values.ToArray());

                    // Применяем цвета
                    for (int i = 0; i < pie.Slices.Count && i < colors.Count; i++)
                    {
                        pie.Slices[i].FillColor = colors[i];
                    }

                    // Добавляем легенду вместо меток на каждом секторе
                    _tasksStatusPiePlot.Plot.Legend.IsVisible = true;
                    _tasksStatusPiePlot.Plot.Legend.Location = Alignment.UpperRight;

                    _tasksStatusPiePlot.Plot.Title("Распределение задач по статусам");
                }

                _tasksStatusPiePlot.Refresh();
            }
        }

        private void UpdateEmployeeLoadChart()
        {
            Console.WriteLine($"UpdateTasksProgressChart called. _tasksProgressPlot is null: {_tasksProgressPlot == null}");

            if (_tasksProgressPlot == null)
            {
                Console.WriteLine("TasksProgressChart is null, returning");
                return;
            }


            _employeeLoadPlot.Plot.Clear();

            if (!TopEmployeesList.Any()) return;

            var names = TopEmployeesList.Select(e => e.EmployeeName).Reverse().ToArray();
            var loads = TopEmployeesList.Select(e => (double)e.LoadPercent).Reverse().ToArray();
            var positions = Enumerable.Range(0, names.Length).Select(i => (double)i).ToArray();

            // Создаем горизонтальные полосы
            var bars = _employeeLoadPlot.Plot.Add.Bars(loads, positions);
            bars.Horizontal = true; // Делаем горизонтальными

            // Настраиваем цвета для каждого бара
            for (int i = 0; i < bars.Bars.Count; i++)
            {
                bars.Bars[i].FillColor = ScottPlot.Color.FromHex("#00ACC1");
            }

            // Настройка осей
            _employeeLoadPlot.Plot.Axes.Left.SetTicks(positions, names);
            _employeeLoadPlot.Plot.Axes.Bottom.Min = 0;
            _employeeLoadPlot.Plot.Axes.Bottom.Max = 100;

            // Заголовки
            _employeeLoadPlot.Plot.Title("Загрузка сотрудников");
            _employeeLoadPlot.Plot.YLabel("Сотрудники");
            _employeeLoadPlot.Plot.XLabel("Загрузка (%)");

            _employeeLoadPlot.Refresh();
        }

        private string GetReportTypeString()
        {
            switch (SelectedReportType)
            {
                case 0:
                    ShowTaskChart = true;
                    ShowEmployeeChart = false;
                    ShowProductionChart = false;
                    return "tasks";
                case 1:
                    ShowTaskChart = false;
                    ShowEmployeeChart = true;
                    ShowProductionChart = false;
                    return "employees";
                case 2:
                    ShowTaskChart = false;
                    ShowEmployeeChart = false;
                    ShowProductionChart = true;
                    return "production";
                default:
                    ShowTaskChart = true;
                    ShowEmployeeChart = false;
                    ShowProductionChart = false;
                    return "tasks";
            }
        }
    }

    // Класс для отображения информации о загрузке сотрудников
    public class EmployeeLoadViewModelAnalytic : ViewModelBase
    {
        public string EmployeeName { get; set; }
        public int LoadPercent { get; set; }
        public int LoadBarWidth { get; set; }
        public int TotalTasks { get; set; }
        public int CompletedTasks { get; set; }
        public int InProgressTasks { get; set; }
    }
}