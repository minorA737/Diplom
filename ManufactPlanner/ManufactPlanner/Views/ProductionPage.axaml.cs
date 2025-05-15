using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using ManufactPlanner.Models;
using ManufactPlanner.ViewModels;

namespace ManufactPlanner.Views;

public partial class ProductionPage : UserControl
{
    public ProductionPage(MainWindowViewModel mainWindowViewModel, PostgresContext dbContext)
    {
        InitializeComponent();
        DataContext = new ProductionViewModel(mainWindowViewModel, dbContext);
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}