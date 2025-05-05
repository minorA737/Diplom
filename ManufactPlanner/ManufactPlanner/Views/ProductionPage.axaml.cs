using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using ManufactPlanner.Models;
using ManufactPlanner.ViewModels;

namespace ManufactPlanner.Views;

public partial class ProductionPage : UserControl
{
    private readonly MainWindowViewModel _mainWindowViewModel;
    private readonly PostgresContext _dbContext;

    public ProductionPage(MainWindowViewModel mainWindowViewModel, PostgresContext dbContext)
    {
        InitializeComponent();
        _mainWindowViewModel = mainWindowViewModel;
        _dbContext = dbContext;

        // Подписываемся на событие загрузки контрола
        this.Loaded += DocumentationPage_Loaded;
    }

    private void DocumentationPage_Loaded(object sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        // Получаем окно только после полной загрузки контрола
        var window = TopLevel.GetTopLevel(this) as Window;
        DataContext = new ProductionViewModel(_mainWindowViewModel, _dbContext, window);
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}