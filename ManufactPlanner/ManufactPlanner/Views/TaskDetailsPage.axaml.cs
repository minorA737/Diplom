using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using ManufactPlanner.Models;
using ManufactPlanner.ViewModels;

namespace ManufactPlanner.Views;

public partial class TaskDetailsPage : UserControl
{
    public TaskDetailsPage(MainWindowViewModel mainWindowViewModel, PostgresContext dbContext, int taskId)
    {
        InitializeComponent();
        DataContext = new TaskDetailsViewModel(mainWindowViewModel, dbContext, taskId);
    }
}