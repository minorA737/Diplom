using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using ManufactPlanner.Models;
using ManufactPlanner.ViewModels;

namespace ManufactPlanner.Views;

public partial class UserManagementPage : UserControl
{
    public UserManagementPage(MainWindowViewModel mainWindowViewModel, PostgresContext dbContext)
    {
        InitializeComponent();
        DataContext = new UserManagementViewModel(mainWindowViewModel, dbContext);
    }
}