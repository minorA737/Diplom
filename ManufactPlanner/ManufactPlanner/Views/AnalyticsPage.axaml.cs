using Avalonia.Controls;
using ManufactPlanner.Models;
using ManufactPlanner.ViewModels;

namespace ManufactPlanner.Views;

public partial class AnalyticsPage : UserControl
{
    public AnalyticsPage(MainWindowViewModel mainWindowViewModel, PostgresContext dbContext)
    {
        InitializeComponent();

        var viewModel = new AnalyticsViewModel(mainWindowViewModel, dbContext);

        // Инициализируем ссылки на графики в ViewModel
        viewModel.TasksProgressPlot = TasksProgressPlot;
        viewModel.TasksStatusPiePlot = StatusPieChart;
        viewModel.EmployeeLoadPlot = EmployeeLoadChart;
        viewModel.ProductionStagesPlot = ProductionStagesPlot;
        viewModel.ProductionTimeline = ProductionTimeline;

        DataContext = viewModel;

        viewModel.InitializeAfterViewsReady();
    }
}