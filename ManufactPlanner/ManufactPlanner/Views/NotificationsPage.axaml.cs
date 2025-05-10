// NotificationsPage.axaml.cs
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using ManufactPlanner.Models;
using ManufactPlanner.ViewModels;

namespace ManufactPlanner.Views
{
    public partial class NotificationsPage : UserControl
    {
        public NotificationsPage(MainWindowViewModel mainWindowViewModel, PostgresContext dbContext)
        {
            InitializeComponent();
            DataContext = new NotificationsPageViewModel(mainWindowViewModel, dbContext);
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}