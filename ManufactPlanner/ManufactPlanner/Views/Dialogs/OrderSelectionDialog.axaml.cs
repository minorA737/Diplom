// Views/Dialogs/OrderSelectionDialog.axaml.cs
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using ManufactPlanner.ViewModels.Dialogs;
using ManufactPlanner.Models;

namespace ManufactPlanner.Views.Dialogs
{
    public partial class OrderSelectionDialog : Window
    {
        public OrderSelectionDialog()
        {
            InitializeComponent();
        }

        public OrderSelectionDialog(PostgresContext dbContext)
        {
            InitializeComponent();
            var viewModel = new OrderSelectionDialogViewModel(dbContext);
            DataContext = viewModel;

            // Важно: устанавливаем ссылку на окно в ViewModel
            viewModel.SetDialogWindow(this);
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}