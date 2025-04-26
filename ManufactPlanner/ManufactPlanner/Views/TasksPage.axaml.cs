using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using ManufactPlanner.Models;
using ManufactPlanner.ViewModels;

namespace ManufactPlanner.Views;

public partial class TasksPage : UserControl
{
    public TasksPage(MainWindowViewModel mainWindowViewModel, PostgresContext dbContext)
    {
        InitializeComponent();
        DataContext = new TasksViewModel(mainWindowViewModel, dbContext);
    }
}