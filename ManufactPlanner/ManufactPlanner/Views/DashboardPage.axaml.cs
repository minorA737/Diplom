using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using ManufactPlanner.Models;
using ManufactPlanner.ViewModels;
using ScottPlot;
using System.Linq;

namespace ManufactPlanner.Views;

public partial class DashboardPage : UserControl
{
    public DashboardPage(MainWindowViewModel mainWindowViewModel, PostgresContext dbContext)
    {
        InitializeComponent();
        DataContext = new DashboardViewModel(mainWindowViewModel, dbContext);

        // Подписываемся на изменения данных
        if (DataContext is DashboardViewModel vm)
        {
            vm.PropertyChanged += OnViewModelPropertyChanged;
            // Обновляем графики после инициализации
            UpdatePlots(vm);
        }
    }

    private void OnViewModelPropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (sender is DashboardViewModel vm)
        {
            if (e.PropertyName == nameof(DashboardViewModel.TaskCompletionGraphPoints) ||
                e.PropertyName == nameof(DashboardViewModel.TasksByStatus))
            {
                UpdatePlots(vm);
            }
        }
    }

    private void UpdatePlots(DashboardViewModel vm)
    {
        UpdateTaskCompletionPlot(vm);
        UpdateTaskStatusPieChart(vm);
    }

    private void UpdateTaskCompletionPlot(DashboardViewModel vm)
    {
        if (TaskCompletionPlot == null || vm.TaskCompletionGraphPoints == null)
            return;

        TaskCompletionPlot.Plot.Clear();

        if (vm.TaskCompletionGraphPoints.Count > 0)
        {
            // Подготавливаем данные
            double[] x = vm.TaskCompletionGraphPoints.Select((p, i) => (double)i).ToArray();
            double[] yCompleted = vm.TaskCompletionGraphPoints.Select(p => p.Y).ToArray();

            // Создаем линию выполненных задач
            var completedPlot = TaskCompletionPlot.Plot.Add.Scatter(x, yCompleted);
            completedPlot.Color = ScottPlot.Color.FromHex("#00ACC1");
            completedPlot.LineWidth = 3;
            completedPlot.MarkerSize = 8;

            // Добавляем линию плана (если есть данные)
            if (vm.TaskPlanGraphPoints != null && vm.TaskPlanGraphPoints.Count > 0)
            {
                double[] yPlan = vm.TaskPlanGraphPoints.Select(p => p.Y).ToArray();
                var plannedPlot = TaskCompletionPlot.Plot.Add.Scatter(x, yPlan);
                plannedPlot.Color = ScottPlot.Color.FromHex("#9575CD");
                plannedPlot.LineWidth = 3;
                plannedPlot.LineStyle.Pattern = LinePattern.DenselyDashed;
                plannedPlot.MarkerSize = 8;
            }

            // Настройки осей
            TaskCompletionPlot.Plot.Axes.Bottom.MajorTickStyle.Color = ScottPlot.Color.FromHex("#808080");
            TaskCompletionPlot.Plot.Axes.Left.MajorTickStyle.Color = ScottPlot.Color.FromHex("#808080");

            // Добавляем заголовки осей
            TaskCompletionPlot.Plot.XLabel("Месяц");
            TaskCompletionPlot.Plot.YLabel("Кол-во задач");

            // Скрываем легенду на графике
            TaskCompletionPlot.Plot.Legend.IsVisible = false;
        }

        TaskCompletionPlot.Refresh();
    }

    private void UpdateTaskStatusPieChart(DashboardViewModel vm)
    {
        if (TaskStatusPieChart == null || vm.TasksByStatus == null)
            return;

        TaskStatusPieChart.Plot.Clear();

        if (vm.TasksByStatus.Count > 0)
        {
            // Подготавливаем данные для pie chart
            var values = vm.TasksByStatus.Select(s => (double)s.Count).ToArray();
            var colors = vm.TasksByStatus.Select(s => ScottPlot.Color.FromHex(s.ColorHex)).ToArray();

            if (values.Length > 0)
            {
                // Создаем pie chart
                var pie = TaskStatusPieChart.Plot.Add.Pie(values);

                // Применяем цвета к слайсам
                for (int i = 0; i < pie.Slices.Count && i < colors.Length; i++)
                {
                    pie.Slices[i].FillColor = colors[i];
                }

                // Скрываем легенду на диаграмме
                TaskStatusPieChart.Plot.Legend.IsVisible = false;

                // Убираем заголовок с диаграммы
                TaskStatusPieChart.Plot.Title("");
            }
        }

        TaskStatusPieChart.Refresh();
    }
}