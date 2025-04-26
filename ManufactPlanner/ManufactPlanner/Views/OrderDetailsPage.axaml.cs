using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using ManufactPlanner.Models;
using ManufactPlanner.ViewModels;

namespace ManufactPlanner.Views;

public partial class OrderDetailsPage : UserControl
{
    public OrderDetailsPage(MainWindowViewModel mainWindowViewModel, PostgresContext dbContext, int orderId)
    {
        InitializeComponent();
        DataContext = new OrderDetailsViewModel(mainWindowViewModel, dbContext, orderId);
    }
}