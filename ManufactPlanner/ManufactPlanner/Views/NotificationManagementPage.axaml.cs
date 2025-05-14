using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using ManufactPlanner.Models;
using ManufactPlanner.ViewModels;

namespace ManufactPlanner.Views
{
    public partial class NotificationManagementPage : UserControl
    {
        public NotificationManagementPage(MainWindowViewModel mainWindowViewModel, PostgresContext dbContext)
        {
            InitializeComponent();
            DataContext = new NotificationManagementViewModel(mainWindowViewModel, dbContext);
        }
    }
}