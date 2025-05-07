using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using ManufactPlanner.Models;
using ManufactPlanner.ViewModels;

namespace ManufactPlanner.Views;

public partial class SettingsPage : UserControl
{
    public SettingsPage()
    {
        InitializeComponent();
    }

    // Конструктор с параметрами для реальной работы
    public SettingsPage(MainWindowViewModel mainWindowViewModel, PostgresContext dbContext)
    {
        InitializeComponent();
        DataContext = new SettingsViewModel(mainWindowViewModel, dbContext);
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}