using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using ManufactPlanner.Models;
using ManufactPlanner.ViewModels;
using static ManufactPlanner.ViewModels.TasksViewModel;

namespace ManufactPlanner.Views;

public partial class OrderDetailsPage : UserControl
{
    public OrderDetailsPage(MainWindowViewModel mainWindowViewModel, PostgresContext dbContext, int orderId)
    {
        InitializeComponent();
        var viewModel = new OrderDetailsViewModel(mainWindowViewModel, dbContext, orderId);

        // ”станавливаем родительское окно дл€ диалогов
        viewModel.SetParentWindow(AppWindows.MainWindow);

        DataContext = viewModel;
    }
}